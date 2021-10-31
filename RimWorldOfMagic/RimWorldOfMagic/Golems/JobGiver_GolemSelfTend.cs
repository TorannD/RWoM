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
    public class JobGiver_GolemSelfTend : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn is TMPawnGolem) || !pawn.health.HasHediffsNeedingTend() || pawn.InAggroMentalState || pawn.health.hediffSet.BleedRateTotal <= 0)
            {
                return null;
            }
            else
            {
                Job job = JobMaker.MakeJob(TorannMagicDefOf.JobDriver_GolemSelfTend, pawn);
                job.endAfterTendedOnce = true;
                return job;
            }
        }
    } 
}
