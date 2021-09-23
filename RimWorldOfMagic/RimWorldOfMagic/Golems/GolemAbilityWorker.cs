using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AbilityUser;
using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemAbilityWorker
    {
        public void ResolveAbilityUse(TMPawnGolem golem, CompGolem comp, TM_GolemAbility ability)
        {
            //    ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            //    if (golem.jobs != null && golem.CurJob != null && golem.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && golem.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf &&
            //        && golem.GetPosture() == PawnPosture.Standing)
            //    {
            //        //Log.Message("pawn " + golem.LabelShort + " current job is " + golem.CurJob.def.defName);
            //        bool castSuccess = false;
            //        if (this.Mana != null && this.Mana.CurLevelPercentage >= settingsRef.autocastMinThreshold)
            //        {
            //            foreach (MagicPower mp in this.MagicData.MagicPowersCustom)
            //            {
            //                if (mp.learned && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.AIUsable)
            //                {
            //                    //try
            //                    //{ 
            //                    TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
            //                    bool canUseWithEquippedWeapon = true;
            //                    bool canUseIfViolentAbility = golem.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ? !tmad.MainVerb.isViolent : true;
            //                    if (tmad.requiredWeaponsOrCategories != null && tmad.IsRestrictedByEquipment(golem))
            //                    {
            //                        continue;
            //                    }
            //                    if (canUseWithEquippedWeapon && canUseIfViolentAbility)
            //                    {
            //                        PawnAbility ability = this.AbilityData.Powers.FirstOrDefault((PawnAbility x) => x.Def == tmad);
            //                        LocalTargetInfo currentTarget = golem.TargetCurrentlyAimingAt != null ? golem.TargetCurrentlyAimingAt : golem.CurJob.targetA;
            //                        if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && currentTarget != null)
            //                        {
            //                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, mp.autocasting, currentTarget);
            //                            if (localTarget != null && localTarget.IsValid)
            //                            {
            //                                Thing targetThing = localTarget.Thing;
            //                                if (!(targetThing.GetType() == mp.autocasting.GetTargetType))
            //                                {
            //                                    continue;
            //                                }
            //                                if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(golem.Position, targetThing, golem, mp.autocasting.minRange, ability.Def.MainVerb.range))
            //                                {
            //                                    continue;
            //                                }
            //                                if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (golem.Position - targetThing.Position).LengthHorizontal)
            //                                {
            //                                    continue;
            //                                }
            //                                bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(golem.Faction);
            //                                if (TE && targetThing is Pawn)
            //                                {
            //                                    Pawn targetPawn = targetThing as Pawn;
            //                                    if (targetPawn.Downed || targetPawn.IsPrisoner)
            //                                    {
            //                                        continue;
            //                                    }
            //                                }
            //                                bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(golem.Faction));
            //                                if (TN && targetThing is Pawn)
            //                                {
            //                                    Pawn targetPawn = targetThing as Pawn;
            //                                    if (targetPawn.Downed || targetPawn.IsPrisoner)
            //                                    {
            //                                        continue;
            //                                    }
            //                                    if (mp.abilityDef.MainVerb.isViolent && targetThing.Faction != null && !targetPawn.InMentalState)
            //                                    {
            //                                        continue;
            //                                    }
            //                                }
            //                                bool TF = mp.autocasting.targetFriendly && targetThing.Faction == golem.Faction;
            //                                if (!(TE || TN || TF))
            //                                {
            //                                    continue;
            //                                }
            //                                //if (targetThing is Pawn)
            //                                //{
            //                                //    Pawn targetPawn = targetThing as Pawn;
            //                                //    if (targetPawn.IsPrisoner)
            //                                //    {
            //                                //        continue;
            //                                //    }
            //                                //}
            //                                if (!mp.autocasting.ValidConditions(golem, targetThing))
            //                                {
            //                                    continue;
            //                                }
            //                                AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
            //                            }
            //                        }
            //                        if (mp.autocasting.type == TMDefs.AutocastType.OnSelf)
            //                        {
            //                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, mp.autocasting, golem);
            //                            if (localTarget != null && localTarget.IsValid)
            //                            {
            //                                Pawn targetThing = localTarget.Pawn;
            //                                if (!(targetThing.GetType() == mp.autocasting.GetTargetType))
            //                                {
            //                                    continue;
            //                                }
            //                                if (!mp.autocasting.ValidConditions(golem, targetThing))
            //                                {
            //                                    continue;
            //                                }
            //                                AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
            //                            }
            //                        }
            //                        if (mp.autocasting.type == TMDefs.AutocastType.OnCell && currentTarget != null)
            //                        {
            //                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, mp.autocasting, currentTarget);
            //                            if (localTarget != null && localTarget.IsValid)
            //                            {
            //                                IntVec3 targetThing = localTarget.Cell;
            //                                if (!(targetThing.GetType() == mp.autocasting.GetTargetType))
            //                                {
            //                                    continue;
            //                                }
            //                                if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(golem.Position, targetThing, golem, mp.autocasting.minRange, ability.Def.MainVerb.range))
            //                                {
            //                                    continue;
            //                                }
            //                                if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (golem.Position - targetThing).LengthHorizontal)
            //                                {
            //                                    continue;
            //                                }
            //                                if (!mp.autocasting.ValidConditions(golem, targetThing))
            //                                {
            //                                    continue;
            //                                }
            //                                AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
            //                            }
            //                        }
            //                        if (mp.autocasting.type == TMDefs.AutocastType.OnNearby)
            //                        {
            //                            LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, mp.autocasting, currentTarget);
            //                            if (localTarget != null && localTarget.IsValid)
            //                            {
            //                                Thing targetThing = localTarget.Thing;
            //                                if (!(targetThing.GetType() == mp.autocasting.GetTargetType))
            //                                {
            //                                    continue;
            //                                }
            //                                if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(golem.Position, targetThing, golem, mp.autocasting.minRange, ability.Def.MainVerb.range))
            //                                {
            //                                    continue;
            //                                }
            //                                if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (golem.Position - targetThing.Position).LengthHorizontal)
            //                                {
            //                                    continue;
            //                                }
            //                                bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(golem.Faction);
            //                                if (TE && targetThing is Pawn)
            //                                {
            //                                    Pawn targetPawn = targetThing as Pawn;
            //                                    if (targetPawn.Downed || targetPawn.IsPrisoner)
            //                                    {
            //                                        continue;
            //                                    }
            //                                }
            //                                bool TN = mp.autocasting.targetNeutral && (targetThing.Faction == null || !targetThing.Faction.HostileTo(golem.Faction));
            //                                if (TN && targetThing is Pawn)
            //                                {
            //                                    Pawn targetPawn = targetThing as Pawn;
            //                                    if (targetPawn.Downed || targetPawn.IsPrisoner)
            //                                    {
            //                                        continue;
            //                                    }
            //                                    if (mp.abilityDef.MainVerb.isViolent && targetThing.Faction != null && !targetPawn.InMentalState)
            //                                    {
            //                                        continue;
            //                                    }
            //                                }
            //                                bool TF = mp.autocasting.targetFriendly && targetThing.Faction == golem.Faction;
            //                                if (!(TE || TN || TF))
            //                                {
            //                                    continue;
            //                                }
            //                                //if (targetThing is Pawn)
            //                                //{
            //                                //    Pawn targetPawn = targetThing as Pawn;
            //                                //    if (targetPawn.IsPrisoner)
            //                                //    {
            //                                //        continue;
            //                                //    }
            //                                //}
            //                                if (!mp.autocasting.ValidConditions(golem, targetThing))
            //                                {
            //                                    continue;
            //                                }
            //                                AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
            //                            }
            //                        }
            //                    }
            //                    //}
            //                    //catch
            //                    //{
            //                    //    Log.Message("no index found at " + mp.level + " for " + mp.abilityDef.defName);
            //                    //}
            //                }
            //                if (castSuccess) goto AIAutoCastExit;
            //            }
            //            AIAutoCastExit:;
            //        }
            //    }
        }
    }
}
