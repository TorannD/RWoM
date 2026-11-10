using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class ApothecaryClinicalTests
{
    private const BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    private static ThingDef Def()=> (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
    private static void MapFor(Pawn p)
    {
        var m=p.Map;
        m.info=new MapInfo{Size=new IntVec3(40,1,40)};m.cellIndices=new CellIndices(m);m.thingGrid=new ThingGrid(m);
        m.edificeGrid=new EdificeGrid(m);m.fogGrid=new FogGrid(m);
        var pc=new PathingContext(m,new PathGrid(m,new PathGridDef {defName="ApothecaryPath"}));
        m.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));Set(m.pathing,"normal",pc);Set(m.pathing,"fenceBlocked",pc);
        Set(p,"positionInt",new IntVec3(10,0,10));p.def.category=ThingCategory.Pawn;p.def.size=new IntVec2(1,1);
    }
    internal static void Harvest(Pawn p,Hediff_ApothecaryHerbs stock,Pawn? owner=null,bool accepted=true,bool legacy=false)
    {
        MapFor(p);
        var product=new Thing{def=Def(),stackCount=3};
        var pd=Def();pd.category=ThingCategory.Plant;pd.plant=new PlantProperties{harvestedThingDef=product.def,harvestMinGrowth=.65f,harvestYield=4};
        var plant=new Plant{def=pd,thingIDNumber=23001};plant.Growth=1f;Set(plant,"mapIndexOrState",(sbyte)0);Set(plant,"positionInt",p.Position);
        var old=p.jobs;
        try
        {
            var job=new Job{def=new JobDef{defName="Harvest",driverClass=typeof(JobDriver_PlantHarvest)},targetA=plant};
            p.jobs=new Pawn_JobTracker(p);Set(p.jobs,"curJob",job);Set(p.jobs,"curDriver",new JobDriver_PlantHarvest{pawn=p,job=job});
            var api=typeof(ApothecaryKitAbility).Assembly.GetType("TorannMagic.ClassProgression.ApothecaryProductHarvest")!;
            var callback=api.GetMethod("Postfix",Hidden)!;
            callback.Invoke(null,[p,new Thing{def=Def(),stackCount=1}]);Check.Equal(0f,stock.Severity,"unrelated crop cannot pay herbs");
            var oldHook=typeof(TorannMagicMod).GetNestedType("ApothecaryHarvest_Patch")!.GetMethod("Postfix",Hidden)!;
            oldHook.Invoke(null,[plant,p]);
            if(!legacy)Check.Equal(0f,stock.Severity,"legacy hook cannot duplicate new progression harvest");
            callback.Invoke(null,[p,product]);Check.Equal(accepted,stock.Severity>0,"native harvest receipt only adds herbs for an available owner");
            if(!accepted || legacy) return;
            var state=ClassIdentity.Component(owner??p).State.Find("rwom.apothecary")!;
            var data=state.GetType().GetField("MartialData",Hidden)!.GetValue(state)!;
            var plants=(List<int>)data.GetType().GetField("Plants",Hidden)!.GetValue(data)!;
            Check.Equal(1,plants.Count,"harvest trial credited to the selected owner once");
            plants.Clear();
        }
        finally {p.jobs=old;}
    }
    internal static void Detox(Pawn p,Hediff_ApothecaryHerbs stock,Pawn? owner=null,MightAbility? ability=null)
    {
        var old=HediffDefOf.FoodPoisoning;var reserve=stock.Severity;
        try
        {
            HediffDefOf.FoodPoisoning=new HediffDef{defName="ApothecaryPoison",maxSeverity=1f};
            var illness=new Hediff{def=HediffDefOf.FoodPoisoning,pawn=p,loadID=999};
            Set(illness,"severityInt",.8f);p.health.hediffSet.hediffs.Add(illness);
            var verb=new Verb_ApothecaryAntitoxin{caster=p,Ability=ability,verbProps=new VerbProperties_Ability()};Set(verb,"currentTarget",new LocalTargetInfo(p));
            bool Fire()=>(bool)typeof(Verb_ApothecaryAntitoxin).GetMethod("TryCastShot",Hidden)!.Invoke(verb,null)!;
            stock.Severity=19;Check.Equal(false,Fire(),"insufficient herbs");
            stock.Severity=39;Check.Equal(true,Fire(),"first antidote on real illness");Check.Equal(19f,stock.Severity,"one herb payment");
            if(ability!=null)Check.Equal(TorannMagic.Compatibility.AbilityTiming.Cooldown(2400),ability.CooldownTicksLeft,"cooldown belongs to actual source ability");
            Check.Equal(true,Math.Abs(illness.Severity-.64f)<.001f,"bounded severity reduction");
            stock.Severity=80;Check.Equal(false,Fire(),"same illness cannot be dosed twice");Check.Equal(80f,stock.Severity,"repeat cannot charge");
            var path=ClassIdentity.Component(owner??p).State.Find("rwom.apothecary")!;
            var data=path.GetType().GetField("MartialData",Hidden)!.GetValue(path)!;
            ((List<int>)data.GetType().GetField("Poisonings",Hidden)!.GetValue(data)!).Clear();
            p.health.hediffSet.hediffs.Remove(illness);
        }
        finally {HediffDefOf.FoodPoisoning=old;stock.Severity=reserve;}
    }
    internal static void Kit(RuntimeFixture f,Pawn p,CompAbilityUserMight might,Hediff_ApothecaryHerbs stock,Pawn? owner=null,Action? afterReload=null)
    {
        using var jobDefs=new SavedDefinitions<JobDef>();
        var medDef=ThingDefOf.MedicineHerbal;
        var defs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        var original=p.jobs;float reserve=stock.Severity;
        var wound=new Hediff_Injury{def=new HediffDef{defName="RecentKitInjury",initialSeverity=30,maxSeverity=100,injuryProps=new InjuryProps()},pawn=p,Part=p.RaceProps.body.corePart,loadID=7701};
        Set(wound,"severityInt",30f);
        p.health.hediffSet.hediffs.Add(wound);
        try
        {
            var item=ThingDefOf.MedicineHerbal=Def();item.defName="RegressionHerbalMedicine";item.category=ThingCategory.Item;
            item.thingClass=typeof(Medicine);item.stackLimit=75;item.comps=[];item.destroyable=true;
            var dose=new Medicine{def=item,stackCount=3,thingIDNumber=44322};
            Check.Equal(true,p.inventory.innerContainer.TryAdd(dose),"real herbal stack carried");
            var dressing=new HediffDef{defName="TM_ApothecaryDressing",hediffClass=typeof(Hediff_ApothecaryDressing),initialSeverity=.1f,
                stages=[new HediffStage{totalBleedFactor=.5f}]};
            if(DefDatabase<HediffDef>.GetNamedSilentFail(dressing.defName)==null)DefDatabase<HediffDef>.Add(dressing);
            p.jobs=new Pawn_JobTracker(p);
            var jobDef=DefDatabase<JobDef>.GetNamedSilentFail("TM_ApothecaryGoldenHourCast");
            if(jobDef==null){jobDef=new JobDef{defName="TM_ApothecaryGoldenHourCast",driverClass=typeof(JobDriver_ApothecaryKit)};DefDatabase<JobDef>.Add(jobDef);}
            var job=new Job{def=jobDef,targetA=p,targetB=p.Position,targetC=owner??p};
            var driver=new JobDriver_ApothecaryKit{pawn=p,job=job};Set(p.jobs,"curJob",job);Set(p.jobs,"curDriver",driver);
            object? Call(string name,params object[] args)=>typeof(JobDriver_ApothecaryKit).GetMethod(name,Hidden)!.Invoke(driver,args);
            stock.Severity=90;Set(driver,"begun",true);Set(driver,"mapId",p.Map.uniqueID);
            Check.Equal(false,(bool)Call("Commit")!,"short medicine fails atomically");
            Check.Equal(90f,stock.Severity,"no spend on failed commitment");
            var path=ClassIdentity.Component(owner??p).State.Find("rwom.apothecary")!;
            var data=path.GetType().GetField("MartialData",Hidden)!.GetValue(path)!;
            Call("Finish");Call("Finish");Check.Equal(84f,stock.Severity,"preparation interrupt paid once");
            var ability=might.AbilityData.Powers.OfType<ApothecaryKitAbility>().FirstOrDefault();
            if(ability!=null)Check.Equal(TorannMagic.Compatibility.AbilityTiming.Cooldown(6000),ability.CooldownTicksLeft,"interruption cooldown remains on source");
            Check.Equal(3,dose.stackCount,"interrupt preserved medicines");
            Set(data,"KitReadyAt",0);Set(driver,"finished",false);Set(driver,"begun",true);dose.stackCount=4;
            if(ability!=null)ability.CooldownTicksLeft=0;
            if(owner!=null)Check.Equal(false,p.health.ShouldBeDowned(),$"carrier fixture remains awake: pain={p.health.InPainShock}, awake={p.health.capacities.CanBeAwake}, moving={p.health.capacities.CapableOf(PawnCapacityDefOf.Moving)}");
            Check.Equal(true,(bool)Call("Commit")!,"atomic herbs + four medicines");
            Check.Equal(true,Math.Abs(24f-stock.Severity)<.001f,"60 herb cost");
            Check.Equal(true,dose.Destroyed,"four doses consumed");
            if(ability!=null)Check.Equal(TorannMagic.Compatibility.AbilityTiming.Cooldown(120000),ability.CooldownTicksLeft,"successful cooldown remains on source");
            Check.Equal(false,(bool)Call("Commit")!,"no second commit");
            Call("Pulse",0);Call("Pulse",0);Check.Equal(25f,wound.Severity,"one pulse heals five, no replay");
            string file=Path.Combine(f.WorkingDirectory,"golden-hour.xml");
            try { Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref job,"job"); Scribe_Deep.Look(ref driver,"driver"); Scribe.saver.FinalizeSaving(); }
            finally {Scribe.ForceStop();}
            driver=null!;job=null!;
            try {Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref job,"job");Scribe_Deep.Look(ref driver,"driver");driver.pawn=p;driver.job=job;var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",Hidden)!.GetValue(Scribe.loader.crossRefs)!;directory.RegisterLoaded(p);if(owner!=null && owner!=p)directory.RegisterLoaded(owner);Scribe.loader.FinalizeLoading();}
            finally {Scribe.ForceStop();}
            // Resume the saved job with the patient resolved through the normal Scribe directory.
            Set(p.jobs,"curJob",job);Set(p.jobs,"curDriver",driver);
            Check.Same(owner??p,job.targetC.Pawn,"native Job Scribe keeps exact paying pawn");
            Check.Same(p,job.targetA.Pawn,"native Job Scribe keeps physical patient");
            afterReload?.Invoke();
            var activeDressing=p.health.hediffSet.hediffs.OfType<Hediff_ApothecaryDressing>().Single();
            activeDressing.ageTicks=1205; // Native toil transitions add ticks beyond four 300-tick waits.
            if(afterReload==null)Check.Equal(false,activeDressing.ShouldRemove,"active paid treatment survives through its final pulse");
            if(afterReload!=null)Check.Equal(true,p.health.hediffSet.hediffs.OfType<Hediff_ApothecaryDressing>().Single().ShouldRemove,"depletion ends dressing protection immediately");
            Call("Pulse",0);Call("Pulse",1);Call("Pulse",2);Call("Pulse",3);Call("Finish");
            Check.Equal(true,activeDressing.ShouldRemove,"completed treatment cannot retain protection");
            Check.Equal(afterReload==null?10f:25f,wound.Severity,"saved pulses resume only while original source remains valid");Check.Equal(true,Math.Abs(24f-stock.Severity)<.001f,"no repeat charge");
        }
        finally
        {
            p.jobs=original;stock.Severity=reserve;ThingDefOf.MedicineHerbal=medDef;p.health.hediffSet.hediffs.Remove(wound);
            DefDatabase<HediffDef>.Clear();foreach(var def in defs)DefDatabase<HediffDef>.Add(def);
        }
    }
}
