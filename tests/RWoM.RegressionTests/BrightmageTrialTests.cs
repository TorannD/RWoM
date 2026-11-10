using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class BrightmageTrialTests
{
    static FieldInfo BurnField => typeof(MagicAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf", true)!.GetField("TM_BurningLight")!;
    static DamageDef Burn => (DamageDef)BurnField.GetValue(null)!;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BrightmageTrial.OwnEmitterActualDamageAndNestedScopes", Damage);
        yield return new("BrightmageTrial.EmitterOwnershipAndRecipientBoundaries", Boundaries);
        yield return new("BrightmageTrial.EmitterAndCounterScribe", Save);
        yield return new("BrightmageTrial.BlindnessDeltaAndDrift", Blindness);
        yield return new("BrightmageTrial.NativeLightBurstSeverityMutation", NativeBurst);
        yield return new("BrightmageTrial.NativeZeroDamageDispatcher", NativeZero);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly RecordDef[] records = DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        readonly object oldBurn = BurnField.GetValue(null)!;
        readonly ThingDef oldEmitter = TorannMagicDefOf.FlyingObject_LightLance;
        readonly HediffDef oldBlind = TorannMagicDefOf.TM_LightBurstHD;
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public Faction Enemy;
        public DisciplineProgress Path => Progression.State.Find("rwom.brightmage");
        public Fixture(RuntimeFixture f)
        {
            EnsureEmitterPresentationInitialized();
            LoadBaseDefinitions(f); DefDatabase<RecordDef>.Add(new RecordDef { defName = "BrightTrialRecord" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Find.TickManager, "gameStartAbsTick", 1); Set(Current.Game, "battleLog", new BattleLog());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) map.events = new MapEvents(map);
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World)); world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } }; Enemy = new Faction { def = new FactionDef() };
            world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction);
            Set(faction, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } });
            Set(Enemy, "relations", new List<FactionRelation> { new() { other = faction, kind = FactionRelationKind.Hostile } });
            int id = 43100;
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.thingIDNumber = id++; pawn.guest = new Pawn_GuestTracker(pawn); pawn.kindDef = new PawnKindDef { defName = "BrightTrialPawn" };
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn); pawn.records = new Pawn_RecordsTracker(pawn);
                Set(pawn, "positionInt", IntVec3.Zero); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", faction); Set(Target, "factionInt", Enemy);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.brightmage", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "BrightTrialProfile", profileId = "rwom.brightmage", domain = DisciplineDomain.Magic });
            var emitterDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); emitterDef.defName = "FlyingObject_LightLance"; emitterDef.thingClass = typeof(FlyingObject_LightLance); emitterDef.category = ThingCategory.Projectile; emitterDef.comps = new(); emitterDef.projectile = new ProjectileProperties { speed = 30, flyOverhead = true };
            TorannMagicDefOf.FlyingObject_LightLance = emitterDef; DefDatabase<ThingDef>.Add(emitterDef);
            TorannMagicDefOf.TM_LightBurstHD = new HediffDef { defName = "TM_LightBurstHD", maxSeverity = 1000 };
            DefDatabase<HediffDef>.Add(TorannMagicDefOf.TM_LightBurstHD);
            BurnField.SetValue(null, new DamageDef { defName = "BrightTrialBurn", workerClass = typeof(DamageWorker) });
        }
        public MagicPower Owner(bool burst) => Magic.MagicData.MagicPowersBrightmage.Single(p => p.TMabilityDefs.Contains(burst ? TorannMagicDefOf.TM_LightBurst : TorannMagicDefOf.TM_LightLance));
        public PawnAbility Ability(bool burst = false, bool owned = true)
        { Owner(burst).learned = true; var a = new PawnAbility(Magic); Set(a, "powerDef", burst ? TorannMagicDefOf.TM_LightBurst : TorannMagicDefOf.TM_LightLance); if (owned) Magic.AbilityData.Powers.Add(a); return a; }
        public void Adept() { Path.RecordCompletedTrial(BrightmageTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.brightmage", InitiationStage.Foundation, BrightmageTrials.FoundationTrialId); }
        public FlyingObject_LightLance Emitter(bool owned = true)
        {
            var a = Ability(owned: owned); var snapshot = BrightmageTrials.CaptureSpawn(a);
            var e = new FlyingObject_LightLance { def = TorannMagicDefOf.FlyingObject_LightLance, thingIDNumber = 43200 };
            Set(e, "pawn", Caster); Set(e, "mapIndexOrState", (sbyte)0); BrightmageTrials.CompleteSpawn(snapshot, e); return e;
        }
        public void Hit(FlyingObject_LightLance emitter, float amount, DamageDef? def = null)
        {
            var previous = BrightmageTrials.EnterEmitter(emitter, Target); var hit = BrightmageTrials.BeginHit(Target, new DamageInfo(def ?? Burn, amount, instigator: Caster));
            try { BrightmageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = amount }); BrightmageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = amount }); }
            finally { BrightmageTrials.EndHit(hit); BrightmageTrials.ExitEmitter(previous); }
        }
        public Hediff Blind(float severity)
        { var hd = new Hediff { pawn = Target, def = TorannMagicDefOf.TM_LightBurstHD }; Set(hd, "severityInt", severity); Target.health.hediffSet.hediffs.Add(hd); return hd; }
        public void Dispose()
        {
            BrightmageTrials.ExitEmitter(null!); Current.Game = previous; TorannMagicDefOf.FlyingObject_LightLance = oldEmitter; TorannMagicDefOf.TM_LightBurstHD = oldBlind; BurnField.SetValue(null, oldBurn);
            Restore(profiles); Restore(things); Restore(hediffs); Restore(records);
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var d in defs) DefDatabase<T>.Add(d); }
    }
    static void Damage(RuntimeFixture f)
    {
        using var w = new Fixture(f); var emitter = w.Emitter();
        var unrelated = BrightmageTrials.BeginHit(w.Target, new DamageInfo(Burn, 30, instigator: w.Caster)); BrightmageTrials.CompleteHit(unrelated, new DamageWorker.DamageResult { totalDamageDealt = 30 }); BrightmageTrials.EndHit(unrelated);
        Check.Equal(0f, w.Path.BrightmageTrials.LanceDamage, "shared BurningLight without exact emitter scope earns nothing");
        w.Hit(w.Emitter(false), 30); foreach (float n in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) w.Hit(emitter, n);
        Check.Equal(0f, w.Path.BrightmageTrials.LanceDamage, "copied emitter and invalid actual damage excluded");
        var previous = BrightmageTrials.EnterEmitter(emitter, w.Target); var hit = BrightmageTrials.BeginHit(w.Target, new DamageInfo(Burn, 7, instigator: w.Caster));
        var nested = BrightmageTrials.BeginHit(w.Target, new DamageInfo(Burn, 99, instigator: w.Caster)); BrightmageTrials.CompleteHit(nested, new DamageWorker.DamageResult { totalDamageDealt = 99 }); BrightmageTrials.EndHit(nested);
        BrightmageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 7 }); BrightmageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 7 }); BrightmageTrials.EndHit(hit); BrightmageTrials.ExitEmitter(previous);
        Check.Equal(7f, w.Path.BrightmageTrials.LanceDamage, "nested damage cannot borrow source and result consumed once");
        w.Hit(emitter, 8); w.Hit(emitter, 9); Check.Equal(20f, w.Path.BrightmageTrials.LanceDamage, "sum bounded20");
        Check.Equal(true, w.Path.CompletedTrials.Contains(BrightmageTrials.FoundationTrialId), "actual cumulative damage completes trial"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual still required");
    }
    static void Boundaries(RuntimeFixture f)
    {
        using var w = new Fixture(f); var emitter = w.Emitter();
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; w.Hit(emitter, 30); } w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "factionInt", w.Caster.Faction); w.Hit(emitter, 30); Set(w.Target, "factionInt", w.Enemy);
        Set(w.Target, "mapIndexOrState", (sbyte)1); w.Hit(emitter, 30); Set(w.Target, "mapIndexOrState", (sbyte)0);
        w.Hit(emitter, 30, new DamageDef { defName = "UnrelatedDamage" });
        w.Owner(false).learned = false; w.Hit(emitter, 30); w.Owner(false).learned = true;
        var abilities = w.Magic.AbilityData.Powers.ToArray(); w.Magic.AbilityData.Powers.Clear(); w.Hit(emitter, 30); w.Magic.AbilityData.Powers.AddRange(abilities);
        var previous = BrightmageTrials.EnterEmitter(emitter, w.Target); var hit = BrightmageTrials.BeginHit(w.Target, new DamageInfo(Burn, 30, instigator: w.Caster)); w.Adept(); BrightmageTrials.CompleteHit(hit, new DamageWorker.DamageResult { totalDamageDealt = 30 }); BrightmageTrials.EndHit(hit); BrightmageTrials.ExitEmitter(previous);
        Check.Equal(0f, w.Path.BrightmageTrials.LanceDamage, "recipient owner runtime and stage boundaries reject");
    }
    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new(); public FlyingObject_LightLance emitter = null!;
        public void ExposeData() { Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref emitter, "emitter"); }
    }
    static void Save(RuntimeFixture f)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            SaveLance(f);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }
    static void SaveLance(RuntimeFixture f)
    {
        using var w = new Fixture(f); var emitter = w.Emitter(); w.Hit(emitter, 7);
        emitter.usedTarget = emitter.intendedTarget = IntVec3.Zero;
        emitter.def.projectile.speed = 200f;
        Set(emitter, "origin", new UnityEngine.Vector3(5, 0, 5));
        Set(emitter, "destination", new UnityEngine.Vector3(25, 0, 5));
        Set(emitter, "ticksToImpact", 6);
        Set(emitter, "speed", emitter.def.projectile.speed);
        Set(emitter, "lightPotency", 1.6f);
        var positionBeforeSave = emitter.ExactPosition;
        var envelope = new Envelope { state = w.Progression.State, emitter = emitter }; var file = Path.Combine(f.WorkingDirectory, "bright-lance.xml");
        try { Scribe.saver.InitSaving(file, "trial"); Scribe_Deep.Look(ref envelope, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope Load(string path)
        {
            Envelope result = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref result, "data");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(w.Caster); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            return result;
        }
        var loaded = Load(file);
        Check.Same(w.Caster, loaded.emitter.TrialCaster, "native saved pawn reference resolves to exact caster");
        Check.Equal(positionBeforeSave, loaded.emitter.ExactPosition, "moving lance resumes from the same point");
        Check.Equal(1.6f, (float)typeof(FlyingObject_LightLance).GetField("lightPotency", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(loaded.emitter)!, "moving lance keeps the paid light potency");
        Set(w.Progression, "state", loaded.state); Set(loaded.emitter, "mapIndexOrState", (sbyte)0);
        Check.Equal(7f, w.Path.BrightmageTrials.LanceDamage, "counter survives native Scribe");
        Check.Equal(w.Caster.thingIDNumber, loaded.emitter.trialSource.casterId, "real emitter saved source survives");
        w.Hit(loaded.emitter, 13); Check.Equal(20f, w.Path.BrightmageTrials.LanceDamage, "saved source contributes after load");
        var xml = System.Xml.Linq.XDocument.Load(file);
        var row = xml.Descendants("emitter").Single();
        row.Element("TM_lanceLightPotency")!.Remove();
        foreach (var name in new[]{"origin","destination"}) row.Add(new System.Xml.Linq.XElement(row.Elements(name).Single()));
        string oldFile = Path.Combine(f.WorkingDirectory, "bright-lance-old.xml");
        xml.Save(oldFile);
        var old = Load(oldFile).emitter;
        Check.Equal(positionBeforeSave, old.ExactPosition, "original duplicated coordinates resume using the authored speed");
        Check.Equal(.5f, (float)typeof(FlyingObject_LightLance).GetField("lightPotency", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(old)!, "historical save did not record light potency");
    }
    static void Blindness(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability(true); var hd = w.Blind(1);
        Check.Equal(true, BrightmageTrials.CaptureBurst(w.Ability(true, false), w.Target) == null, "copied burst rejected");
        Check.Equal(false, BrightmageTrials.CompleteBurst(BrightmageTrials.CaptureBurst(ability, w.Target)), "unchanged effect rejected");
        Set(w.Target, "factionInt", w.Caster.Faction); Check.Equal(true, BrightmageTrials.CaptureBurst(ability, w.Target) == null, "ally rejected"); Set(w.Target, "factionInt", w.Enemy);
        var trial = BrightmageTrials.CaptureBurst(ability, w.Target); HealthUtility.AdjustSeverity(w.Target, hd.def, 1); w.Magic.AbilityData.Powers.Remove(ability);
        Check.Equal(false, BrightmageTrials.CompleteBurst(trial), "runtime source drift rejected"); w.Magic.AbilityData.Powers.Add(ability);
        trial = BrightmageTrials.CaptureBurst(ability, w.Target); HealthUtility.AdjustSeverity(w.Target, hd.def, 1);
        Check.Equal(true, BrightmageTrials.CompleteBurst(trial), "real positive native severity delta qualifies"); Check.Equal(false, BrightmageTrials.CompleteBurst(trial), "single use");
    }
    static Pawn? nearby;
    static bool Nearby(ref List<Pawn> __result) { __result = new() { nearby! }; return false; }
    static bool Skip() => false;
    static void EnsureEmitterPresentationInitialized()
    {
        var type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        string id = "rwom.brighttrial.material." + Guid.NewGuid(); var harmony = Activator.CreateInstance(type, id)!;
        try
        {
            var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(BrightmageTrialTests).GetMethod(nameof(Skip), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            foreach (var method in typeof(MaterialPool).GetMethods().Where(m => m.Name == nameof(MaterialPool.MatFrom)))
                type.GetMethod("Patch")!.Invoke(harmony, [method, hook, null, null, null]);
            var packs = LoadedModManager.RunningModsListForReading.ToArray();
            try
            {
                LoadedModManager.RunningModsListForReading.Clear();
                RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle);
                RuntimeHelpers.RunClassConstructor(typeof(FlyingObject_LightLance).TypeHandle);
            }
            finally { LoadedModManager.RunningModsListForReading.AddRange(packs); }
        }
        finally { type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
    }

    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!; readonly string id = "rwom.brighttrial." + Guid.NewGuid(); readonly object harmony;
        public Hooks(bool burst)
        {
            harmony = Activator.CreateInstance(type, id)!;
            if (burst)
            {
                Prefix(typeof(TM_Calc).GetMethod("FindAllPawnsAround")!, nameof(Nearby));
                foreach (var m in typeof(TM_Action).GetMethods().Where(m => m.Name == "DamageEntities")) Prefix(m, nameof(Skip));
            }
            else
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [typeof(LightningDamagePatch)])!; processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        void Prefix(MethodInfo m, string name)
        { var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(BrightmageTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!; type.GetMethod("Patch")!.Invoke(harmony, [m, hook, null, null, null]); }
        public void Dispose() { type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); nearby = null; }
    }
    static void NativeBurst(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(true); w.Adept(); var ability = w.Ability(true); w.Blind(1); nearby = w.Target;
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(BodyPartTagDefOf));
        var previousSight = BodyPartTagDefOf.SightSource;
        try
        {
            BodyPartTagDefOf.SightSource = new BodyPartTagDef { defName = "BrightTrialSightSource" };
            var body = new BodyDef(); var eye = new BodyPartRecord { body = body, def = new BodyPartDef { defName = "BrightTrialEye", tags = new() { BodyPartTagDefOf.SightSource } } }; body.corePart = eye; body.AllParts.Add(eye);
            w.Target.def.race.body = body; Set(w.Target.def.race, "fleshType", new FleshTypeDef { defName = "BrightTrialFlesh", isOrganic = true });
            var verb = new Verb_LightBurst { Ability = ability }; Set(verb, "caster", w.Caster); Set(verb, "lightPotency", .5f); Set(verb, "pwrVal", 0);
            verb.ApplyEffects(IntVec3.Zero, w.Caster.Map, 0); Check.Equal(false, w.Path.CompletedTrials.Contains(BrightmageTrials.MasteryTrialId), "native zero multiplier gives no credit");
            verb.ApplyEffects(IntVec3.Zero, w.Caster.Map, 2); Check.Equal(true, w.Path.CompletedTrials.Contains(BrightmageTrials.MasteryTrialId), "native ApplyEffects severity mutation earns trial");
            Check.Equal(1.1f, w.Target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightBurstHD).Severity, "native blind formula unchanged");
        }
        finally { BodyPartTagDefOf.SightSource = previousSight; }
    }
    static void NativeZero(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(false); var emitter = w.Emitter();
        var previous = BrightmageTrials.EnterEmitter(emitter, w.Target);
        try { w.Target.TakeDamage(new DamageInfo(Burn, 0, instigator: w.Caster)); } finally { BrightmageTrials.ExitEmitter(previous); }
        Check.Equal(0f, w.Path.BrightmageTrials.LanceDamage, "shared real TakeDamage zero result grants nothing");
    }
}
