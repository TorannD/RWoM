using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.Golems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class GolemEnergyTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GolemEnergy.UpgradedBatterySurvivesSaveLoad", SaveLoad);
        yield return new("GolemEnergy.ActivationUsesUpgradedCapacity", Activate);
        yield return new("GolemEnergy.LowPowerGolemKeepsWalkingHome", LowPowerWalking);
        yield return new("GolemEnergy.LowPowerUnreachableHomeKeepsGolemIntact", LowPowerUnreachable);
        yield return new("GolemEnergy.ImpossibleHomeKeepsEmergencyFlag", ImpossibleHome);
        yield return new("GolemEnergy.EmergencyCollapseAvoidsCrushingFurniture", CollapseCell);
        yield return new("GolemEnergy.ArrivalAtRestSpotStillDespawns", Arrival);
    }

    // --- workshop #44: an out-of-energy golem must walk home, never collapse on
    // top of storage or furniture ---------------------------------------------

    private static int reachResult;
    private static bool InterceptCanReach(Pawn pawn, ref bool __result)
    {
        __result = reachResult == 0;
        return false;
    }

    private static void LowPowerWalking(RuntimeFixture fixture)
    {
        var despawnDef = new JobDef { defName = "JobDriver_GolemDespawn" };
        JobDef oldDespawn = TorannMagicDefOf.JobDriver_GolemDespawn;
        TorannMagicDefOf.JobDriver_GolemDespawn = despawnDef;
        try
        {
            using var world = new GolemWorld(fixture);
            var job = new Job { def = despawnDef, targetA = new IntVec3(10, 0, 10) };
            FleshStatTests.Set(world.Golem.jobs, "curJob", job);
            world.Comp.CompTick();
            Check.Equal(true, world.Golem.Spawned, "a walking low-power golem is not despawned mid-route");
            Check.Equal(true, world.Comp.shouldDespawn, "low power keeps the despawn intention");
            Check.Equal(false, world.Comp.despawnNow, "a reachable home never flags an emergency collapse");
            Check.Same(job, world.Golem.CurJob, "the in-progress walk home is not interrupted");
        }
        finally { TorannMagicDefOf.JobDriver_GolemDespawn = oldDespawn; }
    }

    private static void LowPowerUnreachable(RuntimeFixture fixture)
    {
        using var world = new GolemWorld(fixture);
        world.WithReachPatch(() =>
        {
            reachResult = 1; // no path to the rest spot exists right now
            world.Comp.CompTick();
        });
        Check.Equal(true, world.Golem.Spawned, "a blocked path never collapses the golem");
        Check.Equal(true, world.Comp.shouldDespawn, "the despawn intention survives while the home is blocked");
        Check.Equal(false, world.Comp.despawnNow, "a temporarily blocked home is not an emergency");
        Check.Equal(null, world.Golem.CurJob, "no forced job churn while the home is unreachable");
    }

    private static void ImpossibleHome(RuntimeFixture fixture)
    {
        using var world = new GolemWorld(fixture);
        world.Comp.dormantMap = null; // the rest spot lives on a forgotten map
        CallPrivate(world.Comp, "ShutdownGolemLowPower");
        Check.Equal(true, world.Comp.despawnNow, "a home that can never be reached still flags the emergency shutdown");
        Check.Equal(false, world.Comp.shouldDespawn, "no walk-home intention without a reachable home");
    }

    private static void CollapseCell(RuntimeFixture fixture)
    {
        using var world = new GolemWorld(fixture);
        var station = Blank<ThingDef>();
        station.category = ThingCategory.Building; station.size = new IntVec2(1, 1);
        IntVec3 Spot() => (IntVec3)CallPrivate(world.Comp, "SafeCollapseCell", world.Map, station);
        Check.Equal(new IntVec3(5, 0, 5), Spot(), "a clear cell collapses the golem exactly where it stands");
        world.PlaceEdifice(new IntVec3(5, 0, 5));
        var alternative = Spot();
        Check.Equal(true, alternative.IsValid, "a blocked cell redirects the emergency collapse");
        Check.Equal(true, alternative != new IntVec3(5, 0, 5), "the collapse never lands on the occupied cell");
        Check.Equal(true, alternative.Standable(world.Map), "the redirected collapse lands on a standable cell");
        Check.Equal(true, world.Map.edificeGrid[alternative] == null, "the redirected collapse lands on a clear cell");
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                if (dx != 0 || dz != 0)
                    world.PlaceEdifice(new IntVec3(5 + dx, 0, 5 + dz));
        Check.Equal(IntVec3.Invalid, Spot(), "a surrounded golem has no safe placement");
    }

    private static void Arrival(RuntimeFixture fixture)
    {
        using var world = new GolemWorld(fixture);
        world.Comp.shouldDespawn = true;
        world.Comp.dormantPosition = new IntVec3(5, 0, 5); // the golem's own cell
        Check.Equal(true, (bool)CallPrivate(world.Comp, "ShouldDespawnNow")!, "arrival at the rest spot still triggers the shutdown");
        world.Comp.dormantPosition = new IntVec3(10, 0, 10);
        Check.Equal(false, (bool)CallPrivate(world.Comp, "ShouldDespawnNow")!, "the shutdown waits until the golem arrives");
    }

    private static object CallPrivate(object target, string name, params object[] args)
    {
        var type = typeof(CompGolem);
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method != null) return method.Invoke(target, args)!;
        return type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    }

    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private sealed class GolemPawnComp : ThingComp { }

    private sealed class GolemWorld : IDisposable
    {
        private static readonly MethodInfo Headless = typeof(SpellItemEffectTests).GetMethod("HeadlessDefOf",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        public readonly Map Map;
        public readonly Pawn Golem;
        public readonly CompGolem Comp;
        public readonly Need_GolemEnergy Need;
        private readonly Game game, oldGame;
        private readonly NeedDef needDef, oldNeed;
        private readonly PawnCapacityDef oldMoving, oldConsciousness;
        private readonly CapacityScope movingScope, consciousnessScope;

        public GolemWorld(RuntimeFixture fixture)
        {
            oldGame = Current.Game;
            // HeadlessDefOf must run before any DefOf static is read, or its class
            // constructor logs an "uninitialized DefOf" warning into the clean log.
            Headless.Invoke(null, [typeof(PawnCapacityDefOf)]);
            oldMoving = PawnCapacityDefOf.Moving;
            oldConsciousness = PawnCapacityDefOf.Consciousness;
            movingScope = new CapacityScope(new PawnCapacityDef { defName = "GolemEnergyMoving", minForCapable = .1f });
            consciousnessScope = new CapacityScope(new PawnCapacityDef { defName = "GolemEnergyConsciousness", minForCapable = .1f });
            PawnCapacityDefOf.Moving = movingScope.Definition;
            PawnCapacityDefOf.Consciousness = consciousnessScope.Definition;
            game = Blank<Game>();
            FleshStatTests.Set(game, "tickManager", new TickManager());
            FleshStatTests.Set(game, "uniqueIDsManager", new UniqueIDsManager());
            Current.Game = game;
            Map = Blank<Map>();
            Map.info = new MapInfo { Size = new IntVec3(24, 1, 24) };
            FleshStatTests.Set(game, "maps", new List<Map> { Map });
            Map.cellIndices = new CellIndices(Map);
            Map.thingGrid = new ThingGrid(Map);
            Map.edificeGrid = new EdificeGrid(Map);
            // Real native path storage, initially zero-cost; Standable/Walkable run
            // against it without any terrain or path-definition registration.
            var pathGrid = new PathGrid(Map, new PathGridDef { defName = "GolemEnergyFixturePath" });
            Map.pathing = Blank<Pathing>();
            var context = new PathingContext(Map, pathGrid);
            FleshStatTests.Set(Map.pathing, "normal", context);
            FleshStatTests.Set(Map.pathing, "fenceBlocked", context);
            var race = Blank<ThingDef>();
            race.defName = "GolemEnergyPawn"; race.category = ThingCategory.Pawn;
            race.size = new IntVec2(1, 1); race.passability = Traversability.Standable;
            race.race = new RaceProperties { intelligence = Intelligence.Humanlike };
            Golem = new Pawn { def = race, kindDef = new PawnKindDef { defName = "GolemEnergyPawn", race = race } };
            Golem.health = new Pawn_HealthTracker(Golem);
            Golem.jobs = new Pawn_JobTracker(Golem);
            Golem.needs = new Pawn_NeedsTracker(Golem);
            // 1.7 GetComp unconditionally indexes comps[0]; a real dummy keeps
            // attachment checks (IsBurning) from throwing on an empty list.
            FleshStatTests.Set(Golem, "comps", new List<ThingComp> { new GolemPawnComp() });
            FleshStatTests.Set(Golem, "positionInt", new IntVec3(5, 0, 5));
            FleshStatTests.Set(Golem, "mapIndexOrState", (sbyte)0);
            Map.thingGrid.Register(Golem);
            CacheCapacities();
            needDef = new NeedDef { defName = "TM_GolemEnergy", needClass = typeof(Need_GolemEnergy) };
            Need = (Need_GolemEnergy)RuntimeHelpers.GetUninitializedObject(typeof(Need_GolemEnergy));
            Need.def = needDef; Need.maxEnergy = 1000f; Need.energyEfficiency = .5f;
            Golem.needs.AllNeeds.Add(Need);
            oldNeed = TorannMagicDefOf.TM_GolemEnergy;
            TorannMagicDefOf.TM_GolemEnergy = needDef;
            Comp = new CompGolem { parent = Golem };
            Comp.age = 1;
            FleshStatTests.Set(Comp, "initialized", true);
            FleshStatTests.Set(Comp, "nextActionTick", int.MaxValue);
            Comp.dormantMap = Map;
            Comp.dormantPosition = new IntVec3(10, 0, 10);
        }

        public void WithReachPatch(Action body)
        {
            var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
            var hookType = Assembly.Load("0Harmony").GetType("HarmonyLib.HarmonyMethod")!;
            string id = "rwom.golem-energy." + Guid.NewGuid();
            var harmony = Activator.CreateInstance(harmonyType, id)!;
            var hook = Activator.CreateInstance(hookType,
                typeof(GolemEnergyTests).GetMethod(nameof(InterceptCanReach), BindingFlags.NonPublic | BindingFlags.Static)!)!;
            var method = typeof(ReachabilityUtility).GetMethods().Single(m => m.Name == "CanReach"
                && m.GetParameters().Length == 7 && m.GetParameters()[3].ParameterType == typeof(Danger));
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [method, hook, null, null, null]);
            try { body(); }
            finally { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
        }

        public void PlaceEdifice(IntVec3 cell)
        {
            var def = Blank<ThingDef>();
            def.defName = "GolemEnergyFurniture"; def.size = new IntVec2(1, 1); def.fillPercent = 1f;
            def.passability = Traversability.Impassable; def.category = ThingCategory.Building;
            var building = new Building { def = def };
            FleshStatTests.Set(building, "positionInt", cell);
            FleshStatTests.Set(building, "mapIndexOrState", (sbyte)0);
            Map.thingGrid.Register(building);
            Map.edificeGrid.Register(building);
        }

        private void CacheCapacities()
        {
            var cache = Activator.CreateInstance(CapacityCacheField.FieldType)!;
            foreach (var capacity in new[] { PawnCapacityDefOf.Moving, PawnCapacityDefOf.Consciousness })
            {
                var entry = CapacityCacheField.FieldType.GetProperty("Item", [typeof(PawnCapacityDef)])!
                    .GetValue(cache, [capacity])!;
                FleshStatTests.Set(entry, "status", Enum.Parse(entry.GetType().GetField("status")!.FieldType, "Cached"));
                FleshStatTests.Set(entry, "value", 1f);
            }
            CapacityCacheField.SetValue(Golem.health.capacities, cache);
        }

        public void Dispose()
        {
            TorannMagicDefOf.TM_GolemEnergy = oldNeed;
            PawnCapacityDefOf.Moving = oldMoving;
            PawnCapacityDefOf.Consciousness = oldConsciousness;
            consciousnessScope.Dispose();
            movingScope.Dispose();
            Current.Game = oldGame;
        }

        // DefMap's constructor throws on an empty database, so the capacity defs the
        // health cache needs must live in DefDatabase for the fixture's lifetime.
        private sealed class CapacityScope : IDisposable
        {
            public PawnCapacityDef Definition { get; }
            private readonly PawnCapacityDef[] old;
            public CapacityScope(PawnCapacityDef definition)
            {
                Definition = definition;
                old = DefDatabase<PawnCapacityDef>.AllDefsListForReading.Where(d => d.defName != definition.defName).ToArray();
                DefDatabase<PawnCapacityDef>.Clear();
                foreach (var item in old) DefDatabase<PawnCapacityDef>.Add(item);
                DefDatabase<PawnCapacityDef>.Add(Definition);
            }
            public void Dispose()
            {
                DefDatabase<PawnCapacityDef>.Clear();
                foreach (var item in old) DefDatabase<PawnCapacityDef>.Add(item);
            }
        }

        private static readonly FieldInfo CapacityCacheField = typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
    }

    private static void SaveLoad(RuntimeFixture fixture)
    {
        var original = new BatterySave();
        original.Energy.Upgrade_StoredEnergy(3);
        original.Energy.SetEnergy(2200f);
        string path = Path.Combine(fixture.WorkingDirectory, "golem-energy.xml");
        try
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref original, "battery");
            Scribe.saver.FinalizeSaving();
        }
        finally { Scribe.ForceStop(); }
        Check.Equal(2200f, original.Energy.StoredEnergy, "saving preserves live energy");
        BatterySave loaded = null!;
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "battery");
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        Check.Equal(2500f, loaded.Energy.StoredEnergyMax, "capacity upgrades survive load");
        Check.Equal(2200f, loaded.Energy.StoredEnergy, "energy above base capacity survives every load pass");
        Check.Equal(1000f, ((CompProperties_Battery)loaded.Energy.props).storedEnergyMax, "shared battery properties stay unchanged");
    }

    private static void Activate(RuntimeFixture fixture)
    {
        var pawn = new Pawn();
        typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pawn, new List<ThingComp>());
        pawn.needs = new Pawn_NeedsTracker(pawn);
        var definition = new NeedDef { defName = "TM_GolemEnergy", needClass = typeof(Need_GolemEnergy) };
        var need = (Need_GolemEnergy)RuntimeHelpers.GetUninitializedObject(typeof(Need_GolemEnergy));
        need.def = definition;
        need.maxEnergy = 1000f;
        need.energyEfficiency = .5f;
        pawn.needs.AllNeeds.Add(need);
        NeedDef previous = TorannMagicDefOf.TM_GolemEnergy;
        TorannMagicDefOf.TM_GolemEnergy = definition;
        try
        {
            var station = (Building_TMGolemBase)RuntimeHelpers.GetUninitializedObject(typeof(Building_TMGolemBase));
            var energy = new BatterySave().Energy;
            energy.Upgrade_StoredEnergy(3);
            energy.SetEnergy(2200f);
            energy.parent = station;
            typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(station, new List<ThingComp> { energy });
            var golem = new CompGolem { parent = pawn };
            typeof(CompGolem).GetField("dormantThing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(golem, station);
            typeof(CompGolem).GetMethod("ApplyNeeds", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(golem, null);
            Check.Equal(2500f, need.MaxLevel, "active golem receives upgraded capacity");
            Check.Equal(2200f, need.CurLevel, "activation does not clamp charge to constructor capacity");
        }
        finally { TorannMagicDefOf.TM_GolemEnergy = previous; }
    }

    public sealed class BatterySave : IExposable
    {
        public CompGolemEnergyHandler Energy = new() { props = new CompProperties_GolemEnergyHandler { storedEnergyMax = 1000f, storedEnergyMaxUpgradeIncrease = 500f } };
        public void ExposeData() => Energy.PostExposeData();
    }
}
