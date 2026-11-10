using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.SketchGen;
using Verse;

namespace TorannMagic.Odyssey
{
    // Native arrival owns placement, starting pawns/items, home area and activation.
    public class ScenPart_MagicalShipArrival : ScenPart_PlayerPawnsArriveMethod
    {
        public bool includeReservoir;
        [ThreadStatic] internal static ScenPart_MagicalShipArrival active;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref includeReservoir, "includeReservoir");
        }
        public override void GenerateIntoMap(Map map)
        {
            var previous = active;
            var existing = new HashSet<Thing>(map.listerThings.AllThings);
            active = this;
            try
            {
                base.GenerateIntoMap(map);
                foreach (Thing thing in map.listerThings.AllThings)
                    if (!existing.Contains(thing) && (thing.def.defName == "TM_MagicalGravEngine" || thing.def.defName == "TM_MagofuelReservoir"))
                    {
                        var fuel = thing.TryGetComp<CompRefuelable>();
                        if (fuel != null && fuel.Fuel > 100f) fuel.ConsumeFuel(fuel.Fuel - 100f);
                    }
            }
            finally { active = previous; }
        }
    }

    [HarmonyPatch(typeof(SketchGen), nameof(SketchGen.Generate))]
    public static class MagicalStarterShipSketchPatch
    {
        public static void Prefix(ref SketchResolverDef root, ref SketchResolveParams parms)
        {
            if (ScenPart_MagicalShipArrival.active == null || root != SketchResolverDefOf.Gravship) return;
            root = DefDatabase<SketchResolverDef>.GetNamed("TM_MagicalStarterShip");
            parms.SetCustom("TM_StarterReservoir", ScenPart_MagicalShipArrival.active.includeReservoir);
        }
    }

    public class SketchResolver_MagicalStarterShip : SketchResolver
    {
        protected override bool CanResolveInt(SketchResolveParams parms) => parms.sketch != null;
        protected override void ResolveInt(SketchResolveParams parms)
        {
            parms.sketch.Merge(Build(parms.GetCustom<bool>("TM_StarterReservoir")));
        }
        public static Sketch Build(bool includeReservoir)
        {
            var sketch = new Sketch();
            // Bow at east; two stern pods exhaust west, outside the hull.
            for (int z = 0; z <= 8; z++)
                for (int x = 0; x <= 10; x++)
                {
                    if ((x == 0 || x == 10) && (z == 0 || z == 8)) continue;
                    var cell = new IntVec3(x, 0, z);
                    sketch.AddTerrain(TerrainDefOf.Substructure, cell);
                    bool pod = x == 0 && (z == 2 || z == 6);
                    if (!pod && (x == 0 || x == 10 || z == 0 || z == 8))
                        sketch.AddThing(x == 5 && (z == 0 || z == 8) ? ThingDefOf.Door : ThingDefOf.GravshipHull, cell, Rot4.North, ThingDefOf.Steel);
                }
            // Native substructure placement requires a spawned engine footprint first.
            // Match the vanilla gravship resolver's engine-before-terrain ordering.
            sketch.AddThing(DefDatabase<ThingDef>.GetNamed("TM_MagicalGravEngine"),
                new IntVec3(7, 0, 4), Rot4.West, spawnOrder: Sketch.SpawnOrder_BeforeTerrain);
            Add("TM_SmallMagicalThruster", 0, 2, Rot4.East);
            Add("TM_SmallMagicalThruster", 0, 6, Rot4.East);
            Add("TM_ManaBeacon", 3, 2, Rot4.North);
            Add("TM_ManaBeacon", 5, 6, Rot4.North);
            sketch.AddThing(ThingDefOf.PilotConsole, new IntVec3(4, 0, 4), Rot4.East);
            foreach (int z in new[] { 1, 3, 5, 7 })
                sketch.AddThing(ThingDefOf.Shelf, new IntVec3(2, 0, z), Rot4.North, ThingDefOf.Steel);
            sketch.AddThing(ThingDefOf.Shelf, new IntVec3(5, 0, 1), Rot4.North, ThingDefOf.Steel);
            sketch.AddThing(ThingDefOf.Shelf, new IntVec3(7, 0, 7), Rot4.North, ThingDefOf.Steel);
            // Core faces west so its charging cell is east, clear of the pilot console.
            // Leave the tank's western charging cell open too.
            if (includeReservoir) Add("TM_MagofuelReservoir", 8, 2, Rot4.East);
            return sketch;
            void Add(string name, int x, int z, Rot4 rot) => sketch.AddThing(DefDatabase<ThingDef>.GetNamed(name), new IntVec3(x, 0, z), rot);
        }
    }

    [HarmonyPatch(typeof(SketchTerrain), nameof(SketchTerrain.Spawn))]
    internal static class MagicalShipFoundationPlacement
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => instructions.MethodReplacer(AccessTools.Method(typeof(TerrainGrid),nameof(TerrainGrid.SetFoundation)),
                AccessTools.Method(typeof(MagicalShipFoundationPlacement),nameof(Place)));

        private static void Place(TerrainGrid grid, IntVec3 cell, TerrainDef foundation)
        {
            // RimWorld 1.7 rejects foundations on a removable floor (e.g. a
            // generated ruin). Only clear it when the scenario actually places
            // a hull cell after the native sketch's collision checks.
            if (ScenPart_MagicalShipArrival.active != null && foundation == TerrainDefOf.Substructure &&
                grid.UnderTerrainAt(cell) != null)
            {
                if (grid.TempTerrainAt(cell) != null) grid.RemoveTopLayer(cell, false);
                grid.RemoveTopLayer(cell, false);
            }
            grid.SetFoundation(cell, foundation);
        }
    }
}
