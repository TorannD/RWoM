using System;
using Verse;
using RimWorld;
using System.Linq;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_EnchantedAura : ThoughtWorker
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
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Undead) || pawn.story.traits.HasTrait(TorannMagicDefOf.Lich) || pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                {
                    return false;
                }
                if (other.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedAuraHD, false))
                {
                    CompAbilityUserMagic comp = other.GetCompAbilityUserMagic();

                    if (comp != null)
                    {
                        return ThoughtState.ActiveAtStage(comp.MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchantedBody_pwr").level);                        
                    }
                }
            }
            return false;
        }
    }
}
