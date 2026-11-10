using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    public sealed class Gizmo_FleshThirst : Gizmo
    {
        private readonly Pawn pawn;
        public Gizmo_FleshThirst(Pawn pawn) { this.pawn = pawn; Order = -101f; }
        public override float GetWidth(float maxWidth) => 100f;
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var data = FleshRepository.TryGet(pawn);
            if (data == null) return new GizmoResult(GizmoState.Clear);
            Rect rect = new Rect(topLeft.x + 2f, topLeft.y, GetWidth(maxWidth), 75f);
            if (parms.highLight) Widgets.DrawStrongHighlight(rect.ExpandedBy(12f));
            Utils.TMIsekaiUI.Panel(rect, FleshPresentation.Border, FleshPresentation.Corner);
            var oldFont = Text.Font; var oldAnchor = Text.Anchor;
            try {
                Text.Font = GameFont.Tiny; Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(rect.x + 4, rect.y + 3, rect.width - 8, 22), "TM_Flesh_ThirstLabel".Translate());
                Rect bar = new Rect(rect.x + 6, rect.y + 29, rect.width - 12, 36);
                Widgets.DrawBoxSolid(bar, new Color(.18f, .10f, .11f));
                Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * FleshPresentation.ThirstFraction(data), bar.height), new Color(.65f, .16f, .20f));
                Widgets.Label(bar, data.Thirst.ToString("0.#") + " / " + data.MaxThirst.ToString("0"));
                TooltipHandler.TipRegion(rect, "TM_Flesh_ThirstTip".Translate());
            } finally { Text.Font = oldFont; Text.Anchor = oldAnchor; }
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
