using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

// Characterization of the installed Odyssey engine, before implementing magical thrusters.
internal static class OdysseyThrusterContractTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.ThrusterContract.NativeFourRotationClearance", _ => Geometry());
        yield return new("Odyssey.ThrusterContract.NativeWallsPlantsAndSubstructure", f => OdysseyOxygenTests.Run(f, Blocking));
    }
    static IEnumerable<(ThingDef Def, CompProperties_GravshipThruster Props)> Originals()
    {
        var xml=new XmlDocument();
        xml.Load(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!,"Data/Odyssey/Defs/ThingDefs_Buildings/Buildings_Gravship.xml"));
        foreach(var name in new[]{"SmallThruster","LargeThruster"})
        {
            var node=xml.SelectSingleNode($"Defs/ThingDef[defName='{name}']")!;
            // Geometry only: don't register unrelated shader/fleck cross-references in the fixture.
            var props=new CompProperties_GravshipThruster {
                exclusionAreaSize=ParseHelper.FromString<IntVec3>(node.SelectSingleNode("comps/li[@Class='CompProperties_GravshipThruster']/exclusionAreaSize")!.InnerText),
                exclusionAreaOffset=ParseHelper.FromString<IntVec3>(node.SelectSingleNode("comps/li[@Class='CompProperties_GravshipThruster']/exclusionAreaOffset")!.InnerText)
            };
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName=name; def.size=ParseHelper.FromString<IntVec2>(node.SelectSingleNode("size")!.InnerText); def.comps=[props];
            yield return (def,props);
        }
    }
    static void Geometry()
    {
        var pos=new IntVec3(20,0,20);
        foreach(var (def,props) in Originals())
        foreach(var rot in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West})
        {
            List<IntVec3> cells=[]; props.GetExclusionZone(pos,rot,ref cells);
            int width=def.defName=="SmallThruster"?1:2, depth=def.defName=="SmallThruster"?5:7;
            Check.Equal(width*depth,cells.Count,def.defName+" native exhaust area "+rot);
            Check.Equal(cells.Count,cells.Distinct().Count(),"unique exclusion cells");
            var footprint=GenAdj.OccupiedRect(pos,rot,def.size);
            Check.Equal(false,cells.Any(footprint.Contains),"clearance outside building footprint");
            Check.Equal(true,cells.All(c=>(c-pos).x*rot.AsIntVec3.x+(c-pos).z*rot.AsIntVec3.z<0),"exhaust opposite thrust heading");
        }
    }
    static void Blocking()
    {
        var odyssey=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.NonPublic|BindingFlags.Static)!;
        var old=odyssey.GetValue(null);
        try
        {
            odyssey.SetValue(null,true);
            var map=new OdysseyOxygenTests.Pump().Oxygen.parent.Map;
            map.cellIndices=new CellIndices(map); map.thingGrid=new ThingGrid(map); map.terrainGrid=new TerrainGrid(map);
            foreach(var (def,props) in Originals())
            foreach(var rot in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West})
            {
                var pos=new IntVec3(20,0,20); List<IntVec3> cells=[]; props.GetExclusionZone(pos,rot,ref cells);
                var cell=cells[0]; var things=map.thingGrid.ThingsListAt(cell);
                bool Blocked(out Thing by,out bool floor) => CompGravshipThruster.IsBlocked(def,map,pos,rot,out by,out floor);
                Check.Equal(false,Blocked(out _,out _),"clear exhaust works "+def.defName+rot);
                var blockerDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); blockerDef.blockWind=true;
                var wall=new Building { def=blockerDef }; things.Add(wall);
                Check.Equal(true,Blocked(out var found,out _),"wind-blocking structure blocks exhaust"); Check.Same(wall,found,"reports actual blocker");
                things.Clear();
                var plant=(Plant)RuntimeHelpers.GetUninitializedObject(typeof(Plant)); plant.def=blockerDef; things.Add(plant);
                Check.Equal(false,Blocked(out _,out _),"plants explicitly excluded despite blockWind"); things.Clear();
                blockerDef.blockWind=false; things.Add(wall);
                Check.Equal(false,Blocked(out _,out _),"low non-blockWind object allowed"); things.Clear();
                // Skip only texture initialization; the native foundation/tag query stays real.
                var foundation=(TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef));
                foundation.defName="TestSubstructure"; foundation.tags=["Substructure"];
                map.terrainGrid.foundationGrid[map.cellIndices.CellToIndex(cell)]=foundation;
                Check.Equal(true,Blocked(out _,out var substructure),"foundation substructure blocks"); Check.Equal(true,substructure,"substructure reported separately");
                map.terrainGrid.foundationGrid[map.cellIndices.CellToIndex(cell)]=null!;
                Check.Equal(false,Blocked(out _,out _),"removing obstruction restores clearance");
            }
        }
        finally { odyssey.SetValue(null,old); }
    }
}
