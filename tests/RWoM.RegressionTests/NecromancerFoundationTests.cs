using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class NecromancerFoundationTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("NecromancerFoundation.NativeBookAndNoLichGrants", f =>
        {
            LoadBaseDefinitions(f);
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading) { a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            var previous = Current.Game;
            var profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToList();
            var designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToList();
            try
            {
                Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
                var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
                DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "NecromancerBookDesignation" });
                map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map);
                map.reservationManager = new Verse.AI.ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
                Set(Current.Game, "maps", new List<Map> { map });
                var p = PawnWithComps(true, false); Set(p.pawn, "mapIndexOrState", (sbyte)0);
                var progression = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(progression); progression.BeginNewProgression(Vocation.Mage);
                p.magic.MagicData.MagicAbilityPoints = 6;
                var bookDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                bookDef.defName = "Torn_BookOfUndead"; bookDef.thingClass = typeof(Thing); bookDef.category = ThingCategory.Item;
                bookDef.destroyable = true; bookDef.stackLimit = 75; bookDef.drawerType = DrawerType.RealtimeOnly;
                var node = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                    .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.necromancer");
                Check.Equal(bookDef.defName, node.Element("foundationBook")!.Value, "explicit native torn-book mapping");
                DefDatabase<ClassDisciplineDef>.Clear();
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef {
                    defName = "TM_PathNecromancer", profileId = "rwom.necromancer", domain = DisciplineDomain.Magic,
                    legacyTrait = TorannMagicDefOf.Necromancer, magicPowerList = node.Element("magicPowerList")!.Value, foundationBook = bookDef,
                    spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell {
                        ability = DefDatabase<TMAbilityDef>.GetNamed(e.Element("ability")!.Value),
                        stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value) }).ToList()
                });
                var book = new Thing { def = bookDef, stackCount = 2 };
                p.pawn.inventory = new Pawn_InventoryTracker(p.pawn);
                Check.Equal(true, p.pawn.inventory.innerContainer.TryAdd(book), "native inventory receives necromancer book");
                Check.Equal(true, ClassInitiation.CanUseFoundation(p.pawn, book, out _), "novice can select necromancer");
                Check.Equal(true, ClassInitiation.CompleteBook(p.pawn, book), "foundation completes without trait grant");
                Check.Equal(1, book.stackCount, "one book consumed");
                Check.Equal(false, ClassInitiation.CompleteBook(p.pawn, book), "foundation cannot repeat");
                p.magic.InitializeSpell(); p.magic.ResolveFactionChange();
                var powers = p.magic.MagicData.MagicPowersN;
                Check.Equal(2, powers.Count(p => p.learned), "restore only owns two foundation spells");
                Check.Equal(true, powers[0].learned && powers[3].learned, "RaiseUndead and ConsumeCorpse learned");
                foreach (int i in new[] { 1, 2, 4, 5, 6 }) Check.Equal(false, ClassSpellAccess.CanLearn(p.pawn, powers[i]), "later family remains locked: " + i);
                Check.Equal(false, p.magic.spell_LichForm || p.magic.spell_Flight, "foundation never transforms or grants flight");
                Check.Equal(2, p.magic.AbilityData.Powers.Count, "no DeathBolt or other runtime grants");
                Check.Equal(0, p.pawn.story.traits.allTraits.Count, "no legacy class trait inserted");
                Check.Equal(6, p.magic.MagicData.MagicAbilityPoints, "existing point owner unchanged");
                var profile = ClassIdentity.Profile("rwom.necromancer");
                var fullDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                fullDef.defName = node.Element("adeptBook")!.Value;
                fullDef.thingClass = typeof(Thing); fullDef.category = ThingCategory.Item;
                fullDef.destroyable = true; fullDef.stackLimit = 75; fullDef.drawerType = DrawerType.RealtimeOnly;
                profile.adeptBook = fullDef; profile.adeptTrialId = node.Element("adeptTrialId")!.Value;
                Check.Equal(NecromancerControlTrial.TrialId, profile.adeptTrialId, "XML uses maintained-servant trial");
                var full = new Thing { def = fullDef, stackCount = 2 };
                Check.Equal(true, p.pawn.inventory.innerContainer.TryAdd(full), "native inventory receives full tome");
                Check.Equal(false, ClassInitiation.CompleteBook(p.pawn, full), "full tome cannot bypass trial");
                Check.Equal(2, full.stackCount, "rejected tome untouched");
                progression.State.Find("rwom.necromancer").RecordCompletedTrial(NecromancerControlTrial.TrialId);
                Check.Equal(true, ClassInitiation.CompleteBook(p.pawn, full), "completed trial permits full tome");
                Check.Equal(1, full.stackCount, "one full tome consumed");
                Check.Equal(InitiationStage.Adept, progression.State.Find("rwom.necromancer").Stage, "advances to adept");
                Check.Equal(true, powers[1].learned, "Death Mark unlocked");
                Check.Equal(3, powers.Count(power => power.learned), "only one additional spell granted");
                Check.Equal(false, ClassInitiation.CompleteBook(p.pawn, full), "adept cannot repeat");
                Check.Equal(1, full.stackCount, "repeat consumes nothing");
                Check.Equal(6, p.magic.MagicData.MagicAbilityPoints, "advancement retains points");
                var masterDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                masterDef.defName = node.Element("masterBook")!.Value;
                masterDef.thingClass = typeof(Thing); masterDef.category = ThingCategory.Item;
                masterDef.destroyable = true; masterDef.stackLimit = 75; masterDef.drawerType = DrawerType.RealtimeOnly;
                profile.masterBook = masterDef; profile.masterTrialId = node.Element("masterTrialId")!.Value;
                Check.Equal(NecromancerDeathMarkTrial.TrialId, profile.masterTrialId, "master trial matches actual mark");
                var master = new Thing { def = masterDef, stackCount = 2 };
                Check.Equal(true, p.pawn.inventory.innerContainer.TryAdd(master), "master tome in native inventory");
                Check.Equal(false, ClassInitiation.CompleteBook(p.pawn, master), "master requires marked-enemy trial");
                Check.Equal(2, master.stackCount, "rejected master tome untouched");
                progression.State.Find("rwom.necromancer").RecordCompletedTrial(NecromancerDeathMarkTrial.TrialId);
                Check.Equal(true, ClassInitiation.CompleteBook(p.pawn, master), "trial permits mastery");
                Check.Equal(1, master.stackCount, "one master tome consumed");
                Check.Equal(InitiationStage.Master, progression.State.Find("rwom.necromancer").Stage, "master stage");
                Check.Equal(true, powers[2].learned && powers[4].learned, "Fog and Corpse Explosion unlocked");
                Check.Equal(5, powers.Count(power => power.learned), "only ordinary class spells learned");
                Check.Equal(false, powers[5].learned || powers[6].learned || p.magic.spell_Flight, "master grants no lich or deathbolt");
                Check.Equal(false, ClassInitiation.CompleteBook(p.pawn, master), "master cannot repeat");
                Check.Equal(1, master.stackCount, "repeat consumes no tome");
            }
            finally
            {
                Current.Game = previous;
                DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
                DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
            }
        });
    }
}
