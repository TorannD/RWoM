using System;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    // A spending policy on the native physical store, never a second resource pool.
    // Flight uses its existing native total/payment paths and intentionally bypasses this policy.
    public static class MagofuelReserve
    {
        public static bool IsStorage(CompMagofuelReservoir bank) => bank != null
            && bank.GetType() == typeof(CompMagofuelReservoir)
            && (bank.parent?.def?.defName == "TM_MagofuelReservoir" || bank.parent is Building_MagicalGravEngine);

        public static float Protected(CompRefuelable fuel)
        {
            var bank = fuel?.parent?.GetComp<CompMagofuelReservoir>();
            return IsStorage(bank) ? bank.FuelReserve : 0f;
        }

        public static float Available(CompRefuelable fuel, bool allowReserve = false)
        {
            float amount = fuel?.Fuel ?? 0f;
            if (!CompMagofuelReservoir.PositiveFinite(amount)) return 0f;
            return Math.Max(0f, amount - (allowReserve ? 0f : Protected(fuel)));
        }

        // Revalidate settings after native callbacks, using the pre-debit quantity.
        internal static bool AllowsDebit(CompRefuelable fuel, float before, float debit, bool allowReserve = false)
            => debit <= Math.Max(0f, before - (allowReserve ? 0f : Protected(fuel)));

        // Permission follows the receiving device, never its user-selected priority.
        internal static bool IsLifeSupport(Thing receiver) => receiver?.TryGetComp<CompMagicalOxygen>() != null
            || receiver?.TryGetComp<CompMagicalThermal>() != null || receiver?.TryGetComp<CompGrandAtmosphereDome>() != null;
    }
}
