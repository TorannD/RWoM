using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> ThrusterNetworkCases()
    {
        yield return new("Odyssey.MagicalThruster.ShipNetworkOwnershipRelaysAndModes", f => Run(f, () => ThrusterEnvironment(ShipNetworks)));
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER")))
        {
            yield return new("Odyssey.MagicalThruster.VgeActualProviderDebitDryRunRefund", f => Run(f, () => ThrusterEnvironment(() => VgeProvider(false, f))));
            yield return new("Odyssey.MagicalThruster.VgeChapter2ActualProviderDebitDryRunRefund", f => Run(f, () => ThrusterEnvironment(() => VgeProvider(true, f))));
            yield return new("Odyssey.MagicalThruster.VgeCoreTankDebitSaveAndRefund", f => Run(f, () => ThrusterEnvironment(() => VgeProvider(false, f, true))));
        }
        else Console.WriteLine("CAPABILITY SKIPPED: VGE thruster interface tests (optional VGE adapter unavailable); core isolation tests still run.");
        yield return new("Odyssey.MagicalThruster.OptionalAssemblyIsolation", f =>
        {
            Check.Equal(false, typeof(MagicalThrusterFuel).Assembly.GetReferencedAssemblies().Any(a => a.Name!.Contains("GravshipExpanded")), "core has no optional assembly reference");
            Check.Equal(false, new CompMagicalThruster().UsesVgeFuelProvider, "native path selected without optional subclass");
        });
        yield return new("Odyssey.MagicalThruster.PaymentCallbackRollbackAndReentry", f => Run(f, ThrusterCallbacks));
    }
    static void ThrusterEnvironment(Action action)
    {
        var field=typeof(ModLister).GetField("odysseyInstalled",BindingFlags.NonPublic|BindingFlags.Static)!;
        var old=field.GetValue(null);var oldLanguage=LanguageDatabase.activeLanguage;var oldDefault=LanguageDatabase.defaultLanguage;
        try {
            field.SetValue(null,true);
            var language=(LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName="English";Set(language,"dataIsLoaded",true);Set(language,"workerInt",new LanguageWorker_English());
            language.keyedReplacements=new Dictionary<string,LoadedLanguage.KeyedReplacement>();
            foreach(string key in new[]{"Gravship","TM_MagicalThrusterFuelName","TM_MagicalThrusterFuelAmount","TM_MagicalThrusterFuelRange"})
                language.keyedReplacements[key]=new LoadedLanguage.KeyedReplacement{key=key,value=key};
            LanguageDatabase.activeLanguage=LanguageDatabase.defaultLanguage=language;
            action();
        }
        finally {field.SetValue(null,old);LanguageDatabase.activeLanguage=oldLanguage;LanguageDatabase.defaultLanguage=oldDefault;}
    }
    static void ThrusterCallbacks()
    {
        var a=AddFuel(Tank(9,9),10);var b=AddFuel(Tank(10,9),20);
        var group=new MagicalThrusterFuelGroup(new List<CompRefuelable>{a,b},6f);
        var callback=new CallbackComp{parent=b.parent,Callback=s=>{if(s=="RanOutOfFuel")throw new InvalidOperationException("regression callback");}};
        b.parent.AllComps.Add(callback);
        try {group.ConsumeRatio(1f,true);throw new Exception("expected callback exception");}
        catch(InvalidOperationException e) when(e.Message=="regression callback") {}
        Check.Equal(30f,a.Fuel+b.Fuel,"throwing native callback rolls back entire group debit");
        callback.Callback=null;
        var recursive=new CallbackComp{parent=a.parent,Callback=s=>{if(s=="RanOutOfFuel")Check.Equal(0f,group.ConsumeRatio(1f,true),"reentry cannot debit same reservoirs twice");}};
        a.parent.AllComps.Add(recursive);
        Check.Equal(30f,group.ConsumeRatio(1f,true),"single guarded payment completes");
        Check.Equal(0f,a.Fuel+b.Fuel,"all physical fuel paid once");
    }
    static HashSet<IntVec3> ShipFloor()
    {
        var floors=new HashSet<IntVec3>();
        for(int x=5;x<=30;x++) for(int z=5;z<=20;z++) floors.Add(new IntVec3(x,0,z));
        return floors;
    }
    public sealed class VgeReceiptEnvelope : IExposable
    {
        public static Type ReceiptType=null!;
        public IExposable Receipt=(IExposable)Activator.CreateInstance(ReceiptType)!;
        public void ExposeData()=>Receipt.ExposeData();
    }
    static void ShipNetworks()
    {
        var floors=ShipFloor(); var engine=NativeEngine(floors);
        var a=Make("ManaBeacon",10,10); var relay=Make("ManaBeacon",15,10); var b=Make("ManaBeacon",20,10);
        var source=Tank(9,9); source.parent.def.defName="TM_MagofuelReservoir"; var fuel=AddFuel(source,60);
        var thruster=NativeThruster(engine,21,10,10);
        Check.Equal(1,MagicalThrusterFuel.Sources(thruster).Count,"connected ship reservoir found through relay");
        floors.Remove(relay.parent.Position);
        Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"ground relay cannot bridge two ship networks");
        floors.Add(relay.parent.Position);
        floors.Remove(source.parent.Position);
        Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"ground reservoir excluded");
        floors.Add(source.parent.Position);
        ((CompManaNetworkEndpoint)source).FuelMode=ManaNetworkMode.Off;
        Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"off reservoir excluded");
        ((CompManaNetworkEndpoint)source).FuelMode=ManaNetworkMode.Send;
        Set(source.parent,"factionInt",new Faction {def=new FactionDef {isPlayer=true}});
        Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"foreign reservoir excluded");
        Set(source.parent,"factionInt",faction);
        source.parent.def.defName="RegressionChemicalTank";
        Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"other fuel-bearing endpoint cannot substitute for magofuel reservoir");
        source.parent.def.defName="TM_MagofuelReservoir";
        network.GetType().GetMethod("Unregister",[T("CompManaBeacon")])!.Invoke(network,[relay]);
        Check.Equal(0,MagicalThrusterFuel.Sources(thruster).Count,"removed relay breaks network immediately");
        var near=NativeThruster(engine,13,10,10);
        var ground=Make("ManaBeacon",12,10); floors.Remove(ground.parent.Position);
        Check.Equal(0,MagicalThrusterFuel.Sources(near).Count,"a ground nearest beacon cannot silently reattach endpoint to a different ship network");
        network.GetType().GetMethod("Unregister",[T("CompManaBeacon")])!.Invoke(network,[ground]);
        Check.Equal(1,MagicalThrusterFuel.Sources(near).Count,"removing nearest ground beacon permits normal nearest ship attachment");
        Check.Equal(60f,fuel.Fuel,"all topology previews are free");
    }
    static void VgeProvider(bool chapter2, RuntimeFixture fixture, bool coreTank = false)
    {
        string? adapterPath=Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER");
        if(string.IsNullOrEmpty(adapterPath)) throw new InvalidOperationException("VGE adapter is required for this explicitly registered test.");
        const string workshop="C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/";
        Assembly? Resolve(AssemblyLoadContext ctx,AssemblyName name)
        {
            foreach(string folder in new[]{"2023507013","3609835606"}) {
                string file=Path.Combine(workshop,folder,"1.7/Assemblies",name.Name+".dll");
                if(File.Exists(file)) return ctx.LoadFromAssemblyPath(file);
            }
            return null;
        }
        var odyssey=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.Static|BindingFlags.NonPublic)!;
        var old=odyssey.GetValue(null); AssemblyLoadContext.Default.Resolving+=Resolve;
        object? harmony=null;string patchId="rwom.regression.vge-thruster-refund."+Guid.NewGuid();
        try
        {
            odyssey.SetValue(null,true);
            var vge=Assembly.LoadFrom(Path.Combine(workshop,"3609835606/1.7/Assemblies/VanillaGravshipExpanded.dll"));
            if(chapter2) Assembly.LoadFrom(Path.Combine(workshop,"3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll"));
            var adapter=Assembly.LoadFrom(adapterPath);
            var providerType=adapter.GetType("TorannMagic.VGE.CompMagicalThrusterVge")!;
            var iface=vge.GetType("VanillaGravshipExpanded.IGravshipFuelProvider")!;
            Check.Equal(true,iface.IsAssignableFrom(providerType),"adapter implements installed VGE interface");
            var engine=NativeEngine(ShipFloor()); Make("ManaBeacon",10,10);
            var source=Tank(9,9); source.parent.def.defName="TM_MagofuelReservoir"; var fuel=AddFuel(source,100);
            if(coreTank)
            {
                engine=MagicalEngineFrom(engine);
                foreach(var comp in source.parent.AllComps.ToArray()) {comp.parent=engine; engine.AllComps.Add(comp);}
            }
            var thrusters=new List<CompGravshipThruster>();
            for(int i=0;i<2;i++)
            {
                var native=NativeThruster(engine,11+i,10,10);
                var provider=(CompMagicalThruster)Activator.CreateInstance(providerType)!;
                provider.parent=native.parent;provider.props=native.props;provider.engine=engine;
                provider.LinkedBuildings.Add(engine);Set(provider,"outdoors",true);
                native.parent.AllComps.Remove(native);native.parent.AllComps.Add(provider);
                engine.GravshipComponents.Remove(native);engine.GravshipComponents.Add(provider);thrusters.Add(provider);
            }
            Check.Equal(0,MagicalThrusterFuel.NativeGroups(engine).Count,"native debit excludes VGE-owned group");
            var utility=vge.GetType("VanillaGravshipExpanded.GravshipFuelProviderUtility")!;
            var harmonyAssembly=Assembly.Load("0Harmony");
            harmony=Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.Harmony")!,patchId)!;
            var refundPostfix=Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!,adapter.GetType("TorannMagic.VGE.MagicalThrusterVgeRefundPatch")!.GetMethod("Postfix")!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[utility.GetMethod("RefundFuelForAllProviders")!,null,refundPostfix,null,null]);
            object? Invoke(string name,params object?[] args)=>utility.GetMethod(name)!.Invoke(null,args);
            Check.Equal(20f,(float)Invoke("CurrentRangeForAllProviders",engine)!,"actual VGE traversal deduplicates network");
            var others=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(iface))!;
            others.Add(thrusters[1]);
            var dry=providerType.GetMethod("ConsumeFuelRatio")!.Invoke(thrusters[0],[engine,.5f,thrusters,others,false])!;
            Check.Equal(50f,(float)dry.GetType().GetField("totalAmount")!.GetValue(dry)!,"VGE dry run computes physical magofuel");
            Check.Equal(100f,fuel.Fuel,"VGE dry run preserves reservoir");
            var spent=Activator.CreateInstance(vge.GetType("VanillaGravshipExpanded.FuelSpentData")!)!;
            Invoke("ConsumeFuelRatioForAllProviders",engine,1f,spent);
            Check.Equal(0f,fuel.Fuel,"actual VGE full launch pays one time");
            var receipt=(IDictionary)spent.GetType().GetField("fuelData")!.GetValue(spent)!;
            Check.Equal(1,receipt.Count,"VGE records one physical tank receipt");
            Check.Equal(true,receipt.Contains(source.parent),"refund identity is original tank rather than representative thruster");
            string receiptPath=Path.Combine(fixture.WorkingDirectory,"thruster-refund-"+chapter2+"-"+coreTank+".xml");
            VgeReceiptEnvelope.ReceiptType=spent.GetType();
            var saved=new VgeReceiptEnvelope{Receipt=(IExposable)spent};
            try {Scribe.saver.InitSaving(receiptPath,"regression");Scribe_Deep.Look(ref saved,"receipt");Scribe.saver.FinalizeSaving();}
            finally {Scribe.ForceStop();}
            saved=null!;
            try {
                Scribe.loader.InitLoading(receiptPath);Scribe_Deep.Look(ref saved,"receipt");
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(source.parent);Scribe.loader.FinalizeLoading();
            }
            finally {Scribe.ForceStop();}
            spent=saved.Receipt;
            receipt=(IDictionary)spent.GetType().GetField("fuelData")!.GetValue(spent)!;
            Check.Equal(true,receipt.Contains(source.parent),"native VGE Scribe receipt resolves original reservoir identity");
            Check.Equal(0f,(float)Invoke("CurrentRangeForAllProviders",engine)!,"empty reservoirs offer zero range");
            // The original representative may be blocked, and the live network may attach to
            // different fuel by the time the native delayed launch refund is requested.
            Set(thrusters[1],"outdoors",false);
            ((CompManaNetworkEndpoint)source).FuelMode=ManaNetworkMode.Off;
            var replacement=Tank(10,8);replacement.parent.def.defName="TM_MagofuelReservoir";var replacementFuel=AddFuel(replacement,40);
            Invoke("RefundFuelForAllProviders",engine,1f,spent);
            Check.Equal(100f,fuel.Fuel,"VGE refunds original empty source despite blocked representative and changed network");
            Check.Equal(40f,replacementFuel.Fuel,"refund never enters a replacement source");
        }
        finally { if(harmony!=null)harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]);odyssey.SetValue(null,old);AssemblyLoadContext.Default.Resolving-=Resolve; }
    }
}
