using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthRecoveryJobTests
{
    static Type Production(string name) => Check.NotNull(typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression." + name), "internal paid recovery " + name + " exists");
    public static IEnumerable<TestCase> Cases()
    {
        foreach (string origin in new[] { "Inventory", "CarryDroppedByPriorJob", "UnloadedToMap", "CancelledAndRetried", "CircleChangedDuringSpawn", "JobReplacedDuringSpawn" })
        yield return new("SpiritRecoveryJob.NativePaidRelease." + origin, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Production("SpiritRebirthRecoveryJob");
            Check.Equal("ConvertedHeld", c.Execute(), "real original paid and resurrected before recovery job");
            using var fixture = new Fixture(r, c);
            using var observations = new NativeObservations(c);
            var receipt = Receipt(c.Original);
            if (origin != "CarryDroppedByPriorJob")
                Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "receipt secures original to native inventory");
            if (origin == "UnloadedToMap")
                Check.Equal(true, c.Creator.inventory.innerContainer.TryDrop(c.Original, new IntVec3(11, 0, 9), c.Map, ThingPlaceMode.Direct, out _, playDropSound: false), "actual inventory unload places same pending original on map");
            var job = Check.NotNull(Create(c.Creator, c.Original), "paid exact-owner recovery accepted");
            c.Creator.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
            try { c.Creator.jobs.StartJob(job, addToJobsThisTick: false); }
            catch { RuntimeLog.AssertClean(); throw; }
            Check.Same(job, c.Creator.CurJob, "native StartJob installs recovery");
            Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "native recovery toils acquire exact original");
            if (origin == "Inventory" || origin == "CancelledAndRetried")
            {
                var oldJob = job;
                SaveJob(r, c);
                job = c.Creator.CurJob;
                Check.Equal(false, ReferenceEquals(oldJob, job), "native recovery job deep-loaded");
                Check.Same(job, c.Creator.jobs.curDriver.job, "native tracker binds loaded recovery driver");
                Check.Same(c.Original, job.targetB.Thing, "loaded job resolves exact externally owned original");
                Check.Same(c.Original, c.Creator.carryTracker.CarriedThing, "save has no ownership mutation");
            }
            if (origin == "CancelledAndRetried")
            {
                c.Creator.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
                c.Creator.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), addToJobsThisTick: false);
                Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "cancel cleanup and next native job preserve inactive original");
                Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "cancellation does not release");
                Check.Equal(0, NativeObservations.resurrections + NativeObservations.payments, "cancellation causes no irreversible effects");
                var retry = Check.NotNull(Create(c.Creator, c.Original), "same creator may order one later recovery");
                c.Creator.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
                c.Creator.jobs.StartJob(retry, addToJobsThisTick: false);
            }
            if (origin == "CircleChangedDuringSpawn") NativeObservations.changedCircle = c.Circle;
            if (origin == "JobReplacedDuringSpawn") NativeObservations.replaceJob = true;
            Set(Find.TickManager, "ticksGameInt", 1); c.Creator.jobs.JobTrackerTick();
            if (origin == "CircleChangedDuringSpawn" || origin == "JobReplacedDuringSpawn")
            {
                Check.Equal(1, NativeObservations.spawnCallbacks, "actual native spawn callback changed context");
                Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "changed circle cannot commit Ethereal");
                Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "guard rejection retains same original through native cleanup");
                if (origin == "JobReplacedDuringSpawn")
                    Check.Same(NativeObservations.replacement, c.Creator.CurJob, "old recovery driver must preserve unrelated replacement job");
                Check.Equal(null, receipt.GetType().GetField("releaseCommitGuard", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(receipt), "transient guard cleared after rejected final commit");
                Check.Equal(0, NativeObservations.resurrections + NativeObservations.payments, "guard rejection never repeats irreversible effects");
                RuntimeLog.AssertClean(); return;
            }
            Check.Equal(SpiritRebirthForm.Ethereal, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "one observed recovery tick releases without repeating work");
            Check.Same(c.Map.spawnedThings, c.Original.holdingOwner, "same original native map owner");
            Check.Equal(null, Create(c.Creator, c.Original), "completed receipt cannot order duplicate recovery");
            Check.Same(receipt, Receipt(c.Original), "same paid receipt reused");
            Check.Equal(0, NativeObservations.resurrections, "native resurrection boundary never called by recovery");
            Check.Equal(0, NativeObservations.payments, "native resource destruction never repeated by recovery");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRecoveryJob.RejectsForeignOwnershipAndUnpaid", r =>
        {
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                Check.Equal("ConvertedHeld", c.Execute(), "paid original fixture");
                using var fixture = new Fixture(r, c);
                using var observations = new NativeObservations(c);
                var receipt = Receipt(c.Original);
                Check.Equal(true, SpiritRebirthStateTests.Invoke(receipt, "TryRetainReleaseForCleanup", c.Creator), "authorized original inventory custody");
                var foreign = PawnWithComps(false, false).pawn; foreign.inventory = new Pawn_InventoryTracker(foreign);
                Check.Equal(null, Create(foreign, c.Original), "foreign creator cannot adopt paid receipt");
                Check.Equal(null, Create(null!, c.Original), "null creator rejected");
                c.Creator.inventory.innerContainer.Remove(c.Original);
                Check.Equal(true, foreign.inventory.innerContainer.TryAdd(c.Original, false), "actual foreign native owner fixture");
                Check.Equal(null, Create(c.Creator, c.Original), "original creator cannot retrieve pawn from foreign owner");
                Check.Same(foreign.inventory.innerContainer, c.Original.holdingOwner, "rejection does not steal native custody");
                Check.Equal(0, NativeObservations.resurrections + NativeObservations.payments, "invalid orders have no irreversible native calls");
                RuntimeLog.AssertClean();
            });
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                typeof(SpiritRebirthRitualTransactionTests).GetMethod("AliveException", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c]);
                using var fixture = new Fixture(r, c);
                using var observations = new NativeObservations(c);
                Check.Equal(null, Create(c.Creator, c.Original), "actual incomplete resurrection receipt cannot receive free recovery activation");
                Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "unpaid pending original stays inactive");
                Check.Equal(0, NativeObservations.resurrections + NativeObservations.payments, "unpaid rejection has no irreversible native calls");
                RuntimeLog.AssertClean();
            });
        });
        yield return new("SpiritRecoveryJob.DefinitionContract", r =>
        {
            var root = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/JobDefs/TM_SpiritRebirth.xml")).Root!;
            var def = root.Elements("JobDef").Single(x => (string?)x.Element("defName") == "TM_SpiritRebirthRecovery");
            Check.Equal("TorannMagic.ClassProgression.JobDriver_SpiritRebirthRecovery", (string?)def.Element("driverClass"), "actual XML binds recovery driver");
            Check.Equal(false, (bool)def.Element("dropThingBeforeJob")!, "actual recovery XML preserves carried original");
            Check.Equal(true, (bool)def.Element("carryThingAfterJob")!, "actual recovery XML defensive cleanup boundary");
        });
    }
    static void SaveJob(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c)
    {
        var path = Path.Combine(r.WorkingDirectory, "paid-recovery-native-job.xml");
        JobGraph graph = new(c.Creator, c.Map);
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref graph, "graph", c.Creator, c.Map); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        graph = null!;
        try
        {
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref graph, "graph", c.Creator, c.Map);
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
            foreach (var anchor in c.Map.spawnedThings.Cast<ILoadReferenceable>().Concat(new ILoadReferenceable[] { c.Map, c.Original }).Distinct()) directory.RegisterLoaded(anchor);
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
    }
    public sealed class JobGraph : IExposable
    {
        readonly Pawn pawn; readonly Map map;
        Pawn_JobTracker jobs; ReservationManager reservations;
        public JobGraph(Pawn pawn, Map map) { this.pawn = pawn; this.map = map; jobs = pawn.jobs; reservations = map.reservationManager; }
        public void ExposeData()
        {
            Scribe_Deep.Look(ref jobs, "jobs", pawn); Scribe_Deep.Look(ref reservations, "reservations", map);
            if (Scribe.mode == LoadSaveMode.LoadingVars) { pawn.jobs = jobs; map.reservationManager = reservations; }
        }
    }
    static object Receipt(Pawn pawn) => ClassIdentity.Component(pawn).GetType().GetProperty("SpiritRitual", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ClassIdentity.Component(pawn))!;
    static Job? Create(Pawn creator, Pawn original) => (Job?)Production("SpiritRebirthRecoveryJob").GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [creator, original]);
    sealed class Fixture : IDisposable
    {
        readonly JobDef[] jobs = DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        readonly JobDef wait = JobDefOf.Wait_MaintainPosture;
        readonly SpiritRebirthReleaseTests.ReleaseFixture release;
        readonly PathFinder finder;
        public Fixture(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c)
        {
            release = new(r, c);
            DefDatabase<JobDef>.Add(new JobDef { defName = "TM_SpiritRebirthRecovery", driverClass = Production("JobDriver_SpiritRebirthRecovery"), dropThingBeforeJob = false, carryThingAfterJob = true });
            JobDefOf.Wait_MaintainPosture = new JobDef { defName = "RecoveryWait", driverClass = typeof(JobDriver_Wait) }; DefDatabase<JobDef>.Add(JobDefOf.Wait_MaintainPosture);
            c.Job.def.driverClass = typeof(JobDriver_Wait);
            c.Creator.ageTracker = new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }]; Set(c.Creator.ageTracker, "cachedLifeStageIndex", 0);
            c.Creator.thinker = new Pawn_Thinker(c.Creator); c.Creator.def.race.thinkTreeMain = new ThinkTreeDef { thinkRoot = new ThinkNode_Priority() };
            c.Creator.pather = new Pawn_PathFollower(c.Creator);
            c.Map.reachability = new Reachability(c.Map);
            c.Map.resourceCounter = new ResourceCounter(c.Map);
            var region = Region.MakeNewUnfilled(new IntVec3(0, 0, 0), c.Map); region.extentsClose = region.extentsLimit = new CellRect(0, 0, 30, 30);
            var grid = (Region[])typeof(RegionGrid).GetField("regionGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(c.Map.regionGrid)!;
            for (int i = 0; i < grid.Length; i++) grid[i] = region;
            finder = new PathFinder(c.Map); c.Map.pathFinder = finder;
        }
        public void Dispose() { finder.Dispose(); release.Dispose(); JobDefOf.Wait_MaintainPosture = wait; DefDatabase<JobDef>.Clear(); foreach (var def in jobs) DefDatabase<JobDef>.Add(def); }
    }
    sealed class NativeObservations : IDisposable
    {
        readonly object harmony; readonly string id = "rwom.recovery-observe." + Guid.NewGuid();
        static Pawn? original, creator; static ThingDef? magicyte, silver;
        internal static Building_TMMagicCircle? changedCircle;
        internal static bool replaceJob;
        internal static Job? replacement;
        internal static int spawnCallbacks;
        internal static int resurrections, payments;
        static void Resurrect(Pawn pawn) { if (ReferenceEquals(pawn, original)) resurrections++; }
        static void Destroy(Thing __instance) { if (__instance.def == magicyte || __instance.def == silver) payments++; }
        static void Spawn(Pawn pawn)
        {
            if (!ReferenceEquals(pawn, original) || (changedCircle == null && !replaceJob)) return;
            spawnCallbacks++;
            var receipt = Receipt(pawn);
            var field = receipt.GetType().GetField("releaseCommitGuard", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var guard = Check.NotNull(field.GetValue(receipt), "outer release guard active at actual native callback");
            var args = new object[] { creator!, creator!.Map, pawn.Position };
            Check.Equal("Rejected", receipt.GetType().GetMethod("TryRelease", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(receipt, args)!.ToString(), "nested direct release rejected");
            Check.Equal("Rejected", receipt.GetType().GetMethod("TryReleaseWithGuard", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(receipt, [creator, creator.Map, pawn.Position, (Func<bool>)(() => true)])!.ToString(), "nested guarded release rejected");
            Check.Same(guard, field.GetValue(receipt), "nested rejection preserves exact outer guard");
            if (replaceJob)
            {
                replacement = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
                creator.jobs.StartJob(replacement, JobCondition.InterruptForced, addToJobsThisTick: false);
            }
            else changedCircle!.MageList.Add(creator);
        }
        public NativeObservations(SpiritRebirthRitualTransactionTests.Context c)
        {
            original = c.Original; creator = c.Creator; changedCircle = null; replaceJob = false; replacement = null; magicyte = c.Magicyte.def; silver = c.Silver.def; resurrections = payments = spawnCallbacks = 0;
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            foreach (var pair in new[] { (typeof(ResurrectionUtility).GetMethod(nameof(ResurrectionUtility.TryResurrect))!, nameof(Resurrect)), (typeof(Thing).GetMethod(nameof(Thing.Destroy))!, nameof(Destroy)), (typeof(PawnComponentsUtility).GetMethod(nameof(PawnComponentsUtility.AddComponentsForSpawn))!, nameof(Spawn)) })
            {
                var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(NativeObservations).GetMethod(pair.Item2, BindingFlags.NonPublic | BindingFlags.Static)!)!;
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [pair.Item1, method, null, null, null]);
            }
        }
        public void Dispose() { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]); original = creator = null; changedCircle = null; replaceJob = false; replacement = null; magicyte = silver = null; }
    }
}
