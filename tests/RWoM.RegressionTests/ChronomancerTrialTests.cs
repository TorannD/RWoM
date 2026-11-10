using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class ChronomancerTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ChronomancerTrial.ReverseCorpseNeverAddsRotOrChargesFailedRevival", f => {
            using var w=new Fixture(f);
            var verb=new Verb_ReverseTime();Set(verb,"pwrVal",1);Set(verb,"verVal",0);
            var corpse=new Corpse();
            var rot=new CompRottable { parent=corpse };
            rot.Initialize(new CompProperties_Rottable { daysToRotStart=5 });
            Set(corpse,"comps",new List<ThingComp> { rot });
            var apply=typeof(Verb_ReverseTime).GetMethod("AgeCorpse",BindingFlags.NonPublic|BindingFlags.Instance)!;
            bool Reverse(float amount) {
                Set(rot,"rotProgressInt",amount);
                return (bool)apply.Invoke(verb,[corpse,w.Caster])!;
            }
            Check.Equal(false,Reverse(100),"insufficient versatility cannot revive");
            Check.Equal(false,Reverse(12000),"no forced increase to the minimum retained rot");
            Check.Equal(12000f,rot.RotProgress,"failed cast leaves corpse untouched");
            Check.Equal(true,Reverse(100000),"decayed corpse can be partially restored");
            Check.Equal(27500f,rot.RotProgress,"one useful rot reduction");
        });
        yield return new("ChronomancerTrial.AccelerationAgeDoesNotOverflow", f => {
            var next=typeof(HediffComp_AccelerateTime).GetMethod("NextAge",BindingFlags.Static|BindingFlags.NonPublic)!;
            long Step(long ticks,int years,bool bad)=>(long)next.Invoke(null,[ticks,years,3.5f,bad])!;
            Check.Equal(2500027500L,Step(2500000000L,99,false),"older ticks never narrow to a 32-bit value");
            Check.Equal(2503600000L,Step(2500000000L,100,true),"harmful century advances one year");
            Check.Equal(long.MaxValue,Step(long.MaxValue-5,200,true),"modded extreme ages do not wrap");
        });
        yield return new("ChronomancerTrial.AlterFateRequiresOwnedPendingEventOnCurrentMap", AlterFate);
        yield return new("ChronomancerTrial.PredictionExactQueueAndActiveOwnedSource", Prediction);
        yield return new("ChronomancerTrial.PredictionSourceAndStageBoundaries", PredictionBoundaries);
        yield return new("ChronomancerTrial.PredictionMarkerScribe", MarkerSave);
        yield return new("ChronomancerTrial.NativePredictionPresentationFailureKeepsOneQueuedEvent", PresentationFailure);
        yield return new("ChronomancerTrial.NativeReverseRepairAndFood", NativeReverse);
        yield return new("ChronomancerTrial.ReverseOwnershipTargetsAndDrift", ReverseBoundaries);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef prediction = TorannMagicDefOf.TM_PredictionHD;
        readonly StatDef hp = StatDefOf.MaxHitPoints;
        public Pawn Caster;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public ThingWithComps Item;
        public CompRottable Rot;
        public IncidentQueue Queue;
        public Faction Enemy;
        public DisciplineProgress Path => Progression.State.Find("rwom.chronomancer");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) map.events = new MapEvents(map);
            var storyteller = (Storyteller)RuntimeHelpers.GetUninitializedObject(typeof(Storyteller)); Queue = new IncidentQueue(); storyteller.incidentQueue = Queue; Set(Current.Game, "storyteller", storyteller);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic;
            var faction = new Faction { def = new FactionDef { isPlayer = true } }; Enemy = new Faction { def = new FactionDef() };
            Set(faction, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } });
            Set(Enemy, "relations", new List<FactionRelation> { new() { other = faction, kind = FactionRelationKind.Hostile } });
            Caster.guest = new Pawn_GuestTracker(Caster); Caster.kindDef = new PawnKindDef { defName = "ChronomancerTrialPawn" };
            Set(Caster, "factionInt", faction); Set(Caster, "mapIndexOrState", (sbyte)0);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.chronomancer", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "ChronomancerTrialProfile", profileId = "rwom.chronomancer", domain = DisciplineDomain.Magic });
            TorannMagicDefOf.TM_PredictionHD = new HediffDef { defName = "TM_PredictionHD", maxSeverity = 1000, hediffClass = typeof(HediffWithComps) };
            StatDefOf.MaxHitPoints = FleshStatTests.Stat("ChronomancerTrialHitPoints", 1000);
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = "ChronomancerTrialItem"; def.category = ThingCategory.Item; def.useHitPoints = true;
            Item = new ThingWithComps { def = def, HitPoints = 100 }; Set(Item, "mapIndexOrState", (sbyte)0);
            Rot = new CompRottable { parent = Item }; Rot.Initialize(new CompProperties_Rottable { daysToRotStart = 5 }); Set(Item, "comps", new List<ThingComp> { Rot }); Set(Rot, "rotProgressInt", 100f);
        }
        public MagicPower Owner(bool reverse) => Magic.MagicData.MagicPowersC.Single(p => p.TMabilityDefs.Contains(reverse ? TorannMagicDefOf.TM_ReverseTime : TorannMagicDefOf.TM_Prediction));
        public PawnAbility Ability(bool reverse = false, bool owned = true)
        { Owner(reverse).learned = true; var a = new PawnAbility(Magic); Set(a, "powerDef", reverse ? TorannMagicDefOf.TM_ReverseTime : TorannMagicDefOf.TM_Prediction); if (owned) Magic.AbilityData.Powers.Add(a); return a; }
        public void Adept() { Path.RecordCompletedTrial(ChronomancerTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.chronomancer", InitiationStage.Foundation, ChronomancerTrials.FoundationTrialId); }
        public HediffComp_Prediction Effect()
        {
            var comp = new HediffComp_Prediction(); var hd = new HediffWithComps { pawn = Caster, def = TorannMagicDefOf.TM_PredictionHD, comps = new() { comp } }; comp.parent = hd;
            Set(hd, "severityInt", 1f); Caster.health.hediffSet.hediffs.Add(hd); return comp;
        }
        public QueuedIncident Entry() => new QueuedIncident(new FiringIncident(new IncidentDef { defName = "ChronomancerTrialIncident" }, null, new IncidentParms { target = Caster.Map }), 500);
        public void Dispose()
        {
            Current.Game = previous; TorannMagicDefOf.TM_PredictionHD = prediction; StatDefOf.MaxHitPoints = hp;
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
        }
    }
    static void AlterFate(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var ability=new MagicAbility(w.Magic){Def=TorannMagicDefOf.TM_AlterFate};
        var method=typeof(Verb_AlterFate).GetMethod("Available",BindingFlags.Static|BindingFlags.NonPublic)!;
        bool Ready() => (bool)method.Invoke(null,[w.Caster,ability])!;
        var entry=w.Entry();
        w.Magic.predictionIncidentDef=entry.FiringIncident.def;
        Check.Equal(false,Ready(),"stale label cannot alter anything in the queue");
        ChronomancerPrediction.Clear(w.Magic);
        Check.Equal(true,ChronomancerPrediction.TryEnqueue(w.Magic,w.Queue,entry),"owned native incident inserted");
        Check.Equal(true,Ready(),"exact pending event is eligible");
        Set(w.Caster,"mapIndexOrState",(sbyte)1);
        Check.Equal(false,Ready(),"caster moved away from the incident map");
        Set(w.Caster,"mapIndexOrState",(sbyte)0);
        Check.Same(entry,ChronomancerPrediction.ResolveOwned(w.Magic,w.Queue),"rejected cast keeps queue untouched");
    }
    static void Prediction(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability();
        var activation = ChronomancerTrials.CapturePrediction(ability, w.Caster); var hd = w.Effect();
        Check.Equal(true, ChronomancerTrials.CompletePrediction(activation), "new own native effect receives provenance");
        Check.Equal(false, w.Path.CompletedTrials.Contains(ChronomancerTrials.FoundationTrialId), "toggle alone does not complete trial");
        var entry = w.Entry(); Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "not queued rejected");
        Check.Equal(true, ChronomancerPrediction.TryEnqueue(w.Magic, w.Queue, entry), "native queue accepts exact entry");
        w.Magic.predictionHash++; Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "metadata drift rejected"); w.Magic.predictionHash--;
        Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, w.Entry()), "same-shaped different entry rejected");
        Check.Equal(true, ChronomancerTrials.RecordPrediction(w.Magic, entry), "actual queued prediction with owned active source completes");
        Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "repeat rejected");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual still required");
    }
    static void PredictionBoundaries(RuntimeFixture f)
    {
        using var w = new Fixture(f); var ability = w.Ability(); var copy = w.Ability(owned: false);
        Check.Equal(true, ChronomancerTrials.CapturePrediction(copy, w.Caster) == null, "item/copy toggle cannot mark effect");
        var hd = w.Effect(); var entry = w.Entry(); ChronomancerPrediction.TryEnqueue(w.Magic, w.Queue, entry);
        Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "unmarked old/item effect rejected despite current learned owner");
        Check.Equal(true, ChronomancerTrials.CapturePrediction(ability, w.Caster) == null, "unchanged active effect cannot gain marker");
        w.Caster.health.hediffSet.hediffs.Remove(hd.parent); var activation = ChronomancerTrials.CapturePrediction(ability, w.Caster); hd = w.Effect();
        w.Magic.AbilityData.Powers.Remove(ability); Check.Equal(false, ChronomancerTrials.CompletePrediction(activation), "activation owner drift rejected"); w.Magic.AbilityData.Powers.Add(ability);
        w.Caster.health.hediffSet.hediffs.Remove(hd.parent); activation = ChronomancerTrials.CapturePrediction(ability, w.Caster); hd = w.Effect(); ChronomancerTrials.CompletePrediction(activation);
        Set(w.Caster, "mapIndexOrState", (sbyte)1); Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "different event map rejected"); Set(w.Caster, "mapIndexOrState", (sbyte)0);
        hd.removeNow = true; Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "inactive source rejected"); hd.removeNow = false;
        w.Owner(false).learned = false; Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "native owner absent rejected"); w.Owner(false).learned = true;
        Set(w.Path, "stage", InitiationStage.Adept); Check.Equal(false, ChronomancerTrials.RecordPrediction(w.Magic, entry), "stage drift rejected");
    }
    public sealed class MarkerEnvelope : IExposable
    {
        public HediffComp_Prediction source = new();
        public void ExposeData() => source.CompExposeData();
    }
    static void MarkerSave(RuntimeFixture f)
    {
        var envelope = new MarkerEnvelope(); envelope.source.ownPredictionTrialSource = true; var file = System.IO.Path.Combine(f.WorkingDirectory, "prediction-marker.xml");
        try { Scribe.saver.InitSaving(file, "trial"); Scribe_Deep.Look(ref envelope, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        MarkerEnvelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Check.Equal(true, loaded.source.ownPredictionTrialSource, "own native activation marker survives Scribe");
        Check.Equal(false, new HediffComp_Prediction().ownPredictionTrialSource, "legacy/default effect unmarked");
    }
    static int failedLetters;
    static bool EligibleIncident(ref bool __result) { __result = true; return false; }
    static bool FailLetter() { failedLetters++; throw new NullReferenceException("Injected post-enqueue letter failure"); }
    sealed class PredictionPresentationHooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.chronopresentation." + Guid.NewGuid(); readonly object harmony;
        public PredictionPresentationHooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            void Prefix(MethodInfo method, string name)
            {
                var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ChronomancerTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
                type.GetMethod("Patch")!.Invoke(harmony, [method, hook, null, null, null]);
            }
            // The incident's environmental prerequisites and UI are boundaries; source selection, chance, insertion and trial are native.
            Prefix(typeof(IncidentWorker).GetMethod("CanFireNow")!, nameof(EligibleIncident));
            foreach (var method in typeof(LetterStack).GetMethods().Where(m => m.Name == "ReceiveLetter")) Prefix(method, nameof(FailLetter));
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void PresentationFailure(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var language = new SpellItemEffectTests.LanguageScope(f.Repository); using var hooks = new PredictionPresentationHooks();
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(IncidentCategoryDefOf)); RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(LetterDefOf));
        var originalCategory = IncidentCategoryDefOf.Misc; var bypass = TorannMagic.ModOptions.Constants.GetBypassPrediction();
        try
        {
            IncidentCategoryDefOf.Misc = new IncidentCategoryDef { defName = "ChronomancerTrialMisc" };
            TorannMagic.ModOptions.Constants.SetBypassPrediction(false);
            foreach (var key in new[] { "TM_PredictionLetter", "TM_PredictionText" }) LanguageDatabase.activeLanguage.keyedReplacements[key] = new LoadedLanguage.KeyedReplacement { key = key, value = "Prediction" };
            foreach (var map in Current.Game.Maps) { map.info = new MapInfo { Size = new IntVec3(10, 1, 10) }; map.mapPawns = new MapPawns(map); }
            Set(w.Caster.Map.mapPawns, "pawnsSpawned", new List<Pawn> { w.Caster });
            w.Caster.Name = new NameSingle("Predictor");
            Set(Current.Game, "letterStack", new LetterStack());
            var ability = w.Ability(); var activation = ChronomancerTrials.CapturePrediction(ability, w.Caster); w.Effect();
            Check.Equal(true, ChronomancerTrials.CompletePrediction(activation), "native owned active provenance ready");
            var def = new IncidentDef { defName = "ChronomancerTrialPresentation", label = "trial event", workerClass = typeof(IncidentWorker), category = IncidentCategoryDefOf.Misc };
            var worker = new IncidentWorker { def = def }; var parms = new IncidentParms { target = w.Caster.Map };
            failedLetters = 0; bool runOriginal = true, result = false;
            for (int seed = 0; seed < 100 && w.Queue.Count == 0; seed++)
            {
                Rand.PushState(seed);
                try { runOriginal = TorannMagicMod.IncidentWorker_TryExecute_Prefix_Patch.Prefix(worker, ref parms, ref result); }
                finally { Rand.PopState(); }
            }
            Check.Equal(1, failedLetters, "injected letter failure occurs after actual insertion");
            Check.Equal(false, runOriginal, "post-commit presentation failure suppresses immediate original execution");
            Check.Equal(true, result, "original caller receives successful intercepted outcome");
            Check.Equal(1, w.Queue.Count, "one actual queued event survives");
            var entry = ChronomancerPrediction.ResolveOwned(w.Magic, w.Queue);
            Check.Equal(true, entry != null && ReferenceEquals(entry.FiringIncident.parms, parms), "exact native incident params retained");
            Check.Equal(true, w.Path.CompletedTrials.Contains(ChronomancerTrials.FoundationTrialId), "trial recorded from actual native insertion before failed presentation");
        }
        finally { IncidentCategoryDefOf.Misc = originalCategory; TorannMagic.ModOptions.Constants.SetBypassPrediction(bypass); }
    }
    sealed class VisualHook : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.chronotrial." + Guid.NewGuid(); readonly object harmony;
        public VisualHook()
        {
            harmony = Activator.CreateInstance(type, id)!;
            var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ChronomancerTrialTests).GetMethod(nameof(Skip), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            type.GetMethod("Patch")!.Invoke(harmony, [typeof(Verb_ReverseTime).GetMethod("TransmutateEffects")!, hook, null, null, null]);
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static bool Skip() => false;
    static void Apply(Verb_ReverseTime verb, Thing thing, bool food) => typeof(Verb_ReverseTime).GetMethod(food ? "AgeFood" : "AgeThing", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(verb, new object[] { thing });
    static void NativeReverse(RuntimeFixture f)
    {
        foreach (bool food in new[] { false, true })
        {
            using var w = new Fixture(f); using var hook = new VisualHook(); w.Adept(); var ability = w.Ability(true);
            var verb = new Verb_ReverseTime { Ability = ability }; Set(verb, "caster", w.Caster); Set(verb, "pwrVal", 1);
            if (food) Set(w.Rot, "rotProgressInt", 0f); else w.Item.HitPoints = 1000;
            Apply(verb, w.Item, food); Check.Equal(false, w.Path.CompletedTrials.Contains(ChronomancerTrials.MasteryTrialId), "fresh/full item no improvement");
            if (food) Set(w.Rot, "rotProgressInt", 100f); else w.Item.HitPoints = 100;
            Apply(verb, w.Item, food);
            Check.Equal(true, w.Path.CompletedTrials.Contains(ChronomancerTrials.MasteryTrialId), "native useful mutation observed");
            if (food) Check.Equal(27.5f, w.Rot.RotProgress, "native food formula unchanged"); else Check.Equal(400, w.Item.HitPoints, "native repair formula unchanged");
        }
    }
    static void ReverseBoundaries(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability(true);
        Check.Equal(true, ChronomancerTrials.CaptureReverse(w.Ability(true, false), w.Item, false) == null, "copy/item instance rejected");
        Set(w.Item, "factionInt", w.Enemy); Check.Equal(true, ChronomancerTrials.CaptureReverse(ability, w.Item, false) == null, "hostile item rejected"); Set(w.Item, "factionInt", null!);
        Set(w.Item, "mapIndexOrState", (sbyte)1); Check.Equal(true, ChronomancerTrials.CaptureReverse(ability, w.Item, false) == null, "other map rejected"); Set(w.Item, "mapIndexOrState", (sbyte)0);
        Check.Equal(false, ChronomancerTrials.CompleteReverse(ChronomancerTrials.CaptureReverse(ability, w.Item, false)), "unchanged target rejected");
        var trial = ChronomancerTrials.CaptureReverse(ability, w.Item, false); w.Item.HitPoints += 10; w.Magic.AbilityData.Powers.Remove(ability);
        Check.Equal(false, ChronomancerTrials.CompleteReverse(trial), "runtime ownership drift rejected"); w.Magic.AbilityData.Powers.Add(ability);
        trial = ChronomancerTrials.CaptureReverse(ability, w.Item, true); Set(w.Rot, "rotProgressInt", float.NaN);
        Check.Equal(false, ChronomancerTrials.CompleteReverse(trial), "invalid rot mutation rejected"); Set(w.Rot, "rotProgressInt", 100f);
        trial = ChronomancerTrials.CaptureReverse(ability, w.Item, false); w.Item.HitPoints += 10; Set(w.Path, "stage", InitiationStage.Master);
        Check.Equal(false, ChronomancerTrials.CompleteReverse(trial), "stage drift rejected");
    }
}
