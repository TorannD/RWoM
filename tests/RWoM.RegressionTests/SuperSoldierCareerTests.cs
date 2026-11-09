using System.Reflection;
using AbilityUser;
using RimWorld;
using System.Runtime.CompilerServices;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class SuperSoldierCareerTests
{
    private sealed class Definitions : IDisposable
    {
        internal readonly Dictionary<TMAbilityDef, Type> Old=new();
        public void Dispose(){foreach(var e in Old)e.Key.abilityClass=e.Value;}
    }
    internal static IDisposable Setup(RuntimeFixture f)
    {
        var defs=new Definitions();
        foreach(string name in new[]{"TM_SuperSoldierFieldDressing","TM_CQC","TM_PistolSpec","TM_RifleSpec","TM_ShotgunSpec","TM_PistolWhip","TM_SuppressingFire","TM_Buckshot"})
        {
            var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail(name)??f.AddAbility(name);
            defs.Old.Add(def,def.abilityClass);def.abilityClass=typeof(PawnAbility);
        }
        return defs;
    }
    private sealed class Wound : Hediff_Injury
    {
        internal bool Bandaged;
        internal Action? AfterTend;
        public override bool TendableNow(bool ignoreTimer=false)=>!Bandaged;
        public override void Tended(float quality,float maxQuality,int batchPosition=0) {Bandaged=true;AfterTend?.Invoke();}
    }
    public static bool Potency(ref float __result){__result=1;return false;}
    public static bool Skip()=>false;
    internal static void Supplies(Pawn p,Pawn? disciplineOwner=null,Action? afterTend=null,bool credit=true)
    {
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.test.soldier."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        void Prefix(MethodInfo method,string name)
        {
            var hook=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(SuperSoldierCareerTests).GetMethod(name)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,hook,null,null,null]);
        }
        var wound=new Wound{pawn=p,def=new HediffDef{defName="SoldierFieldWound",tendable=true}};
        var item=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        item.defName="SoldierMedicine";item.category=ThingCategory.Item;item.stackLimit=20;item.thingClass=typeof(Medicine);item.comps=[];item.destroyable=true;
        var med=new Medicine{def=item,stackCount=3,thingIDNumber=61321};
        bool Dress()=>(bool)typeof(Verb_SuperSoldierFieldDressing).GetMethod("Dress",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[p])!;
        try
        {
            Prefix(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValue" && m.GetParameters().Length==4 && m.GetParameters()[0].ParameterType==typeof(Thing)),nameof(Potency));
            Check.Equal(true,p.inventory.innerContainer.TryAdd(med),"actual medicine stack in inventory");
            Check.Equal(false,Dress(),"no wound costs no medicine");
            p.health.hediffSet.hediffs.Add(wound);
            Check.Equal(true,Dress(),"field dressing succeeds");
            Check.Equal(true,wound.Bandaged,"wound actually tended");
            Check.Equal(2,med.stackCount,"one paid medicine");
            Check.Equal(false,Dress(),"an already dressed wound is not billed again");
            var oldWorld=Current.Game.World;var actorJobs=p.jobs;var actorRecords=p.records;var industrial=ThingDefOf.MedicineIndustrial;
            try
            {
                var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
                world.factionManager=new FactionManager();world.factionManager.AllFactionsListForReading.Add(p.Faction);
                Set(world.factionManager,"ofPlayer",p.Faction);Set(Current.Game,"worldInt",world);
                RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(RecordDefOf));
                var cls=typeof(Verb_SuperSoldierFieldDressing).Assembly.GetType("TorannMagic.ClassProgression.SuperSoldierMedicalTrial")!;
                var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[cls])!;proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
                Prefix(typeof(TendUtility).GetMethods().Single(m=>m.Name=="CalculateBaseTendQuality" && m.GetParameters().Length==3),nameof(Potency));
                Prefix(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValueAbstract" && m.GetParameters()[0].ParameterType==typeof(BuildableDef) && m.GetParameters().Length==3),nameof(Potency));
                Prefix(typeof(Pawn_RecordsTracker).GetMethod("Increment")!,nameof(Skip));
                ThingDefOf.MedicineIndustrial=item;
                var target=PawnWithComps(false,false).pawn;
                target.def.category=ThingCategory.Pawn;target.def.size=new IntVec2(1,1);
                target.kindDef=new PawnKindDef{race=target.def};target.guest=new Pawn_GuestTracker(target);
                target.mindState=new Pawn_MindState(target);target.thingIDNumber=57281;
                Set(target,"mapIndexOrState",(sbyte)0);Set(target,"positionInt",p.Position+IntVec3.East);Set(target,"factionInt",p.Faction);
                target.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));
                p.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));
                p.jobs=new Pawn_JobTracker(p);
                var job=new Job{def=new JobDef{defName="TendPatient",driverClass=typeof(JobDriver_TendPatient)},targetA=target};
                Set(p.jobs,"curJob",job);Set(p.jobs,"curDriver",new JobDriver_TendPatient{job=job,pawn=p});
                var allied=new Wound{def=new HediffDef{defName="FieldTrainingInjury",tendable=true},pawn=target,loadID=9271,AfterTend=afterTend};
                Set(allied,"severityInt",1f);
                target.health.hediffSet.hediffs.Add(allied);
                TendUtility.DoTend(p,target,med);
                Check.Equal(true,allied.Bandaged,"native medical work finishes");
                Check.Equal(1,med.stackCount,"native tend consumes real medicine");
                var path=ClassIdentity.Component(disciplineOwner??p).State.Find("rwom.supersoldier")!;
                const BindingFlags field=BindingFlags.NonPublic|BindingFlags.Instance;
                var data=path.GetType().GetField("MartialData",field)!.GetValue(path)!;
                if(credit) Check.NotNull(data,"medical receipt reaches discipline owner");
                Check.Equal(credit,data!=null && ((List<int>)data.GetType().GetField("Wounds",field)!.GetValue(data)!).Contains(9271),"native tending credit follows exact surviving owner");
            }
            finally{Set(Current.Game,"worldInt",oldWorld);p.jobs=actorJobs;p.records=actorRecords;ThingDefOf.MedicineIndustrial=industrial;}
            if(disciplineOwner!=null && disciplineOwner!=p) return;
            Prefix(typeof(HediffSet).GetMethod("DirtyCache")!,nameof(Skip));
            Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!,nameof(Skip));
            var originalThing=TorannMagicDefOf.BookOfSuperSoldier;
            var originalHediff=TorannMagicDefOf.TM_SS_SerumHD;
            var doseDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));doseDef.defName="BookOfSuperSoldier";doseDef.category=ThingCategory.Item;
            doseDef.stackLimit=20;doseDef.thingClass=typeof(ThingWithComps);doseDef.comps=[];doseDef.destroyable=true;
            var serumDef=new HediffDef{defName="TM_SS_SerumHD",hediffClass=typeof(Hediff),initialSeverity=.1f,maxSeverity=10};
            var doses=new ThingWithComps{def=doseDef,stackCount=3,thingIDNumber=62320};
            try
            {
                TorannMagicDefOf.BookOfSuperSoldier=doseDef;TorannMagicDefOf.TM_SS_SerumHD=serumDef;
                var use=new CompUseEffect_LearnMight{parent=doses};
                use.DoEffect(p);
                Check.Equal(3,doses.stackCount,"cannot pay from an unowned serum");
                Check.Equal(true,p.inventory.innerContainer.TryAdd(doses),"real carried serum");
                use.DoEffect(p);use.DoEffect(p);
                Check.Equal(2,doses.stackCount,"one physically administered dose");
                Check.Equal(1,p.health.hediffSet.hediffs.Count(h=>h.def==serumDef),"one medical hediff");
                Check.Equal(.1f,p.health.hediffSet.hediffs.Single(h=>h.def==serumDef).Severity,"initial severity independent of school");
            }
            finally
            {
                p.health.hediffSet.hediffs.RemoveAll(h=>h.def==serumDef);
                p.inventory.innerContainer.Remove(doses);
                TorannMagicDefOf.BookOfSuperSoldier=originalThing;TorannMagicDefOf.TM_SS_SerumHD=originalHediff;
            }
        }
        finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);p.health.hediffSet.hediffs.Remove(wound);p.inventory.innerContainer.Remove(med);}
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var type=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.SuperSoldierMartialPath",true)!;
        object? Run(string name,params object[] args)=>type.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,args);
        var data=might.MightData;
        var serum=pawn.health?.hediffSet?.hediffs.FirstOrDefault(h=>h.def==TorannMagicDefOf.TM_SS_SerumHD);
        Check.Equal(true,serum==null,"manual gives no serum");
        if(alternate) Supplies(pawn);
        string school=alternate?"TM_ShotgunSpec":"TM_PistolSpec";
        Check.Equal(true,(bool)Run("SelectSchool",pawn,school)!,"one weapon school");
        Check.Equal(false,(bool)Run("SelectSchool",pawn,"TM_RifleSpec")!,"no second school");
        Check.Equal(false,(bool)Run("Advance",pawn,false)!,"trial not met");
        for(int i=1;i<=10;i++) Run("Event",pawn,i,false,0);
        Check.Equal(true,(bool)Run("Advance",pawn,false)!,"service or medicine");
        for(int i=1;i<=10;i++) Run("Event",pawn,i,true,0);
        Check.Equal(true,(bool)Run("Advance",pawn,false)!,"school attack trial");
        for(int i=0;i<8;i++) Run("Event",pawn,1,false,i);
        Check.Equal(false,(bool)Run("Advance",pawn,alternate)!,"level gate");
        data.MightUserLevel=30;
        Check.Equal(true,(bool)Run("Advance",pawn,alternate)!,"graduate");
        var oldSerum=TorannMagicDefOf.TM_SS_SerumHD;
        var physicalSerum=TorannMagicDefOf.TM_SS_SerumHD??=new HediffDef{defName="TM_SS_SerumHD",maxSeverity=100};
        var existingSerum=new Hediff{def=physicalSerum,pawn=pawn};
        Set(existingSerum,"severityInt",1.37f);pawn.health.hediffSet.hediffs.Add(existingSerum);
        try {
        var state=comp.State;string file=Path.Combine(f.WorkingDirectory,"soldier-training.xml");
        try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}
        finally{Scribe.ForceStop();}
        state=null!;
        try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}
        finally{Scribe.ForceStop();}
        Set(comp,"state",state);
        ClassProgressionFeature.Enabled=false;
        try {Check.Equal(false,(bool)Run("SelectSchool",pawn,"TM_RifleSpec")!,"beta disabled");Check.Equal(true,Run("Path",pawn)==null,"no active path during pause");}
        finally {ClassProgressionFeature.Enabled=true;}
        var path=comp.State.Find("rwom.supersoldier")!;
        type.GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(Activator.CreateInstance(type,true),[might,path]);
        Check.Equal(InitiationStage.Ascended,path.Stage,"saved stage");
        Check.Equal(1,data.MightPowersSS.Count(x=>x.learned && new[]{"TM_PistolSpec","TM_RifleSpec","TM_ShotgunSpec"}.Contains(x.abilityDef?.defName)),"one saved specialization");
        Check.Equal(1,pawn.health.hediffSet.hediffs.Count(h=>h.def==physicalSerum),"only the original serum");
        Check.Equal(1.37f,existingSerum.Severity,"restoration preserves the original dose");
        Check.Equal(false,(bool)Run("CanInject",pawn)!,"no repeat injection with active serum");
        } finally {pawn.health.hediffSet.hediffs.Remove(existingSerum);TorannMagicDefOf.TM_SS_SerumHD=oldSerum;}
    }
}
