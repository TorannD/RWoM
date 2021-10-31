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
    public class JobGiver_GolemForceGotoDespawn : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            CompGolem cg = pawn.TryGetComp<CompGolem>();
            if(cg == null)
            {
                return null;
            }
            if(!cg.shouldDespawn)
            {
                return null;
            }
            if (!cg.dormantPosition.IsValid)
            {
                return null;
            }
            if (pawn.CanReach(cg.dormantPosition, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                Job job = JobMaker.MakeJob(TorannMagicDefOf.JobDriver_GolemDespawn, cg.dormantPosition);
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                return job;
            }
            cg.shouldDespawn = false;
            return null;
        }
    } 
}
