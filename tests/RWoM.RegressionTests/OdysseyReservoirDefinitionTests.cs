using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using TorannMagic.Odyssey;

namespace RWoM.RegressionTests;

internal static class OdysseyReservoirDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Reservoir.Definitions.NativeComponentsAndInheritance", Native);
        yield return new("Odyssey.Reservoir.Definitions.SevenLanguagesAndFourViews", Content);
    }
    static string FilePath(RuntimeFixture f) => Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Buildings_Mana.xml");
    static void Native(RuntimeFixture f)
    {
        var doc = new XmlDocument(); doc.Load(FilePath(f));
        var parent = new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var old = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var node = doc.SelectSingleNode("Defs/ThingDef[defName='TM_MagofuelReservoir']")!;
            Check.Equal("TM_BuildingBase",node.Attributes!["ParentName"]!.Value,"shared actual building parent");
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved = XmlInheritance.GetResolvedNodeFor(node);
            Check.Equal("Building",resolved.SelectSingleNode("thingClass")!.InnerText,"native building owner");
            Check.Equal("Building",resolved.SelectSingleNode("category")!.InnerText,"inherits building category");
            Check.Equal("MinifiedThing",resolved.SelectSingleNode("minifiedDef")!.InnerText,"native minification");
            Check.Equal("(2,2)",resolved.SelectSingleNode("size")!.InnerText,"two by two footprint");
            Check.Equal("(0,0,-1)",resolved.SelectSingleNode("interactionCellOffset")!.InnerText,"front interaction cell outside footprint");
            Check.Equal("true",resolved.SelectSingleNode("rotatable")!.InnerText,"four orientations");
            var footprint=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            footprint.size=ParseHelper.FromString<IntVec2>(resolved.SelectSingleNode("size")!.InnerText);
            footprint.hasInteractionCell=ParseHelper.FromString<bool>(resolved.SelectSingleNode("hasInteractionCell")!.InnerText);
            footprint.interactionCellOffset=ParseHelper.FromString<IntVec3>(resolved.SelectSingleNode("interactionCellOffset")!.InnerText);
            var position=new IntVec3(20,0,20);
            foreach(var rotation in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West})
            {
                var rect=GenAdj.OccupiedRect(position,rotation,footprint.size);
                var interaction=ThingUtility.InteractionCellWhenAt(footprint,position,rotation,null!);
                Check.Equal(4,rect.Cells.Count(),rotation+" native footprint has four cells");
                Check.Equal(false,rect.Contains(interaction),rotation+" interaction cell outside impassable building");
                Check.Equal(true,GenAdj.CardinalDirections.Any(offset=>rect.Contains(interaction+offset)),rotation+" interaction cell touches building edge");
            }
            Check.Equal("TM_MagofuelRefining",resolved.SelectSingleNode("researchPrerequisites/li")!.InnerText,"existing research gate");
            var all = new[] { "v1.7/Defs", "v1.7/Odyssey/Defs" }.SelectMany(p=>Directory.GetFiles(Path.Combine(f.Repository,p),"*.xml",SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data/Core/Defs"),"*.xml",SearchOption.AllDirectories))
                .SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList();
            foreach (XmlNode cost in resolved.SelectNodes("costList/*")!)
                Check.Equal(true,all.Any(x=>x.Name=="ThingDef" && (string?)x.Element("defName")==cost.Name),"real construction material "+cost.Name);
            Check.Equal(true,all.Any(x=>x.Name=="ResearchProjectDef" && (string?)x.Element("defName")=="TM_MagofuelRefining"),"real research exists");
            Check.Equal(true,all.Any(x=>x.Name=="ThingDef" && (string?)x.Element("defName")=="TM_Magofuel"),"real fuel exists");
            Check.Equal(true,all.Any(x=>x.Name=="ThingDef" && (string?)x.Element("defName")=="MinifiedThing"),"real native minified definition exists");
            var comps = resolved.SelectNodes("comps/li")!.Cast<XmlNode>().ToList();
            Check.Equal(4,comps.Count,"native fuel, custom charge storage, network endpoint and visual glow; no explosive comp");
            var fuel = DirectXmlToObject.ObjectFromXml<CompProperties_Refuelable>(comps.Single(n=>n.Attributes?["Class"]?.Value=="CompProperties_Refuelable"),false);
            var charge = DirectXmlToObject.ObjectFromXml<CompProperties_MagofuelReservoir>(comps.Single(n=>n.Attributes?["Class"]?.Value==typeof(CompProperties_MagofuelReservoir).FullName),false);
            var item=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); item.defName="TM_Magofuel"; item.category=ThingCategory.Item; item.virtualDefs=[];
            DefDatabase<ThingDef>.Clear(); DefDatabase<ThingDef>.Add(item);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors); fuel.fuelFilter.ResolveReferences();
            Check.Equal(true,fuel.fuelFilter.Allows(item),"resolved fuel link");
            Check.Equal(1,fuel.fuelFilter.AllowedThingDefs.Count(),"no ordinary chemfuel");
            Check.Equal(500f,fuel.fuelCapacity,"physical capacity"); Check.Equal(0f,fuel.initialFuelPercent,"no free initial fuel");
            Check.Equal(0f,fuel.fuelConsumptionRate,"no automatic fuel consumption");
            Check.Equal(0f,fuel.fuelConsumptionPerTickInRain,"no rain loss");
            Check.Equal(true,fuel.functionsInVacuum,"fuel available in vacuum");
            Check.Equal(1f,fuel.FuelMultiplierCurrentDifficulty,"one physical item per fuel unit");
            Check.Equal(true,fuel.targetFuelLevelConfigurable && fuel.initialAllowAutoRefuel && fuel.showAllowAutoRefuelToggle && fuel.allowRefuelIfNotEmpty,"native refill controls");
            Check.Equal(false,fuel.destroyOnNoFuel || fuel.factorByDifficulty,"no destruction or difficulty multiplication");
            Check.Equal(500f,fuel.initialConfigurableTargetFuelLevel,"initial refill target");
            Check.Equal(80,int.Parse(resolved.SelectSingleNode("costList/RawMagicyte")!.InnerText),"reservoir magicyte construction cost");
            Check.Equal(1000f,charge.manaCapacity,"mana capacity"); Check.Equal(10f,charge.manaPerFuel,"conversion ratio"); Check.Equal(10f,charge.maxTransfer,"bounded transfer");
            var job=DirectXmlToObject.ObjectFromXml<JobDef>(doc.SelectSingleNode("Defs/JobDef[defName='TM_ChargeMagofuelReservoir']")!,false);
            Check.Equal("TorannMagic.Odyssey.JobDriver_ChargeMagofuelReservoir",Check.NotNull(job.driverClass,"production charge driver loaded").FullName!,"charge job resolves production driver");
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
        Dictionary<string,string> Keys(string language)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",language,"Keyed/TM_MagofuelReservoir.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English"); Check.Equal(11,english.Count,"all UI keys present");
        string Tokens(string value)=>string.Join("|",Regex.Matches(value,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" keyed coverage");
            foreach(var pair in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[pair.Key]),lang+" nonempty "+pair.Key); Check.Equal(Tokens(pair.Value),Tokens(keys[pair.Key]),lang+" placeholders "+pair.Key); }
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(x=>x.Name.LocalName is "label" or "description" or "reportString"))
            {
                if(lang=="English")continue;
                var path=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",def.Name.LocalName,"TM_MagofuelReservoir.xml");
                var text=XDocument.Load(path).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(text),lang+" translated "+field.Name); Check.Equal(Tokens(field.Value),Tokens(text!),lang+" definition placeholders");
            }
        }
        foreach(var direction in new[]{"north","east","south","west"})
            Check.Equal(true,File.Exists(Path.Combine(f.Repository,"v1.7/Odyssey/Textures",graphic.Element("texPath")!.Value+"_"+direction+".png")),"dedicated "+direction+" view");
    }
}



