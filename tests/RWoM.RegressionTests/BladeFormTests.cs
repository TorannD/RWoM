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

internal static class BladeFormTests
{
    private static readonly Dictionary<int,int> strikes = new();
    private static readonly List<IntVec3> requestedSteps = new();
    public static bool StartPath(LocalTargetInfo dest, PathEndMode peMode)
    {
        Check.Equal(PathEndMode.OnCell,peMode,"each step must arrive on the precise cell");
        requestedSteps.Add(dest.Cell);
        return false;
    }
    public static bool Contact(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
    {
        Check.Equal(11f,dinfo.Amount,"55 percent of measured melee weapon damage");
        strikes[__instance.thingIDNumber]=strikes.GetValueOrDefault(__instance.thingIDNumber)+1;
        __result=new DamageWorker.DamageResult{totalDamageDealt=dinfo.Amount};
        return false;
    }

    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might)
    {
        var path=ClassIdentity.Component(pawn).State.Find("rwom.bladedancer")!;
        var impl=BindingFlags.NonPublic|BindingFlags.Instance;
        var oldMap=pawn.Map;
        var maps=(List<Map>)typeof(Game).GetField("maps",impl)!.GetValue(Current.Game)!;
        int index=maps.IndexOf(oldMap);
        var oldGear=pawn.equipment;var oldPos=pawn.Position;var oldJobs=pawn.jobs;var oldPather=pawn.pather;
        var oldStamina=TorannMagicDefOf.TM_Stamina;var oldQuests=Current.Game.questManager;
        var oldDefs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        var oldTicks=Find.TickManager.TicksGame;var relations=typeof(Faction).GetField("relations",impl)!;
        var oldRelations=relations.GetValue(pawn.Faction);
        var owner=might.AbilityData.Powers.OfType<BladeFormAbility>().Single();
        var oldVerb=owner.Def.MainVerb.verbClass;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;string id="rwom.blade.form."+Guid.NewGuid();
        var h=Activator.CreateInstance(ht,id)!;
        void Patch(MethodInfo target,MethodInfo prefix) => ht.GetMethod("Patch")!.Invoke(h,[target,Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,prefix),null,null,null]);
        try
        {
            var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));maps[index]=map;
            map.info=new MapInfo{Size=new IntVec3(44,1,44)};map.cellIndices=new CellIndices(map);map.thingGrid=new ThingGrid(map);
            map.edificeGrid=new EdificeGrid(map);map.fogGrid=new FogGrid(map);map.coverGrid=new CoverGrid(map);
            map.designationManager=new DesignationManager(map);map.listerMergeables=new ListerMergeables(map);
            map.regionGrid=new RegionGrid(map);
            var grid=new PathGrid(map,new PathGridDef {defName="BladeFormPath"});var ctx=new PathingContext(map,grid);
            map.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));Set(map.pathing,"normal",ctx);Set(map.pathing,"fenceBlocked",ctx);
            var start=new IntVec3(10,0,10);var end=start+IntVec3.East*12;Set(pawn,"positionInt",start);
            Current.Game.questManager=new QuestManager();
            var foe=new Faction{def=new FactionDef()};Set(foe,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));var tool=new Tool{power=20};
            def.category=ThingCategory.Item;def.equipmentType=EquipmentType.Primary;def.tools=[tool];def.comps=[];
            var sword=new ThingWithComps{def=def};var eq=new CompEquippable{parent=sword};eq.Initialize(new CompProperties{compClass=typeof(CompEquippable)});
            var cut=new DamageDef{defName="BladeFormCut"};Set(cut,"externalViolence",true);
            var strike=new Verb_MeleeAttackDamage{caster=pawn,verbTracker=eq.verbTracker,verbProps=new VerbProperties{verbClass=typeof(Verb_MeleeAttackDamage),meleeDamageDef=cut},tool=tool};
            Set(eq.verbTracker,"verbs",new List<Verb>{strike});Set(sword,"comps",new List<ThingComp>{eq});
            pawn.equipment=new Pawn_EquipmentTracker(pawn);pawn.equipment.AddEquipment(sword);
            owner.Def.MainVerb.verbClass=typeof(Verb_BladeSevenSteps);Set(owner,"verb",null!);
            if(DefDatabase<JobDef>.GetNamedSilentFail("TM_BladeSevenStepsCast")==null)
                DefDatabase<JobDef>.Add(new JobDef{defName="TM_BladeSevenStepsCast",driverClass=typeof(JobDriver_BladeSevenSteps)});
            if(might.Stamina==null)
            {
                TorannMagicDefOf.TM_Stamina=new NeedDef{defName="FormStamina",needClass=typeof(Need_Stamina)};
                pawn.needs.AllNeeds.Add(new Need_Stamina(pawn){def=TorannMagicDefOf.TM_Stamina});
            }
            Set(pawn.ageTracker,"ageBiologicalTicksInt",20L*3600000L);might.Stamina.CurLevelPercentage=1f;
            var verb=(Verb_BladeSevenSteps)owner.Verb;
            Check.Equal(false,verb.CanHitTargetFrom(start,start),"zero length");
            Check.Equal(false,verb.CanHitTargetFrom(start,end+IntVec3.East),"route longer than twelve cells");
            var door=new Building_Door{def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
            door.def.size=new IntVec2(1,1);door.def.building=new BuildingProperties();door.def.fillPercent=1;
            Set(door,"positionInt",start+IntVec3.East*3);Set(door,"mapIndexOrState",(sbyte)index);map.edificeGrid.Register(door);
            Check.Equal(false,verb.CanHitTargetFrom(start,end),"closed door blocks entire preview");map.edificeGrid.DeRegister(door);
            Check.Equal(true,verb.CanHitTargetFrom(start,end),"open route targets normally");
            var job=owner.GetJob(AbilityContext.Player,end);
            var driver=new JobDriver_BladeSevenSteps{pawn=pawn,job=job};
            object? Call(string method,params object[] args)=>typeof(JobDriver_BladeSevenSteps).GetMethod(method,impl)!.Invoke(driver,args);
            Check.Equal(true,driver.TryMakePreToilReservations(false),"native entry with melee weapon");
            var steps=((IEnumerable<Toil>)Call("MakeNewToils")!).ToArray();
            steps[0].initAction();Call("FinishForm");Call("FinishForm");
            Check.Equal(true,Math.Abs(might.Stamina.CurLevelPercentage-.94f)<.001f,"cancelled preparation partial price once");
            Set(path,"BladeFormReadyAt",0);owner.CooldownTicksLeft=0;might.Stamina.CurLevelPercentage=1;
            driver=new JobDriver_BladeSevenSteps{pawn=pawn,job=job};steps=((IEnumerable<Toil>)Call("MakeNewToils")!).ToArray();
            steps[0].initAction();steps[2].initAction();steps[2].initAction();
            Check.Equal(true,Math.Abs(might.Stamina.CurLevelPercentage-.4f)<.001f,"60 percent once");
            Check.Equal(start,pawn.Position,"starting form did not relocate pawn");
            pawn.jobs=new Pawn_JobTracker(pawn);Set(pawn.jobs,"curDriver",driver);
            var incoming=new DamageInfo(cut,20f);ClassIdentity.Component(pawn).PostPreApplyDamage(ref incoming,out _);
            Check.Equal(23f,incoming.Amount,"15 percent exposure");
            requestedSteps.Clear();pawn.pather=new Pawn_PathFollower(pawn);
            Patch(typeof(Pawn_PathFollower).GetMethod(nameof(Pawn_PathFollower.StartPath),[typeof(LocalTargetInfo),typeof(PathEndMode)])!,
                typeof(BladeFormTests).GetMethod(nameof(StartPath))!);
            for(int i=0;i<12;i++)
            {
                var move=steps[3+i*3];move.actor=pawn;move.initAction();
                Check.Equal(start+IntVec3.East*(i+1),requestedSteps.Last(),"native pather receives next route cell");
            }
            Check.Equal(12,requestedSteps.Count,"twelve movement requests");
            Check.Equal(start,pawn.Position,"no hidden teleport during movement requests");
            Set(door,"positionInt",start+IntVec3.East);map.edificeGrid.Register(door);
            Check.Equal(false,(bool)Call("StepOpen",start,start+IntVec3.East)!,"closed door interrupts current step");
            map.edificeGrid.DeRegister(door);
            List<Pawn> targets=[];
            foreach(var offset in new[]{IntVec3.North,IntVec3.East,IntVec3.South,IntVec3.West})
            {
                var target=PawnWithComps(false,false).pawn;target.def.category=ThingCategory.Pawn;target.def.size=new IntVec2(1,1);target.thingIDNumber=8100+targets.Count;
                target.kindDef=new PawnKindDef{race=target.def};target.guest=new Pawn_GuestTracker(target);target.mindState=new Pawn_MindState(target);
                Set(target,"factionInt",foe);Set(target,"mapIndexOrState",(sbyte)index);Set(target,"positionInt",start+offset);
                map.thingGrid.Register(target);targets.Add(target);
            }
            Patch(typeof(VerbProperties).GetMethod(nameof(VerbProperties.AdjustedMeleeDamageAmount),[typeof(Verb),typeof(Pawn)])!,typeof(BladePassageTests).GetMethod("WeaponDamage")!);
            Patch(typeof(VerbProperties).GetMethod(nameof(VerbProperties.AdjustedArmorPenetration),[typeof(Verb),typeof(Pawn)])!,typeof(BladePassageTests).GetMethod("WeaponPenetration")!);
            Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,typeof(BladeFormTests).GetMethod(nameof(Contact))!);
            strikes.Clear();Call("Contact",1);Call("Contact",1);
            Check.Equal(1,strikes.Values.Sum(),"single attempted step cannot deal double damage");
            var file=Path.Combine(f.WorkingDirectory,"blade-form-job.xml");
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref driver,"job");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            driver=null!;try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref driver,"job");driver.pawn=pawn;driver.job=job;Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Call("Contact",1);for(int i=2;i<=12;i++)Call("Contact",i);
            Check.Equal(7,strikes.Values.Sum(),"at most seven actual contacts");
            Check.Equal(3,strikes.Count,"at most three distinct enemies");
            Check.Equal(3,strikes.Values.Max(),"at most three contacts on one enemy");
            Set(Find.TickManager,"ticksGameInt",oldTicks+200);Call("FinishForm");Call("FinishForm");
            Check.Equal(oldTicks+560,(int)path.GetType().GetField("BladeExhaustedUntil",impl)!.GetValue(path)!,"six seconds exhaustion");
            Check.Equal(oldTicks+120000,(int)path.GetType().GetField("BladeFormReadyAt",impl)!.GetValue(path)!,"original cooldown preserved on finish");
            Check.Equal(true,Math.Abs(might.Stamina.CurLevelPercentage-.4f)<.001f,"saved result never billed twice");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(h,[id]);maps[index]=oldMap;Set(pawn,"positionInt",oldPos);
            pawn.jobs=oldJobs;pawn.pather=oldPather;pawn.equipment=oldGear;relations.SetValue(pawn.Faction,oldRelations);
            Set(Find.TickManager,"ticksGameInt",oldTicks);TorannMagicDefOf.TM_Stamina=oldStamina;
            Current.Game.questManager=oldQuests;owner.Def.MainVerb.verbClass=oldVerb;
            DefDatabase<JobDef>.Clear();foreach(var def in oldDefs)DefDatabase<JobDef>.Add(def);
        }
    }
}
