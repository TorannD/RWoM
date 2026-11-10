using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

internal static class OdysseyCombatDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Combat.Definitions.NativeComponentsAndInheritance", Native);
        yield return new("Odyssey.Combat.Definitions.SevenLanguagesAndSingleGraphics", Content);
    }
    static string FilePath(RuntimeFixture f) => Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Combat.xml");
    static void Native(RuntimeFixture f)
    {
        Check.Equal(true,File.Exists(FilePath(f)),"combat buildings are defined");
        var doc=new XmlDocument(); doc.Load(FilePath(f));
        var parent=new XmlDocument(); parent.Load(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        try
        {
            XmlInheritance.TryRegister(parent.SelectSingleNode("Defs/ThingDef[@Name='TM_BuildingBase']")!,null);
            var nodes=doc.SelectNodes("Defs/ThingDef")!.Cast<XmlNode>().ToList();
            Check.Equal(2,nodes.Count,"generator and booster definitions");
            foreach(var node in nodes) XmlInheritance.TryRegister(node,null);
            XmlInheritance.Resolve();
            var all=new[]{"v1.7/Defs","v1.7/Odyssey/Defs"}.SelectMany(p=>Directory.GetFiles(Path.Combine(f.Repository,p),"*.xml",SearchOption.AllDirectories))
                .Concat(new[]{"Core","Odyssey"}.SelectMany(module=>Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data",module,"Defs"),"*.xml",SearchOption.AllDirectories))).SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList();
            foreach(var node in nodes)
            {
                var resolved=XmlInheritance.GetResolvedNodeFor(node);
                string Value(string path)=>resolved.SelectSingleNode(path)!.InnerText;
                bool main=Value("defName")=="TM_CombatShieldGenerator";
                Check.Equal(main?"TM_CombatShieldGenerator":"TM_CombatShieldBooster",Value("defName"),"known combat building");
                foreach(var pair in new Dictionary<string,string>{{"thingClass","Building"},{"designationCategory","TM_RWoM"},{"minifiedDef","MinifiedThing"},{"thingCategories/li","BuildingsMisc"},{"passability","Impassable"},{"fillPercent","1"},{"rotatable","false"},{"size",main?"(2,2)":"(1,1)"}})
                    Check.Equal(pair.Value,Value(pair.Key),Value("defName")+" "+pair.Key);
                Check.Equal(true,resolved.SelectNodes("researchPrerequisites/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet().SetEquals(new[]{"TM_MagofuelRefining","OrbitalTech"}),"research gates");
                foreach(var pair in new[]{("designationCategory","DesignationCategoryDef"),("minifiedDef","ThingDef"),("thingCategories/li","ThingCategoryDef"),("researchPrerequisites/li","ResearchProjectDef")})
                    foreach(XmlNode reference in resolved.SelectNodes(pair.Item1)!) Check.Equal(true,all.Any(x=>x.Name==pair.Item2&&(string?)x.Element("defName")==reference.InnerText),"real cross-reference "+reference.InnerText);
                foreach(XmlNode cost in resolved.SelectNodes("costList/*")!) Check.Equal(true,all.Any(x=>x.Name=="ThingDef"&&(string?)x.Element("defName")==cost.Name),"real material "+cost.Name);
                Check.Equal(main?200:50,int.Parse(Value("costList/Steel")),"steel cost");
                Check.Equal(main?4:1,int.Parse(Value("costList/ComponentIndustrial")),"component cost");
                Check.Equal(main?150:50,int.Parse(Value("costList/RawMagicyte")),"magicyte cost");
                var parsed=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().Select(n=>DirectXmlToObject.ObjectFromXml<CompProperties>(n,false)).ToList();
                var combat=parsed.Single(p=>p.GetType().Name==(main?"CompProperties_MagicalCombat":"CompProperties_MagicalCombatBooster"));
                object Field(CompProperties p,string name)=>p.GetType().GetField(name)!.GetValue(p)!;
                Check.Equal(main?"CompMagicalCombat":"CompMagicalCombatBooster",combat.compClass.Name,"production shield component");
                Check.Equal(main?1000f:250f,(float)Field(combat,"hitPoints"),"shield capacity");
                if(main)
                {
                    Check.Equal(25f,(float)Field(combat,"regenerationPerSecond"),"network regeneration rate");
                    Check.Equal(.01f,(float)Field(combat,"manaPerHitPoint"),"paid regeneration");
                    Check.Equal(.0005f,(float)Field(combat,"manaPerCellSecond"),"paid upkeep");
                    Check.Equal(600,(int)Field(combat,"regenerationDelayTicks"),"ten seconds without hits");
                    Check.Equal(1800,(int)Field(combat,"rebootTicks"),"thirty second collapse delay");
                }
                var graphic=DirectXmlToObject.ObjectFromXml<GraphicData>(resolved.SelectSingleNode("graphicData")!,false);
                Check.Equal(typeof(Graphic_Single),graphic.graphicClass,"native single graphic");
                Check.Equal(main?"Buildings/CombatShield":"Buildings/ShieldBooster",graphic.texPath,"correct dedicated artwork");
                var endpoint=parsed.Single(p=>p.GetType().Name=="CompProperties_ManaNetworkEndpoint");
                Check.Equal(main,(bool)Field(endpoint,"supportsMana"),"only generator receives mana");
                Check.Equal(false,(bool)Field(endpoint,"supportsFuel"),"no fuel channel");
                Check.Equal(main?"Receive":"Off",Field(endpoint,"defaultManaMode").ToString()!,"mana channel default");
                Check.Equal("Off",Field(endpoint,"defaultFuelMode").ToString()!,"fuel disabled");
                var buffers=parsed.Where(p=>p.GetType().Name=="CompProperties_ShieldManaBuffer").ToList();
                Check.Equal(main?1:0,buffers.Count,"only main has a mana bank");
                if(main) Check.Equal(100f,(float)Field(buffers.Single(),"manaCapacity"),"main buffer capacity");
                Check.Equal(main?1:0,parsed.Count(p=>p.GetType().Name=="CompProperties_MagicalCombatInterceptor"),"booster does not extend field");
                Check.Equal(0,parsed.OfType<CompProperties_Refuelable>().Count(),"no physical refueling");
                Check.Equal(0,parsed.OfType<CompProperties_Power>().Count(),"no electrical requirement");
                var building=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                building.defName=Value("defName"); building.comps=parsed;
                foreach(var props in parsed.Where(p=>p.GetType().Namespace=="TorannMagic.Odyssey"))
                    Check.Equal("",string.Join(";",props.ConfigErrors(building)),"valid production config "+props.GetType().Name);
            }
            var beacons=new XmlDocument(); beacons.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Beacons.xml"));
            var intercept=beacons.SelectSingleNode("Defs/ThingDef[defName='TM_ManaBeacon']/comps/li[@Class='TorannMagic.Odyssey.CompProperties_MagicalCombatInterceptor']");
            Check.NotNull(intercept,"beacon extends combat interception");
            Check.Equal("CompMagicalCombatInterceptor",DirectXmlToObject.ObjectFromXml<CompProperties>(intercept!,false).compClass.Name,"native interceptor resolves");
            RuntimeLog.AssertClean();
        }
        finally { DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear(); }
    }
    static void Content(RuntimeFixture f)
    {
        Check.Equal(true,File.Exists(FilePath(f)),"combat buildings are defined");
        var defs=XDocument.Load(FilePath(f)).Root!.Elements().ToList();
        string[] langs=["English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"];
        Dictionary<string,string> Keys(string lang)=>XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"Keyed/TM_CombatShieldGenerator.xml")).Root!.Elements().ToDictionary(x=>x.Name.LocalName,x=>x.Value);
        var english=Keys("English");
        Check.Equal(true,english.Keys.ToHashSet().SetEquals(new[]{"TM_CombatShieldStatusStandby","TM_CombatShieldStatus","TM_CombatShieldSourceHP","TM_CombatShieldReboot","TM_CombatShieldNetworkHP","TM_CombatShieldPeace","TM_CombatShieldCombat"}),"combat inspect translation contract");
        string Tokens(string value)=>string.Join("|",Regex.Matches(value,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x));
        foreach(var lang in langs)
        {
            var keys=Keys(lang); Check.Equal(true,english.Keys.ToHashSet().SetEquals(keys.Keys),lang+" keyed coverage");
            foreach(var pair in english) { Check.Equal(false,string.IsNullOrWhiteSpace(keys[pair.Key]),lang+" nonempty "+pair.Key); Check.Equal(Tokens(pair.Value),Tokens(keys[pair.Key]),lang+" placeholders "+pair.Key); }
            var translations=XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Languages",lang,"DefInjected/ThingDef/TM_CombatShieldGenerator.xml")).Root!;
            foreach(var def in defs)
            foreach(var field in new[]{"label","description"}) Check.Equal(false,string.IsNullOrWhiteSpace(translations.Element(def.Element("defName")!.Value+"."+field)?.Value),lang+" translated "+field);
        }
        foreach(var def in defs)
        {
            var graphic=def.Element("graphicData")!;
            Check.Equal("Graphic_Single",graphic.Element("graphicClass")!.Value,"approved symmetric single graphic");
            var path=Path.Combine(f.Repository,"v1.7/Odyssey/Textures",graphic.Element("texPath")!.Value+".png");
            var bytes=File.ReadAllBytes(path);
            Check.Equal(true,bytes.Length>32&&bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"valid shield PNG");
        }
    }
}

