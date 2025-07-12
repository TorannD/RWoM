using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_Lullaby : Verb_UseAbility
    {

        private int verVal;
        private int pwrVal;
        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            bool flag = false;
            this.TargetsAoE.Clear();
            this.UpdateTargets();
            //MagicPowerSkill pwr = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Lullaby.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Lullaby_pwr");
            //MagicPowerSkill ver = base.CasterPawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Lullaby.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Lullaby_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            //if (base.CasterPawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = base.CasterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = base.CasterPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}
            verVal = TM_Calc.GetSkillVersatilityLevel(CasterPawn, this.Ability.Def as TMAbilityDef);
            pwrVal = TM_Calc.GetSkillPowerLevel(CasterPawn, this.Ability.Def as TMAbilityDef);
            bool flag2 = this.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && this.TargetsAoE.Count > 1;
            if (flag2)
            {
                this.TargetsAoE.RemoveRange(0, this.TargetsAoE.Count - 1);
            }
            for (int i = 0; i < this.TargetsAoE.Count; i++)
            {
                Pawn newPawn = this.TargetsAoE[i].Thing as Pawn;
                if(newPawn != null && newPawn.RaceProps.IsFlesh)
                {
                    if (Rand.Chance(.4f + (.1f * pwrVal) * TM_Calc.GetSpellSuccessChance(this.CasterPawn, newPawn, true)))
                    {
                        if(newPawn.InMentalState)
                        {
                            newPawn.mindState.mentalStateHandler.Reset();
                        }
                        //
                        //newPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        newPawn.needs.rest.CurLevel = 0f;
                        Job job = JobMaker.MakeJob(JobDefOf.LayDown, newPawn.Position);
                        job.forceSleep = true;
                        newPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        TM_MoteMaker.ThrowNoteMote(newPawn.DrawPos, newPawn.Map, Rand.Range(.3f, .8f));
                        TM_MoteMaker.ThrowNoteMote(newPawn.DrawPos, newPawn.Map, Rand.Range(.3f, .8f));
                        TM_MoteMaker.ThrowNoteMote(newPawn.DrawPos, newPawn.Map, Rand.Range(.3f, .8f));
                    }
                    else
                    {
                        MoteMaker.ThrowText(newPawn.DrawPos, newPawn.Map, "TM_ResistedSpell".Translate(), -1);
                    }
                    HealthUtility.AdjustSeverity(newPawn, HediffDef.Named("TM_LullabyHD"), .95f + verVal);
                }
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }
        
    }
}
