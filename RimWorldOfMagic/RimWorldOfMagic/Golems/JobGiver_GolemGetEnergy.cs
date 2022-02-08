using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class JobGiver_GolemGetEnergy : ThinkNode_JobGiver
    {
        private GolemEnergyCategory minCategory = GolemEnergyCategory.Critical;

        private float maxLevelPercentage = 1f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_GolemGetEnergy obj = (JobGiver_GolemGetEnergy)base.DeepCopy(resolve);
            obj.minCategory = minCategory;
            obj.maxLevelPercentage = maxLevelPercentage;
            return obj;
        }

        public override float GetPriority(Pawn pawn)
        {
            Need_GolemEnergy energy = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
            if (energy == null)
            {
                return 0f;
            }
            if(pawn.jobs != null && pawn.jobs.curJob.def == JobDefOf.AttackMelee)
            {
                return 0f;
            }
            if (energy.CurCategory == GolemEnergyCategory.Critical)
            {
                return 10f;
            }            
            float curLevel = energy.CurLevelPercentage;
            CompGolem Golem = pawn.TryGetComp<CompGolem>();
            if (Golem != null && curLevel < Golem.energyPctShouldRest)
            {
                return 8f;
            }
            //Lord lord = pawn.GetLord();
            //if (lord != null && !lord.CurLordToil.AllowSatisfyLongNeeds)
            //{
            //    return 0f;
            //}
            //if ((int)energy.CurCategory > (int)minCategory)
            //{
            //    return 0f;
            //}
            //if (energy.CurLevelPercentage > maxLevelPercentage)
            //{
            //    return 0f;
            //}
            return 0f;         
            throw new NotImplementedException();
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            CompGolem Golem = pawn.TryGetComp<CompGolem>();
            Need_GolemEnergy energy = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
            if (energy == null || Golem == null || energy.CurLevelPercentage >= Golem.energyPctShouldRest || energy.CurLevelPercentage > maxLevelPercentage)
            {
                return null;
            }
            if (pawn.Downed)
            {
                return null;
            }
            if (pawn.jobs != null && pawn.jobs.curJob != null && pawn.jobs.curJob.def == JobDefOf.AttackMelee)
            {
                return null;
            }
            Pawn threat = TM_Calc.FindNearbyEnemy(pawn, Mathf.RoundToInt(Golem.threatRange));
            if(threat != null && Golem.TargetIsValid(pawn, threat))
            {
                return null;
            }
            if (Golem.dormantMap == pawn.Map && Golem.dormantPosition.Walkable(pawn.Map) && Golem.dormantPosition.Standable(pawn.Map))
            {
                Golem.shouldDespawn = true;
                return JobMaker.MakeJob(JobDefOf.Goto, Golem.dormantPosition);                
            }
            else
            {
                Golem.shouldDespawn = true;
                Golem.despawnNow = true;
                return null;
            }
        }
    } 
}
