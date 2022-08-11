using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using TorannMagic.TMDefs;
using System;
using UnityEngine;
using Verse.Sound;

namespace TorannMagic.Golems
{
    internal class JobDriver_FleshHarvest : JobDriver
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
            //this.FailOnThingMissingDesignation(TargetIndex.A, DesignationDefOf.CutPlant);
            //this.FailOnCellMissingDesignation(TargetIndex.A, DesignationDefOf.CutPlant);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;                
                actor.pather.StartPath(TargetA, PathEndMode.Touch);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return toil;

            Toil doJob = new Toil();
            doJob.initAction = delegate
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                TM_GolemUpgrade gu = cg.Upgrades.FirstOrDefault((TM_GolemUpgrade x) => x.golemUpgradeDef.ability?.jobDef == TorannMagicDefOf.JobDriver_FleshHarvest);
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
                    List<Thing> dList = new List<Thing>();
                    dList.Clear();
                    IEnumerable<Thing> tmpThings = from t in pawn.Map.listerThings.AllThings
                                                    where (t.def.plant != null && !t.Fogged() && !t.IsForbidden(p) && (t.Position - TargetLocA).LengthHorizontal < 2f)
                                                    select t;
                    foreach (Thing t in tmpThings)
                    {
                        if (t.Map.designationManager.AllDesignationsOn(t).Any((Designation x) => x.def == DesignationDefOf.HarvestPlant))
                        {
                            dList.Add(t);
                        }
                        else if (t is Plant)
                        {
                            Plant plant = t as Plant;
                            if (!plant.Blighted && plant.Growth >= 1 && plant.HarvestableNow && plant.DeliberatelyCultivated())
                            {
                                dList.Add(plant);
                            }
                        }
                    }
                    if (dList != null && dList.Count > 0)
                    {
                        float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(p.DrawPos, TargetThingA.DrawPos)).ToAngleFlat();
                        int num2 = dList.Count;
                        for (int i = 0; i < num2; i++)
                        {
                            Plant plant = dList[i] as Plant;
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirt, plant.DrawPos, plant.Map, .7f, .05f, .15f, .38f, 0, 1f, angle, angle);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Cleave, plant.DrawPos, plant.Map, .6f, .05f, .04f, .15f, 1000, 1, angle, angle);
                            if (!plant.Destroyed && plant != null && plant.def.plant.harvestedThingDef != null)
                            {
                                int num = plant.YieldNow();
                                if (num > 0)
                                {
                                    Thing thing = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef);
                                    thing.stackCount = num;
                                    if (pawn.Faction != Faction.OfPlayer)
                                    {
                                        thing.SetForbidden(value: true);
                                    }
                                    Find.QuestManager.Notify_PlantHarvested(pawn, thing);
                                    GenPlace.TryPlaceThing(thing, pawn.Position, base.Map, ThingPlaceMode.Near);
                                    pawn.records.Increment(RecordDefOf.PlantsHarvested);
                                    plant.def.plant.soundHarvestFinish.PlayOneShot(pawn);
                                }
                            }
                            p.Map.designationManager.RemoveAllDesignationsOn(plant);
                            plant.PlantCollected(pawn);
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