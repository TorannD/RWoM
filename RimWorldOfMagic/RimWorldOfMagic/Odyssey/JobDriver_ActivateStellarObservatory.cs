using System.Collections.Generic;
using Verse;
using Verse.AI;
namespace TorannMagic.Odyssey
{
    public class JobDriver_ActivateStellarObservatory : JobDriver
    {
        private int workedTicks, lastWorkTick = -1;
        private bool completed;
        private CompStellarObservatory Observatory => (TargetA.Thing as ThingWithComps)?.GetComp<CompStellarObservatory>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workedTicks, "TM_stellarWorked", 0);
            Scribe_Values.Look(ref lastWorkTick, "TM_stellarLastWork", -1);
            Scribe_Values.Look(ref completed, "TM_stellarJobCompleted", false);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Observatory?.CanActivate(pawn, false) == true
            && pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil
            {
                tickAction = () =>
                {
                    if (completed) return;
                    if (Observatory?.CanPerformRitual(pawn) != true || !pawn.Map.reservationManager.ReservedBy(TargetA, pawn, job))
                    { EndJobWith(JobCondition.Incompletable); return; }
                    int now = Find.TickManager.TicksGame;
                    if (now <= lastWorkTick) return;
                    lastWorkTick = now;
                    if (++workedTicks < Observatory.Props.ritualTicks) return;
                    completed = true;
                    EndJobWith(Observatory.CompleteActivation(pawn) ? JobCondition.Succeeded : JobCondition.Incompletable);
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
        }
    }
}
