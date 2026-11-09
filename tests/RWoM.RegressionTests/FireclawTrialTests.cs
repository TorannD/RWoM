using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class FireclawTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FireclawTrial.DistinctVictimsSameCastAndInterleaving", Pair);
        yield return new("FireclawTrial.EligibilityAndActualPositiveDamage", Eligibility);
        yield return new("FireclawTrial.NestedScopesAndExceptionCleanup", Scopes);
        yield return new("FireclawTrial.NativeExplosionAndWitnessScribe", Save);
        yield return new("FireclawTrial.BoundedWitnesses", Bounded);
        yield return new("FireclawTrial.NativeWorkerAndProjectileStampHooks", Native);
        yield return new("FireclawTrial.NullExplosionPrefixesEnterNothing", NullExplosionPrefixes);
    }

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly DamageDef[] damageDefs = DefDatabase<DamageDef>.AllDefsListForReading.ToArray();
        readonly RecordDef[] records = DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        readonly Faction friendly = new() { def = new FactionDef { isPlayer = true } };
        readonly Faction enemy = new() { def = new FactionDef() };
        int nextId = 19000;
        public readonly Pawn Caster, First, Second;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.fire");
        public readonly DamageDef Burn = new() { defName = "FireclawTestBurn", workerClass = typeof(DamageWorker) };
        public readonly ThingDef ExplosionDef;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            DefDatabase<RecordDef>.Add(new RecordDef { defName = "FireclawFixtureRecord" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Find.TickManager, "gameStartAbsTick", 1);
            Set(Current.Game, "battleLog", new BattleLog());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager();
            Set(Current.Game, "worldInt", world);
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            Set(friendly, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
            Set(enemy, "relations", new List<FactionRelation> { new() { other = friendly, kind = FactionRelationKind.Hostile } });
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic; Prepare(Caster, friendly);
            First = PawnWithComps(false, false).pawn; Prepare(First, enemy);
            Second = PawnWithComps(false, false).pawn; Prepare(Second, enemy);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.fire", DisciplineDomain.Magic);
            Path.RecordCompletedTrial(FireboltDamageTrial.TrialId);
            Progression.State.TryAdvanceAfterTrial("rwom.fire", InitiationStage.Foundation, FireboltDamageTrial.TrialId);
            Magic.MagicData.MagicPowersIF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fireclaw)).learned = true;
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "FireclawTestProfile", profileId = "rwom.fire", domain = DisciplineDomain.Magic });
            ExplosionDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            ExplosionDef.defName = "FireclawTestExplosion"; ExplosionDef.thingClass = typeof(Explosion); ExplosionDef.category = ThingCategory.Ethereal;
            DefDatabase<ThingDef>.Add(ExplosionDef); DefDatabase<DamageDef>.Add(Burn);
        }
        void Prepare(Pawn pawn, Faction faction)
        {
            pawn.thingIDNumber = nextId++; pawn.kindDef = new PawnKindDef { defName = "FireclawTestPawn" };
            pawn.guest = new Pawn_GuestTracker(pawn); pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
            pawn.records = new Pawn_RecordsTracker(pawn);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
        }
        public Explosion Explosion(int castId, bool tagged = true)
        {
            var explosion = new Explosion { def = ExplosionDef, thingIDNumber = nextId++, instigator = Caster, damType = Burn, damAmount = 0, radius = 1, doSoundEffects = false, doVisualEffects = false };
            if (tagged) FireclawTrial.Stamp(explosion, castId);
            return explosion;
        }
        public void Hit(int castId, Pawn victim, float damage)
            => FireclawTrial.Complete(FireclawTrial.Capture(Caster, victim, castId), damage);
        public bool Complete => Path.CompletedTrials.Contains(FireclawTrial.TrialId);
        public void Dispose()
        {
            FireclawTrial.ExitExplosion(null!); Current.Game = previous;
            Restore(profiles); Restore(things); Restore(damageDefs); Restore(records);
        }
        static void Restore<T>(T[] defs) where T : Def, new()
        { DefDatabase<T>.Clear(); foreach (var def in defs) DefDatabase<T>.Add(def); }
    }

    static void Pair(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        w.Magic.MagicData.MagicUserXP = 72; w.Magic.MagicData.MagicAbilityPoints = 6;
        var first = FireclawTrial.Capture(w.Caster, w.First, 100);
        FireclawTrial.Complete(first, 8); FireclawTrial.Complete(first, 8);
        w.Hit(100, w.First, 11);
        Check.Equal(false, w.Complete, "repeated damage on one enemy cannot complete pair");
        w.Hit(200, w.Second, 9);
        Check.Equal(false, w.Complete, "different casts cannot combine victims");
        Check.Equal(2, w.Path.FireclawWitnesses.Count, "interleaved casts each retain first witness");
        var lethal = FireclawTrial.Capture(w.Caster, w.Second, 100);
        Set(w.Second.health, "healthState", PawnHealthState.Dead);
        FireclawTrial.Complete(lethal, 4);
        Check.Equal(true, w.Complete, "A/B/A interleaving and predeath snapshot complete cast A");
        Check.Equal(0, w.Path.FireclawWitnesses.Count, "completed trial drops pending witnesses");
        FireclawTrial.Complete(lethal, 100); w.Hit(300, w.First, 1);
        Check.Equal(1, w.Path.CompletedTrials.Count(t => t == FireclawTrial.TrialId), "repeated callback never rewards twice");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "trial alone never advances stage");
        Check.Equal(72, w.Magic.MagicData.MagicUserXP, "trial preserves XP");
        Check.Equal(6, w.Magic.MagicData.MagicAbilityPoints, "trial preserves points");
    }

    static void Eligibility(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) w.Hit(100, w.First, amount);
        Check.Equal(0, w.Path.FireclawWitnesses.Count, "only finite positive actual damage records a victim");
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave })
        {
            w.First.guest.guestStatusInt = status;
            Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, status + " excluded");
        }
        w.First.guest.guestStatusInt = GuestStatus.Guest;
        var enemy = w.First.Faction; Set(w.First, "factionInt", w.Caster.Faction);
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, "allies excluded");
        var aggressive = new MentalStateDef { defName = "FireclawAggressive", category = MentalStateCategory.Aggro };
        Set(w.First.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Berserk { pawn = w.First, def = aggressive });
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, "friendly berserk excluded");
        Set(w.First, "factionInt", null);
        Set(w.First.mindState.mentalStateHandler, "curStateInt", null);
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, "peaceful factionless target excluded");
        Set(w.First.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Manhunter { pawn = w.First, def = aggressive });
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) != null, "hostile factionless manhunter qualifies");
        Set(w.First.mindState.mentalStateHandler, "curStateInt", null); Set(w.First, "factionInt", enemy);
        var power = w.Magic.MagicData.MagicPowersIF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fireclaw));
        power.learned = false;
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, "unlearned Fireclaw excluded");
        power.learned = true; Set(w.Path, "stage", InitiationStage.Foundation);
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, "foundation cannot earn mastery trial");
        Set(w.Path, "stage", InitiationStage.Master);
        Check.Equal(true, FireclawTrial.Capture(w.Caster, w.First, 100) == null, "already master does not track trial");
    }

    static void Scopes(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var tagged = w.Explosion(100); var ordinary = w.Explosion(0, false);
        var info = new DamageInfo(w.Burn, 999, instigator: w.Caster);
        FireclawExplosionDamagePatch.Prefix(tagged, w.First, out var outer);
        FireclawExplosionDamagePatch.Prefix(ordinary, w.Second, out var nested);
        FireclawDamagePatch.Prefix(w.Second, info, out var masked);
        FireclawDamagePatch.Postfix(new DamageWorker.DamageResult { totalDamageDealt = 50 }, masked);
        FireclawDamagePatch.Finalizer(null!, masked);
        var error = new InvalidOperationException("fixture nested failure");
        Check.Same(error, FireclawExplosionDamagePatch.Finalizer(error, nested), "nested exception propagates unchanged");
        Check.Equal(0, w.Path.FireclawWitnesses.Count, "ordinary nested explosion cannot borrow tagged cast");
        FireclawDamagePatch.Prefix(w.First, info, out var hit);
        FireclawDamagePatch.Prefix(w.Second, info, out var collateral);
        FireclawDamagePatch.Postfix(new DamageWorker.DamageResult { totalDamageDealt = 10 }, collateral);
        FireclawDamagePatch.Finalizer(null!, collateral);
        FireclawDamagePatch.Postfix(new DamageWorker.DamageResult { totalDamageDealt = 1 }, hit);
        FireclawDamagePatch.Finalizer(null!, hit);
        Check.Equal(1, w.Path.FireclawWitnesses.Count, "actual direct hit counts while recursive collateral is masked");
        Check.Equal(w.First.thingIDNumber, w.Path.FireclawWitnesses[0].firstVictimId, "only the engine worker target owns the witness");
        FireclawExplosionDamagePatch.Finalizer(error, outer);
        FireclawDamagePatch.Prefix(w.Second, info, out var after);
        FireclawDamagePatch.Postfix(new DamageWorker.DamageResult { totalDamageDealt = 10 }, after);
        FireclawDamagePatch.Finalizer(null!, after);
        Check.Equal(false, w.Complete, "exception cleanup leaves no ambient cast for later ordinary damage");
        var scope = FireclawTrial.EnterExplosion(tagged, w.Second);
        hit = FireclawTrial.BeginHit(w.Second, info); FireclawTrial.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 2 });
        FireclawTrial.EndHit(hit); FireclawTrial.ExitExplosion(scope);
        Check.Equal(true, w.Complete, "later genuine explosion from same cast completes pair");
    }

    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new();
        public Explosion explosion = null!;
        public void ExposeData() { Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref explosion, "explosion"); }
    }

    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.fireclaw.tests." + Guid.NewGuid();
        readonly object harmony;
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            foreach (var patch in new[] { typeof(FireclawExplosionSavePatch), typeof(FireclawExplosionDamagePatch), typeof(FireclawDamagePatch) })
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }

    static void Save(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        w.Hit(400, w.First, 7); w.Hit(500, w.Second, 6);
        var data = new Envelope { state = w.Progression.State, explosion = w.Explosion(400) };
        data.explosion.instigator = null; // Native instigator reference is independently restored by the game; test tag has no pawn references.
        var file = System.IO.Path.Combine(f.WorkingDirectory, "fireclaw-pending-explosion.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Set(w.Progression, "state", loaded.state); loaded.explosion.instigator = w.Caster;
        Check.Equal(2, w.Path.FireclawWitnesses.Count, "native Scribe preserves interleaved pending casts");
        Check.Equal(400, w.Path.FireclawWitnesses[0].castId, "native Scribe preserves cast identifier");
        var previous = FireclawTrial.EnterExplosion(loaded.explosion, w.Second);
        var hit = FireclawTrial.BeginHit(w.Second, new DamageInfo(w.Burn, 5, instigator: w.Caster));
        FireclawTrial.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 5 });
        FireclawTrial.EndHit(hit); FireclawTrial.ExitExplosion(previous);
        Check.Equal(true, w.Complete, "native saved explosion retains cast tag and finishes its first victim's pair");
        Check.Equal(0, w.Path.FireclawWitnesses.Count, "save-resumed completion clears bounded tracking");
    }

    static void Bounded(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        for (int i = 1; i <= FireclawTrial.WitnessLimit + 1; i++) w.Hit(i, w.First, 1);
        Check.Equal(FireclawTrial.WitnessLimit, w.Path.FireclawWitnesses.Count, "pending witnesses bounded to 32 casts");
        Check.Equal(2, w.Path.FireclawWitnesses[0].castId, "oldest cast evicted first");
        w.Hit(1, w.Second, 1);
        Check.Equal(false, w.Complete, "evicted cast cannot combine unrelated stored witnesses");
        w.Hit(FireclawTrial.WitnessLimit + 1, w.Second, 1);
        Check.Equal(true, w.Complete, "recent retained cast still completes");
    }

    static void Native(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        var explosion = w.Explosion(400);
        var worker = new DamageWorker { def = w.Burn };
        var native = typeof(DamageWorker).GetMethod("ExplosionDamageThing", BindingFlags.Instance | BindingFlags.NonPublic)!;
        native.Invoke(worker, [explosion, w.First, new List<Thing>(), new List<Thing>(), IntVec3.Zero]);
        Check.Equal(0, w.Path.FireclawWitnesses.Count, "actual patched native worker plus TakeDamage zero result gives no witness");
        var projectile = typeof(MagicAbility).Assembly.GetType("TorannMagic.Projectile_Fireclaw", true)!;
        var method = projectile.GetMethod("Explosion")!;
        var harmony = Assembly.Load("0Harmony");
        var read = harmony.GetType("HarmonyLib.PatchProcessor")!.GetMethods().Single(m => m.Name == "GetOriginalInstructions" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        var instructions = ((IEnumerable)read.Invoke(null, [method, null])!).Cast<object>().ToList();
        object? Operand(object i) => i.GetType().GetField("operand")!.GetValue(i);
        int stamp = instructions.FindIndex(i => Equals(Operand(i), typeof(FireclawTrial).GetMethod(nameof(FireclawTrial.Stamp))));
        int start = instructions.FindIndex(i => Operand(i) is MethodInfo m && m.DeclaringType == typeof(Explosion) && m.Name == "StartExplosion");
        Check.Equal(true, stamp >= 0 && start > stamp, "actual Fireclaw projectile stamps explosion before native start");
    }

    static void NullExplosionPrefixes(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        // ConditionalWeakTable throws on a null key: a null explosion reaching the
        // patched DamageWorker.ExplosionDamageThing prefixes used to abort the whole
        // explosion cell with "Explosion could not affect cell ..." errors.
        FireclawExplosionDamagePatch.Prefix(null, w.First, out var fire);
        Check.Same(null, fire, "fire trial enters nothing for a null explosion");
        IceExplosionDamagePatch.Prefix(null, w.First, out var ice);
        Check.Same(null, ice, "ice trial enters nothing for a null explosion");
        LightningExplosionDamagePatch.Prefix(null, w.First, out var bolt);
        Check.Same(null, bolt.lightning, "lightning trial enters nothing for a null explosion");
        Check.Same(null, bolt.blood, "blood trial enters nothing for a null explosion");
        // Finalizers unwind without a state and rethrow nothing when nothing failed.
        Check.Same(null, FireclawExplosionDamagePatch.Finalizer(null, null), "fire finalizer rethrows nothing");
        Check.Same(null, IceExplosionDamagePatch.Finalizer(null, null), "ice finalizer rethrows nothing");
        Check.Same(null, LightningExplosionDamagePatch.Finalizer(null, default), "lightning finalizer rethrows nothing");
    }
}
