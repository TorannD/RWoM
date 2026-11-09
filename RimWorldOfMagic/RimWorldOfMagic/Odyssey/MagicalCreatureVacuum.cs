using HarmonyLib;
using RimWorld;
using TorannMagic.Golems;
using Verse;

namespace TorannMagic.Odyssey
{
    public static class MagicalCreatureVacuum
    {
        // Read current state: no cached pawn classifications or new save fields.
        public static bool IsNaturallyVacuumImmune(Pawn pawn)
        {
            if (!ModsConfig.OdysseyActive || pawn == null) return false;
            if (ClassProgression.SpiritRebirthFormPolicy.IsEthereal(pawn)) return true;
            if (pawn is TMPawnGolem || (pawn.def != null && pawn.def == TorannMagicDefOf.TM_SpiritTD)) return true;
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            foreach (var hediff in hediffs)
            {
                if (hediff.def == null) continue;
                if (hediff.def == TorannMagicDefOf.TM_UndeadHD || hediff.def == TorannMagicDefOf.TM_UndeadAnimalHD
                    || hediff.def == TorannMagicDefOf.TM_LichHD || hediff.def == TorannMagicDefOf.TM_SpiritPossessorHD)
                    return true;
                if (hediff.def != TorannMagicDefOf.TM_SpiritPossessionHD) continue;
                var possession = hediff.TryGetComp<HediffComp_SpiritPossession>();
                var spirit = possession?.SpiritPawn;
                // Provenance belongs to this possession's actually held spirit, never just
                // a trait or a surviving Need_Spirit.wasDead flag on a former host.
                if (spirit == null || spirit.holdingOwner != possession.GetDirectlyHeldThings()) continue;
                if (possession.ReanimatedHost && ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(pawn,out var original)
                    && original == spirit) return true;
                var state = spirit.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessorHD) as Hediff_Possessor;
                if (state != null && state.wasDead) return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue), new[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })]
    public static class MagicalCreatureVacuumStatPatch
    {
        // Outside StatWorker's temporary cache, so adding/removing/possessing a pawn
        // changes exposure and pathing immediately without poisoning cached base stats.
        public static void Postfix(Thing thing, StatDef stat, ref float __result)
        {
            if (stat != null && stat == StatDefOf.VacuumResistance && thing is Pawn pawn
                && MagicalCreatureVacuum.IsNaturallyVacuumImmune(pawn)) __result = 1f;
        }
    }

    [HarmonyPatch(typeof(VacuumUtility), nameof(VacuumUtility.CanBeVacuumBurnt))]
    public static class MagicalCreatureVacuumBurnPatch
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result && MagicalCreatureVacuum.IsNaturallyVacuumImmune(pawn)) __result = false;
        }
    }
}
