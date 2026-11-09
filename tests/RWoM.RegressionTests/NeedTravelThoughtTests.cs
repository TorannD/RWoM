using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// The travel-need thought used to throw NotImplementedException for the satisfied
// "Complacent" middle band — a red-error loop for every traveller in range. The
// band is now inactive, matching the wanderlust/adventuring stages around it.
internal static class NeedTravelThoughtTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("NeedTravel.ComplacentIsInactiveNoThrow", _ => Band(.5f, active: false, stage: 0));
        yield return new("NeedTravel.WanderlustActive", _ => Band(.1f, active: true, stage: 0));
        yield return new("NeedTravel.AdventuringActive", _ => Band(.9f, active: true, stage: 1));
    }

    static void Band(float level, bool active, int stage)
    {
        var pawn = new Pawn();
        pawn.needs = new Pawn_NeedsTracker(pawn);
        var def = new NeedDef { defName = "TM_Travel", needClass = typeof(Need_Travel) };
        var need = (Need_Travel)RuntimeHelpers.GetUninitializedObject(typeof(Need_Travel));
        need.def = def;
        pawn.needs.AllNeeds.Add(need);
        need.CurLevel = level; // MaxLevel is 1; the setter clamps
        NeedDef oldNeed = TorannMagicDefOf.TM_Travel;
        TorannMagicDefOf.TM_Travel = def;
        try
        {
            var worker = (ThoughtWorker)RuntimeHelpers.GetUninitializedObject(typeof(TorannMagic.Thoughts.ThoughtWorker_TM_NeedTravel));
            var method = typeof(ThoughtWorker).GetMethod("CurrentStateInternal",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var state = (ThoughtState)method.Invoke(worker, new object?[] { pawn })!;
            Check.Equal(active, state.Active, $"travel need {level:P0} thought activity");
            if (active)
                Check.Equal(stage, state.StageIndex, $"travel need {level:P0} thought stage");
        }
        finally { TorannMagicDefOf.TM_Travel = oldNeed; }
    }
}
