using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal enum SpiritRitualResult { Rejected, RefundPending, RecoveryRequired, ConvertedHeld }

    // Saved accounting on the original. Resources belong to its native inventory;
    // this record is never a second holder of either resources or the Pawn.
    internal sealed partial class SpiritRebirthRitualTransaction : IExposable
    {
        public sealed class Debit : IExposable
        {
            internal Thing source, escrow;
            internal ThingDef resource;
            internal int count;
            internal IntVec3 sourceCell;
            internal bool consumed, refunded;
            public void ExposeData()
            {
                Scribe_References.Look(ref source, "source"); Scribe_References.Look(ref escrow, "escrow");
                Scribe_Defs.Look(ref resource, "resource");
                Scribe_Values.Look(ref count, "count", 0); Scribe_Values.Look(ref sourceCell, "sourceCell");
                Scribe_Values.Look(ref consumed, "consumed", false); Scribe_Values.Look(ref refunded, "refunded", false);
            }
        }

        private int version = 1;
        private Pawn original, creator;
        private Thing circle;
        private Corpse corpse;
        private Job expectedJob;
        private Map map;
        private List<Debit> debits = new List<Debit>();
        private bool attempted, converted, paid, completed, refundPending, recoveryRequired;
        private bool renounceNecromancy;
        private static readonly HashSet<Pawn> inFlight = new HashSet<Pawn>();
        internal bool Attempted => attempted;
        internal bool Paid => paid;
        internal bool Converted => converted;
        internal bool Completed => completed; // transaction only; result is held and inactive
        internal Pawn Original => original;
        internal bool FullyRefunded => !attempted && !converted && !paid && refundPending
            && original != null && original.Dead && corpse?.InnerPawn == original
            && debits != null && debits.All(d => d.escrow == null || d.refunded);

        public SpiritRebirthRitualTransaction() { }

        // Work duration is supplied by the future saved ordered driver. This service
        // is not proof that an actual four-hour job has run and has no UI entry point.
        internal static SpiritRitualResult Execute(Pawn caster, Thing selectedCircle, Corpse selectedCorpse,
            Pawn selectedOriginal, Job job, List<ThingCountClass> staged, int workTicks)
        {
            var comp = ClassIdentity.Component(selectedOriginal);
            if (comp == null || !inFlight.Add(selectedOriginal)) return SpiritRitualResult.Rejected;
            try
            {
                var existing = comp.SpiritRitual;
                if (existing != null)
                {
                    if (!ReferenceEquals(existing.original, selectedOriginal) || !ReferenceEquals(existing.creator, caster)
                        || !ReferenceEquals(existing.circle, selectedCircle)
                        || (!existing.attempted && (!ReferenceEquals(existing.corpse, selectedCorpse)
                            || !ReferenceEquals(existing.expectedJob, job)))) return SpiritRitualResult.Rejected;
                    return existing.Resume();
                }
                if (workTicks < SpiritRebirthRitualEligibility.RitualTicks || job == null || caster?.CurJob != job
                    || job.targetA.Thing != selectedCircle || caster.Position != selectedCircle?.InteractionCell
                    || SpiritRebirthRitualEligibility.Rejection(caster, selectedCircle, selectedCorpse, selectedOriginal) != SpiritRebirthRitualRejection.None
                    || !SpiritNecromancyRenunciation.Allowed(job, selectedOriginal)
                    || !InventoryValid(selectedOriginal) || !InventoryValid(caster) || caster.carryTracker == null
                    || (caster.carryTracker.CarriedThing != null && caster.carryTracker.CarriedThing != selectedCorpse)
                    || !Reserved(caster, job, selectedCircle, 1) || !Reserved(caster, job, selectedCorpse, 1)
                    || !ValidStaging(caster, selectedCircle, job, staged)
                    || !SpiritRebirthHealthPlan.TryPrepare(selectedOriginal, out var healthPlan, out _)) return SpiritRitualResult.Rejected;

                var receipt = new SpiritRebirthRitualTransaction {
                    original = selectedOriginal, creator = caster, circle = selectedCircle,
                    renounceNecromancy = SpiritNecromancyRenunciation.Required(selectedOriginal),
                    corpse = selectedCorpse, expectedJob = job, map = caster.Map,
                    debits = staged.Select(s => new Debit { source = s.thing, resource = s.thing.def, count = s.Count, sourceCell = s.thing.Position }).ToList()
                };
                if (!comp.TryBeginSpiritRitual(receipt)) return SpiritRitualResult.Rejected;
                receipt.Perform(healthPlan);
                return receipt.Resume();
            }
            finally { inFlight.Remove(selectedOriginal); }
        }

        private static bool InventoryValid(Pawn pawn)
            => pawn?.inventory?.innerContainer != null && ReferenceEquals(pawn.inventory.innerContainer.Owner, pawn.inventory);

        private static bool Reserved(Pawn caster, Job job, Thing thing, int count)
            => thing != null && caster?.Map?.reservationManager?.ReservationsReadOnly.Any(r =>
                r.Claimant == caster && r.Job == job && r.Layer == null && r.Target.Thing == thing
                && (r.StackCount == -1 || r.StackCount >= count)) == true;

        private static bool ValidStaging(Pawn caster, Thing circle, Job job, List<ThingCountClass> staged)
        {
            if (staged == null || staged.Count == 0 || staged.Any(s => s == null || s.thing == null || s.Count <= 0)
                || staged.Select(s => s.thing).Distinct().Count() != staged.Count
                || staged.Select(s => s.thing.Position).Distinct().Count() != staged.Count) return false;
            long magicyte = 0, silver = 0;
            foreach (var entry in staged)
            {
                var thing = entry.thing;
                if (thing.Destroyed || !thing.Spawned || thing.Map != caster.Map
                    || thing.holdingOwner != caster.Map.spawnedThings || thing.holdingOwner?.Owner != caster.Map
                    || !caster.Map.spawnedThings.Contains(thing)
                    || thing.stackCount != entry.Count || !thing.Position.InHorDistOf(circle.Position, 3.9f)
                    || thing.IsForbidden(caster) || !Reserved(caster, job, thing, entry.Count)) return false;
                if (thing.def == TorannMagicDefOf.RawMagicyte) magicyte += entry.Count;
                else if (thing.def == ThingDefOf.Silver) silver += entry.Count;
                else return false;
            }
            return magicyte == SpiritRebirthRitualEligibility.MagicyteCost && silver == SpiritRebirthRitualEligibility.SilverCost;
        }

        private void Perform(SpiritRebirthHealthPlan healthPlan)
        {
            SpiritRebirthWorkSettings work = null;
            Exception failure = null;
            bool custodySecured = false;
            try
            {
                work = SpiritRebirthWorkSettings.Capture(original);
                foreach (var debit in debits)
                {
                    // The ordered hauling job must stage exact whole stacks. Keep
                    // the known actual Thing before any native despawn callbacks;
                    // payment never allocates/splits a resource or a Pawn.
                    debit.escrow = debit.source;
                    debit.escrow.DeSpawn(DestroyMode.WillReplace);
                    if (debit.escrow == null || debit.escrow.stackCount != debit.count
                        || !original.inventory.innerContainer.TryAdd(debit.escrow, false))
                    { refundPending = true; return; }
                }
                if (!EscrowIntact() || creator.CurJob != expectedJob
                    || (SpiritNecromancyRenunciation.Required(original) && (!renounceNecromancy || !SpiritNecromancyRenunciation.Allowed(expectedJob, original)))
                    || SpiritRebirthRitualEligibility.Rejection(creator, circle, corpse, original) != SpiritRebirthRitualRejection.None
                    || !Reserved(creator, expectedJob, circle, 1) || !Reserved(creator, expectedJob, corpse, 1))
                { refundPending = true; return; }
                attempted = true; // Native removal callbacks are already irreversible.
                if (!healthPlan.Apply()) { recoveryRequired = true; return; }
                bool revived = ResurrectionUtility.TryResurrect(original, new ResurrectionParams {
                    dontSpawn = true, noLord = true, removeDiedThoughts = false,
                    gettingScarsChance = 0f, invisibleStun = false
                });
                if (!revived || original.Dead || !EscrowIntact()) { recoveryRequired = true; return; }
                if (work != null && !work.Restore()) { recoveryRequired = true; return; }
                converted = true;
            }
            catch (Exception error)
            {
                if (attempted) recoveryRequired = true;
                else refundPending = true;
                failure = error;
            }
            finally
            {
                // A result reference does not own a Pawn. Secure the actual original
                // even when resurrection throws after detaching/destroying its corpse.
                try { if (attempted && !(custodySecured = EnsureOwnership())) recoveryRequired = true; }
                catch (Exception error) { recoveryRequired = true; failure = failure ?? error; }
                finally
                {
                    if (attempted && original != null && !original.Dead)
                    {
                        if (ClassIdentity.Component(original)?.EnsureSpiritRitualPending(this) != true) recoveryRequired = true;
                        try { if (work != null) work.Restore(); }
                        catch (Exception error) { recoveryRequired = true; failure = failure ?? error; }
                    }
                    if (attempted)
                    {
                        expectedJob = null; // no dangling reference after the ordered job is discarded
                        if (corpse?.Destroyed == true) corpse = null;
                        foreach (var debit in debits) debit.source = null;
                    }
                }
                if (failure != null) Compatibility.TM_Log.Warning("SpiritRitual", "Conversion/payment interrupted; custody verified=" + custodySecured + ": " + failure);
                if (attempted && !custodySecured) throw new InvalidOperationException("Original has no verified native custody after ritual conversion.", failure);
            }
        }

        private bool EscrowIntact() => InventoryValid(original) && debits != null && debits.Count > 0
            && debits.All(d => d != null && !d.consumed && !d.refunded && d.count > 0 && d.escrow != null
                && !d.escrow.Destroyed && !d.escrow.Spawned && d.escrow.stackCount == d.count && d.escrow.def == d.resource
                && original.inventory.innerContainer.Contains(d.escrow)
                && ReferenceEquals(d.escrow.holdingOwner, original.inventory.innerContainer));

        private bool EnsureOwnership()
        {
            if (original == null || original.Destroyed || original.Discarded) return false;
            if (original.Spawned) return SpiritRebirthFormPolicy.HasMapCustody(original);
            if (original.holdingOwner != null && original.holdingOwner.Contains(original)
                && original.holdingOwner.Owner != null) return true;
            var worlds = Find.WorldPawns;
            if (worlds?.Contains(original) == true) return true;
            var carry = creator?.carryTracker?.GetDirectlyHeldThings();
            if (creator != null && !creator.Destroyed && carry != null && carry.Count == 0
                && ReferenceEquals(carry.Owner, creator.carryTracker) && carry.TryAdd(original, false))
                return ReferenceEquals(original.holdingOwner, carry) && carry.Contains(original);
            return TryInventoryFallback();
        }

        private SpiritRitualResult Resume()
        {
            if (version != 1 || original == null || ClassIdentity.Component(original)?.SpiritRitual != this || !ValidManifest())
                return SpiritRitualResult.RecoveryRequired;
            if (completed) return paid && SpiritRebirthState.TryGet(original, out _)
                ? SpiritRitualResult.ConvertedHeld : SpiritRitualResult.RecoveryRequired;
            if (refundPending) return Refund();
            if (recoveryRequired || !converted) return SpiritRitualResult.RecoveryRequired;
            if (!EnsureOwnership() || original.Dead || !InventoryValid(original)) return SpiritRitualResult.RecoveryRequired;
            try
            {
                foreach (var debit in debits)
                {
                    if (debit.consumed) continue;
                    if (debit.escrow == null || debit.escrow.Destroyed || debit.refunded || debit.count <= 0 || debit.escrow.def != debit.resource
                        || debit.escrow.stackCount != debit.count || !original.inventory.innerContainer.Contains(debit.escrow)
                        || debit.escrow.holdingOwner != original.inventory.innerContainer) return SpiritRitualResult.RecoveryRequired;
                    try { debit.escrow.Destroy(DestroyMode.Vanish); }
                    finally
                    {
                        if (debit.escrow.Destroyed) { debit.consumed = true; debit.escrow = null; }
                    }
                    if (!debit.consumed) return SpiritRitualResult.RecoveryRequired;
                }
                paid = debits.Count > 0 && debits.All(d => d.consumed);
                if (!paid) return SpiritRitualResult.RecoveryRequired;
                var comp = ClassIdentity.Component(original);
                // Payment commits conversion accounting, not gameplay release.
                // Dormant remains until the full form/appearance pipeline exists.
                if (!comp.EnsureSpiritRitualPending(this)) return SpiritRitualResult.RecoveryRequired;
                if (renounceNecromancy) SpiritNecromancyRenunciation.Apply(original);
                completed = true;
                return SpiritRitualResult.ConvertedHeld;
            }
            catch (Exception error)
            {
                Compatibility.TM_Log.Warning("SpiritRitual", "Converted original retained; remaining payment may resume: " + error.Message);
                return SpiritRitualResult.RecoveryRequired;
            }
        }

        private SpiritRitualResult Refund()
        {
            if (attempted || converted || paid || original == null) return SpiritRitualResult.RecoveryRequired;
            foreach (var debit in debits)
            {
                if (debit.escrow == null || debit.refunded) continue;
                var thing = debit.escrow;
                try
                {
                if (thing.def != debit.resource || thing.stackCount != debit.count) return SpiritRitualResult.RefundPending;
                if (thing.Spawned && thing.Map == map && thing.holdingOwner == map?.spawnedThings
                    && thing.holdingOwner?.Owner == map && map.spawnedThings.Contains(thing))
                { debit.refunded = true; continue; }
                if (!thing.Destroyed && !thing.Spawned && thing.holdingOwner == null)
                {
                    if (InventoryValid(original)) original.inventory.innerContainer.TryAdd(thing, false);
                    if (thing.holdingOwner == null && creator?.inventory?.innerContainer != null)
                        creator.inventory.innerContainer.TryAdd(thing, false);
                    if (thing.holdingOwner == null && map != null && GenPlace.TryPlaceThing(thing, debit.sourceCell, map, ThingPlaceMode.Near, out var _))
                    { debit.refunded = true; continue; }
                    if (thing.holdingOwner == null) throw new InvalidOperationException("Ritual refund could not acquire native resource custody.");
                }
                if (thing.Destroyed || thing.Spawned || (thing.holdingOwner != original.inventory?.innerContainer
                    && thing.holdingOwner != creator?.inventory?.innerContainer))
                    return SpiritRitualResult.RefundPending;
                if (map == null || !thing.holdingOwner.TryDrop(thing, debit.sourceCell, map, ThingPlaceMode.Near, out var _, playDropSound: false))
                    return SpiritRitualResult.RefundPending;
                debit.refunded = true;
                }
                catch (Exception error)
                {
                    // Native TryDrop removes the item before GenPlace runs. A
                    // receipt reference alone cannot save that detached item.
                    // Reacquire its actual custody before returning a retry state.
                    if (!RetainRefundResource(thing)) throw new InvalidOperationException("Ritual refund lost native resource custody.", error);
                    Compatibility.TM_Log.Warning("SpiritRitual", "Refund interrupted; actual resource retained for retry: " + error.Message);
                    return SpiritRitualResult.RefundPending;
                }
            }
            if (FullyRefunded) ClassIdentity.Component(original)?.ClearRefundedSpiritRitual(this);
            return SpiritRitualResult.Rejected;
        }

        private bool RetainRefundResource(Thing thing)
        {
            if (thing == null || thing.Destroyed) return false;
            if (thing.Spawned) return thing.Map == map && thing.holdingOwner == map?.spawnedThings
                && thing.holdingOwner?.Owner == map && map.spawnedThings.Contains(thing);
            if (thing.holdingOwner != null) return thing.holdingOwner.Owner != null && thing.holdingOwner.Contains(thing);
            foreach (var inventory in new[] { original?.inventory, creator?.inventory })
            {
                if (inventory?.innerContainer == null || inventory.innerContainer.Owner != inventory) continue;
                try { inventory.innerContainer.TryAdd(thing, false); }
                catch { /* Native callbacks may throw after acquiring the item. */ }
                if (thing.holdingOwner != null) return thing.holdingOwner.Owner != null && thing.holdingOwner.Contains(thing);
            }
            return false;
        }

        private bool ValidManifest()
        {
            if (debits == null || debits.Count == 0 || debits.Any(d => d == null || d.count <= 0
                || (d.resource != TorannMagicDefOf.RawMagicyte && d.resource != ThingDefOf.Silver))) return false;
            return debits.Where(d => d.resource == TorannMagicDefOf.RawMagicyte).Sum(d => (long)d.count) == SpiritRebirthRitualEligibility.MagicyteCost
                && debits.Where(d => d.resource == ThingDefOf.Silver).Sum(d => (long)d.count) == SpiritRebirthRitualEligibility.SilverCost;
        }

        public void ExposeData()
        {
            ExposeReleaseData();
            Scribe_Values.Look(ref version, "version", 0);
            Scribe_References.Look(ref original, "original", true); Scribe_References.Look(ref creator, "creator");
            Scribe_References.Look(ref circle, "circle"); Scribe_References.Look(ref corpse, "corpse");
            Scribe_References.Look(ref expectedJob, "job"); Scribe_References.Look(ref map, "map");
            Scribe_Collections.Look(ref debits, "debits", LookMode.Deep);
            Scribe_Values.Look(ref attempted, "attempted", false); Scribe_Values.Look(ref converted, "converted", false);
            Scribe_Values.Look(ref paid, "paid", false); Scribe_Values.Look(ref completed, "completed", false);
            Scribe_Values.Look(ref renounceNecromancy, "renounceNecromancy");
            Scribe_Values.Look(ref refundPending, "refundPending", false); Scribe_Values.Look(ref recoveryRequired, "recoveryRequired", false);
        }
    }
}
