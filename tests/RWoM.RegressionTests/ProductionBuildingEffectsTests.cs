using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.Visuals;
using UnityEngine;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    public static IEnumerable<TestCase> ProductionBuildingEffectsCases()
    {
        yield return new("ProductionBuildingEffects.DefinitionsAndBoundedState", ProductionEffectsDefinitions);
        yield return new("ProductionBuildingEffects.DirectionalAnchors", _ => ProductionEffectsAnchors());
        yield return new("ProductionBuildingEffects.ActualWorkAndNativeFuel", f => World(f, ProductionEffectsActualWork));
        yield return new("ProductionBuildingEffects.WorkFadeAndLifecycle", f => World(f, ProductionEffectsLifecycle));
        yield return new("ProductionBuildingEffects.RitualActivity", f => World(f, ProductionEffectsRitual));
    }

    static CompProductionBuildingEffects ProductionEffect(ThingWithComps building, ProductionBuildingEffect effect)
    {
        var fx = new CompProductionBuildingEffects { parent = building, props = new CompProperties_ProductionBuildingEffects { effect = effect } };
        building.AllComps.Add(fx); fx.PostSpawnSetup(false); return fx;
    }

    static void ProductionEffectsDefinitions(RuntimeFixture f)
    {
        var expected = new Dictionary<string, (string Effect, string Class, string Size)>
        {
            ["TableGemcutting"] = ("Gemcutting", "Building_WorkTable", "(5,2)"),
            ["TableMagicPrinter"] = ("Scribing", "Building_WorkTable", "(1.6,1.6)"),
            ["TableArcaneForge"] = ("Forge", "TorannMagic.Building_TMArcaneForge", "(3.3,2.2)"),
            ["TableMagicCircle"] = ("MagicCircle", "TorannMagic.Building_TMMagicCircle", "(5.2,5.2)"),
            ["TableSmallMagicCircle"] = ("SmallMagicCircle", "TorannMagic.Building_TMSmallMagicCircle", "(5.2,5.2)")
        };
        var wired = Directory.GetFiles(Path.Combine(f.Repository, "v1.7/Defs"), "*.xml", SearchOption.AllDirectories)
            .SelectMany(p => XDocument.Load(p).Descendants("ThingDef"))
            .Where(d => d.Element("comps")?.Elements("li").Any(c => (string?)c.Attribute("Class") == "TorannMagic.Visuals.CompProperties_ProductionBuildingEffects") == true).ToArray();
        Check.Equal(5, wired.Length, "exactly the five approved buildings receive effects");
        foreach (var def in wired)
        {
            var contract = expected[def.Element("defName")!.Value];
            var comp = def.Element("comps")!.Elements("li").Single(c => (string?)c.Attribute("Class") == "TorannMagic.Visuals.CompProperties_ProductionBuildingEffects");
            Check.Equal(contract.Effect, comp.Element("effect")?.Value, "approved effect mapping");
            Check.Equal(contract.Class, def.Element("thingClass")?.Value, "native gameplay class preserved");
            Check.Equal(contract.Size, def.Element("graphicData")?.Element("drawSize")?.Value, "native sprite dimensions preserved");
            Check.Equal("Graphic_Multi", def.Element("graphicData")?.Element("graphicClass")?.Value, "native directional sprites preserved");
            Check.Equal(true, new[] { "RealtimeOnly", "MapMeshAndRealTime" }.Contains(def.Element("drawerType")?.Value), "comp receives real-time draws");
            Check.Equal(false, def.DescendantsAndSelf().Attributes().Any(a => a.Name.LocalName.StartsWith("MayRequire")), "effects require no DLC");
        }
        var renderer = typeof(CompProductionBuildingEffects).Assembly.GetType("TorannMagic.Visuals.ProductionBuildingEffectsRenderer", true)!;
        Check.Equal(true, renderer.IsDefined(typeof(StaticConstructorOnStartup), false), "render resources initialize on main thread");
        Check.Equal(1.4f, (float)renderer.GetField("Brightness")!.GetRawConstantValue()!, "approved brightness retained");
        Check.Equal(false, typeof(CompProductionBuildingEffects).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(x => typeof(Thing).IsAssignableFrom(x.FieldType) || typeof(Map).IsAssignableFrom(x.FieldType)), "visual state cannot retain worlds statically");
    }

    static void ProductionEffectsLifecycle(WorldState w)
    {
        var building = LegacyBuilding<Building_WorkTable>(w);
        var fx = ProductionEffect(building, ProductionBuildingEffect.Gemcutting);
        Check.Equal(0f, fx.Sample(0), "idle starts dark");
        Check.Equal(0f, fx.Sample(100), "time alone cannot manufacture work");
        for (int tick = 101; tick <= 166; tick++) { fx.NotifyWork(tick); fx.Sample(tick); }
        Check.Equal(true, Math.Abs(1 - fx.VisualStrength) < .00001f, "sustained real work fades to full strength");
        float halfway = fx.Sample(201);
        Check.Equal(true, halfway > 0 && halfway < 1, "stopped work gradually fades");
        Check.Equal(0f, fx.Sample(250), "stopped work settles completely dark");
        fx.NotifyWork(300); fx.Sample(300); fx.NotifyWork(366); fx.Sample(366);
        Set(building, "factionInt", new Faction { def = new FactionDef() });
        Check.Equal(0f, fx.Sample(367), "ownership change discards work and fade state");
        fx.NotifyWork(400); fx.Sample(400); fx.PostDeSpawn(w.Map);
        Check.Equal(-1, fx.WorkTick, "despawn clears work timestamp");
        Check.Equal(0f, fx.VisualStrength, "despawn clears visible strength");
        fx.NotifyWork(500); fx.PostSpawnSetup(true);
        Check.Equal(-1, fx.WorkTick, "load respawn cannot replay old work");
        fx.NotifyWork(600); fx.Sample(600);
        Check.Equal(0f, fx.Sample(599), "tick rewind clears future work");
        Check.Equal(-1, fx.WorkTick, "future work timestamp discarded");
        var flick = new CompFlickable { parent = building, props = new CompProperties_Flickable() };
        building.AllComps.Add(flick); Set(flick, "switchOnInt", false);
        fx.NotifyWork(700);
        Check.Equal(-1, fx.WorkTick, "disabled table rejects new work event");
        Check.Equal(0f, fx.Sample(766), "disabled table cannot glow");
    }

    static void ProductionEffectsActualWork(WorldState w)
    {
        using var patches = new Patches();
        patches.PatchType(typeof(ProductionBuildingWorkPatch));
        var unrelated = LegacyBuilding<Building_WorkTable>(w);
        unrelated.AllComps.Add(new FixtureComp { parent = unrelated, props = new CompProperties() });
        unrelated.UsedThisTick();
        Check.Equal(1, unrelated.AllComps.Count, "global hook leaves unrelated native tables alone");
        foreach (var effect in new[] { ProductionBuildingEffect.Gemcutting, ProductionBuildingEffect.Scribing, ProductionBuildingEffect.Forge })
        {
            Building_WorkTable table = effect == ProductionBuildingEffect.Forge ? LegacyBuilding<Building_TMArcaneForge>(w) : LegacyBuilding<Building_WorkTable>(w);
            var fuel = LegacyFuel(table, 80);
            ((CompProperties_Refuelable)fuel.props).fuelConsumptionRate = 60000;
            Set(table, "refuelableComp", fuel);
            var fx = ProductionEffect(table, effect);
            Set(Find.TickManager, "ticksGameInt", 100); fx.Sample(100);
            Check.Equal(-1, fx.WorkTick, "spawned fueled table is idle without native work");
            table.UsedThisTick();
            Check.Equal(100, fx.WorkTick, "actual native work callback emits production activity");
            Check.Equal(79f, fuel.Fuel, "native per-work fuel payment remains exactly once");
            for (int tick = 101; tick <= 166; tick++)
            {
                Set(Find.TickManager, "ticksGameInt", tick); table.UsedThisTick(); fx.Sample(tick);
            }
            Check.Equal(true, Math.Abs(1 - fx.VisualStrength) < .00001f, "actual sustained work enables approved effect");
            Check.Equal(13f, fuel.Fuel, "visual notification cannot duplicate resource payment");
            Check.Equal(0f, fx.Sample(233), "completed work cannot leave an idle ghost");
            Check.Equal(13f, fuel.Fuel, "sampling cannot consume resources");
        }
    }

    static void ProductionEffectsRitual(WorldState w)
    {
        using var patches = new Patches();
        foreach (var m in typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial")) patches.Prefix(m, nameof(SkipGraphics));
        foreach (bool small in new[] { false, true })
        {
            Building_TMMagicCircleBase circle = small ? LegacyBuilding<Building_TMSmallMagicCircle>(w) : LegacyBuilding<Building_TMMagicCircle>(w);
            var fx = ProductionEffect(circle, small ? ProductionBuildingEffect.SmallMagicCircle : ProductionBuildingEffect.MagicCircle);
            circle.IsPending = true;
            fx.NotifyWork(0); fx.Sample(0);
            Check.Equal(0f, fx.Sample(66), "pending ritual/work notification cannot activate circle");
            Set(circle, "isActive", true);
            Set(circle, "activeDuration", 100); Set(circle, "nextCircleEffect", 1000);
            Set(Find.TickManager, "ticksGameInt", 132);
            typeof(Building_TMMagicCircleBase).GetMethod("Tick", Flags)!.Invoke(circle, null);
            Check.Equal(1f, fx.Sample(132), "native active ritual enables full effect");
            Check.Equal(99, (int)Get(circle, "activeDuration")!, "overlay preserves native ritual countdown");
            Set(circle, "isActive", false);
            Check.Equal(.5f, fx.Sample(165), "ritual completion fades overlay");
            Check.Equal(0f, fx.Sample(198), "completed ritual stays dark despite pending flag");
            Check.Equal(true, circle.IsPending, "sampling never changes ritual state");
        }
    }

    static void ProductionEffectsAnchors()
    {
        var props = new CompProperties_ProductionBuildingEffects { effect = ProductionBuildingEffect.Gemcutting };
        Check.Equal(new Vector3(0,0,.28f), props.AnchorOffset(Rot4.North, new Vector2(5,2)), "north sparks sit on raised gemcutting work surface");
        Check.Equal(Vector3.zero, props.AnchorOffset(Rot4.South, new Vector2(5,2)), "south sprite uses its own surface center");
        var east = props.AnchorOffset(Rot4.East, new Vector2(2,5));
        var west = props.AnchorOffset(Rot4.West, new Vector2(2,5));
        Check.Equal(true, Math.Abs(east.x - .12f) < .00001f && Math.Abs(east.z - .15f) < .00001f, "horizontal sprite dimensions swap before anchor calculation");
        Check.Equal(-east.x, west.x, "east and west anchors match their mirrored work surfaces");
        Check.Equal(east.z, west.z, "east and west retain surface height");
        foreach (var effect in new[] { ProductionBuildingEffect.Forge, ProductionBuildingEffect.MagicCircle, ProductionBuildingEffect.SmallMagicCircle })
        {
            props.effect = effect;
            foreach (var rotation in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
                Check.Equal(Vector3.zero, props.AnchorOffset(rotation, new Vector2(5.2f,5.2f)), "centered effects preserve native sprite center");
        }
    }
}
