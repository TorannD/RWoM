using System;
using System.Collections.Generic;
using Verse.AI;



namespace TorannMagic
{
    internal class JobDriver_UsePortal : JobDriver
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
            portalBldg = TargetA.Thing as Building_TMPortal;
            yield return Toils_Reserve.Reserve(building);
            
            Toil gotoPortal = new Toil()
            {
                initAction = () =>
                {
                    pawn.pather.StartPath(portalBldg.InteractionCell, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoPortal;

            yield return Toils_Goto.GotoThing(building, PathEndMode.InteractionCell);
           
        }        
    }
}
