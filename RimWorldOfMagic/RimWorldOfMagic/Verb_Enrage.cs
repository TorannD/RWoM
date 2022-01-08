using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_Enrage : Verb_UseAbility
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
            CompAbilityUserMagic comp = caster.GetComp<CompAbilityUserMagic>();
            if (comp != null && comp.MagicData != null)
            {
                //pwrVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_Enrage, "TM_Enrage", "_pwr", true);
                //verVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_Enrage, "TM_Enrage", "_ver", true);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
                arcaneDmg = comp.arcaneDmg;
            }

            if(hitPawn != null && hitPawn.RaceProps != null && hitPawn.RaceProps.Humanlike && !TM_Calc.IsUndead(hitPawn))
            {
                if(hitPawn.Faction != caster.Faction && hitPawn.mindState != null && hitPawn.mindState.mentalStateHandler != null)
                {
                    if(Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, hitPawn, true)))
                    {
                        try
                        {
                            hitPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, true, false, caster);
                        }
                        catch
                        {
                            MoteMaker.ThrowText(hitPawn.DrawPos, hitPawn.Map, "Failed", -1);
                        }
                    }
                    else
                    {
                        MoteMaker.ThrowText(hitPawn.DrawPos, hitPawn.Map, "TM_ResistedSpell".Translate(), -1);
                        return false;
                    }
                }
                HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_EnrageHD, (.25f + (.05f+pwrVal)) * (.5f*arcaneDmg));

                HediffComp_Enrage hdc = hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnrageHD).TryGetComp<HediffComp_Enrage>();
                hdc.reductionFactor = (1f - (.1f * verVal));
                if(verVal >= 4)
                {
                    hdc.consumeJoy = true;
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
