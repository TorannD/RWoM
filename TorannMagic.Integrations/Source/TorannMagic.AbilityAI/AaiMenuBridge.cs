using System;
using System.Collections.Generic;
using AbilityAI;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Integrations.AbilityAICompat
{
    // Thin, guarded access to Ability AI's internal autocast menu and touch
    // helpers. AbilityAI.dll's AssemblyVersion stays 1.0.0.0 across releases,
    // so a missing member surfaces as a lookup failure here, never as a
    // startup crash; the fallback keeps the public SetMode/ModeFor API.
    internal static class AaiMenuBridge
    {
        private static readonly MethodInfo OpenMenuMethod = AccessTools.Method(
            typeof(AbilityAIAutocast), "OpenMenu",
            new[] { typeof(Pawn), typeof(Def), typeof(bool), typeof(List<Pawn>), typeof(string) });
        private static readonly MethodInfo IsTouchMethod = AccessTools.Method(
            typeof(AbilityAIAutocast), "IsTouch", new[] { typeof(Def) });
        private static bool warned;

        private static readonly Color BadgeColor = new Color(0.55f, 0.85f, 1f);

        internal static bool IsTouch(Def def)
        {
            if (IsTouchMethod != null)
            {
                try { return (bool)IsTouchMethod.Invoke(null, new object[] { def }); }
                catch (Exception) { /* fall through to the shape check */ }
            }
            return def is AbilityDef abilityDef && (abilityDef.verbProperties?.range ?? 0f) <= 1.5f;
        }

        internal static void OpenMenu(Pawn clicked, Def def, List<Pawn> pawns, string title)
        {
            bool touch = IsTouch(def);
            if (OpenMenuMethod != null)
            {
                try
                {
                    OpenMenuMethod.Invoke(null, new object[] { clicked, def, touch, pawns, title });
                    return;
                }
                catch (Exception e) when (e is MissingMethodException || e is TargetInvocationException)
                {
                    WarnOnce(e);
                }
            }
            FallbackMenu(clicked, def, pawns, title);
        }

        // Ability AI versions without the internal menu still accept the
        // public per-pawn mode API; present the three modes directly.
        private static void FallbackMenu(Pawn clicked, Def def, List<Pawn> pawns, string title)
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("AAI_Mode_Off".Translate(), delegate
                {
                    foreach (Pawn p in pawns) AbilityAIAutocast.SetMode(p, def, AbilityAIAutocastMode.Off);
                }),
                new FloatMenuOption("AAI_Mode_Stationary".Translate(), delegate
                {
                    foreach (Pawn p in pawns) AbilityAIAutocast.SetMode(p, def, AbilityAIAutocastMode.Stationary);
                }),
                new FloatMenuOption("AAI_Mode_Full".Translate(), delegate
                {
                    foreach (Pawn p in pawns) AbilityAIAutocast.SetMode(p, def, AbilityAIAutocastMode.Full);
                })
            };
            Find.WindowStack.Add(new FloatMenu(options, title));
        }

        // The same mode badge Ability AI draws on Command_Ability: one marker
        // so the player can see which gizmos Ability AI drives.
        internal static void DrawBadge(Rect button, AbilityAIAutocastMode mode, bool shrunk)
        {
            string text = mode == AbilityAIAutocastMode.Full
                ? "AAI_Badge_Full".Translate()
                : "AAI_Badge_Stationary".Translate();
            Text.Font = GameFont.Tiny;
            Vector2 size = Text.CalcSize(text);
            float y = shrunk ? button.yMax - size.y - 1f : button.y + 3f + Text.LineHeight;
            float width = Mathf.Min(size.x + 4f, button.width - 6f);
            Rect r = new Rect(button.x + 3f, y, width, size.y);
            GUI.DrawTexture(r, TexUI.GrayTextBG);
            GUI.color = BadgeColor;
            Widgets.Label(new Rect(r.x + 2f, r.y, r.width - 2f, size.y), text.Truncate(r.width - 2f));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private static void WarnOnce(Exception e)
        {
            if (warned) return;
            warned = true;
            Log.Warning("[RWoM Integrations] Ability AI is missing its internal autocast menu; using the plain mode menu. " + e.Message);
        }
    }
}
