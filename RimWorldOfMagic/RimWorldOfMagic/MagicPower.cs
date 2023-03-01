using AbilityUser;
using System.Collections.Generic;
using Verse;

namespace TorannMagic 
{
    public class MagicPower : TMPower
    {
        public bool requiresScroll;

        public MagicPower() {}
        public MagicPower(List<AbilityDef> newAbilityDefs, bool requireScrollToLearn = false) : base(newAbilityDefs)
        {
            requiresScroll = requireScrollToLearn;

            if (abilityDef.defName is "TM_TechnoBit" or "TM_TechnoTurret" or "TM_TechnoWeapon")
            {
                learnCost = 0;
            }
            if (abilityDef.defName is "TM_TechnoShield" or "TM_Sabotage" or "TM_Overdrive")
            {
                learnCost = 99;
            }
            if (abilityDef.defName is "TM_Firebolt" or "TM_Icebolt" or "TM_Rainmaker" or "TM_LightningBolt"
                or "TM_Blink" or "TM_Summon" or "TM_Heal" or "TM_SummonExplosive" or "TM_SummonPylon" or "TM_Poison"
                or "TM_FogOfTorment" or "TM_AdvancedHeal" or "TM_CorpseExplosion" or "TM_Entertain" or "TM_Encase"
                or "TM_EarthernHammer")
            {
                learnCost = 1;
            }
            if(abilityDef.defName is "TM_Fireball" or "TM_LightningStorm" or "TM_SummonElemental" || abilityDef == TorannMagicDefOf.TM_DeathBolt ||
               abilityDef == TorannMagicDefOf.TM_Sunfire || abilityDef == TorannMagicDefOf.TM_Refraction || abilityDef == TorannMagicDefOf.TM_ChainLightning)
            {
                learnCost = 3;
            }

            LoadLegacyClassAutocast();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref requiresScroll, "requiresScroll");
        }

        private void LoadLegacyClassAutocast()
        {
            if (abilityDef == TorannMagicDefOf.TM_RaiseUndead)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Corpse",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = true,
                    targetFriendly = true,
                    targetNoFaction = true,
                    hostileCasterOnly = true,
                    maxRange = 20
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_DeathMark)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 30
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_FogOfTorment || 
                abilityDef == TorannMagicDefOf.TM_LightningCloud || 
                abilityDef == TorannMagicDefOf.TM_IgniteBlood ||
                abilityDef == TorannMagicDefOf.TM_Attraction ||
                abilityDef == TorannMagicDefOf.TM_Repulsion ||
                abilityDef == TorannMagicDefOf.TM_HolyWrath)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 40,
                    minRange = 20,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_3EnemiesWithin15Cells"
                    }
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_Fireball ||
                abilityDef == TorannMagicDefOf.TM_Snowball ||
                abilityDef == TorannMagicDefOf.TM_Blizzard ||
                abilityDef == TorannMagicDefOf.TM_Firestorm ||
                abilityDef == TorannMagicDefOf.TM_ChainLightning)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 55,
                    minRange = 20,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_3EnemiesWithin15Cells"
                    }
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_LightningBolt ||
               abilityDef == TorannMagicDefOf.TM_ShadowBolt ||
               abilityDef == TorannMagicDefOf.TM_Firebolt ||
               abilityDef == TorannMagicDefOf.TM_Icebolt)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnTarget,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 35,
                    minRange = 5
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_MagicMissile ||
                abilityDef == TorannMagicDefOf.TM_FrostRay)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnTarget,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 22
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_Dominate)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnTarget,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 45
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_Scorn)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 290,
                    minRange = 30
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_ValiantCharge ||
                abilityDef == TorannMagicDefOf.TM_SummonTotemEarth)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 30,
                    minRange = 3
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_Overwhelm)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnSelf,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = true,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = true,
                    targetNoFaction = false,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_3EnemiesWithin15Cells"
                    }
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_SummonTotemLightning)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnSelf,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = true,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = true,
                    targetNoFaction = false,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_1EnemyWithin30Cells"
                    }
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_Enrage ||
                abilityDef == TorannMagicDefOf.TM_AMP)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = false,
                    magicUser = true,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = true,
                    targetEnemy = false,
                    targetNeutral = false,
                    targetFriendly = true,
                    targetNoFaction = false,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_1EnemyWithin15Cells"
                    }
                };
            }
        }
    }
}
