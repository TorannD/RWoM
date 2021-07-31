using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_Boundless : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (pawn != null && other != null)
            {
                if (!other.RaceProps.Humanlike || other.Dead)
                {
                    return false;
                }
                if (!RelationsUtility.PawnsKnowEachOther(pawn, other))
                {
                    return false;
                }
                if (pawn.RaceProps.Humanlike && other.RaceProps.Humanlike)
                {
                    if ((TM_Calc.IsMightUser(pawn) && !pawn.story.traits.HasTrait(TorannMagicDefOf.TM_BoundlessTD)) && other.story.traits.HasTrait(TorannMagicDefOf.TM_BoundlessTD))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
