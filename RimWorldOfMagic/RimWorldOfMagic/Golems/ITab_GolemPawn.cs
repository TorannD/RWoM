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
    internal class ITab_GolemPawn : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(432f, 584f);
        private Vector2 scrollPosition = Vector2.zero;
        private string pawnMasterName = "None";
        private bool abilityOptions = false;
        private bool changeName = false;

        private static List<BodyPartDef> upgradesCoveringParts;
        private static List<BodyPartDef> UpgradesCoverParts
        {
            get
            {
                if(upgradesCoveringParts == null)
                {
                    upgradesCoveringParts = new List<BodyPartDef>();
                    upgradesCoveringParts.Clear();
                }
                return upgradesCoveringParts;
            }
        }

        private static List<TM_GolemUpgrade> Upgrades
        {
            get
            {
                Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
                if (singleSelectedThing != null && singleSelectedThing is TMPawnGolem)
                {
                    TMPawnGolem golem_pawn = singleSelectedThing as TMPawnGolem;
                    if(golem_pawn != null)
                    {
                        CompGolem cg = golem_pawn.TryGetComp<CompGolem>();
                        if (cg != null)
                        {
                            return cg.Upgrades;
                        }
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
                foreach(TM_GolemUpgrade gu in Upgrades)
                {
                    if(gu.currentLevel > 0)
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

        public ITab_GolemPawn()
        {
            this.size = ITab_GolemPawn.WinSize;
            this.labelKey = "TabGolem";
        }

        protected override void FillTab()
        {            
            TMPawnGolem golem_pawn  = Find.Selector.SingleSelectedThing as TMPawnGolem;
            if(golem_pawn != null && Upgrades != null)
            {
                if (golem_pawn.Golem.pawnMaster != null)
                {
                    pawnMasterName = golem_pawn.Golem.pawnMaster.Label;
                }
                else
                {
                    pawnMasterName = "None";
                }
                int num = 2;
                float scrollHeight = ActiveUpgradeCount > 9 ? 380 + ActiveUpgradeCount * 18 : 550;
                Rect canvas = new Rect(17f, 17f, ITab_GolemPawn.WinSize.x, ITab_GolemPawn.WinSize.y);
                Rect rect = new Rect(canvas.x, canvas.y, ITab_GolemPawn.WinSize.x - 34f, ITab_GolemPawn.WinSize.y - 34f);
                Rect sRect = new Rect(rect.x, rect.y, rect.width - 34, scrollHeight);
                scrollPosition = GUI.BeginScrollView(rect, scrollPosition, sRect, false, true);

                Rect rect2 = GetRowRect(rect, num, 10);
                rect2.width -= 34f;
                Text.Font = GameFont.Small;
                Rect rectNameLabel = new Rect(rect2.x, rect2.y, 100f, 28f);                
                Widgets.Label(rectNameLabel, "TM_GolemName".Translate());
                string tmpName = golem_pawn.LabelShortCap;
                Rect rectNameButton = new Rect(rect2.x + 110, rect2.y, 254f, rectNameLabel.height);
                changeName = Widgets.ButtonText(rectNameButton, tmpName, true, false, true);
                if (changeName)
                {
                    GolemNameWindow newWindow = new GolemNameWindow();
                    newWindow.cg = golem_pawn.Golem;
                    newWindow.golemName = golem_pawn.Name.ToStringShort;
                    Find.WindowStack.Add(newWindow);
                }

                //if (tmpName != "")
                //{
                //    golem_pawn.Name = NameTriple.FromString(tmpName);
                //}
                //else
                //{
                //    golem_pawn.Name = NameTriple.FromString("Blank");
                //}
                num +=2;
                Rect rectMasterLabel = GetRowRect(rectNameLabel, num, 10);
                Widgets.Label(rectMasterLabel, "TM_GolemMasterLabel".Translate());
                Rect rectPawnMaster = GetRowRect(rectNameButton, num, 10);
                GolemUtility.MasterButton(rectPawnMaster, pawnMasterName, golem_pawn.Golem);
                //if (Widgets.ButtonText(rectPawnMaster, pawnMasterName))
                //{
                //    List<string> tmpPawns = new List<string>();
                //    tmpPawns.Add("None");
                //    foreach (Pawn p in TM_Calc.GolemancersInFaction(golem_pawn.Faction).Where((Pawn x) => x != golem_pawn.Golem.pawnMaster))
                //    {
                //        tmpPawns.Add(p.LabelShort);
                //    }
                //    List<FloatMenuOption> list = new List<FloatMenuOption>();
                //    foreach (string pawnName in tmpPawns)
                //    {
                //        string text = pawnName;
                //        FloatMenuOption item = new FloatMenuOption(text, delegate
                //        {
                //            if (pawnName != pawnMasterName)
                //            {
                //                golem_pawn.Golem.pawnMaster = TM_Calc.GolemancersInFaction(golem_pawn.Faction).FirstOrDefault((Pawn p) => p.LabelShort == pawnName);
                //            }
                //            if (pawnName == "None")
                //            {
                //                golem_pawn.Golem.pawnMaster = null;
                //            }
                //        });
                //        list.Add(item);
                //    }
                //    Find.WindowStack.Add(new FloatMenu(list));
                //}
                //TooltipHandler.TipRegion(rectPawnMaster, "TM_GolemMasterDesc".Translate());
                num += 2;
                abilityOptions = Widgets.ButtonText(GetRowRect(rect2, num, 10), "TM_GolemAbilitiesButton".Translate(), true, false, true);
                if (abilityOptions)
                {
                    GolemAbilitiesWindow newWindow = new GolemAbilitiesWindow();
                    newWindow.cg = golem_pawn.Golem;
                    Find.WindowStack.Add(newWindow);
                }
                num += 2;

                Rect rectFollowMaster = GetRowRect(rect2, num);
                rectFollowMaster.width = rect2.width / 2.2f;
                Widgets.CheckboxLabeled(rectFollowMaster, "TM_GolemFollowsMaster".Translate(), ref golem_pawn.Golem.followsMaster, false);
                TooltipHandler.TipRegion(rectFollowMaster, "TM_GolemFollowsMasterDesc".Translate());
                Rect rectFollowMasterDrafted = rectFollowMaster;
                rectFollowMasterDrafted.x += rectFollowMasterDrafted.width + 34f;
                Widgets.CheckboxLabeled(rectFollowMasterDrafted, "TM_GolemDraftedFollow".Translate(), ref golem_pawn.Golem.followsMasterDrafted, false);
                TooltipHandler.TipRegion(rectFollowMasterDrafted, "TM_GolemDraftedFollowDesc".Translate());
                num += 2;
                Rect rectShowDormantPos = GetRowRect(rect2, num);
                rectShowDormantPos.width = rect2.width / 2.2f;
                Widgets.CheckboxLabeled(rectShowDormantPos, "TM_GolemShowDormant".Translate(), ref golem_pawn.showDormantPosition, false);
                TooltipHandler.TipRegion(rectShowDormantPos, "TM_GolemShowDormantDesc".Translate());
                Rect rectRestrictedAggression = rectShowDormantPos;
                rectRestrictedAggression.x += rectRestrictedAggression.width + 34f;
                Widgets.CheckboxLabeled(rectRestrictedAggression, "TM_GolemRestrictedAggression".Translate(), ref golem_pawn.Golem.checkThreatPath, false);
                TooltipHandler.TipRegion(rectRestrictedAggression, "TM_GolemRestrictedAggressionDesc".Translate());
                num += 2;

                Rect rectStayDormant = GetRowRect(rect2, num);
                Widgets.CheckboxLabeled(rectStayDormant, "TM_GolemDormantWhenUpgrading".Translate(), ref golem_pawn.Golem.remainDormantWhenUpgrading, false);
                TooltipHandler.TipRegion(rectStayDormant, "TM_GolemDormantWhenUpgradingDesc".Translate());
                num += 2;
                Rect rectUseAbilitiesDormant = GetRowRect(rect2, num);
                Widgets.CheckboxLabeled(rectUseAbilitiesDormant, "TM_GolemUseAbilitiesWhileDormant".Translate(), ref golem_pawn.Golem.useAbilitiesWhenDormant, false);
                TooltipHandler.TipRegion(rectUseAbilitiesDormant, "TM_GolemUseAbilitiesWhileDormantDesc".Translate());
                num += 2;

                Rect rectThreatRange = GetRowRect(rect2, num);
                golem_pawn.Golem.threatRange = Widgets.HorizontalSlider(rectThreatRange, golem_pawn.Golem.threatRange, 0f, 100f, false, "TM_GolemThreatRange".Translate() + " " + golem_pawn.Golem.threatRange.ToString("N"), "0", "100", 1f);
                TooltipHandler.TipRegion(rectThreatRange, "TM_GolemThreatRangeDesc".Translate());
                num += 2;

                Rect rectAbilityMinimum = GetRowRect(rect2, num);
                golem_pawn.Golem.minEnergyPctForAbilities = Widgets.HorizontalSlider(rectAbilityMinimum, golem_pawn.Golem.minEnergyPctForAbilities, 0f, 1f, false, "TM_GolemAbilityMinimum".Translate() + " " + golem_pawn.Golem.minEnergyPctForAbilities.ToString("P2"), "0%", "100%", .01f);
                TooltipHandler.TipRegion(rectAbilityMinimum, "TM_GolemAbilityMinimumDesc".Translate());
                num += 2;

                Rect rectRestMinimum = GetRowRect(rect2, num);
                golem_pawn.Golem.energyPctShouldRest = Widgets.HorizontalSlider(rectRestMinimum, golem_pawn.Golem.energyPctShouldRest, 0f, 1f, false, "TM_GolemRestMinimum".Translate() + " " + golem_pawn.Golem.energyPctShouldRest.ToString("P2"), "0%", "100%", .01f);
                TooltipHandler.TipRegion(rectRestMinimum, "TM_GolemRestMinimumDesc".Translate());
                num += 2;

                Rect rectAwakeMinimum = GetRowRect(rect2, num);
                golem_pawn.Golem.energyPctShouldAwaken = Widgets.HorizontalSlider(rectAwakeMinimum, golem_pawn.Golem.energyPctShouldAwaken, .1f, 1f, false, "TM_GolemAwakenMinimum".Translate() + " " + TM_GolemUtility.ShouldAwkenString(golem_pawn.Golem.energyPctShouldAwaken), "10%", "100%", .01f);
                TooltipHandler.TipRegion(rectAwakeMinimum, "TM_GolemAwakenMinimumDesc".Translate());
                num += 2;

                Rect rectUpgradeLabel = GetRowRect(rect2, num, 10);
                string upgrades = "TM_UpgradesVerbatum".Translate();
                Widgets.Label(rectUpgradeLabel, upgrades);
                Widgets.DrawLineHorizontal(rectUpgradeLabel.x, rectUpgradeLabel.yMax-10, rect.width);
                num++;                
                Text.Font = GameFont.Tiny;
                Rect rectUpgrades = GetRowRect(rect2, num);                
                foreach(TM_GolemUpgrade gu in Upgrades)
                {                    
                    if (gu.currentLevel > 0 && gu.golemUpgradeDef.maxLevel > 0)
                    {
                        Rect rectZ = GetRowRect(rect2, num);
                        GUI.color = Color.white;
                        if (gu.currentLevel == gu.golemUpgradeDef.maxLevel)
                        {
                            GUI.color = Color.cyan;
                        }
                        if(!gu.enabled)
                        {
                            GUI.color = Color.red;
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
