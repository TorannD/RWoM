using System.Reflection;
using TorannMagic;
using Verse;
using Verse.AI;
using AbilityUser;
using RimWorld;
using TorannMagic.AutoCast;
using System.Collections;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    static IEnumerable<TestCase> FlightCases()
    {
        yield return new("FlightAutocast.CarrierRetainsWorkCargoAndQueue", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = FlightBoundaries(w);
            var cargo = FlightCargo(w);
            var queued = new Job(new JobDef { defName = "FlightQueuedWork" }); w.Caster.jobs.jobQueue.EnqueueLast(queued);
            var driver = new JobDriver_Goto { pawn = w.Caster, job = w.Job }; w.Caster.jobs.curDriver = driver;
            var goal = w.Caster.pather.Destination;
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan, TM_WorkFlight.TicksPerCell), "useful flight plan");
            var flight = FlightCarrier(w);
            Check.Equal(true, flight.Begin(w.Caster, plan), "flight takes off");
            Check.Equal(false, w.Caster.Spawned, "airborne pawn is removed from map ticking");
            Check.Same(flight, w.Caster.ParentHolder, "real ThingOwner holds the pawn");
            Check.Equal(true, ThingOwnerUtility.ContentsSuspended(flight), "same-tick job execution must stop while airborne");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "same cargo owner through takeoff");
            Check.Same(driver, w.Caster.jobs.curDriver, "same active toil driver");
            Check.Same(w.Job, w.Caster.CurJob, "non-suspendable work retained");
            Check.Same(queued, w.Caster.jobs.jobQueue.Peek().job, "queue retained");
            Check.Equal(true, TM_WorkFlight.PreserveJobs(w.Caster.jobs), "despawn protection ends synchronously");
            Check.Equal(true, TM_WorkFlight.PreserveReservations(w.Caster), "reservation protection ends synchronously");
            Check.Equal(true, flight.TryLand(), "landing succeeds");
            Check.Equal(0, flight.GetDirectlyHeldThings().Count, "ownership transferred exactly once");
            Check.Equal(true, w.Caster.Spawned, "pawn back on map");
            Check.Equal(plan.Landing, w.Caster.Position, "lands at planned location");
            Check.Same(w.Job, w.Caster.CurJob, "same job after landing");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "same cargo after landing");
            Check.Equal(12, cargo.stackCount, "no cargo duplication or splitting");
            Check.Equal(goal, resumedGoal, "original path resumed");
            Check.Equal(PathEndMode.Touch, resumedMode, "same work endpoint mode");
            Check.Equal(false, flight.TryLand(), "second landing is a no-op");
        }));
        yield return new("FlightAutocast.FailedLandingRetainsOwnerThenRecovers", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = FlightBoundaries(w);
            var cargo = FlightCargo(w);
            TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan, TM_WorkFlight.TicksPerCell);
            var flight = FlightCarrier(w); Check.Equal(true, flight.Begin(w.Caster, plan), "takeoff");
            flightDropFails = true;
            Check.Equal(false, flight.TryLand(), "failed spawn reported");
            flight.Destroy();
            Check.Equal(false, flight.Destroyed, "failed cleanup cannot destroy owned pawn");
            Check.Same(w.Caster, flight.Passenger, "real owner retains passenger for retry");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "cargo retained on failure");
            flightDropFails = false; w.Building(plan.Landing);
            Check.Equal(true, flight.TryLand(), "retry uses neighboring safe cell");
            Check.NotEqual(plan.Landing, w.Caster.Position, "does not place pawn in new wall");
            Check.Equal(true, w.Caster.Position.Standable(w.Map), "safe fallback");
        }));
        yield return new("FlightAutocast.FlightTimeMustBeatWalking", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = TravelBoundaries(w);
            travelCost = c => c == w.Caster.Position ? 100 : 65;
            Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _, TM_WorkFlight.TicksPerCell), "small nominal saving is lost to flight duration");
            travelCost = c => c == w.Caster.Position ? 900 : 20;
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out _, TM_WorkFlight.TicksPerCell), "substantial saving can fly");
        }));
        yield return new("FlightAutocast.LandingDoesNotOverwriteChangedWork", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = FlightBoundaries(w); FlightCargo(w);
            TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan, TM_WorkFlight.TicksPerCell);
            var flight = FlightCarrier(w); Check.Equal(true, flight.Begin(w.Caster, plan), "takeoff");
            var replacement = new Job(w.Job.def); Set(w.Caster.jobs, "curJob", replacement);
            Check.Equal(true, flight.TryLand(), "changed work does not prevent safe landing");
            Check.Same(replacement, w.Caster.CurJob, "external replacement retained");
            Check.Equal(false, resumedGoal.IsValid, "does not resume stale work destination");
        }));
        yield return new("FlightAutocast.InterruptedTakeoffCannotOrphanPawn", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = FlightBoundaries(w); var cargo = FlightCargo(w);
            TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan, TM_WorkFlight.TicksPerCell);
            var flight = FlightCarrier(w); flightDespawnThrows = true;
            Check.Equal(false, flight.Begin(w.Caster, plan), "failed takeoff reports failure");
            Check.Same(w.Caster, flight.Passenger, "pawn already off map is still owned for recovery");
            Check.Equal(true, TM_WorkFlight.PreserveJobs(w.Caster.jobs), "failure resets preservation scope");
            Check.Equal(true, flight.TryLand(), "failed takeoff can recover on map");
            Check.Equal(plan.Origin, w.Caster.Position, "failed takeoff recovers at origin without free travel");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "recovery retains cargo");
            AcceptFlightFault("injected flight despawn failure");
        }));
        yield return new("FlightAutocast.SaveInAirRetainsNativeJobQueueAndCargo", f => World(f, w =>
        {
            PrepareTravel(w); using var patches = FlightBoundaries(w); var cargo = FlightCargo(w);
            w.Job.targetA = cargo; w.Job.targetB = w.Caster.pather.Destination; w.Job.loadID = 412;
            w.Job.haulMode = HaulMode.ToCellStorage;
            w.Job.def.driverClass = typeof(JobDriver_HaulToCell); w.Job.def.suspendable = false;
            w.Caster.jobs.curDriver = new JobDriver_HaulToCell { pawn = w.Caster, job = w.Job };
            var queued = new Job(w.Job.def) { loadID = 413, count = 7 }; w.Caster.jobs.jobQueue.EnqueueLast(queued);
            TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan, TM_WorkFlight.TicksPerCell);
            var flight = FlightCarrier(w);
            using var carrierDef = new DefinitionScope<ThingDef>(flight.def);
            using var raceDef = new DefinitionScope<ThingDef>(w.Caster.def);
            using var cargoDef = new DefinitionScope<ThingDef>(cargo.def);
            using var jobDef = new DefinitionScope<JobDef>(w.Job.def);
            Check.Equal(true, flight.Begin(w.Caster, plan), "takeoff before save");
            Set(flight, "elapsed", 17);
            patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.ExposeData))!, nameof(FlightPawnSave));
            patches.Prefix(typeof(Find).GetProperty(nameof(Find.FactionManager))!.GetMethod!, nameof(FlightFactions));
            string file = Path.Combine(f.WorkingDirectory, "flight-in-air.xml");
            try
            {
                Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref flight, "flight"); Scribe.saver.FinalizeSaving();
            }
            finally { Scribe.ForceStop(); }
            TM_WorkFlight loaded = null!;
            try
            {
                Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "flight"); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            RuntimeLog.AssertClean();
            Check.Equal(17, (int)Get(loaded, "elapsed")!, "flight time restored");
            var pawn = Check.NotNull(loaded.Passenger, "loaded owner has passenger");
            Check.Same(loaded, pawn.ParentHolder, "loaded real owner rebound");
            Check.Equal(12, pawn.carryTracker.CarriedThing.stackCount, "native carry owner restores stack");
            Check.Equal(7, pawn.jobs.jobQueue.Peek().job.count, "native queue restored");
            Check.Equal(412, pawn.CurJob.loadID, "original job identity restored");
            Check.Same(pawn.carryTracker.CarriedThing, pawn.CurJob.targetA.Thing, "haul job targets its loaded carried stack");
            Check.Same(pawn.CurJob, Get(loaded, "work"), "cross-reference points to actual loaded job");
            Set(loaded, "mapIndexOrState", (sbyte)0);
            Check.Equal(true, loaded.TryLand(), "loaded flight lands");
            Check.Equal(plan.Destination, resumedGoal, "loaded job route resumed");
            Check.Equal(12, pawn.carryTracker.CarriedThing.stackCount, "loaded landing retains cargo");
        }));
        yield return new("FlightAutocast.ShippedCarrierDefinitionLoads", f =>
        {
            var xml = new System.Xml.XmlDocument(); xml.Load(Path.Combine(f.Repository, "v1.7/Defs/ThingDefs_Misc/TM_WorkFlight.xml"));
            var node = xml.DocumentElement!.FirstChild!;
            // ThingDef's constructor loads GPU shaders in this runtime. Validate
            // the shipped field names/types without pretending to run that loader.
            foreach (System.Xml.XmlNode field in node.ChildNodes)
                Check.NotNull(typeof(ThingDef).GetField(field.Name), "valid native ThingDef XML field " + field.Name);
            var type = typeof(TM_WorkFlight).Assembly.GetType(node["thingClass"]!.InnerText, true)!;
            Check.Equal(typeof(TM_WorkFlight), type, "shipped XML creates the owning carrier");
            Check.Equal(true, Activator.CreateInstance(type) is IThingHolder, "configured carrier owns its passenger");
            Check.Equal(TickerType.Normal, Enum.Parse<TickerType>(node["tickerType"]!.InnerText), "flight progresses every tick");
            Check.NotEqual("false", node["isSaveable"]?.InnerText, "definition retains engine saveable default");
        });
        yield return new("FlightAutocast.OwnCellRejectedBeforeLineOfSight", f => World(f, w =>
        {
            var verb = new Effect_Flight { caster = w.Caster, verbProps = new VerbProperties { range = 30 } };
            using var patches = new Patches();
            patches.Prefix(typeof(Verb).GetMethod("TryFindShootLineFromTo", Flags)!, nameof(FlightShootLine));
            Check.Equal(false, verb.CanHitTargetFrom(w.Caster.Position, w.Caster.Position), "Flight must reject its own cell even with clear line of sight");
        }));
        yield return new("FlightAutocast.SpecializedRouteConsumesIdleAndNPCAttempts", f => World(f, w =>
        {
            var magic = new CompAbilityUserMagic { parent = w.Caster };
            var def = f.AddAbility("TM_Flight");
            var method = typeof(CompAbilityUserMagic).GetMethod("TryHandleSpecializedAutocast", Flags)!;
            foreach (bool ai in new[] { false, true })
            {
                object?[] args = [new MagicPower { autocast = true }, def, null, ai, false];
                Check.Equal(true, (bool)method.Invoke(magic, args)!, "Flight must never fall through into generic OnSelf casting");
                Check.Equal(false, (bool)args[4]!, "ineligible flight is not a cast");
            }
        }));
        yield return new("FlightAutocast.SinglePaymentAndNoDuplicatePassenger", f => World(f, w =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            using var sense = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null
                ? new DefinitionScope<HediffDef>(new HediffDef { defName = "TM_SyrriumSenseHD" }) : null;
            PrepareTravel(w); using var patches = FlightBoundaries(w); var cargo = FlightCargo(w);
            w.Map.gameConditionManager = new GameConditionManager(w.Map);
            patches.Prefix(typeof(HistoryEventsManager).GetMethod(nameof(HistoryEventsManager.RecordEvent))!, nameof(SkipGraphics));
            patches.Prefix(typeof(GameConditionManager).GetProperty("Parent", Flags)!.GetMethod!, nameof(NoParentConditions));
            patches.Prefix(typeof(GenSpawn).GetMethods().Single(m => m.Name == "Spawn" && m.GetParameters()[0].ParameterType == typeof(Thing) && m.GetParameters().Length == 4), nameof(FlightSpawn));
            var carrierDef = Blank<ThingDef>(); carrierDef.defName = "TM_WorkFlight"; carrierDef.thingClass = typeof(TM_WorkFlight); carrierDef.category = ThingCategory.Ethereal;
            using var registered = new DefinitionScope<ThingDef>(carrierDef);
            var magic = new CompAbilityUserMagic { parent = w.Caster }; w.Caster.AllComps.Add(magic); TM_Calc.InvalidateCompCache(w.Caster);
            w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
            w.Caster.ageTracker = new Pawn_AgeTracker(w.Caster); Set(w.Caster.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
            Set(w.Caster.ageTracker, "cachedLifeStageIndex", 0);
            w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
            var mana = new Need_Mana(w.Caster); mana.CurLevel = .9f; w.Caster.needs.AllNeeds.Add(mana);
            Set(magic, "magicData", new MagicData());
            var def = f.AddAbility("TM_FlightPayment"); def.MainVerb.range = 16; def.MainVerb.warmupTime = 0; def.MainVerb.requiresLineOfSight = false;
            def.manaCost = .04f; def.consumeEnergy = true;
            var ability = new MagicAbility(magic) { Def = def };
            mana.CurLevel = 0;
            Check.Equal(false, TM_TravelAutocast.TryExecuteFlight(w.Caster, def, ability), "no mana cannot fly");
            mana.CurLevel = .9f;
            Check.Equal(true, TM_TravelAutocast.TryExecuteFlight(w.Caster, def, ability), "flight executes through production adapter");
            Check.Equal(true, Math.Abs(mana.CurLevel - .86f) < .00001f, "real ability charges exactly once");
            Check.Equal(true, ability.CooldownTicksLeft > 0, "real cooldown started");
            Check.Equal(1, magic.MagicUsed.Count, "one recorded magic use");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "paid flight retains cargo");
            Check.Equal(false, TM_TravelAutocast.TryExecuteFlight(w.Caster, def, ability), "repeat cannot fly again");
            Check.Equal(1, magic.MagicUsed.Count, "retry does not pay again");
            Check.Equal(true, lastFlight!.TryLand(), "paid flight lands");
        }));
    }
    static bool FlightShootLine(ref bool __result) { __result = true; return false; }
    static bool FlightFactions(ref FactionManager __result) { __result = new FactionManager(); return false; }
    static bool flightDropFails;
    static bool flightDespawnThrows;
    static TM_WorkFlight? lastFlight;
    static bool FlightSpawn(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
    {
        Set(newThing, "mapIndexOrState", (sbyte)0); Set(newThing, "positionInt", loc);
        lastFlight = newThing as TM_WorkFlight; __result = newThing; return false;
    }
    static Thing FlightCargo(WorldState w)
    {
        w.Caster.carryTracker = new Pawn_CarryTracker(w.Caster);
        var def = Blank<ThingDef>(); def.defName = "FlightCargo"; def.stackLimit = 75;
        var cargo = new Thing { def = def, stackCount = 12, thingIDNumber = 410 };
        Check.Equal(true, w.Caster.carryTracker.innerContainer.TryAdd(cargo), "real carried owner populated");
        return cargo;
    }
    static TM_WorkFlight FlightCarrier(WorldState w)
    {
        var def = Blank<ThingDef>(); def.defName = "TM_WorkFlight"; def.category = ThingCategory.Ethereal;
        var flight = new TM_WorkFlight { def = def, thingIDNumber = 411 };
        Set(flight, "mapIndexOrState", (sbyte)0); Set(flight, "positionInt", w.Caster.Position);
        return flight;
    }
    static Patches FlightBoundaries(WorldState w)
    {
        var patches = TravelBoundaries(w); flightDropFails = false; flightDespawnThrows = false;
        // Native map registration is outside this small map fixture. Keep real
        // ThingOwner transfer and the actual work-preservation prefixes.
        patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!, nameof(FlightDespawn));
        patches.Prefix(typeof(GenPlace).GetMethods().Single(m => m.Name == "TryPlaceThing" && m.GetParameters().Any(p => p.IsOut)), nameof(FlightPlace));
        foreach (string name in new[] { "WorkFlightKeepJobs", "WorkFlightKeepReservations" })
            patches.PatchType(typeof(TM_WorkFlight).Assembly.GetType("TorannMagic.AutoCast." + name)!);
        return patches;
    }
    static bool FlightDespawn(Pawn __instance)
    {
        Check.Equal(false, TM_WorkFlight.PreserveJobs(__instance.jobs), "only active flight skips native StopAll");
        Check.Equal(false, TM_WorkFlight.PreserveReservations(__instance), "only active flight keeps work reservations");
        Check.Equal(true, TM_WorkFlight.PreserveReservations(new Pawn()), "unrelated pawn is not protected");
        __instance.jobs.StopAll(); // Real native methods, guarded by production Harmony attributes.
        __instance.ClearAllReservations(false);
        active!.Map.thingGrid.Deregister(__instance); Set(__instance, "mapIndexOrState", (sbyte)-1);
        __instance.pather.StopDead();
        if (flightDespawnThrows) throw new InvalidOperationException("injected flight despawn failure");
        return false;
    }
    static bool FlightPlace(Thing thing, IntVec3 center, Map map, ref Thing lastResultingThing, ref bool __result)
    {
        lastResultingThing = thing;
        if (flightDropFails) { __result = false; return false; }
        Set(thing, "positionInt", center); Set(thing, "mapIndexOrState", (sbyte)0); map.thingGrid.Register(thing);
        __result = true; return false;
    }
    static bool FlightPawnSave(Pawn __instance)
    {
        // Isolated pawn boundary: retain native JobTracker/driver/queue and carry
        // serialization. Rendering, ideology and unrelated pawn systems are not
        // present in this small fixture; this is not a whole colony save test.
        Scribe_Defs.Look(ref __instance.def, "def");
        Scribe_Values.Look(ref __instance.thingIDNumber, "id");
        Scribe_Deep.Look(ref __instance.jobs, "jobs", __instance);
        Scribe_Deep.Look(ref __instance.carryTracker, "carry", __instance);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            __instance.health = new Pawn_HealthTracker(__instance);
            __instance.ageTracker = new Pawn_AgeTracker(__instance); Set(__instance.ageTracker, "cachedLifeStageIndex", 0);
            __instance.apparel = new Pawn_ApparelTracker(__instance);
            __instance.mindState = Blank<Pawn_MindState>();
            __instance.mindState.mentalStateHandler = new MentalStateHandler(__instance);
            __instance.pather = new Pawn_PathFollower(__instance);
        }
        return false;
    }
    static void AcceptFlightFault(string text)
    {
        var messages = (List<string>)typeof(RuntimeLog).GetField("Problems", Flags)!.GetValue(null)!;
        Check.Equal(1, messages.Count, "only the injected failure is logged");
        Check.Equal(true, messages[0].Contains(text), "expected injected failure remains visible");
        RuntimeLog.Clear();
    }
}


