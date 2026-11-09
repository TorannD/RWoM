using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class CompProperties_GrandAtmosphereDome : CompProperties
    {
        public float fuelPerDay = 100f;
        public int ritualTicks = 3600;
        public float ritualManaFraction = .1f;
        public float personalReserve = .2f;
        public CompProperties_GrandAtmosphereDome() { compClass = typeof(CompGrandAtmosphereDome); }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef)) yield return error;
            if (!Valid) yield return "Grand atmosphere dome requires positive fuel/day, ritual duration and mana fraction, and a finite personal reserve between zero and one minus the ritual fraction.";
            if (!parentDef.HasComp(typeof(CompRefuelable))) yield return "Grand atmosphere dome requires physical fuel storage.";
        }
        public bool Valid => CompMagofuelReservoir.PositiveFinite(fuelPerDay) && ritualTicks > 0 && CompMagofuelReservoir.PositiveFinite(ritualManaFraction) && ritualManaFraction <= 1f && personalReserve >= 0f && personalReserve <= 1f - ritualManaFraction;
    }

    public class CompGrandAtmosphereDome : ThingComp
    {
        private bool armed;
        private double paidFuelCredit;
        private int lastPaidTick = -1;
        private long receiptEnd = -1;
        private long activeUntil = -1;
        private bool paying;
        private bool lowFuelWarned;
        public CompProperties_GrandAtmosphereDome Props => (CompProperties_GrandAtmosphereDome)props;
        public bool Armed => armed;
        public double PaidFuelCredit => paidFuelCredit;
        private CompRefuelable Fuel => parent.GetComp<CompRefuelable>();
        private double AvailableFuel => paidFuelCredit + (Fuel?.Fuel ?? 0f);
        public bool LowFuel => AvailableFuel < Props.fuelPerDay * .1d;
        public bool CanOperate => ModsConfig.OdysseyActive && parent != null && parent.Spawned && !parent.Destroyed && parent.Faction != null && parent.Faction.IsPlayer && parent.Map.Biome.inVacuum && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown() && Props.Valid;
        public bool ValidateArmed()
        {
            if (armed && !CanOperate) Disarm();
            return armed;
        }
        // Read-only presentation snapshot; unlike HasPaidCoverage this never disarms gameplay.
        public bool VisualCoverage => armed && CanOperate && !paying && Find.TickManager.TicksGame >= lastPaidTick && Find.TickManager.TicksGame < activeUntil;
        public bool HasPaidCoverage => ValidateArmed() && !paying && Find.TickManager.TicksGame >= lastPaidTick && Find.TickManager.TicksGame < activeUntil;
        private void Disarm()
        {
            armed = false;
            activeUntil = -1;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref armed, "TM_grandDomeArmed", false);
            Scribe_Values.Look(ref paidFuelCredit, "TM_grandDomeCredit", 0d);
            Scribe_Values.Look(ref lastPaidTick, "TM_grandDomeLastPaidTick", -1);
            Scribe_Values.Look(ref receiptEnd, "TM_grandDomeReceiptEnd", -1L);
            Scribe_Values.Look(ref lowFuelWarned, "TM_grandDomeLowFuelWarned", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                activeUntil = -1;
                paying = false;
                if (paidFuelCredit < 0d || double.IsNaN(paidFuelCredit) || double.IsInfinity(paidFuelCredit)) paidFuelCredit = 0d;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<MapComponent_GrandAtmosphereDome>().Register(this);
        }
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Disarm();
            map.GetComponent<MapComponent_GrandAtmosphereDome>().Unregister(this);
            base.PostDeSpawn(map, mode);
        }
        public override void CompTick() => PayInterval(Find.TickManager.TicksGame, 1);
        public override void CompTickInterval(int delta) => PayInterval(Find.TickManager.TicksGame, delta);
        internal void PrepareTick(int now) => PayInterval(now, 1);
        private void PayInterval(int now, int delta)
        {
            if (paying || delta <= 0 || !ValidateArmed() || !parent.Map.GetComponent<MapComponent_GrandAtmosphereDome>().IsSelected(this)) return;
            // Receipts persist across saves, transfers and interrupted callbacks. Never replay an
            // overlapping interval, and never infer payment from elapsed time after suspension.
            if (now < 0 || now <= lastPaidTick || now < receiptEnd) return;
            if (!parent.Map.GetComponent<MapComponent_GrandAtmosphereDome>().ReservePayment(this, now, delta)) return;
            double demand = (double)Props.fuelPerDay * delta / 60000d;
            lastPaidTick = now;
            receiptEnd = (long)now + delta;
            activeUntil = -1;
            paying = true;
            try
            {
                if (!LowFuel) lowFuelWarned = false;
                double deficit = demand - paidFuelCredit;
                if (deficit > 1e-10)
                {
                    var fuel = Fuel;
                    // Keep the native tank integral: native refueling rounds deficits to items.
                    // Store the already paid fraction here instead, so refilling never loses it.
                    float debit = (float)Math.Min(Math.Ceiling(deficit - 1e-10), Math.Floor(fuel?.Fuel ?? 0f));
                    if (debit > 0f)
                    {
                        float before = fuel.Fuel;
                        try { fuel.ConsumeFuel(debit); }
                        finally
                        {
                            // Native RanOutOfFuel signals may throw or reenter. Ownership follows
                            // the actual debit even when a later callback fails.
                            double paid = Math.Max(0d, before - fuel.Fuel);
                            paidFuelCredit += paid;
                        }
                    }
                }
                // A native fuel signal can despawn, switch off or transfer ownership of this
                // building. The prepaid fuel remains owned, but no interval may be purchased.
                if (!ValidateArmed()) return;
                if (paidFuelCredit + 1e-10 < demand)
                {
                    Disarm();
                    Messages.Message("TM_GrandDomeExhausted".Translate(), parent, MessageTypeDefOf.NegativeEvent);
                    return;
                }
                paidFuelCredit = Math.Max(0d, paidFuelCredit - demand);
                if (ValidateArmed()) activeUntil = receiptEnd;
                if (LowFuel && !lowFuelWarned && armed)
                {
                    lowFuelWarned = true;
                    Messages.Message("TM_GrandDomeLowFuel".Translate((AvailableFuel / Props.fuelPerDay).ToString("0.###")), parent, MessageTypeDefOf.CautionInput);
                }
            }
            finally { paying = false; }
        }
        private Need_Mana ActivationNeed(Pawn pawn, bool requirePosition)
        {
            if (armed || !CanOperate || AvailableFuel < Props.fuelPerDay / 60000d || pawn == null || !pawn.Spawned || pawn.Map != parent.Map || pawn.Faction != parent.Faction || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState || pawn.needs == null || (requirePosition && pawn.Position != parent.InteractionCell)) return null;
            var magic = pawn.GetCompAbilityUserMagic();
            if (magic == null || !magic.SetIsMagicUser() || !magic.IsMagicUser) return null;
            var need = magic.Mana;
            if (need == null || !CompMagofuelReservoir.PositiveFinite(need.MaxLevel) || !CompMagofuelReservoir.PositiveFinite(need.CurLevel) || need.CurLevel - need.MaxLevel * Props.ritualManaFraction < need.MaxLevel * Props.personalReserve) return null;
            if (!CompMagofuelReservoir.CanPayPersonalMana(pawn, need, need.MaxLevel * Props.ritualManaFraction)) return null;
            return need;
        }
        public bool CanActivate(Pawn pawn, bool requirePosition = true) => ActivationNeed(pawn, requirePosition) != null;
        public bool CompleteActivation(Pawn pawn)
        {
            var need = ActivationNeed(pawn, true);
            if (need == null) return false;
            float debit = need.MaxLevel * Props.ritualManaFraction;
            if (!(need.CurLevel - debit < need.CurLevel)) return false;
            if (!CompMagofuelReservoir.TryPayPersonalMana(pawn, need, debit)) return false;
            armed = true;
            parent.Map.GetComponent<MapComponent_GrandAtmosphereDome>().InvalidateSelection();
            return true;
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            if (!CanActivate(pawn, false)) yield break;
            if (!pawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Some))
            {
                yield return new FloatMenuOption("TM_GrandDomeRitual".Translate() + ": " + "TM_GrandDomeUnavailable".Translate(), null);
                yield break;
            }
            yield return new FloatMenuOption("TM_GrandDomeRitual".Translate(), () =>
            {
                if (CanActivate(pawn, false)) pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_ActivateGrandDome"), parent), JobTag.Misc);
            });
        }
        public override string CompInspectStringExtra()
        {
            string status = !ValidateArmed() ? "TM_GrandDomeUnarmed" : parent.Map.GetComponent<MapComponent_GrandAtmosphereDome>().IsSelected(this) ? "TM_GrandDomeActive" : "TM_GrandDomeStandby";
            return status.Translate().ToString() + (LowFuel ? "\n" + "TM_GrandDomeLowFuel".Translate((AvailableFuel / Props.fuelPerDay).ToString("0.###")).ToString() : "");
        }
    }
}
