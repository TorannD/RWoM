using System.Reflection;
using System.Runtime.CompilerServices;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static class MagicShieldTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MagicShield.UnspawnedExpiresNormally", _ => Expires());
        yield return new("MagicShield.UnspawnedSeverityLossSkipsEffects", _ => SeverityLoss());
    }

    private static HediffComp_MagicShield Create()
    {
        // No map or graphics services: this is the world-pawn state from the report.
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "ShieldWorldPawn";
        def.size = new IntVec2(1, 1);
        var pawn = new Pawn { def = def };
        var hediff = new HediffWithComps { pawn = pawn };
        return new HediffComp_MagicShield { parent = hediff };
    }

    private static void Expires()
    {
        var shield = Create();
        float adjustment = 0;
        for (int tick = 0; tick < 2699; tick++)
        {
            shield.CompPostTick(ref adjustment);
            Check.Equal(0f, adjustment, "shield must not expire early off-map");
        }
        Check.Equal(1f, shield.Energy, "world pawn timer must advance");
        shield.CompPostTick(ref adjustment);
        Check.Equal(-10f, adjustment, "normal removal requested at expiry");
        Check.Equal(0f, shield.Energy, "expired shield energy");
    }

    private static void SeverityLoss()
    {
        var shield = Create();
        // Simulate a recorded hit between ticks; avoid a real damage/map pipeline.
        typeof(HediffComp_MagicShield).GetField("lastSev", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(shield, 1f);
        float adjustment = 0;
        shield.CompPostTick(ref adjustment);
        Check.Equal(2699f, shield.Energy, "hit visuals must not interrupt world pawn ticking");
        Check.Equal(0f, adjustment, "hit must not force early expiry");
    }
}
