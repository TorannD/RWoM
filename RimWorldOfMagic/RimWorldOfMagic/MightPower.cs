using AbilityUser;
using System.Collections.Generic;

namespace TorannMagic 
{
    public class MightPower : TMPower
    {
        public MightPower(List<AbilityDef> newAbilityDefs) : base(newAbilityDefs)
        {
            if (abilityDef == TorannMagicDefOf.TM_PsionicBarrier || abilityDef == TorannMagicDefOf.TM_PsionicBarrier_Projected)
            {
                learnCost = 2;
                costToLevel = 2;
            }

            if (abilityDef == TorannMagicDefOf.TM_PistolSpec || abilityDef == TorannMagicDefOf.TM_RifleSpec || abilityDef == TorannMagicDefOf.TM_ShotgunSpec)
            {
                learnCost = 0;
            }

            LoadLegacyClassAutocast();
        }

        private void LoadLegacyClassAutocast()
        {
            if (abilityDef == TorannMagicDefOf.TM_Headshot)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnTarget,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 36
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_ShadowStrike ||
                abilityDef == TorannMagicDefOf.TM_Spite ||
                abilityDef == TorannMagicDefOf.TM_DisablingShot)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnTarget,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 20
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_Nightshade)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnSelf,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = true,
                    targetEnemy = true,
                    targetNeutral = true,
                    targetFriendly = true,
                    targetNoFaction = true,
                    maxRange = 20,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_DoesNotHaveNightshadeHediff"
                    }
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_GraveBlade)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
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
                    minRange = 10,
                    advancedConditionDefs = new List<string>
                    {
                        "TM_3EnemiesWithin15Cells"
                    }
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_WaveOfFear ||
                abilityDef == TorannMagicDefOf.TM_BladeSpin)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnSelf,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
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
            if (abilityDef == TorannMagicDefOf.TM_Whirlwind ||
                abilityDef == TorannMagicDefOf.TM_DragonStrike ||
                abilityDef == TorannMagicDefOf.TM_PsionicDash)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 15,
                    minRange = 3
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_PhaseStrike)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = false,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 25,
                    minRange = 3
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_TigerStrike ||
                abilityDef == TorannMagicDefOf.TM_ThunderStrike)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnTarget,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 1.4f,
                };
            }
            if (abilityDef == TorannMagicDefOf.TM_PsionicBlast)
            {
                autocasting = new TMDefs.TM_Autocast
                {
                    type = TMDefs.AutocastType.OnNearby,
                    targetType = "Pawn",
                    mightUser = true,
                    magicUser = false,
                    drafted = false,
                    undrafted = true,
                    requiresLoS = true,
                    AIUsable = true,
                    includeSelf = false,
                    targetEnemy = true,
                    targetNeutral = false,
                    targetFriendly = false,
                    targetNoFaction = false,
                    maxRange = 25,
                    minRange = 3
                };
            }
        }
    }
}
