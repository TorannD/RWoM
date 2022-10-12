using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Sprint : Verb_UseAbility
    {

        private int pwrVal = 0;

        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            //MightPowerSkill pwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Sprint.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Sprint_pwr");
            //pwrVal = pwr.level;
            //if(pwrVal == 0)
            //{
            //    pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level;
            //}
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            if (pawn == null || pawn.Dead) return true;

            bool hadSprint = false;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def == TorannMagicDefOf.TM_HediffSprint)
                {
                    pawn.health.RemoveHediff(hediff);
                    hadSprint = true;
                }
            }

            if (!hadSprint)
            {
                HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffSprint, .5f + pwrVal);
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
            }
            return true;
        }
    }
}
