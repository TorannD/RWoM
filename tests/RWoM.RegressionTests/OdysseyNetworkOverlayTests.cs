using System.Xml.Linq;
using System.Runtime.CompilerServices;
using TorannMagic;
using UnityEngine;
using Verse;

namespace RWoM.RegressionTests;

internal static class OdysseyNetworkOverlayTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Beacon.Overlay.RangeRotationAndMapEdges", Geometry);
        yield return new("Odyssey.Beacon.Overlay.PlacementDefinitionsAndTranslucency", Wiring);
    }

    static Type Overlay => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.ManaNetworkOverlay", true)!;
    static HashSet<IntVec3> Cells(ThingDef def, IntVec3 center, Rot4 rot, IntVec3 size) =>
        ((IEnumerable<IntVec3>)Overlay.GetMethod("Cells")!.Invoke(null, [def, center, rot, size])!).ToHashSet();

    static void Geometry(RuntimeFixture f)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.size = new IntVec2(1, 1);
        var center = new IntVec3(20, 0, 20);
        var size = new IntVec3(50, 1, 50);
        var cells = Cells(def, center, Rot4.North, size);
        Check.Equal(81, cells.Count, "five-cell disk includes every interior tile, not just ring");
        Check.Equal(true, cells.Contains(center + new IntVec3(3, 0, 4)), "3-4-5 boundary included");
        Check.Equal(false, cells.Contains(center + new IntVec3(4, 0, 4)), "diagonal outside real radius excluded");
        Check.Equal(false, cells.Contains(center + new IntVec3(6, 0, 0)), "six-cell range excluded");
        foreach (var rot in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
            Check.Equal(true, cells.SetEquals(Cells(def, center, rot, size)), "symmetric beacon rotations");

        def.size = new IntVec2(5, 1);
        var north = Cells(def, center, Rot4.North, size);
        var east = Cells(def, center, Rot4.East, size);
        Check.Equal(true, north.Contains(center + new IntVec3(7, 0, 0)), "endpoint radius starts at footprint edge");
        Check.Equal(false, east.Contains(center + new IntVec3(7, 0, 0)), "rotated footprint changes attachment zone");
        Check.Equal(true, east.Contains(center + new IntVec3(0, 0, 7)), "rotated long edge included");
        var clipped = Cells(def, IntVec3.Zero, Rot4.East, new IntVec3(8, 1, 8));
        Check.Equal(true, clipped.All(c => c.x >= 0 && c.z >= 0 && c.x < 8 && c.z < 8), "map-edge clipping");
        Check.Equal(0, Cells(def, center, Rot4.North, IntVec3.Zero).Count, "empty map has no overlays");
    }

    static void Wiring(RuntimeFixture f)
    {
        Check.Equal(true, Overlay.IsDefined(typeof(StaticConstructorOnStartup), false),
            "overlay owning a static Material declares main-thread startup");
        foreach (var file in new[] { "Buildings_Beacons.xml", "Buildings_Mana.xml", "Buildings_Power.xml" })
        {
            var def = XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs", file)).Root!.Element("ThingDef")!;
            string name = def.Element("placeWorkers")!.Element("li")!.Value;
            var type = typeof(MagicData).Assembly.GetType(name, true)!;
            Check.Equal(true, typeof(PlaceWorker).IsAssignableFrom(type), file + " native placement worker");
            Check.Equal(type, type.GetMethod("DrawGhost")!.DeclaringType!, file + " placement preview implemented");
        }
        var color = (Color)Overlay.GetField("FillColor")!.GetValue(null)!;
        Check.Equal(true, color.a > 0 && color.a < .3f, "translucent fill preserves ground visibility");
        Check.Equal(true, color.r > color.g && color.b > color.g, "purple fill");
    }
}
