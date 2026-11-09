using System.Reflection;
using RimWorld;
using TorannMagic.SpellItems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class SpellItemTests
{
    static IEnumerable<TestCase> ExtendedLifecycleCases()
    {
        yield return new("SpellItems.Resurrection.LongPreparationSaveResumeCancelAndPayOnce", f => InWorld(pawn =>
        {
            // This case covers the saved cast scheduler/payment. Real corpse and revival
            // outcome checks live in SpellItemEffectTests, not in this effect boundary.
            var item = Item(SpellItemKind.Resurrection, stack: 3); item.Props.warmupTicks = 1800; Hold(pawn, item);
            using var effects = EffectScope();
            using var end = new Patches("resurrection-timing"); end.Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!, nameof(EndJob));
            var driver = Driver(pawn, item); driver.ReadyForNextToil();
            for (int tick = 1; tick <= 600; tick++) { Tick(100 + tick); driver.DriverTick(); }
            Check.Equal(1200, driver.ticksLeftThisToil, "thirty-second cast has twenty seconds remaining");
            Check.Equal(0, effectCalls, "no resurrection before preparation finishes");
            string file = Path.Combine(f.WorkingDirectory, "resurrection-warmup.xml");
            WarmupEnvelope? envelope = new(pawn, item) { Job = driver.job, Driver = driver };
            try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref envelope, "cast", pawn, item); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            pawn.inventory.innerContainer.Remove(item.parent);
            item = Item(SpellItemKind.Resurrection, stack: 3); item.Props.warmupTicks = 1800; Hold(pawn, item); envelope = null;
            try
            {
                Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref envelope, "cast", pawn, item);
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(pawn); directory.RegisterLoaded(item.parent); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            driver = envelope!.Driver;
            Toils(driver)[0].preTickActions?.Clear(); Toils(driver)[0].preTickIntervalActions?.Clear();
            Check.Equal(1200, driver.ticksLeftThisToil, "Scribe retains exact remaining delay");
            for (int tick = 1; tick < 1200; tick++) { Tick(700 + tick); driver.DriverTick(); }
            Check.Equal(3, item.parent.stackCount, "no premature payment after loading");
            Tick(1900); driver.DriverTick();
            Check.Equal(1, effectCalls, "one delayed effect"); Check.Equal(2, item.parent.stackCount, "one scroll paid");
            Toils(driver)[1].initAction();
            Check.Equal(1, effectCalls, "no repeated delayed effect"); Check.Equal(2, item.parent.stackCount, "no repeated payment");
            Tick(2020); driver = Driver(pawn, item); driver.ReadyForNextToil(); driver.DriverTick();
            driver.Cleanup(JobCondition.InterruptForced);
            Check.Equal(false, item.IsBusy, "interruption releases token"); Check.Equal(2, item.parent.stackCount, "interrupted resurrection never pays");
        }));
    }
}
