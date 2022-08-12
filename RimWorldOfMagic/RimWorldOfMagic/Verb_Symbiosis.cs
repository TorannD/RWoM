using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_Symbiosis : Verb_UseAbility
    {

        private int verVal = 0;
        private int pwrVal = 0;
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
            Pawn hitPawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            if (comp != null && comp.MagicData != null)
            {
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);                
            }

            if(hitPawn != null && hitPawn.RaceProps != null && hitPawn.RaceProps.Humanlike && !TM_Calc.IsUndead(hitPawn) && hitPawn.Faction == caster.Faction)
            {
                if (!hitPawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SymbiosisHD))
                {
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_SymbiosisHD, .5f + pwrVal);
                    HediffWithComps hdc = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD) as HediffWithComps;
                    if (hdc != null)
                    {
                        HediffComp_SymbiosisHost hd_sh = hdc.TryGetComp<HediffComp_SymbiosisHost>();
                        if (hd_sh != null)
                        {
                            hd_sh.symbiote = caster;
                        }
                    }
                    HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_OutOfBodyHD, .5f);
                    HediffWithComps hdc2 = caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD) as HediffWithComps;
                    if (hdc2 != null)
                    {
                        HediffComp_SymbiosisCaster hd_sc = hdc2.TryGetComp<HediffComp_SymbiosisCaster>();
                        if (hd_sc != null)
                        {
                            hd_sc.symbiosisHost = hitPawn;
                        }
                    }
                }
                else
                {
                    Messages.Message("TM_AlreadyHasSymbiote".Translate(CasterPawn.LabelShort), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(CasterPawn.LabelShort, this.Ability.Def.label), MessageTypeDefOf.RejectInput);
            }

            this.PostCastShot(flag, out flag);
            return flag;
        }        
    }
}
