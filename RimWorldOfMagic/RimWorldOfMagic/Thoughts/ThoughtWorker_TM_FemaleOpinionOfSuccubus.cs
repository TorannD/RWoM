using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_FemaleOpinionOfSuccubus : ThoughtWorker
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
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead))
                {
                    return false;
                }
                if (other.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || pawn.gender == Gender.Male)
                    {
                        return false;
                    }
                    if (pawn.gender == Gender.Female && !pawn.story.traits.HasTrait(TraitDefOf.Gay))
                    {
                        int num = pawn.story.traits.DegreeOfTrait(TraitDefOf.Beauty);
                        if (num >= 1)
                        {
                            return ThoughtState.ActiveAtStage(1);
                        }
                        else
                        {
                            return ThoughtState.ActiveAtStage(0);
                        }
                    }
                }
            }
            return false;
        }
    }
}
