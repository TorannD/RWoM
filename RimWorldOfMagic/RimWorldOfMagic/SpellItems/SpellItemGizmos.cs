using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace TorannMagic.SpellItems
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class SpellItemGizmos
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = Append(__instance, __result);
        }

        public static IEnumerable<Gizmo> Append(Pawn pawn, IEnumerable<Gizmo> original)
        {
            foreach (Gizmo gizmo in original) yield return gizmo;
            if (pawn?.Faction?.IsPlayer != true || pawn.Dead || pawn.RaceProps?.Humanlike != true) yield break;
            foreach (CompSpellItem item in SpellItemCasting.Sources(pawn))
            {
                string count = item.Props.requiresEquipped ? item.RemainingUses + "/" + item.Props.maxUses
                    : "×" + item.parent.stackCount;
                Command_Action command = new Command_Action
                {
                    defaultLabel = "TM_SpellItem_CastLabel".Translate(item.parent.LabelNoCount, count),
                    defaultDesc = item.parent.def.description,
                    icon = item.parent.def.uiIcon,
                    action = () => BeginTargeting(pawn, item)
                };
                string reason = item.UseRejection(pawn);
                if (reason != null) command.Disable(reason.Translate());
                yield return command;
            }
        }

        public static void BeginTargeting(Pawn pawn, CompSpellItem item)
        {
            if (item == null || item.UseRejection(pawn) != null) return;
            var parameters = new TargetingParameters
            {
                canTargetPawns = item.Props.spell != SpellItemKind.Blink && item.Props.spell != SpellItemKind.Portal && item.Props.spell != SpellItemKind.Resurrection && item.Props.spell != SpellItemKind.BreachSeal,
                canTargetLocations = item.Props.spell == SpellItemKind.BreachSeal || item.Props.spell == SpellItemKind.Blink || item.Props.spell == SpellItemKind.Portal || IsAttack(item.Props.spell),
                canTargetBuildings = IsAttack(item.Props.spell),
                canTargetItems = item.Props.spell == SpellItemKind.Resurrection,
                canTargetCorpses = item.Props.spell == SpellItemKind.Resurrection,
                validator = info => SpellItemEffects.TargetRejection(pawn,
                    info.HasThing ? new LocalTargetInfo(info.Thing) : new LocalTargetInfo(info.Cell), item.Props) == null
            };
            Find.Targeter.BeginTargeting(parameters,
                target => { if (item.Props.spell == SpellItemKind.Portal) ChoosePortalMap(pawn, item, target); else SpellItemCasting.TryStart(pawn, item.parent, target); },
                highlightAction: target => DrawPreview(pawn, item.Props,
                    target.IsValid ? target : new LocalTargetInfo(UI.MouseCell())),
                targetValidator: target => item.UseRejection(pawn) == null
                    && (item.Props.spell != SpellItemKind.BreachSeal || Odyssey.BreachSeal.ValidateSelection(pawn,
                        target.IsValid ? target.Cell : UI.MouseCell(), item.Props.range, true))
                    && SpellItemEffects.TargetRejection(pawn, target, item.Props) == null,
                caster: pawn,
                onGuiAction: target =>
                {
                    if (item.Props.spell == SpellItemKind.BreachSeal)
                        Odyssey.BreachSeal.DrawTargetGui(pawn, target.IsValid ? target.Cell : UI.MouseCell(), item.Props.range);
                });
        }

        private static bool IsAttack(SpellItemKind kind) => kind == SpellItemKind.Fireball || kind == SpellItemKind.Lightning;

        // Selection only: no source token or payment exists until both endpoints are chosen.
        private static void ChoosePortalMap(Pawn pawn, CompSpellItem item, LocalTargetInfo entrance)
        {
            Map origin = pawn.Map;
            Find.WindowStack.Add(new FloatMenu(Find.Maps.Select(map => new FloatMenuOption(
                map.Parent.LabelCap, () =>
                {
                    if (item.UseRejection(pawn) != null || !Find.Maps.Contains(map)) return;
                    Current.Game.CurrentMap = map; // Explicit destination selection, never on teleport.
                    Find.Targeter.BeginTargeting(new TargetingParameters
                    {
                        canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false, canTargetItems = false,
                        validator = t => SpellItemPortal.ValidDestination(origin, entrance.Cell, new GlobalTargetInfo(t.Cell, map))
                    }, destination => SpellItemCasting.TryStartPortal(pawn, item.parent, entrance, new GlobalTargetInfo(destination.Cell, map)),
                    null, () => { if (Find.Maps.Contains(origin)) Current.Game.CurrentMap = origin; });
                })).ToList()));
        }

        public static void DrawPreview(Pawn pawn, CompProperties_SpellItem props, LocalTargetInfo target)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || props == null) return;
            Map map = pawn.Map;
            DrawRing(pawn.Position, props.range, Color.white, cell => cell.InBounds(map) && !cell.Fogged(map)
                && (props.spell == SpellItemKind.Blink || GenSight.LineOfSight(pawn.Position, cell, map, skipFirstCell: true)));
            if (!target.IsValid || !target.Cell.InBounds(map) || target.Cell.Fogged(map)
                || (target.HasThing && target.Thing.Map != map)) return;
            bool valid = SpellItemEffects.TargetRejection(pawn, target, props) == null;
            DrawLine(pawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays),
                target.Cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays), valid ? SimpleColor.Green : SimpleColor.Red);
            if (props.spell == SpellItemKind.BreachSeal) Odyssey.BreachSeal.DrawPreview(pawn, target.Cell, props.range);
            if (props.spell == SpellItemKind.Fireball)
            {
                DrawRing(target.Cell, Projectile_SpellItemFireball.BlastRadius, valid ? Color.yellow : Color.red,
                    cell => cell.InBounds(map) && !cell.Fogged(map));
            }
        }

        // Rendering boundaries: headless checks exercise preview policy and real
        // sight/fog grids without invoking the GPU.
        public static void DrawRing(IntVec3 center, float radius, Color color, System.Func<IntVec3, bool> predicate)
            => GenDraw.DrawRadiusRing(center, radius, color, predicate);

        public static void DrawLine(Vector3 from, Vector3 to, SimpleColor color)
            => GenDraw.DrawLineBetween(from, to, color);
    }
}
