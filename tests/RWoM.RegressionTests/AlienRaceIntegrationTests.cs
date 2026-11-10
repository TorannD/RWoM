using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

internal static class AlienRaceIntegrationTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IntegrationHAR.DependencyBoundary", f =>
        {
            Check.Equal(false, typeof(TorannMagic.MagicData).Assembly.GetReferencedAssemblies().Any(a => a.Name == "AlienRace"), "Content has no HAR assembly reference");
            var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
            Check.NotNull(adapter.GetType("TorannMagic.Integrations.AlienRaceTraitRestrictions"), "Integrations owns HAR restrictions");
        });
        var path = Environment.GetEnvironmentVariable("RWOM_TEST_HAR");
        if (string.IsNullOrEmpty(path)) yield break;
        yield return new("IntegrationHAR.InstalledRestrictions", f =>
        {
            var har = Assembly.LoadFrom(path);
            var raceType = har.GetType("AlienRace.ThingDef_AlienRace", true)!;
            var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(raceType);
            var alienField = raceType.GetField("alienRace")!;
            var settings = RuntimeHelpers.GetUninitializedObject(alienField.FieldType);
            alienField.SetValue(race, settings);
            var restrictionsField = settings.GetType().GetField("raceRestriction")!;
            var restrictions = RuntimeHelpers.GetUninitializedObject(restrictionsField.FieldType);
            restrictionsField.SetValue(settings, restrictions);
            var trait = new TraitDef { defName = "TestHarForbidden" };
            var list = new List<TraitDef>();
            restrictions.GetType().GetField("traitList")!.SetValue(restrictions, list);
            var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Integrations.AlienRaceTraitRestrictions", true)!;
            var allows = adapter.GetMethod("Allows", BindingFlags.Public | BindingFlags.Static)!;
            var pawn = new Pawn();
            bool Query(ThingDef def) => (bool)allows.Invoke(null, [def, pawn, trait])!;
            Check.Equal(true, Query(race), "empty legacy restriction list permits trait");
            list.Add(trait);
            Check.Equal(false, Query(race), "legacy race restriction retained");
            list.Clear();
            pawn.story = new Pawn_StoryTracker(pawn);
            pawn.story.Childhood = new BackstoryDef { disallowedTraits = [new BackstoryTrait { def = trait }] };
            Check.Equal(false, Query(race), "backstory restriction retained");
            Check.Equal(true, Query((ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))), "ordinary races unaffected");
            var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
            const string id = "rwom.tests.har-boundary";
            var harmony = Activator.CreateInstance(harmonyType, id)!;
            try
            {
                var processor = harmonyType.GetMethod("CreateClassProcessor")!.Invoke(harmony, [adapter])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
                Check.Equal(false, TorannMagic.ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(race, pawn, trait), "public legacy caller reaches optional adapter");
            }
            finally { harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
            Check.Equal(true, TorannMagic.ModCheck.AlienHumanoidRaces.TryGetBackstory_DisallowedTrait(race, pawn, trait), "Content works when integration removed");
        });
    }
}
