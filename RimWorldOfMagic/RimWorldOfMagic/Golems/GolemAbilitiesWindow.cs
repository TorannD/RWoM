using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using Verse;
using RimWorld;
using System.Diagnostics;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemAbilitiesWindow : Window
    {

        public override Vector2 InitialSize => new Vector2(480f, 640f);
        public static float HeaderSize = 28f;
        public static float TextSize = 22f;

        private bool reset = false;
        private bool challenge = false;

        public Vector2 scrollPosition = Vector2.zero;
        public CompGolem cg = null;
        List<TM_GolemUpgrade> upgrades = new List<TM_GolemUpgrade>();
        List<TM_GolemDef.GolemWorkTypes> workTypes = new List<TM_GolemDef.GolemWorkTypes>();


        public GolemAbilitiesWindow()
        {
            base.closeOnCancel = true;
            base.doCloseButton = true;
            base.doCloseX = true;
            base.absorbInputAroundWindow = true;
            base.forcePause = true;
        }

        public override void Close(bool doCloseSound = true)
        {
            try
            {
                if (upgrades != null && upgrades.Count > 0)
                {
                    foreach (TM_GolemUpgrade gu in upgrades)
                    {
                        if (gu.golemUpgradeDef.defName == "TM_Golem_HollowPGIce")
                        {
                            TMHollowGolem hg = cg.PawnGolem as TMHollowGolem;
                            if (hg != null)
                            {
                                hg.hasSlipStreamUpgrade = gu.enabled;
                            }
                        }
                        else if (gu.golemUpgradeDef.defName == "TM_Golem_HollowPGDeath")
                        {
                            TMHollowGolem hg = cg.PawnGolem as TMHollowGolem;
                            if (hg != null)
                            {
                                hg.hasDeathCloakUpgrade = gu.enabled;
                            }
                        }
                        else if (gu.golemUpgradeDef.defName == "TM_Golem_HollowPGDoom")
                        {
                            TMHollowGolem hg = cg.PawnGolem as TMHollowGolem;
                            if (hg != null)
                            {
                                hg.doomFieldHediffEnabled = gu.enabled;
                            }
                        }
                        else if (gu.golemUpgradeDef.defName == "TM_Golem_HollowFGDeath")
                        {
                            TMHollowGolem hg = cg.PawnGolem as TMHollowGolem;
                            if (hg != null)
                            {
                                hg.deathFieldHediffEnabled = gu.enabled;
                            }
                        }
                    }
                    if (cg != null && cg.PawnGolem != null && cg.PawnGolem.Spawned)
                    {
                        cg.PawnGolem.verbCommands.Clear();
                        cg.PawnGolem.ValidRangedVerbs(true);
                    }
                }
                cg.PawnGolem.SetGolemWorkPrioritiesAndSkills();
            }
            catch(NullReferenceException ex)
            {
                base.Close(doCloseSound);
            }
            base.Close(doCloseSound);
        }

        public override void DoWindowContents(Rect inRect)
        {                       
            upgrades.Clear();
            foreach(TM_GolemUpgrade gu in cg.Upgrades)
            {
                if(gu.currentLevel > 0)
                {
                    if(gu.golemUpgradeDef.workstationEffects != null && gu.golemUpgradeDef.workstationEffects.Count > 0)
                    {
                        upgrades.Add(gu);                        
                    }
                    else if(gu.golemUpgradeDef.ability != null )
                    {
                        if (gu.golemUpgradeDef.ability.effects != null && gu.golemUpgradeDef.ability.effects.Count > 0)
                        {
                            upgrades.Add(gu);
                        }
                        else if(gu.golemUpgradeDef.ability.jobEffect != null && gu.golemUpgradeDef.ability.jobEffect.Count > 0)
                        {
                            upgrades.Add(gu);
                        }
                    }
                    else if(gu.golemUpgradeDef.verbProjectile != null)
                    {
                        upgrades.Add(gu);
                    }
                    else if(gu.golemUpgradeDef.abilityToggle)
                    {
                        upgrades.Add(gu);
                    }
                }
            }
            workTypes.Clear();
            if (cg.Golem.golemDef.golemWorkTypes != null)
            {
                foreach (TMDefs.TM_GolemDef.GolemWorkTypes gwt in cg.Golem.golemDef.golemWorkTypes)
                {
                    if (!gwt.requiresUpgrade || cg.Golem.upgrades.Any((TM_GolemUpgrade y) => y.golemUpgradeDef == gwt.golemUpgradeDef && y.currentLevel > 0))
                    {
                        workTypes.Add(gwt);
                    }
                }
            }
            int num = 0;
            float rowHeight = 28f;
            //GUI.BeginGroup(inRect);
            int scrollCount = 256;
            if((workTypes.Count + upgrades.Count) > 8)
            {
                scrollCount = upgrades.Count * 40;
            }
            Rect sRect = new Rect(inRect.x, inRect.y, inRect.width - 36f, inRect.height + scrollCount);
            scrollPosition = GUI.BeginScrollView(inRect, scrollPosition, sRect, false, true);

            Text.Font = GameFont.Medium;
            float x = Text.CalcSize("TM_GolemAbilities".Translate(cg.PawnGolem.LabelShortCap)).x;
            Rect headerRect = new Rect(inRect.width / 2f - (x / 2), inRect.y, inRect.width, GolemAbilitiesWindow.HeaderSize);
            Widgets.Label(headerRect, "TM_GolemAbilities".Translate(cg.PawnGolem.LabelShortCap));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect rect1 = new Rect(inRect);
            rect1.width /= 2.2f;
            num+=2;           

            for (int i = 0; i < upgrades.Count; i++)
            {
                Rect upgradeRect = GetRowRect(rect1, num);
                Widgets.CheckboxLabeled(upgradeRect, upgrades[i].golemUpgradeDef.label, ref upgrades[i].enabled, false);
                TooltipHandler.TipRegion(upgradeRect, "TM_GolemAbilityEnabledDesc".Translate(upgrades[i].golemUpgradeDef.label, upgrades[i].golemUpgradeDef.description));
                num++;                
            }
            if (cg.PawnGolem?.skills != null)
            {
                for (int i = 0; i < workTypes.Count; i++)
                {
                    if (workTypes[i].workTypeDef.relevantSkills?.Count > 0)
                    {
                        Rect upgradeRect = GetRowRect(rect1, num);
                        Widgets.CheckboxLabeled(upgradeRect, workTypes[i].workTypeDef.pawnLabel + " [" + cg.PawnGolem.skills.GetSkill(workTypes[i].upgradedSkill).Level + "]", ref workTypes[i].enabled, false);
                        TooltipHandler.TipRegion(upgradeRect, "TM_GolemWorkTypeEnabledDesc".Translate(workTypes[i].workTypeDef.pawnLabel));
                        num++;
                    }
                }
            }
            num++;
            Rect rowRect99 = GetRowRect(rect1, num);
            rowRect99.width = 100f;
            reset = Widgets.ButtonText(rowRect99, "Enable All", true, false, true);
            if (reset)
            {
                foreach(TM_GolemUpgrade gu in upgrades)
                {
                    gu.enabled = true;
                }
                foreach (TM_GolemDef.GolemWorkTypes gwt in workTypes)
                {
                    gwt.enabled = true;
                }
            }
            GUI.EndScrollView();
        }

        public static Rect GetRowRect(Rect inRect, int row, float addedHeight = 0f)
        {
            float y = 24f * (float)row;
            Rect result = new Rect(inRect.x, y, inRect.width,  24f + addedHeight);
            return result;
        }
    }   
}
