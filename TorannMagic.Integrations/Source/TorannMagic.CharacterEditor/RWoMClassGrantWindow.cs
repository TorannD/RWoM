using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Integrations.CharacterEditorCompat
{
    // Floating class grant tool opened from the Character Editor bridge.
    // Collapsed by default as a compact button window; expands into the full
    // picker. The target pawn is picked inside the window (cycle through the
    // player pawns on the current map), and the grant runs the full
    // enrollment workflow via ClassGrantTool. No CE internals are touched.
    // The window closes itself when the CE editor window disappears.
    public class RWoMClassGrantWindow : Window
    {
        private bool expanded;
        private List<Pawn> targets;
        private int targetIndex;
        private Vector2 scroll;
        private float listHeight = 1f;
        private string lastError;
        private string lastGranted;

        public RWoMClassGrantWindow()
        {
            doCloseX = false;
            forcePause = false;
            absorbInputAroundWindow = false;
            draggable = true;
            resizeable = false;
            closeOnAccept = false;
            closeOnCancel = false;
            doCloseButton = false;
        }

        // Force a known rect at every open: the collapsed strip was observed
        // rendering as a thin sliver when the live windowRect got shrunk by
        // something external. Logs the rect for remote diagnosis.
        public override void PreOpen()
        {
            base.PreOpen();
            var size = InitialSize;
            windowRect = new Rect(UI.screenWidth - size.x - 20f, 120f, size.x, size.y);
            Log.Message($"[RWoM CE] grant tool open, expanded={expanded}, rect={windowRect}");
        }

        // The vanilla window clips all content to rect.ContractedBy(18) — the
        // sizes below include that margin so buttons stay comfortably inside.
        public override Vector2 InitialSize =>
            expanded ? new Vector2(396f, 596f) : new Vector2(240f, 80f);

        private void RefreshTargets()
        {
            var map = Find.CurrentMap;
            targets = map == null
                ? new List<Pawn>()
                : map.mapPawns.AllPawnsSpawned
                    .Where(p => p.RaceProps.Humanlike && p.Faction == Faction.OfPlayer && !p.Dead && !p.Downed)
                    .OrderBy(p => p.LabelShortCap).ToList();
            if (targetIndex >= targets.Count) targetIndex = 0;
        }

        private Pawn Target => targets != null && targets.Count > 0
            ? targets[Mathf.Clamp(targetIndex, 0, targets.Count - 1)] : null;

        public override void DoWindowContents(Rect inRect)
        {
            // Close silently when the CE editor window is gone (user closed it).
            if (!Find.WindowStack.Windows.Any(w =>
                    w.GetType().FullName == "CharacterEditor.CEditor+EditorUI"))
            {
                Close(false);
                return;
            }

            Text.Font = GameFont.Small;

            if (!expanded)
            {
                if (Widgets.ButtonText(new Rect(inRect.x, inRect.y, inRect.width, 28f), "RWoM: Grant class"))
                {
                    expanded = true;
                    // InitialSize is only read once at PreOpen; the live window
                    // must be resized explicitly or the expanded layout draws
                    // outside the old (collapsed) rect.
                    windowRect = new Rect(windowRect.x, windowRect.y, 360f, 560f);
                }
                return;
            }

            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 70f, 28f), "RWoM: grant class");
            if (Widgets.ButtonText(new Rect(inRect.xMax - 68f, inRect.y, 66f, 26f), "—"))
            {
                expanded = false;
                windowRect = new Rect(windowRect.x, windowRect.y, 240f, 80f);
                return;
            }

            RefreshTargets();
            if (targets.Count == 0)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 30f), "(no player pawns on this map)");
                return;
            }

            // Target pawn: cycle arrows + name.
            var row = new Rect(inRect.x, inRect.y + 34f, inRect.width, 30f);
            if (Widgets.ButtonText(new Rect(row.x, row.y, 30f, row.height), "<"))
                targetIndex = (targetIndex + targets.Count - 1) % targets.Count;
            if (Widgets.ButtonText(new Rect(row.xMax - 30f, row.y, 30f, row.height), ">"))
                targetIndex = (targetIndex + 1) % targets.Count;
            var target = Target;
            Widgets.Label(new Rect(row.x + 36f, row.y + 4f, row.width - 76f, row.height),
                target != null ? target.LabelShortCap : "-");

            var top = inRect.y + 76f;
            var listHeight = 1200f;
            var outRect = new Rect(inRect.x, top, inRect.width, inRect.yMax - top);
            var view = new Rect(0f, 0f, outRect.width - 16f, listHeight);
            Widgets.BeginScrollView(outRect, ref scroll, view, true);
            var cursor = 0f;
            foreach (var option in TorannMagic.ClassProgression.ClassGrantTool.Options())
            {
                var classRow = new Rect(view.x, cursor, view.width, 30f);
                if (Widgets.ButtonText(classRow, option.Label))
                {
                    if (target == null)
                    {
                        lastError = "no pawn selected";
                        lastGranted = null;
                    }
                    else if (TorannMagic.ClassProgression.ClassGrantTool.TryGrant(target, option.Id, out var grantError))
                    {
                        lastGranted = option.Label;
                        lastError = null;
                    }
                    else
                    {
                        lastError = grantError ?? "refused";
                    }
                }
                cursor += 32f;
            }
            listHeight = Mathf.Max(cursor, outRect.height);
            Widgets.EndScrollView();
            var notice = lastError != null ? "Refused: " + lastError : lastGranted != null ? "Granted: " + lastGranted : null;
            if (notice != null)
                Widgets.Label(new Rect(inRect.x, inRect.yMax - 22f, inRect.width, 20f), notice);
        }
    }
}
