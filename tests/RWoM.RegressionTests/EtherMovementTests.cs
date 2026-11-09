using System.Reflection;
using LudeonTK;
using RimWorld;
using TorannMagic.Odyssey;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class EtherMovementTests
{
    internal sealed class Offsets(NativeArray<ushort> values) : PathRequest.IPathGridCustomizer
    { public NativeArray<ushort> GetOffsetGrid() => values; }
    internal sealed class Hooks : IDisposable
    {
        readonly object harmony;
        readonly string id = "rwom.ether.movement." + Guid.NewGuid();
        internal Hooks()
        {
            var type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
            harmony = Activator.CreateInstance(type, id)!;
            foreach (var patch in new[] { typeof(EtherPawnContextPatch), typeof(EtherTraverseContextPatch), typeof(EtherTerrainChangedPatch), typeof(EtherStartPathPatch), typeof(EtherReachabilityPatch), typeof(EtherRequestPatch), typeof(EtherGridJobPatch), typeof(EtherPrepareConnectivityPatch), typeof(EtherConnectivityPatch) })
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EtherMovement.NativeMapSpaceCostsAndWalls", f => TeleportSpiritTests.EtherMovementMap(f, false));
        yield return new("EtherMovement.NativePathJobsAcrossVoidAndTerrainChanges", f => TeleportSpiritTests.EtherMovementMap(f, true));
    }
}

internal static partial class TeleportSpiritTests
{
    static bool EtherEmptyRegion(ref Region? __result) { __result = null; return false; }
    static bool EtherCardinalTicks(ref float __result) { __result = 13; return false; }
    static bool EtherDiagonalTicks(ref float __result) { __result = 18; return false; }
    public static void EtherMovementMap(RuntimeFixture fixture, bool nativeJobs) => CoreReturnWorld(fixture, w =>
    {
        _ = CoreReturnCore(w);
        var map = w.Map;
        map.terrainGrid = new TerrainGrid(map); map.snowGrid = new SnowGrid(map);
        var installed = typeof(ModLister).GetField("odysseyInstalled", Flags)!; var wasInstalled = installed.GetValue(null);
        try { installed.SetValue(null, true); map.sandGrid = new SandGrid(map); }
        finally { installed.SetValue(null, wasInstalled); }
        Set(map.pathing, "map", map);
        map.regionGrid = new RegionGrid(map);
        map.reachability = new Reachability(map);
        var space = Blank<TerrainDef>(); space.defName = "Space"; space.exposesToVacuum = true; space.passability = Traversability.Impassable; space.pathCost = 300;
        var floor = Blank<TerrainDef>(); floor.defName = "EtherTestFloor"; floor.passability = Traversability.Standable;
        Array.Fill(map.terrainGrid.topGrid, space);
        var start = new IntVec3(3, 0, 10); var end = new IntVec3(16, 0, 10);
        map.terrainGrid.topGrid[map.cellIndices[start]] = floor; map.terrainGrid.topGrid[map.cellIndices[end]] = floor;
        var component = new MapComponent_EtherMovement(map);
        Set(map, "components", new List<MapComponent> { component });
        var pawn = new Pawn_EtherServant { def = w.Caster.def, kindDef = w.Caster.kindDef, health = w.Caster.health, thingIDNumber = 601, mindState = w.Caster.mindState, jobs = new Pawn_JobTracker(w.Caster), drafter = w.Caster.drafter, guest = w.Caster.guest };
        Set(pawn, "positionInt", start); Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "factionInt", w.Player);
        using var patches = new Patches();
        using var hooks = new EtherMovementTests.Hooks();
        // Empty regions match exposed Space. Do not replace movement, cost or
        // reachability; only avoid building the unrelated room graph headlessly.
        patches.Prefix(typeof(RegionGrid).GetMethod("GetValidRegionAt")!, nameof(EtherEmptyRegion));
        patches.Prefix(typeof(Pawn).GetProperty("TicksPerMoveCardinal")!.GetMethod!, nameof(EtherCardinalTicks));
        patches.Prefix(typeof(Pawn).GetProperty("TicksPerMoveDiagonal")!.GetMethod!, nameof(EtherDiagonalTicks));
        try
        {
            Check.Equal(false, component.Initialized, "no grid allocation until a servant needs it");
            var context = component.Context;
            Check.Same(context, pawn.GetPathContext(map.pathing), "patched native pawn context selects private grid");
            Check.Same(map.pathing.Normal, w.Caster.GetPathContext(map.pathing), "ordinary pawn retains native context");
            var cellCost = typeof(Pawn_PathFollower).GetMethod("CostToMoveIntoCell", Flags, [typeof(Pawn), typeof(IntVec3)])!;
            Check.Equal(13f, (float)cellCost.Invoke(null, [pawn, new IntVec3(4, 0, 10)])!, "real cardinal entry has no 300 tick Space penalty");
            Check.Equal(18f, (float)cellCost.Invoke(null, [pawn, new IntVec3(4, 0, 11)])!, "real diagonal entry keeps diagonal move cost");
            Check.Equal(0, context.pathGrid.CalculatedCostAt(new IntVec3(8, 0, 10), false, start), "no open-Space movement penalty");
            Check.Equal(10000, map.pathing.Normal.pathGrid.CalculatedCostAt(new IntVec3(8, 0, 10), false, start), "ordinary pawn Space remains impassable");
            Check.Equal(300, space.pathCost, "shared terrain cost unchanged");
            Check.Equal(Traversability.Impassable, space.passability, "shared terrain passability unchanged");
            Check.Equal(true, EtherMovement.CanReach(pawn, start, end, PathEndMode.OnCell, TraverseParms.For(pawn)), "servant crosses open void between platforms");
            Check.Equal(true, map.reachability.CanReach(new IntVec3(4, 0, 10), end, PathEndMode.OnCell, TraverseParms.For(pawn)), "patched native reachability accepts regionless Space start without null-region errors");
            var marker = component.Wrap(null!);
            Check.Same(marker, component.Wrap(null!), "same terrain version reuses isolated request cache identity");
            using (var offsets = new NativeArray<ushort>(map.cellIndices.NumGridCells, Allocator.Persistent))
                Check.Equal(true, component.Wrap(new EtherMovementTests.Offsets(offsets)).GetOffsetGrid().Equals(offsets), "external requester offset grid survives wrapping");
            EtherStartPathPatch.Prefix(pawn, out var before);
            EtherStartPathPatch.Prefix(w.Caster, out var nested);
            EtherStartPathPatch.Finalizer(nested);
            Check.Same(pawn, typeof(EtherMovement).GetField("startPathPawn", Flags)!.GetValue(null), "nested start-path finalizer restores outer servant context");
            EtherStartPathPatch.Finalizer(before);
            Check.Equal<object?>(null, typeof(EtherMovement).GetField("startPathPawn", Flags)!.GetValue(null), "outer finalizer releases pawn context");
            typeof(EtherMovement).GetField("preparedJob", Flags)!.SetValue(null, component);
            EtherConnectivityPatch.Finalizer();
            Check.Equal<object?>(null, typeof(EtherMovement).GetField("preparedJob", Flags)!.GetValue(null), "connectivity finalizer clears exceptional scheduling state");
            var walls = new List<Thing>();
            for (int z = 0; z < map.Size.z; z++)
            {
                var d = Blank<ThingDef>(); d.defName = "EtherTestWall"; d.size = IntVec2.One; d.passability = Traversability.Impassable;
                var wall = new Thing { def = d };
                Set(wall, "positionInt", new IntVec3(10, 0, z)); Set(wall, "mapIndexOrState", (sbyte)0);
                map.thingGrid.Register(wall); walls.Add(wall); component.Refresh(wall.Position);
            }
            Check.Equal(false, ReferenceEquals(marker, component.Wrap(null!)), "terrain/obstacle update invalidates servant grid cache identity");
            Check.Equal(false, EtherMovement.CanReach(pawn, start, end, PathEndMode.OnCell, TraverseParms.For(pawn)), "continuous wall blocks space traversal");
            Check.Equal(10000, context.pathGrid.CalculatedCostAt(new IntVec3(10, 0, 10), false, start), "wall remains impassable even over Space");
            if (nativeJobs) Check.Equal(0, EtherNativePath(map, pawn, component, start, end), "native grid+path jobs cannot cross solid wall");
            var gap = walls[10]; map.thingGrid.Deregister(gap); component.Refresh(gap.Position);
            Check.Equal(true, EtherMovement.CanReach(pawn, start, end, PathEndMode.OnCell, TraverseParms.For(pawn)), "opening one gap restores reachability");
            if (nativeJobs) Check.Equal(true, EtherNativePath(map, pawn, component, start, end) > 0, "real native path job crosses open Space gap");
            var lava = Blank<TerrainDef>(); lava.defName = "Lava"; lava.exposesToVacuum = true; lava.passability = Traversability.Impassable; lava.pathCost = 300;
            map.terrainGrid.topGrid[map.cellIndices[gap.Position]] = lava; component.Refresh(gap.Position);
            Check.Equal(false, EtherMovement.CanReach(pawn, start, end, PathEndMode.OnCell, TraverseParms.For(pawn)), "vacuum-exposed lava is not treated as Space");
            map.terrainGrid.topGrid[map.cellIndices[gap.Position]] = floor; component.Refresh(gap.Position);
            Check.Equal(true, EtherMovement.CanReach(pawn, start, end, PathEndMode.OnCell, TraverseParms.For(pawn)), "terrain restoration is reflected immediately");
            foreach (var c in new[] { new IntVec3(1, 0, 0), new IntVec3(0, 0, 1) })
            {
                var d = Blank<ThingDef>(); d.defName = "CornerWall"; d.size = IntVec2.One; d.passability = Traversability.Impassable;
                var wall = new Thing { def = d }; Set(wall, "positionInt", c); Set(wall, "mapIndexOrState", (sbyte)0);
                map.thingGrid.Register(wall); component.Refresh(c);
            }
            Check.Equal(false, EtherMovement.CanReach(pawn, IntVec3.Zero, new IntVec3(1, 0, 1), PathEndMode.OnCell, TraverseParms.For(pawn)), "no diagonal shortcut between touching walls");
            if (nativeJobs) Check.Equal(0, EtherNativePath(map, pawn, component, IntVec3.Zero, new IntVec3(1, 0, 1)), "native path diagonal check also blocks corner shortcut");
        }
        finally { component.Dispose(); component.Dispose(); map.sandGrid.Dispose(); map.snowGrid.Dispose(); }
        Check.Equal(false, component.Initialized, "map disposal releases private grids");
    });

    static int EtherNativePath(Map map, Pawn pawn, MapComponent_EtherMovement component, IntVec3 start, IntVec3 end)
    {
        var finder = Blank<PathFinder>(); Set(finder, "map", map);
        var request = finder.CreateRequest(start, end, null, TraverseParms.For(pawn), null, PathEndMode.OnCell, pawn);
        Check.NotNull(request.customizer, "patched native request carries isolated cache identity");
        var ordinaryRequest = finder.CreateRequest(start, end, null, TraverseParms.For(TraverseMode.PassDoors), null);
        Check.Equal<PathRequest.IPathGridCustomizer?>(null, ordinaryRequest.customizer, "ordinary request cache identity is unchanged");
        int n = map.cellIndices.NumGridCells;
        using var costs = new NativeArray<int>(n, Allocator.Persistent);
        using var bits = new NativeBitArray(n, Allocator.Persistent);
        using var emptyBits = new NativeBitArray(0, Allocator.Persistent);
        using var shorts = new NativeArray<ushort>(n, Allocator.Persistent);
        using var emptyShorts = new NativeArray<ushort>(0, Allocator.Persistent);
        using var emptyBytes = new NativeArray<byte>(0, Allocator.Persistent);
        var grid = new PathGridJob
        {
            indicies = map.cellIndices, grid = costs, pathGridDirect = map.pathing.Normal.pathGrid.Grid_Unsafe.AsReadOnly(),
            tuning = PathFinderCostTuning.DefaultTuning,
            traverseParams = PathFinder.UnmanagedGridTraverseParams.For(TraverseParms.For(TraverseMode.ByPawn)),
            building = bits.AsReadOnly(), buildingDestroyable = bits.AsReadOnly(), player = bits.AsReadOnly(),
            water = bits.AsReadOnly(), fence = bits.AsReadOnly(), fogged = bits.AsReadOnly(), darknessDanger = bits.AsReadOnly(), persistentDanger = bits.AsReadOnly(),
            factionCosts = shorts.AsReadOnly(), buildingHitPoints = shorts.AsReadOnly(), perceptualCost = shorts.AsReadOnly(),
            lordGrid = emptyBits.AsReadOnly(), allowedGrid = emptyBits.AsReadOnly(), custom = emptyShorts.AsReadOnly(), avoidGrid = emptyBytes.AsReadOnly()
        };
        EtherGridJobPatch.Postfix(request, ref grid);
        for (int i = 0; i < n; i++) grid.Execute(i);
        using var calc = new NativeArray<PathFinderJob.CalcNode>(n, Allocator.Persistent);
        using var frontier = new NativePriorityQueue<int, float, FloatMinComparer>(n, new FloatMinComparer(), Allocator.Persistent);
        using var path = new NativeList<IntVec3>(Allocator.Persistent);
        using var result = new NativeReference<PathFinderJob.ResultData>(Allocator.Persistent);
        using var blocked = new NativeArray<bool>(n, Allocator.Persistent);
        using var excluded = new NativeList<int>(Allocator.Persistent);
        var job = new PathFinderJob
        {
            indices = map.cellIndices, heuristicStrength = 1f, moveTicksCardinal = 13, moveTicksDiagonal = 18,
            start = start, destCell = end, destRect = CellRect.SingleCell(end), traverseParams = grid.traverseParams,
            calcGrid = calc, frontier = frontier, path = path, result = result, grid = costs.AsReadOnly(),
            providerCost = shorts.AsReadOnly(), blocked = blocked.AsReadOnly(), excludedRectIndices = excluded,
            fences = bits.AsReadOnly(), buildings = bits.AsReadOnly()
        };
        EtherPrepareConnectivityPatch.Postfix(request);
        EtherConnectivityPatch.Postfix(map, ref job);
        job.Execute();
        return path.Length;
    }
}
