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
    public class JobGiver_DraftedGolemRangedAttack : ThinkNode_JobGiver
    {
        public Job TryGetJob(Pawn p)
        {
            return TryGiveJob(p);
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            TMPawnGolem pg = pawn as TMPawnGolem;
            if (pg != null && pg.Drafted && !pg.rangedToggle)
            {                
                CompGolem Golem = pawn.TryGetComp<CompGolem>();
                Need_GolemEnergy energy = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy) as Need_GolemEnergy;
                Thing threat = Golem.ActiveThreat;
                if (threat == null)
                {
                    threat = pawn.TargetCurrentlyAimingAt.Thing;
                    if (threat == null)
                    {
                        threat = TM_Calc.FindNearbyEnemy(pawn, Mathf.RoundToInt(Golem.threatRange * 3));
                    }
                }
                Golem.threatTarget = threat;
                if (pg.verbCommands != null && threat != null && pg.ValidRangedVerbs().Count > 0)
                {
                    Verb v = pg.GetBestVerb;
                    if (v != null && (pg.Position - threat.Position).LengthHorizontal > v.verbProps.minRange)
                    {
                        return TM_GolemUtility.CreateRangedJob(pg, threat, v);
                    }
                }
                             
            }
            return null;
        }
    } 
}
