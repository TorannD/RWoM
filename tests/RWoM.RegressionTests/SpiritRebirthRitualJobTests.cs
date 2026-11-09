using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthRitualJobTests
{
    static Type Production(string name) => Check.NotNull(typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression." + name), "internal ordered ritual " + name + " exists");
    static object? Call(object target, string name, params object[] args)
    {
        try { return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args); }
        catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); throw; }
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRitualJob.PlayerCircleOffersEligibleCorpse", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var setup=new Fixture(c);
            using var language=new SpellItemEffectTests.LanguageScope(r.Repository);
            foreach (var text in System.Xml.Linq.XDocument.Load(Path.Combine(r.Repository,"v1.7/Languages/English/Keyed/TM_SpiritRitualOrders.xml")).Root!.Elements())
                LanguageDatabase.activeLanguage.keyedReplacements[text.Name.LocalName]=new LoadedLanguage.KeyedReplacement {key=text.Name.LocalName,value=text.Value};
            c.Map.mapPawns=new MapPawns(c.Map);
            var lib=Assembly.Load("0Harmony");var harmonyType=lib.GetType("HarmonyLib.Harmony")!;
            var id="rwom.regression.spiritmenu."+Guid.NewGuid();var harmony=Activator.CreateInstance(harmonyType,id)!;
            try
            {
                var prefix=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(SpiritRebirthRitualEligibilityTests).GetMethod(nameof(SpiritRebirthRitualEligibilityTests.SkipGraphics))!)!;
                harmonyType.GetMethod("Patch")!.Invoke(harmony,[typeof(FloatMenuOption).GetMethod("SetSizeMode",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),prefix,null,null,null]);
                var options=c.Circle.GetFloatMenuOptions(c.Creator).ToList();
                Check.Equal(true,c.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).Contains(c.Corpse),"corpse present in native request group");
                Check.Equal(true,options.Any(x=>x.Label.Contains(c.Original.LabelShort) && x.Label.Contains("300") && !x.Disabled),"large circle offers the original's ritual with visible cost; options "+string.Join("|",options.Select(x=>x.Label)));
                Check.Equal(true,c.Original.Dead,"opening menu does not revive the original");
                var bond=new Hediff_PsychicBond{def=new HediffDef{defName="PsychicBond",label="psychic bond"},pawn=c.Original,target=c.Creator};
                c.Original.health.hediffSet.hediffs.Add(bond);
                var refused=c.Circle.GetFloatMenuOptions(c.Creator).Single(x=>x.Label.Contains(c.Original.LabelShort) && x.Label.Contains("300"));
                Check.Equal(true,refused.Disabled && refused.Label.Contains("psychic bond",StringComparison.OrdinalIgnoreCase),"unsupported link explained before hauling or ritual work");
                Check.Equal(350,c.Magicyte.stackCount+c.Silver.stackCount,"viewing menu spends nothing");
            }
            finally {harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
        }));
        foreach (string scenario in new[] { "NativeObservedWorkAndCompletion", "CancelledWorkSave", "InterruptedReleaseCleanup", "ReleaseCircleChanged", "ReleaseJobReplaced", "RevivalJobReplaced" })
        yield return new("SpiritRitualJob." + scenario, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            Check.NotNull(Production("JobDriver_SpiritRebirth").GetProperty("WorkTicks", BindingFlags.Instance | BindingFlags.NonPublic), "saved observed work counter exists");
            using var setup = new Fixture(c);
            using var release = new SpiritRebirthReleaseTests.ReleaseFixture(r, c);
            using var pathFinder = new PathFinder(c.Map); c.Map.pathFinder = pathFinder;
            c.Creator.pather = new Pawn_PathFollower(c.Creator);
            c.Creator.thinker = new Pawn_Thinker(c.Creator);
            c.Creator.def.race.thinkTreeMain = new ThinkTreeDef { thinkRoot = new ThinkNode_Priority() };
            var job = (Job)Production("SpiritRebirthRitualJob").GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Creator, c.Circle, c.Corpse])!;
            c.Creator.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
            try { c.Creator.jobs.StartJob(job, addToJobsThisTick: false); }
            catch { RuntimeLog.AssertClean(); throw; }
            Check.Same(job, c.Creator.CurJob, "native StartJob installs actual job");
            var driver = c.Creator.jobs.curDriver;
            if (scenario == "NativeObservedWorkAndCompletion")
            {
                var carried = Check.NotNull(c.Creator.carryTracker.CarriedThing, "native initial toils already picked up first trip");
                string carriedId = carried.GetUniqueLoadID();
                SaveJobGraph(r, c, "mid-haul");
                job = c.Creator.CurJob; driver = c.Creator.jobs.curDriver;
                var loadedCarried = Check.NotNull(c.Creator.carryTracker.CarriedThing, "native carry graph restores picked cargo");
                Check.Equal(false, ReferenceEquals(carried, loadedCarried), "carry cargo actually deep-loaded");
                Check.Equal(carriedId, loadedCarried.GetUniqueLoadID(), "same saved cargo load identity");
                Check.Same(c.Creator.carryTracker.GetDirectlyHeldThings(), loadedCarried.holdingOwner, "loaded cargo exact native owner");
                Check.Equal(true, c.Creator.pather.Moving, "native saved path movement intent retained");
                Check.Equal(3, driver.CurToilIndex, "native loaded driver remains at drop approach");
            }
            var staging = driver.GetType().GetProperty("Staging", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver)!;
            int trips = (int)staging.GetType().GetProperty("TripCount", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(staging)!;
            // Prepared-work fixture: actual native transfers, no walking claim.
            for (int i = 0; i < trips; i++)
            {
                Check.Equal(true, (bool)Call(staging, "Pickup", c.Creator, job, i)!, "prepare exact native carry");
                Check.Equal(true, (bool)Call(staging, "Drop", c.Creator, job, i)!, "prepare exact native staging");
            }
            Set(driver, "tripIndex", trips);
            c.Creator.pather.StopDead();
            var toils = (List<Toil>)typeof(JobDriver).GetField("toils", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver)!;
            driver.JumpToToil(toils.Last());
            int Work() => (int)driver.GetType().GetProperty("WorkTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver)!;
            void Tick(int tick) { Set(Find.TickManager, "ticksGameInt", tick); c.Creator.jobs.JobTrackerTick(); }
            for (int i = 1; i <= 9999; i++) Tick(i);
            Check.Equal(9999, Work(), "only observed native scheduler ticks accumulate");
            Check.Equal(true, c.Original.Dead, "9999 ticks cannot convert");
            var oldJob = job;
            c.Creator.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture));
            SaveJobGraph(r, c);
            job = c.Creator.CurJob; driver = c.Creator.jobs.curDriver;
            Check.Equal(false, ReferenceEquals(oldJob, job), "native current job deep-loads as new object");
            Check.Same(job, driver.job, "native job tracker binds loaded driver to loaded job");
            Check.Same(c.Creator, driver.pawn, "native job tracker binds external creator anchor");
            Check.Equal(1, c.Creator.jobs.jobQueue.Count, "native queued job survives");
            Check.Equal(9999, Work(), "load setup does not add work");
            Check.Equal(true, c.Map.reservationManager.ReservationsReadOnly.All(x => x.Claimant == c.Creator && x.Job == job), "native loaded reservation graph points to loaded current job");
            Tick(9999); Check.Equal(9999, Work(), "duplicate tick grants zero work");
            if (scenario == "CancelledWorkSave")
            {
                c.Creator.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false, canReturnToPool: false);
                c.Creator.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), addToJobsThisTick: false);
                Check.Equal(true, c.Original.Dead, "cancelled loaded work cannot resurrect");
                Check.Equal(350, c.Map.spawnedThings.Where(x => x.def == c.Magicyte.def || x.def == c.Silver.def).Sum(x => x.stackCount), "cancelled staged payment remains native map resources");
                Check.Equal(false, c.Map.reservationManager.ReservationsReadOnly.Any(x => x.Job == job), "native cleanup releases ritual claims");
                RuntimeLog.AssertClean(); return;
            }
            if (scenario == "InterruptedReleaseCleanup")
            {
                failingSpawn = c.Original;
                try { using var fault = new Fault(typeof(PawnComponentsUtility).GetMethod(nameof(PawnComponentsUtility.AddComponentsForSpawn))!, nameof(FailReleaseSpawn)); Tick(50000); }
                finally { failingSpawn = null; }
                Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "failed release stays inactive");
                Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "driver cleanup retains exact paid original in native inventory");
                c.Creator.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture), addToJobsThisTick: false);
                Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "native subsequent job does not drop paid original");
                var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Check.Equal(true, problems.Count > 0 && problems.All(p => p.Contains("injected ritual release")), "only explicit release failure diagnostic");
                RuntimeLog.Clear(); return;
            }
            if (scenario == "ReleaseCircleChanged" || scenario == "ReleaseJobReplaced" || scenario == "RevivalJobReplaced")
            {
                transitionContext = c; transitionScenario = scenario; transitionReplacement = null;
                revivalCalls = paymentUnits = transitionCalls = 0;
                try
                {
                    using var revivalObserver = new Fault(typeof(ResurrectionUtility).GetMethod(nameof(ResurrectionUtility.TryResurrect))!, nameof(CountRevival));
                    using var paymentObserver = new Fault(typeof(Thing).GetMethod(nameof(Thing.Destroy))!, nameof(CountPayment));
                    using var callback = new Fault(scenario == "RevivalJobReplaced" ? typeof(ResurrectionUtility).GetMethod(nameof(ResurrectionUtility.TryResurrect))! : typeof(PawnComponentsUtility).GetMethod(nameof(PawnComponentsUtility.AddComponentsForSpawn))!, nameof(ChangeTransition), postfix: scenario == "RevivalJobReplaced");
                    Tick(50000);
                    Check.Equal(1, transitionCalls, "actual native boundary changed ritual context once");
                    Check.Equal(1, revivalCalls, "actual original resurrected exactly once");
                    Check.Equal(350, paymentUnits, "native payment destruction exactly 300 plus 50 once");
                    Check.Equal(SpiritRebirthForm.Dormant, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "changed ritual context cannot commit active form");
                    Check.Same(c.Creator.inventory.innerContainer, c.Original.holdingOwner, "inactive original secured after outer native callback finishes");
                    if (transitionReplacement != null) Check.Same(transitionReplacement, c.Creator.CurJob, "old ritual preserves replacement job");
                    RuntimeLog.AssertClean(); return;
                }
                finally { transitionContext = null; transitionScenario = null; transitionReplacement = null; }
            }
            Tick(50000);
            Check.Equal(false, c.Original.Dead, "one observed tick after gap completes real resurrection");
            Check.Equal(SpiritRebirthForm.Ethereal, ClassIdentity.Component(c.Original).SpiritRebirth.Form, "native release commits exact original");
            Check.Same(c.Map.spawnedThings, c.Original.holdingOwner, "native same-Pawn map custody");
            Check.Equal(true, c.Corpse.Destroyed, "same corpse consumed once");
            Check.Equal(0, c.Map.spawnedThings.Where(x => x.def == c.Magicyte.def || x.def == c.Silver.def).Sum(x => x.stackCount), "exact 300 plus 50 staged payment consumed once");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRitualJob.NativePostSplitCallbackException", r => SplitCallback(r, true));
        yield return new("SpiritRitualJob.NativePreAbsorbCallbackException", r => SplitCallback(r, false));
        yield return new("SpiritRitualJob.WholeSourceNativeDespawnException", r => SplitCallback(r, false, true));
        yield return new("SpiritRitualJob.CompetingReservationNotOverridden", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var setup = new Fixture(c);
            c.Job.def.driverClass = typeof(JobDriver_Wait);
            var other = PawnWithComps(false, false).pawn;
            Set(other, "mapIndexOrState", (sbyte)0); Set(other, "factionInt", c.Creator.Faction);
            Check.Equal(true, c.Map.spawnedThings.TryAdd(other, false), "native map owns competitor");
            var competingJob = JobMaker.MakeJob(c.Job.def);
            Check.Equal(true, c.Map.reservationManager.Reserve(other, competingJob, c.Circle), "competitor reserves circle first");
            var job = (Job)Production("SpiritRebirthRitualJob").GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Creator, c.Circle, c.Corpse])!;
            job.playerForced = true;
            Check.Equal(false, job.TryMakePreToilReservations(c.Creator, false), "forced order cannot override ordinary competing reservation");
            Check.Equal(true, c.Map.reservationManager.ReservationsReadOnly.Any(x => x.Job == competingJob && x.Claimant == other && x.Target.Thing == c.Circle), "competing claim retained");
            Check.Equal(false, c.Map.reservationManager.ReservationsReadOnly.Any(x => x.Job == job), "rejected plan leaves no partial claims");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRitualJob.OrderedPreReservationBeforeCurrentJob", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var setup = new Fixture(c);
            c.Job.def.driverClass = typeof(JobDriver_Wait);
            var job = (Job)Production("SpiritRebirthRitualJob").GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Creator, c.Circle, c.Corpse])!;
            Check.Same(c.Job, c.Creator.CurJob, "request still has prior current job");
            Check.Equal(true, job.TryMakePreToilReservations(c.Creator, false), "native ordered-job pre-reservation works before current-job installation");
            var driver = job.GetCachedDriver(c.Creator);
            var staging = driver.GetType().GetProperty("Staging", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver)!;
            Check.Equal(false, (bool)Call(staging, "Pickup", c.Creator, job, 0)!, "non-current reserved job cannot execute hauling");
            Set(c.Creator.jobs, "curJob", job);
            var current = job.MakeDriver(c.Creator);
            Check.Equal(false, ReferenceEquals(driver, current), "native start constructs a fresh driver");
            Check.Equal(true, current.TryMakePreToilReservations(false), "fresh current driver replaces provisional claims with execution manifest");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRitualJob.KnownSplitRefusalAndOccupiedCell", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var setup = new Fixture(c);
            c.Magicyte.stackCount = 420; c.Silver.stackCount = 80;
            var (job, staging) = Start(c);
            refusedOwner = c.Creator.carryTracker.GetDirectlyHeldThings();
            try
            {
                using var fault = new Fault();
                Check.Equal(false, (bool)Call(staging, "Pickup", c.Creator, job, 0)!, "injected native carry rejection fails pickup");
                Check.Equal(420, c.Magicyte.stackCount, "known split reabsorbed into exact unchanged source");
                Check.Equal(null, c.Creator.carryTracker.CarriedThing, "no orphan in carry");
            }
            finally { refusedOwner = null; }
            Check.Equal(true, (bool)Call(staging, "Pickup", c.Creator, job, 0)!, "retry performs one exact pickup");
            var carried = c.Creator.carryTracker.CarriedThing;
            Check.Equal(true, (bool)Call(staging, "Pickup", c.Creator, job, 0)!, "duplicate pickup observes actual same owner");
            Check.Same(carried, c.Creator.carryTracker.CarriedThing, "duplicate cannot split another object");
            var cell = (IntVec3)Call(staging, "CellAt", 0)!;
            var blocker = ThingMaker.MakeThing(c.Silver.def); blocker.stackCount = 1;
            GenSpawn.Spawn(blocker, cell, c.Map);
            Check.Equal(false, (bool)Call(staging, "Drop", c.Creator, job, 0)!, "new native occupant rejects drop before merging or wiping");
            Check.Same(carried, c.Creator.carryTracker.CarriedThing, "blocked drop preserves native carry custody");
            Check.Same(c.Map.spawnedThings, blocker.holdingOwner, "blocker remains untouched");
            Check.Equal(320, c.Magicyte.stackCount, "one pickup only");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritRitualJob.NativeDiscoveryReservationAndSplitStaging", r =>
        {
            Production("JobDriver_SpiritRebirth");
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var setup = new Fixture(c);
                c.Magicyte.stackCount = 420; c.Silver.stackCount = 80;
                var job = (Job)Production("SpiritRebirthRitualJob").GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Creator, c.Circle, c.Corpse])!;
                Check.NotNull(job, "internal factory creates unstarted native job");
                Set(c.Creator.jobs, "curJob", job);
                var driver = job.MakeDriver(c.Creator);
                Check.Equal(true, driver.TryMakePreToilReservations(false), "native region discovery and exact reservations succeed");
                var staging = driver.GetType().GetProperty("Staging", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver)!;
                int trips = (int)staging.GetType().GetProperty("TripCount", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(staging)!;
                Check.Equal(5, trips, "100 native carry capacity plans three magicyte trips, silver and corpse");
                for (int i = 0; i < trips; i++)
                {
                    Check.Equal(true, (bool)Call(staging, "Pickup", c.Creator, job, i)!, "known native split enters exact carry owner");
                    Check.Equal(true, (bool)Call(staging, "Drop", c.Creator, job, i)!, "native whole-object drop stages exact owner");
                }
                var stacks = (List<ThingCountClass>)Call(staging, "StagedMaterials")!;
                Check.Equal(300, stacks.Where(s => s.thing.def == c.Magicyte.def).Sum(s => s.Count), "exact magicyte staged");
                Check.Equal(50, stacks.Where(s => s.thing.def == c.Silver.def).Sum(s => s.Count), "exact silver staged");
                Check.Equal(120, c.Magicyte.stackCount, "same native oversized source keeps surplus");
                Check.Equal(30, c.Silver.stackCount, "same native silver source keeps surplus");
                Check.Equal(stacks.Count, stacks.Select(s => s.thing.Position).Distinct().Count(), "no merged destination stacks");
                foreach (var stack in stacks)
                {
                    Check.Equal(stack.Count, stack.thing.stackCount, "staged entry is complete actual stack");
                    Check.Same(c.Map.spawnedThings, stack.thing.holdingOwner, "native map custody for each staged object");
                }
                Check.Same(c.Original, c.Corpse.InnerPawn, "corpse still owns exact original before work");
                Check.Equal(true, c.Original.Dead, "hauling cannot resurrect or pay");
                RuntimeLog.AssertClean();
            });
        });
    }
    static void SaveJobGraph(RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c, string checkpoint = "work9999")
    {
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try
        {
            foreach (var def in new[] { c.Magicyte.def, c.Silver.def, c.Corpse.def })
                if (DefDatabase<ThingDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<ThingDef>.Add(def);
            var path = Path.Combine(r.WorkingDirectory, "ritual-native-job-graph-" + checkpoint + ".xml");
            JobGraph saved = new(c.Creator, c.Map);
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "graph", c.Creator, c.Map); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            c.Creator.pather.StopDead(); // Retire the old native request, not the saved movement intent.
            JobGraph loaded = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "graph", c.Creator, c.Map);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach (var anchor in c.Map.spawnedThings.Cast<ILoadReferenceable>().Concat(new ILoadReferenceable[] { c.Map, c.Original }).Distinct()) directory.RegisterLoaded(anchor);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            RuntimeLog.AssertClean();
        }
        finally { DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def); }
    }
    public sealed class JobGraph : IExposable
    {
        readonly Pawn pawn; readonly Map map;
        Pawn_JobTracker jobs; Pawn_CarryTracker carry; ReservationManager reservations; Pawn_PathFollower pather;
        public JobGraph(Pawn pawn, Map map) { this.pawn = pawn; this.map = map; jobs = pawn.jobs; carry = pawn.carryTracker; reservations = map.reservationManager; pather = pawn.pather; }
        public void ExposeData()
        {
            Scribe_Deep.Look(ref jobs, "jobs", pawn);
            Scribe_Deep.Look(ref carry, "carry", pawn);
            Scribe_Deep.Look(ref reservations, "reservations", map);
            Scribe_Deep.Look(ref pather, "pather", pawn);
            if (Scribe.mode == LoadSaveMode.LoadingVars) { pawn.jobs = jobs; pawn.carryTracker = carry; map.reservationManager = reservations; pawn.pather = pather; }
        }
    }
    static void SplitCallback(RuntimeFixture r, bool postSplit, bool whole = false)
    {
        SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var setup = new Fixture(c);
            using var productionPatches = new FleshStatTests.ProductionPatches();
            SpiritRebirthActionTests.Patch(productionPatches, "SpiritRebirthNativeSplitCapturePatch");
            c.Magicyte.stackCount = 420;
            var old = whole ? c.Silver : c.Magicyte; var position = old.Position; old.DeSpawn();
            old.def.thingClass = typeof(ThingWithComps);
            old.def.comps = [new CompProperties { compClass = typeof(ThrowingHaulComp) }];
            var source = ThingMaker.MakeThing(old.def); source.stackCount = whole ? 50 : 420;
            if (whole) c.Silver = source; else c.Magicyte = source;
            GenSpawn.Spawn(source, position, c.Map);
            var (job, staging) = Start(c);
            ThrowingHaulComp.source = source; ThrowingHaulComp.split = null;
            ThrowingHaulComp.throwSplit = postSplit;
            ThrowingHaulComp.throwAbsorb = true;
            ThrowingHaulComp.throwDespawn = whole;
            try
            {
                refusedOwner = postSplit ? null : c.Creator.carryTracker.GetDirectlyHeldThings();
                using var fault = new Fault();
                Check.Equal(false, (bool)Call(staging, "Pickup", c.Creator, job, whole ? 3 : 0)!, "native comp exception cannot report successful pickup");
                var split = Check.NotNull(ThrowingHaulComp.split, "native PostSplitOff observed actual base split");
                Check.Same(c.Creator.inventory.innerContainer, split.holdingOwner, "exact native split survives in creator inventory after recovery callback throws");
                Check.Equal(true, c.Creator.inventory.innerContainer.Contains(split), "fallback ownership reciprocal");
                if (whole) { Check.Same(source, split, "whole-source fallback retains exact source object"); Check.Equal(50, split.stackCount, "whole source count preserved"); }
                else Check.Equal(420, c.Magicyte.stackCount + split.stackCount, "native total conserved without replacement objects");
                Check.Equal(null, Production("SpiritRebirthSplitCapture").GetField("current", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null), "exception releases scoped capture reference");
                var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Check.Equal(true, problems.Count > 0 && problems.All(p => p.Contains("injected ritual comp")), "only injected comp diagnostics");
                RuntimeLog.Clear();
            }
            finally { refusedOwner = null; ThrowingHaulComp.source = null; ThrowingHaulComp.split = null; ThrowingHaulComp.throwSplit = ThrowingHaulComp.throwAbsorb = ThrowingHaulComp.throwDespawn = false; }
        });
    }
    public sealed class ThrowingHaulComp : ThingComp
    {
        internal static Thing? source, split;
        internal static bool throwSplit, throwAbsorb, throwDespawn;
        public override void PostDeSpawn(Map map, DestroyMode mode)
        {
            if (!ReferenceEquals(parent, source) || !throwDespawn) return;
            split = parent;
            throw new InvalidOperationException("injected ritual comp PostDeSpawn");
        }
        public override void PostSplitOff(Thing piece)
        {
            if (!ReferenceEquals(parent, source)) return;
            split = piece;
            if (throwSplit) throw new InvalidOperationException("injected ritual comp PostSplitOff");
        }
        public override void PreAbsorbStack(Thing other, int count)
        {
            if (ReferenceEquals(parent, source) && throwAbsorb) throw new InvalidOperationException("injected ritual comp PreAbsorbStack");
        }
    }
    static (Job, object) Start(SpiritRebirthRitualTransactionTests.Context c)
    {
        var job = (Job)Production("SpiritRebirthRitualJob").GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [c.Creator, c.Circle, c.Corpse])!;
        Set(c.Creator.jobs, "curJob", job);
        var driver = job.MakeDriver(c.Creator);
        Check.Equal(true, driver.TryMakePreToilReservations(false), "native reservations acquired");
        return (job, driver.GetType().GetProperty("Staging", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver)!);
    }
    static ThingOwner? refusedOwner;
    static SpiritRebirthRitualTransactionTests.Context? transitionContext;
    static string? transitionScenario;
    static Job? transitionReplacement;
    static int revivalCalls, paymentUnits, transitionCalls;
    static void CountRevival(Pawn pawn) { if (ReferenceEquals(pawn, transitionContext?.Original)) revivalCalls++; }
    static void CountPayment(Thing __instance)
    {
        if (__instance.def == transitionContext?.Magicyte.def || __instance.def == transitionContext?.Silver.def) paymentUnits += __instance.stackCount;
    }
    static void ChangeTransition(Pawn pawn)
    {
        var c = transitionContext;
        if (c == null || !ReferenceEquals(pawn, c.Original)) return;
        transitionCalls++;
        if (transitionScenario == "ReleaseCircleChanged") c.Circle.MageList.Add(c.Creator);
        else
        {
            transitionReplacement = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            c.Creator.jobs.StartJob(transitionReplacement, JobCondition.InterruptForced, canReturnCurJobToPool: true, addToJobsThisTick: false);
        }
    }
    static Pawn? failingSpawn;
    static void FailReleaseSpawn(Pawn pawn) { if (ReferenceEquals(pawn, failingSpawn)) throw new InvalidOperationException("injected ritual release"); }
    static bool RefuseCarry(ThingOwner __instance, ref bool __result)
    {
        if (!ReferenceEquals(__instance, refusedOwner)) return true;
        __result = false; return false;
    }
    sealed class Fault : IDisposable
    {
        readonly object harmony; readonly string id = "rwom.ritual-haul-fault." + Guid.NewGuid();
        public Fault(MethodBase? target = null, string callback = nameof(RefuseCarry), bool postfix = false)
        {
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthRitualJobTests).GetMethod(callback, BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target ?? typeof(ThingOwner<Thing>).GetMethod("TryAdd", [typeof(Thing), typeof(bool)])!, postfix ? null : method, postfix ? method : null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    sealed class Fixture : IDisposable
    {
        readonly JobDef[] jobs = DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        readonly StatDef oldCarry = StatDefOf.CarryingCapacity;
        readonly JobDef oldWait = JobDefOf.Wait_MaintainPosture;
        readonly FieldInfo mainThread = typeof(UnityData).GetField("mainThreadId", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? priorThread;
        public Fixture(SpiritRebirthRitualTransactionTests.Context c)
        {
            priorThread = mainThread.GetValue(null); mainThread.SetValue(null, Environment.CurrentManagedThreadId);
            DefDatabase<JobDef>.Add(new JobDef { defName = "TM_SpiritRebirth", driverClass = Production("JobDriver_SpiritRebirth") });
            JobDefOf.Wait_MaintainPosture = new JobDef { defName = "RitualFollowupWait", driverClass = typeof(JobDriver_Wait) };
            DefDatabase<JobDef>.Add(JobDefOf.Wait_MaintainPosture);
            StatDefOf.CarryingCapacity = new StatDef { defName = "RitualCarryCapacity", defaultBaseValue = 100f, minValue = 0, maxValue = 1000, workerClass = typeof(StatWorker) };
            c.Creator.ageTracker = new Pawn_AgeTracker(c.Creator);
            c.Creator.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }];
            Set(c.Creator.ageTracker, "cachedLifeStageIndex", 0);
            var map = c.Map;
            map.resourceCounter = new ResourceCounter(map); map.fogGrid = new FogGrid(map);
            map.reachability = new Reachability(map);
            var region = Region.MakeNewUnfilled(new IntVec3(0, 0, 0), map);
            region.extentsClose = region.extentsLimit = new CellRect(0, 0, 30, 30);
            var grid = (Region[])typeof(RegionGrid).GetField("regionGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map.regionGrid)!;
            for (int i = 0; i < grid.Length; i++) grid[i] = region;
            foreach (var item in new[] { c.Magicyte, c.Silver })
            {
                map.listerThings.Remove(item); item.def.alwaysHaulable = true; map.listerThings.Add(item);
                region.ListerThings.Add(item);
            }
            c.Corpse.def.thingClass = typeof(Corpse); c.Corpse.def.comps ??= []; c.Corpse.def.size = new IntVec2(1, 1);
            c.Corpse.def.drawerType = DrawerType.RealtimeOnly;
            Check.Equal(true, c.Creator.carryTracker.GetDirectlyHeldThings().TryDrop(c.Corpse, new IntVec3(16, 0, 10), map, ThingPlaceMode.Direct, out _, playDropSound: false), "native corpse staged as source with empty creator carry");
            region.ListerThings.Add(c.Corpse);
            map.reservationManager.ReleaseClaimedBy(c.Creator, c.Job);
        }
        public void Dispose()
        {
            DefDatabase<JobDef>.Clear(); foreach (var d in jobs) DefDatabase<JobDef>.Add(d);
            StatDefOf.CarryingCapacity = oldCarry; mainThread.SetValue(null, priorThread);
            JobDefOf.Wait_MaintainPosture = oldWait;
        }
    }
}
