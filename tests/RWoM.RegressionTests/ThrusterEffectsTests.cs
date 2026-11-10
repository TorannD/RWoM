using System.Xml.Linq;

namespace RWoM.RegressionTests;

internal static class ThrusterEffectsTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Thruster.Effects.NativeAnimatedTextures", NativeTextures);
    }

    static void NativeTextures(RuntimeFixture fixture)
    {
        string root = Path.Combine(fixture.Repository, "v1.7/Odyssey");
        var defs = XDocument.Load(Path.Combine(root, "Defs/Buildings_Thrusters.xml"));
        foreach (string size in new[] { "Small", "Large" })
        {
            var def = defs.Root!.Elements("ThingDef").Single(d => d.Element("defName")!.Value == "TM_" + size + "MagicalThruster");
            var comp = def.Element("comps")!.Elements("li").Single(c => (string?)c.Attribute("Class") == "TorannMagic.Odyssey.CompProperties_MagicalThruster");
            var parameters = comp.Element("flameShaderParameters")!;
            string texture = "/Things/Mote/TM_" + size + "MagicalThruster_Burn";
            Check.Equal(texture, parameters.Element("_MainTex")!.Value, "approved flame has its own native cutscene texture");
            Check.Equal("MoteGlow", comp.Element("flameShaderType")!.Value, "native additive shader");
            Check.Equal(1, parameters.Elements().Count(), "do not overwrite native _Color2 flicker and takeoff/landing alpha");
            Check.Equal(4, comp.Element("flameOffsetsPerDirection")!.Elements().Count(), "all four native nozzle offsets retained");
            Check.Equal("4.0", comp.Element("flameSize")!.Value, "native quad geometry retained");
            byte[] png = File.ReadAllBytes(Path.Combine(root, "Textures", texture.TrimStart('/') + ".png"));
            Check.Equal(true, png.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}), "PNG asset");
            Check.Equal(512, BigEndian(png, 16), "bounded texture width");
            Check.Equal(512, BigEndian(png, 20), "bounded texture height");
            Check.Equal((byte)6, png[25], "RGBA transparency for additive flame");
        }
    }

    static int BigEndian(byte[] bytes, int offset) => (bytes[offset] << 24) | (bytes[offset+1] << 16) | (bytes[offset+2] << 8) | bytes[offset+3];
}
