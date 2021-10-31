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
    public class JobGiver_MechaGolemExtinguish : ThinkNode_JobGiver
    {

        protected override Job TryGiveJob(Pawn pawn)
        {
            CompGolem cg = pawn.TryGetComp<CompGolem>();
            bool flag = false;
            foreach(TM_GolemUpgrade gu in cg.Upgrades)
            {
                if(gu.golemUpgradeDef.label.ToLower() == "extinguisher" && gu.currentLevel > 0)
                {
                    GenExplosion.DoExplosion(pawn.Position, pawn.Map, Rand.Range(4f, 6f), DamageDefOf.Extinguish, pawn, 500, 0, SoundDefOf.Artillery_ShellLoaded);
                    flag = true;
                }                
            }
            if(!flag)
            {
                Fire fire = (Fire)pawn.GetAttachment(ThingDefOf.Fire);
                if (fire != null)
                {
                    fire.Destroy(DestroyMode.Vanish);
                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, .8f);
                }
            }
            return null;
        }
    } 
}
