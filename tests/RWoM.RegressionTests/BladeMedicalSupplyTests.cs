using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeMedicalSupplyTests
{
    public sealed class TestWound : Hediff
    {
        public bool Treated;
        public override bool TendableNow(bool ignoreTimer=false) => !Treated;
        public override void Tended(float quality,float maxQuality,int batchPosition=0) => Treated=true;
    }
    public static bool Enabled;
    public static bool Medical(ref bool __result) {__result=Enabled;return false;}
    public static bool One(ref float __result) {__result=1f;return false;}
    public static bool Skip() => false;

    internal static void Verify(RuntimeFixture f, Pawn actor, Pawn? source=null)
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(WorkTypeDefOf));
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(RecordDefOf));
        var path = ClassIdentity.Component(source??actor).State.Find("rwom.bladedancer")!;
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var covered=path.GetType().GetField("BladeDeliveries",flags)!;
        var next=path.GetType().GetField("BladeNextSupplyAt",flags)!;
        var before=covered.GetValue(path);var ready=next.GetValue(path);
        int Count()=>(int)covered.GetValue(path)!;
        var map=actor.Map;var pos=actor.Position;var info=map.info;var ci=map.cellIndices;var grid=map.thingGrid;
        var works=actor.workSettings;var carry=actor.carryTracker;var jobs=actor.jobs;var oldWorld=Current.Game.World;
        var industrial=ThingDefOf.MedicineIndustrial;var oldRecords=actor.records;var oldTick=Find.TickManager.TicksGame;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.regression.blade.supply."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        void Prefix(MethodBase method,string fn)
        {
            var hook=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(BladeMedicalSupplyTests).GetMethod(fn)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,hook,null,null,null]);
        }
        var api=typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.BladeMedicalSupply")!;
        try
        {
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager=new FactionManager();world.factionManager.AllFactionsListForReading.Add(actor.Faction);
            Set(world.factionManager,"ofPlayer",actor.Faction);Set(Current.Game,"worldInt",world);
            map.info=new MapInfo{Size=new IntVec3(40,1,40)};map.cellIndices=new CellIndices(map);map.thingGrid=new ThingGrid(map);
            actor.workSettings=new Pawn_WorkSettings(actor);actor.carryTracker=new Pawn_CarryTracker(actor);actor.jobs=new Pawn_JobTracker(actor);
            actor.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));Set(Find.TickManager,"ticksGameInt",1000);
            var cell=new IntVec3(10,0,10);Set(actor,"positionInt",cell);
            ThingDef Def()=> (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            var bed=new Building_Bed{def=Def(),thingIDNumber=98601};bed.def.size=new IntVec2(1,1);
            Set(bed,"medicalInt",true);Set(bed,"factionInt",actor.Faction);Set(bed,"positionInt",cell);Set(bed,"mapIndexOrState",(sbyte)0);
            map.thingGrid.Register(bed);
            var patient=PawnWithComps(false,false).pawn;
            patient.kindDef=new PawnKindDef{race=patient.def};patient.def.category=ThingCategory.Pawn;patient.def.size=new IntVec2(1,1);
            patient.mindState=new Pawn_MindState(patient);patient.guest=new Pawn_GuestTracker(patient);patient.records=(Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));
            patient.jobs=new Pawn_JobTracker(patient);Set(patient.jobs,"curJob",new Job{def=new JobDef{defName="LayDown"}});
            patient.jobs.posture=PawnPosture.LayingInBed;Set(patient,"factionInt",actor.Faction);Set(patient,"positionInt",cell);
            Set(patient,"mapIndexOrState",(sbyte)0);map.thingGrid.Register(patient);
            var wound=new TestWound{pawn=patient,def=new HediffDef{defName="SupplyTestWound",tendable=true}};
            patient.health.hediffSet.hediffs.Add(wound);
            var medDef=Def();medDef.defName="SupplyTestMedicine";medDef.category=ThingCategory.Item;medDef.thingClass=typeof(Medicine);medDef.stackLimit=75;medDef.comps=[];
            ThingDefOf.MedicineIndustrial=medDef;
            var stock=new Medicine{def=medDef,thingIDNumber=98602,stackCount=12};
            var assigned=new Job{def=new JobDef{defName="TendPatient",driverClass=typeof(JobDriver_TendPatient)},targetA=patient,targetB=stock};
            void Assign(Job job)
            {
                Set(actor.jobs,"curJob",job);
                Set(actor.jobs,"curDriver",new JobDriver_TendPatient{job=job,pawn=actor});
            }
            Assign(assigned);
            Prefix(typeof(Pawn_WorkSettings).GetMethod("WorkIsActive")!,nameof(Medical)); Enabled=true;
            Prefix(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(One));
            Prefix(typeof(StatExtension).GetMethods().Single(m=>m.Name=="GetStatValueAbstract" && m.GetParameters()[0].ParameterType==typeof(BuildableDef) && m.GetParameters().Length==3),nameof(One));
            Prefix(typeof(Pawn_RecordsTracker).GetMethod("Increment")!,nameof(Skip));
            foreach(var name in new[]{"Pickup","SavedReceipt","Treatment"})
            {
                var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[api!.GetNestedType(name,BindingFlags.NonPublic)])!;
                proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
            }
            var med=stock;
            void Pickup(int distance)
            {
                if (actor.carryTracker.CarriedThing == med) actor.carryTracker.innerContainer.Remove(med);
                Set(med,"mapIndexOrState",(sbyte)0); Set(med,"positionInt",cell+IntVec3.East*distance);
                assigned.targetB=med;
                var toil=new Toil{actor=actor,initAction=()=>{
                    Set(med,"mapIndexOrState",(sbyte)-1);
                    Check.Equal(true,actor.carryTracker.innerContainer.TryAdd(med),"medicine enters native carry container");
                    assigned.targetB=actor.carryTracker.CarriedThing;
                }};
                api!.GetNestedType("Pickup",BindingFlags.NonPublic)!.GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[toil,TargetIndex.B,patient]);
                toil.initAction();
            }
            void Treat()
            {
                wound.Treated=false;
                TendUtility.DoTend(actor,patient,med);
                Check.Equal(true,wound.Treated,"native treatment reaches patient's hediff");
            }
            Enabled=false;Pickup(9);Enabled=true;Treat();
            Check.Equal(0,Count(),"disabled doctor work at pickup gives no receipt");
            Set(bed,"medicalInt",false);Pickup(9);Set(bed,"medicalInt",true);Treat();
            Check.Equal(0,Count(),"ordinary bed at pickup gives no receipt");
            Pickup(2);Treat();Check.Equal(0,Count(),"bedside stock excluded");
            if (source!=null)
            {
                Pickup(9);
                var possession=actor.health.hediffSet.GetFirstHediffOfDef(TorannMagic.TorannMagicDefOf.TM_SpiritPossessionHD);
                actor.health.hediffSet.hediffs.Remove(possession);
                try { Treat(); Check.Equal(0,Count(),"former host completing treatment cannot advance the soul"); }
                finally { actor.health.hediffSet.hediffs.Add(possession); }
            }
            Pickup(9);
            var interrupted = new Job{def=assigned.def,targetA=patient,targetB=med};
            Assign(interrupted);Treat();Check.Equal(0,Count(),"interrupted delivery cannot transfer its claim to another job");
            Assign(assigned);
            var storage=api!.GetNestedType("SavedReceipt",BindingFlags.NonPublic)!.GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!;
            var file=Path.Combine(f.WorkingDirectory,"blade-infirmary.xml");
            try {Scribe.saver.InitSaving(file,"test");storage.Invoke(null,[assigned]);Scribe.saver.FinalizeSaving();} finally {Scribe.ForceStop();}
            var xml=System.Xml.Linq.XDocument.Load(file);
            var savedOwner=xml.Root!.Element("rwomBladeSupplyOwner");
            if (source!=null)
                Check.Equal(source.thingIDNumber,(int?)savedOwner??0,"serialized medical delivery belongs to the original");
            else
            {
                savedOwner?.Remove();
                xml.Save(file); // Legacy native delivery receipt had only bed and medicine.
            }
            assigned=new Job{def=interrupted.def,targetA=patient,targetB=med};Assign(assigned);
            try {Scribe.loader.InitLoading(file);storage.Invoke(null,[assigned]);Scribe.loader.FinalizeLoading();} finally {Scribe.ForceStop();}
            wound.Treated=true;int qty=med.stackCount;TendUtility.DoTend(actor,patient,med);
            Check.Equal(qty,med.stackCount,"no dose without treatment");Check.Equal(0,Count(),"unspent medicine gives no credit");
            Treat();Check.Equal(1,Count(),"fetched dose consumed by a master doctor");
            Check.Equal(qty-1,med.stackCount,"exactly one dose consumed");
            Pickup(9);Treat();Check.Equal(1,Count(),"additional same-day dose gives no second credit");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            covered.SetValue(path,before);next.SetValue(path,ready);map.info=info;map.cellIndices=ci;map.thingGrid=grid;
            actor.jobs=jobs;actor.records=oldRecords;actor.workSettings=works;actor.carryTracker=carry;Set(actor,"positionInt",pos);
            Set(Find.TickManager,"ticksGameInt",oldTick);ThingDefOf.MedicineIndustrial=industrial;Set(Current.Game,"worldInt",oldWorld);
        }
    }
}
