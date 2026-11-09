using Verse;

namespace TorannMagic.Compatibility
{
    /// <summary>Same-map relocation without despawning, changing selection or moving the camera.</summary>
    public static class TM_Teleport
    {
        public static bool CanMove(Pawn pawn, IntVec3 destination)
        {
            Map map = pawn?.Map;
            return pawn != null && pawn.Spawned && !pawn.Destroyed && !pawn.Dead && map != null
                && destination.IsValid && destination != pawn.Position && destination.InBounds(map)
                && destination.Standable(map);
        }

        internal static bool Swap(Pawn first, Pawn second)
        {
            if (first == null || second == null || first.Map != second.Map ||
                !CanMove(first,second.Position) || !CanMove(second,first.Position)) return false;
            var origin=first.Position;
            first.Position=second.Position;
            second.Position=origin;
            first.Notify_Teleported(endCurrentJob:false);
            second.Notify_Teleported(endCurrentJob:false);
            return true;
        }

        public static bool Move(Pawn pawn, IntVec3 destination)
        {
            // Repeat destination validation at completion, not just during target selection.
            if (!CanMove(pawn, destination)) return false;
            pawn.Position = destination;
            pawn.Notify_Teleported(endCurrentJob: false);
            return true;
        }
    }
}
