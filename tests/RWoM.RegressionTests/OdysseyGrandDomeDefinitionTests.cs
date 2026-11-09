using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
namespace RWoM.RegressionTests;
internal static class OdysseyGrandDomeDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.GrandDome.Definitions.NativeComponentsAndReferences", Native);
        yield return new("Odyssey.GrandDome.Definitions.SevenLanguages", Content);
        yield return new("Odyssey.GrandDome.Definitions.ApprovedFourCellFootprintAndGraphic", Footprint);
    }
    static string FilePath(RuntimeFixture f) => Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_GrandDome.xml");
    static void Native(RuntimeFixture f)
    {
        Check.Equal(true,File.Exists(FilePath(f)),"grand dome definitions exist");
        var doc=new XmlDocument(); doc.Load(FilePath(f));
        var parent=new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var old=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var node=doc.SelectSingleNode("Defs/ThingDef[defName='TM_GrandAtmosphereDome']")!;
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved=XmlInheritance.GetResolvedNodeFor(node);
            string Value(string path)=>resolved.SelectSingleNode(path)!.InnerText;
            Check.Equal("Building",Value("thingClass"),"native building");
            Check.Equal("MinifiedThing",Value("minifiedDef"),"minification");
            Check.Equal("BuildingsMisc",Value("thingCategories/li"),"storage category");
            Check.Equal("Impassable",Value("passability"),"solid building");
            Check.Equal("1",Value("fillPercent"),"full fill");
            Check.Equal("TM_RWoM",Value("designationCategory"),"construction menu");
            Check.Equal("120",Value("statBases/Mass"),"mass");
            Check.Equal("700",Value("statBases/MaxHitPoints"),"durability");
            foreach(var cost in new[]{("Steel",400),("RawMagicyte",500),("ComponentIndustrial",8),("GravlitePanel",100)})
                Check.Equal(cost.Item2,int.Parse(Value("costList/"+cost.Item1)),"cost "+cost.Item1);
            Check.Equal("TM_GrandAtmosphereDomes",Value("researchPrerequisites/li"),"construction research");
            var all=new[]{"v1.7/Defs","v1.7/Odyssey/Defs"}.SelectMany(p=>Directory.GetFiles(Path.Combine(f.Repository,p),"*.xml",SearchOption.AllDirectories))
                .Concat(new[]{"Core","Odyssey"}.SelectMany(module=>Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data",module,"Defs"),"*.xml",SearchOption.AllDirectories))).SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList();
            foreach(XmlNode cost in resolved.SelectNodes("costList/*")!) Check.Equal(true,all.Any(x=>x.Name=="ThingDef"&&(string?)x.Element("defName")==cost.Name),"real material "+cost.Name);
            var research=doc.SelectSingleNode("Defs/ResearchProjectDef")!;
            Check.Equal("6000",research.SelectSingleNode("baseCost")!.InnerText,"research cost");
            Check.Equal("Spacer",research.SelectSingleNode("techLevel")!.InnerText,"research tech");
            Check.Equal("TMagic",research.SelectSingleNode("tab")!.InnerText,"research tab");
            Check.Equal(true,research.SelectNodes("prerequisites/li")!.Cast<XmlNode>().Select(x=>x.InnerText).ToHashSet().SetEquals(new[]{"TM_MagofuelRefining","TM_MagicCircle","OrbitalTech"}),"research prerequisites");
            foreach(XmlNode p in research.SelectNodes("prerequisites/li")!) Check.Equal(true,all.Any(x=>x.Name=="ResearchProjectDef"&&(string?)x.Element("defName")==p.InnerText),"real research "+p.InnerText);
            var parsed=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().Select(n=>DirectXmlToObject.ObjectFromXml<CompProperties>(n,false)).ToList();
            Check.Equal(6,parsed.Count,"dome, fuel, endpoint, switch, breakdown and visual glow");
            var dome=parsed.Single(p=>p.GetType().Name=="CompProperties_GrandAtmosphereDome");
            object Field(CompProperties p,string n)=>p.GetType().GetField(n)!.GetValue(p)!;
            Check.Equal("TorannMagic.Odyssey.CompGrandAtmosphereDome",dome.compClass.FullName!,"production dome class");
            Check.Equal(100f,(float)Field(dome,"fuelPerDay"),"fixed daily fuel");
            Check.Equal(3600,(int)Field(dome,"ritualTicks"),"60 second ritual");
            Check.Equal(.1f,(float)Field(dome,"ritualManaFraction"),"personal mana cost");
            Check.Equal(.2f,(float)Field(dome,"personalReserve"),"personal mana reserve");
            var endpoint=parsed.Single(p=>p.GetType().Name=="CompProperties_ManaNetworkEndpoint");
            Check.Equal(false,(bool)Field(endpoint,"supportsMana"),"no mana channel");
            Check.Equal(true,(bool)Field(endpoint,"supportsFuel"),"fuel channel");
            Check.Equal("Receive",Field(endpoint,"defaultFuelMode").ToString()!,"default fuel receive");
            Check.Equal("Off",Field(endpoint,"defaultManaMode").ToString()!,"mana off");
            Check.Equal(1,parsed.OfType<CompProperties_Flickable>().Count(),"switch");
            Check.Equal(1,parsed.OfType<CompProperties_Breakdownable>().Count(),"breakdown");
            var fuel=parsed.OfType<CompProperties_Refuelable>().Single();
            var magic=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); magic.defName="TM_Magofuel"; magic.category=ThingCategory.Item; magic.virtualDefs=[];
            DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(magic);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors); fuel.fuelFilter.ResolveReferences();
            Check.Equal(true,fuel.fuelFilter.Allows(magic),"native magofuel filter");
            Check.Equal(1,fuel.fuelFilter.AllowedThingDefs.Count(),"only magofuel");
            Check.Equal(200f,fuel.fuelCapacity,"local fuel capacity");
            Check.Equal(0f,fuel.initialFuelPercent,"no free fuel");
            Check.Equal(0f,fuel.fuelConsumptionRate,"dome owns consumption");
            Check.Equal(true,fuel.consumeFuelOnlyWhenUsed&&fuel.functionsInVacuum&&fuel.targetFuelLevelConfigurable&&fuel.initialAllowAutoRefuel,"native refuelling in vacuum");
            var job=DirectXmlToObject.ObjectFromXml<JobDef>(doc.SelectSingleNode("Defs/JobDef")!,false);
            Check.Equal("TorannMagic.Odyssey.JobDriver_ActivateGrandDome",job.driverClass.FullName!,"native ritual driver");
            RuntimeLog.AssertClean();
        }
        finally { DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear(); DefDatabase<ThingDef>.Clear(); foreach(var def in old) DefDatabase<ThingDef>.Add(def); }
    }
    static void Footprint(RuntimeFixture f)
    {
        var doc=new XmlDocument(); doc.Load(FilePath(f));
        var node=doc.SelectSingleNode("Defs/ThingDef")!;
        Check.Equal("(4,4)",node.SelectSingleNode("size")?.InnerText,"approved four by four footprint");
        Check.Equal("false",node.SelectSingleNode("rotatable")?.InnerText,"symmetric single view");
        Check.Equal("true",node.SelectSingleNode("hasInteractionCell")?.InnerText,"ritual interaction cell");
        var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.size=ParseHelper.FromString<IntVec2>(node.SelectSingleNode("size")!.InnerText);
        def.hasInteractionCell=true;
        def.interactionCellOffset=ParseHelper.FromString<IntVec3>(node.SelectSingleNode("interactionCellOffset")!.InnerText);
        Check.Equal(new IntVec3(0,0,-2),def.interactionCellOffset,"south interaction offset");
        var position=new IntVec3(20,0,20);
        var rect=GenAdj.OccupiedRect(position,Rot4.North,def.size);
        Check.Equal(16,rect.Count(),"sixteen occupied native cells");
        var interaction=ThingUtility.InteractionCellWhenAt(def,position,Rot4.North,null!);
        Check.Equal(false,rect.Contains(interaction),"interaction outside solid footprint");
        Check.Equal(true,GenAdj.CardinalDirections.Any(offset=>rect.Contains(interaction+offset)),"interaction adjacent to footprint");
        var graphic=DirectXmlToObject.ObjectFromXml<GraphicData>(node.SelectSingleNode("graphicData")!,false);
        Check.Equal(typeof(Graphic_Single),graphic.graphicClass,"single approved graphic");
        Check.Equal("Buildings/GrandAtmosphereDome",graphic.texPath,"dedicated dome texture");
        Check.Equal(new UnityEngine.Vector2(4.4f,4.4f),graphic.drawSize,"approved draw size");
        Check.Equal(false,graphic.allowFlip,"no mirrored fallback");
        var path=Path.Combine(f.Repository,"v1.7/Odyssey/Textures",graphic.texPath+".png");
        Check.Equal(true,File.Exists(path),"approved texture exists");
        var png=File.ReadAllBytes(path);
        Check.Equal(true,png.Length>32&&png.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"PNG format");
    }
    static void Content(RuntimeFixture f)
    {
        Check.Equal(true,File.Exists(FilePath(f)),"grand dome definitions exist");
        var defs=XDocument.Load(FilePath(f)).Root!.Elements().ToList();
        string[] langs=["English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"];
        Dictionary<string,string> Keys(string lang)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"Keyed/TM_GrandAtmosphereDome.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English");
        Check.Equal(true,english.Keys.ToHashSet().SetEquals(new[]{"TM_GrandDomeRitual","TM_GrandDomeUnavailable","TM_GrandDomeUnarmed","TM_GrandDomeStandby","TM_GrandDomeActive","TM_GrandDomeLowFuel","TM_GrandDomeExhausted"}),"all UI keys");
        string Tokens(string value)=>string.Join("|",Regex.Matches(value,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" key coverage");
            foreach(var pair in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[pair.Key]),lang+" nonempty key"); Check.Equal(Tokens(pair.Value),Tokens(keys[pair.Key]),lang+" placeholders"); }
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(x=>x.Name.LocalName is "label" or "description" or "reportString"))
            {
                var path=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",def.Name.LocalName,"TM_GrandAtmosphereDome.xml");
                var text=XDocument.Load(path).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(text),lang+" translated "+field.Name);
                Check.Equal(Tokens(field.Value),Tokens(text!),lang+" definition placeholders");
            }
        }
    }
}
