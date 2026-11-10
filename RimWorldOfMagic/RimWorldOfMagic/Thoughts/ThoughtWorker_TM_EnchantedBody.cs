using System;
using Verse;
using RimWorld;
using System.Linq;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_EnchantedBody : ThoughtWorker
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
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead) || ClassProgression.ClassIdentity.IsLich(pawn) || pawn.story.traits.HasTrait(TraitDefOf.Transhumanist) || ClassProgression.ClassIdentity.IsEnchanter(pawn))
                {
                    return false;
                }
                if (other.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedBodyHD, false))
                {
                    CompAbilityUserMagic comp = HediffComp_EnchanterAspect.Owner(other.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnchantedBodyHD))?.GetCompAbilityUserMagic();

                    if (comp != null)
                    {
                        if (pawn.story.traits.HasTrait(TraitDefOf.BodyPurist))
                        {
                            return ThoughtState.ActiveAtStage(6);
                        }
                        else if(pawn.story.traits.HasTrait(TraitDefOf.Transhumanist))
                        {
                            return ThoughtState.ActiveAtStage(5);
                        }
                        else
                        {
                            return ThoughtState.ActiveAtStage(comp.MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchantedBody_pwr").level);
                        }
                    }
                }
            }
            return false;
        }
    }
}
