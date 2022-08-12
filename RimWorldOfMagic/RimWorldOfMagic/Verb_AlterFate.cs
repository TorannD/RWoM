using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;


namespace TorannMagic
{
    public class Verb_AlterFate : Verb_UseAbility
    {        
        private int pwrVal = 0;
        private float arcaneDmg = 1f;

        private bool confident = false;
        private bool unsure = false;
        private bool uneasy = false;
        private bool terrified = false;

        protected override bool TryCastShot()
        {
            bool flag = false;
            Pawn caster = base.CasterPawn;

            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            //pwrVal = comp.MagicData.MagicPowerSkill_AlterFate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_AlterFate_pwr").level;
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            arcaneDmg = comp.arcaneDmg;

            if(comp.predictionIncidentDef != null)
            {
                if (Rand.Chance((.25f + (.05f * pwrVal))*this.arcaneDmg)) //success end
                {
                    //Log.Message("remove event");
                    List<QueuedIncident> iQue = Traverse.Create(root: Find.Storyteller.incidentQueue).Field(name: "queuedIncidents").GetValue<List<QueuedIncident>>();
                    if(iQue != null && iQue.Count > 0)
                    {
                        for(int i = 0; i < iQue.Count; i++)
                        {
                            //Log.Message("checking ique " + iQue[i].FiringIncident.def.defName + " against " + comp.predictionIncidentDef.defName);
                            if (iQue[i].FiringIncident.def == comp.predictionIncidentDef)
                            {
                                //Log.Message("Removing incident " + iQue[i].FiringIncident.def.defName);
                                iQue.Remove(iQue[i]);
                                if (Rand.Chance(.6f + (.1f * pwrVal)))
                                {
                                    this.confident = true;
                                }
                                else if (Rand.Chance(.1f))
                                {
                                    this.uneasy = true;
                                }
                                else
                                {
                                    this.unsure = true;
                                }
                                break;
                            }
                        }
                    }
                }
                else if (Rand.Chance(.2f - (.02f * pwrVal))) //shifting incident
                {
                    //Log.Message("shift event");
                    List<QueuedIncident> iQue = Traverse.Create(root: Find.Storyteller.incidentQueue).Field(name: "queuedIncidents").GetValue<List<QueuedIncident>>();
                    if (iQue != null && iQue.Count > 0)
                    {
                        for (int i = 0; i < iQue.Count; i++)
                        {
                            //Log.Message("checking ique " + iQue[i].FiringIncident.def.defName + " against " + comp.predictionIncidentDef.defName);
                            if (iQue[i].FiringIncident.def == comp.predictionIncidentDef)
                            {
                                //Log.Message("replacing incident " + iQue[i].FiringIncident.def.defName);
                                iQue.Remove(iQue[i]);
                            }
                        }
                    }

                    IEnumerable<IncidentDef> enumerable = from def in DefDatabase<IncidentDef>.AllDefs
                                                          where (def != comp.predictionIncidentDef && def.TargetAllowed(this.CasterPawn.Map))
                                                          orderby Rand.ValueSeeded(Find.TickManager.TicksGame)
                                                          select def;
                    foreach(IncidentDef item in enumerable)
                    {
                        //Log.Message("checking incident " + item.defName);
                        IncidentDef localDef = item;
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, this.CasterPawn.Map);
                        if(localDef.Worker.CanFireNow(parms))
                        {
                            QueuedIncident iq = new QueuedIncident(new FiringIncident(localDef, null, parms), comp.predictionTick);
                            Find.Storyteller.incidentQueue.Add(iq);
                            //Log.Message("queueing incident " + localDef.defName + " in " + comp.predictionTick + " ticks");
                            //localDef.Worker.TryExecute(parms);
                            if (Rand.Chance(.6f + (.1f * pwrVal)))
                            {
                                this.uneasy = true;
                            }
                            else if (Rand.Chance(.1f))
                            {
                                this.confident = true;
                            }
                            else
                            {
                                this.unsure = true;
                            }
                            break;
                        }
                    }
                }
                else if (Rand.Chance(.11f - (.011f * pwrVal))) //add another event
                {
                    //Log.Message("add event");
                    IEnumerable<IncidentDef> enumerable = from def in DefDatabase<IncidentDef>.AllDefs
                                                          where (def != comp.predictionIncidentDef && def.TargetAllowed(this.CasterPawn.Map))
                                                          orderby Rand.ValueSeeded(Find.TickManager.TicksGame)
                                                          select def;
                    foreach (IncidentDef item in enumerable)
                    {
                        //Log.Message("checking incident " + item.defName);
                        IncidentDef localDef = item;
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, this.CasterPawn.Map);
                        if (localDef.Worker.CanFireNow(parms))
                        {
                            QueuedIncident iq = new QueuedIncident(new FiringIncident(localDef, null, parms), comp.predictionTick + Rand.Range(-500, 10000));

                            Find.Storyteller.incidentQueue.Add(iq);
                            //Log.Message("queueing incident " + localDef.defName + " in " + comp.predictionTick + " ticks");
                            //localDef.Worker.TryExecute(parms);
                            if (Rand.Chance(.4f + (.1f * pwrVal)))
                            {
                                this.uneasy = true;
                            }
                            else if (Rand.Chance(.2f))
                            {
                                this.terrified = true;
                            }
                            else
                            {
                                this.unsure = true;
                            }
                            break;
                        }
                    }
                }
                else if(Rand.Chance (.05f - (.005f * pwrVal))) //butterfly effect
                {
                    int eventCount = Rand.RangeInclusive(1, 5);
                    int butterflyCount = 0;
                    //Log.Message("butteryfly event");
                    IEnumerable<IncidentDef> enumerable = from def in DefDatabase<IncidentDef>.AllDefs
                                                          where (def.TargetAllowed(this.CasterPawn.Map))
                                                          orderby Rand.ValueSeeded(Find.TickManager.TicksGame)
                                                          select def;
                    foreach (IncidentDef item in enumerable)
                    {                        
                        //Log.Message("checking incident " + item.defName);
                        IncidentDef localDef = item;
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, this.CasterPawn.Map);
                        if (localDef.Worker.CanFireNow(parms))
                        {
                            int eventTick = Find.TickManager.TicksGame + Rand.Range(0, 3600);
                            QueuedIncident iq = new QueuedIncident(new FiringIncident(localDef, null, parms), eventTick);
                            Find.Storyteller.incidentQueue.Add(iq);
                            //Log.Message("queueing incident " + localDef.defName + " in " + eventTick + " ticks");
                            //localDef.Worker.TryExecute(parms);
                            butterflyCount++;
                            if (butterflyCount > eventCount)
                            {
                                if (Rand.Chance(.6f + (.1f * pwrVal)))
                                {
                                    this.terrified = true;
                                }
                                else if (Rand.Chance(.3f))
                                {
                                    this.uneasy = true;
                                }
                                else
                                {
                                    this.unsure = true;
                                }
                                break;
                            }
                        }
                    }
                }
                else // failed
                {
                    //Log.Message("failed event");
                    if (Rand.Chance(.6f + (.1f * pwrVal)))
                    {
                        this.unsure = true;
                    }
                    else if (Rand.Chance(.1f))
                    {
                        this.uneasy = true;
                    }
                    else
                    {
                        this.confident = true;
                    }
                    //Messages.Message("TM_AlterGameConditionFailed".Translate(this.CasterPawn.LabelShort, localGC.Label), MessageTypeDefOf.NeutralEvent);
                }
                DisplayConfidence(comp.predictionIncidentDef.label);
            }
            else if(pwrVal >= 3)
            {
                if(this.CasterPawn.Map.GameConditionManager.ActiveConditions.Count > 0)
                {
                    GameCondition localGC = null;
                    foreach(GameCondition activeCondition in this.CasterPawn.Map.GameConditionManager.ActiveConditions)
                    {
                        localGC = activeCondition;
                        if(activeCondition.TicksPassed < (2500 + (250 * pwrVal)))
                        {
                            if(Rand.Chance(.25f + (.05f * pwrVal))) //success
                            {
                                Messages.Message("TM_EndingGameCondition".Translate(this.CasterPawn.LabelShort, localGC.Label), MessageTypeDefOf.PositiveEvent);
                                localGC.End();                                
                            }
                            else if(Rand.Chance(.2f - (.02f * pwrVal))) //shifting game condition
                            {
                                IEnumerable<GameConditionDef> enumerable = from def in DefDatabase<GameConditionDef>.AllDefs
                                                                   where (def != localGC.def)
                                                                   select def;

                                GameConditionDef newGCdef = enumerable.RandomElement();
                                GameConditionMaker.MakeCondition(newGCdef);
                                Messages.Message("TM_GameConditionChanged".Translate(this.CasterPawn.LabelShort, localGC.Label, newGCdef.label), MessageTypeDefOf.NeutralEvent);
                                localGC.End();
                            }
                            else if(Rand.Chance(.02f)) //permanent
                            {
                                Messages.Message("TM_GameConditionMadePermanent".Translate(localGC.Label), MessageTypeDefOf.NeutralEvent);
                                localGC.Permanent = true;
                            }
                            else if(Rand.Chance(.15f - (.015f * pwrVal))) //add another event
                            {
                                IEnumerable<GameConditionDef> enumerable = from def in DefDatabase<GameConditionDef>.AllDefs
                                                                           where (def != localGC.def)
                                                                           select def;
                                GameConditionDef newGCdef = enumerable.RandomElement();
                                GameConditionMaker.MakeCondition(newGCdef);
                                Messages.Message("TM_GameConditionAdded".Translate(this.CasterPawn.LabelShort, newGCdef.label, localGC.Label), MessageTypeDefOf.NeutralEvent);
                            }
                            else
                            {
                                Messages.Message("TM_AlterGameConditionFailed".Translate(this.CasterPawn.LabelShort, localGC.Label), MessageTypeDefOf.NeutralEvent);
                            }
                            break;
                        }
                    }
                }
            }
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 1f, .2f, 0, 1f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 2.5f, .2f, .1f, .8f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 6f, 0f, .2f, .6f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            this.PostCastShot(flag, out flag);
            return flag;
        } 

        private void DisplayConfidence(string incidentName)
        {
            if (this.confident)
            {
                Messages.Message("TM_PredictionFeelsConfident".Translate(this.CasterPawn.LabelShort, this.CasterPawn.gender.GetPronoun(), incidentName), MessageTypeDefOf.PositiveEvent);
            }
            else if (this.unsure)
            {
                Messages.Message("TM_PredictionFeelsUnsure".Translate(this.CasterPawn.LabelShort, this.CasterPawn.gender.GetPronoun(), incidentName), MessageTypeDefOf.NeutralEvent);
            }
            else if (this.uneasy)
            {
                Messages.Message("TM_PredictionUnease".Translate(this.CasterPawn.LabelShort, this.CasterPawn.gender.GetPossessive(), incidentName), MessageTypeDefOf.NeutralEvent);
            }
            else if (this.terrified)
            {
                Messages.Message("TM_PredictionTerrified".Translate(this.CasterPawn.LabelShort, this.CasterPawn.gender.GetPronoun()), MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                Messages.Message("TM_PredictionFeelsConfident".Translate(this.CasterPawn.LabelShort, this.CasterPawn.gender.GetPronoun(), incidentName), MessageTypeDefOf.PositiveEvent);
            }
        }

    }
}