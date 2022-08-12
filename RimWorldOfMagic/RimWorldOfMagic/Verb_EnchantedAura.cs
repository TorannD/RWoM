using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_EnchantedAura : Verb_UseAbility  
    {

        protected override bool TryCastShot()
        {
            bool result = false;
            bool arg_40_0;

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchantedBody.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchantedBody_pwr");            

            if (pawn != null && !pawn.Downed)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedBodyHD))
                {
                    comp.MagicData.MagicPowersE.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedBody).AutoCast = false;
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnchantedBodyHD, false));
                    FleckMaker.ThrowHeatGlow(pawn.Position, pawn.Map, 1f);
                }

                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EnchantedAuraHD))
                {
                    comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura).AutoCast = false;
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnchantedAuraHD, false));
                    FleckMaker.ThrowHeatGlow(pawn.Position, pawn.Map, 1f);
                }
                else
                {
                    if (comp.maxMP >= TorannMagicDefOf.TM_EnchantedAura.upkeepEnergyCost)
                    {
                        comp.MagicData.MagicPowersStandalone.FirstOrDefault<MagicPower>((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EnchantedAura).AutoCast = true;
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_EnchantedAuraHD, .5f + pwr.level);
                        for (int i = 0; i < 3; i++)
                        {
                            FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, Rand.Range(.6f, .8f));
                        }
                        FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                    }
                    else
                    {
                        Messages.Message("TM_NotEnoughManaToSustain".Translate(
                                            pawn.LabelShort,
                                            TorannMagicDefOf.TM_EnchantedAura.label
                                        ), MessageTypeDefOf.RejectInput);
                    }
                }
                arg_40_0 = true;
            }
            else
            {
                arg_40_0 = false;
            }
            bool flag = arg_40_0;
            if (flag)
            {
                
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;

            return result;
        }
    }
}
