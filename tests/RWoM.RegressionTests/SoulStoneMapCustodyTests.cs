using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SoulStoneMapCustodyTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SoulStoneMapCustody.NativePawnDespawnCapturesExactOriginal", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var mainThread = new MainThreadScope();
            var pawn = c.Creator; var map = c.Map;
            Set(pawn.jobs, "curJob", null!);
            map.pawnDestinationReservationManager = new PawnDestinationReservationManager();
            map.enrouteManager = new EnrouteManager(map);
            map.attackTargetReservationManager = new AttackTargetReservationManager(map);
            map.mapPawns = new MapPawns(map); map.autoSlaughterManager = new AutoSlaughterManager(map);
            pawn.def.comps ??= [];
            pawn.def.category = ThingCategory.Pawn; pawn.def.thingClass = typeof(Pawn); pawn.def.size = new IntVec2(1, 1);
            pawn.def.passability = Traversability.Standable;
            pawn.def.drawerType = DrawerType.RealtimeOnly;
            map.thingGrid.Register(pawn); map.listerThings.Add(pawn);
            map.attackTargetsCache.Notify_ThingSpawned(pawn);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(ClassIdentity.Component(pawn), "BeginSpiritRebirth"), "actual map pawn receives explicit marker");
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = "MapCustodyStone"; def.thingClass = typeof(Thing_SoulStone); def.stackLimit = 1; def.comps = [];
            var stone = new Thing_SoulStone { def = def, stackCount = 1 };
            Check.Same(map.spawnedThings, pawn.holdingOwner, "actual map owner before capture");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryCapture", pawn, null!, false), "capture executes native Pawn.DeSpawn before acquiring soul");
            Check.Equal(false, pawn.Spawned, "native despawn clears map state");
            Check.Equal(false, map.spawnedThings.Contains(pawn), "native map custody removed");
            Check.Equal(false, map.thingGrid.ThingsListAt(pawn.Position).Contains(pawn), "native grid deregistered");
            Check.Same(pawn, stone.ContainedSpirit, "same pawn retained in stone");
            Check.Same(stone.GetDirectlyHeldThings(), pawn.holdingOwner, "exclusive native stone ownership");
        }));
    }
    sealed class MainThreadScope : IDisposable
    {
        readonly FieldInfo field = typeof(UnityData).GetField("mainThreadId", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? previous;
        public MainThreadScope() { previous = field.GetValue(null); field.SetValue(null, Environment.CurrentManagedThreadId); }
        public void Dispose() => field.SetValue(null, previous);
    }
}
