using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class ClassInitiationJobTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassInitiationJob.FixedDiscriminatorAndConcreteBookGuards", Guards);
        yield return new("ClassInitiationJob.ActualWorkCompletionAndRepeatedTick", Work);
    }
    static bool Valid(JobDriver_ClassInitiation d) => (bool)typeof(JobDriver_ClassInitiation).GetMethod("Valid", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(d, null)!;
    static JobDriver_ClassInitiation Driver(WarlockProgressionTests.Fixture w, Thing book)
        => new() { pawn = w.Caster, job = new Job { def = new JobDef { defName = "TM_InitiateWarlock", driverClass = typeof(JobDriver_ClassInitiation) }, targetA = book } };
    static void Guards(RuntimeFixture f)
    {
        var jobs = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/JobDefs/TM_ClassInitiation.xml")).Root!.Elements("JobDef").ToList();
        Check.Equal(2, jobs.Count, "exactly two serialized branch job definitions");
        foreach (string name in new[] { "TM_InitiateWarlock", "TM_InitiateSuccubus" })
        {
            var node = jobs.Single(e => (string?)e.Element("defName") == name);
            Check.Equal(typeof(JobDriver_ClassInitiation).FullName, node.Element("driverClass")!.Value, "actual XML driver binding");
            Check.Equal(true, ClassInitiationMenu.ProfileForJob(new JobDef { defName = name }) != null, "actual XML discriminator recognized");
        }
        var nativeBooks = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/ThingDefs/LearnAbility_Item.xml")).Root!.Elements("ThingDef").ToList();
        var torn = nativeBooks.Single(e => (string?)e.Element("defName") == "Torn_BookOfDemons");
        var parent = nativeBooks.Single(e => (string?)e.Attribute("Name") == (string?)torn.Attribute("ParentName"));
        while (!parent.Descendants("useJob").Any()) parent = nativeBooks.Single(e => (string?)e.Attribute("Name") == (string?)parent.Attribute("ParentName"));
        Check.Equal("UseArtifact", parent.Descendants("useJob").Single().Value, "shared book retains native use job");
        int duration = parent.Descendants("useDuration").Select(e => int.Parse(e.Value)).DefaultIfEmpty(new CompProperties_Usable().useDuration).Single();
        Check.Equal(true, duration > 0, "native usable book duration is available");
        foreach (string lang in new[] { "English", "Russian", "Ukrainian", "ChineseSimplified", "Spanish", "Portuguese", "PortugueseBrazilian" })
        {
            var keys = XDocument.Load(Path.Combine(f.Repository, "v1.7/Languages", lang, "Keyed/TM_GrimoireProgression.xml")).Root!.Elements().ToList();
            foreach (string branch in new[] { "Warlock", "Succubus" })
                foreach (string suffix in new[] { "", "FoundationTrial", "MasteryTrial", "AttunementHint", "AscensionAction", "AscensionNeedsOffering", "AscensionRequirements", "AttunementAction", "AttunementNeedsOffering" })
                    Check.Equal(true, !string.IsNullOrWhiteSpace(keys.Single(e => e.Name.LocalName == "TM_Progression" + branch + suffix).Value), "unique branch locale entry: " + lang + branch + suffix);
            string choice = keys.Single(e => e.Name.LocalName == "TM_ProgressionChooseDemonBranch").Value;
            Check.Equal(true, choice.Contains("{0}") && choice.Contains("{1}"), "explicit branch/book placeholders retained: " + lang);
        }
        using var w = new WarlockProgressionTests.Fixture(f, false);
        var first = w.Book(w.Profile.foundationBook); var second = w.Book(w.Profile.foundationBook);
        var d = Driver(w, first);
        Check.Equal(true, Valid(d), "fixed job def selects Warlock before reservations");
        Check.Equal("rwom.warlock", d.SelectedProfileId, "selection bound once");
        d.job.targetA = second;
        Check.Equal(false, Valid(d), "same-def replacement cannot inherit reading work");
        d.job.targetA = first;
        Set(d, "selectedProfileId", "rwom.succubus");
        Check.Equal(false, Valid(d), "mismatched saved branch rejected");
        Set(d, "selectedProfileId", "rwom.missing");
        Check.Equal(false, Valid(d), "unknown discriminator rejected");
        Set(d, "selectedProfileId", null); Set(d, "loaded", true);
        Check.Equal(false, Valid(d), "missing saved discriminator cannot be reconstructed");
        Set(d, "selectedProfileId", "rwom.warlock"); Set(d, "originalBook", null);
        Check.Equal(false, Valid(d), "missing saved book cannot inherit work");
        Check.Equal(2, first.stackCount, "all invalid jobs leave payment untouched");
        var fresh = Driver(w, first); Check.Equal(true, Valid(fresh), "fresh valid job");
        w.Progression.State.TryAddFoundation("rwom.succubus", DisciplineDomain.Magic);
        Check.Equal(false, Valid(fresh), "late class mutation invalidates book job");
        Check.Equal(2, first.stackCount, "late state mutation pays nothing");
    }
    static JobCondition? ended;
    public static bool SkipFacing() => false;
    public static bool EndBoundary(JobDriver __instance, JobCondition condition)
    { if (__instance is not JobDriver_ClassInitiation) return true; ended = condition; return false; }
    static void Tick(JobDriver_ClassInitiation d, int tick)
    {
        Set(Find.TickManager, "ticksGameInt", tick);
        typeof(JobDriver_ClassInitiation).GetMethod("ReadTick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(d, null);
    }
    static JobDriver_ClassInitiation RoundTrip(RuntimeFixture f, JobDriver_ClassInitiation source, Thing book, string name)
    {
        string path = Path.Combine(f.WorkingDirectory, name + ".xml"); var saved = source;
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "driver"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        JobDriver_ClassInitiation loaded = null!;
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "driver");
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
            loaded.pawn = source.pawn; loaded.job = source.job;
            directory.RegisterLoaded(book);
            directory.RegisterLoaded(source.pawn);
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        loaded.pawn = source.pawn; loaded.job = source.job; return loaded;
    }
    static void Work(RuntimeFixture f)
    {
        using var w = new WarlockProgressionTests.Fixture(f, false);
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        w.Caster.thingIDNumber = 900001;
        w.Caster.rotationTracker = new Pawn_RotationTracker(w.Caster);
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        string id = "rwom.regression.initiation-job." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ClassInitiationJobTests).GetMethod(nameof(EndBoundary))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(JobDriver).GetMethod("EndJobWith")!, prefix, null, null, null]);
            var facing = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ClassInitiationJobTests).GetMethod(nameof(SkipFacing))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_RotationTracker).GetMethod("FaceTarget")!, facing, null, null, null]);
            var book = w.Book(w.Profile.foundationBook); book.thingIDNumber = 900002; var d = Driver(w, book); Check.Equal(true, Valid(d), "initial exact selection");
            Set(d, "useDuration", 3);
            Tick(d, 100); Check.Equal(1, d.WorkTicks, "one actual read tick");
            Tick(d, 100); Check.Equal(1, d.WorkTicks, "same clock tick cannot duplicate work");
            d = RoundTrip(f, d, book, "demon-book-reading");
            Check.Equal("rwom.warlock", d.SelectedProfileId, "native Scribe preserves exact branch");
            Check.Equal(true, Valid(d), "native Scribe resolves exact booked Thing");
            Tick(d, 100); Check.Equal(1, d.WorkTicks, "reload cannot duplicate last worked tick");
            Tick(d, 10000); Check.Equal(2, d.WorkTicks, "elapsed gap contributes only actual work");
            Check.Equal(2, book.stackCount, "unfinished reading pays nothing");
            Tick(d, 10001);
            Check.Equal(true, d.Completed, "final work tick completes selected foundation");
            Check.Equal(JobCondition.Succeeded, ended!.Value, "successful driver boundary");
            Check.Equal(1, book.stackCount, "exactly one book paid");
            d = RoundTrip(f, d, book, "demon-book-completed");
            Check.Equal(true, d.Completed, "native Scribe preserves completed latch");
            Tick(d, 10002); Check.Equal(1, book.stackCount, "completion latch prevents second payment");
            Check.Equal("rwom.warlock", w.Progression.State.Disciplines.Single().ClassId, "fixed branch committed");
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
    }
}

