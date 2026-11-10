using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;
internal static partial class OdysseyGrandDomeTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.GrandDome.DayAndSplitIntervals", f => Run(Day));
        yield return new("Odyssey.GrandDome.StandbyLifecycleAndExpiry", f => Run(Lifecycle));
        yield return new("Odyssey.GrandDome.NativeRefillAndCallbackOwnership", f => Run(Refill));
        yield return new("Odyssey.GrandDome.RealScribeAndOldSave", f => Run(() => Save(f)));
        yield return new("Odyssey.GrandDome.RitualPersonalManaAndCancellation", f => Run(() => Ritual(f)));
        yield return new("Odyssey.GrandDome.RitualCapacityDropKeepsMana", _ => Run(() =>
        {
            var dome = Dome(); var pawn = Mage(dome); var mana = pawn.needs.TryGetNeed<Need_Mana>();
            var magic = pawn.GetCompAbilityUserMagic(); magic.maxMP = .4f;
            Check.Equal(false, (bool)Call(dome,"CanActivate",pawn,true), "reject mana setter clamping before ritual");
            Check.Equal(false, (bool)Call(dome,"CompleteActivation",pawn), "completion rechecks actual payment");
            Near(1, mana.CurLevel, "no excess debit after capacity reduction");
            Check.Equal(false, Get<bool>(dome,"Armed"), "unpaid dome stays unarmed");
        }));
        yield return new("Odyssey.GrandDome.CallbackFailureAndExhaustion", f => Run(Callback));
        yield return new("Odyssey.GrandDome.NativeConsumerBeforeSourceTick", f => Run(Ordering));
        yield return new("Odyssey.GrandDome.CallbackOwnershipLossPreservesCredit", f => Run(CallbackOwnership));
        yield return new("Odyssey.FieldTicks.NativeConsumerBoundaryAndReceipts", f => Run(FieldTicks));
        yield return new("Odyssey.FieldTicks.NativeCallbackFailureKeepsDispatching", f => Run(FieldFault));
        yield return new("Odyssey.GrandDome.MapReceiptSurvivesSameTickHandover", f => Run(() => Handover(f)));
        yield return new("Odyssey.GrandDome.LowFuelWarningOnceAndRefillReset", f => Run(() => LowWarning(f)));
    }
    static Type T(string n) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + n) ?? throw new Exception("Grand dome implementation missing: " + n);
    static void Set(object o, string n, object v) { for(Type? t=o.GetType();t!=null;t=t.BaseType) { var p=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly); if(p!=null) {p.SetValue(o,v);return;} } throw new Exception(n); }
    static object Call(object o,string n,params object[] a) => o.GetType().GetMethod(n)!.Invoke(o,a)!;
    static V Get<V>(object o,string n) => (V)o.GetType().GetProperty(n)!.GetValue(o)!;
    static void Near(double e,double a,string why) { if(Math.Abs(e-a)>1e-8) throw new Exception($"{why}: {e} != {a}"); }
    static Map map=null!; static MapComponent field=null!; static Faction faction=null!; static int id;
    static void Run(Action action)
    {
        var old=Current.Game; var flag=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.Static|BindingFlags.NonPublic)!;var oldFlag=flag.GetValue(null);
        var culture=System.Globalization.CultureInfo.CurrentCulture;System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
        using var patches=new Patches();
        var oldLanguage=LanguageDatabase.activeLanguage;var oldDefault=LanguageDatabase.defaultLanguage;
        try {
            var language=(LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));language.folderName="English";Set(language,"dataIsLoaded",true);Set(language,"workerInt",new LanguageWorker_English());language.keyedReplacements=new Dictionary<string,LoadedLanguage.KeyedReplacement>();
            foreach(var key in new[]{"TM_GrandDomeExhausted","TM_GrandDomeLowFuel","TM_GrandDomeActive","TM_GrandDomeStandby","TM_GrandDomeUnarmed"})language.keyedReplacements[key]=new LoadedLanguage.KeyedReplacement{key=key,value=key};LanguageDatabase.activeLanguage=LanguageDatabase.defaultLanguage=language;
            var mods=LoadedModManager.RunningModsListForReading.ToArray();
            try { LoadedModManager.RunningModsListForReading.Clear();RuntimeHelpers.RunClassConstructor(typeof(CompRefuelable).TypeHandle); }
            finally { LoadedModManager.RunningModsListForReading.AddRange(mods); }
            flag.SetValue(null,true); Current.Game=(Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));Set(Current.Game,"tickManager",new TickManager()); Set(Current.Game,"uniqueIDsManager",new UniqueIDsManager()); Current.Game.Scenario=new Scenario();
            map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));map.info=new MapInfo{Size=new IntVec3(60,1,60),isPocketMap=true};map.pocketTileInfo=new SurfaceTile();Set(map.pocketTileInfo,"biome",new BiomeDef{inVacuum=true});Set(Current.Game,"maps",new List<Map>{map});
            map.cellIndices=new CellIndices(map);map.edificeGrid=new EdificeGrid(map);map.roofGrid=new RoofGrid(map);map.thingGrid=new ThingGrid(map);
            map.regionDirtyer=new RegionDirtyer(map);map.regionAndRoomUpdater=new RegionAndRoomUpdater(map);Set(map.regionAndRoomUpdater,"initialized",true);map.regionGrid=new RegionGrid(map);
            field=(MapComponent)Activator.CreateInstance(T("MapComponent_GrandAtmosphereDome"),map)!;map.components=new List<MapComponent>{field};faction=new Faction{def=new FactionDef{isPlayer=true}};id=100;action();
        } finally { saved=null;Current.Game=old;flag.SetValue(null,oldFlag);System.Globalization.CultureInfo.CurrentCulture=culture;LanguageDatabase.activeLanguage=oldLanguage;LanguageDatabase.defaultLanguage=oldDefault; }
    }
    static ThingComp Dome(float fuel=200)
    {
        var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));def.defName="DomeTest";def.size=new IntVec2(3,3);def.hasInteractionCell=true;def.interactionCellOffset=new IntVec3(0,0,-2);def.race=new RaceProperties();
        var b=new Building{def=def,thingIDNumber=++id};Set(b,"comps",new List<ThingComp>());Set(b,"mapIndexOrState",(sbyte)0);Set(b,"positionInt",new IntVec3(20,0,20));Set(b,"factionInt",faction);
        var tank=new CompRefuelable{parent=b,props=new CompProperties_Refuelable{fuelCapacity=200,fuelConsumptionRate=0}};Set(tank,"fuel",fuel);b.AllComps.Add(tank);
        var c=(ThingComp)Activator.CreateInstance(T("CompGrandAtmosphereDome"))!;c.parent=b;c.props=(CompProperties)Activator.CreateInstance(T("CompProperties_GrandAtmosphereDome"))!;b.AllComps.Add(c);c.PostSpawnSetup(false);return c;
    }
    static CompRefuelable Fuel(ThingComp c)=>c.parent.GetComp<CompRefuelable>();
    static double Total(ThingComp c)=>Fuel(c).Fuel+Get<double>(c,"PaidFuelCredit");
    static void Tick(ThingComp c,int now,int interval=1) { Set(Find.TickManager,"ticksGameInt",now);if(interval==1)c.CompTick();else c.CompTickInterval(interval); }
    static float Vacuum(int x=0,int z=0)=> new IntVec3(x,0,z).GetVacuum(map);
    static void Arm(ThingComp c)=>Set(c,"armed",true);
    static void Day()
    {
        var c=Dome();Near(1,Vacuum(),"old building unarmed");Arm(c);
        for(int i=0;i<60000;i++)Tick(c,i);
        Near(100,Total(c),"one day costs exactly100 physical fuel");Near(100,Fuel(c).Fuel,"native store remains integral");Near(0,Vacuum(59,59),"whole map covered");
        Tick(c,59999);c.CompTickInterval(60);Near(100,Total(c),"duplicate callbacks cannot charge");
        var d=Dome();Set(c,"armed",false);Arm(d);int now=60000;foreach(int length in new[]{1,59,61,599,1200,58080}) {Tick(d,now,length);now+=length;}
        Near(100,Total(d),"split intervals same cost");Set(Find.TickManager,"ticksGameInt",now);Near(1,Vacuum(),"no free overdue interval");Tick(d,now+10000);Near(100-1d/600,Total(d),"no missed tick catchup");
    }
    static void Lifecycle()
    {
        var a=Dome();var b=Dome();Arm(a);Arm(b);Tick(b,0);Near(200,Total(b),"higherID standby");Tick(a,0);Near(0,Vacuum(),"active paid");
        var flick=new CompFlickable{parent=a.parent,props=new CompProperties_Flickable()};a.parent.AllComps.Add(flick);Set(flick,"switchOnInt",false);Near(1,Vacuum(),"manualoff invalidates immediately");Check.Equal(false,Get<bool>(a,"Armed"),"manualoff disarms");var credit=Get<double>(a,"PaidFuelCredit");Tick(b,1);Near(0,Vacuum(),"standby takes over with fresh payment");Near(credit,Get<double>(a,"PaidFuelCredit"),"off preserves paid residue");
        Set(b.parent,"factionInt",new Faction{def=new FactionDef()});Near(1,Vacuum(),"ownership loss ends field");Check.Equal(false,Get<bool>(b,"Armed"),"ownership loss disarms");
        Set(flick,"switchOnInt",true);Tick(a,2);Near(1,Vacuum(),"switchon needs new ritual");
        Arm(a);Tick(a,3);a.PostDeSpawn(map);Check.Equal(false,Get<bool>(a,"Armed"),"despawn disarms");Near(1,Vacuum(),"despawn ends field");
    }
    static void Refill()
    {
        var c=Dome();Arm(c);Tick(c,0);Near(199,Fuel(c).Fuel,"whole fuel prepaid");Fuel(c).Refuel(1);Near(200+599d/600,Total(c),"native refill loses no fractional fuel");
        var d=Dome(1);Set(c,"armed",false);Arm(d);for(int i=1;i<=600;i++)Tick(d,i);Near(0,Total(d),"single unit lasts600ticks");
        // Exhaustion notification is presentation only; inspect the persistent warning before exhaustion.
        Check.Equal(true,Get<bool>(d,"LowFuel"),"low fuel warning persists in inspect");
    }
    static ThingComp? saved;
    public sealed class ThrowSignal:ThingComp
    {
        public ThingComp Dome=null!;
        public override void ReceiveCompSignal(string signal) { if(signal=="RanOutOfFuel") {Dome.CompTick();throw new InvalidOperationException("callback failed after native debit");} }
    }
    public sealed class OwnershipSignal:ThingComp
    {
        public override void ReceiveCompSignal(string signal) { if(signal=="RanOutOfFuel")Set(parent,"factionInt",new Faction{def=new FactionDef()}); }
    }
    static void CallbackOwnership()
    {
        var c=Dome(1);c.parent.AllComps.Add(new OwnershipSignal{parent=c.parent});Arm(c);Tick(c,0);Near(1,Total(c),"ownership loss during payment preserves all paidfuel");Near(1,Vacuum(),"ownership callback cannot grant field");Check.Equal(false,Get<bool>(c,"Armed"),"ownership callback disarms");
    }
    static void Callback()
    {
        var c=Dome(1);c.parent.AllComps.Add(new ThrowSignal{parent=c.parent,Dome=c});Arm(c);
        try{Tick(c,0);throw new Exception("expected callback");}catch(InvalidOperationException ex) when(ex.Message=="callback failed after native debit"){}
        Near(0,Fuel(c).Fuel,"native debit survived thrown callback");Near(1,Get<double>(c,"PaidFuelCredit"),"paid credit survives callback");Near(1,Vacuum(),"failed callback grants no field");Tick(c,0);Near(1,Total(c),"failed receipt not replayed");
        for(int i=1;i<=600;i++)Tick(c,i);Near(0,Total(c),"paidcredit spent once");Near(0,Vacuum(),"last paidtick still protects");Tick(c,601);Check.Equal(false,Get<bool>(c,"Armed"),"exhaustion disarms");Near(1,Vacuum(),"exhaustion collapses field");Check.Equal(1,messages,"one collapse message");Tick(c,602);Check.Equal(1,messages,"no repeated collapse message");Fuel(c).Refuel(1);Tick(c,603);Near(1,Vacuum(),"refill alone cannot rearm");
    }
    static void Ordering()
    {
        var c=Dome();Arm(c);Tick(c,20);Set(Find.TickManager,"ticksGameInt",20);
        Set(Find.TickManager,"ticksGameInt",21);Call(field,"PrepareTick",21);
        Near(0,Vacuum(),"next tick consumer before source sees paid coverage");double before=Total(c);Tick(c,21);Near(before,Total(c),"native source callback cannot repeat prepayment");Near(200-2d/600,Total(c),"two actual ticks paid exactly");
        c.parent.def.passability=Traversability.Impassable;c.parent.def.fillPercent=1f;c.parent.def.building=new BuildingProperties();map.edificeGrid.Register((Building)c.parent);
        Near(0,FireUtility.GetEffectiveVacuumForFire(c.parent.Position,map),"native unroofed wallfire receives global field");Near(0,FireUtility.GetEffectiveVacuumForFire(new IntVec3(59,0,59),map),"native distant floor fire receives global field");Check.Equal<RoofDef?>(null,c.parent.Position.GetRoof(map),"dome never creates roof");
    }
    static MapComponent AddField(string name)
    {
        var c=(MapComponent)Activator.CreateInstance(T("MapComponent_"+name),map)!;map.components.Add(c);return c;
    }
    static ThingComp AddComp(ThingWithComps parent,string name)
    {
        var c=(ThingComp)Activator.CreateInstance(T("Comp"+name))!;c.parent=parent;c.props=(CompProperties)Activator.CreateInstance(T("CompProperties_"+name))!;parent.AllComps.Add(c);return c;
    }
    static ThingComp Source(string name,string bank)
    {
        var parent=Dome().parent;var buffer=AddComp(parent,bank);Set(buffer,"storedMana",10f);var c=AddComp(parent,name);c.PostSpawnSetup(false);return c;
    }
    public sealed class ReadConsumer:ThingComp
    {
        public Action Read=null!;public int Reads;
        public override void CompTick(){Read();Reads++;}
    }
    static (TickList,ReadConsumer) NativeList(Action read)
    {
        var definition=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));definition.defName="NativeTickConsumer";definition.tickerType=TickerType.Normal;var parent=new ThingWithComps{def=definition,thingIDNumber=++id};var c=new ReadConsumer{parent=parent,Read=read};Set(parent,"comps",new List<ThingComp>{c});var list=new TickList(TickerType.Normal);list.RegisterThing(parent);return(list,c);
    }
    static void FieldTicks()
    {
        AddField("ManaNetwork");var air=AddField("MagicalAtmosphere");var thermal=AddField("MagicalThermal");var combat=AddField("MagicalCombat");var dome=Dome();Arm(dome);
        var oxygen=Source("MagicalOxygen","OxygenManaBuffer");var heater=Source("MagicalThermal","ShieldManaBuffer");var shield=Source("MagicalCombat","ShieldManaBuffer");Set(shield,"currentHitPoints",1000f);
        float airRead=-1,tempRead=-1,grandRead=-1;bool combatRead=false;
        var(list,consumer)=NativeList(()=>{airRead=(float)Call(air,"EffectiveVacuum",new IntVec3(20,0,20),1f);tempRead=(float)Call(thermal,"EffectiveTemperature",new IntVec3(20,0,20),-100f);combatRead=(bool)Call(combat,"Covers",new UnityEngine.Vector3(20,0,20));grandRead=Vacuum();});
        foreach(int tick in new[]{60,120}) {Set(Find.TickManager,"ticksGameInt",tick);list.Tick();Near(0,airRead,"local atmosphere paid before native consumer at boundary");Near(21,tempRead,"thermal paid before native consumer at boundary");Check.Equal(true,combatRead,"combat paid before native consumer at boundary");Near(0,grandRead,"global dome paid before native consumer");}
        var banks=new[]{oxygen,heater,shield}.Select(c=>c.parent.AllComps.Single(x=>x.GetType().Name.EndsWith("ManaBuffer"))).ToArray();var before=banks.Select(b=>Get<float>(b,"StoredMana")).ToArray();double total=Total(dome);
        new TickList(TickerType.Rare).Tick();new TickList(TickerType.Long).Tick();foreach(var c in map.components)c.MapComponentTick();dome.CompTick();dome.CompTickInterval(1);
        for(int i=0;i<banks.Length;i++)Near(before[i],Get<float>(banks[i],"StoredMana"),"repeated dispatch and map posttick do not repay");Near(total,Total(dome),"grand receipt survives all callback routes");Check.Equal(2,consumer.Reads,"native dispatcher ran each consumer");
        Set(Find.TickManager,"ticksGameInt",2120);list.Tick();Near(total-1d/600,Total(dome),"large tickjump pays one actual dome tick");Near(0,airRead,"fresh bounded local pulse after jump");
    }
    public sealed class FaultNetwork:TorannMagic.Odyssey.MapComponent_ManaNetwork
    {
        public FaultNetwork(Map map):base(map){}
        public override void MapComponentTick()=>throw new InvalidOperationException("injected network callback failure");
    }
    static void FieldFault()
    {
        map.components.Add(new FaultNetwork(map));var later=AddField("MagicalThermal");var first=Dome(1);Arm(first);first.parent.AllComps.Add(new ThrowSignal{parent=first.parent,Dome=first});var firstMap=map;
        map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));map.info=new MapInfo{Size=new IntVec3(60,1,60),isPocketMap=true};map.pocketTileInfo=new SurfaceTile();Set(map.pocketTileInfo,"biome",new BiomeDef{inVacuum=true});map.components=new List<MapComponent>();var secondField=AddField("GrandAtmosphereDome");Find.Maps.Add(map);var second=Dome();Set(second.parent,"mapIndexOrState",(sbyte)1);second.PostSpawnSetup(false);Arm(second);
        int reads=0;var(list,consumer)=NativeList(()=>reads++);Set(Find.TickManager,"ticksGameInt",60);list.Tick();Check.Equal(1,reads,"native consumer still dispatched after fuel callback throws");Near(200-1d/600,Total(second),"later map still paid");Near(1,Total(first),"failed source retains prepaid fuel");
        Check.Equal(60,(int)later.GetType().GetField("lastPulse",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(later)!,"later same-map component still ticks after network fault");
        var problems=(List<string>)typeof(RuntimeLog).GetField("Problems",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check.Equal(2,problems.Count,"two expected isolated diagnostics");Check.Equal(true,problems.Any(p=>p.Contains("callback failed after native debit")),"diagnostic preserves actual fuel callback failure");Check.Equal(true,problems.Any(p=>p.Contains("injected network callback failure")),"diagnostic preserves network failure");RuntimeLog.Clear();
    }
    static void RoundTrip(RuntimeFixture f,ThingComp source,string name)
    {
        saved=source;var save=new DomeSave();string path=Path.Combine(f.WorkingDirectory,name+".xml");try{Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref save,"dome");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
        Set(field,"reservedUntil",-1L);Set(source,"lowFuelWarned",false);
        try{Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref save,"dome");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
    }
    static void Handover(RuntimeFixture f)
    {
        var a=Dome();var b=Dome();Arm(b);Tick(b,10,60);Arm(a);Call(field,"InvalidateSelection");Tick(a,10);Near(200,Total(a),"lowerID activation cannot repay same map tick");Near(0,Vacuum(),"previous paid owner finishes interval without a field gap");RoundTrip(f,b,"map-receipt");Tick(a,69);Near(200,Total(a),"savedmap receipt cannot repay purchased interval");Tick(a,70);Near(200-1d/600,Total(a),"handover purchases first fresh interval");
    }
    static void LowWarning(RuntimeFixture f)
    {
        var c=Dome(10);Arm(c);Tick(c,0);Check.Equal(1,lowMessages,"warn automatically when crossing below0.1day");RoundTrip(f,c,"low-warning");Tick(c,1);Check.Equal(1,lowMessages,"savedwarning not spammed");Fuel(c).Refuel(1);Tick(c,2);Check.Equal(1,lowMessages,"refill clears low state");Tick(c,3,598);Check.Equal(2,lowMessages,"later threshold crossing warns again");
    }
    public sealed class DomeSave:IExposable { public void ExposeData(){saved!.PostExposeData();field.ExposeData();} }
    static void Save(RuntimeFixture f)
    {
        var c=Dome();Arm(c);Tick(c,20);saved=c;var save=new DomeSave();var path=Path.Combine(f.WorkingDirectory,"grand-dome.xml");
        try{Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref save,"dome");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
        Call(field,"Unregister",c);var d=Dome(Fuel(c).Fuel);saved=d;
        try{Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref save,"dome");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
        Near(Total(c),Total(d),"credit survives nativeScribe");Check.Equal(true,Get<bool>(d,"Armed"),"armed persists");Near(1,Vacuum(),"reload has no unpaid coverage");Tick(d,20);Near(Total(c),Total(d),"receipt prevents replay");Tick(d,21);Near(0,Vacuum(),"fresh paidtick restores field");
        var xml=System.Xml.Linq.XDocument.Load(path);foreach(var e in xml.Descendants().Where(e=>e.Name.LocalName.StartsWith("TM_grandDome")).ToList())e.Remove();xml.Save(path);
        try{Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref save,"dome");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}Check.Equal(false,Get<bool>(d,"Armed"),"oldsave unarmed");Near(1,Vacuum(),"oldsave no field");
    }
    static JobDriver SaveDriver(RuntimeFixture f,JobDriver source,string name)
    {
        var path=Path.Combine(f.WorkingDirectory,name+".xml");var savedDriver=source;
        try{Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref savedDriver,"driver");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
        JobDriver loaded=null!;try{Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref loaded,"driver");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}loaded.pawn=source.pawn;loaded.job=source.job;return loaded;
    }
    static Toil RitualToil(JobDriver driver)=>((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).Last();
    static Pawn Mage(ThingComp c)
    {
        var pawn=new Pawn{def=c.parent.def,thingIDNumber=++id};pawn.health=new Pawn_HealthTracker(pawn);pawn.needs=new Pawn_NeedsTracker(pawn);pawn.mindState=(Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));pawn.mindState.mentalStateHandler=new MentalStateHandler(pawn);pawn.story=new Pawn_StoryTracker(pawn){traits=new TraitSet(pawn)};pawn.ageTracker=new Pawn_AgeTracker(pawn);Set(pawn.ageTracker,"ageBiologicalTicksInt",72000000L);Set(pawn,"factionInt",faction);Set(pawn,"mapIndexOrState",(sbyte)0);Set(pawn,"positionInt",c.parent.InteractionCell);
        var magic=new CompAbilityUserMagic{parent=pawn};Set(pawn,"comps",new List<ThingComp>{magic});pawn.story.traits.allTraits.Add(new Trait(new TraitDef{defName="Faceless"}));var mana=(Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana));Set(mana,"pawn",pawn);mana.def=new NeedDef{defName="TM_Mana"};pawn.needs.AllNeeds.Add(mana);mana.CurLevel=1;
        pawn.jobs=new Pawn_JobTracker(pawn);return pawn;
    }
    static void Ritual(RuntimeFixture f)
    {
        var c=Dome();var pawn=Mage(c);var mana=pawn.needs.TryGetNeed<Need_Mana>();map.reservationManager=new ReservationManager(map);map.physicalInteractionReservationManager=new PhysicalInteractionReservationManager();map.events=new MapEvents(map);
        Check.Equal(true,(bool)Call(c,"CanActivate",pawn,true),"mage valid");var driver=(JobDriver)Activator.CreateInstance(T("JobDriver_ActivateGrandDome"))!;driver.pawn=pawn;driver.job=new Job(new JobDef{defName="DomeRitual"},c.parent);
        Check.Equal(true,driver.TryMakePreToilReservations(false),"native reservation succeeds for first mage");var second=Mage(c);var other=(JobDriver)Activator.CreateInstance(T("JobDriver_ActivateGrandDome"))!;other.pawn=second;other.job=new Job(new JobDef{defName="OtherDomeRitual"},c.parent);Check.Equal(false,other.TryMakePreToilReservations(false),"native reservation excludes concurrent second mage");
        var toils=((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(driver,null)!).ToList();var ritual=toils.Last();ritual.initAction();Near(1,mana.CurLevel,"start costs nothing");for(int i=0;i<3599;i++){Set(Find.TickManager,"ticksGameInt",i);ritual.tickAction();}Near(1,mana.CurLevel,"cancel before completion costs nothing");Check.Equal(false,Get<bool>(c,"Armed"),"pendingritual not armed");
        driver=SaveDriver(f,driver,"pending-ritual");ritual=RitualToil(driver);ritual.initAction();ritual.tickAction();Check.Equal(false,Get<bool>(c,"Armed"),"reload at same tick cannot replay work");Near(1,mana.CurLevel,"pending save nocharge");
        mana.CurLevel=.29f;Check.Equal(false,(bool)Call(c,"CanActivate",pawn,true),"20percent reserve after10percentcost");mana.CurLevel=1;Set(pawn,"positionInt",new IntVec3(50,0,50));Check.Equal(false,(bool)Call(c,"CompleteActivation",pawn),"remote cannot complete");Set(pawn,"positionInt",c.parent.InteractionCell);
        Set(Find.TickManager,"ticksGameInt",3599);ritual.tickAction();Check.Equal(true,Get<bool>(c,"Armed"),"3600actualticks arm");if(Math.Abs(mana.CurLevel-.9f)>.000001f)throw new Exception("tenpercent mana not paid");Check.Equal(false,(bool)Call(c,"CompleteActivation",pawn),"repeatcompletion cannot repay");
        driver=SaveDriver(f,driver,"completed-ritual");ritual=RitualToil(driver);ritual.initAction();
        Set(c,"armed",false);ritual.tickAction();Near(.9f,mana.CurLevel,"completed job callback cannot reactivate after collapse");Check.Equal(false,Get<bool>(c,"Armed"),"completed job stays completed");
    }
    static bool Skip()=>false;
    static int messages,lowMessages;
    static bool Message(string __0){if(__0.Contains("TM_GrandDomeLowFuel"))lowMessages++;else messages++;return false;}
    static bool NativeAir(ref float temperature,ref bool __result){temperature=-100f;__result=true;return false;}
    sealed class Patches:IDisposable
    {
        readonly Assembly asm=Assembly.Load("0Harmony");readonly object harmony;readonly string id="rwom.dome."+Guid.NewGuid();
        public Patches()
        {
            messages=lowMessages=0;harmony=Activator.CreateInstance(asm.GetType("HarmonyLib.Harmony")!,id)!;
            Prefix(typeof(ContentFinder<UnityEngine.Texture2D>).GetMethod("Get",new[]{typeof(string),typeof(bool)})!);
            Prefix(typeof(SolidColorMaterials).GetMethod("SimpleSolidColorMaterial",new[]{typeof(UnityEngine.Color),typeof(bool)})!);
            Prefix(typeof(JobDriver).GetMethod("EndJobWith")!);
            Prefix(typeof(Messages).GetMethod("Message",new[]{typeof(string),typeof(LookTargets),typeof(MessageTypeDef),typeof(bool)})!,nameof(Message));
            Postfix(typeof(VacuumUtility).GetMethod(nameof(VacuumUtility.GetVacuum))!,T("MagicalAtmosphereVacuumPatch"));
            Postfix(typeof(FireUtility).GetMethod(nameof(FireUtility.GetEffectiveVacuumForFire))!,T("MagicalAtmosphereFirePatch"));
            Prefix(typeof(GenTemperature).GetMethod("TryGetDirectAirTemperatureForCell")!,nameof(NativeAir));
            var tickPatch=typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.MagicalFieldTickPatch");
            if(tickPatch!=null){var hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,tickPatch.GetMethod("Prefix")!)!;harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{typeof(TickList).GetMethod("Tick")!,hm,null,null,null});}
        }
        void Postfix(MethodBase method,Type type)
        {
            var hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,type.GetMethod("Postfix")!)!;harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{method,null,hm,null,null});
        }
        void Prefix(MethodBase method,string name=nameof(Skip))
        {
            var hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,typeof(OdysseyGrandDomeTests).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{method,hm,null,null,null});
        }
        public void Dispose()=>harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,new object[]{id});
    }
}
