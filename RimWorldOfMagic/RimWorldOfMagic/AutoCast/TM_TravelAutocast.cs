using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using TorannMagic.Compatibility;
using Verse;
using Verse.AI;

namespace TorannMagic.AutoCast
{
    // Short, same-map travel only. Never creates a cast job or takes ownership of
    // the work queue. Combat attacks and transport/pod abilities have other rules.
    public static class TM_TravelAutocast
    {
        private static readonly FieldInfo EndMode = AccessTools.Field(typeof(Pawn_PathFollower), "peMode");
        public sealed class Plan
        {
            public Job Job { get; internal set; }
            public LocalTargetInfo Destination { get; internal set; }
            public PathEndMode Mode { get; internal set; }
            public IntVec3 Origin { get; internal set; }
            public IntVec3 Landing { get; internal set; }
            public float Range { get; internal set; }
            public int HorizonTicks { get; internal set; }
        }

        public static bool Admitted(Pawn pawn)
        {
            return pawn != null && !TM_ExternalCastOwnership.IsCasting(pawn) && pawn.IsColonist && !pawn.IsPrisoner && !pawn.IsSlave
                && !pawn.Dead && !pawn.Downed && !pawn.InMentalState && !pawn.Drafted
                && pawn.CurJob != null && !pawn.CurJob.playerForced
                && pawn.GetPosture() == PawnPosture.Standing
                && pawn.CurJob.locomotionUrgency >= LocomotionUrgency.Jog
                && !TM_AutocastCombat.IsAbilityJob(pawn.CurJob);
        }

        public static bool LandingAllowed(Pawn pawn, IntVec3 landing, float range, int horizonTicks = 0)
        {
            return TM_Teleport.CanMove(pawn, landing) && !landing.Fogged(pawn.Map)
                && (landing - pawn.Position).LengthHorizontal < range
                && landing.GetFirstPawn(pawn.Map) == null
                && !landing.ContainsStaticFire(pawn.Map)
                && Odyssey.TravelEnvironmentSafety.IsSafe(pawn, pawn.Map, landing, horizonTicks);
        }

        // Native path cost retains the pawn's door/traversal restrictions and the
        // original job's OnCell/Touch/InteractionCell semantics. Bounded queries.
        public static float RemainingCost(Pawn pawn, IntVec3 start, LocalTargetInfo destination, PathEndMode mode)
        {
            var path = pawn.Map.pathFinder.FindPathNow(start, destination,
                TraverseParms.For(pawn, pawn.NormalMaxDanger()), null, mode);
            try { return path.Found ? path.TotalCost : float.PositiveInfinity; }
            finally { path.ReleaseToPool(); }
        }

        public static bool TryPlan(Pawn pawn, float range, float minDistance, float minimumSaving, out Plan plan, float ticksPerCell = 0)
        {
            plan = null;
            if (!Admitted(pawn) || range <= 1 || !TM_AutocastCombat.TryGetTravelDestination(pawn, out var goal)) return false;
            float distance = (goal.Cell - pawn.Position).LengthHorizontal;
            if (distance <= minDistance || distance >= 200) return false;
            var mode = (PathEndMode)EndMode.GetValue(pawn.pather);
            float currentCost = RemainingCost(pawn, pawn.Position, goal, mode);
            if (float.IsInfinity(currentCost)) return false;
            float bestCost = currentCost - Math.Max(30f, minimumSaving);
            IntVec3 best = IntVec3.Invalid;
            foreach (var cell in Candidates(pawn, goal.Cell, range).Distinct().Where(c => LandingAllowed(pawn, c, range, (int)Math.Ceiling((c - pawn.Position).LengthHorizontal * ticksPerCell))).Take(16))
            {
                float cost = RemainingCost(pawn, cell, goal, mode) + (cell - pawn.Position).LengthHorizontal * ticksPerCell;
                if (cost < bestCost) { bestCost = cost; best = cell; }
            }
            if (!best.IsValid) return false;
            plan = new Plan { Job = pawn.CurJob, Origin = pawn.Position, Destination = goal, Mode = mode, Landing = best, Range = range,
                HorizonTicks = (int)Math.Ceiling((best - pawn.Position).LengthHorizontal * ticksPerCell) };
            return true;
        }

        private static IEnumerable<IntVec3> Candidates(Pawn pawn, IntVec3 goal, float range)
        {
            // First try the actual goal/adjacent work cells, then useful nodes on
            // the existing route, then several directions around a blocked ray.
            yield return goal;
            foreach (var offset in GenAdj.AdjacentCells) yield return goal + offset;
            foreach (var cell in pawn.pather.curPath.NodesReversed.Take(pawn.pather.curPath.NodesLeftCount)
                .Where(c => (c - pawn.Position).LengthHorizontal < range && c != pawn.Position).Take(4)) yield return cell;
            double angle = Math.Atan2(goal.z - pawn.Position.z, goal.x - pawn.Position.x);
            foreach (double turn in new[] { 0d, .35, -.35, .7, -.7, 1.2, -1.2 })
            {
                double a = angle + turn;
                yield return pawn.Position + new IntVec3((int)(Math.Cos(a) * (range - 1)), 0, (int)(Math.Sin(a) * (range - 1)));
            }
        }

        public static bool StillValid(Pawn pawn, Plan plan)
        {
            return plan != null && Admitted(pawn) && pawn.Position == plan.Origin
                && ReferenceEquals(pawn.CurJob, plan.Job)
                && TM_AutocastCombat.TryGetTravelDestination(pawn, out var goal) && goal == plan.Destination
                && (PathEndMode)EndMode.GetValue(pawn.pather) == plan.Mode
                && LandingAllowed(pawn, plan.Landing, plan.Range, plan.HorizonTicks);
        }

        public static bool TryExecute(Pawn pawn, TMAbilityDef def, PawnAbility ability, float minDistance, bool might)
        {
            if (ability == null || def == null || ability.Def != def || ability.Pawn != pawn
                || ability.CooldownTicksLeft > 0 || !Admitted(pawn)) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            var fighter = pawn.GetCompAbilityUserMight();
            if (might)
            {
                if (fighter?.Stamina == null || fighter.Stamina.CurLevel < fighter.ActualStaminaCost(def)
                    || pawn.equipment?.Primary == null || pawn.equipment.Primary.def.IsRangedWeapon) return false;
            }
            else if (magic?.Mana == null || magic.Mana.CurLevel < magic.ActualManaCost(def)) return false;
            if (!TryPlan(pawn, def.MainVerb.range, minDistance, def.MainVerb.warmupTime * 60f, out var plan)
                || !StillValid(pawn, plan) || !TM_Teleport.Move(pawn, plan.Landing)) return false;

            // Move succeeded. Pay through the existing ability exactly once. It
            // owns cooldown, resource use, XP and magic history; no second event.
            try
            {
                Odyssey.ManualTravelSafety.Settle(ability);
                if (might) fighter.MightUserXP -= (int)(fighter.ActualStaminaCost(def) * 180 * .9f * fighter.xpGain * ModOptions.Settings.Instance.xpMultiplier);
                else magic.MagicUserXP -= (int)(magic.ActualManaCost(def) * 300 * .7f * magic.xpGain * ModOptions.Settings.Instance.xpMultiplier);
            }
            finally
            {
                // Notify_Teleported resets the path. Resume the same work only;
                // arrival may naturally advance its toil, which is intentional.
                if (pawn.Spawned && ReferenceEquals(pawn.CurJob, plan.Job)) pawn.pather.StartPath(plan.Destination, plan.Mode);
            }
            // Visual failures cannot undo paid movement or leave work stopped.
            try { DrawEffects(pawn, plan.Origin, might); }
            catch (Exception e) { TM_Log.Warning("Teleport", "Travel completed; visual effect failed: " + e); }
            return true;
        }

        public static bool TryExecuteFlight(Pawn pawn, TMAbilityDef def, PawnAbility ability)
        {
            if (ability == null || def == null || ability.Def != def || ability.Pawn != pawn
                || ability.CooldownTicksLeft > 0 || !Admitted(pawn)) return false;
            var magic = pawn.GetCompAbilityUserMagic();
            if (magic?.Mana == null || magic.Mana.CurLevel < magic.ActualManaCost(def)) return false;
            var carrierDef = DefDatabase<ThingDef>.GetNamedSilentFail("TM_WorkFlight");
            if (carrierDef == null || !TryPlan(pawn, def.MainVerb.range,
                Math.Max(8f, magic.ActualManaCost(def) * 200f), def.MainVerb.warmupTime * 60f,
                out var plan, TM_WorkFlight.TicksPerCell) || !StillValid(pawn, plan)) return false;
            // Flight's shipped definition does not require LoS. Respect custom
            // definitions that do, instead of flying through their obstacles.
            if (def.MainVerb.requiresLineOfSight && !GenSight.LineOfSight(pawn.Position, plan.Landing, pawn.Map)) return false;
            var flight = (TM_WorkFlight)ThingMaker.MakeThing(carrierDef);
            GenSpawn.Spawn(flight, pawn.Position, pawn.Map);
            if (!flight.Begin(pawn, plan)) { flight.Destroy(); return false; }
            // The pawn is now held safely; retries cannot start another flight.
            Odyssey.ManualTravelSafety.Settle(ability);
            return true;
        }

        public static void DrawEffects(Pawn pawn, IntVec3 origin, bool might)
        {
            foreach (var cell in new[] { origin, pawn.Position })
                for (int i = 0; i < 3; i++)
                {
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, cell.ToVector3Shifted(), pawn.Map,
                        Rand.Range(.6f, 1f), .4f, .1f, .8f, 0, 2, 0, 0);
                    TM_MoteMaker.ThrowGenericMote(might ? TorannMagicDefOf.Mote_Enchanting : TorannMagicDefOf.Mote_Casting,
                        cell.ToVector3Shifted(), pawn.Map, 1.4f, .2f, .05f, .4f, 0, 0, 0, 0);
                }
        }
    }
}
