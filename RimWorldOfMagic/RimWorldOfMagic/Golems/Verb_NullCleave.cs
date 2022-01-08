using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic.Golems
{

    public class Verb_NullCleave : Verb_MeleeAttack
    {

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Shadow, (int)(this.tool.power), 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            damageResult.hitThing = target.Thing;
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.CasterPawn.Position, target.Thing.Position)).ToAngleFlat();
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ShadowCleave, CasterPawn.DrawPos, target.Thing.Map, Rand.Range(1f, 1.25f), .1f, .05f, .1f, 0, 7f, angle, angle);
            for (int i = 0; i < 8; i++)
            {
                IntVec3 intVec = target.Cell + GenAdj.AdjacentCells[i];
                Pawn cleaveVictim = new Pawn();
                cleaveVictim = intVec.GetFirstPawn(target.Thing.Map);
                if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction)
                {
                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Shadow, (int)(this.tool.power * .6f), 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    cleaveVictim.TakeDamage(dinfo2);
                    TM_MoteMaker.ThrowBloodSquirt(cleaveVictim.Position.ToVector3Shifted(), cleaveVictim.Map, Rand.Range(.5f, .8f));
                    TM_MoteMaker.ThrowBloodSquirt(cleaveVictim.Position.ToVector3Shifted(), cleaveVictim.Map, Rand.Range(.5f, .8f));
                }
            }
            TM_MoteMaker.ThrowBloodSquirt(target.Thing.Position.ToVector3Shifted(), target.Thing.Map, 1.2f);
            target.Thing.TakeDamage(dinfo);
            return damageResult;
        }
    }
}
