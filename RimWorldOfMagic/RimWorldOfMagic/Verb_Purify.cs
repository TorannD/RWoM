using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
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
            Pawn caster = this.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetComp<CompAbilityUserMagic>();
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

            //    MightPowerSkill mpwr = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //    arcaneDmg = caster.GetComp<CompAbilityUserMight>().mightPwr;

            //}
            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                int num = Mathf.RoundToInt(1f + (.4f * verVal));
                using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyPartRecord rec = enumerator.Current;
                        bool flag2 = num > 0;
                        if (flag2)
                        {
                            int num2 = 1 + verVal;
                            IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                            Func<Hediff_Injury, bool> arg_BB_1;

                            arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);

                            foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                            {
                                bool flag4 = num2 > 0;
                                if (flag4)
                                {
                                    bool flag5 = !current.CanHealNaturally() && current.IsPermanent();
                                    if (flag5)
                                    {
                                        if (rec.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource))
                                        {
                                            if (pwrVal >= 1)
                                            {
                                                current.Heal(pwrVal * arcaneDmg);
                                                num--;
                                                num2--;
                                            }
                                        }
                                        else
                                        {
                                            current.Heal((2f + pwrVal * 2) * arcaneDmg);
                                            //current.Heal(5.0f + (float)pwrVal * 3f); // power affects how much to heal
                                            num--;
                                            num2--;
                                        }
                                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                                        TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
                                        
                                    }
                                }
                            }
                        }
                    }
                }
                //if (pawn.RaceProps.Humanlike)
                //{
                using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Hediff rec = enumerator.Current;
                        bool flag2 = num > 0;
                        if (flag2)
                        {
                            if (TM_Data.AilmentList().Contains(rec.def))
                            {
                                List<TMDefs.TM_CategoryHediff> ailmentList = HediffCategoryList.Named("TM_Category_Hediffs").ailments;
                                foreach(TMDefs.TM_CategoryHediff chd in ailmentList)
                                {
                                    if(chd.hediffDefname.Contains(rec.def.defName))
                                    {
                                        pwrVal = comp.MagicData.AllMagicPowerSkills.FirstOrDefault((MagicPowerSkill x) => x.label == chd.powerSkillName).level;
                                        verVal = comp.MagicData.AllMagicPowerSkills.FirstOrDefault((MagicPowerSkill x) => x.label == chd.requiredSkillName).level;
                                        if (verVal >= chd.requiredSkillLevel)
                                        {
                                            if (chd.removeOnCure)
                                            {
                                                if (Rand.Chance((chd.chanceToRemove + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg))
                                                {
                                                    pawn.health.RemoveHediff(rec);
                                                    if (chd.replacementHediffDefname != "")
                                                    {
                                                        HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                                    }
                                                    num--;
                                                }
                                                else
                                                {
                                                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + rec.Label + " ...");
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                if ((rec.Severity - (chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg) <= 0 && chd.replacementHediffDefname != "")
                                                {
                                                    HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                                }
                                                rec.Heal((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                                num--;
                                            }
                                        }
                                    }
                                }                                
                            }
                            else
                            {
                                if (rec.def.defName == "Cataract" || rec.def.defName == "HearingLoss" || rec.def.defName.Contains("ToxicBuildup"))
                                {
                                    rec.Heal(.4f + .3f * pwrVal);
                                    num--;
                                }
                                if ((rec.def.defName == "Blindness" || rec.def.defName.Contains("Asthma") || rec.def.defName == "Cirrhosis" || rec.def.defName == "ChemicalDamageModerate") && verVal >= 1)
                                {
                                    rec.Heal(.3f + .2f * pwrVal);
                                    if (rec.def.defName.Contains("Asthma"))
                                    {
                                        pawn.health.RemoveHediff(rec);
                                    }
                                    num--;
                                }
                                if ((rec.def.defName == "Frail" || rec.def.defName == "BadBack" || rec.def.defName.Contains("Carcinoma") || rec.def.defName == "ChemicalDamageSevere") && verVal >= 2)
                                {
                                    rec.Heal(.25f + .2f * pwrVal);
                                    num--;
                                }
                                if ((rec.def.defName.Contains("Alzheimers") || rec.def.defName == "Dementia" || rec.def.defName.Contains("HeartArteryBlockage") || rec.def.defName == "PsychicShock" || rec.def.defName == "CatatonicBreakdown") && verVal >= 3)
                                {
                                    rec.Heal(.15f + .15f * pwrVal);
                                    num--;
                                }
                                if (rec.def.defName.Contains("Abasia") && verVal >= 3)
                                {
                                    if (Rand.Chance(.25f + (.05f * pwrVal)))
                                    {
                                        pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Abasia")));
                                        num--;
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
                    }
                }
                //}
                using (IEnumerator<Hediff_Addiction> enumerator = pawn.health.hediffSet.GetHediffs<Hediff_Addiction>().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Hediff_Addiction rec = enumerator.Current;
                        bool flag2 = num > 0;
                        if (flag2)
                        {
                            if (TM_Data.AddictionList().Contains(rec.def))
                            {
                                List<TMDefs.TM_CategoryHediff> addictionList = HediffCategoryList.Named("TM_Category_Hediffs").addictions;
                                foreach (TMDefs.TM_CategoryHediff chd in addictionList)
                                {
                                    if (chd.hediffDefname.Contains(rec.def.defName))
                                    {
                                        pwrVal = comp.MagicData.AllMagicPowerSkills.FirstOrDefault((MagicPowerSkill x) => x.label == chd.powerSkillName).level;
                                        verVal = comp.MagicData.AllMagicPowerSkills.FirstOrDefault((MagicPowerSkill x) => x.label == chd.requiredSkillName).level;
                                        if (verVal >= chd.requiredSkillLevel)
                                        {
                                            if (chd.removeOnCure)
                                            {
                                                if (Rand.Chance((chd.chanceToRemove + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg))
                                                {
                                                    pawn.health.RemoveHediff(rec);
                                                    if (chd.replacementHediffDefname != "")
                                                    {
                                                        HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                                    }
                                                    num--;
                                                }
                                                else
                                                {
                                                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + rec.Label + " ...");
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                if (rec.Chemical.defName == "Luciferium" && ((rec.Severity - (chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg <= 0)))
                                                {
                                                    Hediff luciHigh = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("LuciferiumHigh"), false);
                                                    pawn.health.RemoveHediff(luciHigh);
                                                }
                                                rec.Severity -= ((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                                num--;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (rec.Chemical.defName == "Alcohol" || rec.Chemical.defName == "Smokeleaf")
                                {
                                    rec.Severity -= ((.3f + .3f * pwrVal) * arcaneDmg);
                                    num--;
                                }
                                if ((rec.Chemical.defName == "GoJuice" || rec.Chemical.defName == "WakeUp") && verVal >= 1)
                                {
                                    rec.Severity -= ((.25f + .25f * pwrVal) * arcaneDmg);
                                    num--;
                                }
                                if (rec.Chemical.defName == "Psychite" && verVal >= 2)
                                {
                                    rec.Severity -= ((.25f + .25f * pwrVal) * arcaneDmg);
                                    num--;
                                }
                                if (verVal >= 3)
                                {
                                    if (rec.Chemical.defName == "Luciferium" && (rec.Severity - ((.15f + .15f * pwrVal) * arcaneDmg) < 0))
                                    {
                                        Hediff luciHigh = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("LuciferiumHigh"), false);
                                        pawn.health.RemoveHediff(luciHigh);
                                    }
                                    rec.Severity -= ((.15f + .15f * pwrVal) * arcaneDmg);
                                    num--;
                                }
                            }


                            TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                            TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);
                        }
                    }
                }
            }
            return true;
        }
    }
}
