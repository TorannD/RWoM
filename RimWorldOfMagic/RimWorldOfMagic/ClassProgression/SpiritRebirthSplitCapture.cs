using System;
using HarmonyLib;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Only the synchronous ritual pickup owns a scope. Observe the native base
    // result before ThingWithComps dispatches arbitrary PostSplitOff callbacks.
    internal sealed class SpiritRebirthSplitCapture : IDisposable
    {
        [ThreadStatic] private static SpiritRebirthSplitCapture current;
        private readonly SpiritRebirthSplitCapture prior;
        private readonly Thing source;
        private readonly int count;
        internal Thing Result { get; private set; }

        internal SpiritRebirthSplitCapture(Thing source, int count)
        {
            this.source = source; this.count = count; prior = current; current = this;
            if (count == source.stackCount) Result = source;
        }
        internal static void Observe(Thing source, int count, Thing result)
        {
            var scope = current;
            if (scope != null && scope.Result == null && ReferenceEquals(scope.source, source) && scope.count == count)
                scope.Result = result;
        }
        public void Dispose() { current = prior; }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.SplitOff))]
    internal static class SpiritRebirthNativeSplitCapturePatch
    {
        static void Postfix(Thing __instance, int count, Thing __result)
            => SpiritRebirthSplitCapture.Observe(__instance, count, __result);
    }
}
