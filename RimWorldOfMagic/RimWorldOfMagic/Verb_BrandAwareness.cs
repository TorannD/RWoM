using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


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
            Pawn hitPawn = this.currentTarget.Thing as Pawn;
            if(hitPawn != null && hitPawn.RaceProps != null)
            {
                CompAbilityUserMagic casterComp = caster.TryGetComp<CompAbilityUserMagic>();
                CompAbilityUserMagic targetComp = hitPawn.TryGetComp<CompAbilityUserMagic>();

                if (casterComp != null && hitPawn.health != null && hitPawn.health.hediffSet != null)
                {
                    RemoveOldBrand(hitPawn);

                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_AwarenessBrandHD, .05f);
                    casterComp.BrandedPawns.Add(hitPawn);
                    Hediff newBrand = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AwarenessBrandHD);                    
                    newBrand.TryGetComp<HediffComp_BrandingBase>().BranderPawn = caster;

                    Effecter effect = EffecterDefOf.Skip_EntryNoDelay.Spawn();
                    effect.Trigger(new TargetInfo(caster), new TargetInfo(hitPawn));
                    effect.Cleanup();
                
                    Effecter effectExit = EffecterDefOf.Skip_ExitNoDelay.Spawn();
                    effectExit.Trigger(new TargetInfo(hitPawn), new TargetInfo(hitPawn));
                    effectExit.Cleanup();
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
        
        private void RemoveOldBrand(Pawn hitPawn)
        {
            Hediff oldBrand = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_AwarenessBrandHD);
            if (oldBrand != null)
            {
                HediffComp_BrandingBase hd_br = oldBrand.TryGetComp<HediffComp_BrandingBase>();
                if (hd_br != null && hd_br.BranderPawn != null && !hd_br.BranderPawn.DestroyedOrNull() && !hd_br.BranderPawn.Dead)
                {
                    CompAbilityUserMagic branderComp = hd_br.BranderPawn.TryGetComp<CompAbilityUserMagic>();
                    if (branderComp != null && branderComp.BrandedPawns != null && branderComp.BrandedPawns.Contains(hitPawn))
                    {
                        branderComp.BrandedPawns.Remove(hitPawn);
                    }
                }
                hitPawn.health.RemoveHediff(oldBrand);
            }
        }
    }
}
