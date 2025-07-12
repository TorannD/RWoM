using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic
{

    [StaticConstructorOnStartup]
    public class Verb_MeleeSlam : Verb_MeleeAttackDamage
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
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, (num * .6f), armorPenetration, -1f, this.CasterPawn, null, source);            
            damageResult.totalDamageDealt = Mathf.Min((float)target.Thing.HitPoints, dinfo.Amount);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EarthCrack, target.Cell.ToVector3Shifted(), this.CasterPawn.Map, 2.2f, .25f, .25f, 1.75f, 0, 0, 0, Rand.Range(0, 360));
            for (int i = 0; i < 8; i++)
            {
                IntVec3 intVec = target.Cell + GenAdj.AdjacentCells[i];
                Pawn cleaveVictim = new Pawn();
                cleaveVictim = intVec.GetFirstPawn(target.Thing.Map);
                float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(target.Cell, intVec)).ToAngleFlat();
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.DustPuffThick, target.CenterVector3, CasterPawn.Map, Rand.Range(.5f, .8f), Rand.Range(.3f, .5f), .1f, Rand.Range(.3f, .5f), 0, 1f, angle, Rand.Range(0, 360));

                if (cleaveVictim != null && cleaveVictim.Faction != caster.Faction)
                {
                    Vector3 direction = (cleaveVictim.Position - CasterPawn.Position).ToVector3();
                    dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                    dinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
                    dinfo.SetWeaponHediff(hediffDef);
                    dinfo.SetAngle(direction);
                    cleaveVictim.TakeDamage(dinfo);
                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), target.Thing.Map);
                }
            }
            FleckMaker.ThrowMicroSparks(target.Thing.Position.ToVector3Shifted(), target.Thing.Map);
            if(target != null && !target.Thing.DestroyedOrNull())
            {
                base.ApplyMeleeDamageToTarget(target);
            }
            return damageResult;
        }
    }
}
