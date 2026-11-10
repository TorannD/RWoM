using UnityEngine;
using Verse;

namespace TorannMagic.Utils
{
    /// <summary>
    /// Shared drawing helpers for the Isekai-styled UI: dark panels with a thin
    /// sky-blue border and decorative bracket corners, matching the look of the
    /// Isekai RPG family windows (Runeforge-style).
    /// </summary>
    public static class TMIsekaiUI
    {
        public static readonly Color Bg = new Color(0.055f, 0.055f, 0.075f, 0.94f);
        public static readonly Color Border = new Color(0.25f, 0.42f, 0.55f, 0.9f);
        public static readonly Color Corner = new Color(0.45f, 0.75f, 0.95f, 0.95f);
        public static readonly Color HeaderGold = new Color(0.85f, 0.65f, 0.30f);
        public static readonly Color LabelGold = new Color(0.95f, 0.80f, 0.35f);
        public static readonly Color Divider = new Color(0.28f, 0.46f, 0.60f, 0.8f);

        /// <summary>Dark panel with a 1px border and bracket corners.</summary>
        public static void Panel(Rect r, bool fill = true) => Panel(r, Border, Corner, fill);

        public static void Panel(Rect r, Color border, Color corner, bool fill = true)
        {
            if (fill)
            {
                Widgets.DrawBoxSolid(r, Bg);
            }
            Widgets.DrawBoxSolid(new Rect(r.x, r.y, r.width, 1f), border);
            Widgets.DrawBoxSolid(new Rect(r.x, r.yMax - 1f, r.width, 1f), border);
            Widgets.DrawBoxSolid(new Rect(r.x, r.y, 1f, r.height), border);
            Widgets.DrawBoxSolid(new Rect(r.xMax - 1f, r.y, 1f, r.height), border);

            const float c = 10f;
            const float t = 1.6f;
            // top-left
            Widgets.DrawBoxSolid(new Rect(r.x, r.y, c, t), corner);
            Widgets.DrawBoxSolid(new Rect(r.x, r.y, t, c), corner);
            // top-right
            Widgets.DrawBoxSolid(new Rect(r.xMax - c, r.y, c, t), corner);
            Widgets.DrawBoxSolid(new Rect(r.xMax - t, r.y, t, c), corner);
            // bottom-left
            Widgets.DrawBoxSolid(new Rect(r.x, r.yMax - t, c, t), corner);
            Widgets.DrawBoxSolid(new Rect(r.x, r.yMax - c, t, c), corner);
            // bottom-right
            Widgets.DrawBoxSolid(new Rect(r.xMax - c, r.yMax - t, c, t), corner);
            Widgets.DrawBoxSolid(new Rect(r.xMax - t, r.yMax - c, t, c), corner);
        }
    }
}
