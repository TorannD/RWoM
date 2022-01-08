using System;
using RimWorld;
using Verse;
using AbilityUser;


namespace TorannMagic
{
	[DefOf]
	public static class TorannMagicDefOf
	{
        //Magic
        public static NeedDef TM_Mana;
        
        public static HediffDef TM_MagicUserHD;
        
        public static ThingDef BookOfInnerFire;
        public static ThingDef Torn_BookOfInnerFire;
        public static ThingDef BookOfHeartOfFrost;
        public static ThingDef Torn_BookOfHeartOfFrost;
        public static ThingDef BookOfStormBorn;
        public static ThingDef Torn_BookOfStormBorn;
        public static ThingDef BookOfArcanist;
        public static ThingDef Torn_BookOfArcanist;
        public static ThingDef BookOfValiant;
        public static ThingDef Torn_BookOfValiant;
        public static ThingDef BookOfSummoner;
        public static ThingDef Torn_BookOfSummoner;
        public static ThingDef BookOfDruid;
        public static ThingDef Torn_BookOfNature;
        public static ThingDef BookOfNecromancer;
        public static ThingDef Torn_BookOfUndead;
        public static ThingDef BookOfPriest;
        public static ThingDef Torn_BookOfPriest;
        public static ThingDef BookOfBard;
        public static ThingDef Torn_BookOfBard;
        public static ThingDef BookOfDemons;
        public static ThingDef Torn_BookOfDemons;
        public static ThingDef BookOfEarth;
        public static ThingDef Torn_BookOfEarth;
        public static ThingDef BookOfMagitech;
        public static ThingDef Torn_BookOfMagitech;
        public static ThingDef BookOfHemomancy;
        public static ThingDef Torn_BookOfHemomancy;
        public static ThingDef BookOfEnchanter;
        public static ThingDef Torn_BookOfEnchanter;
        public static ThingDef BookOfChronomancer;
        public static ThingDef Torn_BookOfChronomancer;
        public static ThingDef BookOfChaos;
        public static ThingDef Torn_BookOfChaos;
        public static ThingDef BookOfShadow;
        public static ThingDef BookOfTheSun;
        public static ThingDef Torn_BookOfTheSun;
        public static ThingDef Torn_BookOfShamanism;
        public static ThingDef BookOfShamanism;
        public static ThingDef Torn_BookOfGolemancy;
        public static ThingDef BookOfGolemancy;

        public static ThingDef BookOfQuestion;

        public static HediffDef TM_Uncertainty;        

        public static ThingDef SpellOf_Rain;
        public static ThingDef SpellOf_Blink;
        public static ThingDef SpellOf_Teleport;
        public static ThingDef SpellOf_Heal;
        public static ThingDef SpellOf_Heater;
        public static ThingDef SpellOf_Cooler;
        public static ThingDef SpellOf_PowerNode;
        public static ThingDef SpellOf_Sunlight;
        public static ThingDef SpellOf_DryGround;
        public static ThingDef SpellOf_WetGround;
        public static ThingDef SpellOf_ChargeBattery;
        public static ThingDef SpellOf_SmokeCloud;
        public static ThingDef SpellOf_Extinguish;
        public static ThingDef SpellOf_EMP;
        public static ThingDef SpellOf_Firestorm;
        public static ThingDef SpellOf_Blizzard;
        public static ThingDef SpellOf_SummonMinion;
        public static ThingDef SpellOf_TransferMana;
        public static ThingDef SpellOf_SiphonMana;
        public static ThingDef SpellOf_RegrowLimb;
        public static ThingDef SpellOf_EyeOfTheStorm;
        public static ThingDef SpellOf_ManaShield;
        public static ThingDef SpellOf_FoldReality;
        public static ThingDef SpellOf_Resurrection;
        public static ThingDef SpellOf_HolyWrath;
        public static ThingDef SpellOf_LichForm;
        public static ThingDef SpellOf_SummonPoppi;
        public static ThingDef SpellOf_BattleHymn;
        public static ThingDef SpellOf_CauterizeWound;
        public static ThingDef SpellOf_FertileLands;
        public static ThingDef SpellOf_SpellMending;
        public static ThingDef SpellOf_PsychicShock;
        public static ThingDef SpellOf_Scorn;
        public static ThingDef SpellOf_Meteor;
        public static ThingDef SpellOf_Sabotage;
        public static ThingDef SpellOf_Overdrive;
        public static ThingDef SpellOf_TechnoShield;
        public static ThingDef SpellOf_OrbitalStrike;
        public static ThingDef SpellOf_BloodMoon;
        public static ThingDef SpellOf_Shapeshift;
        public static ThingDef SpellOf_Blur;
        public static ThingDef SpellOf_BlankMind;
        public static ThingDef SpellOf_DirtDevil;
        public static ThingDef SpellOf_MechaniteReprogramming;
        public static ThingDef SpellOf_ArcaneBolt;
        public static ThingDef SpellOf_LightningTrap;
        public static ThingDef SpellOf_Invisibility;
        public static ThingDef SpellOf_BriarPatch;
        public static ThingDef SpellOf_Recall;
        public static ThingDef SpellOf_MageLight;
        public static HediffDef TM_MageLightHD;
        public static ThingDef SpellOf_Ignite;
        public static ThingDef SpellOf_SnapFreeze;
        public static ThingDef SpellOf_SpiritOfLight;
        public static ThingDef SpellOf_GuardianSpirit;
        public static ThingDef SpellOf_ShieldOther;
        public static ThingDef SpellOf_Discord;
        public static ThingDef SpellOf_BrandSiphon;
        public static ThingDef SpellOf_LivingWall;

        public static ThingDef SkillOf_Sprint;
        public static ThingDef SkillOf_GearRepair;
        public static ThingDef SkillOf_InnerHealing;
        public static ThingDef SkillOf_StrongBack;
        public static ThingDef SkillOf_ThickSkin;
        public static ThingDef SkillOf_FightersFocus;
        public static ThingDef SkillOf_HeavyBlow;
        public static ThingDef SkillOf_ThrowingKnife;
        public static ThingDef SkillOf_BurningFury;
        public static ThingDef SkillOf_PommelStrike;
        public static ThingDef SkillOf_TempestStrike;
        public static ThingDef SkillOf_Legion;

        public static ThingDef ManaPotion;
        public static ThingDef FlyingObject_Spinning;
        public static ThingDef FlyingObject_DirtDevil;
        public static ThingDef FlyingObject_LightningTrap;

        public static GameConditionDef ManaDrain;
        public static GameConditionDef ManaSurge;
        public static GameConditionDef DarkClouds;
        public static GameConditionDef DarkThunderstorm;
        public static GameConditionDef TM_ManaStorm;
        public static HediffDef TM_ManaSickness;
        public static HediffDef TM_ArcaneSickness;
        public static HediffDef TM_ArcaneWeakness;
        public static HediffDef TM_EnergyRegenHD;
        public static HediffDef TM_EnlightenedHD;

        public static HediffDef TM_BrittleBonesHD;
        public static HediffDef TM_SlaggedHD;

        //Artifacts
        public static ThingDef TM_Artifact_Silver;
        public static ThingDef TM_Artifact_Slate;
        public static ThingDef TM_Artifact_Limestone;
        public static ThingDef TM_Artifact_Granite;
        public static ThingDef TM_Artifact_Marble;
        public static ThingDef TM_Artifact_Sandstone;
        public static ThingDef TM_Artifact_Steel;
        public static ThingDef TM_Artifact_Iron;
        public static ThingDef TM_Artifact_Medicine;
        public static ThingDef TM_Artifact_Jade;
        public static ThingDef TM_Artifact_Gold;
        public static ThingDef TM_Artifact_Wood;
        public static ThingDef TM_Artifact_Magicyte;

        public static ThingDef TM_Artifact_BracersOfThePacifist;
        public static ThingDef TM_Artifact_BracersOfDefense;
        public static HediffDef TM_ArtifactBlockHD;
        public static ThingDef TM_Artifact_BracersOfDeflection;
        public static HediffDef TM_ArtifactDeflectHD;
        public static ThingDef TM_Artifact_RingOfBlood;
        public static HediffDef TM_Artifact_BloodBoostHD;
        public static ThingDef TM_Artifact_RingOfMalice;
        public static HediffDef TM_Artifact_HateBoostHD;
        public static ThingDef TM_Artifact_RingOfEternalBlue;
        public static HediffDef TM_Artifact_PsionicBoostHD;
        public static ThingDef TM_Artifact_OrbOfConviction;
        public static TMAbilityDef TM_Artifact_Conviction;
        public static ThingDef TM_Artifact_OrbOfSouls;
        public static AbilityUser.AbilityDef TM_Artifact_TraitThief;
        public static ThingDef TM_Artifact_OrbOfSouls_Full;
        public static AbilityUser.AbilityDef TM_Artifact_TraitInfuse;
        public static HediffDef TM_TraitInfusionHD;
        public static ThingDef TM_Artifact_NecroticOrb;

        //Site Defs
        public static WorldObjectDef ArcaneAdventure;
        //public static SiteCoreDef ArcaneStash;
        //public static SitePartDef ArcaneStashTreasure;
        //public static SitePartDef ArcaneDefenders;
        //public static SitePartDef EnemyRaidOnArrival;
        public static SitePartDef ArcaneBanditSquad;
        //public static IncidentDef ArcaneEnemyRaid;

        public static TraitDef TM_Gifted;
        public static TraitDef TM_OKWithDeath;
        public static TraitDef TM_ManaWellTD;
        public static TraitDef TM_ArcaneConduitTD;

        //Wanderer
        public static TraitDef TM_Wanderer;

        public static TMAbilityDef TM_WandererCraft;
        public static TMAbilityDef TM_Cantrips ;
        public static HediffDef TM_FrostSlowHD;
        public static HediffDef TM_CoolHD;
        public static HediffDef TM_WarmHD;
        public static HediffDef TM_RefreshedHD;
        public static HediffDef TM_JoltHD;
        public static HediffDef TM_ShockTherapyHD;
        
		//Fire
		public static TraitDef InnerFire;

        public static HediffDef InnerFireHD;
        public static HediffDef InnerFire_IHD;
        public static HediffDef InnerFire_IIHD;
        public static HediffDef InnerFire_IIIHD;
        public static HediffDef TM_InnerFire_AuraHD;
        public static TMAbilityDef TM_RayofHope;        
        public static TMAbilityDef TM_RayofHope_I;        
        public static TMAbilityDef TM_RayofHope_II;        
        public static TMAbilityDef TM_RayofHope_III;        
        public static TMAbilityDef TM_Firebolt;
        public static TMAbilityDef TM_Fireball;
		public static TMAbilityDef TM_Fireclaw;
		public static TMAbilityDef TM_Firestorm;

        public static ThingDef TM_Firestorm_Small;
        public static ThingDef TM_Firestorm_Tiny;
        public static ThingDef TM_Firestorm_Large;

		//Ice
		public static TraitDef HeartOfFrost;

        public static TMAbilityDef TM_Soothe;
        public static HediffDef SoothingBreeze;
        public static TMAbilityDef TM_Soothe_I;
        public static HediffDef SoothingBreeze_I;
        public static TMAbilityDef TM_Soothe_II;
        public static HediffDef SoothingBreeze_II;
        public static TMAbilityDef TM_Soothe_III;
        public static HediffDef SoothingBreeze_III;
        public static HediffDef TM_SoothingBreeze_AuraHD;
        public static TMAbilityDef TM_Icebolt;
		public static TMAbilityDef TM_Snowball;
        public static TMAbilityDef TM_FrostRay;
        public static TMAbilityDef TM_FrostRay_I;
        public static TMAbilityDef TM_FrostRay_II;
        public static TMAbilityDef TM_FrostRay_III;
        public static TMAbilityDef TM_Rainmaker;
        public static TMAbilityDef TM_Blizzard;

        public static ThingDef TM_Blizzard_Small;
        public static ThingDef TM_Blizzard_Tiny;
        public static ThingDef TM_Blizzard_Large;

        //Lightning
        public static TraitDef StormBorn;

        public static TMAbilityDef TM_AMP;
        public static TMAbilityDef TM_AMP_I;
        public static TMAbilityDef TM_AMP_II;
        public static TMAbilityDef TM_AMP_III;
        public static TMAbilityDef TM_LightningBolt;
        public static TMAbilityDef TM_LightningCloud;
		public static TMAbilityDef TM_LightningStorm;
        public static TMAbilityDef TM_EyeOfTheStorm;
        public static ThingDef FlyingObject_EyeOfTheStorm;
        

		//Arcane
		public static TraitDef Arcanist;

        public static TMAbilityDef TM_Shadow;
        public static HediffDef Shadow;
        public static TMAbilityDef TM_Shadow_I;
        public static HediffDef Shadow_I;
        public static TMAbilityDef TM_Shadow_II;
        public static HediffDef Shadow_II;        
        public static TMAbilityDef TM_Shadow_III;
        public static HediffDef Shadow_III;
        public static HediffDef TM_Shadow_AuraHD;
        public static TMAbilityDef TM_MagicMissile;
        public static TMAbilityDef TM_MagicMissile_I;
        public static TMAbilityDef TM_MagicMissile_II;
        public static TMAbilityDef TM_MagicMissile_III;
        public static TMAbilityDef TM_Blink;
        public static TMAbilityDef TM_Blink_I;
        public static TMAbilityDef TM_Blink_II;
        public static TMAbilityDef TM_Blink_III;
        public static TMAbilityDef TM_Summon;
        public static TMAbilityDef TM_Summon_I;
        public static TMAbilityDef TM_Summon_II;
        public static TMAbilityDef TM_Summon_III;
        public static TMAbilityDef TM_Teleport;
        public static TMAbilityDef TM_FoldReality;

        public static JobDef PortalDestination;
        public static JobDef UsePortal;
        public static JobDef DeactivatePortal;
        public static JobDef ChargePortal;
        public static JobDef PortalStockpile;

        //Holy (Paladin)
        public static TraitDef Paladin;

        public static TMAbilityDef TM_P_RayofHope;
        public static HediffDef RayofHope;
        public static TMAbilityDef TM_P_RayofHope_I;
        public static HediffDef RayofHope_I;
        public static TMAbilityDef TM_P_RayofHope_II;
        public static HediffDef RayofHope_II;
        public static TMAbilityDef TM_P_RayofHope_III;
        public static HediffDef RayofHope_III;
        public static HediffDef TM_RayOfHope_AuraHD;
        public static TMAbilityDef TM_Heal;
        public static TMAbilityDef TM_Shield;
        public static TMAbilityDef TM_Shield_I;
        public static TMAbilityDef TM_Shield_II;
        public static TMAbilityDef TM_Shield_III;
        public static HediffDef TM_HediffShield;
        public static TMAbilityDef TM_ShieldOther;
        public static HediffDef TM_MagicShieldHD;
        public static TMAbilityDef TM_ValiantCharge;
        public static HediffDef TM_HediffInvulnerable;
        public static HediffDef TM_HediffTimedInvulnerable;
        public static TMAbilityDef TM_Overwhelm;
        public static HediffDef TM_Blind;
        public static TMAbilityDef TM_HolyWrath;

        //Summoner
        public static TraitDef Summoner;
        public static ThingDef TM_Earth_ElementalR;
        public static ThingDef TM_LesserEarth_ElementalR;
        public static ThingDef TM_GreaterEarth_ElementalR;
        public static ThingDef TM_Fire_ElementalR;
        public static ThingDef TM_LesserFire_ElementalR;
        public static ThingDef TM_GreaterFire_ElementalR;
        public static ThingDef TM_Water_ElementalR;
        public static ThingDef TM_LesserWater_ElementalR;
        public static ThingDef TM_GreaterWater_ElementalR;
        public static ThingDef TM_Wind_ElementalR;
        public static ThingDef TM_LesserWind_ElementalR;
        public static ThingDef TM_GreaterWind_ElementalR;
        public static ThingDef TM_MinionR;
        public static ThingDef TM_GreaterMinionR;
        //public static ThingDef TM_InvisMinionR;
        public static ThingDef TM_Poppi;
        public static FactionDef TM_ElementalFaction;
        public static FactionDef TM_SummonedFaction;

        public static TMAbilityDef TM_SummonMinion;
        public static TMAbilityDef TM_DismissMinion;
        public static TMAbilityDef TM_SummonPylon;
        public static TMAbilityDef TM_SummonExplosive;
        public static TMAbilityDef TM_SummonElemental;
        public static TMAbilityDef TM_SummonPoppi;

        //Druid
        public static TraitDef Druid;

        public static TMAbilityDef TM_Poison;
        public static HediffDef TM_Poison_HD;
        public static HediffDef TM_Poisoned_HD;
        public static TMAbilityDef TM_SootheAnimal;
        public static TMAbilityDef TM_SootheAnimal_I;
        public static TMAbilityDef TM_SootheAnimal_II;
        public static TMAbilityDef TM_SootheAnimal_III;
        public static TMAbilityDef TM_Regenerate;
        public static HediffDef TM_Regeneration;
        public static HediffDef TM_Regeneration_I;
        public static HediffDef TM_Regeneration_II;
        public static HediffDef TM_Regeneration_III;
        public static TMAbilityDef TM_CureDisease;
        public static HediffDef TM_DiseaseImmunityHD;
        public static HediffDef TM_DiseaseImmunity2HD;
        public static TMAbilityDef TM_RegrowLimb;  // ultimate 
        public static HediffDef TM_ArmRegrowth;
        public static HediffDef TM_HandRegrowth;
        public static HediffDef TM_FootRegrowth;
        public static HediffDef TM_LegRegrowth;
        public static RecipeDef Regrowth;
        public static RecipeDef UniversalRegrowth;
        public static RecipeDef AdministerOrbOfTheEternal;

        //Necromancer
        public static TraitDef Necromancer;
        public static TraitDef Undead;
        public static TraitDef Lich;

        public static TMAbilityDef TM_RaiseUndead;
        public static HediffDef TM_UndeadHD;
        public static HediffDef TM_UndeadAnimalHD;
        public static HediffDef TM_UndeadStageHD;
        public static TrainableDef Haul;
        public static TMAbilityDef TM_DeathMark;
        public static TMAbilityDef TM_DeathMark_I;
        public static TMAbilityDef TM_DeathMark_II;
        public static TMAbilityDef TM_DeathMark_III;
        public static HediffDef TM_DeathMarkCurse;
        public static HediffDef TM_DeathMarkHD;
        public static TMAbilityDef TM_FogOfTorment;
        public static HediffDef TM_TormentHD;
        public static TMAbilityDef TM_ConsumeCorpse;
        public static TMAbilityDef TM_ConsumeCorpse_I;
        public static TMAbilityDef TM_ConsumeCorpse_II;
        public static TMAbilityDef TM_ConsumeCorpse_III;
        public static TMAbilityDef TM_CorpseExplosion;
        public static TMAbilityDef TM_CorpseExplosion_I;
        public static TMAbilityDef TM_CorpseExplosion_II;
        public static TMAbilityDef TM_CorpseExplosion_III;
        public static TMAbilityDef TM_DismissUndead;
        public static TMAbilityDef TM_DeathBolt;
        public static TMAbilityDef TM_DeathBolt_I;
        public static TMAbilityDef TM_DeathBolt_II;
        public static TMAbilityDef TM_DeathBolt_III;
        public static TMAbilityDef TM_LichForm;
        public static HediffDef TM_LichHD;
        public static ThingDef FlyingObject_DeathBolt;
        public static TMAbilityDef TM_Flight;
        public static ThingDef FlyingObject_Flight;

        public static WorkTypeDef Art;
        public static WorkTypeDef Research;
        public static WorkTypeDef Cleaning;
        public static WorkTypeDef Hauling;
        public static WorkTypeDef Tailoring;
        public static WorkTypeDef Smithing;
        public static WorkTypeDef PlantCutting;
        public static WorkTypeDef Cooking;
        public static WorkTypeDef PatientBedRest;

        //Priest
        public static TraitDef Priest;

        public static TMAbilityDef TM_AdvancedHeal;
        public static TMAbilityDef TM_Purify;
        public static ChemicalDef Luciferium;
        public static TMAbilityDef TM_HealingCircle;
        public static TMAbilityDef TM_HealingCircle_I;
        public static TMAbilityDef TM_HealingCircle_II;
        public static TMAbilityDef TM_HealingCircle_III;
        public static TMAbilityDef TM_BestowMight;
        public static HediffDef BestowMightHD;
        public static HediffDef BestowMightHD_I;
        public static HediffDef BestowMightHD_II;
        public static HediffDef BestowMightHD_III;
        public static TMAbilityDef TM_BestowMight_I;
        public static TMAbilityDef TM_BestowMight_II;
        public static TMAbilityDef TM_BestowMight_III;
        public static HediffDef TM_ResurrectionHD;
        public static TMAbilityDef TM_Resurrection;

        //Bard
        public static TraitDef TM_Bard;

        public static TMAbilityDef TM_BardTraining;
        public static TMAbilityDef TM_Entertain;
        public static InteractionDef TM_EntertainID;
        public static TMAbilityDef TM_Inspire;
        public static TMAbilityDef TM_Lullaby;
        public static TMAbilityDef TM_Lullaby_I;
        public static TMAbilityDef TM_Lullaby_II;
        public static TMAbilityDef TM_Lullaby_III;
        public static HediffDef TM_LullabyHD;
        public static TMAbilityDef TM_BattleHymn;

        //Succubus & Warlock
        public static TraitDef Succubus;
        public static TraitDef Warlock;

        public static TMAbilityDef TM_SoulBond;
        public static HediffDef TM_SDSoulBondPhysicalHD;
        public static HediffDef TM_WDSoulBondMentalHD;
        public static TMAbilityDef TM_ShadowCall;
        public static TMAbilityDef TM_ShadowStep;
        public static TMAbilityDef TM_ShadowBolt;
        public static TMAbilityDef TM_ShadowBolt_I;
        public static TMAbilityDef TM_ShadowBolt_II;
        public static TMAbilityDef TM_ShadowBolt_III;
        public static ThingDef FlyingObject_ShadowBolt;
        public static TMAbilityDef TM_Dominate;
        public static HediffDef TM_SDDominateHD;
        public static HediffDef TM_SDDominateHD_I;
        public static HediffDef TM_SDDominateHD_II;
        public static HediffDef TM_SDDominateHD_III;
        public static HediffDef TM_WDDominateHD;
        public static HediffDef TM_WDDominateHD_I;
        public static HediffDef TM_WDDominateHD_II;
        public static HediffDef TM_WDDominateHD_III;
        public static TMAbilityDef TM_Attraction;
        public static TMAbilityDef TM_Attraction_I;
        public static TMAbilityDef TM_Attraction_II;
        public static TMAbilityDef TM_Attraction_III;
        public static HediffDef TM_GravitySlowHD;
        public static TMAbilityDef TM_Repulsion;
        public static TMAbilityDef TM_Repulsion_I;
        public static TMAbilityDef TM_Repulsion_II;
        public static TMAbilityDef TM_Repulsion_III;
        public static TMAbilityDef TM_Scorn;
        public static HediffDef TM_DemonScornHD;
        public static HediffDef TM_DemonScornHD_I;
        public static HediffDef TM_DemonScornHD_II;
        public static HediffDef TM_DemonScornHD_III;
        public static TMAbilityDef TM_PsychicShock;
        public static TMAbilityDef TM_SummonDemon;
        public static ThingDef FlyingObject_DemonFlight;
        public static PawnKindDef TM_Demon;
        public static ThingDef TM_DemonR;
        public static PawnKindDef TM_LesserDemon;
        public static ThingDef TM_LesserDemonR;

        //Geomancer
        public static TraitDef Geomancer;

        public static TMAbilityDef TM_Stoneskin;
        public static HediffDef TM_StoneskinHD;
        public static TMAbilityDef TM_DispelStoneskin;
        public static TMAbilityDef TM_Encase;
        public static TMAbilityDef TM_Encase_I;
        public static TMAbilityDef TM_Encase_II;
        public static TMAbilityDef TM_Encase_III;
        public static TMAbilityDef TM_EarthSprites;
        public static TMAbilityDef TM_DismissEarthSprites;
        public static TMAbilityDef TM_EarthernHammer;
        public static TMAbilityDef TM_Sentinel;
        public static TMAbilityDef TM_ShatterSentinel;
        public static TMAbilityDef TM_Meteor;
        public static TMAbilityDef TM_Meteor_I;
        public static TMAbilityDef TM_Meteor_II;
        public static TMAbilityDef TM_Meteor_III;

        //Technomancer
        public static TraitDef Technomancer;

        public static TMAbilityDef TM_TechnoBit;
        public static HediffDef TM_TechnoBitHD;
        public static TMAbilityDef TM_TechnoTurret;
        public static TMAbilityDef TM_TechnoWeapon;
        public static TMAbilityDef TM_NanoStimulant;
        public static HediffDef TM_NanoStimulantHD;
        public static TMAbilityDef TM_TechnoShield;
        public static HediffDef TM_TechnoShieldHD;
        public static HediffDef TM_TechnoShieldHD_I;
        public static HediffDef TM_TechnoShieldHD_II;
        public static HediffDef TM_TechnoShieldHD_III;
        public static TMAbilityDef TM_Sabotage;
        //public static HediffDef TM_SabotageHD;
        public static TMAbilityDef TM_Overdrive;
        public static HediffDef TM_OverdriveHD;
        public static HediffDef TM_OverdriveHD_I;
        public static HediffDef TM_OverdriveHD_II;
        public static HediffDef TM_OverdriveHD_III;
        public static TMAbilityDef TM_OrbitalStrike;
        public static TMAbilityDef TM_OrbitalStrike_I;
        public static TMAbilityDef TM_OrbitalStrike_II;
        public static TMAbilityDef TM_OrbitalStrike_III;

        //Blood Mage
        public static TraitDef BloodMage;

        public static HediffDef TM_BloodHD;
        public static TMAbilityDef TM_BloodGift;
        public static TMAbilityDef TM_IgniteBlood;
        public static TMAbilityDef TM_BloodForBlood;
        public static HediffDef TM_BloodForBloodHD;
        public static TMAbilityDef TM_BloodShield;
        public static TMAbilityDef TM_Rend;
        public static TMAbilityDef TM_Rend_I;
        public static TMAbilityDef TM_Rend_II;
        public static TMAbilityDef TM_Rend_III;
        public static HediffDef TM_RendHD;
        public static TMAbilityDef TM_BloodMoon;
        public static TMAbilityDef TM_BloodMoon_I;
        public static TMAbilityDef TM_BloodMoon_II;
        public static TMAbilityDef TM_BloodMoon_III;

        //Enchanter
        public static TraitDef Enchanter;

        public static TMAbilityDef TM_EnchantedBody;
        public static HediffDef TM_EnchantedBodyHD;
        public static TMAbilityDef TM_EnchantedAura;
        public static HediffDef TM_EnchantedAuraHD;
        public static HediffDef TM_ArtifactPathfindingHD;
        public static TMAbilityDef TM_Transmutate;
        public static TMAbilityDef TM_EnchanterStone;
        public static TMAbilityDef TM_DismissEnchanterStones;
        public static TMAbilityDef TM_EnchantWeapon;
        public static HediffDef TM_WeaponEnchantment_IceHD;
        public static HediffDef TM_WeaponEnchantment_FireHD;
        public static HediffDef TM_WeaponEnchantment_LitHD;
        public static HediffDef TM_WeaponEnchantment_DarkHD;
        public static TMAbilityDef TM_DispelEnchantWeapon;
        public static TMAbilityDef TM_Polymorph;
        public static TMAbilityDef TM_Polymorph_I;
        public static TMAbilityDef TM_Polymorph_II;
        public static TMAbilityDef TM_Polymorph_III;
        public static TMAbilityDef TM_Shapeshift;
        public static ThoughtDef Polymorphed;
        public static ThoughtDef Polymorphed_Transhumanist;
        public static TMAbilityDef TM_ShapeshiftDW;
        public static HediffDef TM_ShapeshiftHD;        

        //Chronomancer
        public static TraitDef Chronomancer;

        public static TMAbilityDef TM_Prediction;
        public static HediffDef TM_PredictionHD;
        public static TMAbilityDef TM_AlterFate;
        public static TMAbilityDef TM_AccelerateTime;
        public static HediffDef TM_AccelerateTimeHD;
        public static TMAbilityDef TM_ReverseTime;
        public static HediffDef TM_ReverseTimeHD;
        public static HediffDef TM_ReverseTimeBadHD;
        public static HediffDef TM_DeathReversalHD;
        public static TMAbilityDef TM_ChronostaticField;
        public static TMAbilityDef TM_ChronostaticField_I;
        public static TMAbilityDef TM_ChronostaticField_II;
        public static TMAbilityDef TM_ChronostaticField_III;
        public static ThingDef FlyingObject_TimeDelay;
        public static TMAbilityDef TM_TimeMark;
        public static TMAbilityDef TM_Recall;

        //Chaos Mage
        public static TraitDef ChaosMage;

        public static TMAbilityDef TM_ChaosTradition;
        public static HediffDef TM_ChaosTraditionHD;
        public static HediffDef TM_ChaoticMindHD;
        public static ThoughtDef TM_ChaoticMindTD;
        public static HediffDef TM_ControlledChaosHD;

        //Brightmage
        public static TraitDef TM_Brightmage;

        public static HediffDef TM_LightCapacitanceHD;
        public static TMAbilityDef TM_LightLance;
        public static ThingDef FlyingObject_LightLance;
        public static TMAbilityDef TM_Sunfire;
        public static TMAbilityDef TM_Sunfire_I;
        public static TMAbilityDef TM_Sunfire_II;
        public static TMAbilityDef TM_Sunfire_III;
        public static TMAbilityDef TM_LightBurst;
        public static HediffDef TM_LightBurstHD;
        public static TMAbilityDef TM_LightSkipMass;   //standalone
        public static TMAbilityDef TM_LightSkip;
        public static TMAbilityDef TM_LightSkipGlobal; //standalone
        public static TMAbilityDef TM_Refraction;       
        public static TMAbilityDef TM_SpiritOfLight;
        public static TMAbilityDef TM_SoL_Equalize;
        public static TMAbilityDef TM_SoL_CreateLight;
        public static ThingDef FlyingObject_SpiritOfLight;
        public static ThingDef Projectile_LightLaser;
        public static ThingDef Projectile_ThrowingKnife;
        public static ThingDef TM_LightPod;
        public static ThingDef TM_LightPodLeaving;
        public static WorldObjectDef TM_TravelingTransportLightBeam;
        public static ThingDef TM_LightPodIncoming;
        public static ThingDef TM_ActiveLightPod;
        public static ThoughtDef TM_PleasantDreamsTD;
        public static ThoughtDef TM_BrightDayTD;

        //Shaman
        public static TraitDef TM_Shaman;

        public static TMAbilityDef TM_Totems;
        public static TMAbilityDef TM_SummonTotemLightning;
        public static TMAbilityDef TM_SummonTotemEarth;
        public static TMAbilityDef TM_SummonTotemHealing;
        public static TMAbilityDef TM_ChainLightning;
        public static ThingDef Projectile_Lightning;
        public static TMAbilityDef TM_Enrage;
        public static HediffDef TM_EnrageHD;
        public static TMAbilityDef TM_Hex;
        public static TMAbilityDef TM_Hex_I;
        public static TMAbilityDef TM_Hex_II;
        public static TMAbilityDef TM_Hex_III;
        public static TMAbilityDef TM_Hex_Pain;
        public static TMAbilityDef TM_Hex_CriticalFail;
        public static TMAbilityDef TM_Hex_MentalAssault;
        public static HediffDef TM_HexHD;
        public static HediffDef TM_Hex_CriticalFailHD;
        public static TMAbilityDef TM_SpiritWolves;
        public static TMAbilityDef TM_SpiritWolves_I;
        public static TMAbilityDef TM_SpiritWolves_II;
        public static TMAbilityDef TM_SpiritWolves_III;
        public static ThingDef FlyingObject_SpiritWolves;
        public static TMAbilityDef TM_GuardianSpirit;
        public static TMAbilityDef TM_DismissGuardianSpirit;
        public static ThingDef TM_LightningTotem;
        public static ThingDef TM_EarthTotem;
        public static ThingDef TM_HealingTotem;
        public static ThingDef TM_SpiritBearR;
        public static HediffDef TM_SpiritBondHD;
        public static ThingDef TM_SpiritMongooseR;
        public static ThingDef TM_SpiritCrowR;
        public static HediffDef TM_CrowInsightHD;
        public static HediffDef TM_BirdflightHD;
        public static PawnKindDef TM_SpiritWolf;
        public static ThingDef TM_SpiritWolfR;

        //Golemancer
        public static TraitDef TM_Golemancer;

        public static TMAbilityDef TM_Golemancy;
        public static HediffDef TM_GolemancyVersatilityHD;
        public static HediffDef TM_GolemancyPowerHD;
        public static HediffDef TM_GolemancyEfficiencyHD;
        public static TMAbilityDef TM_RuneCarving;
        public static HediffDef TM_RuneCarvedPartHD;
        public static StatDef TM_RuneCarvingEfficiency;
        public static EffecterDef TM_RuneCarving_EffecterED;
        public static RecipeDef TM_RuneCarveBodyPart;
        public static TMAbilityDef TM_Branding;
        public static TMAbilityDef TM_SiphonBrand;
        public static TMAbilityDef TM_DispelBranding;
        public static HediffDef TM_SiphonBrandHD;
        public static TMAbilityDef TM_FitnessBrand;
        public static HediffDef TM_FitnessBrandHD;
        public static TMAbilityDef TM_EmotionBrand;
        public static HediffDef TM_EmotionBrandHD;
        public static TMAbilityDef TM_VitalityBrand;
        public static HediffDef TM_VitalityBrandHD;
        public static TMAbilityDef TM_ProtectionBrand;
        public static HediffDef TM_ProtectionBrandHD;
        public static TMAbilityDef TM_AwarenessBrand;
        public static HediffDef TM_AwarenessBrandHD;
        public static TMAbilityDef TM_SigilSurge;
        public static TMAbilityDef TM_SigilDrain;
        public static HediffDef TM_SigilPainHD;
        public static TMAbilityDef TM_LivingWall;
        public static ThingDef FlyingObject_LivingWall;
        public static TMAbilityDef TM_DispelLivingWall;

        //Empath
        public static TraitDef TM_Empath;

        public static TMAbilityDef TM_Empathy;
        public static HediffDef TM_EmpathHD;
        public static TMAbilityDef TM_MindKiller;
        public static HediffDef TM_MindKillerHD;
        public static TMAbilityDef TM_HarvestPassion;
        public static HediffDef TM_HarvestPassionHD;
        public static TMAbilityDef TM_IncitePassion;
        public static TMAbilityDef TM_SuppressiveAura;
        public static HediffDef TM_SuppressiveAuraHD;
        public static HediffDef TM_EmotionSuppressionHD;
        public static ThoughtDef TM_PositiveEmpathyTD;
        public static ThoughtDef TM_NegativeEmpathyTD;
        public static ThoughtDef TM_EmotionalWeightTD;

        //Shadow
        public static TraitDef TM_TheShadow;

        public static TMAbilityDef TM_ShadowWalk;   //magic
        public static HediffDef TM_ShadowCloakHD;
        public static TMAbilityDef TM_VeilOfShadows; //might
        public static ThingDef Fog_Shadows;
        public static TMAbilityDef TM_ShadowStrike; //might
        public static TMAbilityDef TM_Nightshade; //might
        public static HediffDef TM_NightshadeHD;
        public static HediffDef TM_NightshadeToxinHD;         

        //Might 
        public static NeedDef TM_Stamina;
        public static HediffDef TM_MightUserHD;

        public static ThingDef BookOfGladiator;
        public static ThingDef BookOfSniper;
        public static ThingDef BookOfBladedancer;
        public static ThingDef BookOfRanger;
        public static ThingDef BookOfFaceless;
        public static ThingDef BookOfPsionic;
        public static ThingDef BookOfDeathKnight;
        public static ThingDef BookOfMonk;
        public static ThingDef BookOfCommander;
        public static ThingDef BookOfSuperSoldier;

        public static TraitDef PhysicalProdigy;
        public static TraitDef TM_BoundlessTD;
        public static ThoughtDef TM_OpinionOfBoundless;

        //Wayfarer
        public static TraitDef TM_Wayfarer;

        public static TMAbilityDef TM_WayfarerCraft;
        public static TMAbilityDef TM_FieldTraining;

        //Apothecary
        public static TraitDef TM_Apothecary;

        public static TMAbilityDef TM_Herbalist;
        public static HediffDef TM_ApothecaryHerbsHD;
        public static TMAbilityDef TM_PoisonFlask;
        public static HediffDef TM_HerbalElixirHD;
        public static TMAbilityDef TM_Elixir;
        public static TMAbilityDef TM_SoothingBalm;
        public static HediffDef TM_SoothingBalmHD;

        //Might (Gladiator)
        public static TraitDef Gladiator;

        public static TMAbilityDef TM_Sprint;
        public static TMAbilityDef TM_Sprint_I;
        public static TMAbilityDef TM_Sprint_II;
        public static TMAbilityDef TM_Sprint_III;
        public static TMAbilityDef TM_Fortitude;
        public static HediffDef TM_HediffFortitude;
        public static TMAbilityDef TM_Grapple;
        public static TMAbilityDef TM_Grapple_I;
        public static TMAbilityDef TM_Grapple_II;
        public static TMAbilityDef TM_Grapple_III;
        public static HediffDef TM_GrapplingHook;
        public static TMAbilityDef TM_Cleave;
        public static HediffDef TM_SunderArmorHD;
        public static TMAbilityDef TM_Whirlwind;
        public static HediffDef TM_Whirlwind_Knockdown;

        //Precision (Sniper)
        public static TraitDef TM_Sniper;

        public static TMAbilityDef TM_SniperFocus;
        public static TMAbilityDef TM_Headshot;
        public static TMAbilityDef TM_DisablingShot;
        public static TMAbilityDef TM_DisablingShot_I;
        public static TMAbilityDef TM_DisablingShot_II;
        public static TMAbilityDef TM_DisablingShot_III;
        public static HediffDef TM_DisablingShot_HD;
        public static TMAbilityDef TM_AntiArmor;
        public static TMAbilityDef TM_ShadowSlayer;
        public static HediffDef TM_ShadowSlayerCloakHD;

        //Bladedancer
        public static TraitDef Bladedancer;

        public static TMAbilityDef TM_BladeFocus;
        public static TMAbilityDef TM_BladeArt;
        public static HediffDef TM_BladeArtHD;
        public static TMAbilityDef TM_SeismicSlash;
        public static TMAbilityDef TM_BladeSpin;
        public static TMAbilityDef TM_PhaseStrike;
        public static TMAbilityDef TM_PhaseStrike_I;
        public static TMAbilityDef TM_PhaseStrike_II;
        public static TMAbilityDef TM_PhaseStrike_III;
        public static HediffDef TM_HamstringHD;

        //Ranger
        public static TraitDef Ranger;

        public static TMAbilityDef TM_RangerTraining;
        public static TMAbilityDef TM_BowTraining;
        public static HediffDef TM_BowTrainingHD;
        public static TMAbilityDef TM_PoisonTrap;
        public static TMAbilityDef TM_AnimalFriend;
        public static HediffDef TM_RangerBondHD;
        public static TMAbilityDef TM_ArrowStorm;
        public static TMAbilityDef TM_ArrowStorm_I;
        public static TMAbilityDef TM_ArrowStorm_II;
        public static TMAbilityDef TM_ArrowStorm_III;

        public static JobDef PlacePoisonTrap;
        public static TrainableDef Rescue;
        public static ThoughtDef RangerSoldBondedPet;
        public static ThoughtDef RangerPetDied;

        //Faceless
        public static TraitDef Faceless;

        public static TMAbilityDef TM_Disguise;
        public static HediffDef TM_DisguiseHD;
        public static HediffDef TM_DisguiseHD_I;
        public static HediffDef TM_DisguiseHD_II;
        public static HediffDef TM_DisguiseHD_III;
        public static TMAbilityDef TM_Mimic;
        public static TMAbilityDef TM_Reversal;
        public static HediffDef TM_ReversalHD;
        public static TMAbilityDef TM_Transpose;
        public static TMAbilityDef TM_Transpose_I;
        public static TMAbilityDef TM_Transpose_II;
        public static TMAbilityDef TM_Transpose_III;
        public static TMAbilityDef TM_Possess;
        public static HediffDef TM_PossessionHD;
        public static HediffDef TM_PossessionHD_I;
        public static HediffDef TM_PossessionHD_II;
        public static HediffDef TM_PossessionHD_III;
        public static HediffDef TM_CoOpPossessionHD;
        public static HediffDef TM_CoOpPossessionHD_I;
        public static HediffDef TM_CoOpPossessionHD_II;
        public static HediffDef TM_CoOpPossessionHD_III;

        //Psionic
        public static TraitDef TM_Psionic;

        public static HediffDef TM_PsionicHD;
        public static HediffDef TM_PsionicSpeedHD;
        public static HediffDef TM_PsionicManipulationHD;
        public static TMAbilityDef TM_PsionicAugmentation;
        public static TMAbilityDef TM_PsionicDash;
        public static TMAbilityDef TM_PsionicBlast;
        public static TMAbilityDef TM_PsionicBlast_I;
        public static TMAbilityDef TM_PsionicBlast_II;
        public static TMAbilityDef TM_PsionicBlast_III;
        public static TMAbilityDef TM_PsionicBarrier;
        public static TMAbilityDef TM_PsionicBarrier_Projected;
        public static TMAbilityDef TM_PsionicStorm;
        public static ThingDef FlyingObject_PsionicDash;
        public static ThingDef FlyingObject_PsionicStorm;
        public static ThingDef FlyingObject_PsiStorm;
        public static ThingDef FlyingObject_PsionicLeap;        

        //Death Knight
        public static TraitDef DeathKnight;

        public static HediffDef TM_HateHD;
        public static TMAbilityDef TM_Shroud;
        public static TMAbilityDef TM_WaveOfFear;
        public static HediffDef TM_WaveOfFearHD;
        public static TMAbilityDef TM_Spite;
        public static TMAbilityDef TM_Spite_I;
        public static TMAbilityDef TM_Spite_II;
        public static TMAbilityDef TM_Spite_III;
        public static TMAbilityDef TM_LifeSteal;
        public static TMAbilityDef TM_GraveBlade;
        public static TMAbilityDef TM_GraveBlade_I;
        public static TMAbilityDef TM_GraveBlade_II;
        public static TMAbilityDef TM_GraveBlade_III;

        //Monk
        public static TraitDef TM_Monk;

        public static TMAbilityDef TM_Chi;
        public static JobDef JobDriver_TM_Meditate;
        public static TMAbilityDef TM_ChiBurst;
        public static HediffDef TM_ChiHD;
        public static TMAbilityDef TM_MindOverBody;
        public static HediffDef TM_MindOverBodyHD;
        public static TMAbilityDef TM_Meditate;
        public static TMAbilityDef TM_TigerStrike;
        public static TMAbilityDef TM_DragonStrike;
        public static ThingDef FlyingObject_DragonStrike;
        public static TMAbilityDef TM_ThunderStrike;

        //Commander
        public static TraitDef TM_Commander;

        public static TMAbilityDef TM_ProvisionerAura;
        public static HediffDef TM_ProvisionerAuraHD;
        public static HediffDef TM_ProvisionerHD;
        public static TMAbilityDef TM_TaskMasterAura;
        public static HediffDef TM_TaskMasterAuraHD;
        public static ThoughtDef TM_TaskMasterTD;
        public static HediffDef TM_TaskMasterHD;
        public static TMAbilityDef TM_CommanderAura;
        public static HediffDef TM_CommanderAuraHD;
        public static ThoughtDef TM_CommanderTD;
        public static HediffDef TM_CommanderHD;
        public static ThoughtDef TM_CommanderTD_I;
        public static HediffDef TM_CommanderHD_I;
        public static ThoughtDef TM_CommanderTD_II;
        public static HediffDef TM_CommanderHD_II;
        public static ThoughtDef TM_CommanderTD_III;
        public static HediffDef TM_CommanderHD_III;
        public static JobDef JobDriver_TM_Command;
        public static TMAbilityDef TM_StayAlert;
        public static TMAbilityDef TM_StayAlert_I;
        public static TMAbilityDef TM_StayAlert_II;
        public static TMAbilityDef TM_StayAlert_III;
        public static HediffDef TM_StayAlertHD;
        public static TMAbilityDef TM_MoveOut;
        public static TMAbilityDef TM_MoveOut_I;
        public static TMAbilityDef TM_MoveOut_II;
        public static TMAbilityDef TM_MoveOut_III;
        public static HediffDef TM_MoveOutHD;
        public static TMAbilityDef TM_HoldTheLine;
        public static TMAbilityDef TM_HoldTheLine_I;
        public static TMAbilityDef TM_HoldTheLine_II;
        public static TMAbilityDef TM_HoldTheLine_III;
        public static HediffDef TM_HoldTheLineHD;
        public static HediffDef TM_HTLShieldHD;
        public static ThoughtDef TM_TakingOrdersTD;

        //Super Soldier
        public static TraitDef TM_SuperSoldier;
        public static HediffDef TM_SS_SerumHD;

        public static TMAbilityDef TM_PistolSpec;
        //public static TMAbilityDef TM_Gunslinger;
        public static TMAbilityDef TM_PistolWhip;
        public static TMAbilityDef TM_RifleSpec;
        public static TMAbilityDef TM_SuppressingFire;
        public static TMAbilityDef TM_Mk203GL;
        public static TMAbilityDef TM_ShotgunSpec;
        public static TMAbilityDef TM_Buckshot;
        public static TMAbilityDef TM_BreachingCharge;
        public static TMAbilityDef TM_CQC;
        public static TMAbilityDef TM_FirstAid;
        public static TMAbilityDef TM_60mmMortar;
        public static ThingDef FlyingObject_60mmMortar;
        public static ThingDef TM_60mmMortar_Base;

        //Standalone
        public static TMAbilityDef TM_Heater;
        public static TMAbilityDef TM_DismissHeater;
        public static TMAbilityDef TM_Cooler;
        public static TMAbilityDef TM_DismissCooler;
        public static TMAbilityDef TM_PowerNode;
        public static TMAbilityDef TM_DismissPowerNode;
        public static TMAbilityDef TM_Sunlight;
        public static TMAbilityDef TM_DismissSunlight;
        public static TMAbilityDef TM_DryGround;
        public static TMAbilityDef TM_WetGround;
        public static TMAbilityDef TM_ChargeBattery;
        public static TMAbilityDef TM_SmokeCloud;
        public static TMAbilityDef TM_Extinguish;
        public static TMAbilityDef TM_EMP;
        public static TMAbilityDef TM_TransferMana;
        public static TMAbilityDef TM_SiphonMana;
        public static TMAbilityDef TM_ManaShield;
        public static HediffDef TM_ManaShieldHD;
        public static TMAbilityDef TM_ArcaneBarrier;
        public static TMAbilityDef TM_CauterizeWound;
        public static TMAbilityDef TM_FertileLands;
        public static TMAbilityDef TM_DismissFertileLands;
        public static TMAbilityDef TM_SpellMending;
        public static TMAbilityDef TM_TeachMagic;
        public static TMAbilityDef TM_TeachMight;
        public static TMAbilityDef TM_Blur;
        public static HediffDef TM_BlurHD;
        public static TMAbilityDef TM_BlankMind;
        public static TMAbilityDef TM_DirtDevil;
        public static TMAbilityDef TM_MechaniteReprogramming;
        public static HediffDef TM_ReprogrammedSenMechanites_HD;
        public static HediffDef TM_ReprogrammedFibMechanites_HD;
        public static HediffDef TM_ReprogrammedLymMechanites_HD;
        public static TMAbilityDef TM_ArcaneBolt;
        public static TMAbilityDef TM_LightningTrap;
        public static JobDef JobDriver_PlaceLightningTrap;
        public static ThingDef TM_Trap_Lightning;
        public static TMAbilityDef TM_DismissLightningTrap;
        public static TMAbilityDef TM_Invisibility;
        public static HediffDef TM_InvisibilityHD;
        public static TMAbilityDef TM_BriarPatch;
        public static TMAbilityDef TM_MageLight;
        public static HediffDef TM_Tranquilizer_HD;
        public static HediffDef TM_HediffFightersFocus;
        public static HediffDef TM_HediffThickSkin;
        public static HediffDef TM_HediffStrongBack;
        public static HediffDef TM_HediffGearRepair;
        public static HediffDef TM_HediffHeavyBlow;
        public static HediffDef TM_HediffSprint;
        public static TMAbilityDef TM_Legion;
        public static TMAbilityDef TM_TempestStrike;
        public static HediffDef TM_MuteHD;
        public static TMAbilityDef TM_Ignite;
        public static TMAbilityDef TM_SnapFreeze;
        public static TMAbilityDef TM_Taunt;
        public static HediffDef TM_TauntHD;
        public static TMAbilityDef TM_Discord;

        public static HediffDef TM_Sight;
        public static HediffDef TM_Breathing;
        public static HediffDef TM_Manipulation;
        public static HediffDef TM_Movement;
        public static HediffDef TM_HasteHD;

        public static HediffDef TM_AntiSight;
        public static HediffDef TM_AntiBreathing;
        public static HediffDef TM_AntiManipulation;
        public static HediffDef TM_AntiMovement;

        public static TMAbilityDef TM_GearRepair;
        public static TMAbilityDef TM_InnerHealing;
        public static TMAbilityDef TM_HeavyBlow;
        public static TMAbilityDef TM_StrongBack;
        public static TMAbilityDef TM_ThickSkin;
        public static TMAbilityDef TM_FightersFocus;
        public static TMAbilityDef TM_ThrowingKnife;
        public static TMAbilityDef TM_BurningFury;
        public static HediffDef TM_BurningFuryHD;
        public static TMAbilityDef TM_PommelStrike;
        public static HediffDef TM_DisablingBlowHD;
        public static HediffDef TM_DiscordHD;
        public static HediffDef TM_DiscordSafeHD;
        public static HediffDef TM_SeverityHasteHD;

        public static TMAbilityDef TM_Explosion;

        //Elemental Magic
        public static TMAbilityDef TM_Elemental_Firebolt;
        public static TMAbilityDef TM_Elemental_Icebolt;

        //Graphics
        public static ThingDef Mote_ArcaneFabricationA;
        public static ThingDef Mote_ArcaneFabricationB;
        public static ThingDef Mote_ArcaneFabricationC;
        public static ThingDef Mote_ArcaneFabricationD;
        public static ThingDef Mote_BloodWolfNorth;
        public static ThingDef Mote_BloodWolfWest;
        public static ThingDef Mote_BloodWolfEast;
        public static ThingDef Mote_BloodWolfSouth;
        public static ThingDef Mote_DWPhase_North;
        public static ThingDef Mote_DWPhase_East;
        public static ThingDef Mote_DWPhase_West;
        public static ThingDef Mote_DWPhase_South;
        public static ThingDef Mote_Demon_Flame;
        public static ThingDef Mote_SpiritFlame;        
        public static ThingDef Mote_SpiritRetaliation;
        public static ThingDef Mote_ArcaneStream;
        public static ThingDef Mote_Psi_Arcane;
        public static ThingDef Mote_GraveBlade;
        public static ThingDef Mote_SummoningCircle;
        public static ThingDef Mote_BladeSweep;
        public static ThingDef Mote_ClawSweep;
        public static ThingDef Mote_Rubble;
        public static ThingDef Mote_Cleave;
        public static ThingDef Mote_BloodCircle;
        public static ThingDef Mote_Psi;
        public static ThingDef Mote_PsiCurrent;
        public static ThingDef Mote_PsiBlastStart;
        public static ThingDef Mote_PsiBlastEnd;
        public static ThingDef Mote_Scan;
        public static ThingDef Mote_ArcaneCircle;
        public static ThingDef Mote_ManaPuff;
        public static ThingDef Mote_Enchanting;
        public static ThingDef Mote_Siphon;
        public static ThingDef Mote_Poison;
        public static ThingDef Mote_Disease;
        public static ThingDef Mote_Regen;
        public static ThingDef Mote_CrossStrike;
        public static ThingDef Mote_BloodSquirt;
        public static ThingDef Mote_BloodMist;
        public static ThingDef Mote_BloodFlame;
        public static ThingDef Mote_MultiStrike;
        public static ThingDef Mote_ScreamMote;
        public static ThingDef Fog_Torment;
        public static ThingDef Mote_PowerBeamBlue;
        public static ThingDef Mote_PowerBeamGold;
        public static ThingDef Mote_PowerBeamPsionic;
        public static ThingDef Mote_Bombardment;
        public static ThingDef Fog_Poison;
        public static ThingDef Mote_ArcaneDaggers;
        public static ThingDef Mote_Bolt;
        public static ThingDef Mote_Arcane;
        public static ThingDef Mote_Note;
        public static ThingDef Mote_Exclamation;
        public static ThingDef Mote_ExclamationRed;
        public static ThingDef Mote_Twinkle;
        public static ThingDef Mote_Flame;
        public static ThingDef Mote_Ice;
        public static ThingDef Mote_Casting;
        public static ThingDef Mote_AntiCasting;
        public static ThingDef Mote_1sText;
        public static ThingDef Mote_DeceptionMask;
        public static ThingDef Mote_Possess;
        public static ThingDef Mote_Shadow;
        public static ThingDef Mote_ShadowCleave;
        public static ThingDef Mote_ArcaneWaves;
        public static ThingDef Mote_BracerBlock;
        public static ThingDef Mote_BracerBlock_NoFlash;
        public static ThingDef Mote_ThickDust;
        public static ThingDef Mote_ArcaneBlast;
        public static ThingDef Mote_Strike;
        public static ThingDef Mote_Psi_Grayscale;
        public static ThingDef Mote_Chi_Grayscale;
        public static ThingDef Mote_DragonStrike;
        public static ThingDef Mote_TigerStrike;
        public static ThingDef Mote_TechnoShield;
        public static ThingDef Mote_AlterFate;
        public static ThingDef Mote_EarthCrack;
        public static ThingDef Mote_Ghost;
        public static ThingDef Mote_GrappleHook;
        public static ThingDef Mote_BoneDust;
        public static ThingDef Mote_Psi_Yellow;
        public static ThingDef Mote_Flowers;
        public static ThingDef Mote_PowerWave;
        public static ThingDef Mote_CQC;
        public static ThingDef Mote_ArcaneFlame;
        public static ThingDef Mote_GreenTwinkle;
        public static ThingDef Mote_BlueTwinkle;
        public static ThingDef Mote_Twinkle_grayscale;
        public static ThingDef Mote_ShadowCloud;
        public static ThingDef Mote_Heat;
        public static ThingDef Mote_LightShield;
        public static ThingDef Mote_LightShield_Glow;
        public static ThingDef Mote_Holy;
        public static ThingDef Mote_LightBarrier;
        public static ThingDef Mote_DirectionalDirt;
        public static ThingDef Mote_DirectionalDirtOverhead;
        public static ThingDef Mote_ExpandingFlame;
        public static ThingDef Mote_Hex;
        public static ThingDef Mote_BlackSmoke;
        public static ThingDef Mote_BlackSmokeLow;
        public static ThingDef Mote_SpiritWolf_South;
        public static ThingDef Mote_SpiritWolf_North;
        public static ThingDef Mote_SpiritWolf_East;
        public static ThingDef Mote_SpiritWolf_West;
        public static ThingDef Mote_Healing_Small;
        public static ThingDef Mote_Base_Smoke;
        public static ThingDef Mote_BlueSpireE;
        public static ThingDef Mote_BlueSpireEs;
        public static ThingDef Mote_BlueSpireWs;
        public static ThingDef Mote_BlueSpireW;
        public static ThingDef Mote_BlueSwirl;
        public static ThingDef Mote_ManaVortex;
        public static ThingDef Mote_GlowingRuneA;
        public static ThingDef Mote_WallSpike;
        public static ThingDef Mote_EnergyStream;
        public static ThingDef Mote_RedSwirl;
        public static ThingDef Mote_PurpleSmoke;
        public static ThingDef Mote_Psi_Black;
        
        public static ThingDef Projectile_FogOfTorment;        

        public static EffecterDef GiantExplosion;
        public static EffecterDef TM_GiantExplosion;
        public static EffecterDef TM_Stoneskin_Effecter;
        public static EffecterDef TM_DeathExplosion;
        public static EffecterDef TM_OSExplosion;
        public static EffecterDef TM_SabotageExplosion;
        public static EffecterDef TM_HolyImplosion;
        public static EffecterDef TM_FearWave;
        public static EffecterDef TM_BFBEffecter;
        public static EffecterDef TM_BloodShieldEffecter;
        public static EffecterDef TM_InvisibilityEffecter;
        public static EffecterDef TM_AttractionEffecter;
        public static EffecterDef TM_AttractionEffecter_I;
        public static EffecterDef TM_AttractionEffecter_II;
        public static EffecterDef TM_AttractionEffecter_III;
        public static EffecterDef TM_ChiBurstED;
        public static EffecterDef TM_TimeAccelerationEffecter;
        public static EffecterDef TM_TimeAccelerationAreaEffecter;
        public static EffecterDef TM_TimeReverseEffecter;
        public static EffecterDef TM_RecallToED;
        public static EffecterDef TM_RecallFromED;
        public static EffecterDef TM_MagicCircleED;
        public static EffecterDef TM_CommanderOrderED;
        public static EffecterDef TM_HTL_EffecterED;
        public static EffecterDef TM_IgniteED;
        public static EffecterDef TM_SnapFreezeED;
        public static EffecterDef TM_SmallMagicCircleED;
        public static EffecterDef TM_LightBurstED;
        public static EffecterDef TM_RageWaveED;
        public static EffecterDef TM_ExplosionED;
        public static EffecterDef TM_MKWaveED;
        public static EffecterDef TM_FadeEffecterED;
        public static EffecterDef TM_FadeEffecter2ED;
        public static EffecterDef TM_AttractionEffecterSmall;

        //psychast copies, used with tm_motemaker.makeoverlay
        //adjusts def values on each cast
        public static ThingDef TM_Mote_PsycastPsychicEffect;
        public static ThingDef TM_Mote_PsycastPsychicLine;
        public static ThingDef TM_Mote_PsycastAreaEffect;

        //Apparel layers
        public static ApparelLayerDef TM_Cloak;
        public static ApparelLayerDef TM_Artifact;

        //Enchantments
        public static HediffDef TM_HediffEnchantment_maxMP;
        public static HediffDef TM_HediffEnchantment_coolDown;
        public static HediffDef TM_HediffEnchantment_mpCost;
        public static HediffDef TM_HediffEnchantment_mpRegenRate;
        public static HediffDef TM_HediffEnchantment_xpGain;
        public static HediffDef TM_HediffEnchantment_arcaneRes;
        public static HediffDef TM_HediffEnchantment_arcaneDmg;
        public static HediffDef TM_HediffEnchantment_arcaneSpectre;
        public static HediffDef TM_HediffEnchantment_phantomShift;
        public static HediffDef TM_HediffEnchantment_fireImmunity;

        //Jobs
        public static JobDef TMCastAbilityVerb;
        public static JobDef TMCastAbilitySelf;
        public static JobDef JobDriver_DoMagicBill;
        public static JobDef JobDriver_AssistMagicCircle;
        public static JobDef JobDriver_RemoveEnchantingGem;
        public static JobDef JobDriver_AddEnchantingGem;
        public static JobDef JobDriver_EnchantItem;
        public static JobDef JobDriver_SleepNow;
        public static JobDef JobDriver_Entertain;
        public static JobDef JobDriver_DoFor;
        public static JobDef JobDriver_SummonDemon;
        public static JobDef JobDriver_PsionicBarrier;
        public static JobDef JobDriver_TM_Teach;
        public static JobDef JobDriver_TM_GotoAndWait;
        public static JobDef JobDriver_GotoAndCast;
        public static JobDef JobDriver_Discord;
        public static JobDef JobDriver_GolemDespawn;
        public static JobDef JobDriver_MechaMine;
        public static JobDef JobDriver_FleshHarvest;
        public static JobDef JobDriver_FleshChop;
        public static JobDef JobDriver_GolemSelfTend;
        public static JobDef JobDriver_GolemAbilityJob;

        //Things
        public static ThingDef RawMagicyte;
        public static ThingDef TM_Plant_Paracyte;
        public static ThingDef TM_Syrrium;
        public static ThingDef TM_Plant_Briar;
        public static ThingDef TM_MageLightTD;
        public static ThingDef TM_SkeletonR;
        public static ThingDef TM_GiantSkeletonR;
        public static ThingDef TM_SkeletonLichR;
        public static ThingDef TM_Arcalleum;
        public static ThingDef TM_Manaweave;
        public static ThingDef TM_Demonhide;
        public static ThingDef TM_Demonscale;
        public static ThingDef TM_Artifact_ClassExtraction;
        public static ThingDef TM_MagicArtifact_MightEssence;
        public static ThingDef TM_MagicArtifact_MagicEssence;

        //Sounds
        public static SoundDef ItemEnchanted;
        public static SoundDef TM_Lightning;
        public static SoundDef TM_Gong;
        public static SoundDef TM_AirWoosh;
        public static SoundDef TM_Vibration;
        public static SoundDef TM_VibrationLow;
        public static SoundDef TM_Launch;
        public static SoundDef TM_SoftExplosion;
        public static SoundDef TM_BattleHymnSD;
        public static SoundDef TM_DemonDeath;
        public static SoundDef TM_Implosion;
        public static SoundDef TM_ThrumBow;
        public static SoundDef TM_FastReleaseSD;
        public static SoundDef TM_FireWooshSD;
        public static SoundDef TM_GaspingAir;
        public static SoundDef TM_MetalImpact;
        public static SoundDef TM_FireBombSD;
        public static SoundDef TM_WindLowSD;
        public static SoundDef TM_Thunder_OnMap;
        public static SoundDef TM_Thunder_OffMap;
        public static SoundDef TM_FlashBang;
        public static SoundDef TM_WolfHowl;
        public static SoundDef TM_FireBurn;
        public static SoundDef TM_Roar;
        public static SoundDef TM_Powerup;
        public static SoundDef TM_Scream;

        //Inspirations
        public static InspirationDef ID_Champion;
        public static InspirationDef ID_FarmingFrenzy;
        public static InspirationDef ID_MiningFrenzy;
        public static InspirationDef ID_Outgoing;
        public static InspirationDef ID_Introspection;
        public static InspirationDef ID_ManaRegen;
        public static InspirationDef Frenzy_Go;
        public static InspirationDef Frenzy_Shoot;
        public static InspirationDef ID_Enlightened;
        public static InspirationDef ID_ArcanePathways;

        //Mental States
        public static MentalStateDef TM_Berserk;
        public static MentalStateDef TM_PanicFlee;
        public static MentalStateDef TM_WanderPsychotic;
        public static MentalStateDef TM_WanderSad;
        public static MentalStateDef TM_WanderConfused;
        public static MentalStateDef WanderConfused;
        public static ThoughtDef TM_MemoryWipe;        

        //Joy kinds
        public static JoyKindDef Social;
        public static JoyKindDef Gaming_Dexterity;
        public static JoyKindDef Gaming_Cerebral;

        //Magic Work Defs
        public static PawnCapacityDef MagicManipulation;
        public static WorkTypeDef TM_Magic;

        //Structures
        public static AbilityUser.AbilityDef Forge_Verb;
        public static ThingDef TableArcaneForge;
        public static RecipeDef ArcaneForge_Replication;
        public static RecipeDef ArcaneForge_Replication_Restore;
        public static ThingDef TableMagicPrinter;
        public static ThingDef TableGemcutting;
        public static ThingDef TM_Portal;
        public static ThingDef TM_ArcaneCapacitor;
        public static ThingDef TM_DimensionalManaPocket;
        //public static ThingDef TM_MagicCircleBase;
        public static ThingDef TableMagicCircle;
        public static ThingDef TableSmallMagicCircle;

        //ThoughtDefs
        public static ThoughtDef TM_PhantomLimb;
        public static ThoughtDef TM_NeedTravel;
        //public static ThoughtDef AteMysteryMeatDirect;
        //public static ThoughtDef AteMysteryMeatAsIngredient;
        public static ThoughtDef TM_SeverMagic_ForApproveTD;
        public static ThoughtDef TM_SeverMagic_ForVeneratedTD;

        //WeatherDefs
        public static WeatherDef TM_HealingRainWD;

        //Factions
        public static FactionDef TM_SkeletalFaction;

        //NeedDefs
        public static NeedDef TM_Travel;

        //Stats
        public static StatDef ArmorRating_Alignment;        

        //Armor Categories
        public static DamageArmorCategoryDef Dark;
        public static DamageArmorCategoryDef Light;

        //CustomDefs
        public static TM_CustomDef TM_CustomDef;
        public static TMAbilityDef TM_CompVerb;         //used for secondary targets

        //Custom ThingCategories
        public static ThingCategoryDef TM_MagicItems;
        public static ThingCategoryDef TM_SkillBooks;
        public static ThingCategoryDef TM_Scrolls;
        public static ThingCategoryDef TM_Magicyte;
        public static ThingCategoryDef TM_MagicArtifacts;
        public static ThingCategoryDef TM_MagicEquipment;

        //HistoryEventDefs
        public static HistoryEventDef TM_OffensiveMagic;
        public static HistoryEventDef TM_KilledMage;
        public static HistoryEventDef TM_KilledFighter;
        public static HistoryEventDef TM_UsedMagic;
        public static HistoryEventDef TM_UsedManeuver;
        public static HistoryEventDef TM_KilledHumanlike;
        public static HistoryEventDef TM_SeverMagicEvent;
        public static HistoryEventDef TM_BestowMagicEvent;
        public static HistoryEventDef TM_BestowClassEvent;

        //Rituals
        public static JobDef TM_SeverMagic;
        public static HediffDef TM_MagicSeverenceHD;
        public static JobDef TM_BestowMagic;
        public static HediffDef TM_BestowMagicClassHD;
        public static JobDef TM_BestowClass;

        //Precept Defs
        public static PreceptDef TM_Mages_Abhorrent;
        public static PreceptDef TM_Mages_Disapprove;
        public static PreceptDef TM_Mages_Approve;
        public static PreceptDef TM_Mages_Venerated;

        //Precept Roles
        public static PreceptDef TM_IdeoRole_VoidSeeker;

        //FleckDefs
        public static FleckDef ElectricalSpark;
        public static FleckDef SparkFlash;

        //Golems
        public static ThingDef TM_MechaGolem;
        public static PawnKindDef TM_MechaGolemK;
        public static ThingDef TM_MechaGolem_Workstation;
        public static ThinkTreeDef TM_GolemMain;
        public static HediffDef TM_GolemHD;
        public static HediffDef TM_BullChargeHD;
        public static HediffDef TM_FleshGolem_BracerGuardHD;
        public static NeedDef TM_GolemEnergy;
        public static NeedDef TM_GolemRage;
        public static ThingDef TM_HollowGolem;
        public static PawnKindDef TM_HollowGolemK;
        public static LifeStageDef TM_HollowGolemLS;        
        public static HediffDef TM_DecayHD;
        public static JobDef JobDriver_GolemAttackStatic;
        public static ThingDef Mote_Golem_LSFA_South;
        public static ThingDef Mote_Golem_LSFA_East;
        public static ThingDef Mote_Golem_LSFA_West;
        public static HediffDef TM_DeathFieldHD;
        public static ThingDef TM_HollowGolem_Workstation;
        public static TMDefs.TM_GolemUpgradeDef TM_Golem_HollowOrbOfExtinguishedFlames;
        //public static TMDefs.TM_GolemAbility TM_Golem_GatlingCannon;

    }
}
