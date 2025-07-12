using System;
using System.Collections.Generic;
using Verse.AI;
using Verse;


namespace TorannMagic
{
    internal class JobDriver_DeactivatePortal : JobDriver
    {
        private const TargetIndex building = TargetIndex.A;
        Building_TMPortal portalBldg = new Building_TMPortal();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(building);
            Toil reserveTargetA = Toils_Reserve.Reserve(building);
            yield return reserveTargetA;
            yield return Toils_Goto.GotoThing(building, PathEndMode.ClosestTouch);

            Toil deactivatePortal = new Toil();

            deactivatePortal.initAction = () =>
            {
                portalBldg = TargetA.Thing as Building_TMPortal;
                if (portalBldg != null)
                {
                    //Log.Message(" " + portalBldg.Label + " is valid at loc " + portalBldg.Position + " on map " + portalBldg.Map);
                    //Log.Message("Portal pairing is " + portalBldg.IsPaired);

                }
            };
            deactivatePortal.AddFinishAction(() =>
            {
                if (portalBldg != null)
                {
                    portalBldg.IsPaired = false;
                    //Log.Message("Portal pairing is now " + portalBldg.IsPaired);
                }
            });

            yield return deactivatePortal;
        }
    }
}
