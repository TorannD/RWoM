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
    public class Verb_TimeMark : ClassProgression.Verb_SpiritTargetable
    {

        private int pwrVal = 0;
        CompAbilityUserMagic comp;
        protected override bool TryCastShot()
        {
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body, Ability);
            comp = owner?.GetCompAbilityUserMagic();
            burstShotsLeft = 0;
            if (body?.Spawned != true || body.Downed || comp == null) return false;
            pwrVal = ClassProgression.SpiritRebirthRuntime.SavedRank(comp.MagicData,"TM_Recall_pwr");
            SetRecallHediffs();
            SetRecallNeeds();
            SetRecallPosition();
            comp.recallBodyId = body.thingIDNumber;
            comp.recallSet = true;
            comp.recallExpiration = Find.TickManager.TicksGame + Mathf.RoundToInt(50000 * (1 + .2f * pwrVal));
            if (owner != body)
            {
                TM_ClassState.AddRuntimeAbility(comp, TorannMagicDefOf.TM_Recall);
                comp.recallSpell = true;
            }
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, body.DrawPos, body.Map, 1f, .2f, 0, 1f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            FleckMaker.ThrowHeatGlow(body.Position, body.Map, 1.4f);
            return true;
        }

        // Old marks may contain plain Hediff snapshots of stateful effects. Check
        // the definition as well as the live instance before restoring or removing one.
        internal static bool PreservesHealthEffect(Hediff effect)
        {
            var def = effect?.def;
            if (def?.defName == null) return true;
            var kind = def.hediffClass;
            return effect is ClassProgression.Hediff_ClassDiscipline or ClassProgression.Hediff_SpiritRebirthForm or Hediff_Addiction
                || kind == typeof(ClassProgression.Hediff_ClassDiscipline) || kind == typeof(ClassProgression.Hediff_SpiritRebirthForm)
                || kind != null && typeof(Hediff_Addiction).IsAssignableFrom(kind)
                || def.defName == "LuciferiumHigh" || def == TorannMagicDefOf.TM_SpiritPossessionHD
                || def.comps?.Any(c => c?.compClass != null && typeof(IThingHolder).IsAssignableFrom(c.compClass)) == true;
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
                if (!PreservesHealthEffect(this.CasterPawn.health.hediffSet.hediffs[i]) && !this.CasterPawn.health.hediffSet.hediffs[i].IsPermanent() && this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_MagicUserHD &&
                    !this.CasterPawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_HediffEnchantment") && !this.CasterPawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_Artifact") &&
                    this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_MightUserHD && this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_BloodHD && 
                    this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_ChiHD && this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_PsionicHD &&
                    this.CasterPawn.health.hediffSet.hediffs[i].def != TorannMagicDefOf.TM_SpiritPossessionHD)
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
            comp.recallNeedValues = new List<float>();
            foreach (var need in CasterPawn.needs.AllNeeds)
            {
                if (need.def.defName == "Chemical_Luciferium" || (comp.Pawn != CasterPawn && need is Need_Mana)) continue;
                comp.recallNeedDefnames.Add(need.def.defName);
                comp.recallNeedValues.Add(need.CurLevel);
            }
            if (comp.Pawn != CasterPawn && comp.Mana != null)
            {
                comp.recallNeedDefnames.Add(comp.Mana.def.defName);
                comp.recallNeedValues.Add(comp.Mana.CurLevel);
            }
        }

        private void SetRecallPosition()
        {
            comp.recallPosition = this.CasterPawn.Position;
            comp.recallMap = this.CasterPawn.Map;
            //Log.Message("position set");
        }
    }
}
