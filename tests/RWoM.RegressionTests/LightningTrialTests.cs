using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class LightningTrialTests
{
    static Type DamageDefs => typeof(MagicAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf", true)!;
    static DamageDef SnowDamage { get => (DamageDef)DamageDefs.GetField("TM_LightningCloud")!.GetValue(null)!; set => DamageDefs.GetField("TM_LightningCloud")!.SetValue(null, value); }
    static DamageDef IceDamage { get => (DamageDef)DamageDefs.GetField("TM_Lightning")!.GetValue(null)!; set => DamageDefs.GetField("TM_Lightning")!.SetValue(null, value); }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LightningTrial.ActualDamageAndDistinctAcrossCasts", Damage);
        yield return new("LightningTrial.NativeOwnerStageAndVictimEligibility", Eligibility);
        yield return new("LightningTrial.PersistedExplosionAndProgress", Save);
        yield return new("LightningTrial.NativeLaunchAndAmpEffect", Amp);
        yield return new("IceTrial.BlizzardPendingSnowSurvivesLoad", BlizzardSnow);
        yield return new("LightningTrial.NestedDamageAndUnrelatedExplosionExcluded", Nested);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly DamageDef[] damageDefs = DefDatabase<DamageDef>.AllDefsListForReading.ToArray();
        readonly DamageDef oldSnowball = SnowDamage, oldIceshard = IceDamage;
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly RecordDef[] records = DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        readonly Faction friendly = new() { def = new FactionDef { isPlayer = true } };
        readonly Faction enemy = new() { def = new FactionDef() };
        int nextId = 19000;
        public readonly Pawn Caster, First, Second;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.lightning");
        public readonly DamageDef Burn = new() { defName = "FireclawTestBurn", workerClass = typeof(DamageWorker) };
        public readonly ThingDef ExplosionDef;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            SnowDamage = new DamageDef { defName = "LightningTrialSnowball", workerClass = typeof(DamageWorker) };
            IceDamage = new DamageDef { defName = "LightningTrialIceshard", workerClass = typeof(DamageWorker) };
            DefDatabase<DamageDef>.Add(SnowDamage); DefDatabase<DamageDef>.Add(IceDamage);
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
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.lightning", DisciplineDomain.Magic);
            Path.RecordCompletedTrial(LightningTrials.FoundationTrialId);
            Progression.State.TryAdvanceAfterTrial("rwom.lightning", InitiationStage.Foundation, LightningTrials.FoundationTrialId);
            Magic.MagicData.MagicPowersSB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningCloud)).learned = true;
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "FireclawTestProfile", profileId = "rwom.lightning", domain = DisciplineDomain.Magic });
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
            var explosion = new Explosion { def = ExplosionDef, thingIDNumber = nextId++, instigator = Caster, damType = SnowDamage, damAmount = 0, radius = 1, doSoundEffects = false, doVisualEffects = false };
            if (tagged) LightningTrials.Stamp(explosion, Caster, LightningTrials.Spell.Cloud);
            return explosion;
        }
        public void Hit(int castId, Pawn victim, float damage)
            => LightningTrials.Complete(LightningTrials.Capture(Caster, victim, LightningTrials.Spell.Cloud), damage);
        public bool Complete => Path.CompletedTrials.Contains(LightningTrials.MasteryTrialId);
        public void Dispose()
        {
            LightningTrials.ExitExplosion(null!); Current.Game = previous;
            SnowDamage = oldSnowball; IceDamage = oldIceshard;
            Restore(profiles); Restore(things); Restore(damageDefs); Restore(records); Restore(hediffs);
        }
        static void Restore<T>(T[] defs) where T : Def, new()
        { DefDatabase<T>.Clear(); foreach (var def in defs) DefDatabase<T>.Add(def); }
    }

    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new();
        public Explosion explosion = null!;
        public void ExposeData() { Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref explosion, "explosion"); }
    }
    public sealed class TestProjectile : Projectile_AbilityBase { public void SeedCaster(Pawn pawn) => launcher = pawn; }
    public sealed class ProjectileEnvelope : IExposable
    {
        public Projectile_AbilityBase projectile = null!;
        public void ExposeData() => Scribe_Deep.Look(ref projectile, "projectile");
    }
    sealed class LanguageScope : IDisposable
    {
        readonly LoadedLanguage previous = LanguageDatabase.activeLanguage, previousDefault = LanguageDatabase.defaultLanguage;
        public LanguageScope()
        {
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English"; Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement> { ["AU_CastSuccess"] = new() { key = "AU_CastSuccess", value = "Success" } };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        }
        public void Dispose() { LanguageDatabase.activeLanguage = previous; LanguageDatabase.defaultLanguage = previousDefault; }
    }

    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.lightning.tests." + Guid.NewGuid();
        readonly object harmony;
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            foreach (var patch in new[] { typeof(LightningExplosionSavePatch), typeof(LightningExplosionDamagePatch), typeof(LightningDamagePatch), typeof(LightningLaunchScopePatch), typeof(LightningLaunchPatch), typeof(LightningProjectileSavePatch), typeof(LightningAmpPatch) })
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
        public void HeadlessGraphics()
        {
            void Prefix(MethodInfo target, string name)
            {
                var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(LightningTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
                type.GetMethod("Patch")!.Invoke(harmony, [target, hook, null, null, null]);
            }
            Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(FixedPosition));
            Prefix(typeof(Pawn_DrawTracker).GetMethod("Notify_DebugAffected")!, nameof(SkipGraphics));
            foreach (var method in typeof(MoteMaker).GetMethods().Where(m => m.Name == "ThrowText")) Prefix(method, nameof(SkipGraphics));
        }
    }


    static void Damage(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) w.Hit(1, w.First, amount);
        Check.Equal(0, w.Path.LightningTrials.CloudVictims.Count, "finite positive actual damage required");
        var hit = LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Cloud);
        LightningTrials.Complete(hit, 1); LightningTrials.Complete(hit, 1); w.Hit(2, w.First, 100);
        Check.Equal(false, w.Complete, "same enemy repeated across casts remains one");
        w.Hit(3, w.Second, 1);
        Check.Equal(true, w.Complete, "second distinct enemy across casts completes mastery");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "no automatic advancement");
        Set(w.Path, "stage", InitiationStage.Foundation);
        Set(w.Path, "completedTrials", new List<string>());
        w.Magic.MagicData.MagicPowersSB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningBolt)).learned = true;
        var bolt = LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Bolt);
        LightningTrials.Complete(bolt, 12); LightningTrials.Complete(bolt, 12);
        Check.Equal(12f, w.Path.LightningTrials.BoltDamage, "one result consumed once");
        LightningTrials.Complete(LightningTrials.Capture(w.Caster, w.Second, LightningTrials.Spell.Bolt), 9);
        Check.Equal(20f, w.Path.LightningTrials.BoltDamage, "damage bounded at threshold");
        Check.Equal(true, w.Path.CompletedTrials.Contains(LightningTrials.FoundationTrialId), "cumulative actual damage completes foundation");
    }
    static void Eligibility(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave })
        {
            w.First.guest.guestStatusInt = status;
            Check.Equal(true, LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Cloud) == null, status + " excluded");
        }
        w.First.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.First, "factionInt", w.Caster.Faction);
        Check.Equal(true, LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Cloud) == null, "ally excluded");
        Set(w.First, "factionInt", null);
        Check.Equal(true, LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Cloud) == null, "peaceful factionless excluded");
        Set(w.First.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Manhunter { pawn = w.First, def = new MentalStateDef { category = MentalStateCategory.Aggro } });
        Check.Equal(true, LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Cloud) != null, "aggressive factionless accepted");
        var power = w.Magic.MagicData.MagicPowersSB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningCloud));
        power.learned = false;
        Check.Equal(true, LightningTrials.Capture(w.Caster, w.Second, LightningTrials.Spell.Cloud) == null, "unlearned native owner cannot borrow item/copy credit");
        power.learned = true;
        foreach (var stage in new[] { InitiationStage.Foundation, InitiationStage.Master, InitiationStage.Ascended })
        {
            Set(w.Path, "stage", stage);
            Check.Equal(true, LightningTrials.Capture(w.Caster, w.Second, LightningTrials.Spell.Cloud) == null, "exact Adept required");
        }
    }
    static void Save(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        Set(w.Path, "stage", InitiationStage.Foundation); Set(w.Path, "completedTrials", new List<string>());
        w.Magic.MagicData.MagicPowersSB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningBolt)).learned = true;
        LightningTrials.Complete(LightningTrials.Capture(w.Caster, w.First, LightningTrials.Spell.Bolt), 15);
        var data = new Envelope { state = w.Progression.State, explosion = w.Explosion(2) };
        data.explosion.damType = IceDamage;
        LightningTrials.Stamp(data.explosion, w.Caster, LightningTrials.Spell.Bolt);
        data.explosion.instigator = null;
        var file = Path.Combine(f.WorkingDirectory, "lightning-pending-explosion.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Set(w.Progression, "state", loaded.state); loaded.explosion.instigator = w.Caster;
        Check.Equal(15f, w.Path.LightningTrials.BoltDamage, "saved actual damage retained");
        var prior = LightningTrials.EnterExplosion(loaded.explosion, w.Second);
        var hit = LightningTrials.BeginHit(w.Second, new DamageInfo(IceDamage, 5, instigator: w.Caster));
        LightningTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 5 });
        LightningTrials.EndHit(hit); LightningTrials.ExitExplosion(prior);
        Check.Equal(true, w.Path.CompletedTrials.Contains(LightningTrials.FoundationTrialId), "persisted native bolt secondary source completes foundation after load");
    }
    static void Amp(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        using var language = new LanguageScope();
        hooks.HeadlessGraphics();
        Set(w.Path, "stage", InitiationStage.Foundation); Set(w.Path, "completedTrials", new List<string>());
        Set(w.First, "factionInt", w.Caster.Faction);
        w.Magic.MagicData.MagicPowersSB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_AMP)).learned = true;
        var ability = new PawnAbility(w.Magic); Set(ability, "powerDef", TorannMagicDefOf.TM_AMP);
        var verb = new Verb_BLOS { Ability = ability }; Set(verb, "caster", w.Caster);
        var def = (ProjectileDef_Ability)RuntimeHelpers.GetUninitializedObject(typeof(ProjectileDef_Ability)); def.defName = "Buff_AMP"; def.projectile = new ProjectileProperties { speed = 100, flyOverhead = true };
        var effect = new HediffDef { defName = "AMP_I", hediffClass = typeof(Hediff), initialSeverity = 1, maxSeverity = 1 };
        DefDatabase<HediffDef>.Add(effect);
        var projectile = new TestProjectile { def = def };
        Set(projectile, "mapIndexOrState", (sbyte)0);
        void Launch()
        {
            var previous = LightningTrials.BeginLaunch(verb, def);
            try { projectile.Launch(w.Caster, ability.Def, UnityEngine.Vector3.zero, w.First, ProjectileHitFlags.IntendedTarget, applyHediffs: new List<ApplyHediffs> { new() { hediffDef = effect, applyChance = 1 } }); }
            finally { LightningTrials.EndLaunch(previous); }
        }
        Launch();
        Check.Equal(true, LightningTrials.CaptureAmp(projectile, w.Caster, w.First) == null, "unowned copied ability cannot stamp a native projectile");
        w.Magic.AbilityData.Powers.Add(ability); Launch();
        Check.Equal(true, LightningTrials.CaptureAmp(projectile, w.Caster, w.First) != null, "actual framework Launch carries native ability provenance");
        def.comps = new List<CompProperties>(); def.thingClass = typeof(TestProjectile); def.category = ThingCategory.Projectile; DefDatabase<ThingDef>.Add(def);
        projectile.thingIDNumber = 19991;
        projectile.SeedCaster(null!); projectile.usedTarget = projectile.intendedTarget = IntVec3.Zero;
        var data = new ProjectileEnvelope { projectile = projectile };
        var file = Path.Combine(f.WorkingDirectory, "lightning-launched-projectile.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        ProjectileEnvelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        RuntimeLog.AssertClean();
        projectile = (TestProjectile)loaded.projectile; projectile.SeedCaster(w.Caster);
        Check.Equal(true, LightningTrials.CaptureAmp(projectile, w.Caster, w.First) != null, "native launch provenance survives complete projectile save/load");
        var existing = new Hediff { def = effect, pawn = w.First }; Set(existing, "severityInt", 1f); w.First.health.hediffSet.hediffs.Add(existing);
        projectile.ApplyHediffsAndMentalStates(w.First, w.Caster, null, ability.Def);
        Check.Equal(false, w.Path.CompletedTrials.Contains(LightningTrials.FoundationTrialId), "unchanged capped AMP is not trial proof");
        Set(existing, "severityInt", 0f);
        projectile.ApplyHediffsAndMentalStates(w.First, w.Caster, null, ability.Def);
        Check.Equal(true, w.Path.CompletedTrials.Contains(LightningTrials.FoundationTrialId), "native framework applies real AMP and completes foundation");
    }
    static void BlizzardSnow(RuntimeFixture f)
    {
        using var fixture = new Fixture(f);
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "RegressionBlizzard"; def.thingClass = typeof(Projectile_Blizzard);
        def.comps = []; def.category = ThingCategory.Projectile; DefDatabase<ThingDef>.Add(def);
        var shot = new Projectile_Blizzard { def = def, thingIDNumber = 19992,
            usedTarget = IntVec3.Zero, intendedTarget = IntVec3.Zero };
        var schedule = typeof(Projectile_Blizzard).GetMethod("ScheduleCell", BindingFlags.Instance | BindingFlags.NonPublic);
        Check.NotNull(schedule, "blizzard records pending snowfall instead of a fixed array");
        Set(shot, "age", 177);
        var cell = new IntVec3(8, 0, 11);
        schedule!.Invoke(shot, [cell, 12]);
        var data = new ProjectileEnvelope { projectile = shot };
        var file = Path.Combine(f.WorkingDirectory, "blizzard-pending-snow.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        ProjectileEnvelope restored = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref restored, "data"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        object Field(string name) => typeof(Projectile_Blizzard).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(restored.projectile)!;
        Check.Equal(cell, ((List<IntVec3>)Field("effectCells")).Single(), "only the actual strike cell survives");
        Check.Equal(189, ((List<int>)Field("effectAtAge")).Single(), "due time remains relative to projectile age");
        RuntimeLog.AssertClean();
    }
    static bool SkipGraphics() => false;
    static bool FixedPosition(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    static void Nested(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        var first = w.Explosion(1);
        var outer = LightningTrials.EnterExplosion(first, w.First);
        var hit = LightningTrials.BeginHit(w.First, new DamageInfo(SnowDamage, 1, instigator: w.Caster));
        var nested = LightningTrials.BeginHit(w.Second, new DamageInfo(SnowDamage, 10, instigator: w.Caster));
        LightningTrials.CompleteHit(nested, new DamageWorker.DamageResult { totalDamageDealt = 10 }); LightningTrials.EndHit(nested);
        LightningTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 1 }); LightningTrials.EndHit(hit);
        LightningTrials.ExitExplosion(outer);
        var unrelated = w.Explosion(2, false);
        outer = LightningTrials.EnterExplosion(unrelated, w.Second);
        hit = LightningTrials.BeginHit(w.Second, new DamageInfo(SnowDamage, 10, instigator: w.Caster));
        LightningTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 10 }); LightningTrials.EndHit(hit); LightningTrials.ExitExplosion(outer);
        Check.Equal(1, w.Path.LightningTrials.CloudVictims.Count, "nested hit and untagged explosion cannot borrow source");
        var projectileDef = (ProjectileDef_Ability)RuntimeHelpers.GetUninitializedObject(typeof(ProjectileDef_Ability)); projectileDef.defName = "Projectile_LightningCloud";
        var ability = new PawnAbility(w.Magic); Set(ability, "powerDef", TorannMagicDefOf.TM_LightningCloud); w.Magic.AbilityData.Powers.Add(ability);
        var verb = new Verb_LightningCloud { Ability = ability }; Set(verb, "caster", w.Caster);
        var projectile = new TestProjectile { def = projectileDef, localAbilityDef = ability.Def }; projectile.SeedCaster(w.Caster);
        var prior = LightningTrials.BeginLaunch(verb, projectileDef);
        LightningTrials.Launched(projectile, w.Caster, ability.Def); LightningTrials.EndLaunch(prior);
        LightningTrials.Damage(projectile, w.Second, new DamageInfo(SnowDamage, 0), LightningTrials.Spell.Cloud);
        Check.Equal(false, w.Complete, "tagged direct native damage wrapper rejects actual zero damage");
    }
}




