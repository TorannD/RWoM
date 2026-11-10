using HarmonyLib;

namespace TorannMagic.Integrations
{
    [HarmonyPatch(typeof(TMPCompat), nameof(TMPCompat.MultiplayerActive))]
    internal static class MultiplayerActiveFacadePatch
    {
        static void Postfix(ref bool __result) => __result = TMPCompatAdapter.MultiplayerActive();
    }
    [HarmonyPatch(typeof(TMPCompat), nameof(TMPCompat.Initialized), MethodType.Getter)]
    internal static class MultiplayerInitializedFacadePatch
    {
        static void Postfix(ref bool __result) => __result = TMPCompatAdapter.Initialized;
    }
    [HarmonyPatch(typeof(TMPCompat), nameof(TMPCompat.Setup))]
    internal static class MultiplayerSetupFacadePatch
    {
        static void Postfix() => TMPCompatAdapter.Setup();
    }
}
