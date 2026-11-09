using HarmonyLib;
using Verse;

namespace TorannMagic.ClassProgression
{
    // A living incomplete ritual result has an accounting owner, not an active
    // body. Consume its native tick without running human jobs/genes/equipment.
    // The existing comp/need receipts deduplicate every legitimate native caller.
    [HarmonyPatch(typeof(Pawn), "Tick")]
    internal static class SpiritRebirthPendingTickPatch
    {
        static bool Prefix(Pawn __instance)
        {
            if (!SpiritRebirthRitualTransaction.IsPendingAccountingOwner(__instance)
                && !MapComponent_SpiritCollapse.IsPendingAccountingOwner(__instance)) return true;
            __instance.GetCompAbilityUserMagic()?.CompTick();
            __instance.GetCompAbilityUserMight()?.CompTick();
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn), "TickInterval")]
    internal static class SpiritRebirthPendingIntervalPatch
    {
        static bool Prefix(Pawn __instance)
            => !SpiritRebirthRitualTransaction.IsPendingAccountingOwner(__instance)
                && !MapComponent_SpiritCollapse.IsPendingAccountingOwner(__instance);
    }
}
