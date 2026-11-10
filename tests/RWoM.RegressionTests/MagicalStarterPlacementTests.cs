using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class MagicalStarterPlacementTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.StartScenario.NativeSpawnFootprintOrdering", f =>
            MagicalStarterShipTests.WithDefinitions(() => Run(f)));
        yield return new("Odyssey.StartScenario.NativeConsoleInteractionAdmission", f =>
            MagicalStarterShipTests.WithDefinitions(() => ConsoleAdmission(f)));
    }

    static void Set(object target, string name, object value)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is FieldInfo field)
            { field.SetValue(target, value); return; }
        throw new MissingFieldException(name);
    }

    static void Run(RuntimeFixture fixture)
    {
        var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.NonPublic | BindingFlags.Static)!;
        var oldFlag = flag.GetValue(null);
        var oldGame = Current.Game;
        var coreXml = XDocument.Load(Path.Combine(fixture.Repository, "v1.7/Odyssey/Defs/Buildings_Gravcore.xml"))
            .Root!.Elements("ThingDef").Single(n => (string?)n.Element("defName") == "TM_MagicalGravEngine");
        float radius = float.Parse(coreXml.Element("comps")!.Elements("li")
            .Single(n => (string?)n.Attribute("Class") == "CompProperties_SubstructureFootprint").Element("radius")!.Value,
            System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            flag.SetValue(null, true);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            foreach (bool tank in new[] { false, true })
            foreach (var rotation in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
            foreach (bool lateCore in new[] { false, true })
            {
                var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
                map.info = new MapInfo { Size = new IntVec3(80, 1, 80) };
                map.cellIndices = new CellIndices(map);
                map.roofGrid = new RoofGrid(map);
                map.listerThings = new ListerThings(ListerThingsUse.Global);
                Set(Current.Game, "maps", new List<Map> { map });
                var sketch = new Sketch();
                sketch.Merge(SketchResolver_MagicalStarterShip.Build(tank));
                sketch.Rotate(rotation);
                var core = sketch.Things.Single(t => t.def.defName == "TM_MagicalGravEngine");
                if (lateCore) core.spawnOrder = Sketch.SpawnOrder_Thing;
                var accepted = new HashSet<IntVec3>();
                var footprint = new List<Thing>();
                var groups = (List<Thing>[])typeof(ListerThings)
                    .GetField("listsByGroup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map.listerThings)!;
                groups[(int)ThingRequestGroup.SubstructureFootprint] = footprint;
                int consoles = 0;
                // Exercise native Sketch.Spawn ordering/offset and native placement eligibility.
                // Headless callback substitutes only entity materialization and rendering: no
                // patched arrival, ordering, footprint lookup or placement worker.
                sketch.Spawn(map, new IntVec3(40, 0, 40), null, Sketch.SpawnPosType.OccupiedCenter,
                    canSpawnThing: (entity, at) =>
                    {
                        if (ReferenceEquals(entity, core))
                        {
                            var engine = new Building { def = core.def };
                            Set(engine, "positionInt", at);
                            Set(engine, "mapIndexOrState", (sbyte)0);
                            Set(engine, "comps", new List<ThingComp> { new CompSubstructureFootprint
                                { parent = engine, props = new CompProperties_SubstructureFootprint { radius = radius } } });
                            footprint.Add(engine);
                        }
                        else if (entity is SketchTerrain terrain)
                        {
                            bool inside = GravshipUtility.InsideFootprint(at, map);
                            if (!lateCore) Check.Equal(true, inside, "native floor spawn requires already-spawned core footprint");
                            if (inside)
                            {
                                Check.Equal(true, new PlaceWorker_InSubstructureFootprint()
                                    .AllowsPlacing(terrain.def, at, Rot4.North, map).Accepted, "native substructure worker accepts floor");
                                accepted.Add(at);
                            }
                        }
                        else if (entity is SketchThing thing && thing.def == ThingDefOf.PilotConsole)
                        {
                            var offset = at - thing.pos;
                            if (thing.OccupiedRect.All(c => accepted.Contains(c + offset))) consoles++;
                        }
                        return false;
                    });
                Check.Equal(lateCore ? 0 : 95, accepted.Count, "all floors admitted before fixtures; late-core negative control reproduces missing platform");
                Check.Equal(lateCore ? 0 : 1, consoles, "console receives complete platform when spawned");
            }
        }
        finally { Current.Game = oldGame; flag.SetValue(null, oldFlag); }
    }
}
