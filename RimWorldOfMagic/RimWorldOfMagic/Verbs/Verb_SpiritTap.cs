using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic
{

    public class Verb_SpiritTap : Verb_MeleeAttack
    {

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            float damageVal = Rand.Range(20f, 40f);
            DamageInfo dinfo = new DamageInfo(this.maneuver.verb.meleeDamageDef, (int)(this.tool.power/damageVal), 2, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            damageResult.hitThing = target.Thing;
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            float angle = (Quaternion.AngleAxis(-90, Vector3.up)*GetVector(this.CasterPawn.Position, target.Thing.Position)).ToAngleFlat();
            for (int i = 0; i < 4; i++)
            {
                Vector3 startPos = target.Thing.DrawPos;
                startPos.x += Rand.Range(-.3f, .3f);
                startPos.z += Rand.Range(-.3f, .3f);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, startPos, target.Thing.Map, Rand.Range(.8f, 1f), .15f, .05f, .1f, 0, 5, (angle + Rand.Range(-20, 20)), Rand.Range(0, 360));
            }
            if (target.Pawn != null && !TM_Calc.IsGolem(target.Pawn) && !TM_Calc.IsUndead(target.Pawn) && !TM_Calc.IsRobotPawn(target.Pawn))
            {
                HealthUtility.AdjustSeverity(target.Pawn, TorannMagicDefOf.TM_SpiritDrainHD, this.tool.power/(damageVal*10f));
                Need_Spirit nd = this.CasterPawn.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                if(nd != null)
                {
                    nd.GainNeed(damageVal * .03f);
                    if (target.Pawn.Dead)
                    {
                        nd.GainNeed(Rand.Range(8f, 12f));
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, this.CasterPawn.DrawPos, this.CasterPawn.Map, Rand.Range(.4f, .6f), .1f, .05f, .05f, 0, Rand.Range(1, 2), 0, 0);
                    }
                }                
            }
            return damageResult;  
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }
}
