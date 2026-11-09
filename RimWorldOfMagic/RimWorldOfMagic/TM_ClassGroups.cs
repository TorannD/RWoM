using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    /// <summary>
    /// Complete class-trait to power-list maps for the Grimoire. The Grimoire only
    /// renders (and only allows spending skill points on) powers that pass these
    /// filters, so every built-in class power collection must have an entry here.
    /// ClassGroupTests asserts the coverage so a new class cannot be silently
    /// missed again (Commander, Super Soldier and Apothecary once were).
    /// </summary>
    public static class TM_ClassGroups
    {
        // trait defName -> MightData power-list property
        private static readonly (string trait, string list)[] MightMap =
        {
            ("Gladiator", "MightPowersG"),
            ("TM_Sniper", "MightPowersS"),
            ("Bladedancer", "MightPowersB"),
            ("Ranger", "MightPowersR"),
            ("Faceless", "MightPowersF"),
            ("TM_Psionic", "MightPowersP"),
            ("DeathKnight", "MightPowersDK"),
            ("TM_Monk", "MightPowersM"),
            ("TM_Wayfarer", "MightPowersW"),
            ("TM_Commander", "MightPowersC"),
            ("TM_SuperSoldier", "MightPowersSS"),
            ("TM_Apothecary", "MightPowersApothecary"),
        };

        // trait defName -> MagicData power-list field
        private static readonly (string trait, string field)[] MagicMap =
        {
            ("InnerFire", "magicPowerIF"),
            ("HeartOfFrost", "magicPowerHoF"),
            ("StormBorn", "magicPowerSB"),
            ("Arcanist", "magicPowerA"),
            ("Paladin", "magicPowerP"),
            ("Summoner", "magicPowerS"),
            ("Druid", "magicPowerD"),
            ("Necromancer", "magicPowerN"),
            ("Lich", "magicPowerN"),
            ("Priest", "magicPowerPR"),
            ("TM_Bard", "magicPowerB"),
            ("Succubus", "magicPowerSD"),
            ("Warlock", "magicPowerWD"),
            ("Geomancer", "magicPowerG"),
            ("Technomancer", "magicPowerT"),
            ("BloodMage", "magicPowerBM"),
            ("Enchanter", "magicPowerE"),
            ("Chronomancer", "magicPowerC"),
            ("TM_Wanderer", "magicPowerW"),
            ("ChaosMage", "magicPowerCM"),
            ("TM_Brightmage", "magicPowerBrightmage"),
            ("TM_Shaman", "magicPowerShaman"),
            ("TM_Golemancer", "magicPowerGolemancer"),
        };

        // exposed for the coverage regression tests
        public static (string trait, string list)[] MightEntriesField => MightMap;
        public static (string trait, string field)[] MagicEntriesField => MagicMap;

        public static List<MightPower> GetMightClassGroup(Pawn pawn, MightData data)
        {
            if (pawn?.story?.traits == null || data == null)
            {
                return null;
            }
            foreach (var entry in MightMap)
            {
                TraitDef trait = DefDatabase<TraitDef>.GetNamedSilentFail(entry.trait);
                if (trait != null && pawn.story.traits.HasTrait(trait))
                {
                    return ResolveMightList(data, entry.list);
                }
            }
            return null;
        }

        public static List<MagicPower> GetMagicClassGroup(Pawn pawn, MagicData data)
        {
            if (ClassProgression.ClassIdentity.TryGetState(pawn, out _))
                return ClassProgression.ClassSpellAccess.ClassPowers(pawn, data);
            if (pawn?.story?.traits == null || data == null)
            {
                return null;
            }
            foreach (var entry in MagicMap)
            {
                TraitDef trait = DefDatabase<TraitDef>.GetNamedSilentFail(entry.trait);
                if (trait != null && pawn.story.traits.HasTrait(trait))
                {
                    return ResolveMagicList(data, entry.field);
                }
            }
            return null;
        }

        public static List<MightPower> ResolveMightList(MightData data, string propertyName)
        {
            return typeof(MightData).GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.GetValue(data) as List<MightPower>;
        }

        public static List<MagicPower> ResolveMagicList(MagicData data, string fieldName)
        {
            string propertyName = "MagicPowers" + fieldName.Substring("magicPower".Length);
            return typeof(MagicData).GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.GetValue(data) as List<MagicPower>;
        }
    }
}
