using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.Odyssey;
using TorannMagic.SpellItems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    static Action? itemTravelContinuation;
    static int itemTravelDialogs, itemTravelOrders;
    static bool CaptureItemTravelWarning(string warning, Action continuation)
    { itemTravelDialogs++; itemTravelContinuation = continuation; return false; }
    static bool CaptureItemTravelOrder(Pawn_JobTracker __instance, Job job, ref bool __result)
    { itemTravelOrders++; Set(__instance, "curJob", job); __result = true; return false; }

    public static IEnumerable<TestCase> ManualItemBlinkCases()
    {
        yield return new("ManualTravel.ItemBlinkRealWarningOwnershipAndChargeOnce", f => World(f, w =>
        {
            using var language = new LanguageScope(f.Repository);
            using var environment = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1 };
            HeadlessDefOf(typeof(PawnCapacityDefOf)); var oldCapacity = PawnCapacityDefOf.Manipulation;
            using var capacity = new DefinitionScope<PawnCapacityDef>(new PawnCapacityDef { defName = "ManualBlinkManipulation", minForCapable = .1f });
            using var jobDefinition = new DefinitionScope<JobDef>(new JobDef { defName = "TM_SpellItemCast", driverClass = typeof(JobDriver_SpellItemCast) });
            try
            {
                PawnCapacityDefOf.Manipulation = capacity.Definition; Capacity(w.Caster, capacity.Definition);
                w.Caster.inventory = new Pawn_InventoryTracker(w.Caster); w.Caster.jobs = new Pawn_JobTracker(w.Caster);
                var originalJob = new Job(new JobDef { defName = "UninterruptedManualWork" }); Set(w.Caster.jobs, "curJob", originalJob);
                var scroll = Item("dangerous blink scroll", SpellItemKind.Blink, false, 3);
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(scroll.parent, false), "native inventory owns three scrolls");
                using var patches = new Patches();
                patches.Prefix(typeof(ManualTravelSafety).GetMethod(nameof(ManualTravelSafety.Confirm))!, nameof(CaptureItemTravelWarning));
                patches.Prefix(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.TryTakeOrderedJob))!, nameof(CaptureItemTravelOrder));
                patches.Prefix(typeof(Thing).GetProperty(nameof(Thing.Position))!.SetMethod!, nameof(AssignPosition));
                patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.Notify_Teleported))!, nameof(NotifyTeleport));
                patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.EmitBlink))!, nameof(EmitBlink));
                patches.Prefix(typeof(SpellItemEffects).GetMethod(nameof(SpellItemEffects.TryApply))!, nameof(RunActualEffect));
                itemTravelContinuation = null; itemTravelDialogs = itemTravelOrders = 0;
                var target = new IntVec3(8,0,6); var origin = w.Caster.Position;
                Check.Equal(false, SpellItemCasting.TryStart(w.Caster, scroll.parent, target), "danger does not immediately submit item job");
                Check.Equal(1, itemTravelDialogs, "actual item admission opens warning");
                Check.Equal(0, itemTravelOrders, "cancelling warning leaves no order");
                Check.Same(originalJob, w.Caster.CurJob, "warning preserves original work");
                Check.Equal(false, scroll.IsBusy, "warning acquires no source token");
                Check.Equal(3, scroll.parent.stackCount, "cancellation costs no item");
                // A source lost while the dialog is open cannot be authorized by its old continuation.
                w.Caster.inventory.innerContainer.Remove(scroll.parent); itemTravelContinuation!();
                Check.Equal(0, itemTravelOrders, "stale ownership rejects continuation");
                Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(scroll.parent, false), "restore real source owner");
                Check.Equal(false, SpellItemCasting.TryStart(w.Caster, scroll.parent, target), "new explicit request warns again");
                itemTravelContinuation!(); itemTravelContinuation!();
                Check.Equal(1, itemTravelOrders, "Continue submits exactly one native item cast order");
                var acceptedJob = w.Caster.CurJob;
                Check.Equal(true, ManualTravelSafety.HasConsent(w.Caster, acceptedJob, w.Map, target), "actual cast job owns exact destination receipt");
                Check.Equal(3, scroll.parent.stackCount, "ordering itself does not consume");
                int token = scroll.TryBegin(w.Caster); Check.Equal(true, token > 0, "real warmup admission owns source token");
                Set(w.Caster.jobs, "curJob", originalJob);
                Check.Equal(false, scroll.TryCommit(w.Caster, token, target), "other job cannot borrow confirmed danger");
                Check.Equal(origin, w.Caster.Position, "rejected receipt cannot move");
                Check.Equal(3, scroll.parent.stackCount, "rejected receipt cannot pay");
                Set(w.Caster.jobs, "curJob", acceptedJob);
                token = scroll.TryBegin(w.Caster); Check.Equal(true, token > 0, "failed completion released its old token");
                Check.Equal(true, scroll.TryCommit(w.Caster, token, target), "actual dispatcher and component commit accepted danger");
                Check.Equal(target, w.Caster.Position, "actual Blink destination applied");
                Check.Equal(2, scroll.parent.stackCount, "exactly one scroll charged");
                Check.Equal(false, scroll.TryCommit(w.Caster, token, target), "repeated token cannot replay move/payment");
                Check.Equal(2, scroll.parent.stackCount, "only one scroll remains charged");
                Check.Equal("Position,Notify,Blink", string.Join(",", w.Events), "one move, native notification, and visual dispatch");
            }
            finally { PawnCapacityDefOf.Manipulation = oldCapacity; }
        }));
    }
}

internal static partial class TeleportSpiritTests
{
    static int portalBoundarySpawns, portalBoundaryDespawns, portalRejectedJobs, portalVisualFailures;
    static bool PortalNativeDespawnBoundary(Pawn __instance)
    {
        if (__instance != active?.Caster) return true;
        portalBoundaryDespawns++; __instance.Map.thingGrid.Deregister(__instance);
        Set(__instance, "mapIndexOrState", (sbyte)-1); Set(__instance.jobs, "curJob", null); return false;
    }
    static bool PortalNativeSpawnBoundary(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
    {
        if (newThing != active?.Caster) return true;
        portalBoundarySpawns++; Set(newThing, "positionInt", loc); Set(newThing, "mapIndexOrState", (sbyte)Find.Maps.IndexOf(map));
        map.thingGrid.Register(newThing); __result = newThing; return false;
    }
    static bool PortalRejectJobBoundary(Pawn_JobTracker __instance, JobCondition condition)
    {
        if (__instance != active?.Caster.jobs) return true;
        Check.Equal(JobCondition.Incompletable, condition, "unsafe portal order ends without transfer");
        portalRejectedJobs++; Set(__instance, "curJob", null); return false;
    }
    static bool PortalVisualFailureBoundary() => throw new InvalidOperationException("injected portal visual failure");
    static bool PortalExpectedVisualWarning(string category, string message)
    {
        Check.Equal("Teleport", category, "diagnostic belongs to teleport");
        Check.Equal(true, message.Contains("injected portal visual failure"), "diagnostic preserves injected visual failure");
        portalVisualFailures++; return false;
    }

    static IEnumerable<TestCase> ManualPortalPaymentCases()
    {
        yield return new("ManualTravel.LegacyPortalTickGuardRefusalAndPaidVisualFailure", f => World(f, w =>
        {
            using var environment = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1 };
            using var patches = ManualBoundaries();
            foreach (var method in typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial"))
                patches.Prefix(method, nameof(SkipGraphics));
            patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!, nameof(PortalNativeDespawnBoundary));
            patches.Prefix(typeof(GenSpawn).GetMethods().Single(m => m.Name == nameof(GenSpawn.Spawn) && m.GetParameters().Length == 7 && m.GetParameters()[0].ParameterType == typeof(Thing)), nameof(PortalNativeSpawnBoundary));
            patches.Prefix(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.EndCurrentJob))!, nameof(PortalRejectJobBoundary));
            patches.Prefix(typeof(Pawn).GetProperty(nameof(Pawn.DrawPos))!.GetMethod!, nameof(DrawPos));
            patches.Prefix(typeof(TM_MoteMaker).GetMethod(nameof(TM_MoteMaker.ThrowCastingMote))!, nameof(PortalVisualFailureBoundary));
            patches.Prefix(typeof(TorannMagic.Compatibility.TM_Log).GetMethod("Warning", [typeof(string), typeof(string)])!, nameof(PortalExpectedVisualWarning));
            // Run actual Tick, PortalPawn, consent evaluation and energy subtraction; only heavy
            // native spawn/despawn registration and rendering are test boundaries.
            var oldJobDef = TorannMagicDefOf.UsePortal;
            try
            {
                TorannMagicDefOf.UsePortal = new JobDef { defName = "UsePortalPaymentFixture" };
                var def = Blank<ThingDef>(); def.defName = "PortalPaymentFixture"; def.category = ThingCategory.Building; def.size = new IntVec2(1,1); def.building = new BuildingProperties();
                var portal = new Building_TMPortal { def = def, IsPaired = true, PortalDestinationMap = w.Map, PortalDestinationPosition = new IntVec3(15,0,15) };
                Set(portal, "positionInt", w.Caster.Position); Set(portal, "mapIndexOrState", (sbyte)0); Set(portal, "arcaneEnergyCur", .8f);
                var otherPortal = new Building_TMPortal { def = def };
                portalBoundarySpawns = portalBoundaryDespawns = portalRejectedJobs = portalVisualFailures = 0;
                Set(Find.TickManager, "ticksGameInt", 100);
                var tick = typeof(Building_TMPortal).GetMethod("Tick", Flags)!;
                Set(w.Caster.jobs, "curJob", new Job(TorannMagicDefOf.UsePortal, otherPortal));
                tick.Invoke(portal, null);
                Check.Equal(0, portalRejectedJobs, "neighboring portal cannot intercept another portal's order");
                Check.Equal(0, portalBoundaryDespawns, "wrong portal target never starts movement");
                Check.Equal(.8f, (float)Get(portal, "arcaneEnergyCur")!, "wrong portal target costs nothing");
                var refused = new Job(TorannMagicDefOf.UsePortal, portal); Set(w.Caster.jobs, "curJob", refused);
                tick.Invoke(portal, null);
                Check.Equal(1, portalRejectedJobs, "native Tick handles unsafe refusal");
                Check.Equal(0, portalBoundaryDespawns, "refusal precedes despawn");
                Check.Equal(.8f, (float)Get(portal, "arcaneEnergyCur")!, "refused Tick deducts no energy");
                Check.Equal(true, portal.IsPaired, "environment refusal preserves pairing");
                var accepted = new Job(TorannMagicDefOf.UsePortal, portal);
                ManualTravelSafety.Submit(w.Caster, accepted, w.Map, portal.PortalDestinationPosition, true,
                    () => true, () => Set(w.Caster.jobs, "curJob", accepted));
                manualContinuation!();
                tick.Invoke(portal, null);
                Check.Equal(1, portalBoundaryDespawns, "real PortalPawn requests one despawn");
                Check.Equal(1, portalBoundarySpawns, "real PortalPawn requests one destination spawn");
                Check.Equal(portal.PortalDestinationPosition, w.Caster.Position, "traveler arrives at actual destination");
                Check.Equal(true, Math.Abs(.75f - (float)Get(portal, "arcaneEnergyCur")!) < .00001f, "successful Tick charges exactly .05 after completed transfer");
                Check.Equal(1, portalVisualFailures, "post-transfer renderer fault diagnosed without freeing travel");
                tick.Invoke(portal, null);
                Check.Equal(1, portalBoundarySpawns, "subsequent Tick cannot replay completed order");
                Check.Equal(true, Math.Abs(.75f - (float)Get(portal, "arcaneEnergyCur")!) < .00001f, "subsequent Tick cannot duplicate energy payment");
            }
            finally { TorannMagicDefOf.UsePortal = oldJobDef; }
        }));
    }
}
