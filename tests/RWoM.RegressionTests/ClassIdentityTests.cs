using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ClassIdentityTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassIdentity.RegistryLoadRefreshesTraitLists", RegistryLoadRefreshesTraitLists);
        yield return new("ClassIdentity.ManualReportExplainsRejectedDegreeWithoutRepair", ManualReport);
    }

    static void ManualReport(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var oldClasses = TM_ClassUtility.CustomClasses;
        try
        {
            var trait = new TraitDef { defName = "TM_IdentityDegree", degreeDatas = [new TraitDegreeData { degree = 0 }, new TraitDegreeData { degree = 4 }] };
            TM_ClassUtility.CustomClasses = [new TM_CustomClass { classTrait = trait, traitDegree = 4, isMage = true }];
            var p = PawnWithComps(false, false);
            p.pawn.story.traits.allTraits.Add(new Trait(trait, 0));
            var output = new System.Collections.Concurrent.ConcurrentQueue<(string Level, string Text)>();
            using var logs = LoggingCompatTests.Configure(null, null, output);
            TorannMagic.ModOptions.TM_DebugTools.RepairMissingClassData(p.pawn);
            Check.Equal(true, output.Any(x => x.Text.Contains("TM_IdentityDegree:0:suppressed=False")), "manual report includes actual trait degree and suppression");
            Check.Equal(true, output.Any(x => x.Text.Contains("resolvedMagic=False") && x.Text.Contains("cachedMagic=False")), "manual report separates authoritative and cached membership");
            Check.Equal(true, output.Any(x => x.Text.Contains("declared=[TM_IdentityDegree:4]")), "manual report identifies declared degree mismatch");
            Check.Equal(true, output.Any(x => x.Text.Contains("status=skipped")), "wrong degree remains unclassified");
            Check.Same(null!, p.magic.MagicData, "report does not create progression for rejected class");
            Check.Equal(0, p.pawn.story.traits.allTraits.Single().Degree, "report does not rewrite trait degree");
        }
        finally { TM_ClassUtility.CustomClasses = oldClasses; }
    }

    static void RegistryLoadRefreshesTraitLists(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var oldClasses = TM_ClassUtility.CustomClasses;
        var categories = new[] { TM_ClassUtility.CustomBaseClasses, TM_ClassUtility.CustomMageClasses,
            TM_ClassUtility.CustomFighterClasses, TM_ClassUtility.CustomAdvancedClasses };
        var oldCategories = categories.Select(list => list.ToArray()).ToArray();
        var oldDefs = DefDatabase<TM_CustomClassDef>.AllDefsListForReading.ToArray();
        var oldSettings = TorannMagic.ModOptions.Settings.Instance;
        try
        {
            TM_ClassUtility.CustomClasses = [];
            TM_Data.ResetDefinitionCaches();
            var beforeMagic = TM_Data.MagicTraits;
            var beforeMight = TM_Data.MightTraits;
            var trait = new TraitDef { defName = "TM_IdentityHybrid", degreeDatas = [new TraitDegreeData { label = "hybrid", degree = 4 }] };
            var cls = new TM_CustomClass { classTrait = trait, traitDegree = 4, isMage = true, isFighter = true };
            DefDatabase<TM_CustomClassDef>.Clear();
            DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [cls] });
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
            TM_ClassUtility.LoadCustomClasses();
            Check.Equal(true, TM_Data.MagicTraits.Contains(trait), "deferred class registration refreshes mage identity used by grimoire label");
            Check.Equal(true, TM_Data.MightTraits.Contains(trait), "deferred class registration refreshes fighter identity");
            var pawn = PawnWithComps(true, true);
            pawn.pawn.story.traits.allTraits.Add(new Trait(trait, 4));
            pawn.magic.MagicData.MagicUserXP = 2000;
            pawn.magic.MagicData.MagicUserLevel = 7;
            pawn.magic.MagicData.MagicAbilityPoints = 3;
            var savedData = pawn.magic.MagicData;
            TM_ClassUtility.LoadCustomClasses();
            Check.Same(savedData, pawn.magic.MagicData, "registry refresh never replaces saved progression");
            Check.Equal(2000, savedData.MagicUserXP, "registry refresh preserves XP exactly");
            Check.Equal(7, savedData.MagicUserLevel, "registry refresh does not infer levels from XP");
            Check.Equal(3, savedData.MagicAbilityPoints, "registry refresh does not grant points");
            DefDatabase<TM_CustomClassDef>.GetNamed("TM_CustomClasses").customClasses = [];
            TM_ClassUtility.LoadCustomClasses();
            Check.Equal(false, TM_Data.MagicTraits.Contains(trait), "registry removal invalidates mage identity too");
            Check.Equal(false, TM_Data.MightTraits.Contains(trait), "registry removal invalidates fighter identity too");
        }
        finally
        {
            DefDatabase<TM_CustomClassDef>.Clear();
            foreach (var def in oldDefs) DefDatabase<TM_CustomClassDef>.Add(def);
            TM_ClassUtility.CustomClasses = oldClasses;
            TM_ClassUtility.LoadClassIndexes();
            TorannMagic.ModOptions.Settings.Instance = oldSettings;
            for (int i = 0; i < categories.Length; i++) { categories[i].Clear(); categories[i].AddRange(oldCategories[i]); }
            TM_Data.ResetDefinitionCaches();
        }
    }
}
