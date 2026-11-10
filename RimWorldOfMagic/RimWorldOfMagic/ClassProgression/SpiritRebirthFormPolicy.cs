using Verse;

namespace TorannMagic.ClassProgression
{
    // Opt-in identity predicate only. No legacy race/trait or host grants this form.
    internal static class SpiritRebirthFormPolicy
    {
        internal static bool IsEthereal(Pawn pawn)
            => pawn != null && !pawn.Dead && HasFreeCustody(pawn)
                && SpiritRebirthState.TryGet(pawn, out var state)
                && state.Form == SpiritRebirthForm.Ethereal && state.Host == null && state.Stone == null;

        internal static bool IsSpawnedEthereal(Pawn pawn)
            => IsEthereal(pawn) && pawn.Spawned;

        private static bool HasFreeCustody(Pawn pawn)
        {
            if (!pawn.Spawned) return pawn.holdingOwner == null;
            return HasMapCustody(pawn);
        }

        internal static bool HasMapCustody(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned) return false;
            var map = pawn.Map;
            return map?.spawnedThings != null && ReferenceEquals(pawn.holdingOwner, map.spawnedThings)
                && ReferenceEquals(map.spawnedThings.Owner, map) && map.spawnedThings.Contains(pawn);
        }
    }
}
