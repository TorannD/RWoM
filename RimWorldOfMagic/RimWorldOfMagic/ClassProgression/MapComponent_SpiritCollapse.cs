using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Owns the original while a host loss on this map is being settled.
    public sealed class MapComponent_SpiritCollapse : MapComponent, IThingHolder, IThingHolderTickable
    {
        private ThingOwner<Thing> contents;
        private List<SpiritPendingHostCollapse> records = new List<SpiritPendingHostCollapse>();
        private int nextId = 1;
        private readonly HashSet<Pawn> busy = new HashSet<Pawn>();
        public MapComponent_SpiritCollapse(Map map) : base(map) { contents = new ThingOwner<Thing>(this, false); }
        public IThingHolder ParentHolder => map;
        public bool ShouldTickContents => false;
        public ThingOwner GetDirectlyHeldThings() => contents;
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);
        internal bool Registered => map != null && map.components != null && map.components.Contains(this);
        internal bool ContainsRecord(SpiritPendingHostCollapse receipt) => records.Contains(receipt);
        private bool DirectlyOwns(Thing thing) => thing != null && !thing.Destroyed && !thing.Spawned
            && ReferenceEquals(thing.holdingOwner, contents) && ReferenceEquals(contents.Owner, this) && contents.Contains(thing);
        private bool OwnsStone(SpiritPendingHostCollapse receipt) => receipt.Stone != null && !receipt.Stone.Destroyed
            && (DirectlyOwns(receipt.Stone) || SpiritRebirthStaging.MapOwns(receipt.Stone, map));
        internal bool OwnsOriginal(SpiritPendingHostCollapse receipt)
        {
            var pawn = receipt.Original;
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.Discarded || pawn.Spawned) return false;
            if (DirectlyOwns(pawn)) return true;
            var stone = receipt.Stone; var owner = stone?.GetDirectlyHeldThings();
            return OwnsStone(receipt) && ReferenceEquals(stone.ContainedSpirit, pawn) && ReferenceEquals(pawn.holdingOwner, owner)
                && ReferenceEquals(owner.Owner, stone) && owner.Contains(pawn);
        }
        private SpiritPendingHostCollapse Find(Pawn pawn)
        {
            for (int i = 0; i < records.Count; i++) if (ReferenceEquals(records[i].Original, pawn)) return records[i];
            return null;
        }

        internal static bool IsPendingAccountingOwner(Pawn pawn)
        {
            if (!SpiritRebirthState.TryGet(pawn, out var state) || !state.HasPendingMapDeath(pawn)
                || pawn.Dead || pawn.Destroyed || pawn.Discarded) return false;
            var owner = pawn.holdingOwner?.Owner;
            MapComponent_SpiritCollapse keeper = owner as MapComponent_SpiritCollapse;
            if (keeper == null && owner is Thing_SoulStone stone)
            {
                keeper = stone.holdingOwner?.Owner as MapComponent_SpiritCollapse;
                if (keeper == null && stone.holdingOwner?.Owner is Map stoneMap && stoneMap.components != null
                    && SpiritRebirthStaging.MapOwns(stone, stoneMap))
                    keeper = stoneMap.GetComponent<MapComponent_SpiritCollapse>();
            }
            if (keeper == null) return false;
            var receipt = keeper.Find(pawn);
            return receipt != null && receipt.OwnsOriginal && state.MatchesPendingMapDeath(pawn, keeper.map, receipt.Id);
        }

        internal bool TryBeginHostDeath(Pawn original, Pawn deadHost)
            => deadHost?.Dead == true && TryBeginHostLoss(original, deadHost, false);

        internal bool TryBeginHostLoss(Pawn original, Pawn deadHost, bool forced)
        {
            if (!Registered || original == null || deadHost == null || !SpiritRebirthState.TryGetStored(original, out var state)) return false;
            var existing = Find(original);
            if (existing != null)
            {
                if (existing.SourceHostId != deadHost.thingIDNumber) return false;
                Retry(existing, true);
                return Retained(existing);
            }
            if (!SpiritRebirthStaging.MapOwns(deadHost, map) || !deadHost.Position.InBounds(map)
                || (!forced && !deadHost.Dead) || !state.CanEnterStone(original, deadHost, deadHost.Dead) || !busy.Add(original)) return false;
            SpiritPendingHostCollapse receipt = null;
            try
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_SoulStone");
                if (def?.thingClass != typeof(Thing_SoulStone)) return false;
                var stone = ThingMaker.MakeThing(def) as Thing_SoulStone;
                if (stone == null) return false;
                try { contents.TryAdd(stone, false); }
                catch { if (!DirectlyOwns(stone)) throw; }
                if (!DirectlyOwns(stone)) return false;
                receipt = new SpiritPendingHostCollapse(this) { Id = nextId++, Original = original, Stone = stone,
                    SourceHostId = deadHost.thingIDNumber, Anchor = deadHost.Position, Forced = forced };
                records.Add(receipt);
                if (!state.BeginPendingMapDeath(original, deadHost, receipt) && !state.MatchesPendingMapDeath(original, map, receipt.Id))
                {
                    records.Remove(receipt);
                    if (stone.ContainedSpirit == null && DirectlyOwns(stone)) { contents.Remove(stone); stone.Destroy(); }
                    return false;
                }
                Process(receipt);
                return Retained(receipt);
            }
            catch (Exception error)
            {
                if (receipt == null) Compatibility.TM_Log.Warning("SpiritMapCollapse", "Death custody could not start: " + error);
                else Warn(receipt, error);
                return receipt != null && Retained(receipt);
            }
            finally
            {
                if (receipt != null) receipt.RetryAt = FindTick() + 250;
                busy.Remove(original);
            }
        }

        private bool Retained(SpiritPendingHostCollapse receipt)
            => receipt.OwnsOriginal || (OwnsStone(receipt) && SpiritRebirthState.TryGetStored(receipt.Original, out var state)
                && state.HasValidStone(receipt.Original) && ReferenceEquals(state.Stone, receipt.Stone));
        private static int FindTick() => Verse.Find.TickManager?.TicksGame ?? 0;
        private void Warn(SpiritPendingHostCollapse receipt, Exception error)
        {
            if (receipt.WarningIssued) return;
            receipt.WarningIssued = true;
            Compatibility.TM_Log.Warning("SpiritMapCollapse", "Pending collapse needs local retry; actual custody must remain verified: " + error);
        }
        private void Retry(SpiritPendingHostCollapse receipt, bool requested = false)
        {
            if (!receipt.Registered || receipt.Original == null || (!requested && FindTick() < receipt.RetryAt) || !busy.Add(receipt.Original)) return;
            try { Process(receipt); }
            catch (Exception error) { Warn(receipt, error); }
            finally { receipt.RetryAt = FindTick() + 250; busy.Remove(receipt.Original); }
        }

        private void Process(SpiritPendingHostCollapse receipt)
        {
            var pawn = receipt.Original; var stone = receipt.Stone;
            if (!receipt.Registered || !SpiritRebirthState.TryGetStored(pawn, out var state)) return;
            if (!OwnsStone(receipt))
            {
                // A moved empty candidate is no longer ours. Only the already
                // secured raw original authorizes a replacement; never reclaim
                // a foreign item or replace an occupied stone.
                if (!DirectlyOwns(pawn) || !state.MatchesPendingMapDeath(pawn, map, receipt.Id)
                    || (stone != null && stone.GetDirectlyHeldThings().Count != 0)) return;
                var def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_SoulStone");
                if (def?.thingClass != typeof(Thing_SoulStone)) return;
                var replacement = ThingMaker.MakeThing(def) as Thing_SoulStone;
                if (replacement == null) return;
                try { contents.TryAdd(replacement, false); }
                catch { if (!DirectlyOwns(replacement)) throw; }
                if (!DirectlyOwns(replacement)) return;
                receipt.Stone = stone = replacement;
            }
            if (state.HasValidStone(pawn) && ReferenceEquals(state.Stone, stone)) { Place(receipt); return; }
            if (!state.MatchesPendingMapDeath(pawn, map, receipt.Id)) return;
            if (!receipt.OwnsOriginal) Evacuate(receipt);
            if (!receipt.OwnsOriginal) throw new InvalidOperationException("Original was not secured in map keeper custody; source loss is not authorized.");
            if (!state.SecurePendingMapDeath(pawn, receipt)) return;
            if (DirectlyOwns(pawn))
            {
                try { contents.Remove(pawn); stone.GetDirectlyHeldThings().TryAdd(pawn, false); }
                finally { EnsureKeeperCustody(receipt); }
            }
            if (state.TryCompletePendingMapDeath(pawn, receipt))
            {
                SpiritRebirthHealthForm.TryRefreshPending(pawn);
                Place(receipt);
            }
        }

        private void Evacuate(SpiritPendingHostCollapse receipt)
        {
            var pawn = receipt.Original; var source = pawn.holdingOwner;
            var possession = source?.Owner as HediffComp_SpiritPossession;
            var host = possession?.parent?.pawn;
            if (host == null || (!receipt.Forced && !host.Dead) || host.thingIDNumber != receipt.SourceHostId
                || !ReferenceEquals(possession.SpiritPawn, pawn) || !source.Contains(pawn)
                || !SpiritRebirthState.TryGetStored(pawn, out var state) || !state.CanEnterStone(pawn, host, host.Dead)) return;
            try { source.Remove(pawn); contents.TryAdd(pawn, false); }
            finally
            {
                if (!receipt.OwnsOriginal && pawn.holdingOwner == null)
                {
                    try { receipt.Stone.GetDirectlyHeldThings().TryAdd(pawn, false); }
                    finally { if (pawn.holdingOwner == null) source.TryAdd(pawn, false); }
                }
            }
        }
        private void EnsureKeeperCustody(SpiritPendingHostCollapse receipt)
        {
            if (receipt.OwnsOriginal) return;
            if (receipt.Original.holdingOwner == null) contents.TryAdd(receipt.Original, false);
            if (!receipt.OwnsOriginal) throw new InvalidOperationException("Pending original lacks verified map keeper custody.");
        }
        private void Place(SpiritPendingHostCollapse receipt)
        {
            var stone = receipt.Stone;
            if (SpiritRebirthStaging.MapOwns(stone, map)) { records.Remove(receipt); return; }
            if (!DirectlyOwns(stone) || !receipt.Anchor.InBounds(map)) return;
            foreach (var cell in GenRadial.RadialCellsAround(receipt.Anchor, 3.9f, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetThingList(map).Count != 0) continue;
                try { GenPlace.TryPlaceThing(stone, cell, map, ThingPlaceMode.Direct, out var _); }
                finally
                {
                    if (SpiritRebirthStaging.MapOwns(stone, map)) records.Remove(receipt);
                    else if (!stone.Spawned && stone.holdingOwner == null) contents.TryAdd(stone, false);
                    if (!OwnsStone(receipt)) throw new InvalidOperationException("Occupied candidate lacks verified native outer custody.");
                }
                return;
            }
        }

        public override void MapComponentTick()
        {
            // Beta progression kill switch: keep ExposeData running so pending custody
            // from an old feature-on save stays loadable (recoverable by re-enabling),
            // but never tick stale records while the feature is disabled.
            if (!ClassProgressionFeature.Enabled) return;
            if (records.Count == 0) return;
            for (int i = 0; i < records.Count; i++)
            {
                var receipt = records[i]; var pawn = receipt.Original;
                if (IsPendingAccountingOwner(pawn))
                {
                    SpiritRebirthHealthForm.TryRefreshPending(pawn);
                    pawn.GetCompAbilityUserMagic()?.CompTick(); pawn.GetCompAbilityUserMight()?.CompTick();
                }
                else if (OwnsStone(receipt)) SpiritRebirthRuntime.TickHeld(receipt.Stone);
            }
            int attempted = 0;
            for (int i = records.Count - 1; i >= 0 && attempted < 4; i--)
                if (FindTick() >= records[i].RetryAt) { attempted++; Retry(records[i]); }
        }
        public override void ExposeData()
        {
            Scribe_Deep.Look(ref contents, "pendingContents", this);
            Scribe_Collections.Look(ref records, "pendingCollapses", LookMode.Deep, this);
            Scribe_Values.Look(ref nextId, "nextCollapseId", 1);
            if (contents == null) contents = new ThingOwner<Thing>(this, false);
            if (records == null) records = new List<SpiritPendingHostCollapse>();
        }
    }
}
