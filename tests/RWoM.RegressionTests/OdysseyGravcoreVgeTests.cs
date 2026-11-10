using System.Reflection;
using System.Runtime.Loader;
using System.Xml;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> GravcoreVgeCases()
    {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER"))) {Console.WriteLine("CAPABILITY SKIPPED: optional VGE gravcore native assembly tests.");yield break;}
        yield return new("Odyssey.Gravcore.VGE.NativePatchComponentsAndLimits", f => GravcoreVge(f, false));
        yield return new("Odyssey.Gravcore.VGE.Chapter2HeatMaintenanceAndDestructionSave", f => Run(f, () => ThrusterEnvironment(() => GravcoreVge(f, true))));
    }
    static void GravcoreVge(RuntimeFixture f, bool lifecycle)
    {
        const string workshop="C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/";
        Assembly? Resolve(AssemblyLoadContext ctx,AssemblyName name)
        {
            foreach(string folder in new[]{"2023507013","3609835606","3799737423"}) {
                string path=Path.Combine(workshop,folder,"1.7/Assemblies",name.Name+".dll");
                if(File.Exists(path)) return ctx.LoadFromAssemblyPath(path);
            }
            return null;
        }
        AssemblyLoadContext.Default.Resolving+=Resolve;
        var registry=LoadedModManager.RunningModsListForReading[0].assemblies.loadedAssemblies; var priorAssemblies=registry.ToArray();
        try
        {
            var vge=Assembly.LoadFrom(Path.Combine(workshop,"3609835606/1.7/Assemblies/VanillaGravshipExpanded.dll"));
            var vge2=Assembly.LoadFrom(Path.Combine(workshop,"3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll"));
            registry.Add(vge); registry.Add(vge2); registry.Add(Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER")!));
            var defs=new XmlDocument(); defs.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Gravcore.xml"));
            var patch=new XmlDocument(); patch.Load(Path.Combine(f.Repository,"TorannMagic.Integrations/v1.7/VGECompat/Patches/MagicalGravcore.xml"));
            foreach(XmlNode n in patch.SelectNodes("Patch/Operation")!)
                Check.Equal(true,DirectXmlToObject.ObjectFromXml<PatchOperation>(n,false).Apply(defs),"native VGE patch applies");
            XmlNode engine=defs.SelectSingleNode("Defs/ThingDef[defName='TM_MagicalGravEngine']")!;
            XmlNode extender=defs.SelectSingleNode("Defs/ThingDef[defName='TM_MagicalGravExtender']")!;
            Check.Equal(1,extender.SelectNodes("tickerType")!.Count,"one extender ticker after VGE patch");
            Check.Equal("Normal",extender.SelectSingleNode("tickerType")!.InnerText,"orbit tick sampling survives VGE");
            Check.Equal("MapMeshAndRealTime",extender.SelectSingleNode("drawerType")!.InnerText,"orbit dynamic draw survives VGE");
            Check.Equal(1,extender.SelectNodes("comps/li[@Class='TorannMagic.Odyssey.CompProperties_ExtenderOrbit']")!.Count,"one orbit after VGE patch");
            Check.Equal("500",engine.SelectSingleNode("statBases/SubstructureSupport")!.InnerText,"approved magical engine lift retained with VGE");
            Check.Equal("18.9",engine.SelectSingleNode("comps/li[@Class='CompProperties_SubstructureFootprint']/radius")!.InnerText,"engine radius");
            Check.Equal("100",extender.SelectSingleNode("comps/li/statOffsets/SubstructureSupport")!.InnerText,"VGE extender lift");
            Check.Equal("10",extender.SelectSingleNode("comps/li/maxSimultaneous")!.InnerText,"VGE extender cap");
            Check.Equal("500",extender.SelectSingleNode("comps/li/maxDistance")!.InnerText,"VGE extender distance");
            Check.Equal("12.9",extender.SelectSingleNode("comps/li[@Class='CompProperties_SubstructureFootprint']/radius")!.InnerText,"extender footprint");
            // Parse actual component properties through the game loader, then instantiate the native implementations.
            var properties=engine.SelectNodes("comps/li")!.Cast<XmlNode>().Select(n=>DirectXmlToObject.ObjectFromXml<CompProperties>(n,false)).ToList();
            var power=properties.OfType<CompProperties_Power>().Single();
            Check.Equal("VanillaGravshipExpanded.CompPowerPlantGravEngine",power.compClass.BaseType!.FullName!,"native variable-output power inherited");
            Check.Equal(-4800f,(float)typeof(CompProperties_Power).GetField("basePowerConsumption",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(power)!,"same maximum electrical output as VGE");
            foreach(string type in new[]{"CompHeatManager","CompGravMaintainable","CompMultipleGravEnginesHandler","CompAffectedByConstantGravshipFacilityBonus"})
                Check.Equal(1,properties.Count(p=>p.compClass.FullName=="VanillaGravshipExpanded."+type),"one real "+type);
            Check.Equal(1,properties.OfType<CompProperties_Breakdownable>().Count(),"one engine breakdown comp");
            Check.Equal(0,properties.OfType<CompProperties_Flickable>().Count(),"native VGE engine has no additional switch");
            Check.Equal(0,engine.SelectNodes("comps/li/linkableFacilities/li[text()='GravFieldExtender']")!.Count,"nonmagical extenders excluded");
            Check.Equal("true",engine.SelectSingleNode("notifyMapRemoved")!.InnerText,"map removal reaches multiple-engine handler");
            Check.Equal(1,engine.SelectNodes("inspectorTabs/li[text()='VanillaGravshipExpanded.ITab_GravshipFuel']")!.Count,"fuel tab");
            if(!lifecycle) {RuntimeLog.AssertClean();return;}
            var building=MagicalEngineFrom(NativeEngine(ShipFloor()));
            ThingComp Attach(string name) {
                var p=properties.Single(p=>p.compClass.Name==name);
                var c=(ThingComp)Activator.CreateInstance(p.compClass)!; c.parent=building; c.Initialize(p); building.AllComps.Add(c); return c;
            }
            var breakdown=Attach("CompBreakdownable");
            var maintain=Attach("CompGravMaintainable"); maintain.PostPostMake();
            Check.Equal(breakdown,maintain.GetType().GetField("compBreakdownable")!.GetValue(maintain),"maintenance initializes native breakdown dependency");
            var priorGame=Current.Game; var priorScenario=priorGame.Scenario;
            GravcoreVgePower(building,Attach("CompMagicalGravEnginePower"));
            Check.Same(priorGame,Current.Game,"power fixture restores game identity");
            Check.Equal(priorScenario,Current.Game.Scenario,"power fixture restores previous scenario");
            var heat=Attach("CompHeatManager");
            Check.Equal(building,heat.GetType().GetProperty("Engine")!.GetValue(heat),"heat recognizes magical subclass");
            float generated=(float)heat.GetType().GetMethod("HeatGeneratedFromFuel")!.Invoke(heat,[20f])!;
            Check.Equal(10f,generated,"native fuel-to-heat economy");
            heat.GetType().GetMethod("AddHeat",[typeof(float),typeof(bool),typeof(bool),typeof(bool)])!.Invoke(heat,[generated,false,false,true]);
            Check.Equal(10f,(float)heat.GetType().GetProperty("HeatUnits")!.GetValue(heat)!,"native heat retained until applied");
            maintain.GetType().GetField("maintenance")!.SetValue(maintain,.42f);
            // Explicitly apply Chapter 2 operation independent of the host's active-mod list.
            var chapter=patch.SelectSingleNode("Patch/Operation[@Class='PatchOperationFindMod']/match")!;
            Check.Equal(true,DirectXmlToObject.ObjectFromXml<PatchOperation>(chapter,false).Apply(defs),"Chapter 2 operations apply");
            var destructionNode=engine.SelectSingleNode("comps/li[@Class='VanillaGravshipExpanded2.CompProperties_GravEngineDestruction']")!;
            var destructionProps=DirectXmlToObject.ObjectFromXml<CompProperties>(destructionNode,false);
            var destruction=(ThingComp)Activator.CreateInstance(destructionProps.compClass)!; destruction.parent=building; destruction.Initialize(destructionProps);
            Check.Equal(7.9f,(float)destructionProps.GetType().GetField("explosionRadius")!.GetValue(destructionProps)!,"medium engine destruction radius");
            Set(destruction,"imploding",true); Set(destruction,"implosionTicksLeft",30);
            var save=new GravcoreVgeSave { Comps=[heat,maintain,destruction] };
            string path=Path.Combine(f.WorkingDirectory,"gravcore-vge.xml");
            try { Scribe.saver.InitSaving(path,"regression");save.ExposeData();Scribe.saver.FinalizeSaving(); } finally {Scribe.ForceStop();}
            Set(heat,"heatUnits",0f);maintain.GetType().GetField("maintenance")!.SetValue(maintain,1f);Set(destruction,"imploding",false);Set(destruction,"implosionTicksLeft",0);
            try { Scribe.loader.InitLoading(path);save.ExposeData();Scribe.loader.FinalizeLoading(); } finally {Scribe.ForceStop();}
            Check.Equal(10f,(float)heat.GetType().GetProperty("HeatUnits")!.GetValue(heat)!,"native heat save");
            Check.Equal(.42f,(float)maintain.GetType().GetField("maintenance")!.GetValue(maintain)!,"native maintenance save");
            destruction.CompTick();
            Check.Equal(29,(int)destruction.GetType().GetField("implosionTicksLeft",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(destruction)!,"restored Chapter 2 implosion resumes");
            RuntimeLog.AssertClean();
        }
        finally {DirectXmlCrossRefLoader.Clear(); registry.Clear();registry.AddRange(priorAssemblies); AssemblyLoadContext.Default.Resolving-=Resolve;}
    }
    static void GravcoreVgePower(Building_GravEngine engine, ThingComp comp)
    {
        var oldStat=StatDefOf.SubstructureSupport; var stat=new StatDef {defName="SubstructureSupport",workerClass=typeof(StatWorker),defaultBaseValue=500,minValue=0,maxValue=99999}; StatDefOf.SubstructureSupport=stat;
        var main=typeof(UnityData).GetField("mainThreadId",BindingFlags.Static|BindingFlags.NonPublic)!;var oldMain=main.GetValue(null);
        var oldState=Current.ProgramState;
        var game=Current.Game;var oldScenario=game.Scenario;
        try
        {
            // Use the real game scenario path; headless UI fixtures can have no window stack.
            game.Scenario=new Scenario();
            main.SetValue(null,Environment.CurrentManagedThreadId);Current.ProgramState=ProgramState.Playing;
            stat.workerClass=typeof(StatWorker);stat.defaultBaseValue=500;
            engine.def.statBases=[new StatModifier {stat=stat,value=500}];
            var plant=(CompPowerPlant)comp;
            var fuel=new CompRefuelable {parent=engine,props=new CompProperties_Refuelable {fuelCapacity=100}};
            engine.AllComps.Add(fuel);Set(plant,"refuelableComp",fuel);
            var switchComp=new CompFlickable {parent=engine,props=new CompProperties_Flickable()};Set(switchComp,"switchOnInt",true);Set(plant,"flickableComp",switchComp);
            // PowerNet transitions are presentation/network plumbing: seed the powered state.
            Set(plant,"powerOnInt",true);
            foreach(var (cells,expected) in new[]{(0,4800f),(250,2400f),(500,0f),(600,0f)})
            {
                Set(engine,"validSubstructure",Enumerable.Range(0,cells).Select(i=>new IntVec3(i,0,0)).ToHashSet());
                plant.UpdateDesiredPowerOutput();
                Check.Equal(expected,plant.PowerOutput,"native spare-lift power at "+cells+" cells with empty flight tank");
                Check.Equal(0f,fuel.Fuel,"power does not consume flight fuel");
            }
            Set(engine,"validSubstructure",new HashSet<IntVec3>());
            Set(switchComp,"switchOnInt",false);plant.UpdateDesiredPowerOutput();Check.Equal(0f,plant.PowerOutput,"switch off stops generation");
            Set(switchComp,"switchOnInt",true);
            var breakdown=engine.GetComp<CompBreakdownable>();Set(breakdown,"brokenDownInt",true);Set(plant,"breakdownableComp",breakdown);
            plant.UpdateDesiredPowerOutput();Check.Equal(0f,plant.PowerOutput,"breakdown stops generation");
            Set(breakdown,"brokenDownInt",false);
        }
        finally {game.Scenario=oldScenario;StatDefOf.SubstructureSupport=oldStat; main.SetValue(null,oldMain);Current.ProgramState=oldState;}
    }
    public sealed class GravcoreVgeSave : IExposable
    {
        public ThingComp[] Comps=[];
        public void ExposeData() {for(int i=0;i<Comps.Length;i++) {Scribe.EnterNode("component"+i);Comps[i].PostExposeData();Scribe.ExitNode();}}
    }
}









