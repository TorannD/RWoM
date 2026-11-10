using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    public static IEnumerable<TestCase> TravelEnvironmentCases()
    {
        yield return new("TravelSafety.NativeVacuumTemperatureAndHeldPassenger", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map);
            env.Vacuum = 1; env.Temperature = 21;
            Check.Equal(true, w.Caster.HarmedByVacuum, "native pawn initially vulnerable");
            Check.Equal(TravelEnvironmentHazard.Vacuum, TravelEnvironmentSafety.Evaluate(w.Caster, w.Map, w.Patient.Position).Hazards, "actual destination vacuum rejected");
            env.Vacuum = .49f;
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "below native damage threshold safe");
            env.Vacuum = .5f;
            Check.Equal(false, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "native damage threshold inclusive");
            env.Vacuum = 0; env.Temperature = -10;
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "native cold tolerance includes ten-degree margin");
            env.Temperature = -10.01f;
            Check.Equal(TravelEnvironmentHazard.Cold, TravelEnvironmentSafety.Evaluate(w.Caster, w.Map, w.Patient.Position).Hazards, "colder than native range refused");
            env.Temperature = 40;
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "native upper margin inclusive");
            env.Temperature = 40.01f;
            Check.Equal(TravelEnvironmentHazard.Heat, TravelEnvironmentSafety.Evaluate(w.Caster, w.Map, w.Patient.Position).Hazards, "hotter than native range refused");
            Set(w.Caster, "mapIndexOrState", (sbyte)-1); env.Temperature = 21;
            Check.Equal<Map?>(null, w.Caster.Map, "held passenger has no direct map");
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "explicit destination map handles held passenger");
            env.Vacuum = 1;
            Check.Equal(false, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "held passenger still rejects vacuum");
            env.Odyssey = false;
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "no DLC ignores vacuum");
            env.Temperature = 50;
            Check.Equal(TravelEnvironmentHazard.Heat, TravelEnvironmentSafety.Evaluate(w.Caster, w.Map, w.Patient.Position).Hazards, "temperature remains native without DLC");
        }));
        yield return new("TravelSafety.ShellExpiryProjectionDoesNotMutateOrPolluteCaches", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map);
            var shell = env.Shell(601); env.Vacuum = 1; env.Temperature = -100;
            Check.Equal(1f, w.Caster.GetStatValue(StatDefOf.VacuumResistance), "live shell immunity cached");
            Check.Equal(-300f, w.Caster.GetStatValue(StatDefOf.ComfyTemperatureMin), "live cold stat cached");
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "601 ticks exceeds default margin");
            Check.Equal(false, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position, 1), "horizon expands inclusive expiry margin");
            shell.ticksToDisappear = 600;
            var result = TravelEnvironmentSafety.Evaluate(w.Caster, w.Map, w.Patient.Position);
            Check.Equal(true, result.ShellExpiring, "600 tick boundary projects expiry");
            Check.Equal(TravelEnvironmentHazard.Vacuum | TravelEnvironmentHazard.Cold, result.Hazards, "actual post-shell hazards evaluated");
            Check.Equal(1, w.Caster.health.hediffSet.hediffs.Count, "projection never removes shell");
            Check.Equal(600, shell.ticksToDisappear, "projection never decrements timer");
            Check.Equal(1f, w.Caster.GetStatValue(StatDefOf.VacuumResistance), "projected result does not poison live stat cache");
            Check.Equal(-300f, w.Caster.GetStatValue(StatDefOf.ComfyTemperatureMin), "cold cache stays protected");
            Check.Equal(1f, shell.parent.CurStage.statOffsets.Single(s => s.stat == StatDefOf.VacuumResistance).value, "shared shell stage unchanged");
            Check.Equal(-300f, shell.parent.CurStage.statOffsets.Single(s => s.stat == StatDefOf.ComfyTemperatureMin).value, "shared cold stage unchanged");
            env.Vacuum = 0; env.Temperature = 21;
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "expiring shell does not ban already-safe room");
        }));
        yield return new("TravelSafety.ExpiringShellPreservesIndependentEquipmentAndAtmosphere", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map);
            env.Shell(100); env.Vacuum = 1; env.Temperature = -100;
            var suit = env.WearProtection(1f, -300f);
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "independent equipment remains fully protective after projected shell expiry");
            Check.Equal(1f, StatDefOf.VacuumResistance.Worker.GetValue(StatRequest.For(w.Caster)), "final resistance is clamped despite stacked shell and suit");
            suit.def.equippedStatOffsets.Single(s => s.stat == StatDefOf.VacuumResistance).value = .75f;
            Check.Equal(TravelEnvironmentHazard.Vacuum, TravelEnvironmentSafety.Evaluate(w.Caster, w.Map, w.Patient.Position).Hazards, "partial .75 equipment is not full damage immunity");
            env.Overlay(w.Patient.Position, 1f);
            Check.Equal(0f, w.Patient.Position.GetVacuum(w.Map), "native query includes actual magical atmosphere overlay");
            Check.Equal(true, TravelEnvironmentSafety.IsSafe(w.Caster, w.Map, w.Patient.Position), "overlay-safe landing admitted despite partial equipment");
        }));
    }
}

// Native map/room/stat backing shared by environment and automatic-travel fixtures.
// Only renderer initialization is omitted; no safety, stat, vacuum, or temperature query is replaced.
internal sealed class TravelEnvironmentFixture : IDisposable
{
    readonly Pawn pawn; readonly Map map; readonly Room room;
    readonly StatDef[] oldDefs = DefDatabase<StatDef>.AllDefsListForReading.ToArray();
    readonly StatDef oldVacuum = StatDefOf.VacuumResistance, oldMin = StatDefOf.ComfyTemperatureMin, oldMax = StatDefOf.ComfyTemperatureMax;
    readonly FieldInfo dlc = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
    readonly object? oldDlc;
    readonly string suffix = Guid.NewGuid().ToString("N");
    readonly Assembly harmonyAssembly = Assembly.Load("0Harmony"); readonly object harmony; readonly string patchId = "rwom.travel-environment." + Guid.NewGuid();
    public bool Odyssey { set => dlc.SetValue(null, value); }
    public bool Space { set => Set(map.pocketTileInfo, "biome", new BiomeDef { inVacuum = value }); }
    public float Vacuum { set => Set(room, "vacuum", value); }
    public float Temperature { set => room.TempTracker.Temperature = value; }
    public TravelEnvironmentFixture(Pawn pawn, Map map)
    {
        this.pawn = pawn; this.map = map; oldDlc = dlc.GetValue(null); Odyssey = true;
        StatDefOf.VacuumResistance = Stat("TravelVacuum", 0, 0, 1);
        StatDefOf.ComfyTemperatureMin = Stat("TravelCold", 0, -1000, 1000);
        StatDefOf.ComfyTemperatureMax = Stat("TravelHeat", 30, -1000, 1000);
        pawn.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef() }];
        Set(pawn.def.race, "fleshType", new FleshTypeDef { defName = "TravelOrganic", isOrganic = true });
        pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
        pawn.apparel = new Pawn_ApparelTracker(pawn);
        map.info.isPocketMap = true; map.pocketTileInfo = new SurfaceTile(); Set(map.pocketTileInfo, "biome", new BiomeDef { inVacuum = true });
        map.regionDirtyer = new RegionDirtyer(map); map.regionAndRoomUpdater = new RegionAndRoomUpdater(map); Set(map.regionAndRoomUpdater, "initialized", true);
        map.regionGrid = new RegionGrid(map); map.roofGrid = new RoofGrid(map); map.components = [];
        room = Blank<Room>(); var district = new District { mapIndex = 0 };
        var region = Blank<Region>(); region.valid = true; region.mapIndex = 0; region.type = RegionType.Normal; region.extentsClose = new CellRect(0,0,map.Size.x,map.Size.z);
        district.Regions.Add(region); Set(region, "districtInt", district); Set(district, "roomInt", room);
        Set(room, "districts", new List<District> { district }); Set(room, "cachedExposedCount", 0); Set(room, "cachedExposedThreshold", 1);
        Set(room, "tempTracker", Blank<RoomTempTracker>());
        Array.Fill(map.regionGrid.DirectGrid, region); Temperature = 21;
        harmony = Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.Harmony")!, patchId)!;
        Patch(typeof(TravelEnvironmentShellProjectionPatch)); Patch(typeof(MagicalAtmosphereVacuumPatch));
    }
    StatDef Stat(string name, float value, float min, float max)
    {
        var stat = new StatDef { defName = name + suffix, defaultBaseValue = value, minValue = min, maxValue = max, cacheable = true };
        DefDatabase<StatDef>.Add(stat); return stat;
    }
    void Patch(Type patch)
    {
        var processor = harmony.GetType().GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
        processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
    }
    public HediffComp_Disappears Shell(int ticks)
    {
        var def = new HediffDef { defName = "TM_EtherShellHD", hediffClass = typeof(Hediff_EtherShell), stages = [new HediffStage { statOffsets = [
            new StatModifier { stat = StatDefOf.VacuumResistance, value = 1 }, new StatModifier { stat = StatDefOf.ComfyTemperatureMin, value = -300 }] }] };
        var shell = new Hediff_EtherShell { pawn = pawn, def = def };
        var timer = new HediffComp_Disappears { parent = shell, props = new HediffCompProperties_Disappears(), ticksToDisappear = ticks };
        shell.comps = [timer]; pawn.health.hediffSet.hediffs.Add(shell); return timer;
    }
    public Apparel WearProtection(float vacuum, float cold)
    {
        var def = Blank<ThingDef>(); def.defName = "IndependentTravelSuit"; def.category = ThingCategory.Item; def.apparel = new ApparelProperties();
        def.equippedStatOffsets = [new StatModifier { stat = StatDefOf.VacuumResistance, value = vacuum }, new StatModifier { stat = StatDefOf.ComfyTemperatureMin, value = cold }];
        var suit = new Apparel { def = def }; pawn.apparel.WornApparel.Add(suit); return suit;
    }
    public void Overlay(IntVec3 cell, float strength)
    {
        var field = new MapComponent_MagicalAtmosphere(map); Set(field, "strength", new Dictionary<IntVec3,float> { [cell] = strength }); Set(field, "lastPulse", Find.TickManager.TicksGame);
        map.components.Add(field);
    }
    public void Dispose()
    {
        harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [patchId]);
        dlc.SetValue(null, oldDlc); StatDefOf.VacuumResistance = oldVacuum; StatDefOf.ComfyTemperatureMin = oldMin; StatDefOf.ComfyTemperatureMax = oldMax;
        DefDatabase<StatDef>.Clear(); foreach (var def in oldDefs) DefDatabase<StatDef>.Add(def);
    }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static void Set(object target, string name, object? value)
    {
        for (Type? t = target.GetType(); t != null; t = t.BaseType)
            if (t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is { } field) { field.SetValue(target, value); return; }
        throw new InvalidOperationException("Missing travel fixture field " + name);
    }
}
