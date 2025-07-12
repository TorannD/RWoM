using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_SpiritDrain : Verb_SB
    {       

        protected override bool TryCastShot()
        {
            bool flag = false;

            Map map = base.CasterPawn.Map;

            Pawn hitPawn = (Pawn)this.currentTarget;
            Pawn caster = base.CasterPawn;

            if (hitPawn != null & !hitPawn.Dead && hitPawn != caster && !TM_Calc.IsUndead(hitPawn) && !TM_Calc.IsRobotPawn(hitPawn) && !TM_Calc.IsGolem(hitPawn))
            {
                CompAbilityUserMagic compCaster = caster.GetCompAbilityUserMagic();              

                Job job = new Job(TorannMagicDefOf.JobDriver_SpiritDrain, hitPawn);
                caster.jobs.EndCurrentJob(JobCondition.InterruptForced);
                caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                this.burstShotsLeft = 0;
                return false;
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(
                    this.CasterPawn.LabelShort,
                    this.Ability.Def.label
                ), MessageTypeDefOf.RejectInput);
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
