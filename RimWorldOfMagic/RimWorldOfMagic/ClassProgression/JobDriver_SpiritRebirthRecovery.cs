using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class JobDriver_SpiritRebirthRecovery : JobDriver
    {
        private bool releaseAttempted;
        private Pawn Original => job.targetB.Thing as Pawn;
        private SpiritRebirthRitualTransaction Receipt => ClassIdentity.Component(Original)?.SpiritRitual;
        private bool Valid => Receipt != null && Receipt.CanOrderPaidRecovery(pawn, Original, job.targetA.Thing);
        private bool Reserved(LocalTargetInfo target, int count)
            => pawn.Map.reservationManager.ReservationsReadOnly.Any(r => r.Claimant == pawn && r.Job == job
                && r.Target == target && r.Layer == null && (r.StackCount == -1 || r.StackCount >= count));
        private bool OwnsCurrentJob => pawn.CurJob == job && ReferenceEquals(pawn.jobs.curDriver, this);
        private bool ExecutionValid => OwnsCurrentJob && Valid && Reserved(job.targetA, 1)
            && Reserved(job.targetB, 1) && Reserved(job.targetA.Thing.InteractionCell, -1) && Reserved(job.targetC, -1);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!Valid || !Receipt.RecoveryDestinationValid(pawn, job.targetC.Cell)
                || !pawn.CanReach(job.targetA, PathEndMode.InteractionCell, Danger.Deadly)
                || (Original.Spawned && !pawn.CanReach(Original, PathEndMode.Touch, Danger.Deadly))) return false;
            var targets = new[] { job.targetA, job.targetB, new LocalTargetInfo(job.targetA.Thing.InteractionCell), job.targetC };
            int[] counts = { 1, 1, -1, -1 };
            var manager = pawn.Map.reservationManager;
            for (int i = 0; i < targets.Length; i++) if (!manager.CanReserve(pawn, targets[i], 1, counts[i], null, false)) return false;
            bool complete = false;
            try
            {
                for (int i = 0; i < targets.Length; i++) if (!manager.Reserve(pawn, job, targets[i], 1, counts[i], null, false, false)) return false;
                return complete = true;
            }
            finally { if (!complete) manager.ReleaseClaimedBy(pawn, job); }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(condition => Receipt?.TryRetainReleaseForCleanup(pawn));
            var acquire = ToilMaker.MakeToil("SpiritRecoveryAcquire");
            yield return Toils_Jump.JumpIf(acquire, () => Original != null && !Original.Spawned);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            acquire.defaultCompleteMode = ToilCompleteMode.Instant;
            acquire.initAction = () => {
                if (!ExecutionValid || (Original.Spawned && !pawn.CanReachImmediate(Original, PathEndMode.Touch))
                    || (!Receipt.RecoveryCarriesOriginal(pawn) && !Receipt.TryRecoverRelease(pawn))) EndJobWith(JobCondition.Incompletable);
            };
            yield return acquire;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            var release = ToilMaker.MakeToil("SpiritRecoveryRelease");
            release.defaultCompleteMode = ToilCompleteMode.Never;
            release.tickAction = () => {
                if (releaseAttempted) { EndJobWith(Receipt?.RecoveryCompleted(pawn) == true ? JobCondition.Succeeded : JobCondition.Incompletable); return; }
                if (!ExecutionValid || pawn.Position != job.targetA.Thing.InteractionCell || !Receipt.RecoveryCarriesOriginal(pawn))
                { EndJobWith(JobCondition.Incompletable); return; }
                releaseAttempted = true;
                var receipt = Receipt;
                bool completed = receipt.TryReleaseWithGuard(pawn, pawn.Map, job.targetC.Cell,
                    () => ExecutionValid && pawn.Position == job.targetA.Thing.InteractionCell) == SpiritReleaseResult.Released && receipt.RecoveryCompleted(pawn);
                if (!OwnsCurrentJob)
                {
                    // Native callback cleanup ran while release was in flight.
                    // Its guard is now clear: retain only this receipt's original,
                    // without ending or changing the replacement job or cargo.
                    receipt.TryRetainReleaseForCleanup(pawn);
                    return;
                }
                EndJobWith(completed ? JobCondition.Succeeded : JobCondition.Incompletable);
            };
            yield return release;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref releaseAttempted, "spiritRecoveryReleaseAttempted");
        }
    }
}
