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

            bool flag = pawn != null;
            if (flag)
            {
                int num = 1;

                if(!pawn.DestroyedOrNull() && pawn.health != null || pawn.health.hediffSet != null && !pawn.Dead) 
                {
                    bool success = false;
                    List<TMDefs.TM_CategoryHediff> mechaniteList = HediffCategoryList.Named("TM_Category_Hediffs").mechanites;
                    using (IEnumerator<Hediff> enumerator = pawn.health.hediffSet.GetHediffs<Hediff>().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Hediff rec = enumerator.Current;
                            bool flag2 = num > 0;


                            if (TM_Data.MechaniteList().Contains(rec.def))
                            {
                                foreach(TMDefs.TM_CategoryHediff chd in mechaniteList)
                                {
                                    if (chd.hediffDefname.Contains(rec.def.defName))
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
                                                    pawn.health.RemoveHediff(rec);
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
                                                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Failed to remove " + rec.Label + " ...");
                                                }
                                                break;
                                            }
                                            else
                                            {
                                                rec.Severity -= ((chd.severityReduction + (chd.powerSkillAdjustment * pwrVal)) * arcaneDmg);
                                                if ((rec.Severity < 0))
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
                                if (rec.def.defName == "SensoryMechanites")
                                {
                                    pawn.health.RemoveHediff(rec);
                                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReprogrammedSenMechanites_HD, .001f);
                                    num--;
                                    success = true;
                                    break;
                                }
                                else if (rec.def.defName == "FibrousMechanites")
                                {
                                    pawn.health.RemoveHediff(rec);
                                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReprogrammedFibMechanites_HD, .001f);
                                    num--;
                                    success = true;
                                    break;
                                }
                                else if (rec.def.defName == "LymphaticMechanites")
                                {
                                    pawn.health.RemoveHediff(rec);
                                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ReprogrammedLymMechanites_HD, .001f);
                                    num--;
                                    success = true;
                                    break;
                                }
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
                
            }
            return false;
        }
    }
}
