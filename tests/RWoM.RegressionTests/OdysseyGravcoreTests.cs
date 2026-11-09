using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    static Building_GravEngine MagicalEngineFrom(Building_GravEngine original)
    {
        var type = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.Building_MagicalGravEngine", true)!;
        var engine = (Building_GravEngine)Activator.CreateInstance(type)!;
        for (Type? t = typeof(Building_GravEngine); t != null; t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                field.SetValue(engine, field.GetValue(original));
        engine.def.defName = "TM_MagicalGravEngine";
        engine.def.size = new IntVec2(3, 3); engine.def.stackLimit = 1;
        Set(map.listerBuildings, "allBuildingsColonist", new List<Building> { engine });
        return engine;
    }
    static void GravcoreFacilityGuards()
    {
        var engine = MagicalEngineFrom(NativeEngine(new HashSet<IntVec3>()));
        var affected = new CompAffectedByFacilities { parent = engine };
        var extender = Make("ManaNetworkEndpoint", 12, 10).parent;
        extender.AllComps.Add(new AlwaysActiveCoreFacility { parent = extender, props = new CompProperties_GravshipFacility {
            statOffsets = [new StatModifier { stat = StatDefOf.SubstructureSupport, value = 250 }] } });
        var assembly = Assembly.Load("0Harmony");
        var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var patchId = "rwom.regression.gravcore.facility." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, patchId)!;
        try {
            var patch = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.MagicalGravEngineFacilityPatch");
            if (patch != null) {
                var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, patch.GetMethod("Prefix")!)!;
                foreach (string method in new[] { "CanLinkTo", "IsFacilityActive" })
                    ht.GetMethod("Patch")!.Invoke(harmony, [typeof(CompAffectedByFacilities).GetMethod(method)!, hm, null, null, null]);
            }
            Check.Equal(false, affected.IsFacilityActive(extender), "native or VGE support extender cannot boost magical core even if already linked");
            Check.Equal(false, affected.CanLinkTo(extender), "native or VGE support extender cannot attach to magical core");
            extender.def.defName = "TM_MagicalGravExtender";
            Check.Equal(true, affected.IsFacilityActive(extender), "own magical extender remains active");
            affected.parent = NativeEngine(new HashSet<IntVec3>());
            Check.Equal(false, affected.IsFacilityActive(extender), "magical extender cannot support vanilla engine");
            extender.def.defName = "OtherFacility";
            Check.Equal(true, affected.IsFacilityActive(extender), "vanilla extender on vanilla engine untouched");
            affected.parent = engine;
            extender.GetComp<CompGravshipFacility>().Props.statOffsets.Clear();
            Check.Equal(true, affected.IsFacilityActive(extender), "non-support facilities remain available on magical engine");
        } finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony, [patchId]); }
    }
    sealed class AlwaysActiveCoreFacility : CompGravshipFacility { public override bool CanBeActive => true; }
    sealed class GravcoreMapHolder : MapComponent, IThingHolder
    {
        public int visits;
        readonly ThingOwner things;
        public GravcoreMapHolder(Map map) : base(map) {things = new ThingOwner<Thing>(this);}
        public IThingHolder ParentHolder => map;
        public ThingOwner GetDirectlyHeldThings() => things;
        public void GetChildHolders(List<IThingHolder> children) { visits++; ThingOwnerUtility.AppendThingHoldersFromThings(children, things); }
    }
    sealed class GravcoreWorldHolder : WorldObject, IThingHolder
    {
        public int visits;
        readonly ThingOwner things;
        public GravcoreWorldHolder() {things = new ThingOwner<Thing>(this);}
        public IThingHolder ParentHolder => null!;
        public ThingOwner GetDirectlyHeldThings() => things;
        public void GetChildHolders(List<IThingHolder> children) { visits++; ThingOwnerUtility.AppendThingHoldersFromThings(children, things); }
    }
    static void GravcoreDiscovery()
    {
        int savedTick = Find.TickManager.TicksGame;
        var odyssey = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var old = odyssey.GetValue(null);
        var oldVanillaDef = ThingDefOf.GravEngine;
        ThingDefOf.GravEngine = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); ThingDefOf.GravEngine.defName = "RegressionVanillaEngine";
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        string patchId = "rwom.regression.gravcore.lookup." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, patchId)!;
        try {
            odyssey.SetValue(null, true);
            var engine = MagicalEngineFrom(NativeEngine(new HashSet<IntVec3>()));
            map.listerThings = new ListerThings(ListerThingsUse.Global);
            map.passingShipManager = new PassingShipManager(map);
            map.listerThings.AllThings.Add(engine);
            var minifiedDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); minifiedDef.defName = "RegressionMinifiedEngine"; minifiedDef.thingClass = typeof(MinifiedThing); minifiedDef.category = ThingCategory.Item; minifiedDef.stackLimit = 1;
            ThingDefOf.GravEngine.minifiedDef = minifiedDef;
            var defs = new Dictionary<ThingDef, List<Thing>>(); Set(map.listerThings, "listsByDef", defs);
            var patch = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.MagicalGravEngineMapLookupPatch")!;
            var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, patch.GetMethod("Postfix")!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(GravshipUtility).GetMethod("GetPlayerGravEngine_NewTemp")!, null, hm, null, null]);
            Building_GravEngine? Lookup() {
                // State transitions represent subsequent game ticks; both native
                // and magical discovery deliberately cache misses within a tick.
                Set(Find.TickManager, "ticksGameInt", Find.TickManager.TicksGame + 1);
                typeof(GravshipUtility).GetField("lastCachedEngineTick", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, -1);
                return GravshipUtility.GetPlayerGravEngine_NewTemp(map);
            }
            Check.Same(engine, Lookup(), "native lookup discovers spawned magical core");
            var vanilla = NativeEngine(new HashSet<IntVec3>());
            defs[ThingDefOf.GravEngine] = [vanilla];
            Check.Same(vanilla, Lookup(), "native engine lookup priority preserved");
            defs.Clear();
            Set(engine, "factionInt", new Faction {def = new FactionDef()});
            Check.Equal<Building_GravEngine?>(null, Lookup(), "foreign magical core excluded");
            engine.ForceSetStateToUnspawned(); engine.AllComps.Add(new CallbackComp {parent = engine});
            Find.Maps.Clear();
            var package = new MinifiedThing {def = minifiedDef, InnerThing = engine};
            Find.Maps.Add(map);
            defs[minifiedDef] = [package]; map.listerThings.AllThings.Clear(); map.listerThings.AllThings.Add(package);
            Check.Equal<Building_GravEngine?>(null, Lookup(), "native packaged shortcut cannot return foreign magical core");
            Set(engine, "factionInt", faction);
            Check.Same(engine, Lookup(), "native packaged shortcut discovers owned magical core");
            defs.Clear(); map.listerThings.AllThings.Clear();
            var mapHolder = new GravcoreMapHolder(map); map.components.Add(mapHolder);
            Find.Maps.Clear(); mapHolder.GetDirectlyHeldThings().TryAdd(package); Find.Maps.Add(map);
            Check.Same(engine, Lookup(), "native lookup finds packaged core recursively held on map");
            odyssey.SetValue(null, false);
            Check.Equal<Building_GravEngine?>(null, Lookup(), "no Odyssey fallback without DLC");
            odyssey.SetValue(null, true); Find.Maps.Clear();
            var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World)); world.worldObjects = new WorldObjectsHolder(); Set(Current.Game,"worldInt",world);
            var worldHolder = new GravcoreWorldHolder(); Set(worldHolder,"factionInt",faction);
            mapHolder.GetDirectlyHeldThings().Remove(package); worldHolder.GetDirectlyHeldThings().TryAdd(package);
            world.worldObjects.AllWorldObjects.Add(worldHolder);
            var worldPatch = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.MagicalGravEngineWorldLookupPatch")!;
            var worldHm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, worldPatch.GetMethod("Postfix")!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(GravshipUtility).GetMethod("PlayerHasGravEngine", Type.EmptyTypes)!,null,worldHm,null,null]);
            Check.Equal(true,GravshipUtility.PlayerHasGravEngine(),"native world lookup finds owned packaged core recursively");
            Set(worldHolder,"factionInt",new Faction {def = new FactionDef()});
            Check.Equal(false,GravshipUtility.PlayerHasGravEngine(),"hostile world holder cannot supply player core");
            Set(worldHolder,"factionInt",faction); Set(engine,"factionInt",new Faction {def = new FactionDef()});
            Check.Equal(false,GravshipUtility.PlayerHasGravEngine(),"owned world holder does not claim hostile core");
            var worldPost = worldPatch.GetMethod("Postfix")!;
            worldHolder.visits = 0;
            for (int i = 0; i < 1000; i++) {
                object[] args = [false]; worldPost.Invoke(null, args);
                Check.Equal(false, (bool)args[0], "world miss remains false");
            }
            Check.Equal(0, worldHolder.visits, "world misses never enumerate inventories");
            Set(engine, "factionInt", faction);
            object[] restoredOwner = [false]; worldPost.Invoke(null, restoredOwner);
            Check.Equal(true, (bool)restoredOwner[0], "same-tick ownership restoration is immediately visible");
            world.worldObjects.AllWorldObjects.Remove(worldHolder);
            object[] removedWorld = [false]; worldPost.Invoke(null, removedWorld);
            Check.Equal(false, (bool)removedWorld[0], "removed world object cannot supply a registered engine");
            world.worldObjects.AllWorldObjects.Add(worldHolder);
            Find.Maps.Add(map);
            mapHolder.visits = 0;
            var post = patch.GetMethod("Postfix")!;
            for (int i = 0; i < 1000; i++) {
                object?[] args = [map, null]; post.Invoke(null, args);
                Check.Equal<Building_GravEngine?>(null, (Building_GravEngine?)args[1], "empty quest map stays empty");
            }
            Check.Equal(true, mapHolder.visits <= 1, "1000 same-tick misses must traverse holders at most once, not 1000 times; visits=" + mapHolder.visits);
            int visits = mapHolder.visits;
            Set(Find.TickManager, "ticksGameInt", Find.TickManager.TicksGame + 1);
            object?[] nextTick = [map, null]; post.Invoke(null, nextTick);
            Check.Equal(visits + 1, mapHolder.visits, "negative cache refreshes on the next tick");
            worldHolder.GetDirectlyHeldThings().Remove(package);
            Set(engine, "factionInt", faction);
            Find.Maps.Clear();
            mapHolder.GetDirectlyHeldThings().TryAdd(package);
            Find.Maps.Add(map);
            Check.Same(engine, Lookup(), "newly arrived held core discovered after cached miss");
            Set(engine, "factionInt", new Faction {def = new FactionDef()});
            object?[] changedOwner = [map, null]; post.Invoke(null, changedOwner);
            Check.Equal<Building_GravEngine?>(null, (Building_GravEngine?)changedOwner[1], "same-tick cached core never survives ownership loss");
        } finally {
            Set(Find.TickManager, "ticksGameInt", savedTick);
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [patchId]);
            ThingDefOf.GravEngine = oldVanillaDef; odyssey.SetValue(null, old);
            typeof(GravshipUtility).GetField("lastCachedEngineTick", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, -1);
        }
    }    static void GravcoreFootprint()
    {
        var active = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var old = active.GetValue(null); active.SetValue(null, true);
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        string patchId = "rwom.regression.gravcore.footprint." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, patchId)!;
        try {
            var patch = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.MagicalGravEngineFacilityActivityPatch");
            if (patch != null) {
                var hm = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, patch.GetMethod("Postfix")!)!;
                ht.GetMethod("Patch")!.Invoke(harmony, [typeof(CompGravshipFacility).GetProperty("CanBeActive")!.GetMethod!, null, hm, null, null]);
            }
            var floors = new HashSet<IntVec3> { new(12,0,10) };
            var engine = MagicalEngineFrom(NativeEngine(floors));
            var building = Make("ManaNetworkEndpoint",12,10).parent;
            var facility = new CompGravshipFacility { parent = building, props = new CompProperties_GravshipFacility {
                maxDistance = 50, statOffsets = [new StatModifier {stat = StatDefOf.SubstructureSupport, value = 250}] } };
            building.AllComps.Add(facility);
            var footprint = new CompSubstructureFootprint {parent = building, props = new CompProperties_SubstructureFootprint()};
            Set(footprint,"facilityComp", facility);
            Check.Equal(false, footprint.Valid, "unlinked native extender cannot expand magical hull footprint");
            building.def.defName = "TM_MagicalGravExtender"; facility.engine = engine;
            Check.Equal(true, footprint.Valid, "magical extender supplies magical hull footprint");
            var vanilla = NativeEngine(floors); facility.engine = vanilla;
            Set(facility, "lastGotEnginesTick", -1);
            Check.Equal(false, footprint.Valid, "magical extender cannot expand native hull footprint");
            building.def.defName = "NativeExtender";
            Check.Equal(true, footprint.Valid, "native extender native hull unchanged");
        } finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]); active.SetValue(null, old); }
    }
    static void GravcoreSharedFuel()
    {
        var odyssey = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var old = odyssey.GetValue(null); odyssey.SetValue(null, true);
        try {
            var floors = new HashSet<IntVec3>(); for(int x=5;x<20;x++) for(int z=5;z<20;z++) floors.Add(new(x,0,z));
            var engine = MagicalEngineFrom(NativeEngine(floors)); Make("ManaBeacon",10,10);
            var endpoint = (CompManaNetworkEndpoint)Tank(10,9); var coreFuel = AddFuel(endpoint,100);
            foreach(var comp in endpoint.parent.AllComps.ToArray()) {comp.parent=engine; engine.AllComps.Add(comp);}
            var external = Tank(9,10); external.parent.def.defName="TM_MagofuelReservoir"; var extraFuel=AddFuel(external,100);
            var thruster=NativeThruster(engine,12,10,20); NativeThruster(engine,13,10,20);
            var groups=MagicalThrusterFuel.NativeGroups(engine);
            Check.Equal(1,groups.Count,"core and external reservoir share one thruster fuel group");
            Check.Equal(2,groups[0].Sources.Count,"core and external physical sources each counted once");
            Check.Equal(200f,groups[0].Fuel,"shared physical fuel budget");
            Check.Equal(50f,groups[0].ConsumeAmount(50,true),"single shared debit");
            Check.Equal(75f,coreFuel.Fuel,"built-in source pays only its proportional share");
            Check.Equal(75f,extraFuel.Fuel,"external source pays only its proportional share");
            endpoint.FuelMode=ManaNetworkMode.Off;
            Check.Equal(false,MagicalThrusterFuel.Sources(thruster).Contains(coreFuel),"disabled core endpoint cannot fund launch");
            endpoint.FuelMode=ManaNetworkMode.Send;
            Set(engine,"factionInt",new Faction{def=new FactionDef()});
            Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"foreign engine cannot use colony network");
        } finally {odyssey.SetValue(null,old);}
    }
    internal static IEnumerable<TestCase> GravcoreCases()
    {
        yield return new("Odyssey.Gravcore.SharedCoreAndExternalFuelOwnership", f => Run(f, () => ThrusterEnvironment(GravcoreSharedFuel)));
        yield return new("Odyssey.Gravcore.NativeFootprintExclusivity", f => Run(f, () => ThrusterEnvironment(GravcoreFootprint)));
        yield return new("Odyssey.Gravcore.NativeCaptureScribeAndResources", f => OdysseyReservoirTransportTests.Transport(f, true));
        yield return new("Odyssey.Gravcore.NativeLookupFallbackOwnership", f => Run(f, () => ThrusterEnvironment(GravcoreDiscovery)));
        yield return new("Odyssey.Gravcore.ExtenderLinkAndActiveGuards", f => Run(f, () => ThrusterEnvironment(GravcoreFacilityGuards)));
        yield return new("Odyssey.Gravcore.NativeCoreTankLaunchPayment", f => Run(f, () => ThrusterEnvironment(() => NativeLaunch(true))));
        yield return new("Odyssey.Gravcore.CoreTypeAndNativeSimulation", f => {
            var type = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.Building_MagicalGravEngine");
            Check.Equal(true, type != null, "custom magical engine must exist");
            Check.Equal(true, typeof(Building_GravEngine).IsAssignableFrom(type), "native engine simulation inherited");
            Check.Equal(typeof(Building_GravEngine), type!.GetMethod("ConsumeFuel")!.DeclaringType, "no second launch debit");
            Check.Equal(type, type.GetProperty("Graphic")!.GetMethod!.DeclaringType, "custom graphic during native cooldown");
            Check.Equal(type, type.GetMethod("DrawAt", BindingFlags.Instance | BindingFlags.NonPublic)!.DeclaringType, "custom rendering excludes vanilla orb");
        });
    }
}
