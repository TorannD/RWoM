using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    internal static void StarterShipNetwork(RuntimeFixture f)
    {
        var odyssey=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.NonPublic|BindingFlags.Static)!;
        var old=odyssey.GetValue(null);
        try {
        odyssey.SetValue(null,true);
        foreach(bool tank in new[]{false,true}) foreach(var rotation in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West})
            Run(f,()=>ThrusterEnvironment(()=>StarterShipNetwork(tank,rotation)));
        } finally {odyssey.SetValue(null,old);}
    }
    static void StarterShipNetwork(bool tank, Rot4 rotation)
    {
        var sketch=SketchResolver_MagicalStarterShip.Build(tank);sketch.Rotate(rotation);
        var offset=new IntVec3(30,0,30);
        var floors=sketch.Terrain.Select(t=>t.pos+offset).ToHashSet();
        var coreSketch=sketch.Things.Single(t=>t.def.defName=="TM_MagicalGravEngine");
        var corePos=coreSketch.pos+offset;
        var engine=MagicalEngineFrom(NativeEngine(floors));
        Set(engine,"positionInt",corePos);Set(engine,"rotationInt",coreSketch.rot);
        var source=Tank(corePos.x,corePos.z);var fuel=AddFuel(source,100);
        foreach(var comp in source.parent.AllComps.ToArray()){comp.parent=engine;engine.AllComps.Add(comp);}
        Set(engine.GetComp<CompMagofuelReservoir>(),"storedMana",0f);
        engine.GetComp<CompMagofuelReservoir>().Props.manaCapacity=500f;
        foreach(var beacon in sketch.Things.Where(t=>t.def.defName=="TM_ManaBeacon"))Make("ManaBeacon",beacon.pos.x+offset.x,beacon.pos.z+offset.z);
        var objects=new List<Thing>{engine};
        if(tank) {
            var st=sketch.Things.Single(t=>t.def.defName=="TM_MagofuelReservoir");var p=st.pos+offset;
            var t=Tank(p.x,p.z);t.parent.def=st.def;Set(t.parent,"rotationInt",st.rot);
            var tankFuel=AddFuel(t,100);tankFuel.Props.fuelCapacity=500f;
            Set(t.parent.GetComp<CompMagofuelReservoir>(),"storedMana",0f);objects.Add(t.parent);
        }
        foreach(var st in sketch.Things.Where(t=>t.def.defName=="TM_SmallMagicalThruster")) {
            var p=st.pos+offset;var thruster=NativeThruster(engine,p.x,p.z,10);
            thruster.parent.def=st.def;Set(thruster.parent,"rotationInt",st.rot);objects.Add(thruster.parent);
        }
        var groups=MagicalThrusterFuel.NativeGroups(engine);
        Check.Equal(1,groups.Count,"real network makes one ship fuel group in every rotation");
        var group=groups.Single();
        Check.Equal(tank?200f:100f,group.Fuel,"core and optional tank each counted once");
        Check.Equal(tank?600f:100f,group.Capacity,"scenario does not reduce physical storage capacities");
        Check.Equal(0f,engine.GetComp<CompMagofuelReservoir>().StoredMana,"first flight works without free mana");
        Check.Equal(20f,group.Range,"two starter thrusters have funded twenty-tile bonus");
        Check.Equal(50f,group.ConsumeRatio(.5f,false),"first-flight dry-run calculates finite payment");
        Check.Equal(tank?200f:100f,group.Fuel,"dry-run leaves startup fuel unchanged");
        Check.Equal(50f,group.ConsumeRatio(.5f,true),"actual first-flight debit spends once");
        Check.Equal(tank?150f:50f,group.Fuel,"remaining starter fuel is finite");
        var ship=(Gravship)Activator.CreateInstance(typeof(Gravship),true)!;Set(ship,"engine",engine);
        var add=typeof(Gravship).GetMethod("AddThing",BindingFlags.NonPublic|BindingFlags.Instance)!;
        foreach(var thing in objects) {thing.def.bringAlongOnGravship=true;add.Invoke(ship,[thing,IntVec3.Zero]);add.Invoke(ship,[thing,IntVec3.Zero]);}
        Check.Equal(objects.Count,ship.Things.Count(),"native capture carries every fuel and propulsion component exactly once");
        Check.Equal(true,ship.Things.Contains(engine),"native capture retains original magic core");
        Check.Equal(tank?150f:50f,group.Fuel,"capture does not grant new fuel");
    }
}

