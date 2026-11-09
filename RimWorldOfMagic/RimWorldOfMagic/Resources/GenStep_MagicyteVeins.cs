using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.Resources
{
    public class GenStep_MagicyteVeins : GenStep
    {
        public bool richAsteroid;
        public override int SeedPart => 91842751;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (MapGenerator.mapBeingGenerated != map) return;
            if (richAsteroid)
            {
                if (map.ParentHolder is SpaceMapParent parent && parent.def?.defName == "TM_MagicyteAsteroid")
                    GenerateVeins(map, Rand.RangeInclusive(3, 5));
            }
            else if (map.Tile.Valid && map.Tile.Layer.IsRootSurface)
                GenerateVeins(map, Rand.RangeInclusive(0, 2));
        }

        internal static bool EligibleRock(ThingDef def)
        {
            return def?.building != null && def.building.isNaturalRock && !def.building.isResourceRock;
        }

        internal static void GenerateVeins(Map map, int count)
        {
            var ore = DefDatabase<ThingDef>.GetNamed("TM_MineableMagicyte");
            // This is a map-generation step, never a save migration or periodic resource refill.
            if (MapGenerator.mapBeingGenerated != map || map.listerThings.ThingsOfDef(ore).Count != 0) return;
            var candidates = map.AllCells.Where(c => EligibleRock(c.GetEdifice(map)?.def)).ToList();
            foreach (var vein in PlanVeins(candidates, count))
            foreach (var cell in vein)
            {
                var rock = cell.GetEdifice(map);
                if (!EligibleRock(rock?.def)) continue;
                rock.Destroy(DestroyMode.Vanish);
                GenSpawn.Spawn(ore, cell, map);
            }
        }

        // Input order and Rand use the map generator's seeded stream; hash sets are only membership tests.
        internal static List<List<IntVec3>> PlanVeins(List<IntVec3> cells, int count)
        {
            var result = new List<List<IntVec3>>();
            var available = new HashSet<IntVec3>(cells);
            var starts = cells.ToList();
            starts.Shuffle();
            foreach (var start in starts)
            {
                if (result.Count >= count) break;
                if (!available.Contains(start)) continue;
                int target = Rand.RangeInclusive(3, 8);
                var vein = new List<IntVec3> { start };
                var chosen = new HashSet<IntVec3> { start };
                while (vein.Count < target)
                {
                    var frontier = new List<IntVec3>();
                    foreach (var cell in vein)
                    foreach (var offset in GenAdj.CardinalDirections)
                    {
                        var next = cell + offset;
                        if (available.Contains(next) && !chosen.Contains(next) && !frontier.Contains(next)) frontier.Add(next);
                    }
                    if (frontier.Count == 0) break;
                    var picked = frontier.RandomElement();
                    chosen.Add(picked);
                    vein.Add(picked);
                }
                if (vein.Count < 3) continue;
                result.Add(vein);
                foreach (var cell in vein)
                {
                    available.Remove(cell);
                    foreach (var offset in GenAdj.AdjacentCells) available.Remove(cell + offset);
                }
            }
            return result;
        }
    }

    // Used exclusively by the optional rich asteroid definition. Ordinary asteroid generation is untouched.
    public class GenStep_MagicyteAsteroid : GenStep_BasicAsteroid
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            if (MapGenerator.mapBeingGenerated != map || !(map.ParentHolder is SpaceMapParent parent)) return;
            var resource = parent.preciousResource;
            try
            {
                // The native pass selects one ordinary resource from mineableCounts. Do not let the
                // location's magicyte identity turn that uncapped native scatter pass into extra veins.
                parent.preciousResource = null;
                base.Generate(map, parms);
            }
            finally { parent.preciousResource = resource; }
            // The separate order-1800 pass adds ore after roads, ruins and other clearing steps.
        }
    }
}
