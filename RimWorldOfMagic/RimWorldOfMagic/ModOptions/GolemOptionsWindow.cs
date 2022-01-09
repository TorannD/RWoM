using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic.ModOptions
{
    public class GolemOptionsWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool reset = false;
        private bool challenge = false;

        public Vector2 scrollPosition = Vector2.zero;

        public GolemOptionsWindow()
        {
            base.closeOnCancel = true;
            base.doCloseButton = true;
            base.doCloseX = true;
            base.absorbInputAroundWindow = true;
            base.forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {            
            int num = 0;
            float rowHeight = 28f;
            //GUI.BeginGroup(inRect);
            int scrollCount = 256;            
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + scrollCount);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_GolemOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, GolemOptionsWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_GolemOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 1.2f;
            num+=2;
            Rect rowRect0 = Controller.UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect0, "TM_GolemShowDormantFrameWhileActive".Translate(), ref Settings.Instance.showDormantFrames, false);
            TooltipHandler.TipRegion(rowRect0, "TM_GolemShowDormantFrameWhileActiveDesc".Translate());
            //Rect rowRect0ShiftRight = Controller.UIHelper.GetRowRect(rowRect0, rowHeight, num);
            //rowRect0ShiftRight.x += rowRect0.width + 98f;
            //Widgets.CheckboxLabeled(rowRect0ShiftRight, "TM_Wayfarer".Translate(), ref Settings.Instance.Wayfayer, false);
            num++;
            Rect rowRect1 = Controller.UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect1, "TM_GolemShowColonistBar".Translate(), ref Settings.Instance.showGolemsOnColonistBar, false);
            TooltipHandler.TipRegion(rowRect1, "TM_GolemShowColonistBarDesc".Translate());            
            num++;
            Rect rowRect99 = UIHelper.GetRowRect(rect1, rowHeight, num);
            rowRect99.width = 100f;
            reset = Widgets.ButtonText(rowRect99, "Default", true, false, true);
            if (reset)
            {
                Settings.Instance.showDormantFrames = false;
            }
            GUI.EndScrollView();
        }

        public static class UIHelper
        {
            public static Rect GetRowRect(Rect inRect, float rowHeight, int row)
            {
                float y = rowHeight * (float)row;
                Rect result = new Rect(inRect.x, y, inRect.width, rowHeight);
                return result;
            }
        }
    }   

}
