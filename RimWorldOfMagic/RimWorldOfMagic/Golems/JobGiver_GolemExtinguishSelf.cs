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
    public class JobGiver_GolemExtinguishSelf : ThinkNode_JobGiver
    {

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (Rand.Value < 0.5f)
            {
                Fire fire = (Fire)pawn.GetAttachment(ThingDefOf.Fire);
                if (fire != null)
                {
                    return JobMaker.MakeJob(JobDefOf.ExtinguishSelf, fire);
                }
            }
            return null;
        }    
    } 
}
