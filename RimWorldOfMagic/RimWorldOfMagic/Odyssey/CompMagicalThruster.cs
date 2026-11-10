using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_MagicalThruster : CompProperties_GravshipThruster
    {
        public CompProperties_MagicalThruster() { compClass = typeof(CompMagicalThruster); providesFuel = true; }
    }

    public class CompMagicalThruster : CompGravshipThruster
    {
        public virtual bool UsesVgeFuelProvider => false;
        public bool NativeCanBeActive => base.CanBeActive;
        // Empty reservoirs remain connected: VGE must be able to refund a fully depleted launch.
        // No source query calls CanBeActive, avoiding network/activation recursion.
        public override bool CanBeActive => NativeCanBeActive && MagicalThrusterFuel.Sources(this).Count > 0;
    }

    public sealed class MagicalThrusterFuelGroup
    {
        static readonly HashSet<CompRefuelable> paying = new HashSet<CompRefuelable>();
        public const float FuelPerTile = 5f;
        public readonly List<CompRefuelable> Sources;
        public readonly float ThrusterRange;
        public Func<bool> StillConnected;
        public readonly Dictionary<CompRefuelable, float> LastDebits = new Dictionary<CompRefuelable, float>();
        public MagicalThrusterFuelGroup(List<CompRefuelable> sources, float range)
        {
            Sources = sources.Where(MapComponent_ManaNetwork.CompatibleFuel).Distinct().OrderBy(s => s.parent.thingIDNumber).ToList();
            ThrusterRange = Positive(range) ? range : 0f;
        }
        static bool Positive(float n) => n > 0f && !float.IsNaN(n) && !float.IsInfinity(n);
        public float Fuel => Sources.Sum(s => s.Fuel);
        public float Capacity => Sources.Sum(s => s.Props.fuelCapacity);
        public float Range => Math.Min(ThrusterRange, Fuel / FuelPerTile);
        public float MaxRange => Math.Min(ThrusterRange, Capacity / FuelPerTile);
        public float ConsumeRatio(float ratio, bool consume)
        {
            if (!Positive(ratio)) return 0f;
            return ConsumeAmount(Range * FuelPerTile * Math.Min(1f, ratio), consume);
        }
        public float ConsumeAmount(float amount, bool consume)
        {
            if (!Positive(amount) || Sources.Any(paying.Contains) || (StillConnected != null && !StillConnected())) return 0f;
            float total = Fuel;
            amount = Math.Min(amount, total);
            if (!consume || amount <= 0f) return amount;
            // Snapshot before any native RanOutOfFuel callbacks. Never recompute the ratio mid-debit.
            var payments = Sources.Select(s => new KeyValuePair<CompRefuelable, float>(s, amount * (s.Fuel / total))).ToList();
            var debited = new Dictionary<CompRefuelable, float>();
            LastDebits.Clear();
            paying.UnionWith(Sources);
            try
            {
                foreach (var payment in payments)
                {
                    if (!MapComponent_ManaNetwork.CompatibleFuel(payment.Key) || (StillConnected != null && !StillConnected()))
                        throw new InvalidOperationException("Magical thruster network changed during launch payment.");
                    float before = payment.Key.Fuel;
                    try { payment.Key.ConsumeFuel(payment.Value); }
                    finally { LastDebits[payment.Key] = debited[payment.Key] = Math.Min(payment.Value, Math.Max(0f, before - payment.Key.Fuel)); }
                }
                if (StillConnected != null && !StillConnected())
                    throw new InvalidOperationException("Magical thruster network changed during launch payment.");
                return debited.Values.Sum();
            }
            catch
            {
                // Native signals can throw after the fuel field changes. Restore measured payment,
                // including the throwing source, before propagating a failed launch transaction.
                foreach (var debit in debited)
                {
                    if (debit.Value <= 0f) continue;
                    float before = debit.Key.Fuel;
                    try { debit.Key.Refuel(debit.Value); }
                    catch { /* Continue restoring later sources; preserve the initiating failure. */ }
                    finally { LastDebits[debit.Key] = Math.Max(0f, debit.Value - Math.Max(0f, debit.Key.Fuel - before)); }
                }
                throw;
            }
            finally { paying.ExceptWith(Sources); }
        }
        public float AddFuel(float amount)
        {
            if (!Positive(amount) || Sources.Any(paying.Contains)) return 0f;
            float added = 0f;
            paying.UnionWith(Sources);
            try
            {
                foreach (var source in Sources)
                {
                    if (!MapComponent_ManaNetwork.CompatibleFuel(source) || (StillConnected != null && !StillConnected())) break;
                    float before = source.Fuel;
                    float credit = Math.Min(amount - added, Math.Max(0f, source.Props.fuelCapacity - before));
                    if (credit <= 0f) continue;
                    try { source.Refuel(credit); }
                    finally { added += Math.Max(0f, source.Fuel - before); }
                }
                return added;
            }
            finally { paying.ExceptWith(Sources); }
        }
    }

    public static class MagicalThrusterFuel
    {
        static List<List<CompManaNetworkEndpoint>> Networks(Building_GravEngine engine) =>
            engine == null || !engine.Spawned ? new List<List<CompManaNetworkEndpoint>>() :
            engine.Map.GetComponent<MapComponent_ManaNetwork>().ShipFuelNetworks(engine);
        static List<CompRefuelable> Reservoirs(List<CompManaNetworkEndpoint> members) => members
            .Where(e => e.FuelMode == ManaNetworkMode.Send && (e.parent.def.defName == "TM_MagofuelReservoir" || e.parent is Building_MagicalGravEngine) && e.parent.GetComp<CompMagofuelReservoir>() != null)
            .Select(e => e.parent.GetComp<CompRefuelable>()).Where(MapComponent_ManaNetwork.CompatibleFuel).Distinct().ToList();
        public static List<CompRefuelable> Sources(CompMagicalThruster thruster)
        {
            var endpoint = thruster.parent.GetComp<CompManaNetworkEndpoint>();
            if (endpoint == null || endpoint.FuelMode != ManaNetworkMode.Receive || thruster.engine == null) return new List<CompRefuelable>();
            var members = Networks(thruster.engine).FirstOrDefault(n => n.Contains(endpoint));
            return members == null ? new List<CompRefuelable>() : Reservoirs(members);
        }
        public static MagicalThrusterFuelGroup Group(Building_GravEngine engine, CompMagicalThruster member, List<CompGravshipThruster> active = null)
        {
            var endpoint = member.parent.GetComp<CompManaNetworkEndpoint>();
            var members = Networks(engine).FirstOrDefault(n => n.Contains(endpoint));
            if (members == null || member.engine != engine || endpoint.FuelMode != ManaNetworkMode.Receive) return new MagicalThrusterFuelGroup(new List<CompRefuelable>(), 0f);
            float range = 0f;
            foreach (var e in members)
            {
                var t = e.parent.GetComp<CompMagicalThruster>();
                if (t == null || t.engine != engine || e.FuelMode != ManaNetworkMode.Receive || !engine.GravshipComponents.Contains(t)) continue;
                if (active != null ? !active.Contains(t) : !t.NativeCanBeActive) continue;
                range += t.Props.statOffsets.GetStatOffsetFromList(StatDefOf.GravshipRange);
            }
            var sources = Reservoirs(members);
            var group = new MagicalThrusterFuelGroup(sources, range);
            group.StillConnected = () => member.engine == engine && member.parent.Spawned &&
                new HashSet<CompRefuelable>(Sources(member)).SetEquals(sources);
            return group;
        }
        public static List<MagicalThrusterFuelGroup> NativeGroups(Building_GravEngine engine) => Groups(engine, false);
        public static List<MagicalThrusterFuelGroup> Groups(Building_GravEngine engine, bool includeVge)
        {
            var groups = new List<MagicalThrusterFuelGroup>();
            var seen = new HashSet<CompRefuelable>();
            foreach (var t in engine.GravshipComponents.OfType<CompMagicalThruster>())
            {
                if ((!includeVge && t.UsesVgeFuelProvider) || t.engine != engine || !t.parent.Spawned || !t.CanBeActive) continue;
                var g = Group(engine, t);
                if (g.Sources.Count == 0 || g.Sources.Any(seen.Contains)) continue;
                seen.UnionWith(g.Sources);
                groups.Add(g);
            }
            return groups;
        }
    }

    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.MaxLaunchDistance), MethodType.Getter)]
    public static class MagicalThrusterLaunchDistancePatch
    {
        public static void Postfix(Building_GravEngine __instance, ref int __result)
        {
            // Chemical/other providers cannot subsidize an empty magical network's range.
            // Keep the native path cost, layer transitions and fuel-use factors untouched.
            float factor = __instance.FuelUseageFactor;
            float unfunded = MagicalThrusterFuel.Groups(__instance, true).Sum(g => g.ThrusterRange -
                (g.Range <= 0f ? 0f : factor <= 0f ? g.ThrusterRange : Math.Min(g.ThrusterRange, g.Range / factor)));
            __result = Math.Max(0, (int)Math.Floor(__result - unfunded));
        }
    }

    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.TotalFuel), MethodType.Getter)]
    public static class MagicalThrusterTotalFuelPatch
    {
        public static void Postfix(Building_GravEngine __instance, ref float __result) =>
            __result += MagicalThrusterFuel.NativeGroups(__instance).Sum(g => g.Range * 10f);
    }
    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.MaxFuel), MethodType.Getter)]
    public static class MagicalThrusterMaxFuelPatch
    {
        public static void Postfix(Building_GravEngine __instance, ref float __result) =>
            __result += MagicalThrusterFuel.NativeGroups(__instance).Sum(g => g.MaxRange * 10f);
    }
    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.ConsumeFuel))]
    public static class MagicalThrusterConsumeFuelPatch
    {
        public sealed class Payment
        {
            public List<KeyValuePair<MagicalThrusterFuelGroup, float>> Groups;
            public List<KeyValuePair<MagicalThrusterFuelGroup, float>> Snapshots;
            public Dictionary<CompRefuelable, float> Before;
            public HashSet<CompRefuelable> NativeSources;
            public Dictionary<CompRefuelable, float> NativeDebits = new Dictionary<CompRefuelable, float>();
            public Dictionary<CompRefuelable, float> ProviderDebits = new Dictionary<CompRefuelable, float>();
            public int Cooldown;
        }
        [ThreadStatic] internal static Payment ActivePayment;
        public static void Prefix(Building_GravEngine __instance, PlanetTile tile, out Payment __state)
        {
            __state = null;
            if (ActivePayment != null) throw new InvalidOperationException("Recursive gravship launch payment.");
            var allGroups = MagicalThrusterFuel.Groups(__instance, true);
            if (allGroups.Count == 0 || !GravshipUtility.TryGetPathFuelCost(__instance.Map.Tile, tile, out float cost, out int _)) return;
            var groups = MagicalThrusterFuel.NativeGroups(__instance);
            float total = __instance.TotalFuel;
            if (total > 0f)
            {
                var native = __instance.GravshipComponents
                    .Where(c => c.parent.Spawned && c.Props.providesFuel && c.CanBeActive)
                    .Select(c => c.parent.GetComp<CompRefuelable>()).Where(c => c != null).ToHashSet();
                var sources = allGroups.SelectMany(g => g.Sources).Distinct();
                __state = new Payment
                {
                    Groups = groups.Select(g => new KeyValuePair<MagicalThrusterFuelGroup, float>(g, g.Range * MagicalThrusterFuelGroup.FuelPerTile * Math.Min(1f, cost / total))).ToList(),
                    Snapshots = allGroups.Select(g => new KeyValuePair<MagicalThrusterFuelGroup, float>(g, g.Range)).ToList(),
                    Before = sources.ToDictionary(s => s, s => s.Fuel),
                    NativeSources = native,
                    Cooldown = __instance.cooldownCompleteTick
                };
                ActivePayment = __state;
            }
        }
        public static float ConsumeProvider(MagicalThrusterFuelGroup group, float ratio, bool consume)
        {
            var transaction = ActivePayment;
            if (!consume || transaction == null) return group.ConsumeRatio(ratio, consume);
            var snapshot = transaction.Snapshots.FirstOrDefault(p => new HashSet<CompRefuelable>(p.Key.Sources).SetEquals(group.Sources));
            if (snapshot.Key == null || group.Sources.Any(s => !transaction.Before.TryGetValue(s, out float before) || before != s.Fuel))
                throw new InvalidOperationException("Magical provider sources changed during launch payment.");
            if (ratio < 0f || float.IsNaN(ratio) || float.IsInfinity(ratio))
                throw new InvalidOperationException("Invalid magical provider launch ratio.");
            float amount = snapshot.Value * MagicalThrusterFuelGroup.FuelPerTile * Math.Min(1f, ratio);
            try
            {
                float paid = group.ConsumeAmount(amount, true);
                if (Math.Abs(paid - amount) > Math.Max(.0001f, amount * .00001f))
                    throw new InvalidOperationException("Magical provider launch payment was incomplete.");
                return paid;
            }
            finally
            {
                foreach (var debit in group.LastDebits)
                {
                    transaction.ProviderDebits.TryGetValue(debit.Key, out float prior);
                    transaction.ProviderDebits[debit.Key] = prior + debit.Value;
                }
            }
        }
        // Only the successful native consumption call pays. Queries and canceled targeting never enter here.
        public static void Postfix(Payment __state)
        {
            if (__state == null) return;
            foreach (var payment in __state.Groups)
            {
                // Native tank signals may have changed a magic source since the prefix.
                // Never silently recompute a smaller launch bill from that changed balance.
                if (payment.Key.Sources.Any(s => s.Fuel != __state.Before[s]))
                    throw new InvalidOperationException("Magical thruster fuel changed during native launch payment.");
                float paid = payment.Key.ConsumeAmount(payment.Value, true);
                if (Math.Abs(paid - payment.Value) > Math.Max(.0001f, payment.Value * .00001f))
                    throw new InvalidOperationException("Magical thruster launch payment was incomplete.");
            }
        }
        public static Exception Finalizer(Building_GravEngine __instance, Payment __state, Exception __exception)
        {
            if (__state == null) return __exception;
            if (ReferenceEquals(ActivePayment, __state)) ActivePayment = null;
            if (__exception == null) return null;
            var debits = __state.NativeDebits.Concat(__state.ProviderDebits).Concat(__state.Groups.SelectMany(p => p.Key.LastDebits))
                .GroupBy(p => p.Key).Select(g => new KeyValuePair<CompRefuelable, float>(g.Key, g.Sum(p => p.Value)));
            foreach (var source in debits)
            {
                if (source.Value <= 0f) continue;
                try { source.Key.Refuel(source.Value); }
                catch { /* Refueled callbacks may throw after credit; continue the remaining refunds. */ }
            }
            __instance.cooldownCompleteTick = __state.Cooldown;
            return __exception;
        }
    }
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.ConsumeFuel))]
    public static class MagicalThrusterNativeDebitPatch
    {
        [ThreadStatic] static int depth;
        public static void Prefix(CompRefuelable __instance, float __0, out bool __state)
        {
            __state = MagicalThrusterConsumeFuelPatch.ActivePayment != null;
            if (!__state) return;
            var payment = MagicalThrusterConsumeFuelPatch.ActivePayment;
            if (depth++ == 0 && payment.NativeSources.Contains(__instance))
            {
                // Native ConsumeFuel subtracts before broadcasting RanOutOfFuel. Nested signal
                // effects belong to their own caller and must never be refunded as launch payment.
                float debit = Math.Min(__instance.Fuel, Math.Max(0f, __0));
                payment.NativeDebits.TryGetValue(__instance, out float prior);
                payment.NativeDebits[__instance] = prior + debit;
            }
        }
        public static Exception Finalizer(bool __state, Exception __exception)
        {
            if (__state) depth--;
            return __exception;
        }
    }
}
