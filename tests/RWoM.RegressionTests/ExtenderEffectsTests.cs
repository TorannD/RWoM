using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> ExtenderEffectCases()
    {
        yield return new("Odyssey.ExtenderEffects.NativeRegistrationLimitsAndLifecycle", f => Run(f, () => ThrusterEnvironment(ExtenderEffectsNative)));
        yield return new("Odyssey.ExtenderEffects.DefinitionAnchorAndBoundedResources", f => {
            var def = XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Gravcore.xml")).Root!.Elements("ThingDef").Single(e => e.Element("defName")?.Value == "TM_MagicalGravExtender");
            Check.Equal("MapMeshAndRealTime",def.Element("drawerType")!.Value,"dynamic overlay retains native map mesh");
            Check.Equal("Normal",def.Element("tickerType")!.Value,"tick-only native activity refresh");
            Check.Equal(1,def.Element("comps")!.Elements("li").Count(e => (string?)e.Attribute("Class") == "TorannMagic.Odyssey.CompProperties_ExtenderOrbit"),"one orbit comp");
            var props = new CompProperties_ExtenderOrbit(); var graphic = new Graphic_Single();
            foreach(var rot in new[]{Rot4.North,Rot4.East,Rot4.South,Rot4.West}) {
                var expected = graphic.QuatFromRot(rot) * new Vector3((134.5f/256f-.5f)*1.8f,.04f,(.5f-97.5f/256f)*1.8f);
                Check.Equal(true,(props.WorldOffset(rot,new Vector2(1.8f,1.8f),graphic.QuatFromRot(rot),Vector3.zero)-expected).sqrMagnitude<.00001f,"native single rotation crystal center "+rot);
            }
            Check.Equal(1.4f,ExtenderOrbitRenderer.Brightness,"approved preview brightness");
            Check.Equal(32,ExtenderOrbitRenderer.Levels,"bounded alpha materials");
            Check.Equal(true,typeof(ExtenderOrbitRenderer).IsDefined(typeof(StaticConstructorOnStartup),false),"main-thread Unity resources");
        });
    }
    static void ExtenderEffectsNative()
    {
        var odyssey = typeof(ModsConfig).GetField("odysseyActive",BindingFlags.Static|BindingFlags.NonPublic)!;
        var old=odyssey.GetValue(null); odyssey.SetValue(null,true);
        try {
            foreach(int limit in new[]{1,6,10}) {
                float support=limit==10 ? 100f : 250f;
                var floors=new HashSet<IntVec3>(); for(int x=8;x<25;x++) for(int z=8;z<14;z++) floors.Add(new(x,0,z));
                var engine=MagicalEngineFrom(NativeEngine(floors));
                var affected=new CompAffectedByFacilities {parent=engine,props=new CompProperties_AffectedByFacilities()};
                engine.AllComps.Add(affected); engine.def.comps=[affected.props];
                map.listerThings=new ListerThings(ListerThingsUse.Global);
                Set(map.listerThings,"listsByDef",new Dictionary<ThingDef,List<Thing>>{{engine.def,[engine]}});
                var facilityProps=new CompProperties_GravshipFacility { maxDistance=50,maxSimultaneous=limit,requiresLOS=false,
                    linkableBuildings=[engine.def],statOffsets=[new StatModifier {stat=StatDefOf.SubstructureSupport,value=support}] };
                ThingDef? sharedDef=null; var effects=new List<CompExtenderOrbit>(); var facilities=new List<CompGravshipFacility>();
                for(int i=0;i<=limit;i++) {
                    var building=Make("ManaNetworkEndpoint",12+i,10).parent;
                    if(sharedDef==null){sharedDef=building.def; sharedDef.defName="TM_MagicalGravExtender";sharedDef.comps=[facilityProps];}
                    building.def=sharedDef;
                    ((CompProperties_AffectedByFacilities)affected.props).linkableFacilities=[sharedDef];
                    var facility=new CompGravshipFacility {parent=building,props=facilityProps}; building.AllComps.Add(facility);
                    // Actual native spawn registers accepted links and engine association; no manual list/engine assignment.
                    facility.PostSpawnSetup(false);
                    engine.substructureDirty=false; // The fixture supplies the already computed native hull cache.
                    var effect=new CompExtenderOrbit {parent=building,props=new CompProperties_ExtenderOrbit()}; building.AllComps.Add(effect);
                    effect.PostSpawnSetup(false);effect.RefreshActivity(100); effects.Add(effect); facilities.Add(facility);
                }
                Check.Equal(limit,affected.LinkedFacilitiesListForReading.Count,"native maxSimultaneous accepted links");
                Check.Equal(limit*support,affected.GetStatOffset(StatDefOf.SubstructureSupport),"native actual capacity contribution");
                for(int i=0;i<limit;i++) Check.Equal(true,effects[i].Working(100),"accepted capacity extender active");
                Check.Equal(false,effects[limit].Working(100),"native over-limit rejected extender dark");
                Check.Equal<Building_GravEngine?>(null,facilities[limit].engine,"native rejected facility has no engine");
                var first=effects[0];first.Sample(100);first.RefreshActivity(178);Check.Equal(1f,first.Sample(178),"fade reaches full");
                var flick=new CompFlickable {parent=first.parent,props=new CompProperties_Flickable()};first.parent.AllComps.Add(flick);Set(flick,"switchOnInt",false);
                Check.Equal(false,first.Working(179),"flick immediately revokes activity");Check.Equal(true,first.Sample(217)<.51f,"disabled fade");
                Set(flick,"switchOnInt",true);
                facilityProps.statOffsets[0].value=0;first.RefreshActivity(220);Check.Equal(false,first.Working(220),"zero capacity no orbit");
                facilityProps.statOffsets[0].value=250;first.RefreshActivity(230);Check.Equal(true,first.Working(230),"capacity restored");
                floors.Clear();first.RefreshActivity(240);Check.Equal(false,first.Working(240),"native invalid hull dark");
                floors.Add(first.parent.Position);first.RefreshActivity(250);Check.Equal(true,first.Working(250),"native hull restored");
                Check.Equal(false,first.Working(311),"expired activity lease never rechecks native queries during draw");
                Set(engine,"factionInt",new Faction{def=new FactionDef()});Check.Equal(false,first.Working(250),"foreign core dark");Set(engine,"factionInt",faction);
                Set(first.parent,"factionInt",new Faction{def=new FactionDef()});Check.Equal(false,first.Working(250),"foreign extender dark");Check.Equal(0f,first.Level,"capture clears inherited fade");Set(first.parent,"factionInt",faction);
                affected.Notify_LinkRemoved(first.parent);Check.Equal(false,first.Working(250),"removed native accepted link immediately dark");
                first.PostDeSpawn(map);Check.Equal(0f,first.Level,"despawn clears fade");first.PostSpawnSetup(true);Check.Equal(false,first.Working(250),"load has no saved visual lease");
            }
        } finally { odyssey.SetValue(null,old); }
    }
}
