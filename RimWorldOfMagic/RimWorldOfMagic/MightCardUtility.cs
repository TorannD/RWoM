using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace TorannMagic
{
    class MightCardUtility //original code by Jecrell
    {

        public static Vector2 mightCardSize = default(Vector2); // new Vector2(700f, 556f);
        public static Vector2 MightCardSize
        {
            get
            {
                if (mightCardSize == default(Vector2))
                {
                    mightCardSize = new Vector2(700f, 556f);
                    if (Screen.currentResolution.height <= 800)
                    {
                        mightCardSize = new Vector2(700f, 476f);
                    }
                }
                return mightCardSize;
            }
        }

        public static Vector2 scrollPosition = Vector2.zero;

        public static float ButtonSize = 40f;

        public static float MagicButtonSize = 46f;

        public static float MagicButtonPointSize = 24f;

        public static float HeaderSize = 24f;

        public static float TextSize = 22f;

        public static float Padding = 3f;

        public static float SpacingOffset = 22f;

        public static float SectionOffset = 8f;

        public static float ColumnSize = 245f;

        public static float SkillsColumnHeight = 113f;

        public static float SkillsColumnDivider = 114f;

        public static float SkillsTextWidth = 138f;

        public static float SkillsBoxSize = 18f;

        public static float PowersColumnHeight = 195f;

        public static float PowersColumnWidth = 123f;

        public static bool adjustedForLanguage = false;

        public static List<TMAbilityDef> GetAbilityList(CompAbilityUserMight comp, List<MightPower> classList = null)
        {
            List<TMAbilityDef> tmpList = new List<TMAbilityDef>();
            tmpList.Clear();
            if (classList != null)
            {
                foreach (MightPower p in classList)
                {
                    foreach (TMAbilityDef t in p.TMabilityDefs)
                    {
                        tmpList.Add(t);
                    }
                }
            }
            if (comp.customClass != null)
            {
                foreach (TMAbilityDef a in comp.customClass.classFighterAbilities)
                {
                    tmpList.Add(a);
                }
            }
            if (comp.AdvancedClasses != null && comp.AdvancedClasses.Count > 0)
            {
                foreach (TMDefs.TM_CustomClass cc in comp.AdvancedClasses)
                {
                    foreach (TMAbilityDef ab in cc.classFighterAbilities)
                    {
                        tmpList.Add(ab);
                    }
                }
            }
            return tmpList;
        }

        public static void DrawMightCard(Rect rect, Pawn pawn)
        {
            //GUI.BeginGroup(rect);
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            int sizeY = 0;
            if (comp.customClass != null || comp.AdvancedClasses.Count > 0)
            {
                List<TMDefs.TM_CustomClass> ccList = new List<TMDefs.TM_CustomClass>();
                ccList.Clear();
                int defaultCount = 0;
                if (comp.customClass != null)
                {
                    ccList.Add(comp.customClass);
                }
                else
                {
                    defaultCount = 5;
                }
                if (comp.AdvancedClasses.Count > 0)
                {
                    foreach (TMDefs.TM_CustomClass cc in comp.AdvancedClasses)
                    {
                        ccList.Add(cc);
                    }
                }
                sizeY = ((comp.MightData.GetUniquePowersWithSkillsCount(ccList) + defaultCount) * 80);
                if (sizeY > 500)
                {
                    sizeY -= 500;
                }
                else
                {
                    sizeY = 0;
                }
            }
            Rect sRect = new Rect(rect.x, rect.y, rect.width - 36f, rect.height + 56f + sizeY);
            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, sRect, false, true);
                        
            bool flag = comp != null && comp.MightData != null;
            if (flag)
            {
                float x = Text.CalcSize("TM_HeaderMight".Translate()).x;
                Rect rect2 = new Rect(rect.width / 2f - (x/2) , rect.y, rect.width, MightCardUtility.HeaderSize); //+ MightCardUtility.SpacingOffset
                Text.Font = GameFont.Small;
                Widgets.Label(rect2, "TM_HeaderMight".Translate().CapitalizeFirst());
                Text.Font = GameFont.Small;
                Widgets.DrawLineHorizontal(rect.x - 10f, rect2.yMax, rect.width - 15f);
                Rect rect9 = new Rect(rect.x, rect2.yMax + MightCardUtility.Padding, rect2.width, MightCardUtility.SkillsColumnHeight);
                Rect inRect = new Rect(rect9.x, rect9.y + MightCardUtility.Padding, MightCardUtility.SkillsColumnDivider, MightCardUtility.SkillsColumnHeight);
                Rect inRect2 = new Rect(rect9.x + MightCardUtility.SkillsColumnDivider, rect9.y + MightCardUtility.Padding, rect9.width - MightCardUtility.SkillsColumnDivider, MightCardUtility.SkillsColumnHeight);
                MightCardUtility.InfoPane(inRect, pawn.GetCompAbilityUserMight(), pawn);
                float x5 = Text.CalcSize("TM_Skills".Translate()).x;
                Rect rect10 = new Rect(rect.width / 2f - x5 / 2f, rect9.yMax - 60f, rect.width, MightCardUtility.HeaderSize);
                Text.Font = GameFont.Small;
                Widgets.Label(rect10, "TM_Skills".Translate().CapitalizeFirst());
                Text.Font = GameFont.Small;
                Widgets.DrawLineHorizontal(rect.x - 10f, rect10.yMax, rect.width - 15f);
                Rect rect11 = new Rect(rect.x, rect10.yMax + MightCardUtility.SectionOffset, rect10.width, MightCardUtility.PowersColumnHeight);
                if (comp.customClass != null)
                {
                    Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                    MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp), TexButton.TMTex_SkillPointUsed);
                }
                else
                {
                    if (pawn.story.traits.HasTrait(TorannMagicDefOf.Gladiator))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersG), TexButton.TMTex_SkillPointUsed);
                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersG, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Sprint, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Fortitude, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Grapple, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Cleave, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Whirlwind, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Sniper))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersS), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersS, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_SniperFocus, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Headshot, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_DisablingShot, comp.MightData.MightPowerSkill_AntiArmor, comp.MightData.MightPowerSkill_ShadowSlayer, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Bladedancer))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersB), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersB, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_BladeFocus, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_BladeArt, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_SeismicSlash, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_BladeSpin, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PhaseStrike, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Ranger))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersR), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersR, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_RangerTraining, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_BowTraining, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PoisonTrap, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_AnimalFriend, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_ArrowStorm, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersF), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersF, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Disguise, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Reversal, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Transpose, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Possess, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersP), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersP, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicBarrier, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicBlast, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicDash, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicStorm, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersDK), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersDK, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Shroud, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_WaveOfFear, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Spite, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_LifeSteal, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_GraveBlade, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Monk))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersM), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersM, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Chi, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_MindOverBody, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Meditate, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_TigerStrike, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_DragonStrike, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_ThunderStrike, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersW), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersW, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_WayfarerCraft, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining, null, null, null, null, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Commander))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp, comp.MightData.MightPowersC), TexButton.TMTex_SkillPointUsed);

                        //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersC, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_ProvisionerAura, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_TaskMasterAura, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_CommanderAura, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_StayAlert, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_MoveOut, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_HoldTheLine, TexButton.TMTex_SkillPointUsed);
                    }
                    else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);

                        if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower mp) => mp.abilityDef == TorannMagicDefOf.TM_PistolSpec).learned == true)
                        {
                            List<TMAbilityDef> ssList = GetAbilityList(comp, comp.MightData.MightPowersSS);
                            ssList.Remove(TorannMagicDefOf.TM_RifleSpec);
                            ssList.Remove(TorannMagicDefOf.TM_ShotgunSpec);
                            MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, ssList, TexButton.TMTex_SkillPointUsed);

                            //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersSS, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PistolSpec, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_CQC, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_FirstAid, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_60mmMortar, null, null, TexButton.TMTex_SkillPointUsed);
                        }
                        else if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower mp) => mp.abilityDef == TorannMagicDefOf.TM_RifleSpec).learned == true)
                        {
                            List<TMAbilityDef> ssList = GetAbilityList(comp, comp.MightData.MightPowersSS);
                            ssList.Remove(TorannMagicDefOf.TM_PistolSpec);
                            ssList.Remove(TorannMagicDefOf.TM_ShotgunSpec);
                            MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, ssList, TexButton.TMTex_SkillPointUsed);
                            //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersSS, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_RifleSpec, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_CQC, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_FirstAid, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_60mmMortar, null, null, TexButton.TMTex_SkillPointUsed);
                        }
                        else if (comp.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower mp) => mp.abilityDef == TorannMagicDefOf.TM_ShotgunSpec).learned == true)
                        {
                            List<TMAbilityDef> ssList = GetAbilityList(comp, comp.MightData.MightPowersSS);
                            ssList.Remove(TorannMagicDefOf.TM_RifleSpec);
                            ssList.Remove(TorannMagicDefOf.TM_PistolSpec);
                            MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, ssList, TexButton.TMTex_SkillPointUsed);
                            // MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersSS, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_ShotgunSpec, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_CQC, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_FirstAid, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_60mmMortar, null, null, TexButton.TMTex_SkillPointUsed);
                        }
                        else
                        {
                            List<TMAbilityDef> ssList = GetAbilityList(comp, comp.MightData.MightPowersSS);
                            ssList.Remove(TorannMagicDefOf.TM_CQC);
                            ssList.Remove(TorannMagicDefOf.TM_FirstAid);
                            ssList.Remove(TorannMagicDefOf.TM_60mmMortar);
                            MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, ssList, TexButton.TMTex_SkillPointUsed);
                            //MightCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMight(), pawn.GetCompAbilityUserMight().MightData.MightPowersSS, null, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PistolSpec, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_RifleSpec, pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_ShotgunSpec, null, null, TexButton.TMTex_SkillPointUsed);
                        }
                    }
                    else if (comp.AdvancedClasses.Count > 0)
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MightCardUtility.PowersColumnWidth, MightCardUtility.PowersColumnHeight);
                        MightCardUtility.CustomPowersHandler(inRect3, comp, comp.MightData.AllMightPowersWithSkills, GetAbilityList(comp), TexButton.TMTex_SkillPointUsed);
                    }
                }
                
            }
            //GUI.EndGroup();
            GUI.EndScrollView();
        }

        public static void InfoPane(Rect inRect, CompAbilityUserMight compMight, Pawn pawn)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width * 0.7f, MightCardUtility.TextSize);
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, "TM_Level".Translate().CapitalizeFirst() + ": " + compMight.MightUserLevel.ToString());
            Text.Font = GameFont.Tiny;
            bool godMode = DebugSettings.godMode;
            if (godMode)
            {
                Rect rect2 = new Rect(rect.xMax, inRect.y, inRect.width * 0.3f, MightCardUtility.TextSize);
                bool flag = Widgets.ButtonText(rect2, "+", true, false, true);
                if (flag)
                {
                    compMight.LevelUp(true);
                }

                Rect rect22 = new Rect(rect.xMax + 60f, inRect.y, 50f, MightCardUtility.TextSize * 2);
                bool flag22 = Widgets.ButtonText(rect22, "Reset Class", true, false, true);
                if (flag22)
                {
                    compMight.ResetSkills();
                }
                //Rect rect23 = new Rect(rect.xMax + 115f, inRect.y, 50f, MightCardUtility.TextSize * 2);
                //bool flag23 = Widgets.ButtonText(rect23, "Remove Powers", true, false, true);
                //if (flag23)
                //{
                //    compMight.RemoveAbilityUser();
                //}
                
            }
            Rect rect4 = new Rect(inRect.x, rect.yMax, inRect.width, MightCardUtility.TextSize);
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect4, "TM_PointsAvail".Translate() + ": " + compMight.MightData.MightAbilityPoints);
            Text.Font = GameFont.Tiny;
            if (!godMode)
            {
                Rect rect6 = new Rect(rect4.xMax + 10f, rect.yMax, inRect.width + 100f, MagicCardUtility.TextSize);
                Widgets.Label(rect6, "TM_LastStaminaGainPct".Translate(
                    (compMight.Stamina.lastGainPct * 200).ToString("0.000")
                    ));
                string str1 = "Base gain: " + (200 * compMight.Stamina.baseStaminaGain).ToString("0.000") + "\nStamina regen adjustment: " + (200 * compMight.Stamina.modifiedStaminaGain).ToString("0.000");
                TooltipHandler.TipRegion(rect6, () => string.Concat(new string[]
                        {
                        str1,                        
                        }), 398552);
                GUI.color = Color.white;
            }
            Rect rect5 = new Rect(rect4.x, rect4.yMax + 3f, inRect.width + 100f, MightCardUtility.HeaderSize * 0.6f);
            MightCardUtility.DrawLevelBar(rect5, compMight, pawn, inRect);

        }

        public static void DrawLevelBar(Rect rect, CompAbilityUserMight compMight, Pawn pawn, Rect rectG)
        {
            bool flag = rect.height > 70f;
            if (flag)
            {
                float num = (rect.height - 70f) / 2f;
                rect.height = 70f;
                rect.y += num;
            }
            bool flag2 = Mouse.IsOver(rect);
            if (flag2)
            {
                Widgets.DrawHighlight(rect);
            }
            TooltipHandler.TipRegion(rect, new TipSignal(() => MightCardUtility.MightXPTipString(compMight), rect.GetHashCode()));
            float num2 = 14f;
            bool flag3 = rect.height < 50f;
            if (flag3)
            {
                num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect2 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height);
            rect2 = new Rect(rect2.x, rect2.y, rect2.width, rect2.height - num2);
            Widgets.FillableBar(rect2, compMight.XPTillNextLevelPercent, (Texture2D)AccessTools.Field(typeof(Widgets), "BarFullTexHor").GetValue(null), BaseContent.GreyTex, false);
            //Rect rect3 = new Rect(rect2.x + (rectG.x/4)+ MightCardUtility.MagicButtonPointSize, rect2.yMin + 24f, 136f, MightCardUtility.TextSize);
            // Rect rect31 = new Rect(rect2.x + (rectG.x / 4), rect2.yMin + 24f, MightCardUtility.MagicButtonPointSize, MightCardUtility.TextSize);
            Rect rect3 = new Rect(rect2.x + 272f + MightCardUtility.MagicButtonPointSize, rectG.y, 136f, MightCardUtility.TextSize);
            Rect rect31 = new Rect(rect2.x + 272f, rectG.y, MightCardUtility.MagicButtonPointSize, MightCardUtility.TextSize);
            Rect rect4 = new Rect(rect3.x + rect3.width + (MightCardUtility.MagicButtonPointSize * 2), rectG.y, 136f, MightCardUtility.TextSize); //rect2.yMin + 24f
            Rect rect41 = new Rect(rect3.x + rect3.width + MightCardUtility.MagicButtonPointSize, rectG.y, MightCardUtility.MagicButtonPointSize, MightCardUtility.TextSize);
            Rect rect5 = new Rect(rect2.x + 272f + MightCardUtility.MagicButtonPointSize, rectG.yMin + 24f, 136f, MightCardUtility.TextSize);
            Rect rect51 = new Rect(rect2.x + 272f, rectG.yMin + 24f, MightCardUtility.MagicButtonPointSize, MightCardUtility.TextSize);
            Rect rect6 = new Rect(rect5.x + rect5.width + (MightCardUtility.MagicButtonPointSize * 2), rectG.y + 24f, 136f, MightCardUtility.TextSize); //rect2.yMin + 24f
            Rect rect61 = new Rect(rect5.x + rect5.width + MightCardUtility.MagicButtonPointSize, rectG.y + 24f, MightCardUtility.MagicButtonPointSize, MightCardUtility.TextSize);


            List<MightPowerSkill> skill1 = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_global_refresh;
            List<MightPowerSkill> skill2 = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_global_seff;
            List<MightPowerSkill> skill3 = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_global_strength;
            List<MightPowerSkill> skill4 = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_global_endurance;

            using (List<MightPowerSkill>.Enumerator enumerator1 = skill1.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    MightPowerSkill skill = enumerator1.Current;
                    TooltipHandler.TipRegion(rect3, new TipSignal(() => skill.desc.Translate(), rect3.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMight.MightData.MightAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect3, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect31, "+", true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect3, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_refresh_pwr")
                            {
                                //compMight.LevelUpSkill_global_refresh(skill.label);
                                skill.level++;
                                compMight.MightData.MightAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
            using (List<MightPowerSkill>.Enumerator enumerator2 = skill2.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    MightPowerSkill skill = enumerator2.Current;
                    TooltipHandler.TipRegion(rect4, new TipSignal(() => skill.desc.Translate(), rect4.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMight.MightData.MightAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect41, "+", true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_seff_pwr")
                            {
                                //compMight.LevelUpSkill_global_seff(skill.label);
                                skill.level++;
                                compMight.MightData.MightAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
            using (List<MightPowerSkill>.Enumerator enumerator3 = skill3.GetEnumerator())
            {
                while (enumerator3.MoveNext())
                {
                    MightPowerSkill skill = enumerator3.Current;
                    TooltipHandler.TipRegion(rect5, new TipSignal(() => skill.desc.Translate(), rect5.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMight.MightData.MightAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect5, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect51, "+", true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect5, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_strength_pwr")
                            {
                                //compMight.LevelUpSkill_global_strength(skill.label);
                                skill.level++;
                                compMight.MightData.MightAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
            using (List<MightPowerSkill>.Enumerator enumerator4 = skill4.GetEnumerator())
            {
                while (enumerator4.MoveNext())
                {
                    MightPowerSkill skill = enumerator4.Current;
                    TooltipHandler.TipRegion(rect6, new TipSignal(() => skill.desc.Translate(), rect6.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMight.MightData.MightAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect6, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect61, "+", true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect6, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_endurance_pwr")
                            {
                                //compMight.LevelUpSkill_global_endurance(skill.label);
                                skill.level++;
                                compMight.MightData.MightAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
        }


        public static string MightXPTipString(CompAbilityUserMight compMight)
        {
            string result;

            result = string.Concat(new string[]
            {
                compMight.MightUserXP.ToString(),
                " / ",
                compMight.MightUserXPTillNextLevel.ToString(),
                "\n",
                "TM_MightXPDesc".Translate()
            });

            return result;
        }

        //PowerGUIHandler no longer used TODO:REMOVE
//        public static void PowersGUIHandler(Rect inRect, CompAbilityUserMight compMight, List<MightPower> MightPowers, List<MightPowerSkill> MightPowerSkill1, List<MightPowerSkill> MightPowerSkill2, List<MightPowerSkill> MightPowerSkill3, List<MightPowerSkill> MightPowerSkill4, List<MightPowerSkill> MightPowerSkill5, List<MightPowerSkill> MightPowerSkill6, Texture2D pointTexture)
//        {
//            float num = inRect.y;
//            int itnum = 1;
//            bool flag999;
//            bool flag998;
//            using (List<MightPower>.Enumerator enumerator = MightPowers.GetEnumerator())
//            {
//                EnumerationStart:;
//                while (enumerator.MoveNext())
//                {
//                    MightPower power = enumerator.Current;

//                    if (MightPowerSkill1 == null)
//                    {
//                        if (power.abilityDef == TorannMagicDefOf.TM_CQC || power.abilityDef == TorannMagicDefOf.TM_FirstAid || power.abilityDef == TorannMagicDefOf.TM_60mmMortar)
//                        {
//                            goto EnumerationStart;
//                        }
//                    }                    
//                    else
//                    {
//                        if (!power.learned && (power.abilityDef == TorannMagicDefOf.TM_PistolSpec || power.abilityDef == TorannMagicDefOf.TM_RifleSpec || power.abilityDef == TorannMagicDefOf.TM_ShotgunSpec))
//                        {
//                            goto EnumerationStart;
//                        }                        
//                    }
//                    if (power.abilityDef == TorannMagicDefOf.TM_PistolWhip || power.abilityDef == TorannMagicDefOf.TM_SuppressingFire || power.abilityDef == TorannMagicDefOf.TM_Mk203GL || power.abilityDef == TorannMagicDefOf.TM_Buckshot || power.abilityDef == TorannMagicDefOf.TM_BreachingCharge)
//                    {
//                        goto EnumerationStart;
//                    }

//                    Text.Font = GameFont.Small;
//                    Rect rect = new Rect(MightCardUtility.MightCardSize.x / 2f - MightCardUtility.MagicButtonSize, num, MightCardUtility.MagicButtonSize, MightCardUtility.MagicButtonSize);
//                    if (itnum > 1)
//                    {
//                        Widgets.DrawLineHorizontal(0f + 20f, rect.y - 2f, 700f - 40f);
//                    }
//                    //power.abilityDef == TorannMagicDefOf.TM_Sprint || power.abilityDef == TorannMagicDefOf.TM_Sprint_I || power.abilityDef == TorannMagicDefOf.TM_Sprint_II ||
//                    if (power.level < 3 && (power.abilityDef == TorannMagicDefOf.TM_Grapple || power.abilityDef == TorannMagicDefOf.TM_Grapple_I || power.abilityDef == TorannMagicDefOf.TM_Grapple_II || 
//                        power.abilityDef == TorannMagicDefOf.TM_DisablingShot || power.abilityDef == TorannMagicDefOf.TM_DisablingShot_I || power.abilityDef == TorannMagicDefOf.TM_DisablingShot_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_PhaseStrike || power.abilityDef == TorannMagicDefOf.TM_PhaseStrike_I || power.abilityDef == TorannMagicDefOf.TM_PhaseStrike_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_ArrowStorm || power.abilityDef == TorannMagicDefOf.TM_ArrowStorm_I || power.abilityDef == TorannMagicDefOf.TM_ArrowStorm_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_PsionicBlast || power.abilityDef == TorannMagicDefOf.TM_PsionicBlast_I || power.abilityDef == TorannMagicDefOf.TM_PsionicBlast_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_GraveBlade || power.abilityDef == TorannMagicDefOf.TM_GraveBlade_I || power.abilityDef == TorannMagicDefOf.TM_GraveBlade_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_Spite || power.abilityDef == TorannMagicDefOf.TM_Spite_I || power.abilityDef == TorannMagicDefOf.TM_Spite_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_StayAlert || power.abilityDef == TorannMagicDefOf.TM_StayAlert_I || power.abilityDef == TorannMagicDefOf.TM_StayAlert_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_MoveOut || power.abilityDef == TorannMagicDefOf.TM_MoveOut_I || power.abilityDef == TorannMagicDefOf.TM_MoveOut_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_HoldTheLine || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_I || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_II ||
//                        power.abilityDef == TorannMagicDefOf.TM_Transpose || power.abilityDef == TorannMagicDefOf.TM_Transpose_I || power.abilityDef == TorannMagicDefOf.TM_Transpose_II))

//                    {
//                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
//                        {
//                            power.abilityDef.label,
//                            "\n\nCurrent Level:\n",
//                            power.abilityDescDef.description,
//                            "\n\nNext Level:\n",
//                            power.nextLevelAbilityDescDef?.description,
//                            "\n\n",                            
//                            MightAbility.PostAbilityDesc((TMAbilityDef)power.abilityDef, compMight, 0),
//                            "\n",
//                            "TM_CheckPointsForMoreInfo".Translate()
//                       }), 398462);
//                    }
//                    else if(power.level < 1 && power.abilityDef == TorannMagicDefOf.TM_PsionicBarrier)  
//                    {
//                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
//                        {
//                            power.abilityDef.label,
//                            "\n\nCurrent Level:\n",
//                            power.abilityDescDef.description,
//                            "\n\nNext Level:\n",
//                            power.nextLevelAbilityDescDef?.description,
//                            "\n\n",
//                            MightAbility.PostAbilityDesc((TMAbilityDef)power.abilityDef, compMight, 0),
//                            "\n",
//                            "TM_CheckPointsForMoreInfo".Translate()
//                       }), 398462);
//                    }
//                    else
//                    {
//                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
//{
//                            power.abilityDef.label,
//                            "\n\n",
//                            power.abilityDescDef.description,
//                            "\n\n",
//                            MightAbility.PostAbilityDesc((TMAbilityDef)power.abilityDef, compMight, 0),
//                            "\n",
//                            "TM_CheckPointsForMoreInfo".Translate()
//                            }), 398462);
//                    }

//                    float x2 = Text.CalcSize("TM_Effeciency".Translate()).x;
//                    float x3 = Text.CalcSize("TM_Versatility".Translate()).x;
//                    Rect rect3 = new Rect(0f + MightCardUtility.SpacingOffset, rect.y + 2f, MightCardUtility.MightCardSize.x, MightCardUtility.ButtonSize * 1.15f);

//                    Rect rect5 = new Rect(rect3.x + rect3.width / 2f - x2, rect3.y, (rect3.width - 20f) / 3f, rect3.height);
//                    Rect rect6 = new Rect(rect3.width - x3 * 2f, rect3.y, rect3.width / 3f, rect3.height);

//                    float x4 = Text.CalcSize(" # / # ").x;
//                    //bool flag9 = power.abilityDef.label == "Sprint" || power.abilityDef.label == "Grapple"; //add all other buffs or xml based upgrades
//                    //power.abilityDef.defName == "TM_Sprint" || power.abilityDef.defName ==  "TM_Sprint_I" || power.abilityDef.defName == "TM_Sprint_II" || power.abilityDef.defName == "TM_Sprint_III" ||
//                    if (power.abilityDef.defName == "TM_Grapple" || power.abilityDef.defName == "TM_Grapple_I" || power.abilityDef.defName == "TM_Grapple_II" || power.abilityDef.defName == "TM_Grapple_III" ||
//                        power.abilityDef.defName == "TM_DisablingShot" || power.abilityDef.defName == "TM_DisablingShot_I" || power.abilityDef.defName == "TM_DisablingShot_II" || power.abilityDef.defName == "TM_DisablingShot_III" ||
//                        power.abilityDef.defName == "TM_PhaseStrike" || power.abilityDef.defName == "TM_PhaseStrike_I" || power.abilityDef.defName == "TM_PhaseStrike_II" || power.abilityDef.defName == "TM_PhaseStrike_III" ||
//                        power.abilityDef.defName == "TM_ArrowStorm" || power.abilityDef.defName == "TM_ArrowStorm_I" || power.abilityDef.defName == "TM_ArrowStorm_II" || power.abilityDef.defName == "TM_ArrowStorm_III" ||
//                        power.abilityDef.defName == "TM_PsionicBlast" || power.abilityDef.defName == "TM_PsionicBlast_I" || power.abilityDef.defName == "TM_PsionicBlast_II" || power.abilityDef.defName == "TM_PsionicBlast_III" ||
//                        power.abilityDef.defName == "TM_GraveBlade" || power.abilityDef.defName == "TM_GraveBlade_I" || power.abilityDef.defName == "TM_GraveBlade_II" || power.abilityDef.defName == "TM_GraveBlade_III" ||
//                        power.abilityDef.defName == "TM_Spite" || power.abilityDef.defName == "TM_Spite_I" || power.abilityDef.defName == "TM_Spite_II" || power.abilityDef.defName == "TM_Spite_III" ||
//                        power.abilityDef.defName == "TM_Transpose" || power.abilityDef.defName == "TM_Transpose_I" || power.abilityDef.defName == "TM_Transpose_II" || power.abilityDef.defName == "TM_Transpose_III" ||
//                        power.abilityDef == TorannMagicDefOf.TM_StayAlert || power.abilityDef == TorannMagicDefOf.TM_StayAlert_I || power.abilityDef == TorannMagicDefOf.TM_StayAlert_II || power.abilityDef == TorannMagicDefOf.TM_StayAlert_III ||
//                        power.abilityDef == TorannMagicDefOf.TM_MoveOut || power.abilityDef == TorannMagicDefOf.TM_MoveOut_I || power.abilityDef == TorannMagicDefOf.TM_MoveOut_II || power.abilityDef == TorannMagicDefOf.TM_MoveOut_III ||
//                        power.abilityDef == TorannMagicDefOf.TM_HoldTheLine || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_I || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_II || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_III ||
//                        power.abilityDef == TorannMagicDefOf.TM_PsionicBarrier || power.abilityDef == TorannMagicDefOf.TM_PsionicBarrier_Projected)
//                    {
//                        flag999 = true;
//                    }
//                    else
//                    {
//                        flag999 = false;
//                    }
//                    bool flag10 = enumerator.Current.level >= power.maxLevel || compMight.MightData.MightAbilityPoints < power.costToLevel;
//                    //Ability label
//                    Rect rectLabel = new Rect(0f + 20f, rect.yMin, 350f - 44f, MagicCardUtility.MagicButtonPointSize);
//                    Widgets.Label(rectLabel, power.abilityDef.LabelCap);

//                    if (enumerator.Current.learned != true)
//                    {
//                        Widgets.DrawTextureFitted(rect, power.Icon, 1f);
//                        Rect rectLearn = new Rect(rect.xMin - 44f, rect.yMin, 40f, MagicCardUtility.MagicButtonPointSize);
//                        if (compMight.MightData.MightAbilityPoints >= enumerator.Current.learnCost)
//                        {
//                            Text.Font = GameFont.Tiny;
//                            bool flagLearn = Widgets.ButtonText(rectLearn, "TM_Learn".Translate(), true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
//                            if (flagLearn)
//                            {
//                                enumerator.Current.learned = true;
//                                if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_PistolSpec)
//                                {
//                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_PistolWhip);
//                                    compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolWhip).learned = true;
//                                    compMight.skill_PistolWhip = true;
//                                }
//                                if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_RifleSpec)
//                                {
//                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_SuppressingFire);
//                                    compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_SuppressingFire).learned = true;
//                                    compMight.skill_SuppressingFire = true;
//                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_Mk203GL);
//                                    compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Mk203GL).learned = true;
//                                    compMight.skill_Mk203GL = true;
//                                }
//                                if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_ShotgunSpec)
//                                {
//                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_Buckshot);
//                                    compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Buckshot).learned = true;
//                                    compMight.skill_Buckshot = true;
//                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_BreachingCharge);
//                                    compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_BreachingCharge).learned = true;
//                                    compMight.skill_BreachingCharge = true;
//                                }
//                            }
//                        }
//                        if (MightPowerSkill1 == null && compMight.Pawn.story.traits.HasTrait(TorannMagicDefOf.TM_SuperSoldier))
//                        {
//                            Rect rectTechnoPath = new Rect(rect.xMax - "TM_SuperSoldierPathWarning".Translate().Length * 3, rect.yMin + MagicCardUtility.MagicButtonSize + 4f, "TM_SuperSoldierPathWarning".Translate().Length * 6, MagicCardUtility.TextSize * 3); // + (2 * (MagicCardUtility.MagicButtonSize + 4f))
//                            Widgets.Label(rectTechnoPath, "TM_SuperSoldierPathWarning".Translate());
//                        }
//                    }
//                    else
//                    {
//                        if (flag10)
//                        {
//                            if (flag999)
//                            {
//                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
//                                Rect rect19 = new Rect(rect.xMax, rect.yMin, x4, MightCardUtility.TextSize);
//                                Widgets.Label(rect19, " " + enumerator.Current.level + " / " + enumerator.Current.maxLevel);
//                            }
//                            else
//                            {
//                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
//                            }

//                        }
//                        else
//                        {
//                            if (flag999)
//                            {
//                                Rect rect10 = new Rect(rect.xMax, rect.yMin, x4, MightCardUtility.TextSize);
//                                bool flag1 = Widgets.ButtonImage(rect, power.Icon) && compMight.AbilityUser.Faction == Faction.OfPlayer;
//                                Widgets.Label(rect10, " " + power.level + " / " + power.maxLevel);
//                                if (flag1)
//                                {
//                                    compMight.LevelUpPower(power);
//                                    compMight.MightData.MightAbilityPoints -= power.costToLevel;
//                                    compMight.FixPowers();
//                                }
//                            }
//                            else
//                            {
//                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
//                            }
//                        }
//                    }
//                    Text.Font = GameFont.Tiny;
//                    float num2 = rect3.x;
//                    if (itnum == 1 && MightPowerSkill1 != null)
//                    {
//                        DrawSkillHandler(num2, compMight, power, enumerator, MightPowerSkill1, rect3);
//                        itnum++;
//                    }
//                    else if (itnum == 2 && MightPowerSkill2 != null)
//                    {
//                        DrawSkillHandler(num2, compMight, power, enumerator, MightPowerSkill2, rect3);
//                        itnum++;
//                    }
//                    else if (itnum == 3 && MightPowerSkill3 != null)
//                    {
//                        DrawSkillHandler(num2, compMight, power, enumerator, MightPowerSkill3, rect3);
//                        itnum++;
//                    }
//                    else if (itnum == 4 && MightPowerSkill4 != null)
//                    {
//                        DrawSkillHandler(num2, compMight, power, enumerator, MightPowerSkill4, rect3);
//                        itnum++;
//                    }
//                    else if (itnum == 5 && MightPowerSkill5 != null)
//                    {
//                        DrawSkillHandler(num2, compMight, power, enumerator, MightPowerSkill5, rect3);
//                        itnum++;
//                    }
//                    else if (itnum == 6 && MightPowerSkill6 != null)
//                    {
//                        DrawSkillHandler(num2, compMight, power, enumerator, MightPowerSkill6, rect3);
//                        itnum++;
//                    }
//                    else
//                    {
//                        //Log.Message("No skill iteration found.");
//                    }
//                    num += MightCardUtility.MagicButtonSize + MightCardUtility.TextSize + 4f;
//                }  
//            }
//        }

        //public static void DrawSkillHandler(float num2, CompAbilityUserMight compMight, MightPower power, List<MightPower>.Enumerator enumerator, List<MightPowerSkill> MightPowerSkillN, Rect rect3)
        //{
        //    using (List<MightPowerSkill>.Enumerator enumeratorN = MightPowerSkillN.GetEnumerator())
        //    {
        //        while (enumeratorN.MoveNext())
        //        {
        //            Rect rect4 = new Rect(num2 + MightCardUtility.MagicButtonPointSize, rect3.yMax, MightCardUtility.MightCardSize.x / 3f, rect3.height);
        //            Rect rect41 = new Rect(num2, rect4.y, MightCardUtility.MagicButtonPointSize, MightCardUtility.MagicButtonPointSize);
        //            Rect rect42 = new Rect(rect41.x, rect4.y, rect4.width - MagicCardUtility.MagicButtonPointSize, rect4.height / 2);
        //            MightPowerSkill skill = enumeratorN.Current;
        //            TooltipHandler.TipRegion(rect42, new TipSignal(() => skill.desc.Translate(), rect4.GetHashCode()));
        //            bool flag11 = (skill.level >= skill.levelMax || compMight.MightData.MightAbilityPoints == 0) || ((enumerator.Current.abilityDef.defName == "TM_PsionicAugmentation" || enumerator.Current.abilityDef.defName == "TM_BladeFocus" || enumerator.Current.abilityDef.defName == "TM_SniperFocus" || enumerator.Current.abilityDef.defName == "TM_BladeArt" || enumerator.Current.abilityDef.defName == "TM_RangerTraining" || enumerator.Current.abilityDef.defName == "TM_BowTraining") && compMight.MightData.MightAbilityPoints < 2);
        //            if (flag11)
        //            {
        //                Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
        //            }
        //            else
        //            {
        //                bool flag12 = Widgets.ButtonText(rect41, "+", true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
        //                Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
        //                if (flag12)
        //                {
        //                    bool flag17 = compMight.AbilityUser.story != null && compMight.AbilityUser.story.DisabledWorkTagsBackstoryAndTraits == WorkTags.Violent && power.abilityDef.MainVerb.isViolent;
        //                    if (flag17)
        //                    {
        //                        Messages.Message("IsIncapableOfViolenceLower".Translate(
        //                                    compMight.parent.LabelShort
        //                        ), MessageTypeDefOf.RejectInput);
        //                        break;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Sprint" || enumerator.Current.abilityDef.defName == "TM_Sprint_I" || enumerator.Current.abilityDef.defName == "TM_Sprint_II" || enumerator.Current.abilityDef.defName == "TM_Sprint_III")
        //                    {
        //                        compMight.LevelUpSkill_Sprint(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Fortitude")
        //                    {
        //                        compMight.LevelUpSkill_Fortitude(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Grapple" || enumerator.Current.abilityDef.defName == "TM_Grapple_I" || enumerator.Current.abilityDef.defName == "TM_Grapple_II" || enumerator.Current.abilityDef.defName == "TM_Grapple_III")
        //                    {
        //                        compMight.LevelUpSkill_Grapple(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Cleave")
        //                    {
        //                        compMight.LevelUpSkill_Cleave(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Whirlwind")
        //                    {
        //                        compMight.LevelUpSkill_Whirlwind(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SniperFocus" && compMight.MightData.MightAbilityPoints >= 2)
        //                    {
        //                        compMight.LevelUpSkill_SniperFocus(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 2;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Headshot")
        //                    {
        //                        compMight.LevelUpSkill_Headshot(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_DisablingShot" || enumerator.Current.abilityDef.defName == "TM_DisablingShot_I" || enumerator.Current.abilityDef.defName == "TM_DisablingShot_II" || enumerator.Current.abilityDef.defName == "TM_DisablingShot_III")
        //                    {
        //                        compMight.LevelUpSkill_DisablingShot(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_AntiArmor")
        //                    {
        //                        compMight.LevelUpSkill_AntiArmor(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ShadowSlayer")
        //                    {
        //                        //compMight.LevelUpSkill_AntiArmor(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BladeFocus" && compMight.MightData.MightAbilityPoints >= 2)
        //                    {
        //                        compMight.LevelUpSkill_BladeFocus(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 2;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BladeArt" && compMight.MightData.MightAbilityPoints >= 2)
        //                    {
        //                        compMight.LevelUpSkill_BladeArt(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 2;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SeismicSlash")
        //                    {
        //                        compMight.LevelUpSkill_SeismicSlash(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BladeSpin")
        //                    {
        //                        compMight.LevelUpSkill_BladeSpin(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PhaseStrike" || enumerator.Current.abilityDef.defName == "TM_PhaseStrike_I" || enumerator.Current.abilityDef.defName == "TM_PhaseStrike_II" || enumerator.Current.abilityDef.defName == "TM_PhaseStrike_III")
        //                    {
        //                        compMight.LevelUpSkill_PhaseStrike(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_RangerTraining" && compMight.MightData.MightAbilityPoints >= 2)
        //                    {
        //                        compMight.LevelUpSkill_RangerTraining(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 2;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BowTraining" && compMight.MightData.MightAbilityPoints >= 2)
        //                    {
        //                        compMight.LevelUpSkill_BowTraining(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 2;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PoisonTrap")
        //                    {
        //                        compMight.LevelUpSkill_PoisonTrap(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_AnimalFriend")
        //                    {
        //                        compMight.LevelUpSkill_AnimalFriend(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ArrowStorm" || enumerator.Current.abilityDef.defName == "TM_ArrowStorm_I" || enumerator.Current.abilityDef.defName == "TM_ArrowStorm_II" || enumerator.Current.abilityDef.defName == "TM_ArrowStorm_III")
        //                    {
        //                        compMight.LevelUpSkill_ArrowStorm(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Transpose" || enumerator.Current.abilityDef.defName == "TM_Transpose_I" || enumerator.Current.abilityDef.defName == "TM_Transpose_II" || enumerator.Current.abilityDef.defName == "TM_Transpose_III")
        //                    {
        //                        compMight.LevelUpSkill_Transpose(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Mimic")
        //                    {
        //                        compMight.LevelUpSkill_Mimic(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Reversal")
        //                    {
        //                        compMight.LevelUpSkill_Reversal(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Disguise")
        //                    {
        //                        compMight.LevelUpSkill_Disguise(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Possess")
        //                    {
        //                        compMight.LevelUpSkill_Possess(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PsionicBlast" || enumerator.Current.abilityDef.defName == "TM_PsionicBlast_I" || enumerator.Current.abilityDef.defName == "TM_PsionicBlast_II" || enumerator.Current.abilityDef.defName == "TM_PsionicBlast_III")
        //                    {
        //                        compMight.LevelUpSkill_PsionicBlast(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PsionicAugmentation" && compMight.MightData.MightAbilityPoints >= 2)
        //                    {
        //                        compMight.LevelUpSkill_PsionicAugmentation(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 2;
        //                        compMight.ResolveClassSkills();
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PsionicBarrier" || enumerator.Current.abilityDef.defName == "TM_PsionicBarrier_Projected")
        //                    {
        //                        compMight.LevelUpSkill_PsionicBarrier(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PsionicDash")
        //                    {
        //                        compMight.LevelUpSkill_PsionicDash(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PsionicStorm")
        //                    {
        //                        compMight.LevelUpSkill_PsionicStorm(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Shroud")
        //                    {
        //                        compMight.LevelUpSkill_Shroud(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_WaveOfFear")
        //                    {
        //                        compMight.LevelUpSkill_WaveOfFear(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Spite" || enumerator.Current.abilityDef.defName == "TM_Spite_I" || enumerator.Current.abilityDef.defName == "TM_Spite_II" || enumerator.Current.abilityDef.defName == "TM_Spite_III")
        //                    {
        //                        compMight.LevelUpSkill_Spite(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_LifeSteal")
        //                    {
        //                        compMight.LevelUpSkill_LifeSteal(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_GraveBlade" || enumerator.Current.abilityDef.defName == "TM_GraveBlade_I" || enumerator.Current.abilityDef.defName == "TM_GraveBlade_II" || enumerator.Current.abilityDef.defName == "TM_GraveBlade_III")
        //                    {
        //                        compMight.LevelUpSkill_GraveBlade(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Chi")
        //                    {
        //                        compMight.LevelUpSkill_Chi(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_MindOverBody")
        //                    {
        //                        compMight.LevelUpSkill_MindOverBody(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Meditate")
        //                    {
        //                        compMight.LevelUpSkill_Meditate(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_TigerStrike")
        //                    {
        //                        compMight.LevelUpSkill_TigerStrike(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_DragonStrike")
        //                    {
        //                        compMight.LevelUpSkill_DragonStrike(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ThunderStrike")
        //                    {
        //                        compMight.LevelUpSkill_ThunderStrike(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_WayfarerCraft")
        //                    {
        //                        compMight.LevelUpSkill_WayfarerCraft(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_FieldTraining")
        //                    {
        //                        compMight.LevelUpSkill_FieldTraining(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_ProvisionerAura)
        //                    {
        //                        compMight.LevelUpSkill_Provisioner(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_TaskMasterAura)
        //                    {
        //                        compMight.LevelUpSkill_TaskMaster(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_CommanderAura)
        //                    {
        //                        compMight.LevelUpSkill_Commander(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_StayAlert || enumerator.Current.abilityDef == TorannMagicDefOf.TM_StayAlert_I || enumerator.Current.abilityDef == TorannMagicDefOf.TM_StayAlert_II || enumerator.Current.abilityDef == TorannMagicDefOf.TM_StayAlert_III)
        //                    {
        //                        compMight.LevelUpSkill_StayAlert(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_MoveOut || enumerator.Current.abilityDef == TorannMagicDefOf.TM_MoveOut_I || enumerator.Current.abilityDef == TorannMagicDefOf.TM_MoveOut_II || enumerator.Current.abilityDef == TorannMagicDefOf.TM_MoveOut_III)
        //                    {
        //                        compMight.LevelUpSkill_MoveOut(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_HoldTheLine || enumerator.Current.abilityDef == TorannMagicDefOf.TM_HoldTheLine_I || enumerator.Current.abilityDef == TorannMagicDefOf.TM_HoldTheLine_II || enumerator.Current.abilityDef == TorannMagicDefOf.TM_HoldTheLine_III)
        //                    {
        //                        compMight.LevelUpSkill_HoldTheLine(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_PistolSpec)
        //                    {
        //                        compMight.LevelUpSkill_PistolSpec(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_RifleSpec)
        //                    {
        //                        compMight.LevelUpSkill_RifleSpec(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_ShotgunSpec)
        //                    {
        //                        compMight.LevelUpSkill_ShotgunSpec(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_CQC)
        //                    {
        //                        compMight.LevelUpSkill_CQC(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_FirstAid)
        //                    {
        //                        compMight.LevelUpSkill_FirstAid(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_60mmMortar)
        //                    {
        //                        compMight.LevelUpSkill_60mmMortar(skill.label);
        //                        skill.level++;
        //                        compMight.MightData.MightAbilityPoints -= 1;
        //                    }
        //                }
        //            }
        //            num2 += (MightCardUtility.MightCardSize.x / 3) - MightCardUtility.SpacingOffset;
        //        }
        //    }
        //}

        public static void CustomPowersHandler(Rect inRect, CompAbilityUserMight compMight, List<MightPower> MightPowers, List<TMAbilityDef> abilityList, Texture2D pointTexture)
        {
            float num = inRect.y;
            int itnum = 1;
            bool flag999;
            using (List<MightPower>.Enumerator enumerator = MightPowers.GetEnumerator())
            {
                EnumerationStart:;
                while (enumerator.MoveNext())
                {
                    MightPower power = enumerator.Current;
                    if(!abilityList.Contains((TMAbilityDef)power.abilityDef))
                    {
                        goto EnumerationStart;
                    }

                    //if (compMight.MightData.GetSkill_Efficiency((TMAbilityDef)power.abilityDef) == null)
                    //{
                    //    goto EnumerationStart;                        
                    //}


                    Text.Font = GameFont.Small;
                    Rect rect = new Rect(MightCardUtility.MightCardSize.x / 2f - MightCardUtility.MagicButtonSize, num, MightCardUtility.MagicButtonSize, MightCardUtility.MagicButtonSize);
                    if (itnum > 1)
                    {
                        Widgets.DrawLineHorizontal(0f + 20f, rect.y - 2f, 700f - 40f);
                    }
                    //power.abilityDef == TorannMagicDefOf.TM_Sprint || power.abilityDef == TorannMagicDefOf.TM_Sprint_I || power.abilityDef == TorannMagicDefOf.TM_Sprint_II ||
                    if (power.level < power.maxLevel && ((power.TMabilityDefs.Count > 1) || (power.abilityDef == TorannMagicDefOf.TM_Grapple || power.abilityDef == TorannMagicDefOf.TM_Grapple_I || power.abilityDef == TorannMagicDefOf.TM_Grapple_II ||
                        power.abilityDef == TorannMagicDefOf.TM_DisablingShot || power.abilityDef == TorannMagicDefOf.TM_DisablingShot_I || power.abilityDef == TorannMagicDefOf.TM_DisablingShot_II ||
                        power.abilityDef == TorannMagicDefOf.TM_PhaseStrike || power.abilityDef == TorannMagicDefOf.TM_PhaseStrike_I || power.abilityDef == TorannMagicDefOf.TM_PhaseStrike_II ||
                        power.abilityDef == TorannMagicDefOf.TM_ArrowStorm || power.abilityDef == TorannMagicDefOf.TM_ArrowStorm_I || power.abilityDef == TorannMagicDefOf.TM_ArrowStorm_II ||
                        power.abilityDef == TorannMagicDefOf.TM_PsionicBlast || power.abilityDef == TorannMagicDefOf.TM_PsionicBlast_I || power.abilityDef == TorannMagicDefOf.TM_PsionicBlast_II ||
                        power.abilityDef == TorannMagicDefOf.TM_GraveBlade || power.abilityDef == TorannMagicDefOf.TM_GraveBlade_I || power.abilityDef == TorannMagicDefOf.TM_GraveBlade_II ||
                        power.abilityDef == TorannMagicDefOf.TM_Spite || power.abilityDef == TorannMagicDefOf.TM_Spite_I || power.abilityDef == TorannMagicDefOf.TM_Spite_II ||
                        power.abilityDef == TorannMagicDefOf.TM_StayAlert || power.abilityDef == TorannMagicDefOf.TM_StayAlert_I || power.abilityDef == TorannMagicDefOf.TM_StayAlert_II ||
                        power.abilityDef == TorannMagicDefOf.TM_MoveOut || power.abilityDef == TorannMagicDefOf.TM_MoveOut_I || power.abilityDef == TorannMagicDefOf.TM_MoveOut_II ||
                        power.abilityDef == TorannMagicDefOf.TM_HoldTheLine || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_I || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_II ||
                        power.abilityDef == TorannMagicDefOf.TM_Transpose || power.abilityDef == TorannMagicDefOf.TM_Transpose_I || power.abilityDef == TorannMagicDefOf.TM_Transpose_II)))

                    {
                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
                        {
                            power.abilityDef.label,
                            "\n\nCurrent Level:\n",
                            power.abilityDescDef.description,
                            "\n\nNext Level:\n",
                            power.nextLevelAbilityDescDef?.description,
                            "\n\n",
                            MightAbility.PostAbilityDesc((TMAbilityDef)power.abilityDef, compMight, 0),
                            "\n",
                            "TM_CheckPointsForMoreInfo".Translate()
                       }), 398462);
                    }
                    else if (power.level < 1 && power.abilityDef == TorannMagicDefOf.TM_PsionicBarrier)
                    {
                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
                        {
                            power.abilityDef.label,
                            "\n\nCurrent Level:\n",
                            power.abilityDescDef.description,
                            "\n\nNext Level:\n",
                            power.nextLevelAbilityDescDef?.description,
                            "\n\n",
                            MightAbility.PostAbilityDesc((TMAbilityDef)power.abilityDef, compMight, 0),
                            "\n",
                            "TM_CheckPointsForMoreInfo".Translate()
                       }), 398462);
                    }
                    else
                    {
                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
{
                            power.abilityDef.label,
                            "\n\n",
                            power.abilityDescDef.description,
                            "\n\n",
                            MightAbility.PostAbilityDesc((TMAbilityDef)power.abilityDef, compMight, 0),
                            "\n",
                            "TM_CheckPointsForMoreInfo".Translate()
                            }), 398462);
                    }

                    float x2 = Text.CalcSize("TM_Effeciency".Translate()).x;
                    float x3 = Text.CalcSize("TM_Versatility".Translate()).x;
                    Rect rect3 = new Rect(0f + MightCardUtility.SpacingOffset, rect.y + 2f, MightCardUtility.MightCardSize.x, MightCardUtility.ButtonSize * 1.15f);

                    Rect rect5 = new Rect(rect3.x + rect3.width / 2f - x2, rect3.y, (rect3.width - 20f) / 3f, rect3.height);
                    Rect rect6 = new Rect(rect3.width - x3 * 2f, rect3.y, rect3.width / 3f, rect3.height);

                    float x4 = Text.CalcSize(" # / # ").x;
                    //bool flag9 = power.abilityDef.label == "Sprint" || power.abilityDef.label == "Grapple"; //add all other buffs or xml based upgrades
                    //power.abilityDef.defName == "TM_Sprint" || power.abilityDef.defName ==  "TM_Sprint_I" || power.abilityDef.defName == "TM_Sprint_II" || power.abilityDef.defName == "TM_Sprint_III" ||
                    if (power.TMabilityDefs.Count > 1 || power.abilityDef.defName == "TM_Grapple" || power.abilityDef.defName == "TM_Grapple_I" || power.abilityDef.defName == "TM_Grapple_II" || power.abilityDef.defName == "TM_Grapple_III" ||
                        power.abilityDef.defName == "TM_DisablingShot" || power.abilityDef.defName == "TM_DisablingShot_I" || power.abilityDef.defName == "TM_DisablingShot_II" || power.abilityDef.defName == "TM_DisablingShot_III" ||
                        power.abilityDef.defName == "TM_PhaseStrike" || power.abilityDef.defName == "TM_PhaseStrike_I" || power.abilityDef.defName == "TM_PhaseStrike_II" || power.abilityDef.defName == "TM_PhaseStrike_III" ||
                        power.abilityDef.defName == "TM_ArrowStorm" || power.abilityDef.defName == "TM_ArrowStorm_I" || power.abilityDef.defName == "TM_ArrowStorm_II" || power.abilityDef.defName == "TM_ArrowStorm_III" ||
                        power.abilityDef.defName == "TM_PsionicBlast" || power.abilityDef.defName == "TM_PsionicBlast_I" || power.abilityDef.defName == "TM_PsionicBlast_II" || power.abilityDef.defName == "TM_PsionicBlast_III" ||
                        power.abilityDef.defName == "TM_GraveBlade" || power.abilityDef.defName == "TM_GraveBlade_I" || power.abilityDef.defName == "TM_GraveBlade_II" || power.abilityDef.defName == "TM_GraveBlade_III" ||
                        power.abilityDef.defName == "TM_Spite" || power.abilityDef.defName == "TM_Spite_I" || power.abilityDef.defName == "TM_Spite_II" || power.abilityDef.defName == "TM_Spite_III" ||
                        power.abilityDef.defName == "TM_Transpose" || power.abilityDef.defName == "TM_Transpose_I" || power.abilityDef.defName == "TM_Transpose_II" || power.abilityDef.defName == "TM_Transpose_III" ||
                        power.abilityDef == TorannMagicDefOf.TM_StayAlert || power.abilityDef == TorannMagicDefOf.TM_StayAlert_I || power.abilityDef == TorannMagicDefOf.TM_StayAlert_II || power.abilityDef == TorannMagicDefOf.TM_StayAlert_III ||
                        power.abilityDef == TorannMagicDefOf.TM_MoveOut || power.abilityDef == TorannMagicDefOf.TM_MoveOut_I || power.abilityDef == TorannMagicDefOf.TM_MoveOut_II || power.abilityDef == TorannMagicDefOf.TM_MoveOut_III ||
                        power.abilityDef == TorannMagicDefOf.TM_HoldTheLine || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_I || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_II || power.abilityDef == TorannMagicDefOf.TM_HoldTheLine_III)
                    {
                        flag999 = true;
                    }
                    else
                    {
                        flag999 = false;
                    }
                    //if (power.abilityDef.defName == "TM_PsionicBarrier" || power.abilityDef.defName == "TM_PsionicBarrier_Projected")
                    //{
                    //    flag998 = true;
                    //}
                    //else
                    //{
                    //    flag998 = false;
                    //}
                    bool flag10 = enumerator.Current.level >= power.maxLevel || compMight.MightData.MightAbilityPoints < enumerator.Current.costToLevel;
                    //if (flag998)
                    //{
                    //    flag10 = enumerator.Current.level >= 1 || compMight.MightData.MightAbilityPoints < 2;
                    //}
                    //Ability label
                    Rect rectLabel = new Rect(0f + 20f, rect.yMin, 350f - 44f, MagicCardUtility.MagicButtonPointSize);
                    Widgets.Label(rectLabel, power.abilityDef.LabelCap);

                    if (enumerator.Current.learned != true)
                    {
                        Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                        Rect rectLearn = new Rect(rect.xMin - 44f, rect.yMin, 40f, MagicCardUtility.MagicButtonPointSize);
                        if (compMight.MightData.MightAbilityPoints >= enumerator.Current.learnCost)
                        {
                            Text.Font = GameFont.Tiny;
                            bool flagLearn = Widgets.ButtonText(rectLearn, "TM_Learn".Translate(), true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                            if (flagLearn)
                            {
                                enumerator.Current.learned = true;
                                if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_PistolSpec)
                                {
                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_PistolWhip);
                                    //compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_PistolWhip).learned = true;
                                    compMight.skill_PistolWhip = true;
                                }
                                if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_RifleSpec)
                                {
                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_SuppressingFire);
                                    //compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_SuppressingFire).learned = true;
                                    compMight.skill_SuppressingFire = true;
                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_Mk203GL);
                                    //compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Mk203GL).learned = true;
                                    compMight.skill_Mk203GL = true;
                                }
                                if (enumerator.Current.abilityDef == TorannMagicDefOf.TM_ShotgunSpec)
                                {
                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_Buckshot);
                                    //compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_Buckshot).learned = true;
                                    compMight.skill_Buckshot = true;
                                    compMight.AddPawnAbility(TorannMagicDefOf.TM_BreachingCharge);
                                    //compMight.MightData.MightPowersSS.FirstOrDefault<MightPower>((MightPower x) => x.abilityDef == TorannMagicDefOf.TM_BreachingCharge).learned = true;
                                    compMight.skill_BreachingCharge = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (flag10)
                        {
                            if (flag999)
                            {
                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                                Rect rect19 = new Rect(rect.xMax, rect.yMin, x4, MightCardUtility.TextSize);
                                Widgets.Label(rect19, " " + enumerator.Current.level + " / " + enumerator.Current.maxLevel);
                            }
                            //else if (flag998)
                            //{
                            //    Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                            //    Rect rect19 = new Rect(rect.xMax, rect.yMin, x4, MightCardUtility.TextSize);
                            //    Widgets.Label(rect19, " " + enumerator.Current.level + " / 1");
                            //}
                            else
                            {
                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                            }

                        }
                        else
                        {
                            if (flag999)
                            {
                                Rect rect10 = new Rect(rect.xMax, rect.yMin, x4, MightCardUtility.TextSize);
                                bool flag1 = Widgets.ButtonImage(rect, power.Icon) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                                Widgets.Label(rect10, " " + power.level + " / " + power.maxLevel);
                                if (flag1)
                                {
                                    compMight.LevelUpPower(power);
                                    compMight.MightData.MightAbilityPoints -= power.costToLevel;
                                    compMight.FixPowers();
                                }
                            }
                            //else if (flag998)
                            //{
                            //    Rect rect10 = new Rect(rect.xMax, rect.yMin, x4, MightCardUtility.TextSize);
                            //    bool flag1 = Widgets.ButtonImage(rect, power.Icon) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                            //    Widgets.Label(rect10, " " + power.level + " / 1");
                            //    if (flag1)
                            //    {
                            //        compMight.LevelUpPower(power);
                            //        compMight.MightData.MightAbilityPoints -= 2;
                            //        compMight.FixPowers();
                            //    }
                            //}
                            else
                            {
                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                            }
                        }
                    }
                    Text.Font = GameFont.Tiny;
                    float num2 = rect3.x;
                    List<MightPowerSkill> mpsList = new List<MightPowerSkill>();
                    mpsList.Clear();

                    MightPowerSkill mps = compMight.MightData.GetSkill_Power((TMAbilityDef)power.abilityDef);
                    if (mps != null)
                    {
                        mpsList.Add(mps);
                    }
                    mps = compMight.MightData.GetSkill_Efficiency((TMAbilityDef)power.abilityDef);
                    if (mps != null)
                    {
                        mpsList.Add(mps);
                    }
                    mps = compMight.MightData.GetSkill_Versatility((TMAbilityDef)power.abilityDef);
                    if (mps != null)
                    {
                        mpsList.Add(mps);
                    }

                    if (mpsList.Count > 0)
                    {
                        CustomSkillHandler(num2, compMight, power, enumerator, mpsList, rect3);                       
                    }
                    itnum++;
                    num += MightCardUtility.MagicButtonSize + MightCardUtility.TextSize + 4f;
                }
            }
        }

        public static void CustomSkillHandler(float num2, CompAbilityUserMight compMight, MightPower power, List<MightPower>.Enumerator enumerator, List<MightPowerSkill> MightPowerSkillN, Rect rect3)
        {
            using (List<MightPowerSkill>.Enumerator enumeratorN = MightPowerSkillN.GetEnumerator())
            {
                while (enumeratorN.MoveNext())
                {
                    Rect rect4 = new Rect(num2 + MightCardUtility.MagicButtonPointSize, rect3.yMax, MightCardUtility.MightCardSize.x / 3f, rect3.height);
                    Rect rect41 = new Rect(num2, rect4.y, MightCardUtility.MagicButtonPointSize, MightCardUtility.MagicButtonPointSize);
                    Rect rect42 = new Rect(rect41.x, rect4.y, rect4.width - MagicCardUtility.MagicButtonPointSize, rect4.height / 2);
                    MightPowerSkill skill = enumeratorN.Current;
                    TooltipHandler.TipRegion(rect42, new TipSignal(() => skill.desc.Translate(), rect4.GetHashCode()));
                    bool flag11 = (skill.level >= skill.levelMax || compMight.MightData.MightAbilityPoints == 0) || (skill.costToLevel > compMight.MightData.MightAbilityPoints);
                    if (flag11)
                    {
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect41, "+", true, false, true) && compMight.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            bool flag17 = compMight.AbilityUser.story != null && compMight.AbilityUser.story.DisabledWorkTagsBackstoryAndTraits == WorkTags.Violent && power.abilityDef.MainVerb.isViolent;
                            if (flag17)
                            {
                                Messages.Message("IsIncapableOfViolenceLower".Translate(
                                            compMight.parent.LabelShort
                                ), MessageTypeDefOf.RejectInput);
                                break;
                            }                                                       
                            skill.level++;
                            compMight.MightData.MightAbilityPoints -= skill.costToLevel;
                            
                            
                        }
                    }
                    num2 += (MightCardUtility.MightCardSize.x / 3) - MightCardUtility.SpacingOffset;
                }
            }
        }
    }
}