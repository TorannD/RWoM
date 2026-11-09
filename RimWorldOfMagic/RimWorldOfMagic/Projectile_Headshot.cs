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
    public class Projectile_Headshot : Projectile_SniperRound
    {

        public float destroyParentPartPctTo = 0.50f;
        Pawn pawn;

        protected override void CaptureWeaponDamage(Pawn body) => shotDamage = WeaponDamage(skillOwner,PhysicalWeaponDamage(body),powerRank);

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            pawn = launcher as Pawn;
            base.Impact(hitThing,blockedByShield);
            if (blockedByShield || (!shotBound && pawn == null) || !(hitThing is Pawn victim) || victim.Dead) return;
            if (!shotBound)
            {
                var weapon = pawn.equipment?.Primary;
                var projectile = weapon?.TryGetComp<CompEquippable>()?.PrimaryVerb?.GetProjectile()?.projectile;
                if (projectile == null || pawn.GetCompAbilityUserMight() == null) return;
                firedWeapon = weapon.def;
                armorPen = projectile.GetArmorPenetration();
                accuracy = pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn);
                versatility = TM_Calc.GetSkillVersatilityLevel(pawn,TorannMagicDefOf.TM_Headshot);
                shotDamage = GetWeaponDmg(pawn);
            }
            if (!Rand.Chance(accuracy)) return;
            PenetratingShot(victim,shotDamage,def.projectile.damageDef);
            var owner = shotBound ? skillOwner : pawn;
            if (victim.Dead && owner?.Dead == false && owner.GetCompAbilityUserMight()?.Stamina is Need_Stamina stamina)
                stamina.CurLevel += .1f * versatility;
        }

        public static int GetWeaponDmg(Pawn pawn) => WeaponDamage(pawn,pawn.GetCompAbilityUserMight().weaponDamage,TM_Calc.GetSkillPowerLevel(pawn,TorannMagicDefOf.TM_Headshot));

        internal static int WeaponDamage(Pawn owner,float damage,int rank)
        {
            if (!owner.IsColonist && ModOptions.Settings.Instance.AIHardMode) damage += 8f;
            return Mathf.Clamp(Mathf.RoundToInt(damage * (1f + .1f * rank) * TorannMagicDefOf.TM_Headshot.weaponDamageFactor),0,100);
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
                dinfo = new DamageInfo(type, amt, armorPen, (float)-1, EffectOwner, hitPart, firedWeapon ?? pawn?.equipment?.Primary?.def, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            else
            {
                amt = (int)(amt / (1 + penetratedParts));
                dinfo = new DamageInfo(type, amt, armorPen, (float)-1, EffectOwner, hitPart, firedWeapon ?? pawn?.equipment?.Primary?.def, DamageInfo.SourceCategory.ThingOrUnknown);
            }
            dinfo.SetAllowDamagePropagation(false);
            //DamageWorker_AddInjury inj = new DamageWorker_AddInjury();
            //inj.Apply(dinfo, victim);
            victim.TakeDamage(dinfo);
            ApplyFriendlyFireGoodwill(victim,-100);
        }
    }
}
