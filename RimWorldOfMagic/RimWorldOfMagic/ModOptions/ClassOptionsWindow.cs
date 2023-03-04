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
            var inst = Settings.Instance;
            int num = 0;
            float rowHeight = 28f;
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + 296f + TM_ClassUtility.CustomClasses.Length * 40);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);
            //GUI.BeginGroup(inRect);
            
            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_ClassOptions".Translate()).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, HeaderSize);
            Widgets.Label(headerRect, "TM_ClassOptions".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.yellow;
            x = Text.CalcSize("TM_ClassWarning".Translate()).x;
            Rect warningRect = new Rect(inRect.width / 2f - (x / 2), inRect.y + HeaderSize + 4f, inRect.width, TextSize);
            Widgets.Label(warningRect, "TM_ClassWarning".Translate());
            // x = Text.CalcSize("TM_RequiresRestart".Translate()).x;
            // Rect restartRect = new Rect(inRect.width / 2f - (x / 2), inRect.y + HeaderSize + TextSize + 4f, inRect.width, TextSize);
            // Widgets.Label(restartRect, "TM_RequiresRestart".Translate());
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 3f;
            num+=2;
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
            inst.baseMageChance = Widgets.HorizontalSlider(slRect0, inst.baseMageChance, 0f, 1f, false, "baseMageChance".Translate() + " " + Rarity(inst.baseMageChance) + " " + TM_Calc.GetWeightedChance(inst.baseMageChance), roundTo: .002f);
            Rect slRect0ShiftRight = Controller.UIHelper.GetRowRect(slRect0, rowHeight, num);
            slRect0ShiftRight.x += slRect0.width + 20f;
            inst.baseFighterChance = Widgets.HorizontalSlider(slRect0ShiftRight, inst.baseFighterChance, 0f, 1f, false, "baseFighterChance".Translate() + " " + Rarity(inst.baseFighterChance) + " " + TM_Calc.GetWeightedChance(inst.baseFighterChance), roundTo: .002f);
            num++;
            Rect slRect1 = Controller.UIHelper.GetRowRect(slRect0, rowHeight, num);
            inst.advMageChance = Widgets.HorizontalSlider(slRect1, inst.advMageChance, 0f, 1f, false, "advMageChance".Translate() + " " + Rarity(inst.advMageChance) + " " + TM_Calc.GetWeightedChance(inst.advMageChance), roundTo: .002f);
            Rect slRect1ShiftRight = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            slRect1ShiftRight.x += slRect0.width + 20f;
            inst.advFighterChance = Widgets.HorizontalSlider(slRect1ShiftRight, inst.advFighterChance, 0f, 1f, false, "advFighterChance".Translate() + " " + Rarity(inst.advFighterChance) + " " + TM_Calc.GetWeightedChance(inst.advFighterChance), roundTo: .002f);
            num++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * num, inRect.width - 15f);
            num++;
            Rect rowRect0 = Controller.UIHelper.GetRowRect(classRect, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect0, "TM_Wanderer".Translate(), ref inst.Wanderer);
            Rect rowRect0ShiftRight = Controller.UIHelper.GetRowRect(rowRect0, rowHeight, num);
            rowRect0ShiftRight.x += rowRect0.width + 98f;
            Widgets.CheckboxLabeled(rowRect0ShiftRight, "TM_Wayfarer".Translate(), ref inst.Wayfarer);
            num++;
            Rect rowRect = Controller.UIHelper.GetRowRect(rowRect0, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect, "TM_Arcanist".Translate(), ref inst.Arcanist);
            Rect rowRectShiftRight = Controller.UIHelper.GetRowRect(rowRect, rowHeight, num);
            rowRectShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRectShiftRight, "TM_Gladiator".Translate(), ref inst.Gladiator);
            num++;
            Rect rowRect1 = Controller.UIHelper.GetRowRect(rowRect, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect1, "TM_FireMage".Translate(), ref inst.FireMage);
            Rect rowRect1ShiftRight = Controller.UIHelper.GetRowRect(rowRect1, rowHeight, num);
            rowRect1ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect1ShiftRight, "TM_Bladedancer".Translate(), ref inst.Bladedancer);
            num++;
            Rect rowRect2 = Controller.UIHelper.GetRowRect(rowRect1, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect2, "TM_IceMage".Translate(), ref inst.IceMage);
            Rect rowRect2ShiftRight = Controller.UIHelper.GetRowRect(rowRect2, rowHeight, num);
            rowRect2ShiftRight.x += rowRect1.width + 98f;
            Widgets.CheckboxLabeled(rowRect2ShiftRight, "TM_Sniper".Translate(), ref inst.Sniper);
            num++;
            Rect rowRect3 = Controller.UIHelper.GetRowRect(rowRect2, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect3, "TM_LitMage".Translate(), ref inst.LitMage);
            Rect rowRect3ShiftRight = Controller.UIHelper.GetRowRect(rowRect3, rowHeight, num);
            rowRect3ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect3ShiftRight, "TM_Ranger".Translate(), ref inst.Ranger);
            num++;
            Rect rowRect4 = Controller.UIHelper.GetRowRect(rowRect3, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect4, "TM_Geomancer".Translate(), ref inst.Geomancer);
            Rect rowRect4ShiftRight = Controller.UIHelper.GetRowRect(rowRect4, rowHeight, num);
            rowRect4ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect4ShiftRight, "TM_Faceless".Translate(), ref inst.Faceless);
            num++;
            Rect rowRect5 = Controller.UIHelper.GetRowRect(rowRect4, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect5, "TM_Druid".Translate(), ref inst.Druid);
            Rect rowRect5ShiftRight = Controller.UIHelper.GetRowRect(rowRect5, rowHeight, num);
            rowRect5ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect5ShiftRight, "TM_Psionic".Translate(), ref inst.Psionic);
            num++;
            Rect rowRect6 = Controller.UIHelper.GetRowRect(rowRect5, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect6, "TM_Paladin".Translate(), ref inst.Paladin);
            Rect rowRect6ShiftRight = Controller.UIHelper.GetRowRect(rowRect6, rowHeight, num);
            rowRect6ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect6ShiftRight, "TM_DeathKnight".Translate(), ref inst.DeathKnight);
            num++;
            Rect rowRect7 = Controller.UIHelper.GetRowRect(rowRect6, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect7, "TM_Priest".Translate(), ref inst.Priest);
            Rect rowRect7ShiftRight = Controller.UIHelper.GetRowRect(rowRect7, rowHeight, num);
            rowRect7ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect7ShiftRight, "TM_Monk".Translate(), ref inst.Monk);
            num++;
            Rect rowRect8 = Controller.UIHelper.GetRowRect(rowRect7, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect8, "TM_Bard".Translate(), ref inst.Bard);
            Rect rowRect8ShiftRight = Controller.UIHelper.GetRowRect(rowRect8, rowHeight, num);
            rowRect8ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect8ShiftRight, "TM_Commander".Translate(), ref inst.Commander);
            num++;
            Rect rowRect9 = Controller.UIHelper.GetRowRect(rowRect8, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect9, "TM_Summoner".Translate(), ref inst.Summoner);
            Rect rowRect9ShiftRight = Controller.UIHelper.GetRowRect(rowRect9, rowHeight, num);
            rowRect9ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect9ShiftRight, "TM_SuperSoldier".Translate(), ref inst.SuperSoldier);
            num++;
            Rect rowRect10 = Controller.UIHelper.GetRowRect(rowRect9, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect10, "TM_Necromancer".Translate(), ref inst.Necromancer);
            Rect rowRect10ShiftRight = Controller.UIHelper.GetRowRect(rowRect10, rowHeight, num);
            rowRect10ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect10ShiftRight, "TM_Shadow".Translate(), ref inst.Shadow);
            num++;
            Rect rowRect11 = Controller.UIHelper.GetRowRect(rowRect10, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect11, "TM_Demonkin".Translate(), ref inst.Demonkin);
            Rect rowRect11ShiftRight = Controller.UIHelper.GetRowRect(rowRect11, rowHeight, num);
            rowRect11ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect11ShiftRight, "TM_Apothecary".Translate(), ref inst.Apothecary);
            num++;
            Rect rowRect12 = Controller.UIHelper.GetRowRect(rowRect11, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect12, "TM_Technomancer".Translate(), ref inst.Technomancer);
            num++;
            Rect rowRect13 = Controller.UIHelper.GetRowRect(rowRect12, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect13, "TM_BloodMage".Translate(), ref inst.BloodMage);
            num++;
            Rect rowRect14 = Controller.UIHelper.GetRowRect(rowRect13, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect14, "TM_Enchanter".Translate(), ref inst.Enchanter);
            num++;
            Rect rowRect15 = Controller.UIHelper.GetRowRect(rowRect14, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect15, "TM_Chronomancer".Translate(), ref inst.Chronomancer);
            num++;
            Rect rowRect16 = Controller.UIHelper.GetRowRect(rowRect15, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect16, "TM_ChaosMage".Translate(), ref inst.ChaosMage);
            num++;
            Rect rowRect17 = Controller.UIHelper.GetRowRect(rowRect16, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect17, "TM_Brightmage".Translate(), ref inst.Brightmage);
            num++;
            Rect rowRect18 = Controller.UIHelper.GetRowRect(rowRect17, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect18, "TM_Shaman".Translate(), ref inst.Shaman);
            num++;
            Rect rowRect21 = Controller.UIHelper.GetRowRect(rowRect18, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect21, "TM_Golemancer".Translate(), ref inst.Golemancer);
            num++;
            Rect rowRect22 = Controller.UIHelper.GetRowRect(rowRect21, rowHeight, num);
            Widgets.CheckboxLabeled(rowRect22, "TM_Empath".Translate(), ref inst.Empath);
            num++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * num, inRect.width - 15f);
            num++;
            Rect slRect2 = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            inst.supportTraitChance = Widgets.HorizontalSlider(slRect2, inst.supportTraitChance, 0f, 1f, false, "supportTraitChance".Translate() + " " + (inst.supportTraitChance).ToString("P1"), "0", "1", .01f);
            //Rect slRect2ShiftRight = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            num++;
            Rect rowRect19 = Controller.UIHelper.GetRowRect(rowRect21, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect19, "TM_ArcaneConduit".Translate(), ref inst.ArcaneConduit);
            Rect rowRect19ShiftRight = Controller.UIHelper.GetRowRect(rowRect19, rowHeight, num);
            rowRect19ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect19ShiftRight, "TM_Boundless".Translate(), ref inst.Boundless);
            num++;
            Rect rowRect20 = Controller.UIHelper.GetRowRect(rowRect19, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20, "TM_ManaWell".Translate(), ref inst.ManaWell);
            Rect rowRect20ShiftRight = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num);
            rowRect20ShiftRight.x += rowRect.width + 98f;
            Widgets.CheckboxLabeled(rowRect20ShiftRight, "TM_GiantsBlood".Translate(), ref inst.GiantsBlood);
            num++;
            Rect rowRect20a = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20a, "TM_FaeBlood".Translate(), ref inst.FaeBlood);
            num++;
            Rect rowRect20b = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20b, "TM_Enlightened".Translate(), ref inst.Enlightened);
            num++;
            Rect rowRect20c = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num); ;
            Widgets.CheckboxLabeled(rowRect20c, "TM_Cursed".Translate(), ref inst.Cursed);
            num++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * num, inRect.width - 15f);
            num++;
            GUI.color = Color.cyan;
            Rect customRect = Controller.UIHelper.GetRowRect(rowRect20, rowHeight, num);
            Widgets.Label(customRect, "TM_CustomClasses".Translate());
            GUI.color = Color.white;
            num++;
            for(int i = 0; i < TM_ClassUtility.CustomClasses.Length; i++)
            {
                TMDefs.TM_CustomClass cClass = TM_ClassUtility.CustomClasses[i];
                bool classEnabled = inst.CustomClass[cClass.classTrait.ToString()];
                if(cClass.classTrait == TorannMagicDefOf.TM_Brightmage)
                {
                    classEnabled = inst.Brightmage;
                }
                else if(cClass.classTrait == TorannMagicDefOf.TM_Shaman)
                {
                    classEnabled = inst.Shaman;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Golemancer)
                {
                    classEnabled = inst.Golemancer;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Empath)
                {
                    classEnabled = inst.Empath;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_TheShadow)
                {
                    classEnabled = inst.Shadow;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Apothecary)
                {
                    classEnabled = inst.Apothecary;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Empath)
                {
                    classEnabled = inst.Empath;
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
                    Widgets.CheckboxLabeled(customRect1, cClass.classTrait.degreeDatas.FirstOrDefault().label, ref classEnabled);
                    num++;
                }
                if (classEnabled != inst.CustomClass[cClass.classTrait.ToString()])
                {
                    inst.CustomClass.Remove(cClass.classTrait.ToString());
                    inst.CustomClass.Add(cClass.classTrait.ToString(), classEnabled);
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
            else if (val < .04f)
            {
                rarity = "Very Rare";
            }
            else if (val < .1f)
            {
                rarity = "Rare";
            }
            else if (val < .2f)
            {
                rarity = "Uncommon";
            }
            else if (val < .4f)
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
