using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> NativeThrusterCases()
    {
        yield return new("Odyssey.MagicalThruster.NativeMixedLaunchPayment", f => Run(f, () => ThrusterEnvironment(() => NativeLaunch())));
    }

    // Real native engine/facility queries, with already-computed room and floor caches.
    // No resource, eligibility, or launch-payment method is replaced by the fixture.
    internal static Building_GravEngine NativeEngine(HashSet<IntVec3> floors)
    {
        typeof(OdysseyReservoirTransportTests).GetMethod("InitializeEngineGraphics", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
        var engine = new Building_GravEngine();
        engine.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        engine.def.defName = "RegressionMagicalEngine"; engine.def.size = new IntVec2(1,1);
        Set(engine, "positionInt", new IntVec3(10,0,10));
        Set(engine, "mapIndexOrState", (sbyte)0); Set(engine, "factionInt", faction);
        Set(engine, "comps", new List<ThingComp>());
        Set(engine, "validSubstructure", floors); engine.substructureDirty=false;
        Set(engine, "gravshipComponentsDirty", false);
        map.listerBuildings = new ListerBuildings();
        Set(map.listerBuildings, "allBuildingsColonist", new List<Building> {engine});
        map.info = new MapInfo { parent = (MapParent)RuntimeHelpers.GetUninitializedObject(typeof(MapParent)) };
        Set(map.info.parent, "tile", default(PlanetTile));
        return engine;
    }

    internal static CompMagicalThruster NativeThruster(Building_GravEngine engine, int x, int z, float range)
    {
        var endpoint = (CompManaNetworkEndpoint)Make("ManaNetworkEndpoint",x,z);
        endpoint.FuelMode = ManaNetworkMode.Receive;
        var thruster = new CompMagicalThruster { parent = endpoint.parent, engine = engine,
            props = new CompProperties_MagicalThruster {maxDistance=500,statOffsets=[new StatModifier {stat=StatDefOf.GravshipRange,value=range}]} };
        endpoint.parent.AllComps.Add(thruster);
        endpoint.parent.AllComps.Add(new CompBreakdownable {parent=endpoint.parent,props=new CompProperties_Breakdownable()});
        thruster.LinkedBuildings.Add(engine); Set(thruster,"outdoors",true);
        engine.GravshipComponents.Add(thruster);
        return thruster;
    }

    static void NativeLaunch(bool coreTank = false)
    {
        var odyssey=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.Static|BindingFlags.NonPublic)!;
        var oldOdyssey=odyssey.GetValue(null);
        var installed=typeof(ModLister).GetField("odysseyInstalled",BindingFlags.Static|BindingFlags.NonPublic)!; var oldInstalled=installed.GetValue(null);
        var routeFields=new[]{"cachedOrigin","cachedDest","cachedCost","cachedDistance","cachedResult"}
            .Select(n=>typeof(GravshipUtility).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!).ToArray();
        object?[] oldRoute=[];
        string patchId="rwom.regression.magic-launch."+Guid.NewGuid();
        object? harmony=null;
        try
        {
            odyssey.SetValue(null,true); installed.SetValue(null,true);
            var floors=new HashSet<IntVec3>();
            for(int x=5;x<=20;x++) for(int z=5;z<=20;z++) floors.Add(new IntVec3(x,0,z));
            var engine=NativeEngine(floors);
            if (coreTank) engine = MagicalEngineFrom(engine);
            Make("ManaBeacon",10,10);
            var source=Tank(10,9); source.parent.def.defName="TM_MagofuelReservoir";
            var fuel=AddFuel(source,100);
            if (coreTank)
            {
                foreach (var comp in source.parent.AllComps.ToArray()) { comp.parent = engine; engine.AllComps.Add(comp); }
            }
            NativeThruster(engine,11,10,10);
            NativeThruster(engine,12,10,10);
            var chemical=AddFuel(Make("ManaNetworkEndpoint",9,10),100);
            chemical.parent.def.defName="RegressionChemicalTank";
            engine.GravshipComponents.Add(new CompGravshipFacility {parent=chemical.parent,props=new CompProperties_GravshipFacility {providesFuel=true,maxDistance=500}});
            var asm=Assembly.Load("0Harmony"); var ht=asm.GetType("HarmonyLib.Harmony")!;
            harmony=Activator.CreateInstance(ht,patchId)!;
            object HM(MethodInfo method)=>Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,method)!;
            foreach(var (getter,patch) in new[]{("TotalFuel",typeof(MagicalThrusterTotalFuelPatch)),("MaxFuel",typeof(MagicalThrusterMaxFuelPatch))})
                ht.GetMethod("Patch")!.Invoke(harmony,[typeof(Building_GravEngine).GetProperty(getter)!.GetMethod!,null,HM(patch.GetMethod("Postfix")!),null,null]);
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(Building_GravEngine).GetMethod("ConsumeFuel")!,HM(typeof(MagicalThrusterConsumeFuelPatch).GetMethod("Prefix")!),HM(typeof(MagicalThrusterConsumeFuelPatch).GetMethod("Postfix")!),null,HM(typeof(MagicalThrusterConsumeFuelPatch).GetMethod("Finalizer")!)]);
            var debitPatch=typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.MagicalThrusterNativeDebitPatch",true)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(CompRefuelable).GetMethod("ConsumeFuel")!,HM(debitPatch.GetMethod("Prefix")!),null,null,HM(debitPatch.GetMethod("Finalizer")!)]);
            Check.Equal(300f,engine.TotalFuel,"one physical reservoir counted once for two thrusters plus vanilla fuel");
            Check.Equal(300f,engine.MaxFuel,"capacity keeps native fuel plus bounded magic range");
            for(int i=0;i<5;i++) _=engine.TotalFuel;
            Check.Equal(100f,fuel.Fuel,"preview leaves magic fuel untouched");
            Check.Equal(100f,chemical.Fuel,"preview leaves vanilla fuel untouched");
            // Seed only a native route-cache entry: this test proves payment, not world generation/pathfinding.
            oldRoute=routeFields.Select(field=>field.GetValue(null)).ToArray();
            object[] route={default(PlanetTile),default(PlanetTile),150f,15,true};
            for(int i=0;i<route.Length;i++) routeFields[i].SetValue(null,route[i]);
            typeof(Building_GravEngine).GetMethod("ConsumeFuel")!.Invoke(engine,[default(PlanetTile)]);
            Check.Equal(50f,fuel.Fuel,"one native launch consumes magic share once");
            Check.Equal(50f,chemical.Fuel,"native method consumes chemical share at same snapshot ratio");
            Check.Equal(true,engine.cooldownCompleteTick>Find.TickManager.TicksGame,"native launch cooldown retained");
            _=engine.TotalFuel;
            Check.Equal(50f,fuel.Fuel,"post-launch query does not replay payment");
            int range=30; // 10 native range + 20 declared magical range.
            MagicalThrusterLaunchDistancePatch.Postfix(engine,ref range);
            Check.Equal(20,range,"partially funded magical range cannot be subsidized by vanilla tanks");
            var saver=engine.GravshipComponents.Single(c=>c.parent==chemical.parent).Props;
            saver.fuelSavingsPercent=.5f;
            range=30; MagicalThrusterLaunchDistancePatch.Postfix(engine,ref range);
            Check.Equal(30,range,"native fifty-percent fuel savings preserve funded magical range");
            var layer=(PlanetLayer)RuntimeHelpers.GetUninitializedObject(typeof(PlanetLayer));
            var layerDef=(PlanetLayerDef)RuntimeHelpers.GetUninitializedObject(typeof(PlanetLayerDef));
            layerDef.rangeDistanceFactor=1f; Set(layer,"def",layerDef);
            Check.Equal(30,GravshipUtility.MaxDistForFuel(engine.TotalFuel,layer,layer,10f,engine.FuelUseageFactor),"installed native fuel-distance math agrees with savings-aware cap");
            saver.fuelSavingsPercent=0f;
            fuel.ConsumeFuel(50);
            range=30; MagicalThrusterLaunchDistancePatch.Postfix(engine,ref range);
            Check.Equal(10,range,"empty magic source contributes no launch range despite remaining chemical fuel");
            Check.Equal(50f,engine.TotalFuel,"empty magic network adds no equivalent fuel");

            // Two independent magic networks plus native fuel must form one launch transaction.
            fuel.Refuel(100); chemical.Refuel(50);
            for(int x=21;x<=40;x++) for(int z=5;z<=20;z++) floors.Add(new IntVec3(x,0,z));
            Make("ManaBeacon",30,10);
            var second=Tank(30,9); second.parent.def.defName="TM_MagofuelReservoir";
            var secondFuel=AddFuel(second,50); NativeThruster(engine,31,10,10);
            Check.Equal(400f,engine.TotalFuel,"two separate magical networks plus native fuel");
            var failure=new CallbackComp {parent=second.parent,Callback=signal=>{
                if(signal=="RanOutOfFuel") throw new InvalidOperationException("regression second network failure");
            }};
            second.parent.AllComps.Add(failure);
            routeFields[2].SetValue(null,400f);
            int priorCooldown=engine.cooldownCompleteTick;
            try { typeof(Building_GravEngine).GetMethod("ConsumeFuel")!.Invoke(engine,[default(PlanetTile)]); throw new Exception("expected native launch transaction failure"); }
            catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException) {}
            Check.Equal(100f,fuel.Fuel,"failed second network restores first network debit");
            Check.Equal(50f,secondFuel.Fuel,"failed second network restores its own debit");
            Check.Equal(100f,chemical.Fuel,"failed magical payment restores native chemical payment");
            Check.Equal(priorCooldown,engine.cooldownCompleteTick,"failed launch restores prior cooldown");
            failure.Callback=null;
            var mutation=new CallbackComp {parent=chemical.parent,Callback=signal=>{
                if(signal=="RanOutOfFuel") fuel.ConsumeFuel(1);
            }};
            chemical.parent.AllComps.Add(mutation);
            try { typeof(Building_GravEngine).GetMethod("ConsumeFuel")!.Invoke(engine,[default(PlanetTile)]); throw new Exception("expected changed fuel launch failure"); }
            catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException) {}
            Check.Equal(99f,fuel.Fuel,"rollback must not recreate fuel consumed by an unrelated callback");
            Check.Equal(50f,secondFuel.Fuel,"unpaid second network unchanged");
            Check.Equal(100f,chemical.Fuel,"native own launch debit restored after source mutation");
            mutation.Callback=signal=>{if(signal=="RanOutOfFuel") floors.Remove(new IntVec3(10,0,10));};
            try { typeof(Building_GravEngine).GetMethod("ConsumeFuel")!.Invoke(engine,[default(PlanetTile)]); throw new Exception("expected disconnected network launch failure"); }
            catch(TargetInvocationException e) when(e.InnerException is InvalidOperationException) {}
            Check.Equal(99f,fuel.Fuel,"disconnected network cannot be charged or silently launch free");
            Check.Equal(100f,chemical.Fuel,"native debit restored when relay disappears during payment");
        }
        finally
        {
            if(harmony!=null) harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]);
            for(int i=0;i<oldRoute.Length;i++) routeFields[i].SetValue(null,oldRoute[i]);
            odyssey.SetValue(null,oldOdyssey); installed.SetValue(null,oldInstalled);
        }
    }
}
