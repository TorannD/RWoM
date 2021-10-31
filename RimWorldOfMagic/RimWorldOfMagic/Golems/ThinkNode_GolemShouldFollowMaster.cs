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
    public class ThinkNode_GolemShouldFollowMaster : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return ShouldFollowMaster(pawn);
        }

        public static bool ShouldFollowMaster(Pawn pawn)
        {
            CompGolem cg = pawn.TryGetComp<CompGolem>();
            if(cg == null)
            {
                return false;
            }
            if (!pawn.Spawned || pawn.playerSettings == null)
            {
                return false;
            }
            Pawn respectedMaster = cg.pawnMaster;
            if (respectedMaster == null)
            {
                return false;
            }
            if (respectedMaster.Spawned)
            {
                if (cg.followsMasterDrafted && respectedMaster.Drafted && pawn.CanReach(respectedMaster, PathEndMode.OnCell, Danger.Deadly))
                {
                    return true;
                }
                if (cg.followsMaster && respectedMaster.mindState.lastJobTag == JobTag.Fieldwork && pawn.CanReach(respectedMaster, PathEndMode.OnCell, Danger.Deadly))
                {
                    return true;
                }
            }
            else
            {
                Pawn carriedBy = respectedMaster.CarriedBy;
                if (carriedBy != null && carriedBy.HostileTo(respectedMaster) && pawn.CanReach(carriedBy, PathEndMode.OnCell, Danger.Deadly))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
