using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ChronomancerProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (string spell in new[] { "AccelerateTime", "ReverseTime" })
        yield return new("ChronomancerProgression."+spell+"TargetsOrdinaryItems", f => {
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName="ChronomancerTestTool";def.category=ThingCategory.Item;
            var target=new TargetInfo(new Thing { def=def });
            foreach (var (file,name) in new[] {
                ("Abilities_TMagic_Chronomancer.xml","TM_"+spell),
                ("Bridge/Bridge_Abilities_TMagic_Chronomancer.xml","TM_Bridge_"+spell.Replace("Time","time")) })
            {
                var root=XDocument.Load(System.IO.Path.Combine(f.Repository,"v1.7/Defs/AbilityDefs",file)).Root!;
                var node=root.Elements().Single(n=>(string?)n.Element("defName")==name).Descendants("targetParams").Single();
                var parameters=new TargetingParameters {
                    canTargetItems=(bool?)node.Element("canTargetItems") ?? false,
                    mapObjectTargetsMustBeAutoAttackable=(bool?)node.Element("mapObjectTargetsMustBeAutoAttackable") ?? true };
                Check.Equal(true,parameters.CanTarget(target),name+" selects a normal noncombat item");
                if (spell=="ReverseTime") Check.Equal(true,(bool?)node.Element("canTargetCorpses") ?? false,name+" accepts corpses");
            }
        });
        yield return new("ChronomancerProgression.NativeBooksAndStageGates", Books);
        yield return new("ChronomancerProgression.NativeTimeMarkAndSavedRetention", Children);
        yield return new("ChronomancerProgression.RecallMarkerSnapshotReferencesUnchanged", Marker);
        yield return new("ChronomancerProgression.KeyedUiCoverageInSevenLanguages", KeyedCoverage);
        yield return new("ChronomancerProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("ChronomancerProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("ChronomancerProgression.LegacyEnrollmentBlockedAndSavedFlagPreserved", LegacyAndFlags);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        List<TMAbilityDef> previousChildren;
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.chronomancer");
        public readonly ClassDisciplineDef Profile;
        public readonly Faction Friendly = new() { def = new FactionDef { isPlayer = true } };
        public readonly Faction Enemy = new() { def = new FactionDef() };
        public Fixture(RuntimeFixture f, bool initiate = true)
        {
            LoadBaseDefinitions(f);
            if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            var nativeRecall = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Chronomancer.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_Recall");
            TorannMagicDefOf.TM_Recall.shouldInitialize = bool.Parse(nativeRecall.Element("shouldInitialize")!.Value);
            previousChildren = TorannMagicDefOf.TM_Recall.childAbilities;
            TorannMagicDefOf.TM_Recall.childAbilities = nativeRecall.Element("childAbilities")!.Elements()
                .Select(e => DefDatabase<TMAbilityDef>.GetNamed(e.Value)).ToList();
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "ChronomancerBookDesignation" });
            map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map);
            map.reservationManager = new Verse.AI.ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
            Set(Current.Game, "maps", new List<Map> { map });
            Set(Friendly, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } });
            Set(Enemy, "relations", new List<FactionRelation> { new() { other = Friendly, kind = FactionRelationKind.Hostile } });
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic;
            Target = PawnWithComps(false, false).pawn;
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn); pawn.guest = new Pawn_GuestTracker(pawn);
                pawn.kindDef = new PawnKindDef { defName = "ChronomancerTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.chronomancer");
            Profile = new ClassDisciplineDef { defName = "ChronomancerProgression", profileId = "rwom.chronomancer", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.Chronomancer, magicPowerList = node.Element("magicPowerList")!.Value,
                foundationBook = BookDef(node.Element("foundationBook")!.Value), adeptBook = BookDef(node.Element("adeptBook")!.Value),
                adeptTrialId = node.Element("adeptTrialId")!.Value,
                masterBook = BookDef(node.Element("masterBook")!.Value), masterTrialId = node.Element("masterTrialId")!.Value,
                spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell {
                    ability = DefDatabase<TMAbilityDef>.GetNamed(e.Element("ability")!.Value), stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value) }).ToList() };
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(Profile);
            if (initiate) Check.Equal(true, ClassInitiation.CompleteBook(Caster, Book(Profile.foundationBook)), "native foundation transaction succeeds");
        }
        public static ThingDef BookDef(string name)
        {
            var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            d.defName = name; d.thingClass = typeof(Thing); d.category = ThingCategory.Item; d.destroyable = true; d.stackLimit = 75; d.drawerType = DrawerType.RealtimeOnly;
            return d;
        }
        public Thing Book(ThingDef def)
        {
            var book = new Thing { def = def, stackCount = 2 };
            Check.Equal(true, Caster.inventory.innerContainer.TryAdd(book), "native inventory owns book"); return book;
        }
        public void Dispose()
        {
            Current.Game = previous;
            TorannMagicDefOf.TM_Recall.childAbilities = previousChildren;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.initialize; }
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
        }
    }

    static void Books(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        w.Magic.MagicData.MagicAbilityPoints = 8; w.Magic.MagicData.MagicUserXP = 43;
        Check.Equal("Torn_BookOfChronomancer", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfChronomancer", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersC;
        foreach (var def in new[] { TorannMagicDefOf.TM_Prediction, TorannMagicDefOf.TM_AlterFate })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_AccelerateTime, TorannMagicDefOf.TM_ReverseTime, TorannMagicDefOf.TM_ChronostaticField, TorannMagicDefOf.TM_Recall })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.chronomancer.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_AccelerateTime, TorannMagicDefOf.TM_ReverseTime })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(4, powers.Count(p => p.learned), "two adept families");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeChronomancer", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.chronomancer.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ChronostaticField)).learned, "ChronostaticField granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall)).learned, "Recall remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsChronomancer(w.Caster), "saved Chronomancer identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var powers = w.Magic.MagicData.MagicPowersC;
        var originalOwners = powers.ToArray();
        var originalSkills = new[] { w.Magic.MagicData.MagicPowerSkill_Prediction, w.Magic.MagicData.MagicPowerSkill_AlterFate,
            w.Magic.MagicData.MagicPowerSkill_AccelerateTime, w.Magic.MagicData.MagicPowerSkill_ReverseTime,
            w.Magic.MagicData.MagicPowerSkill_ChronostaticField, w.Magic.MagicData.MagicPowerSkill_Recall };
        var skillRecords = originalSkills.Select(list => list.ToArray()).ToArray();
        var inherited = w.Magic.MagicData.MagicPowersIF.First(); inherited.learned = true;
        inherited.level = 1; inherited.ticksUntilNextCast = 37;
        var shared = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_AlterFate));
        shared.learned = true; shared.autocast = true; shared.ticksUntilNextCast = 823;
        
        w.Magic.MagicData.MagicPowerSkill_Prediction.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation with shared owner");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, ReferenceEquals(shared, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_AlterFate))), "native shared owner retained");
        Check.Equal(true, originalOwners.SequenceEqual(powers), "all native owner references retained");
        var restoredSkills = new[] { w.Magic.MagicData.MagicPowerSkill_Prediction, w.Magic.MagicData.MagicPowerSkill_AlterFate,
            w.Magic.MagicData.MagicPowerSkill_AccelerateTime, w.Magic.MagicData.MagicPowerSkill_ReverseTime,
            w.Magic.MagicData.MagicPowerSkill_ChronostaticField, w.Magic.MagicData.MagicPowerSkill_Recall };
        for (int i = 0; i < originalSkills.Length; i++)
        {
            Check.Equal(true, ReferenceEquals(originalSkills[i], restoredSkills[i]), "native skill list retained: " + i);
            Check.Equal(true, skillRecords[i].SequenceEqual(restoredSkills[i]), "native skill objects retained: " + i);
        }
        Check.Equal(true, inherited.learned, "inherited other-class grant retained");
        Check.Equal(1, inherited.level, "inherited other-class rank retained");
        Check.Equal(37, inherited.ticksUntilNextCast, "inherited other-class cooldown retained");
        Check.Equal(true, shared.autocast, "autocast retained");
        Check.Equal(823, shared.ticksUntilNextCast, "cooldown retained");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_Prediction.First().level, "skill investment retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == shared.abilityDef), "shared runtime restored once");
        var locked = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall));
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, locked), "missing profile cannot unlock ultimate");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shared), "missing profile preserves learned shared owner");
    }
    static void LegacyAndFlags(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var native = new Trait(TorannMagicDefOf.Chronomancer);
        w.Caster.story.traits.allTraits.Add(native);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy Chronomancer is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        Check.Equal(0, w.Progression.State.Disciplines.Count, "legacy rejection preserves empty progression");
        w.Caster.story.traits.allTraits.Remove(native);
        w.Magic.spell_Recall = true;
        var fold = w.Magic.MagicData.MagicPowersC.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall));
        fold.level = 0; fold.ticksUntilNextCast = 823; fold.autocast = true;
        w.Magic.MagicData.MagicPowerSkill_Recall.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "fresh foundation with old spell flag");
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, fold.learned, "saved ultimate flag retains native owner");
        Check.Equal(0, fold.level, "flag repair preserves native spell rank");
        Check.Equal(TorannMagicDefOf.TM_Recall, fold.abilityDef, "saved rank selects native Recall");
        Check.Equal(823, fold.ticksUntilNextCast, "flag repair preserves cooldown");
        Check.Equal(true, fold.autocast, "flag repair preserves autocast");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_Recall.First().level, "flag repair preserves skill investment");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, fold), "prior ownership survives stage gate");
        Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == fold.abilityDef), "saved learning flag never creates Recall control");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_TimeMark), "saved learning flag restores TimeMark once");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "prior spell does not advance stage");
    }

    static void Children(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var data = w.Magic.MagicData;
        var recall = data.MagicPowersC.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Recall));
        var mark = data.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TimeMark));
        foreach (var skill in data.MagicPowerSkill_Recall) skill.level = 3;
        ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook));
        Check.Equal(false, mark.learned, "skill investment never learns TimeMark");
        Check.Equal(false, w.Magic.recallSet, "foundation never invents marker");
        recall.learned = true;
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, mark.learned, "saved native Recall derives TimeMark owner");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == mark.abilityDef), "TimeMark runtime restored once");
        Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == recall.abilityDef), "learned Recall is not a permanent button");
        Check.Equal(false, w.Magic.recallSet || w.Magic.recallSpell, "learning never activates marker or control flag");
        Check.Equal(default(IntVec3), w.Magic.recallPosition, "learning never chooses marker position");
        recall.learned = false; w.Magic.spell_Recall = false;
        DefDatabase<ClassDisciplineDef>.Clear();
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, mark.learned, "independently saved child survives missing parent/profile");
        Check.Equal(false, recall.learned, "saved child does not invent Recall ownership");
        var field = data.MagicPowersC.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ChronostaticField));
        field.learned = true; field.level = 2; field.ticksUntilNextCast = 842;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(TorannMagicDefOf.TM_ChronostaticField_II, field.abilityDef, "native Field rank retained");
        Check.Equal(842, field.ticksUntilNextCast, "native Field cooldown retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == field.abilityDef), "rank runtime restored once");
        w.Magic.recallSpell = true;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == recall.abilityDef), "orphan dynamic flag cannot fabricate Recall button");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "saved grants never advance stage");
    }

    static void Marker(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var magic = w.Magic;
        magic.spell_Recall = true; magic.recallSet = true; magic.recallSpell = true;
        magic.recallExpiration = 1000; magic.recallPosition = new IntVec3(3, 0, 7); magic.recallMap = w.Caster.Map;
        var names = magic.recallNeedDefnames = new List<string> { "Food", "Rest" };
        var values = magic.recallNeedValues = new List<float> { .3f, .7f };
        var hediff = new Hediff { def = new HediffDef { defName = "ChronoMarkerSnapshot" }, pawn = w.Caster };
        var hediffs = magic.recallHediffList = new List<Hediff> { hediff };
        var severity = magic.recallHediffDefSeverityList = new List<float> { .4f };
        var ticks = magic.recallHediffDefTicksRemainingList = new List<int> { 654 };
        var injuries = magic.recallInjuriesList = new List<Hediff_Injury>();
        var map = magic.recallMap;
        ClassSpellAccess.RestoreLearned(magic); ClassSpellAccess.RestoreLearned(magic);
        Check.Equal(true, magic.recallSet && magic.recallSpell, "active marker preserved");
        Check.Equal(1000, magic.recallExpiration, "marker deadline not refreshed");
        Check.Equal(new IntVec3(3, 0, 7), magic.recallPosition, "marker position retained");
        Check.Same(map, magic.recallMap, "marker map reference retained");
        Check.Same(names, magic.recallNeedDefnames, "need names retained"); Check.Same(values, magic.recallNeedValues, "need values retained");
        Check.Same(hediffs, magic.recallHediffList, "health snapshots retained"); Check.Same(hediff, magic.recallHediffList.Single(), "health object retained");
        Check.Same(severity, magic.recallHediffDefSeverityList, "parallel severities retained"); Check.Same(ticks, magic.recallHediffDefTicksRemainingList, "parallel durations retained");
        Check.Same(injuries, magic.recallInjuriesList, "injury snapshots retained");
        Check.Equal(1, magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Recall), "real active marker restores one dynamic Recall");
        Check.Equal(1, magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_TimeMark), "real active marker keeps one TimeMark");
        magic.AbilityData.Powers.RemoveAll(a => a.Def == TorannMagicDefOf.TM_Recall);
        magic.AbilityData.AllPowers = null;
        magic.ResolveChronomancerTimeMark(); magic.ResolveChronomancerTimeMark();
        Check.Equal(1, magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Recall), "native maintenance repairs saved flag with missing runtime");
        Check.Equal(1000, magic.recallExpiration, "native repair does not refresh deadline");
        Check.Same(names, magic.recallNeedDefnames, "native repair keeps snapshot arrays");
        magic.recallExpiration = 0;
        magic.ResolveChronomancerTimeMark();
        Check.Equal(false, magic.recallSet || magic.recallSpell, "expired marker removes dynamic ownership");
        Check.Equal(0, magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Recall), "expired marker removes Recall control");
        Check.Equal(1, magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_TimeMark), "expiration retains learned TimeMark");
    }

    static void KeyedCoverage(RuntimeFixture f)
    {
        foreach (string language in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
        {
            var xml = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Languages", language, "Keyed/TM_GrimoireProgression.xml"));
            foreach (string suffix in new[] { "", "FoundationTrial", "MasteryTrial", "AttunementHint", "AscensionAction", "AscensionNeedsOffering", "AscensionRequirements", "AttunementAction", "AttunementNeedsOffering" })
            {
                string key = "TM_ProgressionChronomancer" + suffix;
                var entries = xml.Root!.Elements(key).ToArray();
                Check.Equal(1, entries.Length, language + " has exactly one " + key);
                Check.Equal(false, string.IsNullOrWhiteSpace(entries.Single().Value), language + " translates " + key);
            }
        }
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.Chronomancer; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "Chronomancer");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Chronomancer matches native Chronomancer offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "native work restriction parity");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsChronomancer(w.Caster), "invalid stage is not Chronomancer identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsChronomancer(w.Caster), "wrong domain is not Chronomancer identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual Chronomancer trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}