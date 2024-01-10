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
            int row = 0;
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
            row+=2;
            GUI.color = Color.magenta;
            Rect classRect = Controller.UIHelper.GetRowRect(rect1, rowHeight, row);
            Widgets.Label(classRect, "TM_EnabledMages".Translate());
            Rect classRectShiftRight = Controller.UIHelper.GetRowRect(classRect, rowHeight, row);
            classRectShiftRight.x += classRect.width + 98f;
            GUI.color = Color.green;
            Widgets.Label(classRectShiftRight, "TM_EnabledFighters".Translate());
            row++;
            GUI.color = Color.white;
            Rect slRect0 = Controller.UIHelper.GetRowRect(classRect, rowHeight, row);
            slRect0.width = inRect.width / 2.2f;
            inst.baseMageChance = Widgets.HorizontalSlider(slRect0, inst.baseMageChance, 0f, 1f, false, "baseMageChance".Translate() + " " + Rarity(inst.baseMageChance) + " " + TM_Calc.GetWeightedChance(inst.baseMageChance), roundTo: .002f);
            Rect slRect0ShiftRight = Controller.UIHelper.GetRowRect(slRect0, rowHeight, row);
            slRect0ShiftRight.x += slRect0.width + 20f;
            inst.baseFighterChance = Widgets.HorizontalSlider(slRect0ShiftRight, inst.baseFighterChance, 0f, 1f, false, "baseFighterChance".Translate() + " " + Rarity(inst.baseFighterChance) + " " + TM_Calc.GetWeightedChance(inst.baseFighterChance), roundTo: .002f);
            row++;
            Rect slRect1 = Controller.UIHelper.GetRowRect(slRect0, rowHeight, row);
            inst.advMageChance = Widgets.HorizontalSlider(slRect1, inst.advMageChance, 0f, 1f, false, "advMageChance".Translate() + " " + Rarity(inst.advMageChance) + " " + TM_Calc.GetWeightedChance(inst.advMageChance), roundTo: .002f);
            Rect slRect1ShiftRight = Controller.UIHelper.GetRowRect(slRect1, rowHeight, row);
            slRect1ShiftRight.x += slRect0.width + 20f;
            inst.advFighterChance = Widgets.HorizontalSlider(slRect1ShiftRight, inst.advFighterChance, 0f, 1f, false, "advFighterChance".Translate() + " " + Rarity(inst.advFighterChance) + " " + TM_Calc.GetWeightedChance(inst.advFighterChance), roundTo: .002f);
            row++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * row, inRect.width - 15f);

            // Given a matrix of ClassOptions, build the grid of checkboxes!
            void buildCheckboxGrid(Rect startingRect, Settings.CheckboxOption[][] grid)
            {
                foreach (Settings.CheckboxOption[] gridRow in grid)
                {
                    row++;
                    for (int i = 0; i < gridRow.Length; i++)
                    {
                        Rect newRect = Controller.UIHelper.GetRowRect(startingRect, rowHeight, row);
                        newRect.x = (startingRect.width + 98f) * i;
                        Widgets.CheckboxLabeled(newRect, gridRow[i].label.Translate(), ref gridRow[i].isEnabled);
                    }
                }
            }
            buildCheckboxGrid(classRect, new []
            {
                new []{ Settings.Wanderer, Settings.Wayfarer },
                new []{ Settings.Arcanist, Settings.Gladiator },
                new []{ Settings.FireMage, Settings.Bladedancer },
                new []{ Settings.IceMage, Settings.Sniper },
                new []{ Settings.LitMage, Settings.Ranger },
                new []{ Settings.Geomancer, Settings.Faceless },
                new []{ Settings.Druid, Settings.Psionic },
                new []{ Settings.Paladin, Settings.DeathKnight },
                new []{ Settings.Priest, Settings.Monk },
                new []{ Settings.Bard, Settings.Commander },
                new []{ Settings.Summoner, Settings.SuperSoldier },
                new []{ Settings.Necromancer, Settings.Shadow },
                new []{ Settings.Demonkin, Settings.Apothecary },
                new []{ Settings.Technomancer },
                new []{ Settings.BloodMage },
                new []{ Settings.Enchanter },
                new []{ Settings.Chronomancer },
                new []{ Settings.ChaosMage },
                new []{ Settings.Brightmage },
                new []{ Settings.Shaman },
                new []{ Settings.Golemancer },
                new []{ Settings.Empath },
            });
            row++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * row, inRect.width - 15f);
            row++;
            Rect slRect2 = Controller.UIHelper.GetRowRect(slRect1, rowHeight, row);
            inst.supportTraitChance = Widgets.HorizontalSlider(slRect2, inst.supportTraitChance, 0f, 1f, false, "supportTraitChance".Translate() + " " + (inst.supportTraitChance).ToString("P1"), "0", "1", .01f);
            //Rect slRect2ShiftRight = Controller.UIHelper.GetRowRect(slRect1, rowHeight, num);
            buildCheckboxGrid(classRect, new []
            {
                new []{ Settings.ArcaneConduit, Settings.Boundless },
                new []{ Settings.ManaWell, Settings.GiantsBlood },
                new []{ Settings.FaeBlood },
                new []{ Settings.Enlightened },
                new []{ Settings.Cursed }
            });
            row++;
            Widgets.DrawLineHorizontal(inRect.x - 10f, rowHeight * row, inRect.width - 15f);
            row++;
            GUI.color = Color.cyan;
            Rect customRect = Controller.UIHelper.GetRowRect(classRect, rowHeight, row);
            Widgets.Label(customRect, "TM_CustomClasses".Translate());
            GUI.color = Color.white;
            row++;
            for(int i = 0; i < TM_ClassUtility.CustomClasses.Length; i++)
            {
                TMDefs.TM_CustomClass cClass = TM_ClassUtility.CustomClasses[i];
                bool classEnabled = inst.CustomClass[cClass.classTrait.ToString()];
                if(cClass.classTrait == TorannMagicDefOf.TM_Brightmage)
                {
                    classEnabled = Settings.Brightmage.isEnabled;
                }
                else if(cClass.classTrait == TorannMagicDefOf.TM_Shaman)
                {
                    classEnabled = Settings.Shaman.isEnabled;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Golemancer)
                {
                    classEnabled = Settings.Golemancer.isEnabled;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Empath)
                {
                    classEnabled = Settings.Empath.isEnabled;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_TheShadow)
                {
                    classEnabled = Settings.Shadow.isEnabled;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Apothecary)
                {
                    classEnabled = Settings.Apothecary.isEnabled;
                }
                else if (cClass.classTrait == TorannMagicDefOf.TM_Empath)
                {
                    classEnabled = Settings.Empath.isEnabled;
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
                    Rect customRect1 = Controller.UIHelper.GetRowRect(customRect, rowHeight, row);
                    Widgets.CheckboxLabeled(customRect1, cClass.classTrait.degreeDatas.FirstOrDefault().label, ref classEnabled);
                    row++;
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
