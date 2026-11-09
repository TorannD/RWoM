using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TorannMagic.Odyssey;
using VanillaGravshipExpanded;
using Verse;

namespace TorannMagic.VGE
{
    // Loaded solely through the conditional VGE folder. The core DLL has no VGE reference.
    public class CompMagicalThrusterVge : CompMagicalThruster, IGravshipFuelProvider
    {
        public override bool UsesVgeFuelProvider => true;
        public Thing ParentThing => parent;
        MagicalThrusterFuelGroup Group(Building_GravEngine engine, List<CompGravshipThruster> active = null,
            List<IGravshipFuelProvider> others = null)
        {
            var group = MagicalThrusterFuel.Group(engine, this, active);
            // One network is one fuel provider, regardless of how many thrusters discover it.
            others?.RemoveAll(p => p is CompMagicalThrusterVge t && t.engine == engine &&
                MagicalThrusterFuel.Sources(t).Any(group.Sources.Contains));
            return group;
        }
        public bool IsActive(Building_GravEngine engine, List<CompGravshipThruster> activeThrusters, List<IGravshipFuelProvider> otherProviders) =>
            this.engine == engine && Group(engine, activeThrusters).Sources.Count > 0;
        public float CurrentFuel(Building_GravEngine engine) => Group(engine).Fuel;
        public float MaxFuel(Building_GravEngine engine) => Group(engine).Capacity;
        public float CurrentRangeProvidedByFuel(Building_GravEngine engine, List<CompGravshipThruster> activeThrusters, List<IGravshipFuelProvider> otherProviders) =>
            Group(engine, activeThrusters, otherProviders).Range;
        public float MaxRangeProvidedByFuel(Building_GravEngine engine, List<CompGravshipThruster> activeThrusters, List<IGravshipFuelProvider> otherProviders) =>
            Group(engine, activeThrusters, otherProviders).MaxRange;
        public float ConsumeFuelAmount(Building_GravEngine engine, float fuelAmount) => Group(engine).ConsumeAmount(fuelAmount, true);
        public FuelUsageData ConsumeFuelRatio(Building_GravEngine engine, float fuelConsumedRatio,
            List<CompGravshipThruster> activeThrusters, List<IGravshipFuelProvider> otherProviders, bool consumeFuel)
        {
            var group = Group(engine, activeThrusters, otherProviders);
            float amount = MagicalThrusterConsumeFuelPatch.ConsumeProvider(group, fuelConsumedRatio, consumeFuel);
            var data = new FuelUsageData();
            data.totalAmount = amount;
            data.sortingOrder = amount / MagicalThrusterFuelGroup.FuelPerTile;
            // VGE persists ParentThing as the refund key. Record the actual physical reservoirs,
            // never a representative thruster that can move to another network or become inactive.
            foreach (var source in group.Sources)
            {
                float debit = consumeFuel ? (group.LastDebits.TryGetValue(source, out float paid) ? paid : 0f)
                    : (group.Fuel > 0f ? amount * source.Fuel / group.Fuel : 0f);
                if (debit > 0f) data.fuelData[new ReservoirReceipt(source.parent)] = debit;
            }
            if (amount > 0f) data.reportString = amount.ToString("0.##") + " " + "TM_MagicalThrusterFuelName".Translate();
            return data;
        }
        public float AddFuelAmount(Building_GravEngine engine, float amount) => Group(engine).AddFuel(amount);
        public FuelTabEntry GetFuelTabEntry(Building_GravEngine engine, List<CompGravshipThruster> activeThrusters, List<IGravshipFuelProvider> otherProviders)
        {
            var group = Group(engine, activeThrusters, otherProviders);
            var entry = new SimpleMultiLineTextEntry(engine);
            entry.title = "TM_MagicalThrusterFuelName".Translate();
            entry.fuelProviders.AddRange(group.Sources.Select(s => (Thing)s.parent));
            entry.thrusters.AddRange(activeThrusters.OfType<CompMagicalThrusterVge>()
                .Where(t => t.engine == engine && MagicalThrusterFuel.Sources(t).Any(group.Sources.Contains)).Select(t => (Thing)t.parent));
            entry.text.Add("TM_MagicalThrusterFuelAmount".Translate(group.Fuel.ToString("0.##"), group.Capacity.ToString("0.##")));
            entry.text.Add("TM_MagicalThrusterFuelRange".Translate(group.Range.ToString("0.##"), group.MaxRange.ToString("0.##")));
            return entry;
        }
    }

    // Receipt metadata only: these objects never enter the live provider traversal.
    sealed class ReservoirReceipt : IGravshipFuelProvider
    {
        public Thing ParentThing { get; }
        public ReservoirReceipt(Thing thing) { ParentThing = thing; }
        public bool IsActive(Building_GravEngine e, List<CompGravshipThruster> t, List<IGravshipFuelProvider> p) => false;
        public float CurrentFuel(Building_GravEngine e) => 0f;
        public float MaxFuel(Building_GravEngine e) => 0f;
        public float CurrentRangeProvidedByFuel(Building_GravEngine e,List<CompGravshipThruster> t,List<IGravshipFuelProvider> p) => 0f;
        public float MaxRangeProvidedByFuel(Building_GravEngine e,List<CompGravshipThruster> t,List<IGravshipFuelProvider> p) => 0f;
        public float ConsumeFuelAmount(Building_GravEngine e,float n) => 0f;
        public FuelUsageData ConsumeFuelRatio(Building_GravEngine e,float n,List<CompGravshipThruster> t,List<IGravshipFuelProvider> p,bool consume) => new FuelUsageData();
        public float AddFuelAmount(Building_GravEngine e,float n) => 0f;
        public FuelTabEntry GetFuelTabEntry(Building_GravEngine e,List<CompGravshipThruster> t,List<IGravshipFuelProvider> p) => null;
    }

    [StaticConstructorOnStartup]
    public static class MagicalThrusterVgeStartup
    {
        static MagicalThrusterVgeStartup()
        {
            MagicalShieldProjectileAdapters.Register(new WarpodShieldAdapter());
            new Harmony("TorannMagic.VGE.MagicalThrusterRefund").PatchAll(typeof(MagicalThrusterVgeStartup).Assembly);
        }
    }
    [HarmonyPatch(typeof(GravshipFuelProviderUtility), nameof(GravshipFuelProviderUtility.RefundFuelForAllProviders))]
    public static class MagicalThrusterVgeRefundPatch
    {
        public static void Postfix(Building_GravEngine engine, float refundRatio, FuelSpentData fuelSpentData)
        {
            if (engine == null || !engine.Spawned || fuelSpentData?.fuelData == null || refundRatio <= 0f || float.IsNaN(refundRatio) || float.IsInfinity(refundRatio)) return;
            foreach (var entry in fuelSpentData.fuelData)
            {
                var tank = entry.Key as ThingWithComps;
                if (tank == null || (tank.def.defName != "TM_MagofuelReservoir" && !(tank is Building_MagicalGravEngine)) || tank.GetComp<CompMagofuelReservoir>() == null || !tank.Spawned || tank.Map != engine.Map || tank.Faction != engine.Faction || !engine.OnValidSubstructure(tank)) continue;
                var source = tank.GetComp<CompRefuelable>();
                if (!MapComponent_ManaNetwork.CompatibleFuel(source) || entry.Value <= 0f || float.IsNaN(entry.Value) || float.IsInfinity(entry.Value)) continue;
                // Existing VGE owns refund scheduling and the persisted receipt. Only repay its
                // original source; a newly attached network must never receive the refund.
                new MagicalThrusterFuelGroup(new List<CompRefuelable> { source }, 0f).AddFuel(entry.Value * Math.Min(1f, refundRatio));
            }
        }
    }
    [HarmonyPatch(typeof(Building_GravEngine), nameof(Building_GravEngine.ConsumeFuel))]
    public static class MagicalThrusterVgeReceiptRollbackPatch
    {
        public sealed class ReceiptSnapshot
        {
            public FuelSpentData Original;
            public Dictionary<Thing, float> Entries;
        }
        public static void Prefix(Building_GravEngine __instance, out ReceiptSnapshot __state)
        {
            var receipt = __instance.launchInfo?.ExtendedInfo(createIfMissing: false)?.fuelSpentPerTank;
            __state = new ReceiptSnapshot { Original = receipt, Entries = receipt?.fuelData == null ? null : new Dictionary<Thing, float>(receipt.fuelData) };
        }
        public static Exception Finalizer(Building_GravEngine __instance, ReceiptSnapshot __state, Exception __exception)
        {
            if (__exception == null || __state == null) return __exception;
            var info = __instance.launchInfo?.ExtendedInfo(createIfMissing: false);
            if (info != null)
            {
                info.fuelSpentPerTank = __state.Original;
                if (__state.Original != null) __state.Original.fuelData = __state.Entries;
            }
            return __exception;
        }
    }
}
