using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    internal class JobDriver_SleepNow : JobDriver
    {

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            Toil layDown = new Toil();
            float num = 1.25f;
            layDown.initAction = delegate
            {
                Pawn actor = layDown.actor;
                actor.pather.StopDead();
                JobDriver curDriver = actor.jobs.curDriver;
                actor.jobs.posture = PawnPosture.LayingOnGroundNormal;              
                curDriver.asleep = false;
                if (actor.mindState.applyBedThoughtsTick == 0)
                {
                    actor.mindState.applyBedThoughtsTick = Find.TickManager.TicksGame + Rand.Range(2500, 10000);
                    actor.mindState.applyBedThoughtsOnLeave = false;
                }
                if (actor.ownership != null && actor.CurrentBed() != actor.ownership.OwnedBed)
                {
                    ThoughtUtility.RemovePositiveBedroomThoughts(actor);
                }
            };
            layDown.tickAction = delegate
            {
                Pawn actor = layDown.actor;
                Job curJob = actor.CurJob;
                JobDriver curDriver = actor.jobs.curDriver;
                actor.GainComfortFromCellIfPossible();
                if (!curDriver.asleep)
                {
                    if (actor.needs.rest != null && actor.needs.rest.CurLevel < .99f * RestUtility.WakeThreshold(actor))
                    {
                        curDriver.asleep = true;
                    }
                }
                else if ((actor.needs.rest == null || actor.needs.rest.CurLevel >= RestUtility.WakeThreshold(actor)))
                {
                    actor.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
                    this.EndJobWith(JobCondition.Incompletable);
                }

                if (curDriver.asleep && actor.needs.rest != null)
                {                    
                    num = 0.7f * num + 0.3f * num;  //talk about convoluted calculations...
                    actor.needs.rest.TickResting(num);
                    if(actor.needs.rest.CurLevel >= .99f * RestUtility.WakeThreshold(actor))
                    {
                        curDriver.asleep = false;
                        actor.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                }

                if (actor.mindState.applyBedThoughtsTick != 0 && actor.mindState.applyBedThoughtsTick <= Find.TickManager.TicksGame)
                {
                    ApplyBedThoughts(actor);
                    actor.mindState.applyBedThoughtsTick += 60000;
                    actor.mindState.applyBedThoughtsOnLeave = true;
                }
                if (actor.IsHashIntervalTick(100) && !actor.Position.Fogged(actor.Map))
                {
                    if (curDriver.asleep)
                    {
                        FleckMaker.ThrowMetaIcon(actor.Position, actor.Map, FleckDefOf.SleepZ);
                    }
                    if (actor.health.hediffSet.GetNaturallyHealingInjuredParts().Any<BodyPartRecord>())
                    {
                        FleckMaker.ThrowMetaIcon(actor.Position, actor.Map, FleckDefOf.HealingCross);
                    }
                }
            };
            layDown.defaultCompleteMode = ToilCompleteMode.Never;
            layDown.AddFinishAction(delegate
            {
                Pawn actor = layDown.actor;
                JobDriver curDriver = actor.jobs.curDriver;
                if (actor.mindState.applyBedThoughtsOnLeave)
                {
                    ApplyBedThoughts(actor);
                }
                actor.jobs.posture = PawnPosture.Standing;
                curDriver.asleep = false;
            });
            yield return layDown;
        }

        private static void ApplyBedThoughts(Pawn actor)
        {
            if (actor.needs.mood == null)
            {
                return;
            }
            Building_Bed building_Bed = actor.CurrentBed();
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInBedroom);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInBarracks);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOutside);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOnGround);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInCold);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInHeat);
            if (actor.GetRoom(RegionType.Set_Passable).PsychologicallyOutdoors)
            {
                actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptOutside, null);
            }
            if (building_Bed == null || building_Bed.CostListAdjusted().Count == 0)
            {
                actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptOnGround, null);
            }
            if (actor.AmbientTemperature < actor.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null))
            {
                actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInCold, null);
            }
            if (actor.AmbientTemperature > actor.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null))
            {
                actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInHeat, null);
            }
            if (building_Bed != null && building_Bed == actor.ownership.OwnedBed && !building_Bed.ForPrisoners && !actor.story.traits.HasTrait(TraitDefOf.Ascetic))
            {
                ThoughtDef thoughtDef = null;
                if (building_Bed.GetRoom(RegionType.Set_Passable).Role == RoomRoleDefOf.Bedroom)
                {
                    thoughtDef = ThoughtDefOf.SleptInBedroom;
                }
                else if (building_Bed.GetRoom(RegionType.Set_Passable).Role == RoomRoleDefOf.Barracks)
                {
                    thoughtDef = ThoughtDefOf.SleptInBarracks;
                }
                if (thoughtDef != null)
                {
                    int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(building_Bed.GetRoom(RegionType.Set_Passable).GetStat(RoomStatDefOf.Impressiveness));
                    if (thoughtDef.stages[scoreStageIndex] != null)
                    {
                        actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, scoreStageIndex), null);
                    }
                }
            }
        }
    }
}