using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthActionTests
{
    public static IEnumerable<TestCase> Cases() => CastSafetyTests.SpiritActionCases().Concat(RaceCases());
    internal static Type? Policy => typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthActionPolicy");
    internal static bool Allows(Pawn pawn, Job job) => Policy == null || (bool)Policy.GetMethod("Allows", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [pawn, job])!;
    internal static void Patch(FleshStatTests.ProductionPatches patches, string name)
    {
        if (typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression." + name) is Type type) patches.Add(type);
    }
    internal sealed class MovementDefinitions : IDisposable
    {
        readonly Dictionary<FieldInfo, object?> previous = new();
        public MovementDefinitions()
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(JobDefOf));
            foreach (string name in new[] { "Goto", "Wait", "Wait_MaintainPosture", "Wait_Combat" })
            {
                var field = typeof(JobDefOf).GetField(name)!; previous[field] = field.GetValue(null);
                field.SetValue(null, new JobDef { defName = name });
            }
        }
        public void Dispose() { foreach (var entry in previous) entry.Key.SetValue(null, entry.Value); }
    }

    public sealed class ProbeGiver : HediffGiver
    {
        public int Intervals, Added;
        public override void OnIntervalPassed(Pawn pawn, Hediff cause) => Intervals++;
        public override bool OnHediffAdded(Pawn pawn, Hediff hediff) { Added++; return true; }
    }
    public sealed class ProbeHediff : Hediff
    {
        public int Ticks, Intervals;
        public override void Tick() => Ticks++;
        public override void TickInterval(int delta) => Intervals++;
    }
    public sealed class ProbeDriver : JobDriver
    {
        public static int Reservations, Starts;
        public override bool TryMakePreToilReservations(bool errorOnFailed) { Reservations++; return true; }
        public override void SetInitialPosture() { }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var toil = ToilMaker.MakeToil("SpiritActionProbe");
            toil.defaultCompleteMode = ToilCompleteMode.Never; toil.initAction = () => Starts++;
            yield return toil;
        }
    }
    static IEnumerable<TestCase> RaceCases()
    {
        yield return new("SpiritRebirthAction.NativeRaceDispatchPreservesHealthTicks", r =>
        {
            using var f = (IDisposable)Activator.CreateInstance(typeof(LichTransformationTests).GetNestedType("Fixture", BindingFlags.NonPublic)!, r, InitiationStage.Ascended)!;
            var pawn = (Pawn)f.GetType().GetField("Pawn")!.GetValue(f)!;
            var comp = (CompClassProgression)f.GetType().GetField("Progression")!.GetValue(f)!;
            var priorities = (DefMap<WorkTypeDef, int>)f.GetType().GetField("Priorities")!.GetValue(f)!;
            var workDefs = (WorkTypeDef[])f.GetType().GetField("WorkDefs")!.GetValue(f)!;
            var savedPriorities = workDefs.Select(d => priorities[d]).ToArray();
            var savedTimetable = Enumerable.Range(0, 24).Select(pawn.timetable.GetAssignment).ToArray();
            var savedState = comp.State;
            Set(pawn, "mapIndexOrState", (sbyte)-1); pawn.thingIDNumber = 600;
            Set(pawn.def.race, "fleshType", new FleshTypeDef { defName = "SpiritActionFlesh", isOrganic = false });
            pawn.story = null; // No trait disease source in this race-dispatch fixture.
            using var patches = new FleshStatTests.ProductionPatches();
            Patch(patches, "SpiritRebirthRaceIntervalPatch"); Patch(patches, "SpiritRebirthRaceAddedPatch");
            var giver = new ProbeGiver();
            pawn.def.race.hediffGiverSets = [new HediffGiverSetDef { defName = "SpiritActionRace", hediffGivers = [giver] }];
            var hd = new HediffDef { defName = "SpiritActionHealthProbe", hediffClass = typeof(ProbeHediff), initialSeverity = .5f };
            var first = new ProbeHediff { pawn = pawn, def = hd, Severity = .5f };
            pawn.health.AddHediff(first);
            Check.Equal(1, giver.Added, "unmarked native OnHediffAdded dispatch");
            pawn.health.HealthTickInterval(60);
            Check.Equal(1, giver.Intervals, "unmarked native interval dispatch");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(comp, "BeginSpiritRebirth"), "mark race subject");
            pawn.health.AddHediff(new ProbeHediff { pawn = pawn, def = new HediffDef { defName = "SpiritActionSecond", initialSeverity = .5f }, Severity = .5f });
            Check.Equal(1, giver.Added, "marked race OnHediffAdded suppressed");
            int ticks = first.Ticks, intervals = first.Intervals;
            pawn.health.HealthTick(); pawn.health.HealthTickInterval(60);
            Check.Equal(1, giver.Intervals, "marked race interval suppressed");
            Check.Equal(ticks + 1, first.Ticks, "native health Tick remains live");
            Check.Equal(intervals + 1, first.Intervals, "native health TickInterval remains live");
            Check.Equal(true, pawn.def.race.hediffGiverSets[0].hediffGivers.Contains(giver), "shared race definition retained");
            pawn.jobs = new Pawn_JobTracker(pawn);
            Policy!.GetMethod("ClearExistingJobs", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [pawn]);
            Check.Equal(true, savedPriorities.SequenceEqual(workDefs.Select(d => priorities[d])), "every saved work priority retained");
            Check.Equal(true, savedTimetable.SequenceEqual(Enumerable.Range(0, 24).Select(pawn.timetable.GetAssignment)), "all 24 timetable assignments retained");
            Check.Same(savedState, comp.State, "class/vocation owner retained");
        });
    }
}

internal static partial class CastSafetyTests
{
    public static IEnumerable<TestCase> SpiritActionCases()
    {
        yield return new("SpiritRebirthAction.NativeStartFallbackAndExplicitCleanup", f => World(f, w =>
        {
            using var definitions = new SpiritCastingDefinitions(f);
            using var patches = new FleshStatTests.ProductionPatches();
            SpiritRebirthActionTests.Patch(patches, "SpiritRebirthStartJobPatch");
            SpiritRebirthActionTests.Patch(patches, "SpiritRebirthOpportunisticJobPatch");
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(JobDefOf));
            var oldWait = JobDefOf.Wait;
            try
            {
                JobDefOf.Wait = new JobDef { defName = "Wait", driverClass = typeof(SpiritRebirthActionTests.ProbeDriver), allowOpportunisticPrefix = false };
                var physical = new JobDef { defName = "SpiritActionPhysical", driverClass = typeof(SpiritRebirthActionTests.ProbeDriver), allowOpportunisticPrefix = false };
                w.Caster.jobs.curJob = null;
                w.Caster.AllComps.Add(new CompAbilityUserMagic { parent = w.Caster });
                w.Caster.stances = new Pawn_StanceTracker(w.Caster);
                w.Caster.carryTracker = new Pawn_CarryTracker(w.Caster);
                w.Map.events = new MapEvents(w.Map); w.Map.lordManager = new Verse.AI.Group.LordManager(w.Map);
                SpiritRebirthActionTests.ProbeDriver.Reservations = SpiritRebirthActionTests.ProbeDriver.Starts = 0;
                w.Caster.jobs.StartJob(new Job { def = physical }, addToJobsThisTick: false);
                Check.Equal(1, SpiritRebirthActionTests.ProbeDriver.Starts, "unmarked native driver starts normally");
                w.Caster.jobs.StopAll(canReturnToPool: false);
                MarkSpirit(w.Caster);
                w.Caster.jobs.StartJob(new Job { def = physical }, addToJobsThisTick: false);
                Check.Same(JobDefOf.Wait, w.Caster.CurJobDef, "forbidden native StartJob becomes bounded wait");
                Check.Equal(true, w.Caster.CurJob.expiryInterval > 0, "fallback has bounded expiry");
                int starts = SpiritRebirthActionTests.ProbeDriver.Starts;
                for (int i = 0; i < 15; i++) w.Caster.jobs.StartJob(new Job { def = physical }, addToJobsThisTick: false);
                Check.Equal(starts, SpiritRebirthActionTests.ProbeDriver.Starts, "repeated refused jobs preserve active wait without churn");
                w.Caster.jobs.jobQueue.EnqueueLast(new Job { def = physical });
                Check.NotNull(SpiritRebirthActionTests.Policy, "explicit policy exists").GetMethod("ClearExistingJobs", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [w.Caster]);
                Check.Equal(0, w.Caster.jobs.jobQueue.Count, "explicit cleanup clears queued jobs");
                Check.Equal(true, w.Caster.CurJob == null, "explicit cleanup stops current job without replacement");
                w.Caster.jobs.jobQueue.EnqueueLast(new Job { def = physical });
                SpiritRebirthActionTests.Policy!.GetMethod("ClearExistingJobs", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [w.Caster]);
                Check.Equal(0, w.Caster.jobs.jobQueue.Count, "queued-only saved jobs also cleared");
            }
            finally { JobDefOf.Wait = oldWait; }
        }));
        yield return new("SpiritRebirthAction.NativeOrderedRejectionBeforeMutation", f => World(f, w =>
        {
            using var definitions = new SpiritCastingDefinitions(f);
            using var patches = new FleshStatTests.ProductionPatches();
            SpiritRebirthActionTests.Patch(patches, "SpiritRebirthOrderedJobPatch");
            Check.Equal(true, w.Caster.jobs.TryTakeOrderedJob(w.Job), "unmarked native same-job admission unchanged");
            MarkSpirit(w.Caster);
            w.Job.playerForced = false;
            Check.Equal(false, w.Caster.jobs.TryTakeOrderedJob(w.Job, requestQueueing: true), "marked former job rejected before native admission");
            Check.Equal(false, w.Job.playerForced, "rejected order not mutated");
            Check.Equal(0, w.Caster.jobs.jobQueue.Count, "rejected order not queued");
            Check.Same(w.Job, w.Caster.CurJob, "rejected order does not cancel current job");
        }));
        yield return new("SpiritRebirthAction.ExactOwnedSpiritJobsAndForms", f => World(f, w =>
        {
            using var definitions = new SpiritCastingDefinitions(f);
            using var movementDefinitions = new SpiritRebirthActionTests.MovementDefinitions();
            var marker = MarkSpirit(w.Caster);
            Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, w.Job), "former ability job denied");
            var magic = new CompAbilityUserMagic { parent = w.Caster }; w.Caster.AllComps.Add(magic);
            var ability = new MagicAbility(magic) { Def = TorannMagicDefOf.TM_SpiritDrain };
            w.Verb.Ability = ability; Set(ability, "verb", w.Verb); magic.AbilityData.Powers.Add(ability);
            Check.Equal(true, SpiritRebirthActionTests.Allows(w.Caster, w.Job), "owned exact Spirit job admitted");
            Check.Equal(true, SpiritRebirthActionTests.Allows(w.Caster, new Job { def = JobDefOf.Goto }), "active original may move");
            Check.Equal(true, SpiritRebirthActionTests.Allows(w.Caster, new Job { def = JobDefOf.Wait }), "active original may wait");
            Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, new Job { def = JobDefOf.Wait_Combat }), "combat wait cannot restore ordinary attacks");
            w.Verb.caster = w.Target;
            Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, w.Job), "foreign caster denied despite owned ability");
            w.Verb.caster = w.Caster;
            var physical = new Job { def = new JobDef { defName = "SpiritActionHaul" }, verbToUse = w.Verb };
            Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, physical), "Spirit verb cannot authorize unrelated physical job");
            magic.AbilityData.Powers.Remove(ability);
            Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, w.Job), "unowned runtime denied");
            magic.AbilityData.Powers.Add(ability);
            foreach (var form in new[] { SpiritRebirthForm.Dormant, SpiritRebirthForm.Possessed, SpiritRebirthForm.Stone, (SpiritRebirthForm)999 })
            {
                Set(marker, "form", form);
                Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, w.Job), "inactive form cannot act");
            }
            Set(marker, "form", SpiritRebirthForm.Ethereal); Set(marker, "version", -1);
            Check.Equal(false, SpiritRebirthActionTests.Allows(w.Caster, new Job { def = JobDefOf.Goto }), "unsupported marker cannot move");
            Check.Equal(true, SpiritRebirthActionTests.Allows(w.Target, physical), "unmarked host or legacy unchanged");
        }));
    }
}
