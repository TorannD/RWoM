
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyPowerTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Power.PaidIntervalsAndNativeNet", f => WithMap(f, Intervals));
        yield return new("Odyssey.Power.StopsWithoutBurn", f => WithMap(f, Stops));
        yield return new("Odyssey.Power.ScribeKeepsPaidResidue", f => WithMap(f, Save));
        yield return new("Odyssey.Power.NativeTickChargesBattery", NativeTick);
        yield return new("Odyssey.Power.WholeRefillAndTransfer", f => WithMap(f, Refill));
        yield return new("Odyssey.Power.PaymentCallbacksPreserveCredit", f => WithMap(f, PaymentCallbacks));
    }
    static void WithMap(RuntimeFixture f, Action<RuntimeFixture> test)
    {
        var oldGame=Current.Game; var oldCulture=System.Globalization.CultureInfo.CurrentCulture; System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
        try {
            Current.Game=(Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game,"maps",new List<Map>{(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map))});
            Set(Current.Game,"uniqueIDsManager",new UniqueIDsManager());
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));world.factionManager=new FactionManager();Set(Current.Game,"worldInt",world);
            test(f);
        } finally { Current.Game=oldGame; System.Globalization.CultureInfo.CurrentCulture=oldCulture; }
    }
    static void Fill(GeneratorSave save, float fuel)
    {
        Set(save.Power.parent,"mapIndexOrState",(sbyte)-1);save.Fuel.Refuel(fuel);Set(save.Power.parent,"mapIndexOrState",(sbyte)0);
    }
    static Type GeneratorType => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.CompPowerTrader_Magofuel") ?? throw new Exception("Magofuel generator not implemented");
    static void Set(object o,string n,object v) { for(Type? t=o.GetType();t!=null;t=t.BaseType) { var f=t.GetField(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly); if(f!=null) { f.SetValue(o,v);return;} } throw new Exception(n); }
    static double Credit(CompPowerTrader c) => (double)GeneratorType.GetProperty("PaidFuel")!.GetValue(c)!;
    static double Advance(CompPowerTrader c,int ticks) => (double)GeneratorType.GetMethod("ProduceForTicks")!.Invoke(c,new object[]{ticks})!;
    static void Near(double a,double b,string why) { if(Math.Abs(a-b)>0.00001) throw new Exception($"{why}: expected {a}, actual {b}"); }
    public sealed class GeneratorSave : IExposable
    {
        public CompPowerTrader Power;
        public CompRefuelable Fuel;
        public CompFlickable Flick;
        public CompBreakdownable Breakdown;
        public GeneratorSave()
        {
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName="TestMagofuelGenerator";
            var parent=new Building { def=def };
            var props=(CompProperties_Power)Activator.CreateInstance(typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.CompProperties_MagofuelPower")!)!;
            Set(props,"basePowerConsumption",-3000f);
            Set(props,"fuelPerDay",6f); // Deliberate rate for interval and fractional-credit arithmetic.
            Power=(CompPowerTrader)Activator.CreateInstance(GeneratorType)!; Power.parent=parent;Power.props=props;
            Fuel=new CompRefuelable {parent=parent,props=new CompProperties_Refuelable{fuelCapacity=50,fuelConsumptionRate=0,consumeFuelOnlyWhenUsed=true}};
            Flick=new CompFlickable{parent=parent,props=new CompProperties_Flickable()};
            Breakdown=new CompBreakdownable{parent=parent,props=new CompProperties_Breakdownable()};
            Set(parent,"comps",new List<ThingComp>{Fuel,Power,Flick,Breakdown});
            Set(Power,"powerOnInt",true); Set(parent,"mapIndexOrState",(sbyte)0);
            _=new PowerNet(new[]{Power});
        }
        public void ExposeData() { Fuel.PostExposeData();Power.PostExposeData(); }
    }
    static void Intervals(RuntimeFixture f)
    {
        _=GeneratorType;
        var a=new GeneratorSave();Fill(a,1);
        Near(25,Advance(a.Power,500),"500 ticks delivers 25 Wd");
        Near(0,a.Fuel.Fuel,"whole physical item debited first");Near(.95,Credit(a.Power),"unused paid fraction retained");
        Near(50,(double)GeneratorType.GetProperty("LastHeat")!.GetValue(a.Power)!,"heat scales with paid 500 ticks");
        Near(3000,a.Power.PowerOutput,"normal output");Near(.05,a.Power.PowerNet.CurrentEnergyGainRate(),"native net sums live output");
        var b=new GeneratorSave();Fill(b,1);double sum=0;for(int i=0;i<500;i++)sum+=Advance(b.Power,1);
        Near(25,sum,"partition invariant energy");Near(Credit(a.Power),Credit(b.Power),"partition invariant credit");
        Near(475,Advance(a.Power,10000),"partial final interval capped by paid fuel");Near(2850,a.Power.PowerOutput,"partial average output");Near(950,(double)GeneratorType.GetProperty("LastHeat")!.GetValue(a.Power)!,"partial interval emits only paid heat");Near(0,Advance(a.Power,1),"no extra free tick");Near(0,a.Power.PowerNet.CurrentEnergyGainRate(),"native net sees empty stop");
    }
    static void Refill(RuntimeFixture f)
    {
        _=GeneratorType;var a=new GeneratorSave();Fill(a,50);Advance(a.Power,500);
        var fuelDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); fuelDef.defName="TM_Magofuel";fuelDef.thingClass=typeof(Thing);fuelDef.stackLimit=75;fuelDef.destroyable=true;fuelDef.category=ThingCategory.Item;
        var stack=new Thing{def=fuelDef,stackCount=2};
        Set(a.Power.parent,"mapIndexOrState",(sbyte)-1);var maps=Find.Maps.ToList();Find.Maps.Clear();try { a.Fuel.Refuel(new List<Thing>{stack}); } finally { Find.Maps.AddRange(maps); }
        Near(1,stack.stackCount,"native refill consumes exactly whole deficit");Near(50,a.Fuel.Fuel,"tank physically full");Near(.95,Credit(a.Power),"refill retains paid partial item");
        var owner=new ThingOwner<Thing>();var destination=new ThingOwner<Thing>();owner.TryAdd(a.Power.parent);
        Check.Equal(true,owner.TryTransferToContainer(a.Power.parent,destination),"native owner transfer succeeds");
        Near(0,Advance(a.Power,100),"packed transfer cannot generate");Near(.95,Credit(a.Power),"move preserves residue");
        destination.Remove(a.Power.parent);Set(a.Power.parent,"mapIndexOrState",(sbyte)0);
        Near(475,Advance(a.Power,9500),"reinstalled object uses prior paid residue");Near(50,a.Fuel.Fuel,"move cannot duplicate debit");
        var day=new GeneratorSave();Fill(day,6);Near(3000,Advance(day.Power,60000),"six fuel pays one full day");Near(0,day.Fuel.Fuel,"day pays six whole units");Near(0,Credit(day.Power),"day consumes exact credit");
        var split=new GeneratorSave();Fill(split,6);double energy=0;for(int i=0;i<240;i++)energy+=Advance(split.Power,250);
        Near(3000,energy,"many intervals over six whole items agree");Near(0,split.Fuel.Fuel,"partition same payment");Near(0,Credit(split.Power),"partition same residue");
    }
    static void Stops(RuntimeFixture f)
    {
        _=GeneratorType;var a=new GeneratorSave();Fill(a,5);
        Set(a.Power.parent,"mapIndexOrState",(sbyte)-1);Near(0,Advance(a.Power,10),"unspawned stops");Set(a.Power.parent,"mapIndexOrState",(sbyte)0);
        var net=a.Power.transNet;a.Power.transNet=null;Near(0,Advance(a.Power,10),"disconnected stops");a.Power.transNet=net;
        Set(a.Flick,"switchOnInt",false);Near(0,Advance(a.Power,10),"flick off stops");Set(a.Flick,"switchOnInt",true);
        Set(a.Breakdown,"brokenDownInt",true);Near(0,Advance(a.Power,10),"broken stops");Set(a.Breakdown,"brokenDownInt",false);
        Near(5,a.Fuel.Fuel,"stops never debit");Near(0,Credit(a.Power),"stops never mint credit");
        Advance(a.Power,1);Near(4,a.Fuel.Fuel,"start debits one");
        Set(a.Power,"powerOnInt",false);Near(0,Advance(a.Power,1),"power off stops");Near(.9999,Credit(a.Power),"stop retains paid residue");
    }
    public sealed class FuelSignal : ThingComp
    {
        public Action OnEmpty = null!;
        public override void ReceiveCompSignal(string signal) { if (signal == "RanOutOfFuel") OnEmpty(); }
    }
    static void PaymentCallbacks(RuntimeFixture f)
    {
        var failed = new GeneratorSave(); Fill(failed, 1);
        failed.Power.parent.AllComps.Add(new FuelSignal { parent = failed.Power.parent, OnEmpty = () => throw new InvalidOperationException("payment callback") });
        try { Advance(failed.Power, 1); throw new Exception("expected callback failure"); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException error && error.Message == "payment callback") { }
        Near(0, failed.Fuel.Fuel, "native debit completed before callback failure");
        Near(1, Credit(failed.Power), "throwing callback retains whole prepaid unit");
        Near(0, failed.Power.PowerOutput, "throwing callback publishes no power");
        Near(.05, Advance(failed.Power, 1), "following tick spends retained credit");
        foreach (bool despawn in new[] { false, true })
        {
            var changed = new GeneratorSave(); Fill(changed, 1);
            changed.Power.parent.AllComps.Add(new FuelSignal { parent = changed.Power.parent, OnEmpty = () =>
            {
                if (despawn) Set(changed.Power.parent, "mapIndexOrState", (sbyte)-1);
                else Set(changed.Flick, "switchOnInt", false);
            } });
            Near(0, Advance(changed.Power, 1), "payment state change cannot produce");
            Near(1, Credit(changed.Power), "payment state change retains credit");
            Near(0, changed.Power.PowerOutput, "payment state change publishes no power");
            Near(0, (double)GeneratorType.GetProperty("LastHeat")!.GetValue(changed.Power)!, "payment state change publishes no heat");
        }
    }
    static bool SkipPresentation() => false;
    sealed class PresentationPatches : IDisposable
    {
        readonly Assembly assembly=Assembly.Load("0Harmony"); readonly object harmony; readonly string id="rwom.odyssey.generator.presentation."+Guid.NewGuid();
        public PresentationPatches()
        {
            harmony=Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!,id)!;
            foreach(var method in typeof(MapDrawer).GetMethods().Where(m=>m.Name=="MapMeshDirty")) Patch(method);
            Patch(typeof(CompPowerTrader).GetMethod("UpdateOverlays",BindingFlags.Instance|BindingFlags.NonPublic)!);
            Patch(typeof(Verse.Sound.SoundStarter).GetMethod(nameof(Verse.Sound.SoundStarter.PlayOneShot))!);
        }
        void Patch(MethodBase method) { var prefix=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,typeof(OdysseyPowerTests).GetMethod(nameof(SkipPresentation),BindingFlags.Static|BindingFlags.NonPublic)!)!; harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{method,prefix,null,null,null}); }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,new object[]{id});
    }
    static void NativeTick(RuntimeFixture f)
    {
        _=GeneratorType;var oldGame=Current.Game;
        try {
            Current.Game=(Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game,"tickManager",new TickManager());
            var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Set(Current.Game,"maps",new List<Map>{map});
            map.gameConditionManager=new GameConditionManager((RimWorld.Planet.World)null!);
            using var presentation=new PresentationPatches();
            map.mapDrawer=(MapDrawer)RuntimeHelpers.GetUninitializedObject(typeof(MapDrawer));
            map.powerNetManager=new PowerNetManager(map);
            var a=new GeneratorSave(); Fill(a,1);
            a.Power.parent.def.comps=new List<CompProperties>{a.Power.props,a.Fuel.props};
            Set(a.Power,"powerOnInt",false);a.Power.PostSpawnSetup(false);
            Check.Equal(true,a.Power.PowerOn,"native generator spawn starts power state");Near(0,a.Power.PowerOutput,"spawn grants no unpaid output");
            Set(a.Power.props,"heatPerSecond",0f); // Headless map has no temperature grid; heat arithmetic is covered separately.
            var net=a.Power.PowerNet; net.powerNetManager=new PowerNetManager(map);
            var battery=new CompPowerBattery{parent=new Building{def=a.Power.parent.def},props=new CompProperties_Battery{storedEnergyMax=1000,efficiency=1}};
            net.batteryComps.Add(battery);
            for(int tick=1;tick<=10;tick++) {
                Find.TickManager.DebugSetTicksGame(tick);a.Power.CompTick();
                double paid=Credit(a.Power);a.Power.CompTick();Near(paid,Credit(a.Power),"same tick cannot pay twice"); ((CompPowerPlant)a.Power).UpdateDesiredPowerOutput(); Near(paid,Credit(a.Power),"external native refresh cannot pay twice");
                net.PowerNetTick();
            }
            Near(.5,battery.StoredEnergy,"native tick distributes prepaid energy to real battery");
            Near(.999,Credit(a.Power),"ten ticks paid exactly");Near(0,a.Fuel.Fuel,"last physical item consumed before output without suppressing credit");
            Set(a.Power,"paidFuel",.00005d);Find.TickManager.DebugSetTicksGame(11);a.Power.CompTick();net.PowerNetTick();
            Near(.525,battery.StoredEnergy,"final half tick distributes half electricity");Near(1500,a.Power.PowerOutput,"last tick averages fractional fuel");
            Find.TickManager.DebugSetTicksGame(12);a.Power.CompTick();net.PowerNetTick();Near(.525,battery.StoredEnergy,"empty following tick never distributes stale power");
            Fill(a,1);Set(a.Flick,"switchOnInt",false);a.Power.ReceiveCompSignal("FlickedOff");
            Check.Equal(false,a.Power.PowerOn,"native flick signal powers off");Find.TickManager.DebugSetTicksGame(199);a.Power.CompTick();net.PowerNetTick();Near(1,a.Fuel.Fuel,"off tick does not debit");
            Set(a.Flick,"switchOnInt",true);a.Power.ReceiveCompSignal("FlickedOn");Find.TickManager.DebugSetTicksGame(200);a.Power.CompTick();net.PowerNetTick();
            Check.Equal(true,a.Power.PowerOn,"native net restarts after flick on");Near(0,a.Fuel.Fuel,"restart pays before output without battery reserve");Near(3000,a.Power.PowerOutput,"flick on self-starts with low battery");
            Find.TickManager.DebugSetTicksGame(201);a.Power.CompTick();net.PowerNetTick();Near(0,a.Fuel.Fuel,"resumed normal tick pays fuel");
            double residue=Credit(a.Power);Set(a.Breakdown,"brokenDownInt",true);a.Power.ReceiveCompSignal("Breakdown");Find.TickManager.DebugSetTicksGame(202);a.Power.CompTick();net.PowerNetTick();Near(residue,Credit(a.Power),"breakdown stops without burn");
            battery.DrawPower(battery.StoredEnergy);net.batteryComps.Clear();Set(a.Breakdown,"brokenDownInt",false);a.Power.ReceiveCompSignal("BreakdownFixed");Find.TickManager.DebugSetTicksGame(400);a.Power.CompTick();net.PowerNetTick();Check.Equal(true,a.Power.PowerOn,"repaired generator restarts without any battery");Near(3000,a.Power.PowerOutput,"repair starts paid production without stored electricity");residue=Credit(a.Power);
            map.generatorDef=new MapGeneratorDef();map.info=new MapInfo();
            var flare=new GameCondition_DisableElectricity{def=new GameConditionDef{defName="RegressionSolarFlare",allowUnderground=true}};
            map.gameConditionManager.ActiveConditions.Add(flare);Find.TickManager.DebugSetTicksGame(401);a.Power.CompTick();
            Near(0,a.Power.PowerOutput,"native solar flare disables generation");Near(residue,Credit(a.Power),"flare retains paid fuel");
            map.gameConditionManager.ActiveConditions.Remove(flare);Find.TickManager.DebugSetTicksGame(402);a.Power.CompTick();Near(3000,a.Power.PowerOutput,"generation resumes after flare");residue=Credit(a.Power);
            a.Power.PostDeSpawn(map);Near(0,a.Power.PowerOutput,"despawn removes live output");Near(residue,Credit(a.Power),"despawn preserves prepaid residue");
        } finally {Current.Game=oldGame;}
    }
    static void Save(RuntimeFixture f)
    {
        _=GeneratorType;var a=new GeneratorSave();Fill(a,2);Advance(a.Power,2500);
        string path=Path.Combine(f.WorkingDirectory,"generator.xml");
        try {Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref a,"generator");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
        GeneratorSave loaded=null!;
        try {Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref loaded,"generator");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
        Near(1,loaded.Fuel.Fuel,"native fuel survives");Near(.75,Credit(loaded.Power),"paid residue survives");
        Near(0,loaded.Power.PowerOutput,"loading never republishes stale output");
        Near(375,Advance(loaded.Power,7500),"load uses existing credit");Near(1,loaded.Fuel.Fuel,"no duplicate payment");
    }
}

