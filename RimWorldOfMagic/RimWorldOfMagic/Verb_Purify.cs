using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;
using TorannMagic.Utils;


namespace TorannMagic
{
    public class Verb_Purify : ClassProgression.Verb_SpiritTargetable
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
            Pawn caster = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            if (caster == null) return false;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            arcaneDmg = comp.arcaneDmg;
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);

            if (pawn == null || pawn.Dead) return true;

            int num = Mathf.RoundToInt(1f + (.4f * verVal));
            // Vanilla GetInjuredParts only yields parts that are actively bleeding, so
            // permanent injuries (scars, lost parts) never entered the heal loop even
            // though the ability description promises scar healing (player report:
            // Purify removed toxic buildup but did nothing to scars or a lost arm).
            // Iterate the injuries directly; fresh natural wounds stay excluded.
            foreach (Hediff_Injury current in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList())
            {
                {
                    BodyPartRecord rec = current.Part;
                    bool flag2 = num > 0;
                    if (flag2)
                    {
                        bool flag5 = !current.CanHealNaturally();
                        if (flag5)
                        {
                            if (current is Hediff_MissingPart)
                            {
                                // A lost part has no injury severity to heal down; vanilla
                                // restores it as a whole, matching the multi-use expectation.
                                pawn.health.RestorePart(rec, null, false);
                                num--;
                            }
                            else if (rec?.def?.tags?.Contains(BodyPartTagDefOf.ConsciousnessSource) == true)
                            {
                                if (pwrVal >= 1)
                                {
                                    current.Heal(pwrVal * arcaneDmg);
                                    num--;
                                }
                            }
                            else
                            {
                                current.Heal((2f + pwrVal * 2) * arcaneDmg);
                                //current.Heal(5.0f + (float)pwrVal * 3f); // power affects how much to heal
                                num--;
                            }
                            TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .6f);
                            TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3Shifted(), pawn.Map, .4f);

                        }
                    }
                }
            }
            //if (pawn.RaceProps.Humanlike)
            //{
            using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.hediffs.ToList().GetEnumerator())
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
            using (IEnumerator<Hediff_Addiction> enumerator = pawn.health.hediffSet.hediffs.OfType<Hediff_Addiction>().ToList().GetEnumerator())
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
                                            if (rec.Chemical?.defName == "Luciferium" && ((rec.Severity - (chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg <= 0)))
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
                            if (rec.Chemical?.defName == "Alcohol" || rec.Chemical?.defName == "Smokeleaf")
                            {
                                rec.Severity -= ((.3f + .3f * pwrVal) * arcaneDmg);
                                num--;
                            }
                            if ((rec.Chemical?.defName == "GoJuice" || rec.Chemical?.defName == "WakeUp") && verVal >= 1)
                            {
                                rec.Severity -= ((.25f + .25f * pwrVal) * arcaneDmg);
                                num--;
                            }
                            if (rec.Chemical?.defName == "Psychite" && verVal >= 2)
                            {
                                rec.Severity -= ((.25f + .25f * pwrVal) * arcaneDmg);
                                num--;
                            }
                            if (verVal >= 3)
                            {
                                if (rec.Chemical?.defName == "Luciferium" && (rec.Severity - ((.15f + .15f * pwrVal) * arcaneDmg) < 0))
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
            
            return true;
        }
    }
}
