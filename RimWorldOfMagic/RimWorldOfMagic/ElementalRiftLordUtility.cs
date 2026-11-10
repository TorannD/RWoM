using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace TorannMagic
{
    internal static class ElementalRiftLordUtility
    {
        internal static Lord FindExisting(Pawn incoming, IEnumerable<Pawn> factionPawns)
        {
            // Other surviving pawns need not belong to a Lord (e.g. after their
            // original group has ended). No match means the caller creates one.
            Pawn nearest = (Pawn)GenClosest.ClosestThing_Global(incoming.Position, factionPawns,
                99999f, thing => thing != incoming && ((Pawn)thing).GetLord() != null, null);
            return nearest?.GetLord();
        }
    }
}
