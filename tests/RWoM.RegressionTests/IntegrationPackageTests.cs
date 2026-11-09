using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Verse;

namespace RWoM.RegressionTests;

internal static class IntegrationPackageTests
{
    internal static string Root(RuntimeFixture f) => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_STAGED_RELEASE"))
        ? Path.Combine(f.Repository, "TorannMagic.Integrations") : Path.Combine(Environment.GetEnvironmentVariable("RWOM_STAGED_RELEASE")!, "Integrations");
    internal static string Patch(RuntimeFixture f, string file) => Path.Combine(Root(f), "v1.7/Patches", file);
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IntegrationPackage.MetadataAndSingleOwnership", Ownership);
        yield return new("IntegrationPackage.NativeContentEnumeration", Enumeration);
        yield return new("IntegrationPackage.IsekaiSingleOwnership", IsekaiOwnership);
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_STAGED_RELEASE")))
            yield return new("IntegrationPackage.StagedNativeRoots", StagedRoots);
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER")))
            yield return new("IntegrationPackage.VgeStartup", VgeStartup);
        foreach (bool active in new[] { false, true })
        {
            yield return new("IntegrationPackage.MedPod.Active=" + active, f => TargetPatch(f, "MedPod", "MedPodx.xml", active));
            yield return new("IntegrationPackage.Hellsing.Active=" + active, f => TargetPatch(f, "Hellsing ARMS Ultimate", "Hellsing_DamageTypex.xml", active));
        }
    }
    static void StagedRoots(RuntimeFixture f)
    {
        var stage = Environment.GetEnvironmentVariable("RWOM_STAGED_RELEASE")!;
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(stage, "manifest.json")));
        var savedAudit = Environment.GetEnvironmentVariable("RWOM_SAVED_TYPE_AUDIT");
        if (!string.IsNullOrEmpty(savedAudit))
        {
            using var audit = System.Text.Json.JsonDocument.Parse(File.ReadAllText(savedAudit));
            var copy = audit.RootElement.GetProperty("copy").GetString()!;
            Check.Equal(audit.RootElement.GetProperty("sha256").GetString()!.ToUpperInvariant(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(copy))), "save copy retains original bytes");
            var save = XDocument.Load(copy);
            var names = save.Descendants().Attributes("Class").Select(a=>a.Value).Where(n=>n.StartsWith("TorannMagic.",StringComparison.Ordinal)).Distinct().ToArray();
            Check.Equal(true,names.Length>0,"real saved class names examined");
            foreach (var name in names)
                Check.NotNull(typeof(TorannMagic.MagicData).Assembly.GetType(name), "existing saved type remains in Content: " + name);
            Console.WriteLine("Saved Content type identities resolved: " + names.Length + " (XML inventory, not full game load)");
        }
        foreach (var item in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            var path = Path.Combine(stage, item.GetProperty("package").GetString()!, item.GetProperty("path").GetString()!);
            Check.Equal(item.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), "staged hash: " + path);
        }
        var modsField = typeof(ModLister).GetField("mods", BindingFlags.Static | BindingFlags.NonPublic)!;
        var activeField = typeof(ModsConfig).GetField("activeModsHashSet", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldMods = modsField.GetValue(null); var oldActive = activeField.GetValue(null);
        var indexField = typeof(ModLister).GetField("modsByPackageIdIgnorePostfix", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldIndex = indexField.GetValue(null);
        var ids = new[] { "vanillaexpanded.gravship", "rwmt.multiplayer", "zetrith.multiplayer", "poupun.isekaidungeonsaddon" };
        var metadata = new List<ModMetaData>();
        foreach (var id in ids)
        {
            var directory = Path.Combine(f.WorkingDirectory, "staged-provider", id);
            Directory.CreateDirectory(Path.Combine(directory, "About"));
            new XDocument(new XElement("ModMetaData", new XElement("name", id), new XElement("author", "Fixture"), new XElement("packageId", id),
                new XElement("supportedVersions", new XElement("li", "1.7")))).Save(Path.Combine(directory,"About/About.xml"));
            metadata.Add(new ModMetaData(directory));
        }
        try
        {
            modsField.SetValue(null, metadata);
            indexField.SetValue(null, metadata.ToDictionary(x=>x.PackageId.ToLowerInvariant(), x=>new List<ModMetaData>{x}));
            var xml = new XmlDocument(); xml.Load(Path.Combine(stage,"Integrations/LoadFolders.xml"));
            var folders = new ModLoadFolders(); folders.LoadDataFromXmlCustom(xml.DocumentElement!);
            foreach (var active in new[] { Array.Empty<string>(), new[] { ids[0] }, new[] { ids[1] }, new[] { ids[2] }, ids })
            {
                activeField.SetValue(null, new HashSet<string>(active));
                var roots = folders.FoldersForVersion("1.7").Where(x=>x.ShouldLoad).Select(x=>Path.Combine(stage,"Integrations",x.folderName)).ToList();
                var assemblies = ModContentPack.GetAllFilesForMod(null,"Assemblies/",p=>p.EndsWith(".dll",StringComparison.OrdinalIgnoreCase),roots).Values.Select(x=>x.Name).OrderBy(x=>x).ToArray();
                var expected = new List<string>{"TorannMagic.Integrations.dll"};
                if(active.Contains(ids[0])) expected.Add("TorannMagic.VGE.dll");
                if(active.Contains(ids[1]) || active.Contains(ids[2])) expected.Add("TorannMagic.Multiplayer.dll");
                Check.Equal(string.Join(",",expected.OrderBy(x=>x)),string.Join(",",assemblies),"native conditional assemblies: "+string.Join(",",active));
                var definitions = ModContentPack.GetAllFilesForMod(null,"Defs/",null,roots).Values;
                Check.Equal(active.Contains(ids[3])?1:0,definitions.Count(x=>x.Name=="RWoM_Dungeons.xml"),"native dungeon activation");
            }
            Check.Equal(1,Directory.GetFiles(Path.Combine(stage,"Content"),"*.dll",SearchOption.AllDirectories).Length,"Content ships only its own assembly");
            Check.Equal(1,Directory.GetFiles(Path.Combine(stage,"Core"),"*.dll",SearchOption.AllDirectories).Length,"Core ships only its own assembly");
        }
        finally { modsField.SetValue(null,oldMods); activeField.SetValue(null,oldActive); indexField.SetValue(null,oldIndex); }
    }
    static void IsekaiOwnership(RuntimeFixture f)
    {
        const string relative = "v1.7/IsekaiCompat/Defs/IsekaiDungeons/RWoM_Dungeons.xml";
        Check.Equal(false, File.Exists(Path.Combine(f.Repository, relative)), "Content no longer owns dungeon definitions");
        var path = Path.Combine(Root(f), relative);
        Check.Equal("0BAC92E3986DCB26BD1D519C47D6314EFEF4E0FB71C7F7AFC4688611BDFA5A24",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), "dungeon definitions preserved byte-for-byte");
        var xml = new XmlDocument(); xml.Load(Path.Combine(Root(f), "LoadFolders.xml"));
        var folders = new ModLoadFolders(); folders.LoadDataFromXmlCustom(xml.DocumentElement!);
        var root = folders.FoldersForVersion("1.7").Single(x => x.folderName == Path.Combine("v1.7", "IsekaiCompat"));
        Check.Equal("Poupun.IsekaiDungeonsAddon", root.requiredAnyOfPackageIds.Single(), "native loader retains optional provider gate");
        var files = ModContentPack.GetAllFilesForMod(null, "Defs/", null,
            new List<string> { Path.Combine(Root(f), root.folderName) }).Values;
        Check.Equal(1, files.Count(x => x.Name == "RWoM_Dungeons.xml"), "one native dungeon file owner");
        var parent = XDocument.Load(Path.Combine(f.Repository, "LoadFolders.xml"));
        Check.Equal(false, parent.Descendants("li").Any(x => x.Value.Contains("IsekaiCompat")), "Content does not load adapter root");
    }
    static void VgeStartup(RuntimeFixture f)
    {
        const string workshop = "C:/Program Files (x86)/Steam/steamapps/workshop/content/294100";
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var id in new[] { "2023507013", "3609835606", "3799737423" })
            {
                var path = Path.Combine(workshop, id, "1.7/Assemblies", name.Name + ".dll");
                if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
            }
            return null;
        }
        const string owner = "TorannMagic.VGE.MagicalThrusterRefund";
        var registry = (System.Collections.IList)typeof(TorannMagic.Odyssey.MagicalShieldProjectileAdapters)
            .GetField("adapters", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var before = registry.Cast<object>().ToArray();
        var ht = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var harmony = Activator.CreateInstance(ht, owner)!;
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            Assembly.LoadFrom(Path.Combine(workshop,"3609835606/1.7/Assemblies/VanillaGravshipExpanded.dll"));
            if (Environment.GetEnvironmentVariable("RWOM_TEST_VGE_CHAPTER2") == "1")
                Assembly.LoadFrom(Path.Combine(workshop,"3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll"));
            var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER")!);
            var startup = adapter.GetType("TorannMagic.VGE.MagicalThrusterVgeStartup", true)!;
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            Check.Equal(1,registry.Cast<object>().Count(a=>a.GetType().FullName=="TorannMagic.VGE.WarpodShieldAdapter"),"startup registers one adapter on repeated access");
            var installed = new List<MethodInfo>();
            foreach (var method in (IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null,null)!)
            {
                var info = ht.GetMethod("GetPatchInfo")!.Invoke(null,[method])!;
                foreach (var kind in new[]{"Prefixes","Postfixes","Transpilers","Finalizers"})
                    foreach (var patch in (System.Collections.IEnumerable)info.GetType().GetField(kind)!.GetValue(info)!)
                        if ((string)patch.GetType().GetField("owner")!.GetValue(patch)! == owner)
                            installed.Add((MethodInfo)patch.GetType().GetProperty("PatchMethod")!.GetValue(patch)!);
            }
            foreach (var name in new[]{"MagicalCombatArtilleryBeamPatch","MagicalCombatAnticraftImpactPatch","MagicalCombatAnticraftPulsePatch","MagicalCombatWarpodImpactPatch","MagicalCombatWarpodTickPatch","MagicalCombatWarpodSavePatch"})
            {
                var type=adapter.GetType("TorannMagic.Odyssey."+name,true)!;
                bool available=type.GetMethod("TargetMethod")!.Invoke(null,null)!=null;
                Check.Equal(available?1:0,installed.Count(m=>m.DeclaringType==type),"startup installs optional patch once: "+name);
            }
            Check.Equal(0,installed.Count(m=>m.DeclaringType?.Name=="MagicalCombatNativeBeamPatch"),"adapter does not take over native beams");
            Console.WriteLine("VGE startup chapter2 loaded: " + AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name=="VanillaGravshipExpanded2"));
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[owner]);
            registry.Clear(); foreach(var value in before) registry.Add(value);
            AssemblyLoadContext.Default.Resolving -= Resolve;
        }
    }
    static void Enumeration(RuntimeFixture f)
    {
        List<string> Roots(string directory)
        {
            var xml = new XmlDocument(); xml.Load(Path.Combine(directory,"LoadFolders.xml"));
            var folders = new ModLoadFolders(); folders.LoadDataFromXmlCustom(xml.DocumentElement!);
            return folders.FoldersForVersion("1.7").Select(folder => Path.Combine(directory, folder.folderName)).ToList();
        }
        // Include even conditionally inactive parent roots: this is a stronger exclusion check.
        var parentFiles = ModContentPack.GetAllFilesForMod(null, "Patches/", null, Roots(f.Repository)).Values.ToArray();
        var ownFiles = ModContentPack.GetAllFilesForMod(null, "Patches/", null, Roots(Root(f))).Values.ToArray();
        Check.Equal(true, parentFiles.Length > 0, "native loader actually reads main patch content");
        Check.Equal(10, ownFiles.Length, "native loader finds three shared, four VGE and three Ability AI patches");
        foreach (var file in ownFiles)
        {
            var hash = SHA256.HashData(File.ReadAllBytes(file.FullName));
            Check.Equal(false, parentFiles.Any(p => p.FullName == file.FullName ||
                SHA256.HashData(File.ReadAllBytes(p.FullName)).SequenceEqual(hash)), "independent patch is not loaded by parent: " + file.Name);
        }
        Check.Equal(false, Directory.Exists(Path.Combine(f.Repository, "v1.7/VGECompat")), "old VGE root removed, not merely disabled");
    }
    static void Ownership(RuntimeFixture f)
    {
        var about = XDocument.Load(Path.Combine(Root(f), "About/About.xml")).Root!;
        Check.Equal("huginn.rwom.integrations", about.Element("packageId")!.Value, "stable independent identity");
        Check.Equal("1.7", string.Join(",", about.Element("supportedVersions")!.Elements().Select(x => x.Value)), "no accidental v1.6 migration");
        Check.Equal("huginn.rwom.core,Torann.ARimworldOfMagic", string.Join(",", about.Element("modDependencies")!.Elements().Select(x => x.Element("packageId")!.Value)), "Core contract and Content required; vendor mods stay optional");
        var folders = XDocument.Load(Path.Combine(Root(f), "LoadFolders.xml"));
        Check.Equal("v1.7,v1.7/VGECompat,v1.7/MultiplayerCompat,v1.7/IsekaiCompat,v1.7/AbilityAICompat,v1.7/CECompat", string.Join(",", folders.Root!.Element("v1.7")!.Elements().Select(x => x.Value)), "explicit adapter content roots");
        var parent = XDocument.Load(Path.Combine(f.Repository, "LoadFolders.xml"));
        Check.Equal(false, parent.Descendants("li").Any(x => x.Value.Contains("TorannMagic.Integrations")), "parent does not load independent package");
        foreach (var (file, hash) in new[] {
            ("MedPodx.xml", "5F59FAEE58DBCFE5E7D43A9EEBA122523C3BE05AC189683231ABC987DEFC1E7C"),
            ("Hellsing_DamageTypex.xml", "A4B9860F5E072079926C36C94C46DFC6ACFFFE06A28C525F73541561A1F13A5A"),
            ("KureStaminaPotion.xml", "57ABDB1CC5DE5377586F08AABFEE47FDA08B0F4198E6CE4A712D9A63B27CF905") })
        {
            Check.Equal(false, File.Exists(Path.Combine(f.Repository,"v1.7/Patches",file)), "one patch owner: " + file);
            Check.Equal(hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Patch(f,file)))), "extraction preserves original patch bytes: " + file);
        }
        Check.Equal(true, File.Exists(Path.Combine(f.Repository,"v1.6/Patches/MedPodx.xml")), "v1.6 retained");
    }
    static void TargetPatch(RuntimeFixture f, string name, string file, bool active)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var modsField = typeof(ModLister).GetField("mods", flags)!;
        var activeField = typeof(ModsConfig).GetField("activeModsHashSet", flags)!;
        var previousMods = modsField.GetValue(null);
        var previousActive = activeField.GetValue(null);
        string directory = Path.Combine(f.WorkingDirectory, "integration-target", file);
        Directory.CreateDirectory(Path.Combine(directory,"About"));
        new XDocument(new XElement("ModMetaData",new XElement("name",name),new XElement("author","Fixture"),
            new XElement("packageId","rwom.tests.optional"),new XElement("supportedVersions",new XElement("li","1.7"))))
            .Save(Path.Combine(directory,"About/About.xml"));
        try
        {
            modsField.SetValue(null, new List<ModMetaData> { new(directory) });
            activeField.SetValue(null, active ? new HashSet<string> { "rwom.tests.optional" } : new HashSet<string>());
            var defs = new XmlDocument();
            if (name == "MedPod")
                defs.LoadXml("<Defs><ThingDef Name='MedPodBedBase'><comps><li Class='MedPod.CompProperties_TreatmentRestrictions'><neverTreatableHediffs><li>ExistingRestriction</li></neverTreatableHediffs></li></comps></ThingDef></Defs>");
            else
                defs.LoadXml("<Defs>" + string.Join("", new[] { "TM_Holy","TM_Overwhelm","TM_BurningLight","TM_Shadow","TM_Spirit","TM_ArcaneSpectre","TM_DecayDD","TM_Torment","TM_DeathBolt","TM_CorpseExplosion" }
                    .Select(n => "<DamageDef><defName>" + n + "</defName>" + (new[] {"TM_Torment","TM_DeathBolt","TM_CorpseExplosion"}.Contains(n) ? "<additionalHediffs/>" : "") + "</DamageDef>")) + "</Defs>");
            var before = defs.OuterXml;
            var source = new XmlDocument(); source.Load(Patch(f,file));
            foreach (XmlNode node in source.SelectNodes("/Patch/Operation")!)
                Check.Equal(true, DirectXmlToObject.ObjectFromXml<PatchOperation>(node,false).Apply(defs), "native optional patch succeeds");
            if (!active) Check.Equal(before, defs.OuterXml, "inactive target is no-op");
            else if (name == "MedPod")
            {
                Check.Equal(1, defs.SelectNodes("//neverTreatableHediffs/li[text()='TM_ArcaneWeakness']")!.Count, "one restriction added");
                Check.Equal(1, defs.SelectNodes("//neverTreatableHediffs/li[text()='ExistingRestriction']")!.Count, "other restrictions retained");
            }
            else
            {
                Check.Equal(3, defs.SelectNodes("//additionalHediffs/li[hediff='HSAU_SearingSanctification']")!.Count, "three holy damage integrations");
                Check.Equal(7, defs.SelectNodes("//additionalHediffs/li[hediff='HSAU_DirgeDecay']")!.Count, "seven decay damage integrations");
            }
        }
        finally { modsField.SetValue(null,previousMods); activeField.SetValue(null,previousActive); }
    }
}
