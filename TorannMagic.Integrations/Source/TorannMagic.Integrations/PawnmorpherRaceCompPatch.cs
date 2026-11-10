using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic.Compatibility
{
    // Pawnmorpher reconciles AllComps with the new race's declared properties.
    // JecsTools ability components are injected dynamically (null properties),
    // so that reconciliation removes their progression, not just cached runtime.
    [HarmonyPatch]
    internal static class PawnmorpherRaceCompPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.TypeByName("Pawnmorph.Hybrids.RaceShiftUtilities"),
            "AddRemoveDynamicRaceComps", new[] { typeof(Pawn), typeof(ThingDef) });

        private static bool Prepare() => TargetMethod() != null;

        private static bool IsProgressionOwner(ThingComp comp) =>
            comp is CompAbilityUserMagic || comp is CompAbilityUserMight ||
            comp is CompClassProgression || comp is AbilityKnowledgeComp;

        private static void Prefix(Pawn pawn, out List<ThingComp> __state)
        {
            __state = pawn?.AllComps?.Where(IsProgressionOwner).ToList();
        }

        // A finalizer also protects already-removed owners when a different race
        // component throws. It deliberately does not swallow that exception.
        private static void Finalizer(Pawn pawn, List<ThingComp> __state)
        {
            if (pawn == null || __state == null || __state.Count == 0) return;
            var comps = pawn.AllComps;
            foreach (var owner in __state)
            {
                if (owner.parent != pawn) continue;
                // Keep the original object: copying only level/XP would lose
                // ranks, cooldowns, summons, equipment holders and beta state.
                comps.RemoveAll(comp => !ReferenceEquals(comp, owner) && comp.GetType() == owner.GetType());
                if (!comps.Contains(owner)) comps.Add(owner);
            }
            TM_Calc.InvalidateCompCache(pawn);
        }
    }
}
