using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Synchronous conversion snapshot only. Ordinary corpses have already lost
    // this tracker at death; do not fabricate pre-death priorities for them.
    internal sealed class SpiritRebirthWorkSettings
    {
        private readonly Pawn original;
        private readonly Dictionary<WorkTypeDef, int> priorities;
        private bool restored;

        private SpiritRebirthWorkSettings(Pawn pawn)
        {
            original = pawn;
            // GetPriority flattens enabled priorities to 3 when manual priorities
            // are hidden. Preserve the stored values without toggling global UI settings.
            var values = typeof(Pawn_WorkSettings).GetField("priorities", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(pawn.workSettings) as DefMap<WorkTypeDef, int>;
            if (values == null) throw new System.InvalidOperationException("Cannot snapshot retained Spirit work priorities.");
            priorities = DefDatabase<WorkTypeDef>.AllDefsListForReading
                .ToDictionary(def => def, def => values[def]);
        }

        internal static SpiritRebirthWorkSettings Capture(Pawn pawn)
            => pawn?.workSettings?.Initialized == true ? new SpiritRebirthWorkSettings(pawn) : null;

        internal bool Restore()
        {
            if (restored || original == null || original.Destroyed || original.Dead
                || original.workSettings?.Initialized != true) return false;
            restored = true;
            foreach (var pair in priorities)
                if (ReferenceEquals(DefDatabase<WorkTypeDef>.GetNamedSilentFail(pair.Key.defName), pair.Key))
                    original.workSettings.SetPriority(pair.Key, original.WorkTypeIsDisabled(pair.Key) ? 0 : pair.Value);
            return true;
        }
    }
}
