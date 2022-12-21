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
    public class JobGiver_GolemDespawning : ThinkNode_JobGiver
    {

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!(pawn.Spawned && pawn.Map != null))
            {
                return JobMaker.MakeJob(TorannMagicDefOf.JobDriver_GolemDespawnWait, pawn);                
            }
            return null;
        }    
    } 
}
