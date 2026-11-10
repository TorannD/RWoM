using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using Verse;
using UnityEngine;

namespace RWoM.RegressionTests;

internal static class OdysseyVgeOxygenTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.VGE.Oxygen.OptionalWithoutFramework", f => OdysseyOxygenTests.Run(f, Absent));
        yield return new("Odyssey.VGE.Oxygen.ConditionalXmlNativePatch", Definitions);
        yield return new("Odyssey.VGE.Oxygen.Chapter1NativeStoragePaymentAndSave", f => OdysseyOxygenTests.Run(f, () => Native(f, false)));
        yield return new("Odyssey.VGE.Oxygen.Chapter2NativeStoragePaymentAndSave", f => OdysseyOxygenTests.Run(f, () => Native(f, true)));
    }
    static Type T(string name) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + name) ?? throw new Exception("Missing " + name);
    static void Output(OdysseyOxygenTests.Pump p, string mode) => p.Oxygen.GetType().GetProperty("OutputMode")!.SetValue(p.Oxygen, Enum.Parse(T("OxygenOutputMode"), mode));
    static void Tick(OdysseyOxygenTests.Pump p, int tick)
    {
        typeof(TickManager).GetField("ticksGameInt", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Find.TickManager, tick);
        p.Oxygen.CompTick();
    }
    static void Near(double a, double b, string why) { if (Math.Abs(a-b) > .00001) throw new Exception($"{why}: {a} != {b}"); }
    static void Absent()
    {
        var p = new OdysseyOxygenTests.Pump(); p.Mana(10);
        Check.Equal("Atmosphere", p.Oxygen.GetType().GetProperty("OutputMode")!.GetValue(p.Oxygen)!.ToString()!, "old saves default to atmosphere");
        Output(p, "PipeNetwork"); Tick(p, 60);
        Near(10, p.Charge, "missing optional connector cannot consume mana");
        Check.Equal(false, (bool)p.Oxygen.GetType().GetProperty("PipeNetworkAvailable")!.GetValue(p.Oxygen)!, "no connector even if DLL was loaded by another test");
    }
    static bool SkipTexture() => false;
    static void Definitions(RuntimeFixture f)
    {
        Check.Equal(true, typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.VgeOxygenBridge") == null, "vendor oxygen bridge has left Content");
        var folders=System.Xml.Linq.XDocument.Load(Path.Combine(IntegrationPackageTests.Root(f),"LoadFolders.xml"));
        var folder=folders.Root!.Element("v1.7")!.Elements("li").Single(e=>e.Value=="v1.7/VGECompat");
        Check.Equal("vanillaexpanded.gravship",(string?)folder.Attribute("IfModActive"),"adapter only loaded with active VGE1");
        Check.Equal(false, XDocument.Load(Path.Combine(f.Repository,"LoadFolders.xml")).Descendants("li").Any(e=>e.Value.Contains("VGECompat")),"main no longer owns the adapter root");
        var about=System.Xml.Linq.XDocument.Load(Path.Combine(IntegrationPackageTests.Root(f),"About/About.xml"));
        foreach(var id in new[]{"vanillaexpanded.gravship","vanillaexpanded.gravship2"})
            Check.Equal(true,about.Root!.Element("loadAfter")!.Elements("li").Any(e=>e.Value==id),"optional load order "+id);
        Check.Equal(false,about.Descendants("modDependenciesByVersion").Descendants("packageId").Any(e=>e.Value.StartsWith("vanillaexpanded.gravship")),"no mandatory dependency");
        var defs=new System.Xml.XmlDocument(); defs.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Oxygen.xml"));
        var patches=new System.Xml.XmlDocument(); patches.Load(Path.Combine(IntegrationPackageTests.Root(f),"v1.7/VGECompat/Patches/Oxygen.xml"));
        foreach(System.Xml.XmlNode node in patches.SelectNodes("Patch/Operation")!)
            Check.Equal(true,DirectXmlToObject.ObjectFromXml<PatchOperation>(node,false).Apply(defs),"native patch succeeds");
        Check.Equal(1,defs.SelectNodes("Defs/ThingDef/comps/li[@Class='PipeSystem.CompProperties_Resource'][pipeNet='VGE_OxygenNet']")!.Count,"one real oxygen connector");
        Check.Equal(1,defs.SelectNodes("Defs/ThingDef/comps/li[compClass='TorannMagic.VGE.CompVgeOxygenOutput']")!.Count,"one external output adapter");
        Check.Equal(1,defs.SelectNodes("Defs/ThingDef/modExtensions/li[@Class='VEF.Buildings.GravshipLaunchExtension'][isOxygenPusher='true']")!.Count,"native launch recognition");
    }
    static void AttachOutput(OdysseyOxygenTests.Pump pump)
    {
        var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER") ?? throw new Exception("Missing VGE adapter build"));
        var output = (ThingComp)Activator.CreateInstance(adapter.GetType("TorannMagic.VGE.CompVgeOxygenOutput", true)!)!;
        output.parent = pump.Oxygen.parent; output.props = new CompProperties(output.GetType());
        pump.Oxygen.parent.AllComps.Add(output);
    }
    static void Native(RuntimeFixture f, bool chapter2)
    {
        string vef = "C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/2023507013/1.7/Assemblies";
        Assembly? Resolve(AssemblyLoadContext c, AssemblyName n) { string file = Path.Combine(vef,n.Name+".dll"); return File.Exists(file) ? c.LoadFromAssemblyPath(file) : null; }
        AssemblyLoadContext.Default.Resolving += Resolve;
        var oldDesignations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        FieldInfo? drainField = null; object? oldDrain = null;
        try
        {
            var pipe = Assembly.LoadFrom(Path.Combine(vef,"PipeSystem.dll"));
            string workshop = Path.GetFullPath(Path.Combine(vef,"../../.."));
            var vge = Assembly.LoadFrom(Path.Combine(workshop,"3609835606/1.7/Assemblies/VanillaGravshipExpanded.dll"));
            if (chapter2) Assembly.LoadFrom(Path.Combine(workshop,"3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll"));
            Type P(string n) => pipe.GetType("PipeSystem."+n)!;
            RuntimeLog.EnsureHeadlessDefOfInitialized(P("PSDefOf"));
            drainField = P("PSDefOf").GetField("PS_Drain")!; oldDrain = drainField.GetValue(null);
            var drain = new DesignationDef { defName = "PS_Drain", targetType = TargetType.Thing };
            DefDatabase<DesignationDef>.Clear(); DefDatabase<DesignationDef>.Add(drain); drainField.SetValue(null,drain);
            var harmonyAsm = Assembly.Load("0Harmony"); string id = "rwom.vge.oxygen.graphics";
            var h = Activator.CreateInstance(harmonyAsm.GetType("HarmonyLib.Harmony")!, id)!;
            var prefix = Activator.CreateInstance(harmonyAsm.GetType("HarmonyLib.HarmonyMethod")!, typeof(OdysseyVgeOxygenTests).GetMethod(nameof(SkipTexture),BindingFlags.NonPublic|BindingFlags.Static)!)!;
            try {
                h.GetType().GetMethod("Patch")!.Invoke(h,[typeof(ContentFinder<Texture2D>).GetMethod("Get",[typeof(string),typeof(bool)])!,prefix,null,null,null]);
                RuntimeHelpers.RunClassConstructor(P("CompResourceStorage").TypeHandle);
            } finally { h.GetType().GetMethod("UnpatchAll")!.Invoke(h,[id]); }
            var p = new OdysseyOxygenTests.Pump(); AttachOutput(p); p.Mana(10); Output(p,"PipeNetwork");
            var map = p.Oxygen.parent.Map;
            map.Biome.inVacuum = false; // Native VGE output remains paid and functional on planets.
            map.designationManager = new DesignationManager(map);
            var netDef = (Def)Activator.CreateInstance(P("PipeNetDef"))!; netDef.defName="VGE_OxygenNet";
            var net = Activator.CreateInstance(vge.GetType("VanillaGravshipExpanded.OxygenPipeNet")!)!;
            P("PipeNet").GetField("def")!.SetValue(net,netDef); P("PipeNet").GetField("map")!.SetValue(net,map);
            var connector = (ThingComp)Activator.CreateInstance(P("CompResource"))!;
            connector.parent=p.Oxygen.parent; connector.props=(CompProperties)Activator.CreateInstance(P("CompProperties_Resource"))!;
            connector.props.GetType().GetField("pipeNet")!.SetValue(connector.props,netDef);
            P("CompResource").GetProperty("PipeNet")!.SetValue(connector,net); p.Oxygen.parent.AllComps.Add(connector);
            var tank = new OdysseyOxygenTests.Pump(15).Oxygen.parent;
            var storage = (ThingComp)Activator.CreateInstance(P("CompResourceStorage"))!;
            storage.parent=tank; storage.props=(CompProperties)Activator.CreateInstance(P("CompProperties_ResourceStorage"))!;
            storage.props.GetType().GetField("storageCapacity")!.SetValue(storage.props,2f);
            storage.props.GetType().GetField("pipeNet")!.SetValue(storage.props,netDef);
            P("CompResource").GetProperty("PipeNet")!.SetValue(storage,net);
            ((System.Collections.IList)P("PipeNet").GetField("storages")!.GetValue(net)!).Add(storage);
            double Stored() => (float)P("CompResourceStorage").GetProperty("AmountStored")!.GetValue(storage)!;
            Tick(p,60); Near(1,Stored(),"paid oxygen enters real VEF storage"); Near(9.9,p.Charge,"one oxygen costs .1 mana");
            Tick(p,60); Near(1,Stored(),"same tick no replay");
            Tick(p,120); Near(2,Stored(),"second paid pulse");
            Tick(p,180); Near(9.8,p.Charge,"full tanks stop payment");
            P("CompResourceStorage").GetMethod("DrawResource")!.Invoke(storage,[.5f]);
            Tick(p,240); Near(2,Stored(),"fractional demand fills remaining capacity"); Near(9.75,p.Charge,"partial demand pays proportionally");
            Output(p,"Atmosphere"); P("CompResourceStorage").GetMethod("DrawResource")!.Invoke(storage,[1f]);
            Tick(p,300); Near(1,Stored(),"atmosphere mode never exports oxygen"); Near(9.75,p.Charge,"separate modes avoid double payment");
            Output(p,"PipeNetwork"); P("CompResource").GetProperty("PipeNet")!.SetValue(connector,null);
            Tick(p,360); Near(9.75,p.Charge,"disconnection stops spending");
            P("CompResource").GetProperty("PipeNet")!.SetValue(connector,net);
            typeof(CompFlickable).GetField("switchOnInt",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(p.Flick,false);
            Tick(p,420); Near(9.75,p.Charge,"switched off stops spending");
            typeof(CompFlickable).GetField("switchOnInt",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(p.Flick,true);
            p.Mode("Magicyte"); p.Fuel.Refuel(1);
            p.Oxygen.parent.AllComps.Add(new OdysseyOxygenTests.SignalComp { parent=p.Oxygen.parent, Action=s => { if(s=="RanOutOfFuel") P("CompResource").GetProperty("PipeNet")!.SetValue(connector,null); } });
            Tick(p,480); Near(1,Stored(),"callback disconnect prevents delivery"); Near(0,p.Fuel.Fuel,"crystal already paid"); Near(9.9,p.Credit,"unused crystal retained");
            Near(1,(double)p.Oxygen.GetType().GetProperty("PendingPipeOxygen")!.GetValue(p.Oxygen)!,"paid output retained when disconnected");
            var saved = new OdysseyOxygenTests.PumpSave { Value=p };
            string path=Path.Combine(f.WorkingDirectory,"vge-oxygen.xml");
            try { Scribe.saver.InitSaving(path,"regression"); Scribe_Deep.Look(ref saved,"pump"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            OdysseyOxygenTests.PumpSave loaded=null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded,"pump"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
            p=loaded.Value; Near(1,(double)p.Oxygen.GetType().GetProperty("PendingPipeOxygen")!.GetValue(p.Oxygen)!,"real Scribe preserves pending output");
            Check.Equal("PipeNetwork",p.Oxygen.GetType().GetProperty("OutputMode")!.GetValue(p.Oxygen)!.ToString()!,"output mode saved independently of fuel mode");
            connector.parent=p.Oxygen.parent; p.Oxygen.parent.AllComps.Add(connector); P("CompResource").GetProperty("PipeNet")!.SetValue(connector,net);
            Check.Equal(false, (bool)p.Oxygen.GetType().GetProperty("PipeNetworkAvailable")!.GetValue(p.Oxygen)!, "native connector alone cannot activate a missing integration");
            Tick(p,480); Near(1,Stored(),"missing output adapter cannot deliver"); Near(9.9,p.Credit,"missing output adapter cannot charge");
            Near(1,(double)p.Oxygen.GetType().GetProperty("PendingPipeOxygen")!.GetValue(p.Oxygen)!,"missing output adapter retains saved credit");
            AttachOutput(p);
            Tick(p,480); Near(1,Stored(),"load does not replay paid output in same pulse");
            Tick(p,540); Near(2,Stored(),"pending output resumes after load"); Near(9.9,p.Credit,"pending output does not pay again");
            P("CompResourceStorage").GetMethod("DrawResource")!.Invoke(storage,[.1f]);
            var secondStorage=(ThingComp)Activator.CreateInstance(P("CompResourceStorage"))!;
            secondStorage.parent=new OdysseyOxygenTests.Pump(20).Oxygen.parent;
            secondStorage.props=(CompProperties)Activator.CreateInstance(P("CompProperties_ResourceStorage"))!;
            secondStorage.props.GetType().GetField("storageCapacity")!.SetValue(secondStorage.props,10f);
            secondStorage.props.GetType().GetField("pipeNet")!.SetValue(secondStorage.props,netDef);
            P("CompResource").GetProperty("PipeNet")!.SetValue(secondStorage,net);
            ((System.Collections.IList)P("PipeNet").GetField("storages")!.GetValue(net)!).Add(secondStorage);
            Tick(p,600); Near(2,Stored(),"small first tank deficit filled");
            Near(.9,(float)P("CompResourceStorage").GetProperty("AmountStored")!.GetValue(secondStorage)!,"remaining pulse capacity reaches second tank");
            Near(9.8,p.Credit,"one full pulse paid across both tanks");
            var tankXml = System.Xml.Linq.XDocument.Load(Path.Combine(workshop,chapter2?"3799737423/1.7/Defs/NetDefs/OxygenNet.xml":"3609835606/1.7/Defs/NetDefs/OxygenNet.xml"));
            Check.Equal(true,tankXml.Descendants("li").Any(e=>(string?)e.Attribute("Class")=="PipeSystem.CompProperties_ResourceStorage"&&(string?)e.Element("pipeNet")=="VGE_OxygenNet"),"installed chapter tanks use tested storage contract");
        }
        finally {
            drainField?.SetValue(null,oldDrain);
            DefDatabase<DesignationDef>.Clear(); foreach(var d in oldDesignations) DefDatabase<DesignationDef>.Add(d);
            AssemblyLoadContext.Default.Resolving -= Resolve;
        }
    }
}
