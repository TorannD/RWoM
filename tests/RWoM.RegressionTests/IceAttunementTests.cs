using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class IceAttunementTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IceAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f));
        yield return new("LightningAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, lightning: true));
        yield return new("EmpathAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, empath: true));
        yield return new("GolemancerAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, golemancer: true));
        yield return new("ShamanAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, shaman: true));
        yield return new("BrightmageAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, brightmage: true));
        yield return new("ChronomancerAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, chronomancer: true));
        yield return new("EnchanterAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, enchanter: true));
        yield return new("BloodMageAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, bloodmage: true));
        yield return new("TechnomancerAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, technomancer: true));
        yield return new("GeomancerAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, geomancer: true));
        yield return new("WarlockAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, warlock: true));
        yield return new("SuccubusAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, succubus: true));
        yield return new("DruidAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, druid: true));
        yield return new("BardAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, bard: true));
        yield return new("PaladinAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, paladin: true));
        yield return new("SummonerAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, summoner: true));
        yield return new("ArcanistAttunement.ActualWorkSavePaymentAndBooksRemainRequired", f => Work(f, arcanist: true));
    }
    public sealed class FaultSilver : Thing
    {
        public static int Fault;
        public static Action? Callback;
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Callback?.Invoke();
            if (Fault == 1) throw new InvalidOperationException("before silver payment");
            base.Destroy(mode);
            if (Fault == 2) throw new InvalidOperationException("after silver payment");
        }
    }
    static JobCondition? ended;
    public static bool SkipFacing() => false;
    public static bool EndBoundary(JobDriver __instance, JobCondition condition)
    {
        if (__instance is not JobDriver_IceAttunement) return true;
        ended = condition; return false;
    }
    static void Tick(JobDriver_IceAttunement driver, int tick)
    {
        Set(Find.TickManager, "ticksGameInt", tick);
        typeof(JobDriver_IceAttunement).GetMethod("AdvanceRitual", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(driver, null);
    }
    static JobDriver_IceAttunement RoundTrip(RuntimeFixture f, JobDriver_IceAttunement source, string name, bool legacyIce = false)
    {
        var file = Path.Combine(f.WorkingDirectory, name + ".xml"); var saved = source;
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref saved, "driver"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        if (legacyIce)
        {
            var xml = System.Xml.Linq.XDocument.Load(file);
            foreach (var element in xml.Descendants("iceAttunementClassId").ToArray()) element.Remove();
            xml.Save(file);
        }
        JobDriver_IceAttunement loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "driver"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        loaded.pawn = source.pawn; loaded.job = source.job; return loaded;
    }
    static void Work(RuntimeFixture f, bool lightning = false, bool arcanist = false, bool paladin = false, bool summoner = false, bool druid = false, bool bard = false, bool warlock = false, bool succubus = false, bool geomancer = false, bool technomancer = false, bool bloodmage = false, bool enchanter = false, bool chronomancer = false, bool brightmage = false, bool shaman = false, bool golemancer = false, bool empath = false)
    {
        var classId = empath ? "rwom.empath" : golemancer ? "rwom.golemancer" : shaman ? "rwom.shaman" : brightmage ? "rwom.brightmage" : chronomancer ? "rwom.chronomancer" : enchanter ? "rwom.enchanter" : bloodmage ? "rwom.bloodmage" : technomancer ? "rwom.technomancer" : geomancer ? "rwom.geomancer" : warlock ? "rwom.warlock" : succubus ? "rwom.succubus" : druid ? "rwom.druid" : bard ? "rwom.bard" : paladin ? "rwom.paladin" : summoner ? "rwom.summoner" : arcanist ? "rwom.arcanist" : lightning ? "rwom.lightning" : "rwom.ice";
        LoadBaseDefinitions(f);
        using var empathDefinitions = empath ? EmpathProgressionTests.LoadDefinitions(f) : null;
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var previous = Current.Game;
        var designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        var classes = TM_ClassUtility.CustomClasses;
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.ice-attunement." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            ended = null;
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(IceAttunementTests).GetMethod(nameof(EndBoundary))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(JobDriver).GetMethod("EndJobWith")!, prefix, null, null, null]);
            var facing = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(IceAttunementTests).GetMethod(nameof(SkipFacing))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_RotationTracker).GetMethod("FaceTarget")!, facing, null, null, null]);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "IceAttunementDesignation" });
            map.reservationManager = new ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager(); map.events = new MapEvents(map);
            map.lordManager = new Verse.AI.Group.LordManager(map);
            map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map);
            Set(Current.Game, "maps", new List<Map> { map });
            TM_ClassUtility.CustomClasses = [];
            var p = PawnWithComps(true, false);
            p.pawn.mindState = new Pawn_MindState(p.pawn); p.pawn.rotationTracker = new Pawn_RotationTracker(p.pawn);
            p.pawn.kindDef = new PawnKindDef { defName = "IceAttunementColonist" };
            p.pawn.jobs = new Pawn_JobTracker(p.pawn); p.pawn.carryTracker = new Pawn_CarryTracker(p.pawn);
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager();
            world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction);
            Set(Current.Game, "worldInt", world);
            Set(p.pawn, "factionInt", faction); Set(p.pawn, "mapIndexOrState", (sbyte)0);
            var progression = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(progression);
            progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation(classId, DisciplineDomain.Magic);
            var path = progression.State.Find(classId);
            var circleDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            circleDef.defName = "IceAttunementCircle"; circleDef.thingClass = typeof(Building_TMMagicCircle);
            circleDef.hasInteractionCell = true; circleDef.interactionCellOffset = new IntVec3(0, 0, -1); circleDef.size = new IntVec2(3, 3);
            var circle = new Thing { def = circleDef }; Set(circle, "factionInt", faction);
            Set(circle, "mapIndexOrState", (sbyte)0); Set(circle, "positionInt", new IntVec3(10, 0, 10));
            Set(p.pawn, "positionInt", circle.InteractionCell);
            var silverDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            silverDef.defName = "Silver"; silverDef.thingClass = typeof(FaultSilver); silverDef.category = ThingCategory.Item;
            silverDef.destroyable = true; silverDef.stackLimit = 500; silverDef.smallVolume = true;
            var silver = new FaultSilver { def = silverDef, stackCount = 250 };
            Check.Equal(false, IceAttunement.CanBegin(p.pawn, circle, silver), "unowned unspawned silver cannot pay");
            Check.Equal(true, p.pawn.carryTracker.innerContainer.TryAdd(silver), "native carry owns offering");
            silver.stackCount = 99;
            Check.Equal(false, IceAttunement.CanBegin(p.pawn, circle, silver), "full hundred required");
            silver.stackCount = 250;
            Set(circle, "factionInt", new Faction());
            Check.Equal(false, IceAttunement.CanBegin(p.pawn, circle, silver), "foreign circle rejected");
            Set(circle, "factionInt", faction);
            silverDef.defName = "Steel";
            Check.Equal(false, IceAttunement.CanBegin(p.pawn, circle, silver), "other resources rejected");
            silverDef.defName = "Silver";
            FaultSilver.Fault = 1;
            Check.Equal(false, IceAttunement.Complete(p.pawn, circle, silver), "failed destruction grants nothing");
            Check.Equal(0, path.CompletedTrials.Count, "failed payment leaves trial incomplete");
            Check.Equal(150, silver.stackCount, "partial split leaves original remainder");
            var recovered = p.pawn.inventory.innerContainer.Single();
            Check.Equal(100, recovered.stackCount, "actual unpaid split remains save-owned in inventory");
            FaultSilver.Fault = 0;
            p.pawn.inventory.innerContainer.Remove(recovered);
            silver.TryAbsorbStack(recovered, true);
            Check.Equal(250, silver.stackCount, "retry fixture recombines retained silver without creating currency");
            var nativePowers = empath ? EmpathOwners.Resolve(p.magic.MagicData) : golemancer ? p.magic.MagicData.MagicPowersGolemancer : shaman ? p.magic.MagicData.MagicPowersShaman : brightmage ? p.magic.MagicData.MagicPowersBrightmage : chronomancer ? p.magic.MagicData.MagicPowersC : enchanter ? p.magic.MagicData.MagicPowersE : bloodmage ? p.magic.MagicData.MagicPowersBM : technomancer ? p.magic.MagicData.MagicPowersT : geomancer ? p.magic.MagicData.MagicPowersG : warlock ? p.magic.MagicData.MagicPowersWD : succubus ? p.magic.MagicData.MagicPowersSD : druid ? p.magic.MagicData.MagicPowersD : bard ? p.magic.MagicData.MagicPowersB : paladin ? p.magic.MagicData.MagicPowersP : summoner ? p.magic.MagicData.MagicPowersS : arcanist ? p.magic.MagicData.MagicPowersA : lightning ? p.magic.MagicData.MagicPowersSB : p.magic.MagicData.MagicPowersHoF;
            var powers = nativePowers.Select(power => power.learned).ToArray();
            var driver = new JobDriver_IceAttunement { pawn = p.pawn, job = new Job(new JobDef { defName = "IceAttunementTest" }, circle, silver) };
            Check.Equal(true, driver.TryMakePreToilReservations(false), "native circle reservation succeeds");
            Check.Equal(100, driver.job.count, "native hauling must pick up hundred rather than one");
            Check.Equal(250, silver.stackCount, "reservation never pays");
            Tick(driver, 100); Tick(driver, 100);
            Check.Equal(1, driver.WorkTicks, "same tick cannot duplicate work");
            Tick(driver, 10000);
            Check.Equal(2, driver.WorkTicks, "offline time adds only actual tick");
            driver = RoundTrip(f, driver, "ice-attunement-pending", legacyIce: !lightning && !arcanist && !paladin && !summoner && !druid && !bard && !warlock && !succubus && !geomancer && !technomancer && !bloodmage && !enchanter && !chronomancer && !brightmage && !shaman && !golemancer && !empath);
            Check.Equal(2, driver.WorkTicks, "Scribe preserves work");
            Tick(driver, 10000); Check.Equal(2, driver.WorkTicks, "reload cannot duplicate tick");
            // A resumed job must keep both its original discipline and stage binding.
            foreach (var otherClass in new[] { "rwom.ice", "rwom.lightning", "rwom.arcanist", "rwom.paladin", "rwom.summoner", "rwom.druid", "rwom.bard", "rwom.warlock", "rwom.succubus", "rwom.geomancer", "rwom.technomancer", "rwom.bloodmage", "rwom.enchanter", "rwom.chronomancer", "rwom.brightmage", "rwom.shaman", "rwom.golemancer", "rwom.empath" }.Where(other => other != classId))
            {
                var originalState = progression.State;
                Set(progression, "state", new ClassProgressionState(Vocation.Mage));
                progression.State.TryAddFoundation(otherClass, DisciplineDomain.Magic);
                Check.Equal(false, driver.TryMakePreToilReservations(false), "saved job cannot bind to another class");
                Tick(driver, 10001);
                Check.Equal(JobCondition.Incompletable, ended!.Value, "class switch interrupts saved work");
                Check.Equal(250, silver.stackCount, "class switch cannot pay");
                Check.Equal(0, progression.State.Find(otherClass).CompletedTrials.Count, "class switch grants no other trial");
                Set(progression, "state", originalState);
            }
            Set(p.pawn, "positionInt", new IntVec3(30, 0, 30)); Tick(driver, 10001);
            Check.Equal(JobCondition.Incompletable, ended!.Value, "leaving circle interrupts");
            Check.Equal(250, silver.stackCount, "interruption retains silver");
            Check.Equal(0, path.CompletedTrials.Count, "interruption grants no trial");
            Set(p.pawn, "positionInt", circle.InteractionCell);
            Set(driver, "workTicks", IceAttunement.RitualTicks - 1);
            bool? recursive = null;
            FaultSilver.Callback = () => recursive = IceAttunement.Complete(p.pawn, circle, silver);
            FaultSilver.Fault = 2;
            Tick(driver, 10002);
            FaultSilver.Callback = null; FaultSilver.Fault = 0;
            Check.Equal(false, recursive!.Value, "nested payment callback cannot reenter transaction");
            Check.Equal(true, driver.Completed, "final work tick commits");
            Check.Equal(150, silver.stackCount, "exactly hundred silver paid");
            Check.Equal(true, path.CompletedTrials.Contains(classId + ".foundation"), "foundation trial completed");
            Check.Equal(InitiationStage.Foundation, path.Stage, "Adept still requires book");
            Check.Equal(true, powers.SequenceEqual(nativePowers.Select(power => power.learned)), "no spells taught");
            driver = RoundTrip(f, driver, "ice-attunement-completed");
            Tick(driver, 10003);
            Check.Equal(true, driver.Completed, "Scribe completion latch preserved");
            Check.Equal(150, silver.stackCount, "repeated callback cannot repay");
            Check.Equal(false, IceAttunement.Complete(p.pawn, circle, silver), "repeated direct completion rejected");
            Check.Equal(150, silver.stackCount, "repeated direct completion cannot repay");
            progression.State.TryAdvanceAfterTrial(classId, InitiationStage.Foundation, classId + ".foundation");
            Check.Equal(classId + ".mastery", IceAttunement.CandidateTrialId(p.pawn), "Adept can choose peaceful mastery trial");
            var stale = new JobDriver_IceAttunement { pawn = p.pawn, job = driver.job };
            Set(stale, "classId", classId); Set(stale, "trialId", classId + ".foundation"); Set(stale, "workTicks", IceAttunement.RitualTicks - 1);
            Tick(stale, 10004);
            Check.Equal(false, path.CompletedTrials.Contains(classId + ".mastery"), "old foundation work cannot pay mastery");
            Check.Equal(150, silver.stackCount, "stale stage work cannot pay");
            Check.Equal(true, IceAttunement.Complete(p.pawn, circle, silver), "Adept trial can complete with separate payment");
            Check.Equal(50, silver.stackCount, "second trial costs full hundred");
            Check.Equal(true, path.CompletedTrials.Contains(classId + ".mastery"), "mastery trial recorded");
            Check.Equal(InitiationStage.Adept, path.Stage, "Master still requires book");
            progression.State.TryAdvanceAfterTrial(classId, InitiationStage.Adept, classId + ".mastery");
            Check.Equal<string?>(null, IceAttunement.CandidateTrialId(p.pawn), "Master has no additional attunement trial");
        }
        finally
        {
            FaultSilver.Callback = null; FaultSilver.Fault = 0;
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); Current.Game = previous; TM_ClassUtility.CustomClasses = classes;
            DefDatabase<DesignationDef>.Clear(); foreach (var def in designations) DefDatabase<DesignationDef>.Add(def);
        }
    }
}


