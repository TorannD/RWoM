using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using TorannMagic;

namespace RWoM.RegressionTests;

internal static class OdysseyPowerDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Power.Definitions.NativeComponentsAndInheritance", Native);
        yield return new("Odyssey.Power.Definitions.SevenLanguagesAndFourViews", Content);
    }
    static string FilePath(RuntimeFixture f) => Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Buildings_Power.xml");
    static void Native(RuntimeFixture f)
    {
        var doc = new XmlDocument(); doc.Load(FilePath(f));
        var parent = new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var old = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var node = doc.SelectSingleNode("Defs/ThingDef[defName='TM_MagofuelGenerator']")!;
            Check.Equal("TM_BuildingBase",node.Attributes!["ParentName"]!.Value,"shared actual building parent");
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved = XmlInheritance.GetResolvedNodeFor(node);
            Check.Equal("Building",resolved.SelectSingleNode("thingClass")!.InnerText,"native building owner");
            Check.Equal("Building",resolved.SelectSingleNode("category")!.InnerText,"inherits building category");
            Check.Equal("MinifiedThing",resolved.SelectSingleNode("minifiedDef")!.InnerText,"native minification");
            Check.Equal("(2,2)",resolved.SelectSingleNode("size")!.InnerText,"two by two footprint");
            Check.Equal("true",resolved.SelectSingleNode("rotatable")!.InnerText,"four orientations");
            foreach(var rotation in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West})
                Check.Equal(4,GenAdj.OccupiedRect(new IntVec3(20,0,20),rotation,ParseHelper.FromString<IntVec2>(resolved.SelectSingleNode("size")!.InnerText)).Cells.Count(),rotation+" native footprint");
            Check.Equal("TM_RWoM",resolved.SelectSingleNode("designationCategory")!.InnerText,"RWoM build menu");
            Check.Equal(true,resolved.SelectNodes("researchPrerequisites/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet().SetEquals(new[]{"TM_MagofuelRefining","Electricity"}),"requires refining and electrical technology");
            Check.Equal(true,resolved.SelectNodes("costList/*")!.Count>0,"construction requires materials");
            var all = new[] { "v1.7/Defs", "v1.7/Odyssey/Defs" }.SelectMany(p=>Directory.GetFiles(Path.Combine(f.Repository,p),"*.xml",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data/Core/Defs"),"*.xml",SearchOption.AllDirectories))
                .SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList();
            Check.Equal(true,all.Any(x=>x.Name=="DesignationCategoryDef" && (string?)x.Element("defName")==resolved.SelectSingleNode("designationCategory")!.InnerText),"real build menu category");
            foreach (XmlNode cost in resolved.SelectNodes("costList/*")!)
            {
                Check.Equal(true,all.Any(x=>x.Name=="ThingDef" && (string?)x.Element("defName")==cost.Name),"real construction material "+cost.Name);
                Check.Equal(true,int.Parse(cost.InnerText)>0,"positive material cost "+cost.Name);
            }
            Check.Equal(true,all.Any(x=>x.Name=="ResearchProjectDef" && (string?)x.Element("defName")=="TM_MagofuelRefining"),"real research exists");
            Check.Equal(true,all.Any(x=>x.Name=="ThingDef" && (string?)x.Element("defName")=="TM_Magofuel"),"real fuel exists");
            Check.Equal(true,all.Any(x=>x.Name=="ThingDef" && (string?)x.Element("defName")=="MinifiedThing"),"real native minified definition exists");
            var comps = resolved.SelectNodes("comps/li")!.Cast<XmlNode>().ToList();
            Check.Equal(6,comps.Count,"fuel, power, switch, breakdown, network endpoint and visual glow; no independent heat or explosive comp");
            Check.Equal(1,comps.Count(n=>n.Attributes?["Class"]?.Value=="CompProperties_Flickable"),"native switch");
            Check.Equal(1,comps.Count(n=>n.Attributes?["Class"]?.Value=="CompProperties_Breakdownable"),"native breakdown");
            var fuel = DirectXmlToObject.ObjectFromXml<CompProperties_Refuelable>(comps.Single(n=>n.Attributes?["Class"]?.Value=="CompProperties_Refuelable"),false);
            var powerNode=comps.Single(n=>n.Attributes?["Class"]?.Value=="TorannMagic.Odyssey.CompProperties_MagofuelPower");
            var power=DirectXmlToObject.ObjectFromXml<CompProperties>(powerNode,false);
            Check.Equal("TorannMagic.Odyssey.CompPowerTrader_Magofuel",power.compClass.FullName!,"production component resolves");
            var electrical=(CompProperties_Power)power;
            Check.Equal(-3000f,electrical.PowerConsumption,"generator supplies positive 3000W");
            Check.Equal(true,electrical.transmitsPower,"native grid connection");
            Check.Equal(3f,(float)power.GetType().GetField("fuelPerDay")!.GetValue(power)!,"explicit operating fuel rate");
            Check.Equal(3f,(float)power.GetType().GetField("fuelPerDay")!.GetValue(Activator.CreateInstance(power.GetType()))!,"default operating fuel rate");
            Check.Equal(100,int.Parse(resolved.SelectSingleNode("costList/RawMagicyte")!.InnerText),"generator magicyte construction cost");
            Check.Equal(6f,(float)power.GetType().GetField("heatPerSecond")!.GetValue(power)!,"explicit paid heat rate");
            foreach(XmlNode research in resolved.SelectNodes("researchPrerequisites/li")!)
                Check.Equal(true,all.Any(x=>x.Name=="ResearchProjectDef" && (string?)x.Element("defName")==research.InnerText),"real research "+research.InnerText);
            var item=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); item.defName="TM_Magofuel"; item.category=ThingCategory.Item; item.virtualDefs=[];
            DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(item);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors); fuel.fuelFilter.ResolveReferences();
            Check.Equal(true,fuel.fuelFilter.Allows(item),"resolved fuel link");
            Check.Equal(1,fuel.fuelFilter.AllowedThingDefs.Count(),"no ordinary chemfuel");
            Check.Equal(50f,fuel.fuelCapacity,"physical capacity"); Check.Equal(0f,fuel.initialFuelPercent,"no free initial fuel");
            Check.Equal(0f,fuel.fuelConsumptionRate,"no automatic fuel consumption");
            Check.Equal(0f,fuel.fuelConsumptionPerTickInRain,"no rain loss");
            Check.Equal(true,fuel.functionsInVacuum,"fuel available in vacuum");
            Check.Equal(1f,fuel.FuelMultiplierCurrentDifficulty,"one physical item per fuel unit");
            Check.Equal(true,fuel.targetFuelLevelConfigurable && fuel.initialAllowAutoRefuel && fuel.showAllowAutoRefuelToggle && fuel.allowRefuelIfNotEmpty,"native refill controls");
            Check.Equal(false,fuel.destroyOnNoFuel || fuel.factorByDifficulty,"no destruction or difficulty multiplication");
            RuntimeLog.AssertClean();
        }
        finally { DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear(); DefDatabase<ThingDef>.Clear(); foreach(var def in old)DefDatabase<ThingDef>.Add(def); }
    }
    static void Content(RuntimeFixture f)
    {
        var folder=XDocument.Load(Path.Combine(f.Repository,"LoadFolders.xml")).Root!.Element("v1.7")!.Elements("li").Single(x=>x.Value=="v1.7/Odyssey");
        Check.Equal("ludeon.rimworld.odyssey",(string?)folder.Attribute("IfModActive"),"optional module");
        var defs=XDocument.Load(FilePath(f)).Root!.Elements().ToList();
        var graphic=defs.Single(x=>x.Name=="ThingDef").Element("graphicData")!;
        Check.Equal("Graphic_Multi",graphic.Element("graphicClass")!.Value,"native directional graphics"); Check.Equal("false",graphic.Element("allowFlip")!.Value,"no mirrored fallback");
        string[] langs=["English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"];
        Dictionary<string,string> Keys(string language)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",language,"Keyed/TM_MagofuelGenerator.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English"); Check.Equal(true,english.Count>0,"UI text exists");
        string Tokens(string value)=>string.Join("|",Regex.Matches(value,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" keyed coverage");
            foreach(var pair in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[pair.Key]),lang+" nonempty "+pair.Key); Check.Equal(Tokens(pair.Value),Tokens(keys[pair.Key]),lang+" placeholders "+pair.Key); }
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(x=>x.Name.LocalName is "label" or "description" or "reportString"))
            {
                if(lang=="English")continue;
                var path=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",def.Name.LocalName,"TM_MagofuelGenerator.xml");
                var text=XDocument.Load(path).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(text),lang+" translated "+field.Name); Check.Equal(Tokens(field.Value),Tokens(text!),lang+" definition placeholders");
            }
        }
        foreach(var direction in new[]{"north","east","south","west"})
            Check.Equal(true,File.Exists(Path.Combine(f.Repository,"v1.7/Odyssey/Textures",graphic.Element("texPath")!.Value+"_"+direction+".png")),"dedicated "+direction+" view");
    }
}



