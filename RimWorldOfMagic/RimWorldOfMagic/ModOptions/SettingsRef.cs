using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace TorannMagic.ModOptions
{
    public class SettingsRef
    {
        public float xpMultiplier = Settings.Instance.xpMultiplier;
        public float needMultiplier = Settings.Instance.needMultiplier;
        public float deathExplosionRadius = Settings.Instance.deathExplosionRadius;
        public bool AICasting = Settings.Instance.AICasting;
        public bool AIAggressiveCasting = Settings.Instance.AIAggressiveCasting;
        public bool AIHardMode = Settings.Instance.AIHardMode;
        public bool AIMarking = Settings.Instance.AIMarking;
        public bool AIFighterMarking = Settings.Instance.AIFighterMarking;
        public bool AIFriendlyMarking = Settings.Instance.AIFriendlyMarking;
        public float baseMageChance = Settings.Instance.baseMageChance;
        public float baseFighterChance = Settings.Instance.baseFighterChance;
        public float advMageChance = Settings.Instance.advMageChance;
        public float advFighterChance = Settings.Instance.advFighterChance;
        public float supportTraitChance = Settings.Instance.supportTraitChance;
        public int deathExplosionMin = Settings.Instance.deathExplosionMin;
        public int deathExplosionMax = Settings.Instance.deathExplosionMax;
        public float magicyteChance = Settings.Instance.magicyteChance;
        public bool showIconsMultiSelect = Settings.Instance.showIconsMultiSelect;
        public float riftChallenge = Settings.Instance.riftChallenge;
        public float wanderingLichChallenge = Settings.Instance.wanderingLichChallenge;
        public float demonAssaultChallenge = Settings.Instance.demonAssaultChallenge;
        public bool showGizmo = Settings.Instance.showGizmo;
        public bool showLevelUpMessage = Settings.Instance.showLevelUpMessage;
        public bool changeUndeadPawnAppearance = Settings.Instance.changeUndeadPawnAppearance;
        public bool changeUndeadAnimalAppearance = Settings.Instance.changeUndeadAnimalAppearance;
        public bool showClassIconOnColonistBar = Settings.Instance.showClassIconOnColonistBar;
        public float classIconSize = Settings.Instance.classIconSize;
        public bool unrestrictedBloodTypes = Settings.Instance.unrestrictedBloodTypes;
        public float paracyteSoftCap = Settings.Instance.paracyteSoftCap;
        public bool paracyteMagesCount = Settings.Instance.paracyteMagesCount;
        public bool unrestrictedWeaponCopy = Settings.Instance.unrestrictedWeaponCopy;
        public float undeadUpkeepMultiplier = Settings.Instance.undeadUpkeepMultiplier;
        public float deathRetaliationDelayFactor = Settings.Instance.deathRetaliationDelayFactor;
        public float deathRetaliationChance = Settings.Instance.deathRetaliationChance;
        public bool deathRetaliationIsLethal = Settings.Instance.deathRetaliationIsLethal;
        public bool shrinkIcons = Settings.Instance.shrinkIcons;
        public Vector2 iconPosition = Settings.Instance.iconPosition;
        public bool cameraSnap= Settings.Instance.cameraSnap;

        //autocast
        public bool autocastEnabled = Settings.Instance.autocastEnabled;
        public bool autocastAnimals = Settings.Instance.autocastAnimals;
        public float autocastMinThreshold = Settings.Instance.autocastMinThreshold;
        public float autocastCombatMinThreshold = Settings.Instance.autocastCombatMinThreshold;
        public float autocastEvaluationFrequency = Settings.Instance.autocastEvaluationFrequency;
        public bool autocastQueueing = Settings.Instance.autocastQueueing;

        //golem options
        public bool showDormantFrames = Settings.Instance.showDormantFrames;
        public bool showGolemsOnColonistBar = Settings.Instance.showGolemsOnColonistBar;

        //Class options
        public bool Arcanist = Settings.Instance.Arcanist;
        public bool FireMage = Settings.Instance.FireMage;
        public bool IceMage = Settings.Instance.IceMage;
        public bool LitMage = Settings.Instance.LitMage;
        public bool Druid = Settings.Instance.Druid;
        public bool Paladin = Settings.Instance.Paladin;
        public bool Necromancer = Settings.Instance.Necromancer;
        public bool Bard = Settings.Instance.Bard;
        public bool Priest = Settings.Instance.Priest;
        public bool Demonkin = Settings.Instance.Demonkin;
        public bool Geomancer = Settings.Instance.Geomancer;
        public bool Summoner = Settings.Instance.Summoner;
        public bool Technomancer = Settings.Instance.Technomancer;
        public bool BloodMage = Settings.Instance.BloodMage;
        public bool Enchanter = Settings.Instance.Enchanter;
        public bool Chronomancer = Settings.Instance.Chronomancer;
        public bool Wanderer = Settings.Instance.Wanderer;
        public bool ChaosMage = Settings.Instance.ChaosMage;
        public bool Brightmage = Settings.Instance.Brightmage;
        public bool Shaman = Settings.Instance.Shaman;
        public bool Golemancer = Settings.Instance.Golemancer;
        public bool Empath = Settings.Instance.Empath;

        public bool Gladiator = Settings.Instance.Gladiator;
        public bool Bladedancer = Settings.Instance.Bladedancer;
        public bool Sniper = Settings.Instance.Sniper;
        public bool Ranger = Settings.Instance.Ranger;
        public bool Faceless = Settings.Instance.Faceless;
        public bool Psionic = Settings.Instance.Psionic;
        public bool DeathKnight = Settings.Instance.DeathKnight;
        public bool Monk = Settings.Instance.Monk;
        public bool Wayfarer = Settings.Instance.Wayfayer;
        public bool Commander = Settings.Instance.Commander;
        public bool SuperSoldier = Settings.Instance.SuperSoldier;
        public bool Shadow = Settings.Instance.Shadow;
        public bool Apothecary = Settings.Instance.Apothecary;

        public bool ArcaneConduit = Settings.Instance.ArcaneConduit;
        public bool ManaWell = Settings.Instance.ManaWell;
        public bool Boundless = Settings.Instance.Boundless;

        //faction settings
        public Dictionary<string, float> FactionFighterSettings = Settings.Instance.FactionFighterSettings;
        public Dictionary<string, float> FactionMageSettings = Settings.Instance.FactionMageSettings;

        public Dictionary<string, bool> CustomClass = Settings.Instance.CustomClass;
    }
}
