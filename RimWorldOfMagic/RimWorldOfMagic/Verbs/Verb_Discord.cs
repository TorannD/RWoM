using Verse;
using AbilityUser;
using System;
using Verse.AI;

namespace TorannMagic
{
    public class Verb_Discord : Verb_UseAbility
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
            Pawn pawn = base.CasterPawn;

            if (currentTarget != null && currentTarget.Pawn != null)
            {
                Pawn p = currentTarget.Pawn;
                if (p.RaceProps.Humanlike)
                {
                    Job job = new Job(TorannMagicDefOf.JobDriver_Discord, p);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }
            this.Ability.PostAbilityAttempt();


            this.burstShotsLeft = 0;
            return false;
        }
    }
}
