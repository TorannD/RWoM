using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    internal class Gizmo_StatusBar : Gizmo
    {
        private static readonly Texture2D FullTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.32f, 0.4f, 0.0f));
        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Pawn pawn;
        public Enchantment.CompEnchantedItem itemComp;

        public override float GetWidth(float maxWidth)
        {
            return 75f;            
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (!pawn.DestroyedOrNull() && !pawn.Dead && itemComp != null)
            {

                Rect overRect = new Rect(topLeft.x + 2, topLeft.y, this.GetWidth(75), 75); //overall rect size (shell)
                if (parms.highLight)
                {
                    QuickSearchWidget.DrawStrongHighlight(overRect.ExpandedBy(12f));
                }
                Find.WindowStack.ImmediateWindow(984798, overRect, WindowLayer.GameUI, delegate
                    {
                        int barHeight = ((75 - 5));
                        Rect rect = overRect.AtZero().ContractedBy(6f); //inner, smaller rect
                        rect.height = barHeight;
                        Rect rect2 = rect; //label rect, starts at top             
                        Text.Font = GameFont.Tiny;
                        float fillPercent = 0;
                        float yShift = 0f;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        if (itemComp.NecroticEnergy != 0)
                        {
                            rect2.y += yShift;
                            try
                            {
                                fillPercent = itemComp.NecroticEnergy / 100f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_StatusBar.FullTex, Gizmo_StatusBar.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "" + (itemComp.NecroticEnergy.ToString("F0")) + " / " + 100f.ToString("F0"));
                            }
                            catch
                            {
                                fillPercent = 0f;
                                Widgets.FillableBar(rect2, fillPercent, Gizmo_StatusBar.FullTex, Gizmo_StatusBar.EmptyShieldBarTex, false);
                                Widgets.Label(rect2, "");
                            }
                            yShift += (barHeight) + 5f;
                        }
                        
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                    }, true, false, 1f);
                
            }
            else
            {
                Rect overRect = new Rect(topLeft.x + 2, topLeft.y, this.GetWidth(100), 75); //overall rect size (shell)
                float barHeight;
                float initialShift = 0;
                Find.WindowStack.ImmediateWindow(984798, overRect, WindowLayer.GameUI, delegate
                {
                    barHeight = ((75 - 5) / 1);
                    Rect rect = overRect.AtZero().ContractedBy(6f); //inner, smaller rect
                    rect.height = barHeight;
                    Rect rect2 = rect; //label rect, starts at top             
                    Text.Font = GameFont.Tiny;
                    float fillPercent = 0;
                    float yShift = initialShift;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    rect2.y += yShift;
                    fillPercent = 0f;
                    Widgets.FillableBar(rect2, fillPercent, Gizmo_StatusBar.FullTex, Gizmo_StatusBar.EmptyShieldBarTex, false);
                    Widgets.Label(rect2, "" );
                    yShift += (barHeight) + 5f;
                }, true, false, 1f);
            }
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
