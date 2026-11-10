using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class JobDriver_ChargeMagofuelReservoir : JobDriver
    {
        private int nextChargeTick = -1;
        private CompMagofuelReservoir Reservoir => (TargetA.Thing as ThingWithComps)?.GetComp<CompMagofuelReservoir>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextChargeTick, "TM_nextChargeTick", -1);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            Reservoir?.CanChargeFromMage(pawn, false) == true && pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil
            {
                initAction = () => { if (nextChargeTick < 0) nextChargeTick = Find.TickManager.TicksGame + 60; },
                tickAction = () =>
                {
                    if (Reservoir?.CanChargeFromMage(pawn) != true)
                    {
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                    int now = Find.TickManager.TicksGame;
                    if (now < nextChargeTick) return;
                    // Persist the deadline before payment. Reload never catches up missed pulses.
                    nextChargeTick = now + 60;
                    if (Reservoir?.ChargeFromMage(pawn) != 1f)
                    {
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
        }
    }
}
