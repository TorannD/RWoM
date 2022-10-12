using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using TorannMagic.Utils;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Purify : Verb_UseAbility
    {
        private int verVal;
        private int pwrVal;
        float arcaneDmg = 1f;

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
            Pawn caster = this.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            arcaneDmg = comp.arcaneDmg;
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            //if (comp != null && !caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{                
            //    MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Purify.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Purify_pwr");
            //    MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_Purify.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Purify_ver");
            //    pwrVal = pwr.level;
            //    verVal = ver.level;
                
            //}
            //else if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{

            //    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //    arcaneDmg = caster.GetCompAbilityUserMight().mightPwr;

            //}
            if (pawn == null || pawn.Dead) return true;

            int injuriesToHeal = Mathf.RoundToInt(1f + .4f * verVal);
            int injuriesPerBodyPart = 1 + verVal;

            IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.IsPermanent())
                .DistinctBy(injury => injury.Part, injuriesPerBodyPart)
                .Take(injuriesToHeal);
            foreach (Hediff_Injury injury in injuries)
            {
                float healAmount;
                if (injury.Part.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource))
                    healAmount = pwrVal * arcaneDmg;
                else
                    healAmount = (2f + pwrVal * 2) * arcaneDmg;
                injury.Heal(healAmount);
                injuriesToHeal--;

                TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
            }
            //if (pawn.RaceProps.Humanlike)
            //{
            List<TMDefs.TM_CategoryHediff> ailmentList = HediffCategoryList.Named("TM_Category_Hediffs").ailments;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (injuriesToHeal <= 0) return true;

                if (TM_Data.AilmentList().Contains(hediff.def))
                {
                    foreach(TMDefs.TM_CategoryHediff chd in ailmentList)
                    {
                        if(chd.hediffDefname.Contains(hediff.def.defName))
                        {
                            pwrVal = comp.MagicData.AllMagicPowerSkills.First(mps => mps.label == chd.powerSkillName).level;
                            verVal = comp.MagicData.AllMagicPowerSkills.First(mps => mps.label == chd.requiredSkillName).level;
                            if (verVal >= chd.requiredSkillLevel)
                            {
                                if (chd.removeOnCure)
                                {
                                    if (Rand.Chance((chd.chanceToRemove + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg))
                                    {
                                        pawn.health.RemoveHediff(hediff);
                                        if (chd.replacementHediffDefname != "")
                                        {
                                            HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                        }
                                        injuriesToHeal--;
                                    }
                                    else
                                    {
                                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + hediff.Label + " ...");
                                    }
                                    break;
                                }
                                else
                                {
                                    if ((hediff.Severity - (chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg) <= 0 && chd.replacementHediffDefname != "")
                                    {
                                        HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                    }
                                    hediff.Heal((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                    injuriesToHeal--;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (hediff.def.defName == "Cataract" || hediff.def.defName == "HearingLoss" || hediff.def.defName.Contains("ToxicBuildup"))
                    {
                        hediff.Heal(.4f + .3f * pwrVal);
                        injuriesToHeal--;
                    }
                    if ((hediff.def.defName == "Blindness" || hediff.def.defName.Contains("Asthma") || hediff.def.defName == "Cirrhosis" || hediff.def.defName == "ChemicalDamageModerate") && verVal >= 1)
                    {
                        hediff.Heal(.3f + .2f * pwrVal);
                        if (hediff.def.defName.Contains("Asthma"))
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                        injuriesToHeal--;
                    }
                    if ((hediff.def.defName == "Frail" || hediff.def.defName == "BadBack" || hediff.def.defName.Contains("Carcinoma") || hediff.def.defName == "ChemicalDamageSevere") && verVal >= 2)
                    {
                        hediff.Heal(.25f + .2f * pwrVal);
                        injuriesToHeal--;
                    }
                    if ((hediff.def.defName.Contains("Alzheimers") || hediff.def.defName == "Dementia" || hediff.def.defName.Contains("HeartArteryBlockage") || hediff.def.defName == "PsychicShock" || hediff.def.defName == "CatatonicBreakdown") && verVal >= 3)
                    {
                        hediff.Heal(.15f + .15f * pwrVal);
                        injuriesToHeal--;
                    }
                    if (hediff.def.defName.Contains("Abasia") && verVal >= 3)
                    {
                        if (Rand.Chance(.25f + (.05f * pwrVal)))
                        {
                            pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Abasia")));
                            injuriesToHeal--;
                        }
                        else
                        {
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove Abasia...");
                        }
                    }
                }
                TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
            }
            //}
            foreach (Hediff_Addiction addiction in pawn.health.hediffSet.hediffs.OfType<Hediff_Addiction>())
            {
                if (injuriesToHeal <= 0) return true;

                if (TM_Data.AddictionList().Contains(addiction.def))
                {
                    List<TMDefs.TM_CategoryHediff> addictionList = HediffCategoryList.Named("TM_Category_Hediffs").addictions;
                    foreach (TMDefs.TM_CategoryHediff chd in addictionList)
                    {
                        if (chd.hediffDefname.Contains(addiction.def.defName))
                        {
                            pwrVal = comp.MagicData.AllMagicPowerSkills.First(mps => mps.label == chd.powerSkillName).level;
                            verVal = comp.MagicData.AllMagicPowerSkills.First(mps => mps.label == chd.requiredSkillName).level;
                            if (verVal >= chd.requiredSkillLevel)
                            {
                                if (chd.removeOnCure)
                                {
                                    if (Rand.Chance((chd.chanceToRemove + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg))
                                    {
                                        pawn.health.RemoveHediff(addiction);
                                        if (chd.replacementHediffDefname != "")
                                        {
                                            HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                        }
                                        injuriesToHeal--;
                                    }
                                    else
                                    {
                                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + addiction.Label + " ...");
                                    }
                                    break;
                                }
                                else
                                {
                                    if (addiction.Chemical.defName == "Luciferium" && ((addiction.Severity - (chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg <= 0)))
                                    {
                                        Hediff luciHigh = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("LuciferiumHigh"), false);
                                        pawn.health.RemoveHediff(luciHigh);
                                    }
                                    addiction.Severity -= ((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                    injuriesToHeal--;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (addiction.Chemical.defName == "Alcohol" || addiction.Chemical.defName == "Smokeleaf")
                    {
                        addiction.Severity -= ((.3f + .3f * pwrVal) * arcaneDmg);
                        injuriesToHeal--;
                    }
                    if ((addiction.Chemical.defName == "GoJuice" || addiction.Chemical.defName == "WakeUp") && verVal >= 1)
                    {
                        addiction.Severity -= ((.25f + .25f * pwrVal) * arcaneDmg);
                        injuriesToHeal--;
                    }
                    if (addiction.Chemical.defName == "Psychite" && verVal >= 2)
                    {
                        addiction.Severity -= ((.25f + .25f * pwrVal) * arcaneDmg);
                        injuriesToHeal--;
                    }
                    if (verVal >= 3)
                    {
                        if (addiction.Chemical.defName == "Luciferium" && (addiction.Severity - ((.15f + .15f * pwrVal) * arcaneDmg) < 0))
                        {
                            Hediff luciHigh = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("LuciferiumHigh"), false);
                            pawn.health.RemoveHediff(luciHigh);
                        }
                        addiction.Severity -= ((.15f + .15f * pwrVal) * arcaneDmg);
                        injuriesToHeal--;
                    }
                }

                TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
            }
            return true;
        }
    }
}
