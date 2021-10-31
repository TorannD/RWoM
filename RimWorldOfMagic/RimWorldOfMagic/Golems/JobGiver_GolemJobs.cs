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
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class JobGiver_GolemJobs : ThinkNode_JobGiver
    {
        private List<TM_GolemAbilityDef> jobAbilities = null;
        IEnumerable<Thing> workThings;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_GolemJobs obj = (JobGiver_GolemJobs)base.DeepCopy(resolve);
            obj.jobAbilities = jobAbilities;
            obj.workThings = workThings;
            return obj;
        }

        public override float GetPriority(Pawn pawn)
        {
            jobAbilities = GolemAbilityWorker.JobAbilities(pawn);            
            Need_GolemEnergy energy = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
            if (jobAbilities == null)
            {
                return 0f;
            }
            if (jobAbilities.Count < 1)
            {
                return 0f;
            }            
            if (energy.CurLevel < energy.ActualNeedCost(jobAbilities[0].needCost))
            {
                return 0f;
            }
            if (energy.CurCategory == GolemEnergyCategory.Critical)
            {
                return 0f;
            }
            return 4f;        
            throw new NotImplementedException();
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            CompGolem Golem = pawn.TryGetComp<CompGolem>();
            Need_GolemEnergy energy = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
            if(Golem.shouldDespawn)
            {
                return null;
            }
            if (energy == null || Golem == null)
            {
                return null;
            }
            if (pawn.Downed || pawn.Map == null)
            {
                return null;
            }
            jobAbilities = GolemAbilityWorker.JobAbilities(pawn);
            if (jobAbilities == null)
            {
                return null;
            }
            if (jobAbilities.Count < 1)
            {
                return null;
            }
            if(energy.CurLevel < energy.ActualNeedCost(jobAbilities[0].needCost))
            {
                return null;
            }
            workThings = GolemAbilityWorker.PotentialAvailableWorkThingsForJob(pawn, jobAbilities[0].jobDef);
            if (workThings == null)
            {
                return null;
            }
            if (workThings.Count() < 1)
            {
                return null;
            }
            Thing jobThing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, workThings, PathEndMode.ClosestTouch, TraverseParms.For(pawn));
            if (jobThing != null)
            {
                foreach (TM_GolemAbilityDef job in jobAbilities)
                {
                    if (job.jobDef == TorannMagicDefOf.JobDriver_MechaMine)
                    {
                        //FleckMaker.ThrowLightningGlow(jobThing.DrawPos, jobThing.Map, 1.5f);
                        return JobMaker.MakeJob(TorannMagicDefOf.JobDriver_MechaMine, jobThing);
                    }
                    if (job.jobDef == TorannMagicDefOf.JobDriver_FleshHarvest)
                    {
                        //FleckMaker.ThrowLightningGlow(jobThing.DrawPos, jobThing.Map, 1.5f);
                        return JobMaker.MakeJob(TorannMagicDefOf.JobDriver_FleshHarvest, jobThing);
                    }
                    if (job.jobDef == TorannMagicDefOf.JobDriver_FleshChop)
                    {
                        //FleckMaker.ThrowLightningGlow(jobThing.DrawPos, jobThing.Map, 1.5f);
                        return JobMaker.MakeJob(TorannMagicDefOf.JobDriver_FleshChop, jobThing);
                    }
                }
                return null;                               
            }
            return null;
        }
    } 
}
