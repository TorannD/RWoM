using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedBolt
    {
        private static readonly (string job, Func<TMAbilityDef> spell, Type effect)[] Families =
            new (string job, Func<TMAbilityDef> spell, Type effect)[] {
            ("TM_SpiritFirebolt", () => TorannMagicDefOf.TM_Firebolt, typeof(Projectile_Firebolt)),
            ("TM_SpiritFireclaw", () => TorannMagicDefOf.TM_Fireclaw, typeof(Projectile_Fireclaw)),
            ("TM_SpiritIcebolt", () => TorannMagicDefOf.TM_Icebolt, typeof(Projectile_Icebolt)),
            ("TM_SpiritFireball", () => TorannMagicDefOf.TM_Fireball, typeof(Projectile_Fireball)),
            ("TM_SpiritSnowball", () => TorannMagicDefOf.TM_Snowball, typeof(Projectile_Snowball)),
            ("TM_SpiritFirestorm", () => TorannMagicDefOf.TM_Firestorm, typeof(Projectile_Firestorm)),
            ("TM_SpiritBlizzard", () => TorannMagicDefOf.TM_Blizzard, typeof(Projectile_Blizzard)),
            ("TM_SpiritFrostRay", () => TorannMagicDefOf.TM_FrostRay, typeof(Projectile_SpiritRay)),
            ("TM_SpiritFrostRay_I", () => TorannMagicDefOf.TM_FrostRay_I, typeof(Projectile_SpiritRay)),
            ("TM_SpiritFrostRay_II", () => TorannMagicDefOf.TM_FrostRay_II, typeof(Projectile_SpiritRay)),
            ("TM_SpiritFrostRay_III", () => TorannMagicDefOf.TM_FrostRay_III, typeof(Projectile_SpiritRay)),
            ("TM_SpiritHeal", () => TorannMagicDefOf.TM_Heal, typeof(Verb_Heal)),
            ("TM_SpiritAdvancedHeal", () => TorannMagicDefOf.TM_AdvancedHeal, typeof(Verb_AdvancedHeal)),
            ("TM_SpiritPurify", () => TorannMagicDefOf.TM_Purify, typeof(Verb_Purify)),
            ("TM_SpiritRegenerate", () => TorannMagicDefOf.TM_Regenerate, typeof(Verb_Regenerate)),
            ("TM_SpiritCureDisease", () => TorannMagicDefOf.TM_CureDisease, typeof(Verb_CureDisease)),
            ("TM_SpiritRegrowLimb", () => TorannMagicDefOf.TM_RegrowLimb, typeof(Verb_RegrowLimb)),
            ("TM_SpiritBestowMight", () => TorannMagicDefOf.TM_BestowMight, typeof(Projectile_SpiritNativeAbility)),
            ("TM_SpiritBestowMight_I", () => TorannMagicDefOf.TM_BestowMight_I, typeof(Projectile_SpiritNativeAbility)),
            ("TM_SpiritBestowMight_II", () => TorannMagicDefOf.TM_BestowMight_II, typeof(Projectile_SpiritNativeAbility)),
            ("TM_SpiritBestowMight_III", () => TorannMagicDefOf.TM_BestowMight_III, typeof(Projectile_SpiritNativeAbility)),
            ("TM_SpiritDeathBolt", () => TorannMagicDefOf.TM_DeathBolt, typeof(Projectile_HostedDeathBolt)),
            ("TM_SpiritDeathBolt_I", () => TorannMagicDefOf.TM_DeathBolt_I, typeof(Projectile_HostedDeathBolt)),
            ("TM_SpiritDeathBolt_II", () => TorannMagicDefOf.TM_DeathBolt_II, typeof(Projectile_HostedDeathBolt)),
            ("TM_SpiritDeathBolt_III", () => TorannMagicDefOf.TM_DeathBolt_III, typeof(Projectile_HostedDeathBolt)),
            ("TM_SpiritFogOfTorment", () => TorannMagicDefOf.TM_FogOfTorment, typeof(Projectile_FogOfTorment)),
            ("TM_SpiritPoison", () => TorannMagicDefOf.TM_Poison, typeof(Projectile_Poison)),
            ("TM_SpiritSootheAnimal", () => TorannMagicDefOf.TM_SootheAnimal, typeof(Verb_SootheAnimal)),
            ("TM_SpiritSootheAnimal_I", () => TorannMagicDefOf.TM_SootheAnimal_I, typeof(Verb_SootheAnimal)),
            ("TM_SpiritSootheAnimal_II", () => TorannMagicDefOf.TM_SootheAnimal_II, typeof(Verb_SootheAnimal)),
            ("TM_SpiritSootheAnimal_III", () => TorannMagicDefOf.TM_SootheAnimal_III, typeof(Verb_SootheAnimal)),
            ("TM_SpiritRainmaker", () => TorannMagicDefOf.TM_Rainmaker, typeof(Verb_Rainmaker)),
            ("TM_SpiritResurrection", () => TorannMagicDefOf.TM_Resurrection, typeof(Projectile_Resurrection)),
            ("TM_SpiritHealingCircle", () => TorannMagicDefOf.TM_HealingCircle, typeof(Projectile_HealingCircle)),
            ("TM_SpiritHealingCircle_I", () => TorannMagicDefOf.TM_HealingCircle_I, typeof(Projectile_HealingCircle)),
            ("TM_SpiritHealingCircle_II", () => TorannMagicDefOf.TM_HealingCircle_II, typeof(Projectile_HealingCircle)),
            ("TM_SpiritHealingCircle_III", () => TorannMagicDefOf.TM_HealingCircle_III, typeof(Projectile_HealingCircle)),
            ("TM_SpiritRaiseUndead", () => TorannMagicDefOf.TM_RaiseUndead, typeof(Projectile_RaiseUndead)),
            ("TM_SpiritCorpseExplosion", () => TorannMagicDefOf.TM_CorpseExplosion, typeof(Projectile_CorpseExplosion)),
            ("TM_SpiritCorpseExplosion_I", () => TorannMagicDefOf.TM_CorpseExplosion_I, typeof(Projectile_CorpseExplosion)),
            ("TM_SpiritCorpseExplosion_II", () => TorannMagicDefOf.TM_CorpseExplosion_II, typeof(Projectile_CorpseExplosion)),
            ("TM_SpiritCorpseExplosion_III", () => TorannMagicDefOf.TM_CorpseExplosion_III, typeof(Projectile_CorpseExplosion)),
            ("TM_SpiritConsumeCorpse", () => TorannMagicDefOf.TM_ConsumeCorpse, typeof(Verb_ConsumeCorpse)),
            ("TM_SpiritConsumeCorpse_I", () => TorannMagicDefOf.TM_ConsumeCorpse_I, typeof(Verb_ConsumeCorpse)),
            ("TM_SpiritConsumeCorpse_II", () => TorannMagicDefOf.TM_ConsumeCorpse_II, typeof(Verb_ConsumeCorpse)),
            ("TM_SpiritConsumeCorpse_III", () => TorannMagicDefOf.TM_ConsumeCorpse_III, typeof(Verb_ConsumeCorpse)),
            ("TM_SpiritConsumeCorpse_Mass", () => TorannMagicDefOf.TM_ConsumeCorpse_Mass, typeof(Verb_ConsumeCorpse_Mass)),
            ("TM_SpiritDeathMark", () => TorannMagicDefOf.TM_DeathMark, typeof(Verb_DeathMark)),
            ("TM_SpiritDeathMark_I", () => TorannMagicDefOf.TM_DeathMark_I, typeof(Verb_DeathMark)),
            ("TM_SpiritDeathMark_II", () => TorannMagicDefOf.TM_DeathMark_II, typeof(Verb_DeathMark)),
            ("TM_SpiritDeathMark_III", () => TorannMagicDefOf.TM_DeathMark_III, typeof(Verb_DeathMark)),
            ("TM_SpiritValiantCharge", () => TorannMagicDefOf.TM_ValiantCharge, typeof(Projectile_SpiritNativeAbility)),
            ("TM_SpiritOverwhelm", () => TorannMagicDefOf.TM_Overwhelm, typeof(Projectile_Overwhelm)),
            ("TM_SpiritHolyWrath", () => TorannMagicDefOf.TM_HolyWrath, typeof(Projectile_HolyWrath)),
            ("TM_SpiritOrbitalStrike", () => TorannMagicDefOf.TM_OrbitalStrike, typeof(Projectile_OrbitalStrike)),
            ("TM_SpiritOrbitalStrike_I", () => TorannMagicDefOf.TM_OrbitalStrike_I, typeof(Projectile_OrbitalStrike)),
            ("TM_SpiritOrbitalStrike_II", () => TorannMagicDefOf.TM_OrbitalStrike_II, typeof(Projectile_OrbitalStrike)),
            ("TM_SpiritOrbitalStrike_III", () => TorannMagicDefOf.TM_OrbitalStrike_III, typeof(Projectile_OrbitalStrike)),
            ("TM_SpiritOverdrive", () => TorannMagicDefOf.TM_Overdrive, typeof(Verb_Overdrive)),
            ("TM_SpiritSabotage", () => TorannMagicDefOf.TM_Sabotage, typeof(Projectile_Sabotage)),
            ("TM_SpiritSunfire", () => TorannMagicDefOf.TM_Sunfire, typeof(Projectile_Sunfire)),
            ("TM_SpiritLightBurst", () => TorannMagicDefOf.TM_LightBurst, typeof(Verb_LightBurst)),
            ("TM_SpiritLightLance", () => TorannMagicDefOf.TM_LightLance, typeof(Effect_LightLance)),
            ("TM_SpiritLightSkip", () => TorannMagicDefOf.TM_LightSkip, typeof(Projectile_LightSkip)),
            ("TM_SpiritLightSkipMass", () => TorannMagicDefOf.TM_LightSkipMass, typeof(Projectile_LightSkipMass)),
            ("TM_SpiritRefraction", () => TorannMagicDefOf.TM_Refraction, typeof(Projectile_Refraction)),
            ("TM_SpiritLullaby", () => TorannMagicDefOf.TM_Lullaby, typeof(Verb_Lullaby)),
            ("TM_SpiritLullaby_I", () => TorannMagicDefOf.TM_Lullaby_I, typeof(Verb_Lullaby)),
            ("TM_SpiritLullaby_II", () => TorannMagicDefOf.TM_Lullaby_II, typeof(Verb_Lullaby)),
            ("TM_SpiritLullaby_III", () => TorannMagicDefOf.TM_Lullaby_III, typeof(Verb_Lullaby)),
            ("TM_SpiritIgniteBlood", () => TorannMagicDefOf.TM_IgniteBlood, typeof(Projectile_IgniteBlood)),
            ("TM_SpiritRend", () => TorannMagicDefOf.TM_Rend, typeof(Verb_Rend)),
            ("TM_SpiritRend_I", () => TorannMagicDefOf.TM_Rend_I, typeof(Verb_Rend)),
            ("TM_SpiritRend_II", () => TorannMagicDefOf.TM_Rend_II, typeof(Verb_Rend)),
            ("TM_SpiritRend_III", () => TorannMagicDefOf.TM_Rend_III, typeof(Verb_Rend)),
            ("TM_SpiritEarthSprites", () => TorannMagicDefOf.TM_EarthSprites, typeof(Verb_EarthSprites)),
            ("TM_SpiritDismissEarthSprites", () => TorannMagicDefOf.TM_DismissEarthSprites, typeof(Verb_DismissEarthSprites)),
            ("TM_SpiritChronostaticField", () => TorannMagicDefOf.TM_ChronostaticField, typeof(Projectile_ChronostaticField)),
            ("TM_SpiritChronostaticField_I", () => TorannMagicDefOf.TM_ChronostaticField_I, typeof(Projectile_ChronostaticField)),
            ("TM_SpiritChronostaticField_II", () => TorannMagicDefOf.TM_ChronostaticField_II, typeof(Projectile_ChronostaticField)),
            ("TM_SpiritChronostaticField_III", () => TorannMagicDefOf.TM_ChronostaticField_III, typeof(Projectile_ChronostaticField)),
            ("TM_SpiritEnchantedBody", () => TorannMagicDefOf.TM_EnchantedBody, typeof(Verb_EnchantedBody)),
            ("TM_SpiritEnchantedAura", () => TorannMagicDefOf.TM_EnchantedAura, typeof(Verb_EnchantedAura)),
            ("TM_SpiritPolymorph", () => TorannMagicDefOf.TM_Polymorph, typeof(Verb_Polymorph)),
            ("TM_SpiritPolymorph_I", () => TorannMagicDefOf.TM_Polymorph_I, typeof(Verb_Polymorph)),
            ("TM_SpiritPolymorph_II", () => TorannMagicDefOf.TM_Polymorph_II, typeof(Verb_Polymorph)),
            ("TM_SpiritPolymorph_III", () => TorannMagicDefOf.TM_Polymorph_III, typeof(Verb_Polymorph)),
            ("TM_SpiritShapeshift", () => TorannMagicDefOf.TM_Shapeshift, typeof(Verb_Shapeshift)),
            ("TM_SpiritShapeshiftDW", () => TorannMagicDefOf.TM_ShapeshiftDW, typeof(Verb_ShapeshiftDW)),
            ("TM_SpiritAwarenessBrand", () => TorannMagicDefOf.TM_AwarenessBrand, typeof(Verb_BrandAwareness)),
            ("TM_SpiritEmotionBrand", () => TorannMagicDefOf.TM_EmotionBrand, typeof(Verb_BrandEmotion)),
            ("TM_SpiritFitnessBrand", () => TorannMagicDefOf.TM_FitnessBrand, typeof(Verb_BrandFitness)),
            ("TM_SpiritProtectionBrand", () => TorannMagicDefOf.TM_ProtectionBrand, typeof(Verb_BrandProtection)),
            ("TM_SpiritVitalityBrand", () => TorannMagicDefOf.TM_VitalityBrand, typeof(Verb_BrandVitality)),
            ("TM_SpiritSiphonBrand", () => TorannMagicDefOf.TM_SiphonBrand, typeof(Verb_BrandSiphon)),
            ("TM_SpiritSuppressiveAura", () => TorannMagicDefOf.TM_SuppressiveAura, typeof(Verb_ToggleHediff)),
            ("TM_SpiritShadowBolt", () => TorannMagicDefOf.TM_ShadowBolt, typeof(Projectile_HostedShadowBolt)),
            ("TM_SpiritShadowBolt_I", () => TorannMagicDefOf.TM_ShadowBolt_I, typeof(Projectile_HostedShadowBolt)),
            ("TM_SpiritShadowBolt_II", () => TorannMagicDefOf.TM_ShadowBolt_II, typeof(Projectile_HostedShadowBolt)),
            ("TM_SpiritShadowBolt_III", () => TorannMagicDefOf.TM_ShadowBolt_III, typeof(Projectile_HostedShadowBolt)),
            ("TM_SpiritShadowStep", () => TorannMagicDefOf.TM_ShadowStep, typeof(Verb_ShadowStep)),
            ("TM_SpiritShadowCall", () => TorannMagicDefOf.TM_ShadowCall, typeof(Verb_ShadowCall)),
            ("TM_SpiritRepulsion", () => TorannMagicDefOf.TM_Repulsion, typeof(Projectile_Repulsion)),
            ("TM_SpiritRepulsion_I", () => TorannMagicDefOf.TM_Repulsion_I, typeof(Projectile_Repulsion)),
            ("TM_SpiritRepulsion_II", () => TorannMagicDefOf.TM_Repulsion_II, typeof(Projectile_Repulsion)),
            ("TM_SpiritRepulsion_III", () => TorannMagicDefOf.TM_Repulsion_III, typeof(Projectile_Repulsion)),
            ("TM_SpiritAttraction", () => TorannMagicDefOf.TM_Attraction, typeof(Projectile_Attraction)),
            ("TM_SpiritAttraction_I", () => TorannMagicDefOf.TM_Attraction_I, typeof(Projectile_Attraction)),
            ("TM_SpiritAttraction_II", () => TorannMagicDefOf.TM_Attraction_II, typeof(Projectile_Attraction)),
            ("TM_SpiritAttraction_III", () => TorannMagicDefOf.TM_Attraction_III, typeof(Projectile_Attraction)),
            ("TM_SpiritSummonDemon", () => TorannMagicDefOf.TM_SummonDemon, typeof(Verb_SummonDemon)),
            ("TM_SpiritScorn", () => TorannMagicDefOf.TM_Scorn, typeof(Projectile_Scorn)),
            ("TM_SpiritPsychicShock", () => TorannMagicDefOf.TM_PsychicShock, typeof(Projectile_PsychicShock)),
            ("TM_SpiritDominate", () => TorannMagicDefOf.TM_Dominate, typeof(Verb_Dominate)),
            ("TM_SpiritSoulBond", () => TorannMagicDefOf.TM_SoulBond, typeof(Verb_SoulBond)),
            ("TM_SpiritSymbiosis", () => TorannMagicDefOf.TM_Symbiosis, typeof(Verb_Symbiosis)),
            ("TM_SpiritFadeEmotions", () => TorannMagicDefOf.TM_FadeEmotions, typeof(Verb_FadeEmotions)),
            ("TM_SpiritHarvestPassion", () => TorannMagicDefOf.TM_HarvestPassion, typeof(Verb_HarvestPassion)),
            ("TM_SpiritIncitePassion", () => TorannMagicDefOf.TM_IncitePassion, typeof(Verb_IncitePassion)),
            ("TM_SpiritMindKiller", () => TorannMagicDefOf.TM_MindKiller, typeof(Verb_MindKiller)),
            ("TM_SpiritLivingWall", () => TorannMagicDefOf.TM_LivingWall, typeof(Verb_LivingWall)),
            ("TM_SpiritDispelLivingWall", () => TorannMagicDefOf.TM_DispelLivingWall, typeof(Verb_DispelLivingWall)),
            ("TM_SpiritSigilSurge", () => TorannMagicDefOf.TM_SigilSurge, typeof(Verb_SigilSurge)),
            ("TM_SpiritSigilDrain", () => TorannMagicDefOf.TM_SigilDrain, typeof(Verb_SigilDrain)),
            ("TM_SpiritDispelBranding", () => TorannMagicDefOf.TM_DispelBranding, typeof(Verb_DispelBranding)),
            ("TM_SpiritEnchantWeapon", () => TorannMagicDefOf.TM_EnchantWeapon, typeof(Verb_EnchantWeapon)),
            ("TM_SpiritDispelEnchantWeapon", () => TorannMagicDefOf.TM_DispelEnchantWeapon, typeof(Verb_DispelEnchantWeapon)),
            ("TM_SpiritEnchanterStone", () => TorannMagicDefOf.TM_EnchanterStone, typeof(Verb_EnchanterStone)),
            ("TM_SpiritDismissEnchanterStones", () => TorannMagicDefOf.TM_DismissEnchanterStones, typeof(Verb_DismissEnchanterStones)),
            ("TM_SpiritTransmutate", () => TorannMagicDefOf.TM_Transmutate, typeof(Verb_Transmutate)),
            ("TM_SpiritSummonTotemEarth", () => TorannMagicDefOf.TM_SummonTotemEarth, typeof(Verb_SummonTotemEarth)),
            ("TM_SpiritSummonTotemLightning", () => TorannMagicDefOf.TM_SummonTotemLightning, typeof(Verb_SummonTotemLightning)),
            ("TM_SpiritSummonTotemHealing", () => TorannMagicDefOf.TM_SummonTotemHealing, typeof(Verb_SummonTotemHealing)),
            ("TM_SpiritHex", () => TorannMagicDefOf.TM_Hex, typeof(Verb_Hex)),
            ("TM_SpiritHex_I", () => TorannMagicDefOf.TM_Hex_I, typeof(Verb_Hex)),
            ("TM_SpiritHex_II", () => TorannMagicDefOf.TM_Hex_II, typeof(Verb_Hex)),
            ("TM_SpiritHex_III", () => TorannMagicDefOf.TM_Hex_III, typeof(Verb_Hex)),
            ("TM_SpiritHex_Pain", () => TorannMagicDefOf.TM_Hex_Pain, typeof(Verb_Hex_Pain)),
            ("TM_SpiritHex_CriticalFail", () => TorannMagicDefOf.TM_Hex_CriticalFail, typeof(Verb_Hex_CriticalFail)),
            ("TM_SpiritHex_MentalAssault", () => TorannMagicDefOf.TM_Hex_MentalAssault, typeof(Verb_Hex_MentalAssault)),
            ("TM_SpiritGuardianSpirit", () => TorannMagicDefOf.TM_GuardianSpirit, typeof(Verb_SummonGuardianSpirit)),
            ("TM_SpiritDismissGuardianSpirit", () => TorannMagicDefOf.TM_DismissGuardianSpirit, typeof(Verb_DismissGuardianSpirit)),
            ("TM_SpiritEnrage", () => TorannMagicDefOf.TM_Enrage, typeof(Verb_Enrage)),
            ("TM_SpiritChainLightning", () => TorannMagicDefOf.TM_ChainLightning, typeof(Projectile_ChainLightning)),
            ("TM_SpiritTimeMark", () => TorannMagicDefOf.TM_TimeMark, typeof(Verb_TimeMark)),
            ("TM_SpiritRecall", () => TorannMagicDefOf.TM_Recall, typeof(Verb_Recall)),
            ("TM_SpiritReverseTime", () => TorannMagicDefOf.TM_ReverseTime, typeof(Verb_ReverseTime)),
            ("TM_SpiritAccelerateTime", () => TorannMagicDefOf.TM_AccelerateTime, typeof(Projectile_AccelerateTime)),
            ("TM_SpiritAlterFate", () => TorannMagicDefOf.TM_AlterFate, typeof(Verb_AlterFate)),
            ("TM_SpiritPrediction", () => TorannMagicDefOf.TM_Prediction, typeof(Verb_Prediction)),
            ("TM_SpiritMeteor", () => TorannMagicDefOf.TM_Meteor, typeof(Verb_Meteor)),
            ("TM_SpiritMeteor_I", () => TorannMagicDefOf.TM_Meteor_I, typeof(Verb_Meteor)),
            ("TM_SpiritMeteor_II", () => TorannMagicDefOf.TM_Meteor_II, typeof(Verb_Meteor)),
            ("TM_SpiritMeteor_III", () => TorannMagicDefOf.TM_Meteor_III, typeof(Verb_Meteor)),
            ("TM_SpiritSentinel", () => TorannMagicDefOf.TM_Sentinel, typeof(Projectile_Sentinel)),
            ("TM_SpiritShatterSentinel", () => TorannMagicDefOf.TM_ShatterSentinel, typeof(Verb_ShatterSentinel)),
            ("TM_SpiritEarthernHammer", () => TorannMagicDefOf.TM_EarthernHammer, typeof(Projectile_EarthernHammer)),
            ("TM_SpiritEncase", () => TorannMagicDefOf.TM_Encase, typeof(Projectile_Encase)),
            ("TM_SpiritEncase_I", () => TorannMagicDefOf.TM_Encase_I, typeof(Projectile_Encase)),
            ("TM_SpiritEncase_II", () => TorannMagicDefOf.TM_Encase_II, typeof(Projectile_Encase)),
            ("TM_SpiritEncase_III", () => TorannMagicDefOf.TM_Encase_III, typeof(Projectile_Encase)),
            ("TM_SpiritStoneskin", () => TorannMagicDefOf.TM_Stoneskin, typeof(Verb_Stoneskin)),
            ("TM_SpiritDispelStoneskin", () => TorannMagicDefOf.TM_DispelStoneskin, typeof(Verb_DispelStoneskin)),
            ("TM_SpiritBloodMoon", () => TorannMagicDefOf.TM_BloodMoon, typeof(Projectile_BloodMoon)),
            ("TM_SpiritBloodMoon_I", () => TorannMagicDefOf.TM_BloodMoon_I, typeof(Projectile_BloodMoon)),
            ("TM_SpiritBloodMoon_II", () => TorannMagicDefOf.TM_BloodMoon_II, typeof(Projectile_BloodMoon)),
            ("TM_SpiritBloodMoon_III", () => TorannMagicDefOf.TM_BloodMoon_III, typeof(Projectile_BloodMoon)),
            ("TM_SpiritBloodForBlood", () => TorannMagicDefOf.TM_BloodForBlood, typeof(Verb_BloodForBlood)),
            ("TM_SpiritBloodShield", () => TorannMagicDefOf.TM_BloodShield, typeof(Verb_BloodShield)),
            ("TM_SpiritBattleHymn", () => TorannMagicDefOf.TM_BattleHymn, typeof(Verb_BattleHymn)),
            ("TM_SpiritEntertain", () => TorannMagicDefOf.TM_Entertain, typeof(Verb_Entertain)),
            ("TM_SpiritSoL_CreateLight", () => TorannMagicDefOf.TM_SoL_CreateLight, typeof(Verb_SoL_CreateLight)),
            ("TM_SpiritSoL_Equalize", () => TorannMagicDefOf.TM_SoL_Equalize, typeof(Verb_SoL_Equalize)),
            ("TM_SpiritSpiritOfLight", () => TorannMagicDefOf.TM_SpiritOfLight, typeof(Effect_SpiritOfLight)),
            ("TM_SpiritSunfire_I", () => TorannMagicDefOf.TM_Sunfire_I, typeof(Projectile_Sunfire)),
            ("TM_SpiritSunfire_II", () => TorannMagicDefOf.TM_Sunfire_II, typeof(Projectile_Sunfire)),
            ("TM_SpiritSunfire_III", () => TorannMagicDefOf.TM_Sunfire_III, typeof(Projectile_Sunfire)),
            ("TM_SpiritNanoStimulant", () => TorannMagicDefOf.TM_NanoStimulant, typeof(Verb_NanoStimulant)),
            ("TM_SpiritTechnoWeapon", () => TorannMagicDefOf.TM_TechnoWeapon, typeof(Verb_TechnoWeapon)),
            ("TM_SpiritTechnoTurret", () => TorannMagicDefOf.TM_TechnoTurret, typeof(Projectile_TechnoTurret)),
            ("TM_SpiritTechnoShield", () => TorannMagicDefOf.TM_TechnoShield, typeof(Verb_TechnoShield)),
            ("TM_SpiritLightSkipGlobal", () => TorannMagicDefOf.TM_LightSkipGlobal, typeof(Verb_LightSkipGlobal)),
            ("TM_SpiritFoldReality", () => TorannMagicDefOf.TM_FoldReality, typeof(Verb_FoldReality)),
            ("TM_SpiritTeleport", () => TorannMagicDefOf.TM_Teleport, typeof(Projectile_Teleport)),
            ("TM_SpiritBlink", () => TorannMagicDefOf.TM_Blink, typeof(Verb_Blink)),
            ("TM_SpiritBlink_I", () => TorannMagicDefOf.TM_Blink_I, typeof(Verb_Blink)),
            ("TM_SpiritBlink_II", () => TorannMagicDefOf.TM_Blink_II, typeof(Verb_Blink)),
            ("TM_SpiritBlink_III", () => TorannMagicDefOf.TM_Blink_III, typeof(Verb_Blink)),
            ("TM_SpiritSummon", () => TorannMagicDefOf.TM_Summon, typeof(Verb_Summon)),
            ("TM_SpiritSummon_I", () => TorannMagicDefOf.TM_Summon_I, typeof(Verb_Summon)),
            ("TM_SpiritSummon_II", () => TorannMagicDefOf.TM_Summon_II, typeof(Verb_Summon)),
            ("TM_SpiritSummon_III", () => TorannMagicDefOf.TM_Summon_III, typeof(Verb_Summon)),
            ("TM_SpiritLightningStorm", () => TorannMagicDefOf.TM_LightningStorm, typeof(Projectile_LightningStorm)),
            ("TM_SpiritAMP", () => TorannMagicDefOf.TM_AMP, typeof(Projectile_SpiritAmp)),
            ("TM_SpiritAMP_I", () => TorannMagicDefOf.TM_AMP_I, typeof(Projectile_SpiritAmp)),
            ("TM_SpiritAMP_II", () => TorannMagicDefOf.TM_AMP_II, typeof(Projectile_SpiritAmp)),
            ("TM_SpiritAMP_III", () => TorannMagicDefOf.TM_AMP_III, typeof(Projectile_SpiritAmp)),
            ("TM_SpiritLightningBolt", () => TorannMagicDefOf.TM_LightningBolt, typeof(Laser_LightningBolt)),
            ("TM_SpiritLightningCloud", () => TorannMagicDefOf.TM_LightningCloud, typeof(Projectile_LightningCloud)),
            ("TM_SpiritMagicMissile", () => TorannMagicDefOf.TM_MagicMissile, typeof(Projectile_SpiritMissile)),
            ("TM_SpiritMagicMissile_I", () => TorannMagicDefOf.TM_MagicMissile_I, typeof(Projectile_SpiritMissile)),
            ("TM_SpiritMagicMissile_II", () => TorannMagicDefOf.TM_MagicMissile_II, typeof(Projectile_SpiritMissile)),
            ("TM_SpiritMagicMissile_III", () => TorannMagicDefOf.TM_MagicMissile_III, typeof(Projectile_SpiritMissile)),
            ("TM_SpiritP_RayofHope", () => TorannMagicDefOf.TM_P_RayofHope, typeof(Verb_ApplyAura)),
            ("TM_SpiritP_RayofHope_I", () => TorannMagicDefOf.TM_P_RayofHope_I, typeof(Verb_ApplyAura)),
            ("TM_SpiritP_RayofHope_II", () => TorannMagicDefOf.TM_P_RayofHope_II, typeof(Verb_ApplyAura)),
            ("TM_SpiritP_RayofHope_III", () => TorannMagicDefOf.TM_P_RayofHope_III, typeof(Verb_ApplyAura)),
            ("TM_SpiritRayofHope", () => TorannMagicDefOf.TM_RayofHope, typeof(Verb_ApplyAura)),
            ("TM_SpiritRayofHope_I", () => TorannMagicDefOf.TM_RayofHope_I, typeof(Verb_ApplyAura)),
            ("TM_SpiritRayofHope_II", () => TorannMagicDefOf.TM_RayofHope_II, typeof(Verb_ApplyAura)),
            ("TM_SpiritRayofHope_III", () => TorannMagicDefOf.TM_RayofHope_III, typeof(Verb_ApplyAura)),
            ("TM_SpiritShadow", () => TorannMagicDefOf.TM_Shadow, typeof(Verb_ApplyAura)),
            ("TM_SpiritShadow_I", () => TorannMagicDefOf.TM_Shadow_I, typeof(Verb_ApplyAura)),
            ("TM_SpiritShadow_II", () => TorannMagicDefOf.TM_Shadow_II, typeof(Verb_ApplyAura)),
            ("TM_SpiritShadow_III", () => TorannMagicDefOf.TM_Shadow_III, typeof(Verb_ApplyAura)),
            ("TM_SpiritSoothe", () => TorannMagicDefOf.TM_Soothe, typeof(Verb_ApplyAura)),
            ("TM_SpiritSoothe_I", () => TorannMagicDefOf.TM_Soothe_I, typeof(Verb_ApplyAura)),
            ("TM_SpiritSoothe_II", () => TorannMagicDefOf.TM_Soothe_II, typeof(Verb_ApplyAura)),
            ("TM_SpiritSoothe_III", () => TorannMagicDefOf.TM_Soothe_III, typeof(Verb_ApplyAura))
        }.Concat(SpiritHostedSummons.Families).ToArray();
        internal static TMAbilityDef Spell(JobDef job) => Families.FirstOrDefault(f => f.job == job?.defName).spell?.Invoke();

        internal static MagicAbility Find(Pawn body, Pawn soul, TMAbilityDef def)
        {
            if (def == null || !Families.Any(f => f.spell() == def) ||
                !SpiritRebirthState.TryGetPossessingSpirit(body, out var held) || held != soul ||
                !SpiritRebirthState.TryGet(soul, out var state) || state.DepletionPending) return null;
            string baseName = TMDefs.TM_AutocastDefaults.BaseName(def.defName);
            bool SameRankFamily(AbilityUser.AbilityDef other) => other == def ||
                other?.defName != null && TMDefs.TM_AutocastDefaults.BaseName(other.defName) == baseName && Families.Any(f => f.spell() == other);
            bool HasLocal(CompAbilityUser comp) => comp?.AbilityData.AllPowers.Any(a => SameRankFamily(a?.Def)) == true;
            if (HasLocal(body.GetCompAbilityUserMagic()) || HasLocal(body.GetCompAbilityUserMight())) return null;
            if (def.MainVerb?.verbClass == typeof(Verb_ApplyAura) && SpiritHostedAura.HasLocalEquivalent(body, def)) return null;
            var magic = soul.GetCompAbilityUserMagic();
            if (magic?.ExistingMagicData == null) return null;
            // A legacy save may still contain multiple runtime ranks. Only its highest
            // already granted rank can supply a command; no owner data is rewritten.
            if (magic.AbilityData.Powers.Any(a => a?.Def?.defName != null && a.Def.defName.Length > def.defName.Length &&
                ReferenceEquals(a.AbilityUser, magic) && SameRankFamily(a.Def))) return null;
            var matches = magic.AbilityData.Powers.OfType<MagicAbility>()
                .Where(a => a.Def == def && ReferenceEquals(a.AbilityUser, magic)).Take(2).ToList();
            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool DeathBolt(TMAbilityDef def) => def?.MainVerb?.verbClass == typeof(Effect_DeathBolt)
            && def.MainVerb.defaultProjectile?.thingClass == typeof(Projectile_HostedDeathBolt);

        internal static bool IsBurst(TMAbilityDef def) => DeathBolt(def) || def?.MainVerb?.verbClass == typeof(Effect_LightLance) || def?.MainVerb?.verbClass == typeof(Verb_LightBurst) || def?.MainVerb?.verbClass == typeof(Verb_UseAbility_TrueBurst)
            && (def.MainVerb.defaultProjectile?.thingClass == typeof(Projectile_SpiritMissile)
                || def.MainVerb.defaultProjectile?.thingClass == typeof(Projectile_SpiritRay) && def.MainVerb.burstShotCount > 1);

        internal static bool Ready(Pawn body, MagicAbility power, bool paidBurst = false)
        {
            if (power == null || !body.Spawned || body.Dead || body.Faction?.IsPlayer != true || body.IsPrisoner ||
                power.Pawn.Faction?.IsPlayer != true || body.Downed || body.InMentalState ||
                (!paidBurst && power.CooldownTicksLeft > 0) ||
                body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_MuteHD)) return false;
            var def = power.magicDef;
            if (def.MainVerb == null || def.MainVerb.isViolent && body.WorkTagIsDisabled(WorkTags.Violent) ||
                !Families.Any(f => f.spell() == def && (f.effect == def.MainVerb.verbClass || (DeathBolt(def) && f.effect == typeof(Projectile_HostedDeathBolt)) ||
                    ((def.MainVerb.verbClass == typeof(Verb_UseAbility) || def.MainVerb.verbClass == typeof(Effect_ValiantCharge) || def.MainVerb.verbClass == typeof(Verb_SB) ||
                    def.MainVerb.verbClass == typeof(Verb_Scorn) || def.MainVerb.verbClass == typeof(Verb_Attraction) || def.MainVerb.verbClass == typeof(Effect_ShadowBolt) || def.MainVerb.verbClass == typeof(Verb_LightSkip) || def.MainVerb.verbClass == typeof(Verb_BLOS) || def.MainVerb.verbClass == typeof(Verb_FogOfTorment) || def.MainVerb.verbClass == typeof(Verb_LightningCloud) || typeof(Verb_RankedStormField).IsAssignableFrom(def.MainVerb.verbClass) ||
                    def.MainVerb.verbClass == typeof(Verb_UseAbility_TrueBurst)) &&
                    f.effect == def.MainVerb.defaultProjectile?.thingClass &&
                    (typeof(Projectile_SpiritNativeAbility).IsAssignableFrom(f.effect) || typeof(Projectile_SpiritBolt).IsAssignableFrom(f.effect) || typeof(Projectile_SpiritRay).IsAssignableFrom(f.effect) || typeof(Projectile_DemonField).IsAssignableFrom(f.effect) || f.effect == typeof(Projectile_ChronostaticField) || f.effect == typeof(Projectile_AccelerateTime) || f.effect == typeof(Projectile_Sentinel) || f.effect == typeof(Projectile_EarthernHammer) || f.effect == typeof(Projectile_Encase) || f.effect == typeof(Projectile_IgniteBlood) || f.effect == typeof(Projectile_BloodMoon) || SpiritHostedSummons.IsProjectile(f.effect))))) ||
                (def.MainVerb.burstShotCount != 1 && !IsBurst(def)) || def.bloodCost != 0 && def != TorannMagicDefOf.TM_BloodShield && def != TorannMagicDefOf.TM_IgniteBlood && def != TorannMagicDefOf.TM_BloodForBlood && def.MainVerb.verbClass != typeof(Verb_Rend) && def.MainVerb.defaultProjectile?.thingClass != typeof(Projectile_BloodMoon) || def.chiCost != 0 ||
                (def.requiredNeed != null && def != TorannMagicDefOf.TM_MindKiller && def != TorannMagicDefOf.TM_SuppressiveAura && def != TorannMagicDefOf.TM_IncitePassion && def != TorannMagicDefOf.TM_HarvestPassion && def != TorannMagicDefOf.TM_FadeEmotions && def != TorannMagicDefOf.TM_Symbiosis) ||
                def.requiredHediff != null || !def.consumeEnergy || power.MagicUser.Mana == null) return false;
            if (def == TorannMagicDefOf.TM_SummonDemon && !DemonRitual.CanStart(body,power.Pawn,out _,out _,out _)) return false;
            if (def == TorannMagicDefOf.TM_HarvestPassion && !body.Inspired) return false;
            if (def == TorannMagicDefOf.TM_IncitePassion && (body.IsSlave || power.Pawn.IsSlave)) return false;
            if (def == TorannMagicDefOf.TM_SuppressiveAura)
            {
                var aura = body.health.hediffSet.GetFirstHediffOfDef(def.abilityHediff);
                if (aura != null && aura.TryGetComp<HediffComp_SuppressiveAura>()?.SourceId != power.Pawn.thingIDNumber) return false;
            }
            if (def.requiredNeed != null)
            {
                var need = body.needs?.TryGetNeed(def.requiredNeed);
                float price = MagicAbility.ActualNeedCost(def, power.MagicUser);
                if (need == null || !float.IsFinite(price) || price < 0 || !(need.CurLevel >= price)) return false;
            }
            if (def.bloodCost != 0)
            {
                float required = power.ActualBloodCost * 100f;
                var meter = body.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_BloodHD"));
                if (meter == null || !float.IsFinite(required) || required < 0 || meter.Severity < required) return false;
            }
            float cost = power.MagicUser.ActualManaCost(def);
            if (def == TorannMagicDefOf.TM_Recall && (!TM_Action.RecallMatches(body,power.MagicUser) || power.MagicUser.recallExpiration <= Verse.Find.TickManager.TicksGame)) return false;
            if (def == TorannMagicDefOf.TM_DispelLivingWall && (power.MagicUser.livingWall?.Spawned != true || power.MagicUser.livingWall.CasterPawn != power.Pawn)) return false;
            if (def == TorannMagicDefOf.TM_DispelBranding && power.MagicUser.BrandPawns.Count == 0) return false;
            if (def == TorannMagicDefOf.TM_DispelEnchantWeapon && (power.MagicUser.weaponEnchants?.Count ?? 0) == 0) return false;
            if (def == TorannMagicDefOf.TM_DismissEnchanterStones && power.MagicUser.enchanterStones?.Any(s => !s.DestroyedOrNull()) != true) return false;
            if (def == TorannMagicDefOf.TM_DismissGuardianSpirit && !ShamanLifecycle.OwnsGuardian(power.MagicUser)) return false;
            if (ShamanLifecycle.Control(def) && !power.MagicUser.HexedPawns.Any(p => p.Spawned)) return false;
            if (def == TorannMagicDefOf.TM_AlterFate && !Verb_AlterFate.Available(body,power)) return false;
            if (def == TorannMagicDefOf.TM_ShatterSentinel && !power.MagicUser.summonedSentinels.Any(t => t?.Spawned == true)) return false;
            if (def == TorannMagicDefOf.TM_DismissEarthSprites && power.MagicUser.earthSpriteType == 0) return false;
            if (def == TorannMagicDefOf.TM_DispelStoneskin && power.MagicUser.StoneskinPawns.Count == 0) return false;
            if (def == TorannMagicDefOf.TM_SoL_CreateLight && (power.MagicUser.SoL?.Spawned != true ||
                power.MagicUser.SoL.Map != body.Map || power.MagicUser.SoL.OwnerPawn != power.Pawn)) return false;
            if (def == TorannMagicDefOf.TM_SoL_Equalize && !Verb_SoL_Equalize.Available(power.MagicUser, body)) return false;
            if (def.MainVerb.verbClass == typeof(Verb_ApplyAura) && !SpiritHostedAura.CanToggle(body, power.Pawn, def)) return false;
            return cost >= 0 && (paidBurst ? IsBurst(def) : power.MagicUser.Mana.CurLevel >= cost);
        }

        private static bool CanTarget(Pawn body, MagicAbility power, LocalTargetInfo target, bool paidBurst = false)
        {
            if (power?.magicDef.MainVerb.AbilityTargetCategory == AbilityTargetCategory.TargetSelf)
                return Ready(body, power) && target.Thing == body && power.magicDef.MainVerb.targetParams.canTargetSelf;
            if (!Ready(body,power,paidBurst) || !target.IsValid || !target.Cell.InBounds(body.Map) || target.Cell.Fogged(body.Map) ||
                (target.HasThing && target.Thing.Map != body.Map) ||
                !power.magicDef.MainVerb.targetParams.CanTarget(target.ToTargetInfo(body.Map))) return false;
            if (power.magicDef == TorannMagicDefOf.TM_EnchantWeapon && target.Pawn?.equipment?.Primary?.def.IsMeleeWeapon != true) return false;
            if (power.magicDef == TorannMagicDefOf.TM_SoulBond)
            {
                var ally = target.Pawn;
                var soul = power.Pawn;
                bool mental = ClassIdentity.DemonEffect(soul,true,soul.gender == Gender.Male);
                bool physical = ClassIdentity.DemonEffect(soul,false,soul.gender == Gender.Female);
                if (ally == null || ally == body || ally.Dead || !ally.RaceProps.Humanlike || ally.Faction != body.Faction ||
                    ally.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) ||
                    !Verb_SoulBond.CanReplace(power.MagicUser,ally,physical,mental) || !DemonSoulBond.SourceAvailable(body,soul,mental)) return false;
            }
            if (power.magicDef == TorannMagicDefOf.TM_Symbiosis && !Verb_Symbiosis.CanLink(body,target.Pawn)) return false;
            if (power.magicDef == TorannMagicDefOf.TM_FadeEmotions && !Verb_FadeEmotions.CanFade(body,target.Pawn)) return false;
            if (power.magicDef == TorannMagicDefOf.TM_HarvestPassion && !Verb_HarvestPassion.CanHarvest(body,power.Pawn,target.Pawn)) return false;
            if (power.magicDef == TorannMagicDefOf.TM_LivingWall && Verb_LivingWall.Segment(body,target) == null) return false;
            if (power.magicDef == TorannMagicDefOf.TM_EnchanterStone && Verb_EnchanterStone.Source(body, power.Pawn, target) == null) return false;
            if (power.magicDef == TorannMagicDefOf.TM_Transmutate && Verb_Transmutate.Source(body, power.Pawn, target) == null) return false;
            if (power.magicDef == TorannMagicDefOf.TM_Sentinel && target.Cell.GetFirstBuilding(body.Map) is Building existing &&
                (!(existing is Building_TMSentinel) || !power.MagicUser.summonedSentinels.Contains(existing))) return false;
            if (power.magicDef == TorannMagicDefOf.TM_RaiseUndead &&
                !Projectile_RaiseUndead.HasHostedTarget(body.Map, target.Cell, power.magicDef.MainVerb.defaultProjectile)) return false;
            if (power.magicDef.MainVerb.defaultProjectile?.thingClass == typeof(Projectile_CorpseExplosion) &&
                !Projectile_CorpseExplosion.HasTarget(body.Map,target.Cell)) return false;
            if (power.magicDef == TorannMagicDefOf.TM_BloodForBlood && !Verb_BloodForBlood.ValidVictim(body,target.Pawn)) return false;
            if (power.magicDef == TorannMagicDefOf.TM_BloodShield && (target.Pawn == null || target.Pawn == body || target.Pawn.Dead)) return false;
            if (power.magicDef == TorannMagicDefOf.TM_Teleport && !Projectile_Teleport.HasSpace(body.Map, target.Cell)) return false;
            if (!SpiritHostedSummons.CanPlace(power.magicDef, body.Map, target.Cell)) return false;
            var verb = PhysicalVerb(body, power);
            if (verb is Verb_Brand brand && !brand.Accepts(body,target.Pawn)) return false;
            if (verb is ISpiritCorpseConsumer consumer && !consumer.HasConsumableTarget(target)) return false;
            return verb.CanHitTargetFrom(body.Position, target);
        }

        private static Verb_UseAbility PhysicalVerb(Pawn body, MagicAbility power)
        {
            var props = power.magicDef.MainVerb;
            var verb = (Verb_UseAbility)Activator.CreateInstance(props.verbClass);
            verb.caster = body;
            verb.verbProps = props;
            verb.Ability = power;
            return verb;
        }

        internal static IEnumerable<Gizmo> Commands(IEnumerable<Gizmo> existing, Pawn body)
        {
            foreach (var gizmo in existing) yield return gizmo;
            if (!SpiritRebirthState.TryGetPossessingSpirit(body, out var soul)) yield break;
            var magic = soul.GetCompAbilityUserMagic();
            if (magic?.ExistingMagicData == null) yield break;
            if (SpiritHostedTechnoBit.Available(body,magic))
            {
                yield return magic.CreateTechnoToggle("technoBit", () => SpiritHostedTechnoBit.Available(body,magic));
                yield return magic.CreateTechnoToggle("technoRepair", () => SpiritHostedTechnoBit.Available(body,magic));
            }
            if (TechnomancerAmmunition.Owner(body) == magic)
                yield return magic.CreateTechnoToggle("elementalShot", () => TechnomancerAmmunition.Owner(body) == magic);
            foreach (var power in magic.AbilityData.Powers)
            {
                if (!(power is MagicAbility) || !ReferenceEquals(power.AbilityUser, magic)) continue;
                var family = Families.FirstOrDefault(f => f.spell() == power.Def);
                if (family.job == null) continue;
                var job = DefDatabase<JobDef>.GetNamedSilentFail(family.job);
                var command = CommandFor(body,soul,job);
                if (command != null) yield return command;
            }
        }

        private static Command CommandFor(Pawn body, Pawn soul, JobDef job)
        {
            var def = Spell(job);
            var power = Find(body, soul, def);
            if (power == null) return null;
            void Order(LocalTargetInfo target)
            {
                var map = body.Map;
                bool Valid() => body.Spawned && body.Map == map && body.CurJobDef != job && CanTarget(body, Find(body, soul, def), target);
                if (!Valid())
                {
                    Messages.Message("TM_SpiritRebirthCastUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                var order = JobMaker.MakeJob(job, target, soul);
                if (def == TorannMagicDefOf.TM_IncitePassion) Verb_IncitePassion.BindSelection(order, power.MagicUser.incitePassionSkill?.def.defName);
                if (def == TorannMagicDefOf.TM_EnchantWeapon) order.targetC = target.Pawn.equipment.Primary;
                if (def == TorannMagicDefOf.TM_LivingWall) order.targetC = Verb_LivingWall.Segment(body,target);
                if (def == TorannMagicDefOf.TM_EnchanterStone) order.targetC = Verb_EnchanterStone.Source(body, soul, target);
                if (def == TorannMagicDefOf.TM_Transmutate) order.targetC = Verb_Transmutate.Source(body, soul, target);
                void Submit() => body.jobs.TryTakeOrderedJob(order, JobTag.Misc);
                if (def == TorannMagicDefOf.TM_Scorn) Verb_Scorn.Submit(body,order,target,true,Valid,() => body.jobs.TryTakeOrderedJob(order, JobTag.Misc));
                else if (DemonBondTravel.Applies(power))
                    DemonBondTravel.Submit(body, power, order, true, Valid, Submit);
                else if (def == TorannMagicDefOf.TM_ValiantCharge || def.MainVerb.verbClass == typeof(Verb_LightSkip) || Odyssey.ManualTravelSafety.IsTravel(power))
                    Odyssey.ManualTravelSafety.Submit(body, order, map, target.Cell, true, Valid, Submit);
                else Submit();
            }
            Command_Action command = def == TorannMagicDefOf.TM_IncitePassion
                ? new IncitePassionCommand { owner = power.MagicUser } : new Command_Action();
            command.defaultLabel = def.LabelCap;
            command.defaultDesc = def.description;
            command.icon = def.uiIcon;
            command.action = () => {
                if (def.MainVerb.AbilityTargetCategory == AbilityTargetCategory.TargetSelf) { Order(body); return; }
                var field = def.MainVerb.verbClass == typeof(Verb_Scorn) || def.MainVerb.verbClass == typeof(Verb_Attraction) || def.MainVerb.verbClass == typeof(Effect_ValiantCharge) || def.MainVerb.verbClass == typeof(Verb_FogOfTorment) || def.MainVerb.verbClass == typeof(Verb_LightningCloud) || typeof(Verb_RankedStormField).IsAssignableFrom(def.MainVerb.verbClass)
                    ? PhysicalVerb(body, power) : null;
                Verse.Find.Targeter.BeginTargeting(def.MainVerb.targetParams, Order, highlightAction: target => {
                    if (!body.Spawned) return;
                    GenDraw.DrawRadiusRing(body.Position, def.MainVerb.range);
                    if (!target.IsValid || !target.Cell.InBounds(body.Map)) return;
                    var area = def.MainVerb.TargetAoEProperties;
                    if (area?.showRangeOnSelect == true)
                        GenDraw.DrawRadiusRing(area.startsFromCaster ? body.Position : target.Cell, area.range);
                    else if (field != null)
                    {
                        float radius = field.HighlightFieldRadiusAroundTarget(out _);
                        var center = field is Verb_Scorn ? Projectile_Scorn.LandingCell(target.Cell,body.Map) : target.Cell;
                        if (radius > 0f && center.IsValid) GenDraw.DrawRadiusRing(center,radius);
                    }
                },
                targetValidator: target => CanTarget(body, Find(body,soul,def), target), caster: body);
            };
            if (body.CurJobDef == job || !Ready(body, power)) command.Disable("TM_SpiritRebirthCastUnavailable".Translate());
            return command;
        }

        private sealed class IncitePassionCommand : Command_Action
        {
            internal CompAbilityUserMagic owner;
            public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
            {
                get
                {
                    foreach (var skill in owner.Pawn.skills.skills.Where(s => s.passion != Passion.None))
                    {
                        yield return new FloatMenuOption(skill.def.LabelCap + " (" + skill.passion + ")", () =>
                        {
                            owner.incitePassionSkill = skill;
                            Messages.Message("TM_IncitePassionSelection".Translate(skill.def.label), MessageTypeDefOf.NeutralEvent);
                        });
                    }
                }
            }
        }

        internal static bool Launch(Pawn body, Pawn soul, TMAbilityDef def, LocalTargetInfo target, bool paidBurst = false)
        {
            var power = Find(body,soul,def);
            if (!CanTarget(body,power,target,paidBurst)) return false;
            if ((def == TorannMagicDefOf.TM_ValiantCharge || def.MainVerb.verbClass == typeof(Verb_LightSkip)) && !Odyssey.ManualTravelSafety.CanComplete(body, body.Map, target.Cell)) return false;
            var props = power.magicDef.MainVerb;
            var verb = PhysicalVerb(body, power);
            if (verb is Verb_EnchantWeapon && target.Pawn?.equipment?.Primary != body.CurJob?.targetC.Thing) return false;
            if (verb is Verb_Transmutate || verb is Verb_EnchanterStone || verb is Verb_LivingWall)
            {
                var source = body.CurJob?.targetC.Thing;
                if (source?.Spawned != true || source.Map != body.Map || source.Position != target.Cell) return false;
                target = source;
            }
            if (verb is Effect_LightLance)
            {
                if (!(body.jobs?.curDriver is JobDriver_SpiritBolt driver) || body.CurJob.targetB.Pawn != soul ||
                    Spell(body.CurJob.def) != def || paidBurst != (driver.PaidShots > 0)) return false;
                if (!paidBurst && !Shoot(body, power, verb, target)) return false;
                if (!Effect_LightLance.PayForPulse(soul, def, props.burstShotCount - driver.PaidShots, props.burstShotCount))
                {
                    driver.FinishSeries(props.burstShotCount);
                    return true;
                }
                Effect_LightLance.Emit(body, power, target);
                if (!paidBurst) power.PayForAttempt(body);
                else props.soundCast?.PlayOneShot(new TargetInfo(body.Position, body.Map));
                return true;
            }
            if (verb is Verb_LightBurst flash)
            {
                if (!(body.jobs?.curDriver is JobDriver_SpiritBolt driver) || body.CurJob?.targetB.Pawn != soul ||
                    Spell(body.CurJob.def) != def || !driver.EmitFlash(flash, target, paidBurst)) return false;
                if (!paidBurst) power.PayForAttempt(body);
                props.soundCast?.PlayOneShot(new TargetInfo(body.Position, body.Map));
                return true;
            }
            if (verb is Verb_SpiritTargetable direct && Families.Any(f => f.spell() == def && f.effect == props.verbClass))
            {
                bool travel = Odyssey.ManualTravelSafety.IsTravel(power);
                if (!travel && !(verb is Verb_LivingWall) && !(verb is Verb_EnchanterStone) && !(verb is Verb_Transmutate) && !(verb is Verb_SummonGuardianSpirit) && !(verb is Verb_SummonTotem) && !(verb is Verb_ReverseTime) && !(verb is Verb_EarthSprites) && !(verb is Verb_Summon) && !(verb is Verb_SummonSpiritWolves) && !(verb is Verb_RegrowLimb) && !(verb is ISpiritCorpseConsumer) && !(verb is Verb_Overdrive) && !(verb is Verb_TechnoWeapon) &&
                    props.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && !(target.Thing is Pawn { Dead: false }))
                    return false;
                var soundPosition = new TargetInfo(body.Position, body.Map);
                if (!direct.CastFromHost(target)) return false;
                if (!travel && !(direct is Verb_SummonDemon) && !DemonBondTravel.Applies(power) && !(direct is Verb_FoldReality) && !(direct is Verb_LightSkipGlobal)) power.PayForAttempt(body);
                props.soundCast?.PlayOneShot(soundPosition);
                return true;
            }
            if (props.AbilityTargetCategory == AbilityTargetCategory.TargetAoE && verb is Verb_SpiritTargetable area)
            {
                bool emitted = false;
                foreach (var victim in area.TargetsFromHost(target))
                {
                    if (!Shoot(body, power, verb, victim)) continue;
                    if (!emitted) power.PayForAttempt(body);
                    emitted = true;
                }
                return emitted;
            }
            if (!Shoot(body, power, verb, target)) return false;
            if (!paidBurst && def != TorannMagicDefOf.TM_Scorn) power.PayForAttempt(body);
            return true;
        }

        private static bool Shoot(Pawn body, MagicAbility power, Verb_UseAbility verb, LocalTargetInfo target)
        {
            var props = power.magicDef.MainVerb;
            bool clear = verb.TryFindShootLineFromTo(body.Position, target, out var line);
            if (!clear && props.requireLineOfSight && props.stopBurstWithoutLos) return false;
            var shot = (Projectile_AbilityBase)ThingMaker.MakeThing(props.defaultProjectile);
            if (shot is Projectile_SpiritBolt bolt) bolt.PrepareProjection(power);
            else if (shot is Projectile_SpiritNativeAbility native) native.SetSource(power.Pawn);
            else if (shot is Projectile_SpiritRay laser) laser.PrepareProjection(power);
            else if (shot is Projectile_DemonField demonField) demonField.PrepareHosted(power,body);
            else if (shot is Projectile_BloodMoon moon) moon.PrepareHosted(power);
            else if (shot is Projectile_ChronostaticField stasis) stasis.PrepareHosted(power);
            else if (shot is Projectile_AccelerateTime accelerate) accelerate.PrepareHosted(power);
            else if (shot is Projectile_Sentinel sentinel) sentinel.PrepareHosted(power);
            else if (shot is Projectile_EarthernHammer hammer) hammer.PrepareHosted(power);
            else if (shot is Projectile_Encase encase) encase.PrepareHosted(power);
            else if (shot is Projectile_IgniteBlood ignite) ignite.PrepareHosted(power);
            else if (!SpiritHostedSummons.Prepare(shot, power)) return false;
            var map = body.Map;
            var start = body.Position;
            var drawPos = body.DrawPos;
            // Taking off despawns the physical caster and ends its current job.
            if (props.verbClass == typeof(Effect_ValiantCharge) && !Effect_ValiantCharge.StartCharge(body, power.Pawn, target))
                return false;
            GenSpawn.Spawn(shot, clear ? line.Source : start, map);
            shot.extraDamages = props.extraDamages;
            // Standard player ability casts allow interception by other pawns.
            var flags = ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetPawns;
            if (!target.HasThing || target.Thing.def.Fillage == FillCategory.Full) flags |= ProjectileHitFlags.NonTargetWorld;
            shot.Launch(body,power.Def,drawPos,target,flags,false,null,props.hediffsToApply,props.mentalStatesToApply,props.thingsToSpawn);
            if (shot is Projectile_DemonField projected) DemonTrials.BindHosted(projected,verb);
            if (shot is Projectile_Encase field) GeomancerTrials.StampHosted(field, power);
            if (shot is Projectile_IgniteBlood fire) BloodMageTrials.StampHosted(fire, power);
            if (shot is Projectile_HostedShadowBolt) Effect_ShadowBolt.SpawnOrb(body,power.Pawn,target);
            if (shot is Projectile_HostedDeathBolt) Effect_DeathBolt.SpawnOrb(body, power.Pawn, target);
            if (shot is Projectile_SpiritMissile) ArcanistTrials.StampHostedMissile(shot, power);
            if (shot is Projectile_LightningCloud) LightningTrials.StampHosted(shot, power, LightningTrials.Spell.Cloud);
            if (shot is Projectile_SpiritAmp) LightningTrials.StampHosted(shot, power, LightningTrials.Spell.Amp);
            if (shot is Laser_LightningBolt) LightningTrials.StampHosted(shot, power, LightningTrials.Spell.Bolt);
            props.soundCast?.PlayOneShot(new TargetInfo(start,map));
            props.soundCastTail?.PlayOneShotOnCamera();
            return true;
        }
    }

    public abstract class Verb_SpiritTargetable : Verb_UseAbility
    {
        internal List<LocalTargetInfo> TargetsFromHost(LocalTargetInfo center)
        {
            currentTarget = center;
            UpdateTargets();
            return TargetsAoE;
        }
        internal bool CastFromHost(LocalTargetInfo target)
        {
            currentTarget = target;
            return ApplyHostedEffect();
        }
        // Some legacy verbs signal that they did not fire a projectile even when their direct effect succeeded.
        protected virtual bool ApplyHostedEffect() => TryCastShot();
    }

    public sealed class JobDriver_SpiritBolt : JobDriver
    {
        private bool attempted;
        private int shotsFired, nextShotTick;
        internal int PaidShots => shotsFired;
        internal void FinishSeries(int count) => shotsFired = count;
        private Verb_LightBurst.FlashSeries flashes;
        internal bool EmitFlash(Verb_LightBurst verb, LocalTargetInfo target, bool paid)
        {
            if (paid != (flashes != null) || paid && shotsFired >= flashes.count) return false;
            if (!paid) flashes = verb.BeginSeries(verb.Ability.Pawn);
            verb.EmitHostedFlash(target, flashes);
            return true;
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref attempted,"attempted");
            Scribe_Values.Look(ref shotsFired,"shotsFired");
            Scribe_Values.Look(ref nextShotTick,"nextShotTick");
            Scribe_Deep.Look(ref flashes, "TM_lightBurstSeries");
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var def = SpiritHostedBolt.Spell(job.def);
            this.FailOn(() => SpiritHostedBolt.Find(pawn, job.targetB.Pawn, def) == null);
            var wait = Toils_General.Wait(1);
            wait.initAction = () => ticksLeftThisToil = Mathf.Max(1,
                GenTicks.SecondsToTicks(def.MainVerb.warmupTime * Compatibility.AbilityTiming.WarmupMultiplier));
            yield return wait;
            yield return Toils_General.Do(() => {
                if (attempted || pawn.CurJob != job || pawn.jobs.curDriver != this) { EndJobWith(JobCondition.Incompletable); return; }
                attempted = true;
                if (!SpiritHostedBolt.Launch(pawn,job.targetB.Pawn,def,job.targetA)) { EndJobWith(JobCondition.Incompletable); return; }
                shotsFired = 1;
                nextShotTick = Find.TickManager.TicksGame + def.MainVerb.ticksBetweenBurstShots;
            });
            if (!SpiritHostedBolt.IsBurst(def)) yield break;
            var burst = ToilMaker.MakeToil("Continue projected spell burst");
            burst.defaultCompleteMode = ToilCompleteMode.Never;
            burst.tickAction = () => {
                if (shotsFired >= (flashes?.count ?? def.MainVerb.burstShotCount)) { ReadyForNextToil(); return; }
                if (Find.TickManager.TicksGame < nextShotTick) return;
                if (!attempted || shotsFired < 1 || !SpiritHostedBolt.Launch(pawn,job.targetB.Pawn,def,job.targetA,true))
                { EndJobWith(JobCondition.Incompletable); return; }
                shotsFired++;
                nextShotTick = Find.TickManager.TicksGame + def.MainVerb.ticksBetweenBurstShots;
            };
            yield return burst;
        }
    }
}
