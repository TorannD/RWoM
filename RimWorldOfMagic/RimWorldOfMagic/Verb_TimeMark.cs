using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    public class Verb_TimeMark : Verb_UseAbility  
    {

        private int pwrVal = 0;
        CompAbilityUserMagic comp;
        Map map;

        protected override bool TryCastShot()
        {
            bool result = false;
            map = this.CasterPawn.Map;
            comp = this.CasterPawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = comp.MagicData.MagicPowerSkill_Recall.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Recall_pwr");
            pwrVal = pwr.level;

            if (this.CasterPawn != null && !this.CasterPawn.Downed && comp != null)
            {
                SetRecallHediffs();
                SetRecallNeeds();
                SetRecallPosition();
                comp.recallSet = true;
                comp.recallExpiration = Mathf.RoundToInt(Find.TickManager.TicksGame + (20 * 2500 * (1 + (.2f * pwrVal))));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 1f, .2f, 0, 1f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
                FleckMaker.ThrowHeatGlow(this.CasterPawn.Position, this.CasterPawn.Map, 1.4f);
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }

            this.burstShotsLeft = 0;
            return result;
        }

        private void SetRecallHediffs()
        {
            comp.recallHediffList = new List<Hediff>();
            comp.recallHediffList.Clear();
            comp.recallHediffDefSeverityList = new List<float>();
            comp.recallHediffDefSeverityList.Clear();
            comp.recallHediffDefTicksRemainingList = new List<int>();
            comp.recallHediffDefTicksRemainingList.Clear();
            comp.recallInjuriesList = new List<Hediff_Injury>();
            comp.recallInjuriesList.Clear();
            for (int i = 0; i < this.CasterPawn.health.hediffSet.hediffs.Count; i++)
            {
                if (!this.CasterPawn.health.hediffSet.hediffs[i].IsPermanent() && this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_MagicUserHD && 
                    !this.CasterPawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_HediffEnchantment") && !this.CasterPawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_Artifact") &&
                    this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_MightUserHD && this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_BloodHD && 
                    this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_ChiHD && this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_PsionicHD)
                {
                    if (this.CasterPawn.health.hediffSet.hediffs[i] is Hediff_Injury)
                    {                        
                        Hediff_Injury rhd = this.CasterPawn.health.hediffSet.hediffs[i] as Hediff_Injury;
                        Hediff_Injury hediff = new Hediff_Injury();
                        //hediff = TM_Calc.Clone<Hediff>(this.CasterPawn.health.hediffSet.hediffs[i]);
                        hediff.def = rhd.def;
                        hediff.Part = rhd.Part;
                        Traverse.Create(root: hediff).Field(name: "visible").SetValue(rhd.Visible);
                        Traverse.Create(root: hediff).Field(name: "severityInt").SetValue(rhd.Severity);
                        //hediff.Severity = rhd.Severity;                        
                        hediff.ageTicks = rhd.ageTicks;
                        comp.recallInjuriesList.Add(hediff);
                    }
                    else if(this.CasterPawn.health.hediffSet.hediffs[i] is Hediff_MissingPart || this.CasterPawn.health.hediffSet.hediffs[i] is Hediff_AddedPart || this.CasterPawn.health.hediffSet.hediffs[i].def.defName == "PsychicAmplifier")
                    {
                        //do nothing
                    }
                    else if(this.CasterPawn.health.hediffSet.hediffs[i] is Hediff_Addiction)
                    {
                        //Hediff_Addiction rhd = this.CasterPawn.health.hediffSet.hediffs[i] as Hediff_Addiction;                        
                    }
                    else if(this.CasterPawn.health.hediffSet.hediffs[i].def.defName == "LuciferiumHigh")
                    {
                        //do nothing
                    }
                    else
                    {
                        Hediff rhd = this.CasterPawn.health.hediffSet.hediffs[i] as Hediff;
                        //Log.Message("sev def is " + rhd.def.defName);
                        if (rhd != null)
                        {
                            Hediff hediff = new Hediff();                            
                            //hediff = TM_Calc.Clone<Hediff>(this.CasterPawn.health.hediffSet.hediffs[i]);
                            hediff.def = rhd.def;
                            hediff.loadID = rhd.loadID;
                            hediff.Part = rhd.Part;
                            //foreach(HediffComp hdc in rhd.comps)
                            //{                            
                            //    if(hdc is HediffComp_Disappears)
                            //    {
                            //        HediffComp_Disappears rhd_comp = hdc as HediffComp_Disappears;
                            //        HediffComp_Disappears hdc_d = new HediffComp_Disappears();
                            //        hdc_d.ticksToDisappear = rhd_comp.ticksToDisappear;
                            //        hediff.comps.Add(hdc_d);
                            //    }
                            //    else
                            //    {
                            //        hediff.comps.Add(hdc);
                            //    }
                            //}
                            //Traverse.Create(root: hediff).Field(name: "visible").SetValue(rhd.Visible);
                            //Traverse.Create(root: hediff).Field(name: "severityInt").SetValue(rhd.Severity);
                            hediff.ageTicks = rhd.ageTicks;
                            //Log.Message("saving hediff " + hediff.def.defName + " with severity " + rhd.Severity);
                            comp.recallHediffList.Add(hediff);
                            comp.recallHediffDefSeverityList.Add(rhd.Severity);
                            HediffComp_Disappears hdc_d = rhd.TryGetComp<HediffComp_Disappears>();
                            if(hdc_d != null)
                            {
                                //Log.Message("hediff has disappears at ticks " + hdc_d.ticksToDisappear);
                                comp.recallHediffDefTicksRemainingList.Add(hdc_d.ticksToDisappear);
                            }
                            else
                            {
                                comp.recallHediffDefTicksRemainingList.Add(-1);
                            }
                        }
                        //else
                        //{
                        //    Hediff _rhd = this.CasterPawn.health.hediffSet.hediffs[i];
                        //    if(_rhd != null)
                        //    {
                        //        HediffWithComps hediff = new HediffWithComps();
                        //        hediff.def = rhd.def;
                        //        hediff.loadID = rhd.loadID;
                        //        hediff.Part = rhd.Part;
                        //        Traverse.Create(root: hediff).Field(name: "visible").SetValue(rhd.Visible);
                        //        Traverse.Create(root: hediff).Field(name: "severityInt").SetValue(rhd.Severity);
                        //        hediff.Severity = rhd.Severity;
                        //        hediff.ageTicks = rhd.ageTicks;
                        //        comp.recallHediffList.Add(hediff);
                        //    }
                        //}
                    }
                    //Log.Message("adding " + this.CasterPawn.health.hediffSet.hediffs[i].def + " at severity " + this.CasterPawn.health.hediffSet.hediffs[i].Severity);
                }
            }
            //Log.Message("hediffs set");
        }

        private void SetRecallNeeds()
        {
            comp.recallNeedDefnames = new List<string>();
            comp.recallNeedDefnames.Clear();
            comp.recallNeedValues = new List<float>();
            comp.recallNeedValues.Clear();
            //comp.recallNeedValues = new List<Need>();
            //comp.recallNeedValues.Clear();
            for (int i = 0; i < this.CasterPawn.needs.AllNeeds.Count; i++)
            {
                //Log.Message("" + this.CasterPawn.needs.AllNeeds[i].def.defName);
                if (this.CasterPawn.needs.AllNeeds[i].def.defName != "Chemical_Luciferium")
                {                    
                    comp.recallNeedDefnames.Add(this.CasterPawn.needs.AllNeeds[i].def.defName);
                    comp.recallNeedValues.Add(this.CasterPawn.needs.AllNeeds[i].CurLevel);
                }
                //comp.recallNeedValues.Add(TM_Calc.Clone<Need>(this.CasterPawn.needs.AllNeeds[i]));
            }
            //Log.Message("needs set");
        }

        private void SetRecallPosition()
        {
            comp.recallPosition = this.CasterPawn.Position;
            comp.recallMap = this.CasterPawn.Map;
            //Log.Message("position set");
        }
    }
}
