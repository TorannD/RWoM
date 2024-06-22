using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic.ModOptions
{
    public class DisplayOptionsWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool reset = false;
        private bool challenge = false;

        private bool changeOffset = false;
        private bool changeOffsetVal = false;
        private bool changeCloakDepth = false;
        private bool changeCloakDepthNorth = false;

        private string multilayerVal = "0";
        private string cloakVal = "0";
        private string cloakNorthVal = "0";

        public Vector2 scrollPosition = Vector2.zero;

        public DisplayOptionsWindow()
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

            multilayerVal = Settings.Instance.offsetMultiLayerClothingAmount.ToString();
            cloakVal = Settings.Instance.cloakDepth.ToString();
            cloakNorthVal = Settings.Instance.cloakDepthNorth.ToString();

            //GUI.BeginGroup(inRect);
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + 360f);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_DisplayOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, EventOptionsWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_DisplayOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 1.2f;
            num+=2;
            Rect rowRect1 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect1, "TM_CameraSnap".Translate(), ref Settings.Instance.cameraSnap, false);
            TooltipHandler.TipRegion(rowRect1, "TM_CameraSnapDesc".Translate());
            num++;
            Rect rowRect1ShiftRight = UIHelper.GetRowRect(rowRect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect1ShiftRight, "showLevelUpMessage".Translate(), ref Settings.Instance.showLevelUpMessage, false);
            num++;
            Rect rowRect2 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect2, "AIMarking".Translate(), ref Settings.Instance.AIMarking, false);
            TooltipHandler.TipRegion(rowRect2, "TM_AIMarkingDesc".Translate());
            num++;
            Rect rowRect21 = UIHelper.GetRowRect(rowRect2, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect21, "AIFighterMarking".Translate(), ref Settings.Instance.AIFighterMarking, false);
            TooltipHandler.TipRegion(rowRect21, "TM_AIFighterMarkingDesc".Translate());
            num++;
            Rect rowRect3 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect3, "AIFriendlyMarking".Translate(), ref Settings.Instance.AIFriendlyMarking, false);
            TooltipHandler.TipRegion(rowRect21, "TM_AIFriendlyMarkingDesc".Translate());
            num++;
            Rect rowRect4 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect4, "showClassIconOnColonistBar".Translate(), ref Settings.Instance.showClassIconOnColonistBar, false);
            num++;
            Rect rowRect4ShiftRight = UIHelper.GetRowRect(rowRect4, rowHeight, num);
            if (Settings.Instance.showClassIconOnColonistBar)
            {
                Settings.Instance.classIconSize = Widgets.HorizontalSlider(rowRect4ShiftRight, Settings.Instance.classIconSize, .5f, 2.5f, false, "classIconSize".Translate() + " " + Settings.Instance.classIconSize.ToString("P1"), "0", "2.5", .01f);
            }
            num++;
            Rect rowRect5 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect5, "showMagicGizmo".Translate(), ref Settings.Instance.showGizmo, false);
            num++;
            Rect rowRect5ShiftRight = UIHelper.GetRowRect(rowRect5, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect5ShiftRight, "TM_shrinkIcons".Translate(), ref Settings.Instance.shrinkIcons, false);
            TooltipHandler.TipRegion(rowRect5ShiftRight, "TM_shrinkIconsDesc".Translate());
            num++;
            Rect rowRect6 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect6, "showUndeadPawnChange".Translate(), ref Settings.Instance.changeUndeadPawnAppearance, false);
            TooltipHandler.TipRegion(rowRect6, "showUndeadPawnChangeDesc".Translate());
            num++;
            Rect rowRect6ShiftRight = UIHelper.GetRowRect(rowRect6, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect6ShiftRight, "showUndeadAnimalChange".Translate(), ref Settings.Instance.changeUndeadAnimalAppearance, false);
            TooltipHandler.TipRegion(rowRect6ShiftRight, "showUndeadAnimalChangeDesc".Translate());
            num++;
            Rect rowRect7 = UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect7, "TM_AlterClothingDepth".Translate(), ref Settings.Instance.offSetClothing, false);
            TooltipHandler.TipRegion(rowRect7, "TM_AlterClothingDepthDesc".Translate());
            num++;
            if (Settings.Instance.offSetClothing)
            {
                Rect rowRect70 = UIHelper.GetRowRect(rect1, rowHeight, num);
                Settings.Instance.offsetApplyAtValue = Widgets.HorizontalSlider(rowRect70, Settings.Instance.offsetApplyAtValue, -1f, 1f, false, "TM_offsetApplyAt".Translate() + " " + Settings.Instance.offsetApplyAtValue, "-1", "1", .000001f);
                TooltipHandler.TipRegion(rowRect70, "TM_offsetApplyAtDesc".Translate());
                num++;
                Rect rowRect70ShiftRight = UIHelper.GetRowRect(rowRect70, rowHeight, num);
                Rect rectChangeOffsetVal = new Rect(rowRect70ShiftRight.x, rowRect70ShiftRight.y + .5f, 100f, 28f);
                changeOffsetVal = Widgets.ButtonText(rectChangeOffsetVal, "TM_SetVerbatum".Translate(), true, false, true);
                if (changeOffsetVal)
                {
                    ChangeOffsetWindow newWindow = new ChangeOffsetWindow();
                    newWindow.currentOffset = Settings.Instance.offsetApplyAtValue.ToString();
                    Find.WindowStack.Add(newWindow);
                }
                num++;
                num++;
                Rect rowRect71 = UIHelper.GetRowRect(rect1, rowHeight, num);
                Settings.Instance.offsetMultiLayerClothingAmount = Widgets.HorizontalSlider(rowRect71, Settings.Instance.offsetMultiLayerClothingAmount, -1f, 1f, false, "TM_offsetMultilayerClothing".Translate() + " " + Settings.Instance.offsetMultiLayerClothingAmount, "-1", "1", .000001f);
                TooltipHandler.TipRegion(rowRect71, "TM_offsetMultilayerClothingDesc".Translate());
                num++;
                Rect rowRect71ShiftRight = UIHelper.GetRowRect(rowRect71, rowHeight, num);
                Rect rectChangeOffset = new Rect(rowRect71ShiftRight.x, rowRect71ShiftRight.y+.5f, 100f, 28f);
                changeOffset = Widgets.ButtonText(rectChangeOffset, "TM_SetVerbatum".Translate(), true, false, true);
                if (changeOffset)
                {
                    ChangeOffsetWindow newWindow = new ChangeOffsetWindow();
                    newWindow.currentOffset = Settings.Instance.offsetMultiLayerClothingAmount.ToString();
                    Find.WindowStack.Add(newWindow);
                }
                num++;
                num++;
                Rect rowRect72 = UIHelper.GetRowRect(rect1, rowHeight, num);
                Settings.Instance.cloakDepth = Widgets.HorizontalSlider(rowRect72, Settings.Instance.cloakDepth, -1f, 1f, false, "TM_CloakDepth".Translate() + " " + Settings.Instance.cloakDepth, "-1", "1", .000001f);
                TooltipHandler.TipRegion(rowRect72, "TM_CloakDepthDesc".Translate());
                num++;
                Rect rowRect72ShiftRight = UIHelper.GetRowRect(rowRect72, rowHeight, num);
                Rect rectChangeCloakDepth = new Rect(rowRect72ShiftRight.x, rowRect72ShiftRight.y + .5f, 100f, 28f);
                changeCloakDepth = Widgets.ButtonText(rectChangeCloakDepth, "TM_SetVerbatum".Translate(), true, false, true);
                if (changeCloakDepth)
                {
                    ChangeCloakDepthWindow newWindow = new ChangeCloakDepthWindow();
                    newWindow.currentOffset = Settings.Instance.cloakDepth.ToString();
                    Find.WindowStack.Add(newWindow);
                }
                num++;
                num++;
                Rect rowRect73 = UIHelper.GetRowRect(rect1, rowHeight, num);
                Settings.Instance.cloakDepthNorth = Widgets.HorizontalSlider(rowRect73, Settings.Instance.cloakDepthNorth, -1f, 1f, false, "TM_CloakDepthNorth".Translate() + " " + Settings.Instance.cloakDepthNorth, "-1", "1", .000001f);
                TooltipHandler.TipRegion(rowRect73, "TM_CloakDepthNorthDesc".Translate());
                num++;
                Rect rowRect73ShiftRight = UIHelper.GetRowRect(rowRect73, rowHeight, num);
                Rect rectChangeCloakDepthNorth = new Rect(rowRect73ShiftRight.x, rowRect73ShiftRight.y + .5f, 100f, 28f);
                changeCloakDepthNorth = Widgets.ButtonText(rectChangeCloakDepthNorth, "TM_SetVerbatum".Translate(), true, false, true);
                if (changeCloakDepthNorth)
                {
                    ChangeCloakDepthNorthWindow newWindow = new ChangeCloakDepthNorthWindow();
                    newWindow.currentOffset = Settings.Instance.cloakDepthNorth.ToString();
                    Find.WindowStack.Add(newWindow);
                }
            }
            num++;
            num++;
            Rect rowRect99 = UIHelper.GetRowRect(rect1, rowHeight, num);
            rowRect99.width = 100f;
            reset = Widgets.ButtonText(rowRect99, "Default", true, false, true);
            if (reset)
            {
                Settings.Instance.AIMarking = true;
                Settings.Instance.AIFighterMarking = false;
                Settings.Instance.AIFriendlyMarking = false;
                Settings.Instance.showIconsMultiSelect = true;
                Settings.Instance.showGizmo = true;
                Settings.Instance.showLevelUpMessage = true;
                Settings.Instance.changeUndeadPawnAppearance = true;
                Settings.Instance.changeUndeadAnimalAppearance = true;
                Settings.Instance.showClassIconOnColonistBar = true;
                Settings.Instance.classIconSize = 1f;
                Settings.Instance.shrinkIcons = false;
                Settings.Instance.cameraSnap = true;
                Settings.Instance.offSetClothing = false;
                Settings.Instance.offsetMultiLayerClothingAmount = -.025384f;
                Settings.Instance.cloakDepth = 0f;
                Settings.Instance.cloakDepthNorth = 0f;
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
