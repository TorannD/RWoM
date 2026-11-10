using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;

namespace RWoM.RegressionTests;

// The Grimoire only renders (and only allows spending points on) powers that
// pass the class-group filters. Commander, Super Soldier and Apothecary once
// had no filter entry at all: their owners could spend points on attributes
// but no skill rows were drawn. These tests pin the maps to the full set of
// class power collections so a new class cannot be silently missed again.
internal static class ClassGroupTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassGroups.MightMapCoversAllCollections", MightCoverage);
        yield return new("ClassGroups.MagicMapCoversAllCollections", MagicCoverage);
        yield return new("ClassGroups.CommanderGroupResolves", CommanderResolves);
        yield return new("Autocast.MaintainedAbilitiesNeverAutocast", Maintained);
    }

    private static readonly string[] MightExcluded =
    {
        // granted through custom classes, no built-in class trait exists
        "MightPowersShadow",
        "MightPowersCustom", "MightPowersCustomStandalone", "MightPowersCustomAll", "MightPowersStandalone",
    };

    private static readonly string[] MagicExcluded =
    {
        // granted through custom classes, no built-in class trait exists
        "magicPowerShadow",
        "magicPowerCustom", "magicPowerCustomStandalone", "magicPowerStandalone",
    };

    private static IEnumerable<string> MightCollectionNames()
    {
        return typeof(MightData).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.PropertyType == typeof(List<MightPower>) && p.Name.StartsWith("MightPowers"))
            .Select(p => p.Name)
            .Where(n => !MightExcluded.Contains(n));
    }

    private static IEnumerable<string> MagicCollectionNames()
    {
        return typeof(MagicData).GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.FieldType == typeof(List<MagicPower>) && p.Name.StartsWith("magicPower"))
            .Select(p => p.Name)
            .Where(n => !MagicExcluded.Contains(n));
    }

    private static void MightCoverage(RuntimeFixture fixture)
    {
        var mapped = TM_ClassGroups.MightEntriesField.Select(e => e.list).ToHashSet();
        var missing = MightCollectionNames().Where(n => !mapped.Contains(n)).ToList();
        Check.Equal(0, missing.Count, "might class collections without a Grimoire group: " + string.Join(", ", missing));
        foreach (var entry in TM_ClassGroups.MightEntriesField)
        {
            Check.NotNull(typeof(MightData).GetProperty(entry.list, BindingFlags.Instance | BindingFlags.Public),
                "mapped might collection exists: " + entry.list);
        }
    }

    private static void MagicCoverage(RuntimeFixture fixture)
    {
        var mapped = TM_ClassGroups.MagicEntriesField.Select(e => e.field).ToHashSet();
        var missing = MagicCollectionNames().Where(n => !mapped.Contains(n)).ToList();
        Check.Equal(0, missing.Count, "magic class collections without a Grimoire group: " + string.Join(", ", missing));
        foreach (var entry in TM_ClassGroups.MagicEntriesField)
        {
            Check.NotNull(typeof(MagicData).GetField(entry.field, BindingFlags.Instance | BindingFlags.Public),
                "mapped magic collection exists: " + entry.field);
        }
    }

    private static void CommanderResolves(RuntimeFixture fixture)
    {
        var data = new MightData();
        var powers = new List<MightPower>();
        // MightPowersC is a lazy property without a setter: write its backing field
        typeof(MightData).GetField("mightPowerC", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(data, powers);
        var pawn = new Pawn();
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        var trait = DefDatabase<TraitDef>.GetNamedSilentFail("TM_Commander");
        if (trait == null)
        {
            trait = new TraitDef { defName = "TM_Commander" };
            DefDatabase<TraitDef>.Add(trait);
        }
        pawn.story.traits.allTraits.Add(new Trait(trait));
        var group = TM_ClassGroups.GetMightClassGroup(pawn, data);
        Check.Same(powers, group, "Commander trait resolves the MightPowersC group for the Grimoire");
    }

    private static void Maintained(RuntimeFixture fixture)
    {
        string[] maintained =
        {
            "TM_EnchantedBody", "TM_Shadow", "TM_Entertain", "TM_Prediction", "TM_EnchantedAura",
            "TM_Sprint", "TM_Sprint_II", "TM_ThickSkin", "TM_StrongBack", "TM_FightersFocus",
            "TM_ManaShield", "TM_Stoneskin", "TM_Blur", "TM_BattleHymn", "TM_Invisibility",
            "TM_HeatShield", "TM_SoulBond", "TM_ReverseTime", "TM_MageLight", "TM_InnerHealing",
            "TM_AnimalFriend", "TM_ChaosTradition", "TM_BurningFury", "TM_BladeFocus", "TM_SniperFocus",
            "TM_HeavyBlow", "TM_GearRepair", "TM_SuppressiveAura", "TM_Nightshade",
            "TM_RayofHope", "TM_RayofHope_II", "TM_Soothe", "TM_P_RayofHope",
        };
        foreach (string name in maintained)
        {
            // shipped custom powers are already registered by ResetDefinitions
            TMAbilityDef def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name) ?? fixture.AddAbility(name);
            var config = TorannMagic.TMDefs.TM_AutocastDefaults.For(def, false);
            Check.Equal(TorannMagic.TMDefs.AutocastType.Null, config.type,
                name + " (a maintained toggle) is never autocast; def=" + def.defName + ", NeverAutocast=" + TorannMagic.TMDefs.TM_AutocastDefaults.NeverAutocast(def) + ", type=" + config.type);
        }
        TMAbilityDef fireball = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_Fireball") ?? fixture.AddAbility("TM_Fireball");
        var ordinary = TorannMagic.TMDefs.TM_AutocastDefaults.For(fireball, false);
        Check.NotEqual(TorannMagic.TMDefs.AutocastType.Null, ordinary.type, "instant offense abilities keep their autocast");
    }
}
