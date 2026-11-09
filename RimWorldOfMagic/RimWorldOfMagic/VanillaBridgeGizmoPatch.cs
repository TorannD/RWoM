using AbilityUser;
using HarmonyLib;
using Verse;

namespace TorannMagic
{
    // Ф5 phase Б: when the vanilla tracker provides the bridge gizmo for a
    // spell (family enabled + bridge def exists), the classic Command_Pawn
    // Ability gizmo hides — one gizmo per spell, cast path unchanged (the
    // bridge delegates into the same JecsLite owner).
    [HarmonyPatch(typeof(PawnAbility), nameof(PawnAbility.ShouldShowGizmo))]
    internal static class VanillaBridgeGizmoVisibilityPatch
    {
        [System.ThreadStatic] private static PawnAbility trackerQuery;

        internal static bool VisibleThroughTracker(PawnAbility owner)
        {
            var previous = trackerQuery;
            trackerQuery = owner;
            try { return owner.ShouldShowGizmo(); }
            finally { trackerQuery = previous; }
        }

        static void Postfix(PawnAbility __instance, ref bool __result)
        {
            if (!__result || ReferenceEquals(__instance, trackerQuery)) return;
            if (VanillaBridgeSync.ShouldHideLegacyGizmo(__instance.AbilityUser?.Pawn, __instance.Def as TMAbilityDef))
            {
                __result = false;
            }
        }
    }
}
