using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class LichTransformationTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LichTransformation.PreservesPawnAndInvestments", Preserve);
        yield return new("LichTransformation.RejectsIneligibleAndUnownedAbilities", Reject);
        yield return new("LichTransformation.NativeScribeAndMissingHealthRestore", SaveRestore);
        yield return new("LichTransformation.NativeVerbConsumesOwnedFormOnce", Cast);
        yield return new("LichTransformation.NativeEnchantmentResourceBonusOnce", Resources);
    }

    static bool SkipPhysiology() => false;

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly PawnCapacityDef[] capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        readonly WorkTypeDef[] works = DefDatabase<WorkTypeDef>.AllDefsListForReading.ToArray();
        readonly SkillDef[] skills = DefDatabase<SkillDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        readonly string harmonyId = "rwom.tests.lich-form." + Guid.NewGuid();
        readonly object harmony;
        public readonly Pawn Pawn;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.necromancer");
        public readonly PawnAbility Ability;
        public readonly HediffDef LichHealth;
        public readonly DefMap<WorkTypeDef, int> Priorities;
        public readonly WorkTypeDef[] WorkDefs;

        public Fixture(RuntimeFixture f, InitiationStage stage = InitiationStage.Ascended)
        {
            LoadBaseDefinitions(f);
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(TimeAssignmentDefOf));
            foreach (var def in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            {
                abilities.Add((def, def.abilityClass, def.shouldInitialize));
                def.abilityClass = typeof(PawnAbility); def.shouldInitialize = true;
            }
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "questManager", new QuestManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.events = new MapEvents(map); map.lordManager = new Verse.AI.Group.LordManager(map);
            Set(Current.Game, "maps", new List<Map> { map });
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef {
                defName = "LichFormNecromancer", profileId = "rwom.necromancer", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.Necromancer, magicPowerList = "magicPowerN",
                spells = [new DisciplineSpell { ability = TorannMagicDefOf.TM_LichForm, stage = InitiationStage.Ascended },
                    new DisciplineSpell { ability = TorannMagicDefOf.TM_DeathBolt, stage = InitiationStage.Ascended }] });
            DefDatabase<HediffDef>.Clear();
            // Real native Hediff storage/callbacks, without the production comp's Unity effects.
            LichHealth = new HediffDef { defName = "TM_LichHD", hediffClass = typeof(Hediff), initialSeverity = .5f, maxSeverity = 1f };
            DefDatabase<HediffDef>.Add(LichHealth);
            DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "LichFormCapacity" });
            var assembly = Assembly.Load("0Harmony");
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, harmonyId)!;
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(LichTransformationTests).GetMethod(nameof(SkipPhysiology), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            // The headless fixture lacks physiological defs; transformation eligibility and hediff mutation remain real.
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange"), prefix, null, null, null]);
            var p = PawnWithComps(true, false); Pawn = p.pawn; Magic = p.magic;
            Pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "LichFormBody" } } };
            Pawn.kindDef = new PawnKindDef { defName = "LichFormPawn" };
            Pawn.mindState = new Verse.AI.Pawn_MindState(Pawn);
            Set(Pawn, "mapIndexOrState", (sbyte)0);
            Progression = new CompClassProgression { parent = Pawn }; Pawn.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            Progression.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
            while (Path.Stage < stage)
            {
                var trial = "lich-fixture-" + Path.Stage;
                Path.RecordCompletedTrial(trial);
                Check.Equal(true, Progression.State.TryAdvanceAfterTrial(Path.ClassId, Path.Stage, trial), "fixture advances through verified trial storage");
            }
            Magic.MagicData.MagicPowersN[5].learned = true; Magic.spell_LichForm = true;
            Ability = new PawnAbility(Magic); Set(Ability, "powerDef", TorannMagicDefOf.TM_LichForm);
            Magic.AbilityData.Powers.Add(Ability);
            DefDatabase<SkillDef>.Clear();
            for (int i = 0; i < 3; i++) DefDatabase<SkillDef>.Add(new SkillDef { defName = "LichFormSkill" + i });
            Pawn.skills = new Pawn_SkillTracker(Pawn);
            for (int i = 0; i < Pawn.skills.skills.Count; i++)
            {
                Pawn.skills.skills[i].passion = (Passion)(i % 3);
                Pawn.skills.skills[i].levelInt = 8 + i;
                Pawn.skills.skills[i].xpSinceLastLevel = 120 + i;
            }
            Pawn.timetable = new Pawn_TimetableTracker(Pawn);
            var assignments = new[] { new TimeAssignmentDef { defName = "LichSleep" }, new TimeAssignmentDef { defName = "LichWork" } };
            for (int hour = 0; hour < 24; hour++) Pawn.timetable.SetAssignment(hour, assignments[hour % 2]);
            DefDatabase<WorkTypeDef>.Clear();
            WorkDefs = Enumerable.Range(0, 3).Select(i => new WorkTypeDef { defName = "LichWork" + i }).ToArray();
            foreach (var work in WorkDefs) DefDatabase<WorkTypeDef>.Add(work);
            Pawn.workSettings = new Pawn_WorkSettings(Pawn); Priorities = new DefMap<WorkTypeDef, int>();
            Set(Pawn.workSettings, "priorities", Priorities);
            for (int i = 0; i < WorkDefs.Length; i++) Priorities[WorkDefs[i]] = i + 1;
        }

        public void Dispose()
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [harmonyId]); Current.Game = previous;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.initialize; }
            Restore(profiles); Restore(hediffs); Restore(capacities); Restore(works); Restore(skills);
        }
        static void Restore<T>(T[] definitions) where T : Def, new()
        { DefDatabase<T>.Clear(); foreach (var definition in definitions) DefDatabase<T>.Add(definition); }
    }

    static void Preserve(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var data = w.Magic.MagicData;
        data.MagicUserXP = 447; data.MagicUserLevel = 11; data.MagicAbilityPoints = 7;
        data.MagicPowersN[0].learned = true; data.MagicPowersN[0].level = 2;
        var ranks = data.MagicPowersN.Select(p => p.level).ToArray();
        var skills = w.Pawn.skills.skills.Select(s => (s.passion, s.levelInt, s.xpSinceLastLevel)).ToArray();
        var timetable = Enumerable.Range(0, 24).Select(w.Pawn.timetable.GetAssignment).ToArray();
        var priorities = w.WorkDefs.Select(d => w.Priorities[d]).ToArray();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Pawn, data.MagicPowersN[6]), "ascension alone cannot learn Death Bolt");
        Check.Equal(true, LichTransformation.TryTransform(w.Pawn, w.Ability), "owned learned form transforms ascended necromancer");
        Check.Equal(true, w.Path.LichFormActive && ClassIdentity.IsLich(w.Pawn), "saved form owns lich identity");
        Check.Equal(0, w.Pawn.story.traits.allTraits.Count, "no legacy Lich or Necromancer trait inserted");
        Check.Equal(true, data.MagicPowersN[6].learned && w.Magic.spell_Flight, "transformation grants Death Bolt and Flight");
        Check.Equal(false, w.Magic.spell_LichForm, "one-time form flag retired");
        Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_LichForm), "spent form runtime ability removed");
        foreach (var def in new[] { TorannMagicDefOf.TM_DeathBolt, TorannMagicDefOf.TM_Flight })
            Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == def), "one runtime grant: " + def.defName);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, w.Ability), "repeated cast rejected");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(1, w.Pawn.health.hediffSet.hediffs.Count(h => h.def == w.LichHealth), "repeat and restore never duplicate form health");
        Check.Equal(447, data.MagicUserXP, "XP preserved"); Check.Equal(11, data.MagicUserLevel, "level preserved"); Check.Equal(7, data.MagicAbilityPoints, "points preserved");
        Check.Equal(true, ranks.SequenceEqual(data.MagicPowersN.Select(p => p.level)), "all spell ranks preserved");
        Check.Equal(true, skills.SequenceEqual(w.Pawn.skills.skills.Select(s => (s.passion, s.levelInt, s.xpSinceLastLevel))), "all passions, skill levels and XP preserved");
        Check.Equal(true, timetable.SequenceEqual(Enumerable.Range(0, 24).Select(w.Pawn.timetable.GetAssignment)), "all 24 timetable assignments preserved");
        Check.Equal(true, priorities.SequenceEqual(w.WorkDefs.Select(d => w.Priorities[d])), "every work priority preserved");
    }

    static void Reject(RuntimeFixture f)
    {
        using (var master = new Fixture(f, InitiationStage.Master))
        {
            Check.Equal(false, LichTransformation.TryTransform(master.Pawn, master.Ability), "master cannot transform despite owned ability");
            Check.Equal(false, master.Path.LichFormActive, "rejected master has no form state");
        }
        using var w = new Fixture(f);
        var fake = new PawnAbility(w.Magic); Set(fake, "powerDef", TorannMagicDefOf.TM_LichForm);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, fake), "matching definition without native owned instance rejected");
        var foreign = PawnWithComps(true, false);
        var stolen = new PawnAbility(foreign.magic); Set(stolen, "powerDef", TorannMagicDefOf.TM_LichForm);
        w.Magic.AbilityData.Powers.Add(stolen);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, stolen), "foreign ability owner rejected even in powers list");
        w.Magic.MagicData.MagicPowersN[5].learned = false;
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, w.Ability), "runtime ability without learned ownership rejected");
        w.Magic.MagicData.MagicPowersN[5].learned = true;
        Set(w.Pawn, "mapIndexOrState", (sbyte)-1);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, w.Ability), "unspawned pawn rejected");
        Set(w.Pawn, "mapIndexOrState", (sbyte)0); Set(w.Pawn.health, "healthState", PawnHealthState.Down);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, w.Ability), "downed pawn rejected");
        Set(w.Pawn.health, "healthState", PawnHealthState.Mobile);
        w.Progression.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, w.Ability), "ineligible mixed disciplines rejected");
        Check.Equal(false, w.Path.LichFormActive || w.Magic.MagicData.MagicPowersN[6].learned || w.Magic.spell_Flight, "rejections grant no form, Death Bolt or Flight");
        Check.Equal(0, w.Pawn.health.hediffSet.hediffs.Count, "rejections grant no health effect");
    }

    static void Resources(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        w.Pawn.apparel = new Pawn_ApparelTracker(w.Pawn);
        w.Pawn.equipment = new Pawn_EquipmentTracker(w.Pawn);
        w.Pawn.ageTracker = new Pawn_AgeTracker(w.Pawn);
        w.Pawn.def.race.lifeStageAges = [new LifeStageAge { minAge = 0, def = new LifeStageDef { defName = "LichResourceAdult", developmentalStage = DevelopmentalStage.Adult } }];
        Set(w.Pawn.ageTracker, "ageBiologicalTicksInt", 72000000L);
        var oldPsychic = StatDefOf.PsychicSensitivity;
        StatDefOf.PsychicSensitivity = new StatDef { defName = "LichResourcePsychicSensitivity", defaultBaseValue = 1f };
        foreach (string suffix in new[] { "maxEnergy", "energyRegen", "coolDown", "xpGain", "energyCost", "dmgResistance", "dmgBonus", "arcalleumCooldown", "arcaneSpectre", "phantomShift" })
            DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_HediffEnchantment_" + suffix, hediffClass = typeof(Hediff), initialSeverity = .5f });
        try
        {
            w.Magic.ResolveEnchantments();
            float capacity = w.Magic.maxMP, regeneration = w.Magic.mpRegenRate;
            Check.Equal(true, LichTransformation.TryTransform(w.Pawn, w.Ability), "form activates before native resource recalculation");
            w.Magic.ResolveEnchantments();
            Check.Equal(capacity + .5f, w.Magic.maxMP, "saved lich form adds native half mana capacity");
            Check.Equal(regeneration + .5f, w.Magic.mpRegenRate, "saved lich form adds native half regeneration");
            w.Magic.ResolveEnchantments();
            Check.Equal(capacity + .5f, w.Magic.maxMP, "recalculation cannot stack capacity");
            Check.Equal(regeneration + .5f, w.Magic.mpRegenRate, "recalculation cannot stack regeneration");
            Check.Equal(true, w.Magic.MagicData.MagicPowersN[5].learned, "resource recalculation preserves learned Lich Form investment");
            Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_LichForm), "recalculation does not restore spent form button");
            Check.Equal(0, w.Pawn.story.traits.allTraits.Count, "native resources require no legacy Lich trait");
        }
        finally { StatDefOf.PsychicSensitivity = oldPsychic; }
    }

    static void Cast(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var verb = new Verb_LichForm { caster = w.Pawn, Ability = w.Ability };
        var cast = typeof(Verb_LichForm).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        w.Magic.MagicData.MagicUserXP = 83; w.Magic.MagicData.MagicAbilityPoints = 4;
        Check.Equal(true, (bool)cast.Invoke(verb, null)!, "actual verb returns success while consuming its owned runtime ability");
        Check.Equal(true, w.Path.LichFormActive, "actual verb activates saved form");
        Check.Equal(false, w.Magic.AbilityData.Powers.Contains(w.Ability), "actual verb safely removes its own runtime ability");
        Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_LichForm), "no castable form remains");
        Check.Equal(false, (bool)cast.Invoke(verb, null)!, "repeated actual verb callback fails");
        Check.Equal(1, w.Pawn.health.hediffSet.hediffs.Count(h => h.def == w.LichHealth), "actual verb callbacks create one health effect");
        foreach (var def in new[] { TorannMagicDefOf.TM_DeathBolt, TorannMagicDefOf.TM_Flight })
            Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == def), "actual verb grants ability once: " + def.defName);
        Check.Equal(83, w.Magic.MagicData.MagicUserXP, "verb grants no extra XP");
        Check.Equal(4, w.Magic.MagicData.MagicAbilityPoints, "verb grants no extra points");
        Check.Equal(0, w.Pawn.story.traits.allTraits.Count, "verb never enters classic trait transformation");
    }

    static void SaveRestore(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Equal(true, LichTransformation.TryTransform(w.Pawn, w.Ability), "transform before save");
        var saved = w.Progression.State;
        var file = Path.Combine(f.WorkingDirectory, "lich-form-state.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref saved, "progression"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        ClassProgressionState loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "progression"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Check.Equal(true, loaded.Find("rwom.necromancer").LichFormActive, "native Scribe persists active form");
        Check.Equal(InitiationStage.Ascended, loaded.Find("rwom.necromancer").Stage, "native Scribe preserves ascension");
        Set(w.Progression, "state", loaded);
        w.Pawn.health.RemoveHediff(w.Pawn.health.hediffSet.hediffs.Single(h => h.def == w.LichHealth));
        Check.Equal(0, w.Pawn.health.hediffSet.hediffs.Count, "native health effect removed for repair scenario");
        Check.Equal(true, ClassIdentity.IsLich(w.Pawn), "missing hediff cannot erase saved form authority");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(1, w.Pawn.health.hediffSet.hediffs.Count(h => h.def == w.LichHealth), "native missing lich health restored once");
        foreach (var def in new[] { TorannMagicDefOf.TM_DeathBolt, TorannMagicDefOf.TM_Flight })
            Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == def), "postload grant remains unique: " + def.defName);
        Check.Equal(false, LichTransformation.TryTransform(w.Pawn, w.Ability), "saved form cannot transform twice");
        Check.Equal(0, w.Pawn.story.traits.allTraits.Count, "postload repair creates no legacy trait");
    }
}
