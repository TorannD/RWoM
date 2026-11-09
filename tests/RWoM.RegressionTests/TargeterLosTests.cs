using System.Reflection;
using RimWorld;
using AbilityUser;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// Live report (card 120): Blink required line of sight only when directed by
// the player. The ability targeter demands sight to the center cell whenever
// the verb reports needLOSToCenter, and the JecsLite verb hardcodes it true,
// so no-sight spells (requiresLineOfSight=false, e.g. Blink) were gated in
// the player targeting UI while AI paths bypassed the targeter. The
// production postfix mirrors the spell's own requireLineOfSight flag back
// into needLOSToCenter; these cases pin the flag contract.
internal static class TargeterLosTests
{
    private static readonly BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("TargeterLos.NoSightSpellReleasesTargeterFlag", NoSightSpellReleasesTargeterFlag);
        yield return new("TargeterLos.SightSpellKeepsTargeterFlag", SightSpellKeepsTargeterFlag);
    }

    private static Verb_UseAbility LosVerb(bool requireLineOfSight)
    {
        var verb = new Verb_UseAbility();
        var props = new VerbProperties_Ability();
        props.verbClass = typeof(Verb_UseAbility);
        // The vanilla field drives cast-time LOS (PreCastShotCheck,
        // CanHitCellFromCellIgnoringRange); the postfix mirrors it into the
        // targeter flag.
        props.requireLineOfSight = requireLineOfSight;
        typeof(Verb).GetField("verbProps", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .SetValue(verb, props);
        return verb;
    }

    private static void RunPostfix(Verb_UseAbility verb, ref bool needLOSToCenter)
    {
        var args = new object?[] { verb, needLOSToCenter };
        typeof(TorannMagicMod).GetMethod("VerbUseAbility_TargeterLOS_Postfix", Flags)!.Invoke(null, args);
        needLOSToCenter = (bool)args[1]!;
    }

    private static void NoSightSpellReleasesTargeterFlag(RuntimeFixture f)
    {
        var verb = LosVerb(requireLineOfSight: false);
        bool needLOSToCenter = true; // the JecsLite override hardcodes this
        RunPostfix(verb, ref needLOSToCenter);
        Check.Equal(false, needLOSToCenter,
            "no-sight spells must not demand line of sight in the player targeting UI");
    }

    private static void SightSpellKeepsTargeterFlag(RuntimeFixture f)
    {
        var verb = LosVerb(requireLineOfSight: true);
        bool needLOSToCenter = true;
        RunPostfix(verb, ref needLOSToCenter);
        Check.Equal(true, needLOSToCenter,
            "sight-requiring spells keep the targeter's line-of-sight gate");
    }
}
