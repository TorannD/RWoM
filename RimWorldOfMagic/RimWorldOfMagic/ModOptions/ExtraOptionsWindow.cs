using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic.ModOptions
{
    public class ExtraOptionsWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        public ExtraOptionsWindow()
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

            GUI.BeginGroup(inRect);
            
            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_ExtraOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, ClassOptionsWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_ExtraOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 3f;
            num+=3;
            //Labels
            Rect classRect = Controller.UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.Label(classRect, "TM_EnabledMages".Translate());
            Rect classRectShiftRight = Controller.UIHelper.GetRowRect(classRect, rowHeight, num);
            classRectShiftRight.x += classRect.width + 140f;
            GUI.color = Color.green;
            Widgets.Label(classRectShiftRight, "TM_EnabledFighters".Translate());
            num++;

            //Options
            GUI.color = Color.white;
            Rect rowRect = Controller.UIHelper.GetRowRect(classRect, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect, "TM_Arcanist".Translate(), ref Settings.Instance.Arcanist, false);
            Rect rowRectShiftRight = Controller.UIHelper.GetRowRect(rowRect, rowHeight, num);
            rowRectShiftRight.x += rowRect.width + 140f;
            Widgets.CheckboxLabeled(rowRectShiftRight, "TM_Gladiator".Translate(), ref Settings.Instance.Gladiator, false);
            num++;
            

            GUI.EndGroup();
        }        
    }   
}
