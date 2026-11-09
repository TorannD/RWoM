using System;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace TorannMagic.Odyssey
{
    public class CompProperties_MagofuelReservoir : CompProperties
    {
        // One unit of physical magofuel yields ten stored mana units.
        public float manaCapacity = 1000f;
        public float manaPerFuel = 10f;
        public float maxTransfer = 10f;

        public CompProperties_MagofuelReservoir() { compClass = typeof(CompMagofuelReservoir); }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (!CompMagofuelReservoir.PositiveFinite(manaCapacity) ||
                !CompMagofuelReservoir.PositiveFinite(manaPerFuel) ||
                !CompMagofuelReservoir.PositiveFinite(maxTransfer))
                yield return "Magofuel reservoir capacities, conversion and flow must be finite and positive.";
            if (!parentDef.HasComp(typeof(CompRefuelable)))
                yield return "Magofuel reservoir requires CompRefuelable for physical fuel storage.";
        }
    }

    // No automatic ticking: consumers request a bounded transfer, and pay before use.
    // Physical fuel remains owned, refilled and saved by the game's CompRefuelable.
    public class CompMagofuelReservoir : ThingComp
    {
        private float storedMana;
        private bool transferring;
        private float personalReserve = .2f;
        private float fuelReserve;
        private float chemfuelHopper;
        private bool synthesisOn = true;
        private const float SynthManaPerFuel = 10f;
        private const float SynthChemfuelPerFuel = 1f;
        private const float SynthHopperCapacity = 100f;
        private float synthesisTicks;
        private float FuelReserveCapacity => parent?.GetComp<CompRefuelable>() is CompRefuelable fuel
            && PositiveFinite(fuel.Props.fuelCapacity) ? (float)Math.Floor(fuel.Props.fuelCapacity) : 0f;
        public float FuelReserve
        {
            get => MagofuelReserve.IsStorage(this) && PositiveFinite(fuelReserve) ? Math.Min(fuelReserve, FuelReserveCapacity) : 0f;
            set => fuelReserve = float.IsNaN(value) || float.IsInfinity(value) ? 0f : (float)Math.Floor(Math.Max(0f, Math.Min(value, FuelReserveCapacity)));
        }
        public float PersonalReserve
        {
            get => personalReserve;
            set => personalReserve = float.IsNaN(value) || float.IsInfinity(value) ? .2f : Math.Max(.1f, Math.Min(.9f, value));
        }
        public CompProperties_MagofuelReservoir Props => (CompProperties_MagofuelReservoir)props;
        public float StoredMana => storedMana;
        public float ChemfuelHopper => chemfuelHopper;
        public bool SynthesisOn => synthesisOn;
        public float FreeCapacity => Math.Max(0f, Props.manaCapacity - storedMana);
        private bool Usable => parent != null && !parent.Destroyed && !transferring &&
            PositiveFinite(Props.manaCapacity) && PositiveFinite(Props.maxTransfer);
        internal bool UsableInternal => Usable;
        internal static bool PositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storedMana, "TM_reservoirMana", 0f);
            Scribe_Values.Look(ref personalReserve, "TM_reservoirPersonalReserve", .2f);
            Scribe_Values.Look(ref fuelReserve, "TM_reservoirFuelReserve", 0f);
            Scribe_Values.Look(ref chemfuelHopper, "TM_synthChemfuelHopper", 0f);
            Scribe_Values.Look(ref synthesisOn, "TM_synthOn", true);
            // Retain a valid fixed quantity if a later balance patch temporarily reduces capacity.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (fuelReserve < 0f || float.IsNaN(fuelReserve) || float.IsInfinity(fuelReserve))) fuelReserve = 0f;
            if (Scribe.mode == LoadSaveMode.PostLoadInit) PersonalReserve = personalReserve;
            // Do not clamp legitimate saved charge if a later balance patch lowers capacity.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (storedMana < 0f || float.IsNaN(storedMana) || float.IsInfinity(storedMana)))
                storedMana = 0f;
        }

        private Need_Mana ChargingNeed(Pawn pawn, bool requirePosition)
        {
            if (!Usable || !parent.Spawned || parent.Faction == null || !parent.Faction.IsPlayer ||
                pawn == null || !pawn.Spawned || pawn.Map != parent.Map || pawn.Faction != parent.Faction ||
                pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState || pawn.needs == null ||
                (requirePosition && pawn.Position != parent.InteractionCell)) return null;
            var magic = pawn.GetCompAbilityUserMagic();
            // Recompute from current traits, without initializing MagicData or granting abilities.
            if (magic == null || !magic.SetIsMagicUser() || !magic.IsMagicUser) return null;
            var need = magic.Mana;
            if (need == null || !PositiveFinite(need.MaxLevel) || !PositiveFinite(need.CurLevel) ||
                need.CurLevel - .01f < need.MaxLevel * PersonalReserve || FreeCapacity < 1f || Props.maxTransfer < 1f) return null;
            if (!CanPayPersonalMana(pawn, need, .01f)) return null;
            return need;
        }

        public bool CanChargeFromMage(Pawn pawn, bool requirePosition = true) => ChargingNeed(pawn, requirePosition) != null;

        // Each successful pulse costs .01 personal mana and grants one stored unit.
        public float ChargeFromMage(Pawn pawn)
        {
            var need = ChargingNeed(pawn, true);
            if (need == null || !(need.CurLevel - .01f < need.CurLevel) || !(storedMana + 1f > storedMana)) return 0f;
            if (!TryPayPersonalMana(pawn, need, .01f)) return 0f;
            storedMana += 1f;
            parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(1f, Find.TickManager.TicksGame);
            return 1f;
        }

        // Auto-synthesis (magofuel reservoir only): stored mana plus hauled chemfuel
        // slowly becomes magofuel in the same tank — 50 units per day — pausing while
        // inputs are missing or the tank reached its configured target.
        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!synthesisOn || !MagofuelReserve.IsStorage(this)) return;
            var fuel = parent?.GetComp<CompRefuelable>();
            if (fuel == null) return;
            synthesisTicks += delta;
            while (synthesisTicks >= 1200f)
            {
                synthesisTicks -= 1200f;
                if (!TrySynthesizeOne(fuel))
                {
                    // Retry after 60 ticks without accumulating missed production.
                    synthesisTicks = 1200f - 60f;
                    return;
                }
            }
        }

        private bool TrySynthesizeOne(CompRefuelable fuel)
        {
            if (chemfuelHopper < SynthChemfuelPerFuel ||
                Math.Min(fuel.TargetFuelLevel, fuel.Props.fuelCapacity) - fuel.Fuel < 1f) return false;
            // Synthesis feeds from the shared pool when the local mana is short.
            if (storedMana < SynthManaPerFuel)
                MagofuelPool.DrawMana(this, SynthManaPerFuel - storedMana);
            if (storedMana < SynthManaPerFuel) return false;
            chemfuelHopper -= SynthChemfuelPerFuel;
            storedMana -= SynthManaPerFuel;
            fuel.Refuel(1f);
            parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(1f, Find.TickManager.TicksGame);
            return true;
        }

        public void LoadNearbyChemfuel()
        {
            Map map = parent?.Map;
            if (map == null) return;
            int remaining = (int)(SynthHopperCapacity - chemfuelHopper);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, 4f, true))
            {
                if (remaining <= 0) break;
                if (!cell.InBounds(map)) continue;
                Thing stack = null;
                foreach (var candidate in map.thingGrid.ThingsListAt(cell))
                    if (candidate != null && candidate.def == ThingDefOf.Chemfuel) { stack = candidate; break; }
                if (stack == null) continue;
                int take = Math.Min(remaining, stack.stackCount);
                LoadChemfuel(stack, take);
                remaining -= take;
            }
        }

        public void LoadChemfuel(Thing chemfuelStack, int count)
        {
            if (chemfuelStack == null || chemfuelStack.def != ThingDefOf.Chemfuel) return;
            int taken = Math.Min(count, Math.Min(chemfuelStack.stackCount, (int)(SynthHopperCapacity - chemfuelHopper)));
            if (taken <= 0) return;
            chemfuelStack.SplitOff(taken).Destroy(DestroyMode.Vanish);
            chemfuelHopper += taken;
        }

        public void AddChemfuelToHopper(int count)
        {
            chemfuelHopper = Math.Min(chemfuelHopper + count, SynthHopperCapacity);
        }

        public override string CompInspectStringExtra() =>
            "TM_ReservoirCharge".Translate(storedMana.ToString("0.##"), Props.manaCapacity.ToString("0.##")) + "\n" +
            "TM_ReservoirReserve".Translate(PersonalReserve.ToString("P0")) +
            (MagofuelReserve.IsStorage(this) ? "\n" + "TM_ReservoirFuelReserveInspect".Translate(FuelReserve.ToString("0"), MagofuelReserve.Available(parent.GetComp<CompRefuelable>()).ToString("0.##")).ToString() : "") +
            (MagofuelReserve.IsStorage(this) && synthesisOn ? "\n" + "TM_SynthInspect".Translate(chemfuelHopper.ToString("0")).ToString() : "") +
            (MagofuelPool.InspectLine(parent) is string pool ? "\n" + pool : "");

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == null || !parent.Faction.IsPlayer) yield break;
            var convert = new Command_Action
            {
                defaultLabel = "TM_ReservoirConvert".Translate(),
                defaultDesc = "TM_ReservoirConvertDesc".Translate(Props.manaCapacity.ToString("0.##")),
                icon = ContentFinder<Texture2D>.Get("Items/Magofuel"),
                action = () => ConvertAvailableFuel()
            };
            if (FreeCapacity < Props.manaPerFuel || MagofuelReserve.Available(parent.GetComp<CompRefuelable>()) < 1f) convert.Disable();
            yield return convert;
            if (MagofuelReserve.IsStorage(this)) yield return new Command_Action
            {
                defaultLabel = "TM_ReservoirFuelReserveLabel".Translate(FuelReserve.ToString("0")),
                defaultDesc = "TM_ReservoirFuelReserveDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Items/Magofuel"),
                action = () =>
                {
                    var options = new List<FloatMenuOption>();
                    for (int i = 0; i <= 10; i++)
                    {
                        float quantity = (float)Math.Floor(FuelReserveCapacity * i / 10f);
                        options.Add(new FloatMenuOption("TM_ReservoirFuelReserveLabel".Translate(quantity.ToString("0")), () => FuelReserve = quantity));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "TM_ReservoirReserveLabel".Translate(PersonalReserve.ToString("P0")),
                defaultDesc = "TM_ReservoirReserveDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
                action = () =>
                {
                    var options = new List<FloatMenuOption>();
                    for (int i = 1; i <= 9; i++)
                    {
                        float reserve = i / 10f;
                        options.Add(new FloatMenuOption(reserve.ToString("P0"), () => PersonalReserve = reserve));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
            if (MagofuelReserve.IsStorage(this))
            {
                var synth = new Command_Toggle
                {
                    defaultLabel = "TM_SynthToggleLabel".Translate(),
                    defaultDesc = "TM_SynthToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Items/Magofuel"),
                    isActive = () => synthesisOn,
                    toggleAction = () => synthesisOn = !synthesisOn
                };
                yield return synth;
                var load = new Command_Action
                {
                    defaultLabel = "TM_SynthLoad".Translate(chemfuelHopper.ToString("0")),
                    defaultDesc = "TM_SynthLoadDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
                    action = () => LoadNearbyChemfuel()
                };
                if (chemfuelHopper >= SynthHopperCapacity) load.Disable();
                yield return load;
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var option in base.CompFloatMenuOptions(selPawn)) yield return option;
            if (!CanChargeFromMage(selPawn, false)) yield break;
            if (!selPawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Some))
            {
                yield return new FloatMenuOption("TM_ReservoirChargeJob".Translate() + ": " + "TM_ReservoirUnavailable".Translate(), null);
                yield break;
            }
            yield return new FloatMenuOption("TM_ReservoirChargeJob".Translate(), () =>
            {
                if (CanChargeFromMage(selPawn, false))
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_ChargeMagofuelReservoir"), parent), JobTag.Misc);
            });
        }

        public float ConvertFuel(float requestedMana)
            => ConvertFuelForPurpose(requestedMana, null);

        internal float ConvertFuelForReceiver(float requestedMana, Thing receiver)
            => ConvertFuelForPurpose(requestedMana, receiver);

        private float ConvertFuelForPurpose(float requestedMana, Thing receiver)
        {
            if (!Usable || !PositiveFinite(requestedMana) || !PositiveFinite(Props.manaPerFuel)) return 0f;
            var fuel = parent.GetComp<CompRefuelable>();
            if (fuel == null || !PositiveFinite(fuel.Fuel)) return 0f;
            bool allowReserve = MagofuelReserve.IsLifeSupport(receiver);
            float amount = Math.Min(Math.Min(requestedMana, Props.maxTransfer), Math.Min(FreeCapacity, MagofuelReserve.Available(fuel, allowReserve) * Props.manaPerFuel));
            // Native refueling consumes integer stacks and rounds a deficit up. Keep fuel integral
            // so its next refill cannot destroy an item fraction at the capacity clamp.
            float debit = (float)Math.Floor(amount / Props.manaPerFuel);
            amount = debit * Props.manaPerFuel;
            if (!PositiveFinite(amount)) return 0f;
            if (!(fuel.Fuel - debit < fuel.Fuel) || !(storedMana + amount > storedMana)) return 0f;
            transferring = true;
            float fuelBefore = fuel.Fuel;
            float converted = 0f;
            try
            {
                fuel.ConsumeFuel(debit);
            }
            finally
            {
                try
                {
                    // Revalidate even when RanOutOfFuel throws. Preserve allowed paid charge;
                    // a changed reserve or removed life-support purpose restores physical fuel.
                    if (fuel.Fuel == fuelBefore - debit)
                    {
                        if (MagofuelReserve.AllowsDebit(fuel, fuelBefore, debit, allowReserve && MagofuelReserve.IsLifeSupport(receiver)))
                        {
                            storedMana += amount;
                            converted = amount;
                        }
                        else fuel.Refuel(debit);
                    }
                }
                finally { transferring = false; }
            }
            return converted;
        }

        public float ConvertAvailableFuel()
        {
            if (!Usable) return 0f;
            float before = storedMana;
            // A user command batches bounded atomic operations. Hard limit also bounds malformed defs.
            int operations = (int)Math.Min(10000d, Math.Ceiling((double)FreeCapacity / Props.maxTransfer));
            for (int i = 0; i < operations && ConvertFuel(Props.maxTransfer) > 0f; i++) { }
            return storedMana - before;
        }

        // Returns the exact paid amount. Callers must apply only that amount, never the request.
        // A short local tank tops up from its beacon network's shared pool first, so
        // consumers draw from any network tank while off-network tanks stay local.
        public float WithdrawMana(float requestedMana)
        {
            if (!Usable || !PositiveFinite(requestedMana)) return 0f;
            float wanted = Math.Min(requestedMana, Props.maxTransfer);
            if (storedMana < wanted)
                MagofuelPool.DrawMana(this, wanted - storedMana);
            float amount = Math.Min(wanted, storedMana);
            if (!(storedMana - amount < storedMana)) return 0f;
            storedMana -= amount;
            return amount;
        }

        // Restore an exact transaction debit even after ownership/capacity changes in a callback.
        internal void RefundWithdrawnMana(float amount)
        {
            if (PositiveFinite(amount)) storedMana += amount;
        }

        public float TransferManaTo(CompMagofuelReservoir receiver, float requestedMana)
        {
            if (!Usable || receiver == null || receiver == this || !receiver.Usable || !PositiveFinite(requestedMana)) return 0f;
            float amount = Math.Min(Math.Min(requestedMana, Props.maxTransfer), Math.Min(receiver.Props.maxTransfer, Math.Min(storedMana, receiver.FreeCapacity)));
            if (!PositiveFinite(amount)) return 0f;
            if (!(storedMana - amount < storedMana) || !(receiver.storedMana + amount > receiver.storedMana)) return 0f;
            // No callbacks between debit and credit: on the game thread this cannot interleave.
            storedMana -= amount;
            receiver.storedMana += amount;
            parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(amount, Find.TickManager.TicksGame);
            receiver.parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(amount, Find.TickManager.TicksGame);
            return amount;
        }

        // Siphons pay the native personal need and credit this real tank together. The
        // exact native need excludes overridden setters that could reenter during payment.
        internal static bool CanPayPersonalMana(Pawn donor, Need_Mana need, float debit)
        {
            if (need == null || need.GetType() != typeof(Need_Mana) || !PositiveFinite(debit)) return false;
            var magic = donor?.GetCompAbilityUserMagic();
            if (magic?.Mana != need || !PositiveFinite(magic.maxMP)) return false;
            if (!(need.CurLevel - debit < need.CurLevel) || need.CurLevel < debit) return false;
            // Capacity can fall before the next native need update. Its setter clamps
            // to twice maxMP; do not mistake that unrelated loss for the agreed payment.
            if (need.CurLevel - debit > 2f * magic.maxMP) return false;
            return true;
        }

        internal static bool TryPayPersonalMana(Pawn donor, Need_Mana need, float debit)
        {
            if (!CanPayPersonalMana(donor, need, debit)) return false;
            need.CurLevel -= debit;
            return true;
        }

        internal float SiphonRoom => Usable ? Math.Min(Props.maxTransfer, FreeCapacity) : 0f;
        internal bool CanReceiveSiphonMana(float amount) => PositiveFinite(amount) && amount <= SiphonRoom && storedMana + amount > storedMana;
        // Called only after a siphon's complete allocation and payment are validated.
        // No callbacks occur between its personal debit and these tank/buffer credits.
        internal void ReceiveSiphonMana(float amount)
        {
            storedMana += amount;
            parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(amount, Find.TickManager.TicksGame);
        }
    }
}

