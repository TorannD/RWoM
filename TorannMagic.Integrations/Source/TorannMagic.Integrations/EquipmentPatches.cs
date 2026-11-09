using HarmonyLib;
using TorannMagic.Compatibility;
using Verse;

namespace TorannMagic.Integrations
{
    [HarmonyPatch(typeof(EquipmentCompatibility), nameof(EquipmentCompatibility.MountingAvailable))]
    internal static class MountingAvailablePatch { static void Postfix(ref bool __result) => __result |= GiddyUpAdapter.Available; }
    [HarmonyPatch(typeof(EquipmentCompatibility), nameof(EquipmentCompatibility.WeaponMemoryAvailable))]
    internal static class WeaponMemoryAvailablePatch { static void Postfix(ref bool __result) => __result |= SidearmsAdapter.Available; }
    [HarmonyPatch(typeof(EquipmentCompatibility), nameof(EquipmentCompatibility.ForceDismount))]
    internal static class DismountPatch { static void Postfix(Pawn pawn) => GiddyUpAdapter.ForceDismount(pawn); }
    [HarmonyPatch(typeof(EquipmentCompatibility), nameof(EquipmentCompatibility.IsMount))]
    internal static class IsMountPatch { static void Postfix(Pawn pawn, ref bool __result) => __result |= GiddyUpAdapter.IsMount(pawn); }
    [HarmonyPatch(typeof(EquipmentCompatibility), nameof(EquipmentCompatibility.GetMount))]
    internal static class GetMountPatch { static void Postfix(Pawn rider, ref Pawn __result) { if (__result == null) __result = GiddyUpAdapter.GetMount(rider); } }
    [HarmonyPatch(typeof(EquipmentCompatibility), nameof(EquipmentCompatibility.ClearWeaponMemory))]
    internal static class ClearWeaponMemoryPatch { static void Postfix(Pawn pawn) => SidearmsAdapter.ClearWeaponMemory(pawn); }
}
