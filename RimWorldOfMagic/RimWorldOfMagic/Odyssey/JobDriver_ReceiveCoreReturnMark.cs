using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class JobDriver_ReceiveCoreReturnMark : JobDriver
    {
        private CompCoreReturn Core => TargetA.Thing?.TryGetComp<CompCoreReturn>();
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Core?.CanMark(pawn) == true && pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Core?.CanMark(pawn) != true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = () =>
                {
                    if (Core?.CanMark(pawn, true) != true) { EndJobWith(JobCondition.Incompletable); return; }
                    var mark = CompCoreReturn.FindMark(pawn);
                    if (mark != null)
                    {
                        // The consent captured by target B must still describe the current binding.
                        if (mark.Core == Core.parent) return;
                        if (mark.Core != job.targetB.Thing) { EndJobWith(JobCondition.Incompletable); return; }
                        mark.Comp?.Unregister(pawn);
                        mark.Core = Core.parent;
                        Core.Register(pawn);
                        return;
                    }
                    if (job.targetB.HasThing || !Core.Bind(pawn)) EndJobWith(JobCondition.Incompletable);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
