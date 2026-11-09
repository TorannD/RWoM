using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public sealed class BreachSealPlan
    {
        public Building_GravEngine Core;
        public List<IntVec3> EdgeCells = new List<IntVec3>();
        public List<IntVec3> SpawnCells = new List<IntVec3>();
    }

    public class BreachSealAbility : MagicAbility
    {
        private bool appliedReceipt;
        public BreachSealAbility() { }
        public BreachSealAbility(AbilityUser.CompAbilityUser user) : base(user) { }
        public BreachSealAbility(Pawn user, AbilityUser.AbilityDef def) : base(user, def) { }
        internal void SettleAppliedSeal()
        {
            appliedReceipt = true;
            PostAbilityAttempt();
        }
        public override void PostAbilityAttempt()
        {
            if (!appliedReceipt) return;
            appliedReceipt = false;
            base.PostAbilityAttempt();
        }
    }

    public static class BreachSeal
    {
        public const string WallName = "TM_BreachSealWall";
        public const int DurationTicks = 60000;

        public static List<IntVec3> GetFarEdgeCells(IntVec3 center, CellRect coreFootprint)
        {
            var cells = new List<IntVec3>();
            if (coreFootprint.Contains(center)) return cells;
            int dx = center.x * 2 - coreFootprint.minX - coreFootprint.maxX;
            int dz = center.z * 2 - coreFootprint.minZ - coreFootprint.maxZ;
            // Doubled footprint center avoids even-size bias; diagonal ties choose X.
            if (Math.Abs(dx) >= Math.Abs(dz))
                for (int offset = -1; offset <= 1; offset++)
                    cells.Add(new IntVec3(center.x + Math.Sign(dx), 0, center.z + offset));
            else
                for (int offset = -1; offset <= 1; offset++)
                    cells.Add(new IntVec3(center.x + offset, 0, center.z + Math.Sign(dz)));
            return cells;
        }

        public static string TargetRejection(Pawn caster, IntVec3 center)
        {
            TryPlan(caster, center, out _, out string reason);
            return reason;
        }

        public static bool TryPlan(Pawn caster, IntVec3 center, out BreachSealPlan plan, out string reason)
        {
            plan = new BreachSealPlan();
            reason = "TM_SpellItem_InvalidTarget";
            if (!ModsConfig.OdysseyActive || DefDatabase<ThingDef>.GetNamedSilentFail(WallName) == null)
            {
                reason = "TM_BreachSeal_Unavailable";
                return false;
            }
            if (caster == null || caster.Dead || !caster.Spawned || caster.Faction == null
                || !center.IsValid || !center.InBounds(caster.Map) || center.Fogged(caster.Map)) return false;
            Map map = caster.Map;
            foreach (Building_GravEngine core in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).OfType<Building_GravEngine>())
            {
                if (!core.Spawned || core.Destroyed || core.Map != map || core.Faction != caster.Faction
                    || !core.ValidSubstructureAt(center)) continue;
                if (plan.Core != null)
                {
                    reason = "TM_BreachSeal_AmbiguousCore";
                    return false;
                }
                plan.Core = core;
            }
            if (plan.Core == null) { reason = "TM_BreachSeal_NoCore"; return false; }
            plan.EdgeCells = GetFarEdgeCells(center, plan.Core.OccupiedRect());
            if (plan.EdgeCells.Count == 0) { reason = "TM_BreachSeal_CoreOverlap"; return false; }
            // Only the center and affected edge must belong to the ship. The other six cells are untouched.
            foreach (IntVec3 cell in plan.EdgeCells.Concat(new[] { center }))
            {
                if (!cell.InBounds(map) || cell.Fogged(map) || !plan.Core.ValidSubstructureAt(cell)
                    || cell.GetTerrain(map) == null || cell.GetTerrain(map).exposesToVacuum)
                {
                    reason = "TM_BreachSeal_InvalidFloor";
                    return false;
                }
            }
            foreach (IntVec3 cell in plan.EdgeCells)
            {
                bool sealedCell = false;
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Building wall && !(thing is Frame)
                        && wall.Faction == caster.Faction && wall.def.building?.isWall == true && wall.IsAirtight)
                    {
                        sealedCell = true;
                        continue;
                    }
                    if (thing is Pawn || thing is Building || thing is Blueprint || thing is Frame
                        || thing is Plant || thing.def.category == ThingCategory.Item)
                    {
                        reason = "TM_BreachSeal_Blocked";
                        return false;
                    }
                }
                if (!sealedCell) plan.SpawnCells.Add(cell);
            }
            if (plan.SpawnCells.Count == 0) { reason = "TM_BreachSeal_AlreadySealed"; return false; }
            reason = null;
            return true;
        }

        public static bool TryApply(Pawn caster, IntVec3 center)
            => TryApply(caster, center, DurationTicks);

        public static bool TryApply(Pawn caster, IntVec3 center, int duration)
        {
            if (!TryPlan(caster, center, out BreachSealPlan plan, out _)) return false;
            var created = new List<Thing>();
            Map map = caster.Map;
            try
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamed(WallName);
                foreach (IntVec3 cell in plan.SpawnCells)
                {
                    Thing wall = ThingMaker.MakeThing(def);
                    created.Add(wall);
                    wall.SetFaction(caster.Faction);
                }
                for (int i = 0; i < created.Count; i++)
                {
                    SpawnWall(created[i], plan.SpawnCells[i], map);
                    if (!created[i].Spawned || created[i].Map != map || created[i].Position != plan.SpawnCells[i])
                        throw new InvalidOperationException("Seal wall did not spawn at its planned cell.");
                    // Keep the native saved deadline and transport behavior; never mutate shared comp props.
                    var timer = created[i].TryGetComp<CompDestroyAfterDelay>();
                    if (timer != null) timer.spawnTick = Find.TickManager.TicksGame + duration - timer.Props.delayTicks;
                }
                return true;
            }
            catch (Exception exception)
            {
                // Never touch the pre-existing walls that made a one-cell repair possible.
                foreach (Thing wall in created)
                {
                    if (wall.Destroyed) continue;
                    try { wall.Destroy(DestroyMode.Vanish); }
                    catch (Exception cleanupException)
                    {
                        // A failing destruction notification must not prevent cleanup of the other new walls.
                        try { if (wall.Spawned) wall.DeSpawn(DestroyMode.Vanish); }
                        catch (Exception despawnException)
                        {
                            Compatibility.TM_Log.Warning("BreachSeal", "Seal rollback could not despawn a new wall: " + despawnException.Message);
                        }
                        Compatibility.TM_Log.Warning("BreachSeal", "Seal rollback destruction notification failed: " + cleanupException.Message);
                    }
                }
                Compatibility.TM_Log.Warning("BreachSeal", (created.Any(wall => wall.Spawned)
                    ? "Seal placement failed; rollback incomplete: " : "Seal placement rolled back: ") + exception.Message);
                return false;
            }
        }

        public static void SpawnWall(Thing wall, IntVec3 cell, Map map) => GenSpawn.Spawn(wall, cell, map);

        public static void DrawPreview(Pawn caster, IntVec3 center, float range = 15f)
        {
            if (caster == null || !caster.Spawned || !center.InBounds(caster.Map)) return;
            bool valid = TryPlan(caster, center, out BreachSealPlan plan, out string reason);
            if ((caster.Position - center).LengthHorizontal > range)
                reason = "TM_SpellItem_OutOfRange";
            else if (!GenSight.LineOfSight(caster.Position, center, caster.Map, skipFirstCell: true))
                reason = "TM_SpellItem_NoLineOfSight";
            valid = reason == null;
            EmitPreviewCells(CellRect.CenteredOn(center, 1).Cells.Where(c => c.InBounds(caster.Map)).ToList(), Color.white);
            if (plan.EdgeCells.Count > 0)
            {
                EmitPreviewCells(plan.EdgeCells.Where(c => c.InBounds(caster.Map)).ToList(), valid ? Color.gray : Color.red);
                if (valid) EmitPreviewCells(plan.SpawnCells, Color.cyan);
            }
        }
        public static string SelectionRejection(Pawn caster, IntVec3 center, float range, out BreachSealPlan plan)
        {
            TryPlan(caster, center, out plan, out string reason);
            if (caster == null || !caster.Spawned || !center.InBounds(caster.Map)) return reason;
            if ((caster.Position - center).LengthHorizontal > range) return "TM_SpellItem_OutOfRange";
            if (!GenSight.LineOfSight(caster.Position, center, caster.Map, skipFirstCell: true)) return "TM_SpellItem_NoLineOfSight";
            return reason;
        }
        public static void DrawTargetGui(Pawn caster, IntVec3 center, float range)
        {
            string reason = SelectionRejection(caster, center, range, out _);
            if (reason != null) EmitPreviewReason(reason);
        }
        public static bool ValidateSelection(Pawn caster, IntVec3 center, float range, bool showMessages)
        {
            string reason = SelectionRejection(caster, center, range, out var plan);
            if (reason == null) return true;
            if (showMessages && caster != null && caster.Spawned) ReportRejection(caster, center, plan, reason);
            return false;
        }
        private static void ReportRejection(Pawn caster, IntVec3 center, BreachSealPlan plan, string reason)
        {
            // Invoked by selection validation, never by per-frame world rendering.
            Messages.Message(reason.Translate(), caster, MessageTypeDefOf.RejectInput, false);
            string cells = string.Join("; ", plan.EdgeCells.Where(c => c.InBounds(caster.Map)).Select(c =>
                c + " floor=" + c.GetTerrain(caster.Map)?.defName + " platform=" + (plan.Core?.ValidSubstructureAt(c) == true)
                + " things=" + string.Join(",", c.GetThingList(caster.Map).Select(t => t.def.defName))));
            Compatibility.TM_Log.Message("BreachSeal", "Rejected click; pawn=" + caster.ThingID + "; center=" + center
                + "; reason=" + reason + "; core=" + plan.Core?.ThingID + "; edge=" + cells);
        }
        // Renderer boundaries keep headless tests on the real planner and preview policy.
        public static void EmitPreviewCells(List<IntVec3> cells, Color color) => GenDraw.DrawFieldEdges(cells, color);
        public static void EmitPreviewReason(string reason) => Widgets.MouseAttachedLabel(reason.Translate());
    }

    public class Building_BreachSeal : Building_TMBarrier
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Rand.PushState();
            try { base.DrawAt(drawLoc, flip); }
            finally { Rand.PopState(); }
        }
    }
}

namespace TorannMagic
{
    public class Verb_BreachSeal : Verb_SB
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
            => Odyssey.BreachSeal.ValidateSelection(CasterPawn, target.IsValid ? target.Cell : UI.MouseCell(), verbProps.range, showMessages)
                && base.ValidateTarget(target, showMessages);

        public override void OnGUI(LocalTargetInfo target)
        {
            base.OnGUI(target);
            Odyssey.BreachSeal.DrawTargetGui(CasterPawn, target.IsValid ? target.Cell : UI.MouseCell(), verbProps.range);
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo target)
        {
            if (!target.IsValid || Odyssey.BreachSeal.TargetRejection(CasterPawn, target.Cell) != null) return false;
            Map map = CasterPawn.Map;
            if (target.HasThing && (target.Thing.Destroyed || !target.Thing.Spawned || target.Thing.Map != map)) return false;
            // Match scroll/staff targeting: the center is untouched, so it need not be walkable.
            return (root - target.Cell).LengthHorizontal <= verbProps.range
                && GenSight.LineOfSight(root, target.Cell, map, skipFirstCell: true);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            Odyssey.BreachSeal.DrawPreview(CasterPawn, target.IsValid ? target.Cell : UI.MouseCell(), verbProps.range);
        }

        protected override bool TryCastShot()
        {
            var ability = Ability as Odyssey.BreachSealAbility;
            bool result = ability != null && ability.CanCastPowerCheck(AbilityUser.AbilityContext.Player, out _)
                && CanHitTargetFrom(CasterPawn.Position, currentTarget)
                && Odyssey.BreachSeal.TryApply(CasterPawn, currentTarget.Cell,
                    Odyssey.SpaceSpellProgression.Duration(CasterPawn, "TM_BreachSeal"));
            if (result) ability.SettleAppliedSeal();
            PostCastShot(result, out result);
            return result;
        }
    }
}
