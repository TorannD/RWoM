using System.Reflection;
using RimWorld.Planet;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

// Native disposal investigation only. No production caravan-loss handler exists.
internal static class SpiritCaravanLossNativeTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritCaravanLoss.NativeDiscardWithoutWorldRegistration", r => Run(r));
    }
    internal static void Run(RuntimeFixture r, Action<SpiritRebirthRitualTransactionTests.Context, Thing_SoulStone>? dispose = null,
        bool expectCompleted = true)
        => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            typeof(SpellItemEffectTests).GetMethod("HeadlessDefOf", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [typeof(PawnRelationDefOf)]);
            Check.Equal("ConvertedHeld", c.Execute(), "actual original revived and pending");
            c.Map.attackTargetReservationManager = new AttackTargetReservationManager(c.Map);
            c.Map.pawnDestinationReservationManager = new PawnDestinationReservationManager();
            var oldWorldObjects = Find.World.worldObjects;
            bool oldDestroyable = c.Original.def.destroyable;
            try
            {
            Find.World.worldObjects = new WorldObjectsHolder();
            var original = c.Original;
            original.def.destroyable = true;
            var faction = original.Faction;
            var relative = original.relations.DirectRelations.First().otherPawn;
            var linkedUndead = NewUndead();
            linkedUndead.GetType().GetField("linkedPawn")!.SetValue(linkedUndead, original);
            var stone = new Thing_SoulStone { def = c.Magicyte.def, thingIDNumber = 69991 };
            original.holdingOwner.Remove(original);
            Check.Equal(true, stone.GetDirectlyHeldThings().TryAdd(original, false), "native stone owns exact marked original for disposal probe");
            Check.Equal(true, c.Creator.inventory.innerContainer.TryAdd(stone, false), "native inventory owns occupied shell");
            Check.Equal(false, Find.WorldPawns.Contains(original), "original is not a world pawn");
            passCalls = 0;
            using var observer = new PassObserver();
            if (dispose != null) dispose(c, stone);
            else
            {
                var discard = typeof(WorldPawns).GetMethod("DiscardPawn", BindingFlags.Instance | BindingFlags.NonPublic)!;
                try { discard.Invoke(Find.WorldPawns, [original, true]); }
                catch (TargetInvocationException e) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException!).Throw(); }
            }
            if (!expectCompleted) return;
            Check.Equal(true, original.Destroyed && original.Discarded, "actual native destroy and discard complete");
            Check.Equal(false, Find.WorldPawns.IsBeingDiscarded(original), "native discard scope cleared");
            Check.Equal(false, Find.WorldPawns.Contains(original), "no persisted world-pawn original");
            Check.Equal(0, passCalls, "native guarded destroy never calls PassToWorld");
            Check.Same(faction, original.Faction, "native disposal does not change faction");
            Check.Equal(false, relative.relations.DirectRelations.Any(x => ReferenceEquals(x.otherPawn, original)), "native discard removes reciprocal surviving-relative reference");
            Check.Equal(0, original.relations.DirectRelations.Count, "native discard clears original relationships");
            Check.Equal(0, stone.GetDirectlyHeldThings().Count, "native destroy removes original from stone owner");
            stone.Destroy();
            Check.Equal(true, stone.Destroyed, "now-empty shell follows ordinary destruction");
            Check.Equal(false, c.Creator.inventory.innerContainer.Contains(stone), "native shell destruction removes inventory entry");
            var path = Path.Combine(r.WorkingDirectory, "native-loss-undead-reference.xml");
            var graph = new UndeadLinkGraph { link = linkedUndead };
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref graph, "graph"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            graph = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref graph, "graph"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Check.Equal(true, graph.link.GetType().GetField("linkedPawn")!.GetValue(graph.link) == null, "actual undead CompExposeData does not persist discarded original");
            RuntimeLog.AssertClean();
            }
            finally
            {
                c.Original.def.destroyable = oldDestroyable;
                Find.World.worldObjects = oldWorldObjects;
            }
        });

    static int passCalls;
    static HediffComp NewUndead() => (HediffComp)Activator.CreateInstance(typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.HediffComp_Undead")!)!;
    public sealed class UndeadLinkGraph : IExposable
    {
        public HediffComp link = NewUndead();
        public void ExposeData() => link.CompExposeData();
    }
    static void ObservePass() { passCalls++; }
    sealed class PassObserver : IDisposable
    {
        readonly object harmony;
        readonly string id = "rwom.native-caravan-loss." + Guid.NewGuid();
        public PassObserver()
        {
            var assembly = Assembly.Load("0Harmony");
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritCaravanLossNativeTests).GetMethod(nameof(ObservePass), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(WorldPawns).GetMethod(nameof(WorldPawns.PassToWorld))!, prefix, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}
