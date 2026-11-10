using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    // The virtual mana/magofuel pool: consumers see every tank on their beacon
    // network as one shared store. Tanks stay physical and saves stay per-building;
    // the pool is computed on the fly from the ordinary registered topology. A
    // building without a beacon (or off its group) sees only its own tank.
    public static class MagofuelPool
    {
        public static MapComponent_ManaNetwork Network(Thing thing)
        {
            var map = thing?.Map;
            if (map?.components == null) return null;
            return map.GetComponent<MapComponent_ManaNetwork>();
        }

        static T CompOf<T>(Thing thing) where T : ThingComp => (thing as ThingWithComps)?.GetComp<T>();

        static CompManaNetworkEndpoint Endpoint(Thing thing) => CompOf<CompManaNetworkEndpoint>(thing);

        static List<CompManaNetworkEndpoint> Tanks(Thing thing)
        {
            var endpoint = Endpoint(thing);
            var network = Network(thing);
            if (endpoint == null || network == null) return new List<CompManaNetworkEndpoint>();
            return network.PoolTanks(endpoint);
        }

        static CompMagofuelReservoir TankOf(CompManaNetworkEndpoint endpoint) =>
            endpoint?.parent?.GetComp<CompMagofuelReservoir>();

        // Move mana from the pool into the caller's tank. Local stock is always the
        // first member, so a topped-up tank never re-pulls from itself. Each source
        // honours its own maxTransfer; callers re-withdraw whatever actually arrived.
        public static float DrawMana(CompMagofuelReservoir tank, float requestedMana)
        {
            if (tank == null || !CompMagofuelReservoir.PositiveFinite(requestedMana) || !tank.UsableInternal)
                return 0f;
            float moved = 0f;
            foreach (var endpoint in Tanks(tank.parent))
            {
                if (moved >= requestedMana) break;
                var source = TankOf(endpoint);
                if (source == null || source == tank) continue;
                moved += source.TransferManaTo(tank, requestedMana - moved);
            }
            return moved;
        }

        // Move whole physical fuel units from pool tanks into the caller's native
        // store. Sources keep their configured reserve; a native debit that throws
        // or moves the building is rolled back before any credit is granted.
        public static float DrawFuel(Thing target, float requestedFuel)
        {
            var fuel = CompOf<CompRefuelable>(target);
            if (fuel == null || !CompMagofuelReservoir.PositiveFinite(requestedFuel) || fuel.Props != null &&
                (fuel.Props.destroyOnNoFuel || fuel.Props.fuelIsMortarBarrel))
                return 0f;
            float moved = 0f;
            var originalMap = target.Map;
            foreach (var endpoint in Tanks(target))
            {
                if (moved >= requestedFuel) break;
                if (endpoint.parent == target) continue;
                var source = endpoint.parent.GetComp<CompRefuelable>();
                if (source == null) continue;
                if (!MapComponent_ManaNetwork.CompatibleFuel(source)) continue;
                float room = Math.Min(fuel.TargetFuelLevel, fuel.Props.fuelCapacity) - fuel.Fuel;
                float amount = (float)Math.Floor(Math.Min(requestedFuel - moved, Math.Min(MagofuelReserve.Available(source, false), room)));
                if (amount < 1f) continue;
                float before = source.Fuel;
                try
                {
                    source.ConsumeFuel(amount);
                }
                catch
                {
                    if (source.Fuel == before - amount) source.Refuel(amount);
                    continue;
                }
                if (!endpoint.parent.Spawned || endpoint.parent.Destroyed || endpoint.parent.Map != originalMap ||
                    source.Fuel != before - amount || !MapComponent_ManaNetwork.CompatibleFuel(fuel) ||
                    Math.Min(fuel.TargetFuelLevel, fuel.Props.fuelCapacity) - fuel.Fuel < amount ||
                    !MagofuelReserve.AllowsDebit(source, before, amount, false))
                {
                    source.Refuel(amount);
                    continue;
                }
                fuel.Refuel(amount);
                moved += amount;
            }
            return moved;
        }

        // Network-wide view for UI: mana and fuel totals over the pool's tanks.
        public static (float mana, float manaCapacity, float fuel, float fuelCapacity, int tanks) Totals(Thing thing)
        {
            float mana = 0f, manaCapacity = 0f, fuel = 0f, fuelCapacity = 0f;
            int tanks = 0;
            foreach (var endpoint in Tanks(thing))
            {
                var tank = TankOf(endpoint);
                if (tank == null) continue;
                tanks++;
                mana += tank.StoredMana;
                manaCapacity += tank.Props.manaCapacity;
                var store = endpoint.parent.GetComp<CompRefuelable>();
                if (store != null)
                {
                    fuel += store.Fuel;
                    fuelCapacity += store.Props.fuelCapacity;
                }
            }
            return (mana, manaCapacity, fuel, fuelCapacity, tanks);
        }

        // One inspect line for network buildings; null for local-only tanks.
        public static string InspectLine(Thing thing)
        {
            var totals = Totals(thing);
            if (totals.tanks <= 1) return null;
            return "TM_PoolInspect".Translate(
                totals.mana.ToString("0.#"), totals.manaCapacity.ToString("0"),
                totals.fuel.ToString("0.#"), totals.fuelCapacity.ToString("0"),
                totals.tanks);
        }
    }
}
