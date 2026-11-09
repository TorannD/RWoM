using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    // Coverage is prepaid and transient. No room, map weather or stored air temperature is changed.
    public class MapComponent_MagicalThermal : MapComponent
    {
        public const int Interval = 60;
        public const float Radius = 5f, ManaPerCellSecond = .001f;
        readonly HashSet<CompMagicalThermal> generators = new HashSet<CompMagicalThermal>();
        readonly Dictionary<IntVec3, List<Effect>> effects = new Dictionary<IntVec3, List<Effect>>();
        readonly Dictionary<CompMagicalThermal, float> strengths = new Dictionary<CompMagicalThermal, float>();
        List<CompMagicalThermal> sourceSnapshot = new List<CompMagicalThermal>();
        List<CompManaBeacon> beaconSnapshot = new List<CompManaBeacon>();
        List<Placement> placements = new List<Placement>();
        int lastPulse = -1;
        bool pulsing;
        public MapComponent_MagicalThermal(Map map) : base(map)
        {
        }

        public void Register(CompMagicalThermal source)
        {
            generators.Add(source);
        }

        public void Unregister(CompMagicalThermal source)
        {
            generators.Remove(source);
        }

        public void Invalidate()
        {
            effects.Clear();
            strengths.Clear();
        }

        bool Live(CompMagicalThermal source) => source?.parent != null && source.parent.Map == map && source.CanOperate;
        public bool IsStandby(CompMagicalThermal source)
        {
            if (!Live(source))
                return false;
            var graph = map.GetComponent<MapComponent_ManaNetwork>();
            var endpoint = source.parent.GetComp<CompManaNetworkEndpoint>();
            if (endpoint == null)
                return false;
            var nodes = graph.GetConnectedBeacons(endpoint);
            if (nodes.Count == 0)
                return false;
            return generators.Any(other => other != source && Live(other) && other.parent.thingIDNumber < source.parent.thingIDNumber && other.parent.GetComp<CompManaNetworkEndpoint>()is CompManaNetworkEndpoint otherEndpoint && nodes.Contains(graph.AttachedBeacon(otherEndpoint)));
        }

        bool Current()
        {
            if (!ModsConfig.OdysseyActive) return false;
            // These reads run for every protected pawn/cell. Keep the full live
            // validation without LINQ enumerators or captured delegates per read.
            foreach (var source in sourceSnapshot)
                if (!Live(source) || !generators.Contains(source)) return false;
            foreach (var placement in placements)
                if (!placement.Current) return false;
            int liveCount = 0;
            foreach (var source in generators)
                if (Live(source)) liveCount++;
            if (liveCount != sourceSnapshot.Count) return false;
            return map.GetComponent<MapComponent_ManaNetwork>().HasSameLiveBeacons(beaconSnapshot);
        }

        bool Valid()
        {
            int now = Find.TickManager.TicksGame;
            if (!ModsConfig.OdysseyActive || now < lastPulse || (long)now - lastPulse >= Interval)
            {
                Invalidate();
                return false;
            }

            if (!Current())
            {
                // An unrelated network may change during a prepaid interval. Retain
                // only receipts whose own topology and elected source are unchanged.
                var invalid = new HashSet<Effect>(effects.Values.SelectMany(list => list).Distinct().Where(effect => !ReceiptCurrent(effect)));
                foreach (var cell in effects.Keys.ToList())
                {
                    effects[cell].RemoveAll(invalid.Contains);
                    if (effects[cell].Count == 0) effects.Remove(cell);
                }
                foreach (var effect in invalid) strengths.Remove(effect.Source);
                CaptureSnapshot();
            }
            return true;
        }

        void CaptureSnapshot()
        {
            sourceSnapshot = generators.Where(Live).OrderBy(s => s.parent.thingIDNumber).ToList();
            beaconSnapshot = map.GetComponent<MapComponent_ManaNetwork>().GetLiveBeacons();
            placements = sourceSnapshot.Select(s => new Placement(s.parent)).Concat(beaconSnapshot.Select(b => new Placement(b.parent))).ToList();
        }

        bool ReceiptCurrent(Effect effect)
        {
            if (!generators.Contains(effect.Source) || !Live(effect.Source) || effect.Placements.Any(p => !p.Current) || IsStandby(effect.Source)) return false;
            var endpoint = effect.Source.parent.GetComp<CompManaNetworkEndpoint>();
            var nodes = endpoint == null ? new List<CompManaBeacon>() : map.GetComponent<MapComponent_ManaNetwork>().GetConnectedBeacons(endpoint);
            return nodes.Count == effect.Nodes.Count && nodes.All(effect.Nodes.Contains);
        }

        public float StrengthFor(CompMagicalThermal source) => strengths.Count > 0 && Valid() && strengths.TryGetValue(source, out var value) ? value : 0f;
        public float EffectiveTemperature(IntVec3 cell, float native)
        {
            if (pulsing || !effects.TryGetValue(cell, out var local))
                return native;
            // Warming can never affect an already warmer cell. No topology walk is
            // needed for this answer, even when the stored receipt has expired.
            bool canWarm = false;
            foreach (var effect in local)
                if (!(native >= effect.Target)) { canWarm = true; break; }
            if (!canWarm || !Valid() || !effects.TryGetValue(cell, out local)) return native;
            float result = native;
            foreach (var effect in local)
                result = Math.Max(result, native + Math.Max(0f, effect.Target - native) * effect.Strength);
            return result;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPulse, "TM_thermalLastPulse", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Invalidate();
                pulsing = false;
            }
        }

        public override void MapComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (pulsing || now < 0 || (lastPulse < 0 ? now < Interval : (long)now - lastPulse < Interval))
                return;
            lastPulse = now;
            Invalidate();
            if (!ModsConfig.OdysseyActive)
                return;
            pulsing = true;
            try
            {
                var graph = map.GetComponent<MapComponent_ManaNetwork>();
                CaptureSnapshot();
                var elected = new HashSet<Thing>();
                foreach (var source in sourceSnapshot)
                {
                    var endpoint = source.parent.GetComp<CompManaNetworkEndpoint>();
                    var nodes = endpoint == null ? new List<CompManaBeacon>() : graph.GetConnectedBeacons(endpoint);
                    Thing key = nodes.Count == 0 ? source.parent : nodes[0].parent;
                    // Lowest eligible thing ID wins, even if empty; standby generators do not add capacity.
                    if (!elected.Add(key))
                        continue;
                    var cells = new HashSet<IntVec3>();
                    AddCircle(cells, source.parent.Position);
                    foreach (var node in nodes)
                        AddCircle(cells, node.parent.Position);
                    // During this pulse the hook returns native temperatures, including previously processed groups.
                    cells.RemoveWhere(c => !GenTemperature.TryGetTemperatureForCell(c, map, out var temp) || temp >= source.Props.minTemperature);
                    if (cells.Count == 0)
                        continue;
                    if (!Current())
                    {
                        Invalidate();
                        return;
                    }

                    float demand = cells.Count * source.Props.manaPerCellSecond;
                    float paid = source.SupplyPulse(demand);
                    if (!Current())
                    {
                        Invalidate();
                        return;
                    }

                    float fraction = Mathf.Clamp01(paid / demand);
                    strengths[source] = fraction;
                    if (fraction <= 0f)
                        continue;
                    source.parent.GetComp<CompCrystalGlow>()?.NotifyService(now, Interval);
                    var effect = new Effect(source, nodes, fraction);
                    foreach (var cell in cells)
                    {
                        if (!effects.TryGetValue(cell, out var list))
                            effects[cell] = list = new List<Effect>();
                        list.Add(effect);
                    }
                }
            }
            catch
            {
                Invalidate();
                throw;
            }
            finally
            {
                pulsing = false;
            }
        }

        void AddCircle(HashSet<IntVec3> cells, IntVec3 center)
        {
            for (int x = -5; x <= 5; x++)
                for (int z = -5; z <= 5; z++)
                    if (x * x + z * z <= 25)
                    {
                        var c = center + new IntVec3(x, 0, z);
                        if (c.InBounds(map))
                            cells.Add(c);
                    }
        }

        sealed class Effect
        {
            public readonly float Target, Strength;
            public readonly CompMagicalThermal Source;
            public readonly List<CompManaBeacon> Nodes;
            public readonly List<Placement> Placements;
            public Effect(CompMagicalThermal source, List<CompManaBeacon> nodes, float strength)
            {
                Source = source;
                Nodes = nodes;
                Placements = nodes.Select(n => new Placement(n.parent)).Concat(new[] { new Placement(source.parent) }).ToList();
                Target = source.Props.minTemperature;
                Strength = strength;
            }
        }

        sealed class Placement
        {
            readonly Thing thing;
            readonly Map map;
            readonly Faction faction;
            readonly IntVec3 position;
            readonly Rot4 rotation;
            readonly ManaNetworkMode manaMode, fuelMode;
            public Placement(Thing thing)
            {
                this.thing = thing;
                map = thing.Map;
                faction = thing.Faction;
                position = thing.Position;
                rotation = thing.Rotation;
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
                    if (!thing.Spawned || thing.Destroyed || thing.Map != map || thing.Faction != faction || thing.Position != position || thing.Rotation != rotation)
                        return false;
                    var endpoint = thing.TryGetComp<CompManaNetworkEndpoint>();
                    return endpoint == null || (endpoint.ManaMode == manaMode && endpoint.FuelMode == fuelMode);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GenTemperature), nameof(GenTemperature.TryGetTemperatureForCell))]
    public static class MagicalThermalTemperaturePatch
    {
        public static void Postfix(IntVec3 c, Map map, ref float tempResult, bool __result)
        {
            if (__result && map != null && ModsConfig.OdysseyActive)
                tempResult = map.GetComponent<MapComponent_MagicalThermal>()?.EffectiveTemperature(c, tempResult) ?? tempResult;
        }
    }
}
