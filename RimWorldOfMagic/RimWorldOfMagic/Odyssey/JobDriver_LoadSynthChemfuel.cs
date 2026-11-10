using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class JobDriver_LoadSynthChemfuel : JobDriver
    {
        private CompMagofuelReservoir Bank => (TargetB.Thing as ThingWithComps)?.GetComp<CompMagofuelReservoir>();

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => Bank == null || Bank.ChemfuelHopper >= 100f);

            // Keep the same five indices as jobs saved by previous versions.
            // Target A can already be in the carry tracker when Verse rebuilds toils.
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.Do(() =>
            {
                var carried = pawn.carryTracker.CarriedThing;
                if (carried == null || carried.def != ThingDefOf.Chemfuel || Bank == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                Bank.LoadChemfuel(carried, carried.stackCount);
            });
        }
    }
}
