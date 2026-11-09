using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public class JobDriver_ClassAscension : JobDriver
    {
        private int workTicks;
        private int lastWorkTick = -1;
        private bool completionAttempted;
        private bool completed;
        public int WorkTicks => workTicks;
        public bool Completed => completed;
        private Thing Circle => job.GetTarget(TargetIndex.A).Thing;
        private Thing Offering => job.GetTarget(TargetIndex.B).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workTicks, "classAscensionWorkTicks", 0);
            Scribe_Values.Look(ref lastWorkTick, "classAscensionLastWorkTick", -1);
            Scribe_Values.Look(ref completionAttempted, "classAscensionAttempted", false);
            Scribe_Values.Look(ref completed, "classAscensionCompleted", false);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            job.count = 1;
            if (!ClassAscension.CanBegin(pawn, Circle, Offering)) return false;
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)) return false;
            // A carried offering already has an exclusive owner and no map cell to reserve.
            return pawn.carryTracker?.CarriedThing == Offering
                || pawn.Reserve(job.targetB, job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            this.FailOn(() => !completed && (completionAttempted || !ClassAscension.CanBegin(pawn, Circle, Offering)));
            var goCircle = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(goCircle, () => pawn.carryTracker?.CarriedThing == Offering);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            // Native toil splits job.count=1 and rewrites TargetB to CarriedThing.
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, failIfStackCountLessThanJobCount: true);
            yield return goCircle;
            var ritual = ToilMaker.MakeToil("ClassAscensionWork");
            ritual.defaultCompleteMode = ToilCompleteMode.Never;
            ritual.handlingFacing = true;
            ritual.tickAction = AdvanceRitual;
            ritual.WithProgressBar(TargetIndex.A, () => (float)workTicks / ClassAscension.RitualTicks);
            yield return ritual;
        }

        private void AdvanceRitual()
        {
            if (completed) { EndJobWith(JobCondition.Succeeded); return; }
            if (completionAttempted || !ClassAscension.CanBegin(pawn, Circle, Offering)
                || pawn.Position != Circle.InteractionCell || pawn.carryTracker?.CarriedThing != Offering)
            { EndJobWith(JobCondition.Incompletable); return; }
            pawn.rotationTracker.FaceTarget(Circle);
            // Actual job ticks only; never infer elapsed work from the world clock.
            int now = Find.TickManager.TicksGame;
            if (now <= lastWorkTick) return;
            lastWorkTick = now;
            workTicks++;
            if (workTicks < ClassAscension.RitualTicks) return;
            completionAttempted = true;
            completed = ClassAscension.Complete(pawn, Circle, Offering);
            EndJobWith(completed ? JobCondition.Succeeded : JobCondition.Incompletable);
        }
    }
}
