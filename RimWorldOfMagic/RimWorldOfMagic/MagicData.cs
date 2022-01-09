using AbilityUser;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic
{
    public class MagicData : IExposable
    {
        private Pawn magicPawn;
        private int magicUserLevel = 0;
        private int magicAbilityPoints = 0;
        private int magicUserXP = 1;
        private int ticksToLearnMagicXP = -1;
        public bool initialized = false;
        private Faction affiliation = null;
        private int ticksAffiliation = 0;
        private bool isNecromancer = false;
        private int dominationCount = 0;

        public bool customPowersInitialized = false;
        public List<MagicPower> magicPowerCustom;
        public List<MagicPower> MagicPowersCustom  //supports customs abilities
        {
            get
            {
                bool flag = this.magicPowerCustom == null;// || !this.customPowersInitialized;
                if (flag)
                {
                    this.customPowersInitialized = true;
                    IEnumerable<TM_CustomPowerDef> enumerable = TM_Data.CustomMagePowerDefs();

                    if (magicPowerCustom == null || magicPowerCustom.Count <= 0)
                    {
                        this.magicPowerCustom = new List<MagicPower>();
                        this.magicPowerCustom.Clear();
                    }
                    foreach(TM_CustomPowerDef current in enumerable)
                    {
                        bool newPower = false;
                        List<AbilityUser.AbilityDef> abilityList = current.customPower.abilityDefs;
                        bool requiresScroll = current.customPower.requiresScroll;
                        MagicPower mp = new MagicPower(abilityList, requiresScroll);
                        mp.maxLevel = current.customPower.maxLevel;
                        mp.learnCost = current.customPower.learnCost;
                        mp.costToLevel = current.customPower.costToLevel;
                        mp.autocasting = current.customPower.autocasting;
                        if (!magicPowerCustom.Any(a => a.GetAbilityDef(0) == mp.GetAbilityDef(0)))
                        {                            
                            newPower = true;
                        }
                        bool hasSkills = false;
                        if (current.customPower.skills != null)
                        {
                            foreach (TM_CustomSkill skill in current.customPower.skills)
                            {
                                if (skill != null)
                                {
                                    MagicPowerSkill mps = new MagicPowerSkill(skill.label, skill.description);
                                    mps.levelMax = skill.levelMax;
                                    mps.costToLevel = skill.costToLevel;
                                    if (!MagicPowerSkill_Custom.Any(b => b.label == mps.label) && !AllMagicPowerSkills.Any(b => b.label == mps.label))
                                    {
                                        MagicPowerSkill_Custom.Add(mps);
                                    }
                                    hasSkills = true;
                                }
                            }
                        }
                        if (newPower)
                        {
                            if (hasSkills)
                            {
                                magicPowerCustom.Add(mp);
                            }
                            else if(!MagicPowersCustomStandalone.Any((a => a.GetAbilityDef(0) == mp.GetAbilityDef(0))))
                            {
                                MagicPowersCustomStandalone.Add(mp);
                            }
                        }
                        if(hasSkills && current.customPower.chaosMageUseable && !AllMagicPowersForChaosMage.Contains(mp))
                        {
                            this.AllMagicPowersForChaosMage.Add(mp);
                        }
                    }
                    this.allMagicPowerSkillsList = null; //force rediscovery and caching to include custom defs
                }
                return this.magicPowerCustom;
            }
        }

        public List<MagicPower> magicPowerCustomStandalone;
        public List<MagicPower> MagicPowersCustomStandalone  //supports customs abilities
        {
            get
            {
                bool flag = this.magicPowerCustomStandalone == null;
                if (flag)
                {
                    this.magicPowerCustomStandalone = new List<MagicPower>();
                    this.magicPowerCustomStandalone.Clear();
                }
                return this.magicPowerCustomStandalone;
            }
        }

        public List<MagicPowerSkill> magicPowerSkill_Custom;
        public List<MagicPowerSkill> MagicPowerSkill_Custom
        {
            get
            {
                bool flag = this.magicPowerSkill_Custom == null;
                if (flag)
                {
                    this.magicPowerSkill_Custom = new List<MagicPowerSkill>();
                    this.magicPowerSkill_Custom.Clear();
                }
                return this.magicPowerSkill_Custom;
            }
        }

        public List<MagicPower> magicPowerIF;
        public List<MagicPower> magicPowerHoF;
        public List<MagicPower> magicPowerSB;
        public List<MagicPower> magicPowerA;
        public List<MagicPower> magicPowerP;
        public List<MagicPower> magicPowerS;
        public List<MagicPower> magicPowerD;
        public List<MagicPower> magicPowerN;
        public List<MagicPower> magicPowerPR;
        public List<MagicPower> magicPowerB;
        public List<MagicPower> magicPowerWD;
        public List<MagicPower> magicPowerSD;
        public List<MagicPower> magicPowerG;
        public List<MagicPower> magicPowerT;
        public List<MagicPower> magicPowerBM;
        public List<MagicPower> magicPowerE;
        public List<MagicPower> magicPowerC;
        public List<MagicPower> magicPowerW;
        public List<MagicPower> magicPowerCM;
        public List<MagicPower> magicPowerShadow;
        public List<MagicPower> magicPowerBrightmage;
        public List<MagicPower> magicPowerShaman;
        public List<MagicPower> magicPowerGolemancer;

        public List<MagicPower> magicPowerStandalone;        

        public List<MagicPowerSkill> magicPowerSkill_global_regen;
        public List<MagicPowerSkill> magicPowerSkill_global_eff;
        public List<MagicPowerSkill> magicPowerSkill_global_spirit;

        public List<MagicPowerSkill> magicPowerSkill_WandererCraft;
        public List<MagicPowerSkill> magicPowerSkill_Cantrips;
        
        public List<MagicPowerSkill> magicPowerSkill_RayofHope;
        public List<MagicPowerSkill> magicPowerSkill_Firebolt;
        public List<MagicPowerSkill> magicPowerSkill_Fireball;
        public List<MagicPowerSkill> magicPowerSkill_Fireclaw;
        public List<MagicPowerSkill> magicPowerSkill_Firestorm;

        public List<MagicPowerSkill> magicPowerSkill_Soothe;
        public List<MagicPowerSkill> magicPowerSkill_Rainmaker;
        public List<MagicPowerSkill> magicPowerSkill_Icebolt;
        public List<MagicPowerSkill> magicPowerSkill_FrostRay;
        public List<MagicPowerSkill> magicPowerSkill_Snowball;
        public List<MagicPowerSkill> magicPowerSkill_Blizzard;

        public List<MagicPowerSkill> magicPowerSkill_AMP;
        public List<MagicPowerSkill> magicPowerSkill_LightningBolt;
        public List<MagicPowerSkill> magicPowerSkill_LightningCloud;
        public List<MagicPowerSkill> magicPowerSkill_LightningStorm;
        public List<MagicPowerSkill> magicPowerSkill_EyeOfTheStorm;

        public List<MagicPowerSkill> magicPowerSkill_Shadow;
        public List<MagicPowerSkill> magicPowerSkill_Blink;
        public List<MagicPowerSkill> magicPowerSkill_Summon;
        public List<MagicPowerSkill> magicPowerSkill_MagicMissile;
        public List<MagicPowerSkill> magicPowerSkill_Teleport;
        public List<MagicPowerSkill> magicPowerSkill_FoldReality;

        public List<MagicPowerSkill> magicPowerSkill_P_RayofHope;
        public List<MagicPowerSkill> magicPowerSkill_Heal;
        public List<MagicPowerSkill> magicPowerSkill_Shield;
        public List<MagicPowerSkill> magicPowerSkill_ValiantCharge;
        public List<MagicPowerSkill> magicPowerSkill_Overwhelm;
        public List<MagicPowerSkill> magicPowerSkill_HolyWrath;
        
        public List<MagicPowerSkill> magicPowerSkill_SummonMinion;        
        public List<MagicPowerSkill> magicPowerSkill_SummonPylon;
        public List<MagicPowerSkill> magicPowerSkill_SummonExplosive;
        public List<MagicPowerSkill> magicPowerSkill_SummonElemental;
        public List<MagicPowerSkill> magicPowerSkill_SummonPoppi;

        public List<MagicPowerSkill> magicPowerSkill_Poison;
        public List<MagicPowerSkill> magicPowerSkill_SootheAnimal;
        public List<MagicPowerSkill> magicPowerSkill_Regenerate;
        public List<MagicPowerSkill> magicPowerSkill_CureDisease;
        public List<MagicPowerSkill> magicPowerSkill_RegrowLimb;

        public List<MagicPowerSkill> magicPowerSkill_RaiseUndead;
        public List<MagicPowerSkill> magicPowerSkill_DeathMark;
        public List<MagicPowerSkill> magicPowerSkill_FogOfTorment;
        public List<MagicPowerSkill> magicPowerSkill_ConsumeCorpse;
        public List<MagicPowerSkill> magicPowerSkill_CorpseExplosion;
        public List<MagicPowerSkill> magicPowerSkill_LichForm;
        public List<MagicPowerSkill> magicPowerSkill_DeathBolt;

        public List<MagicPowerSkill> magicPowerSkill_AdvancedHeal;
        public List<MagicPowerSkill> magicPowerSkill_Purify;
        public List<MagicPowerSkill> magicPowerSkill_HealingCircle;
        public List<MagicPowerSkill> magicPowerSkill_BestowMight;
        public List<MagicPowerSkill> magicPowerSkill_Resurrection;

        public List<MagicPowerSkill> magicPowerSkill_BardTraining;
        public List<MagicPowerSkill> magicPowerSkill_Entertain;
        public List<MagicPowerSkill> magicPowerSkill_Inspire;
        public List<MagicPowerSkill> magicPowerSkill_Lullaby;
        public List<MagicPowerSkill> magicPowerSkill_BattleHymn;

        public List<MagicPowerSkill> magicPowerSkill_SoulBond;
        public List<MagicPowerSkill> magicPowerSkill_ShadowBolt;
        public List<MagicPowerSkill> magicPowerSkill_Dominate;
        public List<MagicPowerSkill> magicPowerSkill_Repulsion;
        public List<MagicPowerSkill> magicPowerSkill_Attraction;
        public List<MagicPowerSkill> magicPowerSkill_Scorn;             
        public List<MagicPowerSkill> magicPowerSkill_PsychicShock;
        //public List<MagicPowerSkill> magicPowerSkill_SummonDemon;

        public List<MagicPowerSkill> magicPowerSkill_Stoneskin;
        public List<MagicPowerSkill> magicPowerSkill_Encase;
        public List<MagicPowerSkill> magicPowerSkill_EarthSprites;
        public List<MagicPowerSkill> magicPowerSkill_EarthernHammer;
        public List<MagicPowerSkill> magicPowerSkill_Sentinel;
        public List<MagicPowerSkill> magicPowerSkill_Meteor;

        public List<MagicPowerSkill> magicPowerSkill_TechnoBit;
        public List<MagicPowerSkill> magicPowerSkill_TechnoTurret;
        public List<MagicPowerSkill> magicPowerSkill_TechnoWeapon;
        public List<MagicPowerSkill> magicPowerSkill_TechnoShield;
        public List<MagicPowerSkill> magicPowerSkill_Sabotage;
        public List<MagicPowerSkill> magicPowerSkill_Overdrive;
        public List<MagicPowerSkill> magicPowerSkill_OrbitalStrike;

        public List<MagicPowerSkill> magicPowerSkill_BloodGift;         //BloodGift & BloodSacrifice
        public List<MagicPowerSkill> magicPowerSkill_IgniteBlood;
        public List<MagicPowerSkill> magicPowerSkill_BloodForBlood;
        public List<MagicPowerSkill> magicPowerSkill_BloodShield;
        public List<MagicPowerSkill> magicPowerSkill_Rend;
        public List<MagicPowerSkill> magicPowerSkill_BloodMoon;

        public List<MagicPowerSkill> magicPowerSkill_EnchantedBody;  //EnchantedBody & EnchantedAura
        public List<MagicPowerSkill> magicPowerSkill_Transmutate;
        public List<MagicPowerSkill> magicPowerSkill_EnchanterStone;
        public List<MagicPowerSkill> magicPowerSkill_EnchantWeapon;
        public List<MagicPowerSkill> magicPowerSkill_Polymorph;
        public List<MagicPowerSkill> magicPowerSkill_Shapeshift;

        public List<MagicPowerSkill> magicPowerSkill_Prediction;
        public List<MagicPowerSkill> magicPowerSkill_AlterFate;
        public List<MagicPowerSkill> magicPowerSkill_AccelerateTime;
        public List<MagicPowerSkill> magicPowerSkill_ReverseTime;
        public List<MagicPowerSkill> magicPowerSkill_ChronostaticField;
        public List<MagicPowerSkill> magicPowerSkill_Recall;       

        public List<MagicPowerSkill> magicPowerSkill_ChaosTradition;

        public List<MagicPowerSkill> magicPowerSkill_ShadowWalk;

        public List<MagicPowerSkill> magicPowerSkill_LightLance;
        public List<MagicPowerSkill> magicPowerSkill_Sunfire;
        public List<MagicPowerSkill> magicPowerSkill_LightBurst;
        public List<MagicPowerSkill> magicPowerSkill_LightSkip;
        public List<MagicPowerSkill> magicPowerSkill_Refraction;
        public List<MagicPowerSkill> magicPowerSkill_SpiritOfLight;

        public List<MagicPowerSkill> magicPowerSkill_Totems;            //healing totem, hex/buff totem, elemental totem
        public List<MagicPowerSkill> magicPowerSkill_ChainLightning;    
        public List<MagicPowerSkill> magicPowerSkill_Enrage;
        public List<MagicPowerSkill> magicPowerSkill_Hex;
        public List<MagicPowerSkill> magicPowerSkill_SpiritWolves;
        public List<MagicPowerSkill> magicPowerSkill_GuardianSpirit;

        public List<MagicPowerSkill> magicPowerSkill_Golemancy;            
        public List<MagicPowerSkill> magicPowerSkill_RuneCarving;
        public List<MagicPowerSkill> magicPowerSkill_Branding;
        public List<MagicPowerSkill> magicPowerSkill_SigilSurge;
        public List<MagicPowerSkill> magicPowerSkill_SigilDrain;
        public List<MagicPowerSkill> magicPowerSkill_LivingWall;

        public List<MagicPowerSkill> MagicPowerSkill_global_regen
        {
            get
            {
                bool flag = this.magicPowerSkill_global_regen == null;
                if (flag)
                {
                    this.magicPowerSkill_global_regen = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_global_regen_pwr", "TM_global_regen_pwr_desc")
                    };
                }
                return this.magicPowerSkill_global_regen;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_global_eff
        {
            get
            {
                bool flag = this.magicPowerSkill_global_eff == null;
                if (flag)
                {
                    this.magicPowerSkill_global_eff = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_global_eff_pwr", "TM_global_eff_pwr_desc")
                    };
                }
                return this.magicPowerSkill_global_eff;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_global_spirit
        {
            get
            {
                bool flag = this.magicPowerSkill_global_spirit == null;
                if (flag)
                {
                    this.magicPowerSkill_global_spirit = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_global_spirit_pwr", "TM_global_spirit_pwr_desc")
                    };
                }
                return this.magicPowerSkill_global_spirit;
            }
        }

        private List<MagicPower> magicPower;
        public List<MagicPower> MagicPowers
        {
            get
            {
                bool flag = this.magicPower == null;
                if (flag)
                {
                    this.magicPower = new List<MagicPower>
                    {

                    };
                }
                return this.magicPower;
            }
            
        }

        public List<MagicPower> MagicPowersStandalone  //spells needed for magicpower reference during autocast
        {
            get
            {
                bool flag = this.magicPowerStandalone == null;
                if (flag)
                {
                    this.magicPowerStandalone = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TransferMana
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SiphonMana
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SpellMending
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_CauterizeWound
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TeachMagic
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EnchantedAura
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_MechaniteReprogramming
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_DirtDevil
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Heater
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Cooler
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_PowerNode
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Sunlight
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_DryGround
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_WetGround
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ChargeBattery
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SmokeCloud
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Extinguish
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EMP
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ManaShield
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ArcaneBarrier
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Flight
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_FertileLands
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Blur
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BlankMind
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ArcaneBolt
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightningTrap
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Invisibility
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BriarPatch
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TimeMark
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_MageLight
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_NanoStimulant
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Ignite
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SnapFreeze
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ShapeshiftDW
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightSkipMass
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightSkipGlobal
                        }),
                    };
                }
                return this.magicPowerStandalone;
            }
        }

        public List<MagicPower> MagicPowersW
        {
            get
            {
                bool flag = this.magicPowerW == null;
                if (flag)
                {
                    this.magicPowerW = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_WandererCraft
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Cantrips
                        }),
                    };
                }
                return this.magicPowerW;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_WandererCraft
        {
            get
            {
                bool flag = this.magicPowerSkill_WandererCraft == null;
                if (flag)
                {
                    this.magicPowerSkill_WandererCraft = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_WandererCraft_pwr", "TM_WandererCraft_pwr_desc"),
                        new MagicPowerSkill("TM_WandererCraft_eff", "TM_WandererCraft_eff_desc"),
                        new MagicPowerSkill("TM_WandererCraft_ver", "TM_WandererCraft_ver_desc")
                    };
                }
                return this.magicPowerSkill_WandererCraft;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Cantrips
        {
            get
            {
                bool flag = this.magicPowerSkill_Cantrips == null;
                if (flag)
                {
                    this.magicPowerSkill_Cantrips = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Cantrips_pwr", "TM_Cantrips_pwr_desc"),
                        new MagicPowerSkill("TM_Cantrips_eff", "TM_Cantrips_eff_desc"),
                        new MagicPowerSkill("TM_Cantrips_ver", "TM_Cantrips_ver_desc")
                    };
                }
                return this.magicPowerSkill_Cantrips;
            }
        }

        public List<MagicPower> MagicPowersIF
        {
            get
            {
                bool flag = this.magicPowerIF == null;
                if (flag)
                {
                    this.magicPowerIF = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_RayofHope,
                            TorannMagicDefOf.TM_RayofHope_I,
                            TorannMagicDefOf.TM_RayofHope_II,
                            TorannMagicDefOf.TM_RayofHope_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Firebolt
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Fireclaw
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Fireball
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Firestorm
                        },true),
                    };
                }
                return this.magicPowerIF;
            }
        }

        public List<MagicPowerSkill> MagicPowerSkill_RayofHope
        {
            get
            {
                bool flag = this.magicPowerSkill_RayofHope == null;
                if (flag)
                {
                    this.magicPowerSkill_RayofHope = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_RayofHope_eff", "TM_RayofHope_eff_desc")
                    };
                }
                return this.magicPowerSkill_RayofHope;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Firebolt
        {
            get
            {
                bool flag = this.magicPowerSkill_Firebolt == null;
                if (flag)
                {
                    this.magicPowerSkill_Firebolt = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Firebolt_pwr", "TM_Firebolt_pwr_desc"),
                        new MagicPowerSkill("TM_Firebolt_eff", "TM_Firebolt_eff_desc")
                    };
                }
                return this.magicPowerSkill_Firebolt;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Fireball
        {
            get
            {
                bool flag = this.magicPowerSkill_Fireball == null;
                if (flag)
                {
                    this.magicPowerSkill_Fireball = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Fireball_pwr", "TM_Fireball_pwr_desc"),
                        new MagicPowerSkill("TM_Fireball_eff", "TM_Fireball_eff_desc"),
                        new MagicPowerSkill("TM_Fireball_ver", "TM_Fireball_ver_desc")
                    };
                }
                return this.magicPowerSkill_Fireball;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Fireclaw
        {
            get
            {
                bool flag = this.magicPowerSkill_Fireclaw == null;
                if (flag)
                {
                    this.magicPowerSkill_Fireclaw = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Fireclaw_pwr", "TM_Fireclaw_pwr_desc"),
                        new MagicPowerSkill("TM_Fireclaw_eff", "TM_Fireclaw_eff_desc"),
                        new MagicPowerSkill("TM_Fireclaw_ver", "TM_Fireclaw_ver_desc")
                    };
                }
                return this.magicPowerSkill_Fireclaw;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Firestorm
        {
            get
            {
                bool flag = this.magicPowerSkill_Firestorm == null;
                if (flag)
                {
                    this.magicPowerSkill_Firestorm = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Firestorm_pwr", "TM_Firestorm_pwr_desc"),
                        new MagicPowerSkill("TM_Firestorm_eff", "TM_Firestorm_eff_desc"),
                        new MagicPowerSkill("TM_Firestorm_ver", "TM_Firestorm_ver_desc")
                    };
                }
                return this.magicPowerSkill_Firestorm;
            }
        }

        public List<MagicPower> MagicPowersHoF
        {
            get
            {
                bool flag = this.magicPowerHoF == null;
                if (flag)
                {
                    this.magicPowerHoF = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Soothe,
                            TorannMagicDefOf.TM_Soothe_I,
                            TorannMagicDefOf.TM_Soothe_II,
                            TorannMagicDefOf.TM_Soothe_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Rainmaker
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Icebolt
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_FrostRay,
                            TorannMagicDefOf.TM_FrostRay_I,
                            TorannMagicDefOf.TM_FrostRay_II,
                            TorannMagicDefOf.TM_FrostRay_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Snowball
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Blizzard
                        },true),
                    };
                }
                return this.magicPowerHoF;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Soothe
        {
            get
            {
                bool flag = this.magicPowerSkill_Soothe == null;
                if (flag)
                {
                    this.magicPowerSkill_Soothe = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Soothe_eff", "TM_Soothe_eff_desc")
                    };
                }
                return this.magicPowerSkill_Soothe;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Icebolt
        {
            get
            {
                bool flag = this.magicPowerSkill_Icebolt == null;
                if (flag)
                {
                    this.magicPowerSkill_Icebolt = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Icebolt_pwr", "TM_Icebolt_pwr_desc"),
                        new MagicPowerSkill("TM_Icebolt_eff", "TM_Icebolt_eff_desc"),
                        new MagicPowerSkill("TM_Icebolt_ver", "TM_Icebolt_ver_desc")
                    };
                }
                return this.magicPowerSkill_Icebolt;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_FrostRay
        {
            get
            {
                bool flag = this.magicPowerSkill_FrostRay == null;
                if (flag)
                {
                    this.magicPowerSkill_FrostRay = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_FrostRay_eff", "TM_FrostRay_eff_desc" )
                    };
                }
                return this.magicPowerSkill_FrostRay;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Snowball
        {
            get
            {
                bool flag = this.magicPowerSkill_Snowball == null;
                if (flag)
                {
                    this.magicPowerSkill_Snowball = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Snowball_pwr", "TM_Snowball_pwr_desc" ),
                        new MagicPowerSkill("TM_Snowball_eff", "TM_Snowball_eff_desc" ),
                        new MagicPowerSkill("TM_Snowball_ver", "TM_Snowball_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Snowball;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Rainmaker
        {
            get
            {
                bool flag = this.magicPowerSkill_Rainmaker == null;
                if (flag)
                {
                    this.magicPowerSkill_Rainmaker = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Rainmaker_eff", "TM_Rainmaker_eff_desc" )
                    };
                }
                return this.magicPowerSkill_Rainmaker;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Blizzard
        {
            get
            {
                bool flag = this.magicPowerSkill_Blizzard == null;
                if (flag)
                {
                    this.magicPowerSkill_Blizzard = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Blizzard_pwr", "TM_Blizzard_pwr_desc" ),
                        new MagicPowerSkill("TM_Blizzard_eff", "TM_Blizzard_eff_desc" ),
                        new MagicPowerSkill("TM_Blizzard_ver", "TM_Blizzard_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Blizzard;
            }
        }

        public List<MagicPower> MagicPowersSB
        {
            get
            {
                bool flag = this.magicPowerSB == null;
                if (flag)
                {
                    this.magicPowerSB = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_AMP,
                            TorannMagicDefOf.TM_AMP_I,
                            TorannMagicDefOf.TM_AMP_II,
                            TorannMagicDefOf.TM_AMP_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightningBolt
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightningCloud
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightningStorm
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EyeOfTheStorm
                        },true),
                    };
                }
                return this.magicPowerSB;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_AMP
        {
            get
            {
                bool flag = this.magicPowerSkill_AMP == null;
                if (flag)
                {
                    this.magicPowerSkill_AMP = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_AMP_eff", "TM_AMP_eff_desc" )
                    };
                }
                return this.magicPowerSkill_AMP;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LightningBolt
        {
            get
            {
                bool flag = this.magicPowerSkill_LightningBolt == null;
                if (flag)
                {
                    this.magicPowerSkill_LightningBolt = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LightningBolt_pwr", "TM_LightningBolt_pwr_desc" ),
                        new MagicPowerSkill("TM_LightningBolt_eff", "TM_LightningBolt_eff_desc" ),
                        new MagicPowerSkill("TM_LightningBolt_ver", "TM_LightningBolt_ver_desc" )
                    };
                }
                return this.magicPowerSkill_LightningBolt;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LightningCloud
        {
            get
            {
                bool flag = this.magicPowerSkill_LightningCloud == null;
                if (flag)
                {
                    this.magicPowerSkill_LightningCloud = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LightningCloud_pwr", "TM_LightningCloud_pwr_desc" ),
                        new MagicPowerSkill("TM_LightningCloud_eff", "TM_LightningCloud_eff_desc" ),
                        new MagicPowerSkill("TM_LightningCloud_ver", "TM_LightningCloud_ver_desc" )
                    };
                }
                return this.magicPowerSkill_LightningCloud;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LightningStorm
        {
            get
            {
                bool flag = this.magicPowerSkill_LightningStorm == null;
                if (flag)
                {
                    this.magicPowerSkill_LightningStorm = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LightningStorm_pwr", "TM_LightningStorm_pwr_desc" ),
                        new MagicPowerSkill("TM_LightningStorm_eff", "TM_LightningStorm_eff_desc" ),
                        new MagicPowerSkill("TM_LightningStorm_ver", "TM_LightningStorm_ver_desc" )
                    };
                }
                return this.magicPowerSkill_LightningStorm;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_EyeOfTheStorm
        {
            get
            {
                bool flag = this.magicPowerSkill_EyeOfTheStorm == null;
                if (flag)
                {
                    this.magicPowerSkill_EyeOfTheStorm = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_EyeOfTheStorm_pwr", "TM_EyeOfTheStorm_pwr_desc" ),
                        new MagicPowerSkill("TM_EyeOfTheStorm_eff", "TM_EyeOfTheStorm_eff_desc" ),
                        new MagicPowerSkill("TM_EyeOfTheStorm_ver", "TM_EyeOfTheStorm_ver_desc" )
                    };
                }
                return this.magicPowerSkill_EyeOfTheStorm;
            }
        }

        public List<MagicPower> MagicPowersA
        {
            get
            {
                bool flag = this.magicPowerA == null;
                if (flag)
                {
                    this.magicPowerA = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Shadow,
                            TorannMagicDefOf.TM_Shadow_I,
                            TorannMagicDefOf.TM_Shadow_II,
                            TorannMagicDefOf.TM_Shadow_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_MagicMissile,
                            TorannMagicDefOf.TM_MagicMissile_I,
                            TorannMagicDefOf.TM_MagicMissile_II,
                            TorannMagicDefOf.TM_MagicMissile_III

                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Blink,
                            TorannMagicDefOf.TM_Blink_I,
                            TorannMagicDefOf.TM_Blink_II,
                            TorannMagicDefOf.TM_Blink_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Summon,
                            TorannMagicDefOf.TM_Summon_I,
                            TorannMagicDefOf.TM_Summon_II,
                            TorannMagicDefOf.TM_Summon_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Teleport
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_FoldReality
                        },true),
                    };
                }
                return this.magicPowerA;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Shadow
        {
            get
            {
                bool flag = this.magicPowerSkill_Shadow == null;
                if (flag)
                {
                    this.magicPowerSkill_Shadow = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Shadow_eff", "TM_Shadow_eff_desc" )
                    };
                }
                return this.magicPowerSkill_Shadow;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_MagicMissile
        {
            get
            {
                bool flag = this.magicPowerSkill_MagicMissile == null;
                if (flag)
                {
                    this.magicPowerSkill_MagicMissile = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_MagicMissile_eff", "TM_MagicMissile_eff_desc" )
                    };
                }
                return this.magicPowerSkill_MagicMissile;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Blink
        {
            get
            {
                bool flag = this.magicPowerSkill_Blink == null;
                if (flag)
                {
                    this.magicPowerSkill_Blink = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Blink_eff", "TM_Blink_eff_desc" )
                    };
                }
                return this.magicPowerSkill_Blink;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Summon
        {
            get
            {
                bool flag = this.magicPowerSkill_Summon == null;
                if (flag)
                {
                    this.magicPowerSkill_Summon = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Summon_eff", "TM_Summon_eff_desc" )
                    };
                }
                return this.magicPowerSkill_Summon;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Teleport
        {
            get
            {
                bool flag = this.magicPowerSkill_Teleport == null;
                if (flag)
                {
                    this.magicPowerSkill_Teleport = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Teleport_pwr", "TM_Teleport_pwr_desc" ),
                        new MagicPowerSkill("TM_Teleport_eff", "TM_Teleport_eff_desc" ),
                        new MagicPowerSkill("TM_Teleport_ver", "TM_Teleport_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Teleport;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_FoldReality
        {
            get
            {
                bool flag = this.magicPowerSkill_FoldReality == null;
                if (flag)
                {
                    this.magicPowerSkill_FoldReality = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_FoldReality_eff", "TM_FoldReality_eff_desc" )
                    };
                }
                return this.magicPowerSkill_FoldReality;
            }
        }

        bool hasPaladinBuff = false;
        public List<MagicPower> MagicPowersP
        {
            get
            {                
                bool flag = this.magicPowerP == null;
                if (flag)
                {
                    this.magicPowerP = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_P_RayofHope,
                            TorannMagicDefOf.TM_P_RayofHope_I,
                            TorannMagicDefOf.TM_P_RayofHope_II,
                            TorannMagicDefOf.TM_P_RayofHope_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Heal
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Shield,
                            TorannMagicDefOf.TM_Shield_I,
                            TorannMagicDefOf.TM_Shield_II,
                            TorannMagicDefOf.TM_Shield_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ValiantCharge
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Overwhelm
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_HolyWrath
                        },true),

                    };
                }
                if (!hasPaladinBuff)
                {
                    if(this.magicPowerP.Count >= 6)
                    {
                        hasPaladinBuff = true;
                    }                       
                    if(!hasPaladinBuff)
                    {                        
                        MagicPower pBuff = new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_P_RayofHope,
                            TorannMagicDefOf.TM_P_RayofHope_I,
                            TorannMagicDefOf.TM_P_RayofHope_II,
                            TorannMagicDefOf.TM_P_RayofHope_III
                        });
                        List<MagicPower> oldList = new List<MagicPower>();
                        oldList.Add(pBuff);
                        oldList.AddRange(magicPowerP);
                        //magicPowerP.Add(pBuff);
                        this.magicPowerP = oldList;
                        hasPaladinBuff = true;
                    }
                }
                return this.magicPowerP;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_P_RayofHope
        {
            get
            {
                bool flag = this.magicPowerSkill_P_RayofHope == null;
                if (flag)
                {
                    this.magicPowerSkill_P_RayofHope = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_P_RayofHope_eff", "TM_P_RayofHope_eff_desc")
                    };
                }
                return this.magicPowerSkill_P_RayofHope;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Heal
        {
            get
            {
                bool flag = this.magicPowerSkill_Heal == null;
                if (flag)
                {
                    this.magicPowerSkill_Heal = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Heal_pwr", "TM_Heal_pwr_desc" ),
                        new MagicPowerSkill("TM_Heal_eff", "TM_Heal_eff_desc" ),
                        new MagicPowerSkill("TM_Heal_ver", "TM_Heal_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Heal;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Shield
        {
            get
            {
                bool flag = this.magicPowerSkill_Shield == null;
                if (flag)
                {
                    this.magicPowerSkill_Shield = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Shield_eff", "TM_Shield_eff_desc" )
                    };
                }
                return this.magicPowerSkill_Shield;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ValiantCharge
        {
            get
            {
                bool flag = this.magicPowerSkill_ValiantCharge == null;
                if (flag)
                {
                    this.magicPowerSkill_ValiantCharge = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ValiantCharge_pwr", "TM_ValiantCharge_pwr_desc" ),
                        new MagicPowerSkill("TM_ValiantCharge_eff", "TM_ValiantCharge_eff_desc" ),
                        new MagicPowerSkill("TM_ValiantCharge_ver", "TM_ValiantCharge_ver_desc" )
                    };
                }
                return this.magicPowerSkill_ValiantCharge;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Overwhelm
        {
            get
            {
                bool flag = this.magicPowerSkill_Overwhelm == null;
                if (flag)
                {
                    this.magicPowerSkill_Overwhelm = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Overwhelm_pwr", "TM_Overwhelm_pwr_desc" ),
                        new MagicPowerSkill("TM_Overwhelm_eff", "TM_Overwhelm_eff_desc" ),
                        new MagicPowerSkill("TM_Overwhelm_ver", "TM_Overwhelm_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Overwhelm;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_HolyWrath
        {
            get
            {
                bool flag = this.magicPowerSkill_HolyWrath == null;
                if (flag)
                {
                    this.magicPowerSkill_HolyWrath = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_HolyWrath_pwr", "TM_HolyWrath_pwr_desc" ),
                        new MagicPowerSkill("TM_HolyWrath_eff", "TM_HolyWrath_eff_desc" ),
                        new MagicPowerSkill("TM_HolyWrath_ver", "TM_HolyWrath_ver_desc" )
                    };
                }
                return this.magicPowerSkill_HolyWrath;
            }
        }

        public List<MagicPower> MagicPowersS
        {
            get
            {
                bool flag = this.magicPowerS == null;
                if (flag)
                {
                    this.magicPowerS = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SummonMinion
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SummonPylon
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SummonExplosive
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SummonElemental
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SummonPoppi
                        },true),

                    };
                }
                return this.magicPowerS;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SummonMinion
        {
            get
            {
                bool flag = this.magicPowerSkill_SummonMinion == null;
                if (flag)
                {
                    this.magicPowerSkill_SummonMinion = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SummonMinion_pwr", "TM_SummonMinion_pwr_desc" ),
                        new MagicPowerSkill("TM_SummonMinion_eff", "TM_SummonMinion_eff_desc" ),
                        new MagicPowerSkill("TM_SummonMinion_ver", "TM_SummonMinion_ver_desc" )
                    };
                }
                return this.magicPowerSkill_SummonMinion;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SummonPylon
        {
            get
            {
                bool flag = this.magicPowerSkill_SummonPylon == null;
                if (flag)
                {
                    this.magicPowerSkill_SummonPylon = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SummonPylon_pwr", "TM_SummonPylon_pwr_desc" ),
                        new MagicPowerSkill("TM_SummonPylon_eff", "TM_SummonPylon_eff_desc" ),
                        new MagicPowerSkill("TM_SummonPylon_ver", "TM_SummonPylon_ver_desc" )
                    };
                }
                return this.magicPowerSkill_SummonPylon;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SummonExplosive
        {
            get
            {
                bool flag = this.magicPowerSkill_SummonExplosive == null;
                if (flag)
                {
                    this.magicPowerSkill_SummonExplosive = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SummonExplosive_pwr", "TM_SummonExplosive_pwr_desc" ),
                        new MagicPowerSkill("TM_SummonExplosive_eff", "TM_SummonExplosive_eff_desc" ),
                        new MagicPowerSkill("TM_SummonExplosive_ver", "TM_SummonExplosive_ver_desc" )
                    };
                }
                return this.magicPowerSkill_SummonExplosive;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SummonElemental
        {
            get
            {
                bool flag = this.magicPowerSkill_SummonElemental == null;
                if (flag)
                {
                    this.magicPowerSkill_SummonElemental = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SummonElemental_pwr", "TM_SummonElemental_pwr_desc" ),
                        new MagicPowerSkill("TM_SummonElemental_eff", "TM_SummonElemental_eff_desc" ),
                        new MagicPowerSkill("TM_SummonElemental_ver", "TM_SummonElemental_ver_desc" )
                    };
                }
                return this.magicPowerSkill_SummonElemental;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SummonPoppi
        {
            get
            {
                bool flag = this.magicPowerSkill_SummonPoppi == null;
                if (flag)
                {
                    this.magicPowerSkill_SummonPoppi = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SummonPoppi_pwr", "TM_SummonPoppi_pwr_desc" ),
                        new MagicPowerSkill("TM_SummonPoppi_eff", "TM_SummonPoppi_eff_desc" ),
                        new MagicPowerSkill("TM_SummonPoppi_ver", "TM_SummonPoppi_ver_desc" )
                    };
                }
                return this.magicPowerSkill_SummonPoppi;
            }
        }

        public List<MagicPower> MagicPowersD
        {
            get
            {
                bool flag = this.magicPowerD == null;
                if (flag)
                {
                    this.magicPowerD = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Poison
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SootheAnimal,
                            TorannMagicDefOf.TM_SootheAnimal_I,
                            TorannMagicDefOf.TM_SootheAnimal_II,
                            TorannMagicDefOf.TM_SootheAnimal_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Regenerate
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_CureDisease
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_RegrowLimb
                        },true),
                    };
                }
                return this.magicPowerD;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Poison
        {
            get
            {
                bool flag = this.magicPowerSkill_Poison == null;
                if (flag)
                {
                    this.magicPowerSkill_Poison = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Poison_pwr", "TM_Poison_pwr_desc" ),
                        new MagicPowerSkill("TM_Poison_eff", "TM_Poison_eff_desc" ),
                        new MagicPowerSkill("TM_Poison_ver", "TM_Poison_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Poison;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SootheAnimal
        {
            get
            {
                bool flag = this.magicPowerSkill_SootheAnimal == null;
                if (flag)
                {
                    this.magicPowerSkill_SootheAnimal = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SootheAnimal_pwr", "TM_SootheAnimal_pwr_desc" ),
                        new MagicPowerSkill("TM_SootheAnimal_eff", "TM_SootheAnimal_eff_desc" )
                    };
                }
                return this.magicPowerSkill_SootheAnimal;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Regenerate
        {
            get
            {
                bool flag = this.magicPowerSkill_Regenerate == null;
                if (flag)
                {
                    this.magicPowerSkill_Regenerate = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Regenerate_pwr", "TM_Regenerate_pwr_desc" ),
                        new MagicPowerSkill("TM_Regenerate_eff", "TM_Regenerate_eff_desc" ),
                        new MagicPowerSkill("TM_Regenerate_ver", "TM_Regenerate_ver_desc" )
                    };
                }
                return this.magicPowerSkill_Regenerate;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_CureDisease
        {
            get
            {
                bool flag = this.magicPowerSkill_CureDisease == null;
                if (flag)
                {
                    this.magicPowerSkill_CureDisease = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_CureDisease_pwr", "TM_CureDisease_pwr_desc" ),
                        new MagicPowerSkill("TM_CureDisease_eff", "TM_CureDisease_eff_desc" ),
                        new MagicPowerSkill("TM_CureDisease_ver", "TM_CureDisease_ver_desc" )
                    };
                }
                return this.magicPowerSkill_CureDisease;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_RegrowLimb
        {
            get
            {
                bool flag = this.magicPowerSkill_RegrowLimb == null;
                if (flag)
                {
                    this.magicPowerSkill_RegrowLimb = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_RegrowLimb_eff", "TM_RegrowLimb_eff_desc")
                    };
                }
                return this.magicPowerSkill_RegrowLimb;
            }
        }

        public List<MagicPower> MagicPowersN
        {
            get
            {
                bool flag = this.magicPowerN == null;
                if (flag)
                {
                    this.magicPowerN = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_RaiseUndead
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_DeathMark,
                            TorannMagicDefOf.TM_DeathMark_I,
                            TorannMagicDefOf.TM_DeathMark_II,
                            TorannMagicDefOf.TM_DeathMark_III
                        }),                        
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_FogOfTorment
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ConsumeCorpse,
                            TorannMagicDefOf.TM_ConsumeCorpse_I,
                            TorannMagicDefOf.TM_ConsumeCorpse_II,
                            TorannMagicDefOf.TM_ConsumeCorpse_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_CorpseExplosion,
                            TorannMagicDefOf.TM_CorpseExplosion_I,
                            TorannMagicDefOf.TM_CorpseExplosion_II,
                            TorannMagicDefOf.TM_CorpseExplosion_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LichForm
                        },true),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_DeathBolt,
                            TorannMagicDefOf.TM_DeathBolt_I,
                            TorannMagicDefOf.TM_DeathBolt_II,
                            TorannMagicDefOf.TM_DeathBolt_III
                        }),
                    };
                }
                return this.magicPowerN;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_RaiseUndead
        {
            get
            {
                bool flag = this.magicPowerSkill_RaiseUndead == null;
                if (flag)
                {
                    this.magicPowerSkill_RaiseUndead = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_RaiseUndead_pwr", "TM_RaiseUndead_pwr_desc"),
                        new MagicPowerSkill("TM_RaiseUndead_eff", "TM_RaiseUndead_eff_desc"),
                        new MagicPowerSkill("TM_RaiseUndead_ver", "TM_RaiseUndead_ver_desc")
                    };
                }
                return this.magicPowerSkill_RaiseUndead;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_DeathMark
        {
            get
            {
                bool flag = this.magicPowerSkill_DeathMark == null;
                if (flag)
                {
                    this.magicPowerSkill_DeathMark = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_DeathMark_pwr", "TM_DeathMark_pwr_desc"),
                        new MagicPowerSkill("TM_DeathMark_eff", "TM_DeathMark_eff_desc"),
                        new MagicPowerSkill("TM_DeathMark_ver", "TM_DeathMark_ver_desc")
                    };
                }
                return this.magicPowerSkill_DeathMark;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_FogOfTorment
        {
            get
            {
                bool flag = this.magicPowerSkill_FogOfTorment == null;
                if (flag)
                {
                    this.magicPowerSkill_FogOfTorment = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_FogOfTorment_pwr", "TM_FogOfTorment_pwr_desc"),
                        new MagicPowerSkill("TM_FogOfTorment_eff", "TM_FogOfTorment_eff_desc"),
                        new MagicPowerSkill("TM_FogOfTorment_ver", "TM_FogOfTorment_ver_desc")
                    };
                }
                return this.magicPowerSkill_FogOfTorment;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ConsumeCorpse
        {
            get
            {
                bool flag = this.magicPowerSkill_ConsumeCorpse == null;
                if (flag)
                {
                    this.magicPowerSkill_ConsumeCorpse = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ConsumeCorpse_eff", "TM_ConsumeCorpse_eff_desc"),
                        new MagicPowerSkill("TM_ConsumeCorpse_ver", "TM_ConsumeCorpse_ver_desc")
                    };
                }
                return this.magicPowerSkill_ConsumeCorpse;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_CorpseExplosion
        {
            get
            {
                bool flag = this.magicPowerSkill_CorpseExplosion == null;
                if (flag)
                {
                    this.magicPowerSkill_CorpseExplosion = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_CorpseExplosion_pwr", "TM_CorpseExplosion_pwr_desc"),
                        new MagicPowerSkill("TM_CorpseExplosion_eff", "TM_CorpseExplosion_eff_desc"),
                        new MagicPowerSkill("TM_CorpseExplosion_ver", "TM_CorpseExplosion_ver_desc")
                    };
                }
                return this.magicPowerSkill_CorpseExplosion;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LichForm
        {
            get
            {
                bool flag = this.magicPowerSkill_LichForm == null;
                if (flag)
                {
                    this.magicPowerSkill_LichForm = new List<MagicPowerSkill>
                    {

                    };
                }
                return this.magicPowerSkill_LichForm;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_DeathBolt
        {
            get
            {
                bool flag = this.magicPowerSkill_DeathBolt == null;
                if (flag)
                {
                    this.magicPowerSkill_DeathBolt = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_DeathBolt_pwr", "TM_DeathBolt_pwr_desc"),
                        new MagicPowerSkill("TM_DeathBolt_eff", "TM_DeathBolt_eff_desc"),
                        new MagicPowerSkill("TM_DeathBolt_ver", "TM_DeathBolt_ver_desc")
                    };
                }
                return this.magicPowerSkill_DeathBolt;
            }
        }

        public List<MagicPower> MagicPowersPR
        {
            get
            {
                bool flag = this.magicPowerPR == null;
                if (flag)
                {
                    this.magicPowerPR = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_AdvancedHeal
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Purify
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_HealingCircle,
                            TorannMagicDefOf.TM_HealingCircle_I,
                            TorannMagicDefOf.TM_HealingCircle_II,
                            TorannMagicDefOf.TM_HealingCircle_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BestowMight,
                            TorannMagicDefOf.TM_BestowMight_I,
                            TorannMagicDefOf.TM_BestowMight_II,
                            TorannMagicDefOf.TM_BestowMight_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Resurrection
                        },true),
                    };
                }
                return this.magicPowerPR;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_AdvancedHeal
        {
            get
            {
                bool flag = this.magicPowerSkill_AdvancedHeal == null;
                if (flag)
                {
                    this.magicPowerSkill_AdvancedHeal = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_AdvancedHeal_pwr", "TM_AdvancedHeal_pwr_desc"),
                        new MagicPowerSkill("TM_AdvancedHeal_eff", "TM_AdvancedHeal_eff_desc"),
                        new MagicPowerSkill("TM_AdvancedHeal_ver", "TM_AdvancedHeal_ver_desc")
                    };
                }
                return this.magicPowerSkill_AdvancedHeal;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Purify
        {
            get
            {
                bool flag = this.magicPowerSkill_Purify == null;
                if (flag)
                {
                    this.magicPowerSkill_Purify = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Purify_pwr", "TM_Purify_pwr_desc"),
                        new MagicPowerSkill("TM_Purify_eff", "TM_Purify_eff_desc"),
                        new MagicPowerSkill("TM_Purify_ver", "TM_Purify_ver_desc")
                    };
                }
                return this.magicPowerSkill_Purify;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_HealingCircle
        {
            get
            {
                bool flag = this.magicPowerSkill_HealingCircle == null;
                if (flag)
                {
                    this.magicPowerSkill_HealingCircle = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_HealingCircle_pwr", "TM_HealingCircle_pwr_desc"),
                        new MagicPowerSkill("TM_HealingCircle_eff", "TM_HealingCircle_eff_desc"),
                        new MagicPowerSkill("TM_HealingCircle_ver", "TM_HealingCircle_ver_desc")
                    };
                }
                return this.magicPowerSkill_HealingCircle;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BestowMight
        {
            get
            {
                bool flag = this.magicPowerSkill_BestowMight == null;
                if (flag)
                {
                    this.magicPowerSkill_BestowMight = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BestowMight_eff", "TM_BestowMight_eff_desc")
                    };
                }
                return this.magicPowerSkill_BestowMight;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Resurrection
        {
            get
            {
                bool flag = this.magicPowerSkill_Resurrection == null;
                if (flag)
                {
                    this.magicPowerSkill_Resurrection = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Resurrection_eff", "TM_Resurrection_eff_desc"),
                        new MagicPowerSkill("TM_Resurrection_ver", "TM_Resurrection_ver_desc")
                    };
                }
                return this.magicPowerSkill_Resurrection;
            }
        }

        public List<MagicPower> MagicPowersB
        {
            get
            {
                bool flag = this.magicPowerB == null;
                if (flag)
                {
                    this.magicPowerB = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BardTraining
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Entertain
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Inspire
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Lullaby,
                            TorannMagicDefOf.TM_Lullaby_I,
                            TorannMagicDefOf.TM_Lullaby_II,
                            TorannMagicDefOf.TM_Lullaby_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BattleHymn
                        },true),
                    };
                }
                return this.magicPowerB;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BardTraining
        {
            get
            {
                bool flag = this.magicPowerSkill_BardTraining == null;
                if (flag)
                {
                    this.magicPowerSkill_BardTraining = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BardTraining_pwr", "TM_BardTraining_pwr_desc")
                    };
                }
                return this.magicPowerSkill_BardTraining;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Entertain
        {
            get
            {
                bool flag = this.magicPowerSkill_Entertain == null;
                if (flag)
                {
                    this.magicPowerSkill_Entertain = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Entertain_pwr", "TM_Entertain_pwr_desc"),
                        new MagicPowerSkill("TM_Entertain_ver", "TM_Entertain_ver_desc")
                    };
                }
                return this.magicPowerSkill_Entertain;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Inspire
        {
            get
            {
                bool flag = this.magicPowerSkill_Inspire == null;
                if (flag)
                {
                    this.magicPowerSkill_Inspire = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Inspire_pwr", "TM_Inspire_pwr_desc"),
                        new MagicPowerSkill("TM_Inspire_ver", "TM_Inspire_ver_desc")
                    };
                }
                return this.magicPowerSkill_Inspire;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Lullaby
        {
            get
            {
                bool flag = this.magicPowerSkill_Lullaby == null;
                if (flag)
                {
                    this.magicPowerSkill_Lullaby = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Lullaby_pwr", "TM_Lullaby_pwr_desc"),
                        new MagicPowerSkill("TM_Lullaby_eff", "TM_Lullaby_eff_desc"),
                        new MagicPowerSkill("TM_Lullaby_ver", "TM_Lullaby_ver_desc")
                    };
                }
                return this.magicPowerSkill_Lullaby;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BattleHymn
        {
            get
            {
                bool flag = this.magicPowerSkill_BattleHymn == null;
                if (flag)
                {
                    this.magicPowerSkill_BattleHymn = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BattleHymn_pwr", "TM_BattleHymn_pwr_desc"),
                        new MagicPowerSkill("TM_BattleHymn_eff", "TM_BattleHymn_eff_desc"),
                        new MagicPowerSkill("TM_BattleHymn_ver", "TM_BattleHymn_ver_desc")
                    };
                }
                return this.magicPowerSkill_BattleHymn;
            }
        }

        public List<MagicPower> MagicPowersWD
        {
            get
            {
                bool flag = this.magicPowerWD == null;
                if (flag)
                {
                    this.magicPowerWD = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SoulBond
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ShadowBolt,
                            TorannMagicDefOf.TM_ShadowBolt_I,
                            TorannMagicDefOf.TM_ShadowBolt_II,
                            TorannMagicDefOf.TM_ShadowBolt_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Dominate
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Repulsion,
                            TorannMagicDefOf.TM_Repulsion_I,
                            TorannMagicDefOf.TM_Repulsion_II,
                            TorannMagicDefOf.TM_Repulsion_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_PsychicShock
                        },true),
                        //new MagicPower(new List<AbilityUser.AbilityDef>
                        //{
                        //    TorannMagicDefOf.TM_SummonDemon
                        //}),
                    };
                }
                return this.magicPowerWD;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SoulBond
        {
            get
            {
                bool flag = this.magicPowerSkill_SoulBond == null;
                if (flag)
                {
                    this.magicPowerSkill_SoulBond = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SoulBond_pwr", "TM_SoulBond_pwr_desc"),
                        new MagicPowerSkill("TM_SoulBond_eff", "TM_SoulBond_eff_desc"),
                        new MagicPowerSkill("TM_SoulBond_ver", "TM_SoulBond_ver_desc")
                    };
                }
                return this.magicPowerSkill_SoulBond;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ShadowBolt
        {
            get
            {
                bool flag = this.magicPowerSkill_ShadowBolt == null;
                if (flag)
                {
                    this.magicPowerSkill_ShadowBolt = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ShadowBolt_pwr", "TM_ShadowBolt_pwr_desc"),
                        new MagicPowerSkill("TM_ShadowBolt_eff", "TM_ShadowBolt_eff_desc"),
                        new MagicPowerSkill("TM_ShadowBolt_ver", "TM_ShadowBolt_ver_desc")
                    };
                }
                return this.magicPowerSkill_ShadowBolt;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Dominate
        {
            get
            {
                bool flag = this.magicPowerSkill_Dominate == null;
                if (flag)
                {
                    this.magicPowerSkill_Dominate = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Dominate_pwr", "TM_Dominate_pwr_desc"),
                        new MagicPowerSkill("TM_Dominate_eff", "TM_Dominate_eff_desc"),
                        new MagicPowerSkill("TM_Dominate_ver", "TM_Dominate_ver_desc")
                    };
                }
                return this.magicPowerSkill_Dominate;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Repulsion
        {
            get
            {
                bool flag = this.magicPowerSkill_Repulsion == null;
                if (flag)
                {
                    this.magicPowerSkill_Repulsion = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Repulsion_pwr", "TM_Repulsion_pwr_desc"),
                        new MagicPowerSkill("TM_Repulsion_eff", "TM_Repulsion_eff_desc"),
                        new MagicPowerSkill("TM_Repulsion_ver", "TM_Repulsion_ver_desc")
                    };
                }
                return this.magicPowerSkill_Repulsion;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_PsychicShock
        {
            get
            {
                bool flag = this.magicPowerSkill_PsychicShock == null;
                if (flag)
                {
                    this.magicPowerSkill_PsychicShock = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_PsychicShock_pwr", "TM_PsychicShock_pwr_desc"),
                        new MagicPowerSkill("TM_PsychicShock_eff", "TM_PsychicShock_eff_desc"),
                        new MagicPowerSkill("TM_PsychicShock_ver", "TM_PsychicShock_ver_desc")
                    };
                }
                return this.magicPowerSkill_PsychicShock;
            }
        }
        //public List<MagicPowerSkill> MagicPowerSkill_SummonDemon
        //{
        //    get
        //    {
        //        bool flag = this.magicPowerSkill_SummonDemon == null;
        //        if (flag)
        //        {
        //            this.magicPowerSkill_SummonDemon = new List<MagicPowerSkill>
        //            {
        //                new MagicPowerSkill("TM_SummonDemon_pwr", "TM_SummonDemon_pwr_desc"),
        //                new MagicPowerSkill("TM_SummonDemon_eff", "TM_SummonDemon_eff_desc"),
        //                new MagicPowerSkill("TM_SummonDemon_ver", "TM_SummonDemon_ver_desc")
        //            };
        //        }
        //        return this.magicPowerSkill_SummonDemon;
        //    }
        //}

        public List<MagicPower> MagicPowersSD
        {
            get
            {
                bool flag = this.magicPowerSD == null;
                if (flag)
                {
                    this.magicPowerSD = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SoulBond
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ShadowBolt,
                            TorannMagicDefOf.TM_ShadowBolt_I,
                            TorannMagicDefOf.TM_ShadowBolt_II,
                            TorannMagicDefOf.TM_ShadowBolt_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Dominate
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Attraction,
                            TorannMagicDefOf.TM_Attraction_I,
                            TorannMagicDefOf.TM_Attraction_II,
                            TorannMagicDefOf.TM_Attraction_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Scorn
                        },true),
                        //new MagicPower(new List<AbilityUser.AbilityDef>
                        //{
                        //    TorannMagicDefOf.TM_SummonDemon
                        //}),
                    };
                }
                return this.magicPowerSD;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Attraction
        {
            get
            {
                bool flag = this.magicPowerSkill_Attraction == null;
                if (flag)
                {
                    this.magicPowerSkill_Attraction = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Attraction_pwr", "TM_Attraction_pwr_desc"),
                        new MagicPowerSkill("TM_Attraction_eff", "TM_Attraction_eff_desc"),
                        new MagicPowerSkill("TM_Attraction_ver", "TM_Attraction_ver_desc")
                    };
                }
                return this.magicPowerSkill_Attraction;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Scorn
        {
            get
            {
                bool flag = this.magicPowerSkill_Scorn == null;
                if (flag)
                {
                    this.magicPowerSkill_Scorn = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Scorn_pwr", "TM_Scorn_pwr_desc"),
                        new MagicPowerSkill("TM_Scorn_eff", "TM_Scorn_eff_desc"),
                        new MagicPowerSkill("TM_Scorn_ver", "TM_Scorn_ver_desc")
                    };
                }
                return this.magicPowerSkill_Scorn;
            }
        }

        public List<MagicPower> MagicPowersG
        {
            get
            {
                bool flag = this.magicPowerG == null;
                if (flag)
                {
                    this.magicPowerG = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Stoneskin
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Encase,
                            TorannMagicDefOf.TM_Encase_I,
                            TorannMagicDefOf.TM_Encase_II,
                            TorannMagicDefOf.TM_Encase_III
                        }),                        
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EarthSprites
                        }),                        
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EarthernHammer
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Sentinel
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Meteor,
                            TorannMagicDefOf.TM_Meteor_I,
                            TorannMagicDefOf.TM_Meteor_II,
                            TorannMagicDefOf.TM_Meteor_III
                        },true),
                    };
                }
                return this.magicPowerG;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Stoneskin
        {
            get
            {
                bool flag = this.magicPowerSkill_Stoneskin == null;
                if (flag)
                {
                    this.magicPowerSkill_Stoneskin = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Stoneskin_pwr", "TM_Stoneskin_pwr_desc"),
                        new MagicPowerSkill("TM_Stoneskin_eff", "TM_Stoneskin_eff_desc"),
                        new MagicPowerSkill("TM_Stoneskin_ver", "TM_Stoneskin_ver_desc")
                        
                    };
                }
                return this.magicPowerSkill_Stoneskin;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Encase
        {
            get
            {
                bool flag = this.magicPowerSkill_Encase == null;
                if (flag)
                {
                    this.magicPowerSkill_Encase = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Encase_pwr", "TM_Encase_pwr_desc"),                        
                        new MagicPowerSkill("TM_Encase_eff", "TM_Encase_eff_desc"),
                        new MagicPowerSkill("TM_Encase_ver", "TM_Encase_ver_desc")
                    };
                }
                return this.magicPowerSkill_Encase;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_EarthSprites
        {
            get
            {
                bool flag = this.magicPowerSkill_EarthSprites == null;
                if (flag)
                {
                    this.magicPowerSkill_EarthSprites = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_EarthSprites_pwr", "TM_EarthSprites_pwr_desc"),
                        new MagicPowerSkill("TM_EarthSprites_eff", "TM_EarthSprites_eff_desc"),
                        new MagicPowerSkill("TM_EarthSprites_ver", "TM_EarthSprites_ver_desc")
                    };
                }
                return this.magicPowerSkill_EarthSprites;
            }
        }        
        public List<MagicPowerSkill> MagicPowerSkill_EarthernHammer
        {
            get
            {
                bool flag = this.magicPowerSkill_EarthernHammer == null;
                if (flag)
                {
                    this.magicPowerSkill_EarthernHammer = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_EarthernHammer_pwr", "TM_EarthernHammer_pwr_desc" ),
                        new MagicPowerSkill("TM_EarthernHammer_eff", "TM_EarthernHammer_eff_desc" ),
                        new MagicPowerSkill("TM_EarthernHammer_ver", "TM_EarthernHammer_ver_desc" )
                    };
                }
                return this.magicPowerSkill_EarthernHammer;
            }
        }        
        public List<MagicPowerSkill> MagicPowerSkill_Sentinel
        {
            get
            {
                bool flag = this.magicPowerSkill_Sentinel == null;
                if (flag)
                {
                    this.magicPowerSkill_Sentinel = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Sentinel_pwr", "TM_Sentinel_pwr_desc"),
                        new MagicPowerSkill("TM_Sentinel_eff", "TM_Sentinel_eff_desc"),
                        new MagicPowerSkill("TM_Sentinel_ver", "TM_Sentinel_ver_desc")
                    };
                }
                return this.magicPowerSkill_Sentinel;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Meteor
        {
            get
            {
                bool flag = this.magicPowerSkill_Meteor == null;
                if (flag)
                {
                    this.magicPowerSkill_Meteor = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Meteor_ver", "TM_Meteor_ver_desc"),
                        new MagicPowerSkill("TM_Meteor_eff", "TM_Meteor_eff_desc")
                    };
                }
                return this.magicPowerSkill_Meteor;
            }
        }

        public List<MagicPower> MagicPowersT
        {
            get
            {
                bool flag = this.magicPowerT == null;
                if (flag)
                {
                    this.magicPowerT = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TechnoBit
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TechnoTurret
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TechnoWeapon
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_TechnoShield  
                        },true),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Overdrive
                        },true),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {                            
                            TorannMagicDefOf.TM_Sabotage
                        },true),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_OrbitalStrike,
                            TorannMagicDefOf.TM_OrbitalStrike_I,
                            TorannMagicDefOf.TM_OrbitalStrike_II,
                            TorannMagicDefOf.TM_OrbitalStrike_III
                        },true),
                    };
                }
                return this.magicPowerT;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_TechnoBit
        {
            get
            {
                bool flag = this.magicPowerSkill_TechnoBit == null;
                if (flag)
                {
                    this.magicPowerSkill_TechnoBit = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_TechnoBit_pwr", "TM_TechnoBit_pwr_desc"),
                        new MagicPowerSkill("TM_TechnoBit_eff", "TM_TechnoBit_eff_desc"),
                        new MagicPowerSkill("TM_TechnoBit_ver", "TM_TechnoBit_ver_desc")
                    };
                }
                return this.magicPowerSkill_TechnoBit;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_TechnoTurret
        {
            get
            {
                bool flag = this.magicPowerSkill_TechnoTurret == null;
                if (flag)
                {
                    this.magicPowerSkill_TechnoTurret = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_TechnoTurret_pwr", "TM_TechnoTurret_pwr_desc"),
                        new MagicPowerSkill("TM_TechnoTurret_eff", "TM_TechnoTurret_eff_desc"),
                        new MagicPowerSkill("TM_TechnoTurret_ver", "TM_TechnoTurret_ver_desc")
                    };
                }
                return this.magicPowerSkill_TechnoTurret;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_TechnoWeapon
        {
            get
            {
                bool flag = this.magicPowerSkill_TechnoWeapon == null;
                if (flag)
                {
                    this.magicPowerSkill_TechnoWeapon = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_TechnoWeapon_pwr", "TM_TechnoWeapon_pwr_desc"),
                        new MagicPowerSkill("TM_TechnoWeapon_eff", "TM_TechnoWeapon_eff_desc"),
                        new MagicPowerSkill("TM_TechnoWeapon_ver", "TM_TechnoWeapon_ver_desc")
                    };
                }
                return this.magicPowerSkill_TechnoWeapon;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_TechnoShield
        {
            get
            {
                bool flag = this.magicPowerSkill_TechnoShield == null;
                if (flag)
                {
                    this.magicPowerSkill_TechnoShield = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_TechnoShield_pwr", "TM_TechnoShield_pwr_desc"),
                        new MagicPowerSkill("TM_TechnoShield_eff", "TM_TechnoShield_eff_desc"),
                        new MagicPowerSkill("TM_TechnoShield_ver", "TM_TechnoShield_ver_desc")
                    };
                }
                return this.magicPowerSkill_TechnoShield;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Sabotage
        {
            get
            {
                bool flag = this.magicPowerSkill_Sabotage == null;
                if (flag)
                {
                    this.magicPowerSkill_Sabotage = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Sabotage_pwr", "TM_Sabotage_pwr_desc"),
                        new MagicPowerSkill("TM_Sabotage_eff", "TM_Sabotage_eff_desc"),
                        new MagicPowerSkill("TM_Sabotage_ver", "TM_Sabotage_ver_desc")
                    };
                }
                return this.magicPowerSkill_Sabotage;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Overdrive
        {
            get
            {
                bool flag = this.magicPowerSkill_Overdrive == null;
                if (flag)
                {
                    this.magicPowerSkill_Overdrive = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Overdrive_pwr", "TM_Overdrive_pwr_desc"),
                        new MagicPowerSkill("TM_Overdrive_eff", "TM_Overdrive_eff_desc"),
                        new MagicPowerSkill("TM_Overdrive_ver", "TM_Overdrive_ver_desc")
                    };
                }
                return this.magicPowerSkill_Overdrive;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_OrbitalStrike
        {
            get
            {
                bool flag = this.magicPowerSkill_OrbitalStrike == null;
                if (flag)
                {
                    this.magicPowerSkill_OrbitalStrike = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_OrbitalStrike_pwr", "TM_OrbitalStrike_pwr_desc"),
                        new MagicPowerSkill("TM_OrbitalStrike_eff", "TM_OrbitalStrike_eff_desc"),
                        new MagicPowerSkill("TM_OrbitalStrike_ver", "TM_OrbitalStrike_ver_desc")
                    };
                }
                return this.magicPowerSkill_OrbitalStrike;
            }
        }

        public List<MagicPower> MagicPowersBM
        {
            get
            {
                bool flag = this.magicPowerBM == null;
                if (flag)
                {
                    this.magicPowerBM = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BloodGift
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_IgniteBlood
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BloodForBlood
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BloodShield
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Rend,
                            TorannMagicDefOf.TM_Rend_I,
                            TorannMagicDefOf.TM_Rend_II,
                            TorannMagicDefOf.TM_Rend_III
                        }),                        
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_BloodMoon,
                            TorannMagicDefOf.TM_BloodMoon_I,
                            TorannMagicDefOf.TM_BloodMoon_II,
                            TorannMagicDefOf.TM_BloodMoon_III
                        }, true),
                    };
                }
                return this.magicPowerBM;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BloodGift
        {
            get
            {
                bool flag = this.magicPowerSkill_BloodGift == null;
                if (flag)
                {
                    this.magicPowerSkill_BloodGift = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BloodGift_pwr", "TM_BloodGift_pwr_desc"),
                        new MagicPowerSkill("TM_BloodGift_eff", "TM_BloodGift_eff_desc"),
                        new MagicPowerSkill("TM_BloodGift_ver", "TM_BloodGift_ver_desc")

                    };
                }
                return this.magicPowerSkill_BloodGift;
            }
        }        
        public List<MagicPowerSkill> MagicPowerSkill_IgniteBlood
        {
            get
            {
                bool flag = this.magicPowerSkill_IgniteBlood == null;
                if (flag)
                {
                    this.magicPowerSkill_IgniteBlood = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_IgniteBlood_pwr", "TM_IgniteBlood_pwr_desc"),
                        new MagicPowerSkill("TM_IgniteBlood_eff", "TM_IgniteBlood_eff_desc"),
                        new MagicPowerSkill("TM_IgniteBlood_ver", "TM_IgniteBlood_ver_desc")
                    };
                }
                return this.magicPowerSkill_IgniteBlood;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BloodForBlood
        {
            get
            {
                bool flag = this.magicPowerSkill_BloodForBlood == null;
                if (flag)
                {
                    this.magicPowerSkill_BloodForBlood = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BloodForBlood_pwr", "TM_BloodForBlood_pwr_desc" ),
                        new MagicPowerSkill("TM_BloodForBlood_eff", "TM_BloodForBlood_eff_desc" ),
                        new MagicPowerSkill("TM_BloodForBlood_ver", "TM_BloodForBlood_ver_desc" )
                    };
                }
                return this.magicPowerSkill_BloodForBlood;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BloodShield
        {
            get
            {
                bool flag = this.magicPowerSkill_BloodShield == null;
                if (flag)
                {
                    this.magicPowerSkill_BloodShield = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BloodShield_pwr", "TM_BloodShield_pwr_desc"),
                        new MagicPowerSkill("TM_BloodShield_eff", "TM_BloodShield_eff_desc"),
                        new MagicPowerSkill("TM_BloodShield_ver", "TM_BloodShield_ver_desc")
                    };
                }
                return this.magicPowerSkill_BloodShield;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Rend
        {
            get
            {
                bool flag = this.magicPowerSkill_Rend == null;
                if (flag)
                {
                    this.magicPowerSkill_Rend = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Rend_pwr", "TM_Rend_pwr_desc"),
                        new MagicPowerSkill("TM_Rend_eff", "TM_Rend_eff_desc"),
                        new MagicPowerSkill("TM_Rend_ver", "TM_Rend_ver_desc")
                    };
                }
                return this.magicPowerSkill_Rend;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_BloodMoon
        {
            get
            {
                bool flag = this.magicPowerSkill_BloodMoon == null;
                if (flag)
                {
                    this.magicPowerSkill_BloodMoon = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_BloodMoon_pwr", "TM_BloodMoon_pwr_desc"),
                        new MagicPowerSkill("TM_BloodMoon_eff", "TM_BloodMoon_eff_desc"),
                        new MagicPowerSkill("TM_BloodMoon_ver", "TM_BloodMoon_ver_desc")
                    };
                }
                return this.magicPowerSkill_BloodMoon;
            }
        }

        public List<MagicPower> MagicPowersE
        {
            get
            {
                bool flag = this.magicPowerE == null;
                if (flag)
                {
                    this.magicPowerE = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EnchantedBody
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Transmutate
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EnchanterStone
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_EnchantWeapon
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Polymorph,
                            TorannMagicDefOf.TM_Polymorph_I,
                            TorannMagicDefOf.TM_Polymorph_II,
                            TorannMagicDefOf.TM_Polymorph_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Shapeshift
                        }, true),
                    };
                }
                return this.magicPowerE;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_EnchantedBody
        {
            get
            {
                bool flag = this.magicPowerSkill_EnchantedBody == null;
                if (flag)
                {
                    this.magicPowerSkill_EnchantedBody = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_EnchantedBody_pwr", "TM_EnchantedBody_pwr_desc"),
                        new MagicPowerSkill("TM_EnchantedBody_eff", "TM_EnchantedBody_eff_desc"),
                        new MagicPowerSkill("TM_EnchantedBody_ver", "TM_EnchantedBody_ver_desc")

                    };
                }
                return this.magicPowerSkill_EnchantedBody;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Transmutate
        {
            get
            {
                bool flag = this.magicPowerSkill_Transmutate == null;
                if (flag)
                {
                    this.magicPowerSkill_Transmutate = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Transmutate_pwr", "TM_Transmutate_pwr_desc"),
                        new MagicPowerSkill("TM_Transmutate_eff", "TM_Transmutate_eff_desc"),
                        new MagicPowerSkill("TM_Transmutate_ver", "TM_Transmutate_ver_desc")
                    };
                }
                return this.magicPowerSkill_Transmutate;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_EnchanterStone
        {
            get
            {
                bool flag = this.magicPowerSkill_EnchanterStone == null;
                if (flag)
                {
                    this.magicPowerSkill_EnchanterStone = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_EnchanterStone_eff", "TM_EnchanterStone_eff_desc" ),
                        new MagicPowerSkill("TM_EnchanterStone_ver", "TM_EnchanterStone_ver_desc" )
                    };
                }
                return this.magicPowerSkill_EnchanterStone;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_EnchantWeapon
        {
            get
            {
                bool flag = this.magicPowerSkill_EnchantWeapon == null;
                if (flag)
                {
                    this.magicPowerSkill_EnchantWeapon = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_EnchantWeapon_pwr", "TM_EnchantWeapon_pwr_desc"),
                        new MagicPowerSkill("TM_EnchantWeapon_eff", "TM_EnchantWeapon_eff_desc")
                    };
                }
                return this.magicPowerSkill_EnchantWeapon;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Polymorph
        {
            get
            {
                bool flag = this.magicPowerSkill_Polymorph == null;
                if (flag)
                {
                    this.magicPowerSkill_Polymorph = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Polymorph_pwr", "TM_Polymorph_pwr_desc"),
                        new MagicPowerSkill("TM_Polymorph_eff", "TM_Polymorph_eff_desc"),
                        new MagicPowerSkill("TM_Polymorph_ver", "TM_Polymorph_ver_desc")
                    };
                }
                return this.magicPowerSkill_Polymorph;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Shapeshift
        {
            get
            {
                bool flag = this.magicPowerSkill_Shapeshift == null;
                if (flag)
                {
                    this.magicPowerSkill_Shapeshift = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Shapeshift_pwr", "TM_Shapeshift_pwr_desc"),
                        new MagicPowerSkill("TM_Shapeshift_eff", "TM_Shapeshift_eff_desc"),
                        new MagicPowerSkill("TM_Shapeshift_ver", "TM_Shapeshift_ver_desc")
                    };
                }
                return this.magicPowerSkill_Shapeshift;
            }
        }

        public List<MagicPower> MagicPowersC
        {
            get
            {
                bool flag = this.magicPowerC == null;
                if (flag)
                {
                    this.magicPowerC = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Prediction
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_AlterFate
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_AccelerateTime
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ReverseTime
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ChronostaticField,
                            TorannMagicDefOf.TM_ChronostaticField_I,
                            TorannMagicDefOf.TM_ChronostaticField_II,
                            TorannMagicDefOf.TM_ChronostaticField_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Recall
                        }, true),
                    };
                }
                return this.magicPowerC;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Prediction
        {
            get
            {
                bool flag = this.magicPowerSkill_Prediction == null;
                if (flag)
                {
                    this.magicPowerSkill_Prediction = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Prediction_pwr", "TM_Prediction_pwr_desc"),
                        new MagicPowerSkill("TM_Prediction_eff", "TM_Prediction_eff_desc"),
                        new MagicPowerSkill("TM_Prediction_ver", "TM_Prediction_ver_desc")

                    };
                }
                return this.magicPowerSkill_Prediction;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_AlterFate
        {
            get
            {
                bool flag = this.magicPowerSkill_AlterFate == null;
                if (flag)
                {
                    this.magicPowerSkill_AlterFate = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_AlterFate_pwr", "TM_AlterFate_pwr_desc"),
                        new MagicPowerSkill("TM_AlterFate_eff", "TM_AlterFate_eff_desc")
                    };
                }
                return this.magicPowerSkill_AlterFate;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_AccelerateTime
        {
            get
            {
                bool flag = this.magicPowerSkill_AccelerateTime == null;
                if (flag)
                {
                    this.magicPowerSkill_AccelerateTime = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_AccelerateTime_pwr", "TM_AccelerateTime_pwr_desc"),
                        new MagicPowerSkill("TM_AccelerateTime_eff", "TM_AccelerateTime_eff_desc"),
                        new MagicPowerSkill("TM_AccelerateTime_ver", "TM_AccelerateTime_ver_desc")
                    };
                }
                return this.magicPowerSkill_AccelerateTime;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ReverseTime
        {
            get
            {
                bool flag = this.magicPowerSkill_ReverseTime == null;
                if (flag)
                {
                    this.magicPowerSkill_ReverseTime = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ReverseTime_pwr", "TM_ReverseTime_pwr_desc" ),
                        new MagicPowerSkill("TM_ReverseTime_eff", "TM_ReverseTime_eff_desc" ),
                        new MagicPowerSkill("TM_ReverseTime_ver", "TM_ReverseTime_ver_desc" )
                    };
                }
                return this.magicPowerSkill_ReverseTime;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ChronostaticField
        {
            get
            {
                bool flag = this.magicPowerSkill_ChronostaticField == null;
                if (flag)
                {
                    this.magicPowerSkill_ChronostaticField = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ChronostaticField_pwr", "TM_ChronostaticField_pwr_desc"),
                        new MagicPowerSkill("TM_ChronostaticField_eff", "TM_ChronostaticField_eff_desc"),
                        new MagicPowerSkill("TM_ChronostaticField_ver", "TM_ChronostaticField_ver_desc")
                    };
                }
                return this.magicPowerSkill_ChronostaticField;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Recall
        {
            get
            {
                bool flag = this.magicPowerSkill_Recall == null;
                if (flag)
                {
                    this.magicPowerSkill_Recall = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Recall_pwr", "TM_Recall_pwr_desc"),
                        new MagicPowerSkill("TM_Recall_eff", "TM_Recall_eff_desc"),
                        new MagicPowerSkill("TM_Recall_ver", "TM_Recall_ver_desc")                        
                    };
                }
                return this.magicPowerSkill_Recall;
            }
        }

        public List<MagicPower> MagicPowersCM
        {
            get
            {
                bool flag = this.magicPowerCM == null;
                if (flag)
                {
                    this.magicPowerCM = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ChaosTradition
                        }),
                    };
                }
                return this.magicPowerCM;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ChaosTradition
        {
            get
            {
                bool flag = this.magicPowerSkill_ChaosTradition == null;
                if (flag)
                {
                    this.magicPowerSkill_ChaosTradition = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ChaosTradition_pwr", "TM_ChaosTradition_pwr_desc"),
                        new MagicPowerSkill("TM_ChaosTradition_eff", "TM_ChaosTradition_eff_desc"),
                        new MagicPowerSkill("TM_ChaosTradition_ver", "TM_ChaosTradition_ver_desc")
                    };
                }
                return this.magicPowerSkill_ChaosTradition;
            }
        }

        public List<MagicPower> MagicPowersShadow
        {
            get
            {
                bool flag = this.magicPowerShadow == null;
                if (flag)
                {
                    this.magicPowerShadow = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ShadowWalk
                        }),
                    };
                }
                return this.magicPowerShadow;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ShadowWalk
        {
            get
            {
                bool flag = this.magicPowerSkill_ShadowWalk == null;
                if (flag)
                {
                    this.magicPowerSkill_ShadowWalk = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ShadowWalk_pwr", "TM_ShadowWalk_pwr_desc"), // applies invisibility and duration of invisibility
                        new MagicPowerSkill("TM_ShadowWalk_eff", "TM_ShadowWalk_eff_desc"), // reduces mana cost
                        new MagicPowerSkill("TM_ShadowWalk_ver", "TM_ShadowWalk_ver_desc")  // heals and can invis target
                    };
                }
                return this.magicPowerSkill_ShadowWalk;
            }
        }

        public List<MagicPower> MagicPowersBrightmage
        {
            get
            {
                bool flag = this.magicPowerBrightmage == null;
                if (flag)
                {
                    this.magicPowerBrightmage = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightLance
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Sunfire,
                            TorannMagicDefOf.TM_Sunfire_I,
                            TorannMagicDefOf.TM_Sunfire_II,
                            TorannMagicDefOf.TM_Sunfire_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightBurst
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LightSkip
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Refraction
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SpiritOfLight
                        }, true),
                    };
                }
                return this.magicPowerBrightmage;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LightLance
        {
            get
            {
                bool flag = this.magicPowerSkill_LightLance == null;
                if (flag)
                {
                    this.magicPowerSkill_LightLance = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LightLance_pwr", "TM_LightLance_pwr_desc"),     //damage
                        new MagicPowerSkill("TM_LightLance_eff", "TM_LightLance_eff_desc"),     //mana cost
                        new MagicPowerSkill("TM_LightLance_ver", "TM_LightLance_ver_desc")      //beam width and duration
                    };
                }
                return this.magicPowerSkill_LightLance;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Sunfire
        {
            get
            {
                bool flag = this.magicPowerSkill_Sunfire == null;
                if (flag)
                {
                    this.magicPowerSkill_Sunfire = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Sunfire_pwr", "TM_Sunfire_pwr_desc"),           //damage
                        new MagicPowerSkill("TM_Sunfire_eff", "TM_Sunfire_eff_desc"),           //mana cost
                        new MagicPowerSkill("TM_Sunfire_ver", "TM_Sunfire_ver_desc")            //lance count
                    };
                }
                return this.magicPowerSkill_Sunfire;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LightBurst
        {
            get
            {
                bool flag = this.magicPowerSkill_LightBurst == null;
                if (flag)
                {
                    this.magicPowerSkill_LightBurst = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LightBurst_pwr", "TM_LightBurst_pwr_desc"),     //hediff severity
                        new MagicPowerSkill("TM_LightBurst_eff", "TM_LightBurst_eff_desc"),     //mana cost
                        new MagicPowerSkill("TM_LightBurst_ver", "TM_LightBurst_ver_desc")      //effects mechanoids, redirects bullets to another target or "spray"
                    };
                }
                return this.magicPowerSkill_LightBurst;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LightSkip
        {
            get
            {
                bool flag = this.magicPowerSkill_LightSkip == null;
                if (flag)
                {
                    this.magicPowerSkill_LightSkip = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LightSkip_pwr", "TM_LightSkip_pwr_desc"),       //3 tiers, self->aoe->global (2pts per)
                        new MagicPowerSkill("TM_LightSkip_eff", "TM_LightSkip_eff_desc")        //mana cost, light requirement
                    };
                }
                return this.magicPowerSkill_LightSkip;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Refraction
        {
            get
            {
                bool flag = this.magicPowerSkill_Refraction == null;
                if (flag)
                {
                    this.magicPowerSkill_Refraction = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Refraction_pwr", "TM_Refraction_pwr_desc"),     //+friendly accuracy; -hostile accuracy
                        new MagicPowerSkill("TM_Refraction_eff", "TM_Refraction_eff_desc"),     //mana cost
                        new MagicPowerSkill("TM_Refraction_ver", "TM_Refraction_ver_desc")      //duration
                    };
                }
                return this.magicPowerSkill_Refraction;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SpiritOfLight
        {
            get
            {
                bool flag = this.magicPowerSkill_SpiritOfLight == null;
                if (flag)
                {
                    this.magicPowerSkill_SpiritOfLight = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SpiritOfLight_pwr", "TM_SpiritOfLight_pwr_desc"), //increases damage of abilities, reduces delay between abilities
                        new MagicPowerSkill("TM_SpiritOfLight_eff", "TM_SpiritOfLight_eff_desc"), //decreased upkeep cost
                        new MagicPowerSkill("TM_SpiritOfLight_ver", "TM_SpiritOfLight_ver_desc")  //increased light energy gain and increased max capacity
                    };
                }
                return this.magicPowerSkill_SpiritOfLight;
            }
        }

        public List<MagicPower> MagicPowersShaman
        {
            get
            {
                bool flag = this.magicPowerShaman == null;
                if (flag)
                {
                    this.magicPowerShaman = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Totems
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_ChainLightning
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Enrage
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Hex,
                            TorannMagicDefOf.TM_Hex_I,
                            TorannMagicDefOf.TM_Hex_II,
                            TorannMagicDefOf.TM_Hex_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SpiritWolves,
                            TorannMagicDefOf.TM_SpiritWolves_I,
                            TorannMagicDefOf.TM_SpiritWolves_II,
                            TorannMagicDefOf.TM_SpiritWolves_III
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_GuardianSpirit
                        }, true),
                    };
                }
                return this.magicPowerShaman;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Totems
        {
            get
            {
                bool flag = this.magicPowerSkill_Totems == null;
                if (flag)
                {
                    this.magicPowerSkill_Totems = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Totems_pwr", "TM_Totems_pwr_desc"), // power of totems
                        new MagicPowerSkill("TM_Totems_eff", "TM_Totems_eff_desc"), // mana cost 
                        new MagicPowerSkill("TM_Totems_ver", "TM_Totems_ver_desc")  // duration
                    };
                }
                return this.magicPowerSkill_Totems;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_ChainLightning
        {
            get
            {
                bool flag = this.magicPowerSkill_ChainLightning == null;
                if (flag)
                {
                    this.magicPowerSkill_ChainLightning = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_ChainLightning_pwr", "TM_ChainLightning_pwr_desc"), // damage
                        new MagicPowerSkill("TM_ChainLightning_eff", "TM_ChainLightning_eff_desc"), // reduces mana cost
                        new MagicPowerSkill("TM_ChainLightning_ver", "TM_ChainLightning_ver_desc")  // number of forks, fork count
                    };
                }
                return this.magicPowerSkill_ChainLightning;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Enrage
        {
            get
            {
                bool flag = this.magicPowerSkill_Enrage == null;
                if (flag)
                {
                    this.magicPowerSkill_Enrage = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Enrage_pwr", "TM_Enrage_pwr_desc"), // severity
                        new MagicPowerSkill("TM_Enrage_eff", "TM_Enrage_eff_desc"), // reduces mana cost
                        new MagicPowerSkill("TM_Enrage_ver", "TM_Enrage_ver_desc")  // application count
                    };
                }
                return this.magicPowerSkill_Enrage;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Hex
        {
            get
            {
                bool flag = this.magicPowerSkill_Hex == null;
                if (flag)
                {
                    this.magicPowerSkill_Hex = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Hex_pwr", "TM_Hex_pwr_desc"), // severity, what other effects are available after hexing
                        new MagicPowerSkill("TM_Hex_eff", "TM_Hex_eff_desc"), // reduces mana cost
                        new MagicPowerSkill("TM_Hex_ver", "TM_Hex_ver_desc")  // appication count, other stuff related to hex actions
                    };
                }
                return this.magicPowerSkill_Hex;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SpiritWolves
        {
            get
            {
                bool flag = this.magicPowerSkill_SpiritWolves == null;
                if (flag)
                {
                    this.magicPowerSkill_SpiritWolves = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SpiritWolves_pwr", "TM_SpiritWolves_pwr_desc"), // damage
                        new MagicPowerSkill("TM_SpiritWolves_eff", "TM_SpiritWolves_eff_desc"), // reduces mana cost
                        new MagicPowerSkill("TM_SpiritWolves_ver", "TM_SpiritWolves_ver_desc")  // width
                    };
                }
                return this.magicPowerSkill_SpiritWolves;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_GuardianSpirit
        {
            get
            {
                bool flag = this.magicPowerSkill_GuardianSpirit == null;
                if (flag)
                {
                    this.magicPowerSkill_GuardianSpirit = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_GuardianSpirit_pwr", "TM_GuardianSpirit_pwr_desc"), // power of GuardianSpirit
                        new MagicPowerSkill("TM_GuardianSpirit_eff", "TM_GuardianSpirit_eff_desc"), // mana cost 
                        new MagicPowerSkill("TM_GuardianSpirit_ver", "TM_GuardianSpirit_ver_desc")  // effect radius, effects
                    };
                }
                return this.magicPowerSkill_GuardianSpirit;
            }
        }

        public List<MagicPower> MagicPowersGolemancer
        {
            get
            {
                bool flag = this.magicPowerGolemancer == null;
                if (flag)
                {
                    this.magicPowerGolemancer = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Golemancy
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_RuneCarving
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_Branding
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SigilSurge
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_SigilDrain
                        }),
                        new MagicPower(new List<AbilityUser.AbilityDef>
                        {
                            TorannMagicDefOf.TM_LivingWall
                        }, true),
                    };
                }
                return this.magicPowerGolemancer;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Golemancy
        {
            get
            {
                bool flag = this.magicPowerSkill_Golemancy == null;
                if (flag)
                {
                    this.magicPowerSkill_Golemancy = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Golemancy_pwr", "TM_Golemancy_pwr_desc"), // power of golems, 15 ranks
                        new MagicPowerSkill("TM_Golemancy_eff", "TM_Golemancy_eff_desc"), // mana cost to upkeep a golem, 15 ranks
                        new MagicPowerSkill("TM_Golemancy_ver", "TM_Golemancy_ver_desc")  // abilities and skills available to a golem, 15 ranks
                    };
                }
                return this.magicPowerSkill_Golemancy;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_RuneCarving
        {
            get
            {
                bool flag = this.magicPowerSkill_RuneCarving == null;
                if (flag)
                {
                    this.magicPowerSkill_RuneCarving = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_RuneCarving_pwr", "TM_RuneCarving_pwr_desc"), // efficiency boost to parts, 3 ranks, 2 pt level cost
                        new MagicPowerSkill("TM_RuneCarving_eff", "TM_RuneCarving_eff_desc"), // returns 10% mana per skill level
                        new MagicPowerSkill("TM_RuneCarving_ver", "TM_RuneCarving_ver_desc")  // increases chance of success by 5%
                    };
                }
                return this.magicPowerSkill_RuneCarving;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_Branding
        {
            get
            {
                bool flag = this.magicPowerSkill_Branding == null;
                if (flag)
                {
                    this.magicPowerSkill_Branding = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_Branding_pwr", "TM_Branding_pwr_desc"), // severity, 5 ranks
                        new MagicPowerSkill("TM_Branding_eff", "TM_Branding_eff_desc") // reduces upkeep cost, 5 ranks
                        //new MagicPowerSkill("TM_Branding_ver", "TM_Branding_ver_desc")  // 
                    };
                }
                return this.magicPowerSkill_Branding;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SigilSurge
        {
            get
            {
                bool flag = this.magicPowerSkill_SigilSurge == null;
                if (flag)
                {
                    this.magicPowerSkill_SigilSurge = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SigilSurge_pwr", "TM_SigilSurge_pwr_desc"), // severity boost when active
                        new MagicPowerSkill("TM_SigilSurge_eff", "TM_SigilSurge_eff_desc"), // reduces mana upkeep while active
                        new MagicPowerSkill("TM_SigilSurge_ver", "TM_SigilSurge_ver_desc")  // reduces 'feedback' effects on golemancer (pain)
                    };
                }
                return this.magicPowerSkill_SigilSurge;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_SigilDrain
        {
            get
            {
                bool flag = this.magicPowerSkill_SigilDrain == null;
                if (flag)
                {
                    this.magicPowerSkill_SigilDrain = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_SigilDrain_pwr", "TM_SigilDrain_pwr_desc"), // bonus of drain effects on golemancer
                        new MagicPowerSkill("TM_SigilDrain_eff", "TM_SigilDrain_eff_desc"), // reduces mana upkeep cost
                        new MagicPowerSkill("TM_SigilDrain_ver", "TM_SigilDrain_ver_desc")  // reduces feedback effects on other pawn (pain)
                    };
                }
                return this.magicPowerSkill_SigilDrain;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_LivingWall
        {
            get
            {
                bool flag = this.magicPowerSkill_LivingWall == null;
                if (flag)
                {
                    this.magicPowerSkill_LivingWall = new List<MagicPowerSkill>
                    {
                        new MagicPowerSkill("TM_LivingWall_pwr", "TM_LivingWall_pwr_desc"), // power of living walls abilities 
                        new MagicPowerSkill("TM_LivingWall_eff", "TM_LivingWall_eff_desc"), // mana upkeep cost
                        new MagicPowerSkill("TM_LivingWall_ver", "TM_LivingWall_ver_desc")  // movement and action quickness
                    };
                }
                return this.magicPowerSkill_LivingWall;
            }
        }

        public bool IsNecromancer
        {
            get
            {
                return this.isNecromancer || TM_Calc.IsNecromancer(this.MagicPawn);
            }
            set
            {
                this.isNecromancer = value;
            }               
        }

        public int DominationCount
        {
            get
            {
                return this.dominationCount;
            }
            set
            {
                this.dominationCount = value;
            }
        }

        public bool Initialized
        {
            get
            {
                return this.initialized;
            }
            set
            {
                this.initialized = value;
            }
        }

        public int MagicUserLevel
        {
            get
            {
                return this.magicUserLevel;
            }
            set
            {
                this.magicUserLevel = value;
            }
        }

        public int MagicUserXP
        {
            get
            {
                return this.magicUserXP;
            }
            set
            {
                this.magicUserXP = value;
            }
        }

        public int MagicAbilityPoints
        {
            get
            {
                return this.magicAbilityPoints;
            }
            set
            {
                this.magicAbilityPoints = value;
            }
        }

        public int TicksToLearnMagicXP
        {
            get
            {
                return this.ticksToLearnMagicXP;
            }
            set
            {
                this.ticksToLearnMagicXP = value;
            }
        }

        public int TicksAffiliation
        {
            get
            {
                return this.ticksAffiliation;
            }
            set
            {
                this.ticksAffiliation = value;
            }
        }

        public Pawn MagicPawn
        {
            get
            {
                return this.magicPawn;
            }
        }

        public Faction Affiliation
        {
            get
            {
                return this.affiliation;
            }
            set
            {
                this.affiliation = value; 
            }
        }

        private Dictionary<TMAbilityDef, MagicPowerSkill> skillEfficiency = new Dictionary<TMAbilityDef, MagicPowerSkill>();
        public MagicPowerSkill GetSkill_Efficiency(TMAbilityDef ability)
        {
            if (!skillEfficiency.ContainsKey(ability))
            {
                bool hasSkill = false;
                string s = ability.defName.ToString();
                char[] trim = { '_', 'I', 'V', 'X' };
                s = s.TrimEnd(trim) + "_eff";
                for (int i = 0; i < AllMagicPowerSkills.Count; i++)
                {

                    MagicPowerSkill mps = AllMagicPowerSkills[i];
                    
                    if (mps.label.Contains(s))
                    {
                        skillEfficiency.Add(ability, mps);
                        hasSkill = true;
                    }
                }
                if (!hasSkill)
                {
                    skillEfficiency.Add(ability, null);
                }
            }
            return skillEfficiency[ability];
        }

        private Dictionary<TMAbilityDef, MagicPowerSkill> skillVersatility = new Dictionary<TMAbilityDef, MagicPowerSkill>();
        public MagicPowerSkill GetSkill_Versatility(TMAbilityDef ability)
        {
            if (!skillVersatility.ContainsKey(ability))
            {
                bool hasSkill = false;
                string s = ability.defName.ToString();
                char[] trim = { '_', 'I', 'V', 'X' };
                s = s.TrimEnd(trim) + "_ver";
                for (int i = 0; i < AllMagicPowerSkills.Count; i++)
                {
                    MagicPowerSkill mps = AllMagicPowerSkills[i];
                    
                    if (mps.label.Contains(s))
                    {
                        skillVersatility.Add(ability, mps);
                        hasSkill = true;
                    }
                }
                if (!hasSkill) //check custom powers for different ability to skill names
                {
                    List<TM_CustomPowerDef> customPowers = TM_Data.CustomMagePowerDefs().ToList();
                    for (int i = 0; i < customPowers.Count; i++)
                    {
                        for (int j = 0; j < customPowers[i].customPower.abilityDefs.Count; j++)
                        {
                            if (ability.defName == customPowers[i].customPower.abilityDefs[j].ToString())
                            {
                                for (int k = 0; k < AllMagicPowerSkills.Count; k++)
                                {
                                    MagicPowerSkill mps = AllMagicPowerSkills[k];
                                    foreach (TM_CustomSkill cs in customPowers[i].customPower.skills)
                                    {
                                        if (cs.label.Contains("_ver") && cs.label == mps.label)
                                        {
                                            skillVersatility.Add(ability, mps);
                                            hasSkill = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (!hasSkill)
                {
                    skillVersatility.Add(ability, null);
                }
            }
            return skillVersatility[ability];
        }

        private Dictionary<TMAbilityDef, MagicPowerSkill> skillPower = new Dictionary<TMAbilityDef, MagicPowerSkill>();
        public MagicPowerSkill GetSkill_Power(TMAbilityDef ability)
        {
            if (!skillPower.ContainsKey(ability))
            {
                bool hasSkill = false;
                string s = ability.defName.ToString();
                char[] trim = { '_', 'I', 'V', 'X' };
                s = s.TrimEnd(trim) + "_pwr";
                for (int i = 0; i < AllMagicPowerSkills.Count; i++)
                {                   
                    MagicPowerSkill mps = AllMagicPowerSkills[i];
                    
                    if (mps.label.Contains(s))
                    {
                        skillPower.Add(ability, mps);
                        hasSkill = true;
                    }
                }
                if (!hasSkill) //check custom powers for different ability to skill names
                {
                    List<TM_CustomPowerDef> customPowers = TM_Data.CustomMagePowerDefs().ToList();
                    for (int i = 0; i < customPowers.Count; i++)
                    {
                        for (int j = 0; j < customPowers[i].customPower.abilityDefs.Count; j++)
                        {
                            if (ability.defName == customPowers[i].customPower.abilityDefs[j].ToString())
                            {
                                for (int k = 0; k < AllMagicPowerSkills.Count; k++)
                                {
                                    MagicPowerSkill mps = AllMagicPowerSkills[k];
                                    foreach (TM_CustomSkill cs in customPowers[i].customPower.skills)
                                    {
                                        if (cs.label.Contains("_pwr") && cs.label == mps.label)
                                        {
                                            skillPower.Add(ability, mps);
                                            hasSkill = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (!hasSkill)
                {
                    skillPower.Add(ability, null);
                }
            }
            return skillPower[ability];
        }

        private Dictionary<HediffDef, TMAbilityDef> hediffAbility = new Dictionary<HediffDef, TMAbilityDef>();
        public TMAbilityDef GetHediffAbility(Hediff hd)
        {
            if (!hediffAbility.ContainsKey(hd.def))
            {
                bool hasAbility = false;
                for (int i = 0; i < AllMagicPowers.Count; i++)
                {
                    if (AllMagicPowers[i].abilityDef is TMAbilityDef)
                    {
                        TMAbilityDef ability = (TMAbilityDef)AllMagicPowers[i].abilityDef;
                        if (ability.abilityHediff != null && ability.abilityHediff == hd.def)
                        {
                            hediffAbility.Add(hd.def, ability);
                            hasAbility = true;
                        }
                    }
                }
                if (!hasAbility)
                {
                    hediffAbility.Add(hd.def, null);
                }
            }
            return hediffAbility[hd.def];
        }

        private int uniquePowersCount = 0;
        public int GetUniquePowersWithSkillsCount(TMDefs.TM_CustomClass customClass)
        {
            if (uniquePowersCount != 0)
            {
                return uniquePowersCount;
            }
            List<TMAbilityDef> abilities = new List<TMAbilityDef>();
            abilities.Clear();
            for (int i = 0; i < customClass.classMageAbilities.Count; i++)
            {
                bool unique = true;
                for (int j = 0; j < abilities.Count; j++)
                {
                    if (customClass.classMageAbilities[i].defName.Contains(abilities[j].defName))
                    {
                        unique = false;
                    }
                }
                if (unique)
                {
                    abilities.Add(customClass.classMageAbilities[i]);
                }
            }
            uniquePowersCount = abilities.Count;
            return uniquePowersCount;
        }

        public MagicPower ReturnMatchingMagicPower(TMAbilityDef def)
        {
            for (int i = 0; i < AllMagicPowers.Count; i++)
            {
                if (AllMagicPowers[i].TMabilityDefs.Contains(def))
                {
                    if (AllMagicPowers[i] == MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                                            AllMagicPowers[i] == MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                                            AllMagicPowers[i] == MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate))
                    {
                        //do nothing
                    }
                    else
                    {
                        return AllMagicPowers[i];
                    }
                }
            }
            return null;
        }

        public IEnumerable<MagicPower> Powers
        {
            get
            {
                return this.MagicPowersCM.Concat(this.MagicPowersW.Concat(this.MagicPowersC.Concat(this.MagicPowersE.Concat(this.MagicPowersBM.Concat(this.MagicPowersIF.Concat(this.MagicPowersHoF.Concat(this.MagicPowersSB.Concat(this.MagicPowersA.Concat(this.MagicPowersP.Concat(this.MagicPowersS.Concat(this.MagicPowersD.Concat(this.MagicPowersN.Concat(this.MagicPowersPR.Concat(this.MagicPowersB.Concat(this.MagicPowersWD.Concat(this.MagicPowersSD.Concat(this.MagicPowersG.Concat(this.MagicPowersT.Concat(this.MagicPowersStandalone)))))))))))))))))));
            }
        }

        List<MagicPower> allMagicPowersForChaosMageList = new List<MagicPower>();
        public List<MagicPower> AllMagicPowersForChaosMage
        {
            get
            {
                if (allMagicPowersForChaosMageList == null || allMagicPowersForChaosMageList.Count <= 0)
                {
                    List<MagicPower> tmpList = new List<MagicPower>();
                    tmpList.Clear();
                    allMagicPowersForChaosMageList = new List<MagicPower>();
                    allMagicPowersForChaosMageList.Clear();
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersW);
                    tmpList.Add(this.MagicPowersIF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Firestorm));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersIF.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(this.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Rainmaker));
                    tmpList.Add(this.MagicPowersHoF.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Blizzard));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersHoF.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(this.MagicPowersSB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersSB.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(this.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FoldReality));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersA.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(this.MagicPowersP.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_HolyWrath));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersP.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersS.Except(MagicPowersS.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SummonPoppi)));
                    tmpList.Add(this.MagicPowersD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RegrowLimb));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersD.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(this.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_RaiseUndead));
                    tmpList.Add(this.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_LichForm));
                    tmpList.Add(this.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DeathBolt));
                    tmpList.Add(this.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DeathBolt_I));
                    tmpList.Add(this.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DeathBolt_II));
                    tmpList.Add(this.MagicPowersN.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_DeathBolt_III));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersN.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersPR.Except(MagicPowersPR.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Resurrection)));
                    tmpList.Add(this.MagicPowersB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BardTraining));
                    tmpList.Add(this.MagicPowersB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Inspire));
                    tmpList.Add(this.MagicPowersB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Entertain));
                    tmpList.Add(this.MagicPowersB.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_BattleHymn));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersB.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersWD.Except(MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_PsychicShock)));
                    tmpList.Add(this.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond));
                    tmpList.Add(this.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt));
                    tmpList.Add(this.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate));
                    tmpList.Add(this.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Scorn));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersSD.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersG.Except(MagicPowersG.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Meteor)));
                    tmpList.Add(this.MagicPowersT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike));
                    tmpList.Add(this.MagicPowersT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_I));
                    tmpList.Add(this.MagicPowersT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_II));
                    tmpList.Add(this.MagicPowersT.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_III));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersT.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersE.Except(MagicPowersE.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Shapeshift)));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersC.Except(MagicPowersC.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Recall)));
                    allMagicPowersForChaosMageList.Add((this.MagicPowersShadow.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowWalk)));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersBrightmage.Except(MagicPowersBrightmage.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SpiritOfLight)));
                    tmpList.Add(MagicPowersShaman.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_GuardianSpirit));
                    //tmpList.Add(this.MagicPowersShaman.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Totems));
                    allMagicPowersForChaosMageList.AddRange(this.MagicPowersShaman.Except(tmpList));
                    tmpList.Clear();
                }
                return allMagicPowersForChaosMageList;
            }
        }

        List<MagicPower> allMagicPowersList = new List<MagicPower>();
        public List<MagicPower> AllMagicPowers
        {
            get
            {
                if (allMagicPowersList == null || allMagicPowersList.Count <= 0)
                {
                    allMagicPowersList = new List<MagicPower>();
                    allMagicPowersList.Clear();
                    allMagicPowersList.AddRange(AllMagicPowersWithSkills);
                    allMagicPowersList.AddRange(this.MagicPowersStandalone);
                    allMagicPowersList.AddRange(this.MagicPowersCustomStandalone);
                }
                return allMagicPowersList;
            }
        }

        List<MagicPower> allMagicPowersWithSkillsList = new List<MagicPower>();
        public List<MagicPower> AllMagicPowersWithSkills
        {
            get
            {
                if (allMagicPowersWithSkillsList == null || allMagicPowersWithSkillsList.Count <= 0)
                {
                    allMagicPowersWithSkillsList = new List<MagicPower>();
                    allMagicPowersWithSkillsList.Clear();
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersCustom);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersCM);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersW);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersC);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersE);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersBM);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersIF);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersHoF);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersSB);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersA);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersP);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersPR);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersS);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersD);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersN);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersB);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersSD);
                    //allMagicPowersWithSkillsList.Add(this.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Repulsion));
                    //allMagicPowersWithSkillsList.Add(this.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_PsychicShock));
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersWD);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersG);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersT);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersShadow);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersBrightmage);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersShaman);
                    allMagicPowersWithSkillsList.AddRange(this.MagicPowersGolemancer);
                }
                return allMagicPowersWithSkillsList;
            }
        }

        private List<MagicPower> uniquePowers = new List<MagicPower>();
        public List<MagicPower> AllUniqueMagicPowers
        {
            get
            {
                if(uniquePowers == null || uniquePowers.Count <= 0)
                {
                    List<MagicPower> tmpList = new List<MagicPower>();
                    tmpList.Clear();
                    for(int i = 0; i < AllMagicPowers.Count; i++)
                    {
                        if(!tmpList.Contains(AllMagicPowers[i]))
                        {
                            tmpList.Add(AllMagicPowers[i]);
                        }
                    }
                    uniquePowers = tmpList;
                }
                return uniquePowers;
            }
        }

        List<MagicPowerSkill> allMagicPowerSkillsList = new List<MagicPowerSkill>();
        public List<MagicPowerSkill> AllMagicPowerSkills
        {
            get
            {
                if (allMagicPowerSkillsList == null || allMagicPowerSkillsList.Count <= 0)
                {
                    allMagicPowerSkillsList = new List<MagicPowerSkill>();
                    allMagicPowerSkillsList.Clear();
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_AccelerateTime);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_AdvancedHeal);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_AlterFate);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_AMP);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Attraction);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BardTraining);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BattleHymn);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BestowMight);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Blink);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Blizzard);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BloodForBlood);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BloodGift);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BloodMoon);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_BloodShield);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Cantrips);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ChaosTradition);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ChronostaticField);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ConsumeCorpse);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_CorpseExplosion);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_CureDisease);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_DeathBolt);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_DeathMark);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Dominate);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_EarthernHammer);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_EarthSprites);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Encase);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_EnchantedBody);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_EnchanterStone);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_EnchantWeapon);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Entertain);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_EyeOfTheStorm);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Fireball);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Firebolt);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Fireclaw);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Firestorm);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_FogOfTorment);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_FoldReality);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_FrostRay);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_global_eff);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_global_regen);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_global_spirit);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Heal);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_HealingCircle);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_HolyWrath);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Icebolt);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_IgniteBlood);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Inspire);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LichForm);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LightningBolt);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LightningCloud);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LightningStorm);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Lullaby);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_MagicMissile);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Meteor);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_OrbitalStrike);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Overdrive);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Overwhelm);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Poison);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Polymorph);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Prediction);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_PsychicShock);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Purify);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Rainmaker);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_RaiseUndead);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_RayofHope);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_P_RayofHope);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Recall);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Regenerate);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_RegrowLimb);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Rend);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Repulsion);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Resurrection);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ReverseTime);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Sabotage);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Scorn);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Sentinel);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Shadow);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ShadowBolt);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Shapeshift);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Shield);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Snowball);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Soothe);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SootheAnimal);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SoulBond);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Stoneskin);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Summon);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SummonElemental);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SummonExplosive);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SummonMinion);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SummonPoppi);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SummonPylon);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_TechnoBit);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_TechnoShield);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_TechnoTurret);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_TechnoWeapon);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Teleport);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Transmutate);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ValiantCharge);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_WandererCraft);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ShadowWalk);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LightLance);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Sunfire);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LightBurst);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LightSkip);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Refraction);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SpiritOfLight);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Totems);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_ChainLightning);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Enrage);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Hex);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SpiritWolves);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_GuardianSpirit);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Golemancy);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_RuneCarving);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Branding);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SigilSurge);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_SigilDrain);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_LivingWall);
                    allMagicPowerSkillsList.AddRange(this.MagicPowerSkill_Custom);
                }
                return allMagicPowerSkillsList;
            }
        }

        public void ResetAllSkills()
        {
            List<MagicPowerSkill> mps = new List<MagicPowerSkill>();
            mps.Clear();
            mps = this.AllMagicPowerSkills; 
            for(int i = 0; i < mps.Count; i++)
            {
                mps[i].level = 0;
            }
        }

        public MagicData()
        {
        }

        public MagicData(CompAbilityUserMagic newUser)
        {
            this.magicPawn = newUser.AbilityUser;            
        }

        public void ClearData()
        {
            this.magicUserLevel = 0;
            this.magicUserXP = 0;
            this.magicAbilityPoints = 0;
            this.dominationCount = 0;
            this.magicPowerW.Clear();
            this.magicPowerA.Clear();
            this.magicPowerB.Clear();
            this.magicPowerD.Clear();
            this.magicPowerG.Clear();
            this.magicPowerHoF.Clear();
            this.magicPowerIF.Clear();
            this.magicPowerN.Clear();
            this.magicPowerP.Clear();
            this.magicPowerPR.Clear();
            this.magicPowerS.Clear();
            this.magicPowerSB.Clear();
            this.magicPowerSD.Clear();
            this.magicPowerC.Clear();
            this.magicPowerCM.Clear();
            this.magicPowerShadow.Clear();
            this.magicPowerBrightmage.Clear();
            this.magicPowerShaman.Clear();
            this.magicPowerGolemancer.Clear();
            this.magicPowerCustom.Clear();
            this.magicPawn = null;            
            this.initialized = false;           
        }

        public void ExposeData()
        {
            Scribe_References.Look<Pawn>(ref this.magicPawn, "magicPawn", false);
            Scribe_Values.Look<int>(ref this.magicUserLevel, "magicUserLevel", 0, false);
            Scribe_Values.Look<int>(ref this.magicUserXP, "magicUserXP", 0, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.magicAbilityPoints, "magicAbilityPoints", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToLearnMagicXP, "ticksToLearnMagicXP", -1, false);
            Scribe_Values.Look<int>(ref this.ticksAffiliation, "ticksAffiliation", -1, false);
            Scribe_Values.Look<int>(ref this.dominationCount, "dominationCount", 0, false);
            Scribe_Values.Look<bool>(ref this.isNecromancer, "isNecromancer", false, false);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_global_eff, "magicPowerSkill_global_eff", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_global_regen, "magicPowerSkill_global_regen", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_global_spirit, "magicPowerSkill_global_spirit", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerStandalone, "magicPowerStandalone", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerCustom, "magicPowerCustom", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerCustomStandalone, "magicPowerCustomStandalone", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Custom, "magicPowerSkill_Custom", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerW, "magicPowerW", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_WandererCraft, "magicPowerSkill_WandererCraft", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Cantrips, "magicPowerSkill_Cantrips", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerIF, "magicPowerIF", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_RayofHope, "magicPowerSkill_RayofHope", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Fireball, "magicPowerSkill_Fireball", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Firebolt, "magicPowerSkill_Firebolt", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Fireclaw, "magicPowerSkill_Fireclaw", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Firestorm, "magicPowerSkill_Firestorm", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerHoF, "magicPowerHoF", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Soothe, "magicPowerSkill_Soothe", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Icebolt, "magicPowerSkill_Icebolt", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_FrostRay, "magicPowerSkill_FrostRay", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Snowball, "magicPowerSkill_Snowball", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Rainmaker, "magicPowerSkill_Rainmaker", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Blizzard, "magicPowerSkill_Blizzard", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerSB, "magicPowerSB", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_AMP, "magicPowerSkill_AMP", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LightningBolt, "magicPowerSkill_LightningBolt", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LightningCloud, "magicPowerSkill_LightningCloud", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LightningStorm, "magicPowerSkill_LightningStorm", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_EyeOfTheStorm, "magicPowerSkill_EyeOfTheStorm", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerA, "magicPowerA", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Shadow, "magicPowerSkill_Shadow", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_MagicMissile, "magicPowerSkill_MagicMissile", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Blink, "magicPowerSkill_Blink", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Summon, "magicPowerSkill_Summon", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Teleport, "magicPowerSkill_Teleport", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_FoldReality, "magicPowerSkill_FoldReality", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerP, "magicPowerP", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_P_RayofHope, "magicPowerSkill_P_RayofHope", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Heal, "magicPowerSkill_Heal", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Shield, "magicPowerSkill_Shield", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ValiantCharge, "magicPowerSkill_ValiantCharge", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Overwhelm, "magicPowerSkill_Overwhelm", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_HolyWrath, "magicPowerSkill_HolyWrath", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerS, "magicPowerS", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SummonMinion, "magicPowerSkill_SummonMinion", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SummonPylon, "magicPowerSkill_SummonPylon", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SummonExplosive, "magicPowerSkill_SummonExplosive", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SummonElemental, "magicPowerSkill_SummonElemental", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SummonPoppi, "magicPowerSkill_SummonPoppi", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerD, "magicPowerD", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Poison, "magicPowerSkill_Poison", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SootheAnimal, "magicPowerSkill_SootheAnimal", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Regenerate, "magicPowerSkill_Regenerate", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_CureDisease, "magicPowerSkill_CureDisease", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_RegrowLimb, "magicPowerSkill_RegrowLimb", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerN, "magicPowerN", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_RaiseUndead, "magicPowerSkill_RaiseUndead", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_DeathMark, "magicPowerSkill_DeathMark", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_FogOfTorment, "magicPowerSkill_FogOfTorment", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ConsumeCorpse, "magicPowerSkill_ConsumeCorpse", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_CorpseExplosion, "magicPowerSkill_CorpseExplosion", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LichForm, "magicPowerSkill_LichForm", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_DeathBolt, "magicPowerSkill_DeathBolt", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerPR, "magicPowerPR", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_AdvancedHeal, "magicPowerSkill_AdvancedHeal", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Purify, "magicPowerSkill_Purify", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_HealingCircle, "magicPowerSkill_HealingCircle", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BestowMight, "magicPowerSkill_BestowMight", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Resurrection, "magicPowerSkill_Resurrection", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerB, "magicPowerB", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BardTraining, "magicPowerSkill_BardTraining", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Entertain, "magicPowerSkill_Entertain", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Inspire, "magicPowerSkill_Inspire", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Lullaby, "magicPowerSkill_Lullaby", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BattleHymn, "magicPowerSkill_BattleHymn", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerWD, "magicPowerWD", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerSD, "magicPowerSD", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SoulBond, "magicPowerSkill_SoulBond", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ShadowBolt, "magicPowerSkill_ShadowBolt", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Dominate, "magicPowerSkill_Dominate", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Repulsion, "magicPowerSkill_Repulsion", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Attraction, "magicPowerSkill_Attraction", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Scorn, "magicPowerSkill_Scorn", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_PsychicShock, "magicPowerSkill_PsychicShock", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerG, "magicPowerG", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Stoneskin, "magicPowerSkill_Stoneskin", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Encase, "magicPowerSkill_Encase", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_EarthSprites, "magicPowerSkill_EarthSprites", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_EarthernHammer, "magicPowerSkill_EarthernHammer", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Sentinel, "magicPowerSkill_Sentinel", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Meteor, "magicPowerSkill_Meteor", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerT, "magicPowerT", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_TechnoBit, "magicPowerSkill_TechnoBit", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_TechnoTurret, "magicPowerSkill_TechnoTurret", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_TechnoWeapon, "magicPowerSkill_TechnoWeapon", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_TechnoShield, "magicPowerSkill_TechnoShield", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Sabotage, "magicPowerSkill_Sabotage", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Overdrive, "magicPowerSkill_Overdrive", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_OrbitalStrike, "magicPowerSkill_OrbitalStrike", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerBM, "magicPowerBM", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BloodGift, "magicPowerSkill_BloodGift", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_IgniteBlood, "magicPowerSkill_IgniteBlood", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BloodForBlood, "magicPowerSkill_BloodForBlood", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BloodShield, "magicPowerSkill_BloodShield", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Rend, "magicPowerSkill_Rend", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_BloodMoon, "magicPowerSkill_BloodMoon", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerE, "magicPowerE", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_EnchantedBody, "magicPowerSkill_EnchantedBody", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Transmutate, "magicPowerSkill_Transmutate", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_EnchanterStone, "magicPowerSkill_EnchanterStone", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_EnchantWeapon, "magicPowerSkill_EnchantWeapon", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Polymorph, "magicPowerSkill_Polymorph", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Shapeshift, "magicPowerSkill_Shapeshift", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerC, "magicPowerC", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Prediction, "magicPowerSkill_Prediction", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_AlterFate, "magicPowerSkill_AlterFate", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_AccelerateTime, "magicPowerSkill_AccelerateTime", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ReverseTime, "magicPowerSkill_ReverseTime", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ChronostaticField, "magicPowerSkill_ChronostaticField", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Recall, "magicPowerSkill_Recall", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerCM, "magicPowerCM", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ChaosTradition, "magicPowerSkill_ChaosTradition", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerShadow, "magicPowerShadow", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ShadowWalk, "magicPowerSkill_ShadowWalk", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerBrightmage, "magicPowerBrightmage", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LightLance, "magicPowerSkill_LightLance", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Sunfire, "magicPowerSkill_Sunfire", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LightBurst, "magicPowerSkill_LightBurst", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LightSkip, "magicPowerSkill_LightSkip", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Refraction, "magicPowerSkill_Refraction", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SpiritOfLight, "magicPowerSkill_SpiritOfLight", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerShaman, "magicPowerShaman", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Totems, "magicPowerSkill_Totems", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_ChainLightning, "magicPowerSkill_ChainLightning", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Enrage, "magicPowerSkill_Enrage", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Hex, "magicPowerSkill_Hex", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SpiritWolves, "magicPowerSkill_SpiritWolves", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_GuardianSpirit, "magicPowerSkill_GuardianSpirit", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPower>(ref this.magicPowerGolemancer, "magicPowerGolemancer", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Golemancy, "magicPowerSkill_Golemancy", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_RuneCarving, "magicPowerSkill_RuneCarving", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_Branding, "magicPowerSkill_Branding", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SigilSurge, "magicPowerSkill_SigilSurge", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_SigilDrain, "magicPowerSkill_SigilDrain", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<MagicPowerSkill>(ref this.magicPowerSkill_LivingWall, "magicPowerSkill_LivingWall", LookMode.Deep, new object[0]);

        }
    }
}
