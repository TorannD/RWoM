using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    // A prepaid cell field with one missed-check grace period. Native room pressure is never modified.
    public class MapComponent_MagicalAtmosphere : MapComponent
    {
        public const float Radius = 5f;
        public const float ManaPerCellSecond = CompMagicalOxygen.ManaPerCellSecond;
        public const int Interval = 60;
        readonly HashSet<CompMagicalOxygen> generators = new HashSet<CompMagicalOxygen>();
        Dictionary<IntVec3, float> strength = new Dictionary<IntVec3, float>();
        HashSet<IntVec3> missed = new HashSet<IntVec3>();
        readonly Dictionary<CompMagicalOxygen, float> pressure = new Dictionary<CompMagicalOxygen, float>();
        List<Placement> placements = new List<Placement>();
        List<CompManaBeacon> beaconSnapshot = new List<CompManaBeacon>();
        List<CompMagicalOxygen> pumpSnapshot = new List<CompMagicalOxygen>();
        int lastPulse = -1;
        bool pulsing;
        public MapComponent_MagicalAtmosphere(Map map) : base(map)
        {
        }

        public void Register(CompMagicalOxygen pump)
        {
            if (generators.Add(pump))
                Invalidate();
        }

        public void Unregister(CompMagicalOxygen pump)
        {
            if (generators.Remove(pump))
                Invalidate();
        }

        public void Invalidate()
        {
            pressure.Clear();
        }

        public float PressureFractionFor(CompMagicalOxygen pump)
        {
            if (!Current())
                pressure.Clear();
            return pressure.TryGetValue(pump, out var value) ? value : 0f;
        }

        bool Live(CompMagicalOxygen pump) => pump?.parent != null && pump.parent.Map == map && pump.CanOperate && pump.OutputMode == OxygenOutputMode.Atmosphere;
        bool Current()
        {
            if (!map.Biome.inVacuum)
                return false;
            foreach (var placement in placements)
                if (!placement.Current)
                    return false;
            foreach (var pump in pumpSnapshot)
                if (!Live(pump))
                    return false;
            int count = 0;
            foreach (var pump in generators)
                if (Live(pump))
                    count++;
            if (count != pumpSnapshot.Count)
                return false;
            return map.GetComponent<MapComponent_ManaNetwork>().HasSameLiveBeacons(beaconSnapshot);
        }

        public float StrengthAt(IntVec3 cell)
        {
            if (!ModsConfig.OdysseyActive || !map.Biome.inVacuum || !strength.TryGetValue(cell, out var value))
                return 0f;
            // Reads never consume a miss or buy oxygen. Bound stale data if a map stops ticking.
            int now = Find.TickManager.TicksGame;
            long age = (long)now - lastPulse;
            return age < 0 || age >= (missed.Contains(cell) ? Interval : 2 * Interval) ? 0f : value;
        }

        public float EffectiveVacuum(IntVec3 cell, float nativeVacuum) => nativeVacuum * (1f - StrengthAt(cell));
        public IEnumerable<IntVec3> CoveredCells => strength.Keys.Where(cell => StrengthAt(cell) > 0f);
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPulse, "TM_atmosphereLastPulse", -1);
            Scribe_Collections.Look(ref strength, "TM_atmosphereStrength", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref missed, "TM_atmosphereMissed", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                strength = strength ?? new Dictionary<IntVec3, float>();
                missed = missed ?? new HashSet<IntVec3>();
                foreach (var cell in strength.Keys.ToList())
                    if (!cell.InBounds(map) || float.IsNaN(strength[cell]) || strength[cell] <= 0f)
                        strength.Remove(cell);
                    else
                        strength[cell] = Mathf.Clamp01(strength[cell]);
                missed.RemoveWhere(cell => !strength.ContainsKey(cell));
                Invalidate();
                pulsing = false;
            }
        }

        public override void MapComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (pulsing || now < 0 || (lastPulse < 0 ? now < Interval : (long)now - lastPulse < Interval))
                return;
            bool skippedCheck = lastPulse >= 0 && (long)now - lastPulse >= 2 * Interval;
            lastPulse = now;
            Invalidate();
            if (!ModsConfig.OdysseyActive || !map.Biome.inVacuum)
            {
                strength.Clear();
                missed.Clear();
                return;
            }
            var paidStrength = new Dictionary<IntVec3, float>();
            pulsing = true;
            try
            {
                var graph = map.GetComponent<MapComponent_ManaNetwork>();
                pumpSnapshot = generators.Where(Live).OrderBy(p => p.parent.thingIDNumber).ToList();
                beaconSnapshot = graph.GetLiveBeacons();
                placements = pumpSnapshot.Select(p => new Placement(p.parent, p)).Concat(beaconSnapshot.Select(b => new Placement(b.parent, null))).ToList();
                var groups = new Dictionary<Thing, Group>();
                foreach (var pump in pumpSnapshot)
                {
                    var endpoint = pump.parent.GetComp<CompManaNetworkEndpoint>();
                    var nodes = endpoint == null ? new List<CompManaBeacon>() : graph.GetConnectedBeacons(endpoint);
                    Thing key = nodes.Count == 0 ? pump.parent : nodes.OrderBy(n => n.parent.thingIDNumber).First().parent;
                    if (!groups.TryGetValue(key, out var group))
                        groups.Add(key, group = new Group());
                    group.Pumps.Add(pump);
                    AddCircle(group.Cells, pump.parent.Position);
                    foreach (var beacon in nodes)
                        AddCircle(group.Cells, beacon.parent.Position);
                }

                foreach (var group in groups.Values)
                {
                    float demand = group.Cells.Count * ManaPerCellSecond;
                    float paid = 0f;
                    foreach (var pump in group.Pumps)
                    {
                        if (!Current())
                        {
                            Invalidate();
                            paidStrength.Clear();
                            return;
                        }

                        if (paid >= demand)
                            break;
                        paid += pump.SupplyPulse(demand - paid);
                    }

                    if (!Current())
                    {
                        Invalidate();
                        paidStrength.Clear();
                        return;
                    }

                    float fraction = demand > 0 ? Mathf.Clamp01(paid / demand) : 0f;
                    foreach (var pump in group.Pumps)
                        pressure[pump] = fraction;
                    if (fraction <= 0)
                        continue;
                    foreach (var cell in group.Cells)
                        if (!paidStrength.TryGetValue(cell, out var existing) || fraction > existing)
                            paidStrength[cell] = fraction;
                }
            }
            catch
            {
                paidStrength.Clear();
                Invalidate();
                throw;
            }
            finally
            {
                // Visit only the paid union and cells that previously held air.
                foreach (var cell in strength.Keys.ToList())
                {
                    if (paidStrength.ContainsKey(cell))
                        missed.Remove(cell);
                    else if (skippedCheck || !missed.Add(cell))
                    {
                        strength.Remove(cell);
                        missed.Remove(cell);
                    }
                }
                foreach (var cell in paidStrength)
                    strength[cell.Key] = cell.Value;
                pulsing = false;
            }
        }

        void AddCircle(HashSet<IntVec3> cells, IntVec3 center)
        {
            for (int x = -5; x <= 5; x++)
                for (int z = -5; z <= 5; z++)
                {
                    if (x * x + z * z > Radius * Radius)
                        continue;
                    var cell = new IntVec3(center.x + x, 0, center.z + z);
                    if (cell.InBounds(map))
                        cells.Add(cell);
                }
        }

        sealed class Group
        {
            public readonly List<CompMagicalOxygen> Pumps = new List<CompMagicalOxygen>();
            public readonly HashSet<IntVec3> Cells = new HashSet<IntVec3>();
        }

        sealed class Placement
        {
            readonly Thing thing;
            readonly Map map;
            readonly Faction faction;
            readonly IntVec3 position;
            readonly Rot4 rotation;
            readonly CompMagicalOxygen pump;
            readonly OxygenSupplyMode mode;
            readonly ManaNetworkMode manaMode, fuelMode;
            public Placement(Thing thing, CompMagicalOxygen pump)
            {
                this.thing = thing;
                this.pump = pump;
                map = thing.Map;
                faction = thing.Faction;
                position = thing.Position;
                rotation = thing.Rotation;
                if (pump != null)
                    mode = pump.SupplyMode;
                var endpoint = thing.TryGetComp<CompManaNetworkEndpoint>();
                if (endpoint != null)
                {
                    manaMode = endpoint.ManaMode;
                    fuelMode = endpoint.FuelMode;
                }
            }

            public bool Current
            {
                get
                {
                    if (!thing.Spawned || thing.Destroyed || thing.Map != map || thing.Faction != faction || thing.Position != position || thing.Rotation != rotation || (pump != null && pump.SupplyMode != mode))
                        return false;
                    var endpoint = thing.TryGetComp<CompManaNetworkEndpoint>();
                    return endpoint == null || (endpoint.ManaMode == manaMode && endpoint.FuelMode == fuelMode);
                }
            }
        }
    }
}
