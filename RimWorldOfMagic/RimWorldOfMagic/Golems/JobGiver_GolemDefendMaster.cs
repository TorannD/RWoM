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
    public class JobGiver_GolemDefendMaster : JobGiver_AIDefendPawn
    {
        protected override Pawn GetDefendee(Pawn pawn)
        {
            return pawn.TryGetComp<CompGolem>().pawnMaster;
        }

        protected override float GetFlagRadius(Pawn pawn)
        {
            return pawn.TryGetComp<CompGolem>().threatRange;
        }
    } 
}
