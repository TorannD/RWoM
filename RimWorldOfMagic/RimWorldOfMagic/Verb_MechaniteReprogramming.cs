using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_MechaniteReprogramming : Verb_UseAbility
    {

        bool validTarg;
        int pwrVal = 0;
        int verVal = 0;
        float arcaneDmg = 1f;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;
            CompAbilityUserMagic comp = pawn.TryGetComp <CompAbilityUserMagic>();

            if (pawn == null) return false;

            int num = 1;

            if(!pawn.DestroyedOrNull() && pawn.health?.hediffSet != null && !pawn.Dead)
            {
                bool success = false;
                List<TMDefs.TM_CategoryHediff> mechaniteList = HediffCategoryList.Named("TM_Category_Hediffs").mechanites;
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (TM_Data.MechaniteList().Contains(hediff.def))
                    {
                        foreach(TMDefs.TM_CategoryHediff chd in mechaniteList)
                        {
                            if (chd.hediffDefname.Contains(hediff.def.defName))
                            {
                                if (comp != null)
                                {
                                    if (chd.requiredSkillName != "TM_Purify_ver")
                                    {
                                        verVal = comp.MagicData.AllMagicPowerSkills.FirstOrDefault((MagicPowerSkill x) => x.label == chd.requiredSkillName).level;
                                    }
                                    if (chd.powerSkillName != "TM_Purify_pwr")
                                    {
                                        pwrVal = comp.MagicData.AllMagicPowerSkills.FirstOrDefault((MagicPowerSkill x) => x.label == chd.powerSkillName).level;
                                    }
                                }
                                if (verVal >= chd.requiredSkillLevel)
                                {
                                    if (chd.removeOnCure)
                                    {
                                        if (Rand.Chance((chd.chanceToRemove + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg))
                                        {
                                            pawn.health.RemoveHediff(hediff);
                                            if(chd.replacementHediffDefname != "")
                                            {
                                                HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                            }
                                            success = true;
                                            num--;
                                            break;
                                        }
                                        else
                                        {
                                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + hediff.Label + " ...");
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        hediff.Severity -= ((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                        if ((hediff.Severity < 0))
                                        {
                                            if (chd.replacementHediffDefname != "")
                                            {
                                                HealthUtility.AdjustSeverity(pawn, HediffDef.Named(chd.replacementHediffDefname), chd.replacementHediffSeverity);
                                            }
                                        }
                                        success = true;
                                        num--;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (hediff.def.defName == "SensoryMechanites")
                        {
                            pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReprogrammedSenMechanites_HD, .001f);
                            success = true;
                            break;
                        }
                        else if (hediff.def.defName == "FibrousMechanites")
                        {
                            pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReprogrammedFibMechanites_HD, .001f);
                            success = true;
                            break;
                        }
                        else if (hediff.def.defName == "LymphaticMechanites")
                        {
                            pawn.health.RemoveHediff(hediff);
                            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReprogrammedLymMechanites_HD, .001f);
                            success = true;
                            break;
                        }
                    }
                }
                if (success)
                {
                    TM_MoteMaker.ThrowRegenMote(pawn.Position.ToVector3(), pawn.Map, 1.5f);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Mechanite Reprogramming" + ": " + StringsToTranslate.AU_CastSuccess, -1f);
                }
                else
                {
                    Messages.Message("TM_CureDiseaseTypeFail".Translate(), MessageTypeDefOf.NegativeEvent);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Mechanite Reprogramming" + ": " + StringsToTranslate.AU_CastFailure, -1f);
                }
            }
            else
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Mechanite Reprogramming" + ": " + StringsToTranslate.AU_CastFailure, -1f);
            }
            return false;
        }
    }
}
