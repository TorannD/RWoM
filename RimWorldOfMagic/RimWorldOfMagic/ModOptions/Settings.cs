using System;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;


namespace TorannMagic.ModOptions
{
    public class Settings : Verse.ModSettings
    {
        // This class lets us link boolean values to their labels for the class options
        public class CheckboxOption
        {
            public bool isEnabled = true;
            public string label;

            public CheckboxOption(string _label)
            {
                label = _label;
            }
        }

        public float xpMultiplier = 1f;
        public float needMultiplier = 1f;        
        public bool AICasting = true;
        public bool AIAggressiveCasting = true;
        public bool AIHardMode = false;
        public bool AIMarking = true;
        public bool AIFighterMarking = false;
        public bool AIFriendlyMarking = false;
        public float baseMageChance = 0.063f;
        public float baseFighterChance = 0.063f;
        public float advMageChance = 0.083f;
        public float advFighterChance = 0.042f;
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
        public static CheckboxOption Arcanist = new("TM_Arcanist");
        public static CheckboxOption FireMage = new("TM_FireMage");
        public static CheckboxOption IceMage = new("TM_IceMage");
        public static CheckboxOption LitMage = new("TM_LitMage");
        public static CheckboxOption Druid = new("TM_Druid");
        public static CheckboxOption Paladin = new("TM_Paladin");
        public static CheckboxOption Necromancer = new("TM_Necromancer");
        public static CheckboxOption Bard = new("TM_Bard");
        public static CheckboxOption Priest = new("TM_Priest");
        public static CheckboxOption Demonkin = new("TM_Demonkin");
        public static CheckboxOption Geomancer = new("TM_Geomancer");
        public static CheckboxOption Summoner = new("TM_Summoner");
        public static CheckboxOption Technomancer = new("TM_Technomancer");
        public static CheckboxOption BloodMage = new("TM_BloodMage");
        public static CheckboxOption Enchanter = new("TM_Enchanter");
        public static CheckboxOption Chronomancer = new("TM_Chronomancer");
        public static CheckboxOption Wanderer = new("TM_Wanderer");
        public static CheckboxOption ChaosMage = new("TM_ChaosMage");
        public static CheckboxOption Brightmage = new("TM_Brightmage");
        public static CheckboxOption Shaman = new("TM_Shaman");
        public static CheckboxOption Golemancer = new("TM_Golemancer");
        public static CheckboxOption Empath = new("TM_Empath");

        public static CheckboxOption Gladiator = new("TM_Gladiator");
        public static CheckboxOption Bladedancer = new("TM_Bladedancer");
        public static CheckboxOption Sniper = new("TM_Sniper");
        public static CheckboxOption Ranger = new("TM_Ranger");
        public static CheckboxOption Faceless = new("TM_Faceless");
        public static CheckboxOption Psionic = new("TM_Psionic");
        public static CheckboxOption DeathKnight = new("TM_DeathKnight");
        public static CheckboxOption Monk = new("TM_Monk");
        public static CheckboxOption Wayfarer = new("TM_Wayfarer");
        public static CheckboxOption Commander = new("TM_Commander");
        public static CheckboxOption SuperSoldier = new("TM_SuperSoldier");
        public static CheckboxOption Shadow = new("TM_Shadow");
        public static CheckboxOption Apothecary = new("TM_Apothecary");

        public static CheckboxOption ArcaneConduit = new("TM_ArcaneConduit");
        public static CheckboxOption ManaWell = new("TM_ManaWell");
        public static CheckboxOption Boundless = new("TM_Boundless");
        public static CheckboxOption Enlightened = new("TM_Enlightened");
        public static CheckboxOption Cursed = new("TM_Cursed");
        public static CheckboxOption FaeBlood = new("TM_FaeBlood");
        public static CheckboxOption GiantsBlood = new("TM_GiantsBlood");

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
            Scribe_Values.Look<float>(ref baseMageChance, "baseMageChance", 0.063f);
            Scribe_Values.Look<float>(ref baseFighterChance, "baseFighterChance", 0.063f);
            Scribe_Values.Look<float>(ref advMageChance, "advMageChance", 0.83f);
            Scribe_Values.Look<float>(ref advFighterChance, "advFighterChance", 0.042f);
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

            Scribe_Values.Look<bool>(ref this.showDormantFrames, "sClassOptionhowDormantFrames", false, false);
            Scribe_Values.Look<bool>(ref this.showGolemsOnColonistBar, "showGolemsOnColonistBar", false, false);
            // Mage classes enabled
            Scribe_Values.Look<bool>(ref Arcanist.isEnabled, "Arcanist", true);
            Scribe_Values.Look<bool>(ref Bard.isEnabled, "Bard", true);
            Scribe_Values.Look<bool>(ref BloodMage.isEnabled, "BloodMage", true);
            Scribe_Values.Look<bool>(ref Brightmage.isEnabled, "Brightmage", true);
            Scribe_Values.Look<bool>(ref ChaosMage.isEnabled, "ChaosMage", true);
            Scribe_Values.Look<bool>(ref Chronomancer.isEnabled, "Chronomancer", true);
            Scribe_Values.Look<bool>(ref Demonkin.isEnabled, "Demonkin", true);
            Scribe_Values.Look<bool>(ref Druid.isEnabled, "Druid", true);
            Scribe_Values.Look<bool>(ref Empath.isEnabled, "Empath", true);
            Scribe_Values.Look<bool>(ref Enchanter.isEnabled, "Enchanter", true);
            Scribe_Values.Look<bool>(ref FireMage.isEnabled, "FireMage", true);
            Scribe_Values.Look<bool>(ref Geomancer.isEnabled, "Geomancer", true);
            Scribe_Values.Look<bool>(ref Golemancer.isEnabled, "Golemancer", true);
            Scribe_Values.Look<bool>(ref IceMage.isEnabled, "IceMage", true);
            Scribe_Values.Look<bool>(ref LitMage.isEnabled, "LitMage", true);
            Scribe_Values.Look<bool>(ref Necromancer.isEnabled, "Necromancer", true);
            Scribe_Values.Look<bool>(ref Paladin.isEnabled, "Paladin", true);
            Scribe_Values.Look<bool>(ref Priest.isEnabled, "Priest", true);
            Scribe_Values.Look<bool>(ref Shaman.isEnabled, "Shaman", true);
            Scribe_Values.Look<bool>(ref Summoner.isEnabled, "Summoner", true);
            Scribe_Values.Look<bool>(ref Technomancer.isEnabled, "Technomancer", true);
            Scribe_Values.Look<bool>(ref Wanderer.isEnabled, "Wanderer", true);
            // Fighter classes enabled
            Scribe_Values.Look<bool>(ref Apothecary.isEnabled, "Apothecary", true);
            Scribe_Values.Look<bool>(ref Bladedancer.isEnabled, "Bladedancer", true);
            Scribe_Values.Look<bool>(ref Commander.isEnabled, "Commander", true);
            Scribe_Values.Look<bool>(ref DeathKnight.isEnabled, "DeathKnight", true);
            Scribe_Values.Look<bool>(ref Faceless.isEnabled, "Faceless", true);
            Scribe_Values.Look<bool>(ref Gladiator.isEnabled, "Gladiator", true);
            Scribe_Values.Look<bool>(ref Monk.isEnabled, "Monk", true);
            Scribe_Values.Look<bool>(ref Psionic.isEnabled, "Psionic", true);
            Scribe_Values.Look<bool>(ref Ranger.isEnabled, "Ranger", true);
            Scribe_Values.Look<bool>(ref Shadow.isEnabled, "Shadow", true);
            Scribe_Values.Look<bool>(ref Sniper.isEnabled, "Sniper", true);
            Scribe_Values.Look<bool>(ref SuperSoldier.isEnabled, "SuperSoldier", true);
            Scribe_Values.Look<bool>(ref Wayfarer.isEnabled, "Wayfarer", true);

            Scribe_Collections.Look(ref this.CustomClass, "CustomClass");
            Scribe_Values.Look<bool>(ref ManaWell.isEnabled, "ManaWell", true, false);
            Scribe_Values.Look<bool>(ref ArcaneConduit.isEnabled, "ArcaneConduit", true, false);
            Scribe_Values.Look<bool>(ref Boundless.isEnabled, "Boundless", true, false);
            Scribe_Values.Look<bool>(ref Enlightened.isEnabled, "Enlightened", true, false);
            Scribe_Values.Look<bool>(ref Cursed.isEnabled, "Cursed", true, false);
            Scribe_Values.Look<bool>(ref FaeBlood.isEnabled, "FaeBlood", true, false);
            Scribe_Values.Look<bool>(ref GiantsBlood.isEnabled, "GiantsBlood", true, false);
            Scribe_Collections.Look(ref this.FactionFighterSettings, "FactionFighterSettings");
            Scribe_Collections.Look(ref this.FactionMageSettings, "FactionMageSettings");
        }
    }
}
