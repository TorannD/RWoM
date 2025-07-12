using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_ShadowSlayerCloak : Verb_UseAbility
    {
        private int verVal = 0;
        private int pwrVal = 0;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_ShadowSlayer, "TM_ShadowSlayer", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_ShadowSlayer, "TM_ShadowSlayer", "_pwr", true);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_ShadowSlayer);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_ShadowSlayer);

                float sev = 40 + (comp.mightPwr * (3 * verVal));
                HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_ShadowSlayerCloakHD, sev);

                HediffComp_Disappears hdComp = caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowSlayerCloakHD).TryGetComp<HediffComp_Disappears>();
                if (hdComp != null)
                {
                    hdComp.ticksToDisappear = Mathf.RoundToInt(60 * sev);
                }

                for (int i = 0; i < 3; i++)
                {
                    Vector3 rndPos = caster.DrawPos;
                    rndPos.x += Rand.Range(-.5f, .5f);
                    rndPos.z += Rand.Range(-.5f, .5f);
                    FleckMaker.ThrowSmoke(rndPos, caster.Map, Rand.Range(.6f, 1.2f));
                }
            }
            return true;
        }
    }
}
