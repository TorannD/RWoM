using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class PawnmorpherProgressionTests
{
    // Native Pawn.ExposeData, not a replacement serializer for its components.
    static void NativeSave(RuntimeFixture f) => SpiritNativeResurrectionTests.WithPreparedCorpse(f, (pawn, corpse) =>
    {
        var originalRace = pawn.def;
        originalRace.category = ThingCategory.Pawn;
        ClassProgressionBootstrap.EnsureComp(originalRace);
        var race = (ThingDef)typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(originalRace, null)!;
        race.defName = "PawnmorpherSaveHybrid";
        race.comps = originalRace.comps?.ToList() ?? new List<CompProperties>();
        ClassProgressionBootstrap.EnsureComp(race);
        pawn.def = race;
        var magic = pawn.GetCompAbilityUserMagic();
        magic.MagicData.MagicUserLevel = 17; magic.MagicData.MagicUserXP = 123; magic.MagicData.MagicAbilityPoints = 6;
        var trained = magic.MagicData.MagicPowersPR[2]; trained.learned = true; trained.level = 1; trained.autocast = true;
        var might = pawn.GetCompAbilityUserMight();
        if (might == null) { might = new CompAbilityUserMight { parent = pawn, customIndex = -1 }; pawn.AllComps.Add(might); }
        var mightData = new MightData(might) { MightUserLevel = 12, MightUserXP = 91, MightAbilityPoints = 4 };
        Set(might, "mightData", mightData);
        var knowledge = pawn.GetComp<AbilityKnowledgeComp>();
        if (knowledge == null) { knowledge = new AbilityKnowledgeComp { parent = pawn }; pawn.AllComps.Add(knowledge); }
        knowledge.SetSkillRank("rwom.firebolt", 0, 2);
        var native = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_TEST_PAWNMORPHER")!).GetType("Pawnmorph.Hybrids.RaceShiftUtilities", true)!.GetMethod("AddRemoveDynamicRaceComps", BindingFlags.Static | BindingFlags.NonPublic)!;
        var lib = Assembly.Load("0Harmony"); var id = "rwom.tests.pawnmorpher-save." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!, id)!;
        var patch = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Compatibility.PawnmorpherRaceCompPatch", true)!;
        var processor = harmony.GetType().GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
        processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
        // Activate the real JecsTools injection callback used by the game loader.
        var injection = typeof(AbilityUserMod).GetMethod("InitializeComps_PostFix")!;
        var callback = Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!, injection)!;
        harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(ThingWithComps).GetMethod("InitializeComps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), null, callback, null, null]);
        try
        {
            native.Invoke(null, [pawn, race]);
            void Register<T>(T def) where T : Def, new() { if (def != null && DefDatabase<T>.GetNamedSilentFail(def.defName) == null) DefDatabase<T>.Add(def); }
            foreach (var type in new[] { typeof(BodyDefOf), typeof(BeardDefOf), typeof(TattooDefOf) }) RuntimeLog.EnsureHeadlessDefOfInitialized(type);
            var hair = new HairDef { defName = "PawnmorpherSaveHair" }; Register(hair); pawn.story.hairDef = hair;
            var head = new HeadTypeDef { defName = "PawnmorpherSaveHead" }; Register(head); pawn.story.headType = head;
            Register(race); Register(originalRace); Register(pawn.kindDef); Register(race.race.body);
            foreach (var skill in pawn.skills.skills) Register(skill.def);
            foreach (var time in pawn.timetable.times) Register(time);
            int hediffId = 23000;
            foreach (var hediff in pawn.health.hediffSet.hediffs) { Register(hediff.def); hediff.loadID = hediffId++; }
            var references = new List<ILoadReferenceable>();
            foreach (var relation in pawn.relations.DirectRelations) { Register(relation.def); references.Add(relation.otherPawn); }
            if (pawn.Faction != null) references.Add(pawn.Faction);
            string path = Path.Combine(f.WorkingDirectory, "hybrid-pawn.xml");
            var priorCulture = System.Globalization.CultureInfo.CurrentCulture;
            Pawn loaded = null!;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref pawn, "pawn"); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                try
                {
                    Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "pawn");
                    var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                    foreach (var reference in references) directory.RegisterLoaded(reference);
                    Scribe.loader.FinalizeLoading();
                }
                finally { Scribe.ForceStop(); }
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = priorCulture; }
            RuntimeLog.AssertClean();
            var savedMagic = (MagicData)typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loaded.GetCompAbilityUserMagic())!;
            Check.Equal(17, savedMagic.MagicUserLevel, "saved level after pruning");
            Check.Equal(123, savedMagic.MagicUserXP, "saved XP after pruning");
            Check.Equal(6, savedMagic.MagicAbilityPoints, "saved unspent points");
            Check.Equal(1, savedMagic.MagicPowersPR[2].level, "saved learned spell rank");
            Check.Equal(true, savedMagic.MagicPowersPR[2].learned, "saved learned spell");
            Check.Equal(true, savedMagic.MagicPowersPR[2].autocast, "saved autocast choice");
            var savedMight = (MightData)typeof(CompAbilityUserMight).GetField("mightData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loaded.GetCompAbilityUserMight())!;
            Check.Equal(12, savedMight.MightUserLevel, "inactive physical level survives");
            Check.Equal(91, savedMight.MightUserXP, "inactive physical XP survives");
            Check.Equal(4, savedMight.MightAbilityPoints, "inactive physical points survive");
            Check.Equal(13, loaded.skills.skills.Single().levelInt, "ordinary skill retains level");
            Check.Equal(2, loaded.GetComp<AbilityKnowledgeComp>().GetSkillRank("rwom.firebolt", 0), "saved spell knowledge");
            Check.NotNull(loaded.GetComp<CompClassProgression>().State.Find("rwom.priest"), "saved new progression foundation");
            loaded.def = originalRace; native.Invoke(null, [loaded, originalRace]);
            Check.Equal(17, ((MagicData)typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loaded.GetCompAbilityUserMagic())!).MagicUserLevel, "return to original race retains loaded progression");
        }
        finally { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]); pawn.def = originalRace; }
    });
}
