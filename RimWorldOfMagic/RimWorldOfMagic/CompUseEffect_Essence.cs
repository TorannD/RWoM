using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using TorannMagic.Enchantment;
using UnityEngine;

namespace TorannMagic
{
    public class CompUseEffect_Essence : CompUseEffect
    {

        public override float OrderPriority => -800f;
        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMagic compMagic = user.GetCompAbilityUserMagic();
            CompAbilityUserMight compMight = user.GetCompAbilityUserMight();

            if(this.parent.def == TorannMagicDefOf.TM_MagicArtifact_MightEssence && compMight != null && compMight.IsMightUser)
            {
                CompEnchantedItem compItem = this.parent.TryGetComp<CompEnchantedItem>();
                if (compItem != null && compItem.HasEnchantment)
                {
                    if(compItem.mightEssence != 0)
                    {
                        compMight.MightUserXP += compItem.mightEssence;
                    }
                    else
                    {
                        Log.Message("might essence granted 0 experience");
                    }
                    FleckMaker.ThrowSmoke(this.parent.DrawPos, this.parent.Map, Rand.Range(.5f, .8f));
                    FleckMaker.ThrowHeatGlow(this.parent.Position, this.parent.Map, .8f);
                    TM_Action.TransmutateEffects(user.Position, user);
                    TargetInfo ti = new TargetInfo(this.parent.Position, this.parent.Map, false);
                    TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, user.Map, Vector3.zero, .1f, 0f, .05f, .4f, .2f, 1f);
                    parent.SplitOff(1).Destroy();
                }
            }
            else if (this.parent.def == TorannMagicDefOf.TM_MagicArtifact_MagicEssence && compMagic != null && compMagic.IsMagicUser)
            {
                CompEnchantedItem compItem = this.parent.TryGetComp<CompEnchantedItem>();
                if (compItem != null && compItem.HasEnchantment)
                {
                    if (compItem.magicEssence != 0)
                    {
                        compMagic.MagicUserXP += compItem.magicEssence;
                    }
                    else
                    {
                        Log.Message("magic essence granted 0 experience");
                    }
                    FleckMaker.ThrowSmoke(this.parent.DrawPos, this.parent.Map, Rand.Range(.5f, .8f));
                    FleckMaker.ThrowHeatGlow(this.parent.Position, this.parent.Map, .8f);
                    TM_Action.TransmutateEffects(user.Position, user);
                    TargetInfo ti = new TargetInfo(this.parent.Position, this.parent.Map, false);
                    TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, user.Map, Vector3.zero, .1f, 0f, .05f, .4f, .2f, 1f);
                    parent.SplitOff(1).Destroy();
                }
            }
            else
            {
                Messages.Message("TM_InvalidAction".Translate(user.LabelShort, this.parent.def.label), MessageTypeDefOf.RejectInput);
            }

        }
    }
}
