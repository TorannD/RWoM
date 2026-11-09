using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using TorannMagic.Guidance;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static class StellarObservatoryDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.StellarObservatory.Definitions", Definitions);
        yield return new("Odyssey.StellarObservatory.SevenLanguages", Languages);
        yield return new("Odyssey.StellarObservatory.Artwork", Artwork);
        yield return new("Odyssey.StellarObservatory.DirectConsumerUi", DirectConsumerUi);
    }

    static XDocument Document(RuntimeFixture f) => XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Buildings_StellarObservatory.xml"));

    static void Definitions(RuntimeFixture f)
    {
        var doc = Document(f);
        var thing = doc.Root!.Element("ThingDef")!;
        Check.Equal("TM_StellarObservatory", thing.Element("defName")!.Value, "stable device identifier");
        foreach (var pair in new[] { ("size", "(2,2)"), ("rotatable", "false"), ("tickerType", "Normal"), ("drawerType", "MapMeshAndRealTime"), ("hasInteractionCell", "true"), ("interactionCellOffset", "(0,0,-1)"), ("minifiedDef", "MinifiedThing"), ("designationCategory", "TM_RWoM") })
            Check.Equal(pair.Item2, thing.Element(pair.Item1)!.Value, pair.Item1);
        Check.Equal("Graphic_Single", thing.Element("graphicData")!.Element("graphicClass")!.Value, "one symmetric sprite");
        Check.Equal("TorannMagic.Odyssey.PlaceWorker_ManaNetwork", thing.Element("placeWorkers")!.Element("li")!.Value, "network placement overlay");
        Check.Equal("TM_MagofuelRefining,OrbitalTech", string.Join(",", thing.Element("researchPrerequisites")!.Elements().Select(e => e.Value)), "existing orbital construction progression");
        Check.Equal(0, doc.Root.Elements("ResearchProjectDef").Count(), "no new research gate");
        var comps = thing.Element("comps")!.Elements().ToArray();
        Check.Equal(false, comps.Any(e => ((string?)e.Attribute("Class"))?.Contains("Refuelable") == true || ((string?)e.Attribute("Class"))?.Contains("Buffer") == true), "no hidden local resource storage");
        var endpoint = comps.Single(e => (string?)e.Attribute("Class") == "TorannMagic.Odyssey.CompProperties_ManaNetworkEndpoint");
        Check.Equal("true", endpoint.Element("directConsumer")!.Value, "bufferless consumer UI contract");
        foreach (var resource in new[] { "Mana", "Fuel" })
        {
            Check.Equal("true", endpoint.Element("supports" + resource)!.Value, "network " + resource);
            Check.Equal("Receive", endpoint.Element("default" + resource + "Mode")!.Value, "receive " + resource);
        }
        // Use the game's XML loader so missing comp classes or misspelled fields fail here.
        foreach (var comp in comps)
        {
            var xml = new XmlDocument(); xml.LoadXml(comp.ToString());
            var props = DirectXmlToObject.ObjectFromXml<CompProperties>(xml.DocumentElement!, false);
            Check.NotNull(props.compClass, "resolved comp " + comp.Attribute("Class")!.Value);
        }
        var job = doc.Root.Element("JobDef")!;
        Check.Equal("TM_ActivateStellarObservatory", job.Element("defName")!.Value, "ritual job");
        Check.NotNull(typeof(MagicGuides).Assembly.GetType(job.Element("driverClass")!.Value), "ritual driver resolves");
        var loadFolders = XDocument.Load(Path.Combine(f.Repository, "LoadFolders.xml"));
        Check.Equal("ludeon.rimworld.odyssey", loadFolders.Root!.Element("v1.7")!.Elements("li").Single(e => e.Value == "v1.7/Odyssey").Attribute("IfModActive")!.Value, "Odyssey gated without VGE dependency");
        var baseXml = new XmlDocument(); baseXml.Load(Path.Combine(f.Repository, "v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var deviceXml = new XmlDocument(); deviceXml.LoadXml(thing.ToString());
        XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(baseXml.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!, null);
            XmlInheritance.TryRegister(deviceXml.DocumentElement!, null);
            XmlInheritance.Resolve();
            var resolved = XmlInheritance.GetResolvedNodeFor(deviceXml.DocumentElement!);
            Check.Equal("Building", resolved.SelectSingleNode("thingClass")!.InnerText, "real inherited building class");
            Check.Equal(5, resolved.SelectNodes("comps/li")!.Count, "no inherited payment or upkeep components");
        }
        finally { XmlInheritance.Clear(); }
        // Device help attaches without research, including salvaged observatories.
        var previous = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<ThingDef>.Clear();
            var def = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = "TM_StellarObservatory"; def.category = ThingCategory.Building; def.comps = new();
            DefDatabase<ThingDef>.Add(def);
            MagicGuides.AttachDeviceGuides(); MagicGuides.AttachDeviceGuides();
            Check.Equal(1, def.comps.Count(c => c.compClass == typeof(CompMagicGuide)), "device help attaches once without operation research gating");
        }
        finally { DefDatabase<ThingDef>.Clear(); foreach (var def in previous) DefDatabase<ThingDef>.Add(def); }
    }

    static void Languages(RuntimeFixture f)
    {
        var root = Path.Combine(f.Repository, "v1.7/Odyssey/Languages");
        var english = XDocument.Load(Path.Combine(root, "English/Keyed/TM_StellarObservatory.xml")).Root!;
        foreach (var language in Directory.GetDirectories(root))
        {
            var keys = XDocument.Load(Path.Combine(language, "Keyed/TM_StellarObservatory.xml")).Root!;
            Check.Equal(english.Elements().Count(), keys.Elements().Count(), "key parity " + language);
            foreach (var source in english.Elements())
            {
                var value = (string?)keys.Element(source.Name);
                Check.Equal(false, string.IsNullOrWhiteSpace(value), "translation " + language + " " + source.Name);
                string Tokens(string text) => string.Join(",", Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).OrderBy(s => s));
                Check.Equal(Tokens(source.Value), Tokens(value!), "format placeholders " + source.Name);
            }
            foreach (var pair in new[] { ("ThingDef", "TM_StellarObservatory.label"), ("ThingDef", "TM_StellarObservatory.description"), ("JobDef", "TM_ActivateStellarObservatory.reportString") })
                Check.Equal(false, string.IsNullOrWhiteSpace((string?)XDocument.Load(Path.Combine(language, "DefInjected", pair.Item1, "TM_StellarObservatory.xml")).Root!.Element(pair.Item2)), "translated definition " + language + " " + pair.Item2);
        }
    }

    static void Artwork(RuntimeFixture f)
    {
        var texture = Document(f).Root!.Element("ThingDef")!.Element("graphicData")!.Element("texPath")!.Value;
        Check.Equal(true, File.Exists(Path.Combine(f.Repository, "v1.7/Odyssey/Textures", texture + ".png")), "dedicated observatory sprite exists");
    }

    static void DirectConsumerUi(RuntimeFixture f)
    {
        var oldLanguage = LanguageDatabase.activeLanguage; var oldDefault = LanguageDatabase.defaultLanguage;
        var language = (LoadedLanguage)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
        language.folderName = "English";
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(LoadedLanguage).GetField("dataIsLoaded", flags)!.SetValue(language, true);
        typeof(LoadedLanguage).GetField("workerInt", flags)!.SetValue(language, new LanguageWorker_English());
        language.keyedReplacements = XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Languages/English/Keyed/TM_ManaBeacon.xml")).Root!.Elements()
            .ToDictionary(e => e.Name.LocalName, e => new LoadedLanguage.KeyedReplacement { key = e.Name.LocalName, value = e.Value });
        LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        try
        {
        var parent = (ThingWithComps)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingWithComps));
        typeof(Thing).GetField("mapIndexOrState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(parent, (sbyte)-1);
        var endpoint = new CompManaNetworkEndpoint { parent = parent };
        endpoint.Initialize(new CompProperties_ManaNetworkEndpoint { directConsumer = true });
        Check.Equal(ManaNetworkMode.Receive, endpoint.ManaMode, "default send becomes receive for direct consumers");
        endpoint.FuelMode = ManaNetworkMode.Send;
        Check.Equal(ManaNetworkMode.Receive, endpoint.FuelMode, "direct fuel consumer cannot send");
        Check.Equal("Off,Receive", string.Join(",", endpoint.AvailableModes), "menu only offers supported modes");
        var inspect = endpoint.CompInspectStringExtra();
        Check.Equal(false, inspect.Contains("TM_ManaNetworkUnsupportedFuel".Translate().ToString()), "no missing local tank warning");
        Check.Equal(false, inspect.Contains("TM_ManaNetworkTarget".Translate(endpoint.ManaTarget.ToString("P0")).ToString()), "no local mana target");
        endpoint.ManaMode = ManaNetworkMode.Off;
        Check.Equal(ManaNetworkMode.Off, endpoint.ManaMode, "receive can be disabled");
        endpoint.Initialize(new CompProperties_ManaNetworkEndpoint());
        Check.Equal(ManaNetworkMode.Send, endpoint.ManaMode, "legacy endpoints retain send default");
        Check.Equal("Off,Send,Receive", string.Join(",", endpoint.AvailableModes), "legacy endpoint menu unchanged");
        }
        finally { LanguageDatabase.activeLanguage = oldLanguage; LanguageDatabase.defaultLanguage = oldDefault; }
    }
}
