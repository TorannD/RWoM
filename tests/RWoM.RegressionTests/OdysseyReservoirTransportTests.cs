using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;
using UnityEngine;

namespace RWoM.RegressionTests;

internal static class OdysseyReservoirTransportTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Reservoir.Transport.NativeGravshipCaptureRotationAndScribe", f => Transport(f));
    }

    static void Set(object obj, string name, object value)
    {
        for (Type? t = obj.GetType(); t != null; t = t.BaseType)
        {
            var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) { field.SetValue(obj, value); return; }
        }
        throw new Exception(name);
    }

    internal static void Transport(RuntimeFixture fixture, bool magicalCore = false)
    {
        var oldGame = Current.Game;
        var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
        var oldLanguage = LanguageDatabase.activeLanguage;
        var oldDefaultLanguage = LanguageDatabase.defaultLanguage;
        var oldThings = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var oldWorldObjects = DefDatabase<WorldObjectDef>.AllDefsListForReading.ToArray();
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English"; Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement>
            { ["Gravship"] = new LoadedLanguage.KeyedReplacement { key = "Gravship", value = "Gravship" } };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
            world.worldObjects = new WorldObjectsHolder(); world.factionManager = new FactionManager();
            Set(Current.Game, "worldInt", world);
            InitializeEngineGraphics();
            var tankDef = Definition("RegressionTransportReservoir", typeof(Building), new IntVec2(2, 2));
            tankDef.comps = [new CompProperties_Refuelable { fuelCapacity = 100, fuelConsumptionRate = 0 }, new CompProperties_MagofuelReservoir()];
            var engineDef = Definition("RegressionTransportEngine", magicalCore ? typeof(Building_MagicalGravEngine) : typeof(Building_GravEngine), magicalCore ? new IntVec2(3, 3) : new IntVec2(1, 1));
            if (magicalCore) engineDef.comps = [new CompProperties_Refuelable { fuelCapacity = 100, fuelConsumptionRate = 0 }, new CompProperties_MagofuelReservoir { manaCapacity = 500 }, new CompProperties_ManaNetworkEndpoint()];
            var shipDef = (WorldObjectDef)RuntimeHelpers.GetUninitializedObject(typeof(WorldObjectDef));
            shipDef.defName = "RegressionTransportGravship"; shipDef.comps = [];
            DefDatabase<ThingDef>.Add(tankDef); DefDatabase<ThingDef>.Add(engineDef); DefDatabase<WorldObjectDef>.Add(shipDef);
            var tank = (Building)ThingMaker.MakeThing(tankDef);
            var engine = (Building_GravEngine)ThingMaker.MakeThing(engineDef);
            Set(engine, "gravshipName", "Regression transport ship");
            if (magicalCore) {
                engine.GetComp<CompRefuelable>().Refuel(100);
                for (int i = 0; i < 5; i++) engine.GetComp<CompMagofuelReservoir>().ConvertFuel(10);
                engine.GetComp<CompMagofuelReservoir>().PersonalReserve = .7f;
                engine.GetComp<CompManaNetworkEndpoint>().FuelMode = ManaNetworkMode.Receive;
                engine.cooldownCompleteTick = 12345;
            }
            tank.Position = new IntVec3(20, 0, 20); tank.Rotation = Rot4.North;
            var fuel = tank.GetComp<CompRefuelable>(); var mana = tank.GetComp<CompMagofuelReservoir>();
            fuel.Refuel(12); mana.ConvertFuel(10); mana.PersonalReserve = .6f;

            // Supply the engine's already-computed floor cache. Native OnValidSubstructure
            // still checks the real footprint; no capture/placement/payment methods are replaced.
            Set(tank, "mapIndexOrState", (sbyte)0);
            var floors = tank.OccupiedRect().Cells.ToHashSet();
            Set(engine, "validSubstructure", floors); engine.substructureDirty = false;
            var ship = (Gravship)Activator.CreateInstance(typeof(Gravship), true)!;
            ship.def = shipDef; ship.ID = 9876;
            Set(ship, "engine", engine);
            var add = typeof(Gravship).GetMethod("AddThing", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var offset = new IntVec3(3, 0, 2);
            var missingFloor = floors.First(); floors.Remove(missingFloor);
            add.Invoke(ship, [tank, offset]);
            Check.Equal(0, ship.Things.Count(), "native capture rejects partially unsupported reservoir");
            floors.Add(missingFloor);
            add.Invoke(ship, [tank, offset]); add.Invoke(ship, [tank, offset]);
            Check.Equal(1, ship.Things.Count(), "native capture deduplicates repeated cell visits");
            Check.Equal(true, ReferenceEquals(tank, ship.Things.Single()), "ship retains original tank, not a reconstructed copy");
            VerifyRotations(ship, tank);
            tank.ForceSetStateToUnspawned();

            // Include the actual engine in the native deep graph so its reference resolves.
            engine.Position = new IntVec3(10, 0, 10); Set(engine, "mapIndexOrState", (sbyte)0);
            floors.UnionWith(engine.OccupiedRect()); add.Invoke(ship, [engine, IntVec3.Zero]); engine.ForceSetStateToUnspawned();
            string path = Path.Combine(fixture.WorkingDirectory, "native-gravship.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref ship, "ship"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            Gravship loaded = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "ship"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Check.Equal(2, loaded.Things.Count(), "native deep graph restores exactly tank and engine");
            var loadedTank = loaded.Things.Single(t => t.def == tankDef);
            Check.Equal(false, ReferenceEquals(tank, loadedTank), "loaded reservoir is independently reconstructed by native Scribe");
            Check.Equal(true, loaded.Things.Any(t => ReferenceEquals(t, loaded.Engine)), "engine reference resolves into same graph");
            Check.Equal(.6f, loadedTank.TryGetComp<CompMagofuelReservoir>().PersonalReserve, "personal reserve survives native ship graph");
            VerifyRotations(loaded, loadedTank);
            if (magicalCore) {
                Check.Equal(true, loaded.Engine is Building_MagicalGravEngine, "native ship graph preserves custom engine class");
                Check.Equal(50f, loaded.Engine.GetComp<CompMagofuelReservoir>().StoredMana, "built-in mana survives native graph");
                Check.Equal(95f, loaded.Engine.GetComp<CompRefuelable>().Fuel, "built-in physical fuel survives native graph");
                Check.Equal(.7f, loaded.Engine.GetComp<CompMagofuelReservoir>().PersonalReserve, "built-in charging reserve survives");
                Check.Equal(ManaNetworkMode.Receive, loaded.Engine.GetComp<CompManaNetworkEndpoint>().FuelMode, "endpoint mode survives graph");
                Check.Equal(12345, loaded.Engine.cooldownCompleteTick, "native cooldown retained");
                Check.Equal(10f, loaded.Engine.GetComp<CompMagofuelReservoir>().ConvertFuel(10), "loaded engine converts through established tank contract");
                Check.Equal(94f, loaded.Engine.GetComp<CompRefuelable>().Fuel, "loaded engine pays exact conversion once");
                Check.Equal(95f, engine.GetComp<CompRefuelable>().Fuel, "loaded engine cannot affect original source");
            }
            Check.Equal(10f, loadedTank.TryGetComp<CompMagofuelReservoir>().ConvertFuel(10), "post-load conversion remains usable");
            Check.Equal(10f, loadedTank.TryGetComp<CompRefuelable>().Fuel, "post-load payment occurs once");
            Check.Equal(11f, fuel.Fuel, "loaded graph cannot debit original reservoir");
        }
        finally
        {
            Scribe.ForceStop(); Current.Game = oldGame;
            System.Globalization.CultureInfo.CurrentCulture = oldCulture;
            LanguageDatabase.activeLanguage = oldLanguage; LanguageDatabase.defaultLanguage = oldDefaultLanguage;
            DefDatabase<ThingDef>.Clear(); foreach (var d in oldThings) DefDatabase<ThingDef>.Add(d);
            DefDatabase<WorldObjectDef>.Clear(); foreach (var d in oldWorldObjects) DefDatabase<WorldObjectDef>.Add(d);
        }
    }

    static ThingDef Definition(string name, Type type, IntVec2 size)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = name; def.thingClass = type; def.category = ThingCategory.Building;
        def.size = size; def.rotatable = true; def.bringAlongOnGravship = true;
        def.building = new BuildingProperties(); def.comps = []; def.virtualDefs = []; def.stackLimit = 1;
        return def;
    }

    static bool SkipTexture() => false;
    static void InitializeEngineGraphics()
    {
        // Engine static icons are unrelated to the native transport methods under test.
        // Use the same scoped texture boundary as existing headless UI fixtures.
        var assembly = Assembly.Load("0Harmony");
        string id = "rwom.regression.reservoir-transport." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(OdysseyReservoirTransportTests).GetMethod(nameof(SkipTexture), BindingFlags.Static | BindingFlags.NonPublic)!)!;
        var mods = LoadedModManager.RunningModsListForReading.ToArray();
        try
        {
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(ContentFinder<Texture2D>).GetMethod("Get", [typeof(string), typeof(bool)])!, prefix, null, null, null]);
            // No asset bundles exist in this headless process; ShaderDatabase still creates
            // its real late-bound shaders without touching the type-resolution-only mod pack.
            LoadedModManager.RunningModsListForReading.Clear();
            RuntimeHelpers.RunClassConstructor(typeof(Building_GravEngine).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(WorldObject).TypeHandle);
        }
        finally
        {
            LoadedModManager.RunningModsListForReading.AddRange(mods);
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
        }
        RuntimeLog.AssertClean();
    }

    static void VerifyRotations(Gravship ship, Thing tank)
    {
        var expected = new[] { new IntVec3(3, 0, 2), new IntVec3(2, 0, -3), new IntVec3(-3, 0, -2), new IntVec3(-2, 0, 3) };
        for (int i = 0; i < 4; i++)
        {
            ship.Rotation = new Rot4(i);
            var placement = ship.ThingPlacements[tank];
            Check.Equal(expected[i], placement.local, "native landing placement rotates offset " + i);
            Check.Equal(new Rot4(i), placement.rotation, "native landing placement rotates tank " + i);
            Check.Equal(11f, tank.TryGetComp<CompRefuelable>().Fuel, "rotation preserves physical fuel " + i);
            Check.Equal(10f, tank.TryGetComp<CompMagofuelReservoir>().StoredMana, "rotation preserves charge " + i);
        }
    }
}
