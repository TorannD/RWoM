using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RWoM.RegressionTests;
internal static class OdysseyThermalTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Thermal.NativeColdOnlyReadAndPayment", f => Run(Boundary));
        yield return new("Odyssey.Thermal.OneGeneratorNetworkAndStandby", f => Run(Network));
        yield return new("Odyssey.Thermal.PartialReadAndSave", f => Run(() => NativeAndSave(f)));
        yield return new("Odyssey.Thermal.NativePlantColdAndPawnRecovery", f => Run(NativeLiving));
        yield return new("Odyssey.FuelReserve.ThermalAccess", f => Run(() => NetworkFuel(true)));
        yield return new("Odyssey.Thermal.NativeNetworkFuelAndStandby", f => Run(NetworkFuel));
        yield return new("Odyssey.Thermal.LifecycleAndBoundedPayment", f => Run(Lifecycle));
        yield return new("Odyssey.Thermal.CrossMapCannotRepay", f => Run(CrossMap));
        yield return new("Odyssey.Thermal.IndependentNetworkReceipts", f => Run(IndependentNetworks));
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
        var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldFlag = flag.GetValue(null);
        using var patches = new Patches();
        nativeTemperature = -100f;
        try
        {
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
            map.pollutionGrid = new PollutionGrid(map);
            map.regionDirtyer = new RegionDirtyer(map);
            map.regionAndRoomUpdater = new RegionAndRoomUpdater(map);
            Set(map.regionAndRoomUpdater, "initialized", true);
            map.regionGrid = new RegionGrid(map);
            field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalThermal"), map)!;
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
            savedSource = null;
            Current.Game = old;
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

    static float nativeTemperature;
    static ThingComp Generator(int x = 15, int z = 15, float mana = 1)
    {
        var b = BuildingAt(x, z);
        b.def.size = new IntVec2(2, 2);
        var c = Comp(b, "MagicalThermal");
        var bank = Comp(b, "ShieldManaBuffer");
        Set(bank, "storedMana", mana);
        Comp(b, "ManaNetworkEndpoint");
        c.PostSpawnSetup(false);
        return c;
    }

    static ThingComp Beacon(int x, int z)
    {
        var c = Comp(BuildingAt(x, z), "ManaBeacon");
        c.PostSpawnSetup(false);
        return c;
    }

    static float Charge(ThingComp c)
    {
        var b = c.parent.AllComps.Single(x => x.GetType().Name == "CompShieldManaBuffer");
        return (float)b.GetType().GetProperty("StoredMana")!.GetValue(b)!;
    }

    static void Tick(int t)
    {
        Set(Find.TickManager, "ticksGameInt", t);
        field.MapComponentTick();
    }

    static float Temp(int x = 15, int z = 15) => GenTemperature.GetTemperatureForCell(new IntVec3(x, 0, z), map);
    static void IndependentNetworks()
    {
        var a = Generator(); var b = Generator(40, 40); var ownBeacon = Beacon(15, 16); Beacon(40, 41);
        Tick(60); float paid = Charge(a);
        var flick = new CompFlickable { parent = b.parent, props = new CompProperties_Flickable() };
        b.parent.AllComps.Add(flick); Set(flick, "switchOnInt", false);
        Set(Find.TickManager, "ticksGameInt", 61);
        Near(21, Temp(), "unrelated switch preserves prepaid warmth");
        Near(-100, Temp(40, 40), "switched source loses warmth immediately");
        var unrelated = Generator(45, 45); Near(21, Temp(), "unrelated registration preserves warmth");
        unrelated.PostDeSpawn(map); Near(21, Temp(), "unrelated removal preserves warmth");
        Near(paid, Charge(a), "validation never repays");
        ownBeacon.PostDeSpawn(map); Near(-100, Temp(), "own topology change invalidates immediately");
        Near(paid, Charge(a), "topology validation never repays");
        Tick(120); Near(21, Temp(), "next paid interval restores valid source");
        Set(a.parent, "factionInt", new Faction { def = new FactionDef() }); Near(-100, Temp(), "own ownership change invalidates immediately");
    }
    static void Boundary()
    {
        var c = Generator();
        Near(-100, Temp(), "unpaid native cold");
        Tick(60);
        Near(21, Temp(20), "radius five edge warm");
        Near(-100, Temp(21), "outside cold");
        Near(.919f, Charge(c), "unique circle paid");
        Tick(60);
        Near(.919f, Charge(c), "repeat no debit");
        nativeTemperature = 100;
        Near(100, Temp(), "hot remains hot");
        Tick(120);
        Near(.919f, Charge(c), "warm field no cost");
        nativeTemperature = -100;
        Tick(180);
        Near(21, Temp(), "ground cold warmed");
        Set(c.parent, "positionInt", new IntVec3(16, 0, 15));
        nativeTemperature = 100;
        Near(100, Temp(), "hot fast path stays native even after topology changes");
        nativeTemperature = -100;
        Near(-100, Temp(), "move immediate invalidation");
        Tick(180);
        Near(.838f, Charge(c), "move cannot repeat payment");
        Tick(240);
        Near(21, Temp(), "next pulse resumes");
        Set(Find.TickManager, "ticksGameInt", 300);
        Near(-100, Temp(), "expiry no free warmth");
    }

    static void Network()
    {
        Beacon(15, 15);
        var b = Beacon(20, 15);
        var a = Generator();
        var second = Generator(20, 15);
        Tick(60);
        Near(1, Charge(second), "same type standby no debit");
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
                        cells.Add(center + new IntVec3(x, 0, z));
        Near(21, Temp(25), "one source warms entire network union");
        Near(1 - cells.Count * .001f, Charge(a), "unique union cells paid by one source");
        a.PostDeSpawn(map);
        Set(a.parent, "mapIndexOrState", (sbyte)-1);
        Near(-100, Temp(25), "source removal invalidates");
        Tick(60);
        Near(1, Charge(second), "standby cannot repay same pulse");
        Tick(120);
        Near(1 - cells.Count * .001f, Charge(second), "standby next pulse active");
        Set(b.parent, "positionInt", new IntVec3(40, 0, 40));
        Near(-100, Temp(25), "beacon move invalidates");
    }

    static void Lifecycle()
    {
        var c = Generator(mana: 100);
        c.props.GetType().GetField("manaPerCellSecond")!.SetValue(c.props, .2f);
        var flick = new CompFlickable
        {
            parent = c.parent,
            props = new CompProperties_Flickable()
        };
        c.parent.AllComps.Add(flick);
        var broken = new CompBreakdownable
        {
            parent = c.parent,
            props = new CompProperties_Breakdownable()
        };
        c.parent.AllComps.Add(broken);
        Tick(60);
        Near(90, Charge(c), "withdraw limited to reservoir maxTransfer");
        Near(-100 + 121 * (10f / 16.2f), Temp(), "bounded withdrawal gives partial field");
        Set(flick, "switchOnInt", false);
        Near(-100, Temp(), "flick invalidates immediately");
        Tick(120);
        Near(90, Charge(c), "off no payment");
        Set(flick, "switchOnInt", true);
        Tick(180);
        Set(broken, "brokenDownInt", true);
        Near(-100, Temp(), "breakdown invalidates immediately");
        Set(broken, "brokenDownInt", false);
        Tick(240);
        Set(c.parent, "factionInt", new Faction { def = new FactionDef() });
        Near(-100, Temp(), "ownership loss invalidates immediately");
        Set(c.parent, "factionInt", faction);
        Tick(300);
        Near(60, Charge(c), "new interval new payment");
        Set(Find.TickManager, "ticksGameInt", 299);
        Near(-100, Temp(), "time reversal invalidates");
    }

    static void CrossMap()
    {
        var c = Generator();
        Tick(60);
        Near(.919f, Charge(c), "first map prepaid");
        c.PostDeSpawn(map);
        var destination = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        destination.info = new MapInfo
        {
            Size = new IntVec3(60, 1, 60),
            isPocketMap = true
        };
        destination.cellIndices = new CellIndices(destination);
        destination.thingGrid = new ThingGrid(destination);
        var other = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalThermal"), destination)!;
        destination.components = new List<MapComponent>
        {
            other,
            (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), destination)!
        };
        Set(Current.Game, "maps", new List<Map> { map, destination });
        Set(c.parent, "mapIndexOrState", (sbyte)1);
        c.PostSpawnSetup(false);
        other.MapComponentTick();
        Near(.919f, Charge(c), "map transfer cannot repay same interval");
        Near(-100, GenTemperature.GetTemperatureForCell(c.parent.Position, destination), "moved source cannot reuse old paid geometry");
        Set(Find.TickManager, "ticksGameInt", 120);
        other.MapComponentTick();
        Near(.838f, Charge(c), "next interval pays once on new map");
        Near(21, GenTemperature.GetTemperatureForCell(c.parent.Position, destination), "new map next pulse warmth");
    }

    static void NetworkFuel() => NetworkFuel(false);
    static void NetworkFuel(bool reserve)
    {
        Beacon(15, 15);
        var active = Generator(mana: 0);
        var standby = Generator(15, 15, 0);
        void Receiver(ThingComp source)
        {
            var e = source.parent.AllComps.Single(c => c.GetType().Name == "CompManaNetworkEndpoint");
            e.GetType().GetProperty("ManaMode")!.SetValue(e, Enum.Parse(T("ManaNetworkMode"), "Receive"));
            e.GetType().GetProperty("FuelMode")!.SetValue(e, Enum.Parse(T("ManaNetworkMode"), "Off"));
            e.PostSpawnSetup(false);
        }

        Receiver(active);
        Receiver(standby);
        var parent = BuildingAt(14, 15);
        var bank = Comp(parent, "MagofuelReservoir");
        var sender = Comp(parent, "ManaNetworkEndpoint");
        sender.PostSpawnSetup(false);
        var fuelDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        fuelDef.defName = "TM_Magofuel";
        fuelDef.virtualDefs = new List<ThingDef>();
        var filter = new ThingFilter();
        filter.SetAllow(fuelDef, true);
        var fuel = new CompRefuelable
        {
            parent = parent,
            props = new CompProperties_Refuelable
            {
                fuelCapacity = 500,
                fuelFilter = filter,
                consumeFuelOnlyWhenUsed = true
            }
        };
        parent.AllComps.Add(fuel);
        fuel.Refuel(3);
        if (reserve) { parent.def.defName = "TM_MagofuelReservoir"; bank.GetType().GetProperty("FuelReserve")!.SetValue(bank, 3f); }
        var graph = map.components.Single(c => c.GetType().Name == "MapComponent_ManaNetwork");
        void NetworkTick(int t)
        {
            Set(Find.TickManager, "ticksGameInt", t);
            graph.MapComponentTick();
        }

        NetworkTick(60);
        Near(2, fuel.Fuel, "thermal demand converts one whole remote fuel unit");
        Near(10, Charge(active), "derived buffer receives native network mana");
        Tick(60);
        Near(21, Temp(), "converted fuel yields paid thermal coverage");
        NetworkTick(60);
        Near(2, fuel.Fuel, "network repeated tick no conversion");
        var activeBuffer = active.parent.AllComps.Single(c => c.GetType().Name == "CompShieldManaBuffer");
        Set(activeBuffer, "storedMana", 100f);
        Set(bank, "storedMana", 0f);
        NetworkTick(120);
        Near(2, fuel.Fuel, "standby alone cannot trigger new fuel conversion");
        var flick = new CompFlickable
        {
            parent = active.parent,
            props = new CompProperties_Flickable()
        };
        active.parent.AllComps.Add(flick);
        Set(flick, "switchOnInt", false);
        var standbyFlick = new CompFlickable
        {
            parent = standby.parent,
            props = new CompProperties_Flickable()
        };
        standby.parent.AllComps.Add(standbyFlick);
        Set(standbyFlick, "switchOnInt", false);
        Set(activeBuffer, "storedMana", 0f);
        NetworkTick(180);
        Near(2, fuel.Fuel, "off generators cannot trigger conversion");
    }

    static ThingComp? savedSource;
    public sealed class FieldSave : IExposable
    {
        public void ExposeData()
        {
            field.ExposeData();
            savedSource?.PostExposeData();
        }
    }

    static void NativeAndSave(RuntimeFixture f)
    {
        var c = Generator(mana: .04f);
        Tick(60);
        float expected = -100 + 121 * (.04f / .081f);
        Near(expected, Temp(), "partial payment applied once");
        GenTemperature.TryGetTemperatureForCell(new IntVec3(15, 0, 15), map, out float direct);
        Near(expected, direct, "direct native Try agrees");
        savedSource = c;
        var save = new FieldSave();
        var path = Path.Combine(f.WorkingDirectory, "thermal.xml");
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

        var parent = c.parent;
        parent.AllComps.Remove(c);
        c = Comp(parent, "MagicalThermal");
        savedSource = c;
        var old = field;
        field = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalThermal"), map)!;
        map.components.Remove(old);
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

        c.PostSpawnSetup(true);
        var bank = c.parent.AllComps.Single(x => x.GetType().Name == "CompShieldManaBuffer");
        Set(bank, "storedMana", 1f);
        Near(0, (float)c.GetType().GetMethod("SupplyPulse")!.Invoke(c, new object[] { .081f })!, "source receipt survives recreated component load");
        Tick(60);
        Near(1, Charge(c), "load deadline prevents repay");
        Near(-100, Temp(), "load no coverage");
        Tick(120);
        Near(21, Temp(), "new paid pulse resumes");
        var xml = System.Xml.Linq.XDocument.Load(path);
        foreach (var element in xml.Descendants().Where(e => e.Name.LocalName is "TM_thermalLastPulse" or "TM_thermalLastSupplyTick").ToList())
            element.Remove();
        xml.Save(path);
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

        Near(-100, Temp(), "old save with no thermal keys restores no field");
        float beforeOld = Charge(c);
        Tick(180);
        Near(beforeOld - .081f, Charge(c), "old save initializes a fresh paid pulse");
    }

    public sealed class ColdPlant : Plant
    {
        public int ColdLeafless;
        public void CheckCold() => CheckMakeLeafless();
        public override void MakeLeafless(LeaflessCause cause, bool sendMessage = true)
        {
            if (cause == LeaflessCause.Cold)
                ColdLeafless++;
        }
    }

    static bool SkipPresentation() => false;
    static bool Outdoor(ref bool __result)
    {
        __result = true;
        return false;
    }

    static bool PlantRoom(ref Room __result)
    {
        __result = (Room)RuntimeHelpers.GetUninitializedObject(typeof(Room));
        return false;
    }

    static bool NativeTerrain(ref TerrainDef __result)
    {
        __result = (TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef));
        return false;
    }

    static bool Comfort(ref FloatRange __result)
    {
        __result = new FloatRange(10f, 30f);
        return false;
    }

    static void NativeLiving()
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

        using var dependencies = new Patches(false);
        dependencies.Prefix(typeof(RegionAndRoomQuery).GetMethod("GetRoom", new[] { typeof(Thing), typeof(RegionType) })!, nameof(PlantRoom));
        dependencies.Prefix(typeof(Room).GetProperty("UsesOutdoorTemperature")!.GetMethod!, nameof(Outdoor));
        dependencies.Prefix(typeof(GenTemperature).GetMethod("ComfortableTemperatureRange", new[] { typeof(Pawn) })!, nameof(Comfort));
        dependencies.Prefix(typeof(GridsUtility).GetMethod("GetTerrain", new[] { typeof(IntVec3), typeof(Map) })!, nameof(NativeTerrain));
        var generator = Generator();
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "ThermalPlant";
        def.plant = new PlantProperties
        {
            growDays = 1f
        };
        def.size = new IntVec2(1, 1);
        var plant = new ColdPlant
        {
            def = def,
            thingIDNumber = 234
        };
        Set(plant, "positionInt", new IntVec3(15, 0, 15));
        Set(plant, "mapIndexOrState", (sbyte)0);
        Near(0, plant.GrowthRateFactor_Temperature, "native cold blocks growth");
        plant.CheckCold();
        Check.Equal(1, plant.ColdLeafless, "native cold causes leafless");
        var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        race.defName = "ThermalPawn";
        race.race = new RaceProperties();
        var pawn = new Pawn
        {
            def = race,
            thingIDNumber = 1234
        };
        pawn.health = new Pawn_HealthTracker(pawn);
        Set(pawn, "positionInt", new IntVec3(15, 0, 15));
        Set(pawn, "mapIndexOrState", (sbyte)0);
        var coldDef = new HediffDef
        {
            defName = "ThermalCold",
            hediffClass = typeof(Hediff),
            maxSeverity = 1f
        };
        var cold = new Hediff
        {
            def = coldDef,
            pawn = pawn,
            Severity = .1f
        };
        pawn.health.hediffSet.hediffs.Add(cold);
        var giver = new HediffGiver_Hypothermia
        {
            hediff = coldDef,
            hediffInsectoid = coldDef
        };
        giver.OnIntervalPassed(pawn, null !);
        Check.Equal(true, cold.Severity > .1f, "native cold severity rises outside paid field");
        Tick(60);
        Near(1, plant.GrowthRateFactor_Temperature, "native growth temperature restored");
        plant.CheckCold();
        Check.Equal(1, plant.ColdLeafless, "covered native leafless branch blocked");
        Check.Equal(true, plant.DyingBecauseExposedToVacuum, "thermal does not supply oxygen");
        float before = cold.Severity;
        giver.OnIntervalPassed(pawn, null !);
        Check.Equal(true, cold.Severity < before, "native hypothermia recovery inside field");
        Set(plant, "positionInt", new IntVec3(21, 0, 15));
        plant.CheckCold();
        Check.Equal(2, plant.ColdLeafless, "outside native leafless branch remains");
        Near(0, plant.GrowthRateFactor_Temperature, "outside growth stays cold");
        nativeTemperature = 100f;
        var heatDef = new HediffDef
        {
            defName = "ThermalHeat",
            hediffClass = typeof(Hediff),
            maxSeverity = 1f
        };
        var heat = new Hediff
        {
            def = heatDef,
            pawn = pawn,
            Severity = .1f
        };
        pawn.health.hediffSet.hediffs.Add(heat);
        new HediffGiver_Heat
        {
            hediff = heatDef
        }.OnIntervalPassed(pawn, null !);
        Check.Equal(true, heat.Severity > .1f, "covered native heatstroke unchanged");
    }

    static bool NativeAir(ref float temperature, ref bool __result)
    {
        temperature = nativeTemperature;
        __result = true;
        return false;
    }

    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.thermal." + Guid.NewGuid();
        public Patches(bool gameplay = true)
        {
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            if (!gameplay)
                return;
            Patch(typeof(GenTemperature).GetMethod("TryGetDirectAirTemperatureForCell")!, typeof(OdysseyThermalTests).GetMethod(nameof(NativeAir), BindingFlags.Static | BindingFlags.NonPublic)!, true);
            var patch = T("MagicalThermalTemperaturePatch");
            if (patch != null)
                Patch(typeof(GenTemperature).GetMethod("TryGetTemperatureForCell")!, patch.GetMethod("Postfix")!, false);
        }

        public void Prefix(MethodBase target, string method = nameof(SkipPresentation)) => Patch(target, typeof(OdysseyThermalTests).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!, true);
        void Patch(MethodBase target, MethodInfo method, bool prefix)
        {
            var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, method)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object? [] { target, prefix ? hm : null, prefix ? null : hm, null, null });
        }

        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { id });
    }
}

