using System.Reflection;
using System.Runtime.Loader;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> VgeLaunchCases()
    {
        if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER")))
            yield return new("Odyssey.MagicalThruster.VgeNativeLaunchAtomicFailure", f=>Run(f,()=>ThrusterEnvironment(VgeLaunchFailure)));
    }

    static void VgeLaunchFailure()
    {
        const string workshop="C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/";
        Assembly? Resolve(AssemblyLoadContext ctx,AssemblyName name)
        {
            foreach(string folder in new[]{"2023507013","3609835606"}) {
                string file=Path.Combine(workshop,folder,"1.7/Assemblies",name.Name+".dll");
                if(File.Exists(file)) return ctx.LoadFromAssemblyPath(file);
            }
            return null;
        }
        AssemblyLoadContext.Default.Resolving+=Resolve;
        var odyssey=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.Static|BindingFlags.NonPublic)!;
        var oldOdyssey=odyssey.GetValue(null);
        var cache=new[]{"cachedOrigin","cachedDest","cachedCost","cachedDistance","cachedResult"}.Select(n=>typeof(GravshipUtility).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!).ToArray();
        object?[] prior=[]; object? harmony=null; string patchId="rwom.regression.vge-launch."+Guid.NewGuid();
        try
        {
            odyssey.SetValue(null,true);
            var vge=Assembly.LoadFrom(Path.Combine(workshop,"3609835606/1.7/Assemblies/VanillaGravshipExpanded.dll"));
            var adapter=Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER")!);
            var providerType=adapter.GetType("TorannMagic.VGE.CompMagicalThrusterVge")!;
            var floors=ShipFloor(); for(int x=31;x<40;x++) for(int z=5;z<=20;z++) floors.Add(new IntVec3(x,0,z));
            var engine=NativeEngine(floors); engine.launchInfo=new LaunchInfo();
            var fuels=new List<CompRefuelable>();
            foreach(int x in new[]{10,30})
            {
                Make("ManaBeacon",x,10);var source=Tank(x-1,9);source.parent.def.defName="TM_MagofuelReservoir";
                fuels.Add(AddFuel(source,50));
                var native=NativeThruster(engine,x+1,10,10);
                var provider=(CompMagicalThruster)Activator.CreateInstance(providerType)!;
                provider.parent=native.parent;provider.props=native.props;provider.engine=engine;
                provider.LinkedBuildings.Add(engine);Set(provider,"outdoors",true);
                native.parent.AllComps.Remove(native);native.parent.AllComps.Add(provider);
                engine.GravshipComponents.Remove(native);engine.GravshipComponents.Add(provider);
            }
            // VGE traverses last provider first: first-created source fails after the other paid.
            fuels[0].parent.AllComps.Add(new CallbackComp {parent=fuels[0].parent,Callback=signal=>{
                if(signal=="RanOutOfFuel") throw new InvalidOperationException("regression VGE second network");
            }});
            var chemical=AddFuel(Make("ManaNetworkEndpoint",9,10),100);
            engine.GravshipComponents.Add(new CompGravshipFacility {parent=chemical.parent,props=new CompProperties_GravshipFacility {providesFuel=true,maxDistance=500}});
            var ha=Assembly.Load("0Harmony"); harmony=Activator.CreateInstance(ha.GetType("HarmonyLib.Harmony")!,patchId)!;
            object? HM(Type type,string? method)=>method==null?null:Activator.CreateInstance(ha.GetType("HarmonyLib.HarmonyMethod")!,type.GetMethod(method,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!)!;
            void Patch(MethodInfo target,Type type,string? prefix=null,string? postfix=null,string? finalizer=null)=>harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[target,HM(type,prefix),HM(type,postfix),null,HM(type,finalizer)]);
            var consume=typeof(Building_GravEngine).GetMethod("ConsumeFuel")!;
            Patch(typeof(Building_GravEngine).GetProperty("TotalFuel")!.GetMethod!,vge.GetType("VanillaGravshipExpanded.Building_GravEngine_TotalFuel_Patch")!,postfix:"Postfix");
            Patch(consume,typeof(MagicalThrusterConsumeFuelPatch),"Prefix","Postfix","Finalizer");
            Patch(typeof(CompRefuelable).GetMethod("ConsumeFuel")!,typeof(MagicalThrusterNativeDebitPatch),"Prefix",finalizer:"Finalizer");
            Patch(consume,adapter.GetType("TorannMagic.VGE.MagicalThrusterVgeReceiptRollbackPatch")!,"Prefix",finalizer:"Finalizer");
            Patch(consume,vge.GetType("VanillaGravshipExpanded.Building_GravEngine_ConsumeFuel_Patch")!,"Prefix","Postfix");
            Check.Equal(300f,engine.TotalFuel,"real VGE total includes two magic networks and native fuel once");
            prior=cache.Select(f=>f.GetValue(null)).ToArray();object[] values={default(PlanetTile),default(PlanetTile),300f,30,true};
            for(int i=0;i<cache.Length;i++) cache[i].SetValue(null,values[i]);
            int cooldown=engine.cooldownCompleteTick;
            try { consume.Invoke(engine,[default(PlanetTile)]);throw new Exception("expected second VGE network failure"); }
            catch(TargetInvocationException ex) when(ex.InnerException is InvalidOperationException e && e.Message=="regression VGE second network") {}
            Check.Equal(50f,fuels[0].Fuel,"failing VGE group restored");
            Check.Equal(50f,fuels[1].Fuel,"already-paid VGE group restored");
            Check.Equal(100f,chemical.Fuel,"native chemical payment restored with VGE group failure");
            Check.Equal(cooldown,engine.cooldownCompleteTick,"failed VGE launch restores cooldown");
            var extension=vge.GetTypes().SelectMany(t=>t.GetMethods(BindingFlags.Static|BindingFlags.Public))
                .Single(m=>m.Name=="ExtendedInfo" && m.GetParameters().Length==2 && m.GetParameters()[0].ParameterType==typeof(LaunchInfo));
            var info=extension.Invoke(null,[engine.launchInfo,false])!;
            Check.Equal<object?>(null,info.GetType().GetField("fuelSpentPerTank")!.GetValue(info),"aborted launch receipt cannot refund reverted payments again");
        }
        finally
        {
            if(harmony!=null) harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]);
            for(int i=0;i<prior.Length;i++) cache[i].SetValue(null,prior[i]);
            odyssey.SetValue(null,oldOdyssey);AssemblyLoadContext.Default.Resolving-=Resolve;
        }
    }
}
