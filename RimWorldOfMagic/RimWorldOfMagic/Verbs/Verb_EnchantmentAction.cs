using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic
{

    public class Verb_EnchantmentAction : Verb_MeleeAttack
    {

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, (int)(this.tool.power), 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            damageResult.hitThing = target.Thing;
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            float angle = (Quaternion.AngleAxis(90, Vector3.up)*TM_Calc.GetVector(this.CasterPawn.Position, target.Thing.Position)).ToAngleFlat();
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ClawSweep, target.CenterVector3, target.Thing.Map, Rand.Range(1.2f, 1.4f), .15f, .05f, .1f, 0, Rand.Range(.2f, .4f), (angle + Rand.Range(-10,10)), (angle + Rand.Range(-10, 10)));
            for (int i = 0; i < 8; i++)
            {
                IntVec3 intVec = target.Cell + GenAdj.AdjacentCells[i];
                Pawn cleaveVictim = new Pawn();
                cleaveVictim = intVec.GetFirstPawn(target.Thing.Map);
                if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction)
                {
                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, (int)(this.tool.power * .6f), 0, (float)-1, this.CasterPawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    cleaveVictim.TakeDamage(dinfo2);
                    TM_MoteMaker.ThrowBloodSquirt(cleaveVictim.Position.ToVector3Shifted(), cleaveVictim.Map, Rand.Range(.6f, 1f));
                    TM_MoteMaker.ThrowBloodSquirt(cleaveVictim.Position.ToVector3Shifted(), cleaveVictim.Map, Rand.Range(.6f, 1f));
                }
            }
            TM_MoteMaker.ThrowBloodSquirt(target.Thing.Position.ToVector3Shifted(), target.Thing.Map, 1.2f);
            target.Thing.TakeDamage(dinfo);
            return damageResult;  
        }

    }

}
