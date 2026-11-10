using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;

internal static class LegacyIntegrationTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IntegrationLegacy.BirthMagicTrait", f => Birth(f, TorannMagicDefOf.InnerFire, false));
        yield return new("IntegrationLegacy.BirthFighterTrait", f => Birth(f, TorannMagicDefOf.Gladiator, true));
        yield return new("IntegrationLegacy.ActivePoliciesAndBirthPatch", Policies);
        yield return new("IntegrationLegacy.GuidanceAndBodyProviders", Providers);
        yield return new("IntegrationLegacy.RemainingVendorBoundaries", f =>
        {
            var root = Path.Combine(f.Repository,"RimWorldOfMagic/RimWorldOfMagic");
            Check.Equal(false,File.ReadAllText(Path.Combine(root,"Guidance/MagicGuidance.cs")).Contains("vanillaexpanded.gravship"),"guidance asks a neutral capability");
            Check.Equal(false,File.ReadAllText(Path.Combine(root,"ModOptions/Constants.cs")).Contains("ssulunge.BBBodySupport"),"Content does not probe optional body provider");
        });
    }
    static void Providers(RuntimeFixture f)
    {
        var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
        var enabled = asm.GetType("TorannMagic.Integrations.LegacyModPolicies", true)!.GetMethod("Enabled")!;
        var load = asm.GetType("TorannMagic.Integrations.LegacyBodyTextures", true)!.GetMethod("Load", BindingFlags.Static | BindingFlags.NonPublic)!;
        var active = typeof(ModsConfig).GetField("activeModsHashSet", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldActive = active.GetValue(null);
        try
        {
            foreach (var ids in new[] { Array.Empty<string>(), new[] { "vanillaexpanded.gravship" }, new[] { "ssulunge.bbbodysupport" }, new[] { "vanillaexpanded.gravship", "ssulunge.bbbodysupport" } })
            {
                active.SetValue(null, new HashSet<string>(ids));
                Check.Equal(ids.Contains("vanillaexpanded.gravship"), (bool)enabled.Invoke(null, ["ShipOxygenGuidance"])!, "guidance follows exact VGE package gate");
                var paths = new List<string>();
                load.Invoke(null, new object[] { (Action<string>)paths.Add });
                Check.Equal(ids.Contains("ssulunge.bbbodysupport") ? 2 : 0, paths.Count, "body textures only loaded with provider");
                if (paths.Count > 0)
                    Check.Equal("Equipment/demonlordcloakc_FemaleBB_south|Equipment/opencloak_FemaleBB_south", string.Join("|", paths), "original texture paths and ordering");
            }
            var startup = asm.GetType("TorannMagic.Integrations.LegacyBodyTextureStartup", true)!;
            Check.Equal(true, startup.IsDefined(typeof(StaticConstructorOnStartup), false), "texture access scheduled on game startup thread");
        }
        finally { active.SetValue(null, oldActive); }
    }
    static void Policies(RuntimeFixture f)
    {
        var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
        var ht = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var id = "rwom.tests.legacypolicies." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, id)!;
        var packs = new List<ModContentPack>();
        void Patch(string name)
        {
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, [asm.GetType("TorannMagic.Integrations." + name, true)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
        }
        try
        {
            Patch("LegacyModPolicies");
            Check.Equal(false, TorannMagic.ModCheck.Validate.PrisonLabor.IsInitialized(), "absent prison policy");
            Check.Equal(false, TorannMagic.ModCheck.Validate.NoJobAuthors.IsInitialized(), "absent sharing policy");
            var birth = asm.GetType("TorannMagic.Integrations.LegacyChildrenBirthPatch", true)!;
            var prepare = birth.GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic)!;
            Check.Equal(false, (bool)prepare.Invoke(null, null)!, "native birth patch absent by default");
            foreach (var name in new[] { "Prison Labor", "No Job Authors", "Children, school and learning" })
            {
                var pack = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
                typeof(ModContentPack).GetField("nameInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pack, name);
                LoadedModManager.RunningModsListForReading.Add(pack); packs.Add(pack);
            }
            Check.Equal(true, TorannMagic.ModCheck.Validate.PrisonLabor.IsInitialized(), "active prison policy");
            Check.Equal(true, TorannMagic.ModCheck.Validate.NoJobAuthors.IsInitialized(), "active sharing policy");
            Check.Equal(true, (bool)prepare.Invoke(null, null)!, "native birth patch active only with legacy provider");
            Patch("LegacyChildrenBirthPatch");
            var target = typeof(PawnUtility).GetMethod(nameof(PawnUtility.TrySpawnHatchedOrBornPawn))!;
            var info = ht.GetMethod("GetPatchInfo")!.Invoke(null, [target])!;
            var owners = (IEnumerable<string>)info.GetType().GetProperty("Owners")!.GetValue(info)!;
            Check.Equal(true, owners.Contains(id), "legacy native birth target is actually patched");
            var postfixes = (System.Collections.IEnumerable)info.GetType().GetField("Postfixes")!.GetValue(info)!;
            var ownPatch = postfixes.Cast<object>().Single(p => (string)p.GetType().GetField("owner")!.GetValue(p)! == id);
            Check.Equal(10, (int)ownPatch.GetType().GetField("priority")!.GetValue(ownPatch)!, "legacy late birth priority preserved");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
            foreach (var pack in packs) LoadedModManager.RunningModsListForReading.Remove(pack);
        }
    }
    static void Birth(RuntimeFixture f, TraitDef original, bool fighter)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        // Definitions are populated by LoadBaseDefinitions, so resolve after setup.
        original = fighter ? TorannMagicDefOf.Gladiator : TorannMagicDefOf.InnerFire;
        var pawn = GrimoireProgressionTests.PawnWithComps(true, true).pawn;
        pawn.story.traits.allTraits.Clear();
        var ordinary = new TraitDef { defName = "RegressionOrdinary", degreeDatas = [new TraitDegreeData()] };
        pawn.story.traits.allTraits.Add(new Trait(original));
        pawn.story.traits.allTraits.Add(new Trait(ordinary));
        var magicDegrees = TorannMagicDefOf.TM_Gifted.degreeDatas;
        var fighterDegrees = TorannMagicDefOf.PhysicalProdigy.degreeDatas;
        TorannMagicDefOf.TM_Gifted.degreeDatas = [new TraitDegreeData()];
        TorannMagicDefOf.PhysicalProdigy.degreeDatas = [new TraitDegreeData()];
        var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
        var bridge = asm.GetType("TorannMagic.Integrations.LegacyBirthBridgePatch");
        var ht = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var id = "rwom.tests.legacybirth." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            if (bridge != null)
            {
                var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, [bridge])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
            bool result = true;
            TorannMagicMod.TM_Children_TrySpawnHatchedOrBornPawn_Tweak(ref pawn, null, ref result);
            Check.Equal(2, pawn.story.traits.allTraits.Count, "ordinary trait and one potential retained");
            Check.Equal(true, pawn.story.traits.HasTrait(ordinary), "ordinary trait unchanged");
            Check.Equal(true, pawn.story.traits.HasTrait(fighter ? TorannMagicDefOf.PhysicalProdigy : TorannMagicDefOf.TM_Gifted), "class replaced by matching potential");
            Check.Equal(false, pawn.story.traits.HasTrait(original), "newborn does not retain full class");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
            TorannMagicDefOf.TM_Gifted.degreeDatas = magicDegrees;
            TorannMagicDefOf.PhysicalProdigy.degreeDatas = fighterDegrees;
        }
    }
}
