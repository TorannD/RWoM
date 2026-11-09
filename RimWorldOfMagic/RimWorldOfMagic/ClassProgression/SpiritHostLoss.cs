using HarmonyLib;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostLoss
    {
        // Only the exact pawn owned by the possession can identify a ritual Spirit.
        internal static bool IsMarkedHost(Pawn host)
            => TorannMagicDefOf.TM_SpiritPossessionHD != null
                && Compatibility.TM_SpiritCommands.Possession(host)?.SpiritPawn is Pawn spirit
                && SpiritRebirthState.TryGetStored(spirit,out _);

        internal static bool Secure(Pawn host, bool forced)
        {
            var keeper = host.Map?.GetComponent<MapComponent_SpiritCollapse>();
            var original = Compatibility.TM_SpiritCommands.Possession(host)?.SpiritPawn;
            return keeper != null && original != null && keeper.TryBeginHostLoss(original, host, forced);
        }
    }

    // Kill can be vetoed; SetDead is the native commit point before corpse creation.
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead))]
    internal static class SpiritHostSetDeadPatch
    {
        static void Postfix(Pawn ___pawn, bool __runOriginal)
        {
            if (!__runOriginal || !___pawn.Spawned || !___pawn.Dead || !SpiritHostLoss.IsMarkedHost(___pawn)) return;
            var possession = Compatibility.TM_SpiritCommands.Possession(___pawn);
            if (SpiritHostLoss.Secure(___pawn, false) && possession.SpiritPawn == null)
                ___pawn.health.RemoveHediff(possession.parent);
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    internal static class SpiritHostRemoveHediffPatch
    {
        [HarmonyPriority(Priority.Last)]
        static bool Prefix(Pawn ___pawn, Hediff hediff)
        {
            if (!___pawn.Spawned || hediff?.def != TorannMagicDefOf.TM_SpiritPossessionHD
                || !SpiritHostLoss.IsMarkedHost(___pawn) || !ReferenceEquals(Compatibility.TM_SpiritCommands.Possession(___pawn)?.parent, hediff))
                return true;
            return SpiritHostLoss.Secure(___pawn, true);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
    internal static class SpiritHostDestroyPatch
    {
        [HarmonyPriority(Priority.Last)]
        static bool Prefix(Pawn __instance)
        {
            if (!__instance.def.destroyable && !Thing.allowDestroyNonDestroyable) return true;
            if (SpiritSummonedHost.IsSummoned(__instance))
                return (!ClassProgressionFeature.Enabled && !SpiritHostLoss.IsMarkedHost(__instance)) || SpiritSummonedHost.CanDismiss(__instance);
            return !__instance.Spawned || !SpiritHostLoss.IsMarkedHost(__instance) || SpiritHostLoss.Secure(__instance, true);
        }
    }
}
