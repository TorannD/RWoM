using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Unity.Collections;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    // The ordinary terrain and every other pawn's grids remain untouched. The
    // extra arrays exist only on maps where a servant has requested pathing.
    public sealed class MapComponent_EtherMovement : MapComponent, IDisposable
    {
        private PathingContext context;
        private NativeArray<CellConnection> connections;
        private NativeArray<ushort> emptyOffsets;
        private object version = new object();
        private readonly Dictionary<PathRequest.IPathGridCustomizer, Marker> wrappers = new Dictionary<PathRequest.IPathGridCustomizer, Marker>();
        private Marker plainMarker;
        public MapComponent_EtherMovement(Map map) : base(map) { }
        public bool Initialized => context != null;
        public PathingContext Context
        {
            get
            {
                if (context == null)
                {
                    var grid = new EtherPathGrid(map, new PathGridDef { defName = "TM_EtherPrivatePathGrid", fencePassable = true });
                    context = new PathingContext(map, grid);
                    connections = new NativeArray<CellConnection>(map.cellIndices.NumGridCells, Allocator.Persistent);
                    emptyOffsets = new NativeArray<ushort>(0, Allocator.Persistent);
                    var values = grid.Grid_Unsafe;
                    foreach (var c in map.AllCells)
                    {
                        values[map.cellIndices[c]] = grid.CalculatedCostAt(c, true, IntVec3.Invalid);
                        CellConnection flags = CellConnection.Self;
                        for (int i = 0; i < 8; i++)
                            if ((c + CellConnectionExtensions.OffsetFromBitIndex[i]).InBounds(map)) flags |= CellConnectionExtensions.FlagFromBitIndex[i];
                        connections[map.cellIndices[c]] = flags;
                    }
                }
                return context;
            }
        }
        public NativeArray<CellConnection>.ReadOnly Connections { get { _ = Context; return connections.AsReadOnly(); } }
        public void Refresh(IntVec3 cell)
        {
            if (context == null || !cell.InBounds(map)) return;
            // Complete readers before writing our native array. Native pathfinder
            // invalidates its own grids on the same terrain/building notification.
            EtherMovement.CompleteJobs(map);
            var values = context.pathGrid.Grid_Unsafe;
            values[map.cellIndices[cell]] = context.pathGrid.CalculatedCostAt(cell, true, IntVec3.Invalid);
            version = new object(); wrappers.Clear(); plainMarker = null;
        }
        public PathRequest.IPathGridCustomizer Wrap(PathRequest.IPathGridCustomizer original)
        {
            _ = Context;
            if (original is Marker marker && marker.owner == this && marker.version == version) return marker;
            if (original is Marker old && old.owner == this) original = old.original;
            if (original == null) return plainMarker ?? (plainMarker = new Marker(this, null, version));
            if (!wrappers.TryGetValue(original, out var wrapped)) wrappers[original] = wrapped = new Marker(this, original, version);
            return wrapped;
        }
        private sealed class Marker : PathRequest.IPathGridCustomizer
        {
            internal readonly MapComponent_EtherMovement owner;
            internal readonly PathRequest.IPathGridCustomizer original;
            internal readonly object version;
            internal Marker(MapComponent_EtherMovement owner, PathRequest.IPathGridCustomizer original, object version)
            { this.owner = owner; this.original = original; this.version = version; }
            public NativeArray<ushort> GetOffsetGrid() => original?.GetOffsetGrid() ?? owner.emptyOffsets;
        }
        public override void MapRemoved() => Dispose();
        public void Dispose()
        {
            if (context == null) return;
            if (EtherMovement.preparedJob == this) EtherMovement.preparedJob = null;
            EtherMovement.CompleteJobs(map);
            context.Dispose(); context = null;
            connections.Dispose(); emptyOffsets.Dispose(); wrappers.Clear(); plainMarker = null;
        }
    }

    public sealed class EtherPathGrid : PathGrid
    {
        public EtherPathGrid(Map map, PathGridDef def) : base(map, def) { }
        public static bool IsOpenSpace(TerrainDef terrain) => terrain != null && terrain.defName == "Space" && terrain.exposesToVacuum;
        public override int CalculatedCostAt(IntVec3 cell, bool perceivedStatic, IntVec3 previous, int? baseCostOverride = null)
        {
            if (!IsOpenSpace(map.terrainGrid.TerrainAt(cell))) return base.CalculatedCostAt(cell, perceivedStatic, previous, baseCostOverride);
            // Space supplies no walking penalty. Things, doors and adjacent fire
            // still supply native costs; impassable buildings remain impassable.
            int cost = 0;
            bool repeated = false;
            if (previous.IsValid)
                foreach (var thing in map.thingGrid.ThingsListAt(previous))
                    if (thing.def.pathCost >= 25 && thing.def.pathCostIgnoreRepeat) { repeated = true; break; }
            foreach (var thing in map.thingGrid.ThingsListAt(cell))
            {
                if (thing.def.passability == Traversability.Impassable) return ImpassableCost;
                if (!(repeated && thing.def.pathCost >= 25 && thing.def.pathCostIgnoreRepeat)) cost = Math.Max(cost, thing.def.pathCost);
            }
            if (previous.IsValid && cell.GetEdifice(map) is Building_Door door && previous.GetEdifice(map) is Building_Door priorDoor
                && !door.FreePassage && !priorDoor.FreePassage) cost += 45;
            if (perceivedStatic)
                foreach (var offset in GenAdj.AdjacentCellsAndInside)
                {
                    var nearby = cell + offset;
                    if (!nearby.InBounds(map)) continue;
                    foreach (var thing in map.thingGrid.ThingsListAt(nearby))
                        if (thing is Fire fire && fire.parent == null) { cost += offset.x != 0 || offset.z != 0 ? 150 : 1000; break; }
                }
            return cost;
        }
    }

    public static class EtherMovement
    {
        [ThreadStatic] internal static Pawn startPathPawn;
        [ThreadStatic] internal static MapComponent_EtherMovement preparedJob;
        private static readonly MethodInfo completeJobs = AccessTools.Method(typeof(PathFinder), "ForceCompleteScheduledJobs");
        private static readonly FieldInfo finderDisposed = AccessTools.Field(typeof(PathFinder), "disposed");
        internal static void CompleteJobs(Map map) { if (map.pathFinder != null && !(bool)finderDisposed.GetValue(map.pathFinder)) completeJobs.Invoke(map.pathFinder, null); }
        internal static MapComponent_EtherMovement Component(Pawn pawn) => pawn is Pawn_EtherServant && pawn.Spawned ? pawn.Map.GetComponent<MapComponent_EtherMovement>() : null;
        public static bool CanReach(Pawn pawn, IntVec3 start, LocalTargetInfo destination, PathEndMode endMode, TraverseParms parms)
        {
            var component = Component(pawn);
            if (component == null || !start.InBounds(pawn.Map) || !destination.IsValid || !destination.Cell.InBounds(pawn.Map)
                || (destination.HasThing && destination.Thing.MapHeld != pawn.Map)) return false;
            var map = pawn.Map;
            destination = (LocalTargetInfo)GenPath.ResolvePathMode(pawn, destination.ToTargetInfo(map), ref endMode);
            var grid = component.Context.pathGrid;
            var seen = new HashSet<IntVec3>();
            var queue = new Queue<IntVec3>();
            queue.Enqueue(start); seen.Add(start);
            while (queue.Count != 0)
            {
                var cell = queue.Dequeue();
                if (ReachabilityImmediate.CanReachImmediate(cell, destination, map, endMode, pawn)) return true;
                for (int i = 0; i < 8; i++)
                {
                    var offset = CellConnectionExtensions.OffsetFromBitIndex[i];
                    var next = cell + offset;
                    if (seen.Contains(next) || !Allowed(next)) continue;
                    if (offset.x != 0 && offset.z != 0 && (PathUtility.BlocksDiagonalMovement(next.x, cell.z, component.Context, parms.canBashFences)
                        || PathUtility.BlocksDiagonalMovement(cell.x, next.z, component.Context, parms.canBashFences))) continue;
                    seen.Add(next); queue.Enqueue(next);
                }
            }
            return false;
            bool Allowed(IntVec3 c)
            {
                if (!c.InBounds(map) || !grid.Walkable(c)) return false;
                var door = c.GetDoor(map);
                if (door != null && parms.mode != TraverseMode.PassDoors)
                {
                    if (door.IsForbiddenToPass(pawn)) return false;
                    if (door.BlocksPawn(pawn) && !door.CanPhysicallyPass(pawn)) return false;
                    if (parms.mode == TraverseMode.NoPassClosedDoors || parms.mode == TraverseMode.NoPassClosedDoorsOrWater)
                        if (!door.FreePassage) return false;
                }
                if (parms.mode == TraverseMode.NoPassClosedDoorsOrWater && c.GetTerrain(map).IsWater) return false;
                var region = c.GetRegion(map);
                return region == null || region.Allows(parms, false);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetPathContext))]
    public static class EtherPawnContextPatch
    {
        public static bool Prefix(Pawn __instance, ref PathingContext __result)
        { var c = EtherMovement.Component(__instance); if (c == null) return true; __result = c.Context; return false; }
    }
    [HarmonyPatch(typeof(Pathing), nameof(Pathing.For), new[] { typeof(TraverseParms) })]
    public static class EtherTraverseContextPatch
    {
        public static bool Prefix(TraverseParms parms, Map ___map, ref PathingContext __result)
        { var pawn = parms.pawn ?? EtherMovement.startPathPawn; var c = EtherMovement.Component(pawn); if (c == null || pawn.Map != ___map) return true; __result = c.Context; return false; }
    }
    [HarmonyPatch(typeof(Pathing), nameof(Pathing.RecalculatePerceivedPathCostAt))]
    public static class EtherTerrainChangedPatch
    { public static void Postfix(Map ___map, IntVec3 c) => ___map.GetComponent<MapComponent_EtherMovement>()?.Refresh(c); }
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    public static class EtherStartPathPatch
    {
        public static void Prefix(Pawn ___pawn, out Pawn __state) { __state = EtherMovement.startPathPawn; EtherMovement.startPathPawn = ___pawn; }
        public static void Finalizer(Pawn __state) { EtherMovement.startPathPawn = __state; }
    }
    [HarmonyPatch(typeof(Reachability), nameof(Reachability.CanReach), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) })]
    public static class EtherReachabilityPatch
    {
        public static bool Prefix(Map ___map, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, out bool __state)
        {
            __state = false;
            var pawn = traverseParams.pawn ?? EtherMovement.startPathPawn;
            if (!(pawn is Pawn_EtherServant) || pawn.Map != ___map || !start.InBounds(___map)) return true;
            // Native region traversal assumes every walkable start has a region;
            // exposed Space deliberately has none. Keep its null-region error out
            // of the ordinary region fast path.
            if (!EtherPathGrid.IsOpenSpace(___map.terrainGrid.TerrainAt(start))) return true;
            __state = true;
            __result = EtherMovement.CanReach(pawn, start, dest, peMode, traverseParams);
            return false;
        }
        public static void Postfix(Map ___map, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, bool __state)
        {
            if (__result || __state) return;
            var pawn = traverseParams.pawn ?? EtherMovement.startPathPawn;
            if (!(pawn is Pawn_EtherServant) || pawn.Map != ___map) return;
            __result = EtherMovement.CanReach(pawn, start, dest, peMode, traverseParams);
        }
    }
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.CreateRequest), new[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(IntVec3?), typeof(TraverseParms), typeof(PathFinderCostTuning?), typeof(PathEndMode), typeof(Pawn), typeof(PathRequest.IPathGridCustomizer) })]
    public static class EtherRequestPatch
    {
        public static void Postfix(PathRequest __result)
        { var c = EtherMovement.Component(__result.pawn); if (c != null) __result.customizer = c.Wrap(__result.customizer); }
    }
    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.ParameterizeGridJob))]
    public static class EtherGridJobPatch
    {
        public static void Postfix(PathRequest request, ref PathGridJob job)
        { var c = EtherMovement.Component(request.pawn); if (c != null) job.pathGridDirect = c.Context.pathGrid.Grid_Unsafe.AsReadOnly(); }
    }
    // These are adjacent native calls on the scheduling thread. The handoff is
    // one-shot and every request (including normal pawns) replaces the marker.
    [HarmonyPatch(typeof(PathFinder), "ParameterizePathJob")]
    public static class EtherPrepareConnectivityPatch
    {
        public static void Postfix(PathRequest request) { EtherMovement.preparedJob = EtherMovement.Component(request.pawn); }
        public static void Finalizer(Exception __exception) { if (__exception != null) EtherMovement.preparedJob = null; }
    }
    [HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.ParameterizePathJob))]
    public static class EtherConnectivityPatch
    {
        public static void Postfix(Map ___map, ref PathFinderJob job)
        {
            var c = EtherMovement.preparedJob; EtherMovement.preparedJob = null;
            if (c != null && c.map == ___map) job.connectivity = c.Connections;
        }
        public static void Finalizer() { EtherMovement.preparedJob = null; }
    }
}
