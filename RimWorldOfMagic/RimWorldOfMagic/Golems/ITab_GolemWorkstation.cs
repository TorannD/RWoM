using RimWorld;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    [StaticConstructorOnStartup]
    internal class ITab_GolemWorkstation : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(432f, 550f);
        private Vector2 scrollPosition = Vector2.zero;
        private bool abilityOptions = false;

        private string pawnMasterName = "None";

        private static List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
                if (singleSelectedThing != null && singleSelectedThing is Building_TMGolemBase)
                {
                    Building_TMGolemBase golem_building = singleSelectedThing as Building_TMGolemBase;
                    if(golem_building != null)
                    {
                        return golem_building.Upgrades;
                    }
                }
                return null;
            }
        }

        public static int ActiveUpgradeCount
        {
            get
            {
                int num = 0;
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.currentLevel > 0)
                    {
                        num++;
                    }
                }
                return num;
            }
        }

        public override bool IsVisible
        {
            get
            {
                return Upgrades != null && Upgrades.Count > 0;
            }
        }

        public ITab_GolemWorkstation()
        {
            this.size = ITab_GolemWorkstation.WinSize;
            this.labelKey = "TabGolem";
        }

        protected override void FillTab()
        {
            Building_TMGolemBase golem_building  = Find.Selector.SingleSelectedThing as Building_TMGolemBase;
            if (golem_building != null && Upgrades != null)
            {
                if(golem_building.GolemComp.pawnMaster != null)
                {
                    pawnMasterName = golem_building.GolemComp.pawnMaster.Label;
                }
                else
                {
                    pawnMasterName = "None";
                }
                int num = 2;
                float scrollHeight = ActiveUpgradeCount > 9 ? 380 + ActiveUpgradeCount * 18 : 550;
                Rect canvas = new Rect(17f, 17f, ITab_GolemWorkstation.WinSize.x, ITab_GolemWorkstation.WinSize.y);
                Rect rect = new Rect(canvas.x, canvas.y, ITab_GolemWorkstation.WinSize.x -34f, ITab_GolemWorkstation.WinSize.y - 34f);
                Rect sRect = new Rect(rect.x, rect.y, rect.width-34, scrollHeight);
                scrollPosition = GUI.BeginScrollView(rect, scrollPosition, sRect, false, true);

                Rect rect2 = GetRowRect(rect, num, 10);
                rect2.width -= 34f;
                Text.Font = GameFont.Small;
                if (golem_building.GolemPawn != null)
                {
                    string tmpName = Widgets.TextField(rect2, golem_building.GolemPawn.LabelShortCap != "Blank" ? golem_building.GolemPawn.LabelShortCap : "");
                    if (tmpName != "")
                    {
                        golem_building.GolemPawn.Name = NameTriple.FromString(tmpName);
                    }
                    else
                    {
                        golem_building.GolemPawn.Name = NameTriple.FromString("Blank");
                    }
                    num += 2;

                    Rect rectPawnMaster = GetRowRect(rect2, num, 10);
                    if(Widgets.ButtonText(rectPawnMaster, pawnMasterName))
                    {
                        List<string> tmpPawns = new List<string>();
                        tmpPawns.Add("None");
                        foreach (Pawn p in TM_Calc.GolemancersInFaction(golem_building.Faction).Where((Pawn x) => x != golem_building.GolemComp.pawnMaster))
                        {
                            tmpPawns.Add(p.LabelShort);
                        }
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        foreach(string pawnName in tmpPawns)
                        {
                            string text = pawnName;
                            FloatMenuOption item = new FloatMenuOption(text, delegate
                            {
                                if(pawnName != pawnMasterName)
                                {
                                    golem_building.GolemComp.pawnMaster = TM_Calc.GolemancersInFaction(golem_building.Faction).FirstOrDefault((Pawn p) => p.LabelShort == pawnName);                                    
                                }
                                if(pawnName == "None")
                                {
                                    golem_building.GolemComp.pawnMaster = null;
                                }
                            });
                            list.Add(item);
                        }
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                    TooltipHandler.TipRegion(rectPawnMaster, "TM_GolemMasterDesc".Translate());
                    num += 2;

                    abilityOptions = Widgets.ButtonText(GetRowRect(rect2, num, 10), "TM_GolemAbilitiesButton".Translate(), true, false, true);
                    if (abilityOptions)
                    {
                        Rect rectAbilities = new Rect(64f, 64f, 480, 600);
                        GolemAbilitiesWindow newWindow = new GolemAbilitiesWindow();
                        newWindow.cg = golem_building.GolemComp;
                        Find.WindowStack.Add(newWindow);
                    }
                    num += 2;

                    Rect rectFollowMaster = GetRowRect(rect2, num);
                    rectFollowMaster.width = rect2.width / 2.2f;
                    Widgets.CheckboxLabeled(rectFollowMaster, "TM_GolemFollowsMaster".Translate(), ref golem_building.GolemComp.followsMaster, false);
                    TooltipHandler.TipRegion(rectFollowMaster, "TM_GolemFollowsMasterDesc".Translate());
                    Rect rectFollowMasterDrafted = rectFollowMaster;
                    rectFollowMasterDrafted.x += rectFollowMasterDrafted.width + 34f;
                    Widgets.CheckboxLabeled(rectFollowMasterDrafted, "TM_GolemDraftedFollow".Translate(), ref golem_building.GolemComp.followsMasterDrafted, false);
                    TooltipHandler.TipRegion(rectFollowMasterDrafted, "TM_GolemDraftedFollowDesc".Translate());
                    num += 2;
                    Rect rectShowDormantPos = GetRowRect(rect2, num);
                    rectShowDormantPos.width = rect2.width / 2.2f;
                    Widgets.CheckboxLabeled(rectShowDormantPos, "TM_GolemShowDormant".Translate(), ref golem_building.GolemPawn.showDormantPosition, false);
                    TooltipHandler.TipRegion(rectShowDormantPos, "TM_GolemShowDormantDesc".Translate());
                    num += 2;

                    Rect rectStayDormant = GetRowRect(rect2, num);
                    Widgets.CheckboxLabeled(rectStayDormant, "TM_GolemDormantWhenUpgrading".Translate(), ref golem_building.GolemComp.remainDormantWhenUpgrading, false);
                    TooltipHandler.TipRegion(rectStayDormant, "TM_GolemDormantWhenUpgradingDesc".Translate());
                    num += 2;
                    Rect rectUseAbilitiesDormant = GetRowRect(rect2, num); 
                    Widgets.CheckboxLabeled(rectUseAbilitiesDormant, "TM_GolemUseAbilitiesWhileDormant".Translate(), ref golem_building.GolemComp.useAbilitiesWhenDormant, false);
                    TooltipHandler.TipRegion(rectUseAbilitiesDormant, "TM_GolemUseAbilitiesWhileDormantDesc".Translate());
                    num += 2;

                    Rect rectThreatRange = GetRowRect(rect2, num);
                    golem_building.GolemComp.threatRange = Widgets.HorizontalSlider(rectThreatRange, golem_building.GolemComp.threatRange, 0f, 100f, false, "TM_GolemThreatRange".Translate() + " " + golem_building.GolemComp.threatRange.ToString("N"), "0", "100", 1f);
                    TooltipHandler.TipRegion(rectThreatRange, "TM_GolemThreatRangeDesc".Translate());
                    num += 2;

                    Rect rectAbilityMinimum = GetRowRect(rect2, num);
                    golem_building.GolemComp.minEnergyPctForAbilities = Widgets.HorizontalSlider(rectAbilityMinimum, golem_building.GolemComp.minEnergyPctForAbilities, 0f, 1f, false, "TM_GolemAbilityMinimum".Translate() + " " + golem_building.GolemComp.minEnergyPctForAbilities.ToString("P2"), "0%", "100%", .01f);
                    TooltipHandler.TipRegion(rectAbilityMinimum, "TM_GolemAbilityMinimumDesc".Translate());
                    num += 2;

                    Rect rectRestMinimum = GetRowRect(rect2, num);
                    golem_building.GolemComp.energyPctShouldRest = Widgets.HorizontalSlider(rectRestMinimum, golem_building.GolemComp.energyPctShouldRest, 0f, 1f, false, "TM_GolemRestMinimum".Translate() + " " + golem_building.GolemComp.energyPctShouldRest.ToString("P2"), "0%", "100%", .01f);
                    TooltipHandler.TipRegion(rectRestMinimum, "TM_GolemRestMinimumDesc".Translate());
                    num += 2;

                    Rect rectAwakeMinimum = GetRowRect(rect2, num);
                    golem_building.GolemComp.energyPctShouldAwaken = Widgets.HorizontalSlider(rectAwakeMinimum, golem_building.GolemComp.energyPctShouldAwaken, .1f, 1f, false, "TM_GolemAwakenMinimum".Translate() + " " + TM_GolemUtility.ShouldAwkenString(golem_building.GolemComp.energyPctShouldAwaken), "10%", "100%", .01f);
                    TooltipHandler.TipRegion(rectAwakeMinimum, "TM_GolemAwakenMinimumDesc".Translate());
                    num += 2;                    
                }
                Rect rectUpgradeLabel = GetRowRect(rect2, num, 10);
                string upgrades = "TM_UpgradesVerbatum".Translate();
                Widgets.Label(rectUpgradeLabel, upgrades);
                Widgets.DrawLineHorizontal(rectUpgradeLabel.x, rectUpgradeLabel.yMax-10, rect2.width);
                num++;
                Text.Font = GameFont.Tiny;
                Rect rectUpgrades = GetRowRect(rect2, num);
                foreach (TM_GolemUpgrade gu in Upgrades)
                {
                    if (gu.currentLevel > 0 && gu.golemUpgradeDef.maxLevel > 0)
                    {
                        Rect rectZ = GetRowRect(rect2, num);
                        GUI.color = Color.white;
                        if (gu.currentLevel == gu.golemUpgradeDef.maxLevel)
                        {
                            GUI.color = Color.cyan;
                        }

                        upgrades = gu.golemUpgradeDef.label + ": " + gu.currentLevel + "/" + gu.golemUpgradeDef.maxLevel;
                        Widgets.Label(rectZ, upgrades);
                        TooltipHandler.TipRegion(rectZ, gu.golemUpgradeDef.description);
                        num++;
                    }                    
                }
                GUI.EndScrollView();
            }
        }

        public static Rect GetRowRect(Rect inRect, int row, float addedHeight = 0f)
        {
            float y = 18f * (float)row;
            Rect result = new Rect(inRect.x, y, inRect.width, 18f + addedHeight);
            return result;
        }
    }
}
