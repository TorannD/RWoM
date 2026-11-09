using Verse.AI;
using RimWorld.Planet;
using RimWorld;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    public static IEnumerable<TestCase> CoreReturnCases()
    {
        yield return new("SynthHaul.ResumeWithCargo", f => CoreReturnWorld(f, ResumeSynthHaul));
        yield return new("GolemTravel.BlockedFootprintKeepsPawn", f => CoreReturnWorld(f, BlockedGolem));
        yield return new("CoreReturn.ConfirmedRebinding", f => CoreReturnWorld(f, ConfirmedRebind));
        yield return new("CoreReturn.NativeNetworkLocalFirstAtomic", OdysseyBeaconTests.CoreReturnNetworkCase);
        yield return new("CoreReturn.NativeBindingNonmageAndOwnership", f => CoreReturnWorld(f, CoreReturnBinding));
        yield return new("CoreReturn.MaintenancePartitionAndNoCatchup", f => CoreReturnWorld(f, CoreReturnMaintenance));
        yield return new("CoreReturn.NativeScribeBindingAndMaintenance", f => CoreReturnWorld(f, w => CoreReturnSave(f, w)));
        yield return new("CoreReturn.FuelInsufficientNoPartialPayment", f => CoreReturnWorld(f, CoreReturnFuel));
        yield return new("CoreReturn.NativeMapTransferCargoAndRepeat", f => CoreReturnWorld(f, CoreReturnMove));
        yield return new("CoreReturn.PreflightFuelLandingAndStaleOwner", f => CoreReturnWorld(f, CoreReturnRefusals));
        yield return new("CoreReturn.FuelSignalStaleBindingRefund", f => CoreReturnWorld(f, CoreReturnStaleSignal));
        yield return new("CoreReturn.NativeCaravanSingle", f => CoreReturnWorld(f, w=>CoreReturnCaravan(w,false)));
        yield return new("CoreReturn.NativeCaravanWhole", f => CoreReturnWorld(f, w=>CoreReturnCaravan(w,true)));
        yield return new("CoreReturn.NativeCrossMap", f=>CoreReturnWorld(f,CoreReturnCrossMap));
        yield return new("CoreReturn.ApproachCompletionAndRevalidation", f=>CoreReturnWorld(f,CoreReturnApproach));
        yield return new("CoreReturn.ConfirmationCaravanRosterStale", f=>CoreReturnWorld(f,w=>CoreReturnCaravan(w,true,true)));
        yield return new("CoreReturn.MaintenanceFailureBoundedRetry", f=>CoreReturnWorld(f,CoreReturnRetry));
        yield return new("CoreReturn.GizmosEnumerateWithoutThrowing", f=>CoreReturnWorld(f,CoreReturnGizmos));
        yield return new("CoreReturn.SecondColonistMarkableAfterFirstCycle", f=>CoreReturnWorld(f,CoreReturnSecondBind));
    }

    // Player report: after the first bind+return cycle, "Пометить" claims no
    // suitable colonists exist. A second unbound colonist must remain markable.
    static void ConfirmedRebind(WorldState w)
    {
        using var def = CoreReturnDef(); using var health = CoreReturnHealthBoundary();
        var old = CoreReturnCore(w); var target = CoreReturnCore(w);
        Check.Equal(true, old.Bind(w.Caster), "first mark");
        var mark = CompCoreReturn.FindMark(w.Caster)!;
        var canOrder = typeof(CompCoreReturn).GetMethod("CanOrderMark", Flags)!;
        Check.Equal(true, (bool)canOrder.Invoke(target, [w.Caster, mark, old.parent])!, "confirmed current mark accepted");
        Check.Equal(false, (bool)canOrder.Invoke(target, [w.Caster, null, null])!, "stale unbound snapshot refused");
        var driver = new JobDriver_ReceiveCoreReturnMark { pawn = w.Caster, job = new Job(new JobDef(), target.parent, old.parent) };
        var steps = ((IEnumerable<Toil>)typeof(JobDriver_ReceiveCoreReturnMark).GetMethod("MakeNewToils", Flags)!.Invoke(driver, null)!).ToList();
        steps.Last().initAction();
        Check.Same(mark, CompCoreReturn.FindMark(w.Caster), "same hediff rebound on arrival");
        Check.Equal(true, target.IsBound(w.Caster) && !old.IsBound(w.Caster), "only new core owns pawn");
    }

    static void ResumeSynthHaul(WorldState w)
    {
        var target = LegacyBuilding<Building>(w);
        var bank = new CompMagofuelReservoir { parent = target, props = new CompProperties_MagofuelReservoir() };
        target.AllComps.Add(bank);
        var oldFuel = ThingDefOf.Chemfuel;
        try
        {
            var fuel = Blank<ThingDef>(); fuel.defName = "TestChemfuel"; fuel.category = ThingCategory.Item;
            fuel.thingClass = typeof(Thing); fuel.stackLimit = 75; fuel.destroyable = true;
            ThingDefOf.Chemfuel = fuel;
            w.Map.reservationManager = new ReservationManager(w.Map);
            w.Map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
            var stack = new Thing { def = fuel, stackCount = 10 };
            w.Caster.carryTracker = new Pawn_CarryTracker(w.Caster);
            Check.Equal(true, w.Caster.carryTracker.innerContainer.TryAdd(stack, false), "actual carry owns last stack");
            Set(bank, "chemfuelHopper", 95f);
            var driver = new JobDriver_LoadSynthChemfuel { pawn = w.Caster, job = new Job(new JobDef(), stack, target) };
            var steps = ((IEnumerable<Toil>)typeof(JobDriver_LoadSynthChemfuel).GetMethod("MakeNewToils", Flags)!.Invoke(driver, null)!).ToList();
            Check.Equal(5, steps.Count, "saved delivery index survives without ground fuel");
            steps[4].initAction();
            Check.Equal(100f, bank.ChemfuelHopper, "only available space accepted");
            Check.Equal(5, w.Caster.carryTracker.CarriedThing.stackCount, "excess remains in real carry tracker");
        }
        finally { ThingDefOf.Chemfuel = oldFuel; }
    }

    static void BlockedGolem(WorldState w)
    {
        var station = new TorannMagic.Golems.Building_TMGolemBase { def = Blank<ThingDef>() };
        station.def.size = new IntVec2(2,2);
        var comp = new TorannMagic.Golems.CompGolem { parent = w.Caster, dormantThing = station,
            dormantMap = w.Map, dormantPosition = w.Caster.Position, dormantRotation = Rot4.North };
        var furniture = LegacyBuilding<Building>(w);
        furniture.def.size = new IntVec2(5,5);
        Set(furniture, "positionInt", w.Caster.Position);
        w.Map.edificeGrid.Register(furniture);
        comp.DeSpawnGolem();
        Check.Equal(true, w.Caster.Spawned && !furniture.Destroyed, "blocked shutdown keeps pawn and furniture");
        Check.Same(station, comp.InnerWorkstation, "dormant building retained for retry");
    }

    static void CoreReturnSecondBind(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();
        var core=CoreReturnReady(w,100); // binds w.Caster and walks them away
        var second=w.Pawn("CoreReturnSecond",new IntVec3(12,0,12));
        using var movement=CoreReturnMovementBoundary(); portalBoundarySpawns=portalBoundaryDespawns=0;
        Check.Equal(true,CoreReturnUtility.TryReturn(core,[w.Caster],out var why),"first cycle return "+why);
        Check.Equal(true,core.CanMark(second),"CanMark accepts the unbound colonist");
        var reason=(string)typeof(CompCoreReturn).GetMethod("MarkBlockReason",Flags)!.Invoke(core,[second])!;
        Check.Equal(true, reason == null, "an awake moving colonist has no mark block reason");
        var mark=CompCoreReturn.FindMark(w.Caster);
        Check.Equal(true,mark!=null,"the returned pawn keeps its recall mark by design");
    }

    static void CoreReturnWorld(RuntimeFixture f, Action<WorldState> body)
    {
        typeof(SpellItemEffectTests).GetMethod("HeadlessDefOf",Flags)!.Invoke(null,[typeof(PawnCapacityDefOf)]);
        var culture=System.Globalization.CultureInfo.CurrentCulture; System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
        var old=PawnCapacityDefOf.Moving; var oldConsciousness=PawnCapacityDefOf.Consciousness;
        var moving=new PawnCapacityDef {defName="CoreReturnMoving",minForCapable=.1f}; using var capacity=new DefinitionScope<PawnCapacityDef>(moving);
        var consciousness=new PawnCapacityDef {defName="CoreReturnConsciousness",minForCapable=.1f}; using var consciousScope=new DefinitionScope<PawnCapacityDef>(consciousness); try {PawnCapacityDefOf.Moving=moving;PawnCapacityDefOf.Consciousness=consciousness;World(f,body);}
        finally {System.Globalization.CultureInfo.CurrentCulture=culture;PawnCapacityDefOf.Moving=old;PawnCapacityDefOf.Consciousness=oldConsciousness;}
    }
    static CompCoreReturn CoreReturnCore(WorldState w, float amount = 100)
    {
        var world = Blank<RimWorld.Planet.World>(); world.factionManager = new FactionManager();world.worldPawns=new WorldPawns();
        world.factionManager.AllFactionsListForReading.Add(w.Player); Set(world.factionManager,"ofPlayer",w.Player);
        Set(Current.Game,"worldInt",world);
        w.Caster.def.race.body=new BodyDef {corePart=new BodyPartRecord {def=new BodyPartDef {defName="CoreReturnBody"}}};
        var field=typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels",Flags)!;
        var cache=Activator.CreateInstance(field.FieldType)!;
        foreach(var cap in new[]{PawnCapacityDefOf.Moving,PawnCapacityDefOf.Consciousness})
        {
            var entry=field.FieldType.GetProperty("Item",[typeof(PawnCapacityDef)])!.GetValue(cache,[cap])!;
            Set(entry,"status",Enum.Parse(entry.GetType().GetField("status")!.FieldType,"Cached"));Set(entry,"value",1f);
        }
        field.SetValue(w.Caster.health.capacities,cache);
        var building = LegacyBuilding<Building>(w);
        building.thingIDNumber = 9001;building.def.useHitPoints=false;building.def.label="return core";
        var comp = new CompCoreReturn { parent = building, props = new CompProperties_CoreReturn() };
        building.AllComps.Add(comp);
        var fuel=LegacyFuel(building, amount);
        var fuelDef=Blank<ThingDef>(); fuelDef.defName="TM_Magofuel";fuelDef.virtualDefs=[];
        var filter=new ThingFilter();filter.SetAllow(fuelDef,true); fuel.Props.fuelFilter=filter;
        return comp;
    }
    static DefinitionScope<HediffDef> CoreReturnDef() => new(new HediffDef
    {
        defName = "TM_CoreReturnMark", hediffClass = typeof(Hediff_CoreReturnMark),
        initialSeverity = 1, maxSeverity = 1
    });
    static Patches CoreReturnHealthBoundary()
    {
        var patches = new Patches();
        // Real AddHediff/RemoveHediff ownership and callbacks execute. Only unrelated
        // physiological recalculation needs the complete game body/needs databases.
        patches.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(SkipGraphics));
        return patches;
    }
    static void CoreReturnBinding(WorldState w)
    {
        using var definition = CoreReturnDef(); using var health = CoreReturnHealthBoundary();
        var core = CoreReturnCore(w);
        Check.Equal(true, core.Bind(w.Caster), "native nonmage receives actual hediff at core");
        var mark = CompCoreReturn.FindMark(w.Caster);
        Check.Same(core.parent, mark!.Core, "mark owns exact core reference");
        Check.Equal(1, w.Caster.health.hediffSet.hediffs.OfType<Hediff_CoreReturnMark>().Count(), "one real mark");
        core.Bind(w.Caster); core.Register(w.Caster);
        Check.Equal(1, core.BoundPawns.Count(), "duplicate bind and registration cannot multiply upkeep");
        Set(w.Caster, "factionInt", new Faction { def = new FactionDef() });
        Check.Equal(false, core.CanMark(w.Caster), "foreign pawn cannot receive mark");
        Check.Equal(false, core.IsBound(w.Caster), "lost ownership invalidates recall");
        Set(w.Caster, "factionInt", w.Player);
        core.Unbind(w.Caster);
        Check.Equal(null, CompCoreReturn.FindMark(w.Caster), "unbind removes actual hediff");
        Check.Equal(0, core.BoundPawns.Count(), "unbind releases registry");
    }
    static void CoreReturnMaintenance(WorldState w)
    {
        using var definition = CoreReturnDef(); using var health = CoreReturnHealthBoundary();
        var core = CoreReturnCore(w); Check.Equal(true, core.Bind(w.Caster), "mark admitted");
        var fuel = core.parent.GetComp<CompRefuelable>();
        for (int tick=0; tick<60000; tick+=250) core.TickMaintenance(tick,250);
        Check.Equal(99f, fuel.Fuel, "one living mark consumes one fuel per day");
        double credit = core.PaidFuelCredit;
        core.TickMaintenance(59750,250);
        Check.Equal(99f, fuel.Fuel, "duplicate interval cannot charge twice");
        Check.Equal(credit, core.PaidFuelCredit, "duplicate interval preserves fractional credit");
        core.TickMaintenance(600000,1);
        Check.Equal(true, fuel.Fuel >= 98f, "unloaded time never creates debt catch-up");
        core.Unbind(w.Caster); float before=fuel.Fuel;
        core.TickMaintenance(600001,60000);
        Check.Equal(before, fuel.Fuel, "no marks means no maintenance");
    }
    public sealed class CoreReturnSaveEnvelope : IExposable
    {
        public CompCoreReturn core = new CompCoreReturn();
        public Hediff_CoreReturnMark mark = null!;
        public void ExposeData()
        {
            core.PostExposeData(); Scribe_Deep.Look(ref mark,"mark");
        }
    }
    static void CoreReturnSave(RuntimeFixture f, WorldState w)
    {
        using var definition=CoreReturnDef(); using var health=CoreReturnHealthBoundary();
        var core=CoreReturnCore(w); Check.Equal(true,core.Bind(w.Caster),"mark before save");
        core.TickMaintenance(100,250);
        var save=new CoreReturnSaveEnvelope {core=core,mark=CompCoreReturn.FindMark(w.Caster)!};
        string path=Path.Combine(f.WorkingDirectory,"core-return.xml");
        try {Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref save,"state");Scribe.saver.FinalizeSaving();} finally {Scribe.ForceStop();}
        CoreReturnSaveEnvelope loaded=null!;
        try
        {
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded,"state");
            var directory=(LoadedObjectDirectory)Get(Scribe.loader.crossRefs,"loadedObjectDirectory")!;
            directory.RegisterLoaded(core.parent); directory.RegisterLoaded(w.Caster);
            loaded.core.parent=core.parent; loaded.core.props=core.props;
            Scribe.loader.FinalizeLoading();
        }
        finally {Scribe.ForceStop();}
        Check.Same(core.parent,loaded.mark.Core,"native reference resolution preserves core identity");
        Check.Equal(core.PaidFuelCredit,loaded.core.PaidFuelCredit,"fractional maintenance survives native save");
        double before=loaded.core.PaidFuelCredit; loaded.core.TickMaintenance(100,250);
        Check.Equal(before,loaded.core.PaidFuelCredit,"loaded receipt rejects replay");
    }
    static CompCoreReturn CoreReturnReady(WorldState w, float fuel=100)
    {
        var core=CoreReturnCore(w,fuel);Check.Equal(true,core.Bind(w.Caster),"bind before walking away");
        w.Map.thingGrid.Deregister(w.Caster);Set(w.Caster,"positionInt",new IntVec3(17,0,17));w.Map.thingGrid.Register(w.Caster);
        w.Map.listerBuildings=new ListerBuildings();w.Map.listerThings=new ListerThings(ListerThingsUse.Global);
        w.Map.info.parent=new MapParent {def=new WorldObjectDef {defName="CoreReturnMap",label="destination",canBePlayerHome=true}};Set(w.Map.info.parent,"factionInt",w.Player);
        w.Map.terrainGrid=new TerrainGrid(w.Map);var floor=Blank<TerrainDef>();floor.defName="CoreReturnDryFloor";floor.tags=[];
        Array.Fill(w.Map.terrainGrid.topGrid,floor);
        return core;
    }
    static Patches CoreReturnMovementBoundary()
    {
        var patches=new Patches();
        patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!,nameof(PortalNativeDespawnBoundary));
        patches.Prefix(typeof(GenSpawn).GetMethods().Single(m=>m.Name==nameof(GenSpawn.Spawn)&&m.GetParameters().Length==7&&m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(PortalNativeSpawnBoundary));
        return patches;
    }
    static void CoreReturnMove(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();
        var core=CoreReturnReady(w);var cargo=FlightCargo(w);
        w.Caster.inventory=new Pawn_InventoryTracker(w.Caster);
        var item=new Thing {def=cargo.def,stackCount=7,thingIDNumber=412};w.Caster.inventory.innerContainer.TryAdd(item,false);
        using var movement=CoreReturnMovementBoundary(); portalBoundarySpawns=portalBoundaryDespawns=0;
        Check.Equal(true,CoreReturnUtility.TryPreview(core,[w.Caster],out int count,out string why),"safe map preview "+why);
        Check.Equal(1,count,"one passenger charged, cargo free");
        Check.Equal(true,CoreReturnUtility.TryReturn(core,[w.Caster],out why),"actual map transaction "+why);
        Check.Equal(90f,core.parent.GetComp<CompRefuelable>().Fuel,"ten fuel one passenger");
        Check.Same(cargo,w.Caster.carryTracker.CarriedThing,"same carried stack restored to native owner");
        Check.Same(item,w.Caster.inventory.innerContainer.Single(),"same inventory stack follows owner");
        Check.Equal(12,cargo.stackCount,"carried stack count intact"); Check.Equal(7,item.stackCount,"inventory count intact");
        Check.Equal(1,portalBoundarySpawns,"one native spawn boundary");
        Check.Equal(false,CoreReturnUtility.TryReturn(core,[w.Caster],out _),"already near cannot pay or move twice");
        Check.Equal(90f,core.parent.GetComp<CompRefuelable>().Fuel,"repeat costs nothing");
    }
    static void CoreReturnRefusals(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();
        var core=CoreReturnReady(w,9);var fuel=core.parent.GetComp<CompRefuelable>();var origin=w.Caster.Position;var job=w.Caster.CurJob;
        Check.Equal(false,CoreReturnUtility.TryReturn(core,[w.Caster],out _),"insufficient return payment rejects");
        Check.Equal(9f,fuel.Fuel,"no partial payment");Check.Equal(origin,w.Caster.Position,"no move");Check.Same(job,w.Caster.CurJob,"no job mutation");
        Set(fuel,"fuel",100f);Check.Equal(true,CoreReturnUtility.TryPreview(core,[w.Caster],out _,out _),"preview valid before owner changes");
        Set(w.Caster,"factionInt",new Faction {def=new FactionDef()});
        Check.Equal(false,CoreReturnUtility.TryReturn(core,[w.Caster],out _),"stale preview cannot borrow old ownership");
        Set(w.Caster,"factionInt",w.Player);
        for(int x=0;x<20;x++)for(int z=0;z<20;z++)w.Building(new IntVec3(x,0,z));
        Check.Equal(false,CoreReturnUtility.TryReturn(core,[w.Caster],out _),"no safe landing rejects entire batch");
        Check.Equal(100f,fuel.Fuel,"blocked landing costs nothing");Check.Equal(origin,w.Caster.Position,"blocked landing leaves source intact");
    }
    sealed class CoreReturnSignal : ThingComp
    {
        public Action OnEmpty=null!;
        public override void ReceiveCompSignal(string signal) {if(signal=="RanOutOfFuel")OnEmpty();}
    }
    static void CoreReturnStaleSignal(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();
        var core=CoreReturnReady(w,10);var origin=w.Caster.Position;
        core.parent.AllComps.Add(new CoreReturnSignal {parent=core.parent,props=new CompProperties(),OnEmpty=()=>core.Unbind(w.Caster)});
        Check.Equal(false,CoreReturnUtility.TryReturn(core,[w.Caster],out _),"fuel broadcast removing mark invalidates paid request");
        Check.Equal(10f,core.parent.GetComp<CompRefuelable>().Fuel,"failed post-payment validation refunds fuel");
        Check.Equal(origin,w.Caster.Position,"post-payment invalidation moves nobody");
    }
    static void CoreReturnCaravan(WorldState w, bool whole, bool stale=false)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();
        var core=CoreReturnReady(w);var cargo=FlightCargo(w);
        var animal=w.Pawn("CoreReturnAnimal",new IntVec3(16,0,17));animal.def.race.intelligence=Intelligence.Animal;
        animal.inventory=new Pawn_InventoryTracker(animal);var pack=new Thing {def=cargo.def,stackCount=33,thingIDNumber=413};animal.inventory.innerContainer.TryAdd(pack,false);
        var prisoner=w.Pawn("CoreReturnPrisoner",new IntVec3(15,0,17),false);Set(prisoner.guest,"guestStatusInt",GuestStatus.Prisoner);
        Set(((UIRoot_Play)Current.Root.uiRoot).mapUI,"colonistBar",Blank<ColonistBar>());
        var caravan=new Caravan {def=new WorldObjectDef {defName="CoreReturnCaravan"},ID=908};Set(caravan,"factionInt",w.Player);
        var world=Find.World;world.worldObjects=new WorldObjectsHolder();world.worldObjects.AllWorldObjects.Add(caravan);
        typeof(WorldObjectsHolder).GetMethod("AddToCache",Flags)!.Invoke(world.worldObjects,[caravan]);
        foreach(var pawn in new[]{w.Caster,animal,prisoner})
        {
            pawn.Name=new NameSingle(pawn.def.defName);Set(pawn.mindState,"pawn",pawn);Set(pawn.mindState,"babyCaravanBreastfeed",new Dictionary<Pawn,float>());pawn.inventory ??= new Pawn_InventoryTracker(pawn);pawn.def.race.lifeStageAges=w.Caster.def.race.lifeStageAges;Set(pawn.def.race,"fleshType",w.Caster.RaceProps.FleshType);
            pawn.ageTracker=new Pawn_AgeTracker(pawn);Set(pawn.ageTracker,"cachedLifeStageIndex",0);pawn.apparel=new Pawn_ApparelTracker(pawn);
            w.Map.thingGrid.Deregister(pawn);Set(pawn,"mapIndexOrState",(sbyte)-1);
            Check.Equal(true,caravan.pawns.TryAdd(pawn,false),"native caravan owns passenger");
            ((HashSet<Pawn>)Get(world.worldPawns,"pawnsAlive")!).Add(pawn);
        }
        using var movement=new Patches();
        movement.Prefix(typeof(GenSpawn).GetMethods().Single(m=>m.Name==nameof(GenSpawn.Spawn)&&m.GetParameters().Length==7&&m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(CoreReturnAllSpawn));
        movement.Prefix(typeof(WorldObject).GetMethod(nameof(WorldObject.PostRemove))!,nameof(SkipGraphics));
        var roster=whole?new[]{w.Caster,animal,prisoner}:new[]{w.Caster};
        if(stale)
        {
            movement.Prefix(typeof(Find).GetProperty(nameof(Find.WindowStack))!.GetMethod!,nameof(CoreReturnWindowStack));
            movement.Prefix(typeof(WindowStack).GetMethod(nameof(WindowStack.Add))!,nameof(CoreReturnCaptureDialog));
            movement.Prefix(typeof(Thing).GetProperty(nameof(Thing.LabelCap))!.GetMethod!,nameof(CoreReturnLabel));
            coreReturnConfirmation=null;core.RequestReturn(roster);Check.NotNull(coreReturnConfirmation,"native request opens confirmation");
            Check.Equal(100f,core.parent.GetComp<CompRefuelable>().Fuel,"pending dialog does not debit");
            caravan.pawns.Remove(animal); // Same caller roster now omits a real former caravan member.
            coreReturnConfirmation!();coreReturnConfirmation!();
            Check.Equal(100f,core.parent.GetComp<CompRefuelable>().Fuel,"stale confirmation is rejected without payment");
            Check.Equal(false,w.Caster.Spawned,"no partial move from stale confirmation");
            Check.Equal(false,caravan.Destroyed,"original caravan survives stale confirmation");return;
        }
        bool success=CoreReturnUtility.TryReturn(core,roster,out var why); RuntimeLog.AssertClean(); Check.Equal(true,success,"native caravan return "+why);
        Check.Equal(whole?70f:90f,core.parent.GetComp<CompRefuelable>().Fuel,"all species charged, cargo free");
        Check.Same(cargo,w.Caster.carryTracker.CarriedThing,"same carried cargo arrived");
        Check.Same(pack,animal.inventory.innerContainer.Single(),"pack stack stays with animal");Check.Equal(33,pack.stackCount,"all pack cargo retained");
        Check.Equal(false,Find.WorldPawns.Contains(w.Caster),"arrived colonist left native WorldPawns");
        Check.Equal(!whole,Find.WorldPawns.Contains(animal),"animal world membership matches ownership");
        Check.Equal(whole,caravan.Destroyed,"only fully evacuated caravan destroyed");
        Check.Equal(whole?0:2,caravan.PawnsListForReading.Count,"single leaves animal and prisoner owned by original caravan");
        Check.Equal(whole,animal.Spawned,"whole return brings animal");Check.Equal(whole,prisoner.Spawned,"whole return brings prisoner");
    }
    static bool CoreReturnAllSpawn(Thing newThing,IntVec3 loc,Map map,ref Thing __result)
    {
        Check.Equal(null,newThing.holdingOwner,"native caravan holder removed before spawn");
        if(newThing is Pawn pawn && Find.WorldPawns.Contains(pawn))Find.WorldPawns.RemovePawn(pawn); // Native Pawn.SpawnSetup world registration transition.
        Set(newThing,"positionInt",loc);Set(newThing,"mapIndexOrState",(sbyte)Find.Maps.IndexOf(map));map.thingGrid.Register(newThing);__result=newThing;return false;
    }
    static void CoreReturnCrossMap(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();var core=CoreReturnReady(w);
        var source=Blank<Map>();source.info=new MapInfo {Size=new IntVec3(20,1,20)};source.cellIndices=new CellIndices(source);source.thingGrid=new ThingGrid(source);
        Find.Maps.Add(source);w.Map.thingGrid.Deregister(w.Caster);Set(w.Caster,"mapIndexOrState",(sbyte)1);source.thingGrid.Register(w.Caster);
        using var move=CoreReturnMovementBoundary();
        Check.Equal(true,CoreReturnUtility.TryReturn(core,[w.Caster],out var why),"real cross map transaction "+why);
        Check.Same(w.Map,w.Caster.Map,"native destination map identity");Check.Equal(0,source.thingGrid.ThingsListAt(new IntVec3(17,0,17)).Count,"source map no longer owns pawn");
        Check.Equal(90f,core.parent.GetComp<CompRefuelable>().Fuel,"crossmap costs ten");
    }
    static void CoreReturnApproach(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();var core=CoreReturnCore(w);
        var job=new Job(new JobDef {defName="CoreReturnApproach"},core.parent);
        var driver=new JobDriver_ReceiveCoreReturnMark {pawn=w.Caster,job=job};
        var toils=((IEnumerable<Toil>)typeof(JobDriver_ReceiveCoreReturnMark).GetMethod("MakeNewToils",Flags)!.Invoke(driver,null)!).ToArray();
        Check.Equal(2,toils.Length,"native approach has walk and commit phases");
        Check.Equal(null,CompCoreReturn.FindMark(w.Caster),"constructing approach never grants remote mark");
        toils.Last().initAction();Check.Equal(true,core.IsBound(w.Caster),"arrival commit adds actual mark");
        toils.Last().initAction();Check.Equal(1,core.BoundPawns.Count(),"repeated arrival cannot duplicate mark");
        Set(w.Caster.guest,"hostFactionInt",new Faction {def=new FactionDef()});
        Check.Equal(false,core.IsBound(w.Caster),"foreign custody invalidates retained mark");Set(w.Caster.guest,"hostFactionInt",null);
        Set(w.Caster.health,"healthState",PawnHealthState.Down);
        Check.Equal(true,core.IsBound(w.Caster),"friendly downed pawn remains recallable");Check.Equal(false,core.CanMark(w.Caster),"downed pawn cannot start approach");
    }
    static bool CoreReturnLabel(ref string __result) {__result="return core";return false;}
    static bool CoreReturnSkipTexture(string itemPath, bool reportFailure, ref UnityEngine.Texture2D __result) {__result=null;return false;}
    static void CoreReturnGizmos(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();using var textures=new Patches();
        textures.Prefix(typeof(ContentFinder<UnityEngine.Texture2D>).GetMethod("Get",new[]{typeof(string),typeof(bool)})!,nameof(CoreReturnSkipTexture));
        var core=CoreReturnCore(w);Check.Equal(true,core.Bind(w.Caster),"mark bound before selection");
        var mark=CompCoreReturn.FindMark(w.Caster)!;
        // RimWorld 1.7: the vanilla Hediff.GetGizmos base returns null — the old
        // foreach over it threw NullReferenceException for every selected pawn and
        // vanilla's gizmo-grid guard wiped ALL of the pawn's commands (draft included).
        var gizmos=mark.GetGizmos().ToList();
        Check.Equal(2,gizmos.Count,"recall and unbind commands offered for a bound colonist");
        Set(w.Caster,"factionInt",new Faction {def=new FactionDef()});
        Check.Equal(0,mark.GetGizmos().ToList().Count,"foreign pawn receives no core commands");
    }
    static Action? coreReturnConfirmation;
    static bool CoreReturnWindowStack(ref WindowStack __result) {__result=new WindowStack();return false;}
    static bool CoreReturnCaptureDialog(Window window) {coreReturnConfirmation=((Dialog_MessageBox)window).buttonAAction;return false;}
    static void CoreReturnRetry(WorldState w)
    {
        using var def=CoreReturnDef();using var health=CoreReturnHealthBoundary();var core=CoreReturnCore(w,0);
        Check.Equal(true,core.Bind(w.Caster),"mark can be acquired while unfunded");core.TickMaintenance(0);
        Check.Equal(false,core.MaintenancePaid,"failed maintenance disables recall");Check.Equal(true,core.IsBound(w.Caster),"failed upkeep retains mark");
        var fuel=core.parent.GetComp<CompRefuelable>();Set(fuel,"fuel",1f);
        for(int i=1;i<60;i++)core.TickMaintenance(i);
        Check.Equal(1f,fuel.Fuel,"unpaid retry waits60ticks instead of scanning each tick");
        core.TickMaintenance(60);Check.Equal(true,core.MaintenancePaid,"next bounded retry restores recall");Check.Equal(0f,fuel.Fuel,"retry funds fractional credit once");
    }
    static void CoreReturnFuel(WorldState w)
    {
        var core=CoreReturnCore(w,9); var fuel=core.parent.GetComp<CompRefuelable>();
        Check.Equal(false,CoreReturnUtility.TryConsumeFuel(core.parent,10),"insufficient native fuel rejects batch");
        Check.Equal(9f,fuel.Fuel,"insufficient payment leaves all source fuel intact");
        Check.Equal(true,CoreReturnUtility.TryConsumeFuel(core.parent,9),"exact native balance pays");
        Check.Equal(0f,fuel.Fuel,"exact payment debited once");
        Check.Equal(false,CoreReturnUtility.TryConsumeFuel(core.parent,1),"empty source rejects further payment");
    }
}

internal static partial class OdysseyBeaconTests
{
    public static void CoreReturnNetworkCase(RuntimeFixture f) => Run(f,()=>
    {
        var world=(RimWorld.Planet.World)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
        world.factionManager=new FactionManager();world.factionManager.AllFactionsListForReading.Add(faction);Set(world.factionManager,"ofPlayer",faction);Set(Current.Game,"worldInt",world);
        Make("ManaBeacon",10,10);
        var core=Make("ManaNetworkEndpoint",10,10);((CompManaNetworkEndpoint)core).FuelMode=ManaNetworkMode.Receive;
        var local=AddFuel(core,5);var source=Tank(11,10);source.parent.def.defName="TM_MagofuelReservoir";var remote=AddFuel(source,20);
        var off=Tank(10,11);off.parent.def.defName="TM_MagofuelReservoir";var offFuel=AddFuel(off,100);((CompManaNetworkEndpoint)off).FuelMode=ManaNetworkMode.Off;
        Check.Equal(25f,CoreReturnUtility.AvailableFuel(core.parent),"core plus connected Send reservoir only");
        Check.Equal(false,CoreReturnUtility.TryConsumeFuel(core.parent,26),"network rejects insufficient whole debit");
        Check.Equal(5f,local.Fuel,"failed network debit preserves local");Check.Equal(20f,remote.Fuel,"failed network debit preserves remote");
        Check.Equal(true,CoreReturnUtility.TryConsumeFuel(core.parent,15),"native connected fuel payment");
        Check.Equal(0f,local.Fuel,"local core consumed first");Check.Equal(10f,remote.Fuel,"remaining charge taken once from connected Send reservoir");Check.Equal(100f,offFuel.Fuel,"off source untouched");
        Set(source.parent,"factionInt",new Faction {def=new FactionDef()});
        Check.Equal(0f,CoreReturnUtility.AvailableFuel(core.parent),"foreign reservoir cannot supply core");
    });
}