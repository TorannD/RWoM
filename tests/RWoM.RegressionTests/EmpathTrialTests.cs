using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class EmpathTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EmpathTrial.ActualMoodAndOwnershipBoundaries", Mood);
        yield return new("EmpathTrial.NativeMindKillerSuccessAndResistance", NativeMind);
        yield return new("EmpathTrial.AuraActivationAndNewSuppression", Suppression);
        yield return new("EmpathTrial.NativeToggleAndAuraPulse", NativeAura);
        yield return new("EmpathTrial.AuraNativeScribeSource", Save);
        yield return new("EmpathTrial.MissingOrWrongProfileFailsClosed", ProfileBoundary);
    }
    sealed class Fixture : IDisposable
    {
        readonly IDisposable definitions;
        readonly Game previous = Current.Game;
        readonly PawnCapacityDef[] capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly HediffDef oldAura = TorannMagicDefOf.TM_SuppressiveAuraHD, oldSuppression = TorannMagicDefOf.TM_EmotionSuppressionHD, oldShock = TorannMagicDefOf.TM_MindKillerHD;
        readonly EffecterDef oldWave = TorannMagicDefOf.TM_MKWaveED;
        readonly HediffDef oldAbilityAura;
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public Faction Enemy;
        public DisciplineProgress Path => Progression.State.Find("rwom.empath");
        public Fixture(RuntimeFixture f)
        {
            definitions = EmpathProgressionTests.LoadDefinitions(f);
            oldAbilityAura = TorannMagicDefOf.TM_SuppressiveAura.abilityHediff;
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(TraitDefOf)); RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(BodyPartTagDefOf));
            if (DefDatabase<PawnCapacityDef>.DefCount == 0) DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "EmpathTrialCapacity" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) map.events = new MapEvents(map);
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World)); world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } }; Enemy = new Faction { def = new FactionDef() };
            world.factionManager.AllFactionsListForReading.Add(faction); world.factionManager.AllFactionsListForReading.Add(Enemy); Set(world.factionManager, "ofPlayer", faction);
            Set(faction, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } }); Set(Enemy, "relations", new List<FactionRelation> { new() { other = faction, kind = FactionRelationKind.Hostile } });
            int id = 55100;
            foreach (var p in new[] { Caster, Target })
            {
                p.thingIDNumber = id++; p.guest = new Pawn_GuestTracker(p); p.kindDef = new PawnKindDef { defName = "EmpathTrialPawn" }; p.mindState = new Verse.AI.Pawn_MindState(p);
                p.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "EmpathTrialBody", hitPoints = 100, tags = new() } } };
                p.def.race.body.corePart.body = p.def.race.body; p.def.race.body.AllParts.Add(p.def.race.body.corePart);
                Set(p.def.race, "fleshType", new FleshTypeDef { defName = "EmpathTrialFlesh", isOrganic = true });
                Set(p, "positionInt", new IntVec3(3, 0, 3)); Set(p, "mapIndexOrState", (sbyte)0); Set(p, "factionInt", faction);
                p.needs.mood = new Need_Mood(p); p.needs.mood.CurLevel = .8f;
            }
            Set(Target, "factionInt", Enemy);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression); Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.empath", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "EmpathTrialProfile", profileId = "rwom.empath", domain = DisciplineDomain.Magic });
            TorannMagicDefOf.TM_SuppressiveAuraHD = new HediffDef { defName = "TM_SuppressiveAuraHD", maxSeverity = 1, initialSeverity = .5f, hediffClass = typeof(HediffWithComps), comps = new() { new HediffCompProperties { compClass = typeof(HediffComp_SuppressiveAura) } } };
            TorannMagicDefOf.TM_EmotionSuppressionHD = new HediffDef { defName = "TM_EmotionSuppressionHD", maxSeverity = 1, hediffClass = typeof(Hediff) };
            TorannMagicDefOf.TM_MindKillerHD = new HediffDef { defName = "TM_MindKillerHD", maxSeverity = 1, hediffClass = typeof(Hediff) };
            foreach (var hd in new[] { TorannMagicDefOf.TM_SuppressiveAuraHD, TorannMagicDefOf.TM_EmotionSuppressionHD, TorannMagicDefOf.TM_MindKillerHD }) DefDatabase<HediffDef>.Add(hd);
            TorannMagicDefOf.TM_SuppressiveAura.abilityHediff = TorannMagicDefOf.TM_SuppressiveAuraHD; TorannMagicDefOf.TM_MKWaveED = new EffecterDef();
        }
        public MagicPower Owner(bool aura) => EmpathOwners.Resolve(Magic.MagicData)!.Single(p => p.TMabilityDefs.Contains(aura ? TorannMagicDefOf.TM_SuppressiveAura : TorannMagicDefOf.TM_MindKiller));
        public PawnAbility Ability(bool aura = false, bool permanent = true)
        { Owner(aura).learned = true; var a = new PawnAbility(Magic); Set(a, "powerDef", aura ? TorannMagicDefOf.TM_SuppressiveAura : TorannMagicDefOf.TM_MindKiller); if (permanent) Magic.AbilityData.Powers.Add(a); return a; }
        public void Adept() { Path.RecordCompletedTrial(EmpathTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.empath", InitiationStage.Foundation, EmpathTrials.FoundationTrialId); Set(Target, "factionInt", Caster.Faction); }
        public HediffComp_SuppressiveAura Aura(bool proven = true)
        { var a = Ability(true); var s = proven ? EmpathTrials.CaptureAura(a) : null; HealthUtility.AdjustSeverity(Caster, TorannMagicDefOf.TM_SuppressiveAuraHD, .5f); EmpathTrials.CompleteAura(s); return Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SuppressiveAuraHD).TryGetComp<HediffComp_SuppressiveAura>(); }
        public void Dispose()
        {
            Current.Game = previous; TorannMagicDefOf.TM_SuppressiveAuraHD = oldAura; TorannMagicDefOf.TM_EmotionSuppressionHD = oldSuppression; TorannMagicDefOf.TM_MindKillerHD = oldShock; TorannMagicDefOf.TM_MKWaveED = oldWave;
            TorannMagicDefOf.TM_SuppressiveAura.abilityHediff = oldAbilityAura; Restore(profiles); Restore(hediffs); Restore(capacities); definitions.Dispose();
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var d in defs) DefDatabase<T>.Add(d); }
    }
    static void Mood(RuntimeFixture f)
    {
        using var w = new Fixture(f); var a = w.Ability();
        var throughBody = typeof(EmpathTrials).GetMethod("CaptureMood", BindingFlags.NonPublic | BindingFlags.Static)!;
        Check.Equal(true, throughBody.Invoke(null, [a, w.Target, w.Target]) == null, "a different caster without possession cannot credit the original");
        Check.Equal(true, EmpathTrials.CaptureMood(w.Ability(permanent: false), w.Target) == null, "nonpermanent copy/item runtime rejected");
        Check.Equal(false, EmpathTrials.CompleteMood(EmpathTrials.CaptureMood(a, w.Target)), "no mood delta rejected");
        Set(w.Target, "factionInt", w.Caster.Faction); Check.Equal(true, EmpathTrials.CaptureMood(a, w.Target) == null, "friendly fire excluded"); Set(w.Target, "factionInt", w.Enemy);
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; Check.Equal(true, EmpathTrials.CaptureMood(a, w.Target) == null, "captive excluded"); } w.Target.guest.guestStatusInt = GuestStatus.Guest;
        w.Target.needs.mood.CurLevel = 0; Check.Equal(true, EmpathTrials.CaptureMood(a, w.Target) == null, "zero mood excluded"); w.Target.needs.mood.CurLevel = .8f;
        var s = EmpathTrials.CaptureMood(a, w.Target); w.Target.needs.mood.CurLevel -= .3f; w.Owner(false).learned = false; Check.Equal(false, EmpathTrials.CompleteMood(s), "owner drift rejected"); w.Owner(false).learned = true;
        s = EmpathTrials.CaptureMood(a, w.Target); w.Target.needs.mood.CurLevel -= .3f; Check.Equal(true, EmpathTrials.CompleteMood(s), "actual hostile mood decrease qualifies"); Check.Equal(false, EmpathTrials.CompleteMood(s), "one shot"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual required");
    }
    static void ProfileBoundary(RuntimeFixture f)
    {
        foreach (bool aura in new[] { false, true })
        foreach (bool missing in new[] { false, true })
        {
            using var w = new Fixture(f); using var hooks = new Hooks();
            var profile = ClassIdentity.Profile("rwom.empath");
            var ability = w.Ability(aura);
            if (aura) w.Adept();
            var source = aura ? w.Aura() : null;
            var mood = aura ? null : EmpathTrials.CaptureMood(ability, w.Target);
            var suppression = aura ? EmpathTrials.CaptureSuppression(source!, w.Target) : null;
            Check.Equal(true, aura ? suppression != null : mood != null, "valid profile captures trial");
            if (missing) DefDatabase<ClassDisciplineDef>.Clear(); else profile.domain = DisciplineDomain.Might;
            Check.Equal(true, aura ? EmpathTrials.CaptureSuppression(source!, w.Target) == null : EmpathTrials.CaptureMood(ability, w.Target) == null, "invalid profile cannot capture trial");
            if (aura) HealthUtility.AdjustSeverity(w.Target, TorannMagicDefOf.TM_EmotionSuppressionHD, .5f);
            else w.Target.needs.mood.CurLevel -= .3f;
            Check.Equal(false, aura ? EmpathTrials.CompleteSuppression(suppression) : EmpathTrials.CompleteMood(mood), "profile loss after capture cannot award trial");
            Check.Equal(false, w.Path.CompletedTrials.Contains(aura ? EmpathTrials.MasteryTrialId : EmpathTrials.FoundationTrialId), "invalid profile preserves trial state");
        }
    }
    static void Suppression(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); var source = w.Aura(false); w.Adept();
        Check.Equal(true, EmpathTrials.CaptureSuppression(source, w.Target) == null, "old unmarked active aura excluded");
        w.Caster.health.hediffSet.hediffs.Clear(); var a = w.Ability(true, false); var activation = EmpathTrials.CaptureAura(a); Check.Equal(true, activation == null, "item/copy cannot mark aura");
        source = w.Aura(); Check.Equal(true, source.ownTrialActivation, "own source marked");
        Check.Equal(false, EmpathTrials.CompleteSuppression(EmpathTrials.CaptureSuppression(source, w.Target)), "no actual effect rejected");
        HealthUtility.AdjustSeverity(w.Target, TorannMagicDefOf.TM_EmotionSuppressionHD, .5f); Check.Equal(true, EmpathTrials.CaptureSuppression(source, w.Target) == null, "refresh excluded"); w.Target.health.hediffSet.hediffs.Clear();
        var s = EmpathTrials.CaptureSuppression(source, w.Target); HealthUtility.AdjustSeverity(w.Target, TorannMagicDefOf.TM_EmotionSuppressionHD, .5f); source.trialCaster = w.Target; Check.Equal(false, EmpathTrials.CompleteSuppression(s), "source drift excluded"); source.trialCaster = w.Caster; w.Target.health.hediffSet.hediffs.Clear();
        s = EmpathTrials.CaptureSuppression(source, w.Target); HealthUtility.AdjustSeverity(w.Target, TorannMagicDefOf.TM_EmotionSuppressionHD, .5f); Check.Equal(true, EmpathTrials.CompleteSuppression(s), "new suppression qualifies"); Check.Equal(false, EmpathTrials.CompleteSuppression(s), "repeat excluded");
    }
    static void Cast(Verb_UseAbility verb, Pawn caster)
    { Set(verb, "caster", caster); typeof(Verb_UseAbility).IsAssignableFrom(verb.GetType()); verb.GetType().GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(verb, null); }
    static void NativeMind(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); nearby = w.Target; var a = w.Ability(); success = -1;
        Cast(new Verb_MindKiller { Ability = a }, w.Caster); Check.Equal(.8f, w.Target.needs.mood.CurLevel, "native resisted branch unchanged"); Check.Equal(false, w.Path.CompletedTrials.Contains(EmpathTrials.FoundationTrialId), "resisted no credit");
        success = 1; Cast(new Verb_MindKiller { Ability = a }, w.Caster); Check.Equal(.5f, w.Target.needs.mood.CurLevel, "native .3 mood burn"); Check.Equal(true, w.Path.CompletedTrials.Contains(EmpathTrials.FoundationTrialId), "native effect hook qualifies");
    }
    static void NativeAura(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); var a = w.Ability(true); Cast(new Verb_ToggleHediff { Ability = a }, w.Caster);
        var source = w.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SuppressiveAuraHD).TryGetComp<HediffComp_SuppressiveAura>(); Check.Equal(true, source.ownTrialActivation, "actual native toggle stamps source before Adept");
        w.Adept(); nearby = w.Target; Set(Find.TickManager, "ticksGameInt", 500); float adjustment = 0; success = -1; source.CompPostTick(ref adjustment);
        Check.Equal(false, w.Target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD), "resisted pulse no effect"); success = 1; source.CompPostTick(ref adjustment);
        Check.Equal(true, w.Target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmotionSuppressionHD), "actual pulse created suppression"); Check.Equal(true, w.Path.CompletedTrials.Contains(EmpathTrials.MasteryTrialId), "native pulse qualifies");
        Cast(new Verb_ToggleHediff { Ability = a }, w.Caster); Check.Equal(false, w.Caster.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SuppressiveAuraHD), "native toggle off preserved");
    }
    public sealed class Envelope(Pawn pawn) : IExposable
    {
        public HediffSet hediffs = null!;
        // Native HediffSet restores each child's pawn during cross-reference resolution.
        public void ExposeData() => Scribe_Deep.Look(ref hediffs, "hediffs", pawn);
    }
    static void Save(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); var source = w.Aura(); var data = new Envelope(w.Caster) { hediffs = w.Caster.health.hediffSet }; string file = Path.Combine(f.WorkingDirectory, "empath-aura.xml");
        try { Scribe.saver.InitSaving(file, "trial"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data", w.Caster); var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!; directory.RegisterLoaded(w.Caster); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        var aura = loaded.hediffs.GetFirstHediffOfDef(TorannMagicDefOf.TM_SuppressiveAuraHD);
        var saved = aura.TryGetComp<HediffComp_SuppressiveAura>(); Check.Same(w.Caster, saved.trialCaster, "native source caster reference resolves"); Check.Same(w.Caster, aura.pawn, "native parent pawn reference resolves"); Check.Equal(true, saved.ownTrialActivation, "activation marker persists");
        w.Caster.health.hediffSet = loaded.hediffs; w.Adept(); nearby = w.Target; success = 1; Set(Find.TickManager, "ticksGameInt", 500); float adjustment = 0; saved.CompPostTick(ref adjustment); Check.Equal(true, w.Path.CompletedTrials.Contains(EmpathTrials.MasteryTrialId), "loaded actual source pulse qualifies");
    }
    static Pawn? nearby; static float success = 1;
    static bool Nearby(ref List<Pawn> __result) { __result = nearby == null ? new() : new() { nearby }; return false; }
    static bool Success(ref float __result) { __result = success; return false; }
    static bool Position(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    static bool SpawnEffect(ref Effecter __result) { __result = (Effecter)RuntimeHelpers.GetUninitializedObject(typeof(Effecter)); __result.def = TorannMagicDefOf.TM_MKWaveED; return false; }
    static bool Skip() => false;
    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!; readonly string id = "rwom.empathtrial." + Guid.NewGuid(); readonly object harmony;
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!; Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(Skip));
            Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(Position));
            Prefix(typeof(TM_Calc).GetMethod("FindAllPawnsAround")!, nameof(Nearby));
            foreach (var m in typeof(TM_Calc).GetMethods().Where(m => m.Name == "GetSpellSuccessChance")) Prefix(m, nameof(Success));
            Prefix(typeof(Verb_MindKiller).GetMethod("DrawEffects")!, nameof(Skip));
            foreach (var m in typeof(TM_Action).GetMethods().Where(m => m.Name == "DamageEntities")) Prefix(m, nameof(Skip));
            Prefix(typeof(Verse.AI.MentalStateHandler).GetMethod("TryStartMentalState")!, nameof(Skip));
            foreach (var m in typeof(Verse.Sound.SoundStarter).GetMethods().Where(m => m.Name == "PlayOneShot")) Prefix(m, nameof(Skip));
            Prefix(typeof(EffecterDef).GetMethod("Spawn", Type.EmptyTypes)!, nameof(SpawnEffect));
            foreach (var m in typeof(Effecter).GetMethods().Where(m => m.Name == "Trigger" || m.Name == "Cleanup")) Prefix(m, nameof(Skip));
            foreach (var m in typeof(FleckMaker).GetMethods().Where(m => m.Name == "ThrowLightningGlow" || m.Name == "ThrowDustPuff")) Prefix(m, nameof(Skip));
        }
        void Prefix(MethodInfo m, string name) { var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(EmpathTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!; type.GetMethod("Patch")!.Invoke(harmony, [m, hook, null, null, null]); }
        public void Dispose() { type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); nearby = null; success = 1; }
    }
}
