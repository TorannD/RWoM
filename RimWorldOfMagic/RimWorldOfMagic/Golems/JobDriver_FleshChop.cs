using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using TorannMagic.TMDefs;
using System;
using UnityEngine;

namespace TorannMagic.Golems
{
    internal class JobDriver_FleshChop : JobDriver
    {
        private int age = -1;
        public int durationTicks = 300;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true; // pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            //this.FailOnCellMissingDesignation(TargetIndex.A, DesignationDefOf.CutPlant);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;                
                actor.pather.StartPath(actor.jobs.curJob.GetTarget(TargetIndex.A), PathEndMode.Touch);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return toil;

            Toil doJob = new Toil();
            doJob.initAction = delegate
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                TM_GolemUpgrade gu = cg.Upgrades.FirstOrDefault((TM_GolemUpgrade x) => x.golemUpgradeDef.ability?.jobDef == TorannMagicDefOf.JobDriver_FleshChop);
                if(gu != null && cg != null)
                {
                    durationTicks += gu.currentLevel - 20;
                    cg.Energy.CurLevel -= cg.Energy.ActualNeedCost(gu.golemUpgradeDef.ability.needCost);                  
                }   
                else
                {
                    this.EndJobWith(JobCondition.Errored);
                }
            };
            doJob.tickAction = delegate
            {
                age++;
                if (age >= durationTicks)
                {
                    Pawn p = pawn;
                    IEnumerable<Thing> chopThings = from t in pawn.Map.listerThings.AllThings
                                                    where (t.def.plant != null && !t.Fogged() && !t.IsForbidden(p) && (t.Position - TargetLocA).LengthHorizontal < 2f && t.Map.designationManager.AllDesignationsOn(t).Any((Designation x) => x.def == DesignationDefOf.CutPlant))
                                                    select t;
                    if (chopThings != null)
                    {
                        List<Thing> dList = chopThings.ToList();
                        int num = dList.Count;
                        float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(p.DrawPos, TargetThingA.DrawPos)).ToAngleFlat();
                        for (int i = 0; i < num; i++)
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirt, dList[i].DrawPos, dList[i].Map, .7f, .05f, .15f, .38f, 0, 1f, angle, angle);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Cleave, dList[i].DrawPos, dList[i].Map, .4f, .05f, .04f, .15f, 1000, 1f, angle, angle);
                            if (!dList[i].Destroyed)
                            {
                                if (dList[i].def.category == ThingCategory.Plant && dList[i].def.plant.IsTree && dList[i].def.plant.treeLoversCareIfChopped)
                                {
                                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.CutTree, pawn.Named(HistoryEventArgsNames.Doer)));
                                }
                                dList[i].Destroy(DestroyMode.Vanish);
                            }                            
                        }
                    }
                    this.EndJobWith(JobCondition.Succeeded);
                }
            };
            doJob.defaultCompleteMode = ToilCompleteMode.Never;
            doJob.defaultDuration = this.durationTicks;
            doJob.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Downed)
                {
                    return 1f;
                }
                return 1f - (float)doJob.actor.jobs.curDriver.ticksLeftThisToil / this.durationTicks;
            }, false, 0f);
            yield return doJob;
        }
    }
}