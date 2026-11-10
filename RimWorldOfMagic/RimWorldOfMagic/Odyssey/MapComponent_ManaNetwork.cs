using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    // Registries are rebuilt by component spawn callbacks, including loading and ship placement.
    // Topology is refreshed once per pulse; no map-wide thing scan occurs in a tick.
    public class MapComponent_ManaNetwork : MapComponent
    {
        public const float Range = 5f, ManaBudget = 10f;
        public const int FuelBudget = 5, PulseInterval = 60;
        readonly HashSet<CompManaBeacon> beacons = new HashSet<CompManaBeacon>();
        readonly HashSet<CompManaNetworkEndpoint> endpoints = new HashSet<CompManaNetworkEndpoint>();
        int lastPulse = -1, receiverCursor;
        bool pulsing;
        List<CompManaBeacon> componentNodes;
        List<Placement> componentPlacements;
        List<List<CompManaBeacon>> componentGroups;
        List<CompManaBeacon> liveNodes;
        public MapComponent_ManaNetwork(Map map) : base(map)
        {
        }

        public void Register(CompManaBeacon beacon) { beacons.Add(beacon); liveNodes = null; }
        public void Register(CompManaNetworkEndpoint endpoint) => endpoints.Add(endpoint);
        public void Unregister(CompManaBeacon beacon)
        {
            beacons.Remove(beacon);
            liveNodes = null;
            componentNodes = null;
            componentPlacements = null;
            componentGroups = null;
        }
        public void Unregister(CompManaNetworkEndpoint endpoint) => endpoints.Remove(endpoint);
        bool Live(ThingComp comp) => comp?.parent != null && comp.parent.Spawned && !comp.parent.Destroyed && comp.parent.Map == map && comp.parent.Faction != null && comp.parent.Faction.IsPlayer;
        List<CompManaBeacon> LiveBeacons()
        {
            if (liveNodes != null)
            {
                int count = 0;
                foreach (var beacon in beacons) if (Live(beacon)) count++;
                bool same = count == liveNodes.Count;
                if (same)
                    foreach (var beacon in liveNodes)
                        if (!Live(beacon) || !beacons.Contains(beacon)) { same = false; break; }
                if (same) return liveNodes;
            }
            liveNodes = beacons.Where(Live).OrderBy(b => b.parent.thingIDNumber).ToList();
            return liveNodes;
        }
        static bool Adjacent(CompManaBeacon a, CompManaBeacon b) => a.parent.Faction == b.parent.Faction && a.parent.Position.DistanceToSquared(b.parent.Position) <= Range * Range;
        public CompManaBeacon AttachedBeacon(CompManaNetworkEndpoint endpoint) => Nearest(endpoint, LiveBeacons());
        public List<CompManaBeacon> GetLiveBeacons() => new List<CompManaBeacon>(LiveBeacons());
        public bool HasSameLiveBeacons(List<CompManaBeacon> snapshot)
        {
            int count = 0;
            foreach (var beacon in beacons) if (Live(beacon)) count++;
            if (count != snapshot.Count) return false;
            foreach (var beacon in snapshot) if (!beacons.Contains(beacon)) return false;
            return true;
        }
        public List<CompManaBeacon> GetConnectedBeacons(CompManaNetworkEndpoint endpoint)
        {
            var nodes = LiveBeacons();
            var nearest = Nearest(endpoint, nodes);
            // Callers own their returned list; never expose the cached graph for mutation.
            return nearest == null ? new List<CompManaBeacon>() : new List<CompManaBeacon>(Components(nodes).First(c => c.Contains(nearest)));
        }

        // Rituals use the ordinary registered topology and sending reservoirs; no device buffer.
        public List<CompManaNetworkEndpoint> StellarSources(CompManaNetworkEndpoint receiver, bool mana)
        {
            if (!Live(receiver) || !endpoints.Contains(receiver) || (mana ? receiver.ManaMode : receiver.FuelMode) != ManaNetworkMode.Receive)
                return new List<CompManaNetworkEndpoint>();
            var nodes = LiveBeacons();
            var attached = Nearest(receiver, nodes);
            if (attached == null) return new List<CompManaNetworkEndpoint>();
            var group = Components(nodes).First(c => c.Contains(attached));
            return endpoints.Where(e => e != receiver && Live(e) && e.parent.Faction == receiver.parent.Faction
                && FlickUtility.WantsToBeOn(e.parent) && !e.parent.IsBrokenDown()
                && (mana ? e.ManaMode : e.FuelMode) == ManaNetworkMode.Send && group.Contains(Nearest(e, nodes))
                && (e.parent.def.defName == "TM_MagofuelReservoir" || e.parent is Building_MagicalGravEngine)
                && e.parent.GetComp<CompMagofuelReservoir>()?.GetType() == typeof(CompMagofuelReservoir))
                .OrderBy(e => e.parent.thingIDNumber).ToList();
        }

        // A siphon is a topology-only endpoint. It fills actual tanks, never consumer buffers.
        public List<CompManaNetworkEndpoint> SiphonTanks(CompManaNetworkEndpoint siphon)
        {
            if (!Live(siphon) || !endpoints.Contains(siphon)) return new List<CompManaNetworkEndpoint>();
            var nodes = LiveBeacons();
            var attached = Nearest(siphon, nodes);
            if (attached == null) return new List<CompManaNetworkEndpoint>();
            var group = Components(nodes).First(c => c.Contains(attached));
            return endpoints.Where(e => Live(e) && e.ManaMode != ManaNetworkMode.Off &&
                (e.parent.def.defName == "TM_MagofuelReservoir" || e.parent is Building_MagicalGravEngine) &&
                e.parent.GetComp<CompMagofuelReservoir>()?.GetType() == typeof(CompMagofuelReservoir) &&
                group.Contains(Nearest(e, nodes))).OrderBy(e => e.parent.thingIDNumber).ToList();
        }

        // The virtual pool's members: every live tank-holding endpoint in the
        // endpoint's connected group, nearest first with stable ID tie-breaks. An
        // endpoint without a beacon sees only itself — local tanks stay local.
        public List<CompManaNetworkEndpoint> PoolTanks(CompManaNetworkEndpoint endpoint)
        {
            var result = new List<CompManaNetworkEndpoint>();
            if (!Live(endpoint) || !endpoints.Contains(endpoint))
            {
                result.Add(endpoint);
                return result;
            }
            var nodes = LiveBeacons();
            var attached = Nearest(endpoint, nodes);
            if (attached == null)
            {
                result.Add(endpoint);
                return result;
            }
            var group = Components(nodes).First(c => c.Contains(attached));
            var rect = endpoint.parent.OccupiedRect();
            result.AddRange(endpoints.Where(e => e != endpoint && Live(e) && e.parent.Faction == endpoint.parent.Faction &&
                    (e.parent.def.defName == "TM_MagofuelReservoir" || e.parent is Building_MagicalGravEngine) &&
                    e.parent.GetComp<CompMagofuelReservoir>()?.GetType() == typeof(CompMagofuelReservoir) &&
                    group.Contains(Nearest(e, nodes)))
                .OrderBy(e => ManaNetworkOverlay.DistanceSquared(rect, e.parent.Position))
                .ThenBy(e => e.parent.thingIDNumber));
            result.Insert(0, endpoint);
            return result;
        }

        // Recall draws only from registered live sending tanks on the core's ordinary network.
        public List<CompRefuelable> CoreReturnFuelSources(CompManaNetworkEndpoint core)
        {
            if (!Live(core) || core.FuelMode == ManaNetworkMode.Off) return new List<CompRefuelable>();
            var nodes = LiveBeacons();
            var attached = Nearest(core, nodes);
            if (attached == null) return new List<CompRefuelable>();
            var group = Components(nodes).First(c => c.Contains(attached));
            return endpoints.Where(e => e != core && Live(e) && e.parent.Faction == core.parent.Faction
                && e.FuelMode == ManaNetworkMode.Send && group.Contains(Nearest(e, nodes))
                && (e.parent.def.defName == "TM_MagofuelReservoir" || e.parent is Building_MagicalGravEngine))
                .OrderBy(e => e.parent.thingIDNumber).Select(e => e.parent.GetComp<CompRefuelable>())
                .Where(CompatibleFuel).Distinct().ToList();
        }

        // Launch networks must remain entirely aboard this ship, including intermediate relays.
        public List<List<CompManaNetworkEndpoint>> ShipFuelNetworks(Building_GravEngine engine)
        {
            bool Aboard(ThingComp c) => Live(c) && c.parent.Faction == engine.Faction && engine.OnValidSubstructure(c.parent);
            var allNodes = LiveBeacons();
            var nodes = allNodes.Where(Aboard).ToList();
            var members = endpoints.Where(Aboard).OrderBy(e => e.parent.thingIDNumber).ToList();
            // Preserve the ordinary network's nearest-beacon attachment. Filtering relays
            // must not silently move an endpoint from a ground network onto another ship network.
            var attached = members.ToDictionary(e => e, e => Nearest(e, allNodes));
            return Components(nodes).Select(g => members.Where(e => attached[e] != null && g.Contains(attached[e])).ToList()).ToList();
        }
        CompManaBeacon Nearest(CompManaNetworkEndpoint endpoint, List<CompManaBeacon> nodes)
        {
            if (!Live(endpoint))
                return null;
            CompManaBeacon nearest = null;
            int best = (int)(Range * Range) + 1;
            CellRect rect = endpoint.parent.OccupiedRect();
            foreach (var node in nodes)
            {
                if (!Live(node) || node.parent.Faction != endpoint.parent.Faction)
                    continue;
                IntVec3 p = node.parent.Position;
                int distance = ManaNetworkOverlay.DistanceSquared(rect, p);
                // Sorted IDs resolve exact ties consistently. Endpoints never bridge components.
                if (distance < best)
                {
                    best = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        List<List<CompManaBeacon>> Components(List<CompManaBeacon> nodes)
        {
            bool same = componentNodes != null && componentNodes.Count == nodes.Count;
            if (same)
                for (int i = 0; i < nodes.Count; i++)
                    if (!ReferenceEquals(componentNodes[i], nodes[i]) || !componentPlacements[i].Current)
                    { same = false; break; }
            if (same) return componentGroups;
            var result = new List<List<CompManaBeacon>>();
            var visited = new HashSet<CompManaBeacon>();
            foreach (var node in nodes)
            {
                if (!visited.Add(node))
                    continue;
                var group = new List<CompManaBeacon>
                {
                    node
                };
                for (int i = 0; i < group.Count; i++)
                    foreach (var next in nodes)
                        if (!visited.Contains(next) && Adjacent(group[i], next))
                        {
                            visited.Add(next);
                            group.Add(next);
                        }

                result.Add(group);
            }

            componentNodes = new List<CompManaBeacon>(nodes);
            componentPlacements = nodes.Select(n => new Placement(n.parent)).ToList();
            componentGroups = result;
            return result;
        }

        public int ConnectedBeaconCount(CompManaBeacon beacon) => Components(LiveBeacons()).FirstOrDefault(c => c.Contains(beacon))?.Count ?? 0;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPulse, "TM_networkLastPulse", -1);
            Scribe_Values.Look(ref receiverCursor, "TM_networkReceiverCursor", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                receiverCursor = Math.Max(0, receiverCursor);
                pulsing = false;
            }
        }

        public override void MapComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (pulsing || now < 0 || (lastPulse >= 0 && (long)now - lastPulse < PulseInterval) || (lastPulse < 0 && now < PulseInterval))
                return;
            // Persist the deadline before effects; never replay missed intervals after a load.
            lastPulse = now;
            pulsing = true;
            try
            {
                var nodes = LiveBeacons();
                var members = endpoints.Where(Live).OrderBy(e => e.parent.thingIDNumber).ToList();
                var attached = members.ToDictionary(e => e, e => Nearest(e, nodes));
                var snapshot = nodes.Select(n => n.parent).Concat(members.Select(e => e.parent)).Distinct().Select(t => new Placement(t)).ToList();
                Func<bool> current = () => snapshot.All(s => s.Current) && beacons.Count(Live) == nodes.Count && endpoints.Count(Live) == members.Count && nodes.All(beacons.Contains) && members.All(endpoints.Contains);
                foreach (var group in Components(nodes))
                {
                    var local = members.Where(e => attached[e] != null && group.Contains(attached[e])).ToList();
                    if (group.Count > 1 || local.Count > 0)
                        foreach (var beacon in group) beacon.parent.GetComp<CompCrystalGlow>()?.NotifyService(now, PulseInterval);
                    TransferChannel(local, true, current, group);
                    TransferChannel(local, false, current, group);
                }

                receiverCursor = receiverCursor == int.MaxValue ? 0 : receiverCursor + 1;
            }
            finally
            {
                pulsing = false;
            }
        }

        sealed class Placement
        {
            readonly Thing thing;
            readonly Map map;
            readonly Faction faction;
            readonly IntVec3 position;
            readonly Rot4 rotation;
            public Placement(Thing thing)
            {
                this.thing = thing;
                map = thing.Map;
                faction = thing.Faction;
                position = thing.Position;
                rotation = thing.Rotation;
            }

            public bool Current => thing.Spawned && !thing.Destroyed && thing.Map == map && thing.Faction == faction && thing.Position == position && thing.Rotation == rotation;
        }

        void TransferChannel(List<CompManaNetworkEndpoint> local, bool mana, Func<bool> current, List<CompManaBeacon> group)
        {
            var sources = local.Where(e => (mana ? e.ManaMode : e.FuelMode) == ManaNetworkMode.Send).ToList();
            // Snapshot each channel's ranks once. Native fuel callbacks can change settings;
            // those changes take effect next pulse without reordering an in-flight payment.
            var receivers = local.Where(e => !e.Props.directConsumer && (mana ? e.ManaMode : e.FuelMode) == ManaNetworkMode.Receive)
                .GroupBy(e => mana ? e.ManaPriority : e.FuelPriority).OrderByDescending(g => g.Key)
                .SelectMany(g =>
                {
                    var rank = g.ToList();
                    int start = receiverCursor % rank.Count;
                    return rank.Skip(start).Concat(rank.Take(start));
                }).ToList();
            float remaining = mana ? ManaBudget : FuelBudget;
            for (int i = 0; i < receivers.Count && remaining > 0; i++)
            {
                var receiver = receivers[i];
                foreach (var source in sources)
                {
                    if (remaining <= 0 || !current())
                        return;
                    if (!Live(source) || !Live(receiver) || (mana ? source.ManaMode : source.FuelMode) != ManaNetworkMode.Send || (mana ? receiver.ManaMode : receiver.FuelMode) != ManaNetworkMode.Receive)
                        continue;
                    if (mana)
                    {
                        var oxygen = receiver.parent.GetComp<CompMagicalOxygen>();
                        if (oxygen != null && !oxygen.CanOperate)
                            continue;
                        var from = source.parent.GetComp<CompMagofuelReservoir>();
                        var to = receiver.parent.GetComp<CompMagofuelReservoir>();
                        if (from == null || to == null)
                            continue;
                        float wanted = Math.Min(remaining, Math.Max(0f, to.Props.manaCapacity * receiver.ManaTarget - to.StoredMana));
                        if (wanted > from.StoredMana && NeedsAutomaticManaConversion(receiver)
                            && CompatibleFuel(source.parent.GetComp<CompRefuelable>()) && CompMagofuelReservoir.PositiveFinite(from.Props.manaPerFuel))
                        {
                            // Convert whole fuel units only on demand; unused mana stays in the tank.
                            from.ConvertFuelForReceiver((float)Math.Ceiling((wanted - from.StoredMana) / from.Props.manaPerFuel) * from.Props.manaPerFuel, receiver.parent);
                            // Consuming the last unit broadcasts native signals, which can change the network.
                            if (!current() || !Live(source) || !Live(receiver) || source.ManaMode != ManaNetworkMode.Send
                                || receiver.ManaMode != ManaNetworkMode.Receive || !NeedsAutomaticManaConversion(receiver))
                                continue;
                            wanted = Math.Min(remaining, Math.Max(0f, to.Props.manaCapacity * receiver.ManaTarget - to.StoredMana));
                        }
                        float moved = from.TransferManaTo(to, wanted);
                        remaining -= moved;
                        if (moved > 0f)
                            foreach (var beacon in group) beacon.parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(moved, Find.TickManager.TicksGame);
                    }
                    else
                        remaining -= TransferFuel(source, receiver, remaining, current);
                }
            }
        }

        bool NeedsAutomaticManaConversion(CompManaNetworkEndpoint receiver)
        {
            var oxygen = receiver.parent.GetComp<CompMagicalOxygen>();
            if (oxygen != null && oxygen.CanOperate && oxygen.SupplyMode == OxygenSupplyMode.Mana) return true;
            var thermal = receiver.parent.GetComp<CompMagicalThermal>();
            if (ModsConfig.OdysseyActive && thermal != null && thermal.CanOperate &&
                !map.GetComponent<MapComponent_MagicalThermal>().IsStandby(thermal)) return true;
            var combat = receiver.parent.GetComp<CompMagicalCombat>();
            return ModsConfig.OdysseyActive && combat != null && combat.CanOperate &&
                !map.GetComponent<MapComponent_MagicalCombat>().IsStandby(combat);
        }

        public static bool CompatibleFuel(CompRefuelable fuel)
        {
            if (fuel == null || fuel.GetType() != typeof(CompRefuelable))
                return false;
            var p = fuel.Props;
            return !p.destroyOnNoFuel && !p.fuelIsMortarBarrel && !p.atomicFueling && p.FuelMultiplierCurrentDifficulty == 1f && CompMagofuelReservoir.PositiveFinite(p.fuelCapacity) && p.fuelCapacity <= 1000000f && p.fuelFilter != null && p.fuelFilter.AllowedDefCount == 1 && p.fuelFilter.AllowedThingDefs.First().defName == "TM_Magofuel" && fuel.Fuel >= 0 && !float.IsInfinity(fuel.Fuel) && !float.IsNaN(fuel.Fuel) && fuel.TargetFuelLevel >= 0 && !float.IsInfinity(fuel.TargetFuelLevel) && !float.IsNaN(fuel.TargetFuelLevel);
        }

        float TransferFuel(CompManaNetworkEndpoint source, CompManaNetworkEndpoint receiver, float budget, Func<bool> current)
        {
            var from = source.parent.GetComp<CompRefuelable>();
            var to = receiver.parent.GetComp<CompRefuelable>();
            if (!CompatibleFuel(from) || !CompatibleFuel(to))
                return 0;
            bool allowReserve = MagofuelReserve.IsLifeSupport(receiver.parent);
            float amount = (float)Math.Floor(Math.Min(budget, Math.Min(MagofuelReserve.Available(from, allowReserve), Math.Min(to.TargetFuelLevel, to.Props.fuelCapacity) - to.Fuel)));
            if (amount < 1 || from.Fuel - amount == from.Fuel || to.Fuel + amount == to.Fuel)
                return 0;
            // Native debit can signal RanOutOfFuel. Recheck after that callback before credit.
            float before = from.Fuel;
            try
            {
                from.ConsumeFuel(amount);
            }
            catch
            {
                if (from.Fuel == before - amount)
                    from.Refuel(amount);
                throw;
            }

            if (!current() || !Live(receiver) || receiver.FuelMode != ManaNetworkMode.Receive || source.FuelMode != ManaNetworkMode.Send || !CompatibleFuel(to) || Math.Min(to.TargetFuelLevel, to.Props.fuelCapacity) - to.Fuel < amount
                || !MagofuelReserve.AllowsDebit(from, before, amount, allowReserve && MagofuelReserve.IsLifeSupport(receiver.parent)))
            {
                from.Refuel(amount);
                return 0;
            }

            to.Refuel(amount);
            return amount;
        }

        public void DrawLinks(CompManaBeacon beacon)
        {
            if (!Live(beacon))
                return;
            foreach (var other in LiveBeacons())
                if (other != beacon && Adjacent(beacon, other))
                    GenDraw.DrawLineBetween(beacon.parent.DrawPos, other.parent.DrawPos);
            foreach (var endpoint in endpoints)
                if (AttachedBeacon(endpoint) == beacon)
                    GenDraw.DrawLineBetween(beacon.parent.DrawPos, endpoint.parent.DrawPos);
        }

        public void DrawEndpointLink(CompManaNetworkEndpoint endpoint)
        {
            var beacon = AttachedBeacon(endpoint);
            if (beacon != null)
            {
                GenDraw.DrawLineBetween(endpoint.parent.DrawPos, beacon.parent.DrawPos);
            }
        }
    }
}
