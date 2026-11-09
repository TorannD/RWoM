using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.Resources;
using Verse;
using RimWorld.Planet;

namespace RWoM.RegressionTests;

internal static class MagicyteGenerationTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Magicyte.Generation.NativeOreInheritance", Ore);
        yield return new("Magicyte.Generation.NativePatchAndOptionalAsteroid", Asteroid);
        yield return new("Magicyte.Generation.BoundedSeparatedVeins", Veins);
        yield return new("Magicyte.Generation.SevenLanguages", Locales);
        yield return new("Magicyte.Generation.AsteroidNativeBoundaryAndNoRetrofit", Boundary);
        yield return new("Magicyte.Generation.NativeMapSelectionAndPlacement", Placement);
    }
    static XmlDocument Read(string path) { var d = new XmlDocument(); d.Load(path); return d; }
    static string Game => Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
    static readonly List<IntVec3> placed=[];
    static readonly List<Thing> removed=[];
    static bool SpawnBoundary(ThingDef def,IntVec3 loc,Map map,ref Thing __result)
    {
        Check.Equal("TM_MineableMagicyte",def.defName,"only intended replacement spawns");
        placed.Add(loc);__result=null!;return false;
    }
    static bool DestroyBoundary(Building __instance,DestroyMode mode)
    { Check.Equal(DestroyMode.Vanish,mode,"host replacement produces no mining loot");removed.Add(__instance);return false; }
    static void Placement(RuntimeFixture f)
    {
        using var patches=new Patches();
        var previous=MapGenerator.mapBeingGenerated;
        var old=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));map.info=new MapInfo{Size=new IntVec3(30,1,30)};
        map.cellIndices=new CellIndices(map);map.edificeGrid=new EdificeGrid(map);map.listerThings=new ListerThings(ListerThingsUse.Global);
        ThingDef Def(string name,bool resource)
        {
            var d=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));d.defName=name;d.building=new BuildingProperties{isNaturalRock=true,isResourceRock=resource};d.virtualDefs=[];return d;
        }
        var host=Def("host",false);var otherOre=Def("otherOre",true);var ore=Def("TM_MineableMagicyte",true);
        var method=typeof(GenStep_MagicyteVeins).GetMethod("GenerateVeins",BindingFlags.NonPublic|BindingFlags.Static)!;
        try
        {
            DefDatabase<ThingDef>.Clear();DefDatabase<ThingDef>.Add(ore);
            patches.Prefix(typeof(GenSpawn).GetMethod("Spawn",[typeof(ThingDef),typeof(IntVec3),typeof(Map),typeof(WipeMode)])!,nameof(SpawnBoundary));
            patches.Prefix(typeof(Building).GetMethod("Destroy")!,nameof(DestroyBoundary));
            foreach(var cell in map.AllCells)
            {
                var b=(Building)RuntimeHelpers.GetUninitializedObject(typeof(Building));b.def=cell.x<5?otherOre:host;
                map.edificeGrid.InnerArray[map.cellIndices.CellToIndex(cell)]=b;
            }
            placed.Clear();removed.Clear();
            MapGenerator.mapBeingGenerated=null;
            method.Invoke(null,[map,2]);Check.Equal(0,placed.Count,"saved map never modified");
            MapGenerator.mapBeingGenerated=map;Rand.PushState(91);
            try { method.Invoke(null,[map,2]); } finally { Rand.PopState(); }
            Check.Equal(true,placed.Count>=6&&placed.Count<=16,"two bounded land veins delivered to native spawn boundary");
            Check.Equal(placed.Count,removed.Count,"one host removed per spawned ore");
            Check.Equal(true,removed.All(t=>t.def==host)&&placed.All(c=>c.InBounds(map)&&c.x>=5),"existing ore strip untouched, in-map natural hosts only");
            int count=placed.Count;
            map.listerThings.ThingsOfDef(ore).Add((Thing)RuntimeHelpers.GetUninitializedObject(typeof(Thing)));
            method.Invoke(null,[map,2]);Check.Equal(count,placed.Count,"existing magicyte prevents repeated generation");
        }
        finally { MapGenerator.mapBeingGenerated=previous;DefDatabase<ThingDef>.Clear();foreach(var d in old)DefDatabase<ThingDef>.Add(d);placed.Clear();removed.Clear(); }
    }
    static int nativeCalls, veinCalls, requestedVeins;
    static bool throwNative;
    static bool NativeBoundary(Map map)
    {
        nativeCalls++;
        Check.Equal<ThingDef?>(null,((SpaceMapParent)map.ParentHolder).preciousResource,"native ordinary scatter cannot select magicyte");
        if(throwNative) throw new InvalidOperationException("controlled native generation failure");
        return false;
    }
    static bool VeinBoundary(int count) { veinCalls++; requestedVeins=count; return false; }
    static void Boundary(RuntimeFixture f)
    {
        using var h=new Patches();
        var previous=MapGenerator.mapBeingGenerated;
        var parent=(SpaceMapParent)RuntimeHelpers.GetUninitializedObject(typeof(SpaceMapParent));
        parent.preciousResource=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        var original=parent.preciousResource;
        parent.def=new WorldObjectDef{defName="TM_MagicyteAsteroid"};
        var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));map.info=new MapInfo{parent=parent};
        nativeCalls=veinCalls=0;throwNative=false;
        try
        {
            h.Prefix(typeof(GenStep_Asteroid).GetMethod("Generate")!,nameof(NativeBoundary));
            h.Prefix(typeof(GenStep_MagicyteVeins).GetMethod("GenerateVeins",BindingFlags.Static|BindingFlags.NonPublic)!,nameof(VeinBoundary));
            var step=new GenStep_MagicyteAsteroid();
            var lateStep=new GenStep_MagicyteVeins{richAsteroid=true};
            MapGenerator.mapBeingGenerated=null;
            step.Generate(map,default);
            new GenStep_MagicyteVeins().Generate(map,default);
            lateStep.Generate(map,default);
            Check.Equal(0,nativeCalls+veinCalls,"existing maps cannot be retrofitted");
            MapGenerator.mapBeingGenerated=map;
            Rand.PushState(8923);
            try
            {
                for(int i=0;i<100;i++)
                {
                    step.Generate(map,default);
                    Check.Equal(i,veinCalls,"terrain pass never adds early magicyte");
                    lateStep.Generate(map,default);
                    Check.Equal(true,requestedVeins>=3&&requestedVeins<=5,"rich asteroid caps at five veins");
                    Check.Equal(original,parent.preciousResource,"location resource identity restored");
                }
            }
            finally { Rand.PopState(); }
            Check.Equal(100,nativeCalls,"native asteroid generation called exactly once per fresh invocation");
            Check.Equal(100,veinCalls,"one bounded magicyte pass");
            parent.def.defName="AsteroidBasic";
            lateStep.Generate(map,default);
            Check.Equal(100,veinCalls,"ordinary asteroids reject rich resource pass");
            parent.def.defName="TM_MagicyteAsteroid";
            throwNative=true;
            try { step.Generate(map,default); throw new Exception("expected controlled failure"); }
            catch(InvalidOperationException) { }
            Check.Equal(original,parent.preciousResource,"identity also restored after native failure");
            Check.Equal(100,veinCalls,"no reward pass after native failure");
        }
        finally { throwNative=false;MapGenerator.mapBeingGenerated=previous; }
    }
    static void Ore(RuntimeFixture f)
    {
        XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear();
        try
        {
            var vanilla = Read(Path.Combine(Game,"Data/Core/Defs/ThingDefs_Buildings/Buildings_Natural.xml"));
            foreach(var name in new[]{"BuildingNaturalBase","RockBase"}) XmlInheritance.TryRegister(vanilla.SelectSingleNode($"Defs/ThingDef[@Name='{name}']")!,null);
            var node = Read(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/Buildings_MagicyteOre.xml")).SelectSingleNode("Defs/ThingDef")!;
            XmlInheritance.TryRegister(node,null); XmlInheritance.Resolve();
            var resolved=XmlInheritance.GetResolvedNodeFor(node);
            Check.Equal("false",resolved.SelectSingleNode("saveCompressible")!.InnerText,"saved contents cannot use compressed rock grids");
            Check.Equal(typeof(MineableMagicyte),ParseHelper.FromString<Type>(resolved.SelectSingleNode("thingClass")!.InnerText),"actual production mineable type");
            var building=DirectXmlToObject.ObjectFromXml<BuildingProperties>(resolved.SelectSingleNode("building")!,false);
            Check.Equal(true,building.isNaturalRock && building.isResourceRock && building.veinMineable,"native mineable contracts");
            Check.Equal(0f,building.mineableScatterCommonality,"no uncapped native ore scatter");
            Check.Equal(true,building.mineablePreventMeteorite,"meteorites cannot add extra deposits");
            Check.Equal(false,building.mineableYieldWasteable,"fixed extraction contents");
            Check.Equal(3,building.mineableScatterLumpSizeRange.min,"minimum deposit");
            Check.Equal(8,building.mineableScatterLumpSizeRange.max,"maximum deposit");
            Check.Equal("RawMagicyte",resolved.SelectSingleNode("building/mineableThing")!.InnerText,"resource reference");
            Check.Equal("None",resolved.SelectSingleNode("graphicData/linkType")!.InnerText,"not a rock atlas");
            Check.Equal(null,resolved.SelectSingleNode("graphicData/linkFlags"),"no inherited atlas flags");
        }
        finally { XmlInheritance.Clear(); DirectXmlCrossRefLoader.Clear(); }
    }
    static void Asteroid(RuntimeFixture f)
    {
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        var oldThings=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var oldWorld=DefDatabase<WorldObjectDef>.AllDefsListForReading.ToArray();
        var oldLayers=DefDatabase<PlanetLayerDef>.AllDefsListForReading.ToArray();
        try
        {
            var common=Read(Path.Combine(Game,"Data/Core/Defs/MapGeneration/CommonMapGenerator.xml"));
            var patch=DirectXmlToObject.ObjectFromXml<PatchOperation>(Read(Path.Combine(f.Repository,"v1.7/Patches/MagicyteGeneration.xml")).SelectSingleNode("Patch/Operation")!,false);
            Check.Equal(true,patch.Apply(common),"native patch applies to installed game");
            var landNode=Read(Path.Combine(Game,"Data/Core/Defs/MapGeneration/BasePlayerMapGenerator.xml")).SelectSingleNode("Defs/MapGeneratorDef")!;
            XmlInheritance.TryRegister(common.SelectSingleNode("Defs/MapGeneratorDef[@Name='MapCommonBase']")!,null);
            XmlInheritance.TryRegister(landNode,null);
            var space=Read(Path.Combine(Game,"Data/Odyssey/Defs/MapGeneration/SpaceMapGenerator.xml"));
            XmlInheritance.TryRegister(space.SelectSingleNode("Defs/MapGeneratorDef[@Name='SpaceMapGenerator']")!,null);
            var rich=Read(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/MagicyteAsteroid.xml"));
            var richMap=rich.SelectSingleNode("Defs/MapGeneratorDef")!;
            XmlInheritance.TryRegister(richMap,null); XmlInheritance.Resolve();
            Check.Equal(1,XmlInheritance.GetResolvedNodeFor(landNode).SelectNodes("genSteps/li[text()='TM_MagicyteVeins']")!.Count,"land inheritance includes bounded pass exactly once");
            var resolved=XmlInheritance.GetResolvedNodeFor(richMap);
            Check.Equal(0,resolved.SelectNodes("genSteps/li[text()='TM_MagicyteVeins']")!.Count,"space never inherits terrestrial pass");
            Check.Equal(1,resolved.SelectNodes("genSteps/li[text()='Space']")!.Count,"native space foundation retained");
            var location=DirectXmlToObject.ObjectFromXml<GeneratedLocationDef>(rich.SelectSingleNode("Defs/GeneratedLocationDef")!,false);
            Check.Equal(0.15f,location.weight,"rare optional location weight");
            var step=DirectXmlToObject.ObjectFromXml<GenStep>(rich.SelectSingleNode("Defs/GenStepDef/genStep")!,false);
            Check.Equal(typeof(GenStep_MagicyteAsteroid),step.GetType(),"bounded asteroid generator resolves");
            Check.Equal(true,step is GenStep_BasicAsteroid,"preserves native asteroid terrain generation");
            Check.Equal(7,((GenStep_Asteroid)step).mineableCounts.Count,"ordinary ore pool preserved separately");
            Check.Equal("1800",rich.SelectSingleNode("Defs/GenStepDef[defName='TM_MagicyteAsteroidVeins']/order")!.InnerText,"rich ore pass follows late structures and landing marker");
            Check.Equal("1800",Read(Path.Combine(f.Repository,"v1.7/Defs/ThingDefs/Buildings_MagicyteOre.xml")).SelectSingleNode("Defs/GenStepDef/order")!.InnerText,"land ore pass follows roads and clearing");
            var late=DirectXmlToObject.ObjectFromXml<GenStep>(rich.SelectSingleNode("Defs/GenStepDef[defName='TM_MagicyteAsteroidVeins']/genStep")!,false);
            Check.Equal(true,((GenStep_MagicyteVeins)late).richAsteroid,"dedicated late rich mode parsed natively");
            DefDatabase<ThingDef>.Clear();DefDatabase<WorldObjectDef>.Clear();DefDatabase<PlanetLayerDef>.Clear();
            foreach(string name in new[]{"TM_MineableMagicyte","MineableGold","MineableSilver","MineableSteel","MineableUranium","MineablePlasteel","MineableComponentsIndustrial","MineableJade"})
            {
                var resource=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));resource.defName=name;resource.virtualDefs=[];DefDatabase<ThingDef>.Add(resource);
            }
            var worldDef=new WorldObjectDef{defName="TM_MagicyteAsteroid"};DefDatabase<WorldObjectDef>.Add(worldDef);
            var orbit=new PlanetLayerDef{defName="Orbit"};DefDatabase<PlanetLayerDef>.Add(orbit);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            Check.Equal(worldDef,location.worldObjectDef,"native location resolves dedicated world object");
            Check.Equal(orbit,location.LayerDefs.Single(),"native location restricted to orbit");
            Check.Equal(DefDatabase<ThingDef>.GetNamed("TM_MineableMagicyte"),location.preciousResources.Single(),"native resource selection has exactly one candidate: magicyte");
            Check.Equal(true,((GenStep_Asteroid)step).mineableCounts.All(x=>x.mineable!=null&&x.mineable.defName!="TM_MineableMagicyte"),"native ordinary resource references resolve without uncapped magicyte");
            Check.Equal("TM_MineableMagicyte",rich.SelectSingleNode("Defs/GeneratedLocationDef/preciousResources/li")!.InnerText,"native location identity uses magicyte");
            var folder=XDocument.Load(Path.Combine(f.Repository,"LoadFolders.xml")).Root!.Element("v1.7")!.Elements("li").Single(x=>x.Value=="v1.7/Odyssey");
            Check.Equal("ludeon.rimworld.odyssey",(string?)folder.Attribute("IfModActive"),"true optional content");
        }
        finally
        {
            DirectXmlCrossRefLoader.Clear();XmlInheritance.Clear();
            DefDatabase<ThingDef>.Clear();foreach(var d in oldThings)DefDatabase<ThingDef>.Add(d);
            DefDatabase<WorldObjectDef>.Clear();foreach(var d in oldWorld)DefDatabase<WorldObjectDef>.Add(d);
            DefDatabase<PlanetLayerDef>.Clear();foreach(var d in oldLayers)DefDatabase<PlanetLayerDef>.Add(d);
        }
    }
    static void Veins(RuntimeFixture f)
    {
        var method=typeof(GenStep_MagicyteVeins).GetMethod("PlanVeins",BindingFlags.Static|BindingFlags.NonPublic)!;
        var eligible=typeof(GenStep_MagicyteVeins).GetMethod("EligibleRock",BindingFlags.Static|BindingFlags.NonPublic)!;
        var rock=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); rock.building=new BuildingProperties{isNaturalRock=true};
        Check.Equal(true,(bool)eligible.Invoke(null,[rock])!,"natural host eligible");
        rock.building.isResourceRock=true;
        Check.Equal(false,(bool)eligible.Invoke(null,[rock])!,"existing ores preserved");
        rock.building.isNaturalRock=false;rock.building.isResourceRock=false;
        Check.Equal(false,(bool)eligible.Invoke(null,[rock])!,"buildings preserved");
        var cells=(from x in Enumerable.Range(0,30) from z in Enumerable.Range(0,30) select new IntVec3(x,0,z)).ToList();
        foreach(var count in new[]{0,1,2,3,4,5})
        for(int seed=0;seed<40;seed++)
        {
            Rand.PushState(seed);
            try
            {
                var veins=(List<List<IntVec3>>)method.Invoke(null,[cells,count])!;
                Check.Equal(count,veins.Count,"requested capped count on ample host rock");
                foreach(var vein in veins)
                {
                    Check.Equal(true,vein.Count>=3 && vein.Count<=8,"3..8 tile deposit");
                    Check.Equal(true,vein.All(cells.Contains),"never outside available map rock");
                    var visited=new HashSet<IntVec3>{vein[0]};
                    for(int i=0;i<8;i++) foreach(var c in vein) if(GenAdj.CardinalDirections.Any(o=>visited.Contains(c+o)))visited.Add(c);
                    Check.Equal(vein.Count,visited.Count,"cardinally connected vein");
                    foreach(var other in veins.Where(v=>v!=vein))
                    Check.Equal(false,vein.Any(c=>other.Any(o=>Math.Abs(c.x-o.x)<=1 && Math.Abs(c.z-o.z)<=1)),"veins cannot merge even diagonally");
                }
            }
            finally { Rand.PopState(); }
        }
        Rand.PushState(12);
        try { Check.Equal(0,((List<List<IntVec3>>)method.Invoke(null,[new List<IntVec3>{new(0,0,0),new(1,0,0)},5])!).Count,"never spawns undersized veins in scarce rock"); }
        finally { Rand.PopState(); }
    }
    static void Locales(RuntimeFixture f)
    {
        foreach(var language in new[]{"English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"})
        foreach(var tuple in new[]{("v1.7","ThingDef","TM_MineableMagicyte",2),("v1.7/Odyssey","WorldObjectDef","TM_MagicyteAsteroid",2),("v1.7/Odyssey","MapGeneratorDef","TM_MagicyteAsteroid",1)})
        {
            var values=XDocument.Load(Path.Combine(f.Repository,tuple.Item1,"Languages",language,"DefInjected",tuple.Item2,"TM_MagicyteOre.xml")).Root!.Elements().ToList();
            Check.Equal(tuple.Item4,values.Count,language+" translation count");
            Check.Equal(true,values.All(x=>x.Name.LocalName.StartsWith(tuple.Item3+".")&&!string.IsNullOrWhiteSpace(x.Value)),language+" translated definitions");
        }
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly=Assembly.Load("0Harmony"); readonly object harmony;
        readonly string id="rwom.tests.magicyte.generation."+Guid.NewGuid();
        public Patches()=>harmony=Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!,id)!;
        public void Prefix(MethodBase original,string method)
        {
            var prefix=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,typeof(MagicyteGenerationTests).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[original,prefix,null,null,null]);
        }
        public void Dispose()=>harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
    }
}

