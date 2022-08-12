using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    class Verb_Disguise : Verb_UseAbility  
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            MightPowerSkill pwr = comp.MightData.MightPowerSkill_Disguise.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Disguise_pwr");
            MightPowerSkill ver = comp.MightData.MightPowerSkill_Disguise.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Disguise_ver");

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                float sev = Mathf.RoundToInt(30 + (8f * ver.level) * comp.mightPwr);
                if (pwr.level == 3)
                {
                    HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_DisguiseHD_III"),sev);
                }
                else if(pwr.level == 2)
                {
                    HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_DisguiseHD_II"), sev);
                }
                else if(pwr.level == 1)
                {
                    HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_DisguiseHD_I"), sev);
                }
                else
                {
                    HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_DisguiseHD"), sev);                    
                }
                for(int i =0; i < 4; i++)
                {
                    FleckMaker.ThrowDustPuff(caster.Position, caster.Map, Rand.Range(.6f, 1f));
                }
            }
            return true;
        }
    }
}
