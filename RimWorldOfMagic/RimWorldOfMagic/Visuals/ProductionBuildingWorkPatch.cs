using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Visuals
{
    // The native work tick is also used by loaded vanilla worktable instances. Keeping the
    // original Thing classes avoids requiring players to rebuild existing production tables.
    [HarmonyPatch(typeof(Building_WorkTable), nameof(Building_WorkTable.UsedThisTick))]
    public static class ProductionBuildingWorkPatch
    {
        public static void Postfix(Building_WorkTable __instance)
        {
            __instance.GetComp<CompProductionBuildingEffects>()?.NotifyWork(Find.TickManager.TicksGame);
        }
    }
}
