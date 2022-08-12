using RimWorld;
using System;
using Verse;
using AbilityUser;
using System.Linq;

namespace TorannMagic
{
    class Verb_SoulBond : Verb_UseAbility  
    {
        bool flagSD = false;
        bool flagWD = false;
        new Pawn caster;
        Pawn pawn;
        Pawn oldBondPawn;
        int verVal;
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
            caster = base.CasterPawn;
            pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_pwr");
            //MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_SoulBond.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_SoulBond_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);

            bool flag = pawn != null && !pawn.Dead && pawn.RaceProps.Humanlike && pawn != caster;
            if (flag)
            {
                if (!pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD))
                {
                    if (pawn.Faction != this.CasterPawn.Faction)
                    {
                        Messages.Message("TM_CannotSoulBondUnwilling".Translate(
                            caster.LabelShort,
                            pawn.LabelShort
                       ), MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        flagSD = caster.gender == Gender.Female;
                        flagWD = caster.gender == Gender.Male;                        
                        if (comp.soulBondPawn != null)
                        {
                            oldBondPawn = comp.soulBondPawn;
                            RemoveHediffs();
                            if (oldBondPawn == pawn)
                            {
                                comp.soulBondPawn = null;
                                comp.spell_ShadowCall = false;
                                comp.spell_ShadowStep = false;
                                comp.RemovePawnAbility(TorannMagicDefOf.TM_ShadowCall);
                                comp.RemovePawnAbility(TorannMagicDefOf.TM_ShadowStep);
                            }
                            else
                            {
                                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondPhysicalHD")) && flagSD)
                                {
                                    Messages.Message("TM_CannotSoulBondAnother".Translate(
                                        caster.LabelShort,
                                        pawn.LabelShort
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                else if(pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_SoulBondMentalHD")) && flagWD)
                                {
                                    Messages.Message("TM_CannotSoulBondAnother".Translate(
                                        caster.LabelShort,
                                        pawn.LabelShort
                                    ), MessageTypeDefOf.RejectInput);
                                }
                                else
                                {
                                    ApplyHediffs();
                                    comp.soulBondPawn = pawn;
                                }
                                
                            }
                        }
                        else
                        {
                            ApplyHediffs();
                            comp.spell_ShadowCall = true;
                            comp.spell_ShadowStep = true;
                            comp.AddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
                            comp.AddPawnAbility(TorannMagicDefOf.TM_ShadowStep);                            
                            comp.soulBondPawn = pawn;
                        }
                    }
                }
                else
                {
                    
                    Messages.Message("TM_CannotSoulBondUndead".Translate(
                        caster.LabelShort
                    ), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("TM_CannotSoulBondThing".Translate(
                        caster.LabelShort
                    ), MessageTypeDefOf.RejectInput);
            }
            return true;
        }

        private void ApplyHediffs()
        {
            if (flagSD)
            {
                HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_SDSoulBondPhysicalHD"), .5f + pwrVal);
                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_SoulBondPhysicalHD"), .5f + verVal);
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondPhysicalHD"));
                HediffComp_SoulBondHost comp = hediff.TryGetComp<HediffComp_SoulBondHost>();
                comp.BonderPawn = caster;
            }
            if (flagWD)
            {
                HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_WDSoulBondMentalHD"), .5f + pwrVal);
                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_SoulBondMentalHD"), .5f + verVal);
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondMentalHD"));
                HediffComp_SoulBondHost comp = hediff.TryGetComp<HediffComp_SoulBondHost>();
                comp.BonderPawn = caster;
            }
        }

        private void RemoveHediffs()
        {
            Hediff hediff = new Hediff();
            if (flagSD)
            {
                hediff = oldBondPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondPhysicalHD"));
                if (hediff != null)
                {
                    oldBondPawn.health.RemoveHediff(hediff);
                    hediff = null;
                }
                hediff = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SDSoulBondPhysicalHD"));
                if (hediff != null)
                {
                    caster.health.RemoveHediff(hediff);
                    hediff = null;
                }
            }
            if (flagWD)
            {
                hediff = oldBondPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_SoulBondMentalHD"));
                if (hediff != null)
                {
                    oldBondPawn.health.RemoveHediff(hediff);
                    hediff = null;
                }
                hediff = caster.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_WDSoulBondMentalHD"));
                if (hediff != null)
                {
                    caster.health.RemoveHediff(hediff);
                    hediff = null;
                }
            }
        }
    }
}
