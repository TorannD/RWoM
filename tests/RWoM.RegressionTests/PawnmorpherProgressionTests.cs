using System.Reflection;
using System.Runtime.CompilerServices;

using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// A contract fixture for Pawnmorpher's race-comp pruning boundary, not a claim
// that the full Workshop mod has run under this harness.
internal static partial class PawnmorpherProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Pawnmorpher.PreservesOwnedProgressionDuringPruning", f => Pruning(f, false));
        yield return new("Pawnmorpher.PreservesProgressionWhenPruningThrows", f => Pruning(f, true));
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RWOM_TEST_PAWNMORPHER")))
        {
            yield return new("Pawnmorpher.InstalledNativePruning", f => Pruning(f, false, true));
            yield return new("Pawnmorpher.NativePawnScribeAfterPruning", NativeSave);
        }
        else
            yield return new("Pawnmorpher.OptionalWhenAbsent", f =>
            {
                var patch = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Compatibility.PawnmorpherRaceCompPatch");
                Check.NotNull(patch, "compatibility guard exists");
                Check.Equal(true, typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Compatibility.PawnmorpherRaceCompPatch") == null,"Content no longer owns vendor guard");
                Check.Equal(false, (bool)patch!.GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!, "absent Pawnmorpher skips patch");
            });
    }

    static void Pruning(RuntimeFixture f, bool fail, bool native = false)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, true);
        var magic = p.magic.MagicData; magic.MagicUserLevel = 17; magic.MagicUserXP = 123; magic.MagicAbilityPoints = 6;
        var might = p.might.MightData; might.MightUserLevel = 12; might.MightUserXP = 91; might.MightAbilityPoints = 4;
        var rank = magic.MagicPowersIF[0]; rank.learned = true; rank.level = 2;
        var progression = new CompClassProgression { parent = p.pawn };
        var knowledge = new AbilityKnowledgeComp { parent = p.pawn };
        p.pawn.AllComps.Add(progression); p.pawn.AllComps.Add(knowledge);
        var unrelated = new UnrelatedComp { parent = p.pawn, props = new CompProperties(typeof(UnrelatedComp)) };
        p.pawn.AllComps.Add(unrelated);
        var target = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        target.comps = new List<CompProperties>();
        if (native) target.comps.Add(new CompProperties(typeof(AddedComp)));
        var id = "rwom.tests.pawnmorpher." + Guid.NewGuid();
        var lib = Assembly.Load("0Harmony");
        var harmony = Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!, id)!;
        var method = native
            ? Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_TEST_PAWNMORPHER")!).GetType("Pawnmorph.Hybrids.RaceShiftUtilities", true)!.GetMethod("AddRemoveDynamicRaceComps", BindingFlags.Static | BindingFlags.NonPublic)!
            : typeof(PawnmorpherProgressionTests).GetMethod(nameof(Prune), BindingFlags.Static | BindingFlags.NonPublic)!;
        if (native)
            Console.WriteLine("Pawnmorpher DLL SHA256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(method.Module.Assembly.Location))));
        var patch = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Compatibility.PawnmorpherRaceCompPatch");
        if (patch != null && native)
        {
            var processor = harmony.GetType().GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
        }
        else if (patch != null)
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [method,
                Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!, patch.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)), null, null,
                Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!, patch.GetMethod("Finalizer", BindingFlags.Static | BindingFlags.NonPublic))]);
        try
        {
            for (int pass = 0; pass < 2; pass++)
            {
                Exception? caught = null;
                try
                {
                    if (native) method.Invoke(null, [p.pawn, target]);
                    else Prune(p.pawn, target, fail);
                }
                catch (InvalidOperationException e) { caught = e; }
                Check.Equal(fail, caught != null, "original exception is not swallowed");
                Check.Equal(true, p.pawn.AllComps.Contains(p.magic), "original magic component survives race pruning");
                Check.Equal(true, p.pawn.AllComps.Contains(p.might), "original might component survives race pruning");
                Check.Equal(true, p.pawn.AllComps.Contains(progression), "beta progression owner survives");
                Check.Equal(true, p.pawn.AllComps.Contains(knowledge), "knowledge owner survives");
                Check.Equal(false, p.pawn.AllComps.Contains(unrelated), "legitimate race pruning still runs");
                Check.Equal(native ? 5 : 4, p.pawn.AllComps.Count, "no duplicate owners after repeated pruning");
                if (native) Check.Equal(1, p.pawn.AllComps.OfType<AddedComp>().Count(), "native reconciliation still adds legitimate new race component once");
                Check.Same(p.magic, p.pawn.GetCompAbilityUserMagic(), "lookup resolves retained owner");
                Check.Equal(17, magic.MagicUserLevel, "leveled magic retained");
                Check.Equal(123, magic.MagicUserXP, "magic XP retained");
                Check.Equal(6, magic.MagicAbilityPoints, "unspent magic points retained");
                Check.Equal(12, might.MightUserLevel, "leveled might retained");
                Check.Equal(91, might.MightUserXP, "might XP retained");
                Check.Equal(4, might.MightAbilityPoints, "might points retained");
                Check.Equal(2, rank.level, "spell rank retained");
                Check.Equal(true, rank.learned, "learned flag retained");
            }
        }
        finally { harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Prune(Pawn pawn, ThingDef newRace, bool fail)
    {
        // The destructive selection used by upstream for null dynamically
        // injected properties and plain CompProperties (commit 551232a9).
        foreach (var comp in pawn.AllComps.ToArray())
            if (!newRace.comps.Any(p => ReferenceEquals(p, comp.props) ||
                (p != null && comp.props != null && p.GetType() == typeof(CompProperties) &&
                 comp.props.GetType() == typeof(CompProperties) && p.compClass == comp.props.compClass)))
                pawn.AllComps.Remove(comp);
        if (fail) throw new InvalidOperationException("race update failed after removal");
    }

    sealed class UnrelatedComp : ThingComp { }
    public sealed class AddedComp : ThingComp { }
}
