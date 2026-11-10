using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_ManaSiphon : CompProperties
    {
        public CompProperties_ManaSiphon() { compClass = typeof(CompManaSiphon); }
    }

    // Hidden overflow holds only the paid remainder of an already-started donation.
    public class CompManaSiphon : ThingComp
    {
        public const float BufferCapacity = 100f;
        private float personalReserve = .2f, maximumDonation = .8f;
        private float bufferedMana;
        private int nextDonationTick = -1;
        private int nextDrainTick = -1;
        private bool donating;
        public float BufferedMana => bufferedMana;
        public float PersonalReserve { get => personalReserve; set => personalReserve = Fraction(value, 0f, 1f, .2f); }
        public float MaximumDonation { get => maximumDonation; set => maximumDonation = Fraction(value, 0f, 1f, .8f); }
        static float Fraction(float value, float min, float max, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
        CompManaNetworkEndpoint Endpoint => parent.GetComp<CompManaNetworkEndpoint>();
        List<CompManaNetworkEndpoint> Tanks => parent.Map.GetComponent<MapComponent_ManaNetwork>().SiphonTanks(Endpoint);
        static float Room(CompManaNetworkEndpoint endpoint)
        {
            var tank = endpoint.parent.GetComp<CompMagofuelReservoir>();
            float target = endpoint.ManaMode == ManaNetworkMode.Receive ? endpoint.ManaTarget : 1f;
            return Math.Min(tank.SiphonRoom, Math.Max(0f, tank.Props.manaCapacity * target - tank.StoredMana));
        }
        public Need_Mana DonationNeed(Pawn pawn, bool requirePosition = false)
        {
            if (parent == null || !parent.Spawned || parent.Destroyed || parent.Faction == null || !parent.Faction.IsPlayer ||
                parent.GetComp<CompFlickable>()?.SwitchIsOn == false || pawn == null || !pawn.Spawned || pawn.Map != parent.Map ||
                pawn.Faction != parent.Faction || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState || !pawn.Awake() ||
                pawn.GetPosture() != PawnPosture.Standing || pawn.needs == null ||
                (requirePosition && pawn.Position != parent.InteractionCell)) return null;
            var work = DefDatabase<WorkTypeDef>.GetNamedSilentFail("TM_Magic");
            if (work == null || pawn.workSettings == null || !pawn.workSettings.EverWork || pawn.workSettings.GetPriority(work) <= 0) return null;
            var magic = pawn.GetCompAbilityUserMagic();
            if (magic == null || !magic.SetIsMagicUser() || !magic.IsMagicUser) return null;
            var need = magic.Mana;
            return need != null && need.GetType() == typeof(Need_Mana) && CompMagofuelReservoir.PositiveFinite(need.MaxLevel) &&
                CompMagofuelReservoir.PositiveFinite(need.CurLevel) && need.CurLevel <= 2f * magic.maxMP ? need : null;
        }
        public bool CanBeginDonation(Pawn pawn, bool requirePosition = false)
        {
            var need = DonationNeed(pawn, requirePosition);
            return need != null && need.CurLevel + .000001f >= need.MaxLevel &&
                CanContinueDonation(pawn, need.MaxLevel, 0f, requirePosition);
        }
        float Allowance(Need_Mana need, float startCapacity, float paidThisVisit) =>
            Math.Min(.01f, Math.Min(need.CurLevel - need.MaxLevel * PersonalReserve, startCapacity * MaximumDonation - paidThisVisit));
        public bool CanContinueDonation(Pawn pawn, float startCapacity, float paidThisVisit, bool requirePosition = true)
        {
            var need = DonationNeed(pawn, requirePosition);
            return need != null && PersonalReserve < 1f && CompMagofuelReservoir.PositiveFinite(startCapacity) && paidThisVisit >= 0f &&
                !float.IsInfinity(paidThisVisit) && Allowance(need, startCapacity, paidThisVisit) > .000001f && Tanks.Any(t => Room(t) > .0001f);
        }
        // Returns actual personal mana paid. Tanks receive first; the paid remainder
        // of the pulse may enter the hidden buffer, never a new pulse into full tanks.
        public float Donate(Pawn pawn, float startCapacity, float paidThisVisit)
        {
            int now = Find.TickManager.TicksGame;
            if (donating || now < 0 || now < nextDonationTick) return 0f;
            if (bufferedMana > 0f) DrainBuffer();
            if (bufferedMana > 0f || !CanContinueDonation(pawn, startCapacity, paidThisVisit)) return 0f;
            nextDonationTick = now + 60;
            donating = true;
            try
            {
                var need = DonationNeed(pawn, true);
                float debit = Allowance(need, startCapacity, paidThisVisit);
                float remaining = debit * 100f;
                var allocations = new List<KeyValuePair<CompMagofuelReservoir, float>>();
                foreach (var endpoint in Tanks)
                {
                    var tank = endpoint.parent.GetComp<CompMagofuelReservoir>();
                    float amount = Math.Min(remaining, Room(endpoint));
                    if (!tank.CanReceiveSiphonMana(amount)) continue;
                    allocations.Add(new KeyValuePair<CompMagofuelReservoir, float>(tank, amount));
                    remaining -= amount;
                    if (remaining <= 0f) break;
                }
                if (allocations.Count == 0 || remaining > BufferCapacity - bufferedMana ||
                    !CompMagofuelReservoir.TryPayPersonalMana(pawn, need, debit)) return 0f;
                // Native need setter and native tank credits have no callbacks. The whole
                // allocation is committed on the game thread without topology interleaving.
                foreach (var allocation in allocations) allocation.Key.ReceiveSiphonMana(allocation.Value);
                bufferedMana += remaining;
                parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(debit * 100f, now);
                parent.GetComp<CompCrystalGlow>()?.NotifyService(now, 60);
                NotifyRelayTransfer(debit * 100f - remaining, now);
                return debit;
            }
            finally { donating = false; }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (bufferedMana > 0f) DrainBuffer();
        }
        public float DrainBuffer()
        {
            int now = Find.TickManager.TicksGame;
            // Check the saved deadline before any topology work. A load never catches up.
            if (donating || now < 0 || now < nextDrainTick || bufferedMana <= 0f) return 0f;
            nextDrainTick = now + 60;
            if (!parent.Spawned || parent.Destroyed || parent.Faction?.IsPlayer != true ||
                parent.GetComp<CompFlickable>()?.SwitchIsOn == false) return 0f;
            donating = true;
            try
            {
                float transferred = 0f;
                foreach (var endpoint in Tanks)
                {
                    var tank = endpoint.parent.GetComp<CompMagofuelReservoir>();
                    float amount = Math.Min(Math.Min(bufferedMana, MapComponent_ManaNetwork.ManaBudget - transferred), Room(endpoint));
                    if (!tank.CanReceiveSiphonMana(amount) || !(bufferedMana - amount < bufferedMana)) continue;
                    bufferedMana -= amount;
                    tank.ReceiveSiphonMana(amount);
                    transferred += amount;
                    if (bufferedMana <= 0f || transferred >= MapComponent_ManaNetwork.ManaBudget) break;
                }
                if (transferred > 0f)
                {
                    parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(transferred, now);
                    parent.GetComp<CompCrystalGlow>()?.NotifyService(now, 60);
                    NotifyRelayTransfer(transferred, now);
                }
                return transferred;
            }
            finally { donating = false; }
        }
        void NotifyRelayTransfer(float amount, int now)
        {
            if (!CompMagofuelReservoir.PositiveFinite(amount)) return;
            // One transaction-time graph query, never a render/tick polling query.
            foreach (var beacon in parent.Map.GetComponent<MapComponent_ManaNetwork>().GetConnectedBeacons(Endpoint))
                beacon.parent.GetComp<CompCrystalGlow>()?.NotifyTransfer(amount, now);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref personalReserve, "TM_siphonPersonalReserve", .2f);
            Scribe_Values.Look(ref maximumDonation, "TM_siphonMaximumDonation", .8f);
            Scribe_Values.Look(ref nextDonationTick, "TM_siphonNextDonationTick", -1);
            Scribe_Values.Look(ref bufferedMana, "TM_siphonBufferedMana", 0f);
            Scribe_Values.Look(ref nextDrainTick, "TM_siphonNextDrainTick", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                PersonalReserve = personalReserve; MaximumDonation = maximumDonation; donating = false;
                // Preserve legitimate paid overflow if a future balance patch lowers capacity.
                if (bufferedMana < 0f || float.IsNaN(bufferedMana) || float.IsInfinity(bufferedMana)) bufferedMana = 0f;
            }
        }
        public override string CompInspectStringExtra()
        {
            string text = "TM_SiphonInspect".Translate(PersonalReserve.ToString("P0"), MaximumDonation.ToString("P0"));
            text += "\n" + "TM_SiphonNeedsFullMana".Translate();
            if (parent.Spawned && !Tanks.Any(t => Room(t) > .0001f)) text += "\n" + "TM_SiphonNoTanks".Translate();
            return text;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction?.IsPlayer != true) yield break;
            yield return Setting("TM_SiphonReserve", PersonalReserve, 0, 10, value => PersonalReserve = value);
            yield return Setting("TM_SiphonDonation", MaximumDonation, 0, 10, value => MaximumDonation = value);
        }
        static Command_Action Setting(string key, float current, int min, int max, Action<float> set) => new Command_Action
        {
            defaultLabel = (key + "Label").Translate(current.ToString("P0")),
            defaultDesc = (key + "Desc").Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/transfermana"),
            action = () =>
            {
                var options = new List<FloatMenuOption>();
                for (int i = min; i <= max; i++) { float value = i / 10f; options.Add(new FloatMenuOption(value.ToString("P0"), () => set(value))); }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
    }
}
