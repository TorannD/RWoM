using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic 
{
    public class MightPower : IExposable
    {
        public List<AbilityDef> TMabilityDefs;
        public TMDefs.TM_Autocast autocasting;

        public int ticksUntilNextCast = -1;

        public int level;

        public bool learned = false;
        public bool autocast = false;
        public int learnCost = 2;
        private int interactionTick = 0;
        public int maxLevel = 3;
        public int costToLevel = 1;

        public bool AutoCast
        {
            get
            {
                return autocast;
            }
            set
            {
                if (interactionTick < Find.TickManager.TicksGame)
                {
                    autocast = value;
                    interactionTick = Find.TickManager.TicksGame + 5;
                }
            }
        }

        private void SetMaxLevel()
        {
            this.maxLevel = this.TMabilityDefs.Count - 1;
        }

        public AbilityDef abilityDescDef
        {
            get
            {                
                return this.abilityDef;
            }
        }

        public AbilityDef nextLevelAbilityDescDef
        {
            get
            {
                return this.nextLevelAbilityDef;                
            }
        }

        public AbilityDef abilityDef
        {
            get
            {
                if (TMabilityDefs != null && TMabilityDefs.Count > 0)
                {
                    SetMaxLevel();
                    if (level <= 0)
                    {
                        return this.TMabilityDefs[0];
                    }
                    else if (level >= maxLevel)
                    {
                        return this.TMabilityDefs[maxLevel];
                    }
                    return this.TMabilityDefs[level];                    
                }
                return null;
            }
        }

        public AbilityDef nextLevelAbilityDef
        {
            get
            {
                if (TMabilityDefs == null || TMabilityDefs.Count == 0) return null;
                SetMaxLevel();
                return GetAbilityDef(level >= maxLevel ? maxLevel : level + 1);
            }
        }

        public Texture2D Icon
        {
            get
            {
                return this.abilityDef?.uiIcon;
            }
        }

        public AbilityDef GetAbilityDef(int index)
        {
            if (TMabilityDefs == null || TMabilityDefs.Count == 0) return null;
            return TMabilityDefs[index >= 0 && index < TMabilityDefs.Count ? index : 0];
        }

        public AbilityDef HasAbilityDef(AbilityDef defToFind)
        {
            return this.TMabilityDefs?.FirstOrDefault(x => x == defToFind);
        }

        public MightPower()
        {
        }

        public MightPower(List<AbilityDef> newAbilityDefs)
        {
            this.level = 0;
            this.TMabilityDefs = newAbilityDefs;
            this.maxLevel = newAbilityDefs.Count - 1;            

            if (this.abilityDef == TorannMagicDefOf.TM_PsionicBarrier || this.abilityDef == TorannMagicDefOf.TM_PsionicBarrier_Projected)
            {
                this.learnCost = 2;
                this.costToLevel = 2;
                this.maxLevel = 1;
            }

            if (this.abilityDef == TorannMagicDefOf.TM_PistolSpec || this.abilityDef == TorannMagicDefOf.TM_RifleSpec || this.abilityDef == TorannMagicDefOf.TM_ShotgunSpec)
            {
                this.learnCost = 0;
            }

            LoadLegacyClassAutocast();
            if (this.autocasting == null || TMDefs.TM_AutocastDefaults.NeverAutocast(this.abilityDef as TMAbilityDef))
            {
                this.autocasting = TMDefs.TM_AutocastDefaults.For(this.abilityDef as TMAbilityDef, true);
            }

        }

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.learned, "learned", true, false);
            Scribe_Values.Look<bool>(ref this.autocast, "autocast", false, false);
            Scribe_Values.Look<int>(ref this.learnCost, "learnCost", 2, false);
            Scribe_Values.Look<int>(ref this.costToLevel, "costToLevel", 1, false);
            Scribe_Values.Look<int>(ref this.level, "level", 0, false);
            Scribe_Values.Look<int>(ref this.maxLevel, "maxLevel", 3, false);
            Scribe_Values.Look<int>(ref this.ticksUntilNextCast, "ticksUntilNextCast", -1, false);
            Scribe_Collections.Look<AbilityDef>(ref this.TMabilityDefs, "TMabilityDefs", (LookMode)4, null);
            Scribe_Deep.Look<TMDefs.TM_Autocast>(ref this.autocasting, "autocasting", new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                SpellSanitizer.Sanitize(this);
                if (autocasting == null)
                    autocasting = TMDefs.TM_AutocastDefaults.For(abilityDef as TMAbilityDef, true);
            }
        }

        public void LoadLegacyClassAutocast()
        {
            if (this.abilityDef == TorannMagicDefOf.TM_Headshot)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_ShadowStrike ||
                this.abilityDef == TorannMagicDefOf.TM_Spite ||
                this.abilityDef == TorannMagicDefOf.TM_DisablingShot)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Nightshade)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_GraveBlade)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_WaveOfFear ||
                this.abilityDef == TorannMagicDefOf.TM_BladeSpin)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Whirlwind ||
                this.abilityDef == TorannMagicDefOf.TM_DragonStrike ||
                this.abilityDef == TorannMagicDefOf.TM_PsionicDash)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_PhaseStrike)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_TigerStrike ||
                this.abilityDef == TorannMagicDefOf.TM_ThunderStrike)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_PsionicBlast)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
