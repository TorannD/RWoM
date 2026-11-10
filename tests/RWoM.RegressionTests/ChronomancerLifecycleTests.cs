using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static class ChronomancerLifecycleTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ChronomancerLifecycle.RecallRetainsClassAndCustodyMarkersFromOldMarks", RecallIdentity);
        yield return new("ChronomancerLifecycle.RecallKeepsNewNeedsAndHandlesShortLegacySnapshots", RecallNeeds);
        yield return new("ChronomancerLifecycle.NativeQueueScribeKeepsIdenticalEventsDistinct", SaveDistinct);
        yield return new("ChronomancerLifecycle.UnknownAndAmbiguousOwnershipNeverDeletesEvents", Ambiguous);
        yield return new("ChronomancerLifecycle.ExactReplacementAndFailedInsertionPreserveOtherOwners", Replace);
        yield return new("ChronomancerLifecycle.ReturningPredictionRemainsRecognizedAfterOwnerClears", Returning);
        yield return new("ChronomancerLifecycle.NativeQueueDispatchesReturningPredictionExactlyOnce", NativeDispatch);
        yield return new("ChronomancerLifecycle.AttemptedNativeRetrySurvivesPredictionExpiry", RetryExpiry);
    }
    static void RecallIdentity(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r);
        var body = f.Host;
        Hediff[] markers = [
            new Hediff_ClassDiscipline { def = new HediffDef { defName = "TM_ClassDiscipline", hediffClass = typeof(Hediff_ClassDiscipline) } },
            new Hediff_SpiritRebirthForm { def = new HediffDef { defName = "TM_SpiritRebirthForm", hediffClass = typeof(Hediff_SpiritRebirthForm) } },
            new HediffWithComps { def = TorannMagicDefOf.TM_SpiritPossessionHD },
            new Hediff_Addiction { def = new HediffDef { defName = "TestAddiction", hediffClass = typeof(Hediff_Addiction) } }
        ];
        foreach (var marker in markers) { marker.pawn = body; body.health.hediffSet.hediffs.Add(marker); }
        f.Magic.recallInjuriesList = [];
        f.Magic.recallHediffList = markers.Select(m=>new Hediff { def = m.def }).ToList();
        f.Magic.recallHediffDefSeverityList = markers.Select(m=>.5f).ToList();
        typeof(TM_Action).GetMethod("RecallHediffs", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null,[body,f.Magic]);
        foreach (var marker in markers)
            Check.Same(marker, body.health.hediffSet.hediffs.Single(h=>h.def==marker.def), "old snapshot cannot erase or recreate identity or custody");
        var current = new Hediff { pawn = body, def = new HediffDef { defName = "CurrentCondition" } };
        body.health.hediffSet.hediffs.Add(current);
        f.Magic.recallHediffList = [new Hediff { def = current.def }];
        f.Magic.recallHediffDefSeverityList = [];
        typeof(TM_Action).GetMethod("RecallHediffs", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null,[body,f.Magic]);
        Check.Same(current, body.health.hediffSet.hediffs.Single(h=>h.def==current.def), "missing severity leaves live health intact");
    }
    static void RecallNeeds(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r);
        var pawn = f.Original;
        pawn.needs ??= new Pawn_NeedsTracker(pawn);
        Need_Rest MakeNeed(string name, float level)
        {
            var need = (Need_Rest)RuntimeHelpers.GetUninitializedObject(typeof(Need_Rest));
            GrimoireProgressionTests.Set(need, "pawn", pawn);
            need.def = new NeedDef { defName = name };
            pawn.needs.AllNeeds.Add(need);
            need.CurLevel = level;
            return need;
        }
        var food = MakeNeed("Food", .25f);
        var newNeed = MakeNeed("NewModNeed", .42f);
        var addiction = MakeNeed("Chemical_Luciferium", .13f);
        var recall = typeof(TM_Action).GetMethod("RecallNeeds", BindingFlags.NonPublic | BindingFlags.Static)!;
        f.Magic.recallNeedDefnames = ["Food", "Chemical_Luciferium", "NewModNeed"];
        f.Magic.recallNeedValues = [.78f, .95f];
        recall.Invoke(null, [pawn, f.Magic]);
        Check.Equal(.78f, food.CurLevel, "matching snapshot restored");
        Check.Equal(.13f, addiction.CurLevel, "luciferium remains outside recall");
        Check.Equal(.42f, newNeed.CurLevel, "incomplete snapshot does not overwrite new need");
        Check.Equal(true, pawn.needs.AllNeeds.Contains(newNeed) && pawn.needs.AllNeeds.Contains(addiction), "same need instances are retained");
        f.Magic.recallNeedValues = null;
        recall.Invoke(null, [pawn, f.Magic]);
        Check.Equal(.78f, food.CurLevel, "missing old values are harmless");
    }
    public sealed class Envelope : IExposable
    {
        public IncidentQueue Queue = new();
        public CompAbilityUserMagic First = new(), Second = new();
        public void ExposeData()
        {
            Scribe_Deep.Look(ref Queue, "queue");
            Scribe.EnterNode("first"); ChronomancerPrediction.ExposeComp(First); Scribe.ExitNode();
            Scribe.EnterNode("second"); ChronomancerPrediction.ExposeComp(Second); Scribe.ExitNode();
        }
    }
    sealed class Fixture : IDisposable
    {
        readonly IncidentDef[] previous = DefDatabase<IncidentDef>.AllDefsListForReading.ToArray();
        readonly object harmony;
        readonly string patchId = "rwom.chrono.lifecycle." + Guid.NewGuid();
        public readonly IncidentDef Def = new() { defName = "ChronomancerLifecycleIncident", workerClass = typeof(ProbeWorker) };
        public Fixture()
        {
            DefDatabase<IncidentDef>.Add(Def);
            var assembly = Assembly.Load("0Harmony");
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, patchId)!;
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ChronomancerPredictionScribePatch).GetMethod("Postfix")!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { typeof(QueuedIncident).GetMethod("ExposeData"), null, hook, null, null });
        }
        public QueuedIncident Event(int tick = 7500) => new(new FiringIncident(Def, null, new IncidentParms()), tick, 500);
        public void PatchDispatch()
        {
            var assembly = Assembly.Load("0Harmony");
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ChronomancerLifecycleTests).GetMethod(nameof(DispatchBoundary), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { typeof(Storyteller).GetMethod("TryFire"), hook, null, null, null });
        }
        public void Dispose()
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { patchId });
            DefDatabase<IncidentDef>.Clear(); foreach (var def in previous) DefDatabase<IncidentDef>.Add(def);
        }
    }
    static List<QueuedIncident> Entries(IncidentQueue queue)
    {
        var result = new List<QueuedIncident>(); foreach (QueuedIncident entry in queue) result.Add(entry); return result;
    }
    static string Save(RuntimeFixture f, Envelope envelope, string name)
    {
        string path = Path.Combine(f.WorkingDirectory, name + ".xml");
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "native"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        return path;
    }
    static Envelope Load(string path)
    {
        Envelope envelope = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "native"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        return envelope;
    }
    static Envelope Pair(Fixture f)
    {
        var state = new Envelope();
        Check.Equal(true, ChronomancerPrediction.TryEnqueue(state.First, state.Queue, f.Event()), "first prediction queued");
        Check.Equal(true, ChronomancerPrediction.TryEnqueue(state.Second, state.Queue, f.Event()), "identical second prediction queued separately");
        state.Queue.Add(f.Event());
        return state;
    }
    static void SaveDistinct(RuntimeFixture runtime)
    {
        using var f = new Fixture(); var state = Pair(f);
        var oldFirst = ChronomancerPrediction.ResolveOwned(state.First, state.Queue);
        string firstToken = state.First.predictionToken, secondToken = state.Second.predictionToken;
        var loaded = Load(Save(runtime, state, "chrono-identical-queue"));
        Check.Equal(3, loaded.Queue.Count, "native deep queue restored once");
        Check.Equal(firstToken, loaded.First.predictionToken, "first owner token persisted");
        Check.Equal(secondToken, loaded.Second.predictionToken, "second owner token persisted");
        var first = ChronomancerPrediction.ResolveOwned(loaded.First, loaded.Queue);
        var second = ChronomancerPrediction.ResolveOwned(loaded.Second, loaded.Queue);
        Check.Equal(true, first != null && second != null && !ReferenceEquals(first, second), "identical type/tick entries resolve to different exact objects");
        Check.Equal(false, ReferenceEquals(oldFirst, first), "actual native reconstruction, not stale object reuse");
        Check.Equal(false, first!.TriedToFire, "native pending flag retained");
        Check.Equal(500, first.RetryDurationTicks, "native retry window retained");
        Check.Equal(true, ChronomancerPrediction.Matches(loaded.First, first.FiringIncident.parms, loaded.Queue), "reloaded exact params recognized");
        Check.Equal(false, ChronomancerPrediction.Matches(loaded.First, second!.FiringIncident.parms, loaded.Queue), "same-type foreign params rejected");
        var unrelated = Entries(loaded.Queue).Single(q => q != first && q != second);
        ChronomancerPrediction.Expire(loaded.First, loaded.Queue);
        Check.Equal(2, loaded.Queue.Count, "only expiring owned entry removed");
        Check.Same(second, ChronomancerPrediction.ResolveOwned(loaded.Second, loaded.Queue), "other caster's exact event survives");
        Check.Equal(true, Entries(loaded.Queue).Contains(unrelated), "ordinary untagged event survives");
        ChronomancerPrediction.Expire(loaded.First, loaded.Queue);
        Check.Equal(2, loaded.Queue.Count, "repeated expiry cannot delete foreign entries");
    }
    static void Ambiguous(RuntimeFixture runtime)
    {
        using var f = new Fixture(); var state = Pair(f);
        string path = Save(runtime, state, "chrono-ambiguous");
        var xml = XDocument.Load(path);
        var queueTags = xml.Root!.Element("native")!.Element("queue")!.Descendants("rwomPredictionToken").ToArray();
        Check.Equal(2, queueTags.Length, "only predicted queue entries receive token fields");
        queueTags[1].Value = queueTags[0].Value; xml.Save(path);
        var loaded = Load(path);
        Check.Equal(true, ChronomancerPrediction.ResolveOwned(loaded.First, loaded.Queue) == null, "duplicate token refuses ownership");
        ChronomancerPrediction.Expire(loaded.First, loaded.Queue);
        Check.Equal(3, loaded.Queue.Count, "ambiguous token preserves every event");
        loaded.Second.predictionToken = null!; loaded.Second.predictionIncidentDef = f.Def;
        loaded.Second.predictionTick = 7500; loaded.Second.predictionHash = Entries(loaded.Queue)[0].FiringIncident.parms.GetHashCode();
        ChronomancerPrediction.Expire(loaded.Second, loaded.Queue);
        Check.Equal(3, loaded.Queue.Count, "legacy hash/type/tick never substitutes for exact saved identity");
        Check.Equal(true, loaded.Second.predictionIncidentDef == null, "legacy stale local state clears safely");
    }
    static void Replace(RuntimeFixture runtime)
    {
        using var f = new Fixture(); var state = Pair(f);
        var first = ChronomancerPrediction.ResolveOwned(state.First, state.Queue);
        var second = ChronomancerPrediction.ResolveOwned(state.Second, state.Queue);
        var other = Entries(state.Queue).Single(q => q != first && q != second);
        Check.Equal(false, ChronomancerPrediction.ReplaceOwned(state.First, state.Queue, second, f.Event()), "foreign expected event rejected");
        Check.Equal(false, ChronomancerPrediction.ReplaceOwned(state.First, state.Queue, first, null!), "no viable candidate preserves original");
        Check.Same(first, ChronomancerPrediction.ResolveOwned(state.First, state.Queue), "failed replacement preserves original claim");
        string token = state.First.predictionToken;
        var replacement = f.Event(8100);
        Check.Equal(true, ChronomancerPrediction.ReplaceOwned(state.First, state.Queue, first, replacement), "exact replacement commits");
        Check.Equal(3, state.Queue.Count, "replacement does not duplicate native event");
        Check.Same(replacement, ChronomancerPrediction.ResolveOwned(state.First, state.Queue), "new claim owns actual replacement");
        Check.Equal(false, token == state.First.predictionToken, "replacement gets fresh ownership token");
        Check.Equal(true, Entries(state.Queue).Contains(second) && Entries(state.Queue).Contains(other), "foreign and ordinary entries survive replacement");
        var emptyOwner = new CompAbilityUserMagic();
        Check.Equal(false, ChronomancerPrediction.TryEnqueue(emptyOwner, state.Queue, replacement), "already queued object cannot duplicate or steal ownership");
        Check.Equal(true, string.IsNullOrEmpty(emptyOwner.predictionToken), "failed enqueue leaves no claim");
        Check.Equal(true, ChronomancerPrediction.CancelOwned(state.First, state.Queue), "cancellation removes replacement alone");
        Check.Equal(2, state.Queue.Count, "two unrelated events remain");
    }
    static void Returning(RuntimeFixture runtime)
    {
        using var f = new Fixture(); var state = Pair(f);
        var first = ChronomancerPrediction.ResolveOwned(state.First, state.Queue);
        var parms = first.FiringIncident.parms;
        Check.Equal(true, ChronomancerPrediction.IsReturningPrediction(state.Queue, parms), "exact queued execution recognized");
        ChronomancerPrediction.Clear(state.First); first.Notify_TriedToFire();
        Check.Equal(true, ChronomancerPrediction.IsReturningPrediction(state.Queue, parms), "retry recognized after local owner cleared or toggled off");
        Check.Equal(false, ChronomancerPrediction.IsReturningPrediction(state.Queue, new IncidentParms()), "unrelated execution not suppressed");
        Check.Equal(3, state.Queue.Count, "execution recognition does not remove or requeue events");
    }
    static int dispatches;
    static void RetryExpiry(RuntimeFixture runtime)
    {
        using var f = new Fixture(); var state = Pair(f);
        var attempted = ChronomancerPrediction.ResolveOwned(state.First, state.Queue);
        attempted.Notify_TriedToFire();
        var loaded = Load(Save(runtime, state, "chrono-attempted-retry"));
        var retry = ChronomancerPrediction.ResolveOwned(loaded.First, loaded.Queue);
        Check.Equal(true, retry.TriedToFire, "native retry attempt survives Scribe");
        Check.Equal(500, retry.RetryDurationTicks, "native retry policy survives Scribe");
        ChronomancerPrediction.Expire(loaded.First, loaded.Queue);
        Check.Equal(3, loaded.Queue.Count, "marker expiry cannot delete attempted native retry");
        Check.Equal(true, Entries(loaded.Queue).Contains(retry), "exact retry object retained");
        Check.Equal(true, string.IsNullOrEmpty(loaded.First.predictionToken), "only stale marker ownership cleared");
        Check.Equal(true, ChronomancerPrediction.IsReturningPrediction(loaded.Queue, retry.FiringIncident.parms), "retry still recognized after marker clears");
        ChronomancerPrediction.Expire(loaded.Second, loaded.Queue);
        Check.Equal(2, loaded.Queue.Count, "pending other prediction still expires normally");
        Check.Equal(true, Entries(loaded.Queue).Contains(retry), "pending expiry cannot affect attempted retry");
    }
    static bool DispatchBoundary(FiringIncident fi, ref bool __result)
    {
        // Keep native queue iteration/removal and actual Prediction prefix; stop at
        // world incident execution (raids, letters and StoryState need a full world).
        var worker = new ProbeWorker { def = fi.def };
        var parms = fi.parms; bool result = false;
        var prefix = typeof(CompAbilityUserMagic).Assembly.GetTypes().Single(t => t.Name == "IncidentWorker_TryExecute_Prefix_Patch").GetMethod("Prefix")!;
        object?[] args = { worker, parms, result };
        Check.Equal(true, (bool)prefix.Invoke(null, args)!, "actual Prediction prefix allows returning native event");
        Check.Equal(true, parms.customLetterLabel.StartsWith("Predicted "), "returning path retains native label");
        dispatches++; __result = true; return false;
    }
    sealed class ProbeWorker : IncidentWorker { protected override bool TryExecuteWorker(IncidentParms parms) => true; }
    static void NativeDispatch(RuntimeFixture runtime)
    {
        using var f = new Fixture(); f.PatchDispatch();
        var previous = Current.Game;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            GrimoireProgressionTests.Set(Current.Game, "tickManager", new TickManager());
            GrimoireProgressionTests.Set(Current.Game, "maps", new List<Map>());
            var storyteller = (Storyteller)RuntimeHelpers.GetUninitializedObject(typeof(Storyteller));
            GrimoireProgressionTests.Set(Current.Game, "storyteller", storyteller);
            var state = new Envelope(); storyteller.incidentQueue = state.Queue;
            Check.Equal(true, ChronomancerPrediction.TryEnqueue(state.First, state.Queue, f.Event(0)), "due prediction queued");
            state.Queue.Add(f.Event(10000));
            ChronomancerPrediction.Clear(state.First); // Owner may be elsewhere, have turned off Prediction, or be gone.
            dispatches = 0;
            state.Queue.IncidentQueueTick(); state.Queue.IncidentQueueTick();
            Check.Equal(1, dispatches, "native queue invokes original incident once across repeated ticks");
            Check.Equal(1, state.Queue.Count, "native removal preserves unrelated future event");
            Check.Equal(10000, Entries(state.Queue)[0].FireTick, "unrelated queue deadline unchanged");
        }
        finally { Current.Game = previous; dispatches = 0; }
    }
}
