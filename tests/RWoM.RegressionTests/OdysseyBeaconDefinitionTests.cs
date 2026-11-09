using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using Verse;
namespace RWoM.RegressionTests;
internal static class OdysseyBeaconDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Beacon.Definitions.NativeComponentsAndInheritance", Native);
        yield return new("Odyssey.Beacon.Definitions.SevenLanguagesAndSingleColumn", Content);
        foreach (var file in new[] { "Buildings_Beacons.xml", "Buildings_Mana.xml", "Buildings_Power.xml" })
        {
            var selected = file;
            yield return new("Odyssey.BuildingConfig." + file, f => BuildingConfig(f, selected));
        }
    }
    static void BuildingConfig(RuntimeFixture f, string file)
    {
        var def = XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs", file)).Root!.Element("ThingDef")!;
        var parent = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/ThingDefs/TM_BuildingBase.xml")).Root!.Elements().Single(x => (string?)x.Attribute("Name") == "TM_BuildingBase");
        XElement? Field(string name) => def.Element(name) ?? parent.Element(name);
        var errors = new List<string>();
        var categories = Field("thingCategories")?.Elements("li").Select(x => x.Value).ToList() ?? new List<string>();
        if (Field("minifiedDef") != null && categories.Count == 0) errors.Add("is minifiable but not in any thing category");
        // The two authoring contracts reported by native ThingDef.ConfigErrors in installed 1.7.
        float fill = float.Parse(Field("fillPercent")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
        if (fill < 1f && Field("passability")?.Value == "Impassable" && Field("designationCategory") != null)
            errors.Add("impassable, player-buildable building that can be shot/seen over");
        Check.Equal("", string.Join("; ", errors), file + " native authoring requirements");
        string expected = file == "Buildings_Power.xml" ? "BuildingsPower" : "BuildingsMisc";
        Check.Equal(true, categories.Contains(expected), file + " meaningful storage category");
        var core = Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!, "Data/Core/Defs");
        Check.Equal(true, Directory.GetFiles(core, "*.xml", SearchOption.AllDirectories)
            .SelectMany(p => XDocument.Load(p).Root!.Elements("ThingCategoryDef"))
            .Any(x => (string?)x.Element("defName") == expected), "category exists in installed game");
    }
    static string FilePath(RuntimeFixture f) => Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Beacons.xml");
    static void Native(RuntimeFixture f)
    {
        var doc=new XmlDocument(); doc.Load(FilePath(f));
        var parent=new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var node=doc.SelectSingleNode("Defs/ThingDef[defName='TM_ManaBeacon']")!;
            Check.Equal("TM_BuildingBase",node.Attributes!["ParentName"]!.Value,"shared building parent");
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved=XmlInheritance.GetResolvedNodeFor(node);
            Check.Equal("Building",resolved.SelectSingleNode("thingClass")!.InnerText,"native building");
            Check.Equal("Building",resolved.SelectSingleNode("category")!.InnerText,"building category");
            Check.Equal("MinifiedThing",resolved.SelectSingleNode("minifiedDef")!.InnerText,"native minification");
            Check.Equal("(1,1)",resolved.SelectSingleNode("size")!.InnerText,"one cell footprint");
            Check.Equal("false",resolved.SelectSingleNode("rotatable")!.InnerText,"symmetric column needs no rotation");
            Check.Equal("TM_RWoM",resolved.SelectSingleNode("designationCategory")!.InnerText,"RWoM construction menu");
            Check.Equal(true,resolved.SelectNodes("researchPrerequisites/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet().SetEquals(new[]{"TM_MagofuelRefining","Electricity"}),"research gates");
            var all=new[]{"v1.7/Defs","v1.7/Odyssey/Defs"}.SelectMany(p=>Directory.GetFiles(Path.Combine(f.Repository,p),"*.xml",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data/Core/Defs"),"*.xml",SearchOption.AllDirectories)).SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList();
            foreach(XmlNode cost in resolved.SelectNodes("costList/*")!)
            {
                Check.Equal(true,all.Any(x=>x.Name=="ThingDef"&&(string?)x.Element("defName")==cost.Name),"real material "+cost.Name);
                Check.Equal(true,int.Parse(cost.InnerText)>0,"positive material cost");
            }
            foreach(XmlNode research in resolved.SelectNodes("researchPrerequisites/li")!)
                Check.Equal(true,all.Any(x=>x.Name=="ResearchProjectDef"&&(string?)x.Element("defName")==research.InnerText),"real research "+research.InnerText);
            Check.Equal(true,all.Any(x=>x.Name=="ThingDef"&&(string?)x.Element("defName")=="MinifiedThing"),"real minified definition exists");
            Check.Equal(true,all.Any(x=>x.Name=="DesignationCategoryDef"&&(string?)x.Element("defName")=="TM_RWoM"),"real construction menu exists");
            Check.Equal(50,int.Parse(resolved.SelectSingleNode("costList/Steel")!.InnerText),"steel construction cost");
            Check.Equal(1,int.Parse(resolved.SelectSingleNode("costList/ComponentIndustrial")!.InnerText),"component construction cost");
            Check.Equal(50,int.Parse(resolved.SelectSingleNode("costList/RawMagicyte")!.InnerText),"magicyte construction cost");
            var graphic=DirectXmlToObject.ObjectFromXml<GraphicData>(resolved.SelectSingleNode("graphicData")!,false);
            Check.Equal(typeof(Graphic_Single),graphic.graphicClass,"native shared single graphic");
            Check.Equal("Buildings/ManaBeacon",graphic.texPath,"column texture path");
            Check.Equal(false,graphic.allowFlip,"no mirrored alternative");
            Check.Equal(TickerType.Normal,ParseHelper.FromString<TickerType>(resolved.SelectSingleNode("tickerType")!.InnerText),"normal lifecycle ticks");
            var comps=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().ToList();
            Check.Equal(3,comps.Count,"passive relay, combat interception and visual glow; no fuel or electrical consumption");
            var beacon=DirectXmlToObject.ObjectFromXml<CompProperties>(comps.Single(n=>n.Attributes?["Class"]?.Value=="TorannMagic.Odyssey.CompProperties_ManaBeacon"),false);
            Check.Equal("TorannMagic.Odyssey.CompManaBeacon",beacon.compClass.FullName!,"beacon production comp");
            var network=beacon.compClass.Assembly.GetType("TorannMagic.Odyssey.MapComponent_ManaNetwork")!;
            Check.Equal(5f,(float)network.GetField("Range")!.GetRawConstantValue()!,"five cell range");
            Check.Equal(60,(int)network.GetField("PulseInterval")!.GetRawConstantValue()!,"one second interval");
            Check.Equal(10f,(float)network.GetField("ManaBudget")!.GetRawConstantValue()!,"finite network mana budget");
            Check.Equal(5,(int)network.GetField("FuelBudget")!.GetRawConstantValue()!,"whole item network fuel budget");
            foreach(var entry in new[]{("Buildings_Mana.xml",true,"Send","Send",4),("Buildings_Power.xml",false,"Off","Receive",6)})
            {
                var endpointDoc=new XmlDocument(); endpointDoc.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs",entry.Item1));
                var endpointNodes=endpointDoc.SelectNodes("Defs/ThingDef/comps/li")!.Cast<XmlNode>().ToList();
                Check.Equal(entry.Item5,endpointNodes.Count,"existing components preserved with one endpoint");
                var endpoint=DirectXmlToObject.ObjectFromXml<CompProperties>(endpointNodes.Single(n=>n.Attributes?["Class"]?.Value=="TorannMagic.Odyssey.CompProperties_ManaNetworkEndpoint"),false);
                Check.Equal("TorannMagic.Odyssey.CompManaNetworkEndpoint",endpoint.compClass.FullName!,"production endpoint comp");
                object Field(string name)=>endpoint.GetType().GetField(name)!.GetValue(endpoint)!;
                Check.Equal(entry.Item2,(bool)Field("supportsMana"),"mana capability");
                Check.Equal(true,(bool)Field("supportsFuel"),"fuel capability");
                Check.Equal(entry.Item3,Field("defaultManaMode").ToString()!,"initial mana mode");
                Check.Equal(entry.Item4,Field("defaultFuelMode").ToString()!,"initial fuel mode");
            }
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            RuntimeLog.AssertClean();
        }
        finally { DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear(); }
    }
    static void Content(RuntimeFixture f)
    {
        var folder=XDocument.Load(Path.Combine(f.Repository,"LoadFolders.xml")).Root!.Element("v1.7")!.Elements("li").Single(x=>x.Value=="v1.7/Odyssey");
        Check.Equal("ludeon.rimworld.odyssey",(string?)folder.Attribute("IfModActive"),"optional module");
        var defs=XDocument.Load(FilePath(f)).Root!.Elements().ToList();
        var graphic=defs.Single(x=>x.Name=="ThingDef").Element("graphicData")!;
        Check.Equal("Graphic_Single",graphic.Element("graphicClass")!.Value,"same column from every side"); Check.Equal("false",graphic.Element("allowFlip")!.Value,"no mirrored fallback");
        string[] langs=["English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"];
        Dictionary<string,string> Keys(string language)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",language,"Keyed/TM_ManaBeacon.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English");
        string[] required=["TM_ManaNetworkModeOff","TM_ManaNetworkModeSend","TM_ManaNetworkModeReceive","TM_ManaNetworkManaMode","TM_ManaNetworkFuelMode","TM_ManaNetworkModeDesc","TM_ManaNetworkTarget","TM_ManaNetworkTargetDesc","TM_ManaNetworkConnected","TM_ManaNetworkDisconnected","TM_ManaBeaconInspect","TM_ManaNetworkUnsupportedFuel","TM_ManaNetworkManaPriority","TM_ManaNetworkFuelPriority","TM_ManaNetworkPriorityDesc","TM_ManaNetworkPriority1","TM_ManaNetworkPriority2","TM_ManaNetworkPriority3"];
        Check.Equal(true,required.ToHashSet().SetEquals(english.Keys),"every production UI key exists");
        string Tokens(string value)=>string.Join("|",Regex.Matches(value,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" keyed coverage");
            foreach(var pair in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[pair.Key]),lang+" nonempty "+pair.Key); Check.Equal(Tokens(pair.Value),Tokens(keys[pair.Key]),lang+" placeholders "+pair.Key); }
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(x=>x.Name.LocalName is "label" or "description" or "reportString"))
            {
                if(lang=="English")continue;
                var path=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",def.Name.LocalName,"TM_ManaBeacon.xml");
                var text=XDocument.Load(path).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(text),lang+" translated "+field.Name); Check.Equal(Tokens(field.Value),Tokens(text!),lang+" definition placeholders");
            }
        }
        foreach(var direction in new[]{"single"})
        {
            var path=Path.Combine(f.Repository,"v1.7/Odyssey/Textures",graphic.Element("texPath")!.Value+".png");
            Check.Equal(true,File.Exists(path),"dedicated "+direction+" view");
            var bytes=File.ReadAllBytes(path);
            Check.Equal(true,bytes.Length>32 && bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),direction+" PNG signature");
        }
    }
}



