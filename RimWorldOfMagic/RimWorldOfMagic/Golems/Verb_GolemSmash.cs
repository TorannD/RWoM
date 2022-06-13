using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;


namespace TorannMagic.Golems
{

    public class Verb_GolemSmash : Verb_MeleeAttack
    {
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();            

            if (this.CasterPawn.Map != null)
            {
                List<IntVec3> targetCells = GenRadial.RadialCellsAround(target.Cell, 2, true).ToList();
                IntVec3 curCell = default(IntVec3);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EarthCrack, target.Cell.ToVector3Shifted(), this.CasterPawn.Map, 2.5f, .25f, .25f, 1.75f, 0, 0, 0, Rand.Range(0, 360));
                Find.CameraDriver.shaker.DoShake(.02f);
                if (targetCells != null && targetCells.Count > 0)
                {
                    for (int i = 0; i < targetCells.Count; i++)
                    {
                        curCell = targetCells[i];                        
                        if (curCell.IsValid && curCell.InBoundsWithNullCheck(CasterPawn.Map))
                        {
                            float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(target.Cell, curCell)).ToAngleFlat();
                            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.DustPuffThick, target.CenterVector3, CasterPawn.Map, Rand.Range(.5f, .8f), Rand.Range(.3f, .5f), .1f, Rand.Range(.3f, .5f), 0, 1.5f, angle, Rand.Range(0, 360));
                            List<Thing> thingList = curCell.GetThingList(CasterPawn.Map);
                            if (thingList != null && thingList.Count > 0)
                            {
                                for (int j = 0; j < thingList.Count; j++)
                                {
                                    Pawn p = thingList[j] as Pawn;
                                    if (p != null)
                                    {
                                        if (p.Faction.HostileTo(CasterPawn.Faction) || p.Faction == null)
                                        {
                                            TM_Action.DamageEntities(thingList[j], null, this.tool.power, DamageDefOf.Blunt, CasterPawn);
                                        }
                                    }
                                    else
                                    {
                                        TM_Action.DamageEntities(thingList[j], null, this.tool.power, DamageDefOf.Blunt, CasterPawn);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return damageResult;  
        }
    }
}
