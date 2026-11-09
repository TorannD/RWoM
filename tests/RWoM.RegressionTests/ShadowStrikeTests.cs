using System.Reflection;
using TorannMagic;
using TorannMagic.Compatibility;
using TorannMagic.ModOptions;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    static int shadowMoveWarnings;
    static IEnumerable<TestCase> ShadowStrikeCases()
    {
        foreach (bool previous in new[] { false, true })
            yield return new("ShadowStrike.MoveFailureReportedAndFlightFlagRestored." + previous, f => World(f, w =>
            {
                var projectile = new Projectile_ShadowStrike();
                Set(projectile, "caster", w.Caster);
                Check.Equal(true, w.Caster.IsColonist, "exercise colonist flight flag branch");
                var original = Constants.GetPawnInFlight();
                using var patches = new Patches();
                patches.Prefix(typeof(Pawn).GetMethod(nameof(Pawn.DeSpawn))!, nameof(ShadowFailDespawn));
                patches.Prefix(typeof(TM_Log).GetMethod(nameof(TM_Log.Warning), [typeof(string), typeof(string)])!, nameof(ShadowCaptureWarning));
                try
                {
                    Constants.SetPawnInFlight(previous); shadowMoveWarnings = 0;
                    var origin = w.Caster.Position;
                    Check.Equal(false, projectile.DoMove(w.Patient), "failed move cannot report success");
                    Check.Equal(previous, Constants.GetPawnInFlight(), "temporary flag restored on failure");
                    Check.Equal(1, shadowMoveWarnings, "movement exception no longer swallowed");
                    Check.Equal(true, w.Caster.Spawned, "pre-despawn failure leaves pawn on map");
                    Check.Equal(origin, w.Caster.Position, "failure leaves original position");
                }
                finally { Constants.SetPawnInFlight(original); }
            }));
    }
    static bool ShadowFailDespawn() => throw new InvalidOperationException("injected Shadow Strike move failure");
    static bool ShadowCaptureWarning(string category, string message)
    {
        if (category != "ShadowStrike" || !message.Contains("injected Shadow Strike move failure")) return true;
        shadowMoveWarnings++; return false;
    }
}
