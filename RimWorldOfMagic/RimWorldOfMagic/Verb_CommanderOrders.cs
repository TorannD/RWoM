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

        private int pwrVal;

        private bool flagHTL = false;
        private bool flagSA = false;
        private bool flagMO = false;

        public new List<LocalTargetInfo> TargetsAoE = new List<LocalTargetInfo>();

        protected override bool TryCastShot()
        {
            bool flag = false;
            
            Pawn caster = base.CasterPawn;
            this.TargetsAoE.Clear();
            //this.UpdateTargets();
            FindTargets();
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef, true);
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
                    float socialChance = (float)(caster.skills.GetSkill(SkillDefOf.Social).Level / 20f);
                    float rChance = Mathf.Clamp(socialChance * 2f, .1f, 1f);
                    if (!caster.IsColonist)
                    {
                        rChance = Mathf.Clamp(socialChance * 3f, .5f, 1f);
                        ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                        if(settingsRef.AIHardMode)
                        {
                            socialChance = 1f;
                        }
                    }
                    if (Rand.Chance(rChance))
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
                                OrderED.Cleanup();
                            }
                        }

                        if (newPawn.needs != null && newPawn.needs.mood != null && newPawn.needs.mood.thoughts != null)
                        {
                            if (Rand.Chance(1f - (.2f * socialChance)))
                            {
                                float moodChance = Mathf.Clamp(TM_Calc.GetRelationsFactor(caster, newPawn), .1f, .9f);
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
