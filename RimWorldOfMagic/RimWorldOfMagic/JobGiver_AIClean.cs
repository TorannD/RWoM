using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    public class JobGiver_AIClean : ThinkNode_JobGiver //Code by Mehni from Penguin Mod
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Predicate<Thing> filth = f => f.def.category == ThingCategory.Filth;
            Thing closestThing = null;
            //Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Filth), PathEndMode.ClosestTouch,
            //TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 300f, filth, null, 0, -1, false, RegionType.Set_Passable, false);
            if (pawn != null && pawn.Map != null)
            {
                List<Thing> filthList = pawn.Map.listerFilthInHomeArea.FilthInHomeArea;
                if (filthList != null && filthList.Count > 0)
                {
                    for (int i = 0; i < filthList.Count; i++)
                    {
                        if (!filthList[i].Position.IsForbidden(pawn) && pawn.CanReserve(filthList[i], 1, -1, ReservationLayerDefOf.Floor, false) && pawn.CanReach(filthList[i], PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
                        {
                            Thing thing = filthList[i];                            
                            if(closestThing != null && thing != null && (thing.Position - pawn.Position).LengthHorizontal < (closestThing.Position - pawn.Position).LengthHorizontal)
                            {
                                closestThing = thing;
                            }
                            else if(closestThing == null)// && (pawn.CanReserve(closestThing)))
                            {
                                closestThing = thing;
                            }
                            else
                            {
                                //do nothing
                            }                            
                        }
                    }
                }
                if (closestThing != null && pawn.CanReserve(closestThing))
                {
                    Job job = new Job(JobDefOf.Clean);
                    job.AddQueuedTarget(TargetIndex.A, closestThing);
                    return job;
                }
            }
            return null;
        }
    }
}
