using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_Blur : Verb_UseAbility  
    {
        
        protected override bool TryCastShot()
        {
            bool result = false;

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;

            if (pawn != null && !pawn.Downed)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BlurHD, false))
                {
                    Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_BlurHD);
                    pawn.health.RemoveHediff(hd);                    

                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                    TM_MoteMaker.ThrowSiphonMote(pawn.DrawPos, pawn.Map, 1f);
                }
                else
                {
                    CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                    if (comp != null)
                    {
                        if (comp.maxMP >= TorannMagicDefOf.TM_Blur.upkeepEnergyCost)
                        {
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_BlurHD, 1f);
                            TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                            TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, 1);
                            TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                        }
                        else
                        {
                            Messages.Message("TM_NotEnoughManaToSustain".Translate(
                                            pawn.LabelShort,
                                            TorannMagicDefOf.TM_Blur.label
                                        ), MessageTypeDefOf.RejectInput);
                        }
                    }
                }
            }

            this.burstShotsLeft = 0;
            return result;
        }
    }
}
