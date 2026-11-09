using System.Xml;
using System.Xml.Linq;
using RimWorld;
using Verse;
using TorannMagic.Odyssey;

namespace RWoM.RegressionTests;
internal static class OdysseyGravcoreDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Gravcore.Definitions.NativeInheritanceAndStorage", Native);
        yield return new("Odyssey.Gravcore.Definitions.ResearchAndLanguages", Content);
    }
    static XmlDocument Definitions(RuntimeFixture f)
    {
        string path=Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Gravcore.xml");
        Check.Equal(true,File.Exists(path),"magical grav engine definitions exist");
        var doc=new XmlDocument(); doc.Load(path); return doc;
    }
    static void Native(RuntimeFixture f)
    {
        var doc=Definitions(f);
        string game=Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var core=new XmlDocument(); core.Load(Path.Combine(game,"Data/Core/Defs/ThingDefs_Buildings/Buildings_Base.xml"));
        var native=new XmlDocument(); native.Load(Path.Combine(game,"Data/Odyssey/Defs/ThingDefs_Buildings/Buildings_Gravship.xml"));
        XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear();
        try
        {
            XmlInheritance.TryRegister(core.SelectSingleNode("Defs/ThingDef[@Name='BuildingBase']")!,null);
            XmlInheritance.TryRegister(native.SelectSingleNode("Defs/ThingDef[@Name='GravshipComponentBase']")!,null);
            foreach(XmlNode node in doc.SelectNodes("Defs/ThingDef")!) XmlInheritance.TryRegister(node,null);
            XmlInheritance.Resolve();
            foreach(XmlNode node in doc.SelectNodes("Defs/ThingDef")!)
            {
                var resolved=XmlInheritance.GetResolvedNodeFor(node);
                bool engine=node.SelectSingleNode("defName")!.InnerText=="TM_MagicalGravEngine";
                Check.Equal("Building",resolved.SelectSingleNode("category")!.InnerText,"native building category");
                Check.Equal(engine?"true":"false",resolved.SelectSingleNode("rotatable")!.InnerText,"core charging cell rotates with ship; extender stays symmetric");
                Check.Equal("TM_RWoM",resolved.SelectSingleNode("designationCategory")!.InnerText,"magic build tab");
                Check.Equal("Graphic_Single",resolved.SelectSingleNode("graphicData/graphicClass")!.InnerText,"symmetric single sprite");
                var props=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().Select(n=>DirectXmlToObject.ObjectFromXml<CompProperties>(n,false)).ToList();
                Check.Equal(engine?18.9f:16.9f,props.OfType<CompProperties_SubstructureFootprint>().Single().radius,"native footprint coverage");
                Check.Equal(0,props.OfType<CompProperties_Power>().Count(),"native mode requires no electricity");
                var config=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                config.defName=node.SelectSingleNode("defName")!.InnerText; config.comps=props;
                foreach(var comp in props) Check.Equal("",string.Join(";",comp.ConfigErrors(config)),"valid component configuration "+comp.GetType().Name);
                Check.Equal(true,File.Exists(Path.Combine(f.Repository,"v1.7/Odyssey/Textures",resolved.SelectSingleNode("graphicData/texPath")!.InnerText+".png")),"approved single texture exists");
                if(engine)
                {
                    Check.Equal("TorannMagic.Odyssey.Building_MagicalGravEngine",resolved.SelectSingleNode("thingClass")!.InnerText,"native engine subclass");
                    Check.Equal("MinifiedGravEngine",resolved.SelectSingleNode("minifiedDef")!.InnerText,"native minified engine lookup");
                    Check.Equal("(3,3)",resolved.SelectSingleNode("size")!.InnerText,"engine footprint");
                    Check.Equal("500",resolved.SelectSingleNode("statBases/SubstructureSupport")!.InnerText,"native support");
                    var fuel=props.OfType<CompProperties_Refuelable>().Single();
                    Check.Equal(100f,fuel.fuelCapacity,"compact physical fuel store");
                    Check.Equal("TM_Magofuel",resolved.SelectSingleNode("comps/li[@Class='CompProperties_Refuelable']/fuelFilter/thingDefs/li")!.InnerText,"dedicated magofuel filter");
                    Check.Equal("(0,0,-2)",resolved.SelectSingleNode("interactionCellOffset")!.InnerText,"mage charging cell outside engine footprint");
                    Check.Equal(0f,fuel.initialFuelPercent,"starts without free fuel");
                    Check.Equal(0f,fuel.fuelConsumptionRate,"no second launch cost or passive fuel cost");
                    Check.Equal(true,fuel.functionsInVacuum && fuel.targetFuelLevelConfigurable && fuel.showAllowAutoRefuelToggle,"native refill controls in vacuum");
                    Check.Equal(500f,props.OfType<CompProperties_MagofuelReservoir>().Single().manaCapacity,"built-in mana storage");
                    var endpoint=props.OfType<CompProperties_ManaNetworkEndpoint>().Single();
                    Check.Equal("Send",endpoint.defaultManaMode.ToString(),"source mana direction");
                    Check.Equal("Send",endpoint.defaultFuelMode.ToString(),"source fuel direction");
                    var links=resolved.SelectNodes("comps/li[@Class='CompProperties_AffectedByFacilities']/linkableFacilities/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet();
                    var expected=native.SelectNodes("Defs/ThingDef[defName='GravEngine']/comps/li[@Class='CompProperties_AffectedByFacilities']/linkableFacilities/li")!.Cast<XmlNode>().Select(n=>n.InnerText).Where(n=>n!="GravFieldExtender").Concat(new[]{"TM_MagicalGravExtender","TM_SmallMagicalThruster","TM_LargeMagicalThruster"}).ToHashSet();
                    Check.Equal(true,links.SetEquals(expected),"native facilities except extenders plus magical facilities");
                }
                else
                {
                    var facility=props.OfType<CompProperties_GravshipFacility>().Single();
                    Check.Equal(6,facility.maxSimultaneous,"native extender count");
                    Check.Equal(18.9f,facility.maxDistance,"native extender link distance");
                    Check.Equal(true,facility.onlyRequiresLooseConnection,"extender may support itself");
                    Check.Equal("250",resolved.SelectSingleNode("comps/li[@Class='CompProperties_GravshipFacility']/statOffsets/SubstructureSupport")!.InnerText,"native extender support");
                }
            }
        }
        finally { XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear(); }
    }
    static void Content(RuntimeFixture f)
    {
        var doc=Definitions(f);
        var research=doc.SelectSingleNode("Defs/ResearchProjectDef[defName='TM_MagicalGravtech']")!;
        var prereqs=research.SelectNodes("prerequisites/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet();
        Check.Equal(true,prereqs.SetEquals(new[]{"TM_MagofuelRefining","TM_MagicCircle","OrbitalTech"}),"late magic and orbital research gate");
        foreach(XmlNode thing in doc.SelectNodes("Defs/ThingDef")!)
        {
            Check.Equal("TM_MagicalGravtech",thing.SelectSingleNode("researchPrerequisites/li")!.InnerText,"research unlock");
            Check.Equal(0,thing.SelectNodes("costList/Gravcore")!.Count,"buildable without consuming native gravcore");
        }
        foreach(var lang in new[]{"English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"})
        foreach(var type in new[]{"ThingDef","ResearchProjectDef"})
        {
            var path=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected",type,"TM_MagicalGravEngine.xml");
            Check.Equal(true,File.Exists(path),lang+" translated "+type);
            var text=XDocument.Load(path).Root!;
            foreach(XmlNode def in doc.SelectNodes("Defs/"+type)!)
            foreach(var field in new[]{"label","description"})
                Check.Equal(false,string.IsNullOrWhiteSpace(text.Element(def.SelectSingleNode("defName")!.InnerText+"."+field)?.Value),lang+" translated "+field);
        }
    }
}

