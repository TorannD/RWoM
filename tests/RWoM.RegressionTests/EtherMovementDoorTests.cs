using System.Reflection;
using RimWorld;
using TorannMagic.Odyssey;
using Verse;
using Verse.AI;
namespace RWoM.RegressionTests;
internal static class EtherMovementDoorTests
{
    public static IEnumerable<TestCase> Cases() { yield return new("EtherMovement.NativeDoorOpeningAndForbidden", TeleportSpiritTests.EtherDoor); }
}
internal static partial class TeleportSpiritTests
{
    public static void EtherDoor(RuntimeFixture f) => CoreReturnWorld(f,w=>
    {
        _=CoreReturnCore(w);var map=w.Map;
        map.terrainGrid=new TerrainGrid(map);map.snowGrid=new SnowGrid(map);
        var installed=typeof(ModLister).GetField("odysseyInstalled",Flags)!;var old=installed.GetValue(null);
        try {installed.SetValue(null,true);map.sandGrid=new SandGrid(map);}finally{installed.SetValue(null,old);}
        map.regionGrid=new RegionGrid(map);Set(map.pathing,"map",map);
        map.lordManager=Blank<Verse.AI.Group.LordManager>();map.lordManager.lords=[];
        var space=Blank<TerrainDef>();space.defName="Space";space.exposesToVacuum=true;space.passability=Traversability.Impassable;Array.Fill(map.terrainGrid.topGrid,space);
        var component=new MapComponent_EtherMovement(map);Set(map,"components",new List<MapComponent>{component});
        var pawn=new Pawn_EtherServant {def=w.Caster.def,kindDef=w.Caster.kindDef,health=w.Caster.health,mindState=w.Caster.mindState,guest=w.Caster.guest,thingIDNumber=998};
        Set(pawn,"positionInt",new IntVec3(3,0,10));Set(pawn,"mapIndexOrState",(sbyte)0);Set(pawn,"factionInt",w.Player);
        pawn.drafter=new Pawn_DraftController(pawn);
        using var patches=new Patches();using var hooks=new EtherMovementTests.Hooks();patches.Prefix(typeof(RegionGrid).GetMethod("GetValidRegionAt")!,nameof(EtherEmptyRegion));
        try
        {
            for(int z=0;z<20;z++) if(z!=10)
            {
                var def=Blank<ThingDef>();def.defName="DoorBoundaryWall";def.size=IntVec2.One;def.passability=Traversability.Impassable;
                var wall=new Thing {def=def};Set(wall,"positionInt",new IntVec3(10,0,z));Set(wall,"mapIndexOrState",(sbyte)0);map.thingGrid.Register(wall);
            }
            var d=Blank<ThingDef>();d.defName="EtherNativeDoor";d.size=IntVec2.One;d.category=ThingCategory.Building;d.thingClass=typeof(Building_Door);d.building=new BuildingProperties();d.passability=Traversability.PassThroughOnly;d.comps=[];
            var door=new Building_Door {def=d};Set(door,"positionInt",new IntVec3(10,0,10));Set(door,"mapIndexOrState",(sbyte)0);Set(door,"factionInt",w.Player);
            var forbid=new CompForbiddable {parent=door,props=new CompProperties(typeof(CompForbiddable))};Set(door,"comps",new List<ThingComp>{forbid});Set(door,"compForbiddable",forbid);map.thingGrid.Register(door);map.edificeGrid.Register(door);
            bool Reach(TraverseMode mode=TraverseMode.ByPawn)=>EtherMovement.CanReach(pawn,pawn.Position,new IntVec3(16,0,10),PathEndMode.OnCell,TraverseParms.For(pawn,Danger.Deadly,mode));
            Check.Equal(false,door.Open,"fixture native door starts closed");Check.Equal(true,door.PawnCanOpen(pawn),"native friendly pawn can open door");Check.Equal(true,Reach(),"closed openable friendly door can be routed through");
            Check.Equal(false,Reach(TraverseMode.NoPassClosedDoors),"strict no-closed-door mode rejects same closed door");
            Set(forbid,"forbiddenInt",true);Check.Equal(true,door.IsForbiddenToPass(pawn),"native forbidden contract active");Check.Equal(false,Reach(),"servant cannot bypass forbidden closed door");
            Set(forbid,"forbiddenInt",false);Set(door,"factionInt",null!);pawn.def.race.canOpenFactionlessDoors=false;
            Check.Equal(false,door.PawnCanOpen(pawn),"native unopened factionless door denies worker");Check.Equal(false,Reach(),"non-openable door blocks void route");
        }
        finally {component.Dispose();map.snowGrid.Dispose();map.sandGrid.Dispose();}
    });
}


