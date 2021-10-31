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
    public class JobGiver_GolemFollowMaster : JobGiver_AIFollowPawn
    {
        protected override Pawn GetFollowee(Pawn pawn)
        {
            return pawn.TryGetComp<CompGolem>().pawnMaster;
        }

        protected override float GetRadius(Pawn pawn)
        {
            return pawn.TryGetComp<CompGolem>().threatRange;
        }
    } 
}
