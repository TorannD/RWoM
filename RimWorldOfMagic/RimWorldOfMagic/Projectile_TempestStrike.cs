using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class Projectile_TempestStrike : Projectile_AbilityBase
    {
        private int rotationOffset = 0;
        public bool shouldSpin = true;
        private bool spinCheck = true;

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            Pawn pawn = this.launcher as Pawn;
            base.Impact(hitThing);
            ThingDef def = this.def;
            try
            {
                if (pawn != null)
                {
                    Pawn victim = hitThing as Pawn;
                    CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();

                    if (victim != null && comp != null)
                    {
                        TM_Action.DamageEntities(victim, null, GetWeaponDmg(pawn), this.def.projectile.damageDef, pawn);
                        TM_MoteMaker.ThrowBloodSquirt(victim.DrawPos, victim.Map, .8f);
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                //
            }
        }

        public override void Draw()
        {
            if (spinCheck)
            {
                if (this.launcher is Pawn pawn)
                {
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps weaponComp = pawn.equipment.Primary;
                        if(weaponComp.def.IsRangedWeapon)
                        {
                            shouldSpin = false;
                        }
                    }
                    spinCheck = false;
                }
            }
            if (shouldSpin)
            {
                this.rotationOffset += 49;
            }
            if (this.rotationOffset > 360)
            {
                this.rotationOffset = this.rotationOffset - 360;
            }
            Mesh mesh = MeshPool.GridPlane(this.def.graphicData.drawSize);
            Graphics.DrawMesh(mesh, DrawPos, (Quaternion.AngleAxis(rotationOffset, Vector3.up) * ExactRotation), def.DrawMatSingle, 0);
            
            Comps_PostDraw();
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            if (pawn != null)
            {
                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                if (comp != null)
                {
                    float dmgNum = 0;                    
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps weaponComp = pawn.equipment.Primary;
                        if (weaponComp.def.IsMeleeWeapon)
                        {
                            dmgNum = comp.weaponDamage * TorannMagicDefOf.TM_TempestStrike.weaponDamageFactor;
                        }
                        if (weaponComp.def.IsRangedWeapon)
                        {                            
                            float weaponDPS = comp.weaponDamage;
                            int shots = Mathf.Clamp(weaponComp.def.Verbs.FirstOrDefault().burstShotCount, 1, 5);
                            float shotMultiplier = 1f - ((float)shots / 15f);

                            dmgNum = weaponDPS * shotMultiplier * TorannMagicDefOf.TM_TempestStrike.weaponDamageFactor;
                        }
                    }
                    else
                    {
                        dmgNum = comp.weaponDamage * TorannMagicDefOf.TM_TempestStrike.weaponDamageFactor;
                    }

                    if (comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level >= 8)
                    {
                        dmgNum = dmgNum * 1.2f;
                    }
                    
                    return Mathf.RoundToInt(dmgNum);
                }
                return 0;
            }
            return 0;
        }

    }    
}


