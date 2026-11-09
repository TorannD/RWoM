using Verse;
namespace TorannMagic.ModCheck
{
    public static class GiddyUp
    {
        public static void ForceDismount(Pawn pawn) => Compatibility.EquipmentCompatibility.ForceDismount(pawn);
        public static bool IsMount(Pawn pawn) => Compatibility.EquipmentCompatibility.IsMount(pawn);
        public static Pawn GetMount(Pawn rider) => Compatibility.EquipmentCompatibility.GetMount(rider);
    }
}