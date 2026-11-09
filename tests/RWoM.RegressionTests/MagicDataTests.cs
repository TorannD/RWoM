using System.Collections;
using System.Reflection;
using System.Xml.Linq;
using TorannMagic;
using TorannMagic.TMDefs;

namespace RWoM.RegressionTests;

internal static class MagicDataTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MagicData.UnresolvedCustomOwnerDoesNotBreakLookup", UnresolvedOwner);
        yield return new("MagicData.EmpathyLevel4SurvivesScribe", EmpathyLevel4SurvivesScribe);
        yield return new("MagicData.FreshEmpathyUsesXmlMaximum", FreshEmpathyUsesXmlMaximum);
        yield return new("MagicData.DistortSpiritUsesXmlCost", DistortSpiritUsesXmlCost);
        yield return new("MagicData.AllSevenTreesHaveOneSavedOwner", AllSevenTreesHaveOneSavedOwner);
        yield return new("MagicData.LegacyCustomLoadHasOneLookup", LegacyCustomLoadHasOneLookup);
        yield return new("MagicData.LegacyDuplicatesKeepHighestSavedLevel", LegacyDuplicatesKeepHighestSavedLevel);
        yield return new("MagicData.UnknownLegacySkillsSurvive", UnknownLegacySkillsSurvive);
        yield return new("MagicData.LazyLookupIgnoresStaleInitializedFlag", LazyLookupIgnoresStaleInitializedFlag);
        yield return new("MagicData.LazyLookupDoesNotInitializeUnrelatedPowers", LazyLookupDoesNotInitializeUnrelatedPowers);
        yield return new("MagicData.CachedMissIsInvalidatedByNewDefinition", CachedMissIsInvalidatedByNewDefinition);
        yield return new("MagicData.RepeatedInitializationKeepsLeveledObject", RepeatedInitializationKeepsLeveledObject);
        yield return new("MagicData.FreshStandaloneDataRoundTrip", FreshStandaloneDataRoundTrip);
        yield return new("MagicData.AliasedAbilityUsesCanonicalSkill", AliasedAbilityUsesCanonicalSkill);
        yield return new("MagicData.AggregateInitializationAndRefresh", AggregateInitializationAndRefresh);
        yield return new("MagicData.DuplicateAddedAfterLookupInvalidatesCaches", DuplicateAddedAfterLookupInvalidatesCaches);
        yield return new("MagicData.LoadedCustomPowerIsNotReplaced", LoadedCustomPowerIsNotReplaced);
        yield return new("MagicData.SharedBuiltinSkillKeepsExistingOwner", SharedBuiltinSkillKeepsExistingOwner);
        yield return new("MagicData.SameCountReplacementKeepsLookupAttachedToSave", SameCountReplacementKeepsLookupAttachedToSave);
        yield return new("MagicData.SameCountDuplicateIsNormalizedBeforeSave", SameCountDuplicateIsNormalizedBeforeSave);
        yield return new("MagicData.RepeatedMissReusesDiscoveryUntilGenerationChanges", RepeatedMissReusesDiscoveryUntilGenerationChanges);
    }

    private static void UnresolvedOwner(RuntimeFixture fixture)
    {
        var baseDef=fixture.Ability("TM_Empathy");
        var upper=fixture.Ability("TM_DistortSpirit");
        var magic=new MagicPower { TMabilityDefs=[], level=4, ticksUntilNextCast=82 };
        var might=new MightPower { TMabilityDefs=null, level=3, ticksUntilNextCast=76 };
        var data=new MagicData { magicPowerCustom=[magic,null!] };
        var active=data.MagicPowersCustom;
        Check.Equal(true,active.Contains(magic),"saved record remains owned");
        Check.Equal(1,active.Count(p=>p?.GetAbilityDef(0)==baseDef),"live custom ability initialized once");
        foreach(var field in typeof(MagicData).GetFields())
            if(field.FieldType==typeof(List<MagicPower>)&&!field.Name.Contains("Custom"))field.SetValue(data,new List<MagicPower>());
        data.magicPowerP.AddRange(Enumerable.Range(0,6).Select(_=>new MagicPower()));
        data.GetHediffAbility(new Verse.Hediff { def=new Verse.HediffDef { defName="UnrelatedEffect" } });
        Check.Equal(true,data.AllMagicPowers.All(p=>p!=magic && p!=null),"orphan excluded from active aggregate");
        Check.Equal(true,magic.GetAbilityDef(0)==null && magic.nextLevelAbilityDef==null && magic.Icon==null,"missing magic rank has no display ability");
        Check.Equal(true,might.GetAbilityDef(0)==null && might.nextLevelAbilityDef==null && might.Icon==null,"missing might rank has no display ability");
        Check.Equal(4,magic.level,"lookup preserves saved magic level");
        Check.Equal(82,magic.ticksUntilNextCast,"lookup preserves cooldown");
        Check.Equal(3,might.level,"lookup preserves saved might level");
        magic.TMabilityDefs=[baseDef,upper];might.TMabilityDefs=[baseDef,upper];
        Check.Same(baseDef,magic.GetAbilityDef(8),"out-of-range magic rank keeps original first-rank fallback");
        Check.Same(baseDef,might.GetAbilityDef(-1),"out-of-range might rank keeps original first-rank fallback");
        Check.Same(upper,might.GetAbilityDef(1),"valid rank remains accessible");
        var orphan=new MagicPower {TMabilityDefs=[],ticksUntilNextCast=82};
        var neighbor=new MagicPower {TMabilityDefs=[baseDef,upper],level=1,maxLevel=1,learned=true,autocast=true,ticksUntilNextCast=293};
        var loaded=fixture.Load(fixture.Save(new MagicData {magicPowerCustom=[orphan,neighbor]}));
        var raw=loaded.MagicPowersCustom;
        var unavailable=raw.Single(p=>p.TMabilityDefs?.Count==0 && p.ticksUntilNextCast==82);
        Check.Equal(false,loaded.AllMagicPowers.Contains(unavailable),"reloaded orphan remains dormant");
        var valid=raw.Single(p=>p.TMabilityDefs?.Contains(baseDef)==true);
        Check.Equal(1,valid.level,"neighboring learned ability keeps its rank through Scribe");
        Check.Equal(true,valid.learned&&valid.autocast,"neighboring learned and autocast flags survive");
        Check.Equal(293,valid.ticksUntilNextCast,"neighboring cooldown survives");
    }

    private static void EmpathyLevel4SurvivesScribe(RuntimeFixture fixture)
    {
        var data = new MagicData();
        Check.NotNull(data.GetSkill_Power(fixture.Ability("TM_Empathy")), "Empathy power").level = 4;
        var loaded = fixture.Load(fixture.Save(data));
        Check.Equal(4, Check.NotNull(loaded.GetSkill_Power(fixture.Ability("TM_Empathy")), "loaded Empathy").level,
            "Empathy power level after real Scribe round trip");
    }

    private static void FreshEmpathyUsesXmlMaximum(RuntimeFixture fixture)
    {
        var skill = Check.NotNull(new MagicData().GetSkill_Power(fixture.Ability("TM_Empathy")), "Empathy power");
        Check.Equal(5, skill.levelMax, "fresh Empathy maximum from shipped XML");
    }

    private static void DistortSpiritUsesXmlCost(RuntimeFixture fixture)
    {
        var data = new MagicData();
        foreach (string ability in new[] { "TM_DistortSpirit", "TM_DistortSpirit_I", "TM_DistortSpirit_II", "TM_DistortSpirit_III" })
            Check.Equal(2, Check.NotNull(data.GetSkill_Versatility(fixture.Ability(ability)), "Distort Spirit versatility").costToLevel,
                ability + " versatility cost from shipped XML");
    }

    private static void AllSevenTreesHaveOneSavedOwner(RuntimeFixture fixture)
    {
        var data = new MagicData();
        _ = data.MagicPowersCustom;
        foreach (var definition in fixture.Definitions)
            foreach (var expected in definition.customPower.skills)
            {
                var canonical = data.MagicPowerSkill_Custom.Where(skill => skill.label == expected.label).ToList();
                Check.Equal(1, canonical.Count, expected.label + " saved custom object count");
                Check.Equal(1, data.AllMagicPowerSkills.Count(skill => skill.label == expected.label), expected.label + " aggregate count");
                Check.Same(canonical[0], Skill(data, (TMAbilityDef)definition.customPower.abilityDefs[0], expected.label), expected.label + " lookup owner");
                Check.Equal(expected.levelMax, canonical[0].levelMax, expected.label + " XML maximum");
                Check.Equal(expected.costToLevel, canonical[0].costToLevel, expected.label + " XML cost");
                Check.Equal(expected.description, canonical[0].desc, expected.label + " XML description key");
            }
    }

    private static void LegacyCustomLoadHasOneLookup(RuntimeFixture fixture)
    {
        var data = fixture.LoadLegacy(Saved("TM_Empathy_pwr", 4, 5, 1));
        var result = Check.NotNull(data.GetSkill_Power(fixture.Ability("TM_Empathy")), "legacy lookup");
        Check.Equal(4, result.level, "legacy saved level");
        Check.Equal(5, result.levelMax, "legacy saved maximum");
        Check.Equal(1, data.AllMagicPowerSkills.Count(skill => skill.label == result.label), "legacy aggregate count");
        Check.Same(data.MagicPowerSkill_Custom.Single(skill => skill.label == result.label), result, "legacy canonical lookup");
    }

    private static void LegacyDuplicatesKeepHighestSavedLevel(RuntimeFixture fixture)
    {
        var data = fixture.LoadLegacy(Saved("TM_Empathy_pwr", 0, 3, 1), Saved("TM_Empathy_pwr", 4, 7, 2), Saved("TM_Empathy_pwr", 2, 5, 1));
        var result = Check.NotNull(data.GetSkill_Power(fixture.Ability("TM_Empathy")), "duplicate legacy lookup");
        Check.Equal(4, result.level, "highest saved duplicate level");
        Check.Equal(7, result.levelMax, "leveled saved object's maximum is not rewritten");
        Check.Equal(2, result.costToLevel, "leveled saved object's cost is not rewritten");
        Check.Equal(1, data.MagicPowerSkill_Custom.Count(skill => skill.label == result.label), "duplicate saved labels merged");
        var reloaded = fixture.Load(fixture.Save(data));
        Check.Equal(4, Check.NotNull(reloaded.GetSkill_Power(fixture.Ability("TM_Empathy")), "reloaded duplicate").level, "merged duplicate round trip");
    }

    private static void UnknownLegacySkillsSurvive(RuntimeFixture fixture)
    {
        var data = fixture.LoadLegacy(Saved("TM_RemovedAddon_pwr", 6, 9, 3), Saved("TM_RemovedAddon_pwr", 1, 3, 1));
        var loaded = fixture.Load(fixture.Save(data));
        var skills = loaded.MagicPowerSkill_Custom.Where(skill => skill.label == "TM_RemovedAddon_pwr").ToList();
        Check.Equal(1, skills.Count, "unknown legacy label retained once");
        Check.Equal(6, skills[0].level, "unknown legacy level");
        Check.Equal(9, skills[0].levelMax, "unknown legacy maximum");
        Check.Equal(3, skills[0].costToLevel, "unknown legacy cost");
    }

    private static void LazyLookupIgnoresStaleInitializedFlag(RuntimeFixture fixture)
    {
        var data = new MagicData { initialized = true, customPowersInitialized = true, magicPowerCustom = [] };
        var skill = Check.NotNull(data.GetSkill_Power(fixture.Ability("TM_Empathy")), "lazy Empathy lookup");
        Check.Equal(5, skill.levelMax, "lazy lookup gets XML maximum despite old initialized flags");
        Check.Same(skill, data.MagicPowerSkill_Custom.Single(item => item.label == skill.label), "lazy lookup uses saved owner");
    }

    private static void LazyLookupDoesNotInitializeUnrelatedPowers(RuntimeFixture fixture)
    {
        // An unrelated definition with no resolved abilities must not be constructed by
        // a skill lookup. This catches replacing lazy initialization with a full power walk.
        fixture.AddDefinition(new TM_CustomPowerDef
        {
            defName = "UnrelatedUnavailablePower",
            customPower = new TM_CustomPower { forMage = true, abilityDefs = [null!], skills = null! }
        });
        var data = new MagicData { initialized = true };
        var skill = Check.NotNull(data.GetSkill_Versatility(fixture.Ability("TM_DistortSpirit")), "targeted lookup");
        Check.Equal(2, skill.costToLevel, "targeted lookup uses XML without constructing unrelated powers");
    }

    private static void CachedMissIsInvalidatedByNewDefinition(RuntimeFixture fixture)
    {
        var data = new MagicData();
        var ability = fixture.AddAbility("Regression_LateAbility");
        Check.Equal<MagicPowerSkill?>(null, data.GetSkill_Power(ability), "initial power miss");
        Check.Equal<MagicPowerSkill?>(null, data.GetSkill_Versatility(ability), "initial versatility miss");
        Check.Equal<MagicPowerSkill?>(null, data.GetSkill_Efficiency(ability), "initial efficiency miss");
        _ = data.AllMagicPowerSkills;
        _ = data.MagicPowersCustom;
        fixture.AddDefinition(new TM_CustomPowerDef
        {
            defName = "Regression_LatePower",
            customPower = new TM_CustomPower
            {
                forMage = true, abilityDefs = [ability],
                skills = new[] { "pwr", "ver", "eff" }.Select(suffix => new TM_CustomSkill
                {
                    label = "Regression_LateAbility_" + suffix, description = "late_desc", levelMax = 6, costToLevel = 2
                }).ToList()
            }
        });
        _ = data.MagicPowersCustom;
        foreach (string suffix in new[] { "pwr", "ver", "eff" })
        {
            var skill = Check.NotNull(Skill(data, ability, "Regression_LateAbility_" + suffix), suffix + " after late definition");
            Check.Equal(6, skill.levelMax, suffix + " late maximum");
            Check.Equal(1, data.AllMagicPowerSkills.Count(item => item.label == skill.label), suffix + " updated aggregate");
        }
    }

    private static void RepeatedInitializationKeepsLeveledObject(RuntimeFixture fixture)
    {
        var data = new MagicData();
        var skill = Check.NotNull(data.GetSkill_Power(fixture.Ability("TM_Empathy")), "initial lookup");
        skill.level = 4;
        for (int iteration = 0; iteration < 5; iteration++)
        {
            _ = data.MagicPowersCustom;
            _ = data.MagicPowersCustomAll;
            data.ClearSkill_Dictionaries();
            Check.Same(skill, data.GetSkill_Power(fixture.Ability("TM_Empathy")), "repeat keeps canonical object");
            Check.Equal(4, skill.level, "repeat keeps level");
            Check.Equal(1, data.MagicPowerSkill_Custom.Count(item => item.label == skill.label), "repeat custom count");
            Check.Equal(1, data.MagicPowersCustom.Count(power => power.TMabilityDefs.Contains(fixture.Ability("TM_Empathy"))), "repeat power count");
        }
    }

    private static void FreshStandaloneDataRoundTrip(RuntimeFixture fixture)
    {
        // Standalone MagicData serialization only; no pawn serializer, map, graphics, or game startup.
        var data = new MagicData();
        _ = data.MagicPowersCustom;
        foreach (var definition in fixture.Definitions)
            foreach (var expected in definition.customPower.skills)
                Check.NotNull(Skill(data, (TMAbilityDef)definition.customPower.abilityDefs[0], expected.label), expected.label).level = 2;
        data.GetSkill_Power(fixture.Ability("TM_Empathy")).level = 4;
        string path = fixture.Save(data);
        var saved = XDocument.Load(path).Root!.Element("magicData")!.Element("magicPowerSkill_Custom")!;
        Check.Equal(21, saved.Elements("li").Count(), "fresh save has all 21 XML skills");
        var loaded = fixture.Load(path);
        foreach (var definition in fixture.Definitions)
            foreach (var expected in definition.customPower.skills)
            {
                Check.Equal(1, saved.Elements("li").Count(item => (string?)item.Element("label") == expected.label), expected.label + " serialized once");
                var result = Check.NotNull(Skill(loaded, (TMAbilityDef)definition.customPower.abilityDefs[0], expected.label), expected.label);
                Check.Equal(expected.label == "TM_Empathy_pwr" ? 4 : 2, result.level, expected.label + " loaded level");
                Check.Equal(expected.levelMax, result.levelMax, expected.label + " loaded XML maximum");
                Check.Equal(expected.costToLevel, result.costToLevel, expected.label + " loaded XML cost");
            }
    }

    private static void AliasedAbilityUsesCanonicalSkill(RuntimeFixture fixture)
    {
        var alias = fixture.AddAbility("Regression_EmpathyAlias");
        fixture.Definitions.Single(def => def.defName == "TM_Empathy").customPower.abilityDefs.Add(alias);
        var data = new MagicData();
        foreach (string suffix in new[] { "pwr", "ver", "eff" })
        {
            var baseSkill = Check.NotNull(Skill(data, fixture.Ability("TM_Empathy"), "TM_Empathy_" + suffix), suffix + " base lookup");
            Check.Same(baseSkill, Skill(data, alias, "TM_Empathy_" + suffix), suffix + " alias lookup");
        }
    }

    private static void AggregateInitializationAndRefresh(RuntimeFixture fixture)
    {
        var data = new MagicData();
        // This case tests aggregation, not built-in power construction. Empty the
        // unrelated lists and provide Paladin's six migration slots without DefOf/UI assets.
        foreach (var field in typeof(MagicData).GetFields())
            if (field.FieldType == typeof(List<MagicPower>) && !field.Name.Contains("Custom"))
                field.SetValue(data, new List<MagicPower>());
        data.magicPowerP.AddRange(Enumerable.Range(0, 6).Select(_ => new MagicPower()));
        Check.Equal(13, data.AllMagicPowers.Count, "first aggregate includes custom powers and six built-in slots");
        Check.Equal(13, data.AllUniqueMagicPowers.Count, "first unique aggregate");
        Check.Equal(7, data.MagicPowersCustomAll.Count, "first custom aggregate");
        var late = fixture.AddAbility("Regression_LateAggregate");
        fixture.AddDefinition(new TM_CustomPowerDef
        {
            defName = "Regression_LateAggregate",
            customPower = new TM_CustomPower
            {
                forMage = true, abilityDefs = [late], skills = [new TM_CustomSkill { label = "Regression_LateAggregate_pwr", levelMax = 3 }]
            }
        });
        Check.Equal(14, data.AllMagicPowers.Count, "aggregate refreshes after custom membership changes");
        Check.Equal(14, data.AllMagicPowersWithSkills.Count, "skilled aggregate refreshes");
        Check.Equal(8, data.MagicPowersCustomAll.Count, "custom aggregate refreshes");
        Check.Equal(14, data.AllUniqueMagicPowers.Count, "unique aggregate refreshes");
    }

    private static void DuplicateAddedAfterLookupInvalidatesCaches(RuntimeFixture fixture)
    {
        var data = new MagicData();
        var ability = fixture.Ability("TM_Empathy");
        _ = data.GetSkill_Power(ability);
        _ = data.GetSkill_Versatility(ability);
        _ = data.GetSkill_Efficiency(ability);
        var replacement = Saved("TM_Empathy_pwr", 4, 5, 1);
        data.MagicPowerSkill_Custom.Add(replacement);
        Check.Same(replacement, data.GetSkill_Power(ability), "duplicate merge invalidates old lookup object");
        Check.Equal(1, data.AllMagicPowerSkills.Count(skill => skill.label == replacement.label), "aggregate invalidated after duplicate merge");
    }

    private static void LoadedCustomPowerIsNotReplaced(RuntimeFixture fixture)
    {
        var data = new MagicData();
        var power = data.MagicPowersCustom.Single(item => item.TMabilityDefs.Contains(fixture.Ability("TM_DistortSpirit")));
        power.level = 2;
        power.learned = true;
        power.costToLevel = 4;
        var loaded = fixture.Load(fixture.Save(data));
        var saved = loaded.magicPowerCustom.Single(item => item.TMabilityDefs.Contains(fixture.Ability("TM_DistortSpirit")));
        var initialized = loaded.MagicPowersCustom.Single(item => item.TMabilityDefs.Contains(fixture.Ability("TM_DistortSpirit")));
        Check.Same(saved, initialized, "full initialization keeps loaded power object");
        Check.Equal(2, initialized.level, "loaded power level");
        Check.Equal(true, initialized.learned, "loaded power learned state");
        Check.Equal(4, initialized.costToLevel, "loaded power cost");
    }

    private static void SharedBuiltinSkillKeepsExistingOwner(RuntimeFixture fixture)
    {
        var alias = fixture.AddAbility("Regression_SharedFireball");
        fixture.AddDefinition(new TM_CustomPowerDef
        {
            defName = "Regression_SharedFireball",
            customPower = new TM_CustomPower
            {
                forMage = true, abilityDefs = [alias],
                skills = [new TM_CustomSkill { label = "TM_Fireball_pwr", levelMax = 3, costToLevel = 1 }]
            }
        });
        var data = new MagicData();
        var builtIn = data.MagicPowerSkill_Fireball.Single(skill => skill.label == "TM_Fireball_pwr");
        builtIn.level = 2;
        Check.Same(builtIn, data.GetSkill_Power(alias), "XML alias preserves existing built-in owner");
        Check.Equal(0, data.MagicPowerSkill_Custom.Count(skill => skill.label == builtIn.label), "shared built-in is not duplicated in custom list");
        Check.Equal(2, fixture.Load(fixture.Save(data)).GetSkill_Power(alias).level, "shared built-in still saves");
    }

    private static void SameCountReplacementKeepsLookupAttachedToSave(RuntimeFixture fixture)
    {
        foreach (string suffix in new[] { "pwr", "ver", "eff" })
        {
            var data = new MagicData();
            var ability = fixture.Ability("TM_Empathy");
            string label = "TM_Empathy_" + suffix;
            var original = Check.NotNull(Skill(data, ability, label), suffix + " first lookup");
            int count = data.MagicPowerSkill_Custom.Count;
            var replacement = Saved(label, 4, 5, 1);
            data.MagicPowerSkill_Custom[data.MagicPowerSkill_Custom.IndexOf(original)] = replacement;
            Check.Equal(count, data.MagicPowerSkill_Custom.Count, "replacement preserves list count");
            var actual = Check.NotNull(Skill(data, ability, label), suffix + " after same-count replacement");
            // A cached detached object would lose this investment at the next save.
            actual.level++;
            var loaded = fixture.Load(fixture.Save(data));
            Check.Equal(5, Check.NotNull(Skill(loaded, ability, label), suffix + " reloaded").level,
                suffix + " lookup mutation remains attached to saved object");
            Check.Same(replacement, actual, suffix + " lookup canonical owner");
        }
    }

    private static void SameCountDuplicateIsNormalizedBeforeSave(RuntimeFixture fixture)
    {
        var data = new MagicData();
        _ = data.MagicPowersCustom;
        var ability = fixture.Ability("TM_Empathy");
        _ = data.GetSkill_Power(ability);
        int count = data.MagicPowerSkill_Custom.Count;
        int replaceIndex = data.MagicPowerSkill_Custom.FindIndex(skill => skill.label == "TM_Empathy_ver");
        data.MagicPowerSkill_Custom[replaceIndex] = Saved("TM_Empathy_pwr", 4, 7, 2);
        Check.Equal(count, data.MagicPowerSkill_Custom.Count, "duplicate replacement preserves list count");
        // Save directly, without a lookup that could normalize the replacement first.
        string path = fixture.Save(data);
        var saved = XDocument.Load(path).Root!.Element("magicData")!.Element("magicPowerSkill_Custom")!
            .Elements("li").Where(item => (string?)item.Element("label") == "TM_Empathy_pwr").ToList();
        Check.Equal(1, saved.Count, "same-count duplicate is written once");
        Check.Equal(4, (int)saved[0].Element("level")!, "highest duplicate level is saved");
        var loaded = fixture.Load(path).GetSkill_Power(ability);
        Check.Equal(4, loaded.level, "same-count duplicate loaded level");
        Check.Equal(7, loaded.levelMax, "same-count duplicate preserves saved maximum");
        Check.Equal(2, loaded.costToLevel, "same-count duplicate preserves saved cost");
    }

    private static void RepeatedMissReusesDiscoveryUntilGenerationChanges(RuntimeFixture fixture)
    {
        var data = new MagicData();
        var ability = fixture.AddAbility("TM_EnchantedAura");
        // Observe actual discovery, without replacing results: the wrapper enumerates
        // the production deferred query over the real DefDatabase and counts each walk.
        using var discovery = new CustomPowerDiscoveryCounter();
        Check.Equal<MagicPowerSkill?>(null, data.GetSkill_Efficiency(ability), "first Enchanted Aura efficiency miss");
        Check.Equal(1, discovery.Enumerations, "initial miss performs discovery once");
        for (int i = 0; i < 8; i++)
            Check.Equal<MagicPowerSkill?>(null, data.GetSkill_Efficiency(ability), "repeated efficiency miss");
        Check.Equal(1, discovery.Enumerations, "unchanged misses reuse negative cache");

        fixture.AddDefinition(new TM_CustomPowerDef
        {
            defName = "Regression_LateEnchantedAura",
            customPower = new TM_CustomPower
            {
                forMage = true, abilityDefs = [ability],
                skills = [new TM_CustomSkill { label = "TM_EnchantedAura_eff", levelMax = 6, costToLevel = 2 }]
            }
        });
        var late = Check.NotNull(data.GetSkill_Efficiency(ability), "late definition invalidates negative cache");
        Check.Equal(6, late.levelMax, "late definition maximum");
        Check.Equal(2, discovery.Enumerations, "definition generation triggers one rediscovery");
        for (int i = 0; i < 8; i++)
            Check.Same(late, data.GetSkill_Efficiency(ability), "positive cache keeps canonical skill");
        Check.Equal(2, discovery.Enumerations, "positive hits do not rediscover definitions");

        var membershipData = new MagicData();
        var savedOnly = fixture.AddAbility("Regression_SavedOnly");
        Check.Equal<MagicPowerSkill?>(null, membershipData.GetSkill_Efficiency(savedOnly), "missing saved-only skill");
        _ = membershipData.GetSkill_Power(fixture.Ability("TM_Empathy"));
        Check.Equal<MagicPowerSkill?>(null, membershipData.GetSkill_Efficiency(savedOnly), "cache miss after initializing unrelated skills");
        int count = membershipData.MagicPowerSkill_Custom.Count;
        var replacement = Saved("Regression_SavedOnly_eff", 4, 5, 1);
        membershipData.MagicPowerSkill_Custom[0] = replacement;
        Check.Equal(count, membershipData.MagicPowerSkill_Custom.Count, "saved-only replacement preserves count");
        Check.Same(replacement, membershipData.GetSkill_Efficiency(savedOnly), "membership change invalidates negative cache");
    }

    private sealed class CustomPowerDiscoveryCounter : IEnumerable<TM_CustomPowerDef>, IDisposable
    {
        private readonly FieldInfo field = typeof(TM_Data).GetField("_CustomMagePowerDefs", BindingFlags.Static | BindingFlags.NonPublic)!;
        private readonly IEnumerable<TM_CustomPowerDef> source;
        public int Enumerations { get; private set; }

        public CustomPowerDiscoveryCounter()
        {
            source = TM_Data.CustomMagePowerDefs();
            field.SetValue(null, this);
        }

        public IEnumerator<TM_CustomPowerDef> GetEnumerator()
        {
            Enumerations++;
            return source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Dispose() => field.SetValue(null, source);
    }

    private static MagicPowerSkill Saved(string label, int level, int max, int cost) => new()
    {
        label = label, desc = label + "_desc", level = level, levelMax = max, costToLevel = cost
    };

    private static MagicPowerSkill? Skill(MagicData data, TMAbilityDef ability, string label) => label.EndsWith("_pwr")
        ? data.GetSkill_Power(ability) : label.EndsWith("_ver") ? data.GetSkill_Versatility(ability) : data.GetSkill_Efficiency(ability);
}
