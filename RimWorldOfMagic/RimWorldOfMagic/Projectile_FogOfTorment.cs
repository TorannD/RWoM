﻿using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using TorannMagic.ModOptions;

namespace TorannMagic
{
    class Projectile_FogOfTorment : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 1440;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 120;
        int lastStrike = 0;
        bool initialized = false;
        ThingDef fog;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            Pawn pawn = this.launcher as Pawn;
            Pawn victim = null;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if (comp != null)
            {
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FogOfTorment.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_FogOfTorment_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FogOfTorment.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_FogOfTorment_ver");
                pwrVal = pwr.level;
                verVal = ver.level;
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                if (Settings.Instance.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
            }
            else if(pawn.def == TorannMagicDefOf.TM_SkeletonLichR)
            {
                pwrVal = Rand.RangeInclusive(0,3);
                verVal = Rand.RangeInclusive(0,3);
            }

            if (!this.initialized)
            {
                fog = TorannMagicDefOf.Fog_Torment;
                this.duration = this.duration + (180 * verVal);
                this.strikeDelay = this.strikeDelay - (18 * verVal);

                fog.gas.expireSeconds.min = this.duration/60;
                fog.gas.expireSeconds.max = this.duration/60;
                GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius + verVal, TMDamageDefOf.DamageDefOf.TM_Torment, this.launcher, 0, 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, fog, 1f, 1, null, false, null, 0f, 0, 0.0f, false);
                
                this.initialized = true;
            }

            if (this.age > this.lastStrike + this.strikeDelay)
            {
                IntVec3 curCell;
                IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, this.def.projectile.explosionRadius + verVal, true);
                for (int i = 0; i < targets.Count(); i++)
                {
                    curCell = targets.ToArray<IntVec3>()[i];

                    if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                    {
                        victim = curCell.GetFirstPawn(map);
                        if(victim != null && !victim.Dead && victim.RaceProps.IsFlesh)
                        {
                            if(TM_Calc.IsUndead(victim) || victim.needs.food == null)
                            {
                                //heals undead
                                Hediff_Injury injuryToHeal = victim.health.hediffSet.hediffs
                                    .OfType<Hediff_Injury>()
                                    .FirstOrDefault(injury => injury.CanHealNaturally());
                                injuryToHeal?.Heal(2.0f + pwrVal);

                            }
                            else
                            {
                                //kills living
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(pawn, victim) - .4f))
                                {
                                    float targetMassFactor = victim.def.BaseMass != 0 ? 50f/victim.def.BaseMass : 1f;                                    
                                    damageEntities(victim, Mathf.RoundToInt(Rand.Range(2f + (1f * pwrVal), 4f + (1f * pwrVal)) * this.arcaneDmg * targetMassFactor), TMDamageDefOf.DamageDefOf.TM_Torment);
                                }
                            }
                        }
                    }
                }
                this.lastStrike = this.age;
            }
        }            

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1440, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "shockDelay", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrike, "lastStrike", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Defs.Look<ThingDef>(ref this.fog, "fog");
        }

        public void damageEntities(Pawn e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.5f, 1.5f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            if (this.launcher != null && this.launcher is Pawn caster)
            {
                dinfo = new DamageInfo(type, amt, 0, (float)-1, caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown);                
            }
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
            
        }
    }    
}


