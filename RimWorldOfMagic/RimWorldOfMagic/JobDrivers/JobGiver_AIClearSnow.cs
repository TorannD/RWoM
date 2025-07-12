using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class JobGiver_AIClearSnow : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null) return null;
            if (pawn.Map == null) return null;
            if (pawn.Map.areaManager.SnowOrSandClear == null) return null;
            if (pawn.Map.areaManager.SnowOrSandClear.ActiveCells == null) return null;

            List<IntVec3> tmpCellsToClear = new List<IntVec3>();
            tmpCellsToClear.Clear();
            tmpCellsToClear.AddRange(from c in pawn.Map.areaManager.SnowOrSandClear.ActiveCells
                                        where c.DistanceToSquared(pawn.Position) < 100
                                        select c);
            if (tmpCellsToClear?.Count > 0)
            {
                tmpCellsToClear.SortBy((IntVec3 c) => c.DistanceToSquared(pawn.Position));
                LocalTargetInfo closestThing = tmpCellsToClear.First();
                if (pawn.CanReserve(closestThing))
                {
                    Job job = new Job(JobDefOf.ClearSnow);
                    job.AddQueuedTarget(TargetIndex.A, closestThing);
                    return job;
                }
            }
            
            return null;
        }
    }
}
