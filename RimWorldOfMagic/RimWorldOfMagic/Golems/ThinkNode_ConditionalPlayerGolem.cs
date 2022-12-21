using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class ThinkNode_ConditionalPlayerGolem : ThinkNode_Conditional
    {
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (Satisfied(pawn) == !invert)
            {
                return base.TryIssueJobPackage(pawn, jobParams);
            }
            return ThinkResult.NoJob;
        }

        protected override bool Satisfied(Pawn pawn)
        {
            return (pawn.Faction == Faction.OfPlayer && pawn.Spawned && pawn.Map != null);
        }
    }
}
