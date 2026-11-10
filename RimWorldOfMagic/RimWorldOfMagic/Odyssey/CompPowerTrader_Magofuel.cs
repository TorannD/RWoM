using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_MagofuelPower : CompProperties_Power
    {
        public float fuelPerDay = 3f;
        public float heatPerSecond = 6f;
        public CompProperties_MagofuelPower() { compClass = typeof(CompPowerTrader_Magofuel); }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef)) yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(fuelPerDay) || !CompMagofuelReservoir.PositiveFinite(-PowerConsumption) ||
                heatPerSecond < 0 || float.IsNaN(heatPerSecond) || float.IsInfinity(heatPerSecond))
                yield return "Magofuel power and consumption must be positive and finite; heat must be finite and nonnegative.";
            if (!parentDef.HasComp(typeof(CompRefuelable))) yield return "Magofuel generator requires native CompRefuelable.";
        }
    }

    // Native normal Thing ticks run before MapPostTick.PowerNetsTick. Publish only paid
    // power for that single tick. The native network remains responsible for distribution.
    public class CompPowerTrader_Magofuel : CompPowerPlant
    {
        private double paidFuel;
        private bool producingTick;
        private bool paying;
        private int lastProductionTick = -1;
        public new CompProperties_MagofuelPower Props => (CompProperties_MagofuelPower)props;
        public double PaidFuel => paidFuel;
        public double LastHeat { get; private set; }

        public override void SetUpPowerVars() { base.SetUpPowerVars(); PowerOutput = 0f; }
        public override void ReceiveCompSignal(string signal)
        {
            // Empty physical storage is not empty paid burn credit. Native fuel remains
            // an integer store, so refuelling cannot round away a fractional deficit.
            if (signal == "RanOutOfFuel") return;
            base.ReceiveCompSignal(signal);
            if (!PowerOn) PowerOutput = 0f;
        }
        public override void CompTick()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick == lastProductionTick) return;
            lastProductionTick = tick;
            if (parent.Spawned && parent.Map.gameConditionManager.ElectricityDisabled(parent.Map))
            { PowerOutput = 0f; LastHeat = 0; return; }
            // A fuelled source can start itself. Native PowerNet otherwise reserves
            // five watt-days in a partly charged battery before restarting traders,
            // which can strand the only generator indefinitely after a flick/repair.
            if (!PowerOn && parent.Spawned && !parent.Destroyed && PowerNet != null &&
                FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown() &&
                (paidFuel > 0d || (parent.GetComp<CompRefuelable>()?.Fuel ?? 0f) >= 1f))
            {
                PowerOutput = 0f;
                PowerOn = true;
            }
            producingTick = true;
            try { base.CompTick(); } finally { producingTick = false; }
            if (LastHeat > 0 && parent.Spawned) GenTemperature.PushHeat(parent.Position, parent.Map, (float)LastHeat);
        }
        public override void UpdateDesiredPowerOutput() { if (producingTick) ProduceForTicks(1); }

        // Returns watt-days actually paid for this interval, with average native watts
        // for a partially funded final interval. Production uses exactly one normal tick.
        public double ProduceForTicks(int ticks)
        {
            if (paying) return 0;
            paying = true;
            try { return ProducePaidTicks(ticks); }
            finally { paying = false; }
        }

        private double ProducePaidTicks(int ticks)
        {
            PowerOutput = 0f; LastHeat = 0;
            if (ticks <= 0 || parent == null || !parent.Spawned || parent.Destroyed || PowerNet == null || !PowerOn ||
                !FlickUtility.WantsToBeOn(parent) || parent.IsBrokenDown() ||
                !CompMagofuelReservoir.PositiveFinite(Props.fuelPerDay) || !CompMagofuelReservoir.PositiveFinite(-Props.PowerConsumption)) return 0;
            var fuel = parent.GetComp<CompRefuelable>();
            if (fuel == null) return 0;
            var originalMap = parent.Map;
            var originalNet = PowerNet;
            var originalFaction = parent.Faction;
            var originalPosition = parent.Position;
            double needed = (double)Props.fuelPerDay * ticks / 60000d;
            // A dry tank tops up from the beacon network's shared magofuel pool;
            // off-network engines only ever burn their own storage.
            if (fuel.Fuel < 1f && paidFuel < needed)
                MagofuelPool.DrawFuel(parent, (float)Math.Ceiling(needed - paidFuel));
            // Transfer ownership of whole fuel units to the paid burn balance before
            // publishing electricity. Never prepay beyond what this interval needs.
            while (paidFuel < needed && fuel.Fuel >= 1f)
            {
                float before = fuel.Fuel;
                // Native empty-fuel signals run after the debit and may throw or move
                // the building. Preserve paid ownership before any output is granted.
                try { fuel.ConsumeFuel(1f); }
                finally { if (fuel.Fuel == before - 1f) paidFuel += 1d; }
                if (fuel.Fuel != before - 1f || !parent.Spawned || parent.Destroyed ||
                    parent.Map != originalMap || PowerNet != originalNet || parent.Faction != originalFaction ||
                    parent.Position != originalPosition || !PowerOn || !FlickUtility.WantsToBeOn(parent) || parent.IsBrokenDown()) return 0;
            }
            double used = Math.Min(needed, paidFuel);
            paidFuel -= used;
            if (paidFuel < 1e-12) paidFuel = 0d;
            double fraction = used / needed;
            PowerOutput = (float)(-Props.PowerConsumption * fraction);
            LastHeat = Props.heatPerSecond * ticks / 60d * fraction;
            return -Props.PowerConsumption * used / Props.fuelPerDay;
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            PowerOutput = 0f; LastHeat = 0;
            base.PostDeSpawn(map, mode);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref paidFuel, "TM_generatorPaidFuel", 0d);
            Scribe_Values.Look(ref lastProductionTick, "TM_generatorLastProductionTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (double.IsNaN(paidFuel) || double.IsInfinity(paidFuel) || paidFuel < 0) paidFuel = 0;
                PowerOutput = 0f; LastHeat = 0;
            }
        }
        public override string CompInspectStringExtra()
        {
            double remaining = (parent.GetComp<CompRefuelable>()?.Fuel ?? 0) + paidFuel;
            string pool = MagofuelPool.InspectLine(parent);
            return base.CompInspectStringExtra() + "\n" + "TM_MagofuelGeneratorStatus".Translate(
                PowerOutput.ToString("0"), Props.fuelPerDay.ToString("0.##"), paidFuel.ToString("0.####"),
                (remaining / Props.fuelPerDay).ToString("0.##")) + (pool == null ? "" : "\n" + pool);
        }
    }
}



