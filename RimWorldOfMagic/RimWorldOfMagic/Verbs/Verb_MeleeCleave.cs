using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;


namespace TorannMagic
{

    [StaticConstructorOnStartup]
    public class Verb_MeleeCleave : Verb_MeleeAttackDamage
    {
        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material cleavingMat = MaterialPool.MatFrom("Spells/cleave_straight", ShaderDatabase.Transparent, Verb_MeleeCleave.cleaveColor);

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {            
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            BodyPartGroupDef bodyPartGroupDef = null;
            HediffDef hediffDef = null;
            float num = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
            float armorPenetration = verbProps.AdjustedArmorPenetration(this, CasterPawn);
            ThingDef source = (base.EquipmentSource == null) ? CasterPawn.def : base.EquipmentSource.def;
            bodyPartGroupDef = verbProps.AdjustedLinkedBodyPartsGroup(tool);
            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, (num * .6f), armorPenetration, -1f, this.CasterPawn, null, source);            
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
                    DrawCleaving(cleaveVictim, base.CasterPawn, 10);
                }
            }
            TM_MoteMaker.ThrowCrossStrike(target.Thing.Position.ToVector3Shifted(), target.Thing.Map, 1f);
            TM_MoteMaker.ThrowBloodSquirt(target.Thing.Position.ToVector3Shifted(), target.Thing.Map, 1f);
            target.Thing.TakeDamage(dinfo);
            if(target != null && !target.Thing.DestroyedOrNull())
            {
                base.ApplyMeleeDamageToTarget(target);
            }
            return damageResult;
        }

        private void DrawCleaving(Pawn cleavedPawn, Pawn caster, int magnitude)
        {
            bool flag = !caster.Dead && !caster.Downed;
            if (flag)
            {
                Vector3 vector = cleavedPawn.Position.ToVector3();
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float hyp = Mathf.Sqrt((Mathf.Pow(caster.Position.x - cleavedPawn.Position.x, 2)) + (Mathf.Pow(caster.Position.z - cleavedPawn.Position.z, 2)));
                float angleRad = Mathf.Asin(Mathf.Abs(caster.Position.x - cleavedPawn.Position.x) / hyp);
                float angle = Mathf.Rad2Deg * angleRad;
                //float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(3f, 3f, 5f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Verb_MeleeCleave.cleavingMat, 0);
            }
        }
    }
}
