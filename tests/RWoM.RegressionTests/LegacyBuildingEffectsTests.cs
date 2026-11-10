using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.Visuals;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    public static IEnumerable<TestCase> LegacyBuildingEffectsCases()
    {
        yield return new("LegacyBuildingEffects.DefinitionsAndNativeRotation", LegacyEffectsDefinitions);
        yield return new("LegacyBuildingEffects.TransientLifecycleAndReadOnlyCharge", f => World(f, LegacyEffectsLifecycle));
        foreach (bool pocket in new[] { false, true })
        {
            bool isPocket = pocket;
            yield return new("LegacyBuildingEffects.ActualManaTransfer." + (pocket ? "Pocket" : "Capacitor"), f => World(f, w => LegacyEffectsTransfer(w, isPocket)));
        }
        yield return new("LegacyBuildingEffects.ActualPortalMoveAndPayment", f => World(f, LegacyEffectsPortal));
    }

    static CompLegacyBuildingEffects LegacyEffect(ThingWithComps building, LegacyBuildingEffect kind)
    {
        var fx = new CompLegacyBuildingEffects { parent = building, props = new CompProperties_LegacyBuildingEffects { effect = kind } };
        if (building.AllComps == null) Set(building, "comps", new List<ThingComp>());
        building.AllComps!.Add(fx); fx.PostSpawnSetup(false); return fx;
    }
    static T LegacyBuilding<T>(WorldState w) where T : Building, new()
    {
        var def = Blank<ThingDef>(); def.defName = "LegacyEffectsFixture"; def.category = ThingCategory.Building;
        def.size = new IntVec2(1,1); def.building = new BuildingProperties();
        var building = new T { def = def };
        Set(building, "positionInt", w.Caster.Position); Set(building, "mapIndexOrState", (sbyte)0);
        Set(building, "factionInt", w.Player); Set(building, "comps", new List<ThingComp>());
        return building;
    }
    static CompRefuelable LegacyFuel(ThingWithComps building, float fuel)
    {
        using var patches = new Patches();
        foreach (var m in typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial")) patches.Prefix(m, nameof(SkipGraphics));
        var comp = new CompRefuelable { parent = building, props = new CompProperties_Refuelable { fuelCapacity = 100 } };
        Set(comp, "fuel", fuel); building.AllComps.Add(comp); return comp;
    }
    static void LegacyEffectsDefinitions(RuntimeFixture f)
    {
        var found = Directory.GetFiles(Path.Combine(f.Repository, "v1.7/Defs"), "*.xml", SearchOption.AllDirectories)
            .SelectMany(path => XDocument.Load(path).Descendants("ThingDef"))
            .Where(d => d.Element("comps")?.Elements("li").Any(c => (string?)c.Attribute("Class") == "TorannMagic.Visuals.CompProperties_LegacyBuildingEffects") == true).ToArray();
        Check.Equal(3, found.Length, "three legacy buildings wired without DLC dependency");
        foreach (var def in found)
        {
            Check.Equal(true, new[] { "MapMeshAndRealTime", "RealtimeOnly" }.Contains(def.Element("drawerType")?.Value), "overlay receives native draw callback");
            Check.Equal(false, def.Attributes().Any(a => a.Name.LocalName.StartsWith("MayRequire")), "legacy effect has no DLC gate");
        }
        var renderer = typeof(CompLegacyBuildingEffects).Assembly.GetType("TorannMagic.Visuals.LegacyBuildingEffectsRenderer", true)!;
        Check.Equal(true, renderer.IsDefined(typeof(StaticConstructorOnStartup), false), "Unity resources initialized on main thread");
        Check.Equal(false, typeof(CompLegacyBuildingEffects).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(x => typeof(Thing).IsAssignableFrom(x.FieldType) || typeof(Map).IsAssignableFrom(x.FieldType)), "visual state cannot retain worlds statically");
        var props = new CompProperties_LegacyBuildingEffects(); var native = new Graphic_Single();
        foreach (var rot in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
        {
            var bias = new Vector3(.2f, .04f, -.1f);
            Check.Equal(bias, props.WorldOffset(Vector3.zero, native.QuatFromRot(rot), bias), "vortex remains at native sprite center " + rot);
        }
        for (int i=0; i<5; i++)
        {
            var regular = props.RuneOffset(i, new Vector2(3,3)); var flipped = props.RuneOffset(i, new Vector2(3,3), true);
            Check.Equal(regular.x, -flipped.x, "west fallback mirrors original rune texture anchor");
            Check.Equal(regular.z, flipped.z, "mirroring preserves height");
        }
    }
    static void LegacyEffectsLifecycle(WorldState w)
    {
        var building = LegacyBuilding<Building_TMArcaneCapacitor>(w); var fuel = LegacyFuel(building, 50);
        var flick = new CompFlickable { parent = building, props = new CompProperties_Flickable() }; building.AllComps.Add(flick);
        Set(flick, "switchOnInt", true); var fx = LegacyEffect(building, LegacyBuildingEffect.Capacitor);
        Check.Equal(.5f, fx.Charge, "charge follows real fuel fraction");
        Check.Equal(0f, fx.Sample(0), "steady glow starts dark");
        Check.Equal(.5f, fx.Sample(66), "steady glow reaches actual charge");
        Set(flick, "switchOnInt", false);
        Check.Equal(true, fx.Sample(82) > 0 && fx.VisualCharge < .5f, "off gradually fades steady glow");
        Check.Equal(0f, fx.Sample(99), "off fade settles dark");
        Set(flick, "switchOnInt", true); fx.Sample(132);
        Set(fuel, "fuel", 0f);
        Check.Equal(true, fx.Sample(148) > 0 && fx.VisualCharge < .5f, "empty storage gradually fades steady glow");
        Check.Equal(0f, fx.Sample(165), "empty storage fade completes");
        Set(fuel, "fuel", 50f); fx.PostSpawnSetup(false);
        foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) fx.NotifyTransfer(amount, 100);
        Check.Equal(-1, fx.TransferTick, "invalid or zero credits do not flash");
        fx.NotifyTransfer(.01f, 100); fx.NotifyTransfer(.01f, 100);
        Check.Equal(1f, fx.Pulse(100), "same tick transfers coalesce");
        Check.Equal(.5f, fx.Pulse(133), "bounded pulse fades"); Check.Equal(0f, fx.Pulse(166), "pulse completes");
        Set(fuel, "fuel", 0f); fx.NotifyTransfer(.01f, 200);
        Check.Equal(0f, fx.Charge, "empty storage idle dark"); Check.Equal(1f, fx.Pulse(200), "last paid transfer survives empty storage");
        Set(flick, "switchOnInt", false); Check.Equal(0f, fx.Pulse(201), "disabled state suppresses pulse");
        fx.NotifyTransfer(1, 202); Check.Equal(-1, fx.TransferTick, "off cannot start pulse");
        Set(flick, "switchOnInt", true); fx.NotifyTransfer(1, 210);
        Set(building, "factionInt", new Faction { def = new FactionDef() }); Check.Equal(0f, fx.Pulse(210), "ownership transition clears events");
        fx.NotifyTransfer(1, 220); fx.PostDeSpawn(w.Map); Check.Equal(-1, fx.TransferTick, "despawn resets event");
        fx.NotifyTransfer(1, 230); fx.PostSpawnSetup(true); Check.Equal(-1, fx.TransferTick, "load respawn cannot replay event");
        Check.Equal(0f, fx.VisualCharge, "load respawn clears transient rendered charge");
        fx.NotifyTransfer(1, 240); Check.Equal(0f, fx.Pulse(239), "rewind clears future event");
        Check.Equal(0f, fuel.Fuel, "all state sampling leaves fuel untouched");
    }
    static bool LegacyBuildingDrawPos(Thing __instance, ref Vector3 __result) { __result = __instance.Position.ToVector3Shifted(); return false; }
    static void LegacyEffectsTransfer(WorldState w, bool pocket)
    {
        using var environment = new TravelEnvironmentFixture(w.Caster, w.Map) { Odyssey = false, Space = false };
        Check.Equal(false, ModsConfig.OdysseyActive, "legacy transfer and visual comp run without Odyssey DLC");
        using var patches = w.MovementBoundaries();
        patches.Prefix(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowManaPuff))!, nameof(SkipGraphics));
        patches.Prefix(typeof(Thing).GetProperty(nameof(Thing.DrawPos))!.GetMethod!, nameof(LegacyBuildingDrawPos));
        var magic = new CompAbilityUserMagic { parent = w.Caster }; w.Caster.AllComps.Add(magic); TM_Calc.InvalidateCompCache(w.Caster);
        w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
        var mana = Blank<Need_Mana>(); Set(mana, "pawn", w.Caster); w.Caster.needs.AllNeeds.Add(mana);
        var building = pocket ? (Building)LegacyBuilding<Building_TM_DMP>(w) : LegacyBuilding<Building_TMArcaneCapacitor>(w);
        var fuel = LegacyFuel(building, 80); var transfer = building.GetType().GetMethod("TransferMana", Flags)!;
        void Transfer() => transfer.Invoke(building, pocket ? new object[] { magic } : new object[] { magic, 1f });
        mana.CurLevel = .5f; Transfer(); float baselineMana = mana.CurLevel, baselineFuel = fuel.Fuel;
        var fx = LegacyEffect(building, pocket ? LegacyBuildingEffect.Pocket : LegacyBuildingEffect.Capacitor);
        Set(fuel, "fuel", 80f); mana.CurLevel = .5f; Set(Find.TickManager, "ticksGameInt", 100); Transfer();
        Check.Equal(baselineMana, mana.CurLevel, "visual hook preserves actual legacy mana result");
        Check.Equal(baselineFuel, fuel.Fuel, "visual hook preserves native Refuel setter semantics");
        Check.Equal(100, fx.TransferTick, "actual positive receipt emits pulse");
        mana.CurLevel = float.MaxValue; Set(Find.TickManager, "ticksGameInt", 200); Transfer();
        Check.Equal(100, fx.TransferTick, "clamped full mana cannot fake receipt");
        mana.CurLevel -= .01f; Set(Find.TickManager, "ticksGameInt", 300); Transfer();
        Check.Equal(300, fx.TransferTick, "partial capped receipt still emits pulse");
    }

    static void LegacyEffectsPortal(WorldState w)
    {
        using var environment = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1 };
        using var patches = ManualBoundaries();
        foreach (var m in typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial")) patches.Prefix(m, nameof(SkipGraphics));
        patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!, nameof(PortalNativeDespawnBoundary));
        patches.Prefix(typeof(GenSpawn).GetMethods().Single(m => m.Name == nameof(GenSpawn.Spawn) && m.GetParameters().Length == 7 && m.GetParameters()[0].ParameterType == typeof(Thing)), nameof(PortalNativeSpawnBoundary));
        patches.Prefix(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.EndCurrentJob))!, nameof(PortalRejectJobBoundary));
        patches.Prefix(typeof(Pawn).GetProperty(nameof(Pawn.DrawPos))!.GetMethod!, nameof(DrawPos));
        patches.Prefix(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowCastingMote))!, nameof(PortalVisualFailureBoundary));
        patches.Prefix(typeof(TorannMagic.Compatibility.TM_Log).GetMethod("Warning", [typeof(string), typeof(string)])!, nameof(PortalExpectedVisualWarning));
        var oldJob = TorannMagicDefOf.UsePortal;
        try
        {
            TorannMagicDefOf.UsePortal = new JobDef { defName = "LegacyEffectsPortalJob" };
            var portal = LegacyBuilding<Building_TMPortal>(w); portal.IsPaired = true;
            portal.PortalDestinationMap = w.Map; portal.PortalDestinationPosition = new IntVec3(15,0,15); Set(portal, "arcaneEnergyCur", .05f);
            var fx = LegacyEffect(portal, LegacyBuildingEffect.Portal); var tick = typeof(Building_TMPortal).GetMethod("Tick", Flags)!;
            portalBoundarySpawns = portalBoundaryDespawns = portalRejectedJobs = portalVisualFailures = 0;
            Set(Find.TickManager, "ticksGameInt", 100);
            Set(w.Caster.jobs, "curJob", new Job(TorannMagicDefOf.UsePortal, portal)); tick.Invoke(portal, null);
            Check.Equal(-1, fx.TransferTick, "environment refusal does not flash"); Check.Equal(.05f, portal.ArcaneEnergyCur, "refusal costs no energy");
            var accepted = new Job(TorannMagicDefOf.UsePortal, portal);
            ManualTravelSafety.Submit(w.Caster, accepted, w.Map, portal.PortalDestinationPosition, true, () => true, () => Set(w.Caster.jobs, "curJob", accepted));
            Check.Equal(-1, fx.TransferTick, "warning awaiting acceptance does not flash"); Check.Equal(0, portalBoundarySpawns, "cancelled warning does not move");
            manualContinuation!(); tick.Invoke(portal, null);
            Check.Equal(1, portalBoundarySpawns, "real accepted portal movement completes");
            Check.Equal(100, fx.TransferTick, "actual successful move flashes even if native mote throws");
            Check.Equal(0f, portal.ArcaneEnergyCur, "last transfer pays exactly once"); Check.Equal(0f, fx.Charge, "empty portal idle dark");
            Check.Equal(1f, fx.Pulse(100), "last paid portal transfer retains pulse");
            tick.Invoke(portal, null); Check.Equal(1, portalBoundarySpawns, "completed order cannot replay"); Check.Equal(0f, portal.ArcaneEnergyCur, "no double payment");
            portal.IsPaired = false; Check.Equal(0f, fx.Pulse(101), "unpaired portal suppresses pulse");
            portal.IsPaired = true; Set(portal, "arcaneEnergyCur", 1f); fx.PostSpawnSetup(false);
            fx.Sample(200); Check.Equal(1f, fx.Sample(266), "paired charged portal reaches steady glow");
            portal.IsPaired = false; Check.Equal(.5f, fx.Sample(299), "unpairing fades steady glow");
            Check.Equal(0f, fx.Sample(332), "unpaired portal settles dark");
        }
        finally { TorannMagicDefOf.UsePortal = oldJob; }
    }
}


