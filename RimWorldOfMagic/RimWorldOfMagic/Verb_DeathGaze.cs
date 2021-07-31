using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic
{

    public class Verb_DeathGaze : Verb_MeleeAttack
    {

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Stun, (int)(this.tool.power), 1, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            damageResult.hitThing = target.Thing;
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            for (int i = 0; i < 8; i++)
            {
                IntVec3 intVec = target.Cell + GenAdj.AdjacentCells[i];
                Pawn cleaveVictim = new Pawn();
                cleaveVictim = intVec.GetFirstPawn(target.Thing.Map);
                float angle = (Quaternion.AngleAxis(90, Vector3.up) * GetVector(this.CasterPawn.Position, intVec)).ToAngleFlat();
                if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction)
                {
                    DamageInfo dinfo2 = new DamageInfo(DamageDefOf.Stun, (int)(this.tool.power * .6f), 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    cleaveVictim.TakeDamage(dinfo2);
                }
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Bolt, this.CasterPawn.DrawPos, this.CasterPawn.Map, Rand.Range(.9f, 1.2f), .15f, .05f, .25f, 0, Rand.Range(3f, 4f), (angle + Rand.Range(-10, 10)), angle);
            }
            target.Thing.TakeDamage(dinfo);
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
