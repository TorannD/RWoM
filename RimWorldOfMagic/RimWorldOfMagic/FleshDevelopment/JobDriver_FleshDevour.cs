using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.FleshDevelopment
{
    public sealed class JobDriver_FleshDevour : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !FleshActions.CanActivate(pawn, FleshSkill.Devour) || !FleshActions.ValidTarget(pawn, job.targetA.Thing));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(120, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            var eat = ToilMaker.MakeToil("FleshDevour");
            eat.initAction = () => { if (!FleshActions.CompleteDevour(pawn, job.targetA.Thing)) EndJobWith(JobCondition.Incompletable); };
            eat.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return eat;
        }
    }
}
