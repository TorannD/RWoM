using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.Utils;
using Verse;

namespace RWoM.RegressionTests;

// Editor-made pawns and pawns constructed outside the vanilla generator can reach
// a class grant (tome, ritual) without ability comps, and lookups made before the
// spawn-time comp injection cached a permanent "no comp" sentinel in TM_Calc —
// the pawn ended up with a class trait but no mana and no grimoire tab.
internal static class CompBootstrapTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("CompCache.PoisonedSentinelNeedsInvalidation", SentinelContract);
        yield return new("Tome.EnsureClassCompsCreatesMissingComps", EnsureComps);
        yield return new("PawnTracker.SyncAfterCompInitToleratesStorylessPawn", SyncToleratesGeneration);
        yield return new("LevelUp.GrantsExactlyOnePoint", LevelUpGrantsOnePoint);
    }

    // Users reported +2 skill points per level-up. The only grant site is the
    // MagicUserLevel/MightUserLevel setters; pin one point per LevelUp call for
    // both sides so a regression (or a mod hooking these) is identifiable.
    private static void LevelUpGrantsOnePoint(RuntimeFixture fixture)
    {
        var pawn = BarePawn();
        var comp = new CompAbilityUserMagic { parent = pawn };
        pawn.AllComps.Add(comp);
        var data = new MagicData(comp);
        typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(comp, data);

        comp.LevelUp(true);
        Check.Equal(1, data.MagicUserLevel, "one LevelUp call raises the level by one");
        Check.Equal(1, data.MagicAbilityPoints, "one LevelUp call grants exactly one skill point");

        comp.LevelUp(true);
        Check.Equal(2, data.MagicUserLevel, "second LevelUp call raises the level to two");
        Check.Equal(2, data.MagicAbilityPoints, "second LevelUp call grants exactly one more skill point");

        var mightPawn = BarePawn();
        var mightComp = new CompAbilityUserMight { parent = mightPawn };
        mightPawn.AllComps.Add(mightComp);
        var mightData = new MightData(mightComp);
        typeof(CompAbilityUserMight).GetField("mightData", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(mightComp, mightData);

        mightComp.LevelUp(true);
        Check.Equal(1, mightData.MightUserLevel, "might level rises by one");
        Check.Equal(1, mightData.MightAbilityPoints, "might side grants exactly one skill point per level");
    }

    private static void SyncToleratesGeneration(RuntimeFixture fixture)
    {
        // The InitializeComps postfix also fires from PostMake during pawn
        // generation, where story does not exist yet (crashed dev-settlement
        // startup). SyncAfterCompInit must be a no-op there while still
        // clearing the TM_Calc lookup sentinel.
        var generating = new Pawn();
        TM_PawnTracker.SyncAfterCompInit(generating);

        var pawn = new Pawn();
        typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(pawn, new List<ThingComp>());
        Check.Same(null, pawn.GetCompAbilityUserMagic(), "no-comp sentinel cached by an early lookup");
        var comp = new CompAbilityUserMagic { parent = pawn };
        pawn.AllComps.Add(comp);
        TM_PawnTracker.SyncAfterCompInit(pawn);
        Check.Same(comp, pawn.GetCompAbilityUserMagic(),
            "SyncAfterCompInit clears the sentinel even without story");
    }

    private static Pawn BarePawn()
    {
        var pawn = new Pawn();
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(pawn, new List<ThingComp>());
        return pawn;
    }

    private static void SentinelContract(RuntimeFixture fixture)
    {
        var pawn = BarePawn();
        Check.Same(null, pawn.GetCompAbilityUserMagic(), "pawn without comps initially resolves to null");

        // The framework injects comps at spawn (ThingWithComps.InitializeComps postfix);
        // without a cache invalidation the earlier lookup would keep hiding the comp.
        var comp = new CompAbilityUserMagic { parent = pawn };
        pawn.AllComps.Add(comp);
        Check.Same(null, pawn.GetCompAbilityUserMagic(), "stale sentinel hides a runtime-injected comp");

        TM_Calc.InvalidateCompCache(pawn);
        Check.Same(comp, pawn.GetCompAbilityUserMagic(), "invalidation re-resolves the injected comp");
    }

    private static void EnsureComps(RuntimeFixture fixture)
    {
        var pawn = BarePawn();
        TM_PawnTracker.EnsureClassComps(pawn);

        var magic = pawn.TryGetComp<CompAbilityUserMagic>();
        var might = pawn.TryGetComp<CompAbilityUserMight>();
        Check.NotNull(magic, "EnsureClassComps adds the magic comp");
        Check.NotNull(might, "EnsureClassComps adds the might comp");
        Check.Same(magic, pawn.GetCompAbilityUserMagic(), "cache resolves the freshly created magic comp");

        int afterFirst = pawn.AllComps.Count;
        TM_PawnTracker.EnsureClassComps(pawn);
        Check.Equal(afterFirst, pawn.AllComps.Count, "EnsureClassComps is idempotent");
        Check.Equal(1, pawn.AllComps.Count(c => c is CompAbilityUserMagic), "exactly one magic comp");
        Check.Equal(1, pawn.AllComps.Count(c => c is CompAbilityUserMight), "exactly one might comp");
    }
}
