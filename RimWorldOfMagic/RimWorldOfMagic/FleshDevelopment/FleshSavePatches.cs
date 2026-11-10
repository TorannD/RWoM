using HarmonyLib;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    internal static class FleshSavePatches
    {
        private static void Postfix(Pawn __instance) => FleshRepository.Expose(__instance);
    }
}
