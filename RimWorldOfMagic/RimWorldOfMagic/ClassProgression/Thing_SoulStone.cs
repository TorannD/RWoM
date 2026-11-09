using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.ClassProgression
{
    // The container owns the original pawn, never a progression snapshot or clone.
    public sealed class Thing_SoulStone : ThingWithComps, IThingHolder, IThingHolderTickable
    {
        private ThingOwner<Pawn> contents;

        public Thing_SoulStone() { contents = new ThingOwner<Pawn>(this, true); }
        public Pawn ContainedSpirit => contents.Count == 1 ? contents[0] : null;
        public override string LabelNoCount => ContainedSpirit == null ? base.LabelNoCount
            : "TM_SoulStoneOccupiedLabel".Translate(base.LabelNoCount, ContainedSpirit.LabelShort).ToString();
        public override string GetInspectString()
        {
            string inherited = base.GetInspectString();
            var spirit = ContainedSpirit;
            if (spirit == null) return inherited;
            string details = "TM_SoulStoneContainedSpirit".Translate(spirit.LabelShort).ToString()
                + "\n" + "TM_SoulStoneRecoveryDebt".Translate().ToString();
            return inherited.NullOrEmpty() ? details : inherited + "\n" + details;
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            foreach (var option in base.GetFloatMenuOptions(pawn)) yield return option;
            foreach (var option in SoulStoneImplant.Orders(pawn,this)) yield return option;
        }
        public ThingOwner GetDirectlyHeldThings() => contents;
        public bool ShouldTickContents => false;
        protected override void Tick()
        {
            base.Tick();
            SpiritRebirthRuntime.TickHeld(this);
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
            => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);

        internal bool TryCapture(Pawn original, Pawn expectedHost, bool hostDied)
        {
            if (Destroyed || contents.Count != 0 || !SpiritRebirthState.TryGetStored(original, out var state)
                || !state.CanEnterStone(original, expectedHost, hostDied)) return false;

            // In 1.7 a spawned Pawn also has a native map ThingOwner. It must
            // undergo DeSpawn, not be removed as though this were an inventory.
            bool wasSpawned = original.Spawned;
            var source = wasSpawned ? null : original.holdingOwner;
            var map = original.Map;
            var position = original.Position;
            var rotation = original.Rotation;
            bool committed = false;
            try
            {
                if (wasSpawned) original.DeSpawn(DestroyMode.WillReplace);
                else source.Remove(original);
                if (!contents.TryAdd(original, false)) return false;
                committed = state.TryEnterStone(original, this, expectedHost, hostDied);
                if (committed)
                {
                    original.GetCompAbilityUserMagic()?.RestoreSavedCooldowns();
                    SpiritRebirthHealthForm.TryRefreshPending(original);
                }
                return committed;
            }
            finally
            {
                if (!committed && !original.Spawned)
                {
                    // Restore the exact native owner, not another host's holder.
                    if (contents.Contains(original)) contents.Remove(original);
                    if (source != null)
                    {
                        if (!source.Contains(original) && !source.TryAdd(original, false))
                            contents.TryAdd(original, false); // preserve custody on catastrophic failure
                    }
                    else if (original.holdingOwner == null)
                    {
                        try { GenSpawn.Spawn(original, position, map, rotation); }
                        finally
                        {
                            if (!original.Spawned && original.holdingOwner == null) contents.TryAdd(original, false);
                        }
                    }
                }
            }
        }

        internal bool TryImplant(Pawn original, HediffComp_SpiritPossession destination)
        {
            var candidate = destination?.parent?.pawn;
            var target = destination?.GetDirectlyHeldThings();
            if (Destroyed || !ReferenceEquals(ContainedSpirit, original)
                || !SpiritRebirthState.TryGet(original, out var state) || !state.HasValidStone(original)
                || !ReferenceEquals(state.Stone, this) || candidate == null || candidate == original
                || SpiritSummonedHost.IsSummoned(candidate) || candidate.Dead || candidate.Destroyed || target == null || target.Count != 0
                || !ReferenceEquals(target.Owner, destination)
                || !ReferenceEquals(candidate.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD)
                    ?.TryGetComp<HediffComp_SpiritPossession>(), destination)) return false;

            bool committed = false;
            try
            {
                contents.Remove(original);
                if (!target.TryAdd(original, false)) return false;
                committed = state.TryEnterHost(original, candidate);
                if (committed)
                {
                    original.GetCompAbilityUserMagic()?.RestoreSavedCooldowns();
                    SpiritRebirthHealthForm.TryRefreshPending(original);
                }
                return committed;
            }
            finally
            {
                if (!committed)
                {
                    if (target.Contains(original)) target.Remove(original);
                    if (original.holdingOwner == null) contents.TryAdd(original, false);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref contents, "containedSpirit", this);
            if (contents == null) contents = new ThingOwner<Pawn>(this, true);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // An occupied stone is indestructible. Map abandonment remains distinct
            // from damage and must retain native ownership until that map is discarded.
            if (contents.Count != 0) return;
            base.Destroy(mode);
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if (contents.Count != 0) { absorbed = true; return; }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }

        public override bool CanStackWith(Thing other)
            => contents.Count == 0 && (!(other is Thing_SoulStone stone) || stone.contents.Count == 0)
                && base.CanStackWith(other);

        public override Thing SplitOff(int count)
        {
            // A normal singleton move returns this exact stone. Never split a
            // malformed occupied stack into a second shell with ambiguous custody.
            if (contents.Count != 0 && count < stackCount) return null;
            return base.SplitOff(count);
        }
    }
}
