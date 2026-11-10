using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthReleaseTests
{
    static object Receipt(Pawn pawn) => ClassIdentity.Component(pawn).GetType()
        .GetProperty("SpiritRitual", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ClassIdentity.Component(pawn))!;
    static string Release(object receipt, Pawn creator, Map map, IntVec3 cell)
    {
        var method = receipt.GetType().GetMethod("TryRelease", BindingFlags.Instance | BindingFlags.NonPublic);
        Check.NotNull(method, "paid receipt needs an explicit native release operation");
        try { return method!.Invoke(receipt, [creator, map, cell])!.ToString()!; }
        catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRebirthRelease.NativeSpawnSameOriginalAndRepeat", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "actual resurrection and payment complete first");
            using var setup = new ReleaseFixture(r, c);
            var pawn = c.Original; var race = pawn.def; var story = pawn.story; var name = pawn.Name;
            var data = pawn.GetComp<CompAbilityUserMagic>().MagicData;
            var receipt = Receipt(pawn); var cell = new IntVec3(15, 0, 15);
            string result = Release(receipt, c.Creator, c.Map, cell);
            RuntimeLog.AssertClean();
            Check.Equal("Released", result, "native GenSpawn releases prepared original");
            Check.Same(pawn, c.Map.spawnedThings.Single(t => ReferenceEquals(t, pawn)), "native map owns same pawn exactly once");
            Check.Same(c.Map.spawnedThings, pawn.holdingOwner, "reciprocal map custody");
            Check.Equal(true, pawn.Spawned, "native spawn complete");
            Check.Equal(true, c.Creator.carryTracker.CarriedThing == null, "carry owner emptied by native transfer");
            Check.Same(race, pawn.def, "race preserved"); Check.Same(story, pawn.story, "story preserved"); Check.Same(name, pawn.Name, "actual name preserved");
            Check.Same(data, pawn.GetComp<CompAbilityUserMagic>().MagicData, "saved magic owner preserved");
            var spirit = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND);
            Check.Equal(65f, spirit.CurLevel, "missing Spirit uses native first initialization only");
            spirit.CurLevel = 0f;
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, cell), "repeat only returns recorded success");
            Check.Same(spirit, pawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "need owner retained");
            Check.Equal(0f, spirit.CurLevel, "repeat never refills existing zero");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.NativePartialSpawnRollbackSaveAndRetry", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid original exists before failure injection");
            using var setup = new ReleaseFixture(r, c);
            var spirit = new Need_Spirit(c.Original) { def = TorannMagicDefOf.TM_SpiritND }; spirit.CurLevel = 0;
            c.Original.needs.AllNeeds.Add(spirit);
            var receipt = Receipt(c.Original); var source = c.Original.holdingOwner;
            injected = c.Original;
            try
            {
                using var patch = new Fault(typeof(PawnComponentsUtility).GetMethod(nameof(PawnComponentsUtility.AddComponentsForSpawn))!, nameof(FailSpawn));
                Check.Equal("RecoveryRequired", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "native partial spawn safely rolls back");
            }
            finally { injected = null; }
            Check.Equal(true, sawMapCustody, "injection occurs after actual native map ownership");
            Check.Same(source!, c.Original.holdingOwner, "exact source native owner restored");
            Check.Equal(false, c.Original.Spawned, "native rollback despawns original");
            Check.Equal(false, c.Map.spawnedThings.Contains(c.Original), "map no longer owns original");
            Check.Same(spirit, c.Original.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "existing zero need owner survives failed preparation");
            Check.Equal(0f, spirit.CurLevel, "failed release does not refill zero");
            AcceptFaults("injected release spawn");
            receipt = RoundTripReceipt(r, c, receipt);
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "saved paid preparation retries native release");
            Check.Equal(0f, spirit.CurLevel, "saved retry never initializes retained zero again");
            Check.Equal(true, c.Magicyte.Destroyed && c.Silver.Destroyed, "retry never repays or refunds resources");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.RejectInvalidDestinationAndForeignCreator", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "real paid conversion precedes release");
            var receipt = Receipt(c.Original); var owner = c.Original.holdingOwner;
            Check.Equal("Rejected", Release(receipt, c.Creator, c.Map, IntVec3.Invalid), "invalid cell rejects before transfer");
            Check.Equal("Rejected", Release(receipt, c.Original, c.Map, c.Creator.Position), "foreign creator cannot release receipt");
            Check.Same(owner!, c.Original.holdingOwner, "rejections retain exact native carry owner");
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "rejections cannot activate");
            Check.Equal(true, c.Magicyte.Destroyed && c.Silver.Destroyed, "release does not repay or refund conversion");
            Set(receipt, "creator", null);
            Check.Equal("Rejected", Release(receipt, null!, c.Map, c.Creator.Position), "lost creator reference and null caller fail closed");
            Set(receipt, "creator", c.Creator);
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.CompletedMapFailureResumesWithoutRespawn", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original); injected = c.Original;
            try
            {
                using var commit = new Fault(typeof(SpiritRebirthState).GetMethod("TryCommitRitualRelease", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(FailCommit));
                using var despawn = new Fault(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!, nameof(FailDespawn));
                Check.Equal("RecoveryRequired", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "completed spawn with failed rollback remains explicitly recoverable");
            }
            finally { injected = null; }
            Check.Equal(true, c.Original.Spawned, "failed despawn leaves native map owner intact");
            Check.Same(c.Map.spawnedThings, c.Original.holdingOwner, "no second holder added after failed despawn");
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "failed commit remains inactive");
            AcceptFaults("injected release");
            Set(c.Creator, "mapIndexOrState", (sbyte)-1);
            Check.Equal("Rejected", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "despawned creator cannot commit map recovery");
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "invalid recovery creator does not activate");
            Set(c.Creator, "mapIndexOrState", (sbyte)0);
            using var rejectSpawn = new Fault(typeof(GenSpawn).GetMethod(nameof(GenSpawn.Spawn), [typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool)])!, nameof(FailAnySpawn));
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "known completed map spawn commits without another native spawn");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.CleanupInventoryCustodyAndExactRecovery", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original);
            var faction = c.Original.Faction; var mind = c.Original.mindState;
            observed = c.Original; clearMindCalls = 0;
            using var mindObserver = new Fault(typeof(Pawn).GetMethod(nameof(Pawn.ClearMind_NewTemp))!, nameof(ObserveClearMind));
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "pending original retained before native job drops carry");
            Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "native inventory owns pending original");
            Check.Equal(true, c.Creator.carryTracker.CarriedThing == null, "carry is empty before job cleanup");
            c.Creator.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
            Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "actual native cleanup leaves inventory original owned");
            var followup = new Job(new JobDef { defName = "ReleaseFollowup", driverClass = typeof(ReleaseFollowupDriver), dropThingBeforeJob = true });
            c.Creator.jobs.StartJob(followup, addToJobsThisTick: false);
            Check.Same(followup, c.Creator.CurJob, "native next job actually starts");
            Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "native next job drop-carry path cannot drop inventory original");
            Check.Same(faction, c.Original.Faction, "no world transition changes original faction");
            Check.Same(mind, c.Original.mindState, "no world transition clears original mind tracker");
            Check.Equal(0, clearMindCalls, "native ClearMind callback never runs on retained original");
            observed = null;
            Check.Equal(false, SpiritRebirthStateTests.Invoke(receipt, "TryRecoverRelease", c.Original), "foreign creator denied inventory retrieval");
            receipt = RoundTripReceipt(r, c, receipt);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRecoverRelease", c.Creator), "saved exact receipt recovers original to same creator");
            Check.Equal(false, c.Creator.inventory.innerContainer.Contains(c.Original), "inventory no longer owns carried original");
            Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "same actual original recovered");
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "recovered original releases once");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.ExhaustedRecoveryAlwaysClearsGuard", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original); var source = c.Original.holdingOwner;
            injected = c.Original; refusedSource = source; refusedFallback = c.Creator.inventory.innerContainer;
            try
            {
                using var spawn = new Fault(typeof(PawnComponentsUtility).GetMethod(nameof(PawnComponentsUtility.AddComponentsForSpawn))!, nameof(FailSpawn));
                using var restore = new Fault(typeof(ThingOwner<Thing>).GetMethod("TryAdd", [typeof(Thing), typeof(bool)])!, nameof(FailRestore));
                bool failed = false;
                try { Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)); }
                catch (InvalidOperationException e) { failed = e.Message.Contains("injected release inventory fallback"); }
                Check.Equal(true, failed, "all native custody failures surface a hard error, never false retention success");
            }
            finally { injected = null; refusedSource = null; refusedFallback = null; }
            Check.Equal(false, c.Original.Spawned, "native despawn happened before injected recovery failures");
            Check.Equal(true, source!.TryAdd(c.Original, false), "fixture restores exact original after impossible injected all-owner failure");
            AcceptFaults("injected release");
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "outer guard was cleared despite fallback exception");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.IncompleteLiveConversionCleanupNeverActivates", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            injected = c.Original;
            try
            {
                using var failure = new Fault(typeof(ResurrectionUtility).GetMethod(nameof(ResurrectionUtility.TryResurrect))!, nameof(FailAfterRevival), true);
                Check.Equal("RecoveryRequired", c.Execute(), "native alive conversion exception remains incomplete");
            }
            finally { injected = null; }
            AcceptFaults("injected release native revival");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "cleanup secures attempted live original before payment completion");
            Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "native inventory accepts actual incomplete original");
            Check.Equal(350, c.Original.inventory.innerContainer.Sum(t => t.stackCount), "unpaid real escrow remains inside original");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRecoverRelease", c.Creator), "same creator recovers incomplete original without payment");
            Check.Equal("Rejected", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "incomplete receipt cannot authorize playable release");
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "recovered unpaid original stays inactive");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.NativeInventoryPawnDeepScribeRecovery", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            var receipt = Receipt(c.Original);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "actual original stored in native inventory");
            SaveInventoryGraph(r, c);
            receipt = Receipt(c.Original);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRecoverRelease", c.Creator), "loaded exact receipt recovers deep-owned original");
            Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "native loaded same identity moves inventory to carry");
            Check.Equal(0, c.Creator.inventory.innerContainer.Count, "no duplicate loaded inventory pawn");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.NativeInventoryDropRemainsDormantAndRecoverable", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "native inventory recovery custody acquired");
            Check.Equal(true, c.Creator.inventory.innerContainer.TryDrop(c.Original, new IntVec3(15, 0, 15), c.Map, ThingPlaceMode.Direct, out var dropped, playDropSound: false), "native inventory can unload the actual pending original");
            Check.Same(c.Original, dropped, "drop never clones original");
            Check.Same(c.Map.spawnedThings, c.Original.holdingOwner, "dropped original has native map custody");
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "automatic drop never grants active form");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRecoverRelease", c.Creator), "receipt retrieves exact previously retained original from native map");
            Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "native despawn restores carry owner");
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "actual recovered pawn can complete preparation and release");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.SpawnCallbackInvalidatesCreatorBeforeCommit", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original); var source = c.Original.holdingOwner;
            injected = c.Original; callbackCreator = c.Creator;
            try
            {
                using var callback = new Fault(typeof(Pawn).GetMethod(nameof(Pawn.SpawnSetup))!, nameof(InvalidateCreator), true);
                Check.Equal("RecoveryRequired", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "creator invalidated by real spawn callback cannot commit");
            }
            finally { injected = null; callbackCreator = null; Set(c.Creator, "mapIndexOrState", (sbyte)0); }
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "failed final authority check leaves form inactive");
            Check.Same(source!, c.Original.holdingOwner, "native spawn rollback restores exact carry source");
            Check.Equal("Released", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "valid creator can retry after native rollback");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRebirthRelease.PreparationCallbackBlockerPreventsNativeWipe", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.Equal("ConvertedHeld", c.Execute(), "paid native conversion");
            using var setup = new ReleaseFixture(r, c);
            var receipt = Receipt(c.Original); var source = c.Original.holdingOwner;
            injected = c.Original; callbackMap = c.Map; blocker = new Thing { def = c.Magicyte.def, stackCount = 1, thingIDNumber = 69777 }; pawnSpawns = 0;
            try
            {
                using var callback = new Fault(typeof(Pawn_HealthTracker).GetMethods().Single(m => m.Name == nameof(Pawn_HealthTracker.AddHediff) && m.GetParameters()[0].ParameterType == typeof(Hediff)), nameof(InsertBlocker), true);
                using var observer = new Fault(typeof(GenSpawn).GetMethod(nameof(GenSpawn.Spawn), [typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool)])!, nameof(CountPawnSpawn));
                Check.Equal("RecoveryRequired", Release(receipt, c.Creator, c.Map, new IntVec3(15, 0, 15)), "callback-populated destination refuses release");
                Check.Equal(0, pawnSpawns, "revalidation prevents even entering native Pawn spawn/wipe");
                Check.Equal(true, blocker.Spawned && !blocker.Destroyed, "actual newly inserted blocker remains untouched");
            }
            finally { injected = null; callbackMap = null; blocker = null; }
            Check.Same(source!, c.Original.holdingOwner, "blocked release preserves native carry source");
            Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "blocked release never activates");
            RuntimeLog.AssertClean();
        }));
    }

    static Pawn? injected;
    static Pawn? observed;
    static Pawn? callbackCreator;
    static Map? callbackMap;
    static Thing? blocker;
    static int pawnSpawns;
    static void InvalidateCreator(Pawn __instance) { if (ReferenceEquals(__instance, injected)) Set(callbackCreator!, "mapIndexOrState", (sbyte)-1); }
    static void InsertBlocker(Hediff __0)
    {
        if (ReferenceEquals(__0.pawn, injected) && __0.def.defName == "TM_SpiritRebirthForm" && blocker != null && !blocker.Spawned)
            GenSpawn.Spawn(blocker, new IntVec3(15, 0, 15), callbackMap);
    }
    static void CountPawnSpawn(Thing newThing) { if (ReferenceEquals(newThing, injected)) pawnSpawns++; }
    static int clearMindCalls;
    static void ObserveClearMind(Pawn __instance) { if (ReferenceEquals(__instance, observed)) clearMindCalls++; }
    public sealed class ReleaseFollowupDriver : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        protected override IEnumerable<Toil> MakeNewToils() { yield return new Toil { defaultCompleteMode = ToilCompleteMode.Delay, defaultDuration = 60 }; }
    }
    static bool sawMapCustody;
    static ThingOwner? refusedSource, refusedFallback;
    static void FailRestore(ThingOwner __instance)
    {
        if (ReferenceEquals(__instance, refusedSource)) throw new InvalidOperationException("injected release carry restore");
        if (ReferenceEquals(__instance, refusedFallback)) throw new InvalidOperationException("injected release inventory fallback");
    }
    static void FailCommit(Pawn pawn) { if (ReferenceEquals(pawn, injected)) throw new InvalidOperationException("injected release commit"); }
    static void FailDespawn(Pawn __instance) { if (ReferenceEquals(__instance, injected)) throw new InvalidOperationException("injected release despawn"); }
    static void FailAnySpawn() => throw new InvalidOperationException("unexpected duplicate native spawn");
    static void FailAfterRevival(Pawn pawn) { if (ReferenceEquals(pawn, injected)) throw new InvalidOperationException("injected release native revival"); }
    static void FailSpawn(Pawn pawn)
    {
        if (!ReferenceEquals(pawn, injected)) return;
        sawMapCustody = pawn.Spawned && ReferenceEquals(pawn.holdingOwner, pawn.Map.spawnedThings) && pawn.Map.spawnedThings.Contains(pawn);
        Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(pawn).SpiritRebirth.Form, "native callback cannot observe active casting form");
        throw new InvalidOperationException("injected release spawn");
    }
    static void AcceptFaults(string text)
    {
        var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check.Equal(true, problems.Count > 0 && problems.All(p => p.Contains(text)), "only exact injected boundary diagnostics");
        RuntimeLog.Clear();
    }
    sealed class Fault : IDisposable
    {
        readonly object harmony; readonly string id = "rwom.release-fault." + Guid.NewGuid();
        public Fault(MethodBase target, string prefix, bool postfix = false)
        {
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthReleaseTests).GetMethod(prefix, BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, postfix ? null : method, postfix ? method : null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static object RoundTripReceipt(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c, object receipt)
    {
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try
        {
            foreach (var def in new[] { c.Magicyte.def, c.Silver.def })
                if (DefDatabase<ThingDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<ThingDef>.Add(def);
            IExposable saved = (IExposable)receipt;
            var path = Path.Combine(r.WorkingDirectory, "release-preparation-receipt.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "receipt"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            IExposable loaded = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "receipt");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach (var item in new ILoadReferenceable[] { c.Original, c.Creator, c.Circle, c.Map }) directory.RegisterLoaded(item);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            Set(ClassIdentity.Component(c.Original), "spiritRitual", loaded);
            Check.Same(c.Original, loaded.GetType().GetProperty("Original", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loaded), "native receipt cross-reference resolves exact owned original");
            RuntimeLog.AssertClean();
            return loaded;
        }
        finally { DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def); }
    }

    static void SaveInventoryGraph(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c)
    {
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try
        {
            // Same native owner graph as the transaction Scribe fixture. Keep the
            // actual original, its health/inventory, progression and receipt;
            // optional trackers are covered by separate native identity cases.
            var pawn = c.Original; var identity = pawn.GetUniqueLoadID(); var comp = ClassIdentity.Component(pawn);
            foreach (var field in typeof(Pawn).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (!field.IsInitOnly && field.FieldType.Name.StartsWith("Pawn_") && field.Name != "inventory" && field.Name != "health") field.SetValue(pawn, null);
            pawn.Name = null; pawn.kindDef = null; Set(pawn, "factionInt", null); pawn.health.hediffSet.hediffs.Clear();
            pawn.def.thingClass = typeof(Pawn); pawn.def.category = ThingCategory.Pawn;
            pawn.def.comps = [new CompProperties { compClass = typeof(CompClassProgression) }]; Set(pawn, "comps", new List<ThingComp> { comp });
            foreach (var def in new[] { pawn.def, c.Magicyte.def, c.Silver.def })
                if (DefDatabase<ThingDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<ThingDef>.Add(def);
            c.Map.uniqueID = 654;
            var inventory = c.Creator.inventory;
            var path = Path.Combine(r.WorkingDirectory, "release-inventory-original.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref inventory, "inventory", c.Creator); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            Pawn_InventoryTracker loaded = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "inventory", c.Creator);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(c.Creator); directory.RegisterLoaded(c.Circle); directory.RegisterLoaded(c.Map);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            c.Creator.inventory = loaded; c.Original = (Pawn)loaded.innerContainer.Single();
            Check.Equal(identity, c.Original.GetUniqueLoadID(), "actual Pawn deep-save retains original identity");
            Check.Same(loaded.innerContainer, c.Original.holdingOwner, "loaded original has reciprocal native inventory custody");
            Check.Same(c.Original, Receipt(c.Original).GetType().GetProperty("Original", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Receipt(c.Original)), "receipt resolves to exact deep-owned original");
            RuntimeLog.AssertClean();
        }
        finally { DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def); }
    }

    internal sealed class ReleaseFixture : IDisposable
    {
        readonly IDisposable powers, health;
        readonly FleshStatTests.ProductionPatches patches = new();
        readonly NeedDef[] oldNeeds = DefDatabase<NeedDef>.AllDefsListForReading.ToArray();
        readonly NeedDef oldMana = TorannMagicDefOf.TM_Mana, oldSpirit = TorannMagicDefOf.TM_SpiritND;
        readonly FieldInfo mainThread = typeof(UnityData).GetField("mainThreadId", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? oldThread;
        public ReleaseFixture(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c)
        {
            oldThread = mainThread.GetValue(null); mainThread.SetValue(null, Environment.CurrentManagedThreadId);
            powers = (IDisposable)Activator.CreateInstance(typeof(SpiritRebirthFormTests).GetNestedType("Definitions", BindingFlags.NonPublic)!, [r])!;
            health = (IDisposable)Activator.CreateInstance(typeof(SpiritRebirthHealthFormTests).GetNestedType("Definitions", BindingFlags.NonPublic)!, [r, false])!;
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(NeedDefOf));
            DefDatabase<NeedDef>.Clear();
            NeedDef Need(string name, Type type) { var d = new NeedDef { defName = name, needClass = type, colonistsOnly = true }; DefDatabase<NeedDef>.Add(d); return d; }
            TorannMagicDefOf.TM_SpiritND = Need("ReleaseSpirit", typeof(Need_Spirit));
            TorannMagicDefOf.TM_Mana = Need("ReleaseMana", typeof(Need_Mana));
            patches.Add(typeof(TorannMagicMod.Pawn_NeedsTracker_Patch));
            var pawn = c.Original; var map = c.Map;
            pawn.def.category = ThingCategory.Pawn; pawn.def.thingClass = typeof(Pawn); pawn.def.size = new IntVec2(1, 1);
            pawn.def.comps ??= []; pawn.def.passability = Traversability.Standable; pawn.def.drawerType = DrawerType.RealtimeOnly;
            pawn.jobs = new Pawn_JobTracker(pawn);
            c.Creator.stances = new Pawn_StanceTracker(c.Creator);
            pawn.rotationTracker = new Pawn_RotationTracker(pawn);
            Set(Current.Game, "gameEnder", new GameEnder());
            Find.World.worldObjects = new RimWorld.Planet.WorldObjectsHolder();
            Find.World.storyState = new StoryState(Find.World);
            map.pawnDestinationReservationManager = new PawnDestinationReservationManager();
            map.enrouteManager = new EnrouteManager(map); map.attackTargetReservationManager = new AttackTargetReservationManager(map);
            map.mapPawns = new MapPawns(map); map.autoSlaughterManager = new AutoSlaughterManager(map);
            map.fogGrid = new FogGrid(map);
        }
        public void Dispose()
        {
            patches.Dispose(); health.Dispose(); powers.Dispose();
            TorannMagicDefOf.TM_Mana = oldMana; TorannMagicDefOf.TM_SpiritND = oldSpirit;
            DefDatabase<NeedDef>.Clear(); foreach (var d in oldNeeds) DefDatabase<NeedDef>.Add(d);
            mainThread.SetValue(null, oldThread);
        }
    }
}
