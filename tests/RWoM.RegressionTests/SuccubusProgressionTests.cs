using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class SuccubusProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SuccubusProgression.NativeBooksExactBranchAndStageGates", Books);
        yield return new("SuccubusProgression.SharedChoiceOrderAndGenericUseRejected", Choice);
    }
    internal sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly LoadedLanguage oldLanguage = LanguageDatabase.activeLanguage, oldDefault = LanguageDatabase.defaultLanguage;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.succubus");
        public readonly ClassDisciplineDef Profile;
        public readonly Faction Friendly = new() { def = new FactionDef { isPlayer = true } };
        public readonly Faction Enemy = new() { def = new FactionDef() };
        public Fixture(RuntimeFixture f, bool initiate = true)
        {
            LoadBaseDefinitions(f);
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English"; Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement> { ["TM_ProgressionBookUnavailable"] = new() { key = "TM_ProgressionBookUnavailable", value = "Book unavailable" } };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "SuccubusBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "SuccubusTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.succubus");
            Profile = new ClassDisciplineDef { defName = "SuccubusProgression", profileId = "rwom.succubus", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.Succubus, magicPowerList = node.Element("magicPowerList")!.Value,
                foundationBook = BookDef(node.Element("foundationBook")!.Value), adeptBook = BookDef(node.Element("adeptBook")!.Value),
                adeptTrialId = node.Element("adeptTrialId")!.Value,
                masterBook = BookDef(node.Element("masterBook")!.Value), masterTrialId = node.Element("masterTrialId")!.Value,
                spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell {
                    ability = DefDatabase<TMAbilityDef>.GetNamed(e.Element("ability")!.Value), stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value) }).ToList() };
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(Profile);
            if (initiate) Check.Equal(true, ClassInitiation.CompleteBook(Caster, Book(Profile.foundationBook), Profile.profileId), "native foundation transaction succeeds");
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
            Check.Equal(true, Caster.inventory.innerContainer.TryAdd(book, false), "native inventory owns book"); return book;
        }
        public void Dispose()
        {
            Current.Game = previous; LanguageDatabase.activeLanguage = oldLanguage; LanguageDatabase.defaultLanguage = oldDefault;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.initialize; }
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
        }
    }


    static void Books(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var torn = w.Book(w.Profile.foundationBook);
        w.Magic.MagicData.MagicAbilityPoints = 8; w.Magic.MagicData.MagicUserXP = 43;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn, w.Profile.profileId), "explicit selected foundation");
        Check.Equal(1, torn.stackCount, "one book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn, w.Profile.profileId), "repeat foundation rejected");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no native trait inserted");
        var powers = w.Magic.MagicData.MagicPowersSD;
        foreach (var ability in new[] { TorannMagicDefOf.TM_SoulBond, TorannMagicDefOf.TM_ShadowBolt })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(ability)).learned, "selected foundation owner");
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "trial required");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "existing path resolves shared full book");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Attraction)).learned, "branch adept granted");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "branch mastery granted");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Dominate)).learned, "Dominate owner granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Scorn)).learned, "ultimate stays locked");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal("rwom.succubus", w.Progression.State.Disciplines.Single().ClassId, "branch never rerolls");
    }
    static void Choice(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var other = new ClassDisciplineDef { defName = "OtherDemonBranch", profileId = "rwom.warlock", domain = DisciplineDomain.Magic,
            legacyTrait = TorannMagicDefOf.Warlock, magicPowerList = "magicPowerWD", foundationBook = w.Profile.foundationBook,
            spells = w.Profile.spells.Where(s => s.stage == InitiationStage.Foundation).ToList() };
        DefDatabase<ClassDisciplineDef>.Add(other);
        var book = w.Book(w.Profile.foundationBook);
        Check.Equal(2, ClassInitiation.FoundationCandidates(w.Caster, book).Count(), "both shared-book choices");
        Check.Equal(false, ClassInitiation.CanUseFoundation(w.Caster, book, out _), "generic selection refuses ambiguity");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "generic completion refuses ambiguity");
        var effect = new CompUseEffect_LearnMagic { parent = new ThingWithComps { def = book.def, stackCount = 2 } };
        Check.Equal(false, effect.CanBeUsedBy(w.Caster).Accepted, "native UseArtifact cannot choose a branch");
        Check.Equal(2, book.stackCount, "choice pays nothing");
        Check.Equal(0, w.Progression.State.Disciplines.Count, "choice changes no state");
        DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(other); DefDatabase<ClassDisciplineDef>.Add(w.Profile);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "reverse definition order still rejects");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book, "rwom.missing"), "unknown discriminator rejected");
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book, w.Profile.profileId), "explicit selection succeeds in reverse order");
        Check.Equal("rwom.succubus", w.Progression.State.Disciplines.Single().ClassId, "exact branch selected");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book, other.profileId), "late branch mutation rejected");
    }
}

