using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic.ModOptions
{
    public class EventOptionsWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool reset = false;
        private bool challenge = false;

        public Vector2 scrollPosition = Vector2.zero;

        public EventOptionsWindow()
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
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + 360f);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_EventOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, EventOptionsWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_EventOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 1.2f;
            num+=2;            
            Rect rowRect1 = Controller.UIHelper.GetRowRect(rect1, rowHeight, num);
            Settings.Instance.riftChallenge = Widgets.HorizontalSlider(rowRect1, Settings.Instance.riftChallenge, 0, 3, false, "riftChallenge".Translate() + " " + Challenge(Settings.Instance.riftChallenge), "0", "3", 1);
            num++;
            Rect rowRect2 = Controller.UIHelper.GetRowRect(rowRect1, rowHeight, num);
            Settings.Instance.wanderingLichChallenge = Widgets.HorizontalSlider(rowRect2, Settings.Instance.wanderingLichChallenge, 0, 3, false, "lichChallenge".Translate() + " " + Challenge(Settings.Instance.wanderingLichChallenge), "0", "3", 1);
            num++;            
            Rect rowRect3 = Controller.UIHelper.GetRowRect(rowRect2, rowHeight, num);
            Settings.Instance.demonAssaultChallenge = Widgets.HorizontalSlider(rowRect3, Settings.Instance.demonAssaultChallenge, 0, 3, false, "demonChallenge".Translate() + " " + Challenge(Settings.Instance.demonAssaultChallenge), "0", "3", 1);
            num++;
            num++;
            Rect rowRect99 = UIHelper.GetRowRect(rowRect3, rowHeight, num);
            rowRect99.width = 100f;
            Rect rowRect99ShiftRight1 = UIHelper.GetRowRect(rowRect99, rowHeight, num);
            rowRect99ShiftRight1.x = rowRect99.width + 40f;
            reset = Widgets.ButtonText(rowRect99, "Default", true, false, true);
            if (reset)
            {
                Settings.Instance.riftChallenge = 1f;
                Settings.Instance.wanderingLichChallenge = 1f;
                Settings.Instance.demonAssaultChallenge = 1f;
            }
            challenge = Widgets.ButtonText(rowRect99ShiftRight1, "Challenge", true, false, true);
            if (challenge)
            {
                Settings.Instance.riftChallenge = 3f;
                Settings.Instance.wanderingLichChallenge = 3f;
                Settings.Instance.demonAssaultChallenge = 3f;
            }
            //GUI.EndGroup();
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

        private string Challenge(float val)
        {
            string rarity = "";
            if (val == 0)
            {
                rarity = "None (never happens)";
            }
            else if (val <= 1)
            {
                rarity = "Easy";
            }
            else if (val <= 2)
            {
                rarity = "Normal";
            }
            else
            {
                rarity = "Hard";
            }

            return rarity;
        }
    }   

}
