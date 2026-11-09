using RimWorld;
using Verse;

namespace TorannMagic
{
    internal static class BloodGiftSafety
    {
        internal static bool CanCut(Pawn pawn, BodyPartRecord part, float damage)
        {
            if (part == null || part.def == null || part.depth != BodyPartDepth.Outside || part.coverageAbs <= 0f || part.def.bleedRate <= 0f)
                return false;
            // Sacrificial cuts are not eye/head mutilation. Exclude the entire head,
            // missing/prosthetic parts, and parts too injured to survive both cuts.
            for (var ancestor = part; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.def == BodyPartDefOf.Head || ancestor.def.tags.Contains(BodyPartTagDefOf.SightSource)
                    || pawn.health.hediffSet.PartIsMissing(ancestor) || pawn.health.hediffSet.HasDirectlyAddedPartFor(ancestor)) return false;
            return pawn.health.hediffSet.GetPartHealth(part) > damage + 1f;
        }
    }
}
