using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Verb_EnchantWeapon : Verb_UseAbility  
    {
        
        int pwrVal;
        CompAbilityUserMagic comp;

        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
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
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            comp = caster.GetComp<CompAbilityUserMagic>();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            //MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_EnchantWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EnchantWeapon_pwr");
            //pwrVal = pwr.level;
            //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    pwrVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
            //}
            //if (settingsRef.AIHardMode && !caster.IsColonist)
            //{
            //    pwrVal = 3;
            //}

            if (pawn != null)
            {
                if (pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsMeleeWeapon)
                {
                    RemoveExistingEnchantment(pawn);
                    ApplyEnchantment(pawn);
                }
                else if(this.CasterPawn.IsColonist)
                {
                    Messages.Message("TM_NoMeleeWeaponToEnchant".Translate(
                    pawn
                ), MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            
            return true;
        } 
        
        public void ApplyEnchantment(Pawn pawn)
        {
            HediffDef hediff = null;
            float rnd = Rand.Range(0, 1f);
            if (rnd < .25f)
            {
                hediff = TorannMagicDefOf.TM_WeaponEnchantment_FireHD;                
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Heat, pawn.DrawPos, pawn.Map, 1f, .2f, .1f, .8f, Rand.Range(-30, 30), .3f, Rand.Range(-30, 30), Rand.Range(0, 360));
                FleckMaker.ThrowFireGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 1f);
            }
            else if (rnd < .5f)
            {
                hediff = TorannMagicDefOf.TM_WeaponEnchantment_IceHD;
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ice, pawn.DrawPos, pawn.Map, 1f, .2f, .1f, .8f, Rand.Range(-30, 30), .3f, Rand.Range(-30, 30), Rand.Range(0, 360));
            }
            else if (rnd < .75f)
            {
                hediff = TorannMagicDefOf.TM_WeaponEnchantment_LitHD;
                FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
                FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 1f);
            }
            else
            {
                hediff = TorannMagicDefOf.TM_WeaponEnchantment_DarkHD;
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, pawn.DrawPos, pawn.Map, 1f, .2f, .1f, .8f, Rand.Range(-30, 30), .3f, Rand.Range(-30, 30), Rand.Range(0, 360));
            }
            HealthUtility.AdjustSeverity(pawn, hediff, .5f + pwrVal);
            CompAbilityUserMagic comp = this.CasterPawn.GetComp<CompAbilityUserMagic>();            
            comp.weaponEnchants.Add(pawn);
            Hediff diff = pawn.health.hediffSet.GetFirstHediffOfDef(hediff, false);
            HediffComp_EnchantedWeapon ewComp = diff.TryGetComp<HediffComp_EnchantedWeapon>();
            ewComp.enchantedWeapon = pawn.equipment.Primary;
            ewComp.enchanterPawn = this.CasterPawn;

        }

        public void RemoveExistingEnchantment(Pawn pawn)
        {
            Hediff hediff = null;
            List<Hediff> allHediffs = new List<Hediff>();
            allHediffs.Clear();
            allHediffs = pawn.health.hediffSet.GetHediffs<Hediff>().ToList();
            if (allHediffs != null && allHediffs.Count > 0)
            {
                for (int i = 0; i < allHediffs.Count; i++)
                {
                    hediff = allHediffs[i];
                    if(hediff.def.defName.Contains("TM_WeaponEnchantment"))
                    {
                        HediffComp_EnchantedWeapon ewComp = hediff.TryGetComp<HediffComp_EnchantedWeapon>();
                        if (ewComp != null)
                        {
                            CompAbilityUserMagic comp = ewComp.enchanterPawn.GetComp<CompAbilityUserMagic>();
                            if (comp != null)
                            {
                                comp.weaponEnchants.Remove(pawn);
                            }
                        }
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
        }
    }
}
