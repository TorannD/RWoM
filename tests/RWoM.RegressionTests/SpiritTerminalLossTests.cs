using System.Reflection;
using System.Runtime.ExceptionServices;
using RimWorld.Planet;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritTerminalLossTests
{
    static string Discard(Pawn pawn, ThingOwner owner)
    {
        var type = Check.NotNull(typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthTerminalLoss"), "internal terminal helper exists");
        try { return type.GetMethod("TryDiscard", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [pawn, owner])!.ToString()!; }
        catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException!).Throw(); throw; }
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritTerminalLoss.NativeDiscardRelationsScribeAndReplay", r => SpiritCaravanLossNativeTests.Run(r, (c, stone) =>
        {
            var owner = stone.GetDirectlyHeldThings(); int nestedCalls = 0;
            using var observation = new DestroyBoundary(pawn =>
            {
                if (!ReferenceEquals(pawn, c.Original)) return;
                nestedCalls++;
                Check.Equal("Rejected", Discard(c.Original, owner), "native in-flight discard rejects nested helper call");
            }, false);
            Check.Equal("Completed", Discard(c.Original, owner), "helper performs actual native destroy/discard");
            Check.Equal(1, nestedCalls, "one native Pawn.Destroy");
            Check.Equal("Rejected", Discard(c.Original, owner), "completed loss cannot replay");
            Check.Equal(1, nestedCalls, "replay never enters native lifecycle again");
            // Shared probe now checks real reciprocal relations, no PassToWorld,
            // native empty-shell destruction and actual undead-reference Scribe.
        }));
        yield return new("SpiritTerminalLoss.RejectsForeignUnmarkedAndOwnerless", r => SpiritCaravanLossNativeTests.Run(r, (c, stone) =>
        {
            var owner = stone.GetDirectlyHeldThings(); var state = ClassIdentity.Component(c.Original).SpiritRebirth;
            var foreign = new Thing_SoulStone();
            Check.Equal("Rejected", Discard(c.Original, foreign.GetDirectlyHeldThings()), "foreign actual native owner rejected");
            Set(ClassIdentity.Component(c.Original), "spiritRebirth", null);
            try { Check.Equal("Rejected", Discard(c.Original, owner), "unmarked pawn never enters loss helper"); }
            finally { Set(ClassIdentity.Component(c.Original), "spiritRebirth", state); }
            owner.Remove(c.Original);
            try { Check.Equal("Rejected", Discard(c.Original, owner), "saved expected owner cannot authorize detached pawn"); }
            finally { Check.Equal(true, owner.TryAdd(c.Original, false), "restore exact native fixture owner"); }
            Check.Equal(false, c.Original.Destroyed || c.Original.Discarded, "all rejected calls leave original live");
            Check.Same(owner, c.Original.holdingOwner, "same actual stone custody remains");
            RuntimeLog.AssertClean();
        }, false));
        yield return new("SpiritMapLoss.RetryAfterNativeDestroy", r => SpiritCaravanLossNativeTests.Run(r, (c, stone) =>
        {
            var method = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthTerminalLoss")!
                .GetMethod("TryDiscardOnTerminalLoss", BindingFlags.Static | BindingFlags.NonPublic)!;
            using var observation = new DestroyBoundary(pawn => { if (pawn == c.Original) throw new InvalidOperationException("map retry probe"); }, true);
            Check.Equal("Completed", method.Invoke(null, [c.Original, stone.GetDirectlyHeldThings()])!.ToString(), "authorized native continuation finished");
            var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Check.Equal(1, problems.Count, "one diagnostic for the injected failure");
            Check.Equal(true, problems[0].Contains("map retry probe"), "original diagnostic retained");
            RuntimeLog.Clear();
        }));
        yield return new("SpiritTerminalLoss.NativePartialFailureClearsGuardAndPropagates", r => SpiritCaravanLossNativeTests.Run(r, (c, stone) =>
        {
            var owner = stone.GetDirectlyHeldThings(); var injected = new InvalidOperationException("injected terminal post-destroy failure"); int calls = 0;
            using var observation = new DestroyBoundary(pawn => { if (ReferenceEquals(pawn, c.Original)) { calls++; throw injected; } }, true);
            Exception? caught = null;
            try { Discard(c.Original, owner); } catch (Exception error) { caught = error; }
            Check.Same(injected, caught, "original native callback exception is not masked");
            Check.Equal(true, c.Original.Destroyed, "native destruction already occurred before callback failure");
            Check.Equal(false, c.Original.Discarded, "partial failure is not reported as completed discard");
            Check.Equal(false, Find.WorldPawns.IsBeingDiscarded(c.Original), "native finally clears discard guard");
            Check.Equal(false, Find.WorldPawns.Contains(c.Original), "no world-pawn fallback");
            Check.Equal(0, owner.Count, "native destruction detached actual original");
            Check.Equal("Rejected", Discard(c.Original, owner), "live-only helper cannot fabricate authorization for detached partial retry");
            Check.Equal(1, calls, "no second irreversible call after partial failure");
            var messages = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Check.Equal(1, messages.Count, "one explicit terminal failure diagnostic");
            Check.Equal(true, messages[0].Contains("Destroyed=True") && messages[0].Contains("Discarded=False")
                && messages[0].Contains("currentOwner=none") && messages[0].Contains(injected.Message), "diagnostic reports actual partial-loss state");
            RuntimeLog.Clear();
        }, false));
    }
    static Action<Pawn>? callback;
    public static void NativeDestroy(Pawn __instance) => callback?.Invoke(__instance);
    sealed class DestroyBoundary : IDisposable
    {
        readonly object harmony; readonly string id = "rwom.terminal-loss." + Guid.NewGuid();
        public DestroyBoundary(Action<Pawn> action, bool postfix)
        {
            callback = action;
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritTerminalLossTests).GetMethod(nameof(NativeDestroy))!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn).GetMethod(nameof(Pawn.Destroy))!, postfix ? null : hook, postfix ? hook : null, null, null]);
        }
        public void Dispose() { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]); callback = null; }
    }
}
