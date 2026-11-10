using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using TorannMagic.Compatibility;

namespace TorannMagic.AutoCast
{
    // Only peaceful automatic Flight uses this carrier. Unlike the combat
    // projectiles, it owns the pawn and never suspends/replaces its job driver.
    public class TM_WorkFlight : Thing, IThingHolder, ISuspendableThingHolder
    {
        public const float TicksPerCell = 5f; // same speed as FlyingObject_Flight
        private ThingOwner<Thing> contents;
        private IntVec3 origin, landing;
        private LocalTargetInfo goal;
        private PathEndMode mode;
        private Job work;
        private int elapsed, duration;
        private bool fireAtWill = true;
        public bool IsContentsSuspended => true;
        public Pawn Passenger => contents.Count == 0 ? null : contents[0] as Pawn;
        [ThreadStatic] private static Pawn preserving;
        public TM_WorkFlight() { contents = new ThingOwner<Thing>(this); }

        public bool Begin(Pawn pawn, TM_TravelAutocast.Plan plan)
        {
            if (!Spawned || contents.Count != 0 || pawn?.Map != Map || !TM_TravelAutocast.StillValid(pawn, plan)) return false;
            origin = plan.Origin; landing = plan.Landing; goal = plan.Destination; mode = plan.Mode; work = plan.Job;
            fireAtWill = pawn.drafter?.FireAtWill ?? true;
            duration = Math.Max(1, (int)Math.Ceiling((landing - origin).LengthHorizontal * TicksPerCell));
            var previous = preserving;
            try
            {
                preserving = pawn;
                pawn.DeSpawn(DestroyMode.WillReplace);
            }
            catch (Exception e)
            {
                // A mod notification can throw after the native despawn. Keep
                // the passenger reachable and recover at the launch site.
                landing = origin; elapsed = duration;
                if (!pawn.Spawned && pawn.holdingOwner == null) contents.TryAdd(pawn, false);
                TM_Log.Error("Flight", "Automatic flight takeoff failed; recovering passenger: " + e);
                return false;
            }
            finally { preserving = previous; }
            // Empty, non-merging ThingOwner always accepts the single pawn.
            if (!contents.TryAdd(pawn, false))
            {
                GenSpawn.Spawn(pawn, origin, Map);
                pawn.pather.StartPath(goal, mode);
                return false;
            }
            return true;
        }

        public static bool PreserveJobs(Pawn_JobTracker __instance) => preserving == null || __instance != preserving.jobs;
        public static bool PreserveReservations(Pawn __instance) => __instance != preserving;

        public ThingOwner GetDirectlyHeldThings() => contents;
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref contents, "contents", this);
            Scribe_Values.Look(ref origin, "origin");
            Scribe_Values.Look(ref landing, "landing");
            Scribe_TargetInfo.Look(ref goal, "goal");
            Scribe_Values.Look(ref mode, "mode");
            Scribe_References.Look(ref work, "work");
            Scribe_Values.Look(ref elapsed, "elapsed");
            Scribe_Values.Look(ref duration, "duration");
            Scribe_Values.Look(ref fireAtWill, "fireAtWill", true);
        }

        private bool SafeCell(IntVec3 cell) => cell.InBounds(Map) && cell.Standable(Map)
            && !cell.Fogged(Map) && cell.GetFirstPawn(Map) == null && !cell.ContainsStaticFire(Map)
            && Odyssey.TravelEnvironmentSafety.IsSafe(Passenger, Map, cell);

        public bool TryLand()
        {
            var pawn = Passenger;
            if (pawn == null || !Spawned) return false;
            // Destination may have changed during flight. Try nearby, then the
            // launch site. If none is safe, keep ownership and retry next tick.
            IntVec3 cell = IntVec3.Invalid;
            foreach (var center in new[] { landing, origin })
            {
                foreach (var candidate in GenRadial.RadialCellsAround(center, 3.9f, true))
                    if (SafeCell(candidate)) { cell = candidate; break; }
                if (cell.IsValid) break;
            }
            if (!cell.IsValid) return false;
            if (!contents.TryDrop(pawn, cell, Map, ThingPlaceMode.Direct, out var dropped, playDropSound: false)) return false;
            if (pawn.drafter != null) pawn.drafter.FireAtWill = fireAtWill;
            // The original driver/toil, queue, reservations and carry owner are
            // still the pawn's. Never overwrite new work or a cancelled target.
            if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState && ReferenceEquals(pawn.CurJob, work)
                && goal.IsValid && (!goal.HasThing || (goal.Thing.Spawned && goal.Thing.Map == Map)))
                pawn.pather.StartPath(goal, mode);
            return true;
        }

        protected override void TickInterval(int delta)
        {
            elapsed += delta;
            if (elapsed >= duration && TryLand()) Destroy();
        }
        public override int UpdateRateTicks => 1;
        public override Vector3 DrawPos
        {
            get
            {
                float t = Mathf.Clamp01((float)elapsed / Math.Max(1, duration));
                return Vector3.Lerp(origin.ToVector3Shifted(), landing.ToVector3Shifted(), t)
                    + Vector3.forward * (Mathf.Sin(t * Mathf.PI) * 1.5f);
            }
        }
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            Passenger?.DynamicDrawPhaseAt(phase, DrawPos);
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // Never destroy a carrier while it still owns a pawn, including on
            // a failed landing or repeated cleanup. It can safely retry later.
            if (contents.Count != 0 && !TryLand()) return;
            base.Destroy(mode);
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StopAll))]
    internal static class WorkFlightKeepJobs
    {
        static bool Prefix(Pawn_JobTracker __instance) => TM_WorkFlight.PreserveJobs(__instance);
    }
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ClearAllReservations))]
    internal static class WorkFlightKeepReservations
    {
        static bool Prefix(Pawn __instance) => TM_WorkFlight.PreserveReservations(__instance);
    }
}
