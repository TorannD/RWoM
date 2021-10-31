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
    public class JobGiver_GolemWanderNearMaster : JobGiver_Wander
    {
        public JobGiver_GolemWanderNearMaster()
        {
            wanderRadius = 3f;
            ticksBetweenWandersRange = new IntRange(125, 200);
            wanderDestValidator = delegate (Pawn p, IntVec3 c, IntVec3 root)
            {
                if (root.GetRoom(p.Map) != null && !WanderRoomUtility.IsValidWanderDest(p, c, root))
                {
                    return false;
                }
                return true;
            };
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            return WanderUtility.BestCloseWanderRoot(pawn.TryGetComp<CompGolem>().pawnMaster.Position, pawn);
        }

    } 
}
