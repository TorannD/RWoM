using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class BloodMageTrialTests
{
    static FieldInfo BurnField => typeof(MagicAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf", true)!.GetField("TM_BloodBurn")!;
    static DamageDef Burn => (DamageDef)BurnField.GetValue(null)!;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BloodMageTrial.ShieldPendingLossSurvivesSave", SaveShield);
        yield return new("BloodMageTrial.ShieldActualDeltaLinkAndRecipients", Shield);
        yield return new("BloodMageTrial.ShieldFreshAndDrift", ShieldDrift);
        yield return new("BloodMageTrial.IgniteOwnedSourceDamageAndNestedScopes", Ignite);
        yield return new("BloodMageTrial.IgniteRecipientAndOwnerBoundaries", Boundaries);
        yield return new("BloodMageTrial.ProjectileExplosionAndProgressScribe", Save);
        yield return new("BloodMageTrial.NativeShieldAndLaunchMutation", Native);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly DamageDef[] damages = DefDatabase<DamageDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly RecordDef[] records = DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        readonly object oldBurn = BurnField.GetValue(null)!;
        readonly EffecterDef oldEffect = TorannMagicDefOf.TM_BloodShieldEffecter;
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public Faction Enemy;
        public HediffDef WardDef;
        public ThingDef ExplosionDef;
        public ProjectileDef_Ability ProjectileDef;
        public DisciplineProgress Path => Progression.State.Find("rwom.bloodmage");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            DefDatabase<RecordDef>.Add(new RecordDef { defName = "BloodTrialRecord" });
            TorannMagicDefOf.TM_BloodShieldEffecter = new EffecterDef { defName = "BloodTrialEffect" };
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(SoundDefOf));
            BurnField.SetValue(null, new DamageDef { defName = "BloodTrialBurn", workerClass = typeof(DamageWorker) }); DefDatabase<DamageDef>.Add(Burn);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Find.TickManager, "gameStartAbsTick", 1); Set(Current.Game, "battleLog", new BattleLog());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World)); world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) map.events = new MapEvents(map);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var friendly = new Faction { def = new FactionDef { isPlayer = true } }; Enemy = new Faction { def = new FactionDef() };
            Set(friendly, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } });
            Set(Enemy, "relations", new List<FactionRelation> { new() { other = friendly, kind = FactionRelationKind.Hostile } });
            int id = 29100;
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.thingIDNumber = id++; pawn.kindDef = new PawnKindDef { defName = "BloodTrialPawn" }; pawn.guest = new Pawn_GuestTracker(pawn);
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn); pawn.records = new Pawn_RecordsTracker(pawn);
                Set(pawn, "factionInt", friendly); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.bloodmage", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "BloodTrialProfile", profileId = "rwom.bloodmage", domain = DisciplineDomain.Magic });
            WardDef = new HediffDef { defName = "TM_BloodShieldHD", maxSeverity = 200, hediffClass = typeof(HediffWithComps) };
            DefDatabase<HediffDef>.Add(WardDef);
            ExplosionDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); ExplosionDef.defName = "BloodTrialExplosion"; ExplosionDef.thingClass = typeof(Explosion); ExplosionDef.category = ThingCategory.Ethereal; DefDatabase<ThingDef>.Add(ExplosionDef);
            ProjectileDef = (ProjectileDef_Ability)RuntimeHelpers.GetUninitializedObject(typeof(ProjectileDef_Ability)); ProjectileDef.defName = "Projectile_IgniteBlood"; ProjectileDef.thingClass = typeof(TestProjectile); ProjectileDef.category = ThingCategory.Projectile; ProjectileDef.comps = new(); ProjectileDef.projectile = new ProjectileProperties { speed = 100, flyOverhead = true }; DefDatabase<ThingDef>.Add(ProjectileDef);
        }
        public MagicPower Owner(bool ignite) => Magic.MagicData.MagicPowersBM.Single(p => p.TMabilityDefs.Contains(ignite ? TorannMagicDefOf.TM_IgniteBlood : TorannMagicDefOf.TM_BloodShield));
        public PawnAbility Ability(bool ignite = false, bool owned = true)
        {
            Owner(ignite).learned = true; var ability = new PawnAbility(Magic); Set(ability, "powerDef", ignite ? TorannMagicDefOf.TM_IgniteBlood : TorannMagicDefOf.TM_BloodShield);
            if (owned) Magic.AbilityData.Powers.Add(ability); return ability;
        }
        public void Adept() { Path.RecordCompletedTrial(BloodMageTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.bloodmage", InitiationStage.Foundation, BloodMageTrials.FoundationTrialId); Set(Target, "factionInt", Enemy); }
        public void Injure(float amount = 3)
        { var h = new Hediff_Injury { def = new HediffDef { defName = "TrialInjury" }, pawn = Target }; Set(h, "severityInt", amount); Target.health.hediffSet.hediffs.Add(h); }
        public HediffWithComps Ward(float severity, Pawn? linked = null)
        {
            var comp = (HediffComp)Activator.CreateInstance(typeof(MagicAbility).Assembly.GetType("TorannMagic.HediffComp_BloodShield")!, true)!;
            var hd = new HediffWithComps { pawn = Target, def = WardDef, comps = new() { comp } }; comp.parent = hd; Set(comp, "linkedPawn", linked ?? Caster);
            Set(hd, "severityInt", severity); Target.health.hediffSet.hediffs.Add(hd); return hd;
        }
        public TestProjectile Launch(bool owned = true)
        {
            var ability = Ability(true, owned); var verb = new Verb_BLOS { Ability = ability }; Set(verb, "caster", Caster);
            var projectile = new TestProjectile { def = ProjectileDef, localAbilityDef = ability.Def, thingIDNumber = 29200 }; projectile.SeedCaster(Caster); Set(projectile, "mapIndexOrState", (sbyte)0);
            var prior = BloodMageTrials.BeginLaunch(verb, ProjectileDef);
            try { BloodMageTrials.Launched(projectile, Caster, ability.Def); } finally { BloodMageTrials.EndLaunch(prior); }
            return projectile;
        }
        public Explosion Explosion(TestProjectile? projectile)
        {
            var e = new Explosion { def = ExplosionDef, instigator = Caster, damType = Burn, thingIDNumber = 29300, doSoundEffects = false, doVisualEffects = false, radius = 1 };
            e.Position = IntVec3.Zero; Set(e, "mapIndexOrState", (sbyte)0);
            var prior = BloodMageTrials.BeginExplosion(projectile!, IntVec3.Zero);
            try { BloodMageTrials.StartExplosion(e); } finally { BloodMageTrials.EndExplosion(prior); }
            return e;
        }
        public void Hit(Explosion e, float amount)
        {
            var prior = BloodMageTrials.EnterExplosion(e, Target); var hit = BloodMageTrials.BeginHit(Target, new DamageInfo(Burn, amount, instigator: Caster));
            try { BloodMageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = amount }); BloodMageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = amount }); }
            finally { BloodMageTrials.EndHit(hit); BloodMageTrials.ExitExplosion(prior); }
        }
        public void Dispose()
        {
            BloodMageTrials.ExitExplosion(null!); BloodMageTrials.EndExplosion(null!); BloodMageTrials.EndLaunch(null!);
            BurnField.SetValue(null, oldBurn); TorannMagicDefOf.TM_BloodShieldEffecter = oldEffect; Current.Game = previous; Restore(profiles); Restore(things); Restore(damages); Restore(hediffs); Restore(records);
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var def in defs) DefDatabase<T>.Add(def); }
    }
    public sealed class TestProjectile : Projectile_IgniteBlood { public void SeedCaster(Pawn? pawn) => launcher = pawn; }
    static void Shield(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability();
        Check.Equal(true, BloodMageTrials.CaptureShield(ability, w.Target) == null, "healthy ally excluded"); w.Injure(0);
        Check.Equal(true, BloodMageTrials.CaptureShield(ability, w.Target) == null, "zero injury excluded"); w.Injure();
        Check.Equal(true, BloodMageTrials.CaptureShield(w.Ability(owned: false), w.Target) == null, "copy/item runtime excluded");
        Check.Equal(true, BloodMageTrials.CaptureShield(ability, w.Caster) == null, "self excluded");
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; Check.Equal(true, BloodMageTrials.CaptureShield(ability, w.Target) == null, "captives excluded"); }
        w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "mapIndexOrState", (sbyte)1); Check.Equal(true, BloodMageTrials.CaptureShield(ability, w.Target) == null, "cross map excluded"); Set(w.Target, "mapIndexOrState", (sbyte)0);
        Set(w.Target, "factionInt", w.Enemy); Check.Equal(true, BloodMageTrials.CaptureShield(ability, w.Target) == null, "enemy excluded"); Set(w.Target, "factionInt", w.Caster.Faction);
        var ward = w.Ward(10); Check.Equal(false, BloodMageTrials.CompleteShield(BloodMageTrials.CaptureShield(ability, w.Target)), "unchanged shield excluded");
        var before = BloodMageTrials.CaptureShield(ability, w.Target); HealthUtility.AdjustSeverity(w.Target, w.WardDef, 3); Set(ward.comps[0], "linkedPawn", w.Target);
        Check.Equal(false, BloodMageTrials.CompleteShield(before), "foreign link rejected");
        before = BloodMageTrials.CaptureShield(ability, w.Target); HealthUtility.AdjustSeverity(w.Target, w.WardDef, 3); Set(ward.comps[0], "linkedPawn", w.Caster);
        Check.Equal(true, BloodMageTrials.CompleteShield(before), "actual positive native adjustment with own link qualifies"); Check.Equal(false, BloodMageTrials.CompleteShield(before), "single use");
    }
    static void ShieldDrift(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability(); w.Injure(); var before = BloodMageTrials.CaptureShield(ability, w.Target); var ward = w.Ward(5);
        w.Magic.AbilityData.Powers.Remove(ability); Check.Equal(false, BloodMageTrials.CompleteShield(before), "runtime ownership drift rejected"); w.Magic.AbilityData.Powers.Add(ability);
        before = BloodMageTrials.CaptureShield(ability, w.Target); Set(ward, "severityInt", 8f); w.Owner(false).learned = false; Check.Equal(false, BloodMageTrials.CompleteShield(before), "learned owner drift rejected"); w.Owner(false).learned = true;
        w.Target.health.hediffSet.hediffs.Remove(ward); before = BloodMageTrials.CaptureShield(ability, w.Target); w.Ward(10);
        Check.Equal(true, BloodMageTrials.CompleteShield(before), "fresh shield qualifies"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual still required");
    }
    static void Ignite(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); w.Hit(w.Explosion(w.Launch(false)), 30); w.Hit(w.Explosion(null), 30);
        Check.Equal(0f, w.Path.BloodMageTrials.IgniteDamage, "copied/item/no provenance rejected");
        var e = w.Explosion(w.Launch());
        foreach (var amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) w.Hit(e, amount);
        Check.Equal(0f, w.Path.BloodMageTrials.IgniteDamage, "zero blocked and invalid actual damage rejected");
        var prior = BloodMageTrials.EnterExplosion(e, w.Target); var hit = BloodMageTrials.BeginHit(w.Target, new DamageInfo(Burn, 7, instigator: w.Caster));
        var nested = BloodMageTrials.BeginHit(w.Target, new DamageInfo(Burn, 99, instigator: w.Caster)); BloodMageTrials.CompleteHit(nested, new DamageWorker.DamageResult { totalDamageDealt = 99 }); BloodMageTrials.EndHit(nested);
        BloodMageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 7 }); BloodMageTrials.EndHit(hit); BloodMageTrials.ExitExplosion(prior);
        Check.Equal(7f, w.Path.BloodMageTrials.IgniteDamage, "nested hit cannot borrow provenance"); w.Hit(e, 7); Check.Equal(14f, w.Path.BloodMageTrials.IgniteDamage, "each result consumed once");
        w.Hit(e, 12); Check.Equal(20f, w.Path.BloodMageTrials.IgniteDamage, "bounded sum"); Check.Equal(true, w.Path.CompletedTrials.Contains(BloodMageTrials.MasteryTrialId), "20 actual damage completes trial"); Check.Equal(InitiationStage.Adept, w.Path.Stage, "ritual remains required");
    }
    static void Boundaries(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var e = w.Explosion(w.Launch());
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; w.Hit(e, 30); }
        w.Target.guest.guestStatusInt = GuestStatus.Guest; Set(w.Target, "factionInt", w.Caster.Faction); w.Hit(e, 30); Set(w.Target, "factionInt", w.Enemy);
        Set(w.Target, "mapIndexOrState", (sbyte)1); w.Hit(e, 30); Set(w.Target, "mapIndexOrState", (sbyte)0);
        var previous = BloodMageTrials.EnterExplosion(e, w.Target); var hit = BloodMageTrials.BeginHit(w.Target, new DamageInfo(Burn, 30, instigator: w.Caster));
        w.Owner(true).learned = false; BloodMageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 30 }); BloodMageTrials.EndHit(hit); BloodMageTrials.ExitExplosion(previous); w.Owner(true).learned = true;
        previous = BloodMageTrials.EnterExplosion(e, w.Target); hit = BloodMageTrials.BeginHit(w.Target, new DamageInfo(Burn, 30, instigator: w.Caster)); Set(w.Path, "stage", InitiationStage.Master);
        BloodMageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 30 }); BloodMageTrials.EndHit(hit); BloodMageTrials.ExitExplosion(previous);
        Check.Equal(0f, w.Path.BloodMageTrials.IgniteDamage, "recipient owner and stage boundaries reject credit");
    }
    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new(); public TestProjectile projectile = null!; public Explosion explosion = null!;
        public void ExposeData() { Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref projectile, "projectile"); Scribe_Deep.Look(ref explosion, "explosion"); }
    }
    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!; readonly string id = "rwom.bloodtrial." + Guid.NewGuid(); readonly object harmony;
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            foreach (var patch in new[] { typeof(LightningLaunchPatch), typeof(LightningProjectileSavePatch), typeof(LightningExplosionSavePatch), typeof(LightningDamagePatch), typeof(LightningExplosionDamagePatch) })
            { var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!; processor.GetType().GetMethod("Patch")!.Invoke(processor, null); }
        }
        public void Headless()
        {
            void Prefix(MethodInfo target, string name)
            {
                var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(BloodMageTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
                type.GetMethod("Patch")!.Invoke(harmony, [target, hook, null, null, null]);
            }
            Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(FixedPosition));
            foreach (var method in typeof(Verse.Sound.SoundStarter).GetMethods().Where(m => m.Name == "PlayOneShot")) Prefix(method, nameof(Skip));
            Prefix(typeof(EffecterDef).GetMethod("Spawn", Type.EmptyTypes)!, nameof(SpawnEffect));
            foreach (var method in typeof(Effecter).GetMethods().Where(m => m.Name == "Trigger" || m.Name == "Cleanup")) Prefix(method, nameof(Skip));
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static bool Skip() => false;
    static bool FixedPosition(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    static bool SpawnEffect(ref Effecter __result) { __result = (Effecter)RuntimeHelpers.GetUninitializedObject(typeof(Effecter)); return false; }
    static void Native(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); hooks.Headless();
        var ability = w.Ability(); w.Injure(); var ward = w.Ward(10, w.Target);
        var verb = new Verb_BloodShield { Ability = ability }; Set(verb, "caster", w.Caster); Set(verb, "arcaneDmg", 0f);
        verb.ApplyBloodShield(w.Target); Check.Equal(false, w.Path.CompletedTrials.Contains(BloodMageTrials.FoundationTrialId), "native zero delta rejected");
        Set(verb, "arcaneDmg", 1f); verb.ApplyBloodShield(w.Target);
        Check.Equal(110f, ward.Severity, "native shield adjustment preserved"); Check.Equal(true, w.Path.CompletedTrials.Contains(BloodMageTrials.FoundationTrialId), "observer runs after native linkedPawn assignment");
        w.Adept(); ability = w.Ability(true); var launchVerb = new Verb_BLOS { Ability = ability }; Set(launchVerb, "caster", w.Caster);
        var projectile = new TestProjectile { def = w.ProjectileDef }; Set(projectile, "mapIndexOrState", (sbyte)0);
        var prior = BloodMageTrials.BeginLaunch(launchVerb, w.ProjectileDef);
        try { projectile.Launch(w.Caster, ability.Def, UnityEngine.Vector3.zero, w.Target, ProjectileHitFlags.IntendedTarget); }
        finally { BloodMageTrials.EndLaunch(prior); }
        var e = w.Explosion(projectile); w.Hit(e, 7);
        Check.Equal(7f, w.Path.BloodMageTrials.IgniteDamage, "actual framework launch stamped by shared dispatcher");
        // Native TakeDamage's zero result must remain authoritative even with a valid source.
        var scope = BloodMageTrials.EnterExplosion(e, w.Target);
        try { w.Target.TakeDamage(new DamageInfo(Burn, 0, instigator: w.Caster)); } finally { BloodMageTrials.ExitExplosion(scope); }
        Check.Equal(7f, w.Path.BloodMageTrials.IgniteDamage, "actual native zero damage gives no credit");
    }
    public sealed class ShieldEnvelope : IExposable
    {
        public static HediffComp_BloodShield current = null!;
        public void ExposeData() => current.CompExposeData();
    }

    static void SaveShield(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var ward = w.Ward(23f);
        var before = (HediffComp_BloodShield)ward.comps[0];
        before.linkedPawn = null;
        Set(before, "initialized", true); Set(before, "lastSeverity", 40f);
        Set(before, "initializeDelay", 2); Set(before,"removeNow",true);
        var file = System.IO.Path.Combine(f.WorkingDirectory,"shield-pulse.xml");
        try
        {
            ShieldEnvelope.current = before;
            var envelope = new ShieldEnvelope();
            try { Scribe.saver.InitSaving(file,"regression"); Scribe_Deep.Look(ref envelope,"ward"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            HediffComp_BloodShield Load()
            {
                ShieldEnvelope.current = new HediffComp_BloodShield { parent = ward };
                ShieldEnvelope loaded = null!;
                try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded,"ward"); Scribe.loader.FinalizeLoading(); }
                finally { Scribe.ForceStop(); }
                return ShieldEnvelope.current;
            }
            var restored = Load();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            float Baseline(HediffComp_BloodShield c) => (float)c.GetType().GetField("lastSeverity",fields)!.GetValue(c)!;
            Check.Equal(17f,Baseline(restored)-ward.Severity,"shield loss awaiting healing survives loading");
            Check.Equal(true,restored.CompShouldRemove,"a finished shield cannot resume healing after save");
            var xml=System.Xml.Linq.XDocument.Load(file);
            foreach(var field in new[]{"TM_bloodShieldLastSeverity","TM_bloodShieldDelay","TM_bloodShieldEnding"})
                xml.Descendants(field).Single().Remove();
            xml.Save(file);
            restored=Load();
            Check.Equal(ward.Severity,Baseline(restored),"an old save grants no speculative healing");
            Check.Equal(false,restored.CompShouldRemove,"old live shield remains usable");
        }
        finally { ShieldEnvelope.current = null!; }
    }

    static void Save(RuntimeFixture f)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            SaveFieldAndTrial(f);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }

    static void SaveFieldAndTrial(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); w.Adept(); var projectile = w.Launch(); var e = w.Explosion(projectile); w.Hit(e, 11);
        Set(projectile,"arcaneDmg",2.75f);
        Set(projectile,"BF",new List<BloodFire>{new(new IntVec3(3,0,4),2)});
        projectile.SeedCaster(null); projectile.usedTarget = projectile.intendedTarget = IntVec3.Zero; e.instigator = null;
        var data = new Envelope { state = w.Progression.State, projectile = projectile, explosion = e }; var file = System.IO.Path.Combine(f.WorkingDirectory, "blood-trial.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        Check.Equal(2.75f,(float)typeof(Projectile_IgniteBlood).GetField("arcaneDmg",flags)!.GetValue(loaded.projectile)!,"active field potency survives Scribe");
        var cell=((List<BloodFire>)typeof(Projectile_IgniteBlood).GetField("BF",flags)!.GetValue(loaded.projectile)!).Single();
        Check.Equal(new IntVec3(3,0,4),cell.position,"saved flame cell");
        Check.Equal(2,cell.pulseCount,"saved pulse is not replayed");
        Set(w.Progression, "state", loaded.state); loaded.projectile.SeedCaster(w.Caster); Set(loaded.projectile, "mapIndexOrState", (sbyte)0); loaded.explosion.instigator = w.Caster;
        Check.Equal(11f, w.Path.BloodMageTrials.IgniteDamage, "bounded progress survives Scribe");
        w.Hit(loaded.explosion, 4); Check.Equal(15f, w.Path.BloodMageTrials.IgniteDamage, "saved explosion source survives Scribe");
        w.Hit(w.Explosion(loaded.projectile), 5); Check.Equal(20f, w.Path.BloodMageTrials.IgniteDamage, "saved projectile can stamp later pulse after load");
    }
}
