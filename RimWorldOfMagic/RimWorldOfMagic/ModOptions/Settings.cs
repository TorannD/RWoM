using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;


namespace TorannMagic.ModOptions
{
    public class Settings : Verse.ModSettings
    {
        public float xpMultiplier = 1f;
        public float needMultiplier = 1f;        
        public bool AICasting = true;
        public bool AIAggressiveCasting = true;
        public bool AIHardMode = false;
        public bool AIMarking = true;
        public bool AIFighterMarking = false;
        public bool AIFriendlyMarking = false;
        public float baseMageChance = 1f;
        public float baseFighterChance = 1f;
        public float advMageChance = 0.5f;
        public float advFighterChance = 0.5f;
        public float supportTraitChance = 0.1f;
        public float magicyteChance = .005f;
        public bool showIconsMultiSelect = true;
        public float riftChallenge = 1f;
        public float demonAssaultChallenge = 1f;
        public float wanderingLichChallenge = 1f;
        public bool showGizmo = true;
        public bool showLevelUpMessage = true;
        public bool changeUndeadPawnAppearance = true;
        public bool changeUndeadAnimalAppearance = true;
        public bool showClassIconOnColonistBar = true;
        public float classIconSize = 1f;
        public bool unrestrictedBloodTypes = true;
        public float paracyteSoftCap = 50f;
        public bool paracyteMagesCount = true;
        public bool unrestrictedWeaponCopy = false;
        public float undeadUpkeepMultiplier = 1f;
        public bool shrinkIcons = false;
        public Vector2 iconPosition = Vector2.zero;
        public bool cameraSnap = true;

        //Death Retaliation
        public float deathRetaliationChance = 1f;
        public float deathRetaliationDelayFactor = 1f;
        public bool deathRetaliationIsLethal = true;
        public float deathExplosionRadius = 3f;
        public int deathExplosionMin = 20;
        public int deathExplosionMax = 50;

        //autocast options
        public bool autocastEnabled = true;
        public bool autocastAnimals = false;
        public float autocastMinThreshold = 0.7f;
        public float autocastCombatMinThreshold = 0.2f;
        public int autocastEvaluationFrequency = 180;
        public bool autocastQueueing = false;

        //Golem options
        public bool showDormantFrames = false;
        public bool showGolemsOnColonistBar = false;

        //class options
        public bool Arcanist = true;
        public bool FireMage = true;
        public bool IceMage = true;
        public bool LitMage = true;
        public bool Druid = true;
        public bool Paladin = true;
        public bool Necromancer = true;
        public bool Bard = true;
        public bool Priest = true;
        public bool Demonkin = true;
        public bool Geomancer = true;
        public bool Summoner = true;
        public bool Technomancer = true;
        public bool BloodMage = true;
        public bool Enchanter = true;
        public bool Chronomancer = true;
        public bool Wanderer = true;
        public bool ChaosMage = true;
        public bool Brightmage = true;
        public bool Shaman = true;
        public bool Golemancer = true;
        public bool Empath = true;

        public bool Gladiator = true;
        public bool Bladedancer = true;
        public bool Sniper = true;
        public bool Ranger = true;
        public bool Faceless = true;
        public bool Psionic = true;
        public bool DeathKnight = true;
        public bool Monk = true;
        public bool Wayfayer = true;
        public bool Commander = true;
        public bool SuperSoldier = true;
        public bool Shadow = true;
        public bool Apothecary = true;

        public bool ArcaneConduit = true;
        public bool ManaWell = true;
        public bool Boundless = true;

        //Faction settings
        public Dictionary<string, float> FactionFighterSettings = new Dictionary<string, float>();
        public Dictionary<string, float> FactionMageSettings = new Dictionary<string, float>();
        //Custom Class options
        public Dictionary<string, bool> CustomClass = new Dictionary<string, bool>();

        public static Settings Instance;

        public Settings()
        {
            Settings.Instance = this;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<float>(ref this.xpMultiplier, "xpMultiplier", 1f, false);
            Scribe_Values.Look<float>(ref this.needMultiplier, "needMultiplier", 1f, false);            
            Scribe_Values.Look<bool>(ref this.AICasting, "AICasting", true, false);
            Scribe_Values.Look<bool>(ref this.AIAggressiveCasting, "AIAggressiveCasting", true, false);
            Scribe_Values.Look<bool>(ref this.AIHardMode, "AIHardMode", false, false);
            Scribe_Values.Look<bool>(ref this.AIMarking, "AIMarking", false, false);
            Scribe_Values.Look<bool>(ref this.AIFighterMarking, "AIFighterMarking", false, false);
            Scribe_Values.Look<bool>(ref this.AIFriendlyMarking, "AIFriendlyMarking", false, false);
            Scribe_Values.Look<float>(ref this.baseMageChance, "baseMageChance", 1f, false);
            Scribe_Values.Look<float>(ref this.baseFighterChance, "baseFighterChance", 1f, false);
            Scribe_Values.Look<float>(ref this.advMageChance, "advMageChance", 0.5f, false);
            Scribe_Values.Look<float>(ref this.advFighterChance, "advFighterChance", 0.5f, false);
            Scribe_Values.Look<float>(ref this.supportTraitChance, "supportTraitChance", 0.1f, false);
            Scribe_Values.Look<float>(ref this.magicyteChance, "magicyteChance", 0.005f, false);
            Scribe_Values.Look<bool>(ref this.showIconsMultiSelect, "showIconsMultiSelect", true, false);
            Scribe_Values.Look<float>(ref this.riftChallenge, "riftChallenge", 1f, false);
            Scribe_Values.Look<float>(ref this.demonAssaultChallenge, "demonAssaultChallenge", 1f, false);
            Scribe_Values.Look<float>(ref this.wanderingLichChallenge, "wanderingLichChallenge", 1f, false);
            Scribe_Values.Look<float>(ref this.undeadUpkeepMultiplier, "undeadUpkeepMultiplier", 1f, false);
            Scribe_Values.Look<bool>(ref this.showGizmo, "showGizmo", true, false);
            Scribe_Values.Look<bool>(ref this.showLevelUpMessage, "showLevelUpMessage", true, false);
            Scribe_Values.Look<bool>(ref this.changeUndeadPawnAppearance, "changeUndeadPawnAppearance", true, false);
            Scribe_Values.Look<bool>(ref this.changeUndeadAnimalAppearance, "changeUndeadAnimalAppearance", true, false);
            Scribe_Values.Look<bool>(ref this.showClassIconOnColonistBar, "showClassIconOnColonistBar", true, false);
            Scribe_Values.Look<float>(ref this.classIconSize, "classIconSize", 1f, false);
            Scribe_Values.Look<bool>(ref this.unrestrictedBloodTypes, "unrestrictedBloodTypes", true, false);
            Scribe_Values.Look<float>(ref this.paracyteSoftCap, "paracyteSoftCap", 1f, false);
            Scribe_Values.Look<bool>(ref this.paracyteMagesCount, "paracyteMagesCount", true, false);
            Scribe_Values.Look<bool>(ref this.unrestrictedWeaponCopy, "unrestrictedWeaponCopy", false, false);
            Scribe_Values.Look<bool>(ref this.shrinkIcons, "shrinkIcons", false, false);
            Scribe_Values.Look<Vector2>(ref this.iconPosition, "iconPosition", default(Vector2));
            Scribe_Values.Look<bool>(ref this.cameraSnap, "cameraSnap", true, false);

            Scribe_Values.Look<float>(ref this.deathExplosionRadius, "deathExplosionRadius", 3f, false);
            Scribe_Values.Look<int>(ref this.deathExplosionMin, "deathExplosionMin", 20, false);
            Scribe_Values.Look<int>(ref this.deathExplosionMax, "deathExplosionMax", 50, false);
            Scribe_Values.Look<float>(ref this.deathRetaliationChance, "deathRetaliationChance", 1f, false);
            Scribe_Values.Look<float>(ref this.deathRetaliationDelayFactor, "deathRetaliationDelayFactor", 1f, false);
            Scribe_Values.Look<bool>(ref this.deathRetaliationIsLethal, "deathRetaliationIsLethal", true, false);

            Scribe_Values.Look<bool>(ref this.autocastEnabled, "autocastEnabled", true, false);
            Scribe_Values.Look<float>(ref this.autocastMinThreshold, "autocastMinThreshold", 0.7f, false);
            Scribe_Values.Look<float>(ref this.autocastCombatMinThreshold, "autocastCombatMinThreshold", 0.2f, false);
            Scribe_Values.Look<int>(ref this.autocastEvaluationFrequency, "autocastEvaluationFrequency", 180, false);
            Scribe_Values.Look<bool>(ref this.autocastAnimals, "autocastAnimals", false, false);
            Scribe_Values.Look<bool>(ref this.autocastQueueing, "autocastQueueing", false, false);

            Scribe_Values.Look<bool>(ref this.showDormantFrames, "showDormantFrames", false, false);
            Scribe_Values.Look<bool>(ref this.showGolemsOnColonistBar, "showGolemsOnColonistBar", false, false);

            Scribe_Values.Look<bool>(ref this.Arcanist, "Arcanist", true, false);
            Scribe_Values.Look<bool>(ref this.FireMage, "FireMage", true, false);
            Scribe_Values.Look<bool>(ref this.IceMage, "IceMage", true, false);
            Scribe_Values.Look<bool>(ref this.LitMage, "LitMage", true, false);
            Scribe_Values.Look<bool>(ref this.Geomancer, "Geomancer", true, false);
            Scribe_Values.Look<bool>(ref this.Druid, "Druid", true, false);
            Scribe_Values.Look<bool>(ref this.Paladin, "Paladin", true, false);
            Scribe_Values.Look<bool>(ref this.Priest, "Priest", true, false);
            Scribe_Values.Look<bool>(ref this.Bard, "Bard", true, false);
            Scribe_Values.Look<bool>(ref this.Summoner, "Summoner", true, false);
            Scribe_Values.Look<bool>(ref this.Necromancer, "Necromancer", true, false);
            Scribe_Values.Look<bool>(ref this.Technomancer, "Technomancer", true, false);
            Scribe_Values.Look<bool>(ref this.Demonkin, "Demonkin", true, false);
            Scribe_Values.Look<bool>(ref this.BloodMage, "BloodMage", true, false);
            Scribe_Values.Look<bool>(ref this.Enchanter, "Enchanter", true, false);
            Scribe_Values.Look<bool>(ref this.Chronomancer, "Chronomancer", true, false);
            Scribe_Values.Look<bool>(ref this.Gladiator, "Gladiator", true, false);
            Scribe_Values.Look<bool>(ref this.Bladedancer, "Bladedancer", true, false);
            Scribe_Values.Look<bool>(ref this.Sniper, "Sniper", true, false);
            Scribe_Values.Look<bool>(ref this.Ranger, "Ranger", true, false);
            Scribe_Values.Look<bool>(ref this.Faceless, "Faceless", true, false);
            Scribe_Values.Look<bool>(ref this.Psionic, "Psionic", true, false);
            Scribe_Values.Look<bool>(ref this.DeathKnight, "DeathKnight", true, false);
            Scribe_Values.Look<bool>(ref this.Wanderer, "Wanderer", true, false);
            Scribe_Values.Look<bool>(ref this.Wayfayer, "Wayfarer", true, false);
            Scribe_Values.Look<bool>(ref this.ChaosMage, "ChaosMage", true, false);
            Scribe_Values.Look<bool>(ref this.Monk, "Monk", true, false);
            Scribe_Values.Look<bool>(ref this.Commander, "Commander", true, false);
            Scribe_Values.Look<bool>(ref this.SuperSoldier, "SuperSoldier", true, false);
            Scribe_Values.Look<bool>(ref this.Shadow, "Shadow", true, false);
            Scribe_Values.Look<bool>(ref this.Brightmage, "Brightmage", true, false);
            Scribe_Values.Look<bool>(ref this.Shaman, "Shaman", true, false);
            Scribe_Values.Look<bool>(ref this.Golemancer, "Golemancer", true, false);
            Scribe_Values.Look<bool>(ref this.Empath, "Empath", true, false);
            Scribe_Values.Look<bool>(ref this.Apothecary, "Apothecary", true, false);
            Scribe_Collections.Look(ref this.CustomClass, "CustomClass");
            Scribe_Values.Look<bool>(ref this.ManaWell, "ManaWell", true, false);
            Scribe_Values.Look<bool>(ref this.ArcaneConduit, "ArcaneConduit", true, false);
            Scribe_Values.Look<bool>(ref this.Boundless, "Boundless", true, false);
            Scribe_Collections.Look(ref this.FactionFighterSettings, "FactionFighterSettings");
            Scribe_Collections.Look(ref this.FactionMageSettings, "FactionMageSettings");
        }
    }
}
