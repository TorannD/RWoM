﻿using System;
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
        public static List<TM_GolemAbilityDef> JobAbilities(Pawn p)
        {
            List<TM_GolemAbilityDef> tmpJobs = new List<TM_GolemAbilityDef>();
            tmpJobs.Clear();
            CompGolem cg = p.TryGetComp<CompGolem>();
            if(cg != null)
            {
                foreach(TM_GolemUpgrade gu in cg.Upgrades)
                {
                    if(gu.golemUpgradeDef.ability != null && gu.currentLevel > 0 && gu.golemUpgradeDef.ability.jobDef != null)
                    {
                        tmpJobs.Add(gu.golemUpgradeDef.ability);
                    }
                }
            }
            return tmpJobs;
        }

        public static IEnumerable<Thing> PotentialAvailableWorkThingsForJob(Pawn p, JobDef job)
        {
            if(job == TorannMagicDefOf.JobDriver_MechaMine)
            {
                IEnumerable<Thing> tmpThings = from t in p.Map.listerThings.AllThings
                                               where (t.def.mineable && !t.Fogged() && !t.IsForbidden(p) && t.Map.designationManager.AllDesignationsAt(t.Position).Any((Designation x) => x.def == DesignationDefOf.Mine))
                                               select t;               
                return tmpThings;   
            }
            if(job == TorannMagicDefOf.JobDriver_FleshHarvest)
            {
                IEnumerable<Thing> tmpThings = from t in p.Map.listerThings.AllThings
                                               where (t.def.plant != null && t.def.plant.Harvestable && !t.Fogged() && !t.IsForbidden(p) && t.Map.designationManager.AllDesignationsOn(t).Any((Designation x) => x.def == DesignationDefOf.HarvestPlant))
                                               select t;
                return tmpThings;                
            }
            if (job == TorannMagicDefOf.JobDriver_FleshChop)
            {
                IEnumerable<Thing> tmpThings = from t in p.Map.listerThings.AllThings
                                               where (t.def.plant != null && !t.Fogged() && !t.IsForbidden(p) && t.Map.designationManager.AllDesignationsOn(t).Any((Designation x) => x.def == DesignationDefOf.CutPlant))
                                               select t;
                return tmpThings;
            }
            return null;
        }

        public static bool CanUseAbility(Pawn p, TM_GolemAbilityDef ability)
        {
            if(p.DestroyedOrNull() || ability == null)
            {
                return false;
            }
            if(p.Dead || p.Downed)
            {
                return false;
            }
            if(p.Map == null)
            {
                return false;
            }
            if(ability.interruptsJob && p.jobs != null && p.CurJob.playerForced)
            {
                return false;
            }
            //if(ability.autocasting == null)
            //{
            //    return false;
            //}
            CompGolem cg = p.TryGetComp<CompGolem>();
            if (ability.requiredNeed != null)
            {
                float eff = 1f;
                if (p.needs != null)
                {
                    Need n = p.needs.TryGetNeed(ability.requiredNeed);
                    Need_GolemEnergy n_ge = p.needs.TryGetNeed(ability.requiredNeed) as Need_GolemEnergy;
                    
                    if(n_ge != null)
                    {
                        eff = n_ge.energyEfficiency;
                    }
                    if(n != null && n.CurLevel >= ((ability.needCost * cg.EnergyCostModifier)/eff))
                    {
                        return true;                        
                    }
                    return false;
                }
                return false;
            }
            if(ability.requiredHediff != null)
            {
                if(p.health != null && p.health.hediffSet != null && p.health.hediffSet.HasHediff(ability.requiredHediff))
                {
                    Hediff hd = p.health.hediffSet.GetFirstHediffOfDef(ability.requiredHediff);
                    if(hd != null && hd.Severity >= (ability.hediffCost * cg.EnergyCostModifier))
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return true;
        }

        private static bool IsValidOtherTarget(TMPawnGolem golem, TM_GolemAbility ability, LocalTargetInfo localTarget)
        {
            if (!ability.golemAbilityDef.autocasting.ValidType(ability.golemAbilityDef.autocasting.GetTargetType, localTarget))
            {
                return false;
            }
            
            Thing targetThing = localTarget.Thing;
            if (ability.golemAbilityDef.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(golem.Position, targetThing, golem, ability.golemAbilityDef.autocasting.minRange, ability.golemAbilityDef.autocasting.maxRange) && targetThing != golem)
            {
                return false;
            }
            if (ability.golemAbilityDef.autocasting.maxRange != 0f && ability.golemAbilityDef.autocasting.maxRange < (golem.Position - targetThing.Position).LengthHorizontal)
            {
                return false;
            }
            
            // Autocast targeting early exit conditions
            bool targetIsHostile = targetThing.Faction != null && targetThing.Faction.HostileTo(golem.Faction);
            bool targetNeutral = ability.golemAbilityDef.autocasting.targetNeutral && !targetIsHostile;
            bool targetEnemy = ability.golemAbilityDef.autocasting.targetEnemy && targetIsHostile;
            if (targetThing is Pawn targetPawn)
            {
                if (targetEnemy && (targetPawn.Downed || targetPawn.IsPrisoner))
                {
                    return false;
                }

                if (targetNeutral)
                {
                    if (ability.golemAbilityDef.isViolent && targetThing.Faction != null && !targetPawn.InMentalState)
                    {
                        return false;
                    }
                }
            }
            
            bool targetFriendly = ability.golemAbilityDef.autocasting.targetFriendly && targetThing.Faction == golem.Faction;
            if (!(targetEnemy || targetNeutral || targetFriendly))
            {
                return false;
            }
            return ability.golemAbilityDef.autocasting.ValidConditions(golem, targetThing);
        }

        public static void ResolveAbilityUse(TMPawnGolem golem, CompGolem comp, TM_GolemAbility ability, out bool success)
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            success = false;
            if (CanUseAbility(golem, ability.golemAbilityDef))
            {
                LocalTargetInfo currentTarget = golem.TargetCurrentlyAimingAt != null ? golem.TargetCurrentlyAimingAt : golem.CurJob.targetA;
                if (ability.golemAbilityDef.autocasting != null)
                {
                    if (ability.golemAbilityDef.autocasting.type == TMDefs.AutocastType.OnTarget && currentTarget != null)
                    {
                        LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, ability.golemAbilityDef.autocasting, currentTarget);
                        if (localTarget != null && localTarget.IsValid && IsValidOtherTarget(golem, ability, localTarget))
                        {
                            comp.StartAbility(ability, localTarget.Thing);
                        }
                    }
                    if (ability.golemAbilityDef.autocasting.type == TMDefs.AutocastType.OnSelf)
                    {
                        LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, ability.golemAbilityDef.autocasting, golem);
                        if (localTarget != null && localTarget.IsValid)
                        {
                            Pawn targetThing = localTarget.Pawn;
                            if (!ability.golemAbilityDef.autocasting.ValidType(ability.golemAbilityDef.autocasting.GetTargetType, localTarget))
                            {
                                return;
                            }
                            if (!ability.golemAbilityDef.autocasting.ValidConditions(golem, targetThing))
                            {
                                return;
                            }
                            comp.StartAbility(ability, targetThing);
                        }
                    }
                    if (ability.golemAbilityDef.autocasting.type == TMDefs.AutocastType.OnCell && currentTarget != null)
                    {
                        LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, ability.golemAbilityDef.autocasting, currentTarget);
                        if (localTarget != null && localTarget.IsValid)
                        {
                            IntVec3 targetThing = localTarget.Cell;
                            if (!ability.golemAbilityDef.autocasting.ValidType(ability.golemAbilityDef.autocasting.GetTargetType, localTarget))
                            {
                                return;
                            }
                            if (ability.golemAbilityDef.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(golem.Position, targetThing, golem, ability.golemAbilityDef.autocasting.minRange, ability.golemAbilityDef.autocasting.maxRange))
                            {
                                return;
                            }
                            if (ability.golemAbilityDef.autocasting.maxRange != 0f && ability.golemAbilityDef.autocasting.maxRange < (golem.Position - targetThing).LengthHorizontal)
                            {
                                return;
                            }
                            if (!ability.golemAbilityDef.autocasting.ValidConditions(golem, targetThing))
                            {
                                return;
                            }
                            comp.StartAbility(ability, targetThing);
                        }
                    }
                    if (ability.golemAbilityDef.autocasting.type == TMDefs.AutocastType.OnNearby)
                    {
                        LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(golem, ability.golemAbilityDef.autocasting, currentTarget);
                        if (localTarget != null && localTarget.IsValid && IsValidOtherTarget(golem, ability, localTarget))
                        {
                            comp.StartAbility(ability, localTarget.Thing);
                        }
                    }
                }
                else
                {
                    foreach (CompProperties_GolemAbilityEffect effectDef in ability.golemAbilityDef.effects)
                    {
                        if (effectDef.CanApplyOn(currentTarget, golem, ability.golemAbilityDef))
                        {
                            comp.StartAbility(ability, currentTarget);
                            break;
                        }
                    }
                }
            }
        }
    }
}
