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
        public float baseMageChance = 0.2f;
        public float baseFighterChance = 0.2f;
        public float advMageChance = 0.1f;
        public float advFighterChance = 0.1f;
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
        public bool golemScreenShake = true;

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
        public bool Wayfarer = true;
        public bool Commander = true;
        public bool SuperSoldier = true;
        public bool Shadow = true;
        public bool Apothecary = true;

        public bool ArcaneConduit = true;
        public bool ManaWell = true;
        public bool Boundless = true;
        public bool Enlightened = true;
        public bool Cursed = true;
        public bool FaeBlood = true;
        public bool GiantsBlood = true;

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
            // Mage classes enabled
            Scribe_Values.Look<bool>(ref Arcanist, "Arcanist", true);
            Scribe_Values.Look<bool>(ref Bard, "Bard", true);
            Scribe_Values.Look<bool>(ref BloodMage, "BloodMage", true);
            Scribe_Values.Look<bool>(ref Brightmage, "Brightmage", true);
            Scribe_Values.Look<bool>(ref ChaosMage, "ChaosMage", true);
            Scribe_Values.Look<bool>(ref Chronomancer, "Chronomancer", true);
            Scribe_Values.Look<bool>(ref Demonkin, "Demonkin", true);
            Scribe_Values.Look<bool>(ref Druid, "Druid", true);
            Scribe_Values.Look<bool>(ref Empath, "Empath", true);
            Scribe_Values.Look<bool>(ref Enchanter, "Enchanter", true);
            Scribe_Values.Look<bool>(ref FireMage, "FireMage", true);
            Scribe_Values.Look<bool>(ref Geomancer, "Geomancer", true);
            Scribe_Values.Look<bool>(ref Golemancer, "Golemancer", true);
            Scribe_Values.Look<bool>(ref IceMage, "IceMage", true);
            Scribe_Values.Look<bool>(ref LitMage, "LitMage", true);
            Scribe_Values.Look<bool>(ref Necromancer, "Necromancer", true);
            Scribe_Values.Look<bool>(ref Paladin, "Paladin", true);
            Scribe_Values.Look<bool>(ref Priest, "Priest", true);
            Scribe_Values.Look<bool>(ref Shaman, "Shaman", true);
            Scribe_Values.Look<bool>(ref Summoner, "Summoner", true);
            Scribe_Values.Look<bool>(ref Technomancer, "Technomancer", true);
            Scribe_Values.Look<bool>(ref Wanderer, "Wanderer", true);
            // Fighter classes enabled
            Scribe_Values.Look<bool>(ref Apothecary, "Apothecary", true);
            Scribe_Values.Look<bool>(ref Bladedancer, "Bladedancer", true);
            Scribe_Values.Look<bool>(ref Commander, "Commander", true);
            Scribe_Values.Look<bool>(ref DeathKnight, "DeathKnight", true);
            Scribe_Values.Look<bool>(ref Faceless, "Faceless", true);
            Scribe_Values.Look<bool>(ref Gladiator, "Gladiator", true);
            Scribe_Values.Look<bool>(ref Monk, "Monk", true);
            Scribe_Values.Look<bool>(ref Psionic, "Psionic", true);
            Scribe_Values.Look<bool>(ref Ranger, "Ranger", true);
            Scribe_Values.Look<bool>(ref Shadow, "Shadow", true);
            Scribe_Values.Look<bool>(ref Sniper, "Sniper", true);
            Scribe_Values.Look<bool>(ref SuperSoldier, "SuperSoldier", true);
            Scribe_Values.Look<bool>(ref Wayfarer, "Wayfarer", true);

            Scribe_Collections.Look(ref this.CustomClass, "CustomClass");
            Scribe_Values.Look<bool>(ref this.ManaWell, "ManaWell", true, false);
            Scribe_Values.Look<bool>(ref this.ArcaneConduit, "ArcaneConduit", true, false);
            Scribe_Values.Look<bool>(ref this.Boundless, "Boundless", true, false);
            Scribe_Values.Look<bool>(ref this.Enlightened, "Enlightened", true, false);
            Scribe_Values.Look<bool>(ref this.Cursed, "Cursed", true, false);
            Scribe_Values.Look<bool>(ref this.FaeBlood, "FaeBlood", true, false);
            Scribe_Values.Look<bool>(ref this.GiantsBlood, "GiantsBlood", true, false);
            Scribe_Collections.Look(ref this.FactionFighterSettings, "FactionFighterSettings");
            Scribe_Collections.Look(ref this.FactionMageSettings, "FactionMageSettings");
        }
    }
}
