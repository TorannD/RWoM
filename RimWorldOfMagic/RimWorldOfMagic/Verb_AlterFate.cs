using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using TorannMagic.ClassProgression;


namespace TorannMagic
{
    public class Verb_AlterFate : Verb_SpiritTargetable
    {        
        private int pwrVal = 0;
        private float arcaneDmg = 1f;

        private bool confident = false;
        private bool unsure = false;
        private bool uneasy = false;
        private bool terrified = false;

        protected override bool TryCastShot()
        {
            bool applied = ApplyHostedEffect();
            PostCastShot(applied,out bool fired);
            return fired;
        }

        internal static bool Available(Pawn body, MagicAbility power)
        {
            if (body?.Map == null || power?.MagicUser == null) return false;
            var comp = power.MagicUser;
            if (comp.predictionIncidentDef != null)
            {
                var entry = ChronomancerPrediction.ResolveOwned(comp,Find.Storyteller?.incidentQueue);
                return entry != null && !entry.TriedToFire && entry.FiringIncident.parms.target == body.Map;
            }
            int rank = PowerRank(body,comp,power.magicDef);
            return rank >= 3 && RecentCondition(body.Map,rank) != null;
        }

        private static int PowerRank(Pawn body, CompAbilityUserMagic comp, TMAbilityDef def) =>
            body == comp.Pawn ? TM_Calc.GetSkillPowerLevel(body,def) :
            SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData,"TM_AlterFate_pwr");

        private static GameCondition RecentCondition(Map map,int rank) => map.GameConditionManager.ActiveConditions
            .FirstOrDefault(c => c.def.natural && c.conditionCauser == null && c.quest == null && c.TicksPassed < 2500 + 250 * rank);

        protected override bool ApplyHostedEffect()
        {
            var caster = CasterPawn;
            var owner = SpiritRebirthCasting.SkillOwner(caster,Ability);
            if (Ability is not MagicAbility power || owner != power.Pawn || !Available(caster,power)) return false;
            var comp = power.MagicUser;
            pwrVal = PowerRank(caster,comp,power.magicDef);
            arcaneDmg = comp.arcaneDmg;
            confident = unsure = uneasy = terrified = false;
            var ownedPrediction = ChronomancerPrediction.ResolveOwned(comp,Find.Storyteller.incidentQueue);
            string predictionLabel = ownedPrediction?.FiringIncident.def.label;
            if (ownedPrediction != null)
            {
                if (Rand.Chance((.25f + (.05f * pwrVal))*this.arcaneDmg)) //success end
                {
                    if (ChronomancerPrediction.CancelOwned(comp, Find.Storyteller.incidentQueue))
                    {
                        if (Rand.Chance(.6f + (.1f * pwrVal))) this.confident = true;
                        else if (Rand.Chance(.1f)) this.uneasy = true;
                        else this.unsure = true;
                    }
                }
                else if (Rand.Chance(.2f - (.02f * pwrVal))) //shifting incident
                {
                    IEnumerable<IncidentDef> enumerable = from def in DefDatabase<IncidentDef>.AllDefs
                                                          where (def != comp.predictionIncidentDef && def.TargetAllowed(this.CasterPawn.Map))
                                                          select def;
                    foreach(IncidentDef item in enumerable.InRandomOrder())
                    {
                        //Log.Message("checking incident " + item.defName);
                        IncidentDef localDef = item;
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, this.CasterPawn.Map);
                        if(localDef.Worker.CanFireNow(parms))
                        {
                            QueuedIncident iq = new QueuedIncident(new FiringIncident(localDef, null, parms), comp.predictionTick);
                            if (!ChronomancerPrediction.ReplaceOwned(comp, Find.Storyteller.incidentQueue, ownedPrediction, iq)) break;
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
                                                          select def;
                    foreach (IncidentDef item in enumerable.InRandomOrder())
                    {
                        //Log.Message("checking incident " + item.defName);
                        IncidentDef localDef = item;
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, this.CasterPawn.Map);
                        if (localDef.Worker.CanFireNow(parms))
                        {
                            QueuedIncident iq = new QueuedIncident(new FiringIncident(localDef, null, parms), comp.predictionTick + Rand.Range(-500, 10000));

                            if (!Find.Storyteller.incidentQueue.Add(iq)) continue;
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
                                                          select def;
                    foreach (IncidentDef item in enumerable.InRandomOrder())
                    {                        
                        //Log.Message("checking incident " + item.defName);
                        IncidentDef localDef = item;
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, this.CasterPawn.Map);
                        if (localDef.Worker.CanFireNow(parms))
                        {
                            int eventTick = Find.TickManager.TicksGame + Rand.Range(0, 3600);
                            QueuedIncident iq = new QueuedIncident(new FiringIncident(localDef, null, parms), eventTick);
                            if (!Find.Storyteller.incidentQueue.Add(iq)) continue;
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
                DisplayConfidence(predictionLabel);
            }
            else
            {
                var condition = RecentCondition(caster.Map,pwrVal);
                if (condition == null) return false;
                if (Rand.Chance(.25f + .05f * pwrVal))
                {
                    Messages.Message("TM_EndingGameCondition".Translate(caster.LabelShort, condition.Label), MessageTypeDefOf.PositiveEvent);
                    condition.End();
                }
                else if (Rand.Chance(.2f - .02f * pwrVal))
                {
                    var added = AddNaturalCondition(caster.Map);
                    if (added != null)
                    {
                        Messages.Message("TM_GameConditionChanged".Translate(caster.LabelShort,condition.Label,added.Label), MessageTypeDefOf.NeutralEvent);
                        condition.End();
                    }
                }
                else if (condition.def.canBePermanent && Rand.Chance(.02f))
                {
                    condition.Permanent = true;
                    Messages.Message("TM_GameConditionMadePermanent".Translate(condition.Label), MessageTypeDefOf.NeutralEvent);
                }
                else if (Rand.Chance(.15f - .015f * pwrVal))
                {
                    var added = AddNaturalCondition(caster.Map);
                    if (added != null) Messages.Message("TM_GameConditionAdded".Translate(caster.LabelShort,added.Label,condition.Label),MessageTypeDefOf.NeutralEvent);
                }
                else Messages.Message("TM_AlterGameConditionFailed".Translate(caster.LabelShort,condition.Label),MessageTypeDefOf.NeutralEvent);
            }
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 1f, .2f, 0, 1f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 2.5f, .2f, .1f, .8f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_AlterFate, CasterPawn.DrawPos, this.CasterPawn.Map, 6f, 0f, .2f, .6f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
            return true;
        } 

        private static GameCondition AddNaturalCondition(Map map)
        {
            // Use conditions that the storyteller can create unaided on this particular map.
            var eligible = DefDatabase<IncidentDef>.AllDefsListForReading.Where(d =>
                d.workerClass == typeof(IncidentWorker_MakeGameCondition) && d.gameCondition?.natural == true &&
                d.durationDays.max > 0 && d.TargetAllowed(map) && d.Worker.CanFireNow(StorytellerUtility.DefaultParmsNow(d.category,map)));
            if (!eligible.TryRandomElement(out var incident)) return null;
            var condition = GameConditionMaker.MakeCondition(incident.gameCondition,
                Mathf.Max(1,Mathf.RoundToInt(incident.durationDays.RandomInRange * 60000f)));
            map.GameConditionManager.RegisterCondition(condition);
            return condition;
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

