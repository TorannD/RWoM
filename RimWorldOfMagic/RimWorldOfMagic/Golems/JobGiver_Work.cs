using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Golems
{
    public class JobGiver_Work : RimWorld.JobGiver_Work
    {

        public override float GetPriority(Pawn pawn)
        {
            return 9f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            TMPawnGolem pg = pawn as TMPawnGolem;
            if (pg == null || !pg.Spawned || pg.Destroyed || pg.GetWorkGivers() == null)
            {
                return ThinkResult.NoJob;
            }
            TM_GolemUtility.UpdateWorkSkills(pg);
            return base.TryIssueJobPackage(pawn, jobParams);
            //List<WorkGiver> workGivers = pg.GetWorkGivers();
            //int num = -999;
            //TargetInfo bestTargetOfLastPriority = TargetInfo.Invalid;
            //WorkGiver_Scanner scannerWhoProvidedTarget = null;
            //WorkGiver_Scanner scanner;
            //for (int i = 0; i < workGivers.Count; i++)
            //{
            //    WorkGiver workGiver= workGivers[i];
            //    if (workGiver.def.priorityInType != num && bestTargetOfLastPriority.IsValid)
            //    {
            //        break;
            //    }
            //    if (PawnCanUseWorkGiver(ref pawn, workGiver))
            //    {
            //        try
            //        {
            //            Job job = workGiver.NonScanJob(pawn);
            //            if (job != null)
            //            {
            //                return new ThinkResult(job, this, (JobTag?)workGivers[i].def.tagToGive, false);
            //            }
            //            scanner = (workGiver as WorkGiver_Scanner);
            //            if (scanner != null)
            //            {
            //                if (scanner.def.scanThings)
            //                {
            //                    Predicate<Thing> predicate = (Thing t) => !ForbidUtility.IsForbidden(t, pawn) && scanner.HasJobOnThing(pawn, t, false);
            //                    IEnumerable<Thing> enumerable = scanner.PotentialWorkThingsGlobal(pawn);
            //                    Thing val3;
            //                    try
            //                    {
            //                        if (scanner.Prioritized)
            //                        {
            //                            IEnumerable<Thing> enumerable2 = enumerable;
            //                            if (enumerable2 == null)
            //                            {
            //                                enumerable2 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
            //                            }
            //                            val3 = ((!scanner.AllowUnreachable) ? GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), 0, false, false, false), 9999f, predicate, (Func<Thing, float>)((Thing x) => scanner.GetPriority(pawn, x))) : GenClosest.ClosestThing_Global(pawn.Position, enumerable2, 99999f, predicate, (Func<Thing, float>)((Thing x) => scanner.GetPriority(pawn, x))));
            //                        }
            //                        else if (scanner.AllowUnreachable)
            //                        {
            //                            IEnumerable<Thing> enumerable3 = enumerable;
            //                            if (enumerable3 == null)
            //                            {
            //                                enumerable3 = pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
            //                            }
            //                            val3 = GenClosest.ClosestThing_Global(pawn.Position, enumerable3, 99999f, predicate, (Func<Thing, float>)null);
            //                        }
            //                        else
            //                        {
            //                            val3 = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, scanner.PotentialWorkThingRequest, scanner.PathEndMode, TraverseParms.For(pawn, scanner.MaxPathDanger(pawn), 0, false, false, false), 9999f, predicate, enumerable, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, enumerable != null, RegionType.Set_Passable, false);
            //                        }
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Log.Error("Error in WorkGiver: " + ex.Message);
            //                        val3 = null;
            //                    }
            //                    if (val3 != null)
            //                    {
            //                        bestTargetOfLastPriority = val3;
            //                        scannerWhoProvidedTarget = scanner;
            //                    }
            //                }
            //                int num2 = 200;
            //                if (scanner.def.scanCells)
            //                {
            //                    IntVec3 pawnPosition = pawn.Position;
            //                    float closestDistSquared = 99999f;
            //                    float bestPriority = -3.40282347E+38f;
            //                    bool prioritized = scanner.Prioritized;
            //                    bool allowUnreachable = scanner.AllowUnreachable;
            //                    Danger maxPathDanger = scanner.MaxPathDanger(pawn);
            //                    IEnumerable<IntVec3> enumerable4 = scanner.PotentialWorkCellsGlobal(pawn);
            //                    IList<IntVec3> list;
            //                    if ((list = (enumerable4 as IList<IntVec3>)) != null)
            //                    {
            //                        for (int j = 0; j < list.Count; j++)
            //                        {
            //                            ProcessCell(list[j], ref pawn, ref scanner, ref pawnPosition, prioritized, allowUnreachable, maxPathDanger, ref bestTargetOfLastPriority, ref scannerWhoProvidedTarget, ref closestDistSquared, ref bestPriority);
            //                            if (bestTargetOfLastPriority != TargetInfo.Invalid)
            //                            {
            //                                break;
            //                            }
            //                            if (((object)scanner).ToString() != "RimWorld.WorkGiver_GrowerSow")
            //                            {
            //                                num2--;
            //                            }
            //                            if (num2 <= 0)
            //                            {
            //                                break;
            //                            }
            //                        }
            //                    }
            //                    else
            //                    {
            //                        foreach (IntVec3 item in enumerable4)
            //                        {
            //                            ProcessCell(item, ref pawn, ref scanner, ref pawnPosition, prioritized, allowUnreachable, maxPathDanger, ref bestTargetOfLastPriority, ref scannerWhoProvidedTarget, ref closestDistSquared, ref bestPriority);
            //                            if (bestTargetOfLastPriority != TargetInfo.Invalid)
            //                            {
            //                                break;
            //                            }
            //                            if (((object)scanner).ToString() != "RimWorld.WorkGiver_GrowerSow")
            //                            {
            //                                num2--;
            //                            }
            //                            if (num2 <= 0)
            //                            {
            //                                break;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch (Exception ex2)
            //        {
            //            Log.Error(((object)pawn)?.ToString() + " threw exception in WorkGiver " + workGiver.def.defName + ": " + ex2.ToString());
            //        }
            //        finally
            //        {
            //        }
            //        if (bestTargetOfLastPriority.IsValid)
            //        {
            //            Job val4 = (!bestTargetOfLastPriority.HasThing) ? scannerWhoProvidedTarget.JobOnCell(pawn, bestTargetOfLastPriority.Cell, false) : scannerWhoProvidedTarget.JobOnThing(pawn, bestTargetOfLastPriority.Thing, false);
            //            if (val4 != null)
            //            {
            //                val4.workGiverDef = scannerWhoProvidedTarget.def;
            //                return new ThinkResult(val4, this, (JobTag?)workGivers[i].def.tagToGive, false);
            //            }
            //            string[] obj = new string[6]
            //            {
            //            ((object)scannerWhoProvidedTarget)?.ToString(),
            //            " provided target ",
            //            null,
            //            null,
            //            null,
            //            null
            //            };
            //            TargetInfo val5 = bestTargetOfLastPriority;
            //            obj[2] = ((object)val5).ToString();
            //            obj[3] = " but yielded no actual job for pawn ";
            //            obj[4] = ((object)pawn)?.ToString();
            //            obj[5] = ". The CanGiveJob and JobOnX methods may not be synchronized.";
            //            Log.ErrorOnce(string.Concat(obj), 6112651);
            //        }
            //        num = workGiver.def.priorityInType;
            //    }
            //}
            //return ThinkResult.NoJob;
        }

        private void ProcessCell(IntVec3 c, ref Pawn pawn, ref WorkGiver_Scanner scanner, ref IntVec3 pawnPosition, bool prioritized, bool allowUnreachable, Danger maxPathDanger, ref TargetInfo bestTargetOfLastPriority, ref WorkGiver_Scanner scannerWhoProvidedTarget, ref float closestDistSquared, ref float bestPriority)
        {
            bool flag = false;
            IntVec3 val = c - pawnPosition;
            float num = (float)val.LengthHorizontalSquared;
            float num2 = 0f;
            if (prioritized)
            {
                if (!ForbidUtility.IsForbidden(c, pawn) && scanner.HasJobOnCell(pawn, c, false))
                {
                    if (!allowUnreachable && !ReachabilityUtility.CanReach(pawn, c, scanner.PathEndMode, maxPathDanger, false, false, 0))
                    {
                        return;
                    }
                    num2 = scanner.GetPriority(pawn, c);
                    if (num2 > bestPriority || (num2 == bestPriority && num < closestDistSquared))
                    {
                        flag = true;
                    }
                }
            }
            else if (num < closestDistSquared && !ForbidUtility.IsForbidden(c, pawn) && scanner.HasJobOnCell(pawn, c, false))
            {
                if (!allowUnreachable && !ReachabilityUtility.CanReach(pawn, c, scanner.PathEndMode, maxPathDanger, false, false, TraverseMode.ByPawn))
                {
                    return;
                }
                flag = true;
            }
            if (flag)
            {
                bestTargetOfLastPriority = new TargetInfo(c, pawn.Map, false);
                scannerWhoProvidedTarget = scanner;
                closestDistSquared = num;
                bestPriority = num2;
            }
        }

        private bool PawnCanUseWorkGiver(ref Pawn pawn, WorkGiver giver)
        {
            try
            {
                return !ThingUtility.DestroyedOrNull(pawn) && pawn.Spawned && giver.MissingRequiredCapacity(pawn) == null && !giver.ShouldSkip(pawn, false);
            }
            catch (Exception ex)
            {
                Log.Warning("Golem caught error in PawnCanUseWorkGiver: Golem " + pawn.def.defName + " on WorkGiver '" + giver.def.defName + "', this exception thrown in a try_catch \n" + ex.ToString());
                return false;
            }
        }
    }
}
