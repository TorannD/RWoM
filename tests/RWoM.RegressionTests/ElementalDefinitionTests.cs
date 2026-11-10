using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RWoM.RegressionTests;
internal static class ElementalDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Elementals.Definitions.AllTwelveRanksAndUnchangedGameplay", Definitions);
        yield return new("Elementals.Artwork.FourExplicitDirectionsAndVersionIsolation", Artwork);
        yield return new("Elementals.RenderTree.NativeAnimalChildrenRetained", Tree);
    }
    static string Defs(RuntimeFixture f) => Path.Combine(f.Repository,"v1.7/Defs/PawnDefs/Races_Elemental.xml");
    static void Definitions(RuntimeFixture f)
    {
        var document=XDocument.Load(Defs(f));
        var kinds=document.Root!.Elements("PawnKindDef").ToArray();
        Check.Equal(12,kinds.Length,"four elements times lesser, ordinary and greater");
        foreach(var family in new[]{"Earth","Fire","Water","Wind"})
        foreach(var rank in new[]{"Lesser","","Greater"})
        {
            string name="TM_"+rank+family+"_Elemental";
            var kind=kinds.Single(k=>(string?)k.Element("defName")==name);
            var graphic=kind.Descendants("bodyGraphicData").Single();
            Check.Equal("PawnKind/Elementals/"+family+"/Elemental",(string?)graphic.Element("texPath"),"approved family sprite "+name);
            Check.Equal("(1, 1, 1)",(string?)graphic.Element("color"),"full-color sprite avoids legacy tint multiplication "+name);
            Check.Equal("Graphic_Multi",(string?)graphic.Element("graphicClass"),"four directional assets "+name);
            Check.Equal("TransparentPostLight",(string?)graphic.Element("shaderType"),"existing lighting behavior "+name);
        }
        // Whole pre-change definition, excluding ONLY the three intended visual fields.
        // Includes every stat, skill, attack, race/kind identity, size, AI and death action.
        var normalized=new XmlDocument();normalized.Load(Defs(f));
        foreach(XmlNode node in normalized.SelectNodes("//bodyGraphicData/texPath | //bodyGraphicData/color | //race/renderTree")!.Cast<XmlNode>().ToArray())
            node.ParentNode!.RemoveChild(node);
        string actual=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized.OuterXml)));
        Check.Equal("F7CCD1E90512CF5B694DECEED3F219B2019BADCF49DF6CA82E18B2FD9005C237",actual,"all non-target definition data preserved exactly");
    }
    static void Artwork(RuntimeFixture f)
    {
        foreach(var family in new[]{"Earth","Fire","Water","Wind"})
        {
            var hashes=new HashSet<string>();
            foreach(var direction in new[]{"north","east","south","west"})
            {
                string path=Path.Combine(f.Repository,"v1.7/Textures/PawnKind/Elementals",family,"Elemental_"+direction+".png");
                byte[] png=File.ReadAllBytes(path);
                Check.Equal("89504E470D0A1A0A",Convert.ToHexString(png.AsSpan(0,8)),"PNG signature");
                Check.Equal(512,BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4)),"consistent sprite width");
                Check.Equal(512,BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4)),"consistent sprite height");
                Check.Equal((byte)6,png[25],"explicit alpha RGBA");
                hashes.Add(Convert.ToHexString(SHA256.HashData(png)));
            }
            Check.Equal(4,hashes.Count,"four distinct views, no copied front or automatic west fallback");
        }
        var legacy=XDocument.Load(Path.Combine(f.Repository,"v1.6/Defs/PawnDefs/Races_Elemental.xml"));
        Check.Equal(true,legacy.Descendants("bodyGraphicData").All(g=>(string?)g.Element("texPath")=="PawnKind/Elemental"),"v1.6 remains on original shared sprites");
    }
    static void Tree(RuntimeFixture f)
    {
        var races=XDocument.Load(Defs(f));
        Check.Equal("TM_Elemental",races.Root!.Elements("ThingDef").Single(d=>(string?)d.Attribute("Name")=="TM_Base_Elemental").Element("race")!.Element("renderTree")!.Value,"only elemental base opts into visual tree");
        var custom=XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/PawnRenderTreeDefs/TM_Elemental.xml")).Root!.Element("PawnRenderTreeDef")!;
        string game=Path.GetDirectoryName(typeof(Verse.Pawn).Assembly.Location)!;
        var native=XDocument.Load(Path.Combine(game,"Data/Core/Defs/PawnRenderTreeDefs/PawnRenderTreeDefs.xml")).Root!.Elements("PawnRenderTreeDef").Single(d=>(string?)d.Element("defName")=="Animal");
        var body=custom.Element("root")!.Element("children")!.Elements("li").Single(n=>(string?)n.Element("tagDef")=="Body");
        Check.Equal("TorannMagic.PawnRenderNode_ElementalBody",body.Element("nodeClass")!.Value,"custom body node");
        Check.Equal("TorannMagic.PawnRenderNodeWorker_ElementalBody",body.Element("workerClass")!.Value,"custom body worker");
        custom.Element("defName")!.Value="Animal";
        body.Element("nodeClass")!.Value="PawnRenderNode_AnimalPart_Body";
        body.Element("workerClass")!.Value="PawnRenderNodeWorker_AnimalBody";
        Check.Equal(true,XNode.DeepEquals(native,custom),"native root, carried item, wounds, pack and firefoam nodes unchanged");
    }
}
