using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class JobDriver_DonateManaAtSiphon : JobDriver
    {
        private int nextDonationTick = -1;
        private float startCapacity, paidThisVisit;
        private CompManaSiphon Siphon => (TargetA.Thing as ThingWithComps)?.GetComp<CompManaSiphon>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextDonationTick, "TM_siphonJobNextTick", -1);
            Scribe_Values.Look(ref startCapacity, "TM_siphonJobStartCapacity", 0f);
            Scribe_Values.Look(ref paidThisVisit, "TM_siphonJobPaid", 0f);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            Siphon?.CanBeginDonation(pawn) == true && pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil
            {
                initAction = () =>
                {
                    if (nextDonationTick >= 0) return;
                    if (Siphon?.CanBeginDonation(pawn, true) != true) { EndJobWith(JobCondition.Succeeded); return; }
                    startCapacity = Siphon.DonationNeed(pawn, true).MaxLevel;
                    nextDonationTick = Find.TickManager.TicksGame + 60;
                },
                tickAction = () =>
                {
                    if (Siphon?.CanContinueDonation(pawn, startCapacity, paidThisVisit) != true) { EndJobWith(JobCondition.Succeeded); return; }
                    int now = Find.TickManager.TicksGame;
                    if (now < nextDonationTick) return;
                    nextDonationTick = now + 60;
                    float paid = Siphon.Donate(pawn, startCapacity, paidThisVisit);
                    paidThisVisit += paid;
                    if (paid <= 0f) EndJobWith(JobCondition.Succeeded);
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
        }
    }
}
