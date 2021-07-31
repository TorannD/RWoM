using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic.ModOptions
{
    public class FactionOptionsWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool reset = false;
        private bool challenge = false;

        public Vector2 scrollPosition = Vector2.zero;

        FactionDictionary fDic = new FactionDictionary();

        public FactionOptionsWindow()
        {
            base.closeOnCancel = true;
            base.doCloseButton = true;
            base.doCloseX = true;
            base.absorbInputAroundWindow = true;
            base.forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            List<string> factions = Settings.Instance.FactionFighterSettings.Keys.ToList();
            int num = 0;
            float rowHeight = 28f;
            //GUI.BeginGroup(inRect);
            int scrollCount = 256;
            if(factions.Count > 8)
            {
                scrollCount = factions.Count * 40;
            }
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + scrollCount);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_FactionOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, EventOptionsWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_FactionOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 2.2f;
            num+=2;            
            Rect labelRect;
            Rect factionRowRect;
            Rect factionRowRect_ShiftRight;
            float mage = 1f;
            float fighter = 1f;
            for (int i = 0; i < factions.Count; i++)
            {
                labelRect = Controller.UIHelper.GetRowRect(rect1, rowHeight, num);
                Widgets.Label(labelRect, factions[i]);
                mage = Settings.Instance.FactionMageSettings[factions[i]];
                fighter = Settings.Instance.FactionFighterSettings[factions[i]];
                num++;
                GUI.color = Color.magenta;
                factionRowRect = Controller.UIHelper.GetRowRect(labelRect, rowHeight, num);
                mage = Widgets.HorizontalSlider(factionRowRect, Settings.Instance.FactionMageSettings[factions[i]], 0f, 5f, false, "Mages: " + Settings.Instance.FactionMageSettings[factions[i]].ToString("P0"), "0", "5", .1f);
                factionRowRect_ShiftRight = Controller.UIHelper.GetRowRect(factionRowRect, rowHeight, num);
                factionRowRect_ShiftRight.x += rect1.width + 20f;
                GUI.color = Color.green;
                fighter = Widgets.HorizontalSlider(factionRowRect_ShiftRight, Settings.Instance.FactionFighterSettings[factions[i]], 0f, 5f, false, "Fighters: " + Settings.Instance.FactionFighterSettings[factions[i]].ToString("P0"), "0", "5", .1f);
                fDic.SetFactionSettings(factions[i], fighter, mage);
                GUI.color = Color.white;
                Widgets.DrawLineHorizontal(inRect.x - 10f, factionRowRect.yMax, inRect.width - 15f);
                num++;                
            }
            num++;
            Rect rowRect99 = UIHelper.GetRowRect(rect1, rowHeight, num);
            rowRect99.width = 100f;
            reset = Widgets.ButtonText(rowRect99, "Default", true, false, true);
            if (reset)
            {
                Settings.Instance.FactionFighterSettings.Clear();
                ModOptions.FactionDictionary.InitializeFactionSettings();
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
