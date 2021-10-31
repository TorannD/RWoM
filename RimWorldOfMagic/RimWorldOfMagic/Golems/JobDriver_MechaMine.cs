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
    internal class JobDriver_MechaMine : JobDriver
    {
        private int age = -1;
        public int durationTicks = 300;
        int maxTargets = 3;
        int ticksToLaserPick = 12;
        public List<Mineable> targets;
        LocalTargetInfo secondCell;
        Effecter effecter;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            //this.FailOnCellMissingDesignation(TargetIndex.A, DesignationDefOf.Mine);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;                
                actor.pather.StartPath(actor.jobs.curJob.GetTarget(TargetIndex.A), PathEndMode.Touch);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return toil;

            Toil gotoSecond = new Toil();
            gotoSecond.initAction = delegate
            {
                Pawn actor = toil.actor;
                secondCell = TM_Calc.FindClosestCellPlus1VisibleToTarget(pawn, TargetA, true);
                actor.pather.StartPath(secondCell, PathEndMode.OnCell);
            };
            gotoSecond.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return gotoSecond;

            Toil doJob = new Toil();
            doJob.initAction = delegate
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                TM_GolemUpgrade gu = cg.Upgrades.FirstOrDefault((TM_GolemUpgrade x) => x.golemUpgradeDef.ability?.jobDef == TorannMagicDefOf.JobDriver_MechaMine);
                if(gu != null)
                {
                    durationTicks += gu.currentLevel * 20;
                    maxTargets += gu.currentLevel;
                    targets = new List<Mineable>();
                    targets.Clear();
                    targets.Add(TargetThingA as Mineable);
                    cg.Energy.CurLevel -= cg.Energy.ActualNeedCost(gu.golemUpgradeDef.ability.needCost);                   
                }                
            };
            doJob.tickAction = delegate
            {
                age++;
                ticksToLaserPick--;
                if (age > durationTicks * .5f)
                {                    
                    if(Find.TickManager.TicksGame % 2 == 0)
                    {
                        Mineable m = targets.RandomElement();
                        TMPawnGolem pg = pawn as TMPawnGolem;
                        Vector3 mPos = m.DrawPos;
                        mPos.x += Rand.Range(-.15f, .15f);
                        mPos.z += Rand.Range(-.15f, .15f);
                        DrawMesh mesh = new DrawMesh(TM_MatPool.light_laser_long, pg.EyeVector, mPos, 2, 6, 3);
                        pg.drawQueue.Add(mesh);
                        FleckMaker.ThrowMicroSparks(m.DrawPos, m.Map);
                        FleckMaker.ThrowDustPuff(m.Position, m.Map, Rand.Range(.6f, .8f));
                    }
                    if (ticksToLaserPick <= 0)
                    {
                        Mineable mineable = targets.FirstOrDefault();
                        if (!mineable.DestroyedOrNull())
                        {
                            int num = mineable.def.building.isNaturalRock ? 40 : 20;
                            if (mineable.HitPoints > num)
                            {
                                DamageInfo dinfo = new DamageInfo(DamageDefOf.Mining, (float)num, 0f, -1f, pawn);
                                mineable.TakeDamage(dinfo);
                            }
                            else
                            {
                                mineable.Notify_TookMiningDamage(mineable.HitPoints, pawn);
                                mineable.HitPoints = 0;
                                mineable.DestroyMined(pawn);
                            }
                            if (mineable.Destroyed)
                            {
                                pawn.Map.mineStrikeManager.CheckStruckOre(mineable.Position, mineable.def, pawn);
                                pawn.records.Increment(RecordDefOf.CellsMined);
                                if (pawn.Faction == Faction.OfPlayer && MineStrikeManager.MineableIsVeryValuable(mineable.def))
                                {
                                    TaleRecorder.RecordTale(TaleDefOf.MinedValuable, pawn, mineable.def.building.mineableThing);
                                }
                                if (pawn.Faction == Faction.OfPlayer && MineStrikeManager.MineableIsValuable(mineable.def) && !pawn.Map.IsPlayerHome)
                                {
                                    TaleRecorder.RecordTale(TaleDefOf.CaravanRemoteMining, pawn, mineable.def.building.mineableThing);
                                }
                                ReadyForNextToil();
                            }
                        }
                        
                        ticksToLaserPick = 6;
                    }
                    
                }
                
                if (age > durationTicks)
                {
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
                return 1f - (float)doJob.actor.jobs.curDriver.ticksLeftThisToil / 100;
            }, false, 0f);
            yield return doJob;
        }
    }
}