using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace RWoM.RegressionTests;

// I10: the Ability AI handover. The compat assembly, its conditional load
// root and the rule patches are optional: every case that needs the vendor
// assembly skips cleanly when Ability AI is not installed, and the root/patch
// cases prove the absent state is a no-op.
internal static class AbilityAIIntegrationTests
{
    internal static string Root(RuntimeFixture f) => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_STAGED_RELEASE"))
        ? Path.Combine(f.Repository, "TorannMagic.Integrations") : Path.Combine(Environment.GetEnvironmentVariable("RWOM_STAGED_RELEASE")!, "Integrations");
    internal static string AbilityAIDir(RuntimeFixture f) => Environment.GetEnvironmentVariable("RWOM_ABILITYAI_ASSEMBLY_DIR")
        ?? Path.GetFullPath(Path.Combine(f.Repository, "..", "ext", "AbilityAI"));

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("AbilityAIPackage.RootsAndMetadata", RootsAndMetadata);
        yield return new("AbilityAIPackage.Batch1TargetsExist", Batch1TargetsExist);
        yield return new("AbilityAIPackage.NativePatchApplication", f =>
        {
            if (VendorPresent(f)) NativePatchApplication(f);
            else SkipVendor(f);
        });
        yield return new("AbilityAIPackage.CompatStartupRegistersOnce", f =>
        {
            if (VendorPresent(f)) CompatStartupRegistersOnce(f);
            else SkipVendor(f);
        });
    }

    private static bool VendorPresent(RuntimeFixture f) =>
        File.Exists(Path.Combine(AbilityAIDir(f), "AbilityAI.dll")) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_ABILITYAI_ADAPTER"));

    private static void SkipVendor(RuntimeFixture f) =>
        Console.WriteLine("Ability AI assembly not present; vendor-present case skipped.");

    // Metadata and native load-folder gates: the compat root activates only
    // with the Ability AI package, and the parent never loads it.
    private static void RootsAndMetadata(RuntimeFixture f)
    {
        var about = XDocument.Load(Path.Combine(Root(f), "About/About.xml")).Root!;
        var loadAfter = about.Element("loadAfter")!.Elements().Select(x => x.Value).ToList();
        Check.Equal(true, loadAfter.Contains("saintwacko.AbilityAI"), "compat assembly loads after Ability AI");

        var xml = new XmlDocument(); xml.Load(Path.Combine(Root(f), "LoadFolders.xml"));
        var folders = new ModLoadFolders(); folders.LoadDataFromXmlCustom(xml.DocumentElement!);
        var root = folders.FoldersForVersion("1.7").Single(x => x.folderName == Path.Combine("v1.7", "AbilityAICompat"));
        Check.Equal("saintwacko.AbilityAI", root.requiredAnyOfPackageIds.Single(), "native loader retains the Ability AI gate");

        var parent = XDocument.Load(Path.Combine(f.Repository, "LoadFolders.xml"));
        Check.Equal(false, parent.Descendants("li").Any(x => x.Value.Contains("AbilityAICompat")), "parent does not load the compat root");

        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var activeField = typeof(ModsConfig).GetField("activeModsHashSet", flags)!;
        var previousActive = activeField.GetValue(null);
        try
        {
            foreach (bool active in new[] { false, true })
            {
                activeField.SetValue(null, active ? new HashSet<string> { "saintwacko.abilityai" } : new HashSet<string>());
                var scan = new List<string> { Path.Combine(Root(f), "v1.7") };
                if (active) scan.Add(Path.Combine(Root(f), "v1.7", "AbilityAICompat"));
                var patches = ModContentPack.GetAllFilesForMod(null, "Patches/", null, scan).Values
                    .Select(x => x.Name).ToList();
                Check.Equal(active ? 3 : 0, patches.Count(x => x.StartsWith("AbilityAI_RWoM_Batch", StringComparison.Ordinal)),
                    "rule batches enumerate only with Ability AI active (active=" + active + ")");
                // A repository checkout ships no adapter DLL (builds stay in
                // artifacts); the assembly enumeration is a staged-state check.
                if (File.Exists(Path.Combine(Root(f), "v1.7", "AbilityAICompat", "Assemblies", "TorannMagic.AbilityAI.dll")))
                {
                    var assemblies = ModContentPack.GetAllFilesForMod(null, "Assemblies/",
                        p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase), scan).Values
                        .Select(x => x.Name).OrderBy(x => x).ToList();
                    Check.Equal(active, assemblies.Contains("TorannMagic.AbilityAI.dll"),
                        "compat assembly enumerates only with Ability AI active (active=" + active + ")");
                }
            }
        }
        finally { activeField.SetValue(null, previousActive); }
    }

    // Every rule batch must only reference bridge defs that exist: a renamed
    // or regenerated bridge tree would otherwise leave silent dead rules.
    private static void Batch1TargetsExist(RuntimeFixture f)
    {
        string patchesDir = Path.Combine(Root(f), "v1.7", "AbilityAICompat", "Patches");
        var files = Directory.GetFiles(patchesDir, "AbilityAI_RWoM_Batch*.xml").OrderBy(x => x).ToList();
        Check.Equal(true, files.Count >= 3, "rule batches present: " + files.Count);

        var bridgeIds = new HashSet<string>(StringComparer.Ordinal);
        string dir = Path.Combine(f.Repository, "v1.7", "Defs", "AbilityDefs", "Bridge");
        foreach (string file in Directory.GetFiles(dir, "*.xml"))
            foreach (var el in XDocument.Load(file).Root!.Elements("AbilityDef"))
                if ((string?)el.Element("defName") is { } name) bridgeIds.Add(name);

        var all = new List<string>();
        foreach (string file in files)
        {
            var patch = XDocument.Load(file);
            var referenced = patch.Descendants("xpath")
                .SelectMany(x => System.Text.RegularExpressions.Regex.Matches(x.Value, "defName=\"([^\"]+)\"")
                    .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value))
                .Distinct().ToList();
            Check.Equal(referenced.Count, referenced.Distinct().Count(), "no repeated defName inside one xpath: " + Path.GetFileName(file));
            // The friendly file only adds <hostile>false</hostile> to defs the
            // rule batches already cover; rule files must not overlap.
            if (!Path.GetFileName(file).Contains("Friendly", StringComparison.Ordinal)) all.AddRange(referenced);
            Console.WriteLine(Path.GetFileName(file) + ": " + referenced.Count + " rule targets");
        }
        Check.Equal(true, all.Count >= 240, "batches cover a representative set: " + all.Count);
        Check.Equal(all.Count, all.Distinct().Count(), "a def is ruled in exactly one place");
        var missing = all.Where(x => !bridgeIds.Contains(x)).ToList();
        Check.Equal(0, missing.Count, "every rule target exists in the bridge tree" + (missing.Count > 0 ? ": " + string.Join(",", missing) : ""));
    }

    // The batches apply natively and resolve Ability AI's extension class
    // exactly as the game would resolve it with the mod loaded.
    private static void NativePatchApplication(RuntimeFixture f)
    {
        Assembly.LoadFrom(Path.Combine(AbilityAIDir(f), "AbilityAI.dll"));
        string patchesDir = Path.Combine(Root(f), "v1.7", "AbilityAICompat", "Patches");
        var stubNames = new HashSet<string>(StringComparer.Ordinal);
        var sources = Directory.GetFiles(patchesDir, "AbilityAI_RWoM_Batch*.xml").OrderBy(x => x)
            .Select(x => { var d = new XmlDocument(); d.Load(x); return d; }).ToList();
        foreach (var source in sources)
            foreach (var x in source.SelectNodes("/Patch/Operation/xpath")!.Cast<XmlNode>())
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(x.InnerText, "defName=\"([^\"]+)\""))
                    stubNames.Add(m.Groups[1].Value);
        var defs = new XmlDocument();
        defs.LoadXml("<Defs>" + string.Join("", stubNames.Select(n => "<AbilityDef><defName>" + n + "</defName></AbilityDef>")) + "</Defs>");
        var before = defs.OuterXml;
        foreach (var source in sources)
        {
            int index = 0;
            foreach (XmlNode node in source.SelectNodes("/Patch/Operation")!)
            {
                bool applied = DirectXmlToObject.ObjectFromXml<PatchOperation>(node, false).Apply(defs);
                Check.Equal(true, applied, "native rule patch succeeds: " + Path.GetFileName(source.BaseURI).Replace("\"", "") + " op " + index);
                index++;
            }
        }
        Check.Equal(true, defs.OuterXml != before, "patch changed the stub defs");
        var bolt = defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Arcanebolt']")!;
        Check.Equal(1, bolt.SelectNodes("modExtensions/li[@Class='AbilityAI.AbilityAIExtension']")!.Count, "one rule on a damage spell");
        var heal = defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Advancedheal']")!;
        Check.Equal(2, heal.SelectNodes("modExtensions/li[@Class='AbilityAI.AbilityAIExtension']")!.Count, "rescue and friendly rules on the heal");
        Check.Equal(true, heal.SelectSingleNode("modExtensions/li/workerClass[text()='AbilityAI.AbilityAIWorker_Rescue']") != null,
            "rescue worker named on the heal");
        Check.Equal("false", heal.SelectSingleNode("hostile")!.InnerText, "heal marked non-hostile");
        Check.Equal("false", defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Extinguish']/hostile")!.InnerText, "extinguish marked non-hostile");
        Check.Equal(false, defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Arcanebolt']/hostile") != null,
            "violent spells keep the hostile default");
        // Batch 2 shapes: area radius from the projectile, movement modes,
        // npc gates and deploy rules resolve.
        var aoe = defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Fireball']/modExtensions/li")!;
        Check.Equal("AbilityAI.AbilityAIWorker_AoEHostile", aoe.SelectSingleNode("workerClass")!.InnerText, "area damage worker on Fireball");
        Check.Equal("3.6", aoe.SelectSingleNode("radius")!.InnerText, "Fireball radius from its projectile");
        Check.Equal("GapClose, Escape", defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Blink']/modExtensions/li/modes")!.InnerText,
            "Blink movement modes");
        Check.Equal("InstantKill", defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Deathbolt']/modExtensions/li/npcGate")!.InnerText,
            "death bolt carries the NPC instant-kill gate");
        Check.Equal("AbilityAI.AbilityAIWorker_Deploy", defs.SelectSingleNode("//AbilityDef[defName='TM_Bridge_Summon']/modExtensions/li/workerClass")!.InnerText,
            "summon uses the deploy worker");
    }

    // The handover registers once, on repeated startup access, and the
    // ownership probe answers safely for pawns it knows nothing about.
    private static void CompatStartupRegistersOnce(RuntimeFixture f)
    {
        string coreDll = Path.Combine(f.Repository, "TorannMagic.Core", "Assemblies", "TorannMagic.Core.dll");
        // Earlier fixtures may have loaded Core already (in a different load
        // context); reuse whatever is resident before falling back to a load.
        if (Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "TorannMagic.Core") == null && File.Exists(coreDll))
            Assembly.LoadFrom(coreDll);
        var search = new List<string> { Path.Combine(AbilityAIDir(f), "AbilityAI.dll") };
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name) =>
            search.FirstOrDefault(p => Path.GetFileName(p) == name.Name + ".dll") is { } path
                ? context.LoadFromAssemblyPath(path) : null;
        AssemblyLoadContext.Default.Resolving += Resolve;
        var ht = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        const string owner = "huginn.rwom.integrations.abilityai";
        var harmony = Activator.CreateInstance(ht, owner)!;
        try
        {
            Assembly adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_ABILITYAI_ADAPTER")!);
            var startup = adapter.GetType("TorannMagic.Integrations.AbilityAICompat.AbilityAICompatStartup", true)!;
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);

            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            string providerId = (string)adapter.GetType("TorannMagic.Integrations.AbilityAICompat.TmAbilityAIOwnership", true)!
                .GetField("ProviderId")!.GetValue(null)!;
            // The harness compiles without references to the optional Core and
            // Ability AI assemblies; both are loaded at runtime, so reflect.
            var ownershipType = Type.GetType("TorannMagic.Core.CastOwnership, TorannMagic.Core", true)!;
            var providers = (System.Collections.IDictionary)ownershipType
                .GetField("providers", flags)!.GetValue(null)!;
            Check.Equal(1, providers.Keys.Cast<string>().Count(k => k == providerId), "ownership provider registered exactly once");
            var hooksType = Assembly.Load("AbilityAI").GetType("AbilityAI.AbilityAIHooks", true)!;
            var hooks = (System.Collections.IList)hooksType.GetField("hooks", flags)!.GetValue(null)!;
            Check.Equal(1, hooks.Cast<object>().Count(h => h.GetType().Name == "TmAbilityAIHook"), "claim hook registered once");

            var probe = adapter.GetType("TorannMagic.Integrations.AbilityAICompat.TmAbilityAIOwnership", true)!
                .GetMethod("IsOwned", new[] { typeof(Pawn) })!;
            Check.Equal(false, probe.Invoke(null, new object?[] { null }), "probe tolerates a null pawn");

            var installed = new List<MethodInfo>();
            foreach (var method in (IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!)
            {
                var info = ht.GetMethod("GetPatchInfo")!.Invoke(null, new object?[] { method })!;
                foreach (var kind in new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" })
                    foreach (var patch in (System.Collections.IEnumerable)info.GetType().GetField(kind)!.GetValue(info)!)
                        if ((string)patch.GetType().GetField("owner")!.GetValue(patch)! == owner)
                            installed.Add((MethodInfo)patch.GetType().GetProperty("PatchMethod")!.GetValue(patch)!);
            }
            Check.Equal(2, installed.Count(m => m.DeclaringType?.Name == "Patch_CommandPawnAbility_Autocast"),
                "gizmo menu prefix and badge postfix installed once each");
            Check.Equal(1, installed.Count(m => m.DeclaringType?.Name == "Patch_AutocastToggle_Handover_Magic"),
                "magic autocast toggle handover installed once");
            Check.Equal(1, installed.Count(m => m.DeclaringType?.Name == "Patch_AutocastToggle_Handover_Might"),
                "might autocast toggle handover installed once");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, new object?[] { owner });
            AssemblyLoadContext.Default.Resolving -= Resolve;
        }
    }
}
