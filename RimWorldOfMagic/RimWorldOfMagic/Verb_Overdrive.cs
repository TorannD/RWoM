using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Verb_Overdrive : Verb_UseAbility  
    {        
        int pwrVal;
        int verVal;
        CompAbilityUserMagic comp;

        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
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
            Pawn caster = base.CasterPawn;           

            comp = caster.GetComp<CompAbilityUserMagic>();
            //pwrVal = comp.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overdrive_pwr").level;
            //verVal = comp.MagicData.MagicPowerSkill_Overdrive.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overdrive_ver").level;
            //ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    pwrVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
            //    verVal = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
            //}
            //if (settingsRef.AIHardMode && !caster.IsColonist)
            //{
            //    pwrVal = 3;
            //    verVal = 3;
            //}
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            Thing targetThing = null;
            if(this.currentTarget.Thing != null)
            {
                targetThing = this.currentTarget.Thing;
            }
            else
            {
                targetThing = this.currentTarget.Cell.GetFirstBuilding(this.CasterPawn.Map);
            }
            if (targetThing != null)
            {
                Pawn pawn = targetThing as Pawn;
                if (pawn != null)
                {
                    if (TM_Calc.IsRobotPawn(pawn))
                    {
                        ApplyOverdriveHD(pawn);
                    }
                    else
                    {
                        Log.Message("pawn not a robot, mechanoid, or android");
                    }
                }

                Building bldg = targetThing as Building;
                if(bldg != null)
                {                    
                    CompPower compP = bldg.GetComp<CompPower>();
                    CompPowerTrader cpt = bldg.GetComp<CompPowerTrader>();
                    if(compP != null && compP.Props.basePowerConsumption != 0 && cpt != null && cpt.powerOutputInt != 0)
                    {
                        comp.overdriveBuilding = bldg;
                        comp.overdrivePowerOutput = Mathf.RoundToInt(cpt.powerOutputInt * (2 + .6f * pwrVal * comp.arcaneDmg));
                        comp.overdriveDuration = Mathf.RoundToInt((20 + 2*pwrVal) * comp.arcaneDmg);
                        //compP.Props.basePowerConsumption *= 2;
                    }

                    Building_TurretGun bldgTurret = targetThing as Building_TurretGun;
                    if(bldgTurret != null && bldgTurret.gun != null)
                    {
                        comp.overdriveBuilding = bldgTurret;
                        comp.overdriveDuration = Mathf.RoundToInt((10 + pwrVal) * comp.arcaneDmg);
                    }
                    List<Pawn> odPawns = ModOptions.Constants.GetOverdrivePawnList();
                    if(odPawns != null)
                    {
                        odPawns.AddDistinct(caster);
                        ModOptions.Constants.SetOverdrivePawnList(odPawns);
                    }
                }
            }
            else
            {
                Log.Message("no thing targeted");
            }
            return true;
        }

        public void ApplyOverdriveHD(Pawn pawn)
        {
            ApplyHediffs(pawn);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.5f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, pawn.DrawPos, pawn.Map, .6f, .3f, 0, .3f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
        }

        private void ApplyHediffs(Pawn target)
        {
            if (this.verVal == 3)
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD_III, .5f + pwrVal);
            }
            else if (this.verVal == 2)
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD_II, .5f + pwrVal);
            }
            else if (this.verVal == 1)
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD_I, .5f + pwrVal);
            }
            else
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD, .5f + pwrVal);
            }
        }
    }
}
