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

internal static class CommanderFieldTests
{
    private static bool accept;
    public static bool Decide(ref bool __result) { __result=accept; return false; }
    public static bool Skip() => false;

    internal static void Verify(RuntimeFixture f,Pawn commander, CompAbilityUserMight might,bool operation,Pawn? disciplineOwner=null,Action<Pawn>? afterWork=null)
    {
        var map=commander.Map; var info=map.info; var grid=map.thingGrid; var indices=map.cellIndices;
        var edifices=map.edificeGrid;var fog=map.fogGrid;var pos=commander.Position;
        var oldJobs=commander.jobs;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.commander.field."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        const BindingFlags impl=BindingFlags.NonPublic|BindingFlags.Static;
        var assembly=typeof(CommanderOperationAbility).Assembly;
        var orders=assembly.GetType("TorannMagic.ClassProgression.CommanderOrders")!;
        var mode=assembly.GetType("TorannMagic.ClassProgression.CommanderOrder")!;
        var work=assembly.GetType("TorannMagic.ClassProgression.CommanderWorkReceipts")!;
        var api=assembly.GetType("TorannMagic.ClassProgression.CommanderMartialPath")!;
        var orderType=typeof(CommanderOrderEffect);
        var oldHediffs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        var oldJobDefs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        void Patch(MethodBase method,string name)
        {
            var hm=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(CommanderFieldTests).GetMethod(name)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,hm,null,null,null]);
        }
        try
        {
            map.info=new MapInfo{Size=new IntVec3(60,1,60)};map.cellIndices=new CellIndices(map);map.thingGrid=new ThingGrid(map);
            map.edificeGrid=new EdificeGrid(map);map.fogGrid=new FogGrid(map);
            Set(commander,"positionInt",new IntVec3(10,0,10));
            Patch(typeof(HediffSet).GetMethod("DirtyCache")!,nameof(Skip));
            Patch(orders.GetMethod("Accept",impl)!,nameof(Decide));
            var cls=work.GetNestedType("DeliveredToContainer",BindingFlags.NonPublic)!;
            var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[cls])!;
            proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
            var def=DefDatabase<HediffDef>.GetNamedSilentFail("TM_CommanderAssignment");
            if(def==null)
            {
                def=new HediffDef {defName="TM_CommanderAssignment",hediffClass=orderType,initialSeverity=.5f,stages=Enumerable.Range(0,7).Select(i=>new HediffStage{minSeverity=i/10f}).ToList()};
                DefDatabase<HediffDef>.Add(def);
            }
            var worker=PawnWithComps(false,false).pawn;worker.def.category=ThingCategory.Pawn;worker.def.size=new IntVec2(1,1);
            worker.def.race.body=commander.def.race.body;
            Set(worker.def.race,"fleshType",new FleshTypeDef{defName="CommanderWorkerFlesh",isOrganic=true});
            worker.def.race.lifeStageAges=[new LifeStageAge{def=new LifeStageDef{defName="CommanderWorkerAdult",developmentalStage=DevelopmentalStage.Adult},minAge=0}];
            worker.def.race.baseHealthScale=1f;
            worker.ageTracker=new Pawn_AgeTracker(worker);
            worker.mindState=new Pawn_MindState(worker);worker.kindDef=new PawnKindDef{race=worker.def};worker.guest=new Pawn_GuestTracker(worker);
            Set(worker,"mapIndexOrState",(sbyte)0);Set(worker,"positionInt",commander.Position+IntVec3.East);
            Set(worker,"factionInt",commander.Faction);
            worker.jobs=new Pawn_JobTracker(worker);worker.carryTracker=new Pawn_CarryTracker(worker);
            var box=new Building_Casket {def=BlankDef(ThingCategory.Building,typeof(Building_Casket))};
            Set(box,"comps",new List<ThingComp>());
            var material=new Thing {def=BlankDef(ThingCategory.Item,typeof(Thing)),stackCount=3};
            var job=new Job{def=new JobDef{defName="CommanderTestDelivery"},targetB=box,targetA=material,loadID=3020};
            Set(worker.jobs,"curJob",job);
            var driver=new JobDriver_HaulToContainer {pawn=worker,job=job};Set(worker.jobs,"curDriver",driver);
            map.thingGrid.Register(worker);
            int Issue(string style,int serial) => (int)orders.GetMethod("Issue",impl)!.Invoke(null,[commander,worker.Position,Enum.Parse(mode,style),serial])!;
            // Rally keeps the probabilistic acceptance roll: a refusal must not
            // create an assignment (live report: the Work Crew trial could not
            // complete when every roll refused, so Crew orders are now always
            // taken by an eligible worker — pinned right after the Rally case).
            accept=false;Check.Equal(0,Issue("Rally",203),"refusal creates no assignment");
            Check.Equal(false,worker.health.hediffSet.HasHediff(def),"no stale workforce bonus");
            accept=false;Check.Equal(1,Issue("Crew",201),"crew work order is always accepted");
            Check.Equal(0,Issue("Crew",202),"another commander order cannot stack");
            accept=true;
            var effect=(CommanderOrderEffect)worker.health.hediffSet.GetFirstHediffOfDef(def);
            var path=ClassIdentity.Component(disciplineOwner??commander).State.Find("rwom.commander")!;
            var data=api.GetMethod("Data",impl)!.Invoke(null,[path])!;
            var supplies=(List<int>)data.GetType().GetField("Supplies",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(data)!;
            Check.Equal(true,worker.carryTracker.innerContainer.TryAdd(material),"real item enters worker carry container");
            var deposit=Toils_Haul.DepositHauledThingInContainer(TargetIndex.B,TargetIndex.None);
            deposit.actor=worker;deposit.initAction();
            Check.Equal(3,box.GetDirectlyHeldThings().TotalStackCountOfDef(material.def),"real delivery keeps the load");
            if(path.Stage==InitiationStage.Foundation)
            {
                Check.Equal(true,supplies.Contains(201),"completed native haul credits commander");
                supplies.Remove(201);
            }
            if(disciplineOwner!=null)
            {
                Check.Same(commander,orderType.GetField("Commander",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(effect)!,"receipt retains physical commander");
                Check.Same(disciplineOwner,orderType.GetField("DisciplineOwner",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(effect)!,"receipt pins original discipline owner");
                Check.Equal(false,effect.ShouldRemove,"owned assignment remains live");
                Set(ClassIdentity.Component(disciplineOwner).SpiritRebirth,"depletionPending",true);
                Check.Equal(true,effect.ShouldRemove,"depleted soul stops assignment bonus");
                Set(ClassIdentity.Component(disciplineOwner).SpiritRebirth,"depletionPending",false);
            }
            worker.health.RemoveHediff(effect);
            afterWork?.Invoke(worker);
            if(!operation) return;
            var oldStamina=TorannMagicDefOf.TM_Stamina;
            float oldMaximum=might.maxSP;
            try
            {
                if(might.Stamina==null)
                {
                    TorannMagicDefOf.TM_Stamina=new NeedDef{defName="CommanderStamina",needClass=typeof(Need_Stamina)};
                    var resourceOwner=disciplineOwner??commander;
                    resourceOwner.needs.AllNeeds.Add(new Need_Stamina(resourceOwner){def=TorannMagicDefOf.TM_Stamina});
                }
                Set((disciplineOwner??commander).ageTracker,"ageBiologicalTicksInt",20L*3600000L);
                might.maxSP=.75f; might.Stamina.CurLevelPercentage=1;
                if(DefDatabase<JobDef>.GetNamedSilentFail("TM_CommanderOperationJob")==null)
                    DefDatabase<JobDef>.Add(new JobDef{defName="TM_CommanderOperationJob",driverClass=typeof(JobDriver_CommanderOperation)});
                var owner=might.AbilityData.Powers.OfType<CommanderOperationAbility>().First();
                if(disciplineOwner!=null)
                {
                    var hosted=assembly.GetType("TorannMagic.ClassProgression.SpiritHostedCommander",true)!;
                    var ready=hosted.GetMethod("Ready",impl)!;
                    Check.Equal(true,(bool)ready.Invoke(null,[commander,owner])!,"projected operation enabled");
                    var commands=(IEnumerable<Gizmo>)hosted.GetMethod("Commands",impl)!.Invoke(null,[commander,disciplineOwner])!;
                    Check.Equal(2,commands.Count(),"both operation choices available on body");
                    var target=hosted.GetMethod("Target",impl)!;
                    Check.Equal(true,(bool)target.Invoke(null,[commander,owner,new LocalTargetInfo(worker.Position)])!,"near physical target valid");
                    Check.Equal(false,(bool)target.Invoke(null,[commander,owner,new LocalTargetInfo(new IntVec3(50,0,50))])!,"range measured from carrier");
                    Set(ClassIdentity.Component(disciplineOwner).SpiritRebirth,"depletionPending",true);
                    Check.Equal(false,(bool)ready.Invoke(null,[commander,owner])!,"stale command disabled on depletion");
                    Check.Equal(0,((IEnumerable<Gizmo>)hosted.GetMethod("Commands",impl)!.Invoke(null,[commander,disciplineOwner])!).Count(),"depleted soul lends no operation commands");
                    Set(ClassIdentity.Component(disciplineOwner).SpiritRebirth,"depletionPending",false);
                }
                var action=(TMAbilityDef)owner.Def;
                float oldCost=action.staminaCost;
                try
                {
                    action.staminaCost=.6f;
                    Check.Equal(true,Math.Abs(might.ActualStaminaCost(action)-.45f)<.001f,"UI and native admission use 60% of a nondefault maximum");
                }
                finally {action.staminaCost=oldCost;}
                var make=typeof(CommanderOperationAbility).GetMethod("MakeJobFor",BindingFlags.Instance|BindingFlags.NonPublic);
                if(disciplineOwner!=null)Check.NotNull(make,"hosted operation must bind source and physical position");
                var opJob=disciplineOwner==null ? owner.GetJob(AbilityContext.Player,worker.Position) : (Job)make!.Invoke(owner,[commander,new LocalTargetInfo(worker.Position),AbilityContext.Player])!;
                opJob.count=1;
                var op=new JobDriver_CommanderOperation{pawn=commander,job=opJob};
                commander.jobs=new Pawn_JobTracker(commander);Set(commander.jobs,"curDriver",op);Set(commander.jobs,"curJob",opJob);
                Check.Equal(true,op.TryMakePreToilReservations(false),"real operation admission");
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var steps=((IEnumerable<Toil>)typeof(JobDriver_CommanderOperation).GetMethod("MakeNewToils",flags)!.Invoke(op,null)!).ToArray();
                steps[0].initAction();steps[2].initAction();steps[2].initAction();
                Check.Equal(true,Math.Abs(might.Stamina.CurLevelPercentage-.4f)<.001f,"one 60 percent payment");
                var evacuation=(CommanderOrderEffect)worker.health.hediffSet.GetFirstHediffOfDef(def);
                Check.NotNull(evacuation,"accepted evacuation");
                Check.Equal(25f,(float)orderType.GetField("Guard",flags)!.GetValue(evacuation)!,"individual shield budget");
                Check.Equal(true,evacuation.ShouldRemove==false,"attended order live");
                var enemyFaction=new Faction {def=new FactionDef()};
                Set(enemyFaction,"relations",new List<FactionRelation>{new(){other=worker.Faction,kind=FactionRelationKind.Hostile}});
                var hostile=PawnWithComps(false,false).pawn;
                hostile.kindDef=new PawnKindDef{race=hostile.def}; hostile.mindState=new Pawn_MindState(hostile);hostile.guest=new Pawn_GuestTracker(hostile);
                Set(hostile,"factionInt",enemyFaction);Set(hostile,"mapIndexOrState",(sbyte)0);Set(hostile,"positionInt",worker.Position+IntVec3.East);
                var previousRelations=typeof(Faction).GetField("relations",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(worker.Faction);
                Set(worker.Faction,"relations",new List<FactionRelation>{new(){other=enemyFaction,kind=FactionRelationKind.Hostile}});
                try
                {
                    var shield=assembly.GetType("TorannMagic.ClassProgression.CommanderOrderEffects")!.GetMethod("Shield",impl)!;
                    object[] first=[worker,new DamageInfo(new DamageDef(),10f,instigator:hostile),false];
                    shield.Invoke(null,first);
                    Check.Equal(true,(bool)first[2],"evacuation absorbs first hostile hit");
                    object[] second=[worker,new DamageInfo(new DamageDef(),20f,instigator:hostile),false];
                    shield.Invoke(null,second);
                    Check.Equal(false,(bool)second[2],"shield cannot absorb more than the 25 point budget");
                    Check.Equal(5f,((DamageInfo)second[1]).Amount,"unabsorbed wound reaches native armor");
                }
                finally { Set(worker.Faction,"relations",previousRelations!); }
                if(disciplineOwner!=null)
                {
                    op=SpiritMapCollapseTests.CommanderRoundTrip(f,commander,disciplineOwner,op);
                    opJob=op.job;Set(commander.jobs,"curJob",opJob);
                }
                else
                {
                    var file=Path.Combine(f.WorkingDirectory,"commander-operation.xml");
                    try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref op,"operation");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
                    op=null!;
                    try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref op,"operation");op.pawn=commander;op.job=opJob;Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
                }
                Set(commander.jobs,"curDriver",op);
                Check.Equal(false,evacuation.ShouldRemove,"operation receipt survives mid-job load");
                steps=((IEnumerable<Toil>)typeof(JobDriver_CommanderOperation).GetMethod("MakeNewToils",flags)!.Invoke(op,null)!).ToArray();steps[2].initAction();
                Check.Equal(true,Math.Abs(might.Stamina.CurLevelPercentage-.4f)<.001f,"no second payment after load");
                typeof(JobDriver_CommanderOperation).GetMethod("Finish",flags)!.Invoke(op,null);
                Check.Equal(true,evacuation.ShouldRemove,"cancelled support ends effects");
                if(disciplineOwner!=null)
                {
                    Check.Same(disciplineOwner,opJob.targetC.Pawn,"job pins source independently of current possession");
                    Set(data,"OperationUntil",0);
                    foreach(var power in might.AbilityData.Powers.OfType<CommanderOperationAbility>())power.CooldownTicksLeft=0;
                    might.Stamina.CurLevelPercentage=1;
                    var interruptedJob=(Job)make!.Invoke(owner,[commander,new LocalTargetInfo(worker.Position),AbilityContext.Player])!;
                    var interrupted=new JobDriver_CommanderOperation{pawn=commander,job=interruptedJob};
                    Set(commander.jobs,"curDriver",interrupted);Set(commander.jobs,"curJob",interruptedJob);
                    Check.Equal(true,interrupted.TryMakePreToilReservations(false),"new operation admitted");
                    var interruptedSteps=((IEnumerable<Toil>)typeof(JobDriver_CommanderOperation).GetMethod("MakeNewToils",flags)!.Invoke(interrupted,null)!).ToArray();
                    interruptedSteps[0].initAction();
                    Set(ClassIdentity.Component(disciplineOwner).SpiritRebirth,"depletionPending",true);
                    Check.Equal(false,interrupted.TryMakePreToilReservations(false),"lost soul cannot continue operation");
                    var finish=typeof(JobDriver_CommanderOperation).GetMethod("Finish",flags)!;
                    finish.Invoke(interrupted,null);finish.Invoke(interrupted,null);
                    Check.Equal(true,Math.Abs(might.Stamina.CurLevelPercentage-.94f)<.001f,"interruption charges original source six percent once");
                    Check.Equal(true,owner.CooldownTicksLeft>0,"interruption cooldown remains with source");
                    Set(ClassIdentity.Component(disciplineOwner).SpiritRebirth,"depletionPending",false);
                }
            }
            finally {might.maxSP=oldMaximum;TorannMagicDefOf.TM_Stamina=oldStamina;}
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            DefDatabase<HediffDef>.Clear();foreach(var d in oldHediffs)DefDatabase<HediffDef>.Add(d);
            DefDatabase<JobDef>.Clear();foreach(var d in oldJobDefs)DefDatabase<JobDef>.Add(d);
            commander.jobs=oldJobs;Set(commander,"positionInt",pos);map.info=info;map.cellIndices=indices;map.thingGrid=grid;map.fogGrid=fog;map.edificeGrid=edifices;
        }
    }
    private static ThingDef BlankDef(ThingCategory category,Type type)
    {
        var d=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.category=category;d.thingClass=type;d.size=new IntVec2(1,1);d.stackLimit=50;d.comps=[];
        return d;
    }
}
