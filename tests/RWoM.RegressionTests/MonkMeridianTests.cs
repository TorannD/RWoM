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

internal static class MonkMeridianTests
{
    private sealed class SavedJob : IExposable
    {
        public Pawn Body=null!;
        public Job Order=null!;
        public JobDriver_MonkMeridian Driver=null!;
        public void ExposeData()
        {
            Scribe_References.Look(ref Body,"body");
            Scribe_Deep.Look(ref Order,"order");
            if(Driver!=null){Driver.pawn=Body;Driver.job=Order;}
            Scribe_Deep.Look(ref Driver,"driver");
            if(Driver!=null){Driver.pawn=Body;Driver.job=Order;}
        }
    }
    private static readonly Dictionary<Thing,int> hits=[];
    public static bool Damage(Thing __instance,DamageInfo dinfo,ref DamageWorker.DamageResult __result)
    {
        Check.Equal(12f,dinfo.Amount,"meridian pulse damage");
        hits.TryGetValue(__instance,out var previous); hits[__instance]=previous+1;
        __result=new DamageWorker.DamageResult{totalDamageDealt=dinfo.Amount};return false;
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,Pawn? trainingOwner=null)
    {
        var map=pawn.Map;
        map.info=new MapInfo{Size=new IntVec3(48,1,48)};
        map.cellIndices=new CellIndices(map);map.edificeGrid=new EdificeGrid(map);map.thingGrid=new ThingGrid(map);
        var pos=pawn.Position;
        var field=typeof(SniperSurvey).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf",true)!.GetField("TM_ChiBurn")!;var oldChiDamage=field.GetValue(null);
        var source=trainingOwner??pawn;
        var chi=source.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);var oldChi=chi.Severity;
        var owner=might.AbilityData.Powers.OfType<MonkMeridianAbility>().Single();
        var oldRelations=typeof(Faction).GetField("relations",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(pawn.Faction);
        var jobDefs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        var fo=new Faction{def=new FactionDef()};
        Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=fo,kind=FactionRelationKind.Hostile}});
        Set(fo,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
        const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        var data=ClassIdentity.Component(source).State.Find("rwom.monk")!.GetType().GetField("MartialData",hidden)!.GetValue(ClassIdentity.Component(source).State.Find("rwom.monk"))!;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.test.monkmeridian."+Guid.NewGuid();var patch=Activator.CreateInstance(ht,id)!;
        var targets=new List<Pawn>();
        try
        {
            var prefix=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(MonkMeridianTests).GetMethod(nameof(Damage))!)!;
            ht.GetMethod("Patch")!.Invoke(patch,[typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,prefix,null,null,null]);
            Set(pawn,"positionInt",new IntVec3(16,0,16));Set(Current.Game,"questManager",new QuestManager());
            if(DefDatabase<JobDef>.GetNamedSilentFail("TM_MonkMeridianCast")==null)
                DefDatabase<JobDef>.Add(new JobDef{defName="TM_MonkMeridianCast",driverClass=typeof(JobDriver_MonkMeridian)});
            field.SetValue(null,new DamageDef{defName="MonkTestBurn"});
            for(int i=0;i<5;i++)
            {
                var enemy=PawnWithComps(false,false).pawn;
                enemy.kindDef=new PawnKindDef{race=enemy.def}; enemy.guest=new Pawn_GuestTracker(enemy);enemy.mindState=new Pawn_MindState(enemy);
                enemy.thingIDNumber=99410+i;enemy.def.category=ThingCategory.Pawn;enemy.def.size=new IntVec2(1,1);
                Set(enemy,"mapIndexOrState",(sbyte)0);Set(enemy,"positionInt",pawn.Position+GenAdj.AdjacentCells[i]);Set(enemy,"factionInt",fo);
                if(i==0)
                {
                    enemy.ageTracker=new Pawn_AgeTracker(enemy);
                    enemy.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{developmentalStage=DevelopmentalStage.Adult}}];
                    Set(enemy.ageTracker,"cachedLifeStageIndex",0);
                    enemy.needs=new Pawn_NeedsTracker(enemy);
                    Set(enemy.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
                    enemy.needs.AllNeeds.Add(new Need_Mana(enemy){def=TorannMagicDefOf.TM_Mana,CurLevel=1f});
                    enemy.needs.AllNeeds.Add(new Need_Stamina(enemy){def=TorannMagicDefOf.TM_Stamina,CurLevel=1f});
                    // This target is nearer than the other four and is therefore selected.
                    Set(enemy,"positionInt",pawn.Position);
                }
                map.thingGrid.Register(enemy);targets.Add(enemy);
            }
            chi.Severity=100;var job=owner.GetJob(AbilityContext.Player,pawn);
            if(trainingOwner!=null)
            {
                job=JobMaker.MakeJob(new JobDef{defName="TM_SpiritMonkMeridian",driverClass=typeof(JobDriver_MonkMeridian)},pawn.Position,source);
                DefDatabase<JobDef>.Add(job.def);
            }
            var driver=new JobDriver_MonkMeridian{pawn=pawn,job=job};
            object? Call(string name,params object[] args)=>typeof(JobDriver_MonkMeridian).GetMethod(name,hidden)!.Invoke(driver,args);
            Check.Equal(true,driver.TryMakePreToilReservations(false),"native self-cast admission");
            Set(driver,"begun",true);Set(driver,"mapId",map.uniqueID);
            if(trainingOwner!=null)
            {
                Set(ClassIdentity.Component(source).SpiritRebirth,"depletionPending",true);
                Check.Equal(false,(bool)Call("Valid")!,"source loss interrupts warmup");
            }
            Call("Finish");Call("Finish");
            Check.Equal(94f,chi.Severity,"interrupted warmup only costs six chi once");
            if(trainingOwner!=null) Set(ClassIdentity.Component(source).SpiritRebirth,"depletionPending",false);
            Check.Equal(Find.TickManager.TicksGame+6000,(int)data.GetType().GetField("MeridianReadyAt",hidden)!.GetValue(data)!,"interrupted cooldown");
            Set(data,"MeridianReadyAt",0);chi.Severity=100;owner.CooldownTicksLeft=0;
            driver=new JobDriver_MonkMeridian{pawn=pawn,job=job};Set(driver,"begun",true);Set(driver,"mapId",map.uniqueID);
            Call("Pulse",0);Call("Pulse",0);
            Check.Equal(true,Math.Abs(targets[0].needs.TryGetNeed<Need_Mana>().CurLevel-.85f)<.001f,"hybrid loses fifteen percent of one pool");
            Check.Equal(1f,targets[0].needs.TryGetNeed<Need_Stamina>().CurLevel,"second resource untouched");
            Check.Equal(4,hits.Count,"four victims maximum");
            Check.Equal(true,Math.Abs(chi.Severity-40)<.001f,"first pulse spends sixty chi once");
            Check.Equal(Find.TickManager.TicksGame+120000,(int)data.GetType().GetField("MeridianReadyAt",hidden)!.GetValue(data)!,"paid cooldown");
            var file=Path.Combine(f.WorkingDirectory,"monk-meridian.xml");
            var saved=new SavedJob{Body=pawn,Order=job,Driver=driver};
            try {Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref saved,"state");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            driver=null!;
            saved=null!;
            try
            {
                Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref saved,"state");
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",hidden)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(pawn);
                if(source!=pawn)directory.RegisterLoaded(source);
                foreach(var enemy in targets)directory.RegisterLoaded(enemy);
                Scribe.loader.FinalizeLoading();
            }finally{Scribe.ForceStop();}
            driver=saved.Driver;job=saved.Order;
            Check.Same(pawn,driver.pawn,"loaded meridian retains physical caster");
            if(trainingOwner!=null) Check.Same(source,job.targetB.Pawn,"loaded meridian retains bound source");
            Set(pawn,"positionInt",pawn.Position+IntVec3.East);
            Check.Equal(false,(bool)Call("Valid")!,"displacement interrupts the paid channel");
            Set(pawn,"positionInt",job.targetA.Cell);
            Call("Pulse",0);Call("Pulse",1);Call("Pulse",2);Call("Finish");
            Check.Equal(12,hits.Values.Sum(),"three pulses in all despite load");
            Check.Equal(true,Math.Abs(chi.Severity-40)<.001f,"saved job does not charge again");
            if(trainingOwner!=null)
            {
                Set(ClassIdentity.Component(source).SpiritRebirth,"depletionPending",true);
                Check.Equal(false,(bool)Call("Valid")!,"depletion interrupts hosted meridian");
                Set(ClassIdentity.Component(source).SpiritRebirth,"depletionPending",false);
                job.targetB=LocalTargetInfo.Invalid;
                Check.Equal(false,(bool)Call("Valid")!,"missing saved source cannot become a native meridian");
            }
        }
        finally
        {
            hits.Clear();ht.GetMethod("UnpatchAll")!.Invoke(patch,[id]);Set(pawn.Faction,"relations",oldRelations);
            foreach(var enemy in targets)map.thingGrid.Deregister(enemy);
            Set(pawn,"positionInt",pos);chi.Severity=oldChi;field.SetValue(null,oldChiDamage);
            DefDatabase<JobDef>.Clear();foreach(var def in jobDefs)DefDatabase<JobDef>.Add(def);
        }
    }
}
