using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class CompUseEffect_GemOfInsight : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMight compMight = user.GetCompAbilityUserMight();
            CompAbilityUserMagic compMagic = user.GetCompAbilityUserMagic();

            if(!(compMagic.IsMagicUser || compMight.IsMightUser || user.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted) || user.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy) || user.story.traits.HasTrait(TorannMagicDefOf.Undead)))
            {
                
                if (parent.def != null && parent.def.defName == "GemstoneOfInsight_Magic")
                {
                    if (user.story.traits.allTraits.Count > 7)
                    {
                        int rnd = Rand.RangeInclusive(0, 6);
                        RemoveTrait(rnd, user.story.traits.allTraits);
                    }
                    user.story.traits.GainTrait(new Trait(TorannMagicDefOf.TM_Gifted, 0, false));
                    this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                }
                else if(parent.def != null && parent.def.defName == "GemstoneOfInsight_Might")
                {
                    if (user.story.traits.allTraits.Count > 7)
                    {
                        int rnd = Rand.RangeInclusive(0, 6);
                        RemoveTrait(rnd, user.story.traits.allTraits);
                    }
                    user.story.traits.GainTrait(new Trait(TorannMagicDefOf.PhysicalProdigy, 0, false));
                    this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                }
                else
                {
                    Log.Message("TM_ItemUseFailed".Translate(
                        "Unrecognized Gemstone of Insight"
                    ));
                }                
            }
            else
            {
                Messages.Message("TM_CannotUseGemOfInsight".Translate(
                    user.LabelShort
                    ), MessageTypeDefOf.RejectInput);
            }
        }

        private void RemoveTrait(int index, List<Trait> traits)
        {
            traits.Remove(traits[index]);            
        }
    }
}
