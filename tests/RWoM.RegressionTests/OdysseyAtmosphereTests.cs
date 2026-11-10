using System.Xml.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RWoM.RegressionTests;
internal static class OdysseyAtmosphereTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Atmosphere.TwoMissRetentionAndRecovery", f => Run(Retention));
        yield return new("Odyssey.Atmosphere.PaidOpenCellRadiusAndExpiry", f => Run(Boundary));
        yield return new("Odyssey.Atmosphere.ConnectedUnionCapacityAndRetention", f => Run(Network));
        yield return new("Odyssey.Atmosphere.LargeNetworkOnePumpAtLeastHalfTwoFull", f => Run(() =>
        {
            var cells = new HashSet<IntVec3>();
            for (int x = 10; x <= 50; x += 5)
            {
                Beacon(x, 15);
                for (int dx = -5; dx <= 5; dx++)
                    for (int dz = -5; dz <= 5; dz++)
                        if (dx * dx + dz * dz <= 25) cells.Add(new IntVec3(x + dx, 0, 15 + dz));
            }
            Check.Equal(true, cells.Count > 400 && cells.Count < 500, "representative former roughly22percent network");
            var first = Pump(10, 15);
            Tick(60);
            Check.Equal(true, 1f - V(50, 15) >= .5f, "one generator provides at least half pressure in this network");
            Near(1f - 250f / cells.Count, V(50, 15), "250cell atmospheric capacity");
            Near(.9f, Charge(first), "maximum personal supply rate remains .1mana per second");
            var second = Pump(15, 15);
            Tick(120);
            Near(0f, V(50, 15), "two generators fully fund this network");
            Near(1.9f - cells.Count * .0004f, Charge(first) + Charge(second), "union paid once at reduced cell cost");
        }));
        yield return new("Odyssey.Atmosphere.NativePlantAndFireQueries", f => Run(Native));
        yield return new("Odyssey.Atmosphere.PartialOverlapGroundAndReload", f => Run(() => Persistence(f)));
        yield return new("Odyssey.Atmosphere.NativePawnRecoveryAndUnroofedWallFire", f => Run(PawnAndWall));
        yield return new("Odyssey.Atmosphere.OxygenSeekingDestinationOverridesCurrentCell", f => Run(Destination));
    }

    static Type T(string n) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + n)!;
    static void Set(object o, string n, object v)
    {
        for (Type? t = o.GetType(); t != null; t = t.BaseType)
        {
            var p = t.GetField(n, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (p != null)
            {
                p.SetValue(o, v);
                return;
            }
        }

        throw new Exception(n);
    }

    static void Near(float expected, float actual, string why)
    {
        if (Math.Abs(expected - actual) > .00001f)
            throw new Exception($"{why}: expected {expected}, got {actual}");
    }

    static Map map = null !;
    static MapComponent field = null !;
    static Faction faction = null !;
    static int id;
    static void Run(Action test)
    {
        var old = Current.Game;
        var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
        var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldFlag = flag.GetValue(null);
        using var patches = new Patches();
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            flag.SetValue(null, true);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            // Native StatWorker.FinalizeValue reads the running game's scenario.
            // Without it, Find.Scenario falls through to editor UI left by earlier fixtures.
            Current.Game.Scenario = new Scenario();
            map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo
            {
                Size = new IntVec3(60, 1, 60),
                isPocketMap = true
            };
            map.pocketTileInfo = new SurfaceTile();
            Set(map.pocketTileInfo, "biome", new BiomeDef { inVacuum = true });
            Set(Current.Game, "maps", new List<Map> { map });
            map.cellIndices = new CellIndices(map);
            map.edificeGrid = new EdificeGrid(map);
            map.roofGrid = new RoofGrid(map);
            map.thingGrid = new ThingGrid(map);
            map.regionDirtyer = new RegionDirtyer(map);
            map.regionAndRoomUpdater = new RegionAndRoomUpdater(map);
            Set(map.regionAndRoomUpdater, "initialized", true);
            map.regionGrid = new RegionGrid(map);
            field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalAtmosphere"), map)!;
            map.components = new List<MapComponent>
            {
                field,
                (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), map)!
            };
            faction = new Faction
            {
                def = new FactionDef
                {
                    isPlayer = true
                }
            };
            id = 900;
            test();
        }
        finally
        {
            Current.Game = old;
            System.Globalization.CultureInfo.CurrentCulture = oldCulture;
            flag.SetValue(null, oldFlag);
        }
    }

    static ThingComp Comp(ThingWithComps parent, string name)
    {
        var c = (ThingComp)Activator.CreateInstance(T("Comp" + name))!;
        c.parent = parent;
        c.props = (CompProperties)Activator.CreateInstance(T("CompProperties_" + name))!;
        parent.AllComps.Add(c);
        c.Initialize(c.props);
        return c;
    }

    static Building BuildingAt(int x, int z)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = "AtmosphereTest";
        d.size = new IntVec2(1, 1);
        var b = new Building
        {
            def = d,
            thingIDNumber = ++id
        };
        Set(b, "positionInt", new IntVec3(x, 0, z));
        Set(b, "mapIndexOrState", (sbyte)0);
        Set(b, "factionInt", faction);
        Set(b, "comps", new List<ThingComp>());
        return b;
    }

    static ThingComp Pump(int x = 15, int z = 15, float mana = 1)
    {
        var b = BuildingAt(x, z);
        var pump = Comp(b, "MagicalOxygen");
        var bank = Comp(b, "OxygenManaBuffer");
        Set(bank, "storedMana", mana);
        Comp(b, "ManaNetworkEndpoint");
        field.GetType().GetMethod("Register")!.Invoke(field, new object[] { pump });
        return pump;
    }

    static ThingComp Beacon(int x, int z)
    {
        var c = Comp(BuildingAt(x, z), "ManaBeacon");
        c.PostSpawnSetup(false);
        return c;
    }

    static float Charge(ThingComp p) => (float)p.parent.AllComps.Single(c => c.GetType().Name == "CompOxygenManaBuffer").GetType().GetProperty("StoredMana")!.GetValue(p.parent.AllComps.Single(c => c.GetType().Name == "CompOxygenManaBuffer"))!;
    static void Tick(int time)
    {
        Set(Find.TickManager, "ticksGameInt", time);
        field.MapComponentTick();
    }

    static float V(int x, int z) => new IntVec3(x, 0, z).GetVacuum(map);
    static void Retention()
    {
        var p = Pump(mana: .016f);
        float partialVacuum = 1f - .016f / .0324f;
        Tick(60);
        Near(partialVacuum, V(15, 15), "partial paid pressure established");
        Tick(120);
        Near(partialVacuum, V(15, 15), "first empty check retains exact partial pressure");
        for (int i = 0; i < 5; i++)
        {
            Tick(120);
            Near(partialVacuum, V(15, 15), "same tick reads never advance misses");
        }
        Near(0, Charge(p), "retained pressure never charges extra mana");
        Tick(180);
        Near(1, V(15, 15), "second consecutive empty check clears air");
        var bank = p.parent.AllComps.Single(c => c.GetType().Name == "CompOxygenManaBuffer");
        Set(bank, "storedMana", .016f);
        Tick(240);
        Tick(300);
        Near(partialVacuum, V(15, 15), "new paid air gets one missed check");
        Set(bank, "storedMana", .016f);
        Tick(360);
        Tick(420);
        Near(partialVacuum, V(15, 15), "positive check resets previous miss");
        Tick(480);
        Near(1, V(15, 15), "two new misses clear recovered air");
        var neverPaid = Pump(40, 40, 0);
        Tick(540);
        Near(1, V(40, 40), "zero supply cannot invent retained air");
        Near(0, Charge(neverPaid), "empty generator never pays");
        Set(bank, "storedMana", .2f);
        Tick(600);
        Set(p.parent, "factionInt", new Faction { def = new FactionDef() });
        float beforeCapture = Charge(p);
        Tick(660);
        Near(0, V(15, 15), "captured generator leaves one check of existing air");
        Tick(720);
        Near(1, V(15, 15), "captured generator cannot replenish air");
        Near(beforeCapture, Charge(p), "capture stops new mana payments");
        Set(p.parent, "factionInt", faction);
        Tick(780);
        Set(bank, "storedMana", 0f);
        Set(Find.TickManager, "ticksGameInt", 900);
        Near(1, V(15, 15), "two elapsed checks bound stale air even before map resumes ticking");
        Tick(900);
        Near(1, V(15, 15), "resumed map cannot resurrect expired air with fresh grace");
    }
    static void Boundary()
    {
        var p = Pump();
        Near(1, V(15, 15), "unpaid open vacuum");
        Tick(60);
        Near(0, V(20, 15), "five cell edge oxygenated without room roof or walls");
        Near(1, V(21, 15), "six cell edge native vacuum");
        Near(.9676f, Charge(p), "81 unique circle cells paid once");
        Tick(60);
        Near(.9676f, Charge(p), "repeated field tick cannot double pay");
        Set(Find.TickManager, "ticksGameInt", 120);
        Near(0, V(15, 15), "first scheduled miss retains paid air before tick");
        Tick(120);
        Near(0, V(15, 15), "new payment resumes field");
        Set(p.parent, "positionInt", new IntVec3(16, 0, 15));
        Near(0, V(10, 15), "old location retains previously paid air after movement");
        Near(1, V(21, 15), "movement creates no unpaid new cells");
        var flick = new CompFlickable
        {
            parent = p.parent,
            props = new CompProperties_Flickable()
        };
        p.parent.AllComps.Add(flick);
        var breakdown = new CompBreakdownable
        {
            parent = p.parent,
            props = new CompProperties_Breakdownable()
        };
        p.parent.AllComps.Add(breakdown);
        Tick(180);
        Near(0, V(16, 15), "moved pump can fund new geometry");
        Set(flick, "switchOnInt", false);
        Near(0, V(16, 15), "switch off preserves previously paid air");
        Near(0, V(10, 15), "old location survives first missed check");
        Set(flick, "switchOnInt", true);
        Tick(240);
        Near(0, V(16, 15), "switched on pump pays again");
        Near(1, V(10, 15), "old location clears after second missed check");
        Set(breakdown, "brokenDownInt", true);
        Near(0, V(16, 15), "breakdown preserves existing air");
        Set(breakdown, "brokenDownInt", false);
        Tick(300);
        Near(0, V(16, 15), "field restored before switching output");
        p.GetType().GetProperty("OutputMode")!.SetValue(p, Enum.Parse(T("OxygenOutputMode"),"PipeNetwork"));
        Near(0, V(16, 15), "pipe output stops production but retains existing air");
        float beforePipe = Charge(p); Tick(360);
        Near(beforePipe, Charge(p), "pipe mode excluded from atmosphere billing");
        Near(0, V(16, 15), "first pipe-mode miss retains air");
        Tick(420);
        Near(1, V(16, 15), "second pipe-mode miss clears air");
    }

    static void Network()
    {
        var a = Beacon(15, 15);
        var b = Beacon(20, 15);
        var p = Pump();
        Tick(60);
        var cells = new HashSet<IntVec3>();
        foreach (var center in new[]
        {
            a.parent.Position,
            b.parent.Position
        }

        )
            for (int x = -5; x <= 5; x++)
                for (int z = -5; z <= 5; z++)
                    if (x * x + z * z <= 25)
                        cells.Add(new IntVec3(center.x + x, 0, center.z + z));
        Near(0, V(25, 15), "one pump supports union below 250 cells");
        Near(1f - cells.Count * .0004f, Charge(p), "one pump pays actual union demand");
        var second = Pump(20, 15);
        Tick(120);
        Near(0, V(25, 15), "two pumps share demand across deduplicated network");
        Near(2f - 2f * cells.Count * .0004f, Charge(p) + Charge(second), "second pulse union billed exactly once");
        b.PostDeSpawn(map);
        Set(b.parent, "mapIndexOrState", (sbyte)-1);
        Near(0, V(25, 15), "beacon removal preserves existing atmosphere");
        field.GetType().GetMethod("Unregister")!.Invoke(field, new object[] { second });
        Set(second.parent, "mapIndexOrState", (sbyte)-1);
        Tick(180);
        Near(0, V(25, 15), "disconnected edge retains one missed check");
        Tick(240);
        Near(1, V(25, 15), "disconnected edge expires after two misses");
        Near(0, V(15, 15), "remaining paid pump stays oxygenated");
    }

    public sealed class TestPlant : Plant
    {
        protected override bool Resting => false;
        public override float GrowthRate => 1f; // Fixed light/temperature/fertility; native oxygen gate remains intact.
        public float NativeGrowth => GrowthPerTick;
    }

    static void Native()
    {
        var mods = LoadedModManager.RunningModsListForReading.ToArray();
        using (var visual = new Patches(false))
        {
            visual.Prefix(typeof(ContentFinder<Texture2D>).GetMethod("Get", new[] { typeof(string), typeof(bool) })!);
            foreach (var method in typeof(GraphicDatabase).GetMethods().Where(m => m.Name == "Get" && m.IsGenericMethodDefinition))
                visual.Prefix(method.MakeGenericMethod(typeof(Graphic_Single)));
            visual.Prefix(typeof(GraphicDatabase).GetMethod("GetInner", BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Graphic_Single)));
            try
            {
                LoadedModManager.RunningModsListForReading.Clear();
                RuntimeHelpers.RunClassConstructor(typeof(Plant).TypeHandle);
            }
            finally
            {
                LoadedModManager.RunningModsListForReading.AddRange(mods);
            }
        }

        var p = Pump();
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "TestPlant";
        def.plant = new PlantProperties
        {
            growDays = 1f
        };
        def.size = new IntVec2(1, 1);
        var plant = new TestPlant
        {
            def = def
        };
        Set(plant, "positionInt", new IntVec3(15, 0, 15));
        Set(plant, "mapIndexOrState", (sbyte)0);
        Set(plant, "growthInt", .1f);
        Check.Equal(true, plant.DyingBecauseExposedToVacuum, "native plant vacuum death before supply");
        Near(0, plant.NativeGrowth, "native plant cannot grow in vacuum");
        Near(1, FireUtility.GetEffectiveVacuumForFire(plant.Position, map), "fire native vacuum before supply");
        Tick(60);
        Check.Equal(false, plant.DyingBecauseExposedToVacuum, "native plant oxygenated inside paid bubble");
        Check.Equal(true, plant.NativeGrowth > 0, "native oxygen growth gate opens");
        Near(0, FireUtility.GetEffectiveVacuumForFire(plant.Position, map), "native floor fire gets atmosphere");
        Set(plant, "positionInt", new IntVec3(21, 0, 15));
        Check.Equal(true, plant.DyingBecauseExposedToVacuum, "native plant outside bubble remains exposed");
        Near(0, plant.NativeGrowth, "native outside growth remains blocked");
    }

    public sealed class FieldSave : IExposable
    {
        public void ExposeData() => field.ExposeData();
    }

    static void PawnAndWall()
    {
        var oldStat = StatDefOf.VacuumResistance;
        try
        {
            StatDefOf.VacuumResistance = new StatDef
            {
                defName = "VacuumResistance",
                defaultBaseValue = 0f,
                workerClass = typeof(StatWorker_VacuumResistance)
            };
            var pump = Pump();
            var wall = BuildingAt(15, 16);
            wall.def.passability = Traversability.Impassable;
            wall.def.fillPercent = 1f;
            wall.def.building = new BuildingProperties();
            map.edificeGrid.Register(wall);
            Near(1, FireUtility.GetEffectiveVacuumForFire(wall.Position, map), "native unroofed wall special branch vacuum");
            var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            race.defName = "AtmosphereHuman";
            race.race = new RaceProperties
            {
                lifeStageAges = new List<LifeStageAge>
                {
                    new LifeStageAge
                    {
                        def = new LifeStageDef()
                    }
                }
            };
            var pawn = new Pawn
            {
                def = race,
                kindDef = new PawnKindDef
                {
                    race = race
                },
                thingIDNumber = 1400
            };
            pawn.health = new Pawn_HealthTracker(pawn);
            pawn.ageTracker = new Pawn_AgeTracker(pawn);
            Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
            Set(pawn, "positionInt", new IntVec3(15, 0, 15));
            Set(pawn, "mapIndexOrState", (sbyte)0);
            var effect = new HediffComp_VacuumExposure
            {
                parent = new HediffWithComps
                {
                    pawn = pawn
                },
                props = new HediffCompProperties_VacuumExposure
                {
                    severityPerSecondUnexposed = -.01f
                }
            };
            float adjustment = 0;
            effect.CompPostTickInterval(ref adjustment, 60);
            Near(0, adjustment, "native exposed pawn cannot recover");
            Tick(60);
            Near(0, FireUtility.GetEffectiveVacuumForFire(wall.Position, map), "paid oxygen applies even to native unroofed wall branch");
            effect.CompPostTickInterval(ref adjustment, 60);
            Near(-.01f, adjustment, "native exposure recovery resumes in bubble");
            Set(pawn, "positionInt", new IntVec3(21, 0, 15));
            adjustment = 0;
            effect.CompPostTickInterval(ref adjustment, 60);
            Near(0, adjustment, "recovery stops again outside field");
        }
        finally
        {
            StatDefOf.VacuumResistance = oldStat;
        }
    }

    static void Persistence(RuntimeFixture f)
    {
        var a = Pump(15, 15, .016f);
        var b = Pump(19, 15, .016f);
        Tick(60);
        Near(1f - .016f / .0324f, V(17, 15), "independent overlapping groups use maximum, never added strength");
        Tick(120);
        Near(1f - .016f / .0324f, V(17, 15), "first miss is retained before saving");
        var save = new FieldSave();
        string path = Path.Combine(f.WorkingDirectory, "field.xml");
        try
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref save, "field");
            Scribe.saver.FinalizeSaving();
        }
        finally
        {
            Scribe.ForceStop();
        }

        var previous = field;
        field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalAtmosphere"), map)!;
        map.components.Remove(previous);
        map.components.Insert(0, field);
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref save, "field");
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            Scribe.ForceStop();
        }

        field.GetType().GetMethod("Register")!.Invoke(field, new object[] { a });
        field.GetType().GetMethod("Register")!.Invoke(field, new object[] { b });
        Near(1f - .016f / .0324f, V(17, 15), "load preserves paid partial air and first miss");
        Tick(120);
        Near(0, Charge(a), "loaded map deadline prohibits payment replay");
        Near(1f - .016f / .0324f, V(17, 15), "same-tick reload does not advance miss");
        Tick(180);
        Near(1, V(17, 15), "loaded first miss expires on next missed check");
        var bank = a.parent.AllComps.Single(c => c.GetType().Name == "CompOxygenManaBuffer");
        Set(bank, "storedMana", 1f);
        Tick(240);
        Near(0, V(15, 15), "next payment restores field");
        // Older saves have only the prepaid deadline and cannot invent retained air.
        var oldXml = System.Xml.Linq.XDocument.Load(path);
        oldXml.Descendants("TM_atmosphereStrength").Remove();
        oldXml.Descendants("TM_atmosphereMissed").Remove();
        oldXml.Save(path);
        previous = field;
        field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalAtmosphere"), map)!;
        map.components.Remove(previous);
        map.components.Insert(0, field);
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref save, "field");
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        field.GetType().GetMethod("Register")!.Invoke(field, new object[] { a });
        Near(1, V(15, 15), "old saves initialize without invented atmosphere");
        Set(map.pocketTileInfo, "biome", new BiomeDef { inVacuum = false });
        float before = Charge(a);
        Tick(300);
        Near(before, Charge(a), "nonvacuum planet consumes no mana");
        Near(0, (float)field.GetType().GetMethod("StrengthAt")!.Invoke(field, new object[] { new IntVec3(15, 0, 15) })!, "warm map has no local override");
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.atmosphere." + Guid.NewGuid();
        public Patches(bool gameplay = true)
        {
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            if (gameplay)
            {
                Patch(typeof(VacuumUtility).GetMethod(nameof(VacuumUtility.GetVacuum))!, T("MagicalAtmosphereVacuumPatch"));
                Patch(typeof(FireUtility).GetMethod(nameof(FireUtility.GetEffectiveVacuumForFire))!, T("MagicalAtmosphereFirePatch"));
            }
        }

        public void Prefix(MethodBase method)
        {
            var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(OdysseyAtmosphereTests).GetMethod(nameof(SkipPresentation), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object? [] { method, hm, null, null, null });
        }

        void Patch(MethodBase method, Type patch)
        {
            var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, patch.GetMethod("Postfix")!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object? [] { method, null, hm, null, null });
        }

        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { id });
    }

    static void Destination()
    {
        Pump();
        Tick(60);
        var pawn = new Pawn();
        pawn.jobs = new Pawn_JobTracker(pawn);
        Set(pawn, "positionInt", new IntVec3(15, 0, 15));
        Set(pawn, "mapIndexOrState", (sbyte)0);
        var method = T("MagicalAtmosphereFindOxygenPatch").GetMethod("DestinationVacuum")!;
        float Result() => (float)method.Invoke(null, new object[] { pawn })!;
        Near(0, Result(), "idle pawn uses its current oxygenated cell");
        var job = new Job
        {
            targetA = new IntVec3(21, 0, 15)
        };
        Set(pawn.jobs, "curJob", job);
        Near(1, Result(), "unsafe destination overrides currently safe position");
        Set(pawn, "positionInt", new IntVec3(21, 0, 15));
        job.targetA = new IntVec3(15, 0, 15);
        Near(0, Result(), "safe destination overrides currently unsafe position");
        job.targetA = BuildingAt(21, 15);
        job.targetB = new IntVec3(15, 0, 15);
        Near(0, Result(), "native priority prefers plain cell targets before thing targets");
        job.targetB = LocalTargetInfo.Invalid;
        Near(1, Result(), "thing destination used when no plain cell target exists");
    }

    static bool SkipPresentation() => false;
}
