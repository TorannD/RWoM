using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class RangerAnimalCareTrialTests
{
    private sealed class FreshWound : Hediff_Injury
    {
        public bool Bandaged;
        public override bool TendableNow(bool ignoreTimer=false)=>!Bandaged;
        public override void Tended(float quality,float maxQuality,int batchPosition=0)=>Bandaged=true;
    }
    public static bool One(ref float __result){__result=1f;return false;}
    public static bool Skip()=>false;
    internal static void Verify(Pawn ranger,Pawn? owner=null)
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(RecordDefOf));
        var state=ClassIdentity.Component(owner??ranger).State.Find("rwom.ranger")!;
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var count=state.GetType().GetField("RangerCares",flags)!;var time=state.GetType().GetField("RangerNextCareShift",flags)!;
        var oldCount=count.GetValue(state);var cooldown=time.GetValue(state);
        var jobs=ranger.jobs;var records=ranger.records;var gameWorld=Current.Game.World;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;var id="rwom.test.ranger.animal-care."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(ht,id)!;
        void Prefix(MethodBase method,string fn)
        {
            var hook=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(RangerAnimalCareTrialTests).GetMethod(fn)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,hook,null,null,null]);
        }
        try
        {
            var w=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            w.factionManager=new FactionManager();w.factionManager.AllFactionsListForReading.Add(ranger.Faction);
            Set(w.factionManager,"ofPlayer",ranger.Faction);Set(Current.Game,"worldInt",w);
            var patch=typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.RangerAnimalCareTrial",true)!;
            var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[patch])!;proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
            Prefix(typeof(TendUtility).GetMethods().Single(m=>m.Name=="CalculateBaseTendQuality"&&m.GetParameters().Length==3),nameof(One));
            Prefix(typeof(Pawn_RecordsTracker).GetMethod("Increment")!,nameof(Skip));
            ranger.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));ranger.jobs=new Pawn_JobTracker(ranger);
            var pet=PawnWithComps(false,false).pawn;
            pet.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            pet.def.race=new RaceProperties{intelligence=Intelligence.Animal,playerCanChangeMaster=false};
            Set(pet.def.race,"fleshType",new FleshTypeDef{isOrganic=true});
            pet.kindDef=new PawnKindDef{race=pet.def};pet.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));
            pet.def.category=ThingCategory.Pawn;pet.def.size=new IntVec2(1,1);pet.mindState=new Pawn_MindState(pet);
            Set(pet,"mapIndexOrState",(sbyte)0);Set(pet,"positionInt",ranger.Position+IntVec3.East);
            var job=new Job{def=new JobDef{defName="TendPatient",driverClass=typeof(JobDriver_TendPatient)},targetA=pet};
            Set(ranger.jobs,"curJob",job);Set(ranger.jobs,"curDriver",new JobDriver_TendPatient{job=job,pawn=ranger});
            var wound=new FreshWound{def=new HediffDef{defName="RangerCareInjury",tendable=true},pawn=pet};
            void Treat()=>TendUtility.DoTend(ranger,pet,null);
            pet.health.hediffSet.hediffs.Add(wound);Treat();
            Check.Equal(true,wound.Bandaged,"native treatment reaches injury");
            Check.Equal(0,(int)count.GetValue(state)!,"foreign animal excluded");
            Set(pet,"factionInt",ranger.Faction);Treat();
            Check.Equal(0,(int)count.GetValue(state)!,"no healthy repeat");
            wound.Bandaged=false;Treat();Check.Equal(1,(int)count.GetValue(state)!,"treated colony animal counts");
            wound.Bandaged=false;Treat();Check.Equal(1,(int)count.GetValue(state)!,"one shift per day");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);count.SetValue(state,oldCount);time.SetValue(state,cooldown);
            ranger.jobs=jobs;ranger.records=records;Set(Current.Game,"worldInt",gameWorld);
        }
    }
}
