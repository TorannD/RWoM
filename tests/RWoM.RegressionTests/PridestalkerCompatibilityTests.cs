using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class PridestalkerCompatibilityTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("PridestalkerCompatibility.NativeMenuHasOneWorkingDegree", Menu);
        yield return new("PridestalkerCompatibility.LegacyConstructionAndUnknownDegrees", Construction);
        yield return new("PridestalkerCompatibility.LegacySaveKeepsProgression", Saved);
    }

    static string CandidateRoot(RuntimeFixture f) => Environment.GetEnvironmentVariable("RWOM_KURE_CANDIDATE")
        ?? Path.GetFullPath(Path.Combine(f.Repository, "../artifacts/kure-pridestalker/candidate"));

    static TraitDef Definition(RuntimeFixture f)
    {
        string root = CandidateRoot(f);
        var source = XDocument.Load(Path.Combine(root, "1.7/Defs/RFSTraits.xml")).Root!.Elements()
            .Single(x => (string?)x.Element("defName") == "TM_Pridestalker");
        return new TraitDef { defName = "TM_Pridestalker", degreeDatas = source.Element("degreeDatas")!.Elements().Select(x =>
            new TraitDegreeData { degree = (int)x.Element("degree")!, label = (string)x.Element("label")! }).ToList() };
    }

    static void Menu(RuntimeFixture f) => Menu(f, false);
    internal static void Menu(RuntimeFixture f, bool sdk)
    {
        var old = DefDatabase<TraitDef>.AllDefsListForReading.ToArray();
        var classes = TM_ClassUtility.CustomClasses;
        try
        {
            var trait = Definition(f);
            DefDatabase<TraitDef>.Clear(); DefDatabase<TraitDef>.Add(trait);
            var menu = (System.Collections.ICollection)typeof(DebugToolsPawns).GetMethod("GiveTrait", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
            Check.Equal(1, menu.Count, "native GiveTrait must not offer an unregistered duplicate degree");
            Check.Equal(4, trait.degreeDatas.Single().degree, "existing degree4 saves keep their canonical degree");
            var row = XDocument.Load(Path.Combine(CandidateRoot(f), "Common/Patches/RFS_CustomClasses.xml")).Descendants("li")
                .Single(x => (string?)x.Element("classTrait") == "TM_Pridestalker");
            Check.Equal(true, row.Element("classMageAbilities")!.Elements().Any(), "canonical mage ability declarations retained");
            Check.Equal(true, row.Element("classFighterAbilities")!.Elements().Any(), "canonical fighter ability declarations retained");
            TM_ClassUtility.CustomClasses = [new TM_CustomClass { classTrait = trait, traitDegree = (int)row.Element("traitDegree")!,
                isMage = (bool)row.Element("isMage")!, isFighter = (bool)row.Element("isFighter")! }];
            var p = PawnWithComps(false, false);
            p.pawn.story.traits.allTraits.Add(new Trait(trait, trait.degreeDatas.Single().degree));
            Check.Equal(1, TM_ClassState.CurrentCustomClasses(p.pawn, true).Count, "menu choice resolves mage class");
            Check.Equal(1, TM_ClassState.CurrentCustomClasses(p.pawn, false).Count, "menu choice resolves fighter class");
            if (sdk)
            {
                var result = CoreSdkTests.Rows(p.pawn).Single();
                Check.Equal("legacy.TM_Pridestalker.4", result.GetType().GetProperty("Id")!.GetValue(result), "canonical Kure trait and degree");
                Check.Equal("Classic", result.GetType().GetProperty("Stage")!.GetValue(result)!.ToString(), "classic has no initiation stage");
                Check.Equal(3, Convert.ToInt32(result.GetType().GetProperty("Kind")!.GetValue(result)), "both domains in one row");
            }
        }
        finally { DefDatabase<TraitDef>.Clear(); foreach (var d in old) DefDatabase<TraitDef>.Add(d); TM_ClassUtility.CustomClasses = classes; }
    }

    static Trait New(TraitDef def, int degree) => (Trait)typeof(Trait).GetConstructor([typeof(TraitDef), typeof(int), typeof(bool)])!.Invoke([def, degree, true]);

    static void Construction(RuntimeFixture f)
    {
        using var hooks = new Hooks();
        var def = Definition(f);
        var trait = New(def, 0);
        Check.Equal(4, trait.Degree, "new legacy degree0 grants resolve canonical class");
        Check.Equal(true, trait.ScenForced, "normalization preserves forced trait state");
        Check.Equal(4, New(def, 4).Degree, "canonical grants unchanged");
        Check.Equal(2, New(def, 2).Degree, "unknown degree not guessed");
        Check.Equal(0, New(new TraitDef { defName = "OtherHybrid", degreeDatas = def.degreeDatas }, 0).Degree, "other classes untouched");
        Check.Equal(0, New(new TraitDef { defName = "TM_Pridestalker", degreeDatas = [new TraitDegreeData { degree = 0 }] }, 0).Degree, "no canonical definition means no migration");
    }

    public sealed class Envelope : IExposable
    {
        public Trait trait = null!;
        public MagicData magic = null!;
        public MightData might = null!;
        public void ExposeData() { Scribe_Deep.Look(ref trait, "trait"); Scribe_Deep.Look(ref magic, "magic"); Scribe_Deep.Look(ref might, "might"); }
    }

    static void Saved(RuntimeFixture f)
    {
        var traits=DefDatabase<TraitDef>.AllDefsListForReading.ToArray();
        try { SavedCase(f); }
        finally { DefDatabase<TraitDef>.Clear(); foreach(var trait in traits) DefDatabase<TraitDef>.Add(trait); }
    }

    static void SavedCase(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var def = Definition(f);
        // Earlier custom-class cases may have a stub under this same defName.
        // Scribe must resolve this case's canonical definition, not that stub.
        var others=DefDatabase<TraitDef>.AllDefsListForReading.Where(d=>d.defName!=def.defName).ToArray();
        DefDatabase<TraitDef>.Clear(); foreach(var other in others) DefDatabase<TraitDef>.Add(other);
        DefDatabase<TraitDef>.Add(def);
        var p = PawnWithComps(true, true);
        p.magic.MagicData.MagicUserLevel = 9;
        p.magic.MagicData.MagicUserXP = 12345;
        p.magic.MagicData.MagicAbilityPoints = 7;
        p.magic.MagicData.MagicPowersA[0].level = 2;
        p.magic.MagicData.MagicPowersA[0].learned = true;
        Set(p.magic.MagicData, "magicPawn", null);
        p.might.MightData.MightUserLevel = 6;
        p.might.MightData.MightUserXP = 6543;
        p.might.MightData.MightAbilityPoints = 3;
        Set(p.might.MightData, "mightPawn", null);
        var envelope = new Envelope { trait = new Trait(def, 0, true), magic = p.magic.MagicData, might = p.might.MightData };
        envelope.trait.suppressedByTrait = true;
        string path = Path.Combine(f.WorkingDirectory, "legacy-pridestalker.xml");
        Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "payload"); Scribe.saver.FinalizeSaving(); Scribe.ForceStop();
        using var hooks = new Hooks();
        for (int i = 0; i < 2; i++)
        {
            envelope = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "payload"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Check.Equal(4, envelope.trait.Degree, "saved degree0 normalized before loaded class lookup");
            Check.Equal(true, envelope.trait.ScenForced && envelope.trait.suppressedByTrait, "trait flags preserved");
            Check.Equal(9, envelope.magic.MagicUserLevel, "no level reset or reroll");
            Check.Equal(12345, envelope.magic.MagicUserXP, "XP preserved");
            Check.Equal(7, envelope.magic.MagicAbilityPoints, "no points awarded");
            Check.Equal(2, envelope.magic.MagicPowersA[0].level, "purchased rank retained");
            Check.Equal(true, envelope.magic.MagicPowersA[0].learned, "learned state retained");
            Check.Equal(6, envelope.might.MightUserLevel, "hybrid might level preserved");
            Check.Equal(6543, envelope.might.MightUserXP, "hybrid might XP preserved");
            Check.Equal(3, envelope.might.MightAbilityPoints, "hybrid might points preserved");
            Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "payload"); Scribe.saver.FinalizeSaving(); Scribe.ForceStop();
        }
    }

    internal sealed class Hooks : IDisposable
    {
        readonly object harmony;
        readonly string id = "rwom.pridestalker." + Guid.NewGuid();
        public Hooks()
        {
            var type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
            harmony = Activator.CreateInstance(type, id)!;
            foreach (string name in new[] { "PridestalkerTraitConstructorPatch", "PridestalkerTraitLoadPatch", "PridestalkerRosterDegreePatch" })
            {
                var patch = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Compatibility." + name);
                if (patch == null) continue; // Red run exercises unpatched production.
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}
