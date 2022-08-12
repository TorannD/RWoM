using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_MaleOpinionOfSuccubus : ThoughtWorker
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
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || (pawn.gender == Gender.Female && !pawn.story.traits.HasTrait(TraitDefOf.Gay)))
                    {
                        return false;
                    }
                    else
                    {
                        CompAbilityUserMagic magicComp = pawn.GetCompAbilityUserMagic();
                        CompAbilityUserMight mightComp = pawn.GetCompAbilityUserMight();
                        if (mightComp.IsMightUser)
                        {
                            return ThoughtState.ActiveAtStage(0);
                        }
                        else if(magicComp.IsMagicUser)
                        {
                            return ThoughtState.ActiveAtStage(1);
                        }
                        else
                        {
                            return ThoughtState.ActiveAtStage(2);
                        }
                    }
                }
            }
            return false;
        }
    }
}
