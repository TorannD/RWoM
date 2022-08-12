using RimWorld;
using Verse;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Projectile_Headshot : Projectile_AbilityBase
    {

        public float destroyParentPartPctTo = 0.50f;
        Pawn pawn;

        private static int pwrVal;
        private int verVal;
        private float armorPen = 0f;

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            pawn = this.launcher as Pawn;
            Pawn victim = hitThing as Pawn;
            
            try
            {
                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_Headshot, "TM_Headshot", "_ver", true);
                //MightPowerSkill ver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Headshot.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Headshot_ver");
                //verVal = ver.level;
                //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    verVal = mver.level;
                //}
                verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_Headshot);
                CellRect cellRect = CellRect.CenteredOn(base.Position, 1);
                cellRect.ClipInsideMap(map);
                int dmg = GetWeaponDmg(pawn);
                armorPen = pawn.equipment.Primary.def.Verbs.FirstOrDefault().defaultProjectile.projectile.GetArmorPenetration(1f);

                if (victim != null && Rand.Chance(this.launcher.GetStatValue(StatDefOf.ShootingAccuracyPawn, true)))
                {
                    this.PenetratingShot(victim, dmg, this.def.projectile.damageDef);
                    if (victim.Dead)
                    {
                        comp.Stamina.CurLevel += (.1f * verVal);
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                //Log.Message("null error " + ex);
            }
        }

        public static int GetWeaponDmg(Pawn pawn)
        {
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            //pwrVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_Headshot, "TM_Headshot", "_pwr", true);
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_Headshot);

            float dmg = comp.weaponDamage;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (!pawn.IsColonist && settingsRef.AIHardMode)
            {
                dmg += 8;
            }

            dmg = Mathf.RoundToInt(dmg * (1f + (.1f * pwrVal)) * TorannMagicDefOf.TM_Headshot.weaponDamageFactor);
            return (int)Mathf.Clamp(dmg, 0, 100);
        }

        public void PenetratingShot(Pawn victim, int dmg, DamageDef dmgType)
        {
            BodyPartRecord vitalPart = null;
            if (victim != null && !victim.Dead)
            {
                IEnumerable<BodyPartRecord> partSearch = victim.def.race.body.AllParts;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource));
                if (vitalPart != null)
                {
                    this.HitBodyPartOrParent(victim, dmg, dmgType, vitalPart, 0);
                }
                else
                {
                    vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource));
                    if (vitalPart != null)
                    {
                        this.HitBodyPartOrParent(victim, dmg, dmgType, vitalPart, 0);
                    }
                    else
                    {
                        vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.Spine));
                        if (vitalPart != null)
                        {
                            this.HitBodyPartOrParent(victim, dmg, dmgType, vitalPart, 0);
                        }
                        else
                        {
                            Log.Message("did not find a vital organ, no extra damage applied");
                        }
                    }
                }
            }
        }

        public int HitBodyPartOrParent(Pawn victim, int dmg, DamageDef dmgType, BodyPartRecord hitPart, int penetratedParts)
        {
            BodyPartRecord parentPart = null;            
            if (hitPart.parent != null && hitPart.depth == BodyPartDepth.Inside)
            {
                parentPart = hitPart.parent;
                dmg = this.HitBodyPartOrParent(victim, dmg, dmgType, parentPart, penetratedParts + 1);
                if (penetratedParts == 0)
                {
                    damageEntities(victim, hitPart, dmg, dmgType, penetratedParts);
                    dmg = 0;
                }
                else
                {
                    float maxDmg = this.destroyParentPartPctTo * hitPart.def.GetMaxHealth(victim);
                    if (dmg > maxDmg)
                    {
                        damageEntities(victim, hitPart, (int)maxDmg , dmgType, penetratedParts);
                        dmg = dmg - (int)maxDmg;
                    }
                    else
                    {
                        damageEntities(victim, hitPart, dmg , dmgType, penetratedParts);
                        dmg = 0;
                    }                    
                }
                return dmg;
            }
            else
            {
                float maxDmg = (this.destroyParentPartPctTo * hitPart.def.GetMaxHealth(victim));
                if ( dmg > maxDmg )
                {
                    damageEntities(victim, hitPart, (int)maxDmg, dmgType, penetratedParts);
                    dmg = dmg - (int)maxDmg;                 
                }
                else
                {
                    damageEntities(victim, hitPart, dmg, dmgType, penetratedParts);
                    
                    dmg = 0;
                }                
                return dmg;
            }            
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type, int penetratedParts)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.5f, 1.5f));
            
            if (hitPart.def.GetMaxHealth(victim) > amt)
            {
                //Very large animals or creatures
                dinfo = new DamageInfo(type, amt, armorPen, (float)-1, pawn, hitPart, pawn.equipment.Primary.def, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            else
            {
                amt = (int)(amt / (1 + penetratedParts));
                dinfo = new DamageInfo(type, amt, armorPen, (float)-1, pawn, hitPart, pawn.equipment.Primary.def, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            dinfo.SetAllowDamagePropagation(false);
            //DamageWorker_AddInjury inj = new DamageWorker_AddInjury();
            //inj.Apply(dinfo, victim);
            victim.TakeDamage(dinfo);
            if (!victim.IsColonist && !victim.IsPrisoner && victim.Faction != null && !victim.Faction.HostileTo(pawn.Faction) && victim.Faction != this.launcher.Faction)
            {
                Faction faction = victim.Faction;
                faction.TryAffectGoodwillWith(pawn.Faction, -100, true, false, TorannMagicDefOf.TM_OffensiveMagic, null);
            }
        }
    }
}
