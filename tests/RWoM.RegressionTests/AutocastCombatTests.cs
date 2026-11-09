using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.AutoCast;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class AutocastCombatTests
{
    public static void Initialize()
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(MentalStateDefOf));
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("AutocastCombat.PeacefulCaravanJobCannotAdmitNPC", f => World(f, w =>
        {
            w.Job(new JobDef { defName = "TraderCaravanWander" });
            Check.Equal(false, TM_AutocastCombat.TryGetCombatOpponent(w.Caster, out _), "caravan job has no engagement");
            Check.Equal(false, w.CanAutocast(w.Ally), "friendly support target cannot invent an enemy");
            new CompAbilityUserMagic { parent = w.Caster }.ResolveAIAutoCast();
            new CompAbilityUserMight { parent = w.Caster }.ResolveAIAutoCast();
        }));
        yield return new("AutocastCombat.IdleWaitCombatIsNotCombat", f => World(f, w =>
        {
            w.Job(JobDefOf.Wait_Combat);
            Check.Equal(true, w.Caster.IsFighting(), "engine job-only predicate reproduces the original bug");
            Check.Equal(false, TM_AutocastCombat.TryGetCombatOpponent(w.Caster, out _), "Wait_Combat without enemy is not engaged");
            Check.Equal(false, w.CanAutocast(w.Ally), "idle combat-shaped job cannot heal or buff");
        }));
        yield return new("AutocastCombat.StaleEnemyDoesNotAdmitCaravanJob", f => World(f, w =>
        {
            w.Engage();
            w.Job(new JobDef { defName = "TraderCaravanWander" });
            Check.Equal(false, w.CanAutocast(w.Ally), "stale live enemy reference cannot authorize a peaceful caravan job");
        }));
        yield return new("AutocastCombat.AttackJobWithoutEnemyRejected", f => World(f, w =>
        {
            w.Job(JobDefOf.AttackStatic);
            Check.Equal(true, w.Caster.IsFighting(), "empty attack job is engine fighting");
            Check.Equal(false, w.CanAutocast(w.Ally), "job name alone never authorizes an NPC spell");
        }));
        yield return new("AutocastCombat.RealSameMapHostileOpponentAdmits", f => World(f, w =>
        {
            w.Engage();
            Check.Equal(true, w.Enemy.Spawned && w.Enemy.Map == w.Caster.Map && w.Enemy.HostileTo(w.Caster), "actual spawned hostile enemy fixture");
            Check.Equal(true, TM_AutocastCombat.TryGetCombatOpponent(w.Caster, out Thing opponent), "real engagement admitted");
            Check.Same(w.Enemy, opponent, "engagement is the enemy, not friendly spell target");
            Check.Equal(true, w.CanAutocast(w.Ally), "noncolonist visitor can support an ally in combat");
        }));
        yield return new("AutocastCombat.CombatJobTargetCanSupplyOpponent", f => World(f, w =>
        {
            w.Job(JobDefOf.AttackStatic, w.Enemy);
            Check.Equal(true, TM_AutocastCombat.TryGetCombatOpponent(w.Caster, out Thing opponent), "direct combat target works before mindState updates");
            Check.Same(w.Enemy, opponent, "selected opponent is actual job enemy");
        }));
        yield return new("AutocastCombat.WrongMapAndDownedEnemyRejected", f => World(f, w =>
        {
            w.Engage(); Set(w.Enemy, "mapIndexOrState", (sbyte)-1);
            Check.Equal(false, w.CanAutocast(w.Ally), "despawned enemy cannot authorize a spell");
            Set(w.Enemy, "mapIndexOrState", (sbyte)1);
            Check.Equal(false, w.CanAutocast(w.Ally), "enemy on another map cannot authorize a spell");
            Set(w.Enemy, "mapIndexOrState", (sbyte)0); Set(w.Enemy.health, "healthState", PawnHealthState.Down);
            Check.Equal(false, w.CanAutocast(w.Ally), "incapacitated enemy is not an active threat");
        }));
        yield return new("AutocastCombat.DisabledThreatRejected", f => World(f, w =>
        {
            w.Engage(); w.Enemy.mindState.duty = new PawnDuty(new DutyDef { defName = "Regression_NoThreat", threatDisabled = true });
            Check.Equal(false, w.CanAutocast(w.Ally), "engine-disabled hostile threat cannot authorize casts");
        }));
        foreach (GuestStatus status in new[] { GuestStatus.Prisoner, GuestStatus.Slave })
        {
            GuestStatus captive = status;
            yield return new("AutocastCombat.CaptiveHasNoPeacefulLoophole." + status, f => World(f, w =>
            {
                w.Caster.guest.guestStatusInt = captive; w.Caster.Faction.def.isPlayer = true;
                Check.Equal(true, TM_AutocastCombat.RequiresCombat(w.Caster), "captivity never inherits colonist-only admission");
                Check.Equal(false, w.CanAutocast(w.Ally), "captive cannot autonomously support outside real combat");
            }));
        }
        yield return new("AutocastCombat.FriendlySupportKeepsSeparateOpponentAtStart", f => World(f, w =>
        {
            w.Job(JobDefOf.AttackStatic, w.Enemy);
            Check.Equal(true, w.CanAutocast(w.Ally), "selection admits live job enemy separately from ally");
            w.AbilityJob(false, w.Ally);
            Check.Equal(true, w.AllowsStart(w.Ally), "replacing attack job with support cast retains the live opponent");
            Set(w.Enemy.health, "healthState", PawnHealthState.Down);
            Check.Equal(false, w.AllowsStart(w.Ally), "warmup revalidates opponent instead of trusting cached success");
        }));
        yield return new("AutocastCombat.PreviousEngagementCannotAuthorizeIdleJob", f => World(f, w =>
        {
            w.Job(JobDefOf.AttackStatic, w.Enemy); Check.Equal(true, w.CanAutocast(w.Ally), "initial fight admitted");
            w.Job(JobDefOf.Wait_Combat);
            Check.Equal(false, w.CanAutocast(w.Ally), "short support bridge cannot leak into idle Wait_Combat");
        }));
        yield return new("AutocastCombat.ExternalFrameworkAIJobHasFinalGate", f => World(f, w =>
        {
            w.AbilityJob(false, w.Ally, framework: true);
            Check.Equal(false, w.AllowsStart(w.Ally), "external framework jobs use the same automatic final combat gate");
            w.Engage();
            Check.Equal(true, w.AllowsStart(w.Ally), "framework job can execute during actual combat");
        }));
        yield return new("AutocastCombat.ExplicitManualNPCOrderPreserved", f => World(f, w =>
        {
            w.AbilityJob(true, w.Ally, framework: true);
            Check.Equal(true, w.AllowsStart(w.Ally), "count=1 explicit order is not an NPC automatic cast");
        }));
        yield return new("AutocastCombat.ExternalUseAbilityAdmissionPreservesManual", f => World(f, w =>
        {
            Type patch = typeof(MagicAbility).Assembly.GetType("TorannMagic.Compatibility.AbilityAutomaticJobAdmissionPatch")!;
            var prefix = patch.GetMethod("Prefix")!;
            var ability = new MagicAbility { AbilityUser = new CompAbilityUserMagic { parent = w.Caster }, Def = w.Def };
            object?[] ai = [ability, AbilityContext.AI, (LocalTargetInfo)w.Ally, null];
            Check.Equal(false, (bool)prefix.Invoke(null, ai)!, "external AI cannot even enqueue a peaceful support cast");
            object?[] manual = [ability, AbilityContext.Player, (LocalTargetInfo)w.Ally, null];
            Check.Equal(true, (bool)prefix.Invoke(null, manual)!, "manual requests retain native behavior");
        }));
        yield return new("AutocastCombat.HealthyCureRejectedAtFinalBoundary", f => World(f, w =>
        {
            w.Engage(); w.Def.defName = "TM_CureDisease"; w.AbilityJob(false, w.Ally);
            Check.Equal(false, w.CanAutocast(w.Ally), "healthy ally is not a useful disease cure target");
            Check.Equal(false, w.AllowsStart(w.Ally), "external self/ally profile cannot bypass cure usefulness");
            w.Ally.health.hediffSet.hediffs.Add(new Hediff { pawn = w.Ally, def = new HediffDef { defName = "Flu", isBad = true } });
            Check.Equal(true, w.CanAutocast(w.Ally), "supported disease plus independent live enemy is useful");
            w.Caster.mindState.enemyTarget = null; w.Job(JobDefOf.Wait_Combat);
            Check.Equal(false, w.CanAutocast(w.Ally), "even an afflicted NPC ally cannot be cured outside combat");
        }));
        yield return new("AutocastCombat.HealthyHealRejected", f => World(f, w =>
        {
            w.Engage(); w.Def.defName = "TM_Heal";
            Check.Equal(false, w.CanAutocast(w.Ally), "combat does not make a healthy healing target useful");
        }));
        yield return new("AutocastCombat.AggressiveSchedulersNeverForgePlayerContext", f =>
        {
            foreach (string file in new[] { "CompAbilityUserMagic.cs", "CompAbilityUserMight.cs" })
            {
                string source = File.ReadAllText(Path.Combine(f.Repository, "RimWorldOfMagic/RimWorldOfMagic", file));
                int start = source.IndexOf("//Aggressive AI Casting", StringComparison.Ordinal);
                int end = source.IndexOf("pawnAbility.UseAbility", start, StringComparison.Ordinal);
                Check.Equal(true, start >= 0 && end > start, "aggressive sibling still has a dispatch path");
                string dispatch = source.Substring(end, Math.Min(110, source.Length - end));
                Check.Equal(true, dispatch.Contains("AbilityContext.AI"), "aggressive automatic job retains AI marker in " + file);
                Check.Equal(false, dispatch.Contains("AbilityContext.Player"), "aggressive jobs never masquerade as player orders");
                string admission = source.Substring(start, end - start);
                Check.Equal(true, admission.Contains("TryGetCombatOpponent"), "aggressive branch cannot bypass actual combat");
                Check.Equal(true, admission.Contains("CanAutocast"), "aggressive branch has useful final ability target gate");
            }
        });
        yield return new("AutocastCombat.BlinkFinalLandingRejectsSameCellAndNoProgress", f => World(f, w =>
        {
            IntVec3 origin = w.Caster.Position, goal = new(16, 0, 4);
            Check.Equal(false, TM_AutocastCombat.LandingAllowed(w.Caster, origin), "nearest walkable replacement cannot be caster's own cell");
            Check.Equal(false, TM_AutocastCombat.HasTravelProgress(origin, origin, goal), "same-cell travel has no progress");
            Check.Equal(false, TM_AutocastCombat.HasTravelProgress(origin, new IntVec3(2, 0, 4), goal), "replacement away from actual path goal is useless");
            Check.Equal(false, TM_AutocastCombat.HasTravelProgress(origin, new IntVec3(4, 0, 6), goal), "sideways replacement is not progress");
            Check.Equal(true, TM_AutocastCombat.HasTravelProgress(origin, new IntVec3(10, 0, 4), goal), "real forward landing is useful");
            Check.Equal(false, TM_AutocastCombat.LandingAllowed(w.Caster, new IntVec3(30, 0, 4)), "out-of-map landing rejected");
        }));
        yield return new("AutocastCombat.BlinkNeedsRealPathButPlayerTravelNeedsNoEnemy", f => World(f, w =>
        {
            w.Caster.Faction.def.isPlayer = true; w.Caster.pather = new Pawn_PathFollower(w.Caster);
            Set(w.Caster.pather, "moving", true); Set(w.Caster.pather, "destination", (LocalTargetInfo)new IntVec3(16, 0, 4));
            Check.Equal(false, TM_AutocastCombat.TryGetTravelDestination(w.Caster, out _), "moving flag without a computed path cannot blink");
            w.Caster.pather.curPath = PawnPath.NotFound;
            Check.Equal(false, TM_AutocastCombat.TryGetTravelDestination(w.Caster, out _), "failed path cannot blink");
            w.Caster.pather.curPath = new PawnPath(); w.Caster.pather.curPath.AddNode(new IntVec3(16, 0, 4));
            Check.Equal(true, TM_AutocastCombat.TryGetTravelDestination(w.Caster, out LocalTargetInfo goal), "real found travel path retained");
            Check.Equal(new IntVec3(16, 0, 4), goal.Cell, "actual path destination preserved");
            Check.Equal(false, TM_AutocastCombat.RequiresCombat(w.Caster), "ordinary player travel remains allowed without an enemy");
        }));
    }

    private static void World(RuntimeFixture fixture, Action<WorldState> body)
    {
        var old = Current.Game;
        var settings = TorannMagic.ModOptions.Settings.Instance;
        var panic = MentalStateDefOf.PanicFlee;
        var jobFields = new[] { typeof(JobDefOf).GetField(nameof(JobDefOf.Wait_Combat))!,
            typeof(JobDefOf).GetField(nameof(JobDefOf.AttackStatic))!,
            typeof(JobDefOf).GetField(nameof(JobDefOf.PredatorHunt))!,
            typeof(AbilityUser.AbilityDefOf).GetField(nameof(AbilityUser.AbilityDefOf.CastAbilityVerb))! };
        var oldJobs = jobFields.Select(field => field.GetValue(null)).ToArray();
        try
        {
            MentalStateDefOf.PanicFlee ??= new MentalStateDef { defName = "PanicFlee" };
            foreach (var field in jobFields)
                if (field.GetValue(null) == null) field.SetValue(null, new JobDef { defName = field.Name });
            var game = Blank<Game>(); Set(game, "tickManager", new TickManager()); Set(game, "uniqueIDsManager", new UniqueIDsManager()); Current.Game = game;
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AICasting = true, AIHardMode = false };
            using var patches = new FleshStatTests.ProductionPatches();
            foreach (string name in new[] { "AbilityCastStartPatch", "AbilityPreCastStartPatch", "AbilityProjectileLaunchSafetyPatch" })
                patches.Add(typeof(MagicAbility).Assembly.GetType("TorannMagic.Compatibility." + name)!);
            body(new WorldState(fixture, game));
        }
        finally
        {
            for (int i = 0; i < jobFields.Length; i++) jobFields[i].SetValue(null, oldJobs[i]);
            MentalStateDefOf.PanicFlee = panic;
            Current.Game = old; TorannMagic.ModOptions.Settings.Instance = settings;
        }
    }

    private sealed class WorldState
    {
        public readonly Map Map;
        public readonly Pawn Caster, Ally, Enemy;
        public readonly TMAbilityDef Def;
        public readonly RecordingVerb Verb;
        private readonly Faction friendly = new() { def = new FactionDef { defName = "Regression_Visitors" } };
        private readonly Faction hostile = new() { def = new FactionDef { defName = "Regression_Hostiles" } };
        public WorldState(RuntimeFixture fixture, Game game)
        {
            Map = Blank<Map>(); Map.info = new MapInfo { Size = new IntVec3(20, 1, 20) }; Map.generatorDef = new MapGeneratorDef { defeatRequiresCantReachUnfogged = false };
            var other = Blank<Map>(); other.info = new MapInfo { Size = new IntVec3(20, 1, 20) };
            Set(game, "maps", new List<Map> { Map, other });
            Map.cellIndices = new CellIndices(Map); Map.thingGrid = new ThingGrid(Map); Map.mapPawns = new MapPawns(Map);
            Map.fogGrid = new FogGrid(Map); Map.edificeGrid = new EdificeGrid(Map); Map.coverGrid = new CoverGrid(Map);
            Set(friendly, "relations", new List<FactionRelation> { new() { other = hostile, kind = FactionRelationKind.Hostile } });
            Set(hostile, "relations", new List<FactionRelation> { new() { other = friendly, kind = FactionRelationKind.Hostile } });
            Caster = Pawn("Caster", new IntVec3(4, 0, 4), friendly); Ally = Pawn("Ally", new IntVec3(7, 0, 4), friendly);
            Enemy = Pawn("Enemy", new IntVec3(12, 0, 4), hostile);
            Def = fixture.AddAbility("TM_NPCCombatRegression");
            Def.MainVerb.AbilityTargetCategory = AbilityTargetCategory.TargetThing;
            Def.MainVerb.targetParams = new TargetingParameters { canTargetPawns = true, canTargetSelf = true };
            Def.MainVerb.range = 30f; Def.MainVerb.requireLineOfSight = false; Def.MainVerb.warmupTime = 0f;
            Def.MainVerb.isViolent = false; Def.MainVerb.ai_IsWeapon = false; Def.MainVerb.verbClass = typeof(RecordingVerb);
            Verb = new RecordingVerb { caster = Caster, verbProps = Def.MainVerb, verbTracker = new VerbTracker(Caster), Ability = new MagicAbility { Def = Def } };
            TorannMagicDefOf.TMCastAbilityVerb ??= new JobDef { defName = "TMCastAbilityVerb" };
            Job(new JobDef { defName = "TraderCaravanWait" });
        }
        private Pawn Pawn(string name, IntVec3 cell, Faction faction)
        {
            var def = Blank<ThingDef>(); def.defName = "Regression_NPC_" + name; def.category = ThingCategory.Pawn; def.size = new IntVec2(1, 1);
            def.race = new RaceProperties { intelligence = Intelligence.Humanlike, alwaysAwake = true };
            var pawn = new Pawn { def = def, kindDef = new PawnKindDef { defName = name, race = def } };
            // No components yet: native 1.7 GetComp requires null, not an empty list.
            pawn.health = new Pawn_HealthTracker(pawn);
            pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) }; pawn.jobs = new Pawn_JobTracker(pawn);
            pawn.mindState = new Pawn_MindState(pawn); pawn.stances = new Pawn_StanceTracker(pawn); pawn.guest = new Pawn_GuestTracker(pawn);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "positionInt", cell);
            Map.thingGrid.Register(pawn); ((IList<Pawn>)Map.mapPawns.AllPawnsSpawned).Add(pawn);
            return pawn;
        }
        public void Job(JobDef def, LocalTargetInfo target = default) => Set(Caster.jobs, "curJob", new Job { def = def, targetA = target });
        public void Engage() { Caster.mindState.enemyTarget = Enemy; Job(JobDefOf.Wait_Combat); }
        public void AbilityJob(bool manual, LocalTargetInfo target, bool framework = false) => Set(Caster.jobs, "curJob", new Job
        {
            def = framework ? AbilityUser.AbilityDefOf.CastAbilityVerb : TorannMagicDefOf.TMCastAbilityVerb,
            verbToUse = Verb, targetA = target, count = manual ? 1 : 0, playerForced = true
        });
        public bool CanAutocast(LocalTargetInfo target) => Invoke("CanAutocast", target);
        public bool AllowsStart(LocalTargetInfo target) => Invoke("AllowsStart", target);
        private bool Invoke(string name, LocalTargetInfo target) => (bool)typeof(MagicAbility).Assembly
            .GetType("TorannMagic.Compatibility.AbilityCastSafety")!.GetMethod(name)!.Invoke(null, [Verb, target])!;
    }
    private sealed class RecordingVerb : Verb_UseAbility
    {
        public override float EffectiveRange => 30f;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) => target.IsValid;
        protected override bool TryCastShot() => true;
    }
    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    private static void Set(object target, string name, object value) => FleshStatTests.Set(target, name, value);
}
