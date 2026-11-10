using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class RangerFireTrialTests
{
    private sealed class TestFire : Fire
    {
        public override void Destroy(DestroyMode mode=DestroyMode.Vanish) => Set(this,"mapIndexOrState",(sbyte)-2);
    }
    private static Thing? goods;
    public static bool Flammable(Thing thing,ref float __result)
    {
        if (thing != goods) return true;
        __result=1;return false;
    }
    public static bool NoAudio()=>false;
    internal static void Verify(RuntimeFixture f, Pawn ranger,Pawn? owner=null)
    {
        var map=ranger.Map;
        var state=ClassIdentity.Component(owner??ranger).State.Find("rwom.ranger")!;
        const BindingFlags field=BindingFlags.NonPublic|BindingFlags.Instance;
        var count=state.GetType().GetField("RangerClearances",field)!;
        var times=state.GetType().GetField("RangerClearanceTimes",field)!;
        var outerCount=count.GetValue(state);var outerTimes=times.GetValue(state);
        var oldDest=map.haulDestinationManager;var oldJobs=ranger.jobs;
        var oldGrid=map.thingGrid;var oldWorld=Current.Game.World;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.regression.ranger.fire."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        void Prefix(MethodBase method,string fn)
        {
            var hook=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(RangerFireTrialTests).GetMethod(fn)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,hook,null,null,null]);
        }
        ThingDef Def()=>(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        try
        {
            Set(state,"RangerClearanceTimes",new Dictionary<string,int>());Set(state,"RangerClearances",0);
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager=new FactionManager();world.factionManager.AllFactionsListForReading.Add(ranger.Faction);
            Set(world.factionManager,"ofPlayer",ranger.Faction);Set(Current.Game,"worldInt",world);
            var patch=typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.RangerFireTrial",true)!;
            var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[patch])!;proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
            Prefix(typeof(ImpactSoundUtility).GetMethod("PlayImpactSound")!,nameof(NoAudio));
            Prefix(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(Flammable));
            map.thingGrid=new ThingGrid(map);map.haulDestinationManager=new HaulDestinationManager(map);
            ranger.jobs=new Pawn_JobTracker(ranger);
            var origin=ranger.Position;
            var shelf=new Building_Storage{def=Def(),settings=new StorageSettings()};Set(shelf,"factionInt",ranger.Faction);
            var storeCell=origin+IntVec3.East;var group=new SlotGroup(shelf);var groupGrid=(SlotGroup[,,])typeof(HaulDestinationManager).GetField("groupGrid",field)!.GetValue(map.haulDestinationManager)!;
            var box=new Thing{def=Def(),stackCount=12};box.def.category=ThingCategory.Item;box.def.tradeability=Tradeability.All;
            box.def.virtualDefs=[];box.def.defName="DryRangerSupplies";box.def.thingClass=typeof(Thing);box.def.size=new IntVec2(1,1);
            Set(box,"positionInt",storeCell);Set(box,"mapIndexOrState",(sbyte)0);goods=box;map.thingGrid.Register(box);
            shelf.settings.filter.SetAllow(box.def,true);
            var worker=new DamageWorker_Extinguish();var damage=new DamageInfo(new DamageDef{harmsHealth=false},32f,instigator:ranger);
            TestFire Fire(float size,Thing? instigator=null)
            {
                var fire=new TestFire{def=Def(),fireSize=size,instigator=instigator};
                Set(fire,"mapIndexOrState",(sbyte)0);Set(fire,"positionInt",origin);
                var job=new Job{def=new JobDef{defName="BeatFire",driverClass=typeof(JobDriver_BeatFire)},targetA=fire};
                Set(ranger.jobs,"curJob",job);Set(ranger.jobs,"curDriver",new JobDriver_BeatFire{pawn=ranger,job=job});
                return fire;
            }
            worker.Apply(damage,Fire(.2f));Check.Equal(0,(int)count.GetValue(state)!,"goods outside storage excluded");
            groupGrid[storeCell.x,storeCell.y,storeCell.z]=group;
            worker.Apply(damage,Fire(.2f,ranger));Check.Equal(0,(int)count.GetValue(state)!,"player-started fire excluded");
            var stray=Fire(.2f);Fire(.2f);worker.Apply(damage,stray);Check.Equal(0,(int)count.GetValue(state)!,"wrong job target excluded");
            var actual=Fire(.65f);worker.Apply(damage,actual);
            Check.Equal(false,actual.Destroyed,"first native extinguish strike leaves fire");
            Check.Equal(0,(int)count.GetValue(state)!,"partial extinction does not count");
            worker.Apply(damage,actual);Check.Equal(true,actual.Destroyed,"second native strike extinguishes fire");
            Check.Equal(1,(int)count.GetValue(state)!,"danger to nearby occupied storage counts");
            worker.Apply(damage,Fire(.2f));Check.Equal(1,(int)count.GetValue(state)!,"second fire in same sector/day excluded");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            count.SetValue(state,outerCount);times.SetValue(state,outerTimes);goods=null;map.haulDestinationManager=oldDest;
            map.thingGrid=oldGrid;ranger.jobs=oldJobs;Set(Current.Game,"worldInt",oldWorld);
        }
    }
}
