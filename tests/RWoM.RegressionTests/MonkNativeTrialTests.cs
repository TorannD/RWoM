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

// Keep the native medical and stance transitions; isolate only the rendered
// hit, social notifications and weapon-independent medical stat calculations.
internal static class MonkNativeTrialTests
{
    private sealed class FreshInjury : Hediff_Injury
    {
        internal bool Bandaged;
        public override bool TendableNow(bool ignoreTimer=false)=>!Bandaged;
        public override void Tended(float quality,float maxQuality,int batchPosition=0)=>Bandaged=true;
    }
    public static bool One(ref float __result) { __result=1; return false; }
    public static bool Skip()=>false;
    private static float damage;
    internal static int HitCount;
    private static Thing? instigator;
    public static bool Injury(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
    {
        damage=dinfo.Amount;
        HitCount++;
        instigator=dinfo.Instigator;
        __result=new DamageWorker.DamageResult{totalDamageDealt=damage};
        return false;
    }
    internal static void Verify(Pawn actor,bool cultivation,Pawn? trainingOwner=null,Action<Pawn>? customPalm=null)
    {
        const BindingFlags hidden=BindingFlags.NonPublic|BindingFlags.Instance;
        var asm=typeof(SniperSurvey).Assembly;
        var api=asm.GetType("TorannMagic.ClassProgression.MonkMartialPath",true)!;
        var path=ClassIdentity.Component(trainingOwner??actor).State.Find("rwom.monk")!;
        var field=path.GetType().GetField("MartialData",hidden)!;
        var old=field.GetValue(path);
        var data=Activator.CreateInstance(asm.GetType("TorannMagic.ClassProgression.MonkDisciplineData",true)!,true)!;
        field.SetValue(path,data);
        if(trainingOwner!=null) Set(data,"SealUpgrades",2);
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.regression.monk.trial."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        void Prefix(MethodBase target,string fn)
        {
            var hm=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(MonkNativeTrialTests).GetMethod(fn)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[target,hm,null,null,null]);
        }
        var oldWorld=Current.Game.World;var oldJob=actor.jobs;var oldRecords=actor.records;var pos=actor.Position;
        var oldRelations=typeof(Faction).GetField("relations",hidden)!.GetValue(actor.Faction);
        var map=actor.Map;
        var damageField=asm.GetType("TorannMagic.TMDamageDefOf+DamageDefOf",true)!.GetField("TM_ChiBurn")!;
        var oldDamage=damageField.GetValue(null);
        try
        {
            map.info=new MapInfo{Size=new IntVec3(40,1,40)};map.cellIndices=new CellIndices(map);map.edificeGrid=new EdificeGrid(map);
            var context=new PathingContext(map,new PathGrid(map,new PathGridDef{defName="MonkSlipTest"}));
            map.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));
            Set(map.pathing,"normal",context);Set(map.pathing,"fenceBlocked",context);
            map.thingGrid=new ThingGrid(map);
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager=new FactionManager();world.factionManager.AllFactionsListForReading.Add(actor.Faction);
            Set(world.factionManager,"ofPlayer",actor.Faction);Set(Current.Game,"worldInt",world);
            Set(actor,"positionInt",new IntVec3(18,0,18));
            var foe=new Faction{def=new FactionDef()};
            Set(foe,"relations",new List<FactionRelation>{new(){other=actor.Faction,kind=FactionRelationKind.Hostile}});
            Set(actor.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
            var target=PawnWithComps(false,false).pawn;
            target.def.category=ThingCategory.Pawn;target.def.size=new IntVec2(1,1);
            target.kindDef=new PawnKindDef{race=target.def};target.guest=new Pawn_GuestTracker(target);target.mindState=new Pawn_MindState(target);
            target.thingIDNumber=9843;target.jobs=new Pawn_JobTracker(target);
            Set(target,"mapIndexOrState",(sbyte)0);Set(target,"positionInt",actor.Position+IntVec3.East);
            Set(target,"factionInt",cultivation?actor.Faction:foe);
            if (!cultivation)
            {
                Prefix(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(Injury));
                Prefix(typeof(BattleLog).GetMethod("Add")!,nameof(Skip));
                Set(Find.TickManager, "gameStartAbsTick", 1);
                Set(Current.Game, "battleLog", new BattleLog());
                damageField.SetValue(null,new DamageDef{defName="MonkPalmDamage"});
                target.stances=new Pawn_StanceTracker(target);
                target.stances.curStance=(Stance_Warmup)RuntimeHelpers.GetUninitializedObject(typeof(Stance_Warmup));
                if(customPalm!=null) {HitCount=0;customPalm(target);return;}
                var verb=new Verb_MonkPalmSeal{caster=actor,verbProps=new VerbProperties_Ability()};
                Set(verb,"currentTarget",new LocalTargetInfo(target));
                bool Cast()=>(bool)typeof(Verb_MonkPalmSeal).GetMethod("TryCastShot",hidden)!.Invoke(verb,null)!;
                Check.Equal(true,Cast(),"seal lands on adjacent hostile");
                Check.Equal(true,target.stances.stunner.Stunned,"native stun applied");
                Check.Equal(true,target.stances.curStance is Stance_Mobile,"old preparation actually cancelled");
                Check.Equal(trainingOwner==null?16f:28f,damage,"source spiritual damage rank");
                Check.Same(actor,instigator!,"physical carrier remains damage instigator");
                Check.Equal(1,(int)data.GetType().GetField("Interrupts",hidden)!.GetValue(data)!,"real interruption counted");
                Cast();
                Check.Equal(1,(int)data.GetType().GetField("Interrupts",hidden)!.GetValue(data)!,"already stunned target gives no preparation");
                Set(target,"factionInt",actor.Faction);
                Check.Equal(false,Cast(),"ally rejected");
                if(trainingOwner!=null)
                {
                    Set(target,"factionInt",foe);
                    Set(ClassIdentity.Component(trainingOwner).SpiritRebirth,"depletionPending",true);
                    Check.Equal(false,Cast(),"depleted source cannot strike through carrier");
                }
            }
            else
            {
                RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(RecordDefOf));
                var patch=asm.GetType("TorannMagic.ClassProgression.MonkTendingTrial",true)!;
                var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[patch])!;proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
                Prefix(typeof(TendUtility).GetMethods().Single(m=>m.Name=="CalculateBaseTendQuality"&&m.GetParameters().Length==3),nameof(One));
                Prefix(typeof(Pawn_RecordsTracker).GetMethod("Increment")!,nameof(Skip));
                target.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));
                actor.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));
                actor.jobs=new Pawn_JobTracker(actor);
                var job=new Job{def=new JobDef{defName="TendPatient",driverClass=typeof(JobDriver_TendPatient)},targetA=target};
                Set(actor.jobs,"curJob",job);Set(actor.jobs,"curDriver",new JobDriver_TendPatient{job=job,pawn=actor});
                var wound=new FreshInjury{def=new HediffDef{defName="MonkScratch",tendable=true},pawn=target,loadID=3792};
                target.health.hediffSet.hediffs.Add(wound);
                TendUtility.DoTend(actor,target,null);
                Check.Equal(true,wound.Bandaged,"native tending completed");
                var wounds=(List<int>)data.GetType().GetField("Wounds",hidden)!.GetValue(data)!;
                Check.NotNull(wounds,"tended wounds credited to training owner");
                Check.Equal(1,wounds.Count,"actual wound credited");
                wound.Bandaged=false;TendUtility.DoTend(actor,target,null);
                Check.Equal(1,wounds.Count,"same injury cannot satisfy two awards");
                if(trainingOwner==null)
                    Check.Equal(false,TM_Calc.MonkMeditateWorthAutocast(actor),"cultivation does not interrupt active medical order");
                if(trainingOwner!=null)
                {
                    Set(ClassIdentity.Component(trainingOwner).SpiritRebirth,"depletionPending",true);
                    var another=new FreshInjury{def=wound.def,pawn=target,loadID=3793};
                    target.health.hediffSet.hediffs.Add(another);
                    TendUtility.DoTend(actor,target,null);
                    Check.Equal(true,another.Bandaged,"ordinary carrier tending still works after source depletion");
                    Check.Equal(1,wounds.Count,"depleted source receives no further wound credit");
                }
            }
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);field.SetValue(path,old);Set(Current.Game,"worldInt",oldWorld);
            Set(actor.Faction,"relations",oldRelations);Set(actor,"positionInt",pos);actor.jobs=oldJob;actor.records=oldRecords;
            damageField.SetValue(null,oldDamage);
        }
    }
}
