﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using TorannMagic.Extensions;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_BrandSiphon : Verb_UseAbility
    {

        private int verVal = 0;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;
        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
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
            bool flag = false;
            Pawn caster = this.CasterPawn;
            
            if(caster != null && this.CurrentTarget.HasThing && this.CurrentTarget.Thing is Pawn)
            {
                Pawn hitPawn = this.currentTarget.Thing as Pawn;
                if (hitPawn.RaceProps != null && hitPawn.RaceProps.Humanlike && !TM_Calc.IsUndead(hitPawn) && hitPawn != caster)
                {
                    CompAbilityUserMagic casterComp = caster.GetCompAbilityUserMagic();
                    CompAbilityUserMagic targetComp = hitPawn.GetCompAbilityUserMagic();
                    if (casterComp != null && targetComp != null && targetComp.Mana != null && targetComp.IsMagicUser && hitPawn.health != null && hitPawn.health.hediffSet != null)
                    {
                        //RemoveOldBrand(hitPawn);

                        //HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_SiphonBrandHD, .05f);
                        //if (casterComp.BrandedPawns != null)
                        //{
                        //    casterComp.BrandedPawns.Add(hitPawn);
                        //}
                        //Hediff newBrand = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SiphonBrandHD);
                        //if (newBrand != null && newBrand.TryGetComp<HediffComp_BrandingSiphon>() != null)
                        //{
                        //    newBrand.TryGetComp<HediffComp_BrandingSiphon>().BranderPawn = caster;
                        //}

                        TM_Action.UpdateBrand(hitPawn, caster, casterComp, TorannMagicDefOf.TM_SiphonBrandHD);

                        UpdateHediffComp(hitPawn);
                        DoBrandingEffect(hitPawn);
                    }
                    else
                    {
                        Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, this.Ability.Def.label), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, this.Ability.Def.label), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, this.Ability.Def.label), MessageTypeDefOf.RejectInput);
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }

        private void UpdateHediffComp(Pawn hitPawn)
        {
            Hediff hd = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SiphonBrandHD);
            if (hd != null)
            {
                HediffComp_BrandingBase hdc = hd.TryGetComp<HediffComp_BrandingBase>();
                if (hdc != null)
                {
                    hdc.BranderPawn = this.CasterPawn;
                }
            }
        }

        private void DoBrandingEffect(Pawn hitPawn)
        {
            Effecter effect = EffecterDefOf.Skip_EntryNoDelay.Spawn();
            effect.Trigger(new TargetInfo(this.CasterPawn), new TargetInfo(hitPawn));
            effect.Cleanup();

            Effecter effectExit = EffecterDefOf.Skip_ExitNoDelay.Spawn();
            effectExit.Trigger(new TargetInfo(hitPawn), new TargetInfo(hitPawn));
            effectExit.Cleanup();
        }

        //private void RemoveOldBrand(Pawn hitPawn)
        //{
        //    Hediff oldBrand = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SiphonBrandHD);
        //    if (oldBrand != null)
        //    {
        //        HediffComp_BrandingSiphon hd_br = oldBrand.TryGetComp<HediffComp_BrandingSiphon>();
        //        if (hd_br != null && hd_br.BranderPawn != null && !hd_br.BranderPawn.DestroyedOrNull() && !hd_br.BranderPawn.Dead)
        //        {
        //            CompAbilityUserMagic branderComp = hd_br.BranderPawn.GetCompAbilityUserMagic();
        //            if (branderComp != null && branderComp.BrandedPawns != null && branderComp.BrandedPawns.Contains(hitPawn))
        //            {
        //                branderComp.BrandedPawns.Remove(hitPawn);
        //            }
        //        }
        //        hitPawn.health.RemoveHediff(oldBrand);
        //    }
        //}
    }
}
