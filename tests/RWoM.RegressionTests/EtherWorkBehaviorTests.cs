using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;
namespace RWoM.RegressionTests;
internal static class EtherWorkBehaviorTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EtherWork.NativeDesignatedMiningJob", f => TeleportSpiritTests.EtherNativeWork(f, false));
        yield return new("EtherWork.NativeFrameJobAndSkillExclusion", f => TeleportSpiritTests.EtherNativeWork(f, true));
    }
}
internal static partial class TeleportSpiritTests
{
    static bool EtherWorkReach(ref bool __result) { __result = true; return false; }
    public static void EtherNativeWork(RuntimeFixture f, bool frame)
    {
        var withSkills = typeof(EtherServantWorkTests).GetMethod("WithSkills", Flags)!;
        withSkills.Invoke(null, [new Action(() => CoreReturnWorld(f, w =>
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(ThingCategoryDefOf)); RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(DesignationDefOf)); RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(HistoryEventDefOf));
            var oldArtistic = SkillDefOf.Artistic; SkillDefOf.Artistic = new SkillDef {defName="Artistic"}; DefDatabase<SkillDef>.Add(SkillDefOf.Artistic);
            var oldMine = DesignationDefOf.Mine; var oldVein = DesignationDefOf.MineVein;
            var mine = new DesignationDef { defName="EtherMine", targetType=TargetType.Cell }; var vein = new DesignationDef { defName="EtherMineVein",targetType=TargetType.Cell };
            using var mineScope = new DefinitionScope<DesignationDef>(mine); using var veinScope = new DefinitionScope<DesignationDef>(vein);
            var oldWorkers=DefDatabase<WorkGiverDef>.AllDefsListForReading.ToArray();
            try
            {
                DesignationDefOf.Mine=mine; DesignationDefOf.MineVein=vein;
                w.Map.lordManager = Blank<Verse.AI.Group.LordManager>(); w.Map.lordManager.lords = new List<Verse.AI.Group.Lord>(); w.Map.designationManager = new DesignationManager(w.Map); w.Map.events=new MapEvents(w.Map); w.Map.reservationManager = new ReservationManager(w.Map); w.Map.physicalInteractionReservationManager=new PhysicalInteractionReservationManager(); w.Map.listerThings = new ListerThings(ListerThingsUse.Global);
                _=CoreReturnCore(w); Set(Current.Game,"questManager",new QuestManager()); var storyteller=Blank<Storyteller>(); storyteller.difficulty=new Difficulty(); Set(Current.Game,"storyteller",storyteller);
                var pawn=(Pawn)typeof(EtherServantWorkTests).GetMethod("Pawn",Flags)!.Invoke(null,null)!;
                pawn.def.size=new IntVec2(1,1); pawn.def.category=ThingCategory.Pawn; pawn.health=new Pawn_HealthTracker(pawn); pawn.mindState=Blank<Pawn_MindState>(); pawn.mindState.mentalStateHandler=new MentalStateHandler(pawn);
                pawn.jobs=new Pawn_JobTracker(pawn); pawn.playerSettings=Blank<Pawn_PlayerSettings>(); Set(pawn.playerSettings,"pawn",pawn); Set(pawn.playerSettings,"allowedAreas",new Dictionary<Map,Area>()); pawn.drafter=new Pawn_DraftController(pawn);
                Set(pawn,"factionInt",w.Player); Set(pawn,"positionInt",new IntVec3(10,0,9)); Set(pawn,"mapIndexOrState",(sbyte)0);
                var worker = new WorkGiverDef { defName=frame?"ConstructFinishFrames":"Mine", giverClass=frame?typeof(WorkGiver_ConstructFinishFrames):typeof(WorkGiver_Miner), workType=DefDatabase<WorkTypeDef>.GetNamed(frame?"Construction":"Mining") };
                DefDatabase<WorkGiverDef>.Clear(); DefDatabase<WorkGiverDef>.Add(worker);
                var d=Blank<ThingDef>(); d.defName=frame?"EtherFrame":"EtherRock";d.size=new IntVec2(1,1);d.category=ThingCategory.Building;d.thingClass=frame?typeof(Frame):typeof(Mineable);d.building=new BuildingProperties();d.comps=[];d.mineable=!frame;
                var build=Blank<ThingDef>();build.defName="EtherBuiltWall";build.size=new IntVec2(1,1);build.costList=[];build.building=new BuildingProperties();build.comps=[];
                if(frame) {Set(d,"isFrameInt",true);d.entityDefToBuild=build;d.constructionSkillPrerequisite=5;}
                Thing target=frame?new Frame():new Mineable();target.def=d;Set(target,"factionInt",w.Player);Set(target,"positionInt",new IntVec3(10,0,10));Set(target,"mapIndexOrState",(sbyte)0);w.Map.thingGrid.Register(target); w.Map.listerThings.Add(target);
                using var patches=new Patches();
                // Region graph traversal is independent of job eligibility. Native
                // reservation, designation, skill and worker methods remain unpatched.
                patches.Prefix(typeof(ReachabilityUtility).GetMethods().Single(m=>m.Name=="CanReach"&&m.GetParameters().Length==7),nameof(EtherWorkReach));
                patches.Prefix(typeof(Thing).GetMethod("DirtyMapMesh",Flags)!,nameof(SkipGraphics));
                patches.Prefix(typeof(GenConstruct).GetMethod("CanTouchTargetFromValidCell")!,nameof(EtherWorkReach));
                foreach(var puff in typeof(FleckMaker).GetMethods().Where(m=>m.Name=="ThrowMetaPuffs")) patches.Prefix(puff,nameof(SkipGraphics));
                if(!frame) w.Map.designationManager.AddDesignation(new Designation(target.Position,mine));
                var giver=Activator.CreateInstance(typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.JobGiver_EtherHaul")!)!;
                var give=giver.GetType().GetMethod("TryGiveJob",Flags)!;
                pawn.GetType().GetMethod("EnsureWorkSkills")!.Invoke(pawn,null);
                var job=(Job?)give.Invoke(giver,[pawn]);
                Check.NotNull(job,"actual production giver returns native work job");Check.Same(target,job!.targetA.Thing,"actual native job targets nominated thing");Check.Same(frame?JobDefOf.FinishFrame:JobDefOf.Mine,job.def,"actual native job type");Check.Same(worker,job.workGiverDef,"assigned workgiver identity");
                Check.Equal(true,w.Map.reservationManager.Reserve(w.Caster,new Job {def=job.def},target),"native other worker reserves target");
                Check.Equal<Job?>(null,(Job?)give.Invoke(giver,[pawn]),"native reservation excludes already claimed work"); w.Map.reservationManager.ReleaseAllForTarget(target);
                if(frame) {d.constructionSkillPrerequisite=6;Check.Equal<Job?>(null,(Job?)give.Invoke(giver,[pawn]),"construction5 rejects required6");}
                else {w.Map.designationManager.RemoveDesignation(w.Map.designationManager.DesignationAt(target.Position,mine));Check.Equal<Job?>(null,(Job?)give.Invoke(giver,[pawn]),"without mining designation no mining job");}
            }
            finally {SkillDefOf.Artistic=oldArtistic;DesignationDefOf.Mine=oldMine;DesignationDefOf.MineVein=oldVein;DefDatabase<WorkGiverDef>.Clear();foreach(var def in oldWorkers)DefDatabase<WorkGiverDef>.Add(def);}
        }))]);
    }
}















