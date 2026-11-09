using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static class EtherServantWorkTests
{
    static Type ServantType => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.Pawn_EtherServant")!;
    static void Ensure(Pawn pawn) => Check.NotNull(ServantType.GetMethod("EnsureWorkSkills"), "native work skill initialization").Invoke(pawn,null);
    static Pawn Pawn()
    {
        var pawn=(Pawn)Activator.CreateInstance(ServantType)!;
        pawn.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawn.def.race=new RaceProperties {intelligence=Intelligence.ToolUser};
        FleshStatTests.Set(pawn.def.race,"fleshType",new FleshTypeDef {defName="EtherSpiritFlesh"});
        pawn.kindDef=new PawnKindDef();
        FleshStatTests.Set(pawn,"cachedDisabledWorkTypes",new List<WorkTypeDef>());
        FleshStatTests.Set(pawn,"cachedDisabledWorkTypesPermanent",new List<WorkTypeDef>());
        return pawn;
    }
    static void WithSkills(Action action)
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(SkillDefOf));
        var oldWork=DefDatabase<WorkTypeDef>.AllDefsListForReading.ToArray();
        var old=DefDatabase<SkillDef>.AllDefsListForReading.ToArray(); var mining=SkillDefOf.Mining; var construction=SkillDefOf.Construction;
        try
        {
            DefDatabase<SkillDef>.Clear();
            SkillDefOf.Mining=new SkillDef {defName="Mining",shortHash=31}; SkillDefOf.Construction=new SkillDef {defName="Construction",shortHash=32};
            DefDatabase<SkillDef>.Add(SkillDefOf.Mining); DefDatabase<SkillDef>.Add(SkillDefOf.Construction);
            DefDatabase<WorkTypeDef>.Clear();
            foreach (var name in new[] { "Construction", "Mining", "Hauling", "Cleaning" })
                DefDatabase<WorkTypeDef>.Add(new WorkTypeDef { defName=name, relevantSkills=new List<SkillDef>() });
            FleshStatTests.InGame(() => { FleshStatTests.Set(Current.Game, "questManager", new QuestManager()); action(); });
        }
        finally {DefDatabase<WorkTypeDef>.Clear(); foreach(var def in oldWork) DefDatabase<WorkTypeDef>.Add(def); DefDatabase<SkillDef>.Clear(); foreach(var def in old) DefDatabase<SkillDef>.Add(def); SkillDefOf.Mining=mining; SkillDefOf.Construction=construction;}
    }
    public sealed class WorkEnvelope : IExposable
    {
        public Pawn Worker = Pawn();
        public void ExposeData()
        {
            Scribe_Deep.Look(ref Worker.skills, "skills", Worker);
            ServantType.GetMethod("ExposeWorkData")!.Invoke(Worker, null);
        }
    }
    static void SaveWork(RuntimeFixture f) => WithSkills(() =>
    {
        var value=new WorkEnvelope(); Ensure(value.Worker);
        value.Worker.skills.GetSkill(SkillDefOf.Mining).Level=11;
        value.Worker.skills.GetSkill(SkillDefOf.Construction).xpSinceLastLevel=123;
        string path=Path.Combine(f.WorkingDirectory,"ether-work.xml");
        try {Scribe.saver.InitSaving(path,"regression"); Scribe_Deep.Look(ref value,"worker"); Scribe.saver.FinalizeSaving();} finally {Scribe.ForceStop();}
        WorkEnvelope loaded=null!;
        try {Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded,"worker"); Scribe.loader.FinalizeLoading();} finally {Scribe.ForceStop();}
        Ensure(loaded.Worker);
        Check.Equal(11,loaded.Worker.skills.GetSkill(SkillDefOf.Mining).Level,"native Scribe does not reset mining");
        Check.Equal(123f,loaded.Worker.skills.GetSkill(SkillDefOf.Construction).xpSinceLastLevel,"native Scribe does not reset xp");
    });    static int qualitySkill;
    static bool QualityBoundary(int relevantSkillLevel, ref QualityCategory __result)
    { qualitySkill=relevantSkillLevel; __result=QualityCategory.Normal; return false; }
    static void CheckQualitySkill(Pawn pawn)
    {
        var assembly=Assembly.Load("0Harmony"); var id="rwom.ether.workquality";
        var harmony=Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!,id)!;
        var prefix=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,typeof(EtherServantWorkTests).GetMethod(nameof(QualityBoundary),BindingFlags.NonPublic|BindingFlags.Static)!)!;
        try
        {
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(QualityUtility).GetMethod("GenerateQualityCreatedByPawn",[typeof(int),typeof(bool)])!,prefix,null,null,null]);
            QualityUtility.GenerateQualityCreatedByPawn(pawn,SkillDefOf.Construction,false);
            Check.Equal(5,qualitySkill,"native pawn quality generator passes actual construction5");
        }
        finally {harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
    }
    static void WorkGiverContracts() => WithSkills(() =>
    {
        var old=DefDatabase<WorkGiverDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<WorkGiverDef>.Clear();
            var types=new[] {typeof(WorkGiver_ConstructFinishFrames),typeof(WorkGiver_ConstructDeliverResourcesToFrames),typeof(WorkGiver_ConstructDeliverResourcesToBlueprints),typeof(WorkGiver_Miner),typeof(WorkGiver_HaulGeneral)};
            var names=new[] {"ConstructFinishFrames","ConstructDeliverResourcesToFrames","ConstructDeliverResourcesToBlueprints","Mine","HaulGeneral"};
            for(int i=0;i<types.Length;i++) DefDatabase<WorkGiverDef>.Add(new WorkGiverDef {defName=names[i],giverClass=types[i],workType=DefDatabase<WorkTypeDef>.GetNamed(i<3?"Construction":i==3?"Mining":"Hauling")});
            var method=typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.JobGiver_EtherHaul")!.GetMethod("WorkGivers")!;
            var workers=((IEnumerable<WorkGiver_Scanner>)method.Invoke(null,null)!).ToArray();
            Check.Equal(5,workers.Length,"construction preparation/finish mining haul only");
            for(int i=0;i<workers.Length;i++)
            {
                Check.Equal(types[i],workers[i].GetType(),"actual native worker priority");
                Check.Same(DefDatabase<WorkGiverDef>.GetNamed(names[i]),workers[i].def,"native worker owns initialized work def");
            }
        }
        finally {DefDatabase<WorkGiverDef>.Clear(); foreach(var def in old) DefDatabase<WorkGiverDef>.Add(def);}
    });    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EtherWork.NativeWorkerPriorityAndDefinitions",_=>WorkGiverContracts());
        yield return new("EtherWork.NativeSkillScribe",SaveWork);
        yield return new("EtherWork.NativeSkillsAndNoReinitialization",_=>WithSkills(()=>
        {
            var pawn=Pawn(); Ensure(pawn);
            Check.Equal(10,pawn.skills.GetSkill(SkillDefOf.Mining).Level,"real mining level10");
            Check.Equal(5,pawn.skills.GetSkill(SkillDefOf.Construction).Level,"real construction level5");
            Check.Equal(1f,new SkillNeed_BaseBonus {skill=SkillDefOf.Mining}.ValueFor(pawn),"native stat skill factor reads mining10");
            Check.Equal(.75f,new SkillNeed_BaseBonus {skill=SkillDefOf.Construction}.ValueFor(pawn),"native stat skill factor reads construction5");
            CheckQualitySkill(pawn);
            Check.Equal(1,pawn.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("Construction")),"native construction active first");
            Check.Equal(2,pawn.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("Mining")),"native mining active second");
            Check.Equal(3,pawn.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("Hauling")),"native hauling retained");
            Check.Equal(0,pawn.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cleaning")),"unrequested work disabled");
            pawn.skills.GetSkill(SkillDefOf.Mining).Level=11;
            pawn.skills.GetSkill(SkillDefOf.Construction).xpSinceLastLevel=123;
            Ensure(pawn);
            Check.Equal(11,pawn.skills.GetSkill(SkillDefOf.Mining).Level,"repeat spawn preserves gained level");
            Check.Equal(123f,pawn.skills.GetSkill(SkillDefOf.Construction).xpSinceLastLevel,"repeat spawn preserves xp");
            Check.Equal(false,pawn.RaceProps.Humanlike,"worker remains nonhuman");
        }));
    }
}





