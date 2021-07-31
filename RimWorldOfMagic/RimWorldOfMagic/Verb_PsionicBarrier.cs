using System;
using Verse;
using Verse.AI;
using AbilityUser;



namespace TorannMagic
{
    public class Verb_PsionicBarrier : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            if(this.verbProps.targetParams.canTargetLocations)
            {
                Job job = new Job(TorannMagicDefOf.JobDriver_PsionicBarrier, this.currentTarget);
                this.CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
            else
            {
                Job job = new Job(TorannMagicDefOf.JobDriver_PsionicBarrier, this.CasterPawn.Position);
                this.CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }                       
            this.Ability.PostAbilityAttempt();

            this.burstShotsLeft = 0;
            return false;
        }
    }
}
