using AbilityUser;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using TorannMagic.TMDefs;
using AbilityDef=AbilityUser.AbilityDef;

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

        private Dictionary<ushort, MagicPower> magicPowerStandaloneDictionary;
        private List<MagicPower> magicPowerStandalone;

        private List<MagicPower> magicPowerCustom;
        public List<MagicPower> MagicPowersCustom  //supports customs abilities
        {
            get
            {
                if (magicPowerCustom != null) return magicPowerCustom;

                IEnumerable<TM_CustomPowerDef> enumerable = TM_Data.CustomMagePowerDefs();
                magicPowerCustom = new List<MagicPower>();

                foreach (TM_CustomPowerDef current in enumerable)
                {
                    bool newPower = false;
                    List<AbilityDef> abilityList = current.customPower.abilityDefs;
                    bool requiresScroll = current.customPower.requiresScroll;
                    MagicPower mp = new MagicPower(abilityList, requiresScroll)
                    {
                        maxLevel = current.customPower.maxLevel,
                        learnCost = current.customPower.learnCost,
                        costToLevel = current.customPower.costToLevel,
                        autocasting = current.customPower.autocasting
                    };
                    if (!magicPowerCustom.Any(a => a.GetAbilityDef(0) == mp.GetAbilityDef(0)))
                    {
                        newPower = true;
                    }
                    bool hasSkills = false;
                    if (current.customPower.skills != null)
                    {
                        foreach (TM_CustomSkill skill in current.customPower.skills)
                        {
                            if (skill == null) continue;

                            MagicPowerSkill mps = new MagicPowerSkill(skill.label, skill.description)
                            {
                                levelMax = skill.levelMax,
                                costToLevel = skill.costToLevel
                            };
                            if (!MagicPowerSkill_Custom.Any(b => b.label == mps.label) && !AllMagicPowerSkills.Any(b => b.label == mps.label))
                            {
                                MagicPowerSkill_Custom.Add(mps);
                            }
                            hasSkills = true;
                        }
                    }
                    if (newPower)
                    {
                        if (hasSkills)
                        {
                            magicPowerCustom.Add(mp);
                        }
                        else if (!MagicPowersCustomStandalone.Any(a => a.GetAbilityDef(0) == mp.GetAbilityDef(0)))
                        {
                            MagicPowersCustomStandalone.Add(mp);
                        }
                    }
                    if (hasSkills && current.customPower.chaosMageUseable && !AllMagicPowersForChaosMage.Contains(mp))
                    {
                        AllMagicPowersForChaosMage.Add(mp);
                    }
                }
                allMagicPowerSkillsList = null; //force rediscovery and caching to include custom defs
                return magicPowerCustom;
            }
        }

        private List<MagicPower> magicPowerCustomStandalone;
        //supports customs abilities
        public List<MagicPower> MagicPowersCustomStandalone => magicPowerCustomStandalone ?? (magicPowerCustomStandalone = new List<MagicPower>());

        private List<MagicPower> magicPowerCustomAll;
        public List<MagicPower> MagicPowersCustomAll
        {
            get
            {
                if (magicPowerCustomAll != null) return magicPowerCustomAll;

                magicPowerCustomAll = new List<MagicPower>();
                magicPowerCustomAll.AddRange(MagicPowersCustom);
                magicPowerCustomAll.AddRange(MagicPowersCustomStandalone);
                return magicPowerCustomAll;
            }
        }

        private List<MagicPowerSkill> magicPowerSkill_Custom;
        public List<MagicPowerSkill> MagicPowerSkill_Custom => magicPowerSkill_Custom ?? (magicPowerSkill_Custom = new List<MagicPowerSkill>());

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

        public List<MagicPowerSkill> MagicPowerSkill_global_regen =>
            magicPowerSkill_global_regen ?? (magicPowerSkill_global_regen = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_global_regen_pwr", "TM_global_regen_pwr_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_global_eff =>
            magicPowerSkill_global_eff ?? (magicPowerSkill_global_eff = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_global_eff_pwr", "TM_global_eff_pwr_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_global_spirit =>
            magicPowerSkill_global_spirit ?? (magicPowerSkill_global_spirit = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_global_spirit_pwr", "TM_global_spirit_pwr_desc")
            });

        private List<MagicPower> magicPower;
        public List<MagicPower> MagicPowers => magicPower ?? (magicPower = new List<MagicPower>());

        // Helper function to make code easier to read
        private static MagicPower newPower(AbilityDef abilityDef, bool requiresScrollToLearn = false)
        {
            return new MagicPower(new List<AbilityDef> { abilityDef }, requiresScrollToLearn);
        }

        public Dictionary<ushort, MagicPower> MagicPowerStandaloneDictionary =>
            magicPowerStandaloneDictionary ?? MagicPowersStandalone.ToDictionary(mp => mp.abilityDef.index, mp => mp);

        public List<MagicPower> MagicPowersStandalone  //spells needed for magicpower reference during autocast
        {
            get
            {
                if (magicPowerStandalone != null) return magicPowerStandalone;
                magicPowerStandalone = new List<MagicPower>
                {
                    newPower(TorannMagicDefOf.TM_TransferMana),
                    newPower(TorannMagicDefOf.TM_SiphonMana),
                    newPower(TorannMagicDefOf.TM_SpellMending),
                    newPower(TorannMagicDefOf.TM_CauterizeWound),
                    newPower(TorannMagicDefOf.TM_TeachMagic),
                    newPower(TorannMagicDefOf.TM_EnchantedAura),
                    newPower(TorannMagicDefOf.TM_MechaniteReprogramming),
                    newPower(TorannMagicDefOf.TM_DirtDevil),
                    newPower(TorannMagicDefOf.TM_Heater),
                    newPower(TorannMagicDefOf.TM_Cooler),
                    newPower(TorannMagicDefOf.TM_PowerNode),
                    newPower(TorannMagicDefOf.TM_Sunlight),
                    newPower(TorannMagicDefOf.TM_DryGround),
                    newPower(TorannMagicDefOf.TM_WetGround),
                    newPower(TorannMagicDefOf.TM_ChargeBattery),
                    newPower(TorannMagicDefOf.TM_SmokeCloud),
                    newPower(TorannMagicDefOf.TM_Extinguish),
                    newPower(TorannMagicDefOf.TM_EMP),
                    newPower(TorannMagicDefOf.TM_ManaShield),
                    newPower(TorannMagicDefOf.TM_ArcaneBarrier),
                    newPower(TorannMagicDefOf.TM_Flight),
                    newPower(TorannMagicDefOf.TM_FertileLands),
                    newPower(TorannMagicDefOf.TM_Blur),
                    newPower(TorannMagicDefOf.TM_BlankMind),
                    newPower(TorannMagicDefOf.TM_ArcaneBolt),
                    newPower(TorannMagicDefOf.TM_LightningTrap),
                    newPower(TorannMagicDefOf.TM_Invisibility),
                    newPower(TorannMagicDefOf.TM_BriarPatch),
                    newPower(TorannMagicDefOf.TM_TimeMark),
                    newPower(TorannMagicDefOf.TM_MageLight),
                    newPower(TorannMagicDefOf.TM_NanoStimulant),
                    newPower(TorannMagicDefOf.TM_Ignite),
                    newPower(TorannMagicDefOf.TM_SnapFreeze),
                    newPower(TorannMagicDefOf.TM_ShapeshiftDW),
                    newPower(TorannMagicDefOf.TM_LightSkipMass),
                    newPower(TorannMagicDefOf.TM_LightSkipGlobal),
                    newPower(TorannMagicDefOf.TM_HeatShield)
                };
                return magicPowerStandalone;
            }
        }

        public List<MagicPower> MagicPowersW =>
            magicPowerW ?? (magicPowerW = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_WandererCraft),
                newPower(TorannMagicDefOf.TM_Cantrips),
            });

        public List<MagicPowerSkill> MagicPowerSkill_WandererCraft =>
            magicPowerSkill_WandererCraft ?? (magicPowerSkill_WandererCraft = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_WandererCraft_pwr", "TM_WandererCraft_pwr_desc"),
                new MagicPowerSkill("TM_WandererCraft_eff", "TM_WandererCraft_eff_desc"),
                new MagicPowerSkill("TM_WandererCraft_ver", "TM_WandererCraft_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Cantrips =>
            magicPowerSkill_Cantrips ?? (magicPowerSkill_Cantrips = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Cantrips_pwr", "TM_Cantrips_pwr_desc"),
                new MagicPowerSkill("TM_Cantrips_eff", "TM_Cantrips_eff_desc"),
                new MagicPowerSkill("TM_Cantrips_ver", "TM_Cantrips_ver_desc")
            });

        public List<MagicPower> MagicPowersIF =>
            magicPowerIF ?? (magicPowerIF = new List<MagicPower>
            {
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_RayofHope,
                    TorannMagicDefOf.TM_RayofHope_I,
                    TorannMagicDefOf.TM_RayofHope_II,
                    TorannMagicDefOf.TM_RayofHope_III
                }),
                newPower(TorannMagicDefOf.TM_Firebolt),
                newPower(TorannMagicDefOf.TM_Fireclaw),
                newPower(TorannMagicDefOf.TM_Fireball),
                newPower(TorannMagicDefOf.TM_Firestorm, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_RayofHope =>
            magicPowerSkill_RayofHope ?? (magicPowerSkill_RayofHope = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_RayofHope_eff", "TM_RayofHope_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Firebolt =>
            magicPowerSkill_Firebolt ?? (magicPowerSkill_Firebolt = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Firebolt_pwr", "TM_Firebolt_pwr_desc"),
                new MagicPowerSkill("TM_Firebolt_eff", "TM_Firebolt_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Fireball =>
            magicPowerSkill_Fireball ?? (magicPowerSkill_Fireball = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Fireball_pwr", "TM_Fireball_pwr_desc"),
                new MagicPowerSkill("TM_Fireball_eff", "TM_Fireball_eff_desc"),
                new MagicPowerSkill("TM_Fireball_ver", "TM_Fireball_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Fireclaw =>
            magicPowerSkill_Fireclaw ?? (magicPowerSkill_Fireclaw = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Fireclaw_pwr", "TM_Fireclaw_pwr_desc"),
                new MagicPowerSkill("TM_Fireclaw_eff", "TM_Fireclaw_eff_desc"),
                new MagicPowerSkill("TM_Fireclaw_ver", "TM_Fireclaw_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Firestorm =>
            magicPowerSkill_Firestorm ?? (magicPowerSkill_Firestorm = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Firestorm_pwr", "TM_Firestorm_pwr_desc"),
                new MagicPowerSkill("TM_Firestorm_eff", "TM_Firestorm_eff_desc"),
                new MagicPowerSkill("TM_Firestorm_ver", "TM_Firestorm_ver_desc")
            });

        public List<MagicPower> MagicPowersHoF =>
            magicPowerHoF ?? (magicPowerHoF = new List<MagicPower>
            {
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Soothe,
                    TorannMagicDefOf.TM_Soothe_I,
                    TorannMagicDefOf.TM_Soothe_II,
                    TorannMagicDefOf.TM_Soothe_III
                }),
                newPower(TorannMagicDefOf.TM_Rainmaker),
                newPower(TorannMagicDefOf.TM_Icebolt),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_FrostRay,
                    TorannMagicDefOf.TM_FrostRay_I,
                    TorannMagicDefOf.TM_FrostRay_II,
                    TorannMagicDefOf.TM_FrostRay_III
                }),
                newPower(TorannMagicDefOf.TM_Snowball),
                newPower(TorannMagicDefOf.TM_Blizzard, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Soothe =>
            magicPowerSkill_Soothe ?? (magicPowerSkill_Soothe = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Soothe_eff", "TM_Soothe_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Icebolt =>
            magicPowerSkill_Icebolt ?? (magicPowerSkill_Icebolt = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Icebolt_pwr", "TM_Icebolt_pwr_desc"),
                new MagicPowerSkill("TM_Icebolt_eff", "TM_Icebolt_eff_desc"),
                new MagicPowerSkill("TM_Icebolt_ver", "TM_Icebolt_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_FrostRay =>
            magicPowerSkill_FrostRay ?? (magicPowerSkill_FrostRay = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_FrostRay_eff", "TM_FrostRay_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Snowball =>
            magicPowerSkill_Snowball ?? (magicPowerSkill_Snowball = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Snowball_pwr", "TM_Snowball_pwr_desc"),
                new MagicPowerSkill("TM_Snowball_eff", "TM_Snowball_eff_desc"),
                new MagicPowerSkill("TM_Snowball_ver", "TM_Snowball_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Rainmaker =>
            magicPowerSkill_Rainmaker ?? (magicPowerSkill_Rainmaker = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Rainmaker_eff", "TM_Rainmaker_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Blizzard =>
            magicPowerSkill_Blizzard ?? (magicPowerSkill_Blizzard = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Blizzard_pwr", "TM_Blizzard_pwr_desc"),
                new MagicPowerSkill("TM_Blizzard_eff", "TM_Blizzard_eff_desc"),
                new MagicPowerSkill("TM_Blizzard_ver", "TM_Blizzard_ver_desc")
            });

        public List<MagicPower> MagicPowersSB =>
            magicPowerSB ?? (magicPowerSB = new List<MagicPower>
            {
                new MagicPower(new List<AbilityUser.AbilityDef>
                {
                    TorannMagicDefOf.TM_AMP,
                    TorannMagicDefOf.TM_AMP_I,
                    TorannMagicDefOf.TM_AMP_II,
                    TorannMagicDefOf.TM_AMP_III
                }),
                newPower(TorannMagicDefOf.TM_LightningBolt),
                newPower(TorannMagicDefOf.TM_LightningCloud),
                newPower(TorannMagicDefOf.TM_LightningStorm),
                newPower(TorannMagicDefOf.TM_EyeOfTheStorm, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_AMP =>
            magicPowerSkill_AMP ?? (magicPowerSkill_AMP = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_AMP_eff", "TM_AMP_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_LightningBolt =>
            magicPowerSkill_LightningBolt ?? (magicPowerSkill_LightningBolt = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LightningBolt_pwr", "TM_LightningBolt_pwr_desc"),
                new MagicPowerSkill("TM_LightningBolt_eff", "TM_LightningBolt_eff_desc"),
                new MagicPowerSkill("TM_LightningBolt_ver", "TM_LightningBolt_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_LightningCloud =>
            magicPowerSkill_LightningCloud ?? (magicPowerSkill_LightningCloud = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LightningCloud_pwr", "TM_LightningCloud_pwr_desc"),
                new MagicPowerSkill("TM_LightningCloud_eff", "TM_LightningCloud_eff_desc"),
                new MagicPowerSkill("TM_LightningCloud_ver", "TM_LightningCloud_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_LightningStorm =>
            magicPowerSkill_LightningStorm ?? (magicPowerSkill_LightningStorm = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LightningStorm_pwr", "TM_LightningStorm_pwr_desc"),
                new MagicPowerSkill("TM_LightningStorm_eff", "TM_LightningStorm_eff_desc"),
                new MagicPowerSkill("TM_LightningStorm_ver", "TM_LightningStorm_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_EyeOfTheStorm =>
            magicPowerSkill_EyeOfTheStorm ?? (magicPowerSkill_EyeOfTheStorm = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_EyeOfTheStorm_pwr", "TM_EyeOfTheStorm_pwr_desc"),
                new MagicPowerSkill("TM_EyeOfTheStorm_eff", "TM_EyeOfTheStorm_eff_desc"),
                new MagicPowerSkill("TM_EyeOfTheStorm_ver", "TM_EyeOfTheStorm_ver_desc")
            });

        public List<MagicPower> MagicPowersA =>
            magicPowerA ?? (magicPowerA = new List<MagicPower>
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
                newPower(TorannMagicDefOf.TM_Teleport),
                newPower(TorannMagicDefOf.TM_FoldReality, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Shadow =>
            magicPowerSkill_Shadow ?? (magicPowerSkill_Shadow = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Shadow_eff", "TM_Shadow_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_MagicMissile =>
            magicPowerSkill_MagicMissile ?? (magicPowerSkill_MagicMissile = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_MagicMissile_eff", "TM_MagicMissile_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Blink =>
            magicPowerSkill_Blink ?? (magicPowerSkill_Blink = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Blink_eff", "TM_Blink_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Summon =>
            magicPowerSkill_Summon ?? (magicPowerSkill_Summon = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Summon_eff", "TM_Summon_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Teleport
        {
            get
            {
                return magicPowerSkill_Teleport ?? (magicPowerSkill_Teleport = new List<MagicPowerSkill>
                {
                    new MagicPowerSkill("TM_Teleport_pwr", "TM_Teleport_pwr_desc"),
                    new MagicPowerSkill("TM_Teleport_eff", "TM_Teleport_eff_desc"),
                    new MagicPowerSkill("TM_Teleport_ver", "TM_Teleport_ver_desc")
                });
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_FoldReality =>
            magicPowerSkill_FoldReality ?? (magicPowerSkill_FoldReality = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_FoldReality_eff", "TM_FoldReality_eff_desc")
            });

        bool hasPaladinBuff = false;
        public List<MagicPower> MagicPowersP
        {
            get
            {
                if (magicPowerP == null)
                {
                    magicPowerP = new List<MagicPower>
                    {
                        new MagicPower(new List<AbilityDef>
                        {
                            TorannMagicDefOf.TM_P_RayofHope,
                            TorannMagicDefOf.TM_P_RayofHope_I,
                            TorannMagicDefOf.TM_P_RayofHope_II,
                            TorannMagicDefOf.TM_P_RayofHope_III
                        }),
                        newPower(TorannMagicDefOf.TM_Heal),
                        new MagicPower(new List<AbilityDef>
                        {
                            TorannMagicDefOf.TM_Shield,
                            TorannMagicDefOf.TM_Shield_I,
                            TorannMagicDefOf.TM_Shield_II,
                            TorannMagicDefOf.TM_Shield_III
                        }),
                        newPower(TorannMagicDefOf.TM_ValiantCharge),
                        newPower(TorannMagicDefOf.TM_Overwhelm),
                        newPower(TorannMagicDefOf.TM_HolyWrath, true),

                    };
                }

                if (hasPaladinBuff) return magicPowerP;
                if(magicPowerP.Count >= 6)
                    hasPaladinBuff = true;
                if (hasPaladinBuff) return magicPowerP;

                MagicPower pBuff = new MagicPower(new List<AbilityUser.AbilityDef>
                {
                    TorannMagicDefOf.TM_P_RayofHope,
                    TorannMagicDefOf.TM_P_RayofHope_I,
                    TorannMagicDefOf.TM_P_RayofHope_II,
                    TorannMagicDefOf.TM_P_RayofHope_III
                });
                List<MagicPower> oldList = new List<MagicPower> { pBuff };
                oldList.AddRange(magicPowerP);
                //magicPowerP.Add(pBuff);
                magicPowerP = oldList;
                hasPaladinBuff = true;
                return magicPowerP;
            }
        }
        public List<MagicPowerSkill> MagicPowerSkill_P_RayofHope =>
            magicPowerSkill_P_RayofHope ?? (magicPowerSkill_P_RayofHope = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_P_RayofHope_eff", "TM_P_RayofHope_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Heal =>
            magicPowerSkill_Heal ?? (magicPowerSkill_Heal = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Heal_pwr", "TM_Heal_pwr_desc"),
                new MagicPowerSkill("TM_Heal_eff", "TM_Heal_eff_desc"),
                new MagicPowerSkill("TM_Heal_ver", "TM_Heal_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Shield =>
            magicPowerSkill_Shield ?? (magicPowerSkill_Shield = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Shield_eff", "TM_Shield_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_ValiantCharge =>
            magicPowerSkill_ValiantCharge ?? (magicPowerSkill_ValiantCharge = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ValiantCharge_pwr", "TM_ValiantCharge_pwr_desc"),
                new MagicPowerSkill("TM_ValiantCharge_eff", "TM_ValiantCharge_eff_desc"),
                new MagicPowerSkill("TM_ValiantCharge_ver", "TM_ValiantCharge_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Overwhelm =>
            magicPowerSkill_Overwhelm ?? (magicPowerSkill_Overwhelm = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Overwhelm_pwr", "TM_Overwhelm_pwr_desc"),
                new MagicPowerSkill("TM_Overwhelm_eff", "TM_Overwhelm_eff_desc"),
                new MagicPowerSkill("TM_Overwhelm_ver", "TM_Overwhelm_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_HolyWrath =>
            magicPowerSkill_HolyWrath ?? (magicPowerSkill_HolyWrath = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_HolyWrath_pwr", "TM_HolyWrath_pwr_desc"),
                new MagicPowerSkill("TM_HolyWrath_eff", "TM_HolyWrath_eff_desc"),
                new MagicPowerSkill("TM_HolyWrath_ver", "TM_HolyWrath_ver_desc")
            });

        public List<MagicPower> MagicPowersS =>
            magicPowerS ?? (magicPowerS = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_SummonMinion),
                newPower(TorannMagicDefOf.TM_SummonPylon),
                newPower(TorannMagicDefOf.TM_SummonExplosive),
                newPower(TorannMagicDefOf.TM_SummonElemental),
                newPower(TorannMagicDefOf.TM_SummonPoppi, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_SummonMinion =>
            magicPowerSkill_SummonMinion ?? (magicPowerSkill_SummonMinion = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SummonMinion_pwr", "TM_SummonMinion_pwr_desc"),
                new MagicPowerSkill("TM_SummonMinion_eff", "TM_SummonMinion_eff_desc"),
                new MagicPowerSkill("TM_SummonMinion_ver", "TM_SummonMinion_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_SummonPylon =>
            magicPowerSkill_SummonPylon ?? (magicPowerSkill_SummonPylon = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SummonPylon_pwr", "TM_SummonPylon_pwr_desc"),
                new MagicPowerSkill("TM_SummonPylon_eff", "TM_SummonPylon_eff_desc"),
                new MagicPowerSkill("TM_SummonPylon_ver", "TM_SummonPylon_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_SummonExplosive =>
            magicPowerSkill_SummonExplosive ?? (magicPowerSkill_SummonExplosive = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SummonExplosive_pwr", "TM_SummonExplosive_pwr_desc"),
                new MagicPowerSkill("TM_SummonExplosive_eff", "TM_SummonExplosive_eff_desc"),
                new MagicPowerSkill("TM_SummonExplosive_ver", "TM_SummonExplosive_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_SummonElemental =>
            magicPowerSkill_SummonElemental ?? (magicPowerSkill_SummonElemental = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SummonElemental_pwr", "TM_SummonElemental_pwr_desc"),
                new MagicPowerSkill("TM_SummonElemental_eff", "TM_SummonElemental_eff_desc"),
                new MagicPowerSkill("TM_SummonElemental_ver", "TM_SummonElemental_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_SummonPoppi =>
            magicPowerSkill_SummonPoppi ?? (magicPowerSkill_SummonPoppi = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SummonPoppi_pwr", "TM_SummonPoppi_pwr_desc"),
                new MagicPowerSkill("TM_SummonPoppi_eff", "TM_SummonPoppi_eff_desc"),
                new MagicPowerSkill("TM_SummonPoppi_ver", "TM_SummonPoppi_ver_desc")
            });

        public List<MagicPower> MagicPowersD =>
            magicPowerD ?? (magicPowerD = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_Poison),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_SootheAnimal,
                    TorannMagicDefOf.TM_SootheAnimal_I,
                    TorannMagicDefOf.TM_SootheAnimal_II,
                    TorannMagicDefOf.TM_SootheAnimal_III
                }),
                newPower(TorannMagicDefOf.TM_Regenerate),
                newPower(TorannMagicDefOf.TM_CureDisease),
                newPower(TorannMagicDefOf.TM_RegrowLimb, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Poison =>
            magicPowerSkill_Poison ?? (magicPowerSkill_Poison = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Poison_pwr", "TM_Poison_pwr_desc"),
                new MagicPowerSkill("TM_Poison_eff", "TM_Poison_eff_desc"),
                new MagicPowerSkill("TM_Poison_ver", "TM_Poison_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_SootheAnimal =>
            magicPowerSkill_SootheAnimal ?? (magicPowerSkill_SootheAnimal = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SootheAnimal_pwr", "TM_SootheAnimal_pwr_desc"),
                new MagicPowerSkill("TM_SootheAnimal_eff", "TM_SootheAnimal_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Regenerate =>
            magicPowerSkill_Regenerate ?? (magicPowerSkill_Regenerate = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Regenerate_pwr", "TM_Regenerate_pwr_desc"),
                new MagicPowerSkill("TM_Regenerate_eff", "TM_Regenerate_eff_desc"),
                new MagicPowerSkill("TM_Regenerate_ver", "TM_Regenerate_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_CureDisease =>
            magicPowerSkill_CureDisease ?? (magicPowerSkill_CureDisease = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_CureDisease_pwr", "TM_CureDisease_pwr_desc"),
                new MagicPowerSkill("TM_CureDisease_eff", "TM_CureDisease_eff_desc"),
                new MagicPowerSkill("TM_CureDisease_ver", "TM_CureDisease_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_RegrowLimb =>
            magicPowerSkill_RegrowLimb ?? (magicPowerSkill_RegrowLimb = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_RegrowLimb_eff", "TM_RegrowLimb_eff_desc")
            });

        public List<MagicPower> MagicPowersN =>
            magicPowerN ?? (magicPowerN = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_RaiseUndead),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_DeathMark,
                    TorannMagicDefOf.TM_DeathMark_I,
                    TorannMagicDefOf.TM_DeathMark_II,
                    TorannMagicDefOf.TM_DeathMark_III
                }),
                newPower(TorannMagicDefOf.TM_FogOfTorment),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_ConsumeCorpse,
                    TorannMagicDefOf.TM_ConsumeCorpse_I,
                    TorannMagicDefOf.TM_ConsumeCorpse_II,
                    TorannMagicDefOf.TM_ConsumeCorpse_III
                }),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_CorpseExplosion,
                    TorannMagicDefOf.TM_CorpseExplosion_I,
                    TorannMagicDefOf.TM_CorpseExplosion_II,
                    TorannMagicDefOf.TM_CorpseExplosion_III
                }),
                newPower(TorannMagicDefOf.TM_LichForm, true),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_DeathBolt,
                    TorannMagicDefOf.TM_DeathBolt_I,
                    TorannMagicDefOf.TM_DeathBolt_II,
                    TorannMagicDefOf.TM_DeathBolt_III
                }),
            });

        public List<MagicPowerSkill> MagicPowerSkill_RaiseUndead =>
            magicPowerSkill_RaiseUndead ?? (magicPowerSkill_RaiseUndead = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_RaiseUndead_pwr", "TM_RaiseUndead_pwr_desc"),
                new MagicPowerSkill("TM_RaiseUndead_eff", "TM_RaiseUndead_eff_desc"),
                new MagicPowerSkill("TM_RaiseUndead_ver", "TM_RaiseUndead_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_DeathMark =>
            magicPowerSkill_DeathMark ?? (magicPowerSkill_DeathMark = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_DeathMark_pwr", "TM_DeathMark_pwr_desc"),
                new MagicPowerSkill("TM_DeathMark_eff", "TM_DeathMark_eff_desc"),
                new MagicPowerSkill("TM_DeathMark_ver", "TM_DeathMark_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_FogOfTorment =>
            magicPowerSkill_FogOfTorment ?? (magicPowerSkill_FogOfTorment = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_FogOfTorment_pwr", "TM_FogOfTorment_pwr_desc"),
                new MagicPowerSkill("TM_FogOfTorment_eff", "TM_FogOfTorment_eff_desc"),
                new MagicPowerSkill("TM_FogOfTorment_ver", "TM_FogOfTorment_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_ConsumeCorpse =>
            magicPowerSkill_ConsumeCorpse ?? (magicPowerSkill_ConsumeCorpse = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ConsumeCorpse_eff", "TM_ConsumeCorpse_eff_desc"),
                new MagicPowerSkill("TM_ConsumeCorpse_ver", "TM_ConsumeCorpse_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_CorpseExplosion =>
            magicPowerSkill_CorpseExplosion ?? (magicPowerSkill_CorpseExplosion = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_CorpseExplosion_pwr", "TM_CorpseExplosion_pwr_desc"),
                new MagicPowerSkill("TM_CorpseExplosion_eff", "TM_CorpseExplosion_eff_desc"),
                new MagicPowerSkill("TM_CorpseExplosion_ver", "TM_CorpseExplosion_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_LichForm =>
            magicPowerSkill_LichForm ?? (magicPowerSkill_LichForm = new List<MagicPowerSkill>());

        public List<MagicPowerSkill> MagicPowerSkill_DeathBolt =>
            magicPowerSkill_DeathBolt ?? (magicPowerSkill_DeathBolt = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_DeathBolt_pwr", "TM_DeathBolt_pwr_desc"),
                new MagicPowerSkill("TM_DeathBolt_eff", "TM_DeathBolt_eff_desc"),
                new MagicPowerSkill("TM_DeathBolt_ver", "TM_DeathBolt_ver_desc")
            });

        public List<MagicPower> MagicPowersPR =>
            magicPowerPR ?? (magicPowerPR = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_AdvancedHeal),
                newPower(TorannMagicDefOf.TM_Purify),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_HealingCircle,
                    TorannMagicDefOf.TM_HealingCircle_I,
                    TorannMagicDefOf.TM_HealingCircle_II,
                    TorannMagicDefOf.TM_HealingCircle_III
                }),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_BestowMight,
                    TorannMagicDefOf.TM_BestowMight_I,
                    TorannMagicDefOf.TM_BestowMight_II,
                    TorannMagicDefOf.TM_BestowMight_III
                }),
                newPower(TorannMagicDefOf.TM_Resurrection, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_AdvancedHeal =>
            magicPowerSkill_AdvancedHeal ?? (magicPowerSkill_AdvancedHeal = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_AdvancedHeal_pwr", "TM_AdvancedHeal_pwr_desc"),
                new MagicPowerSkill("TM_AdvancedHeal_eff", "TM_AdvancedHeal_eff_desc"),
                new MagicPowerSkill("TM_AdvancedHeal_ver", "TM_AdvancedHeal_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Purify =>
            magicPowerSkill_Purify ?? (magicPowerSkill_Purify = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Purify_pwr", "TM_Purify_pwr_desc"),
                new MagicPowerSkill("TM_Purify_eff", "TM_Purify_eff_desc"),
                new MagicPowerSkill("TM_Purify_ver", "TM_Purify_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_HealingCircle =>
            magicPowerSkill_HealingCircle ?? (magicPowerSkill_HealingCircle = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_HealingCircle_pwr", "TM_HealingCircle_pwr_desc"),
                new MagicPowerSkill("TM_HealingCircle_eff", "TM_HealingCircle_eff_desc"),
                new MagicPowerSkill("TM_HealingCircle_ver", "TM_HealingCircle_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_BestowMight =>
            magicPowerSkill_BestowMight ?? (magicPowerSkill_BestowMight = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BestowMight_eff", "TM_BestowMight_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Resurrection =>
            magicPowerSkill_Resurrection ?? (magicPowerSkill_Resurrection = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Resurrection_eff", "TM_Resurrection_eff_desc"),
                new MagicPowerSkill("TM_Resurrection_ver", "TM_Resurrection_ver_desc")
            });

        public List<MagicPower> MagicPowersB =>
            magicPowerB ?? (magicPowerB = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_BardTraining),
                newPower(TorannMagicDefOf.TM_Entertain),
                newPower(TorannMagicDefOf.TM_Inspire),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Lullaby,
                    TorannMagicDefOf.TM_Lullaby_I,
                    TorannMagicDefOf.TM_Lullaby_II,
                    TorannMagicDefOf.TM_Lullaby_III
                }),
                newPower(TorannMagicDefOf.TM_BattleHymn, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_BardTraining =>
            magicPowerSkill_BardTraining ?? (magicPowerSkill_BardTraining = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BardTraining_pwr", "TM_BardTraining_pwr_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Entertain =>
            magicPowerSkill_Entertain ?? (magicPowerSkill_Entertain = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Entertain_pwr", "TM_Entertain_pwr_desc"),
                new MagicPowerSkill("TM_Entertain_ver", "TM_Entertain_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Inspire =>
            magicPowerSkill_Inspire ?? (magicPowerSkill_Inspire = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Inspire_pwr", "TM_Inspire_pwr_desc"),
                new MagicPowerSkill("TM_Inspire_ver", "TM_Inspire_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Lullaby =>
            magicPowerSkill_Lullaby ?? (magicPowerSkill_Lullaby = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Lullaby_pwr", "TM_Lullaby_pwr_desc"),
                new MagicPowerSkill("TM_Lullaby_eff", "TM_Lullaby_eff_desc"),
                new MagicPowerSkill("TM_Lullaby_ver", "TM_Lullaby_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_BattleHymn =>
            magicPowerSkill_BattleHymn ?? (magicPowerSkill_BattleHymn = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BattleHymn_pwr", "TM_BattleHymn_pwr_desc"),
                new MagicPowerSkill("TM_BattleHymn_eff", "TM_BattleHymn_eff_desc"),
                new MagicPowerSkill("TM_BattleHymn_ver", "TM_BattleHymn_ver_desc")
            });

        public List<MagicPower> MagicPowersWD =>
            magicPowerWD ?? (magicPowerWD = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_SoulBond),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_ShadowBolt,
                    TorannMagicDefOf.TM_ShadowBolt_I,
                    TorannMagicDefOf.TM_ShadowBolt_II,
                    TorannMagicDefOf.TM_ShadowBolt_III
                }),
                newPower(TorannMagicDefOf.TM_Dominate),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Repulsion,
                    TorannMagicDefOf.TM_Repulsion_I,
                    TorannMagicDefOf.TM_Repulsion_II,
                    TorannMagicDefOf.TM_Repulsion_III
                }),
                newPower(TorannMagicDefOf.TM_PsychicShock, true),
                //newPower(TorannMagicDefOf.TM_SummonDemon)
            });

        public List<MagicPowerSkill> MagicPowerSkill_SoulBond =>
            magicPowerSkill_SoulBond ?? (magicPowerSkill_SoulBond = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SoulBond_pwr", "TM_SoulBond_pwr_desc"),
                new MagicPowerSkill("TM_SoulBond_eff", "TM_SoulBond_eff_desc"),
                new MagicPowerSkill("TM_SoulBond_ver", "TM_SoulBond_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_ShadowBolt =>
            magicPowerSkill_ShadowBolt ?? (magicPowerSkill_ShadowBolt = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ShadowBolt_pwr", "TM_ShadowBolt_pwr_desc"),
                new MagicPowerSkill("TM_ShadowBolt_eff", "TM_ShadowBolt_eff_desc"),
                new MagicPowerSkill("TM_ShadowBolt_ver", "TM_ShadowBolt_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Dominate =>
            magicPowerSkill_Dominate ?? (magicPowerSkill_Dominate = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Dominate_pwr", "TM_Dominate_pwr_desc"),
                new MagicPowerSkill("TM_Dominate_eff", "TM_Dominate_eff_desc"),
                new MagicPowerSkill("TM_Dominate_ver", "TM_Dominate_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Repulsion =>
            magicPowerSkill_Repulsion ?? (magicPowerSkill_Repulsion = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Repulsion_pwr", "TM_Repulsion_pwr_desc"),
                new MagicPowerSkill("TM_Repulsion_eff", "TM_Repulsion_eff_desc"),
                new MagicPowerSkill("TM_Repulsion_ver", "TM_Repulsion_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_PsychicShock =>
            magicPowerSkill_PsychicShock ?? (magicPowerSkill_PsychicShock = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_PsychicShock_pwr", "TM_PsychicShock_pwr_desc"),
                new MagicPowerSkill("TM_PsychicShock_eff", "TM_PsychicShock_eff_desc"),
                new MagicPowerSkill("TM_PsychicShock_ver", "TM_PsychicShock_ver_desc")
            });

        //public List<MagicPowerSkill> MagicPowerSkill_SummonDemon =>
        //    magicPowerSkill_SummonDemon ?? (magicPowerSkill_SummonDemon = new List<MagicPowerSkill>
        //    {
        //        new MagicPowerSkill("TM_SummonDemon_pwr", "TM_SummonDemon_pwr_desc"),
        //        new MagicPowerSkill("TM_SummonDemon_eff", "TM_SummonDemon_eff_desc"),
        //        new MagicPowerSkill("TM_SummonDemon_ver", "TM_SummonDemon_ver_desc")
        //    };

        public List<MagicPower> MagicPowersSD =>
            magicPowerSD ?? (magicPowerSD = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_SoulBond),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_ShadowBolt,
                    TorannMagicDefOf.TM_ShadowBolt_I,
                    TorannMagicDefOf.TM_ShadowBolt_II,
                    TorannMagicDefOf.TM_ShadowBolt_III
                }),
                newPower(TorannMagicDefOf.TM_Dominate),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Attraction,
                    TorannMagicDefOf.TM_Attraction_I,
                    TorannMagicDefOf.TM_Attraction_II,
                    TorannMagicDefOf.TM_Attraction_III
                }),
                newPower(TorannMagicDefOf.TM_Scorn, true),
                //newPower(TorannMagicDefOf.TM_SummonDemon),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Attraction =>
            magicPowerSkill_Attraction ?? (magicPowerSkill_Attraction = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Attraction_pwr", "TM_Attraction_pwr_desc"),
                new MagicPowerSkill("TM_Attraction_eff", "TM_Attraction_eff_desc"),
                new MagicPowerSkill("TM_Attraction_ver", "TM_Attraction_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Scorn =>
            magicPowerSkill_Scorn ?? (magicPowerSkill_Scorn = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Scorn_pwr", "TM_Scorn_pwr_desc"),
                new MagicPowerSkill("TM_Scorn_eff", "TM_Scorn_eff_desc"),
                new MagicPowerSkill("TM_Scorn_ver", "TM_Scorn_ver_desc")
            });

        public List<MagicPower> MagicPowersG =>
            magicPowerG ?? (magicPowerG = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_Stoneskin),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Encase,
                    TorannMagicDefOf.TM_Encase_I,
                    TorannMagicDefOf.TM_Encase_II,
                    TorannMagicDefOf.TM_Encase_III
                }),
                newPower(TorannMagicDefOf.TM_EarthSprites),
                newPower(TorannMagicDefOf.TM_EarthernHammer),
                newPower(TorannMagicDefOf.TM_Sentinel),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Meteor,
                    TorannMagicDefOf.TM_Meteor_I,
                    TorannMagicDefOf.TM_Meteor_II,
                    TorannMagicDefOf.TM_Meteor_III
                }, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Stoneskin =>
            magicPowerSkill_Stoneskin ?? (magicPowerSkill_Stoneskin = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Stoneskin_pwr", "TM_Stoneskin_pwr_desc"),
                new MagicPowerSkill("TM_Stoneskin_eff", "TM_Stoneskin_eff_desc"),
                new MagicPowerSkill("TM_Stoneskin_ver", "TM_Stoneskin_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Encase =>
            magicPowerSkill_Encase ?? (magicPowerSkill_Encase = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Encase_pwr", "TM_Encase_pwr_desc"),
                new MagicPowerSkill("TM_Encase_eff", "TM_Encase_eff_desc"),
                new MagicPowerSkill("TM_Encase_ver", "TM_Encase_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_EarthSprites =>
            magicPowerSkill_EarthSprites ?? (magicPowerSkill_EarthSprites = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_EarthSprites_pwr", "TM_EarthSprites_pwr_desc"),
                new MagicPowerSkill("TM_EarthSprites_eff", "TM_EarthSprites_eff_desc"),
                new MagicPowerSkill("TM_EarthSprites_ver", "TM_EarthSprites_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_EarthernHammer =>
            magicPowerSkill_EarthernHammer ?? (magicPowerSkill_EarthernHammer = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_EarthernHammer_pwr", "TM_EarthernHammer_pwr_desc"),
                new MagicPowerSkill("TM_EarthernHammer_eff", "TM_EarthernHammer_eff_desc"),
                new MagicPowerSkill("TM_EarthernHammer_ver", "TM_EarthernHammer_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Sentinel =>
            magicPowerSkill_Sentinel ?? (magicPowerSkill_Sentinel = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Sentinel_pwr", "TM_Sentinel_pwr_desc"),
                new MagicPowerSkill("TM_Sentinel_eff", "TM_Sentinel_eff_desc"),
                new MagicPowerSkill("TM_Sentinel_ver", "TM_Sentinel_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Meteor =>
            magicPowerSkill_Meteor ?? (magicPowerSkill_Meteor = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Meteor_ver", "TM_Meteor_ver_desc"),
                new MagicPowerSkill("TM_Meteor_eff", "TM_Meteor_eff_desc")
            });

        public List<MagicPower> MagicPowersT =>
            magicPowerT ?? (magicPowerT = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_TechnoBit),
                newPower(TorannMagicDefOf.TM_TechnoTurret),
                newPower(TorannMagicDefOf.TM_TechnoWeapon),
                newPower(TorannMagicDefOf.TM_TechnoShield, true),
                newPower(TorannMagicDefOf.TM_Overdrive, true),
                newPower(TorannMagicDefOf.TM_Sabotage, true),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_OrbitalStrike,
                    TorannMagicDefOf.TM_OrbitalStrike_I,
                    TorannMagicDefOf.TM_OrbitalStrike_II,
                    TorannMagicDefOf.TM_OrbitalStrike_III
                }, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_TechnoBit =>
            magicPowerSkill_TechnoBit ?? (magicPowerSkill_TechnoBit = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_TechnoBit_pwr", "TM_TechnoBit_pwr_desc"),
                new MagicPowerSkill("TM_TechnoBit_eff", "TM_TechnoBit_eff_desc"),
                new MagicPowerSkill("TM_TechnoBit_ver", "TM_TechnoBit_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_TechnoTurret =>
            magicPowerSkill_TechnoTurret ?? (magicPowerSkill_TechnoTurret = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_TechnoTurret_pwr", "TM_TechnoTurret_pwr_desc"),
                new MagicPowerSkill("TM_TechnoTurret_eff", "TM_TechnoTurret_eff_desc"),
                new MagicPowerSkill("TM_TechnoTurret_ver", "TM_TechnoTurret_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_TechnoWeapon =>
            magicPowerSkill_TechnoWeapon ?? (magicPowerSkill_TechnoWeapon = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_TechnoWeapon_pwr", "TM_TechnoWeapon_pwr_desc"),
                new MagicPowerSkill("TM_TechnoWeapon_eff", "TM_TechnoWeapon_eff_desc"),
                new MagicPowerSkill("TM_TechnoWeapon_ver", "TM_TechnoWeapon_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_TechnoShield =>
            magicPowerSkill_TechnoShield ?? (magicPowerSkill_TechnoShield = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_TechnoShield_pwr", "TM_TechnoShield_pwr_desc"),
                new MagicPowerSkill("TM_TechnoShield_eff", "TM_TechnoShield_eff_desc"),
                new MagicPowerSkill("TM_TechnoShield_ver", "TM_TechnoShield_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Sabotage =>
            magicPowerSkill_Sabotage ?? (magicPowerSkill_Sabotage = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Sabotage_pwr", "TM_Sabotage_pwr_desc"),
                new MagicPowerSkill("TM_Sabotage_eff", "TM_Sabotage_eff_desc"),
                new MagicPowerSkill("TM_Sabotage_ver", "TM_Sabotage_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Overdrive =>
            magicPowerSkill_Overdrive ?? (magicPowerSkill_Overdrive = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Overdrive_pwr", "TM_Overdrive_pwr_desc"),
                new MagicPowerSkill("TM_Overdrive_eff", "TM_Overdrive_eff_desc"),
                new MagicPowerSkill("TM_Overdrive_ver", "TM_Overdrive_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_OrbitalStrike =>
            magicPowerSkill_OrbitalStrike ?? (magicPowerSkill_OrbitalStrike = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_OrbitalStrike_pwr", "TM_OrbitalStrike_pwr_desc"),
                new MagicPowerSkill("TM_OrbitalStrike_eff", "TM_OrbitalStrike_eff_desc"),
                new MagicPowerSkill("TM_OrbitalStrike_ver", "TM_OrbitalStrike_ver_desc")
            });

        public List<MagicPower> MagicPowersBM =>
            magicPowerBM ?? (magicPowerBM = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_BloodGift),
                newPower(TorannMagicDefOf.TM_IgniteBlood),
                newPower(TorannMagicDefOf.TM_BloodForBlood),
                newPower(TorannMagicDefOf.TM_BloodShield),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Rend,
                    TorannMagicDefOf.TM_Rend_I,
                    TorannMagicDefOf.TM_Rend_II,
                    TorannMagicDefOf.TM_Rend_III
                }),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_BloodMoon,
                    TorannMagicDefOf.TM_BloodMoon_I,
                    TorannMagicDefOf.TM_BloodMoon_II,
                    TorannMagicDefOf.TM_BloodMoon_III
                }, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_BloodGift =>
            magicPowerSkill_BloodGift ?? (magicPowerSkill_BloodGift = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BloodGift_pwr", "TM_BloodGift_pwr_desc"),
                new MagicPowerSkill("TM_BloodGift_eff", "TM_BloodGift_eff_desc"),
                new MagicPowerSkill("TM_BloodGift_ver", "TM_BloodGift_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_IgniteBlood =>
            magicPowerSkill_IgniteBlood ?? (magicPowerSkill_IgniteBlood = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_IgniteBlood_pwr", "TM_IgniteBlood_pwr_desc"),
                new MagicPowerSkill("TM_IgniteBlood_eff", "TM_IgniteBlood_eff_desc"),
                new MagicPowerSkill("TM_IgniteBlood_ver", "TM_IgniteBlood_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_BloodForBlood =>
            magicPowerSkill_BloodForBlood ?? (magicPowerSkill_BloodForBlood = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BloodForBlood_pwr", "TM_BloodForBlood_pwr_desc"),
                new MagicPowerSkill("TM_BloodForBlood_eff", "TM_BloodForBlood_eff_desc"),
                new MagicPowerSkill("TM_BloodForBlood_ver", "TM_BloodForBlood_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_BloodShield =>
            magicPowerSkill_BloodShield ?? (magicPowerSkill_BloodShield = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BloodShield_pwr", "TM_BloodShield_pwr_desc"),
                new MagicPowerSkill("TM_BloodShield_eff", "TM_BloodShield_eff_desc"),
                new MagicPowerSkill("TM_BloodShield_ver", "TM_BloodShield_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Rend =>
            magicPowerSkill_Rend ?? (magicPowerSkill_Rend = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Rend_pwr", "TM_Rend_pwr_desc"),
                new MagicPowerSkill("TM_Rend_eff", "TM_Rend_eff_desc"),
                new MagicPowerSkill("TM_Rend_ver", "TM_Rend_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_BloodMoon =>
            magicPowerSkill_BloodMoon ?? (magicPowerSkill_BloodMoon = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_BloodMoon_pwr", "TM_BloodMoon_pwr_desc"),
                new MagicPowerSkill("TM_BloodMoon_eff", "TM_BloodMoon_eff_desc"),
                new MagicPowerSkill("TM_BloodMoon_ver", "TM_BloodMoon_ver_desc")
            });

        public List<MagicPower> MagicPowersE =>
            magicPowerE ?? (magicPowerE = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_EnchantedBody),
                newPower(TorannMagicDefOf.TM_Transmutate),
                newPower(TorannMagicDefOf.TM_EnchanterStone),
                newPower(TorannMagicDefOf.TM_EnchantWeapon),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Polymorph,
                    TorannMagicDefOf.TM_Polymorph_I,
                    TorannMagicDefOf.TM_Polymorph_II,
                    TorannMagicDefOf.TM_Polymorph_III
                }),
                newPower(TorannMagicDefOf.TM_Shapeshift, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_EnchantedBody =>
            magicPowerSkill_EnchantedBody ?? (magicPowerSkill_EnchantedBody = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_EnchantedBody_pwr", "TM_EnchantedBody_pwr_desc"),
                new MagicPowerSkill("TM_EnchantedBody_eff", "TM_EnchantedBody_eff_desc"),
                new MagicPowerSkill("TM_EnchantedBody_ver", "TM_EnchantedBody_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Transmutate =>
            magicPowerSkill_Transmutate ?? (magicPowerSkill_Transmutate = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Transmutate_pwr", "TM_Transmutate_pwr_desc"),
                new MagicPowerSkill("TM_Transmutate_eff", "TM_Transmutate_eff_desc"),
                new MagicPowerSkill("TM_Transmutate_ver", "TM_Transmutate_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_EnchanterStone =>
            magicPowerSkill_EnchanterStone ?? (magicPowerSkill_EnchanterStone = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_EnchanterStone_eff", "TM_EnchanterStone_eff_desc"),
                new MagicPowerSkill("TM_EnchanterStone_ver", "TM_EnchanterStone_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_EnchantWeapon =>
            magicPowerSkill_EnchantWeapon ?? (magicPowerSkill_EnchantWeapon = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_EnchantWeapon_pwr", "TM_EnchantWeapon_pwr_desc"),
                new MagicPowerSkill("TM_EnchantWeapon_eff", "TM_EnchantWeapon_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Polymorph =>
            magicPowerSkill_Polymorph ?? (magicPowerSkill_Polymorph = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Polymorph_pwr", "TM_Polymorph_pwr_desc"),
                new MagicPowerSkill("TM_Polymorph_eff", "TM_Polymorph_eff_desc"),
                new MagicPowerSkill("TM_Polymorph_ver", "TM_Polymorph_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_Shapeshift =>
            magicPowerSkill_Shapeshift ?? (magicPowerSkill_Shapeshift = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Shapeshift_pwr", "TM_Shapeshift_pwr_desc"),
                new MagicPowerSkill("TM_Shapeshift_eff", "TM_Shapeshift_eff_desc"),
                new MagicPowerSkill("TM_Shapeshift_ver", "TM_Shapeshift_ver_desc")
            });

        public List<MagicPower> MagicPowersC =>
            magicPowerC ?? (magicPowerC = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_Prediction),
                newPower(TorannMagicDefOf.TM_AlterFate),
                newPower(TorannMagicDefOf.TM_AccelerateTime),
                newPower(TorannMagicDefOf.TM_ReverseTime),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_ChronostaticField,
                    TorannMagicDefOf.TM_ChronostaticField_I,
                    TorannMagicDefOf.TM_ChronostaticField_II,
                    TorannMagicDefOf.TM_ChronostaticField_III
                }),
                newPower(TorannMagicDefOf.TM_Recall, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Prediction =>
            magicPowerSkill_Prediction ?? (magicPowerSkill_Prediction = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Prediction_pwr", "TM_Prediction_pwr_desc"),
                new MagicPowerSkill("TM_Prediction_eff", "TM_Prediction_eff_desc"),
                new MagicPowerSkill("TM_Prediction_ver", "TM_Prediction_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_AlterFate =>
            magicPowerSkill_AlterFate ?? (magicPowerSkill_AlterFate = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_AlterFate_pwr", "TM_AlterFate_pwr_desc"),
                new MagicPowerSkill("TM_AlterFate_eff", "TM_AlterFate_eff_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_AccelerateTime =>
            magicPowerSkill_AccelerateTime ?? (magicPowerSkill_AccelerateTime = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_AccelerateTime_pwr", "TM_AccelerateTime_pwr_desc"),
                new MagicPowerSkill("TM_AccelerateTime_eff", "TM_AccelerateTime_eff_desc"),
                new MagicPowerSkill("TM_AccelerateTime_ver", "TM_AccelerateTime_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_ReverseTime =>
            magicPowerSkill_ReverseTime ?? (magicPowerSkill_ReverseTime = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ReverseTime_pwr", "TM_ReverseTime_pwr_desc"),
                new MagicPowerSkill("TM_ReverseTime_eff", "TM_ReverseTime_eff_desc"),
                new MagicPowerSkill("TM_ReverseTime_ver", "TM_ReverseTime_ver_desc")
            });

        public List<MagicPowerSkill> MagicPowerSkill_ChronostaticField =>
            magicPowerSkill_ChronostaticField ?? (magicPowerSkill_ChronostaticField =
                new List<MagicPowerSkill>
                {
                    new MagicPowerSkill("TM_ChronostaticField_pwr", "TM_ChronostaticField_pwr_desc"),
                    new MagicPowerSkill("TM_ChronostaticField_eff", "TM_ChronostaticField_eff_desc"),
                    new MagicPowerSkill("TM_ChronostaticField_ver", "TM_ChronostaticField_ver_desc")
                });

        public List<MagicPowerSkill> MagicPowerSkill_Recall =>
            magicPowerSkill_Recall ?? (magicPowerSkill_Recall = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Recall_pwr", "TM_Recall_pwr_desc"),
                new MagicPowerSkill("TM_Recall_eff", "TM_Recall_eff_desc"),
                new MagicPowerSkill("TM_Recall_ver", "TM_Recall_ver_desc")
            });

        public List<MagicPower> MagicPowersCM =>
            magicPowerCM ?? (magicPowerCM = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_ChaosTradition),
            });

        public List<MagicPowerSkill> MagicPowerSkill_ChaosTradition =>
            magicPowerSkill_ChaosTradition ?? (magicPowerSkill_ChaosTradition = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ChaosTradition_pwr", "TM_ChaosTradition_pwr_desc"),
                new MagicPowerSkill("TM_ChaosTradition_eff", "TM_ChaosTradition_eff_desc"),
                new MagicPowerSkill("TM_ChaosTradition_ver", "TM_ChaosTradition_ver_desc")
            });

        public List<MagicPower> MagicPowersShadow =>
            magicPowerShadow ?? (magicPowerShadow = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_ShadowWalk),
            });

        public List<MagicPowerSkill> MagicPowerSkill_ShadowWalk =>
            magicPowerSkill_ShadowWalk ?? (magicPowerSkill_ShadowWalk = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ShadowWalk_pwr",
                    "TM_ShadowWalk_pwr_desc"), // applies invisibility and duration of invisibility
                new MagicPowerSkill("TM_ShadowWalk_eff", "TM_ShadowWalk_eff_desc"), // reduces mana cost
                new MagicPowerSkill("TM_ShadowWalk_ver", "TM_ShadowWalk_ver_desc") // heals and can invis target
            });

        public List<MagicPower> MagicPowersBrightmage =>
            magicPowerBrightmage ?? (magicPowerBrightmage = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_LightLance),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Sunfire,
                    TorannMagicDefOf.TM_Sunfire_I,
                    TorannMagicDefOf.TM_Sunfire_II,
                    TorannMagicDefOf.TM_Sunfire_III
                }),
                newPower(TorannMagicDefOf.TM_LightBurst),
                newPower(TorannMagicDefOf.TM_LightSkip),
                newPower(TorannMagicDefOf.TM_Refraction),
                newPower(TorannMagicDefOf.TM_SpiritOfLight, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_LightLance =>
            magicPowerSkill_LightLance ?? (magicPowerSkill_LightLance = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LightLance_pwr", "TM_LightLance_pwr_desc"), //damage
                new MagicPowerSkill("TM_LightLance_eff", "TM_LightLance_eff_desc"), //mana cost
                new MagicPowerSkill("TM_LightLance_ver", "TM_LightLance_ver_desc") //beam width and duration
            });

        public List<MagicPowerSkill> MagicPowerSkill_Sunfire =>
            magicPowerSkill_Sunfire ?? (magicPowerSkill_Sunfire = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Sunfire_pwr", "TM_Sunfire_pwr_desc"), //damage
                new MagicPowerSkill("TM_Sunfire_eff", "TM_Sunfire_eff_desc"), //mana cost
                new MagicPowerSkill("TM_Sunfire_ver", "TM_Sunfire_ver_desc") //lance count
            });

        public List<MagicPowerSkill> MagicPowerSkill_LightBurst =>
            magicPowerSkill_LightBurst ?? (magicPowerSkill_LightBurst = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LightBurst_pwr", "TM_LightBurst_pwr_desc"), //hediff severity
                new MagicPowerSkill("TM_LightBurst_eff", "TM_LightBurst_eff_desc"), //mana cost
                new MagicPowerSkill("TM_LightBurst_ver", "TM_LightBurst_ver_desc") //effects mechanoids, redirects bullets to another target or "spray"
            });

        public List<MagicPowerSkill> MagicPowerSkill_LightSkip =>
            magicPowerSkill_LightSkip ?? (magicPowerSkill_LightSkip = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LightSkip_pwr", "TM_LightSkip_pwr_desc"), //3 tiers, self->aoe->global (2pts per)
                new MagicPowerSkill("TM_LightSkip_eff", "TM_LightSkip_eff_desc") //mana cost, light requirement
            });

        public List<MagicPowerSkill> MagicPowerSkill_Refraction =>
            magicPowerSkill_Refraction ?? (magicPowerSkill_Refraction = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Refraction_pwr", "TM_Refraction_pwr_desc"), //+friendly accuracy; -hostile accuracy
                new MagicPowerSkill("TM_Refraction_eff", "TM_Refraction_eff_desc"), //mana cost
                new MagicPowerSkill("TM_Refraction_ver", "TM_Refraction_ver_desc") //duration
            });

        public List<MagicPowerSkill> MagicPowerSkill_SpiritOfLight =>
            magicPowerSkill_SpiritOfLight ?? (magicPowerSkill_SpiritOfLight = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SpiritOfLight_pwr", "TM_SpiritOfLight_pwr_desc"), //increases damage of abilities, reduces delay between abilities
                new MagicPowerSkill("TM_SpiritOfLight_eff", "TM_SpiritOfLight_eff_desc"), //decreased upkeep cost
                new MagicPowerSkill("TM_SpiritOfLight_ver", "TM_SpiritOfLight_ver_desc") //increased light energy gain and increased max capacity
            });

        public List<MagicPower> MagicPowersShaman =>
            magicPowerShaman ?? (magicPowerShaman = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_Totems),
                newPower(TorannMagicDefOf.TM_ChainLightning),
                newPower(TorannMagicDefOf.TM_Enrage),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_Hex,
                    TorannMagicDefOf.TM_Hex_I,
                    TorannMagicDefOf.TM_Hex_II,
                    TorannMagicDefOf.TM_Hex_III
                }),
                new MagicPower(new List<AbilityDef>
                {
                    TorannMagicDefOf.TM_SpiritWolves,
                    TorannMagicDefOf.TM_SpiritWolves_I,
                    TorannMagicDefOf.TM_SpiritWolves_II,
                    TorannMagicDefOf.TM_SpiritWolves_III
                }),
                newPower(TorannMagicDefOf.TM_GuardianSpirit, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Totems =>
            magicPowerSkill_Totems ?? (magicPowerSkill_Totems = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Totems_pwr", "TM_Totems_pwr_desc"), // power of totems
                new MagicPowerSkill("TM_Totems_eff", "TM_Totems_eff_desc"), // mana cost
                new MagicPowerSkill("TM_Totems_ver", "TM_Totems_ver_desc") // duration
            });

        public List<MagicPowerSkill> MagicPowerSkill_ChainLightning =>
            magicPowerSkill_ChainLightning ?? (magicPowerSkill_ChainLightning = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_ChainLightning_pwr", "TM_ChainLightning_pwr_desc"), // damage
                new MagicPowerSkill("TM_ChainLightning_eff", "TM_ChainLightning_eff_desc"), // reduces mana cost
                new MagicPowerSkill("TM_ChainLightning_ver", "TM_ChainLightning_ver_desc") // number of forks, fork count
            });

        public List<MagicPowerSkill> MagicPowerSkill_Enrage =>
            magicPowerSkill_Enrage ?? (magicPowerSkill_Enrage = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Enrage_pwr", "TM_Enrage_pwr_desc"), // severity
                new MagicPowerSkill("TM_Enrage_eff", "TM_Enrage_eff_desc"), // reduces mana cost
                new MagicPowerSkill("TM_Enrage_ver", "TM_Enrage_ver_desc") // application count
            });

        public List<MagicPowerSkill> MagicPowerSkill_Hex =>
            magicPowerSkill_Hex ?? (magicPowerSkill_Hex = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Hex_pwr", "TM_Hex_pwr_desc"), // severity, what other effects are available after hexing
                new MagicPowerSkill("TM_Hex_eff", "TM_Hex_eff_desc"), // reduces mana cost
                new MagicPowerSkill("TM_Hex_ver", "TM_Hex_ver_desc") // appication count, other stuff related to hex actions
            });

        public List<MagicPowerSkill> MagicPowerSkill_SpiritWolves =>
            magicPowerSkill_SpiritWolves ?? (magicPowerSkill_SpiritWolves = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SpiritWolves_pwr", "TM_SpiritWolves_pwr_desc"), // damage
                new MagicPowerSkill("TM_SpiritWolves_eff", "TM_SpiritWolves_eff_desc"), // reduces mana cost
                new MagicPowerSkill("TM_SpiritWolves_ver", "TM_SpiritWolves_ver_desc") // width
            });

        public List<MagicPowerSkill> MagicPowerSkill_GuardianSpirit =>
            magicPowerSkill_GuardianSpirit ?? (magicPowerSkill_GuardianSpirit = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_GuardianSpirit_pwr", "TM_GuardianSpirit_pwr_desc"), // power of GuardianSpirit
                new MagicPowerSkill("TM_GuardianSpirit_eff", "TM_GuardianSpirit_eff_desc"), // mana cost
                new MagicPowerSkill("TM_GuardianSpirit_ver", "TM_GuardianSpirit_ver_desc") // effect radius, effects
            });

        public List<MagicPower> MagicPowersGolemancer =>
            magicPowerGolemancer ?? (magicPowerGolemancer = new List<MagicPower>
            {
                newPower(TorannMagicDefOf.TM_Golemancy),
                newPower(TorannMagicDefOf.TM_RuneCarving),
                newPower(TorannMagicDefOf.TM_Branding),
                newPower(TorannMagicDefOf.TM_SigilSurge),
                newPower(TorannMagicDefOf.TM_SigilDrain),
                newPower(TorannMagicDefOf.TM_LivingWall, true),
            });

        public List<MagicPowerSkill> MagicPowerSkill_Golemancy =>
            magicPowerSkill_Golemancy ?? (magicPowerSkill_Golemancy = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Golemancy_pwr", "TM_Golemancy_pwr_desc"), // power of golems, 15 ranks
                new MagicPowerSkill("TM_Golemancy_eff", "TM_Golemancy_eff_desc"), // mana cost to upkeep a golem, 15 ranks
                new MagicPowerSkill("TM_Golemancy_ver", "TM_Golemancy_ver_desc") // abilities and skills available to a golem, 15 ranks
            });

        public List<MagicPowerSkill> MagicPowerSkill_RuneCarving =>
            magicPowerSkill_RuneCarving ?? (magicPowerSkill_RuneCarving = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_RuneCarving_pwr", "TM_RuneCarving_pwr_desc"), // efficiency boost to parts, 3 ranks, 2 pt level cost
                new MagicPowerSkill("TM_RuneCarving_eff", "TM_RuneCarving_eff_desc"), // returns 10% mana per skill level
                new MagicPowerSkill("TM_RuneCarving_ver", "TM_RuneCarving_ver_desc") // increases chance of success by 5%
            });

        public List<MagicPowerSkill> MagicPowerSkill_Branding =>
            magicPowerSkill_Branding ?? (magicPowerSkill_Branding = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_Branding_pwr", "TM_Branding_pwr_desc"), // severity, 5 ranks
                new MagicPowerSkill("TM_Branding_eff", "TM_Branding_eff_desc") // reduces upkeep cost, 5 ranks
                //new MagicPowerSkill("TM_Branding_ver", "TM_Branding_ver_desc")  //
            });

        public List<MagicPowerSkill> MagicPowerSkill_SigilSurge =>
            magicPowerSkill_SigilSurge ?? (magicPowerSkill_SigilSurge = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SigilSurge_pwr", "TM_SigilSurge_pwr_desc"), // severity boost when active
                new MagicPowerSkill("TM_SigilSurge_eff", "TM_SigilSurge_eff_desc"), // reduces mana upkeep while active
                new MagicPowerSkill("TM_SigilSurge_ver", "TM_SigilSurge_ver_desc") // reduces 'feedback' effects on golemancer (pain)
            });

        public List<MagicPowerSkill> MagicPowerSkill_SigilDrain =>
            magicPowerSkill_SigilDrain ?? (magicPowerSkill_SigilDrain = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_SigilDrain_pwr", "TM_SigilDrain_pwr_desc"), // bonus of drain effects on golemancer
                new MagicPowerSkill("TM_SigilDrain_eff", "TM_SigilDrain_eff_desc"), // reduces mana upkeep cost
                new MagicPowerSkill("TM_SigilDrain_ver", "TM_SigilDrain_ver_desc") // reduces feedback effects on other pawn (pain)
            });

        public List<MagicPowerSkill> MagicPowerSkill_LivingWall =>
            magicPowerSkill_LivingWall ?? (magicPowerSkill_LivingWall = new List<MagicPowerSkill>
            {
                new MagicPowerSkill("TM_LivingWall_pwr", "TM_LivingWall_pwr_desc"), // power of living walls abilities
                new MagicPowerSkill("TM_LivingWall_eff", "TM_LivingWall_eff_desc"), // mana upkeep cost
                new MagicPowerSkill("TM_LivingWall_ver", "TM_LivingWall_ver_desc") // movement and action quickness
            });

        public bool IsNecromancer
        {
            get => isNecromancer || TM_Calc.IsNecromancer(MagicPawn);
            set => isNecromancer = value;
        }

        public int DominationCount
        {
            get => dominationCount;
            set => dominationCount = value;
        }

        public bool Initialized
        {
            get => initialized;
            set => initialized = value;
        }

        public int MagicUserLevel
        {
            get => magicUserLevel;
            set => magicUserLevel = value;
        }

        public int MagicUserXP
        {
            get => magicUserXP;
            set => magicUserXP = value;
        }

        public int MagicAbilityPoints
        {
            get => magicAbilityPoints;
            set => magicAbilityPoints = value;
        }

        public int TicksToLearnMagicXP
        {
            get => ticksToLearnMagicXP;
            set => ticksToLearnMagicXP = value;
        }

        public int TicksAffiliation
        {
            get => ticksAffiliation;
            set => ticksAffiliation = value;
        }

        public Pawn MagicPawn => magicPawn;

        public Faction Affiliation
        {
            get => affiliation;
            set => affiliation = value;
        }

        public void ClearSkill_Dictionaries()
        {
            skillPower.Clear();
            skillVersatility.Clear();
            skillEfficiency.Clear();
        }

        private Dictionary<TMAbilityDef, MagicPowerSkill> skillEfficiency = new Dictionary<TMAbilityDef, MagicPowerSkill>();
        public MagicPowerSkill GetSkill_Efficiency(TMAbilityDef ability)
        {
            if (skillEfficiency.ContainsKey(ability)) return skillEfficiency[ability];

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
            return skillEfficiency[ability];
        }

        private Dictionary<TMAbilityDef, MagicPowerSkill> skillVersatility = new Dictionary<TMAbilityDef, MagicPowerSkill>();
        public MagicPowerSkill GetSkill_Versatility(TMAbilityDef ability)
        {
            if (skillVersatility.ContainsKey(ability)) return skillVersatility[ability];

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
                        if (ability.defName != customPowers[i].customPower.abilityDefs[j].ToString()) continue;

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
            if (!hasSkill)
            {
                skillVersatility.Add(ability, null);
            }
            return skillVersatility[ability];
        }

        private Dictionary<TMAbilityDef, MagicPowerSkill> skillPower = new Dictionary<TMAbilityDef, MagicPowerSkill>();        
        public MagicPowerSkill GetSkill_Power(TMAbilityDef ability)
        {
            if (skillPower.ContainsKey(ability)) return skillPower[ability];

            bool hasSkill = false;
            string s = ability.defName;
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
                        if (ability.defName != customPowers[i].customPower.abilityDefs[j].ToString()) continue;

                        for (int k = 0; k < AllMagicPowerSkills.Count; k++)
                        {
                            MagicPowerSkill mps = AllMagicPowerSkills[k];
                            foreach (TM_CustomSkill cs in customPowers[i].customPower.skills)
                            {
                                if (cs.label.EndsWith("_pwr") && cs.label == mps.label)
                                {
                                    skillPower.Add(ability, mps);
                                    hasSkill = true;
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
                    if (AllMagicPowers[i].abilityDef is TMAbilityDef ability)
                    {
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

        private int uniquePowersCount;
        public int GetUniquePowersWithSkillsCount(List<TM_CustomClass> customClassList)
        {
            List<TMAbilityDef> abilities = new List<TMAbilityDef>();
            foreach (TM_CustomClass customClass in customClassList)
            {
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
            }
            uniquePowersCount = abilities.Count;
            return uniquePowersCount;
        }

        public MagicPower ReturnMatchingMagicPower(TMAbilityDef def)
        {
            return AllMagicPowers
                .Where(t => t.TMabilityDefs.Contains(def))
                .FirstOrDefault(t =>
                    t != MagicPowersWD.FirstOrDefault(mp =>  mp.abilityDef == TorannMagicDefOf.TM_SoulBond)
                    && t != MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt)
                    && t != MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate)
                );
        }

        public IEnumerable<MagicPower> Powers
        {
            get
            {
                List<MagicPower> powers = new List<MagicPower>();
                powers.AddRange(MagicPowersCM);
                powers.AddRange(MagicPowersW);
                powers.AddRange(MagicPowersC);
                powers.AddRange(MagicPowersE);
                powers.AddRange(MagicPowersBM);
                powers.AddRange(MagicPowersIF);
                powers.AddRange(MagicPowersHoF);
                powers.AddRange(MagicPowersSB);
                powers.AddRange(MagicPowersA);
                powers.AddRange(MagicPowersP);
                powers.AddRange(MagicPowersS);
                powers.AddRange(MagicPowersD);
                powers.AddRange(MagicPowersN);
                powers.AddRange(MagicPowersPR);
                powers.AddRange(MagicPowersB);
                powers.AddRange(MagicPowersWD);
                powers.AddRange(MagicPowersSD);
                powers.AddRange(MagicPowersG);
                powers.AddRange(MagicPowersT);
                powers.AddRange(MagicPowersStandalone);
                return powers;
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
                    allMagicPowersForChaosMageList = new List<MagicPower>();
                    allMagicPowersForChaosMageList.AddRange(MagicPowersW);
                    tmpList.Add(MagicPowersIF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Firestorm));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersIF.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Rainmaker));
                    tmpList.Add(MagicPowersHoF.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Blizzard));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersHoF.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(MagicPowersSB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersSB.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(MagicPowersA.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_FoldReality));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersA.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(MagicPowersP.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_HolyWrath));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersP.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(MagicPowersS.Except(MagicPowersS.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SummonPoppi)));
                    tmpList.Add(MagicPowersD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RegrowLimb));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersD.Except(tmpList));
                    tmpList.Clear();
                    tmpList.Add(MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_RaiseUndead));
                    tmpList.Add(MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_LichForm));
                    tmpList.Add(MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_DeathBolt));
                    tmpList.Add(MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_DeathBolt_I));
                    tmpList.Add(MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_DeathBolt_II));
                    tmpList.Add(MagicPowersN.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_DeathBolt_III));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersN.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(MagicPowersPR.Except(MagicPowersPR.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Resurrection)));
                    tmpList.Add(MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BardTraining));
                    tmpList.Add(MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Inspire));
                    tmpList.Add(MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Entertain));
                    tmpList.Add(MagicPowersB.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_BattleHymn));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersB.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(MagicPowersWD.Except(MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_PsychicShock)));
                    tmpList.Add(MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SoulBond));
                    tmpList.Add(MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowBolt));
                    tmpList.Add(MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Dominate));
                    tmpList.Add(MagicPowersSD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Scorn));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersSD.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(MagicPowersG.Except(MagicPowersG.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Meteor)));
                    tmpList.Add(MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike));
                    tmpList.Add(MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_I));
                    tmpList.Add(MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_II));
                    tmpList.Add(MagicPowersT.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_OrbitalStrike_III));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersT.Except(tmpList));
                    tmpList.Clear();
                    allMagicPowersForChaosMageList.AddRange(MagicPowersE.Except(MagicPowersE.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Shapeshift)));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersC.Except(MagicPowersC.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Recall)));
                    allMagicPowersForChaosMageList.Add((MagicPowersShadow.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_ShadowWalk)));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersBrightmage.Except(MagicPowersBrightmage.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_SpiritOfLight)));
                    tmpList.Add(MagicPowersShaman.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_GuardianSpirit));
                    //tmpList.Add(MagicPowersShaman.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Totems));
                    allMagicPowersForChaosMageList.AddRange(MagicPowersShaman.Except(tmpList));
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
                    allMagicPowersList.AddRange(AllMagicPowersWithSkills);
                    allMagicPowersList.AddRange(MagicPowersStandalone);
                    allMagicPowersList.AddRange(MagicPowersCustomStandalone);
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
                    allMagicPowersWithSkillsList.AddRange(MagicPowersCustom);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersCM);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersW);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersC);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersE);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersBM);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersIF);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersHoF);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersSB);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersA);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersP);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersPR);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersS);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersD);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersN);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersB);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersSD);
                    //allMagicPowersWithSkillsList.Add(MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_Repulsion));
                    //allMagicPowersWithSkillsList.Add(MagicPowersWD.FirstOrDefault(mp => mp.abilityDef == TorannMagicDefOf.TM_PsychicShock));
                    allMagicPowersWithSkillsList.AddRange(MagicPowersWD);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersG);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersT);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersShadow);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersBrightmage);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersShaman);
                    allMagicPowersWithSkillsList.AddRange(MagicPowersGolemancer);
                }
                return allMagicPowersWithSkillsList;
            }
        }

        private List<MagicPower> uniquePowers = new List<MagicPower>();
        public List<MagicPower> AllUniqueMagicPowers
        {
            get
            {
                if (uniquePowers != null && uniquePowers.Count > 0) return uniquePowers;

                uniquePowers = AllMagicPowers.Distinct().ToList();
                return uniquePowers;
            }
        }

        List<MagicPowerSkill> allMagicPowerSkillsList = new List<MagicPowerSkill>();
        public List<MagicPowerSkill> AllMagicPowerSkills
        {
            get
            {
                if (allMagicPowerSkillsList != null && allMagicPowerSkillsList.Count > 0)
                    return allMagicPowerSkillsList;

                allMagicPowerSkillsList = new List<MagicPowerSkill>();
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_AccelerateTime);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_AdvancedHeal);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_AlterFate);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_AMP);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Attraction);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BardTraining);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BattleHymn);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BestowMight);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Blink);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Blizzard);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BloodForBlood);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BloodGift);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BloodMoon);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_BloodShield);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Cantrips);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ChaosTradition);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ChronostaticField);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ConsumeCorpse);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_CorpseExplosion);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_CureDisease);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_DeathBolt);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_DeathMark);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Dominate);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_EarthernHammer);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_EarthSprites);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Encase);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_EnchantedBody);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_EnchanterStone);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_EnchantWeapon);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Entertain);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_EyeOfTheStorm);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Fireball);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Firebolt);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Fireclaw);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Firestorm);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_FogOfTorment);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_FoldReality);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_FrostRay);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_global_eff);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_global_regen);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_global_spirit);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Heal);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_HealingCircle);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_HolyWrath);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Icebolt);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_IgniteBlood);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Inspire);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LichForm);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LightningBolt);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LightningCloud);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LightningStorm);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Lullaby);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_MagicMissile);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Meteor);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_OrbitalStrike);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Overdrive);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Overwhelm);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Poison);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Polymorph);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Prediction);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_PsychicShock);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Purify);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Rainmaker);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_RaiseUndead);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_RayofHope);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_P_RayofHope);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Recall);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Regenerate);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_RegrowLimb);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Rend);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Repulsion);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Resurrection);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ReverseTime);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Sabotage);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Scorn);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Sentinel);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Shadow);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ShadowBolt);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Shapeshift);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Shield);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Snowball);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Soothe);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SootheAnimal);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SoulBond);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Stoneskin);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Summon);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SummonElemental);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SummonExplosive);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SummonMinion);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SummonPoppi);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SummonPylon);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_TechnoBit);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_TechnoShield);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_TechnoTurret);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_TechnoWeapon);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Teleport);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Transmutate);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ValiantCharge);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_WandererCraft);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ShadowWalk);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LightLance);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Sunfire);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LightBurst);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LightSkip);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Refraction);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SpiritOfLight);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Totems);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_ChainLightning);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Enrage);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Hex);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SpiritWolves);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_GuardianSpirit);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Golemancy);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_RuneCarving);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Branding);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SigilSurge);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_SigilDrain);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_LivingWall);
                allMagicPowerSkillsList.AddRange(MagicPowerSkill_Custom);
                return allMagicPowerSkillsList;
            }
        }

        public void ResetAllSkills()
        {
            List<MagicPowerSkill> mps = AllMagicPowerSkills;
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
            magicPawn = newUser.Pawn;
        }

        public void ClearData()
        {
            magicUserLevel = 0;
            magicUserXP = 0;
            magicAbilityPoints = 0;
            dominationCount = 0;
            magicPowerW.Clear();
            magicPowerA.Clear();
            magicPowerB.Clear();
            magicPowerD.Clear();
            magicPowerG.Clear();
            magicPowerHoF.Clear();
            magicPowerIF.Clear();
            magicPowerN.Clear();
            magicPowerP.Clear();
            magicPowerPR.Clear();
            magicPowerS.Clear();
            magicPowerSB.Clear();
            magicPowerSD.Clear();
            magicPowerC.Clear();
            magicPowerCM.Clear();
            magicPowerShadow.Clear();
            magicPowerBrightmage.Clear();
            magicPowerShaman.Clear();
            magicPowerGolemancer.Clear();
            magicPowerCustom.Clear();
            magicPawn = null;
            initialized = false;
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
