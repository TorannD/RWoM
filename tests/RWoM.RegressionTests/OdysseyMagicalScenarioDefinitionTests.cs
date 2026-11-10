using System.Xml;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

internal static class OdysseyMagicalScenarioDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.StartScenario.Definitions.NativeInheritance", Native);
        yield return new("Odyssey.StartScenario.Definitions.Languages", Languages);
    }
    static string Root(RuntimeFixture f) => Path.Combine(f.Repository, "v1.7/Odyssey");
    static XmlDocument Load(RuntimeFixture f)
    {
        string path = Path.Combine(Root(f), "Defs/Scenarios_MagicalShip.xml");
        Check.Equal(true, File.Exists(path), "separate magical scenario exists");
        var doc = new XmlDocument(); doc.Load(path); return doc;
    }
    static void Native(RuntimeFixture f)
    {
        var doc = Load(f);
        var core = new XmlDocument();
        core.Load(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!, "Data/Core/Defs/Scenarios/Scenarios_Classic.xml"));
        var node = doc.SelectSingleNode("Defs/ScenarioDef[defName='TM_MagicalVoyagers']")!;
        XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear();
        try
        {
            XmlInheritance.TryRegister(core.SelectSingleNode("Defs/ScenarioDef[@Name='ScenarioBase']")!, null);
            XmlInheritance.TryRegister(node, null); XmlInheritance.Resolve();
            var resolved = XmlInheritance.GetResolvedNodeFor(node);
            Check.Equal("Surface", resolved.SelectSingleNode("scenario/surfaceLayer/layer")!.InnerText, "normal planetary surface");
            Check.Equal(1, resolved.SelectNodes("scenario/parts/li[layer='Orbit']")!.Count, "normal orbit layer remains available later");
            var parts = resolved.SelectSingleNode("scenario/parts")!;
            Check.Equal(0, parts.SelectNodes("li[contains(@Class,'ForcedMap') or contains(@Class,'PursuingMechanoids') or contains(@Class,'StartingResearch')]")!.Count, "no orbital override, pursuit or free research");
            var config = parts.SelectSingleNode("li[@Class='ScenPart_ConfigPage_ConfigureStartingPawns']")!;
            Check.Equal("3", config.SelectSingleNode("pawnCount")!.InnerText, "three selected colonists");
            Check.Equal("8", config.SelectSingleNode("pawnChoiceCount")!.InnerText, "eight editable candidates");
            var supplies = parts.SelectNodes("li[@Class='ScenPart_StartingThing_Defined']")!.Cast<XmlNode>().ToDictionary(n => n.SelectSingleNode("thingDef")!.InnerText, n => n.SelectSingleNode("count")!.InnerText);
            Check.Equal("MealSurvivalPack=45|MedicineIndustrial=15|RawMagicyte=100|Steel=200|WoodLog=150", string.Join("|", supplies.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => k.Key + "=" + k.Value)), "modest supplies without firearms or advanced equipment");
            var arrival = parts.SelectSingleNode("li[@Class='TorannMagic.Odyssey.ScenPart_MagicalShipArrival']")!;
            Check.Equal("Gravship", arrival.SelectSingleNode("method")!.InnerText, "native gravship arrival");
            Check.Equal(1, parts.SelectNodes("li[@Class='TorannMagic.ScenPart_MagicalCrew']")!.Count, "one class initializer");
            Check.Equal(1, parts.SelectNodes("li[@Class='TorannMagic.Odyssey.ScenPart_MagicalSurfaceStart']")!.Count, "surface-only start guard");
            // Parse real scenario parts through the native XML loader, detecting field/class errors.
            foreach (XmlNode part in parts.ChildNodes)
                if (part.NodeType == XmlNodeType.Element)
                {
                    var className = part.Attributes!["Class"]!.Value;
                    Check.Equal(true, GenTypes.GetTypeInAnyAssembly(className) != null, "scenario part class is available: " + className);
                    Check.Equal(true, DirectXmlToObject.ObjectFromXml<ScenPart>(part, false) != null, "native part deserialization " + part.Attributes?["Class"]?.Value);
                }
            foreach (XmlNode def in doc.SelectNodes("Defs/ScenPartDef")!)
                Check.Equal(true, typeof(ScenPart).IsAssignableFrom(DirectXmlToObject.ObjectFromXml<ScenPartDef>(def, false).scenPartClass), "loadable scenario part registration");
            var resolver = DirectXmlToObject.ObjectFromXml<SketchResolverDef>(doc.SelectSingleNode("Defs/SketchResolverDef")!, false);
            Check.Equal("TorannMagic.Odyssey.SketchResolver_MagicalStarterShip", resolver.resolver.GetType().FullName!, "native sketch resolver registration");
            Check.Equal("false", arrival.SelectSingleNode("includeReservoir")!.InnerText, "core-only ship has no optional reservoir");
            var patch = new XmlDocument();
            patch.Load(Path.Combine(f.Repository, "TorannMagic.Integrations/v1.7/VGECompat/Patches/MagicalStarterShip.xml"));
            foreach (XmlNode operation in patch.SelectNodes("Patch/Operation")!)
                Check.Equal(true, DirectXmlToObject.ObjectFromXml<PatchOperation>(operation, false).Apply(doc), "VGE reservoir patch applies");
            Check.Equal("true", doc.SelectSingleNode("Defs/ScenarioDef/scenario/parts/li/includeReservoir")!.InnerText, "VGE receives a starter reservoir");
        }
        finally { XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear(); }
    }
    static void Languages(RuntimeFixture f)
    {
        Load(f);
        foreach (var lang in new[] { "English", "Russian", "Ukrainian", "Spanish", "ChineseSimplified", "Portuguese", "PortugueseBrazilian" })
        {
            string directory = Path.Combine(Root(f), "Languages", lang);
            var defs = XDocument.Load(Path.Combine(directory, "DefInjected/ScenarioDef/TM_MagicalShip.xml")).Root!;
            foreach (var field in new[] { "label", "description", "scenario.summary" })
                Check.Equal(false, string.IsNullOrWhiteSpace(defs.Element("TM_MagicalVoyagers." + field)?.Value), lang + " " + field);
            var keyed = XDocument.Load(Path.Combine(directory, "Keyed/TM_MagicalShip.xml")).Root!;
            Check.Equal(false, string.IsNullOrWhiteSpace(keyed.Element("TM_MagicalVoyagersStartDialog")?.Value), lang + " start dialog");
            Check.Equal(false, string.IsNullOrWhiteSpace(keyed.Element("TM_MagicalSurfaceOnly")?.Value), lang + " surface restriction");
            var parts = XDocument.Load(Path.Combine(directory, "DefInjected/ScenPartDef/TM_MagicalShip.xml")).Root!;
            foreach (var part in new[] { "TM_MagicalShipArrival", "TM_MagicalCrew", "TM_MagicalSurfaceStart" })
                Check.Equal(false, string.IsNullOrWhiteSpace(parts.Element(part + ".label")?.Value), lang + " part label");
        }
    }
}
