using System.Runtime.CompilerServices;
using Verse;

namespace TorannMagic.Compatibility
{
    // Optional adapters own mounting and remembered-weapon behavior.
    public static class EquipmentCompatibility
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool MountingAvailable() => false;
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool WeaponMemoryAvailable() => false;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ForceDismount(Pawn pawn) { }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool IsMount(Pawn pawn) => false;
        [MethodImpl(MethodImplOptions.NoInlining)] public static Pawn GetMount(Pawn rider) => null;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ClearWeaponMemory(Pawn pawn) { }
    }
}
