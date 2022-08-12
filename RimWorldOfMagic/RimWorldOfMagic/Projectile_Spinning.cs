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
    class Projectile_Spinning : Projectile_AbilityBase
    {
        private int rotationOffset = 0;
        public int daggerCount = 2;

        public override void Tick()
        {
            base.Tick();
            if(Find.TickManager.TicksGame % 2 == 0 && daggerCount > 0 && this.launcher != null && this.launcher is Pawn caster)
            {
                CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                if(comp != null)
                {
                    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                    if((comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level >= 12) || (!caster.IsColonist && settingsRef.AIHardMode))
                    {
                        Projectile_Spinning newProjectile = (Projectile_Spinning)ThingMaker.MakeThing(this.def, null);
                        newProjectile.daggerCount = 0;
                        TM_CopyAndLaunchProjectile.CopyAndLaunchProjectile(newProjectile, caster, this.intendedTarget, this.intendedTarget, ProjectileHitFlags.All, null);
                        this.daggerCount--;
                    }
                    else
                    {
                        daggerCount = 0;
                    }
                }
                else
                {
                    daggerCount = 0;
                }
            }
        }

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

                    if (victim != null && comp != null && Rand.Chance(.8f))
                    {
                        TM_Action.DamageEntities(victim, null, this.def.projectile.GetDamageAmount(1, null) * comp.mightPwr, DamageDefOf.Cut, pawn);
                        TM_MoteMaker.ThrowBloodSquirt(victim.DrawPos, victim.Map, .8f);
                        if (comp.MightData.MightPowerSkill_FieldTraining.FirstOrDefault((MightPowerSkill x) => x.label == "TM_FieldTraining_pwr").level >= 3)
                        {
                            if (victim.IsPrisoner || victim.IsColonist)
                            {
                                LegShot(victim, Rand.Range(4, 6), TMDamageDefOf.DamageDefOf.TM_Tranquilizer);
                                LegShot(victim, Rand.Range(4, 6), TMDamageDefOf.DamageDefOf.TM_DisablingShot);
                            }
                            else if (victim.HostileTo(pawn.Faction))
                            {
                                HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_Poisoned_HD, Rand.Range(2, 4));
                            }
                        }                        
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
            this.rotationOffset += Rand.Range(20, 36);
            if(this.rotationOffset > 360)
            {
                this.rotationOffset = 0;
            }
            Mesh mesh = MeshPool.GridPlane(this.def.graphicData.drawSize);
            Graphics.DrawMesh(mesh, DrawPos, (Quaternion.AngleAxis(rotationOffset, Vector3.up) * ExactRotation), def.DrawMatSingle, 0);
            Comps_PostDraw();
        }

        public void LegShot(Pawn victim, int dmg, DamageDef dmgType)
        {
            BodyPartRecord vitalPart = null;
            if (!victim.Dead)
            {
                IEnumerable<BodyPartRecord> partSearch = victim.def.race.body.AllParts;
                if (Rand.Chance(.5f)) { vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore)); }
                else { vitalPart = partSearch.LastOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore)); }

                if (vitalPart != null)
                {
                    TM_Action.DamageEntities(victim, vitalPart, dmg, dmgType, this.launcher);
                }
                else
                {
                    vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.MovingLimbSegment));
                    if (vitalPart != null)
                    {
                        TM_Action.DamageEntities(victim, vitalPart, dmg, dmgType, this.launcher);
                    }
                    else
                    {
                        TM_Action.DamageEntities(victim, vitalPart, dmg, DamageDefOf.Cut, null);
                    }
                }
            }
        }

    }    
}


