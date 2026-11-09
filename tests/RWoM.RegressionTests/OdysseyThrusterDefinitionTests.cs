using System.Xml;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

internal static class OdysseyThrusterDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Thruster.Definitions.NativeAndFuelOnly", Native);
        yield return new("Odyssey.Thruster.Definitions.FacilityPatches", Facilities);
        yield return new("Odyssey.Thruster.Definitions.Languages", Languages);
        yield return new("Odyssey.Thruster.Definitions.ResolvedInheritance", Inheritance);
    }
    static string Root(RuntimeFixture f) => Path.Combine(f.Repository,"v1.7/Odyssey");
    static XmlDocument Definitions(RuntimeFixture f)
    {
        var path=Path.Combine(Root(f),"Defs/Buildings_Thrusters.xml");
        Check.Equal(true,File.Exists(path),"magical thruster definitions exist");
        var doc=new XmlDocument(); doc.Load(path); return doc;
    }
    static void Native(RuntimeFixture f)
    {
        var doc=Definitions(f);
        var original=new XmlDocument(); original.Load(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data/Odyssey/Defs/ThingDefs_Buildings/Buildings_Gravship.xml"));
        foreach(var size in new[]{"Small","Large"})
        {
            var node=doc.SelectSingleNode($"Defs/ThingDef[defName='TM_{size}MagicalThruster']")!;
            var native=original.SelectSingleNode($"Defs/ThingDef[defName='{size}Thruster']")!;
            Check.Equal("ThrusterBase",node.Attributes!["ParentName"]!.Value,"native placement and building constraints inherited");
            Check.Equal(native.SelectSingleNode("size")!.InnerText,node.SelectSingleNode("size")!.InnerText,"native footprint");
            var comp=node.SelectSingleNode("comps/li[@Class='TorannMagic.Odyssey.CompProperties_MagicalThruster']")!;
            var nativeComp=native.SelectSingleNode("comps/li[@Class='CompProperties_GravshipThruster']")!;
            foreach(var field in new[]{"exclusionAreaSize","exclusionAreaOffset","maxDistance","requiresLOS","directionInfluence","flameOffsetsPerDirection","flameSize"})
                Check.Equal(nativeComp.SelectSingleNode(field)!.InnerText,comp.SelectSingleNode(field)!.InnerText,"native "+field);
            Check.Equal("true",comp.SelectSingleNode("providesFuel")!.InnerText,"adapter participates in fuel accounting");
            Check.Equal(0,node.SelectNodes("comps/li[contains(@Class,'Refuelable') or contains(@Class,'Buffer')]")!.Count,"no local fuel or mana storage");
            var endpoint=node.SelectSingleNode("comps/li[@Class='TorannMagic.Odyssey.CompProperties_ManaNetworkEndpoint']")!;
            Check.Equal("false",endpoint.SelectSingleNode("supportsMana")!.InnerText,"mana disabled");
            Check.Equal("true",endpoint.SelectSingleNode("supportsFuel")!.InnerText,"magofuel enabled");
            Check.Equal("Receive",endpoint.SelectSingleNode("defaultFuelMode")!.InnerText,"network fuel reception");
            Check.Equal(1,node.SelectNodes("comps/li[@Class='CompProperties_Breakdownable']")!.Count,"native breakdown preserved");
        }
    }
    static void Apply(XmlDocument target,string path)
    {
        var patch=new XmlDocument(); patch.Load(path);
        foreach(XmlNode node in patch.SelectNodes("Patch/Operation")!)
            Check.Equal(true,DirectXmlToObject.ObjectFromXml<PatchOperation>(node,false).Apply(target),"patch applies "+path);
    }
    static void Inheritance(RuntimeFixture f)
    {
        var doc=Definitions(f);
        string game=Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var core=new XmlDocument(); core.Load(Path.Combine(game,"Data/Core/Defs/ThingDefs_Buildings/Buildings_Base.xml"));
        var native=new XmlDocument(); native.Load(Path.Combine(game,"Data/Odyssey/Defs/ThingDefs_Buildings/Buildings_Gravship.xml"));
        XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear();
        try
        {
            XmlInheritance.TryRegister(core.SelectSingleNode("Defs/ThingDef[@Name='BuildingBase']")!,null);
            foreach(var name in new[]{"GravshipComponentBase","ThrusterBase"})
                XmlInheritance.TryRegister(native.SelectSingleNode($"Defs/ThingDef[@Name='{name}']")!,null);
            foreach(XmlNode node in doc.SelectNodes("Defs/ThingDef")!) XmlInheritance.TryRegister(node,null);
            XmlInheritance.Resolve();
            foreach(XmlNode node in doc.SelectNodes("Defs/ThingDef")!)
            {
                var resolved=XmlInheritance.GetResolvedNodeFor(node);
                Check.Equal("Building",resolved.SelectSingleNode("thingClass")!.InnerText,"native building class");
                Check.Equal("Rare",resolved.SelectSingleNode("tickerType")!.InnerText,"native thruster update cadence");
                Check.Equal("Substructure",resolved.SelectSingleNode("terrainAffordanceNeeded")!.InnerText,"ship foundation required");
                Check.Equal("Impassable",resolved.SelectSingleNode("passability")!.InnerText,"solid thruster");
                Check.Equal("1",resolved.SelectSingleNode("fillPercent")!.InnerText,"impassable building has full fill");
                Check.Equal("MinifiedThing",resolved.SelectSingleNode("minifiedDef")!.InnerText,"minifiable");
                Check.Equal("BuildingsMisc",resolved.SelectSingleNode("thingCategories/li")!.InnerText,"minified storage category");
                var workers=resolved.SelectNodes("placeWorkers/li")!.Cast<XmlNode>().Select(n=>n.InnerText).ToHashSet();
                Check.Equal(true,workers.SetEquals(new[]{"PlaceWorker_GravshipThruster","TorannMagic.Odyssey.PlaceWorker_ManaNetwork"}),"native exhaust preview and network overlay both retained");
                var props=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().Select(n=> {
                    var headless=n.CloneNode(true);
                    // ShaderParameter parses textures eagerly, unavailable in the headless harness.
                    var shader=headless.SelectSingleNode("flameShaderParameters");
                    if(shader!=null) headless.RemoveChild(shader);
                    return DirectXmlToObject.ObjectFromXml<CompProperties>(headless,false);
                }).ToList();
                var thruster=props.OfType<CompProperties_GravshipThruster>().Single();
                Check.Equal(true,typeof(CompGravshipThruster).IsAssignableFrom(thruster.compClass),"native room, clearance and facility implementation");
                Check.Equal(0,props.OfType<CompProperties_Refuelable>().Count(),"no inherited hidden tank");
                // Avoid graphics initialization only; component ConfigErrors remains production code.
                var def=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName=node.SelectSingleNode("defName")!.InnerText; def.comps=props;
                foreach(var comp in props) Check.Equal("",string.Join(";",comp.ConfigErrors(def)),"component configuration "+comp.GetType().Name);
            }
        }
        finally { XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear(); }
    }
    static void Facilities(RuntimeFixture f)
    {
        var doc=Definitions(f);
        foreach(var engine in new[]{"GravEngine","VGE_GravjumperEngine","VGE_GravhulkEngine"})
        {
            var node=doc.CreateElement("ThingDef"); node.InnerXml=$"<defName>{engine}</defName><comps><li Class='CompProperties_AffectedByFacilities'><linkableFacilities><li>SmallThruster</li></linkableFacilities></li></comps>"; doc.DocumentElement!.AppendChild(node);
        }
        var core=Path.Combine(Root(f),"Patches/MagicalThrusters.xml");
        Apply(doc,core); Apply(doc,core);
        var vge=Path.Combine(f.Repository,"TorannMagic.Integrations/v1.7/VGECompat/Patches/MagicalThrusters.xml");
        Apply(doc,vge); Apply(doc,vge);
        foreach(var engine in new[]{"GravEngine","VGE_GravjumperEngine","VGE_GravhulkEngine"})
        foreach(var size in new[]{"Small","Large"})
            Check.Equal(1,doc.SelectNodes($"Defs/ThingDef[defName='{engine}']/comps/li[@Class='CompProperties_AffectedByFacilities']/linkableFacilities/li[text()='TM_{size}MagicalThruster']")!.Count,"explicit unique engine link "+engine+size);
        foreach(var size in new[]{"Small","Large"})
        {
            var comp=doc.SelectSingleNode($"Defs/ThingDef[defName='TM_{size}MagicalThruster']/comps/li[@Class='TorannMagic.Odyssey.CompProperties_MagicalThruster']")!;
            Check.Equal("TorannMagic.VGE.CompMagicalThrusterVge",comp.SelectSingleNode("compClass")!.InnerText,"optional provider adapter");
            Check.Equal(size=="Small"?"6":"10",comp.SelectSingleNode("statOffsets/GravshipRange")!.InnerText,"VGE range balance");
        }
    }
    static void Languages(RuntimeFixture f)
    {
        Definitions(f);
        foreach(var lang in new[]{"English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"})
        {
            var keyedPath=Path.Combine(Root(f),"Languages",lang,"Keyed/TM_MagicalThrusters.xml");
            Check.Equal(true,File.Exists(keyedPath),lang+" adapter UI translations exist");
            var keyed=XDocument.Load(keyedPath).Root!;
            foreach(var key in new[]{"TM_MagicalThrusterFuelName","TM_MagicalThrusterFuelAmount","TM_MagicalThrusterFuelRange"})
            {
                var value=keyed.Element(key)?.Value;
                Check.Equal(false,string.IsNullOrWhiteSpace(value),lang+key);
                var tokens=System.Text.RegularExpressions.Regex.Matches(value!,@"\{[^{}]+\}").Select(m=>m.Value).OrderBy(x=>x);
                Check.Equal(key.EndsWith("Name")?"":"{0}|{1}",string.Join("|",tokens),lang+key+" argument parity");
            }
            var translated=XDocument.Load(Path.Combine(Root(f),"Languages",lang,"DefInjected/ThingDef/TM_MagicalThrusters.xml"));
            foreach(var size in new[]{"Small","Large"})
            foreach(var field in new[]{"label","description"})
                Check.Equal(false,string.IsNullOrWhiteSpace(translated.Root!.Element($"TM_{size}MagicalThruster.{field}")?.Value),lang+size+field);
        }
        foreach(var size in new[]{"Small","Large"})
        {
            var hashes=new HashSet<string>();
            foreach(var view in new[]{"north","south","east","west"})
            {
                string path=Path.Combine(Root(f),$"Textures/Buildings/{size}MagicalThruster_{view}.png");
                Check.Equal(true,File.Exists(path),"dedicated directional sprite "+size+view);
                byte[] bytes=File.ReadAllBytes(path);
                Check.Equal(true,bytes.Length>33&&bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"valid PNG header "+size+view);
                int width=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4));
                int height=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4));
                Check.Equal(true,width>0&&height>0,"positive sprite dimensions");
                Check.Equal((byte)6,bytes[25],"RGBA sprite includes alpha channel");
                hashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
            }
            Check.Equal(4,hashes.Count,"four distinct authored views "+size);
        }
    }
}
