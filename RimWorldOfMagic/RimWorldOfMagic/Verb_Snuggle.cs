using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic
{

    public class Verb_Snuggle: Verb_MeleeAttackDamage
    {
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            BodyPartGroupDef bodyPartGroupDef = null;
            HediffDef hediffDef = null;
            float num = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
            float armorPenetration = verbProps.AdjustedArmorPenetration(this, CasterPawn);
            ThingDef source = (base.EquipmentSource == null) ? CasterPawn.def : base.EquipmentSource.def;
            bodyPartGroupDef = verbProps.AdjustedLinkedBodyPartsGroup(tool);
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Scratch, num, armorPenetration, -1f, this.CasterPawn, null, source);
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            for (int i = 0; i < 8; i++)
            {
                IntVec3 intVec = target.Cell + GenAdj.AdjacentCells[i];
                Pawn cleaveVictim = new Pawn();
                cleaveVictim = intVec.GetFirstPawn(target.Thing.Map);
                if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction)
                {
                    Vector3 direction = (cleaveVictim.Position - CasterPawn.Position).ToVector3();
                    dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                    dinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
                    dinfo.SetWeaponHediff(hediffDef);
                    dinfo.SetAngle(direction);
                    dinfo.SetAmount(Rand.Range(.8f, 1.2f) * num * .6f);
                    cleaveVictim.TakeDamage(dinfo);
                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), target.Thing.Map);
                    TM_MoteMaker.ThrowCrossStrike(cleaveVictim.Position.ToVector3Shifted(), cleaveVictim.Map, 1f);
                    TM_MoteMaker.ThrowBloodSquirt(cleaveVictim.Position.ToVector3Shifted(), cleaveVictim.Map, 1f);
                }
            }

            target.Thing.TakeDamage(dinfo);
            if (target != null && !target.Thing.DestroyedOrNull())
            {
                base.ApplyMeleeDamageToTarget(target);
                if (target.Thing.Map != null)
                {
                    TM_MoteMaker.ThrowCrossStrike(target.Thing.Position.ToVector3Shifted(), target.Thing.Map, .5f);
                    TM_MoteMaker.ThrowBloodSquirt(target.Thing.Position.ToVector3Shifted(), target.Thing.Map, 1.2f);
                }
            }
            return damageResult;  
        }
    }
}
