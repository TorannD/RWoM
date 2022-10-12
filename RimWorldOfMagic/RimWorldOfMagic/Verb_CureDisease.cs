using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_CureDisease : Verb_SB
    {
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1f;
            
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);

            //MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CureDisease_pwr");
            //MagicPowerSkill ver = comp.MagicData.MagicPowerSkill_CureDisease.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_CureDisease_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            //this.arcaneDmg = caster.GetCompAbilityUserMagic().arcaneDmg;
            //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}
            bool flag = pawn != null;
            if (flag)
            {
                int num = 1;
                float sevAdjustment = 0;
                if (pwrVal >= 2)
                {
                    //apply immunity buff, 60k ticks in a day
                    if (pwrVal == 3)
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_DiseaseImmunity2HD, 5);
                        pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DiseaseImmunity2HD).TryGetComp<HediffComp_DiseaseImmunity>().verVal = verVal;
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_DiseaseImmunityHD, 3);
                    }
                    
                }

                if (pwrVal >= 1)
                {
                    sevAdjustment = 5;
                }
                else
                {
                    sevAdjustment = (Rand.Range(0f, 1f) * this.arcaneDmg);
                }
                if(sevAdjustment >= .25f) 
                {
                    bool success = false;
                    List<TMDefs.TM_CategoryHediff> diseaseList = HediffCategoryList.Named("TM_Category_Hediffs").diseases;
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (TM_Data.AddictionList().Contains(hediff.def))
                        {
                            foreach (TMDefs.TM_CategoryHediff chd in diseaseList)
                            {
                                if (chd.hediffDefname.Contains(hediff.def.defName))
                                {
                                    if (comp != null && chd.requiredSkillName != "TM_Purify_ver")
                                    {
                                        pwrVal = comp.MagicData.AllMagicPowerSkills.First(mps => mps.label == chd.powerSkillName).level;
                                        verVal = comp.MagicData.AllMagicPowerSkills.First(mps => mps.label == chd.requiredSkillName).level;
                                    }
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
                                                success = true;
                                                num--;
                                            }
                                            else
                                            {
                                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + hediff.Label + " ...");
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            if (((hediff.Severity - (chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg <= 0)))
                                            {
                                                if (chd.replacementHediffDefname != "")
                                                {
                                                    HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                                }
                                                success = true;
                                            }
                                            hediff.Severity -= ((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                            num--;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (hediff.def.defName == "WoundInfection" || hediff.def.defName.Contains("Flu") || hediff.def.defName == "Animal_Flu" || hediff.def.defName.Contains("Infection"))
                            {
                                //rec.Severity -= sevAdjustment;
                                pawn.health.RemoveHediff(hediff);
                                success = true;
                            }
                            if (verVal >= 1 && (hediff.def.defName == "GutWorms" || hediff.def == HediffDefOf.Malaria || hediff.def == HediffDefOf.FoodPoisoning))
                            {
                                //rec.Severity -= sevAdjustment;
                                pawn.health.RemoveHediff(hediff);
                                success = true;
                            }
                            if (verVal >= 2 && (hediff.def.defName == "SleepingSickness" || hediff.def.defName == "MuscleParasites") || hediff.def == HediffDefOf.Scaria)
                            {
                                //rec.Severity -= sevAdjustment;
                                pawn.health.RemoveHediff(hediff);
                                success = true;
                            }
                            if (verVal == 3 && (hediff.def.makesSickThought && hediff.def.isBad))
                            {
                                //rec.Severity -= sevAdjustment;
                                if (hediff.def.defName == "BloodRot")
                                {
                                    hediff.Severity = 0.01f;
                                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Tended Blood Rot", -1f);
                                    hediff.Tended(1f, 1f);
                                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3(), pawn.Map, 1.5f);
                                    return false;
                                }
                                else if (hediff.def.defName == "Abasia")
                                {
                                    //do nothing
                                }
                                else
                                {
                                    pawn.health.RemoveHediff(hediff);
                                    success = true;
                                }
                            }
                        }
                        if(success)
                        {
                            break;
                        }
                    }
                    if (success)
                    {                        
                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3(), pawn.Map, 1.5f);
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Cure Disease" + ": " + StringsToTranslate.AU_CastSuccess, -1f);
                    }
                    else
                    {
                        Messages.Message("TM_CureDiseaseTypeFail".Translate(), MessageTypeDefOf.NegativeEvent);
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Cure Disease" + ": " + StringsToTranslate.AU_CastFailure, -1f);
                    }
                }
                else
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Cure Disease" + ": " + StringsToTranslate.AU_CastFailure, -1f);
                }
                
            }
            return false;
        }
    }
}
