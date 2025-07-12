using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using Verse;
using IntVec3 = Verse.IntVec3;
using LocalTargetInfo = Verse.LocalTargetInfo;
using Pawn = Verse.Pawn;
using Thing = Verse.Thing;
using WorkTags = Verse.WorkTags;

namespace TorannMagic
{
    public partial class CompAbilityUserMagic
    {
        public void ResolveAutoCast()
        {
            bool flagCM = this.Pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage);
            bool isCustom = this.customClass != null;
            if (ModOptions.Settings.Instance.autocastEnabled && this.Pawn.jobs != null && this.Pawn.CurJob != null && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilityVerb && this.Pawn.CurJob.def != TorannMagicDefOf.TMCastAbilitySelf && 
                this.Pawn.CurJob.def != JobDefOf.Ingest && this.Pawn.CurJob.def != JobDefOf.ManTurret && this.Pawn.GetPosture() == PawnPosture.Standing && !this.Pawn.CurJob.playerForced && !this.Pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.ManaDrain) && !this.Pawn.Map.GameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
            {
                //Log.Message("pawn " + this.Pawn.LabelShort + " current job is " + this.Pawn.CurJob.def.defName);
                //non-combat (undrafted) spells
                bool castSuccess = false;
                if (this.Pawn.drafter != null && !this.Pawn.Drafted && this.Mana != null && this.Mana.CurLevelPercentage >= ModOptions.Settings.Instance.autocastMinThreshold)
                {
                    foreach (MagicPower mp in this.MagicData.MagicPowersCustomAll)
                    {
                        if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.undrafted)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                            bool canUseWithEquippedWeapon = true;
                            bool canUseIfViolentAbility = this.Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ? !tmad.MainVerb.isViolent : true;
                            if(!TM_Calc.HasResourcesForAbility(this.Pawn, tmad))
                            {
                                continue;
                            }
                            if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                            {
                                PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if (TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnSelf)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Pawn targetThing = localTarget.Pawn;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnCell && this.Pawn.CurJob.targetA != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        IntVec3 targetThing = localTarget.Cell;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnNearby)
                                {
                                    if(mp.autocasting.maxRange == 0f)
                                    {
                                        mp.autocasting.maxRange = mp.abilityDef.MainVerb.range;
                                    }
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.CurJob.targetA);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if (TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }

                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) || flagCM || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersS, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                        if (magicPower != null && magicPower.learned && magicPower.autocast && this.summonedMinions.Count() < 4)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_SummonMinion);
                            AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_SummonMinion, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer) || isCustom) && !this.recallSet)
                    {
                        if (Enumerable.Any(this.AbilityData.Powers, p => p.Def == TorannMagicDefOf.TM_TimeMark))
                        {
                            MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TimeMark);
                            if (magicPower != null && (magicPower.learned || spell_Recall) && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                            {
                                PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_TimeMark);
                                AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_TimeMark, ability, magicPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersA)
                        {
                            if (current != null && current.abilityDef != null)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_Summon || tmad == TorannMagicDefOf.TM_Summon_I || tmad == TorannMagicDefOf.TM_Summon_II || tmad == TorannMagicDefOf.TM_Summon_III) && !this.Pawn.CurJob.playerForced)
                                    {
                                        //Log.Message("evaluating " + tmad.defName);
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersA, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualManaCost(tmad) * 150;
                                            AutoCast.Summon.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if ((tmad == TorannMagicDefOf.TM_Blink || tmad == TorannMagicDefOf.TM_Blink_I || tmad == TorannMagicDefOf.TM_Blink_II || tmad == TorannMagicDefOf.TM_Blink_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersA, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualManaCost(tmad) * 240;
                                            AutoCast.Blink.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                            if (castSuccess)
                                            {
                                                goto AutoCastExit;
                                            }
                                        }
                                        if (flagCM && magicPower != null && this.spell_Blink && !magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            float minDistance = ActualManaCost(tmad) * 200;
                                            AutoCast.Blink.Evaluate(this, tmad, ability, magicPower, minDistance, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersD)
                        {
                            if (current != null && current.abilityDef != null && current.learned && !this.Pawn.CurJob.playerForced)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_Regenerate)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersD, (MagicPower x) => x.abilityDef == current.abilityDef);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Regenerate);
                                        MagicPowerSkill pwr = Enumerable.FirstOrDefault(this.MagicData.MagicPowerSkill_Regenerate, (MagicPowerSkill x) => x.label == "TM_Regenerate_pwr");
                                        if (pwr.level == 0)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration"), 10f, out castSuccess);
                                        }
                                        else if (pwr.level == 1)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_I"), 12f, out castSuccess);
                                        }
                                        else if (pwr.level == 2)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_II"), 14f, out castSuccess);
                                        }
                                        else
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_III"), 16f, out castSuccess);
                                        }
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_CureDisease)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersD, (MagicPower x) => x.abilityDef == current.abilityDef);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_CureDisease);
                                        MagicPowerSkill ver = Enumerable.FirstOrDefault(this.MagicData.MagicPowerSkill_CureDisease, (MagicPowerSkill x) => x.label == "TM_CureDisease_ver");

                                        List<string> afflictionList = new List<string>();
                                        afflictionList.Clear();
                                        foreach (TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").diseases)
                                        {
                                            if (chd.requiredSkillName == "TM_CureDisease_ver" && chd.requiredSkillLevel <= ver.level)
                                            {
                                                afflictionList.Add(chd.hediffDefname);
                                            }
                                        }
                                        //afflictionList.Add("Infection");
                                        //afflictionList.Add("WoundInfection");
                                        //afflictionList.Add("Flu");
                                        //if (ver.level >= 1)
                                        //{
                                        //    afflictionList.Add("GutWorms");
                                        //    afflictionList.Add("Malaria");
                                        //    afflictionList.Add("FoodPoisoning");
                                        //}
                                        //if (ver.level >= 2)
                                        //{
                                        //    afflictionList.Add("SleepingSickness");
                                        //    afflictionList.Add("MuscleParasites");
                                        //    afflictionList.Add("Scaria");
                                        //}
                                        //if (ver.level >= 3)
                                        //{
                                        //    afflictionList.Add("Plague");
                                        //    afflictionList.Add("Animal_Plague");
                                        //    afflictionList.Add("BloodRot");
                                        //}
                                        AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_CureDisease, ability, magicPower, afflictionList, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_RegrowLimb && spell_RegrowLimb)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersD, (MagicPower x) => x.abilityDef == current.abilityDef);
                                    bool workPriorities = true;
                                    if (this.Pawn.CurJob != null && this.Pawn.CurJob.workGiverDef != null && this.Pawn.CurJob.workGiverDef.workType != null)
                                    {
                                        workPriorities = this.Pawn.workSettings.GetPriority(this.Pawn.CurJob.workGiverDef.workType) >= this.Pawn.workSettings.GetPriority(TorannMagicDefOf.TM_Magic);
                                    }
                                    if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && workPriorities)
                                    {
                                        Area tArea = TM_Calc.GetSeedOfRegrowthArea(this.Pawn.Map, false);
                                        if (tArea != null)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_RegrowLimb);
                                            AutoCast.OnTarget_Spell.TryExecute(this, TorannMagicDefOf.TM_RegrowLimb, ability, magicPower, tArea.ActiveCells.RandomElement(), 40, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersP)
                        {
                            if (current != null && current.abilityDef != null && current.learned && !this.Pawn.CurJob.playerForced)
                            {
                                foreach(TMAbilityDef tmad in current.TMabilityDefs)
                                { 
                                    if (tmad == TorannMagicDefOf.TM_Heal)
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersP, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.HealSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if ((tmad == TorannMagicDefOf.TM_Shield || tmad == TorannMagicDefOf.TM_Shield_I || tmad == TorannMagicDefOf.TM_Shield_II || tmad == TorannMagicDefOf.TM_Shield_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersP, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.Shield.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }                                        
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersPR)
                        {
                            if (current != null && current.abilityDef != null && current.learned && !this.Pawn.CurJob.playerForced)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_AdvancedHeal)
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersPR, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.HealSpell.EvaluateMinSeverity(this, tmad, ability, magicPower, 1f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if (tmad == TorannMagicDefOf.TM_Purify)
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersPR, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Purify);
                                            MagicPowerSkill ver = Enumerable.FirstOrDefault(this.MagicData.MagicPowerSkill_Purify, (MagicPowerSkill x) => x.label == "TM_Purify_ver");
                                            AutoCast.HealPermanentSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                            List<string> afflictionList = new List<string>();
                                            afflictionList.Clear();
                                            foreach(TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").ailments)
                                            {
                                                if(chd.requiredSkillName == "TM_Purify_ver" && chd.requiredSkillLevel <= ver.level)
                                                {
                                                    afflictionList.Add(chd.hediffDefname);
                                                }
                                            }
                                            //afflictionList.Add("Cataract");
                                            //afflictionList.Add("HearingLoss");
                                            //afflictionList.Add("ToxicBuildup");
                                            //if (ver.level >= 1)
                                            //{
                                            //    afflictionList.Add("Blindness");
                                            //    afflictionList.Add("Asthma");
                                            //    afflictionList.Add("Cirrhosis");
                                            //    afflictionList.Add("ChemicalDamageModerate");
                                            //}
                                            //if (ver.level >= 2)
                                            //{
                                            //    afflictionList.Add("Frail");
                                            //    afflictionList.Add("BadBack");
                                            //    afflictionList.Add("Carcinoma");
                                            //    afflictionList.Add("ChemicalDamageSevere");
                                            //}
                                            //if (ver.level >= 3)
                                            //{
                                            //    afflictionList.Add("Alzheimers");
                                            //    afflictionList.Add("Dementia");
                                            //    afflictionList.Add("HeartArteryBlockage");
                                            //    afflictionList.Add("PsychicShock");
                                            //    afflictionList.Add("CatatonicBreakdown");
                                            //    afflictionList.Add("Abasia");
                                            //}
                                            AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, afflictionList, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                            List<string> addictionList = new List<string>();
                                            addictionList.Clear();
                                            //addictionList.Add("Alcohol");
                                            //addictionList.Add("Smokeleaf");
                                            //if (ver.level >= 1)
                                            //{
                                            //    addictionList.Add("GoJuice");
                                            //    addictionList.Add("WakeUp");
                                            //}
                                            //if (ver.level >= 2)
                                            //{
                                            //    addictionList.Add("Psychite");
                                            //}
                                            foreach (TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").addictions)
                                            {
                                                if (chd.requiredSkillName == "TM_Purify_ver" && chd.requiredSkillLevel <= ver.level)
                                                {
                                                    addictionList.Add(chd.hediffDefname);
                                                }
                                            }
                                            if (ver.level >= 3)
                                            {
                                                IEnumerable<ChemicalDef> enumerable = from def in DefDatabase<ChemicalDef>.AllDefs
                                                                                      where (true)
                                                                                      select def;
                                                foreach (ChemicalDef addiction in enumerable)
                                                {
                                                    if (addiction.defName != "ROMV_VitaeAddiction" && addiction != TorannMagicDefOf.Luciferium)
                                                    {
                                                        addictionList.AddDistinct(addiction.defName);
                                                    }
                                                }
                                            }
                                            AutoCast.CureAddictionSpell.Evaluate(this, TorannMagicDefOf.TM_Purify, ability, magicPower, addictionList, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter) || flagCM || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersE, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Transmutate);
                        bool workPriorities = true;
                        if (this.Pawn.CurJob != null && this.Pawn.CurJob.workGiverDef != null && this.Pawn.CurJob.workGiverDef.workType != null)
                        {
                            workPriorities = this.Pawn.workSettings.GetPriority(this.Pawn.CurJob.workGiverDef.workType) >= this.Pawn.workSettings.GetPriority(TorannMagicDefOf.TM_Magic);
                        }
                        if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && workPriorities)
                        {
                            Area tArea = TM_Calc.GetTransmutateArea(this.Pawn.Map, false);
                            if (tArea != null)
                            {
                                bool _out;
                                Thing transmuteThing = TM_Calc.GetTransmutableThingFromCell(tArea.ActiveCells.RandomElement(), this.Pawn, out _out, out _out, out _out, out _out, out _out);
                                if (transmuteThing != null)
                                {
                                    PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Transmutate);
                                    AutoCast.OnTarget_Spell.TryExecute(this, TorannMagicDefOf.TM_Transmutate, ability, magicPower, transmuteThing, 50, out castSuccess);
                                    if (castSuccess) goto AutoCastExit;
                                }
                            }
                        }
                    }
                    if ((this.spell_MechaniteReprogramming && this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer)) || flagCM || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_MechaniteReprogramming);
                        if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_MechaniteReprogramming);
                            List<string> afflictionList = new List<string>();
                            afflictionList.Clear();
                            foreach (TMDefs.TM_CategoryHediff chd in HediffCategoryList.Named("TM_Category_Hediffs").mechanites)
                            {
                                afflictionList.Add(chd.hediffDefname);                                
                            }
                            //afflictionList.Add("SensoryMechanites");
                            //afflictionList.Add("FibrousMechanites");
                            AutoCast.CureSpell.Evaluate(this, TorannMagicDefOf.TM_MechaniteReprogramming, ability, magicPower, afflictionList, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_Heal && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) && !isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersP, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Heal);
                            AutoCast.HealSpell.Evaluate(this, TorannMagicDefOf.TM_Heal, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_TransferMana || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TransferMana);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_TransferMana);
                            AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_TransferMana, ability, magicPower, false, false, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_SiphonMana || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SiphonMana);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_SiphonMana);
                            AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_SiphonMana, ability, magicPower, false, true, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_CauterizeWound || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CauterizeWound);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_CauterizeWound);
                            AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_CauterizeWound, ability, magicPower, 40f, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_SpellMending || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SpellMending);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_SpellMending);
                            AutoCast.SpellMending.Evaluate(this, TorannMagicDefOf.TM_SpellMending, ability, magicPower, HediffDef.Named("SpellMendingHD"), out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_Teach || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_TeachMagic);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced)
                        {
                            if (this.Pawn.CurJobDef.joyKind != null || this.Pawn.CurJobDef == JobDefOf.Wait_Wander || Pawn.CurJobDef == JobDefOf.GotoWander)
                            {
                                PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_TeachMagic);
                                AutoCast.Teach.Evaluate(this, TorannMagicDefOf.TM_TeachMagic, ability, magicPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    if (this.spell_SummonMinion && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner) && !isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersS, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonMinion);
                        if (magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && this.summonedMinions.Count() < 4)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_SummonMinion);
                            AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_SummonMinion, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_DirtDevil || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DirtDevil);
                        if (magicPower != null && magicPower.learned && magicPower.autocast && !this.Pawn.CurJob.playerForced && this.Pawn.GetRoom() != null)
                        {
                            float roomCleanliness = this.Pawn.GetRoom().GetStat(RoomStatDefOf.Cleanliness);

                            if (roomCleanliness < -2f)
                            {
                                PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_DirtDevil);
                                AutoCast.MagicAbility_OnSelfPosition.Evaluate(this, TorannMagicDefOf.TM_DirtDevil, ability, magicPower, out castSuccess);
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }
                    if (this.spell_Blink && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) && !flagCM && !isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersA, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blink);
                        if (magicPower.autocast)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Blink);
                            float minDistance = ActualManaCost(TorannMagicDefOf.TM_Blink) * 200;
                            AutoCast.Blink.Evaluate(this, TorannMagicDefOf.TM_Blink, ability, magicPower, minDistance, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                }

                //combat (drafted) spells
                if (this.Pawn.drafter != null && this.Pawn.Drafted && this.Pawn.drafter.FireAtWill && this.Pawn.CurJob.def != JobDefOf.Goto && this.Mana != null && this.Mana.CurLevelPercentage >= ModOptions.Settings.Instance.autocastCombatMinThreshold)
                {
                    foreach (MagicPower mp in this.MagicData.MagicPowersCustom)
                    {
                        if (mp.learned && mp.autocast && mp.autocasting != null && mp.autocasting.magicUser && mp.autocasting.drafted)
                        {
                            TMAbilityDef tmad = mp.TMabilityDefs[mp.level] as TMAbilityDef; // issues with index?
                            bool canUseWithEquippedWeapon = true;
                            bool canUseIfViolentAbility = this.Pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ? !tmad.MainVerb.isViolent : true;
                            if (!TM_Calc.HasResourcesForAbility(this.Pawn, tmad))
                            {
                                continue;
                            }
                            if (canUseWithEquippedWeapon && canUseIfViolentAbility)
                            {
                                PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                if (mp.autocasting.type == TMDefs.AutocastType.OnTarget && this.Pawn.TargetCurrentlyAimingAt != null && this.Pawn.TargetCurrentlyAimingAt.Thing != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if (TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnSelf)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Pawn targetThing = localTarget.Pawn;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnSelf.Evaluate(this, tmad, ability, mp, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnCell && this.Pawn.TargetCurrentlyAimingAt != null)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        IntVec3 targetThing = localTarget.Cell;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnCell.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (mp.autocasting.type == TMDefs.AutocastType.OnNearby)
                                {
                                    LocalTargetInfo localTarget = TM_Calc.GetAutocastTarget(this.Pawn, mp.autocasting, this.Pawn.TargetCurrentlyAimingAt);
                                    if (localTarget != null && localTarget.IsValid)
                                    {
                                        Thing targetThing = localTarget.Thing;
                                        if (!mp.autocasting.ValidType(mp.autocasting.GetTargetType, localTarget))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.requiresLoS && !TM_Calc.HasLoSFromTo(this.Pawn.Position, targetThing, this.Pawn, mp.autocasting.minRange, ability.Def.MainVerb.range))
                                        {
                                            continue;
                                        }
                                        if (mp.autocasting.maxRange != 0f && mp.autocasting.maxRange < (this.Pawn.Position - targetThing.Position).LengthHorizontal)
                                        {
                                            continue;
                                        }
                                        bool TE = mp.autocasting.targetEnemy && targetThing.Faction != null && targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        if (TE && targetThing is Pawn)
                                        {
                                            Pawn targetPawn = targetThing as Pawn;
                                            if (targetPawn.Downed || targetPawn.IsPrisonerInPrisonCell())
                                            {
                                                continue;
                                            }
                                        }
                                        bool TN = mp.autocasting.targetNeutral && targetThing.Faction != null && !targetThing.Faction.HostileTo(this.Pawn.Faction);
                                        bool TNF = mp.autocasting.targetNoFaction && targetThing.Faction == null;
                                        bool TF = mp.autocasting.targetFriendly && targetThing.Faction == this.Pawn.Faction;
                                        if (!(TE || TN || TF || TNF))
                                        {
                                            continue;
                                        }
                                        if (!mp.autocasting.ValidConditions(this.Pawn, targetThing))
                                        {
                                            continue;
                                        }
                                        AutoCast.MagicAbility_OnTarget.TryExecute(this, tmad, ability, mp, targetThing, mp.autocasting.minRange, out castSuccess);
                                    }
                                }
                                if (castSuccess) goto AutoCastExit;
                            }
                        }
                    }

                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersIF)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_Firebolt)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersIF, (MagicPower x) => x.abilityDef == current.abilityDef);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Firebolt);
                                        AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_Firebolt, ability, magicPower, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersHoF)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_Icebolt)
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersHoF, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Icebolt);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Icebolt);
                                            AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_Icebolt, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    else if ((tmad == TorannMagicDefOf.TM_FrostRay || tmad == TorannMagicDefOf.TM_FrostRay_I || tmad == TorannMagicDefOf.TM_FrostRay_II || tmad == TorannMagicDefOf.TM_FrostRay_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersHoF, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                       
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersSB)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_LightningBolt)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersSB, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LightningBolt);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_LightningBolt);
                                        AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_LightningBolt, ability, magicPower, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersA)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_MagicMissile || tmad == TorannMagicDefOf.TM_MagicMissile_I || tmad == TorannMagicDefOf.TM_MagicMissile_II || tmad == TorannMagicDefOf.TM_MagicMissile_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersA, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Druid) || flagCM || isCustom))
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersD)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_Poison && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersD, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Poison);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Poison);
                                        AutoCast.HediffSpell.EvaluateMinRange(this, TorannMagicDefOf.TM_Poison, ability, magicPower, HediffDef.Named("TM_Poisoned_HD"), 10, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                                if (current.abilityDef == TorannMagicDefOf.TM_Regenerate)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersD, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Regenerate);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Regenerate);
                                        MagicPowerSkill pwr = Enumerable.FirstOrDefault(this.MagicData.MagicPowerSkill_Regenerate, (MagicPowerSkill x) => x.label == "TM_Regenerate_pwr");
                                        if (pwr.level == 0)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration"), 10f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                        else if (pwr.level == 1)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_I"), 12f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                        else if (pwr.level == 2)
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_II"), 14f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                        else
                                        {
                                            AutoCast.HediffHealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_Regenerate, ability, magicPower, HediffDef.Named("TM_Regeneration_III"), 16f, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersSD)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_ShadowBolt || tmad == TorannMagicDefOf.TM_ShadowBolt_I || tmad == TorannMagicDefOf.TM_ShadowBolt_II || tmad == TorannMagicDefOf.TM_ShadowBolt_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersSD, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock) || flagCM || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersWD)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if ((tmad == TorannMagicDefOf.TM_ShadowBolt || tmad == TorannMagicDefOf.TM_ShadowBolt_I || tmad == TorannMagicDefOf.TM_ShadowBolt_II || tmad == TorannMagicDefOf.TM_ShadowBolt_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersWD, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.DamageSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }                                        
                                    }
                                }
                            }
                        }
                    }
                    if ((this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin) || flagCM || isCustom))
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersP)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                foreach (TMAbilityDef tmad in current.TMabilityDefs)
                                {
                                    if (tmad == TorannMagicDefOf.TM_Heal)
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersP, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.HealSpell.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                    if ((tmad == TorannMagicDefOf.TM_Shield || tmad == TorannMagicDefOf.TM_Shield_I || tmad == TorannMagicDefOf.TM_Shield_II || tmad == TorannMagicDefOf.TM_Shield_III))
                                    {
                                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersP, (MagicPower x) => x.abilityDef == tmad);
                                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                                        {
                                            ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == tmad);
                                            AutoCast.Shield.Evaluate(this, tmad, ability, magicPower, out castSuccess);
                                            if (castSuccess) goto AutoCastExit;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Priest) || flagCM || isCustom)
                    {
                        PawnAbility ability = null;
                        foreach (MagicPower current in this.MagicData.MagicPowersPR)
                        {
                            if (current != null && current.abilityDef != null && current.learned)
                            {
                                if (current.abilityDef == TorannMagicDefOf.TM_AdvancedHeal)
                                {
                                    MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersPR, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_AdvancedHeal);
                                    if (magicPower != null && magicPower.learned && magicPower.autocast)
                                    {
                                        ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_AdvancedHeal);
                                        AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_AdvancedHeal, ability, magicPower, 1f, out castSuccess);
                                        if (castSuccess) goto AutoCastExit;
                                    }
                                }
                            }
                        }
                    }
                    if ((this.spell_Heal && !this.Pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin)))
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersP, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Heal);
                        if (magicPower.autocast)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_Heal);
                            AutoCast.HealSpell.Evaluate(this, TorannMagicDefOf.TM_Heal, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_SiphonMana || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SiphonMana);
                        if (magicPower.learned && magicPower.autocast)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_SiphonMana);
                            AutoCast.TransferManaSpell.Evaluate(this, TorannMagicDefOf.TM_SiphonMana, ability, magicPower, true, true, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if (this.spell_CauterizeWound || isCustom)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_CauterizeWound);
                        if (magicPower.learned && magicPower.autocast)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_CauterizeWound);
                            AutoCast.HealSpell.EvaluateMinSeverity(this, TorannMagicDefOf.TM_CauterizeWound, ability, magicPower, 40f, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                    if ((this.spell_ArcaneBolt || isCustom) && this.Pawn.story.DisabledWorkTagsBackstoryAndTraits != WorkTags.Violent)
                    {
                        MagicPower magicPower = Enumerable.FirstOrDefault<MagicPower>(this.MagicData.MagicPowersStandalone, (MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ArcaneBolt);
                        if (magicPower != null && magicPower.learned && magicPower.autocast)
                        {
                            PawnAbility ability = Enumerable.FirstOrDefault(this.AbilityData.Powers, (PawnAbility x) => x.Def == TorannMagicDefOf.TM_ArcaneBolt);
                            AutoCast.DamageSpell.Evaluate(this, TorannMagicDefOf.TM_ArcaneBolt, ability, magicPower, out castSuccess);
                            if (castSuccess) goto AutoCastExit;
                        }
                    }
                }
                AutoCastExit:;
            }
        }
    }
}