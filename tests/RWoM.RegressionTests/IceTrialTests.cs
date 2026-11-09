using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class IceTrialTests
{
    static Type DamageDefs => typeof(MagicAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf", true)!;
    static DamageDef SnowDamage { get => (DamageDef)DamageDefs.GetField("Snowball")!.GetValue(null)!; set => DamageDefs.GetField("Snowball")!.SetValue(null, value); }
    static DamageDef IceDamage { get => (DamageDef)DamageDefs.GetField("Iceshard")!.GetValue(null)!; set => DamageDefs.GetField("Iceshard")!.SetValue(null, value); }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IceTrial.ActualDamageAndDistinctAcrossCasts", Damage);
        yield return new("IceTrial.NativeOwnerStageAndVictimEligibility", Eligibility);
        yield return new("IceTrial.PersistedExplosionAndProgress", Save);
        yield return new("IceTrial.NativeWorkerZeroAndCreationScope", Native);
        yield return new("IceTrial.NativeAuraApplication", Aura);
        yield return new("IceTrial.NativeWeatherTransition", Weather);
        yield return new("IceTrial.NestedUnrelatedDamageExcluded", Nested);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly DamageDef[] damageDefs = DefDatabase<DamageDef>.AllDefsListForReading.ToArray();
        readonly DamageDef oldSnowball = SnowDamage, oldIceshard = IceDamage;
        readonly RecordDef[] records = DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        readonly Faction friendly = new() { def = new FactionDef { isPlayer = true } };
        readonly Faction enemy = new() { def = new FactionDef() };
        int nextId = 19000;
        public readonly Pawn Caster, First, Second;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.ice");
        public readonly DamageDef Burn = new() { defName = "FireclawTestBurn", workerClass = typeof(DamageWorker) };
        public readonly ThingDef ExplosionDef;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            SnowDamage = new DamageDef { defName = "IceTrialSnowball", workerClass = typeof(DamageWorker) };
            IceDamage = new DamageDef { defName = "IceTrialIceshard", workerClass = typeof(DamageWorker) };
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
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.ice", DisciplineDomain.Magic);
            Path.RecordCompletedTrial(IceTrials.FoundationTrialId);
            Progression.State.TryAdvanceAfterTrial("rwom.ice", InitiationStage.Foundation, IceTrials.FoundationTrialId);
            Magic.MagicData.MagicPowersHoF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Snowball)).learned = true;
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "FireclawTestProfile", profileId = "rwom.ice", domain = DisciplineDomain.Magic });
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
            if (tagged) IceTrials.Stamp(explosion, Caster, IceTrials.Spell.Snowball);
            return explosion;
        }
        public void Hit(int castId, Pawn victim, float damage)
            => IceTrials.Complete(IceTrials.Capture(Caster, victim, IceTrials.Spell.Snowball), damage);
        public bool Complete => Path.CompletedTrials.Contains(IceTrials.MasteryTrialId);
        public void Dispose()
        {
            IceTrials.ExitExplosion(null!); Current.Game = previous;
            SnowDamage = oldSnowball; IceDamage = oldIceshard;
            Restore(profiles); Restore(things); Restore(damageDefs); Restore(records);
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

    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.ice.tests." + Guid.NewGuid();
        readonly object harmony;
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            foreach (var patch in new[] { typeof(IceExplosionStartPatch), typeof(IceExplosionSavePatch), typeof(IceExplosionDamagePatch), typeof(IceDamagePatch) })
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }


    static void Damage(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (float amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) w.Hit(1, w.First, amount);
        Check.Equal(0, w.Path.IceTrials.SnowballVictims.Count, "finite positive actual damage required");
        var hit = IceTrials.Capture(w.Caster, w.First, IceTrials.Spell.Snowball);
        IceTrials.Complete(hit, 1); IceTrials.Complete(hit, 1); w.Hit(2, w.First, 100);
        Check.Equal(false, w.Complete, "same enemy repeated across casts remains one");
        w.Hit(3, w.Second, 1);
        Check.Equal(true, w.Complete, "second distinct enemy across casts completes mastery");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "no automatic advancement");
        Set(w.Path, "stage", InitiationStage.Foundation);
        Set(w.Path, "completedTrials", new List<string>());
        w.Magic.MagicData.MagicPowersHoF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Icebolt)).learned = true;
        var bolt = IceTrials.Capture(w.Caster, w.First, IceTrials.Spell.Icebolt);
        IceTrials.Complete(bolt, 12); IceTrials.Complete(bolt, 12);
        Check.Equal(12f, w.Path.IceTrials.IceboltDamage, "one result consumed once");
        IceTrials.Complete(IceTrials.Capture(w.Caster, w.Second, IceTrials.Spell.Icebolt), 9);
        Check.Equal(20f, w.Path.IceTrials.IceboltDamage, "damage bounded at threshold");
        Check.Equal(true, w.Path.CompletedTrials.Contains(IceTrials.FoundationTrialId), "cumulative actual damage completes foundation");
    }
    static void Eligibility(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave })
        {
            w.First.guest.guestStatusInt = status;
            Check.Equal(true, IceTrials.Capture(w.Caster, w.First, IceTrials.Spell.Snowball) == null, status + " excluded");
        }
        w.First.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.First, "factionInt", w.Caster.Faction);
        Check.Equal(true, IceTrials.Capture(w.Caster, w.First, IceTrials.Spell.Snowball) == null, "ally excluded");
        Set(w.First, "factionInt", null);
        Check.Equal(true, IceTrials.Capture(w.Caster, w.First, IceTrials.Spell.Snowball) == null, "peaceful factionless excluded");
        Set(w.First.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Manhunter { pawn = w.First, def = new MentalStateDef { category = MentalStateCategory.Aggro } });
        Check.Equal(true, IceTrials.Capture(w.Caster, w.First, IceTrials.Spell.Snowball) != null, "aggressive factionless accepted");
        var power = w.Magic.MagicData.MagicPowersHoF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Snowball));
        power.learned = false;
        Check.Equal(true, IceTrials.Capture(w.Caster, w.Second, IceTrials.Spell.Snowball) == null, "unlearned native owner cannot borrow item/copy credit");
        power.learned = true;
        foreach (var stage in new[] { InitiationStage.Foundation, InitiationStage.Master, InitiationStage.Ascended })
        {
            Set(w.Path, "stage", stage);
            Check.Equal(true, IceTrials.Capture(w.Caster, w.Second, IceTrials.Spell.Snowball) == null, "exact Adept required");
        }
    }
    static void Save(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        w.Hit(1, w.First, 1);
        var data = new Envelope { state = w.Progression.State, explosion = w.Explosion(2) };
        data.explosion.instigator = null;
        var file = Path.Combine(f.WorkingDirectory, "ice-pending-explosion.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Set(w.Progression, "state", loaded.state); loaded.explosion.instigator = w.Caster;
        Check.Equal(1, w.Path.IceTrials.SnowballVictims.Count, "saved distinct enemy retained");
        var prior = IceTrials.EnterExplosion(loaded.explosion, w.Second);
        var hit = IceTrials.BeginHit(w.Second, new DamageInfo(SnowDamage, 5, instigator: w.Caster));
        IceTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 5 });
        IceTrials.EndHit(hit); IceTrials.ExitExplosion(prior);
        Check.Equal(true, w.Complete, "persisted secondary explosion source completes mastery after load");
    }
    static void Native(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        var explosion = w.Explosion(1);
        var worker = new DamageWorker { def = SnowDamage };
        typeof(DamageWorker).GetMethod("ExplosionDamageThing", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(worker, [explosion, w.First, new List<Thing>(), new List<Thing>(), IntVec3.Zero]);
        Check.Equal(0, w.Path.IceTrials.SnowballVictims.Count, "native patched worker zero damage gives no credit");
        var first = w.Explosion(0, false); var nested = w.Explosion(0, false);
        using (IceTrials.Creating(w.Caster, IceTrials.Spell.Snowball))
        { IceTrials.StartExplosion(first); IceTrials.StartExplosion(nested); }
        void Hit(Explosion e, Pawn p)
        {
            var prior = IceTrials.EnterExplosion(e, p);
            var hit = IceTrials.BeginHit(p, new DamageInfo(SnowDamage, 1, instigator: w.Caster));
            IceTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 1 });
            IceTrials.EndHit(hit); IceTrials.ExitExplosion(prior);
        }
        Hit(first, w.First); Hit(nested, w.Second);
        Check.Equal(1, w.Path.IceTrials.SnowballVictims.Count, "only direct primary creation inherits source");
        first.damType = DamageDefOf.Extinguish;
        Hit(first, w.Second);
        Check.Equal(false, w.Complete, "extinguish cannot count even with source tag");
    }
    static void Nested(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var e = w.Explosion(1);
        var prior = IceTrials.EnterExplosion(e, w.First);
        var hit = IceTrials.BeginHit(w.First, new DamageInfo(SnowDamage, 1, instigator: w.Caster));
        var unrelated = IceTrials.BeginHit(w.Second, new DamageInfo(SnowDamage, 1, instigator: w.Caster));
        IceTrials.CompleteHit(unrelated, new DamageWorker.DamageResult { totalDamageDealt = 99 }); IceTrials.EndHit(unrelated);
        IceTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 1 }); IceTrials.EndHit(hit);
        IceTrials.ExitExplosion(prior);
        Check.Equal(1, w.Path.IceTrials.SnowballVictims.Count, "nested unrelated damage not attributed");
        Check.Equal(w.First.thingIDNumber, w.Path.IceTrials.SnowballVictims[0], "only direct worker victim recorded");
    }
    static void Aura(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var previousAura = TorannMagicDefOf.TM_SoothingBreeze_AuraHD;
        var previousEffect = TorannMagicDefOf.SoothingBreeze;
        try
        {
            TorannMagicDefOf.TM_SoothingBreeze_AuraHD = new HediffDef { defName = "IceTrialAura", hediffClass = typeof(Hediff) };
            TorannMagicDefOf.SoothingBreeze = new HediffDef { defName = "IceTrialSoothing", hediffClass = typeof(Hediff), maxSeverity = 1 };
            Set(w.Path, "stage", InitiationStage.Foundation); Set(w.Path, "completedTrials", new List<string>());
            w.Magic.MagicData.MagicPowersHoF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Soothe)).learned = true;
            Set(w.First, "factionInt", w.Caster.Faction);
            var source = new Hediff { def = TorannMagicDefOf.TM_SoothingBreeze_AuraHD, pawn = w.Caster };
            w.Caster.health.hediffSet.hediffs.Add(source);
            var effect = new Hediff { def = TorannMagicDefOf.SoothingBreeze, pawn = w.First };
            w.First.health.hediffSet.hediffs.Add(effect);
            Set(effect, "severityInt", 1f);
            IceTrials.ApplyAura(source, w.First, effect.def);
            Check.Equal(false, w.Path.CompletedTrials.Contains(IceTrials.FoundationTrialId), "existing aura benefit cannot count again");
            Set(effect, "severityInt", 0f);
            w.First.guest.guestStatusInt = GuestStatus.Prisoner;
            IceTrials.ApplyAura(source, w.First, effect.def);
            Check.Equal(false, w.Path.CompletedTrials.Contains(IceTrials.FoundationTrialId), "prisoner excluded despite actual effect");
            Set(effect, "severityInt", 0f); w.First.guest.guestStatusInt = GuestStatus.Guest;
            IceTrials.ApplyAura(source, w.First, effect.def);
            Check.Equal(1f, effect.Severity, "native HealthUtility actually changes recipient severity");
            Check.Equal(true, w.Path.CompletedTrials.Contains(IceTrials.FoundationTrialId), "owned source aura and actual allied effect completes foundation");
        }
        finally { TorannMagicDefOf.TM_SoothingBreeze_AuraHD = previousAura; TorannMagicDefOf.SoothingBreeze = previousEffect; }
    }
    static void Weather(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(WeatherDefOf));
        var clear = new WeatherDef { defName = "IceTrialClear" };
        var rain = new WeatherDef { defName = "IceTrialRain" };
        var map = w.Caster.Map;
        map.weatherManager = new WeatherManager(map) { curWeather = clear };
        var power = w.Magic.MagicData.MagicPowersHoF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Rainmaker));
        power.learned = true;
        map.weatherManager.TransitionTo(rain);
        Check.Equal(false, w.Complete, "natural weather has no trial hook");
        IceTrials.TransitionWeather(w.Caster, map, rain);
        Check.Equal(false, w.Complete, "same weather no-op excluded");
        power.learned = false;
        IceTrials.TransitionWeather(w.Caster, map, clear);
        Check.Equal(false, w.Complete, "unlearned weather transition excluded");
        power.learned = true;
        IceTrials.TransitionWeather(w.Caster, map, rain);
        Check.Same(rain, map.weatherManager.curWeather, "native manager changed weather");
        Check.Equal(true, w.Complete, "owned Rainmaker actual transition completes mastery");
    }}
