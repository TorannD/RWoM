using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
namespace TorannMagic.Odyssey
{
    public static class StellarDivinationPayment
    {
        private static bool paying;
        private static readonly AccessTools.FieldRef<CompRefuelable, float> NativeFuel = AccessTools.FieldRefAccess<CompRefuelable, float>("fuel");
        private static bool FuelSource(CompRefuelable tank) => MapComponent_ManaNetwork.CompatibleFuel(tank)
            && tank.GetType() == typeof(CompRefuelable) && !tank.Props.fuelIsMortarBarrel && !tank.Props.destroyOnNoFuel;
        private static List<CompManaNetworkEndpoint> Sources(Thing thing, bool mana)
        {
            var endpoint = thing?.TryGetComp<CompManaNetworkEndpoint>();
            return endpoint == null || !thing.Spawned || thing.Destroyed ? new List<CompManaNetworkEndpoint>()
                : thing.Map.GetComponent<MapComponent_ManaNetwork>().StellarSources(endpoint, mana);
        }
        public static bool CanPay(Thing thing) => !paying && Sources(thing, true).Sum(e => e.parent.GetComp<CompMagofuelReservoir>().StoredMana) >= 100f
            && Sources(thing, false).Select(e => e.parent.GetComp<CompRefuelable>()).Where(FuelSource).Sum(f => MagofuelReserve.Available(f)) >= 50f;
        public static bool TryPay(Thing thing, Func<bool> validate)
        {
            if (!CanPay(thing) || validate == null) return false;
            bool committed = false;
            var manaDebits = new Dictionary<CompMagofuelReservoir, float>();
            var fuelDebits = new Dictionary<CompRefuelable, float>();
            var beforePayment = new Dictionary<CompRefuelable, float>();
            var mana = Sources(thing, true);
            var fuel = Sources(thing, false).Where(e => FuelSource(e.parent.GetComp<CompRefuelable>())).ToList();
            var map = thing.Map; var position = thing.Position; var faction = thing.Faction;
            bool Current() => thing.Spawned && !thing.Destroyed && thing.Map == map && thing.Position == position && thing.Faction == faction
                && mana.All(Sources(thing, true).Contains) && fuel.All(Sources(thing, false).Contains)
                && fuel.All(e => FuelSource(e.parent.GetComp<CompRefuelable>()));
            bool ReservesCurrent() => fuelDebits.All(p => MagofuelReserve.AllowsDebit(p.Key, beforePayment[p.Key], p.Value));
            paying = true;
            try
            {
                float remaining = 100;
                foreach (var source in mana)
                {
                    var bank = source.parent.GetComp<CompMagofuelReservoir>();
                    // Respect the existing bounded withdrawal contract; no callback or personal mana.
                    int transfers = 0;
                    while (remaining > 0 && bank.StoredMana > 0)
                    {
                        if (++transfers > 1024) return false;
                        float debit = bank.WithdrawMana(remaining);
                        if (debit <= 0) break;
                        manaDebits.TryGetValue(bank, out float prior); manaDebits[bank] = prior + debit;
                        if (!(remaining - debit < remaining)) return false;
                        remaining -= debit;
                    }
                    if (remaining == 0) break;
                }
                if (remaining != 0) return false;
                remaining = 50;
                foreach (var source in fuel)
                {
                    if (!Current() || !ReservesCurrent()) return false;
                    var tank = source.parent.GetComp<CompRefuelable>();
                    float debit = Math.Min(remaining, MagofuelReserve.Available(tank)), before = tank.Fuel;
                    if (debit <= 0) continue;
                    // For ordinary native tanks subtraction precedes RanOutOfFuel callbacks.
                    // Record that exact debit: callbacks may refill or change capacity before returning.
                    fuelDebits[tank] = debit;
                    beforePayment[tank] = before;
                    tank.ConsumeFuel(debit);
                    if (tank.Fuel != before - debit) return false;
                    remaining -= debit;
                    if (remaining == 0) break;
                }
                committed = remaining == 0 && Current() && ReservesCurrent() && validate() && Current() && ReservesCurrent();
                return committed;
            }
            catch (Exception ex)
            {
                Compatibility.TM_Log.Warning("StellarDivination", "Ritual payment cancelled: " + ex.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (!committed)
                    {
                        foreach (var debit in manaDebits) debit.Key.RefundWithdrawnMana(debit.Value);
                        // Refund is restoration, not refueling: native Refuel applies difficulty
                        // multipliers and capacity clamps, both of which can destroy paid resources.
                        foreach (var debit in fuelDebits) NativeFuel(debit.Key) += debit.Value;
                    }
                }
                finally { paying = false; }
            }
        }
    }
}
