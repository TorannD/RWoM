using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ClassAscensionJobTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassAscensionJob.ActualWorkSaveAndInterruption", f => Work(f));
    }
    static JobCondition? ended;
    public static bool SkipFacing() => false;
    public static bool EndBoundary(JobDriver __instance, JobCondition condition)
    {
        if (__instance is not JobDriver_ClassAscension) return true;
        ended = condition; return false; // Headless scheduler boundary, not ritual/payment logic.
    }
    static void Tick(JobDriver_ClassAscension driver, int tick)
    {
        Set(Find.TickManager, "ticksGameInt", tick);
        typeof(JobDriver_ClassAscension).GetMethod("AdvanceRitual", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(driver, null);
    }
    static JobDriver_ClassAscension RoundTrip(RuntimeFixture f, JobDriver_ClassAscension source, string name)
    {
        var path = Path.Combine(f.WorkingDirectory, name + ".xml"); var saved = source;
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "driver"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        JobDriver_ClassAscension loaded = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "driver"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        loaded.pawn = source.pawn; loaded.job = source.job; return loaded;
    }
    internal static void Work(RuntimeFixture f, bool necromancer = false, bool fire = false, bool ice = false, bool lightning = false, bool arcanist = false, bool paladin = false, bool summoner = false, bool druid = false, bool bard = false, bool warlock = false, bool succubus = false, bool geomancer = false, bool technomancer = false, bool bloodmage = false, bool enchanter = false, bool chronomancer = false, bool brightmage = false, bool shaman = false, bool golemancer = false, bool empath = false, bool hybrid = false, bool chaos = false)
    {
        LoadBaseDefinitions(f);
        using var empathDefinitions = empath ? EmpathProgressionTests.LoadDefinitions(f) : null;
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var previous = Current.Game;
        var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        var designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        var classes = TM_ClassUtility.CustomClasses;
        var ultimate = empath ? TorannMagicDefOf.TM_Symbiosis : golemancer ? TorannMagicDefOf.TM_LivingWall : shaman ? TorannMagicDefOf.TM_GuardianSpirit : brightmage ? TorannMagicDefOf.TM_SpiritOfLight : chronomancer ? TorannMagicDefOf.TM_Recall : enchanter ? TorannMagicDefOf.TM_Shapeshift : bloodmage ? TorannMagicDefOf.TM_BloodMoon : technomancer ? TorannMagicDefOf.TM_OrbitalStrike : geomancer ? TorannMagicDefOf.TM_Meteor : warlock ? TorannMagicDefOf.TM_PsychicShock : succubus ? TorannMagicDefOf.TM_Scorn : druid ? TorannMagicDefOf.TM_RegrowLimb : bard ? TorannMagicDefOf.TM_BattleHymn : paladin ? TorannMagicDefOf.TM_HolyWrath : summoner ? TorannMagicDefOf.TM_SummonPoppi : arcanist ? TorannMagicDefOf.TM_FoldReality : lightning ? TorannMagicDefOf.TM_EyeOfTheStorm : ice ? TorannMagicDefOf.TM_Blizzard : fire ? TorannMagicDefOf.TM_Firestorm : necromancer ? TorannMagicDefOf.TM_LichForm : TorannMagicDefOf.TM_Resurrection;
        var classId = chaos ? "rwom.chaos" : empath ? "rwom.empath" : golemancer ? "rwom.golemancer" : shaman ? "rwom.shaman" : brightmage ? "rwom.brightmage" : chronomancer ? "rwom.chronomancer" : enchanter ? "rwom.enchanter" : bloodmage ? "rwom.bloodmage" : technomancer ? "rwom.technomancer" : geomancer ? "rwom.geomancer" : warlock ? "rwom.warlock" : succubus ? "rwom.succubus" : druid ? "rwom.druid" : bard ? "rwom.bard" : paladin ? "rwom.paladin" : summoner ? "rwom.summoner" : arcanist ? "rwom.arcanist" : lightning ? "rwom.lightning" : ice ? "rwom.ice" : fire ? "rwom.fire" : necromancer ? "rwom.necromancer" : "rwom.priest";
        var ultimateClass = ultimate.abilityClass;
        var timeMarkClass = TorannMagicDefOf.TM_TimeMark.abilityClass;
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.ascension-job." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            ended = null;
            ultimate.abilityClass = typeof(AbilityUser.PawnAbility);
            if (chronomancer) TorannMagicDefOf.TM_TimeMark.abilityClass = typeof(AbilityUser.PawnAbility);
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ClassAscensionJobTests).GetMethod(nameof(EndBoundary))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(JobDriver).GetMethod("EndJobWith")!, prefix, null, null, null]);
            var facing = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ClassAscensionJobTests).GetMethod(nameof(SkipFacing))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_RotationTracker).GetMethod("FaceTarget")!, facing, null, null, null]);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "AscensionJobDesignation" });
            map.reservationManager = new ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager(); map.events = new MapEvents(map);
            map.lordManager = new Verse.AI.Group.LordManager(map);
            map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map);
            Set(Current.Game, "maps", new List<Map> { map });
            TM_ClassUtility.CustomClasses = [];
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "AscensionJobPriest", profileId = classId, domain = DisciplineDomain.Magic,
                spells = necromancer
                    ? [new DisciplineSpell { stage = InitiationStage.Ascended, ability = ultimate }, new DisciplineSpell { stage = InitiationStage.Ascended, ability = TorannMagicDefOf.TM_DeathBolt }]
                    : [new DisciplineSpell { stage = InitiationStage.Ascended, ability = ultimate }] });
            var p = PawnWithComps(true, false);
            p.pawn.mindState = new Pawn_MindState(p.pawn); p.pawn.rotationTracker = new Pawn_RotationTracker(p.pawn);
            p.pawn.kindDef = new PawnKindDef { defName = "AscensionJobColonist" };
            p.pawn.jobs = new Pawn_JobTracker(p.pawn); p.pawn.carryTracker = new Pawn_CarryTracker(p.pawn);
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager();
            world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction);
            Set(Current.Game, "worldInt", world);
            Set(p.pawn, "factionInt", faction); Set(p.pawn, "mapIndexOrState", (sbyte)0);
            var progression = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(progression);
            progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation(classId, DisciplineDomain.Magic);
            var other = classId == "rwom.fire" ? "rwom.ice" : "rwom.fire";
            if (hybrid)
            {
                progression.State.TryAddFoundation(other, DisciplineDomain.Magic);
                Set(progression.State.Find(other), "stage", InitiationStage.Master);
            }
            var path = progression.State.Find(classId);
            var adeptTrial = chaos ? "rwom.chaos.foundation" : empath ? "rwom.empath.foundation" : golemancer ? "rwom.golemancer.foundation" : shaman ? "rwom.shaman.foundation" : brightmage ? "rwom.brightmage.foundation" : chronomancer ? "rwom.chronomancer.foundation" : enchanter ? "rwom.enchanter.foundation" : bloodmage ? "rwom.bloodmage.foundation" : technomancer ? "rwom.technomancer.foundation" : geomancer ? "rwom.geomancer.foundation" : warlock ? "rwom.warlock.foundation" : succubus ? "rwom.succubus.foundation" : druid ? "rwom.druid.foundation" : bard ? "rwom.bard.foundation" : paladin ? "rwom.paladin.foundation" : summoner ? "rwom.summoner.foundation" : arcanist ? "rwom.arcanist.foundation" : lightning ? "rwom.lightning.foundation" : ice ? "rwom.ice.foundation" : fire ? FireboltDamageTrial.TrialId : necromancer ? NecromancerDeathMarkTrial.TrialId : PriestHealingTrial.TrialId;
            var masterTrial = chaos ? "rwom.chaos.mastery" : empath ? "rwom.empath.mastery" : enchanter ? "rwom.enchanter.mastery" : bloodmage ? "rwom.bloodmage.mastery" : technomancer ? "rwom.technomancer.mastery" : geomancer ? "rwom.geomancer.mastery" : warlock ? "rwom.warlock.mastery" : succubus ? "rwom.succubus.mastery" : druid ? "rwom.druid.mastery" : bard ? "rwom.bard.mastery" : paladin ? "rwom.paladin.mastery" : summoner ? "rwom.summoner.mastery" : arcanist ? "rwom.arcanist.mastery" : lightning ? "rwom.lightning.mastery" : ice ? "rwom.ice.mastery" : fire ? FireclawTrial.TrialId : necromancer ? NecromancerControlTrial.TrialId : PriestPurificationTrial.TrialId;
            path.RecordCompletedTrial(adeptTrial); progression.State.TryAdvanceAfterTrial(classId, InitiationStage.Foundation, adeptTrial);
            path.RecordCompletedTrial(masterTrial); progression.State.TryAdvanceAfterTrial(classId, InitiationStage.Adept, masterTrial);
            var circleDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            circleDef.defName = "AscensionJobCircle"; circleDef.thingClass = typeof(Building_TMMagicCircle);
            circleDef.hasInteractionCell = true; circleDef.interactionCellOffset = new IntVec3(0, 0, -1); circleDef.size = new IntVec2(3, 3);
            var circle = new Thing { def = circleDef }; Set(circle, "factionInt", faction);
            Set(circle, "mapIndexOrState", (sbyte)0); Set(circle, "positionInt", new IntVec3(10, 0, 10));
            Set(p.pawn, "positionInt", circle.InteractionCell);
            var scrollDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            scrollDef.defName = (empath || chaos) ? "GemstoneOfInsight_Magic" : golemancer ? "SpellOf_LivingWall" : shaman ? "SpellOf_GuardianSpirit" : brightmage ? "SpellOf_SpiritOfLight" : chronomancer ? "SpellOf_Recall" : enchanter ? "SpellOf_Shapeshift" : bloodmage ? "SpellOf_BloodMoon" : technomancer ? "SpellOf_OrbitalStrike" : geomancer ? "SpellOf_Meteor" : warlock ? "SpellOf_PsychicShock" : succubus ? "SpellOf_Scorn" : druid ? "SpellOf_RegrowLimb" : bard ? "SpellOf_BattleHymn" : paladin ? "SpellOf_HolyWrath" : summoner ? "SpellOf_SummonPoppi" : arcanist ? "SpellOf_FoldReality" : lightning ? "SpellOf_EyeOfTheStorm" : ice ? "SpellOf_Blizzard" : fire ? "SpellOf_Firestorm" : necromancer ? "SpellOf_LichForm" : "SpellOf_Resurrection"; scrollDef.thingClass = typeof(Thing); scrollDef.category = ThingCategory.Item;
            scrollDef.destroyable = true; scrollDef.stackLimit = 75;
            var scroll = new Thing { def = scrollDef, stackCount = 2 };
            Check.Equal(true, p.pawn.carryTracker.innerContainer.TryAdd(scroll), "native carry owns unconsumed offering");
            var otherArcanistPowers = p.magic.MagicData.MagicPowersA.Where(power => !power.TMabilityDefs.Contains(TorannMagicDefOf.TM_FoldReality)).ToArray();
            var otherArcanistLearned = otherArcanistPowers.Select(power => power.learned).ToArray();
            var driver = new JobDriver_ClassAscension { pawn = p.pawn, job = new Job(new JobDef { defName = "AscensionJobTest" }, circle, scroll) };
            Check.Equal(true, ClassAscension.CanBegin(p.pawn, circle, scroll), "real service preflight accepts master");
            var offeringName = scrollDef.defName;
            scrollDef.defName = necromancer ? "SpellOf_Resurrection" : "SpellOf_LichForm";
            Check.Equal(false, ClassAscension.CanBegin(p.pawn, circle, scroll), "other class offering cannot pay for ascension");
            scrollDef.defName = offeringName;
            Check.Equal(true, driver.TryMakePreToilReservations(false), "native circle reservation succeeds");
            Check.Equal(2, scroll.stackCount, "preflight/reservation never pays");
            Tick(driver, 100); Tick(driver, 100);
            Check.Equal(1, driver.WorkTicks, "duplicate same-tick callback cannot add work");
            Tick(driver, 10000);
            Check.Equal(2, driver.WorkTicks, "elapsed offline time adds only one actual work tick");
            driver = RoundTrip(f, driver, "ascension-pending-job");
            Check.Equal(2, driver.WorkTicks, "native Scribe preserves actual work count");
            Tick(driver, 10000); Check.Equal(2, driver.WorkTicks, "save reload cannot duplicate last work tick");
            Tick(driver, 10001); Check.Equal(3, driver.WorkTicks, "next tick resumes one unit of work");
            Set(p.pawn, "positionInt", new IntVec3(30, 0, 30)); Tick(driver, 10002);
            Check.Equal(JobCondition.Incompletable, ended!.Value, "leaving circle interrupts ritual");
            Check.Equal(2, scroll.stackCount, "interruption never consumes offering");
            Check.Equal(InitiationStage.Master, path.Stage, "interruption grants no ascension");
            Check.Equal(false, (empath ? EmpathOwners.Resolve(p.magic.MagicData).Single(power => power.TMabilityDefs.Contains(ultimate)) : golemancer ? p.magic.MagicData.MagicPowersGolemancer.Single(power => power.TMabilityDefs.Contains(ultimate)) : shaman ? p.magic.MagicData.MagicPowersShaman.Single(power => power.TMabilityDefs.Contains(ultimate)) : brightmage ? p.magic.MagicData.MagicPowersBrightmage.Single(power => power.TMabilityDefs.Contains(ultimate)) : chronomancer ? p.magic.MagicData.MagicPowersC.Single(power => power.TMabilityDefs.Contains(ultimate)) : enchanter ? p.magic.MagicData.MagicPowersE.Single(power => power.TMabilityDefs.Contains(ultimate)) : bloodmage ? p.magic.MagicData.MagicPowersBM.Single(power => power.TMabilityDefs.Contains(ultimate)) : technomancer ? p.magic.MagicData.MagicPowersT.Single(power => power.TMabilityDefs.Contains(ultimate)) : geomancer ? p.magic.MagicData.MagicPowersG.Single(power => power.TMabilityDefs.Contains(ultimate)) : warlock ? p.magic.MagicData.MagicPowersWD.Single(power => power.TMabilityDefs.Contains(ultimate)) : succubus ? p.magic.MagicData.MagicPowersSD.Single(power => power.TMabilityDefs.Contains(ultimate)) : druid ? p.magic.MagicData.MagicPowersD.Single(power => power.TMabilityDefs.Contains(ultimate)) : bard ? p.magic.MagicData.MagicPowersB.Single(power => power.TMabilityDefs.Contains(ultimate)) : paladin ? p.magic.MagicData.MagicPowersP.Single(power => power.TMabilityDefs.Contains(ultimate)) : summoner ? p.magic.MagicData.MagicPowersS.Single(power => power.TMabilityDefs.Contains(ultimate)) : arcanist ? p.magic.MagicData.MagicPowersA.Single(power => power.TMabilityDefs.Contains(ultimate)) : lightning ? p.magic.MagicData.MagicPowersSB.Single(power => power.TMabilityDefs.Contains(ultimate)) : ice ? p.magic.MagicData.MagicPowersHoF.Single(power => power.TMabilityDefs.Contains(ultimate)) : fire ? p.magic.MagicData.MagicPowersIF[4] : necromancer ? p.magic.MagicData.MagicPowersN[5] : p.magic.MagicData.MagicPowersPR[4]).learned, "interruption grants no ultimate");
            // Resume a valid job at its final work tick and use the real payment service.
            Set(p.pawn, "positionInt", circle.InteractionCell);
            Set(driver, "workTicks", ClassAscension.RitualTicks - 1);
            Tick(driver, 10003);
            Check.Equal(true, driver.Completed, "real final work tick commits completion");
            Check.Equal(1, scroll.stackCount, "real completion spends exactly one carried offering");
            Check.Equal(InitiationStage.Ascended, path.Stage, "real completion advances ascension");
            if (!chaos) Check.Equal(true, (empath ? EmpathOwners.Resolve(p.magic.MagicData).Single(power => power.TMabilityDefs.Contains(ultimate)) : golemancer ? p.magic.MagicData.MagicPowersGolemancer.Single(power => power.TMabilityDefs.Contains(ultimate)) : shaman ? p.magic.MagicData.MagicPowersShaman.Single(power => power.TMabilityDefs.Contains(ultimate)) : brightmage ? p.magic.MagicData.MagicPowersBrightmage.Single(power => power.TMabilityDefs.Contains(ultimate)) : chronomancer ? p.magic.MagicData.MagicPowersC.Single(power => power.TMabilityDefs.Contains(ultimate)) : enchanter ? p.magic.MagicData.MagicPowersE.Single(power => power.TMabilityDefs.Contains(ultimate)) : bloodmage ? p.magic.MagicData.MagicPowersBM.Single(power => power.TMabilityDefs.Contains(ultimate)) : technomancer ? p.magic.MagicData.MagicPowersT.Single(power => power.TMabilityDefs.Contains(ultimate)) : geomancer ? p.magic.MagicData.MagicPowersG.Single(power => power.TMabilityDefs.Contains(ultimate)) : warlock ? p.magic.MagicData.MagicPowersWD.Single(power => power.TMabilityDefs.Contains(ultimate)) : succubus ? p.magic.MagicData.MagicPowersSD.Single(power => power.TMabilityDefs.Contains(ultimate)) : druid ? p.magic.MagicData.MagicPowersD.Single(power => power.TMabilityDefs.Contains(ultimate)) : bard ? p.magic.MagicData.MagicPowersB.Single(power => power.TMabilityDefs.Contains(ultimate)) : paladin ? p.magic.MagicData.MagicPowersP.Single(power => power.TMabilityDefs.Contains(ultimate)) : summoner ? p.magic.MagicData.MagicPowersS.Single(power => power.TMabilityDefs.Contains(ultimate)) : arcanist ? p.magic.MagicData.MagicPowersA.Single(power => power.TMabilityDefs.Contains(ultimate)) : lightning ? p.magic.MagicData.MagicPowersSB.Single(power => power.TMabilityDefs.Contains(ultimate)) : ice ? p.magic.MagicData.MagicPowersHoF.Single(power => power.TMabilityDefs.Contains(ultimate)) : fire ? p.magic.MagicData.MagicPowersIF[4] : necromancer ? p.magic.MagicData.MagicPowersN[5] : p.magic.MagicData.MagicPowersPR[4]).learned, "real completion teaches ultimate");
            driver = RoundTrip(f, driver, "ascension-completed-job");
            Check.Equal(true, driver.Completed, "native Scribe preserves completion latch");
            Tick(driver, 10004);
            Check.Equal(JobCondition.Succeeded, ended!.Value, "completed callback exits without another transaction");
            Check.Equal(1, scroll.stackCount, "completed latch prevents any additional payment");
            Check.Equal(false, ClassAscension.Complete(p.pawn, circle, scroll), "direct repeated completion is rejected");
            Check.Equal(1, scroll.stackCount, "repeated completion cannot spend another scroll");
            if (chaos)
            {
                Check.Equal(InitiationStage.Ascended,path.Stage,"insight offered for the one-use anchor");
                Check.Equal(false,p.magic.spell_Resurrection,"no unrelated ultimate granted");
                return;
            }
            if (hybrid)
            {
                var sibling = progression.State.Find(other);
                Check.Equal(InitiationStage.Master, sibling.Stage, "second path remains Master");
                sibling.RecordCompletedTrial(other + ".ascension");
                Check.Equal(false, progression.State.TryAdvanceAfterTrial(other, InitiationStage.Master, other + ".ascension"), "second ultimate refused even after a stale ritual");
                Check.Equal(true, ClassAscension.CandidateClassId(p.pawn) == null, "no second offering invited");
            }
            if (arcanist)
            {
                Check.Equal(true, p.magic.spell_FoldReality, "ascension persists Fold Reality ownership");
                Check.Equal(true, otherArcanistLearned.SequenceEqual(otherArcanistPowers.Select(power => power.learned)), "ascension changes no other Arcanist spell ownership");
                Check.Equal(true, path.CompletedTrials.Contains("rwom.arcanist.ascension"), "ascension records exact Arcanist trial");
            }
            if (empath)
            {
                Check.Equal(false, p.magic.spell_Resurrection, "Empath does not grant Resurrection");
                Check.Equal(false, p.pawn.health.hediffSet.hediffs.Any(h => h.def == TorannMagicDefOf.TM_OutOfBodyHD || h.def == TorannMagicDefOf.TM_SymbiosisHD), "ascension teaches Symbiosis without casting it");
                Check.Equal(0, p.pawn.story.traits.allTraits.Count, "ritual offering does not invoke gem trait-granting effect");
            }
            if (golemancer) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (shaman) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (brightmage) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (chronomancer) Check.Equal(true, p.magic.spell_Recall, "Recall ownership persisted");
            if (chronomancer) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (enchanter) Check.Equal(true, p.magic.spell_Shapeshift, "Shapeshift ownership persisted");
            if (enchanter) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (bloodmage) Check.Equal(true, p.magic.spell_BloodMoon, "Blood Moon ownership persisted");
            if (bloodmage) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (technomancer) Check.Equal(true, p.magic.spell_OrbitalStrike, "Orbital ownership persisted");
            if (technomancer) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate unchanged");
            if (geomancer) Check.Equal(true, p.magic.spell_Meteor, "Meteor ownership persisted");
            if (geomancer) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate ownership unchanged");
            if (warlock) Check.Equal(true, p.magic.spell_PsychicShock, "PsychicShock ownership persisted");
            if (succubus) Check.Equal(true, p.magic.spell_Scorn, "Scorn ownership persisted");
            if (warlock || succubus) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate ownership unchanged");
            if (druid) Check.Equal(true, p.magic.spell_RegrowLimb, "RegrowLimb ownership persisted");
            if (bard) Check.Equal(true, p.magic.spell_BattleHymn, "BattleHymn ownership persisted");
            if (druid || bard) Check.Equal(false, p.magic.spell_Resurrection, "other ultimate ownership unchanged");
            if (paladin) Check.Equal(true, p.magic.spell_HolyWrath, "HolyWrath ownership persisted");
            if (summoner) Check.Equal(true, p.magic.spell_SummonPoppi, "SummonPoppi ownership persisted");
            if (lightning) Check.Equal(true, p.magic.spell_EyeOfTheStorm, "ascension persists Eye of the Storm ownership");
            if (ice) Check.Equal(true, p.magic.spell_Blizzard, "ascension persists Blizzard ownership");
            if (fire) Check.Equal(true, p.magic.spell_Firestorm, "ascension persists Firestorm ownership");
            if (necromancer)
            {
                Check.Equal(true, p.magic.spell_LichForm, "ascension persists Lich Form ownership");
                Check.Equal(false, p.magic.MagicData.MagicPowersN[6].learned, "ascension does not teach Death Bolt before transformation");
                Check.Equal(false, ClassSpellAccess.CanLearn(p.pawn, p.magic.MagicData.MagicPowersN[6]), "Death Bolt remains locked before transformation");
            }
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); Current.Game = previous; TM_ClassUtility.CustomClasses = classes;
            ultimate.abilityClass = ultimateClass;
            TorannMagicDefOf.TM_TimeMark.abilityClass = timeMarkClass;
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
            DefDatabase<DesignationDef>.Clear(); foreach (var def in designations) DefDatabase<DesignationDef>.Add(def);
        }
    }
}
