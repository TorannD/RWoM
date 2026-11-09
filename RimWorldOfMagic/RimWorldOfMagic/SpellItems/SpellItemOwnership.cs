using HarmonyLib;
using Verse;

namespace TorannMagic.SpellItems
{
    [HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.Notify_ItemRemoved))]
    public static class SpellItemInventoryRemovalPatch
    {
        public static void Postfix(Thing item)
        {
            SpellItemCasting.Comp(item)?.InvalidateCast();
        }
    }
}
