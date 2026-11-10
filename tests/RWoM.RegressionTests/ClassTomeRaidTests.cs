using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class ClassTomeRaidTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in RiftCases()) yield return test;
        yield return new("ClassTome.RaidOfferDoesNotActivateOrCreateReward", f => {
            using var game = new GameScope();
            var part = new QuestPart_ClassTomeRaid();
            part.QuestPartTick();
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Offered, part.Phase, "viewing offer cannot advance");
            Check.Equal(0, part.GetDirectlyHeldThings().Count, "no reward before successful battle");
            Check.Equal(false, part.Claim(null!), "offer cannot claim");
        });
        yield return new("ClassTome.RaidAcceptanceHasOnePreparationDeadline", f => {
            using var game = new GameScope();
            Set(Current.Game, "signalManager", new SignalManager());
            var part = new QuestPart_ClassTomeRaid { inSignalEnable = "accept" };
            var quest = new Quest { id = 492 }; quest.AddPart(part);
            part.Notify_QuestSignalReceived(new Signal("other"));
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Offered, part.Phase, "unrelated signals do not accept");
            part.Notify_QuestSignalReceived(new Signal("accept"));
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Preparing, part.Phase, "acceptance only prepares");
            Check.Equal(2500, (int)Get(part, "arrivalTick"), "native tick delay recorded once");
            Set(Find.TickManager, "ticksGameInt", 2499);
            part.Notify_QuestSignalReceived(new Signal("accept")); part.QuestPartTick();
            Check.Equal(2500, (int)Get(part, "arrivalTick"), "duplicate acceptance cannot postpone or repeat arrival");
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Preparing, part.Phase, "no arrival before deadline");
            Check.Equal(0, part.GetDirectlyHeldThings().Count, "preparation cannot pay");
        });
        yield return new("ClassTome.RaidRewardRecoveryUsesExistingOwnedBook", f => {
            using var game = new GameScope();
            var part = new QuestPart_ClassTomeRaid(); var book = Book();
            part.GetDirectlyHeldThings().TryAdd(book, false);
            Set(part, "phase", QuestPart_ClassTomeRaid.RaidPhase.Active);
            var earn = typeof(QuestPart_ClassTomeRaid).GetMethod("EarnReward", BindingFlags.Instance | BindingFlags.NonPublic)!;
            earn.Invoke(part, null); earn.Invoke(part, null);
            Check.Equal(true, part.RewardPending, "post-transfer recovery commits victory");
            Check.Equal(1, part.GetDirectlyHeldThings().Count, "repeated earning never manufactures another book");
            Check.Same(book, part.GetDirectlyHeldThings()[0], "owned identity remains authoritative");
        });
        yield return new("ClassTome.RaidPhysicalTransferExceptionsNeverDuplicate", f => {
            using var game = new GameScope(); using var fault = new SpawnFault();
            var part = new QuestPart_ClassTomeRaid(); var book = Book();
            part.GetDirectlyHeldThings().TryAdd(book, false);
            Set(part, "phase", QuestPart_ClassTomeRaid.RaidPhase.RewardPending);
            var transfer = typeof(QuestPart_ClassTomeRaid).GetMethod("TransferReward", BindingFlags.Instance | BindingFlags.NonPublic)!;
            bool Transfer() => (bool)transfer.Invoke(part, new object?[] { null, IntVec3.Zero })!;
            Check.Equal(false, Transfer(), "pretransfer exception leaves pending claim");
            Check.Same(book, part.GetDirectlyHeldThings()[0], "failed placement restores same object");
            Check.Same(part.GetDirectlyHeldThings(), book.holdingOwner, "actual original ownership restored");
            externalReward = new ThingOwner<Thing>();
            Check.Equal(true, Transfer(), "callback exception after native owner transfer counts as delivery");
            Check.Same(externalReward, book.holdingOwner, "actual external native holder owns original book");
            Check.Equal(0, part.GetDirectlyHeldThings().Count, "quest no longer serializes delivered object");
            Check.Equal(false, Transfer(), "repeat delivery cannot create or place another reward");
            Check.Equal(1, externalReward.Count, "one delivered physical object");
            RuntimeLog.Clear(); // Two deliberately injected placement failures were logged.
        });
        yield return new("ClassTome.RaidEarnCallbackExceptionThenMapLossKeepsBook", f => {
            using var game = new GameScope();
            var part = new QuestPart_ClassTomeRaid(); Set(part, "rewardDef", Book().def);
            Set(part, "phase", QuestPart_ClassTomeRaid.RaidPhase.Active);
            int additions = 0;
            part.GetDirectlyHeldThings().OnContentsChanged += () => { additions++; throw new InvalidOperationException("Injected after native reward acquisition"); };
            typeof(QuestPart_ClassTomeRaid).GetMethod("EarnReward", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(part, null);
            Check.Equal(true, part.RewardPending, "victory commits despite post-add callback failure");
            Check.Equal(1, part.GetDirectlyHeldThings().Count, "native owner already owns concrete book");
            Thing book = part.GetDirectlyHeldThings()[0];
            part.QuestPartTick(); part.QuestPartTick(); // original map is absent
            Check.Equal(true, part.RewardPending, "subsequent map loss cannot revoke earned victory");
            Check.Same(book, part.GetDirectlyHeldThings()[0], "callback exception never regenerates book");
            Check.Equal(1, additions, "single physical acquisition");
            RuntimeLog.Clear(); // Expected injected native NotifyAdded exception.
        });
        yield return new("ClassTome.RaidCleanupIsTerminalAndNeverPays", f => {
            using var game = new GameScope();
            var part = new QuestPart_ClassTomeRaid();
            part.Cleanup(); part.Cleanup(); part.Notify_QuestSignalReceived(new Signal("accept"));
            Check.Equal(false, part.IsOutstanding, "cancelled offer releases request/map lock");
            Check.Equal(0, part.GetDirectlyHeldThings().Count, "cleanup cannot pay");
        });
        yield return new("ClassTome.RaidMissingMapBeforeVictoryFails", f => {
            using var game = new GameScope();
            var part = new QuestPart_ClassTomeRaid();
            Set(part, "phase", QuestPart_ClassTomeRaid.RaidPhase.Active); Set(part, "arrivalCommitted", true); Set(part, "deadlineTick", 120000);
            part.QuestPartTick();
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Failed, part.Phase, "missing original map checked before empty group victory");
            Check.Equal(0, part.GetDirectlyHeldThings().Count, "lost map never pays");
        });
        yield return new("ClassTome.RaidEarnedRewardSurvivesCleanupAndMissingDestination", f => {
            using var game = new GameScope();
            var part = new QuestPart_ClassTomeRaid();
            var book = Book(); part.GetDirectlyHeldThings().TryAdd(book, false);
            Set(part, "phase", QuestPart_ClassTomeRaid.RaidPhase.RewardPending); Set(part, "combatResolved", true);
            part.Cleanup(); part.QuestPartTick();
            Check.Equal(false, part.Claim(null!), "missing chosen destination leaves reward pending");
            Check.Equal(true, part.RewardPending, "earned reward retained");
            Check.Same(book, part.GetDirectlyHeldThings()[0], "same physical book retained");
            Check.Same(part.GetDirectlyHeldThings(), book.holdingOwner, "actual owner retained");
            Check.Equal(false, part.BlocksMap, "earned claim releases map challenge lock");
        });
        yield return new("ClassTome.RaidNativeScribeKeepsRewardIdentityAndLatches", f => {
            using var game = new GameScope();
            var previous = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
            try {
                var part = new QuestPart_ClassTomeRaid(); var book = Book(); DefDatabase<ThingDef>.Add(book.def);
                part.GetDirectlyHeldThings().TryAdd(book, false);
                Set(part, "phase", QuestPart_ClassTomeRaid.RaidPhase.RewardPending); Set(part, "combatResolved", true); Set(part, "arrivalCommitted", true);
                var savedQuest = new Quest { id = 492 }; savedQuest.AddPart(part);
                string path = Path.Combine(f.WorkingDirectory, "pending-tome.xml");
                try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref savedQuest, "quest"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
                savedQuest = null!;
                try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref savedQuest, "quest"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
                part = (QuestPart_ClassTomeRaid)savedQuest.PartsListForReading.Single();
                Check.Equal(true, part.RewardPending, "pending state survives native save");
                Check.Equal(1, part.GetDirectlyHeldThings().Count, "exactly one physical reward");
                Check.Equal(49201, part.GetDirectlyHeldThings()[0].thingIDNumber, "same item ID, never regenerated");
                Check.Same(part.GetDirectlyHeldThings(), part.GetDirectlyHeldThings()[0].holdingOwner, "holder ownership restored");
                Check.Equal(true, (bool)Get(part, "arrivalCommitted"), "arrival latch restored");
                Check.Equal(true, (bool)Get(part, "combatResolved"), "victory latch restored");
                part.Cleanup(); Check.Equal(1, part.GetDirectlyHeldThings().Count, "cleanup still cannot erase saved reward");
            } finally { DefDatabase<ThingDef>.Clear(); foreach (var def in previous) DefDatabase<ThingDef>.Add(def); }
        });
        yield return new("ClassTome.RaidNativeDeathDowningRetreatResolution", f => {
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
            var pawn = new Pawn { def = pawnDef }; pawn.health = new Pawn_HealthTracker(pawn);
            var method = typeof(QuestPart_ClassTomeRaid).GetMethod("ThreatResolved", BindingFlags.Static | BindingFlags.NonPublic)!;
            bool Resolved() => (bool)method.Invoke(null, new object[] { pawn, map })!;
            Set(pawn.health, "healthState", PawnHealthState.Dead); Check.Equal(true, Resolved(), "native death resolves owned attacker");
            Set(pawn.health, "healthState", PawnHealthState.Down); Check.Equal(true, Resolved(), "native downing resolves owned attacker");
            Set(pawn.health, "healthState", PawnHealthState.Mobile); Check.Equal(true, Resolved(), "off-map retreat resolves only after coordinator arrival guard");
        });
    }
    static ThingOwner<Thing>? externalReward;
    static bool SpawnIntercept(Thing newThing)
    {
        externalReward?.TryAdd(newThing, false);
        throw new InvalidOperationException("Injected tome spawn callback failure");
    }
    sealed class SpawnFault : IDisposable
    {
        readonly object harmony;
        readonly string id = "rwom.tome.transfer." + Guid.NewGuid();
        public SpawnFault()
        {
            externalReward = null;
            var assembly = Assembly.Load("0Harmony");
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var method = typeof(GenSpawn).GetMethod("Spawn", new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(WipeMode) })!;
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ClassTomeRaidTests).GetMethod(nameof(SpawnIntercept), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { method, prefix, null, null, null });
        }
        public void Dispose() { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { id }); externalReward = null; }
    }
    static Thing Book()
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "ClassTomeRaidTestBook"; def.thingClass = typeof(Thing); def.category = ThingCategory.Item; def.stackLimit = 1;
        return new Thing { def = def, thingIDNumber = 49201, stackCount = 1 };
    }
    static object Get(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;
    static void Set(object obj, string name, object value)
    {
        for (Type? t = obj.GetType(); t != null; t = t.BaseType)
        { var field = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); if (field != null) { field.SetValue(obj, value); return; } }
        throw new MissingFieldException(name);
    }
    sealed class GameScope : IDisposable
    {
        readonly Game previous = Current.Game;
        public GameScope()
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map>());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
        }
        public void Dispose() => Current.Game = previous;
    }
}






