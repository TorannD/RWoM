using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    // Saved references describe trips; only native map/carry/inventory owners
    // own their actual resources and corpse. This record never deep-copies them.
    internal sealed class SpiritRebirthStaging : IExposable
    {
        public sealed class Trip : IExposable
        {
            internal Thing source, carried, staged;
            internal ThingDef resource;
            internal int count;
            internal IntVec3 sourceCell, cell;
            internal bool picked, dropped;
            public void ExposeData()
            {
                Scribe_References.Look(ref source, "source"); Scribe_References.Look(ref carried, "carried"); Scribe_References.Look(ref staged, "staged");
                Scribe_Defs.Look(ref resource, "resource"); Scribe_Values.Look(ref count, "count");
                Scribe_Values.Look(ref sourceCell, "sourceCell"); Scribe_Values.Look(ref cell, "cell");
                Scribe_Values.Look(ref picked, "picked"); Scribe_Values.Look(ref dropped, "dropped");
            }
        }
        private Pawn creator, original;
        private Thing circle;
        private Corpse corpse;
        private Job ownerJob;
        private List<Trip> trips = new List<Trip>();
        internal int TripCount => trips.Count;
        internal Corpse Corpse => corpse;
        internal Pawn Original => original;
        internal Thing Circle => circle;
        internal Thing SourceAt(int index) => trips[index].source;
        internal IntVec3 CellAt(int index) => trips[index].cell;
        internal IntVec3 ReleaseCell => trips[trips.Count - 1].cell;
        internal bool ReleaseContextValid(Pawn pawn, Job job)
            => ReferenceEquals(pawn, creator) && ReferenceEquals(job, ownerJob) && pawn.CurJob == job
                && job.targetA.Thing == circle && pawn.Position == circle.InteractionCell
                && Reserved(circle, 1) && Reserved(circle.InteractionCell, -1) && Reserved(ReleaseCell, -1);
        internal bool WorkValid(Pawn pawn, Job job)
            => ValidActor(pawn, job) && pawn.Position == circle.InteractionCell && pawn.carryTracker.CarriedThing == null
                && Reserved(circle, 1) && Reserved(circle.InteractionCell, -1)
                && trips.Count > 0 && trips.All(t => t.dropped && StagedValid(t) && !t.staged.IsForbidden(pawn)
                    && Reserved(t.staged, t.count) && Reserved(t.cell, -1))
                && trips.Where(t => t.resource == TorannMagicDefOf.RawMagicyte).Sum(t => t.count) == SpiritRebirthRitualEligibility.MagicyteCost
                && trips.Where(t => t.resource == ThingDefOf.Silver).Sum(t => t.count) == SpiritRebirthRitualEligibility.SilverCost;

        internal static SpiritRebirthStaging Discover(Pawn pawn, Job job, Thing circle, Corpse corpse)
        {
            if (!InitialValid(pawn, job, circle, corpse)) return null;
            return Plan(pawn, job, circle, corpse,
                HaulAIUtility.FindFixedIngredientCount(pawn, TorannMagicDefOf.RawMagicyte, SpiritRebirthRitualEligibility.MagicyteCost),
                HaulAIUtility.FindFixedIngredientCount(pawn, ThingDefOf.Silver, SpiritRebirthRitualEligibility.SilverCost));
        }

        private static bool InitialValid(Pawn pawn, Job job, Thing circle, Corpse corpse)
            => pawn != null && job != null && job.targetA.Thing == circle && pawn.carryTracker != null
                && pawn.carryTracker.CarriedThing == null && pawn.inventory?.innerContainer != null
                && ReferenceEquals(pawn.inventory.innerContainer.Owner, pawn.inventory)
                && SpiritNecromancyRenunciation.Allowed(job, corpse?.InnerPawn)
                && SpiritRebirthRitualEligibility.Rejection(pawn, circle, corpse, corpse?.InnerPawn) == SpiritRebirthRitualRejection.None;

        internal static SpiritRebirthStaging Plan(Pawn pawn, Job job, Thing circle, Corpse corpse, List<Thing> magicyte, List<Thing> silver)
        {
            if (!InitialValid(pawn, job, circle, corpse) || !MapOwns(corpse, pawn.Map)
                || !pawn.CanReach(circle.InteractionCell, PathEndMode.OnCell, Danger.Deadly)
                || !pawn.CanReach(corpse, PathEndMode.Touch, Danger.Deadly)) return null;
            var plan = new SpiritRebirthStaging { creator = pawn, original = corpse.InnerPawn, circle = circle, corpse = corpse, ownerJob = job };
            if (!plan.AddSources(magicyte, TorannMagicDefOf.RawMagicyte, SpiritRebirthRitualEligibility.MagicyteCost)
                || !plan.AddSources(silver, ThingDefOf.Silver, SpiritRebirthRitualEligibility.SilverCost)
                || pawn.carryTracker.AvailableStackSpace(corpse.def) < 1) return null;
            plan.trips.Add(new Trip { source = corpse, resource = corpse.def, count = 1, sourceCell = corpse.Position });
            var cells = GenRadial.RadialCellsAround(circle.Position, 3.9f, true)
                .Where(c => c.InHorDistOf(circle.Position, 3.9f) && !circle.OccupiedRect().Contains(c) && c != circle.InteractionCell
                    && plan.FreeCell(c, null) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly)
                    && pawn.Map.reservationManager.CanReserve(pawn, c, 1, -1, null, false))
                .Take(plan.trips.Count).ToList();
            if (cells.Count != plan.trips.Count) return null;
            for (int i = 0; i < cells.Count; i++) plan.trips[i].cell = cells[i];
            return plan;
        }

        private bool AddSources(List<Thing> sources, ThingDef def, int remaining)
        {
            if (sources == null || def == null) return false;
            int capacity = creator.carryTracker.AvailableStackSpace(def);
            if (capacity <= 0) return false;
            var seen = new HashSet<Thing>();
            foreach (var source in sources)
            {
                if (remaining == 0) break;
                if (source == null || !seen.Add(source) || source.def != def || !MapOwns(source, creator.Map)
                    || source.IsForbidden(creator) || source.stackCount <= 0) return false;
                int selected = Math.Min(remaining, source.stackCount);
                remaining -= selected;
                while (selected > 0)
                {
                    int count = Math.Min(capacity, selected);
                    trips.Add(new Trip { source = source, resource = def, count = count, sourceCell = source.Position });
                    selected -= count;
                }
            }
            return remaining == 0;
        }

        private bool ValidIdentity(Pawn pawn, Job job)
            => ReferenceEquals(pawn, creator) && ReferenceEquals(job, ownerJob)
                && job?.targetA.Thing == circle && SpiritNecromancyRenunciation.Allowed(job, original)
                && SpiritRebirthRitualEligibility.Rejection(pawn, circle, corpse, original) == SpiritRebirthRitualRejection.None;
        private bool ValidActor(Pawn pawn, Job job) => pawn != null && pawn.CurJob == job && ValidIdentity(pawn, job);
        internal static bool MapOwns(Thing thing, Map map)
            => thing != null && !thing.Destroyed && thing.Spawned && thing.Map == map
                && ReferenceEquals(thing.holdingOwner, map?.spawnedThings) && ReferenceEquals(map.spawnedThings.Owner, map) && map.spawnedThings.Contains(thing);
        private bool FreeCell(IntVec3 cell, Thing thing)
            => cell.InBounds(creator.Map) && cell.Standable(creator.Map) && cell.GetThingList(creator.Map).Count == 0
                && (thing == null || GenPlace.HaulPlaceBlockerIn(thing, cell, creator.Map, true) == null);
        private bool Reserved(LocalTargetInfo target, int count)
            => creator.Map.reservationManager.ReservationsReadOnly.Any(r => r.Claimant == creator && r.Job == ownerJob
                && r.Target == target && r.Layer == null && (r.StackCount == -1 || r.StackCount >= count));

        internal bool Reserve(Pawn pawn, Job job)
        {
            if (!ValidIdentity(pawn, job) || trips == null || trips.Count == 0) return false;
            var claims = new List<KeyValuePair<LocalTargetInfo, int>> {
                new KeyValuePair<LocalTargetInfo, int>(circle, 1), new KeyValuePair<LocalTargetInfo, int>(circle.InteractionCell, -1),
                new KeyValuePair<LocalTargetInfo, int>(corpse, 1)
            };
            foreach (var group in trips.Where(t => !t.picked && t.source != corpse).GroupBy(t => t.source))
                claims.Add(new KeyValuePair<LocalTargetInfo, int>(group.Key, group.Sum(t => t.count)));
            foreach (var trip in trips)
            {
                claims.Add(new KeyValuePair<LocalTargetInfo, int>(trip.cell, -1));
                if (trip.dropped && trip.staged != corpse) claims.Add(new KeyValuePair<LocalTargetInfo, int>(trip.staged, trip.count));
            }
            var manager = pawn.Map.reservationManager;
            // Explicit ordinary preflight prevents playerForced Reserve from
            // stealing a competing claim through the native override path.
            foreach (var claim in claims)
                if (!manager.CanReserve(pawn, claim.Key, 1, claim.Value, null, false)) return false;
            bool complete = false;
            try
            {
                foreach (var claim in claims)
                    if (!manager.Reserve(pawn, job, claim.Key, 1, claim.Value, null, false, false)) return false;
                complete = true;
                return true;
            }
            finally { if (!complete) manager.ReleaseClaimedBy(pawn, job); }
        }

        internal bool Pickup(Pawn pawn, Job job, int index)
        {
            if (!ValidActor(pawn, job) || index < 0 || index >= trips.Count) return false;
            var trip = trips[index];
            if (trip.dropped) return StagedValid(trip);
            if (trip.picked) return CarryOwns(trip.carried, trip);
            if (pawn.carryTracker.CarriedThing != null || !MapOwns(trip.source, pawn.Map) || trip.source.Position != trip.sourceCell
                || trip.source.def != trip.resource || trip.source.stackCount < trip.count || trip.source.IsForbidden(pawn)
                || pawn.carryTracker.AvailableStackSpace(trip.resource) < trip.count || !Reserved(trip.source, trip.count)
                || !Reserved(trip.cell, -1) || !FreeCell(trip.cell, trip.source)) return false;
            int before = trip.source.stackCount;
            using (var capture = new SpiritRebirthSplitCapture(trip.source, trip.count))
            {
                Thing known = capture.Result;
                try
                {
                    known = trip.source.SplitOff(trip.count);
                    trip.carried = known;
                    if (known == null || known.stackCount != trip.count || !pawn.carryTracker.GetDirectlyHeldThings().TryAdd(known, false)
                        || !CarryOwns(known, trip)) return false;
                    trip.picked = true;
                    if (!trips.Any(t => !t.picked && ReferenceEquals(t.source, trip.source)))
                        pawn.Map.reservationManager.Release(trip.source, pawn, job);
                    return true;
                }
                catch (Exception error)
                {
                    known = known ?? capture.Result;
                    trip.carried = known;
                    Compatibility.TM_Log.Warning("SpiritRitualHaul", "Pickup interrupted; recovering known native object: " + error);
                    // An arbitrary failure inside base SplitOff before it returns
                    // a partial allocation cannot provide an observable result.
                    if (known == null) throw;
                    return false;
                }
                finally { if (!trip.picked && known != null) RecoverKnown(known, trip, before); }
            }
        }

        private bool CarryOwns(Thing thing, Trip trip)
            => thing != null && !thing.Destroyed && !thing.Spawned && thing.stackCount == trip.count && thing.def == trip.resource
                && ReferenceEquals(creator.carryTracker.CarriedThing, thing)
                && ReferenceEquals(thing.holdingOwner, creator.carryTracker.GetDirectlyHeldThings())
                && creator.carryTracker.GetDirectlyHeldThings().Contains(thing);
        private bool StagedValid(Trip trip)
            => MapOwns(trip.staged, creator.Map) && trip.staged.Position == trip.cell
                && trip.staged.def == trip.resource && trip.staged.stackCount == trip.count;

        internal bool Drop(Pawn pawn, Job job, int index)
        {
            if (!ValidActor(pawn, job) || index < 0 || index >= trips.Count) return false;
            var trip = trips[index];
            if (trip.dropped) return StagedValid(trip);
            var known = trip.carried;
            if (!trip.picked || !CarryOwns(known, trip) || !Reserved(trip.cell, -1) || !FreeCell(trip.cell, known)) return false;
            try
            {
                if (!pawn.carryTracker.TryDropCarriedThing(trip.cell, ThingPlaceMode.Direct, out var placed)
                    || !ReferenceEquals(placed, known) || !MapOwns(known, pawn.Map) || known.Position != trip.cell
                    || known.stackCount != trip.count || known.def != trip.resource) return false;
                if (!pawn.Map.reservationManager.CanReserve(pawn, known, 1, trip.count, null, false)
                    || !pawn.Map.reservationManager.Reserve(pawn, job, known, 1, trip.count, null, false, false)) return false;
                trip.staged = known; trip.dropped = true;
                HaulAIUtility.UpdateJobWithPlacedThings(job, known, trip.count);
                return true;
            }
            catch (Exception error)
            {
                Compatibility.TM_Log.Warning("SpiritRitualHaul", "Drop interrupted; verifying native custody: " + error);
                return false;
            }
            finally { if (!trip.dropped) RecoverKnown(known, trip, -1); }
        }

        private void RecoverKnown(Thing known, Trip trip, int sourceBefore)
        {
            if (Recovered(known)) return;
            if (sourceBefore > trip.count && MapOwns(trip.source, creator.Map) && trip.source.Position == trip.sourceCell
                && trip.source.stackCount == sourceBefore - trip.count && trip.source.def == known.def)
            {
                try { trip.source.TryAbsorbStack(known, false); }
                catch (Exception error) { Compatibility.TM_Log.Warning("SpiritRitualHaul", "Source recovery interrupted; checking native custody: " + error); }
                if (Recovered(known)) return;
            }
            if (creator.inventory?.innerContainer != null && ReferenceEquals(creator.inventory.innerContainer.Owner, creator.inventory))
            {
                try { creator.inventory.innerContainer.TryAdd(known, false); }
                catch (Exception error) { Compatibility.TM_Log.Warning("SpiritRitualHaul", "Inventory recovery interrupted; checking native custody: " + error); }
                if (Recovered(known)) return;
            }
            throw new InvalidOperationException("Known ritual haul object has no verified native owner after recovery.");
        }
        private bool Recovered(Thing known)
            => known.Destroyed || MapOwns(known, creator.Map)
                || (known.holdingOwner?.Owner != null && known.holdingOwner.Contains(known));

        internal List<ThingCountClass> StagedMaterials()
            => trips.Where(t => t.source != corpse && t.dropped && StagedValid(t)).Select(t => new ThingCountClass(t.staged, t.count)).ToList();

        public void ExposeData()
        {
            Scribe_References.Look(ref creator, "creator"); Scribe_References.Look(ref original, "original", true);
            Scribe_References.Look(ref circle, "circle"); Scribe_References.Look(ref corpse, "corpse"); Scribe_References.Look(ref ownerJob, "job");
            Scribe_Collections.Look(ref trips, "trips", LookMode.Deep);
        }
    }
}
