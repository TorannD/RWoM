using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_VeilOfShadows : Verb_UseAbility
    {
        private int verVal = 0;
        private int pwrVal = 0;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;

            bool flag = caster != null && !caster.Dead;
            if (flag)
            {
                CompAbilityUserMight comp = caster.TryGetComp<CompAbilityUserMight>();
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_VeilOfShadows, "TM_VeilOfShadows", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_VeilOfShadows, "TM_VeilOfShadows", "_pwr", true);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);

                HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_ShadowCloakHD, .2f + (comp.mightPwr * verVal));

                HediffComp_Disappears hdComp = caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowCloakHD).TryGetComp<HediffComp_Disappears>();
                if(hdComp != null)
                {
                    hdComp.ticksToDisappear = 600 + (60 * pwrVal);
                }

                ThingDef fog = TorannMagicDefOf.Fog_Shadows;
                fog.gas.expireSeconds.min = 10 + pwrVal;
                fog.gas.expireSeconds.max = 11  + pwrVal;
                GenExplosion.DoExplosion(caster.Position, caster.Map, 3f + (.3f * verVal), TMDamageDefOf.DamageDefOf.TM_Toxin, caster, 0, 0, TMDamageDefOf.DamageDefOf.TM_Toxin.soundExplosion, null, null, null, fog, 1f, 1, false, null, 0f, 0, 0.0f, false);

                for (int i = 0; i < 6; i++)
                {
                    Vector3 rndPos = caster.DrawPos;
                    rndPos.x += Rand.Range(-1.5f, 1.5f);
                    rndPos.z += Rand.Range(-1.5f, 1.5f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ShadowCloud, rndPos, caster.Map, Rand.Range(1f, 1.8f), .6f, .05f, Rand.Range(.6f, .8f), Rand.Range(-40, 40), Rand.Range(2, 3f), Rand.Range(0, 360), Rand.Range(0, 360));
                }
            }
            return true;
        }
    }
}
