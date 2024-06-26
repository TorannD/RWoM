﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    class Verb_ConsumeCorpse_Mass : Verb_UseAbility
    {

        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt) 
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {

            Pawn caster = this.CasterPawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            MagicPowerSkill eff = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ConsumeCorpse.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ConsumeCorpse_eff");
            MagicPowerSkill ver = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ConsumeCorpse.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ConsumeCorpse_ver");
            MagicPowerSkill manaRegen = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_regen.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_global_regen_pwr");

            float radius = this.verbProps.defaultProjectile.projectile.explosionRadius;
            List<Thing> rangeThings = this.caster.Map.listerThings.AllThings.Where((Thing x) => (x.Position - currentTarget.Cell).LengthHorizontal <= radius).ToList();
            List<Thing> consumeThings = new List<Thing>();
            foreach(Thing t in rangeThings)
            {
                if (t is Pawn undead)
                {
                    if (!undead.Dead)
                    {
                        if (TM_Calc.IsUndead(undead))
                        {
                            consumeThings.Add(t);                            
                        }                        
                    }
                }
                else if(t is Corpse corpse)
                {
                    Pawn undeadPawn = corpse.InnerPawn;
                    if ((undeadPawn.RaceProps.IsFlesh || TM_Calc.IsUndead(undeadPawn)) && (!TM_Calc.IsRobotPawn(undeadPawn)))
                    {
                        consumeThings.Add(t);                        
                    }
                }
            }

            float num = 0;
            float dmgBonus = 1f + (ver.level * .15f);
            foreach (Thing t in consumeThings)
            {                
                float rpct = Mathf.Clamp((1f - (num * .1f)), .1f, 1f);                
                if (t is Pawn undead)
                {
                    if (undead.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD))
                    {
                        comp.Mana.CurLevel += ((.225f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                        ConsumeHumanoid(undead);
                        if (ver.level > 0)
                        {
                            HealCaster(caster, 2 + ver.level, 2, (5f + ver.level) * comp.arcaneDmg);
                        }
                        undead.Destroy();
                    }
                    else if (undead.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD))
                    {
                        comp.Mana.CurLevel += ((.18f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                        ConsumeAnimalKind(undead);
                        if (ver.level > 0)
                        {
                            HealCaster(caster, 2, 2, (3 + ver.level) * comp.arcaneDmg);
                        }
                        undead.Destroy();
                    }
                    else if (undead.def == TorannMagicDefOf.TM_SkeletonLichR)
                    {
                        comp.Mana.CurLevel += ((.15f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                        TM_Action.DamageUndead(undead, Rand.Range(10, 20) * dmgBonus, caster);
                    }
                    else if (undead.def == TorannMagicDefOf.TM_GiantSkeletonR || undead.def == TorannMagicDefOf.TM_SkeletonR)
                    {
                        TM_Action.DamageUndead(undead, Rand.Range(15, 30) * dmgBonus, caster);
                    }
                    else
                    {
                        TM_Action.DamageUndead(undead, Rand.Range(10, 20) * dmgBonus, caster);
                    }
                }
                else if (t is Corpse corpse)
                {                    
                    Pawn undeadPawn = corpse.InnerPawn;
                    if ((undeadPawn.RaceProps.IsFlesh || TM_Calc.IsUndead(undeadPawn)) && (!TM_Calc.IsRobotPawn(undeadPawn)))
                    {
                        if (undeadPawn.RaceProps.Humanlike && !undeadPawn.RaceProps.Animal)
                        {
                            if (!corpse.IsNotFresh())
                            {
                                comp.Mana.CurLevel += ((.13f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                                if (caster.needs != null && caster.needs.rest != null) { caster.needs.rest.CurLevel += .3f; }
                                if (caster.needs != null && caster.needs.mood != null) { caster.needs.mood.CurLevel += .3f; }
                                ConsumeHumanoid(corpse);
                                if (ver.level > 0)
                                {
                                    HealCaster(caster, 1 + ver.level, 1 + ver.level, (2f + ver.level) * comp.arcaneDmg);
                                }
                            }
                            else
                            {
                                comp.Mana.CurLevel += ((.09f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                                ConsumeHumanoid(corpse);
                            }
                            corpse.Destroy();
                        }
                        else if (undeadPawn.RaceProps.Animal || TM_Calc.IsUndead(undeadPawn))
                        {
                            if (!corpse.IsNotFresh())
                            {
                                comp.Mana.CurLevel += ((.09f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                                if (caster.needs != null && caster.needs.food != null) { caster.needs.food.CurLevel += .4f; }
                                ConsumeAnimalKind(corpse);
                                if (ver.level > 0)
                                {
                                    HealCaster(caster, 1, 1, (2f + ver.level) * comp.arcaneDmg);
                                }
                            }
                            else
                            {
                                comp.Mana.CurLevel += ((.07f * (1 + (manaRegen.level * .02f) + (eff.level * .07f)) * comp.arcaneDmg) * rpct);
                                ConsumeAnimalKind(corpse);
                            }
                            corpse.Destroy();
                        }
                    }                    
                }
                num++;
            }
            return false;
        }

        public void ConsumeHumanoid(Pawn undead)
        {            
            TM_MoteMaker.ThrowSiphonMote(undead.Position.ToVector3Shifted(), undead.Map, 1.2f);
            TM_MoteMaker.ThrowBloodSquirt(undead.Position.ToVector3Shifted(), undead.Map, 1f);
            TM_MoteMaker.ThrowBloodSquirt(undead.Position.ToVector3Shifted(), undead.Map, 1.2f);
            TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3Shifted(), caster.Map, 1f);
            if (undead.inventory != null)
            {
                undead.inventory.DropAllNearPawn(undead.Position, false, true);
            }
            if (undead.equipment != null)
            {
                undead.equipment.DropAllEquipment(undead.Position, false);
            }
            if (undead.apparel != null)
            {
                undead.apparel.DropAll(undead.Position, false);
            }
        }

        public void ConsumeAnimalKind(Pawn undead)
        {
            TM_MoteMaker.ThrowSiphonMote(undead.Position.ToVector3Shifted(), undead.Map, .8f);
            TM_MoteMaker.ThrowBloodSquirt(undead.Position.ToVector3Shifted(), undead.Map, 1.5f);
            TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3Shifted(), caster.Map, .8f);
        }

        public void ConsumeHumanoid(Corpse corpse)
        {
            TM_MoteMaker.ThrowSiphonMote(corpse.Position.ToVector3Shifted(), corpse.Map, 1.2f);
            TM_MoteMaker.ThrowBloodSquirt(corpse.Position.ToVector3Shifted(), corpse.Map, 1f);
            TM_MoteMaker.ThrowBloodSquirt(corpse.Position.ToVector3Shifted(), corpse.Map, 1.2f);
            TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3Shifted(), caster.Map, 1f);
            Pawn corpsePawn = corpse.InnerPawn;
            if (corpsePawn.inventory != null)
            {
                corpsePawn.inventory.DropAllNearPawn(corpse.Position, false, true);
            }
            if (corpsePawn.equipment != null)
            {
                corpsePawn.equipment.DropAllEquipment(corpse.Position, false);
            }
            if (corpsePawn.apparel != null)
            {
                corpsePawn.apparel.DropAll(corpse.Position, false);
            }
        }

        public void ConsumeAnimalKind(Corpse corpse)
        {
            TM_MoteMaker.ThrowSiphonMote(corpse.Position.ToVector3Shifted(), corpse.Map, .8f);
            TM_MoteMaker.ThrowBloodSquirt(corpse.Position.ToVector3Shifted(), corpse.Map, 1.5f);
            TM_MoteMaker.ThrowManaPuff(caster.Position.ToVector3Shifted(), caster.Map, .8f);
        }

        public void HealCaster(Pawn caster, int num, int num2, float healAmt)
        {
            foreach(Hediff_Injury injury in caster.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(injury => injury.CanHealNaturally()).Take(num*num2))
            {
                injury.Heal(healAmt);
            }
        }
    }
}
