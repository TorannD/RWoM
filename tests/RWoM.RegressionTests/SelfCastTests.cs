using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class SelfCastTests
{
    public static void Initialize()
    {
        RuntimeHelpers.RunClassConstructor(typeof(StatDefOf).TypeHandle);
        RuntimeLog.AcceptHeadlessDefOfInitialization("StatDefOf");
        StatDefOf.AimingDelayFactor = new StatDef { defName = "AimingDelayFactor", defaultBaseValue = 1f, workerClass = typeof(FixedAimingDelayWorker) };
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SelfCast.AIInitialTargetReachesShot", f => Cast(false));
        yield return new("SelfCast.AIReusedVerbReplacesStaleTarget", f => Cast(true));
        yield return new("SelfCast.ColonistTargetReachesShot", f => Cast(false, colonist: true));
        yield return new("SelfCast.AIInvalidTargetDoesNotCastOrCharge", f => Cast(false, reject: true));
        yield return new("SelfCast.AIUnspawnedCasterDoesNotCastOrCharge", f => Cast(true, unspawned: true));
        yield return new("SelfCast.AIWarmupDefersExactlyOneShot", f => Cast(false, warmup: true));
        yield return new("SelfCast.AIValidCellTargetIsPreserved", f => Cast(false, cell: true));
    }

    private static void Cast(bool stale, bool colonist = false, bool reject = false, bool unspawned = false, bool warmup = false, bool cell = false)
    {
        var root = Blank<Root_Play>(); var ui = Blank<UIRoot_Play>(); var mapUI = Blank<MapInterface>();
        root.uiRoot = ui; ui.mapUI = mapUI; mapUI.targeter = new Targeter();
        Set(typeof(Current), "rootInt", root);
        var game = Blank<Game>(); Set(game, "tickManager", new TickManager());
        Set(game, "uniqueIDsManager", new UniqueIDsManager()); Current.Game = game;
        var map = Blank<Map>();
        map.reservationManager = new ReservationManager(map);
        map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
        map.pawnDestinationReservationManager = new PawnDestinationReservationManager();
        map.enrouteManager = new EnrouteManager(map);
        map.attackTargetReservationManager = new AttackTargetReservationManager(map);
        Set(game, "maps", new List<Map> { map });
        var def = Blank<ThingDef>(); def.defName = "SelfCastPawn";
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = def };
        pawn.jobs = new Pawn_JobTracker(pawn); pawn.health = new Pawn_HealthTracker(pawn);
        pawn.stances = new Pawn_StanceTracker(pawn);
        if (colonist) Set(pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
        Set(pawn, "comps", new List<ThingComp>()); Set(pawn, "mapIndexOrState", (sbyte)0);
        Set(pawn, "positionInt", new IntVec3(5, 0, 5));
        var verb = new RecordingVerb { caster = pawn, verbProps = new VerbProperties_Ability { requireLineOfSight = false, warmupTime = warmup ? .1f : 0f } };
        verb.verbTracker = new VerbTracker(pawn);
        if (unspawned) Set(pawn, "mapIndexOrState", (sbyte)-1);
        var ability = new RecordingAbility(); verb.Ability = ability;
        if (stale) verb.SeedTarget(new Pawn { def = def });
        var job = new Job { def = new JobDef { defName = "SelfCastRegression" }, targetA = pawn, verbToUse = verb };
        if (reject) job.targetA = LocalTargetInfo.Invalid;
        if (cell) job.targetA = new IntVec3(8, 0, 7);
        Set(pawn.jobs, "curJob", job);
        // Isolate the scheduler following EndJobWith; execute the real casting
        // init and finish actions, without starting unrelated AI jobs in a headless map.
        var driver = new Driver { pawn = pawn, job = new Job() };
        var toil = driver.Toils().Last(); toil.actor = pawn;
        toil.initAction();
        if (warmup)
        {
            Check.Equal(0, verb.Shots, "Warmup must defer the shot");
            var stance = pawn.stances.curStance as Stance_Warmup;
            Set(Find.TickManager, "ticksGameInt", 1); // Between graphical mote ticks.
            for (int tick = 0; tick < 6; tick++) toil.tickAction();
            typeof(Stance_Warmup).GetMethod("Expire", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Check.NotNull(stance, "Engine creates warmup stance"), null);
        }
        if (reject || unspawned)
            Check.Equal(0, verb.Shots, "Rejected target must not cast");
        else
        {
            Check.Equal(1, verb.Shots, "Driver casts exactly once");
            if (cell) Check.Equal(new IntVec3(8, 0, 7), verb.ShotTarget.Cell, "Cell-target abilities retain their target");
            else Check.Same(pawn, verb.ShotTarget.Thing, "Actual shot receives job pawn instead of default/stale target");
        }
        toil.Cleanup(0, driver);
        Check.Equal(reject || unspawned ? 0 : 1, ability.Attempts, "Only a started cast is charged");
    }

    private sealed class Driver : TMJobDriver_CastAbilitySelf
    {
        public IEnumerable<Toil> Toils() => MakeNewToils();
    }

    // Keep the engine's real TryStartCastOn and target propagation; isolate only
    // graphical/combat side effects after the target has reached the shot.
    private sealed class RecordingVerb : Verb_UseAbility
    {
        public int Shots;
        public LocalTargetInfo ShotTarget;
        public override float EffectiveRange => 30f;
        public void SeedTarget(LocalTargetInfo target) => currentTarget = target;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target) => target.IsValid;
        public override void WarmupComplete() => TryCastShot();
        protected override bool TryCastShot() { Shots++; ShotTarget = currentTarget; return true; }
    }

    private sealed class RecordingAbility : PawnAbility
    {
        public int Attempts;
        public override void PostAbilityAttempt() => Attempts++;
    }

    public sealed class FixedAimingDelayWorker : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true) => 1f;
        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess) { }
    }

    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    private static void Set(object target, string name, object value)
    {
        for (Type? type = target as Type ?? target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            if (field == null) continue;
            field.SetValue(target is Type ? null : target, value); return;
        }
        throw new Exception("Missing fixture field " + name);
    }
}
