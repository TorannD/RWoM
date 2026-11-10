using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class MagicalStarterPlacementTests
{
    static void ConsoleAdmission(RuntimeFixture fixture)
    {
        string game = Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var nodes = Directory.GetFiles(Path.Combine(game, "Data/Core/Defs"), "*.xml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(game, "Data/Odyssey/Defs/ThingDefs_Buildings"), "*.xml"))
            .Concat(Directory.GetFiles(Path.Combine(fixture.Repository, "v1.7/Odyssey/Defs"), "Buildings*.xml"))
            .Append(Path.Combine(fixture.Repository, "v1.7/Defs/ThingDefs/TM_BuildingBase.xml"))
            .SelectMany(p => XDocument.Load(p).Root!.Elements("ThingDef")).ToArray();
        string? Value(XElement node, string key)
        {
            if (node.Element(key) is XElement value) return value.Value;
            var parent = (string?)node.Attribute("ParentName");
            return parent == null ? null : Value(nodes.Single(n => (string?)n.Attribute("Name") == parent), key);
        }
        var oldGame = Current.Game;
        var oldLanguage = LanguageDatabase.activeLanguage;
        var oldDefault = LanguageDatabase.defaultLanguage;
        try
        {
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement>();
            foreach (string key in new[] { "WouldBlockInteractionSpot", "InteractionSpotBlocked" })
                language.keyedReplacements[key] = new LoadedLanguage.KeyedReplacement { key = key, value = key };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            // Load the actual inherited XML values BEFORE sketch assembly/rotation. In
            // particular, BuildingBase defaults must not silently become rotatable=true.
            foreach (var def in SketchResolver_MagicalStarterShip.Build(true).Things.Select(t => t.def).Distinct())
            {
                var node = nodes.Single(n => (string?)n.Element("defName") == def.defName);
                def.label = def.defName;
                def.size = IntVec2.FromString(Value(node, "size") ?? "(1,1)");
                def.rotatable = bool.Parse(Value(node, "rotatable") ?? "true");
                def.passability = Enum.Parse<Traversability>(Value(node, "passability") ?? "Standable");
                def.hasInteractionCell = bool.Parse(Value(node, "hasInteractionCell") ?? "false");
                if (def.hasInteractionCell) def.interactionCellOffset = IntVec3.FromString(Value(node, "interactionCellOffset")!);
            }
            foreach (bool tank in new[] { false, true })
            foreach (var rotation in new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West })
            {
                var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
                map.info = new MapInfo { Size = new IntVec3(80, 1, 80) };
                map.cellIndices = new CellIndices(map); map.thingGrid = new ThingGrid(map);
                Set(Current.Game, "maps", new List<Map> { map });
                var sketch = new Sketch(); sketch.Merge(SketchResolver_MagicalStarterShip.Build(tank));
                Check.Equal(Rot4.West, sketch.Things.Single(t => t.def.defName == "TM_MagicalGravEngine").rot,
                    "starter core turns ninety degrees with charging cell east of core");
                sketch.Rotate(rotation);
                var offset = new IntVec3(40, 0, 40) - sketch.OccupiedCenter;
                var spawned = new List<Building>();
                foreach (var entry in sketch.Things.OrderBy(t => t.SpawnOrder))
                {
                    var thing = new Building { def = entry.def, Position = entry.pos + offset, Rotation = entry.rot };
                    if (entry.def.defName == "TM_MagicalGravEngine")
                        Check.Equal(entry.rot, thing.Rotation, "native spawned core retains rotated interaction direction");
                    if (entry.def == ThingDefOf.PilotConsole)
                    {
                        Check.Equal(true, GenConstruct.NotBlockingAnyInteractionCells(entry.def, thing.Position, thing.Rotation, map).Accepted,
                            "native console admission must not block already-spawned core interaction cell: " + rotation);
                        Check.Equal(true, GenConstruct.InteractionCellStandable(entry.def, thing.Position, thing.Rotation, map).Accepted,
                            "native console operator cell must be standable: " + rotation);
                    }
                    Set(thing, "mapIndexOrState", (sbyte)0);
                    map.thingGrid.Register(thing); spawned.Add(thing);
                }
                Check.Equal(1, spawned.Count(t => t.def == ThingDefOf.PilotConsole), "exact vanilla console retained");
                foreach (var thing in spawned.Where(t => t.def.hasInteractionCell))
                {
                    Check.Equal(true, GenConstruct.InteractionCellStandable(thing.def, thing.Position, thing.Rotation, map, thing).Accepted,
                        "native interaction cell remains usable after all furniture: " + thing.def.defName + " " + rotation);
                }
            }
        }
        finally
        {
            Current.Game = oldGame; LanguageDatabase.activeLanguage = oldLanguage; LanguageDatabase.defaultLanguage = oldDefault;
        }
    }
}
