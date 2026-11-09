using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.SketchGen;
using TorannMagic.Odyssey;
using Verse;
using System.Xml.Linq;
namespace RWoM.RegressionTests;
internal static class MagicalStarterShipTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.StartScenario.NativeArrivalAndScopedResolver", f => {
            var type = typeof(MagicalThrusterFuel).Assembly.GetType("TorannMagic.Odyssey.ScenPart_MagicalShipArrival");
            Check.Equal(true, type != null, "magical arrival exists");
            Check.Equal(typeof(ScenPart_PlayerPawnsArriveMethod), type!.BaseType!, "reuse native pawn and item arrival");
        });
        yield return new("Odyssey.StartScenario.LayoutAllRotations", f => WithDefinitions(() => Layout()));
        yield return new("Odyssey.StartScenario.ScopedResolverIsolation", f => ScopedResolver());
        yield return new("Odyssey.StartScenario.RealNetworkAndCapture", f => WithDefinitions(() => OdysseyBeaconTests.StarterShipNetwork(f)));
        yield return new("Odyssey.StartScenario.InitialFuelAndScopeCleanup", f => ArrivalBoundary());
        yield return new("Odyssey.StartScenario.NativeInteractionReachability", f => WithDefinitions(() => Interactions(f)));
    }
    static ThingDef Definition(string name, int x = 1, int z = 1)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = name; d.size = new IntVec2(x,z); d.thingClass = typeof(Building); d.category = ThingCategory.Building;
        d.stackLimit=1; d.rotatable=true; d.building=new BuildingProperties(); d.comps=[]; d.virtualDefs=[];
        if(name=="GravshipHull" || name=="Door" || name=="TM_SmallMagicalThruster") {
            d.holdsRoof=true;d.passability=Traversability.Impassable;d.building.isEdifice=true;
        }
        return d;
    }
    internal static void WithDefinitions(Action action)
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(TerrainDefOf));
        var names = new[]{"Steel", "Door", "GravshipHull", "PilotConsole", "Shelf"};
        var fields=names.Select(n=>typeof(ThingDefOf).GetField(n)!).ToArray();
        var old=fields.Select(x=>x.GetValue(null)).ToArray();
        var oldFloor=TerrainDefOf.Substructure;
        var oldDefs=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try {
            for(int i=0;i<names.Length;i++) fields[i].SetValue(null,Definition(names[i], names[i]=="PilotConsole"?3:names[i]=="Shelf"?2:1,names[i]=="PilotConsole"?2:1));
            TerrainDefOf.Substructure=(TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef));
            TerrainDefOf.Substructure.defName="Substructure";
            foreach(var d in new[]{Definition("TM_MagicalGravEngine",3,3),Definition("TM_SmallMagicalThruster",1,2),Definition("TM_ManaBeacon"),Definition("TM_MagofuelReservoir",2,2)}) DefDatabase<ThingDef>.Add(d);
            action();
        } finally {
            for(int i=0;i<fields.Length;i++)fields[i].SetValue(null,old[i]); TerrainDefOf.Substructure=oldFloor;
            DefDatabase<ThingDef>.Clear();foreach(var d in oldDefs)DefDatabase<ThingDef>.Add(d);
        }
    }
    static void ScopedResolver()
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(SketchResolverDefOf));
        var field=typeof(ScenPart_MagicalShipArrival).GetField("active",BindingFlags.NonPublic|BindingFlags.Static)!;
        var oldActive=field.GetValue(null);var oldRoot=SketchResolverDefOf.Gravship;
        var oldDefs=DefDatabase<SketchResolverDef>.AllDefsListForReading.ToArray();
        try {
            var native=new SketchResolverDef {defName="RegressionNativeShip"};
            var custom=new SketchResolverDef {defName="TM_MagicalStarterShip"};
            var other=new SketchResolverDef {defName="RegressionOtherSketch"};
            DefDatabase<SketchResolverDef>.Add(custom);SketchResolverDefOf.Gravship=native;
            field.SetValue(null,null);var root=native;var parms=new SketchResolveParams {sketch=new Sketch()};
            MagicalStarterShipSketchPatch.Prefix(ref root,ref parms);
            Check.Same(native,root,"ordinary gravship arrival unchanged");
            field.SetValue(null,new ScenPart_MagicalShipArrival {includeReservoir=true});
            root=other;MagicalStarterShipSketchPatch.Prefix(ref root,ref parms);
            Check.Same(other,root,"nested unrelated sketch unchanged");
            root=native;MagicalStarterShipSketchPatch.Prefix(ref root,ref parms);
            Check.Same(custom,root,"only own arrival substitutes root");
            Check.Equal(true,parms.GetCustom<bool>("TM_StarterReservoir"),"variant passed without global definition mutation");
        } finally {
            field.SetValue(null,oldActive);SketchResolverDefOf.Gravship=oldRoot;
            DefDatabase<SketchResolverDef>.Clear();foreach(var d in oldDefs)DefDatabase<SketchResolverDef>.Add(d);
        }
    }
    static Action<Map>? nativeArrivalBoundary;
    static bool SkipNativeArrival(Map map) {nativeArrivalBoundary!(map);return false;}
    static void ArrivalBoundary()
    {
        var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));map.listerThings=new ListerThings(ListerThingsUse.Global);
        CompRefuelable Fuel(string name,float capacity) {
            var thing=new Building {def=Definition(name)};
            var fuel=new CompRefuelable {parent=thing,props=new CompProperties_Refuelable{fuelCapacity=capacity}};
            typeof(ThingWithComps).GetField("comps",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(thing,new List<ThingComp>{fuel});
            fuel.Refuel(capacity);return fuel;
        }
        var existing=Fuel("TM_MagofuelReservoir",500);map.listerThings.AllThings.Add(existing.parent);
        var core=Fuel("TM_MagicalGravEngine",100);var tank=Fuel("TM_MagofuelReservoir",500);
        var active=typeof(ScenPart_MagicalShipArrival).GetField("active",BindingFlags.NonPublic|BindingFlags.Static)!;
        var previous=active.GetValue(null);var outer=new ScenPart_MagicalShipArrival();active.SetValue(null,outer);
        var patchId="rwom.regression.starter-arrival."+Guid.NewGuid();
        var ha=Assembly.Load("0Harmony");var ht=ha.GetType("HarmonyLib.Harmony")!;var harmony=Activator.CreateInstance(ht,patchId)!;
        try {
            var hm=Activator.CreateInstance(ha.GetType("HarmonyLib.HarmonyMethod")!,typeof(MagicalStarterShipTests).GetMethod(nameof(SkipNativeArrival),BindingFlags.NonPublic|BindingFlags.Static)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(ScenPart_PlayerPawnsArriveMethod).GetMethod("GenerateIntoMap")!,hm,null,null,null]);
            var arrival=new ScenPart_MagicalShipArrival{includeReservoir=true};
            nativeArrivalBoundary=m=> {Check.Same(arrival,active.GetValue(null)!,"scope active during native arrival");m.listerThings.AllThings.Add(core.parent);m.listerThings.AllThings.Add(tank.parent);};
            arrival.GenerateIntoMap(map);
            Check.Equal(500f,existing.Fuel,"preexisting map tank untouched");Check.Equal(100f,core.Fuel,"core receives only native hundred startup fuel");Check.Equal(100f,tank.Fuel,"scenario tank trimmed to finite hundred");
            Check.Same(outer,active.GetValue(null)!,"outer scope restored after success");
            tank.ConsumeFuel(75);nativeArrivalBoundary=m=>{};arrival.GenerateIntoMap(map);
            Check.Equal(25f,tank.Fuel,"repeated empty arrival cannot refill spent fuel");
            nativeArrivalBoundary=m=>throw new InvalidOperationException("fixture arrival failure");
            try {arrival.GenerateIntoMap(map);throw new Exception("failure must propagate");}catch(InvalidOperationException ex)when(ex.Message=="fixture arrival failure"){}
            Check.Same(outer,active.GetValue(null)!,"outer scope restored after failed native arrival");
        } finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[patchId]);active.SetValue(null,previous);nativeArrivalBoundary=null;}
    }
    static void Interactions(RuntimeFixture f)
    {
        string game=Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var paths=Directory.GetFiles(Path.Combine(game,"Data/Core/Defs"),"*.xml",SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(game,"Data/Odyssey/Defs/ThingDefs_Buildings"),"*.xml"))
            .Concat(Directory.GetFiles(Path.Combine(f.Repository,"v1.7/Odyssey/Defs"),"Buildings*.xml"))
            .Append(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/TM_BuildingBase.xml"));
        var nodes=paths.SelectMany(p=>XDocument.Load(p).Root!.Elements("ThingDef")).ToArray();
        string? Value(XElement node,string key) {
            if(node.Element(key) is XElement value)return value.Value;
            var parent=(string?)node.Attribute("ParentName");
            return parent==null?null:Value(nodes.Single(n=>(string?)n.Attribute("Name")==parent),key);
        }
        foreach(bool tank in new[]{false,true}) foreach(var rotation in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West}) {
            var sketch=SketchResolver_MagicalStarterShip.Build(tank);sketch.Rotate(rotation);
            foreach(var def in sketch.Things.Select(t=>t.def).Distinct()) {
                var node=nodes.Single(n=>(string?)n.Element("defName")==def.defName);
                def.passability=Enum.Parse<Traversability>(Value(node,"passability")??"Standable");
                def.hasInteractionCell=bool.Parse(Value(node,"hasInteractionCell")??"false");
                if(def.hasInteractionCell)def.interactionCellOffset=IntVec3.FromString(Value(node,"interactionCellOffset")!);
            }
            var floors=sketch.Terrain.Select(t=>t.pos).ToHashSet();
            var blocked=sketch.Things.Where(t=>t.def.passability==Traversability.Impassable&&t.def!=ThingDefOf.Door).SelectMany(t=>t.OccupiedRect).ToHashSet();
            foreach(var door in sketch.Things.Where(t=>t.def==ThingDefOf.Door)) {
                var reached=new HashSet<IntVec3>();var pending=new Queue<IntVec3>();pending.Enqueue(door.pos);
                while(pending.Count>0) {var cell=pending.Dequeue();if(!reached.Add(cell))continue;foreach(var dir in GenAdj.CardinalDirections){var next=cell+dir;if(floors.Contains(next)&&!blocked.Contains(next)&&!reached.Contains(next))pending.Enqueue(next);}}
                foreach(var target in sketch.Things.Where(t=>t.def.hasInteractionCell)) {
                    var native=new Building {def=target.def,Position=target.pos,Rotation=target.rot};
                    Check.Equal(false,blocked.Contains(native.InteractionCell),"interaction cell unobstructed: "+target.def.defName+" "+rotation+" "+native.InteractionCell);
                    Check.Equal(true,reached.Contains(native.InteractionCell),"native interaction cell reachable from either door: "+target.def.defName+" "+rotation);
                }
            }
        }
    }
    static void Layout() {
            foreach(bool tank in new[]{false,true}) foreach(var rotation in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West}) {
                var s=SketchResolver_MagicalStarterShip.Build(tank); s.Rotate(rotation);
                var floors=s.Terrain.Select(t=>t.pos).ToHashSet();
                Check.Equal(95,floors.Count,"small ship has 95 floor cells");
                var visited=new HashSet<IntVec3>(); var queue=new Queue<IntVec3>();queue.Enqueue(floors.First());
                while(queue.Count>0) {var c=queue.Dequeue();if(!visited.Add(c)) continue;foreach(var dir in GenAdj.CardinalDirections)if(floors.Contains(c+dir)&&!visited.Contains(c+dir))queue.Enqueue(c+dir);}
                Check.Equal(floors.Count,visited.Count,"all floors connected");
                var occupied=new HashSet<IntVec3>();
                foreach(var thing in s.Things) foreach(var c in thing.OccupiedRect) {
                    Check.Equal(true,floors.Contains(c),"building entirely on substructure: "+thing.def.defName);
                    Check.Equal(true,occupied.Add(c),"no overlapping buildings: "+thing.def.defName+" "+c);
                }
                Check.Equal(1,s.Things.Count(t=>t.def.defName=="TM_MagicalGravEngine"),"one core");
                Check.Equal(tank?1:0,s.Things.Count(t=>t.def.defName=="TM_MagofuelReservoir"),"only VGE adds tank");
                Check.Equal(6,s.Things.Count(t=>t.def==ThingDefOf.Shelf),"native item placement has six shelves");
                Check.Equal(2,s.Things.Count(t=>t.def==ThingDefOf.Door),"two accessible side doors");
                Check.Equal(28,s.Things.Count(t=>t.def==ThingDefOf.GravshipHull),"equipment cannot silently wipe hull cells during sketch assembly");
                foreach(var thruster in s.Things.Where(t=>t.def.defName=="TM_SmallMagicalThruster")) {
                    var aft=thruster.rot.Opposite.FacingCell;
                    for(int n=1;n<=5;n++) Check.Equal(false,floors.Contains(thruster.pos+aft*n),"five cells of clear exhaust");
                }
                var beacons=s.Things.Where(t=>t.def.defName=="TM_ManaBeacon").ToArray();
                Check.Equal(2,beacons.Length,"two beacons");
                Check.Equal(true,beacons[0].pos.DistanceToSquared(beacons[1].pos)<=25,"beacons share a five-cell network");
                foreach(var device in s.Things.Where(t=>t.def.defName.StartsWith("TM_")&&t.def.defName!="TM_ManaBeacon"))
                    Check.Equal(true,beacons.Any(b=>device.OccupiedRect.Any(c=>c.DistanceToSquared(b.pos)<=25)),"all devices reach beacon network: "+device.def.defName);
                var roofs=s.GetSuggestedRoofCells().ToHashSet();
                Check.Equal(true,s.Things.Single(t=>t.def.defName=="TM_MagicalGravEngine").OccupiedRect.All(roofs.Contains),"native suggested roof covers core interior");
                foreach(var thruster in s.Things.Where(t=>t.def.defName=="TM_SmallMagicalThruster")) {
                    var props=new CompProperties_GravshipThruster {exclusionAreaSize=new IntVec3(1,0,5),exclusionAreaOffset=new IntVec3(0,0,-5)};
                    var exhaust=new List<IntVec3>();props.GetExclusionZone(thruster.pos,thruster.rot,ref exhaust);
                    Check.Equal(true,exhaust.All(c=>!floors.Contains(c)&&!occupied.Contains(c)),"native exhaust exclusion stays outside entire ship");
                    Check.Equal(true,GenAdj.CellsAdjacentAlongEdge(thruster.pos,thruster.rot,thruster.def.size,thruster.rot.Opposite.AsInt).All(c=>!floors.Contains(c)&&!roofs.Contains(c)),"native outdoor check probes external unroofed cells");
                }
            }
    }
}




