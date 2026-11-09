using System.Collections.Generic;
using AbilityUser;
using HarmonyLib;
using AbilityAI;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Integrations.AbilityAICompat
{
    // Ability AI attaches its autocast right-click menu and mode badge to
    // Command_Ability. RWoM's bridged spells show the JecsLite owner's
    // Command_PawnAbility instead (TMBridgeAbility.GetGizmos), so this patch
    // offers the same handover there: right-click opens Ability AI's menu,
    // the drawn badge shows the chosen mode. The press is consumed before
    // the command's own click handling sees it, mirroring the way Ability AI
    // treats Command_Ability.
    [HarmonyPatch(typeof(Command_PawnAbility), "GizmoOnGUIInt")]
    internal static class Patch_CommandPawnAbility_Autocast
    {
        static void Prefix(Command_PawnAbility __instance, Rect butRect)
        {
            Event ev = Event.current;
            if (ev.type != EventType.MouseDown || ev.button != 1 || !Mouse.IsOver(butRect)) return;
            PawnAbility pawnAbility = __instance.pawnAbility;
            if (pawnAbility == null) return;
            if (!AbilityAISettings.Enabled || !AbilityAISettings.PlayerAutocast) return;
            Pawn pawn = pawnAbility.AbilityUser?.Pawn;
            TMBridgeAbilityDef bridge = VanillaBridgeSync.BridgeDefFor(pawnAbility.Def as TMAbilityDef);
            if (pawn == null || bridge == null || bridge.HasModExtension<AbilityAIExtension>() == false) return;
            ev.Use();

            // The clicked button stands for every selected pawn that holds the
            // same bridged spell (the grid groups by def), as Ability AI does.
            var pawns = new List<Pawn> { pawn };
            foreach (object selected in Find.Selector.SelectedObjectsListForReading)
            {
                if (selected is Pawn other && other != pawn
                    && other.abilities?.GetAbility(bridge) != null
                    && TmAbilityAIOwnership.IsOwned(other))
                {
                    pawns.Add(other);
                }
            }
            AaiMenuBridge.OpenMenu(pawn, bridge, pawns, __instance.LabelCap);
        }

        static void Postfix(Command_PawnAbility __instance, Rect butRect, GizmoRenderParms parms)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!AbilityAISettings.Enabled || !AbilityAISettings.PlayerAutocast) return;
            PawnAbility pawnAbility = __instance.pawnAbility;
            Pawn pawn = pawnAbility?.AbilityUser?.Pawn;
            TMBridgeAbilityDef bridge = VanillaBridgeSync.BridgeDefFor(pawnAbility.Def as TMAbilityDef);
            if (pawn == null || bridge == null || !bridge.HasModExtension<AbilityAIExtension>()) return;
            var mode = AbilityAIAutocast.HasAny(pawn)
                ? AbilityAIAutocast.ModeFor(pawn, bridge)
                : AbilityAIAutocastMode.Off;
            if (mode == AbilityAIAutocastMode.Off) return;
            AaiMenuBridge.DrawBadge(butRect, mode, parms.shrunk);
        }
    }
}
