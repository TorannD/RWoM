using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic.ModOptions
{
    public class ClassOptionsWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(640f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        public Vector2 scrollPosition = Vector2.zero;

        public ClassOptionsWindow()
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
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + 296f + TM_ClassUtility.CustomClasses().Count * 40);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);
            //GUI.BeginGroup(inRect);
            
            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_ClassOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, ClassOptionsWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_ClassOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.yellow;
            x = Text.CalcSize("TM_ClassWarning".Translate()).x;
            Rect warningRect = new Rect(inRect.width / 2f - (x / 2), inRect.y + ClassOptionsWindow.HeaderSize + 4f, inRect.width, ClassOptionsWindow.TextSize);
            Widgets.Label(warningRect, "TM_ClassWarning".Translate());
            x = Text.CalcSize("TM_RequiresRestart".Translate()).x;
            Rect restartRect = new Rect(inRect.width / 2f - (x / 2), inRect.y + ClassOptionsWindow.HeaderSize + ClassOptionsWindow.TextSize + 4f, inRect.width, ClassOptionsWindow.TextSize);
            Widgets.Label(restartRect, "TM_RequiresRestart".Translate());
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 3f;
            num+=3;
            GUI.color = Color.magenta;
            Rect classRect = Controller.UIHelper.GetRowRect(rect1, rowHeight, num);
            Widgets.Label(classRect, "TM_EnabledMages".Translate());
            Rect classRectShiftRight = Controller.UIHelper.GetRowRect(classRect, rowHeight, num);
            classRectShiftRight.x += classRect.width + 98f;
            GUI.color = Color.green;
            Widgets.Label(classRectShiftRight, "TM_EnabledFighters".Translate());
            num++;
            GUI.color = Color.white;
            Rect slRect0 = Controller.UIHelper.GetRowRect(classRect, rowHeight, num);
            slRect0.width = inRect.width / 2.2f;
            Settings.Instance.baseMageChance = Widgets.HorizontalSlider(slRect0, Settings.Instance.baseMageChance, 0f, 5f, false, "baseMageChance".Translate() + " " + Rarity(Settings.Instance.baseMageChance) + " " + TM_Calc.GetMagePrecurserChance().ToString("P1"), "0", "5", .01f);
            Rect slRect0ShiftRight = Controller.UIHelper.GetRowRect(slRect0, rowHeight, num);
            slRect0ShiftRight.x += slRect0.width + 20f;
            Settings.Instance.baseFighterChance = Widgets.HorizontalSlider(slRect0ShiftRight, Settings.Instance.baseFighterChance, 0f, 5f, false, "baseFighterChance".Translate() + " " + Rarity(Settings.Instance.baseFighterChance) + " " + TM_Calc.GetFighterPrecurserChance().ToString("P1"), "0", "5", .01f);
            num++;
            Rect slRect1 = Controller.UIHelper.GetRowRect(slRect0, rowHeight, num);
            Settings.Instance.advMageChance = Widgets.HorizontalSlider(slRect1, Settings.Instance.advMageChance, 0f, 2f, false, "advMageChance".Translate() + " " + Rarity(Settings.Instance.advMageChance) + " " + TM_Calc.GetMageSpawnChance().ToString("P1"), "0", "2", .01f);
            Rect slRect1ShiftRight = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            slRect1ShiftRight.x += slRect0.width + 20f;
            Settings.Instance.advFighterChance = Widgets.HorizontalSlider(slRect1ShiftRight, Settings.Instance.advFighterChance, 0f, 2f, false, "advFighterChance".Translate() + " " + Rarity(Settings.Instance.advFighterChance) + " " + TM_Calc.GetFighterSpawnChance().ToString("P1"), "0", "2", .01f);
            num++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * num, inRect.width - 15f);
            num++;
            Rect rowRect0 = Controller.UIHelper.GetRowRect(classRect, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect0, "TM_Wanderer".Translate(), ref Settings.Instance.Wanderer, false);
            Rect rowRect0ShiftRight = Controller.UIHelper.GetRowRect(rowRect0, rowHeight, num);
            rowRect0ShiftRight.x += rowRect0.width + 98f;
            Widgets.CheckboxLabeled(rowRect0ShiftRight, "TM_Wayfarer".Translate(), ref Settings.Instance.Wayfayer, false);
            num++;
            Rect rowRect = Controller.UIHelper.GetRowRect(rowRect0, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect, "TM_Arcanist".Translate(), ref Settings.Instance.Arcanist, false);
            Rect rowRectShiftRight = Controller.UIHelper.GetRowRect(rowRect, rowHeight, num);
            rowRectShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRectShiftRight, "TM_Gladiator".Translate(), ref Settings.Instance.Gladiator, false);
            num++;
            Rect rowRect1 = Controller.UIHelper.GetRowRect(rowRect, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect1, "TM_FireMage".Translate(), ref Settings.Instance.FireMage, false);
            Rect rowRect1ShiftRight = Controller.UIHelper.GetRowRect(rowRect1, rowHeight, num);
            rowRect1ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect1ShiftRight, "TM_Bladedancer".Translate(), ref Settings.Instance.Bladedancer, false);
            num++;
            Rect rowRect2 = Controller.UIHelper.GetRowRect(rowRect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect2, "TM_IceMage".Translate(), ref Settings.Instance.IceMage, false);
            Rect rowRect2ShiftRight = Controller.UIHelper.GetRowRect(rowRect2, rowHeight, num);
            rowRect2ShiftRight.x += rowRect1.width + 98f;
            Widgets.CheckboxLabeled(rowRect2ShiftRight, "TM_Sniper".Translate(), ref Settings.Instance.Sniper, false);
            num++;
            Rect rowRect3 = Controller.UIHelper.GetRowRect(rowRect2, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect3, "TM_LitMage".Translate(), ref Settings.Instance.LitMage, false);
            Rect rowRect3ShiftRight = Controller.UIHelper.GetRowRect(rowRect3, rowHeight, num);
            rowRect3ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect3ShiftRight, "TM_Ranger".Translate(), ref Settings.Instance.Ranger, false);
            num++;
            Rect rowRect4 = Controller.UIHelper.GetRowRect(rowRect3, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect4, "TM_Geomancer".Translate(), ref Settings.Instance.Geomancer, false);
            Rect rowRect4ShiftRight = Controller.UIHelper.GetRowRect(rowRect4, rowHeight, num);
            rowRect4ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect4ShiftRight, "TM_Faceless".Translate(), ref Settings.Instance.Faceless, false);
            num++;
            Rect rowRect5 = Controller.UIHelper.GetRowRect(rowRect4, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect5, "TM_Druid".Translate(), ref Settings.Instance.Druid, false);
            Rect rowRect5ShiftRight = Controller.UIHelper.GetRowRect(rowRect5, rowHeight, num);
            rowRect5ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect5ShiftRight, "TM_Psionic".Translate(), ref Settings.Instance.Psionic, false);
            num++;
            Rect rowRect6 = Controller.UIHelper.GetRowRect(rowRect5, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect6, "TM_Paladin".Translate(), ref Settings.Instance.Paladin, false);
            Rect rowRect6ShiftRight = Controller.UIHelper.GetRowRect(rowRect6, rowHeight, num);
            rowRect6ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect6ShiftRight, "TM_DeathKnight".Translate(), ref Settings.Instance.DeathKnight, false);
            num++;
            Rect rowRect7 = Controller.UIHelper.GetRowRect(rowRect6, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect7, "TM_Priest".Translate(), ref Settings.Instance.Priest, false);
            Rect rowRect7ShiftRight = Controller.UIHelper.GetRowRect(rowRect7, rowHeight, num);
            rowRect7ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect7ShiftRight, "TM_Monk".Translate(), ref Settings.Instance.Monk, false);
            num++;
            Rect rowRect8 = Controller.UIHelper.GetRowRect(rowRect7, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect8, "TM_Bard".Translate(), ref Settings.Instance.Bard, false);
            Rect rowRect8ShiftRight = Controller.UIHelper.GetRowRect(rowRect8, rowHeight, num);
            rowRect8ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect8ShiftRight, "TM_Commander".Translate(), ref Settings.Instance.Commander, false);
            num++;
            Rect rowRect9 = Controller.UIHelper.GetRowRect(rowRect8, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect9, "TM_Summoner".Translate(), ref Settings.Instance.Summoner, false);
            Rect rowRect9ShiftRight = Controller.UIHelper.GetRowRect(rowRect9, rowHeight, num);
            rowRect9ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect9ShiftRight, "TM_SuperSoldier".Translate(), ref Settings.Instance.SuperSoldier, false);
            num++;
            Rect rowRect10 = Controller.UIHelper.GetRowRect(rowRect9, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect10, "TM_Necromancer".Translate(), ref Settings.Instance.Necromancer, false);
            Rect rowRect10ShiftRight = Controller.UIHelper.GetRowRect(rowRect10, rowHeight, num);
            rowRect10ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect10ShiftRight, "TM_Shadow".Translate(), ref Settings.Instance.Shadow);
            num++;
            Rect rowRect11 = Controller.UIHelper.GetRowRect(rowRect10, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect11, "TM_Demonkin".Translate(), ref Settings.Instance.Demonkin, false);
            Rect rowRect11ShiftRight = Controller.UIHelper.GetRowRect(rowRect11, rowHeight, num);
            rowRect11ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect11ShiftRight, "TM_Apothecary".Translate(), ref Settings.Instance.Apothecary);
            num++;
            Rect rowRect12 = Controller.UIHelper.GetRowRect(rowRect11, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect12, "TM_Technomancer".Translate(), ref Settings.Instance.Technomancer, false);
            num++;
            Rect rowRect13 = Controller.UIHelper.GetRowRect(rowRect12, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect13, "TM_BloodMage".Translate(), ref Settings.Instance.BloodMage, false);
            num++;
            Rect rowRect14 = Controller.UIHelper.GetRowRect(rowRect13, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect14, "TM_Enchanter".Translate(), ref Settings.Instance.Enchanter, false);
            num++;
            Rect rowRect15 = Controller.UIHelper.GetRowRect(rowRect14, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect15, "TM_Chronomancer".Translate(), ref Settings.Instance.Chronomancer, false);
            num++;
            Rect rowRect16 = Controller.UIHelper.GetRowRect(rowRect15, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect16, "TM_ChaosMage".Translate(), ref Settings.Instance.ChaosMage, false);
            num++;
            Rect rowRect17 = Controller.UIHelper.GetRowRect(rowRect16, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect17, "TM_Brightmage".Translate(), ref Settings.Instance.Brightmage, false);
            num++;
            Rect rowRect18 = Controller.UIHelper.GetRowRect(rowRect17, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect18, "TM_Shaman".Translate(), ref Settings.Instance.Shaman, false);
            num++;
            Rect rowRect21 = Controller.UIHelper.GetRowRect(rowRect18, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect21, "TM_Golemancer".Translate(), ref Settings.Instance.Golemancer, false);
            num++;
            Rect rowRect22 = Controller.UIHelper.GetRowRect(rowRect21, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect22, "TM_Empath".Translate(), ref Settings.Instance.Empath, false);
            num++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * num, inRect.width - 15f);
            num++;
            Rect slRect2 = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            Settings.Instance.supportTraitChance = Widgets.HorizontalSlider(slRect2, Settings.Instance.supportTraitChance, 0f, 1f, false, "supportTraitChance".Translate() + " " + (Settings.Instance.supportTraitChance).ToString("P1"), "0", "1", .01f);
            //Rect slRect2ShiftRight = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            num++;
            Rect rowRect19 = Controller.UIHelper.GetRowRect(rowRect21, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect19, "TM_ArcaneConduit".Translate(), ref Settings.Instance.ArcaneConduit, false);
            Rect rowRect19ShiftRight = Controller.UIHelper.GetRowRect(rowRect19, rowHeight, num);
            rowRect19ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect19ShiftRight, "TM_Boundless".Translate(), ref Settings.Instance.Boundless, false);
            num++;
            Rect rowRect20 = Controller.UIHelper.GetRowRect(rowRect19, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20, "TM_ManaWell".Translate(), ref Settings.Instance.ManaWell, false);
            Rect rowRect20ShiftRight = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num);
            rowRect20ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect20ShiftRight, "GiantsBlood".Translate(), ref Settings.Instance.GiantsBlood, false);
            num++;
            Rect rowRect20a = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20a, "TM_FaeBlood".Translate(), ref Settings.Instance.FaeBlood, false);
            num++;
            Rect rowRect20b = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20b, "TM_Enlightened".Translate(), ref Settings.Instance.Enlightened, false);
            num++;
            Rect rowRect20c = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20c, "TM_Cursed".Translate(), ref Settings.Instance.Cursed, false);
            num++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * num, inRect.width - 15f);
            num++;
            GUI.color = Color.cyan;
            Rect customRect = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num);
            Widgets.Label(customRect, "TM_CustomClasses".Translate());
            GUI.color = Color.white;
            num++;
            for(int i = 0; i < TM_ClassUtility.CustomClasses().Count; i++)
            {
                TMDefs.TM_CustomClass cClass = TM_ClassUtility.CustomClasses()[i];
                bool classEnabled = Settings.Instance.CustomClass[cClass.classTrait.ToString()];
                if(cClass.classTrait == TorannMagicDefOf.TM_Brightmage)
                {
                    classEnabled = Settings.Instance.Brightmage;
                }
                else if(cClass.classTrait == TorannMagicDefOf.TM_Shaman)
                {
                    classEnabled = Settings.Instance.Shaman;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Golemancer)
                {
                    classEnabled = Settings.Instance.Golemancer;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Empath)
                {
                    classEnabled = Settings.Instance.Empath;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_TheShadow)
                {
                    classEnabled = Settings.Instance.Shadow;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Apothecary)
                {
                    classEnabled = Settings.Instance.Apothecary;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Empath)
                {
                    classEnabled = Settings.Instance.Empath;
                }
                if (cClass.shouldShow)
                {                    
                    //if (cClass.isMage && cClass.isFighter)
                    //{
                    //    GUI.color = Color.yellow;
                    //}
                    //else if(cClass.isMage)
                    //{
                    //    GUI.color = Color.magenta;
                    //}
                    //else if(cClass.isFighter)
                    //{
                    //    GUI.color = Color.green;
                    //}
                    //else
                    //{
                    //    GUI.color = Color.gray;
                    //}
                    Rect customRect1 = Controller.UIHelper.GetRowRect(customRect, rowHeight, num);
                    Widgets.CheckboxLabeled(customRect1, cClass.classTrait.degreeDatas.FirstOrDefault().label, ref classEnabled, false);                    
                    num++;
                }
                if (classEnabled != Settings.Instance.CustomClass[cClass.classTrait.ToString()])
                {
                    Settings.Instance.CustomClass.Remove(cClass.classTrait.ToString());
                    Settings.Instance.CustomClass.Add(cClass.classTrait.ToString(), classEnabled);
                }
                //GUI.color = Color.white;
            }
            
            //GUI.EndGroup();
            GUI.EndScrollView();
        }

        private string Rarity(float val)
        {
            string rarity = "";
            if (val == 0)
            {
                rarity = "None";
            }
            else if (val < .2f)
            {
                rarity = "Very Rare";
            }
            else if (val < .5f)
            {
                rarity = "Rare";
            }
            else if (val < 1f)
            {
                rarity = "Uncommon";
            }
            else if (val < 2f)
            {
                rarity = "Common";
            }
            else
            {
                rarity = "Frequent";
            }
            return rarity;
        }
    }   
}
