using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
    public class JobGiver_AIClearPollution : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null) return null;
            if (pawn.Map == null) return null;
            if (pawn.Map.areaManager.PollutionClear == null) return null;
            if (pawn.Map.areaManager.PollutionClear.ActiveCells == null) return null;

            List<IntVec3> tmpCellsToClear = new List<IntVec3>();
            tmpCellsToClear.Clear();
            tmpCellsToClear.AddRange(from c in pawn.Map.areaManager.PollutionClear.ActiveCells
                                        where c.DistanceToSquared(pawn.Position) < 100
                                        select c);
            if (tmpCellsToClear != null && tmpCellsToClear.Count > 0)
            {
                tmpCellsToClear.SortBy((IntVec3 c) => c.DistanceToSquared(pawn.Position));
                int num = 6;
                for (int i = 0; i < tmpCellsToClear.Count; i++)
                {
                    if (CanUnpollute(pawn, pawn.Position, pawn.Map, tmpCellsToClear[i]))
                    {
                        tmpCellsToClear[i].Unpollute(pawn.Map);
                        num--;
                        if (num <= 0)
                        {
                            break;
                        }
                    }
                }
                LocalTargetInfo closestThing = tmpCellsToClear.First();
                if (pawn.CanReserve(closestThing))
                {
                    Job job = new Job(JobDefOf.ClearPollution);
                    job.AddQueuedTarget(TargetIndex.A, closestThing);
                    return job;
                }
            }
            
            return null;
        }

        private bool CanUnpollute(Pawn pawn, IntVec3 root, Map map, IntVec3 c, bool ignoreOtherPawnsCleaningCell = false)
        {
            if (!c.IsPolluted(map))
            {
                return false;
            }
            if (!ignoreOtherPawnsCleaningCell && AnyOtherPawnCleaning(pawn, c))
            {
                return false;
            }
            if (root.GetRoom(map) != c.GetRoom(map))
            {
                return false;
            }
            if (c.DistanceToSquared(root) > 100)
            {
                return false;
            }
            return true;
        }

        private bool AnyOtherPawnCleaning(Pawn pawn, IntVec3 cell)
        {
            List<Pawn> freeColonistsSpawned = pawn.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < freeColonistsSpawned.Count; i++)
            {
                if (freeColonistsSpawned[i] != pawn && freeColonistsSpawned[i].CurJobDef == JobDefOf.ClearPollution)
                {
                    LocalTargetInfo target = freeColonistsSpawned[i].CurJob.GetTarget(TargetIndex.A);
                    if (target.IsValid && target.Cell == cell)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
