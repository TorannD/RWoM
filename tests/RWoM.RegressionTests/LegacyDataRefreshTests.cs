using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;

namespace RWoM.RegressionTests;

internal static class LegacyDataRefreshTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LegacyDataRefresh.MightSavedEmptyListDiscoversNewPowers", f =>
        {
            var def = FighterDef(f, "TM_AuditLegacyFighter");
            var data = new MightData { mightPowerCustom = [], mightPowerSkill_Custom = [], customPowersInitialized = true };
            var owner = data.MightPowersCustom.SingleOrDefault(x => x.TMabilityDefs.Contains(def));
            Check.NotNull(owner, "loaded empty custom list cannot suppress current definitions");
            Check.Equal(7, Check.NotNull(data.GetSkill_Power(def), "new fighter skill").levelMax, "definition maximum");
        });
        yield return new("LegacyDataRefresh.MightPreservesInvestedOwnerAndNewDefinition", f =>
        {
            var first = FighterDef(f, "TM_AuditFirstFighter");
            var data = new MightData();
            var saved = data.MightPowersCustom.Single(x => x.TMabilityDefs.Contains(first));
            saved.level = 0; saved.learned = true;
            var skill = Check.NotNull(data.GetSkill_Power(first), "initial skill");
            skill.level = 4; skill.levelMax = 11; skill.costToLevel = 3;
            data.MightAbilityPoints = 9;
            var second = FighterDef(f, "TM_AuditSecondFighter");
            Check.NotNull(data.MightPowersCustom.SingleOrDefault(x => x.TMabilityDefs.Contains(second)), "new definition discovered after prior access");
            Check.Same(saved, data.MightPowersCustom.Single(x => x.TMabilityDefs.Contains(first)), "saved power owner retained");
            Check.Same(skill, data.GetSkill_Power(first), "saved skill owner retained");
            Check.Equal(4, skill.level, "investment unchanged");
            Check.Equal(11, skill.levelMax, "saved maximum unchanged");
            Check.Equal(3, skill.costToLevel, "saved cost unchanged");
            Check.Equal(9, data.MightAbilityPoints, "no payment or refund during discovery");
        });
        yield return new("LegacyDataRefresh.MightCachedMissRebindsNewDefinition", f =>
        {
            var ability = f.AddAbility("TM_AuditLateFighter");
            var data = new MightData();
            Check.Same(null!, data.GetSkill_Power(ability), "initial missing definition");
            FighterDef(f, ability.defName);
            Check.NotNull(data.GetSkill_Power(ability), "new definition invalidates cached null");
        });
        yield return new("LegacyDataRefresh.RegistryReloadRebindsClassReferences", RegistryRebind);
    }

    static TMAbilityDef FighterDef(RuntimeFixture f, string name)
    {
        var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name) ?? f.AddAbility(name);
        DefDatabase<TM_CustomPowerDef>.Add(new TM_CustomPowerDef { defName = name, customPower = new TM_CustomPower {
            forFighter = true, forMage = false, abilityDefs = [ability], skills = [new TM_CustomSkill {
                label = name + "_pwr", description = name + "_pwr_desc", levelMax = 7, costToLevel = 2
            }]
        }});
        return ability;
    }

    static void RegistryRebind(RuntimeFixture f)
    {
        var refresh = typeof(MagicData).Assembly.GetType("TorannMagic.Compatibility.TM_DefReload");
        Check.NotNull(refresh, "definition-reload bridge exists");
        var oldClasses = TM_ClassUtility.CustomClasses;
        var categories = new[] { TM_ClassUtility.CustomBaseClasses, TM_ClassUtility.CustomMageClasses,
            TM_ClassUtility.CustomFighterClasses, TM_ClassUtility.CustomAdvancedClasses };
        var oldCategories = categories.Select(list => list.ToArray()).ToArray();
        var oldDef = DefDatabase<TM_CustomClassDef>.GetNamedSilentFail("TM_CustomClasses");
        var oldSettings = TorannMagic.ModOptions.Settings.Instance;
        try
        {
            var staleTrait = new TraitDef { defName = "TM_AuditClass", degreeDatas = [new TraitDegreeData { label = "Старое имя" }] };
            var freshTrait = new TraitDef { defName = "TM_AuditClass", degreeDatas = [new TraitDegreeData { label = "Fresh class" }] };
            TM_ClassUtility.CustomClasses = [new TM_CustomClass { classTrait = staleTrait, isMage = true }];
            DefDatabase<TM_CustomClassDef>.Clear();
            var freshClass = new TM_CustomClass { classTrait = freshTrait, isMage = true };
            DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [freshClass] });
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
            refresh!.GetMethod("RefreshDefinitions", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
            Check.Same(freshClass, TM_ClassUtility.CustomClasses.Single(), "old class references replaced by current definitions");
            Check.Same(freshTrait, TM_ClassUtility.CustomMageClasses.Single().classTrait, "debug/category lists use current language trait");
        }
        finally
        {
            DefDatabase<TM_CustomClassDef>.Clear();
            if (oldDef != null) DefDatabase<TM_CustomClassDef>.Add(oldDef);
            TM_ClassUtility.CustomClasses = oldClasses;
            TorannMagic.ModOptions.Settings.Instance = oldSettings;
            for (int i = 0; i < categories.Length; i++)
            {
                categories[i].Clear(); categories[i].AddRange(oldCategories[i]);
            }
        }
    }
}
