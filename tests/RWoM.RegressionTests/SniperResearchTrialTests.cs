using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperResearchTrialTests
{
    public static bool Skip() => false;
    private static float researchSpeed;
    public static bool Stat(StatDef stat, ref float __result)
    {
        __result = stat == StatDefOf.ResearchSpeed ? researchSpeed : 1f;
        return false;
    }

    internal static void Verify(Pawn pawn)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var path = ClassIdentity.Component(pawn).State.Find("rwom.sniper");
        var field = path.GetType().GetField("ResearchWorkTicks", flags)!;
        int Count() => (int)field.GetValue(path)!;
        int originalCount = Count();
        var previousManager = Current.Game.researchManager;
        var previousStory = Current.Game.storyteller;
        var previousSkills = pawn.skills; var previousRecords = pawn.records;
        foreach (var defs in new[] { typeof(SkillDefOf), typeof(EffecterDefOf), typeof(RecordDefOf) })
            RuntimeLog.EnsureHeadlessDefOfInitialized(defs);
        var oldSkill = SkillDefOf.Intellectual; var oldSpeed = StatDefOf.ResearchSpeed;
        var lib = Assembly.Load("0Harmony");
        var ht = lib.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.sniper.research." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, id)!;
        void Prefix(MethodBase method,string name)
        {
            var h = Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!, typeof(SniperResearchTrialTests).GetMethod(name)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [method,h,null,null,null]);
        }
        try
        {
            SkillDefOf.Intellectual ??= new SkillDef { defName="RegressionIntellectual" };
            StatDefOf.ResearchSpeed ??= (StatDef)RuntimeHelpers.GetUninitializedObject(typeof(StatDef));
            var manager = new ResearchManager();
            Current.Game.researchManager = manager;
            Current.Game.storyteller = (Storyteller)RuntimeHelpers.GetUninitializedObject(typeof(Storyteller));
            Current.Game.storyteller.difficulty = new Difficulty { researchSpeedFactor = 1f };
            var project = new ResearchProjectDef {defName="SniperFieldResearch",baseCost=100000f};
            Set(manager,"currentProj",project);
            pawn.skills ??= (Pawn_SkillTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_SkillTracker));
            pawn.records ??= (Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker));

            var patch = typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.SniperResearchTrial",true)!;
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
            Prefix(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(Stat));
            Prefix(typeof(PawnUtility).GetMethod("GainComfortFromCellIfPossible")!,nameof(Skip));
            Prefix(typeof(Pawn_SkillTracker).GetMethod("Learn")!,nameof(Skip));
            Prefix(typeof(Pawn_RecordsTracker).GetMethod("AddTo")!,nameof(Skip));

            var bench = new Building_ResearchBench { def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)) };
            Set(bench,"mapIndexOrState",(sbyte)0);
            var driver = new JobDriver_Research { pawn=pawn, job=new Job {targetA=bench} };
            var toils = ((IEnumerable<Toil>)typeof(JobDriver_Research).GetMethod("MakeNewToils",flags)!.Invoke(driver,null)!).ToArray();
            var work = toils.Single(t=>t.activeSkill?.Invoke()==SkillDefOf.Intellectual && t.tickIntervalAction!=null);
            work.actor = pawn;
            researchSpeed = 2f;
            work.tickIntervalAction(17);
            Check.Equal(originalCount+17,Count(),"research toil awards elapsed work, not research points");
            Check.Equal(true,manager.GetProgress(project)>0f,"native research progressed");
            researchSpeed=0f;
            work.tickIntervalAction(31);
            Check.Equal(originalCount+17,Count(),"unproductive research interval gives no mastery");
            manager.ResearchPerformed(500,pawn);
            Check.Equal(originalCount+17,Count(),"research awarded outside a bench job gives no mastery");
            Set(bench,"mapIndexOrState",(sbyte)-1);
            researchSpeed=1f;
            work.tickIntervalAction(15);
            Check.Equal(originalCount+17,Count(),"despawned bench gives no mastery");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            field.SetValue(path,originalCount);
            Current.Game.researchManager=previousManager;Current.Game.storyteller=previousStory;
            pawn.records=previousRecords;pawn.skills=previousSkills;
            SkillDefOf.Intellectual=oldSkill; StatDefOf.ResearchSpeed=oldSpeed;
        }
    }
}
