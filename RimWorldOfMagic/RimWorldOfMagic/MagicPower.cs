using AbilityUser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic 
{
    public class MagicPower : IExposable
    {
        public List<AbilityDef> TMabilityDefs;
        public TMDefs.TM_Autocast autocasting;

        public int ticksUntilNextCast = -1;

        public int level = 0;
        public bool learned = false;
        public bool autocast = false;
        public int learnCost = 2;
        private int interactionTick = 0;
        public bool requiresScroll = false;
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
                SetMaxLevel();
                if (TMabilityDefs != null && TMabilityDefs.Count > 0)
                {
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
                SetMaxLevel();
                if ((this.level + 1) >= this.maxLevel)
                {
                    return this.TMabilityDefs[maxLevel];
                }
                else
                {
                    return this.TMabilityDefs[level + 1];
                }               
            }
        }

        public Texture2D Icon
        {
            get
            {
                return this.abilityDef.uiIcon;
            }
        }

        public AbilityDef GetAbilityDef(int index)
        {
            try
            {
                return this.TMabilityDefs[index];
            }
            catch
            {
                return this.TMabilityDefs[0];
            }            
        }

        public AbilityDef HasAbilityDef(AbilityDef defToFind)
        {
            return this.TMabilityDefs.FirstOrDefault((AbilityDef x) => x == defToFind);
        }

        public MagicPower()
        {
        }

        public MagicPower(List<AbilityDef> newAbilityDefs, bool requireScrollToLearn = false)
        {
            this.level = 0;
            this.requiresScroll = requireScrollToLearn;
            this.TMabilityDefs = newAbilityDefs;
            this.maxLevel = newAbilityDefs.Count - 1;

            if (this.abilityDef.defName == "TM_TechnoBit" || this.abilityDef.defName == "TM_TechnoTurret" || this.abilityDef.defName == "TM_TechnoWeapon")
            {
                this.learnCost = 0;
            }

            if (this.abilityDef.defName == "TM_TechnoShield" || this.abilityDef.defName == "TM_Sabotage" || this.abilityDef.defName == "TM_Overdrive")
            {
                this.learnCost = 99;
            }

            if (this.abilityDef.defName == "TM_Firebolt" || this.abilityDef.defName == "TM_Icebolt" || this.abilityDef.defName == "TM_Rainmaker" || this.abilityDef.defName == "TM_LightningBolt" ||
                this.abilityDef.defName == "TM_Blink" || this.abilityDef.defName == "TM_Summon" || this.abilityDef.defName == "TM_Heal" || this.abilityDef.defName == "TM_SummonExplosive" ||
                this.abilityDef.defName == "TM_SummonPylon" || this.abilityDef.defName == "TM_Poison" || this.abilityDef.defName == "TM_FogOfTorment" || this.abilityDef.defName == "TM_AdvancedHeal" ||
                this.abilityDef.defName == "TM_CorpseExplosion" || this.abilityDef.defName == "TM_Entertain" || this.abilityDef.defName == "TM_Encase" || this.abilityDef.defName == "TM_EarthernHammer")
            {
                this.learnCost = 1;
            }

            if(this.abilityDef.defName == "TM_Fireball" || this.abilityDef.defName == "TM_LightningStorm" || this.abilityDef.defName == "TM_SummonElemental" || this.abilityDef == TorannMagicDefOf.TM_DeathBolt ||
                this.abilityDef == TorannMagicDefOf.TM_Sunfire || this.abilityDef == TorannMagicDefOf.TM_Refraction || this.abilityDef == TorannMagicDefOf.TM_ChainLightning)
            {
                this.learnCost = 3;
            }

            LoadLegacyClassAutocast();
        }

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.learned, "learned", true, false);
            Scribe_Values.Look<bool>(ref this.autocast, "autocast", false, false);
            Scribe_Values.Look<bool>(ref this.requiresScroll, "requiresScroll", false, false);
            Scribe_Values.Look<int>(ref this.learnCost, "learnCost", 2, false);
            Scribe_Values.Look<int>(ref this.costToLevel, "costToLevel", 1, false);
            Scribe_Values.Look<int>(ref this.level, "level", 0, false);
            Scribe_Values.Look<int>(ref this.maxLevel, "maxLevel", 3, false);
            Scribe_Values.Look<int>(ref this.ticksUntilNextCast, "ticksUntilNextCast", -1, false);
            Scribe_Collections.Look<AbilityDef>(ref this.TMabilityDefs, "TMabilityDefs", LookMode.Def, null);
            Scribe_Deep.Look<TMDefs.TM_Autocast>(ref this.autocasting, "autocasting", new object[0]);
        }

        public void LoadLegacyClassAutocast()
        {
            if (this.abilityDef == TorannMagicDefOf.TM_RaiseUndead)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
                    maxRange = 20
                };
            }
            if (this.abilityDef == TorannMagicDefOf.TM_DeathMark)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_FogOfTorment || 
                this.abilityDef == TorannMagicDefOf.TM_LightningCloud || 
                this.abilityDef == TorannMagicDefOf.TM_IgniteBlood ||
                this.abilityDef == TorannMagicDefOf.TM_Attraction ||
                this.abilityDef == TorannMagicDefOf.TM_Repulsion ||
                this.abilityDef == TorannMagicDefOf.TM_HolyWrath)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Fireball ||
                this.abilityDef == TorannMagicDefOf.TM_Snowball ||
                this.abilityDef == TorannMagicDefOf.TM_Blizzard ||
                this.abilityDef == TorannMagicDefOf.TM_Firestorm ||
                this.abilityDef == TorannMagicDefOf.TM_ChainLightning)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_LightningBolt ||
               this.abilityDef == TorannMagicDefOf.TM_ShadowBolt ||
               this.abilityDef == TorannMagicDefOf.TM_Firebolt ||
               this.abilityDef == TorannMagicDefOf.TM_Icebolt)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_MagicMissile ||
                this.abilityDef == TorannMagicDefOf.TM_FrostRay)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Dominate)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Scorn)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_ValiantCharge ||
                this.abilityDef == TorannMagicDefOf.TM_SummonTotemEarth)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Overwhelm)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_SummonTotemLightning)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
            if (this.abilityDef == TorannMagicDefOf.TM_Enrage ||
                this.abilityDef == TorannMagicDefOf.TM_AMP)
            {
                this.autocasting = new TMDefs.TM_Autocast
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
