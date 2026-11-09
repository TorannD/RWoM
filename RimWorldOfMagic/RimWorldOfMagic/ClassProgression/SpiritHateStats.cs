using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Reuse the possession stage as a receipt; never duplicate the soul's hate reservoir.
    [HarmonyPatch(typeof(HediffStatsUtility), nameof(HediffStatsUtility.GetStatOffsetForSeverity))]
    internal static class SpiritHateStats
    {
        private static void Postfix(StatDef stat, HediffStage stage, Pawn pawn, ref float __result)
        {
            var possessionDef = TorannMagicDefOf.TM_SpiritPossessionHD;
            if (stage == null || possessionDef?.stages?.Contains(stage) != true || pawn?.health == null) return;
            var receipt = pawn.health.hediffSet.GetFirstHediffOfDef(possessionDef);
            if (receipt?.CurStage != stage || TM_Calc.GetHateHediff(pawn) != null) return;
            var owner = DeathKnightMartialPath.PassiveOwner(pawn);
            if (owner == null || owner == pawn) return;
            var hate = TM_Calc.GetHateHediff(owner);
            if (hate?.CurStage == null || hate.CurStage == stage) return;
            __result += HediffStatsUtility.GetStatOffsetForSeverity(stat, hate.CurStage, owner, hate.Severity);
        }
    }
}
