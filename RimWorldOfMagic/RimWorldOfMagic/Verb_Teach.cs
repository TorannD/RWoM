using System;
using Verse;
using Verse.AI;
using AbilityUser;
using RimWorld;


namespace TorannMagic
{
    public class Verb_Teach : Verb_UseAbility
    {
        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            Map map = base.CasterPawn.Map;
            Pawn mentor = base.CasterPawn;

            if(this.currentTarget.Thing is Pawn student && this.currentTarget.Thing != mentor)
            {
                if (this.Ability.Def == TorannMagicDefOf.TM_TeachMagic)
                {
                    if (TM_Calc.IsMagicUser(mentor) && !TM_Calc.IsCrossClass(mentor, true) && student.story != null)
                    {
                        if (TM_Calc.IsMagicUser(student) && !TM_Calc.IsCrossClass(student, true))
                        {
                            if (mentor.relations.OpinionOf(student) >= 0)
                            {
                                Job job = new Job(TorannMagicDefOf.JobDriver_TM_Teach, student);
                                student.jobs.EndCurrentJob(JobCondition.InterruptForced);
                                mentor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }
                            else
                            {
                                Messages.Message("TM_CanNotTeachMagicDislike".Translate(
                                mentor.LabelShort,
                                student.LabelShort
                            ), MessageTypeDefOf.RejectInput, false);
                            }
                        }
                        else
                        {
                            Messages.Message("TM_CanNotTeachMagic".Translate(
                                mentor.LabelShort,
                                student.LabelShort
                            ), MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    else
                    {
                        Log.Message("undetected might or magic user attempting to teach skill");
                    }
                }
                if (this.Ability.Def == TorannMagicDefOf.TM_TeachMight)
                {
                    if (TM_Calc.IsMightUser(mentor) && !TM_Calc.IsCrossClass(mentor, false) && student.story != null)
                    {
                        if (TM_Calc.IsMightUser(student) && !TM_Calc.IsCrossClass(student, false))
                        {
                            if (mentor.relations.OpinionOf(student) >= 0)
                            {
                                Job job = new Job(TorannMagicDefOf.JobDriver_TM_Teach, student);
                                student.jobs.EndCurrentJob(JobCondition.InterruptForced);
                                mentor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }
                            else
                            {
                                Messages.Message("TM_CanNotTeachCombatDislike".Translate(
                                    mentor.LabelShort,
                                    student.LabelShort
                                ), MessageTypeDefOf.RejectInput, false);
                            }
                        }
                        else
                        {
                            Messages.Message("TM_CanNotTeachCombat".Translate(
                                mentor.LabelShort,
                                student.LabelShort
                            ), MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    else
                    {
                        Log.Message("undetected might or magic user attempting to teach skill");
                    }
                }                               
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(
                        mentor.LabelShort,
                        this.Ability.Def.label
                    ), MessageTypeDefOf.RejectInput, false);
            }

            this.burstShotsLeft = 0;
            return false;
        }
    }
}
