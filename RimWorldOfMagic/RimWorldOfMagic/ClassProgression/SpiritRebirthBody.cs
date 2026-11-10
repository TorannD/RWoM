using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthBody
    {
        internal static bool TryNeedAdmission(Pawn pawn, NeedDef need, out bool admitted)
        {
            admitted = false;
            // Physical form deliberately includes depletion awaiting safe custody.
            // Its existing Spirit need must survive to retry; casting is stricter.
            if (need == null || !RetainsNeeds(pawn)) return false;
            admitted = need == TorannMagicDefOf.TM_SpiritND || need == TorannMagicDefOf.TM_Mana;
            if (need == TorannMagicDefOf.TM_Stamina)
                admitted = pawn.needs?.TryGetNeed(need) != null || TM_ClassState.HasCurrentClass(pawn, false);
            if (admitted && ((SpiritRebirthRitualTransaction.IsPendingAccountingOwner(pawn)
                && !SpiritRebirthRitualTransaction.IsPreparing(pawn)) || MapComponent_SpiritCollapse.IsPendingAccountingOwner(pawn)))
                admitted = pawn.needs?.TryGetNeed(need) != null;
            return true;
        }

        // Custody changes must not discard the original's energy/upkeep records.
        // This grants no physical form properties to either the owner or host.
        internal static bool RetainsNeeds(Pawn pawn)
            => SpiritRebirthFormPolicy.IsEthereal(pawn)
                || SpiritRebirthRitualTransaction.IsPendingAccountingOwner(pawn)
                || MapComponent_SpiritCollapse.IsPendingAccountingOwner(pawn)
                || (pawn != null && !pawn.Dead && SpiritRebirthState.TryGet(pawn, out var state)
                    && (state.HasValidStone(pawn) || state.HasValidHost(pawn)));

        internal static bool TryStat(Pawn pawn, StatDef stat, out float value)
        {
            value = 0f;
            if (stat == null || !SpiritRebirthFormPolicy.IsEthereal(pawn)) return false;
            if (stat != StatDefOf.Mass && stat != StatDefOf.Flammability && stat != StatDefOf.ToxicResistance
                && stat != StatDefOf.ComfyTemperatureMin && stat != StatDefOf.ComfyTemperatureMax
                && stat != StatDefOf.MoveSpeed) return false;
            var defaults = TorannMagicDefOf.TM_SpiritTD?.statBases;
            if (defaults == null) return false;
            foreach (var entry in defaults)
            {
                if (entry == null || entry.stat != stat) continue;
                value = entry.value;
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue), new[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })]
    public static class SpiritRebirthBodyStatPatch
    {
        // Keep temporary and immutable native caches physical. A form transition
        // changes the returned value immediately without changing shared race defs.
        public static void Postfix(Thing thing, StatDef stat, ref float __result)
        {
            if (thing is Pawn pawn && SpiritRebirthBody.TryStat(pawn, stat, out float value)) __result = value;
        }
    }
}
