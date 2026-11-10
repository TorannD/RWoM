using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthHealthTransferTests
{
    static readonly Type Health = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthHealthForm", true)!;
    static bool Dirty(SpiritRebirthState state) => (bool)state.GetType().GetProperty("HealthFormDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(state)!;
    static void Refresh(Pawn pawn) => Health.GetMethod("TryRefreshPending", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [pawn]);
    static Thing_SoulStone Stone()
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "HealthTransferStone"; def.stackLimit = 1; def.thingClass = typeof(Thing_SoulStone);
        return new Thing_SoulStone { def = def, thingIDNumber = 68001 };
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritHealthTransfer.NativeCaptureImplantRemovesOwnedForm", r => Run(r, false));
        yield return new("SpiritHealthTransfer.NativeRemovalFailureKeepsCommitAndRetries", r => Run(r, true));
    }
    static Pawn? faultPawn;
    static bool throwRemoval;
    public static void AfterRemoval(Pawn_HealthTracker __instance)
    {
        if (throwRemoval && ReferenceEquals(__instance, faultPawn?.health))
        {
            throwRemoval = false;
            throw new InvalidOperationException("injected post-removal health refresh fault");
        }
    }
    static void Run(RuntimeFixture r, bool fault) => SpiritRebirthHealthFormTests.RunNative(r, true, (f, own, need) =>
    {
        // Reuse the established headless UI-only pause boundary; native health,
        // holder dispatch and runtime clock remain real.
        using var ui = (IDisposable)Activator.CreateInstance(typeof(SpiritRebirthRuntimeTests)
            .GetNestedType("UiPauseScope", BindingFlags.NonPublic)!, true)!;
        var state = f.Comp.SpiritRebirth;
        if (!fault)
        {
            f.PlaceForCustody(f.Original);
            var stages = TorannMagicDefOf.TM_SpiritPossessorHD.stages;
            try
            {
                TorannMagicDefOf.TM_SpiritPossessorHD.stages = null;
                Refresh(f.Original);
                Check.Equal(true, Dirty(state), "missing native stages keep active refresh pending");
                Check.Equal(true, f.Original.health.hediffSet.hediffs.Any(h => h.def == own), "missing stages do not remove existing owned form");
            }
            finally { TorannMagicDefOf.TM_SpiritPossessorHD.stages = stages; }
            Set(Find.TickManager, "ticksGameInt", 1250); Refresh(f.Original);
            Check.Equal(false, Dirty(state), "restored native definition permits bounded refresh");
            f.UnplaceForCustody(f.Original);
        }
        var hostEffects = f.Host.health.hediffSet.hediffs.ToArray();
        var holder = f.Hold(f.Host, f.Original);
        Check.Equal(true, SpiritRebirthStateTests.Invoke(state, "TryEnterHost", f.Original, f.Host), "native host transition commits");
        var stone = Stone();
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.health-transfer." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            if (fault)
            {
                faultPawn = f.Original; throwRemoval = true;
                var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(SpiritRebirthHealthTransferTests).GetMethod(nameof(AfterRemoval))!)!;
                ht.GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.RemoveHediff))!, null, hook, null, null]);
            }
            Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryCapture", f.Original, f.Host, false), "refresh never rolls back committed capture");
            Check.Same(f.Original, stone.ContainedSpirit, "same original belongs to stone");
            Check.Same(stone.GetDirectlyHeldThings(), f.Original.holdingOwner, "actual native custody retained");
            Check.Equal(true, state.HasValidStone(f.Original), "state and owner agree");
            Check.Equal(false, f.Original.health.hediffSet.hediffs.Any(h => h.def == own), "actual native owned effect removed");
            Check.Equal(true, f.Original.GetInvisibilityComp() == null, "native invisibility comp detached");
            if (fault)
            {
                Check.Equal(true, Dirty(state), "post-removal exception leaves refresh pending");
                var messages = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Check.Equal(1, messages.Count, "single bounded warning");
                Check.Equal(true, messages[0].Contains("injected post-removal"), "only injected failure logged"); RuntimeLog.Clear();
                Set(Find.TickManager, "ticksGameInt", 1249); Refresh(f.Original);
                Check.Equal(true, Dirty(state), "no retry before 250-tick deadline");
                Set(Find.TickManager, "ticksGameInt", 1250);
                typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthRuntime", true)!
                    .GetMethod("TickHeld", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [stone]);
            }
            Check.Equal(false, Dirty(state), "refresh completes including cache invalidation");
            Check.Equal(35f, f.Original.GetStatValue(StatDefOf.CarryingCapacity, cacheStaleAfterTicks: 1000), "native cached carrying restored");
            Check.Equal(1f, f.Original.GetStatValue(StatDefOf.PsychicSensitivity, cacheStaleAfterTicks: 1000), "primed native psychic cache cleared even after partial failure");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryImplant", f.Original, holder), "same pawn implants into actual native host holder");
            Check.Equal(false, Dirty(state), "implant refresh completed");
            Check.Equal(true, state.HasValidHost(f.Original), "host custody valid");
            var path = Path.Combine(r.WorkingDirectory, "health-transfer-state-" + fault + ".xml");
            var saved = state;
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "state"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            SpiritRebirthState loaded = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "state");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(f.Original); directory.RegisterLoaded(f.Host);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            Set(f.Comp, "spiritRebirth", loaded);
            Check.Equal(true, Dirty(loaded), "native state load marks refresh pending without native mutation");
            Check.Equal(true, loaded.HasValidHost(f.Original), "loaded refs still require actual reciprocal host custody");
            holder.GetDirectlyHeldThings().Remove(f.Original);
            Refresh(f.Original);
            Check.Equal(true, Dirty(loaded), "dirty flag cannot authorize maintenance without actual reciprocal custody");
            Check.Equal(true, holder.GetDirectlyHeldThings().TryAdd(f.Original, false), "restore exact native custody for tick");
            float severity = 0f; holder.CompPostTick(ref severity);
            Check.Equal(false, Dirty(loaded), "existing native host comp tick performs pending refresh");
            Check.Same(need, f.Original.needs.AllNeeds.Single(), "same native need retained");
            Check.Equal(0f, need.CurLevel, "refresh never refills zero energy");
            Check.Equal(hostEffects.Length + 1, f.Host.health.hediffSet.hediffs.Count, "host receives only preexisting fixture possession owner");
            Check.Same(holder.parent, f.Host.health.hediffSet.hediffs.Last(), "host hediff identity unchanged");
            RuntimeLog.AssertClean();
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); faultPawn = null; throwRemoval = false; }
    });
}
