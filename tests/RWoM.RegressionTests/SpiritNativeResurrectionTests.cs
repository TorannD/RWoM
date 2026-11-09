using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritNativeResurrectionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritNativeResurrection.ActualCorpseSamePawnAndProgression", r => Resurrect(r, false, false));
        yield return new("SpiritNativeResurrection.CleanupThenNativeRevivalKeepsAttachedClassMarker", r => Resurrect(r, true, false));
        yield return new("SpiritNativeResurrection.PlayerCleanupRevivalRunsWorkAndPopulationCallbacks", r => Resurrect(r, true, true));
        yield return new("SpiritNativeResurrection.RetainedWorkSnapshotRestoresAfterNativeReset", r => Resurrect(r, true, true, true));
        yield return new("SpiritNativeResurrection.RetainedHiddenNumericWorkPrioritySurvives", r => Resurrect(r, true, true, true, true));
    }

    static T Blank<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    public sealed class IdentityHediff : Hediff
    {
        public int resurrectionCallbacks;
        public override void Notify_Resurrected() { resurrectionCallbacks++; base.Notify_Resurrected(); }
    }
    sealed class HealthDefinitions : IDisposable
    {
        readonly HediffDef[] prior = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        public HealthDefinitions()
        {
            DefDatabase<HediffDef>.Clear();
            DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_ClassDiscipline", hediffClass = typeof(Hediff_ClassDiscipline) });
            DefDatabase<HediffDef>.Add(new HediffDef { defName = "CochlearImplant", hediffClass = typeof(Hediff_AddedPart) });
        }
        public void Dispose()
        {
            DefDatabase<HediffDef>.Clear(); foreach (var def in prior) DefDatabase<HediffDef>.Add(def);
        }
    }
    sealed class CapacityDefinitions : IDisposable
    {
        readonly PawnCapacityDef[] prior = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        readonly Dictionary<FieldInfo, object?> fields = new();
        public CapacityDefinitions()
        {
            foreach (var type in new[] { typeof(PawnCapacityDefOf), typeof(BodyPartTagDefOf), typeof(PrisonerInteractionModeDefOf), typeof(TimeAssignmentDefOf) })
            {
                RuntimeLog.EnsureHeadlessDefOfInitialized(type);
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    fields[field] = field.GetValue(null);
                    if (field.FieldType == typeof(BodyPartTagDef)) field.SetValue(null, new BodyPartTagDef { defName = "SpiritTest" + field.Name });
                    if (field.FieldType == typeof(PrisonerInteractionModeDef)) field.SetValue(null, new PrisonerInteractionModeDef { defName = "SpiritTest" + field.Name });
                    if (field.FieldType == typeof(TimeAssignmentDef)) field.SetValue(null, new TimeAssignmentDef { defName = "SpiritTest" + field.Name });
                }
            }
            DefDatabase<PawnCapacityDef>.Clear();
            foreach (var name in new[] { "Consciousness", "Moving", "BloodPumping", "Breathing", "BloodFiltration", "Manipulation" })
            {
                var worker = typeof(Pawn).Assembly.GetType("RimWorld.PawnCapacityWorker_" + name)!;
                var def = new PawnCapacityDef { defName = name, workerClass = worker };
                DefDatabase<PawnCapacityDef>.Add(def);
                typeof(PawnCapacityDefOf).GetField(name)!.SetValue(null, def);
            }
        }
        public void Dispose()
        {
            foreach (var pair in fields) pair.Key.SetValue(null, pair.Value);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var def in prior) DefDatabase<PawnCapacityDef>.Add(def);
        }
    }
    internal static void WithPreparedCorpse(RuntimeFixture r, Action<Pawn, Corpse> action)
        => Resurrect(r, true, false, prepared: action);
    static void Resurrect(RuntimeFixture r, bool cleanup, bool player, bool restoreWork = false, bool manualPrioritiesOff = false, Action<Pawn, Corpse>? prepared = null)
    {
        using var fixture = new SpiritRebirthStateTests.Fixture(r);
        var rootField = typeof(Current).GetField("rootInt", BindingFlags.Static | BindingFlags.NonPublic)!;
        var priorRoot = rootField.GetValue(null);
        var priorPainShock = StatDefOf.PainShockThreshold;
        StatDefOf.PainShockThreshold = new StatDef { defName = "SpiritResurrectionPainShock", defaultBaseValue = 0.8f, minValue = 0f, maxValue = 1f, workerClass = typeof(StatWorker) };
        using var capacities = new CapacityDefinitions();
        using var healthDefinitions = cleanup ? new HealthDefinitions() : null;
        var workTypesBefore = DefDatabase<WorkTypeDef>.AllDefsListForReading.ToArray();
        var recordsBefore = DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        if (recordsBefore.Length == 0) DefDatabase<RecordDef>.Add(new RecordDef { defName = "SpiritResurrectionRecord" });
        try
        {
            var root = Blank<Root_Play>(); var ui = Blank<UIRoot_Play>(); var mapUI = Blank<MapInterface>();
            var selector = Blank<Selector>(); Set(selector, "selected", new List<object>());
            root.uiRoot = ui; ui.mapUI = mapUI; mapUI.selector = selector; rootField.SetValue(null, root);
            var world = Blank<RimWorld.Planet.World>(); world.factionManager = new FactionManager();
            Set(Current.Game, "worldInt", world); Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Current.Game.Scenario = new Scenario();
            Set(Current.Game, "maps", new List<Map>());

            var pawn = fixture.Original;
            pawn.Name = new NameTriple("Mira", "Ash", "Voss");
            var part = new BodyPartRecord { def = new BodyPartDef { defName = "SpiritResurrectionCore", hitPoints = 100 }, coverage = 1f };
            part.def.tags = new List<BodyPartTagDef> { BodyPartTagDefOf.MovingLimbCore, BodyPartTagDefOf.ConsciousnessSource };
            pawn.def.race.body = new BodyDef { defName = "SpiritResurrectionBody", corePart = part };
                        var missingLimb = new BodyPartRecord { def = new BodyPartDef { defName = "SpiritMissingLimb", hitPoints = 30 }, coverage = .2f };
            var implantLimb = new BodyPartRecord { def = new BodyPartDef { defName = "SpiritImplantLimb", hitPoints = 30 }, coverage = .2f };
            part.parts.AddRange(new[] { missingLimb, implantLimb });
            pawn.def.race.body.ResolveReferences();
            pawn.def.race.lifeStageAges = new List<LifeStageAge> { new LifeStageAge { def = new LifeStageDef { defName = "SpiritResurrectionAdult", developmentalStage = DevelopmentalStage.Adult }, minAge = 0 } };
            pawn.def.race.baseHealthScale = 1f;
            Set(pawn.def.race, "fleshType", new FleshTypeDef { defName = "SpiritResurrectionFlesh", isOrganic = true });
            pawn.kindDef = new PawnKindDef { defName = "SpiritResurrectionKind", race = pawn.def };
            pawn.skills = new Pawn_SkillTracker(pawn);
            var skill = new SkillRecord(pawn, new SkillDef { defName = "SpiritResurrectionSkill" }) { levelInt = 13, passion = Passion.Major, xpSinceLastLevel = 123 };
            pawn.skills.skills = new List<SkillRecord> { skill };
            pawn.records = new Pawn_RecordsTracker(pawn); pawn.relations = new Pawn_RelationsTracker(pawn);
                        pawn.ageTracker = new Pawn_AgeTracker(pawn);
            pawn.timetable = new Pawn_TimetableTracker(pawn);
            pawn.timetable.SetAssignment(7, TimeAssignmentDefOf.Sleep);
            pawn.timetable.SetAssignment(8, TimeAssignmentDefOf.Work);
            var timetable = pawn.timetable;
            fixture.Other.relations = new Pawn_RelationsTracker(fixture.Other);
            var relationDef = new PawnRelationDef { defName = "SpiritRetainedRelation", reflexive = true };
            pawn.relations.AddDirectRelation(relationDef, fixture.Other);
            var relation = pawn.relations.DirectRelations.Single();
            var reciprocal = fixture.Other.relations.DirectRelations.Single();
            var progression = fixture.Comp; progression.BeginNewProgression(Vocation.WarriorMage);
            string formerClass = prepared == null ? "rwom.necromancer" : "rwom.priest";
            progression.State.TryAddFoundation(formerClass, DisciplineDomain.Magic);
            var magic = fixture.Magic.MagicData; magic.MagicAbilityPoints = 17; magic.MagicUserXP = 1234;
            var power = (prepared == null ? magic.MagicPowersN : magic.MagicPowersPR)[0]; power.learned = true; power.level = 1;
                        Hediff marker = cleanup
                ? new Hediff_ClassDiscipline { pawn = pawn, Part = missingLimb, profileId = formerClass, def = DefDatabase<HediffDef>.GetNamed("TM_ClassDiscipline") }
                : new IdentityHediff { pawn = pawn, def = new HediffDef { defName = "SpiritRetainedIdentity", everCurableByItem = false, keepOnBodyPartRestoration = true } };
            if (cleanup)
                pawn.health.hediffSet.hediffs.AddRange(new Hediff[] {
                    new Hediff_Injury { pawn = pawn, Part = part, def = new HediffDef { defName = "Cut" } },
                    new Hediff_MissingPart { pawn = pawn, Part = missingLimb, def = new HediffDef { defName = "MissingBodyPart" } },
                    new Hediff_AddedPart { pawn = pawn, Part = implantLimb, def = DefDatabase<HediffDef>.GetNamed("CochlearImplant") }
                });
            pawn.health.hediffSet.hediffs.Add(marker);
            var name = pawn.Name; var story = pawn.story; var skills = pawn.skills; var records = pawn.records;
            var relations = pawn.relations; var race = pawn.def; var kind = pawn.kindDef; string id = pawn.GetUniqueLoadID();

            // This is a constructed real native corpse, not a Pawn.Kill fixture.
            // Native resurrection itself and all its non-presentation callbacks run.
            Set(pawn.health, "healthState", PawnHealthState.Dead);
            PawnComponentsUtility.RemoveComponentsOnKilled(pawn);
            var corpseDef = Blank<ThingDef>(); corpseDef.defName = "SpiritResurrectionCorpse";
            corpseDef.thingClass = typeof(Corpse); corpseDef.category = ThingCategory.Item;
            corpseDef.stackLimit = 1; corpseDef.destroyable = true; corpseDef.comps = new List<CompProperties>();
            var corpse = new Corpse { def = corpseDef, thingIDNumber = 68001, InnerPawn = pawn };
            var storage = new ThingOwner<Thing>(); Check.Equal(true, storage.TryAdd(corpse, false), "native corpse holder");
            Check.Same(corpse, pawn.Corpse, "native corpse lookup uses real custody");
            Check.Equal(true, pawn.workSettings == null && pawn.needs == null, "native killed-component removal applied");
            var workSnapshotType = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthWorkSettings", true)!;
            var captureWork = workSnapshotType.GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)!;
            var restoreWorkMethod = workSnapshotType.GetMethod("Restore", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check.Equal(true, captureWork.Invoke(null, new object[] { pawn }) == null, "ordinary corpse with lost tracker has no invented pre-death work snapshot");
            object? workSnapshot = null;
            // Transaction tests share only native preparation; their callback owns
            // all physical cleanup, real resurrection and payment assertions.
            if (prepared != null) { prepared(pawn, corpse); return; }
            if (cleanup)
            {
                Check.Equal(1, pawn.health.hediffSet.GetMissingPartsCommonAncestors().Count, "prime missing limb before cleanup");
                var type = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthHealthPlan", true)!;
                object?[] args = [pawn, null, null];
                Check.Equal(true, (bool)type.GetMethod("TryPrepare", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!, "prepare exact physical cleanup");
                Check.Equal(true, (bool)type.GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(args[1], null)!, "actual native cleanup before resurrection");
                Check.Equal(true, pawn.Dead, "physical cleanup alone does not revive");
                Check.Same(marker, pawn.health.hediffSet.hediffs.Single(), "same attached marker survives physical cleanup");
                Check.Equal(0, pawn.health.hediffSet.GetMissingPartsCommonAncestors().Count, "missing limb cache cleared before native restoration");
            }
            WorkTypeDef work = null!; Pawn_WorkSettings retainedWork = null!;
            StoryWatcher_PopAdaptation adaptation = null!;
            if (player)
            {
                var faction = new Faction { def = new FactionDef { defName = "SpiritPlayer", isPlayer = true } };
                world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction);
                Set(pawn, "factionInt", faction);
                Check.Equal(true, pawn.IsColonist, "native player-faction human colonist branch");
                Set(Current.Game, "questManager", new QuestManager());
                Set(Current.Game, "playSettings", new PlaySettings { useWorkPriorities = true });
                var watcher = new StoryWatcher(); Set(Current.Game, "storyWatcher", watcher);
                adaptation = watcher.watcherPopAdaptation; adaptation.Debug_OffsetAdaptDays(17f);
                DefDatabase<WorkTypeDef>.Clear(); work = new WorkTypeDef { defName = "SpiritRetainedWork", alwaysStartActive = true };
                DefDatabase<WorkTypeDef>.Add(work);
                retainedWork = new Pawn_WorkSettings(pawn); pawn.workSettings = retainedWork;
                Check.Equal(true, captureWork.Invoke(null, new object[] { pawn }) == null, "uninitialized surviving tracker cannot be captured");
                retainedWork.EnableAndInitialize(); retainedWork.SetPriority(work, 1);
                Check.Equal(1, retainedWork.GetPriority(work), "constructed corpse retains explicit work priority before native revival");
                if (restoreWork)
                {
                    if (manualPrioritiesOff)
                    {
                        Find.PlaySettings.useWorkPriorities = false;
                        Check.Equal(3, retainedWork.GetPriority(work), "native UI getter hides actual numeric priority while manual mode is off");
                    }
                    workSnapshot = Check.NotNull(captureWork.Invoke(null, new object[] { pawn }), "capture actual retained priorities");
                    Check.Equal(!manualPrioritiesOff, Find.PlaySettings.useWorkPriorities, "capture does not toggle global manual priority setting");
                    Check.Equal(false, (bool)restoreWorkMethod.Invoke(workSnapshot, null)!, "cannot restore before native revival");
                }
            }
            var parms = new ResurrectionParams { dontSpawn = true, noLord = true, removeDiedThoughts = false, gettingScarsChance = 0f, invisibleStun = false };
            Check.Equal(true, ResurrectionUtility.TryResurrect(pawn, parms), "actual native resurrection succeeds");
            Check.Equal(false, pawn.Dead, "same pawn revived");
            Check.Equal(false, pawn.Spawned, "dontSpawn honored");
            Check.Equal(true, pawn.holdingOwner == null, "corpse releases original pawn");
            Check.Equal(true, corpse.Destroyed, "native corpse consumed"); Check.Equal(0, storage.Count, "corpse removed from original holder");
            Check.Equal(true, corpse.InnerPawn == null && pawn.Corpse == null, "corpse ownership detached both directions");
            Check.Equal(id, pawn.GetUniqueLoadID(), "same pawn load identity"); Check.Same(race, pawn.def, "race unchanged"); Check.Same(kind, pawn.kindDef, "kind unchanged");
            Check.Same(name, pawn.Name, "non-null full name retained");
            Check.Equal("Mira", ((NameTriple)pawn.Name).First, "first name retained"); Check.Equal("Ash", ((NameTriple)pawn.Name).Nick, "nickname retained"); Check.Equal("Voss", ((NameTriple)pawn.Name).Last, "surname retained");
            Check.Same(timetable, pawn.timetable, "retained timetable tracker survives"); Check.Same(TimeAssignmentDefOf.Sleep, pawn.timetable.GetAssignment(7), "sleep assignment retained"); Check.Same(TimeAssignmentDefOf.Work, pawn.timetable.GetAssignment(8), "work assignment retained");
            Check.Same(relation, pawn.relations.DirectRelations.Single(), "exact direct relation entry retained"); Check.Same(fixture.Other, relation.otherPawn, "relationship still targets original other pawn");
            Check.Same(reciprocal, fixture.Other.relations.DirectRelations.Single(), "other pawn reciprocal entry retained"); Check.Same(pawn, reciprocal.otherPawn, "reciprocal still targets exact original pawn"); Check.Same(story, pawn.story, "story and traits retained");
            Check.Same(skills, pawn.skills, "skill tracker retained"); Check.Same(skill, pawn.skills.skills.Single(), "same skill record");
            Check.Equal(13, skill.levelInt, "skill level retained"); Check.Equal(Passion.Major, skill.passion, "passion retained"); Check.Equal(123f, skill.xpSinceLastLevel, "skill XP retained");
            Check.Same(records, pawn.records, "record tracker retained"); Check.Same(relations, pawn.relations, "relation tracker retained");
            Check.Same(progression, pawn.GetComp<CompClassProgression>(), "same progression component");
            Check.Equal(true, progression.State.Find("rwom.necromancer") != null, "class discipline retained");
            Check.Same(magic, fixture.Magic.MagicData, "same magic data"); Check.Same(power, magic.MagicPowersN[0], "same learned power");
            Check.Equal(true, power.learned, "learned state retained"); Check.Equal(1, power.level, "rank retained");
            Check.Equal(17, magic.MagicAbilityPoints, "points retained"); Check.Equal(1234, magic.MagicUserXP, "class XP retained");
            Check.Equal(true, pawn.health.hediffSet.hediffs.Contains(marker), "identity hediff retained");
            if (marker is IdentityHediff identity) Check.Equal(1, identity.resurrectionCallbacks, "native callback executes once");
            if (cleanup) { Check.Same(marker, pawn.health.hediffSet.hediffs.Single(), "same class marker remains after entire cleanup and native revival chain"); Check.Same(missingLimb, marker.Part, "marker stays attached to its repaired limb"); }
            if (player) { Check.Same(retainedWork, pawn.workSettings, "native work initialization keeps surviving tracker"); Check.Equal(3, retainedWork.GetPriority(work), "native player resurrection resets custom priority to initial default"); Check.Equal(0f, adaptation.AdaptDays, "native gained-colonist population callback ran"); }
            if (restoreWork)
            {
                Check.Equal(true, (bool)restoreWorkMethod.Invoke(workSnapshot, null)!, "restore captured priorities after actual native reset");
                Check.Equal(!manualPrioritiesOff, Find.PlaySettings.useWorkPriorities, "restore leaves global manual priority setting unchanged");
                Check.Equal(false, (bool)restoreWorkMethod.Invoke(workSnapshot, null)!, "successful snapshot restoration is once only");
                Check.Equal(!manualPrioritiesOff, Find.PlaySettings.useWorkPriorities, "repeated restore does not toggle global manual priority setting");
                if (manualPrioritiesOff) Find.PlaySettings.useWorkPriorities = true;
                Check.Equal(1, pawn.workSettings.GetPriority(work), "actual original numeric priority restored even when hidden during capture and restore");
                Check.Equal(1, pawn.workSettings.GetPriority(work), "repeat does not change restored priority");
                Check.Same(retainedWork, pawn.workSettings, "restore preserves same original pawn tracker");
                Check.Equal(true, fixture.Other.workSettings == null, "restore cannot write another pawn's settings");
            }
            Check.Equal(true, pawn.needs != null && pawn.workSettings != null, "native resurrection recreates missing trackers");
            RuntimeLog.AssertClean();
            Check.Equal(false, ResurrectionUtility.TryResurrect(pawn, parms), "native repeat rejects living original");
            if (marker is IdentityHediff identityAfter) Check.Equal(1, identityAfter.resurrectionCallbacks, "repeat cannot replay resurrection callback");
            Check.Equal(0, storage.Count, "repeat cannot consume another corpse");
            var diagnostics = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Check.Equal(1, diagnostics.Count, "only expected living-pawn refusal diagnostic");
            Check.Equal(true, diagnostics[0].StartsWith("Tried to resurrect a pawn who is not dead:"), "native repeat refusal diagnostic");
            RuntimeLog.Clear();
        }
        finally { DefDatabase<WorkTypeDef>.Clear(); foreach (var def in workTypesBefore) DefDatabase<WorkTypeDef>.Add(def); StatDefOf.PainShockThreshold = priorPainShock; rootField.SetValue(null, priorRoot); DefDatabase<RecordDef>.Clear(); foreach (var def in recordsBefore) DefDatabase<RecordDef>.Add(def); }
    }
}
