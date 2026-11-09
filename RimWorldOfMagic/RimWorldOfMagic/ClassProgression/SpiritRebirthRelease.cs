using System;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal enum SpiritReleaseResult { Rejected, RecoveryRequired, Released }

    internal sealed partial class SpiritRebirthRitualTransaction
    {
        private bool releasePreparing, releaseSpawnCompleted, releaseInventoryRecovery, released;
        private Func<bool> releaseCommitGuard;

        internal Thing RecoveryCircle => circle;
        internal bool CanOrderPaidRecovery(Pawn caster, Pawn selectedOriginal, Thing selectedCircle)
            => caster != null && ReferenceEquals(caster, creator) && ReferenceEquals(selectedOriginal, original)
                && ReferenceEquals(selectedCircle, circle) && PaidOriginal(original) && !released
                && !caster.Dead && !caster.Destroyed && !caster.Downed && caster.Spawned && caster.Map == map
                && caster.IsColonistPlayerControlled && RecoveryInventoryValid() && caster.carryTracker != null
                && (caster.carryTracker.CarriedThing == null || CreatorCarriesOriginal())
                && circle is Building_TMMagicCircle largeCircle && SpiritRebirthStaging.MapOwns(circle, map)
                && circle.Faction == caster.Faction && !RimWorld.ForbidUtility.IsForbidden(circle, caster)
                && !largeCircle.IsActive && !largeCircle.IsPending && largeCircle.MageList.Count == 0
                && SpiritRebirthState.TryGet(original, out var state) && state.Form == SpiritRebirthForm.Dormant
                && state.Host == null && state.Stone == null
                && (CreatorCarriesOriginal() || KnownInventoryRecovery()
                    || (SpiritRebirthFormPolicy.HasMapCustody(original) && original.Map == map));

        internal bool RecoveryDestinationValid(Pawn caster, IntVec3 cell) => DestinationValid(caster, map, cell);
        internal bool RecoveryCarriesOriginal(Pawn caster) => ReferenceEquals(caster, creator) && CreatorCarriesOriginal();
        internal bool RecoveryCompleted(Pawn caster)
            => ReferenceEquals(caster, creator) && PaidOriginal(original) && released
                && SpiritRebirthFormPolicy.IsSpawnedEthereal(original) && original.Map == map;

        private bool PendingOriginal(Pawn pawn)
            => version == 1 && ReferenceEquals(original, pawn) && pawn != null && !pawn.Dead
                && !pawn.Destroyed && !pawn.Discarded && ReferenceEquals(ClassIdentity.Component(pawn)?.SpiritRitual, this)
                && attempted && ValidManifest();

        private bool PaidOriginal(Pawn pawn)
            => PendingOriginal(pawn) && converted && paid && completed && !refundPending
                && debits.All(d => d.consumed && !d.refunded && d.escrow == null);

        private bool CreatorCarriesOriginal()
        {
            var owner = creator?.carryTracker?.GetDirectlyHeldThings();
            return creator != null && !creator.Destroyed && owner != null && owner.Count == 1
                && ReferenceEquals(owner.Owner, creator.carryTracker)
                && ReferenceEquals(creator.carryTracker.CarriedThing, original)
                && ReferenceEquals(original.holdingOwner, owner) && owner.Contains(original) && !original.Spawned;
        }

        internal static bool IsPreparing(Pawn pawn)
        {
            var receipt = ClassIdentity.Component(pawn)?.SpiritRitual;
            return receipt != null && receipt.releasePreparing && !receipt.released && receipt.PaidOriginal(pawn)
                && SpiritRebirthState.TryGet(pawn, out var state) && state.Form == SpiritRebirthForm.Dormant
                && state.Host == null && state.Stone == null
                && (receipt.CreatorCarriesOriginal()
                    || (SpiritRebirthFormPolicy.HasMapCustody(pawn) && pawn.Map == receipt.map)
                    || receipt.KnownInventoryRecovery());
        }

        internal static bool IsPendingAccountingOwner(Pawn pawn)
        {
            var receipt = ClassIdentity.Component(pawn)?.SpiritRitual;
            return receipt != null && !receipt.released && receipt.PendingOriginal(pawn)
                && SpiritRebirthState.TryGet(pawn, out var state) && state.Form == SpiritRebirthForm.Dormant
                && state.Host == null && state.Stone == null
                && (receipt.CreatorCarriesOriginal() || receipt.KnownInventoryRecovery()
                    || (SpiritRebirthFormPolicy.HasMapCustody(pawn) && pawn.Map == receipt.map));
        }

        private bool RecoveryInventoryValid()
            => creator != null && !creator.Destroyed && creator.inventory?.innerContainer != null
                && ReferenceEquals(creator.inventory.innerContainer.Owner, creator.inventory);

        private bool KnownInventoryRecovery()
            => releaseInventoryRecovery && RecoveryInventoryValid() && !original.Spawned
                && ReferenceEquals(original.holdingOwner, creator.inventory.innerContainer)
                && creator.inventory.innerContainer.Contains(original);

        private bool TryInventoryFallback()
        {
            if (!RecoveryInventoryValid() || original.Spawned || original.holdingOwner != null) return false;
            releaseInventoryRecovery = true;
            return creator.inventory.innerContainer.TryAdd(original, false) && KnownInventoryRecovery();
        }

        internal bool CanCommitRelease(Pawn pawn)
            => IsPreparing(pawn) && SpiritRebirthFormPolicy.HasMapCustody(pawn)
                && ReferenceEquals(ClassIdentity.Component(pawn)?.SpiritRitual, this)
                && creator != null && !creator.Dead && !creator.Destroyed && creator.Spawned && creator.Map == map
                && (releaseCommitGuard == null || releaseCommitGuard());

        private bool DestinationValid(Pawn caster, Map destination, IntVec3 cell)
        {
            if (!ReferenceEquals(caster, creator) || caster == null || caster.Dead || !caster.Spawned
                || !ReferenceEquals(destination, map) || caster.Map != destination || destination == null
                || !cell.InBounds(destination) || original.def == null || original.kindDef == null
                || original.stackCount != 1 || !GenAdj.OccupiedRect(cell, original.Rotation, original.def.Size).InBounds(destination)) return false;
            // GenSpawn has no non-destructive WipeMode. An empty, standable footprint
            // makes its native wipe pass a no-op and prevents relocating bystanders.
            foreach (var occupied in GenAdj.OccupiedRect(cell, original.Rotation, original.def.Size))
                if (!occupied.Standable(destination) || occupied.GetThingList(destination).Count != 0) return false;
            return true;
        }

        internal SpiritReleaseResult TryRelease(Pawn caster, Map destination, IntVec3 cell)
            => ReleaseCore(caster, destination, cell, null);

        internal SpiritReleaseResult TryReleaseWithGuard(Pawn caster, Map destination, IntVec3 cell, Func<bool> commitGuard)
            => commitGuard == null ? SpiritReleaseResult.Rejected : ReleaseCore(caster, destination, cell, commitGuard);

        private SpiritReleaseResult ReleaseCore(Pawn caster, Map destination, IntVec3 cell, Func<bool> commitGuard)
        {
            if (caster == null || !PaidOriginal(original) || !ReferenceEquals(caster, creator)
                || !SpiritRebirthState.TryGet(original, out var state)) return SpiritReleaseResult.Rejected;
            if (released)
                return state.Form == SpiritRebirthForm.Ethereal && SpiritRebirthFormPolicy.HasMapCustody(original)
                    ? SpiritReleaseResult.Released : SpiritReleaseResult.RecoveryRequired;
            if (caster.Dead || caster.Destroyed || !caster.Spawned || caster.Map != map || destination != map)
                return SpiritReleaseResult.Rejected;
            bool resumeSpawned = releasePreparing && releaseSpawnCompleted && IsPreparing(original)
                && SpiritRebirthFormPolicy.HasMapCustody(original) && original.Map == destination;
            if (state.Form != SpiritRebirthForm.Dormant || state.Host != null || state.Stone != null
                || creator?.carryTracker == null || !RecoveryInventoryValid() || (!resumeSpawned && (!CreatorCarriesOriginal()
                    || !DestinationValid(caster, destination, cell)))) return SpiritReleaseResult.Rejected;
            if (!inFlight.Add(original)) return SpiritReleaseResult.Rejected;
            var source = creator.carryTracker.GetDirectlyHeldThings();
            try
            {
                releaseCommitGuard = commitGuard;
                if (releaseCommitGuard != null && !releaseCommitGuard()) return SpiritReleaseResult.RecoveryRequired;
                releasePreparing = true;
                SpiritRebirthActionPolicy.ClearExistingJobs(original);
                if (!SpiritRebirthAbilities.Ensure(original) || original.needs == null || original.health?.hediffSet == null)
                    return SpiritReleaseResult.RecoveryRequired;
                original.needs.AddOrRemoveNeedsAsAppropriate();
                SpiritRebirthHealthForm.Reconcile(original);
                if (!Prepared(original)) return SpiritReleaseResult.RecoveryRequired;
                // Native GenSpawn performs the removal from the exact current owner.
                // Dormant remains visible to every callback until final commit.
                if (!resumeSpawned)
                {
                    if (!CreatorCarriesOriginal() || !DestinationValid(caster, destination, cell)
                        || (releaseCommitGuard != null && !releaseCommitGuard()))
                        return SpiritReleaseResult.RecoveryRequired;
                    GenSpawn.Spawn(original, cell, destination, original.Rotation, WipeMode.Vanish);
                    releaseSpawnCompleted = SpiritRebirthFormPolicy.HasMapCustody(original) && original.Map == destination;
                }
                if (!SpiritRebirthFormPolicy.HasMapCustody(original) || original.Map != destination)
                    return SpiritReleaseResult.RecoveryRequired;
                original.needs.AddOrRemoveNeedsAsAppropriate();
                SpiritRebirthHealthForm.Reconcile(original);
                if (!Prepared(original) || !state.TryCommitRitualRelease(original, this)) return SpiritReleaseResult.RecoveryRequired;
                released = true;
                releasePreparing = false;
                return SpiritReleaseResult.Released;
            }
            catch (Exception error)
            {
                Compatibility.TM_Log.Warning("SpiritRelease", "Release interrupted; attempting native custody recovery: " + error);
                return SpiritReleaseResult.RecoveryRequired;
            }
            finally
            {
                try { if (!released) RestoreReleaseCustody(source); }
                finally { releaseCommitGuard = null; inFlight.Remove(original); }
            }
        }

        private static bool Prepared(Pawn pawn)
            => IsPreparing(pawn) && pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_SpiritND) is Need_Spirit
                && pawn.needs.TryGetNeed(TorannMagicDefOf.TM_Mana) is Need_Mana
                && pawn.health.hediffSet.hediffs.Count(SpiritRebirthHealthForm.Owns) == 1;

        private void RestoreReleaseCustody(ThingOwner source)
        {
            try
            {
                if (original.Spawned) original.DeSpawn(DestroyMode.WillReplace);
                releaseSpawnCompleted = false;
                if (original.holdingOwner == null && source != null && source.Count == 0)
                    source.TryAdd(original, false);
                if (!original.Spawned && original.holdingOwner == null) TryInventoryFallback();
                if (!HasReleaseCustody()) throw new InvalidOperationException("Native release recovery did not acquire original custody.");
            }
            catch (Exception error)
            {
                // A partial native despawn may retain the map owner. Never create
                // a second holder or destroy the original to conceal that failure.
                Compatibility.TM_Log.Warning("SpiritRelease", "Release custody needs recovery: " + error);
                if (!original.Spawned && original.holdingOwner == null) TryInventoryFallback();
                if (!HasReleaseCustody()) throw new InvalidOperationException("Original has no native owner after release recovery.", error);
            }
        }

        private bool HasReleaseCustody()
            => SpiritRebirthFormPolicy.HasMapCustody(original)
                || (original.holdingOwner?.Owner != null && original.holdingOwner.Contains(original));

        // A SpawnSetup callback can fail before the native spawn completes and a
        // DeSpawn callback can also fail. Such a partial map occupant must first
        // return to exact carry custody; it is never treated as a finished spawn.
        internal bool TryRecoverRelease(Pawn caster)
        {
            if (!PendingOriginal(original) || !ReferenceEquals(caster, creator) || released
                || !SpiritRebirthState.TryGet(original, out var state) || state.Form != SpiritRebirthForm.Dormant
                || caster == null || caster.Dead || !caster.Spawned || caster.Map != map
                || !(KnownInventoryRecovery() || ((PaidOriginal(original) || releasePreparing || releaseInventoryRecovery)
                    && SpiritRebirthFormPolicy.HasMapCustody(original) && original.Map == map))
                || creator?.carryTracker == null || creator.carryTracker.GetDirectlyHeldThings().Count != 0
                || !RecoveryInventoryValid() || !inFlight.Add(original)) return false;
            try
            {
                if (KnownInventoryRecovery()) creator.inventory.innerContainer.Remove(original);
                RestoreReleaseCustody(creator.carryTracker.GetDirectlyHeldThings());
                bool restored = CreatorCarriesOriginal();
                if (restored) releaseInventoryRecovery = false;
                return restored;
            }
            finally
            {
                try { if (!HasReleaseCustody()) RestoreReleaseCustody(creator.carryTracker.GetDirectlyHeldThings()); }
                finally { inFlight.Remove(original); }
            }
        }

        // Future ordered-driver Cleanup calls this before native cleanup can drop
        // its carried Pawn. No public job or activation is connected here.
        internal bool TryRetainReleaseForCleanup(Pawn caster)
        {
            if (!PendingOriginal(original) || !ReferenceEquals(caster, creator) || released
                || !SpiritRebirthState.TryGet(original, out var state) || state.Form != SpiritRebirthForm.Dormant
                || !CreatorCarriesOriginal() || !RecoveryInventoryValid() || !inFlight.Add(original)) return false;
            var source = original.holdingOwner;
            try
            {
                source.Remove(original);
                return TryInventoryFallback();
            }
            finally
            {
                try { if (!HasReleaseCustody()) RestoreReleaseCustody(source); }
                finally { inFlight.Remove(original); }
            }
        }

        private void ExposeReleaseData()
        {
            Scribe_Values.Look(ref releasePreparing, "releasePreparing", false);
            Scribe_Values.Look(ref releaseSpawnCompleted, "releaseSpawnCompleted", false);
            Scribe_Values.Look(ref releaseInventoryRecovery, "releaseInventoryRecovery", false);
            Scribe_Values.Look(ref released, "released", false);
        }
    }
}
