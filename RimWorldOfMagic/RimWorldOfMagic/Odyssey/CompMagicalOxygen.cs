using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public enum OxygenOutputMode { Atmosphere, PipeNetwork }
    public enum OxygenSupplyMode
    {
        Mana,
        Magicyte
    }

    public class CompProperties_OxygenManaBuffer : CompProperties_MagofuelReservoir
    {
        public CompProperties_OxygenManaBuffer()
        {
            compClass = typeof(CompOxygenManaBuffer);
            manaCapacity = 100f;
            maxTransfer = 10f;
        }
    }

    // Uses the existing paid reservoir transfer contract, without the tank's conversion
    // and mage-charging commands. Local magicyte belongs to the oxygen supply mode.
    public class CompOxygenManaBuffer : CompMagofuelReservoir
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            yield break;
        }

        public override string CompInspectStringExtra() => "TM_OxygenBuffer".Translate(StoredMana.ToString("0.##"), Props.manaCapacity.ToString("0.##"));
    }

    public class CompProperties_MagicalOxygen : CompProperties
    {
        public float manaPerSecond = .1f;
        public float manaPerMagicyte = 10f;
        public int pulseInterval = 60;
        public CompProperties_MagicalOxygen()
        {
            compClass = typeof(CompMagicalOxygen);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(manaPerSecond) || !CompMagofuelReservoir.PositiveFinite(manaPerMagicyte) || pulseInterval <= 0)
                yield return "Magical oxygen rates and pulse interval must be finite and positive.";
            if (!parentDef.HasComp(typeof(CompOxygenManaBuffer)))
                yield return "Magical oxygen requires its mana buffer.";
            if (!parentDef.HasComp(typeof(CompRefuelable)))
                yield return "Magical oxygen requires native magicyte storage.";
        }
    }

    public class CompMagicalOxygen : ThingComp
    {
        public const float ManaPerCellSecond = .0004f;
        private OxygenSupplyMode supplyMode;
        private double paidMagicyteMana;
        private int lastSupplyTick = -1;
        private bool supplying;
        private OxygenOutputMode outputMode;
        private double pendingPipeOxygen;
        private int lastPipeTick = -1;
        public OxygenOutputMode OutputMode
        {
            get => outputMode;
            set
            {
                outputMode = Enum.IsDefined(typeof(OxygenOutputMode), value) ? value : OxygenOutputMode.Atmosphere;
            }
        }
        // Only one output owns a pulse; duplicate comps must not duplicate supply.
        private IMagicalOxygenOutput PipeOutput => parent?.AllComps?.OfType<IMagicalOxygenOutput>().FirstOrDefault();
        public bool PipeNetworkAvailable => PipeOutput?.Available == true;
        public double PendingPipeOxygen => pendingPipeOxygen;
        public override void CompTick() => TickPipeOutput();
        public override void CompTickInterval(int delta) => TickPipeOutput();
        void TickPipeOutput()
        {
            if (OutputMode != OxygenOutputMode.PipeNetwork || !CanOperate) return;
            int now = Find.TickManager.TicksGame;
            if (now < 0 || (lastPipeTick < 0 ? now < Props.pulseInterval : (long)now - lastPipeTick < Props.pulseInterval)) return;
            lastPipeTick = now;
            PipeOutput?.Supply(this, ref pendingPipeOxygen);
        }
        public CompProperties_MagicalOxygen Props => (CompProperties_MagicalOxygen)props;
        public OxygenSupplyMode SupplyMode { get => supplyMode; set => supplyMode = Enum.IsDefined(typeof(OxygenSupplyMode), value) ? value : OxygenSupplyMode.Mana; }
        public double PaidMagicyteMana => paidMagicyteMana;
        public float LastSuppliedMana { get; private set; }
        public float SupportedCells => Props.manaPerSecond / ManaPerCellSecond;

        public double AvailableSupply
        {
            get
            {
                if (!CanOperate)
                    return 0d;
                if (SupplyMode == OxygenSupplyMode.Mana)
                    return parent.GetComp<CompOxygenManaBuffer>()?.StoredMana ?? 0f;
                var fuel = parent.GetComp<CompRefuelable>();
                return paidMagicyteMana + (CompatibleMagicyte(fuel) ? Math.Floor(fuel.Fuel) * Props.manaPerMagicyte : 0d);
            }
        }

        public bool CanOperate => parent != null && parent.Spawned && (OutputMode == OxygenOutputMode.PipeNetwork || parent.Map.Biome.inVacuum) && !parent.Destroyed && parent.Faction != null && parent.Faction.IsPlayer && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown() && Props.pulseInterval > 0 && CompMagofuelReservoir.PositiveFinite(Props.manaPerSecond) && CompMagofuelReservoir.PositiveFinite(Props.manaPerMagicyte);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map?.GetComponent<MapComponent_MagicalAtmosphere>()?.Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map?.GetComponent<MapComponent_MagicalAtmosphere>()?.Unregister(this);
            LastSuppliedMana = 0f;
            base.PostDeSpawn(map, mode);
        }

        static bool CompatibleMagicyte(CompRefuelable fuel)
        {
            if (fuel == null || fuel.GetType() != typeof(CompRefuelable))
                return false;
            var p = fuel.Props;
            return !p.destroyOnNoFuel && !p.fuelIsMortarBarrel && !p.atomicFueling && p.FuelMultiplierCurrentDifficulty == 1f && CompMagofuelReservoir.PositiveFinite(p.fuelCapacity) && p.fuelCapacity <= 1000000f && p.fuelFilter != null && p.fuelFilter.AllowedDefCount == 1 && p.fuelFilter.AllowedThingDefs.First().defName == "RawMagicyte" && fuel.Fuel >= 0 && !float.IsNaN(fuel.Fuel) && !float.IsInfinity(fuel.Fuel);
        }

        // A caller supplies only the current pulse's useful demand. No missed-time
        // replay, automatic mode fallback, or output is granted without paid input.
        public float SupplyPulse(float requestedMana)
        {
            if (supplying || !CanOperate || !CompMagofuelReservoir.PositiveFinite(requestedMana))
                return 0f;
            int now = Find.TickManager.TicksGame;
            if (now < 0 || (lastSupplyTick >= 0 && (long)now - lastSupplyTick < Props.pulseInterval) || (lastSupplyTick < 0 && now < Props.pulseInterval))
                return 0f;
            float wanted = Math.Min(requestedMana, Props.manaPerSecond * Props.pulseInterval / 60f);
            if (!CompMagofuelReservoir.PositiveFinite(wanted))
                return 0f;
            lastSupplyTick = now;
            LastSuppliedMana = 0f;
            supplying = true;
            try
            {
                if (SupplyMode == OxygenSupplyMode.Mana)
                {
                    LastSuppliedMana = parent.GetComp<CompOxygenManaBuffer>()?.WithdrawMana(wanted) ?? 0f;
                    return LastSuppliedMana;
                }

                var originalMap = parent.Map;
                var originalFaction = parent.Faction;
                var originalPosition = parent.Position;
                var originalRotation = parent.Rotation;
                var fuel = parent.GetComp<CompRefuelable>();
                if (paidMagicyteMana < wanted && CompatibleMagicyte(fuel) && fuel.Fuel >= 1f)
                {
                    float before = fuel.Fuel;
                    float yield = Props.manaPerMagicyte;
                    // Even a throwing native signal has already consumed its physical
                    // unit: retain that payment as residue instead of losing or replaying it.
                    try
                    {
                        fuel.ConsumeFuel(1f);
                    }
                    finally
                    {
                        if (fuel.Fuel == before - 1f)
                            paidMagicyteMana += yield;
                    }
                }

                if (!CanOperate || SupplyMode != OxygenSupplyMode.Magicyte || parent.Map != originalMap || parent.Faction != originalFaction || parent.Position != originalPosition || parent.Rotation != originalRotation)
                    return 0f;
                float supplied = (float)Math.Min(wanted, paidMagicyteMana);
                // Round a partial final pulse down, preserving the unused paid fraction in residue.
                if (supplied > paidMagicyteMana)
                    supplied = BitConverter.ToSingle(BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(supplied), 0) - 1), 0);
                if (!CompMagofuelReservoir.PositiveFinite(supplied) || !(paidMagicyteMana - supplied < paidMagicyteMana))
                    return 0f;
                paidMagicyteMana -= supplied;
                LastSuppliedMana = supplied;
                return supplied;
            }
            finally
            {
                supplying = false;
                if (LastSuppliedMana > 0f) parent.GetComp<CompCrystalGlow>()?.NotifyService(now, Props.pulseInterval);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref supplyMode, "TM_oxygenSupplyMode", OxygenSupplyMode.Mana);
            Scribe_Values.Look(ref paidMagicyteMana, "TM_oxygenPaidMagicyteMana", 0d);
            Scribe_Values.Look(ref lastSupplyTick, "TM_oxygenLastSupplyTick", -1);
            Scribe_Values.Look(ref outputMode, "TM_oxygenOutputMode", OxygenOutputMode.Atmosphere);
            Scribe_Values.Look(ref pendingPipeOxygen, "TM_oxygenPendingPipeOxygen", 0d);
            Scribe_Values.Look(ref lastPipeTick, "TM_oxygenLastPipeTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                SupplyMode = supplyMode;
                OutputMode = outputMode;
                if (double.IsNaN(pendingPipeOxygen) || double.IsInfinity(pendingPipeOxygen) || pendingPipeOxygen < 0d) pendingPipeOxygen = 0d;
                if (double.IsNaN(paidMagicyteMana) || double.IsInfinity(paidMagicyteMana) || paidMagicyteMana < 0d)
                    paidMagicyteMana = 0d;
                supplying = false;
                LastSuppliedMana = 0f;
            }
        }

        static string ModeLabel(OxygenSupplyMode mode) => (mode == OxygenSupplyMode.Mana ? "TM_OxygenModeMana" : "TM_OxygenModeMagicyte").Translate();
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == null || !parent.Faction.IsPlayer)
                yield break;
            if (PipeNetworkAvailable || OutputMode == OxygenOutputMode.PipeNetwork)
                yield return new Command_Action
                {
                    defaultLabel = "TM_OxygenOutput".Translate(OutputLabel),
                    defaultDesc = "TM_OxygenOutputDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
                    action = () => OutputMode = OutputMode == OxygenOutputMode.Atmosphere ? OxygenOutputMode.PipeNetwork : OxygenOutputMode.Atmosphere
                };
            yield return new Command_Action
            {
                defaultLabel = "TM_OxygenMode".Translate(ModeLabel(SupplyMode)),
                defaultDesc = "TM_OxygenModeDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
                action = () =>
                {
                    var options = new List<FloatMenuOption>();
                    foreach (OxygenSupplyMode mode in Enum.GetValues(typeof(OxygenSupplyMode)))
                    {
                        var selected = mode;
                        options.Add(new FloatMenuOption(ModeLabel(mode), () => SupplyMode = selected));
                    }

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }

        public override string CompInspectStringExtra()
        {
            if (OutputMode == OxygenOutputMode.PipeNetwork)
                return "TM_OxygenOutput".Translate(OutputLabel) + "\n" + "TM_OxygenPipeInfo".Translate() + "\n" + "TM_OxygenMode".Translate(ModeLabel(SupplyMode)) + "\n" + "TM_OxygenPaidMagicyte".Translate(paidMagicyteMana.ToString("0.##")) + "\n" + "TM_OxygenPipePending".Translate(pendingPipeOxygen.ToString("0.###"));
            float pressure = parent.Spawned ? parent.Map.GetComponent<MapComponent_MagicalAtmosphere>()?.PressureFractionFor(this) ?? 0f : 0f;
            string status = !CanOperate ? "TM_OxygenStatusOff" : pressure > 0f ? "TM_OxygenStatusRunning" : AvailableSupply <= 0d ? "TM_OxygenStatusEmpty" : "TM_OxygenStatusIdle";
            return status.Translate() + "\n" + "TM_OxygenPressure".Translate(pressure.ToString("P0")) + "\n" + "TM_OxygenMode".Translate(ModeLabel(SupplyMode)) + "\n" + "TM_OxygenPaidMagicyte".Translate(paidMagicyteMana.ToString("0.##")) + "\n" + "TM_OxygenCapacity".Translate(SupportedCells.ToString("0.##"), Props.manaPerSecond.ToString("0.###"));
        }
        string OutputLabel => (OutputMode == OxygenOutputMode.Atmosphere ? "TM_OxygenOutputAtmosphere" : "TM_OxygenOutputPipe").Translate();
    }
}

