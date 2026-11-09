using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_CommanderOrders : Verb_UseAbility
    {
        internal const string MoveOrder="TM_SpiritCommanderMove", AlertOrder="TM_SpiritCommanderAlert", HoldOrder="TM_SpiritCommanderHold";
        internal static TMAbilityDef Definition(string job) => job==MoveOrder?TorannMagicDefOf.TM_MoveOut:
            job==AlertOrder?TorannMagicDefOf.TM_StayAlert:job==HoldOrder?TorannMagicDefOf.TM_HoldTheLine:null;
        internal static bool Handles(string job)=>job==MoveOrder || job==AlertOrder || job==HoldOrder;
        internal static bool Matches(string job,TMAbilityDef def) => def!=null && (job==MoveOrder &&
            (def==TorannMagicDefOf.TM_MoveOut || def==TorannMagicDefOf.TM_MoveOut_I || def==TorannMagicDefOf.TM_MoveOut_II || def==TorannMagicDefOf.TM_MoveOut_III) || job==AlertOrder &&
            (def==TorannMagicDefOf.TM_StayAlert || def==TorannMagicDefOf.TM_StayAlert_I || def==TorannMagicDefOf.TM_StayAlert_II || def==TorannMagicDefOf.TM_StayAlert_III) || job==HoldOrder &&
            (def==TorannMagicDefOf.TM_HoldTheLine || def==TorannMagicDefOf.TM_HoldTheLine_I || def==TorannMagicDefOf.TM_HoldTheLine_II || def==TorannMagicDefOf.TM_HoldTheLine_III));
        internal static bool CastFromHost(Pawn body,MightAbility power) =>
            new Verb_CommanderOrders{caster=body,Ability=power,verbProps=power.mightDef.MainVerb}.TryCastShot();

        private int pwrVal;

        private bool flagHTL = false;
        private bool flagSA = false;
        private bool flagMO = false;

        public new List<LocalTargetInfo> TargetsAoE = new List<LocalTargetInfo>();

        protected override bool TryCastShot()
        {
            bool flag = true;
            
            Pawn caster = base.CasterPawn;
            Pawn source=Ability?.Pawn??caster;
            if(caster?.Spawned!=true || caster.Dead || caster.Downed ||
                source!=caster && ClassProgression.CommanderMartialPath.Owner(caster)!=source) return false;
            flagHTL=flagSA=flagMO=false;
            this.TargetsAoE.Clear();
            //this.UpdateTargets();
            FindTargets();
            pwrVal = TM_Calc.GetSkillPowerLevel(source, this.Ability.Def as TMAbilityDef, true);
            if (this.Ability.Def == TorannMagicDefOf.TM_StayAlert || this.Ability.Def == TorannMagicDefOf.TM_StayAlert_I || this.Ability.Def == TorannMagicDefOf.TM_StayAlert_II || this.Ability.Def == TorannMagicDefOf.TM_StayAlert_III)
            {
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_StayAlert, "TM_StayAlert", "_pwr", true);
                
                flagSA = true;
            }
            if (this.Ability.Def == TorannMagicDefOf.TM_MoveOut || this.Ability.Def == TorannMagicDefOf.TM_MoveOut_I || this.Ability.Def == TorannMagicDefOf.TM_MoveOut_II || this.Ability.Def == TorannMagicDefOf.TM_MoveOut_III)
            {
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_MoveOut, "TM_MoveOut", "_pwr", true);
                
                flagMO = true;
            }
            if (this.Ability.Def == TorannMagicDefOf.TM_HoldTheLine || this.Ability.Def == TorannMagicDefOf.TM_HoldTheLine_I || this.Ability.Def == TorannMagicDefOf.TM_HoldTheLine_II || this.Ability.Def == TorannMagicDefOf.TM_HoldTheLine_III)
            {
                //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_HoldTheLine, "TM_HoldTheLine", "_pwr", true);
                flagHTL = true;
            }
            Effecter OrderED = TorannMagicDefOf.TM_CommanderOrderED.Spawn();
            OrderED.Trigger(new TargetInfo(caster.Position, caster.Map, false), new TargetInfo(caster.Position, caster.Map, false));
            OrderED.Cleanup();
            for (int i = 0; i < this.TargetsAoE.Count; i++)
            {
                Pawn newPawn = this.TargetsAoE[i].Thing as Pawn;
                if(newPawn.RaceProps.Humanlike && newPawn != caster)
                {
                    float socialChance = (float)((source.skills?.GetSkill(SkillDefOf.Social)?.Level??0) / 20f);
                    float rChance = Mathf.Clamp(socialChance * 2f, .1f, 1f);
                    if (!caster.IsColonist)
                    {
                        rChance = Mathf.Clamp(socialChance * 3f, .5f, 1f);
                        
                        if(ModOptions.Settings.Instance.AIHardMode)
                        {
                            socialChance = 1f;
                        }
                    }
                    if (Rand.Chance(ClassProgression.CommanderMartialPath.Acceptance(source,rChance)))
                    {
                        float targetCountFactor = Mathf.Clamp(5f / (float)this.TargetsAoE.Count, .1f, 1f);
                        if (flagSA)
                        {
                            if(newPawn.needs != null && newPawn.needs.rest != null)
                            {
                                newPawn.needs.rest.CurLevel += ((0.5f *targetCountFactor) + .05f * pwrVal);
                            }
                            if(newPawn.health != null && newPawn.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_StayAlertHD, (.7f * targetCountFactor) + (.1f * pwrVal));
                            }
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, newPawn.DrawPos, newPawn.Map, .8f, .2f, .1f, .1f, 0, 1f, 0, Rand.Chance(.5f) ? 0 : 180);
                        }
                        else if(flagMO)
                        {
                            if(newPawn.health != null && newPawn.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_MoveOutHD, Mathf.Clamp(.6f * targetCountFactor, .25f, .6f) + (.13f * pwrVal));
                                ClassProgression.CommanderOrders.AcceptedMove(caster,newPawn);
                            }
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, newPawn.DrawPos, newPawn.Map, .8f, .2f, .1f, .1f, 0, 1f, 0, Rand.Chance(.5f) ? 0 : 180);
                        }
                        else if(flagHTL)
                        {
                            if(newPawn.health != null && newPawn.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_HTLShieldHD, Mathf.Clamp(35f * targetCountFactor, 10f, 35f) + (5f * pwrVal));
                                HealthUtility.AdjustSeverity(newPawn, TorannMagicDefOf.TM_HoldTheLineHD, Mathf.Clamp(.7f *targetCountFactor, .3f, .7f) + (.1f * pwrVal));
                                Effecter HTLShieldED = TorannMagicDefOf.TM_HTL_EffecterED.Spawn();
                                HTLShieldED.Trigger(new TargetInfo(newPawn.Position, newPawn.Map, false), new TargetInfo(newPawn.Position, newPawn.Map, false));
                                HTLShieldED.Cleanup();
                            }
                        }

                        if (newPawn.needs != null && newPawn.needs.mood != null && newPawn.needs.mood.thoughts != null)
                        {
                            if (Rand.Chance(1f - (.2f * socialChance)))
                            {
                                float moodChance = Mathf.Clamp(TM_Calc.GetRelationsFactor(source, newPawn), .1f, .9f);
                                if (Rand.Chance(moodChance))
                                {
                                    newPawn.needs.mood.thoughts.memories.TryGainMemory(TorannMagicDefOf.TM_TakingOrdersTD, null);
                                }
                            }
                        }
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.LightningGlow, newPawn.DrawPos, newPawn.Map, .4f, .3f, .1f, .1f, 0, 0f, 0f, 0f);
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.LightningGlow, newPawn.DrawPos, newPawn.Map, .7f, .2f, .1f, .1f, 0, 0f, 0f, 0f);
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.LightningGlow, newPawn.DrawPos, newPawn.Map, 1.1f, .1f, .1f, .1f, 0, 0f, 0f, 0f);
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.ShotFlash, newPawn.DrawPos, newPawn.Map, .4f, .3f, .1f, .1f, 0, 0f, 0f, 0f);
                    }
                    else
                    {
                        if (newPawn.IsColonist)
                        {
                            MoteMaker.ThrowText(newPawn.DrawPos, newPawn.Map, "TM_IgnoredOrder".Translate(), -1);
                        }
                    }
                }
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }


        private void FindTargets()
        {
            bool flag2 = this.UseAbilityProps.TargetAoEProperties == null;
            if (flag2)
            {
                Log.Error("Tried to Cast AoE-Ability without defining a target class");
            }
            List<Pawn> list = new List<Pawn>();
            IntVec3 aoeStartPosition = this.caster.PositionHeld;
            bool flag3 = !this.UseAbilityProps.TargetAoEProperties.startsFromCaster;
            if (flag3)
            {
                aoeStartPosition = this.currentTarget.Cell;
            }

            list = (from x in this.caster.Map.mapPawns.AllPawnsSpawned
                    where x.Position.InHorDistOf(aoeStartPosition, (float)this.UseAbilityProps.TargetAoEProperties.range) && x.Faction == base.CasterPawn.Faction
                    select x).ToList<Pawn>();

            int maxTargets = this.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.maxTargets;
            List<Pawn> list2 = new List<Pawn>(list.InRandomOrder(null));
            int num = 0;
            while (num < maxTargets && num < list2.Count<Pawn>())
            {
                TargetInfo targ = new TargetInfo(list2[num]);
                bool flag6 = this.UseAbilityProps.targetParams.CanTarget(targ);
                if (flag6)
                {
                    this.TargetsAoE.Add(new LocalTargetInfo(list2[num]));
                }
                num++;
            }
        }

    }
}
