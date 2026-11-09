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
    public class Verb_EnchantWeapon : ClassProgression.Verb_SpiritTargetable
    {
        
        int pwrVal;


        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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

            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(caster, Ability);
            if (owner == null || pawn?.equipment?.Primary?.def.IsMeleeWeapon != true)
            {
                if (pawn != null && caster.IsColonist)
                    Messages.Message("TM_NoMeleeWeaponToEnchant".Translate(pawn), MessageTypeDefOf.RejectInput);
                return false;
            }
            pwrVal = owner == caster ? TM_Calc.GetSkillPowerLevel(owner, Ability.Def as TMAbilityDef) :
                ClassProgression.SpiritRebirthRuntime.SavedRank(owner.GetCompAbilityUserMagic().ExistingMagicData, Ability.Def as TMAbilityDef, "_pwr");
            var trial = ClassProgression.EnchanterTrials.CaptureEnchantment(Ability, pawn);
            RemoveExistingEnchantment(pawn);
            ApplyEnchantment(pawn);
            ClassProgression.EnchanterTrials.CompleteEnchantment(trial);
            return true;
        }

        public void ApplyEnchantment(Pawn pawn)
        {
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            var comp = owner?.GetCompAbilityUserMagic();
            if (comp == null || pawn?.equipment?.Primary?.def.IsMeleeWeapon != true) return;
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
            comp.weaponEnchants ??= new List<Pawn>();
            if (!comp.weaponEnchants.Contains(pawn)) comp.weaponEnchants.Add(pawn);
            Hediff diff = pawn.health.hediffSet.GetFirstHediffOfDef(hediff, false);
            HediffComp_EnchantedWeapon ewComp = diff.TryGetComp<HediffComp_EnchantedWeapon>();
            ewComp.enchantedWeapon = pawn.equipment.Primary;
            ewComp.enchanterPawn = owner;
            comp.SyncEnchantWeaponControl();

        }

        public void RemoveExistingEnchantment(Pawn pawn)
        {
            Verb_DispelEnchantWeapon.RemoveExistingEnchantment(pawn);
        }
    }
}
