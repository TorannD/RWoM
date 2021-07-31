using System;
using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;


namespace TorannMagic
{
    internal class JobDriver_PortalStockpile : JobDriver
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

            Toil portalStockpile = new Toil();

            portalStockpile.initAction = () =>
            {
                foreach (IntVec3 current in portalBldg.PortableCells)
                {
                    Thing stockpileThing = current.GetFirstItem(base.Map);
                    if (stockpileThing != null)
                    {
                        FleckMaker.ThrowHeatGlow(stockpileThing.Position, stockpileThing.Map, 1f);
                        FleckMaker.ThrowLightningGlow(stockpileThing.Position.ToVector3Shifted(), stockpileThing.Map, 1f);
                        stockpileThing.DeSpawn();
                        GenPlace.TryPlaceThing(stockpileThing, portalBldg.PortalDestinationPosition, portalBldg.PortalDestinationMap, ThingPlaceMode.Near, null);
                        stockpileThing.SetForbidden(true, false);
                        stockpileThing.SetForbidden(false, false);
                        FleckMaker.ThrowLightningGlow(stockpileThing.Position.ToVector3Shifted(), stockpileThing.Map, 1f);

                    }
                    List<Thing> thingList;
                    Pawn portalAnimal = null;
                    thingList = current.GetThingList(base.Map);
                    int z = 0;
                    if (thingList != null)
                    {
                        while (z < thingList.Count)
                        {                            
                            bool validator = thingList[z] is Pawn;
                            if (validator)
                            {
                                portalAnimal = thingList[z] as Pawn;
                                if (portalAnimal != null)
                                {
                                    if (!portalAnimal.RaceProps.Humanlike && portalAnimal.RaceProps.Animal && portalAnimal.Faction != null && portalAnimal.Faction == Faction.OfPlayer)
                                    {
                                        FleckMaker.ThrowHeatGlow(current, portalBldg.Map, 1f);
                                        FleckMaker.ThrowLightningGlow(portalAnimal.DrawPos, portalAnimal.Map, 1f);
                                        portalAnimal.jobs.ClearQueuedJobs();
                                        portalAnimal.DeSpawn();
                                        GenPlace.TryPlaceThing(portalAnimal, portalBldg.PortalDestinationPosition, portalBldg.PortalDestinationMap, ThingPlaceMode.Near);
                                        //GenSpawn.Spawn(portalAnimal, portalBldg.PortalDestinationPosition, portalBldg.PortalDestinationMap);
                                        FleckMaker.ThrowLightningGlow(portalAnimal.DrawPos, portalAnimal.Map, 1f);
                                    }
                                }
                            }
                            z++;
                        }
                    }
                }
                portalBldg.ArcaneEnergyCur -= 0.1f;
            };
            yield return portalStockpile;

        }        
    }
}
