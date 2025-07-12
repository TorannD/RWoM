using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using TorannMagic.TMDefs;
using UnityEngine;

namespace TorannMagic
{
    public class Verb_BrandAwareness : Verb_UseAbility
    {

        private int verVal = 0;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;
        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
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
                CompAbilityUserMagic casterComp = caster.GetCompAbilityUserMagic();

                if (casterComp != null && hitPawn.health != null && hitPawn.health.hediffSet != null && hitPawn != caster)
                {
                    TM_Action.UpdateBrand(hitPawn, caster, casterComp, TorannMagicDefOf.TM_AwarenessBrandHD);

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

            this.PostCastShot(flag, out flag);
            return flag;
        }

        private void UpdateHediffComp(Pawn hitPawn)
        {
            Hediff hd = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AwarenessBrandHD);
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
            if(hitPawn != null && hitPawn.Map != null)
            {
                TargetInfo ti = new TargetInfo(hitPawn.Position, hitPawn.Map, false);
                TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, hitPawn.Map, Vector3.zero, 1f, 0f, .1f, .4f, 1.2f, -3f);
            }
            //Effecter effect = EffecterDefOf.Skip_EntryNoDelay.Spawn();
            //effect.Trigger(new TargetInfo(this.CasterPawn), new TargetInfo(hitPawn));
            //effect.Cleanup();

            //Effecter effectExit = EffecterDefOf.Skip_ExitNoDelay.Spawn();
            //effectExit.Trigger(new TargetInfo(hitPawn), new TargetInfo(hitPawn));
            //effectExit.Cleanup();
        }

        //private void ChangeExistingBrand(Pawn hitPawn, CompAbilityUserMagic casterComp)
        //{
        //    Hediff oldBrand = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AwarenessBrandHD);
        //    if (oldBrand != null)
        //    {
        //        HediffComp_BrandingBase hd_br = oldBrand.TryGetComp<HediffComp_BrandingBase>();
        //        if (hd_br != null)
        //        {
        //            if (!hd_br.BranderPawn.DestroyedOrNull())
        //            {
        //                CompAbilityUserMagic branderComp = hd_br.BranderPawn.GetCompAbilityUserMagic();
        //                if (branderComp != null && branderComp.BrandedPawns != null && branderComp.BrandedPawns.Contains(hitPawn))
        //                {
        //                    branderComp.BrandedPawns.Remove(hitPawn);
        //                }
        //            }
        //            hd_br.BranderPawn = this.CasterPawn; 
        //            casterComp.BrandedPawns.Add()
        //        }
        //        //hitPawn.health.RemoveHediff(oldBrand);
        //    }
        //}
    }
}
