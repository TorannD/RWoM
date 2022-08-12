using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using AbilityUser;
using TorannMagic.TMDefs;
using TorannMagic.Golems;

namespace TorannMagic.AutoCast
{

    public static class Phase
    {
        public static void Evaluate(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, float minDistance, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            //LocalTargetInfo jobTarget = caster.CurJob.targetA;
            LocalTargetInfo jobTarget = caster.pather.Destination;
            Thing carriedThing = null;

            //if (caster.CurJob.targetA.Thing != null && caster.CurJob.targetA.Thing.Map != caster.Map)
            //{
            //    Log.Message("" + caster.LabelShort + " jobdef " + caster.CurJobDef.defName + " checking phase - target a: " + caster.CurJob.targetA + " target b: " + caster.CurJob.targetB + " carrying: " + caster.CurJob.targetA.Thing.stackCount + " " + caster.CurJob.targetA);
            //}
            //else
            //{
            //    Log.Message("" + caster.LabelShort + " jobdef " + caster.CurJobDef.defName + " checking phase - target a: " + caster.CurJob.targetA + " target b: " + caster.CurJob.targetB + " carrying: none");
            //}
            //if (caster.CurJob.targetA.Thing != null) //&& caster.CurJob.def.defName != "Sow")
            //{
            //    if (caster.CurJob.targetA.Thing.Map != caster.Map) //carrying TargetA to TargetB
            //    {
            //        jobTarget = caster.CurJob.targetB;
            //        //carriedThing = caster.CurJob.targetA.Thing;                    
            //    }
            //    else if (caster.CurJob.targetB != null && caster.CurJob.targetB.Thing != null && caster.CurJob.def != JobDefOf.Rescue) //targetA using targetB for job
            //    {
            //        if (caster.CurJob.targetB.Thing.Map != caster.Map) //carrying targetB to targetA
            //        {
            //            jobTarget = caster.CurJob.targetA;
            //            //carriedThing = caster.CurJob.targetB.Thing;
            //        }
            //        else if(caster.CurJob.def == JobDefOf.TendPatient || caster.CurJobDef == JobDefOf.Refuel || caster.CurJobDef == JobDefOf.RefuelAtomic || caster.CurJobDef == JobDefOf.RearmTurret || 
            //            caster.CurJobDef == JobDefOf.RearmTurretAtomic || caster.CurJobDef == JobDefOf.FillFermentingBarrel)// || caster.CurJobDef == JobDefOf.)
            //        {
            //            jobTarget = caster.CurJob.targetB;
            //        }
            //        else //Getting targetA to carry to TargetB
            //        {
            //            jobTarget = caster.CurJob.targetA;
            //        }
            //    }
            //    else
            //    {
            //        if (caster.CurJob.targetA.Thing.InteractionCell != null && caster.CurJob.targetA.Cell != caster.CurJob.targetA.Thing.InteractionCell)
            //        {
            //            jobTarget = caster.CurJob.targetA.Thing.InteractionCell;
            //        }
            //        else
            //        {
            //            jobTarget = caster.CurJob.targetA;
            //        }
            //    }
            //}
            if(!jobTarget.Cell.Walkable(caster.Map))
            {
                jobTarget = TM_Calc.FindWalkableCellNextTo(jobTarget.Cell, caster.Map);
            }
            float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
            Vector3 directionToTarget = TM_Calc.GetVector(caster.Position, jobTarget.Cell);
            //Log.Message("" + caster.LabelShort + " job def is " + caster.CurJob.def.defName + " targetA " + caster.CurJob.targetA + " targetB " + caster.CurJob.targetB + " jobTarget " + jobTarget + " at distance " + distanceToTarget + " min distance " + minDistance + " at vector " + directionToTarget);
            //if (caster.carryTracker != null && caster.carryTracker.CarriedThing != null)
            //{
            //    carriedThing = caster.carryTracker.CarriedThing;
            //    //Log.Message("carrying: " + caster.carryTracker.CarriedThing.def.defName + " count " + caster.carryTracker.CarriedThing.stackCount);
            //}
            if (casterComp.Stamina.CurLevel >= casterComp.ActualStaminaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && distanceToTarget < 200)
            {
                if (distanceToTarget > minDistance && caster.CurJob.locomotionUrgency >= LocomotionUrgency.Jog)// && caster.CurJob.bill == null)
                {
                    if (distanceToTarget <= abilitydef.MainVerb.range && jobTarget.Cell != default(IntVec3) && jobTarget.Cell.Walkable(caster.Map))
                    {
                        //Log.Message("doing blink to thing");
                        //DoPhase(caster, casterComp, abilitydef, jobTarget.Cell, ability, carriedThing, power);
                        IntVec3 walkableCell = TM_Action.FindNearestWalkableCell(caster, jobTarget.Cell);
                        if (TM_Calc.PawnCanOccupyCell(caster, walkableCell))
                        {
                            DoPhase2(caster, casterComp, abilitydef, jobTarget.Cell, ability, carriedThing, power);
                            success = true;
                        }
                    }
                    else
                    {
                        IntVec3 phaseToCell = caster.Position + (directionToTarget * abilitydef.MainVerb.range).ToIntVec3();
                        //Log.Message("doing partial blink to cell " + blinkToCell);
                        //FleckMaker.ThrowHeatGlow(blinkToCell, caster.Map, 1f);
                        bool canReach = false;
                        bool isCloser = false;
                        try
                        {
                            canReach = caster.Map.reachability.CanReach(phaseToCell, jobTarget.Cell, PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.PassDoors));
                            //Log.Message("future path cost" + );
                        }
                        catch
                        {
                            //Log.Warning("failed path check");
                        }
                        //Log.Message("can reach after phase: " + canReach);
                        if (canReach && phaseToCell.IsValid && phaseToCell.InBoundsWithNullCheck(caster.Map) && phaseToCell.Walkable(caster.Map) && !phaseToCell.Fogged(caster.Map))// && ((phaseToCell - caster.Position).LengthHorizontal < distanceToTarget))
                        {

                            PawnPath ppc = caster.Map.pathFinder.FindPath(caster.Position, jobTarget.Cell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly), PathEndMode.ClosestTouch);
                            float currentCost = ppc.TotalCost;
                            float futureCost = currentCost;
                            ppc.ReleaseToPool();

                            PawnPath ppf = caster.Map.pathFinder.FindPath(phaseToCell, jobTarget.Cell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly), PathEndMode.ClosestTouch);
                            futureCost = ppf.TotalCost;
                            ppf.ReleaseToPool();
                            isCloser = currentCost > futureCost;

                            if (isCloser)
                            {
                                //DoPhase(caster, casterComp, abilitydef, phaseToCell, ability, carriedThing, power);
                                DoPhase2(caster, casterComp, abilitydef, phaseToCell, ability, carriedThing, power);
                                success = true;
                            }
                        }
                    }
                }
            }
        }

        private static void DoPhase2(Pawn caster, CompAbilityUserMight casterComp, TMAbilityDef abilitydef, IntVec3 targetCell, PawnAbility ability, Thing carriedThing, MightPower power)
        {
            Pawn p = caster;
            Map map = caster.Map;
            IntVec3 casterCell = caster.Position;

            bool selectCaster = false;
            if (Find.Selector.FirstSelectedObject == caster)
            {
                selectCaster = true;
            }

            for (int i = 0; i < 3; i++)
            {
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, caster.DrawPos, caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Enchanting, caster.DrawPos, caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
            }

            LocalTargetInfo pathEndTarget = caster.pather.Destination;
            PathEndMode pem = Traverse.Create(root: caster.pather).Field(name: "peMode").GetValue<PathEndMode>();

            caster.Position = targetCell;
            //caster.Notify_Teleported(false, false);            
            caster.pather.StopDead();
            caster.pather.nextCell = targetCell;
            caster.pather.nextCellCostLeft = 0f;
            caster.pather.nextCellCostTotal = 1f;
            caster.pather.StartPath(pathEndTarget, pem);
            //GenClamor.DoClamor(caster, 2f, ClamorDefOf.Ability);

            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            casterComp.MightUserXP -= (int)((casterComp.ActualStaminaCost(abilitydef) * 180 * .9f * casterComp.xpGain * settingsRef.xpMultiplier));
            ability.PostAbilityAttempt();
            if (selectCaster)
            {
                Find.Selector.Select(caster, false, true);
            }
            for (int i = 0; i < 3; i++)
            {
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Enchanting, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
            }
        }

        private static void DoPhase(Pawn caster, CompAbilityUserMight casterComp, TMAbilityDef abilitydef, IntVec3 targetCell, PawnAbility ability, Thing carriedThing, MightPower power)
        {
            JobDef retainJobDef = caster.CurJobDef;
            LocalTargetInfo retainTargetA = caster.CurJob.targetA;
            int retainJobCount = 1;
            if (caster.CurJob != null)
            {
                retainJobCount = caster.CurJob.count;
            }
            LocalTargetInfo retainTargetB = caster.CurJob.targetB;
            LocalTargetInfo retainTargetC = caster.CurJob.targetC;
            Pawn p = caster;
            Thing cT = carriedThing;
            if (cT != null && cT.stackCount != retainJobCount && retainJobCount == 0)
            {
                //Log.Message("stack count " + cT.stackCount + " rjob count " + retainJobCount + " job count " + caster.CurJob.count);
                retainJobCount = cT.stackCount;
            }
            Map map = caster.Map;
            IntVec3 casterCell = caster.Position;
            bool selectCaster = false;
            if (Find.Selector.FirstSelectedObject == caster)
            {
                selectCaster = true;
            }
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, caster.DrawPos, caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Enchanting, caster.DrawPos, caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }
                caster.ClearReservationsForJob(caster.CurJob);
                caster.DeSpawn();
                GenSpawn.Spawn(p, targetCell, map);
                if (!carriedThing.DestroyedOrNull() && carriedThing.Spawned)
                {
                    carriedThing.DeSpawn();
                    GenPlace.TryPlaceThing(cT, targetCell, map, ThingPlaceMode.Near);
                    //GenSpawn.Spawn(cT, targetCell, map);
                }

                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                casterComp.MightUserXP -= (int)((casterComp.ActualStaminaCost(abilitydef) * 180 * .9f * casterComp.xpGain * settingsRef.xpMultiplier));
                ability.PostAbilityAttempt();
                if (selectCaster)
                {
                    Find.Selector.Select(caster, false, true);
                }
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, caster.DrawPos, caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Enchanting, caster.DrawPos, caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }

                Job job = new Job(retainJobDef, retainTargetA, retainTargetB, retainTargetC)
                {
                    count = retainJobCount,                    
                    //playerForced = false                    
                };
                //caster.jobs.TryTakeOrderedJob();
                caster.jobs.ClearQueuedJobs();
                caster.jobs.startingNewJob = true;
                caster.jobs.StartJob(job);
            }
            catch
            {
                if (!caster.Spawned)
                {
                    GenSpawn.Spawn(p, casterCell, map);
                }
            }
        }
    }

    public static class OnTarget_Spell
    {
        public static void TryExecute(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, LocalTargetInfo target, int maxRange, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= abilitydef.manaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                if (jobTarget != null)
                {
                    float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                    bool canReserve = false;
                    if (jobTarget.Thing != null)
                    {
                        canReserve = caster.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly);
                    }
                    else
                    {
                        canReserve = caster.CanReach(target, PathEndMode.Touch, Danger.Some);
                    }
                    if (distanceToTarget < maxRange && canReserve)
                    {
                        Job job = new Job(TorannMagicDefOf.JobDriver_GotoAndCast, jobTarget);
                        caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        JobDriver_GotoAndCast jobDriver = (JobDriver_GotoAndCast)caster.jobs.curDriver;
                        jobDriver.ability = ability;
                        success = true;
                    }
                }
            }
        }
    }

    public static class MeleeCombat_OnTarget
    {
        public static void TryExecute(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, LocalTargetInfo target, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget < (abilitydef.MainVerb.range) && jobTarget != null && jobTarget.Thing != null)
                {
                    if (jobTarget.Thing != caster)
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        DoJob.Execute(job, caster);
                        success = true;
                    }
                }
            }
        }
    }
    
    public static class CombatAbility_OnTarget
    {
        public static void TryExecute(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, LocalTargetInfo target, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;                
                if (distanceToTarget >= minRange && jobTarget != null && jobTarget.Thing != null) //&& distanceToTarget < (abilitydef.MainVerb.range * .9f)
                {                    
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = false;
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class CombatAbility_OnTarget_LoS
    {
        public static void TryExecute(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, LocalTargetInfo target, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget >= minRange && jobTarget != null && jobTarget.Thing != null && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range)) // distanceToTarget < (abilitydef.MainVerb.range * .9f) &&
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    caster.jobs.TryTakeOrderedJob(job);
                    success = true;
                }
            }
        }
    }

    public static class MagicAbility_OnTarget
    {
        public static void TryExecute(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, LocalTargetInfo target, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= abilitydef.manaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;               
                if (distanceToTarget >= minRange && jobTarget != null && jobTarget.Thing != null) //&& distanceToTarget < (abilitydef.MainVerb.range * .9f)
                {                    
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class CombatAbility_OnCell
    {
        public static void TryExecute(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, LocalTargetInfo target, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget >= minRange && jobTarget != null && jobTarget.Cell.IsValid && jobTarget.Cell.InBoundsWithNullCheck(casterComp.Pawn.Map) && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range)) //&& distanceToTarget < (abilitydef.MainVerb.range * .9f)
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class MagicAbility_OnCell
    {
        public static void TryExecute(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, LocalTargetInfo target, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= abilitydef.manaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = target;
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget >= minRange && jobTarget != null && jobTarget.Cell.IsValid && jobTarget.Cell.InBoundsWithNullCheck(casterComp.Pawn.Map) && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range)) //&& distanceToTarget < (abilitydef.MainVerb.range * .9f)
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class AoECombat
    {
        public static void Evaluate(CompAbilityUserMight mightComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, int minTargetCount, int radiusAround, IntVec3 evaluatedCenter, bool hostile, out bool success)
        {
            success = false;
            if (mightComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = mightComp.Pawn;
                List<Pawn> targetList = TM_Calc.FindPawnsNearTarget(caster, radiusAround, evaluatedCenter, hostile);
                if (targetList != null)
                {
                    LocalTargetInfo jobTarget = null;
                    if (targetList.Count >= minTargetCount && (abilitydef == TorannMagicDefOf.TM_BladeSpin))
                    {
                        jobTarget = caster;
                    }
                    if (jobTarget != null && jobTarget.Thing != caster)
                    {
                        float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                    }
                    if (jobTarget != null && jobTarget.Thing != null)
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        caster.jobs.TryTakeOrderedJob(job);                        
                        success = true;
                    }
                }
            }
        }
    }

    public static class CombatAbility
    {
        public static void Evaluate(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, out bool success)
        {
            success = false;
            EvaluateMinRange(casterComp, abilitydef, ability, power, 3f, out success);
        }

        public static void EvaluateMinRange(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyEnemy(caster, (int)(abilitydef.MainVerb.range * .9f));
                if(jobTarget != null && jobTarget.Thing != null && abilitydef == TorannMagicDefOf.TM_AntiArmor)
                {
                    Pawn targetPawn = jobTarget.Thing as Pawn;
                    if(targetPawn.RaceProps.IsFlesh)
                    {
                        jobTarget = null;
                    }
                }
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if(jobTarget != null && jobTarget.Thing != null && (distanceToTarget > minRange && distanceToTarget < (abilitydef.MainVerb.range * .9f)) && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range))
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class HealSelf
    {
        public static void Evaluate(CompAbilityUserMight mightComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, out bool success)
        {
            success = false;
            if (mightComp != null)
            {
                EvaluateMinSeverity(mightComp, abilitydef, ability, power, 0, out success);
            }
        }

        public static void EvaluateMinSeverity(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, float minSeverity, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= casterComp.ActualStaminaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = casterComp.Pawn;
                if (jobTarget != null && jobTarget.Thing != null && caster.health.HasHediffsNeedingTend(false))
                {
                    if (minSeverity != 0)
                    {
                        float injurySeverity = 0f;
                        using (IEnumerator<BodyPartRecord> enumerator = caster.health.hediffSet.GetInjuredParts().GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                BodyPartRecord rec = enumerator.Current;
                                IEnumerable<Hediff_Injury> arg_BB_0 = caster.health.hediffSet.GetHediffs<Hediff_Injury>();
                                Func<Hediff_Injury, bool> arg_BB_1;
                                arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                                foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                                {
                                    bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                                    if (flag5)
                                    {
                                        injurySeverity += current.Severity;
                                    }
                                }
                            }
                        }
                        if(injurySeverity >= minSeverity)
                        {
                            Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                            DoJob.Execute(job, caster);
                            success = true;
                        }                        
                    }
                    else
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        DoJob.Execute(job, caster);
                        success = true;
                    }                    
                }
            }
        }
    }

    public static class MonkCombatAbility
    {
        public static void Evaluate(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, out bool success)
        {
            success = false;
            EvaluateMinRange(casterComp, abilitydef, ability, power, null, 1.4f, out success);
        }

        public static void EvaluateMinRange(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, LocalTargetInfo jobTarget, float maxRange, out bool success)
        {
            success = false;
            Hediff chi = casterComp.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);
            if (chi != null && chi.Severity >= (abilitydef.chiCost*100) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                if (jobTarget == null || (jobTarget.Thing != null && !(jobTarget.Thing is Pawn)))
                {
                   jobTarget = TM_Calc.FindNearbyEnemy(caster, (int)(abilitydef.MainVerb.range));
                }
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal; //&& distanceToTarget < (abilitydef.MainVerb.range * .9f)             
                if (jobTarget != null && jobTarget.Thing != null && (distanceToTarget < maxRange) && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range))
                {
                    if (abilitydef == TorannMagicDefOf.TM_ThunderStrike && distanceToTarget < 1.5f)
                    {
                        jobTarget = jobTarget.Cell + (TM_Calc.GetVector(caster.Position, jobTarget.Cell) * 2f).ToIntVec3();
                        FleckMaker.ThrowHeatGlow(jobTarget.Cell, caster.Map, 1f);
                    }
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    caster.jobs.TryTakeOrderedJob(job);
                    success = true;
                }
            }
        }
    }

    public static class CureSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, List<string> validAfflictionNames, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = null;
                MagicPowerSkill pwr = casterComp.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CureDisease_pwr");
                if (pwr.level >= 2)
                {
                    jobTarget = TM_Calc.FindNearbyAfflictedPawnAny(caster, (int)(abilitydef.MainVerb.range * .9f));
                    if (jobTarget != null && jobTarget.Thing is Pawn jobPawn)
                    {
                        if (jobPawn.health != null && jobPawn.health?.hediffSet != null && !(jobPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DiseaseImmunityHD) || jobPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DiseaseImmunity2HD)))
                        {

                        }
                        else
                        {
                            jobTarget = null;
                        }
                    }
                    else
                    {
                        jobTarget = null;
                    }
                }
                else
                {
                    jobTarget = TM_Calc.FindNearbyAfflictedPawn(caster, (int)(abilitydef.MainVerb.range * .9f), validAfflictionNames);
                }
                if (jobTarget != null)
                {
                    float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                    if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing != null)
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        DoJob.Execute(job, caster);
                        success = true;
                        TM_Action.TM_Toils.GotoAndWait(jobTarget.Thing as Pawn, caster, Mathf.RoundToInt(ability.Def.MainVerb.warmupTime * 60));
                    }
                }
            }
        }
    }

    public static class CureAddictionSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, List<string> validAddictionNames, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = null;
                MagicPowerSkill ver = casterComp.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CureDisease_ver");
                jobTarget = TM_Calc.FindNearbyAddictedPawn(caster, (int)(abilitydef.MainVerb.range * .9f), validAddictionNames);                
                if (jobTarget != null)
                {
                    float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                    if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing != null)
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        DoJob.Execute(job, caster);
                        success = true;
                        TM_Action.TM_Toils.GotoAndWait(jobTarget.Thing as Pawn, caster, Mathf.RoundToInt(ability.Def.MainVerb.warmupTime * 60));
                    }
                }
            }
        }
    }

    public static class HealSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            EvaluateMinSeverity(casterComp, abilitydef, ability, power, 0, out success);
        }

        public static void EvaluateMinSeverity(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, float minSeverity, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyInjuredPawn(caster, (int)(abilitydef.MainVerb.range * .9f), minSeverity);
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;                
                if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing != null)
                {
                    if (abilitydef == TorannMagicDefOf.TM_CauterizeWound && jobTarget.Thing is Pawn targetPawn)
                    {                        
                        if (targetPawn.health.HasHediffsNeedingTend(false))
                        {
                            Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                            job.endIfCantShootTargetFromCurPos = true;
                            DoJob.Execute(job, caster);
                            success = true;
                        }
                    }
                    else
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        DoJob.Execute(job, caster);
                        success = true;
                    }                    
                }
            }
        }
    }

    public static class HediffHealSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, HediffDef hediffDef, out bool success)
        {
            success = false;
            EvaluateMinSeverity(casterComp, abilitydef, ability, power, hediffDef, 0, out success);
        }

        public static void EvaluateMinSeverity(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, HediffDef hediffDef, float minSeverity, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyInjuredPawn(caster, (int)(abilitydef.MainVerb.range * .9f), minSeverity);
                if (jobTarget != null)
                {
                    float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                    if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget.Thing is Pawn targetPawn)
                    {
                        if (targetPawn.health != null && targetPawn.health.hediffSet != null && !targetPawn.health.hediffSet.HasHediff(hediffDef, false))
                        {
                            Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                            DoJob.Execute(job, caster);
                            success = true;
                            TM_Action.TM_Toils.GotoAndWait(targetPawn, caster, Mathf.RoundToInt(ability.Def.MainVerb.warmupTime * 60));
                        }
                    }
                }
            }
        }
    }

    public static class HealPermanentSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            EvaluateMinSeverity(casterComp, abilitydef, ability, power, 0, out success);
        }

        public static void EvaluateMinSeverity(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, float minSeverity, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyPermanentlyInjuredPawn(caster, (int)(abilitydef.MainVerb.range * .9f), minSeverity);
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing != null)
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    DoJob.Execute(job, caster);
                    success = true;
                    TM_Action.TM_Toils.GotoAndWait(jobTarget.Thing as Pawn, caster, Mathf.RoundToInt(ability.Def.MainVerb.warmupTime * 60));
                }
            }
        }
    }

    public static class HediffSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, HediffDef hediffDef, out bool success)
        {
            success = false;
            EvaluateMinRange(casterComp, abilitydef, ability, power, hediffDef, 6f, out success);
        }

        public static void EvaluateMinRange(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, HediffDef hediffDef, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyEnemy(caster, (int)(abilitydef.MainVerb.range * .9f));
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget > minRange && distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing is Pawn targetPawn && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range))
                {
                    if (!targetPawn.health.hediffSet.HasHediff(hediffDef, false))
                    {
                        Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                        DoJob.Execute(job, caster);
                        success = true;
                    }
                }
            }
        }
    }

    public static class DamageSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            EvaluateMinRange(casterComp, abilitydef, ability, power, 6f, out success);
        }

        public static void EvaluateMinRange(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, float minRange, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyEnemy(caster, (int)(abilitydef.MainVerb.range * .9f));
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget > minRange && distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing != null && TM_Calc.HasLoSFromTo(caster.Position, jobTarget, caster, 0, abilitydef.MainVerb.range))
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    job.endIfCantShootTargetFromCurPos = true;
                    caster.jobs.TryTakeOrderedJob(job);                    
                    success = true;
                }
            }
        }
    }

    public static class TransferManaSpell
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, bool inCombat, bool reverse, out bool success) //reverse == true transfers mana to caster
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyMage(caster, (int)(abilitydef.MainVerb.range * .9f), inCombat);
                if (!inCombat && jobTarget != null && jobTarget.Thing != null)
                {
                    Pawn transferPawn = jobTarget.Thing as Pawn;
                    CompAbilityUserMagic tComp = transferPawn.GetCompAbilityUserMagic();
                    if (reverse)
                    {                        
                        if(casterComp.Mana.CurLevel >= .3f || tComp.Mana.CurLevel <= .9f)
                        {
                            jobTarget = null;
                        }
                    }
                    else
                    {
                        if(casterComp.Mana.CurLevel <= .9f || tComp.Mana.CurLevel >= .9f)
                        {
                            jobTarget = null;
                        }
                    }
                }
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing != null)
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class Summon
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, float minDistance, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            LocalTargetInfo jobTarget = caster.CurJob.targetA;
            float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
            Vector3 directionToTarget = TM_Calc.GetVector(caster.Position, jobTarget.Cell);

            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && jobTarget.Thing != null && jobTarget.Thing.def.EverHaulable)
            {
                //Log.Message("summon: " + caster.LabelShort + " job def is " + caster.CurJob.def.defName + " targetA " + caster.CurJob.targetA + " targetB " + caster.CurJob.targetB + " jobTarget " + jobTarget + " at distance " + distanceToTarget + " min distance " + minDistance + " at vector " + directionToTarget);
                if (distanceToTarget > minDistance && distanceToTarget < abilitydef.MainVerb.range && caster.CurJob.locomotionUrgency >= LocomotionUrgency.Jog && caster.CurJob.bill == null && distanceToTarget < 200)
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    DoJob.Execute(job, caster);
                    success = true;
                }
            }
        }
    }

    public static class Shield
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            LocalTargetInfo jobTarget = caster;

            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && jobTarget.Thing != null)
            {
                float injurySeverity = 0;
                using (IEnumerator<BodyPartRecord> enumerator = caster.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;
                        IEnumerable<Hediff_Injury> arg_BB_0 = caster.health.hediffSet.GetHediffs<Hediff_Injury>();
                        Func<Hediff_Injury, bool> arg_BB_1;
                        arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                        foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                        {
                            bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                            if (flag5)
                            {
                                injurySeverity += current.Severity;                                
                            }
                        }
                    }
                }
                if (injurySeverity != 0 && !(caster.health.hediffSet.HasHediff(HediffDef.Named("TM_HediffShield"))))
                {
                    Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                    DoJob.Execute(job, caster);
                    success = true;
                }            
            }
        }
    }

    public static class CombatAbility_OnSelf
    {
        public static void Evaluate(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            LocalTargetInfo jobTarget = caster;

            if (casterComp.Stamina.CurLevel >= casterComp.ActualStaminaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && jobTarget != null)
            {
                Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                DoJob.Execute(job, caster);
                success = true;
            }
        }
    }

    public static class MagicAbility_OnSelf
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            LocalTargetInfo jobTarget = caster;

            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && jobTarget != null)
            {                
                Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                DoJob.Execute(job, caster);
                success = true;                
            }
        }
    }

    public static class MagicAbility_OnSelfPosition
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            LocalTargetInfo jobTarget = caster.Position;

            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && jobTarget != null)
            {
                Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                DoJob.Execute(job, caster);
                success = true;
            }
        }
    }

    public static class SpellMending
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, HediffDef hediffDef, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyPawn(caster, (int)(abilitydef.MainVerb.range * .9f));
                float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
                if (distanceToTarget < (abilitydef.MainVerb.range * .9f) && jobTarget != null && jobTarget.Thing is Pawn targetPawn)
                {
                    if (targetPawn.RaceProps.Humanlike && targetPawn.IsColonist)
                    {
                        bool tatteredApparel = false;
                        //List<Thought_Memory> targetPawnThoughts = null;
                        //targetPawn.needs.mood.thoughts.GetDistinctMoodThoughtGroups(targetPawnThoughts);
                        //Log.Message("target pawn is " + targetPawn.LabelShort);
                        //List<Thought_Memory> targetPawnThoughts = targetPawn.needs.mood.thoughts.memories.Memories;
                        //for (int i = 0; i < targetPawnThoughts.Count; i++)
                        //{
                        
                        //    if (targetPawnThoughts[i].def == ThoughtDefOf.ApparelDamaged)
                        //    {
                        //        tatteredApparel = true;
                        //    }
                        //}
                        List<Apparel> apparel = targetPawn.apparel.WornApparel;
                        for (int i = 0; i < apparel.Count; i++)
                        {
                            //Log.Message("evaluating equipment " + apparel[i].def.defName + " with hitpoint % of " + (float)(apparel[i].HitPoints/ apparel[i].MaxHitPoints) + " or " + (float)(apparel[i].HitPoints) / (float)(apparel[i].MaxHitPoints));
                            if (((float)(apparel[i].HitPoints) / (float)(apparel[i].MaxHitPoints)) < .5f)
                            {
                                tatteredApparel = true;
                            }
                        }
                        if (targetPawn.equipment.Primary != null)
                        {
                           if((float)(targetPawn.equipment.Primary.HitPoints) / (float)(targetPawn.equipment.Primary.MaxHitPoints) < .5f)
                            {
                                tatteredApparel = true;
                            }
                        }
                        if (!targetPawn.health.hediffSet.HasHediff(hediffDef, false) && tatteredApparel)
                        {
                            Job job = ability.GetJob(AbilityContext.AI, jobTarget);   
                            DoJob.Execute(job, caster);
                            success = true;
                        }
                    }
                }
            }
        }
    }

    public static class DoJob
    {
        public static void Execute(Job job, Pawn caster)
        {
            if (caster.IsColonist && ModOptions.Settings.Instance.autocastQueueing && !caster.Drafted && caster.CurJobDef != JobDefOf.Hunt)
            {
                if (caster.jobs.jobQueue.Count < 1)
                {
                    caster.jobs.jobQueue.EnqueueLast(job, JobTag.DraftedOrder);
                }
            }
            else
            {
                caster.jobs.TryTakeOrderedJob(job);                
            }
        }
    }

    public static class Teach
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, out bool success)
        {
            success = false;
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyMage(caster, (int)(abilitydef.MainVerb.range * 1.5f), false);
                if (jobTarget != null && jobTarget.Thing is Pawn targetPawn && (jobTarget.Cell - caster.Position).LengthHorizontal < (abilitydef.MainVerb.range * 1.5f))
                {
                    CompAbilityUserMagic targetPawnComp = targetPawn.GetCompAbilityUserMagic();
                    if (targetPawn.CurJobDef.joyKind != null || targetPawn.CurJobDef == JobDefOf.Wait_Wander || targetPawn.CurJobDef == JobDefOf.GotoWander)
                    {
                        if (targetPawn.IsColonist && targetPawnComp.MagicUserLevel < casterComp.MagicUserLevel && caster.relations.OpinionOf(targetPawn) >= 0)
                        {
                            Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                            if (ModOptions.Settings.Instance.autocastQueueing)
                            {
                                caster.jobs.jobQueue.EnqueueLast(job, JobTag.DraftedOrder);
                            }
                            else
                            {
                                caster.jobs.TryTakeOrderedJob(job);
                            }
                            success = true;
                        }
                    }
                }
            }
        }
    }

    public static class TeachMight
    {
        public static void Evaluate(CompAbilityUserMight casterComp, TMAbilityDef abilitydef, PawnAbility ability, MightPower power, out bool success)
        {
            success = false;
            if (casterComp.Stamina.CurLevel >= abilitydef.staminaCost && ability.CooldownTicksLeft <= 0)
            {
                Pawn caster = casterComp.Pawn;
                LocalTargetInfo jobTarget = TM_Calc.FindNearbyFighter(caster, (int)(abilitydef.MainVerb.range * 1.5f), false);
                if (jobTarget != null && jobTarget.Thing is Pawn targetPawn && (jobTarget.Cell - caster.Position).LengthHorizontal < (abilitydef.MainVerb.range * 1.5f))
                {
                    CompAbilityUserMight targetPawnComp = targetPawn.GetCompAbilityUserMight();
                    if ((targetPawn.CurJobDef.joyKind != null && targetPawn.CurJobDef != TorannMagicDefOf.JobDriver_TM_Meditate) || targetPawn.CurJobDef == JobDefOf.Wait_Wander || targetPawn.CurJobDef == JobDefOf.GotoWander)
                    {
                        if (targetPawn.IsColonist && targetPawnComp.MightUserLevel < casterComp.MightUserLevel && caster.relations.OpinionOf(targetPawn) >= 0)
                        {
                            Job job = ability.GetJob(AbilityContext.AI, jobTarget);
                            if (ModOptions.Settings.Instance.autocastQueueing)
                            {
                                caster.jobs.jobQueue.EnqueueLast(job, JobTag.DraftedOrder);
                            }
                            else
                            {
                                caster.jobs.TryTakeOrderedJob(job);
                            }
                            success = true;
                        }
                    }
                }
            }
        }
    }

    public static class Blink
    {
        public static void Evaluate(CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, PawnAbility ability, MagicPower power, float minDistance, out bool success)
        {
            success = false;
            Pawn caster = casterComp.Pawn;
            //LocalTargetInfo jobTarget = caster.CurJob.targetA;
            LocalTargetInfo jobTarget = caster.pather.Destination;
            Thing carriedThing = null;
            //if (caster.CurJob.targetA.Thing != null ) //&& caster.CurJob.def.defName != "Sow")
            //{
            //    if(caster.CurJob.targetA.Thing.Map != caster.Map) //carrying thing
            //    {
            //        jobTarget = caster.CurJob.targetB;
            //        //carriedThing = caster.CurJob.targetA.Thing;
            //    }
            //    else if(caster.CurJob.targetB != null && caster.CurJob.targetB.Thing != null && caster.CurJob.def != JobDefOf.Rescue) //targetA using targetB for job
            //    {
            //        if(caster.CurJob.targetB.Thing.Map != caster.Map) //carrying targetB to targetA
            //        {
            //            jobTarget = caster.CurJob.targetA;
            //            //carriedThing = caster.CurJob.targetB.Thing;
            //        }
            //        else if(caster.CurJob.def == JobDefOf.TendPatient || caster.CurJobDef == JobDefOf.Refuel || caster.CurJobDef == JobDefOf.RefuelAtomic || caster.CurJobDef == JobDefOf.RearmTurret ||
            //            caster.CurJobDef == JobDefOf.RearmTurretAtomic || caster.CurJobDef == JobDefOf.FillFermentingBarrel)
            //        {
            //            jobTarget = caster.CurJob.targetB;
            //        }
            //        else //Getting targetA to carry to TargetB
            //        {
            //            jobTarget = caster.CurJob.targetA;
            //        }
            //    }
            //    else
            //    {
            //        jobTarget = caster.CurJob.targetA;
            //    }
            //}
            if (!jobTarget.Cell.Walkable(caster.Map))
            {
                jobTarget = TM_Calc.FindWalkableCellNextTo(jobTarget.Cell, caster.Map);
            }
            float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
            Vector3 directionToTarget = TM_Calc.GetVector(caster.Position, jobTarget.Cell);
            //Log.Message("" + caster.LabelShort + " job def is " + caster.CurJob.def.defName + " targetA " + caster.CurJob.targetA + " targetB " + caster.CurJob.targetB + " jobTarget " + jobTarget + " at distance " + distanceToTarget + " min distance " + minDistance + " at vector " + directionToTarget);
            if (caster.carryTracker != null && caster.carryTracker.CarriedThing != null)
            {
                carriedThing = caster.carryTracker.CarriedThing;
                //Log.Message("carrying: " + caster.carryTracker.CarriedThing.def.defName + " count " + caster.carryTracker.CarriedThing.stackCount);
            }
            if (casterComp.Mana.CurLevel >= casterComp.ActualManaCost(abilitydef) && ability.CooldownTicksLeft <= 0 && distanceToTarget < 200)
            {
                if (distanceToTarget > minDistance && caster.CurJob.locomotionUrgency >= LocomotionUrgency.Jog)// && caster.CurJob.bill == null)
                {
                    if (distanceToTarget <= abilitydef.MainVerb.range && jobTarget.Cell != default(IntVec3))
                    {
                        //Log.Message("doing blink to thing");
                        IntVec3 walkableCell = TM_Action.FindNearestWalkableCell(caster, jobTarget.Cell);
                        if (TM_Calc.PawnCanOccupyCell(caster, walkableCell))
                        {
                            DoBlink(caster, casterComp, abilitydef, walkableCell, ability, carriedThing);
                            success = true;
                        }
                    }
                    else
                    {
                        IntVec3 blinkToCell = caster.Position + (directionToTarget * abilitydef.MainVerb.range).ToIntVec3();
                        //Log.Message("doing partial blink to cell " + blinkToCell);
                        //FleckMaker.ThrowHeatGlow(blinkToCell, caster.Map, 1f);
                        bool canReach = false;
                        bool isCloser = false;
                        if (blinkToCell.Walkable(caster.Map))
                        {
                            try
                            {
                                canReach = caster.Map.reachability.CanReach(blinkToCell, jobTarget.Cell, PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.PassDoors));
                            }
                            catch
                            {
                                Log.Warning("failed path check");
                            }

                            if (canReach && blinkToCell.IsValid && blinkToCell.InBoundsWithNullCheck(caster.Map) && blinkToCell.Walkable(caster.Map) && !blinkToCell.Fogged(caster.Map))// && ((blinkToCell - caster.Position).LengthHorizontal < distanceToTarget))
                            {
                                PawnPath ppc = caster.Map.pathFinder.FindPath(caster.Position, jobTarget.Cell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly), PathEndMode.ClosestTouch);
                                float currentCost = ppc.TotalCost;
                                float futureCost = currentCost;
                                ppc.ReleaseToPool();

                                PawnPath ppf = caster.Map.pathFinder.FindPath(blinkToCell, jobTarget.Cell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly), PathEndMode.ClosestTouch);
                                futureCost = ppf.TotalCost;
                                ppf.ReleaseToPool();
                                isCloser = currentCost > futureCost;

                                if (isCloser)
                                {
                                    DoBlink(caster, casterComp, abilitydef, blinkToCell, ability, carriedThing);
                                    success = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void DoBlink(Pawn caster, CompAbilityUserMagic casterComp, TMAbilityDef abilitydef, IntVec3 targetCell, PawnAbility ability, Thing carriedThing)
        {
            //JobDef retainJobDef = caster.CurJobDef;
            //LocalTargetInfo retainTargetA = caster.CurJob.targetA;
            //int retainJobCount = 1;
            //if (caster.CurJob != null)
            //{
            //    retainJobCount = caster.CurJob.count;
            //}
            //LocalTargetInfo retainTargetB = caster.CurJob.targetB;
            //LocalTargetInfo retainTargetC = caster.CurJob.targetC;
            Pawn p = caster;
            //Thing cT = carriedThing;
            //if (cT != null && cT.stackCount != retainJobCount && retainJobCount == 0)
            //{
            //    //Log.Message("stack count " + cT.stackCount + " rjob count " + retainJobCount + " job count " + caster.CurJob.count);
            //    retainJobCount = cT.stackCount;
            //}
            Map map = caster.Map;
            IntVec3 casterCell = caster.Position;
            bool selectCaster = false;
            if (Find.Selector.FirstSelectedObject == caster)
            {
                selectCaster = true;
            }
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, caster.DrawPos, caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, caster.DrawPos, caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }

                //caster.ClearReservationsForJob(caster.CurJob);
                //caster.DeSpawn();                
                //GenSpawn.Spawn(p, targetCell, map);
                //if(carriedThing != null)
                //{
                //    carriedThing.DeSpawn();
                //    GenPlace.TryPlaceThing(cT, targetCell, map, ThingPlaceMode.Near);
                //    //GenSpawn.Spawn(cT, targetCell, map);
                //}
                LocalTargetInfo pathEndTarget = caster.pather.Destination;
                PathEndMode pem = Traverse.Create(root: caster.pather).Field(name: "peMode").GetValue<PathEndMode>();

                caster.Position = targetCell;
                //caster.Notify_Teleported(false, false);            
                caster.pather.StopDead();
                caster.pather.nextCell = targetCell;
                caster.pather.nextCellCostLeft = 0f;
                caster.pather.nextCellCostTotal = 1f;
                caster.pather.StartPath(pathEndTarget, pem);
                //GenClamor.DoClamor(caster, 2f, ClamorDefOf.Ability);

                if (casterComp != null && casterComp.IsMagicUser)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    casterComp.MagicUserXP -= (int)((casterComp.ActualManaCost(abilitydef) * 300 * .7f * casterComp.xpGain * settingsRef.xpMultiplier));
                    ability.PostAbilityAttempt();
                }
                if(selectCaster)
                {
                    Find.Selector.Select(caster, false, true);
                }
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }

                //Job job = new Job(retainJobDef, retainTargetA, retainTargetB, retainTargetC)
                //{
                //    count = retainJobCount
                //};
                ////caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                //caster.jobs.ClearQueuedJobs();
                //caster.jobs.startingNewJob = true;
                //caster.jobs.StartJob(job);
            }
            catch
            {
                if (!caster.Spawned)
                {
                    GenSpawn.Spawn(p, casterCell, map);
                }
            }
        }
    }

    public static class AnimalBlink
    {
        public static void Evaluate(Pawn casterComp, float minDistance, float maxDistance, out bool success)
        {
            success = false;
            Pawn caster = casterComp;
            LocalTargetInfo jobTarget = caster.CurJob.targetA;
            Thing carriedThing = null;
            if (caster.CurJob.targetA.Thing != null) //&& caster.CurJob.def.defName != "Sow")
            {
                if (caster.CurJob.targetA.Thing.Map != caster.Map) //carrying thing
                {
                    jobTarget = caster.CurJob.targetB;
                    //carriedThing = caster.CurJob.targetA.Thing;
                }
                else if (caster.CurJob.targetB != null && caster.CurJob.targetB.Thing != null && caster.CurJob.def.defName != "Rescue") //targetA using targetB for job
                {
                    if (caster.CurJob.targetB.Thing.Map != caster.Map) //carrying targetB to targetA
                    {
                        jobTarget = caster.CurJob.targetA;
                        //carriedThing = caster.CurJob.targetB.Thing;
                    }
                    else //Getting targetA to carry to TargetB
                    {
                        jobTarget = caster.CurJob.targetA;
                    }
                }
                else
                {
                    if (caster.CurJob.targetA.Thing.InteractionCell != null && caster.CurJob.targetA.Cell != caster.CurJob.targetA.Thing.InteractionCell)
                    {
                        jobTarget = caster.CurJob.targetA.Thing.InteractionCell;
                    }
                    else
                    {
                        jobTarget = caster.CurJob.targetA;
                    }
                }
            }
            if (!jobTarget.Cell.Walkable(caster.Map))
            {
                jobTarget = TM_Calc.FindWalkableCellNextTo(jobTarget.Cell, caster.Map);
            }
            float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
            Vector3 directionToTarget = TM_Calc.GetVector(caster.Position, jobTarget.Cell);
            //Log.Message("" + caster.LabelShort + " job def is " + caster.CurJob.def.defName + " targetA " + caster.CurJob.targetA + " targetB " + caster.CurJob.targetB + " jobTarget " + jobTarget + " at distance " + distanceToTarget + " min distance " + minDistance + " at vector " + directionToTarget);
            if (caster.carryTracker != null && caster.carryTracker.CarriedThing != null)
            {
                carriedThing = caster.carryTracker.CarriedThing;
                //Log.Message("carrying: " + caster.carryTracker.CarriedThing.def.defName + " count " + caster.carryTracker.CarriedThing.stackCount);
            }
            if (distanceToTarget < 200)
            {
                if (distanceToTarget > minDistance && caster.CurJob.locomotionUrgency >= LocomotionUrgency.Jog && caster.CurJob.bill == null)
                {
                    if (distanceToTarget <= maxDistance && jobTarget.Cell != default(IntVec3))
                    {
                        //Log.Message("doing blink to thing");
                        DoBlink(caster, jobTarget.Cell, carriedThing);
                        success = true;
                    }
                    else
                    {
                        IntVec3 blinkToCell = caster.Position + (directionToTarget * maxDistance).ToIntVec3();
                        //Log.Message("doing partial blink to cell " + blinkToCell);
                        //FleckMaker.ThrowHeatGlow(blinkToCell, caster.Map, 1f);
                        if (blinkToCell.IsValid && blinkToCell.InBoundsWithNullCheck(caster.Map) && blinkToCell.Walkable(caster.Map) && !blinkToCell.Fogged(caster.Map) && ((blinkToCell - caster.Position).LengthHorizontal < distanceToTarget))
                        {
                            DoBlink(caster, blinkToCell, carriedThing);
                            success = true;
                        }
                    }
                }
            }
        }

        private static void DoBlink(Pawn caster, IntVec3 targetCell, Thing carriedThing)
        {
            JobDef retainJobDef = caster.CurJobDef;
            LocalTargetInfo retainTargetA = caster.CurJob.targetA;
            int retainJobCount = 1;
            if (caster.CurJob != null)
            {
                retainJobCount = caster.CurJob.count;
            }
            LocalTargetInfo retainTargetB = caster.CurJob.targetB;
            LocalTargetInfo retainTargetC = caster.CurJob.targetC;
            Pawn p = caster;
            Thing cT = carriedThing;
            if (cT != null && cT.stackCount != retainJobCount && retainJobCount == 0)
            {
                //Log.Message("stack count " + cT.stackCount + " rjob count " + retainJobCount + " job count " + caster.CurJob.count);
                retainJobCount = cT.stackCount;
            }
            Map map = caster.Map;
            IntVec3 casterCell = caster.Position;
            bool selectCaster = false;
            if (Find.Selector.FirstSelectedObject == caster)
            {
                selectCaster = true;
            }
            try
            {
                ThingDef moteThrown = null;
                Vector3 moteVector = TM_Calc.GetVector(casterCell, targetCell);
                float angle = moteVector.ToAngleFlat();
                if (angle >= -135 && angle < -45) //north
                {
                    moteThrown = TorannMagicDefOf.Mote_DWPhase_North;
                }
                else if (angle >= 45 && angle < 135) //south
                {
                    moteThrown = TorannMagicDefOf.Mote_DWPhase_South;
                }
                else if (angle >= -45 && angle < 45) //east
                {
                    moteThrown = TorannMagicDefOf.Mote_DWPhase_East;
                }
                else //west
                {
                    moteThrown = TorannMagicDefOf.Mote_DWPhase_West;
                }
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, caster.DrawPos, caster.Map, Rand.Range(.6f, 1f), .2f, .1f, .5f, 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);                    
                }
                TM_MoteMaker.ThrowGenericMote(moteThrown, caster.DrawPos, caster.Map, 1.4f, .1f, 0f, .4f, 0, 5f, (Quaternion.AngleAxis(90, Vector3.up) * moteVector).ToAngleFlat(), 0);
                bool drafted = caster.drafter.Drafted;
                caster.ClearReservationsForJob(caster.CurJob);
                caster.DeSpawn();
                GenSpawn.Spawn(p, targetCell, map);
                if (!carriedThing.DestroyedOrNull() && carriedThing.Spawned)
                {
                    carriedThing.DeSpawn();
                    GenPlace.TryPlaceThing(cT, targetCell, map, ThingPlaceMode.Near);
                    //GenSpawn.Spawn(cT, targetCell, map);
                }
                caster.Position = targetCell;
                caster.Notify_Teleported(true, true);
                //GenClamor.DoClamor(caster, 2f, ClamorDefOf.Ability);
                if (selectCaster)
                {
                    Find.Selector.Select(caster, false, true);
                }
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    //TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, caster.DrawPos, caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }
                Vector3 drawPos = targetCell.ToVector3Shifted() + (-2 * moteVector);
                TM_MoteMaker.ThrowGenericMote(moteThrown, drawPos, caster.Map, 1.4f, .1f, .3f, 0f, 0, 8f, (Quaternion.AngleAxis(90, Vector3.up) * moteVector).ToAngleFlat(), 0);
                if (caster.drafter == null)
                {
                    caster.drafter = new Pawn_DraftController(caster);
                }                

                if (drafted)
                {
                    caster.drafter.Drafted = true;
                }

                Job job = new Job(retainJobDef, retainTargetA, retainTargetB, retainTargetC)
                {
                    count = retainJobCount
                };
                //caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                caster.jobs.ClearQueuedJobs();
                caster.jobs.startingNewJob = true;
                caster.jobs.StartJob(job);
            }
            catch
            {
                if (!caster.Spawned)
                {
                    GenSpawn.Spawn(p, casterCell, map);
                }
            }
        }
    }

    public static class GolemBlink
    {
        public static void Evaluate(TMPawnGolem caster, TM_GolemAbilityDef ability, float minDistance, float maxDistance, out bool success)
        {
            success = false;
            LocalTargetInfo jobTarget = caster.pather.Destination;
            Thing carriedThing = null;
            
            if (!jobTarget.Cell.Walkable(caster.Map))
            {
                jobTarget = TM_Calc.FindWalkableCellNextTo(jobTarget.Cell, caster.Map);
            }
            float distanceToTarget = (jobTarget.Cell - caster.Position).LengthHorizontal;
            Vector3 directionToTarget = TM_Calc.GetVector(caster.Position, jobTarget.Cell);
            //Log.Message("" + caster.LabelShort + " job def is " + caster.CurJob.def.defName + " targetA " + caster.CurJob.targetA + " targetB " + caster.CurJob.targetB + " jobTarget " + jobTarget + " at distance " + distanceToTarget + " min distance " + minDistance + " at vector " + directionToTarget);
            if (caster.carryTracker != null && caster.carryTracker.CarriedThing != null)
            {
                carriedThing = caster.carryTracker.CarriedThing;
                //Log.Message("carrying: " + caster.carryTracker.CarriedThing.def.defName + " count " + caster.carryTracker.CarriedThing.stackCount);
            }
            if (distanceToTarget <= 400 && distanceToTarget > minDistance && caster.CurJob.locomotionUrgency >= LocomotionUrgency.Jog)
            {
                if (distanceToTarget <= maxDistance && jobTarget.Cell != default(IntVec3))
                {
                    //Log.Message("doing blink to thing");
                    IntVec3 walkableCell = TM_Action.FindNearestWalkableCell(caster, jobTarget.Cell);
                    if (TM_Calc.PawnCanOccupyCell(caster, walkableCell))
                    {
                        DoBlink(caster, ability, walkableCell, carriedThing);
                        success = true;
                    }
                }
                else
                {
                    IntVec3 blinkToCell = caster.Position + (directionToTarget * maxDistance).ToIntVec3();
                    //Log.Message("doing partial blink to cell " + blinkToCell);
                    bool canReach = false;
                    bool isCloser = false;
                    if (blinkToCell.Walkable(caster.Map))
                    {
                        try
                        {
                            canReach = caster.Map.reachability.CanReach(blinkToCell, jobTarget.Cell, PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.PassDoors));
                        }
                        catch
                        {
                            Log.Warning("failed path check");
                        }

                        if (canReach && blinkToCell.IsValid && blinkToCell.InBoundsWithNullCheck(caster.Map) && blinkToCell.Walkable(caster.Map) && !blinkToCell.Fogged(caster.Map))
                        {
                            PawnPath ppc = caster.Map.pathFinder.FindPath(caster.Position, jobTarget.Cell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly), PathEndMode.ClosestTouch);
                            float currentCost = ppc.TotalCost;
                            float futureCost = currentCost;
                            ppc.ReleaseToPool();

                            PawnPath ppf = caster.Map.pathFinder.FindPath(blinkToCell, jobTarget.Cell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly), PathEndMode.ClosestTouch);
                            futureCost = ppf.TotalCost;
                            ppf.ReleaseToPool();
                            isCloser = currentCost > futureCost;

                            if (isCloser)
                            {
                                DoBlink(caster, ability, blinkToCell, carriedThing);
                                success = true;
                            }
                        }
                    }
                }                
            }
        }

        private static void DoBlink(TMPawnGolem caster, TM_GolemAbilityDef ability, IntVec3 targetCell, Thing carriedThing)
        {
            Map map = caster.Map;
            IntVec3 casterCell = caster.Position;
            bool selectCaster = false;
            if (Find.Selector.FirstSelectedObject == caster)
            {
                selectCaster = true;
            }
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, caster.DrawPos, caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, caster.DrawPos, caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }

                LocalTargetInfo pathEndTarget = caster.pather.Destination;
                PathEndMode pem = Traverse.Create(root: caster.pather).Field(name: "peMode").GetValue<PathEndMode>();

                caster.Position = targetCell;            
                caster.pather.StopDead();
                caster.pather.nextCell = targetCell;
                caster.pather.nextCellCostLeft = 0f;
                caster.pather.nextCellCostTotal = 1f;
                caster.pather.StartPath(pathEndTarget, pem);

                if (selectCaster)
                {
                    Find.Selector.Select(caster, false, true);
                }
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(.6f, 1f), .4f, .1f, Rand.Range(.8f, 1.2f), 0, Rand.Range(2, 3), Rand.Range(-30, 30), 0);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, targetCell.ToVector3Shifted(), caster.Map, Rand.Range(1.4f, 2f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                }
            }
            catch
            {
                if (!caster.Spawned)
                {
                    GenSpawn.Spawn(caster, casterCell, map);
                }
            }
        }
    }
}
