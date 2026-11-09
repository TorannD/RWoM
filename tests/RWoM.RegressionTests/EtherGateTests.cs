using System.Reflection;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using TorannMagic;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

internal static class EtherGateTests
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Type Type(string name) => Check.NotNull(typeof(MagicData).Assembly.GetType("TorannMagic." + name) ?? typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + name), "Ether implementation " + name);
    static object Call(object target, string name, params object[] args) => Check.NotNull(target.GetType().GetMethod(name, Flags), "production method " + name).Invoke(target, args)!;
    static object Get(object target, string name) => Check.NotNull(target.GetType().GetProperty(name, Flags), "production property " + name).GetValue(target)!;
    static object Reason(string name) => Enum.Parse(Type("EtherServantEndReason"), name);
    static Pawn Servant()
    {
        var pawn = (Pawn)Activator.CreateInstance(Type("Pawn_EtherServant"))!;
        pawn.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "RegressionEtherServant"; pawn.def.race = new RaceProperties();
        pawn.def.destroyable = true; pawn.def.thingClass = pawn.GetType();
        pawn.inventory = new Pawn_InventoryTracker(pawn); pawn.carryTracker = new Pawn_CarryTracker(pawn);
        return pawn;
    }
    static Thing Cargo(Pawn pawn, bool carried)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = carried ? "EtherCarry" : "EtherInventory"; def.stackLimit = 75; def.thingClass = typeof(Thing);
        var thing = new Thing { def = def, stackCount = carried ? 12 : 7 };
        Check.Equal(true, (carried ? pawn.carryTracker.innerContainer : pawn.inventory.innerContainer).TryAdd(thing, false), "native owner accepts cargo");
        return thing;
    }
    static readonly List<Thing> Dropped = new();
    static bool blockDrop;
    static int destroys;
    static bool Drop(Thing thing, ref Thing resultingThing, ref bool __result)
    {
        __result = !blockDrop; resultingThing = blockDrop ? null! : thing;
        if (!blockDrop) Dropped.Add(thing);
        return false;
    }
    static bool Destroy(Pawn __instance)
    {
        Check.Equal(0, __instance.inventory.innerContainer.Count, "destruction only after native inventory empty");
        Check.Equal(0, __instance.carryTracker.innerContainer.Count, "destruction only after native carry empty");
        destroys++; FleshStatTests.Set(__instance, "mapIndexOrState", (sbyte)-2); return false;
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.regression.ether." + Guid.NewGuid();
        public Patches() => harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        public void Prefix(MethodInfo target, string method)
        {
            var patch = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(EtherGateTests).GetMethod(method, Flags)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, patch, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    public static GameComponent InitializeComponents()
    {
        var registry = (GameComponent)Activator.CreateInstance(Type("GameComponent_EtherServants"), Current.Game)!;
        FleshStatTests.Set(Current.Game, "components", new List<GameComponent> { registry });
        return registry;
    }
    static void MapEnd(bool killed) => FleshStatTests.InGame(() =>
    {
        InitializeComponents();
        var oldDefs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try
        {
            FleshStatTests.Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            FleshStatTests.Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var temporaryDesignation = DefDatabase<DesignationDef>.DefCount == 0;
            if (temporaryDesignation) DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "EtherTestDesignation" });
            Find.Maps[0].designationManager = new DesignationManager(Find.Maps[0]);
            if (temporaryDesignation) DefDatabase<DesignationDef>.Clear();
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = "RawMagicyte"; def.stackLimit = 75; def.thingClass = typeof(Thing); def.comps = [];
            DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(def);
            var pawn = Servant(); var carry = Cargo(pawn, true); var inventory = Cargo(pawn, false);
            FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)0); FleshStatTests.Set(pawn, "positionInt", new IntVec3(10, 0, 10));
            using var patches = new Patches();
            // Keep real TryDropAll/TryDrop owner mutation. Only the map placement and
            // post-empty pawn destruction boundary lack a headless colony fixture.
            patches.Prefix(typeof(GenDrop).GetMethod("TryDropSpawn")!, nameof(Drop));
            patches.Prefix(typeof(Pawn).GetMethod("Destroy")!, nameof(Destroy));
            Dropped.Clear(); destroys = 0; blockDrop = true;
            if (killed) pawn.Kill(null); else Call(pawn, "TryEnd", Reason("Expired"));
            Check.Equal(0, destroys, "failed placement never destroys owner");
            Check.Same(carry, pawn.carryTracker.CarriedThing, "failed native carry drop restores exact stack");
            Check.Equal(true, pawn.inventory.innerContainer.Contains(inventory), "failed native inventory drop restores exact stack");
            blockDrop = false;
            Call(pawn, "TryEnd", Reason(killed ? "Killed" : "Expired"));
            Call(pawn, "TryEnd", Reason("Killed"));
            Check.Equal(1, destroys, "completion destroys exactly once");
            Check.Equal(1, Dropped.Count(t => ReferenceEquals(t, carry)), "carried cargo placed exactly once");
            Check.Equal(1, Dropped.Count(t => ReferenceEquals(t, inventory)), "inventory cargo placed exactly once");
            Check.Equal(12, carry.stackCount, "carried stack amount preserved"); Check.Equal(7, inventory.stackCount, "inventory stack amount preserved");
            Check.Equal(killed ? 5 : 0, Dropped.Where(t => t.def == def).Sum(t => t.stackCount), "death produces exactly five, expiry none even after repeated death request");
            Check.Equal(false, Dropped.Any(t => t is Corpse), "no corpse from terminal servant");
        }
        finally { DefDatabase<ThingDef>.Clear(); foreach (var def in oldDefs) DefDatabase<ThingDef>.Add(def); Dropped.Clear(); blockDrop = false; }
    });
    static void Definitions(RuntimeFixture f)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs/EtherGate.xml"));
        var race = new System.Xml.Linq.XElement(doc.Root!.Elements("ThingDef").Single(e => (string?)e.Element("defName") == "TM_EtherServant").Element("race")!);
        foreach (string name in new[] { "body", "fleshType", "renderTree", "trainability", "thinkTreeMain", "lifeStageAges" }) race.Element(name)?.Remove();
        var xml = new System.Xml.XmlDocument(); xml.LoadXml(race.ToString());
        var native = DirectXmlToObject.ObjectFromXml<RaceProperties>(xml.DocumentElement!, false);
        Check.Equal(false, native.needsRest, "native race needs no rest"); Check.Equal(FoodTypeFlags.None, native.foodType, "native race consumes no food");
        Check.Equal(0f, native.baseHungerRate, "native race has no hunger"); Check.Equal(false, native.canBeVacuumBurnt, "native race immune to vacuum burn");
        Check.Equal(Intelligence.ToolUser, native.intelligence, "native tool-user hauling capability");
        var think = new System.Xml.Linq.XElement(doc.Root.Elements("ThinkTreeDef").Single());
        think.Descendants("li").Where(e => (string?)e.Attribute("Class") == "ThinkNode_Subtree").Remove();
        xml.LoadXml(think.ToString());
        var tree = DirectXmlToObject.ObjectFromXml<ThinkTreeDef>(xml.DocumentElement!, false);
        Check.Equal(2, tree.thinkRoot.subNodes.Count, "only hauling and idle jobs remain after engine downed branch");
        Check.Equal(Type("JobGiver_EtherHaul"), tree.thinkRoot.subNodes[0].GetType(), "native XML resolves production hauling giver");
        Check.Equal(typeof(JobGiver_WanderColony), tree.thinkRoot.subNodes[1].GetType(), "idle fallback remains available");
        var gate = doc.Root.Elements("ThingDef").Single(e => (string?)e.Element("defName") == "TM_EtherGate");
        xml.LoadXml(gate.Element("comps")!.Elements().Single(e => (string?)e.Attribute("Class") == "TorannMagic.Odyssey.CompProperties_EtherGate").ToString());
        var props = DirectXmlToObject.ObjectFromXml<CompProperties>(xml.DocumentElement!, false);
        Check.Equal(Type("CompEtherGate"), props.compClass, "native XML binds actual gate component");
        DirectXmlCrossRefLoader.Clear();
    }
    public sealed class AnimationEnvelope : IExposable
    {
        public ThingComp Gate = (ThingComp)Activator.CreateInstance(Type("CompEtherGate"))!;
        public void ExposeData() => Gate.PostExposeData();
    }
    static void AnimationSave(RuntimeFixture f)
    {
        foreach (int start in new[] { 1000, 1500, 640 })
        {
            FleshStatTests.Set(Find.TickManager, "ticksGameInt", 1200);
            var value = new AnimationEnvelope(); FleshStatTests.Set(value.Gate, "summonAnimationStartTick", start);
            var path = Path.Combine(f.WorkingDirectory, "animation-" + start + ".xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref value, "animation"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            AnimationEnvelope loaded = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "animation"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
            Check.Equal(start == 1000 ? 1000 : -1, (int)Get(loaded.Gate, "SummonAnimationStartTick"), "native Scribe keeps current animation, invalidates future/stale without replay");
        }
    }
    public sealed class LifetimeEnvelope : IExposable
    {
        public Pawn Pawn = Servant();
        public void ExposeData() => Call(Pawn, "ExposeLifetimeData");
    }
    static void SaveLifetime(RuntimeFixture f)
    {
        var value = new LifetimeEnvelope(); Call(value.Pawn, "StartLifetime", 1234);
        Cargo(value.Pawn, true); Call(value.Pawn, "TryEnd", Reason("Expired"));
        var path = Path.Combine(f.WorkingDirectory, "lifetime.xml");
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref value, "lifetime"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        LifetimeEnvelope loaded = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "lifetime"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Call(loaded.Pawn, "StartLifetime", 90000);
        Check.Equal(61234L, Convert.ToInt64(Get(loaded.Pawn, "ExpiresAtTick")), "native Scribe preserves absolute expiry, even past deadline");
        Check.Equal("Expired", Get(loaded.Pawn, "EndReason").ToString(), "pending end reason persists without becoming death loot");
        Check.Equal(1234, (int)Get(loaded.Pawn, "SummonedAtTick"), "load never restarts materialization animation");
    }
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in RawCases()) yield return new(test.Name, f => FleshStatTests.InGame(() =>
        {
            FleshStatTests.Set(Current.Game, "maps", new List<Map>());
            InitializeComponents();
            test.Body(f);
        }));
    }
    static IEnumerable<TestCase> RawCases()
    {
        yield return new("EtherGate.SummonSuccessPaysOnceStartsLifetimeAndAnimation", f => TeleportSpiritTests.EtherSummonFailure(f, 3));
        yield return new("EtherGate.SummonInsufficientNoPayment", f => TeleportSpiritTests.EtherSummonFailure(f, 0));
        yield return new("EtherGate.SummonNoLandingNoPayment", f => TeleportSpiritTests.EtherSummonFailure(f, 1));
        yield return new("EtherGate.SummonPlacementFailureRefund", f => TeleportSpiritTests.EtherSummonFailure(f, 2));
        yield return new("EtherGate.NativeDefinitionContracts", Definitions);
        yield return new("EtherGate.AnimationScribeBounds", AnimationSave);
        yield return new("EtherGate.FuelNativeNetworkExactAndRefund", f => OdysseyBeaconTests.EtherFuel(f, false));
        yield return new("EtherGate.FuelNativeCallbackRollback", f => OdysseyBeaconTests.EtherFuel(f, true));
        yield return new("EtherGate.LifetimeScribe", SaveLifetime);
        yield return new("EtherGate.KillNativeCargoRetryOnce", _ => MapEnd(true));
        yield return new("EtherGate.ExpiryNativeCargoRetryNoLoot", _ => MapEnd(false));
        yield return new("EtherGate.AnimationBounds", _ =>
        {
            var type = Type("Visuals.EtherSummonAnimation");
            var progress = Check.NotNull(type.GetMethod("Progress", Flags), "animation progress");
            foreach (var row in new[] { (999, 1000, -1f), (1000, 1000, 0f), (1180, 1000, .5f), (1360, 1000, 1f), (1361, 1000, -1f), (1000, -1, -1f), (int.MaxValue, 1000, -1f) })
                Check.Equal(row.Item3, Convert.ToSingle(progress.Invoke(null, new object[] { row.Item1, row.Item2 })), "bounded saved animation timing");
        });
        yield return new("EtherGate.RegistryDeduplicatesAndExpiresOffMap", _ =>
        {
            var registry = InitializeComponents();
            var pawn = Servant(); var cargo = Cargo(pawn, true);
            Call(pawn, "StartLifetime", 0);
            Call(registry, "Register", pawn); Call(registry, "Register", pawn);
            var tracked = (System.Collections.IList)registry.GetType().GetField("servants", Flags)!.GetValue(registry)!;
            Check.Equal(1, tracked.Count, "registry deduplicates start/spawn registration");
            FleshStatTests.Set(Find.TickManager, "ticksGameInt", 59750); registry.GameComponentTick();
            Check.Equal("Active", Get(pawn, "EndReason").ToString(), "offmap active before deadline");
            FleshStatTests.Set(Find.TickManager, "ticksGameInt", 60000); registry.GameComponentTick();
            Check.Equal("Expired", Get(pawn, "EndReason").ToString(), "registry reaches offmap expiry without pawn tick");
            Check.Same(pawn.carryTracker, cargo.ParentHolder, "registry preserves pending cargo owner");
            registry.GameComponentTick(); Check.Equal(1, tracked.Count, "pending remains tracked without duplication");
        });        yield return new("EtherGate.AbsoluteExpiryDoesNotRestart", _ =>
        {
            var pawn = Servant(); var cargo = Cargo(pawn, true);
            Call(pawn, "StartLifetime", 1234);
            Check.Equal(61234L, Convert.ToInt64(Get(pawn, "ExpiresAtTick")), "one day uses absolute 60000 ticks");
            Call(pawn, "StartLifetime", 20000);
            Check.Equal(61234L, Convert.ToInt64(Get(pawn, "ExpiresAtTick")), "reinitialization cannot renew helper");
            Call(pawn, "TickLifetime", 61233);
            Check.Equal("Active", Get(pawn, "EndReason").ToString(), "active before exact boundary");
            Call(pawn, "TickLifetime", 61234);
            Check.Equal("Expired", Get(pawn, "EndReason").ToString(), "expires at exact boundary");
            Check.Same(cargo, pawn.carryTracker.CarriedThing, "off-map expiry preserves native cargo");
            Check.Equal(false, pawn.Destroyed, "no owner removal until cargo safe");
        });
        foreach (var reason in new[] { "Expired", "Killed" })
        {
            string end = reason;
            yield return new("EtherGate.OffMap" + end + "RetainsNativeOwners", _ =>
            {
                var pawn = Servant(); var carried = Cargo(pawn, true); var inventory = Cargo(pawn, false);
                Call(pawn, "TryEnd", Reason(end)); Call(pawn, "TryEnd", Reason(end));
                Check.Equal(end, Get(pawn, "EndReason").ToString(), "pending reason retained");
                Check.Equal(false, pawn.Destroyed, "cargo owner retained pending safe unload");
                Check.Same(carried, pawn.carryTracker.CarriedThing, "same native carried stack");
                Check.Same(inventory, pawn.inventory.innerContainer.Single(), "same native inventory stack");
                Check.Same(pawn.carryTracker, carried.ParentHolder, "native carry ownership intact");
                Check.Same(pawn.inventory, inventory.ParentHolder, "native inventory ownership intact");
                Check.Equal(12, carried.stackCount, "carried count unchanged"); Check.Equal(7, inventory.stackCount, "inventory count unchanged");
            });
        }
    }
}



internal static partial class OdysseyBeaconTests
{
    public static void EtherFuel(RuntimeFixture f, bool failCallback) => Run(f, () =>
    {
        var paymentType = Check.NotNull(typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.EtherGateFuelPayment"), "Ether fuel payment implementation");
        var debit = paymentType.GetMethod("TryDebit")!;
        var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
        world.factionManager = new FactionManager(); world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction); Set(Current.Game, "worldInt", world);
        Make("ManaBeacon", 10, 10);
        var gate = Make("ManaNetworkEndpoint", 10, 10); gate.GetType().GetProperty("FuelMode")!.SetValue(gate, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        var local = AddFuel(gate, 40); var remote = Tank(11, 10); remote.parent.def.defName = "TM_MagofuelReservoir"; var fuel = AddFuel(remote, 59);
        object[] args = [gate.parent, 100f, null!];
        Check.Equal(false, (bool)debit.Invoke(null, args)!, "99 rejects whole one-day price");
        Check.Equal(40f, local.Fuel, "insufficient leaves local untouched"); Check.Equal(59f, fuel.Fuel, "insufficient leaves remote untouched");
        fuel.Refuel(1);
        if (failCallback) gate.parent.AllComps.Add(new CallbackComp { parent = gate.parent, Callback = signal => { if (signal == "RanOutOfFuel") throw new InvalidOperationException("ether injected debit callback"); } });
        args = [gate.parent, 100f, null!];
        Check.Equal(!failCallback, (bool)debit.Invoke(null, args)!, "transaction commits only when every native debit completes");
        if (!failCallback) { Check.Equal(0f, local.Fuel, "local exact debit"); Check.Equal(0f, fuel.Fuel, "remote exact debit"); }
        paymentType.GetMethod("Refund")!.Invoke(args[2], null); paymentType.GetMethod("Refund")!.Invoke(args[2], null);
        Check.Equal(40f, local.Fuel, "cancel restores local exactly once"); Check.Equal(60f, fuel.Fuel, "cancel restores remote exactly once");
        if (failCallback)
        {
            var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Check.Equal(true, problems.All(p => p.Contains("ether injected debit callback")), "only injected fault diagnostic allowed");
            RuntimeLog.Clear();
        }
    });
}






internal static partial class TeleportSpiritTests
{
    static Pawn? etherGenerated;
    static bool etherLanding, etherSpawnSucceeds;
    static bool EtherGenerate(ref Pawn __result) { __result = etherGenerated!; return false; }
    static bool EtherLanding(ref bool __result) { __result = etherLanding; return false; }
    static bool EtherSpawn(Thing __0, IntVec3 __1, Map __2, ref Thing __result)
    {
        if (!etherSpawnSucceeds) throw new InvalidOperationException("ether injected placement failure");
        Set(__0, "mapIndexOrState", (sbyte)Find.Maps.IndexOf(__2)); Set(__0, "positionInt", __1); __result = __0; return false;
    }
    static bool EtherDestroy(Pawn __instance) { Set(__instance, "mapIndexOrState", (sbyte)-2); return false; }
    public static void EtherSummonFailure(RuntimeFixture f, int stage) => CoreReturnWorld(f, w =>
    {
        EtherGateTests.InitializeComponents();
        var kind = new PawnKindDef { defName = "TM_EtherServantKind" }; using var kindScope = new DefinitionScope<PawnKindDef>(kind);
        var core = CoreReturnCore(w, stage == 0 ? 99 : 100); var fuel = core.parent.GetComp<CompRefuelable>();
        var type = Check.NotNull(typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.CompEtherGate"), "gate implementation");
        var gate = (ThingComp)Activator.CreateInstance(type)!; gate.parent = core.parent; core.parent.AllComps.Add(gate);
        foreach (string key in new[] { "TM_EtherGateNoFuel", "TM_EtherGateNoLanding", "TM_EtherGateFailed" })
            LanguageDatabase.activeLanguage.keyedReplacements[key] = new LoadedLanguage.KeyedReplacement { key = key, value = key };
        etherGenerated = (Pawn)Activator.CreateInstance(typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.Pawn_EtherServant")!)!;
        etherGenerated.def = w.Caster.def; etherGenerated.inventory = new Pawn_InventoryTracker(etherGenerated); etherGenerated.carryTracker = new Pawn_CarryTracker(etherGenerated);
        etherLanding = stage >= 2; etherSpawnSucceeds = stage == 3;
        using var patches = new Patches();
        // Generation needs the full colony definition database. Landing geometry has
        // independent native CoreReturn coverage; here exercise gate transaction order.
        patches.Prefix(typeof(PawnGenerator).GetMethods().Single(m => m.Name == "GeneratePawn" && m.GetParameters().Length == 3), nameof(EtherGenerate));
        patches.Prefix(typeof(TorannMagic.Odyssey.CoreReturnUtility).GetMethod("SafeLanding")!, nameof(EtherLanding));
        patches.Prefix(typeof(GenSpawn).GetMethod("Spawn", new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(WipeMode) })!, nameof(EtherSpawn));
        patches.Prefix(typeof(Pawn).GetMethod("Destroy")!, nameof(EtherDestroy));
        try
        {
            object[] args = [null!];
            if (stage == 3)
            {
                int now = Find.TickManager.TicksGame;
                Check.Equal(true, (bool)type.GetMethod("TrySummon")!.Invoke(gate, args)!, "successful spawn commits");
                Check.Equal(0f, fuel.Fuel, "success spends exactly100 native fuel");
                Check.Equal((long)now + 60000, (long)etherGenerated.GetType().GetProperty("ExpiresAtTick")!.GetValue(etherGenerated)!, "success begins absolute one day");
                Check.Equal(now, (int)type.GetProperty("SummonAnimationStartTick")!.GetValue(gate)!, "success starts animation");
                Check.Equal(false, (bool)type.GetMethod("TrySummon")!.Invoke(gate, args)!, "empty balance prevents repeat summon");
                Check.Equal(0f, fuel.Fuel, "repeat cannot duplicate debit");
                Check.Equal(false, etherGenerated.Destroyed, "committed servant remains spawned");
                return;
            }
            Check.Equal(false, (bool)type.GetMethod("TrySummon")!.Invoke(gate, args)!, "failed summon rejects");
            Check.Equal(stage == 0 ? 99f : 100f, fuel.Fuel, "failed summon preserves exact native fuel balance");
            Check.Equal(-1, (int)type.GetProperty("SummonAnimationStartTick")!.GetValue(gate)!, "failed summon never starts animation");
            if (stage > 0) Check.Equal(true, etherGenerated.Destroyed, "uncommitted empty pawn disposed");
            if (stage == 2)
            {
                var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Check.Equal(true, problems.Count > 0 && problems.All(p => p.Contains("ether injected placement failure")), "only injected placement error recorded"); RuntimeLog.Clear();
            }
        }
        finally { etherGenerated = null; etherSpawnSucceeds = false; }
    });
}





