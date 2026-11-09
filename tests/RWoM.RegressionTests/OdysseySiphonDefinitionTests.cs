using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
namespace RWoM.RegressionTests;
internal static class OdysseySiphonDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Siphon.Definitions.NativeComponentsAndWork", Native);
        yield return new("Odyssey.Siphon.Definitions.SevenLanguages", Content);
        yield return new("Odyssey.Siphon.Definitions.Texture", Texture);
    }
    static string PathFor(RuntimeFixture f) => Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_ManaSiphon.xml");
    static void Native(RuntimeFixture f)
    {
        Check.Equal(true,File.Exists(PathFor(f)),"siphon definition exists");
        var doc=new XmlDocument(); doc.Load(PathFor(f));
        var parent=new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var oldWork=DefDatabase<WorkTypeDef>.AllDefsListForReading.ToArray();
        var oldSkills=DefDatabase<SkillDef>.AllDefsListForReading.ToArray();
        var oldCapacities=DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var node=doc.SelectSingleNode("Defs/ThingDef[defName='TM_ManaSiphon']")!;
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved=XmlInheritance.GetResolvedNodeFor(node);
            foreach(var pair in new[]{("thingClass","Building"),("category","Building"),("minifiedDef","MinifiedThing"),("size","(1,1)"),("rotatable","false"),("designationCategory","TM_RWoM"),("thingCategories/li","BuildingsMisc"),("fillPercent","1"),("passability","Impassable"),("interactionCellOffset","(0,0,-1)"),("researchPrerequisites/li","TM_MagofuelRefining"),("costList/Steel","80"),("costList/RawMagicyte","60"),("costList/ComponentIndustrial","2"),("statBases/WorkToBuild","6000")})
                Check.Equal(pair.Item2,resolved.SelectSingleNode(pair.Item1)!.InnerText,pair.Item1);
            Check.Equal(1,resolved.SelectNodes("researchPrerequisites/li")!.Count,"no advanced research gate");
            var footprint=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            footprint.size=ParseHelper.FromString<IntVec2>(resolved.SelectSingleNode("size")!.InnerText);
            footprint.hasInteractionCell=ParseHelper.FromString<bool>(resolved.SelectSingleNode("hasInteractionCell")!.InnerText);
            footprint.interactionCellOffset=ParseHelper.FromString<IntVec3>(resolved.SelectSingleNode("interactionCellOffset")!.InnerText);
            var position=new IntVec3(20,0,20);
            Check.Equal(new IntVec3(20,0,19),ThingUtility.InteractionCellWhenAt(footprint,position,Rot4.North,null!),"accessible south interaction cell");
            var graphic=DirectXmlToObject.ObjectFromXml<GraphicData>(resolved.SelectSingleNode("graphicData")!,false);
            Check.Equal(typeof(Graphic_Single),graphic.graphicClass,"symmetric single graphic");
            Check.Equal("Buildings/ManaSiphon",graphic.texPath,"dedicated texture");
            var comps=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().Select(n=>DirectXmlToObject.ObjectFromXml<CompProperties>(n,false)).ToList();
            Check.Equal(4,comps.Count,"siphon, passive endpoint, flickable and visual glow; no fuel or power");
            Check.Equal(true,comps.Any(c=>c.compClass==typeof(CompFlickable)),"native switch");
            var siphon=comps.Single(c=>c.compClass.FullName=="TorannMagic.Odyssey.CompManaSiphon");
            var bufferCapacity=Check.NotNull(siphon.compClass.GetField("BufferCapacity"),"siphon overflow buffer capacity exists");
            Check.Equal(100f,(float)bufferCapacity.GetRawConstantValue()!,"bounded internal overflow reserve");
            Check.Equal("Normal",resolved.SelectSingleNode("tickerType")!.InnerText,"automatic buffer drain ticks");
            foreach(var comp in comps) Check.Equal("",string.Join(";",comp.ConfigErrors(footprint)),"native component config "+comp.compClass.Name);
            var endpoint=comps.Single(c=>c.compClass.FullName=="TorannMagic.Odyssey.CompManaNetworkEndpoint");
            foreach(var field in new[]{"supportsMana","supportsFuel"}) Check.Equal(false,(bool)endpoint.GetType().GetField(field)!.GetValue(endpoint)!,"no ordinary endpoint channel "+field);
            var job=DirectXmlToObject.ObjectFromXml<JobDef>(doc.SelectSingleNode("Defs/JobDef")!,false);
            Check.Equal("TorannMagic.Odyssey.JobDriver_DonateManaAtSiphon",job.driverClass.FullName!,"job type resolves");
            var workNode=doc.SelectSingleNode("Defs/WorkGiverDef")!;
            var work=DirectXmlToObject.ObjectFromXml<WorkGiverDef>(workNode,false);
            Check.Equal("TorannMagic.Odyssey.WorkGiver_DonateManaAtSiphon",work.giverClass.FullName!,"workgiver type resolves");
            Check.Equal("TM_Magic",workNode.SelectSingleNode("workType")!.InnerText,"ordinary magic work");
            Check.Equal(true,XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/WorkTypeDefs/Magic_WorkType.xml")).Descendants("defName").Any(n=>n.Value=="TM_Magic"),"real magic work type");
            var prior=XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/WorkGiverDefs/WorkGivers_Magic.xml")).Root!.Elements("WorkGiverDef").Where(x=>(string?)x.Element("workType")=="TM_Magic");
            Check.Equal(true,prior.All(x=>(int)x.Element("priorityInType")!>work.priorityInType),"routine donation follows existing magic work");
            var workDoc=new XmlDocument(); workDoc.Load(Path.Combine(f.Repository,"v1.7/Defs/WorkTypeDefs/Magic_WorkType.xml"));
            var workType=DirectXmlToObject.ObjectFromXml<WorkTypeDef>(workDoc.SelectSingleNode("Defs/WorkTypeDef")!,false);
            DefDatabase<WorkTypeDef>.Clear(); DefDatabase<WorkTypeDef>.Add(workType);
            DefDatabase<SkillDef>.Clear();
            foreach(var name in new[]{"Intellectual","Crafting"}) DefDatabase<SkillDef>.Add(new SkillDef{defName=name});
            var capacityDoc=new XmlDocument(); capacityDoc.Load(Path.Combine(f.Repository,"v1.7/Defs/PawnCapacityDefs/PawnCapacity.xml"));
            var capacity=DirectXmlToObject.ObjectFromXml<PawnCapacityDef>(capacityDoc.SelectSingleNode("Defs/PawnCapacityDef[defName='MagicManipulation']")!,false);
            DefDatabase<PawnCapacityDef>.Clear(); DefDatabase<PawnCapacityDef>.Add(capacity);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            Check.Same(workType,work.workType,"native work type cross reference");
            Check.Same(capacity,work.requiredCapacities.Single(),"native magic capacity cross reference");
            RuntimeLog.AssertClean();
        }
        finally
        {
            DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
            DefDatabase<WorkTypeDef>.Clear(); foreach(var def in oldWork) DefDatabase<WorkTypeDef>.Add(def);
            DefDatabase<SkillDef>.Clear(); foreach(var def in oldSkills) DefDatabase<SkillDef>.Add(def);
            DefDatabase<PawnCapacityDef>.Clear(); foreach(var def in oldCapacities) DefDatabase<PawnCapacityDef>.Add(def);
        }
    }
    static void Content(RuntimeFixture f)
    {
        var defs=XDocument.Load(PathFor(f)).Root!.Elements().ToList();
        string[] langs=["English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"];
        Dictionary<string,string> Keys(string lang)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"Keyed/TM_ManaSiphon.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English");
        Check.Equal(true,new[]{"TM_SiphonReserveLabel","TM_SiphonReserveDesc","TM_SiphonDonationLabel","TM_SiphonDonationDesc","TM_SiphonInspect","TM_SiphonNoTanks","TM_SiphonNeedsFullMana"}.ToHashSet().SetEquals(english.Keys),"all siphon UI keys");
        string Tokens(string s)=>string.Join("|",Regex.Matches(s,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" coverage");
            foreach(var p in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[p.Key]),lang+p.Key); Check.Equal(Tokens(p.Value),Tokens(keys[p.Key]),lang+" placeholders"); }
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(x=>x.Name.LocalName is "label" or "description" or "reportString" or "verb" or "gerund"))
            {
                if(lang=="English")continue;
                var translation=XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",def.Name.LocalName,"TM_ManaSiphon.xml")).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(translation),lang+" translated "+field.Name);
            }
        }
    }
    static void Texture(RuntimeFixture f)
    {
        var file=Path.Combine(f.Repository,"v1.7/Odyssey/Textures/Buildings/ManaSiphon.png");
        Check.Equal(true,File.Exists(file),"approved dedicated texture exists");
        Check.Equal(true,File.ReadAllBytes(file).Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"PNG signature");
    }
}
