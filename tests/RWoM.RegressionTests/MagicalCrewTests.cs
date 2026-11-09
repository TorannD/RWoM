using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class MagicalCrewTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MagicalCrew.ScenarioGrantsOnlyFoundationToFreshMage", f =>
        {
            using var definitions = Prepare(f);
            var pawn = PawnWithComps(false, false);
            ReadyPawn(pawn.pawn);
            var comp = new CompClassProgression { parent = pawn.pawn };
            pawn.pawn.AllComps.Add(comp);
            var reserved = PawnWithComps(false, false).pawn;
            foreach (var entry in TM_ClassGroups.MagicEntriesField.Where(e => e.trait != "Priest"))
                reserved.story.traits.allTraits.Add(new Trait(DefDatabase<TraitDef>.GetNamed(entry.trait)));
            using var scope = new StartScope([reserved, pawn.pawn], 2);
            var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var traits = DefDatabase<TraitDef>.AllDefsListForReading.ToArray();
            try
            {
                DefDatabase<ClassDisciplineDef>.Clear();
                if (DefDatabase<TraitDef>.GetNamedSilentFail("TM_VocationMage") == null)
                    DefDatabase<TraitDef>.Add(new TraitDef { defName = "TM_VocationMage", degreeDatas = [new TraitDegreeData()] });
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "CrewPriest", profileId = "rwom.priest",
                    magicPowerList = "magicPowerPR", legacyTrait = TorannMagicDefOf.Priest, domain = DisciplineDomain.Magic,
                    foundationBook = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)), spells = [
                        new DisciplineSpell { ability = TorannMagicDefOf.TM_AdvancedHeal, stage = InitiationStage.Foundation },
                        new DisciplineSpell { ability = TorannMagicDefOf.TM_Purify, stage = InitiationStage.Foundation }] });
                NewPart().PostGameStart();
                Check.Equal(InitiationStage.Foundation, comp.State.Find("rwom.priest").Stage, "scenario chooses foundation");
                Check.Equal(false, pawn.pawn.story.traits.HasTrait(TorannMagicDefOf.Priest), "identity is stored on the discipline");
                Check.Equal(2, pawn.magic.MagicData.MagicPowersPR.Count(p => p.learned), "exactly two priest spells owned");
                Check.Equal(true, pawn.magic.AbilityData.Powers.Any(p => p.Def == TorannMagicDefOf.TM_Purify), "real cast command restored");
                Check.NotNull(pawn.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "mana available");
                NewPart().PostGameStart();
                Check.Equal(1, comp.State.Disciplines.Count, "repeated scenario entry preserves single grant");
            }
            finally
            {
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var def in profiles) DefDatabase<ClassDisciplineDef>.Add(def);
                DefDatabase<TraitDef>.Clear(); foreach (var def in traits) DefDatabase<TraitDef>.Add(def);
            }
        });
        yield return new("MagicalCrew.PreserveEnrolledNoviceAndWarrior", f =>
        {
            using var definitions = Prepare(f);
            var novice = PawnWithComps(false, false).pawn;
            var warrior = PawnWithComps(false, false).pawn;
            using var scope = new StartScope([novice, warrior], 2);
            foreach (var pawn in new[] { novice, warrior })
            {
                var comp = new CompClassProgression { parent = pawn };
                pawn.AllComps.Add(comp);
                Check.Equal(true, comp.BeginNewProgression(pawn == novice ? Vocation.Mage : Vocation.Warrior), "initialized explicit identity");
            }
            novice.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.PhysicalProdigy));
            NewPart().PostGameStart();
            Check.Equal(true, novice.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy), "unspent second potential kept until a discipline is chosen");
            Check.Equal(0, warrior.story.traits.allTraits.Count, "scenario never overlays a classic magic trait on an enrolled warrior");
        });
        foreach (string className in new[] { "InnerFire", "HeartOfFrost", "StormBorn", "Arcanist", "Paladin", "Summoner", "Druid", "Necromancer", "Priest", "TM_Bard", "Succubus", "Warlock", "Geomancer", "Technomancer", "BloodMage", "Enchanter", "Chronomancer", "TM_Wanderer", "ChaosMage", "TM_Brightmage", "TM_Shaman", "TM_Golemancer" })
            yield return new("MagicalCrew.BuiltinPool." + className, f =>
            {
                using var definitions = Prepare(f);
                var xml = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"));
                Check.Equal(true, xml.Root!.Elements("TraitDef").Any(x => (string?)x.Element("defName") == className), "random candidate is shipped by the base mod");
                var reserved = PawnWithComps(false, false); var p = PawnWithComps(false, false); ReadyPawn(p.pawn);
                foreach (var entry in TM_ClassGroups.MagicEntriesField.Where(e => e.trait != className))
                    reserved.pawn.story.traits.allTraits.Add(new Trait(DefDatabase<TraitDef>.GetNamed(entry.trait)));
                using var scope = new StartScope([reserved.pawn, p.pawn], 2);
                NewPart().PostGameStart();
                Check.Equal(className, p.pawn.story.traits.allTraits.Single().def.defName, "existing crew classes reserved before random allocation");
                Check.Equal(true, p.magic.IsMagicUser, "native component recognizes every eligible class");
                Check.NotNull(p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "every eligible class has mana");
                var rows = MagicRows(Build(p.pawn));
                Check.Equal(true, rows.Count > 0, "every eligible class renders native grimoire rows");
                Check.Equal(true, rows.Any(r => r.learned), "every eligible class gets basic spells");
                Check.Equal(true, p.magic.AbilityData.Powers.Count > 0, "basic spells have runtime abilities");
            });
        yield return new("MagicalCrew.PotentialBecomesClassOnlyForSelectedCrew", f =>
        {
            using var definitions = Prepare(f);
            var crew = Enumerable.Range(0, 2).Select(_ => PawnWithComps(false, false)).ToArray();
            foreach (var p in crew)
            {
                ReadyPawn(p.pawn);
                p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Gifted));
                p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.PhysicalProdigy));
            }
            using var scope = new StartScope(crew.Select(p => p.pawn).ToList(), 1);
            var part = NewPart(); part.PostGameStart();
            Check.Equal(true, TM_ClassState.HasCurrentClass(crew[0].pawn, true), "selected potential receives real class");
            Check.Equal(false, crew[0].pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted), "magical potential removed");
            Check.Equal(false, crew[0].pawn.story.traits.HasTrait(TorannMagicDefOf.PhysicalProdigy), "physical potential removed");
            Check.Equal(2, crew[1].pawn.story.traits.allTraits.Count, "unselected potentials untouched");
            Check.NotNull(crew[0].pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "real mage keeps mana");
            part.PostGameStart();
            Check.Equal(1, crew[0].pawn.story.traits.allTraits.Count, "repeated start cannot reroll or restore potential");
        });
        yield return new("MagicalCrew.ExistingMageLosesOnlyPotential", f =>
        {
            using var definitions = Prepare(f);
            var p = PawnWithComps(true, true);
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.InnerFire));
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Gifted));
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.PhysicalProdigy));
            var other = new TraitDef { defName = "CrewUnrelatedGift", degreeDatas = [new TraitDegreeData()] };
            p.pawn.story.traits.allTraits.Add(new Trait(other));
            var data = p.magic.MagicData; data.MagicUserLevel = 19; data.MagicAbilityPoints = 7;
            data.MagicPowerSkill_Firebolt[0].level = 3;
            using var scope = new StartScope([p.pawn], 1);
            NewPart().PostGameStart();
            Check.Same(data, p.magic.MagicData, "existing progression owner retained");
            Check.Equal(19, data.MagicUserLevel, "existing level retained");
            Check.Equal(7, data.MagicAbilityPoints, "existing points retained");
            Check.Equal(3, data.MagicPowerSkill_Firebolt[0].level, "existing upgrades retained");
            Check.Equal(true, p.pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire), "existing class retained");
            Check.Equal(true, p.pawn.story.traits.HasTrait(other), "unrelated gifts retained");
            Check.Equal(2, p.pawn.story.traits.allTraits.Count, "only two potential traits removed");
        });
        yield return new("MagicalCrew.EmptySelectionDoesNotConsumeRandomness", f =>
        {
            using var definitions = Prepare(f);
            using var scope = new StartScope([], 0);
            int expected;
            Rand.PushState(184); try { expected = Rand.Int; } finally { Rand.PopState(); }
            Rand.PushState(184);
            try { NewPart().PostGameStart(); Check.Equal(expected, Rand.Int, "empty crew does not advance random state"); }
            finally { Rand.PopState(); }
        });
        yield return new("MagicalCrew.ExistingMagicIsCompletelyUntouched", f =>
        {
            using var definitions = Prepare(f);
            var existing = PawnWithComps(true, true);
            existing.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.InnerFire));
            var data = existing.magic.MagicData;
            data.MagicUserLevel = 19; data.MagicAbilityPoints = 7;
            data.MagicPowerSkill_Firebolt[0].level = 3;
            existing.magic.magicPowersInitialized = false;
            using var scope = new StartScope([existing.pawn], 1);
            NewPart().PostGameStart();
            Check.Same(data, existing.magic.MagicData, "existing progression owner unchanged");
            Check.Equal(19, data.MagicUserLevel, "existing level unchanged");
            Check.Equal(3, data.MagicPowerSkill_Firebolt[0].level, "existing skill untouched");
            Check.Equal(false, existing.magic.magicPowersInitialized, "existing mage must not even be repaired");
            Check.Equal(0, existing.pawn.needs.AllNeeds.Count, "existing mage gets no mana repair");
            Check.Equal(1, existing.pawn.story.traits.allTraits.Count, "existing class unchanged");
        });
        yield return new("MagicalCrew.ExistingCustomMagicWithoutDataIsUntouched", f =>
        {
            using var definitions = Prepare(f); var p = PawnWithComps(false, false);
            var trait = new TraitDef { defName = "CrewAddonMage", degreeDatas = [new TraitDegreeData()] };
            TM_ClassUtility.CustomClasses = [new TM_CustomClass { classTrait = trait, isMage = true, isAdvancedClass = true }];
            p.pawn.story.traits.allTraits.Add(new Trait(trait));
            using var scope = new StartScope([p.pawn], 1);
            NewPart().PostGameStart();
            Check.Equal(1, p.pawn.story.traits.allTraits.Count, "custom advanced mage does not receive a base class");
            Check.Same(null!, typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(p.magic)!, "custom missing progression remains untouched");
        });
        yield return new("MagicalCrew.SelectedThreeGetDistinctClassesAndPreserveMight", f =>
        {
            using var definitions = Prepare(f);
            var crew = Enumerable.Range(0, 4).Select(_ => PawnWithComps(false, true)).ToArray();
            foreach (var p in crew) ReadyPawn(p.pawn);
            var fighter = crew[0]; fighter.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Gladiator));
            var might = fighter.might.MightData; might.MightUserLevel = 21; might.MightAbilityPoints = 8;
            var skill = might.AllMightPowerSkills.First(); skill.level = 2;
            using var scope = new StartScope(crew.Select(x => x.pawn).ToList(), 3);
            var part = NewPart(); part.PostGameStart();
            var classes = crew.Take(3).Select(p => p.pawn.story.traits.allTraits.Single(t => TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def.defName)).def.defName).ToList();
            Check.Equal(3, classes.Distinct().Count(), "three distinct random base classes");
            Check.Equal(false, classes.Contains("Lich"), "transformation is not a starting class");
            Check.Equal(0, crew[3].pawn.story.traits.allTraits.Count, "unselected candidate untouched");
            Check.Same(might, fighter.might.MightData, "fighter progression owner retained");
            Check.Equal(21, might.MightUserLevel, "fighter level retained");
            Check.Equal(8, might.MightAbilityPoints, "fighter points retained");
            Check.Equal(2, skill.level, "fighter skill retained");
            Check.Equal(true, fighter.pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator), "fighter trait retained");
            foreach (var p in crew.Take(3))
            {
                Check.Equal(true, p.magic.IsMagicUser, "new class recognized");
                Check.NotNull(p.pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "new mage has mana");
                Check.Equal(true, p.magic.magicPowersInitialized, "new mage cannot reinitialize destructively");
                Check.Equal(true, MagicRows(Build(p.pawn)).Count > 0, "grimoire contains native class powers");
            }
            part.PostGameStart();
            Check.Equal(true, classes.SequenceEqual(crew.Take(3).Select(p => p.pawn.story.traits.allTraits.Single(t => TM_ClassGroups.MagicEntriesField.Any(e => e.trait == t.def.defName)).def.defName)), "repeat cannot reroll");
            // Remove a class after completion: receipt, rather than current traits, must prevent a replacement.
            crew[0].pawn.story.traits.allTraits.RemoveAll(t => t.def.defName == classes[0]);
            var path = Path.Combine(f.WorkingDirectory, "crew-part.xml");
            Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref part, "part"); Scribe.saver.FinalizeSaving(); Scribe.ForceStop();
            ScenPart loaded = null!;
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "part"); Scribe.loader.FinalizeLoading(); Scribe.ForceStop();
            loaded.PostGameStart();
            Check.Equal(false, TM_ClassState.HasCurrentClass(crew[0].pawn, true), "loaded receipt prevents reroll even after player class removal");
        });
    }

    static ScenPart NewPart()
    {
        var type = typeof(MagicData).Assembly.GetType("TorannMagic.ScenPart_MagicalCrew");
        Check.NotNull(type, "magical crew scenario part must exist");
        return (ScenPart)Activator.CreateInstance(type!)!;
    }
    static DefinitionScope Prepare(RuntimeFixture f)
    {
        var scope = new DefinitionScope();
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(NeedDefOf));
        LoadBaseDefinitions(f); TM_ClassUtility.CustomClasses = []; TM_ClassUtility.CustomAdvancedClasses.Clear();
        foreach (var t in DefDatabase<TraitDef>.AllDefsListForReading) { scope.TraitDegrees[t] = t.degreeDatas; t.degreeDatas = [new TraitDegreeData()]; }
        foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading) { a.abilityClass = typeof(PawnAbility); a.shouldInitialize = true; }
        return scope;
    }
    static void ReadyPawn(Pawn pawn)
    {
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        pawn.def.race.lifeStageAges = [new LifeStageAge { minAge = 0, def = new LifeStageDef { defName = "CrewAdult", developmentalStage = DevelopmentalStage.Adult } }];
        Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
        // Use real NeedsTracker policy; only preseed the health enabler to avoid unrelated headless body generation.
        var hd = TorannMagicDefOf.TM_MagicUserHD;
        pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = hd });
        typeof(HediffSet).GetMethod("CacheNeeds", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(pawn.health.hediffSet, null);
    }
    sealed class DefinitionScope : IDisposable
    {
        public readonly Dictionary<TraitDef, List<TraitDegreeData>> TraitDegrees = [];
        readonly NeedDef oldNeed = TorannMagicDefOf.TM_Mana;
        readonly HediffDef oldHediff = TorannMagicDefOf.TM_MagicUserHD;
        public DefinitionScope()
        {
            var need = new NeedDef { defName = "CrewMana", needClass = typeof(Need_Mana), onlyIfCausedByHediff = true };
            TorannMagicDefOf.TM_Mana = need;
            TorannMagicDefOf.TM_MagicUserHD = new HediffDef { defName = "CrewMagicUser", stages = [new HediffStage { enablesNeeds = [need] }] };
        }
        public void Dispose() { TorannMagicDefOf.TM_Mana = oldNeed; TorannMagicDefOf.TM_MagicUserHD = oldHediff; foreach (var pair in TraitDegrees) pair.Key.degreeDatas = pair.Value; }
    }
    sealed class StartScope : IDisposable
    {
        readonly Game previous = Current.Game;
        public StartScope(List<Pawn> pawns, int count)
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "initData", new GameInitData { startingAndOptionalPawns = pawns, startingPawnCount = count });
        }
        public void Dispose() => Current.Game = previous;
    }
}


