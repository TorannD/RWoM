using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    class MagicCardUtility //original code by Jecrell
    {
        public static Vector2 magicCardSize = default(Vector2); // new Vector2(700f, 556f);
        public static Vector2 MagicCardSize
        {
            get
            {
                if (magicCardSize == default(Vector2))
                {
                    magicCardSize = new Vector2(700f, 556f);
                    if(Screen.currentResolution.height <= 800)
                    {
                        magicCardSize = new Vector2(700f, 476f);
                    }
                }
                return magicCardSize;
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

        public static List<TMAbilityDef> GetAbilityList(CompAbilityUserMagic comp, List<MagicPower> classList = null)
        {
            List<TMAbilityDef> tmpList = new List<TMAbilityDef>();
            tmpList.Clear();
            if (classList != null)
            {
                foreach (MagicPower p in classList)
                {
                    foreach (TMAbilityDef t in p.TMabilityDefs)
                    {
                        tmpList.Add(t);
                    }
                }
            }           
            if (comp.customClass != null)
            {
                foreach (TMAbilityDef a in comp.customClass.classMageAbilities)
                {
                    tmpList.Add(a);
                }
            }
            if (comp.AdvancedClasses != null && comp.AdvancedClasses.Count > 0)
            {
                foreach (TMDefs.TM_CustomClass cc in comp.AdvancedClasses)
                {
                    foreach (TMAbilityDef ab in cc.classMageAbilities)
                    {
                        tmpList.Add(ab);
                    }
                }
            }
            return tmpList;
        }

        public static void DrawMagicCard(Rect rect, Pawn pawn)
        {
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            int sizeY = 0;
            if (comp.customClass != null || comp.AdvancedClasses.Count > 0)
            {
                List<TMDefs.TM_CustomClass> ccList = new List<TMDefs.TM_CustomClass>();
                ccList.Clear();
                int defaultCount = 0;
                if(comp.customClass != null)
                {
                    ccList.Add(comp.customClass);
                }
                else
                {
                    defaultCount = 5;
                }
                if(comp.AdvancedClasses.Count > 0)
                {
                    foreach(TMDefs.TM_CustomClass cc in comp.AdvancedClasses)
                    {
                        ccList.Add(cc);
                    }
                }
                sizeY = ((comp.MagicData.GetUniquePowersWithSkillsCount(ccList) + defaultCount) * 80);
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
            
            bool flag = comp != null && comp.MagicData != null;
            if (flag)
            {
                bool flag2 = true; //comp.MagicUserLevel > 0;
                if (flag2)
                {
                    float x = Text.CalcSize("TM_Header".Translate()).x;
                    Rect rect2 = new Rect(rect.width / 2f - (x / 2), rect.y, rect.width, MagicCardUtility.HeaderSize);
                    Text.Font = GameFont.Small;
                    Widgets.Label(rect2, "TM_Header".Translate().CapitalizeFirst());
                    Text.Font = GameFont.Small;
                    Widgets.DrawLineHorizontal(rect.x - 10f, rect2.yMax, rect.width - 15f);
                    Rect rect9 = new Rect(rect.x, rect2.yMax + MagicCardUtility.Padding, rect2.width, MagicCardUtility.SkillsColumnHeight);
                    Rect inRect = new Rect(rect9.x, rect9.y + MagicCardUtility.Padding, MagicCardUtility.SkillsColumnDivider, MagicCardUtility.SkillsColumnHeight);
                    Rect inRect2 = new Rect(rect9.x + MagicCardUtility.SkillsColumnDivider, rect9.y + MagicCardUtility.Padding, rect9.width - MagicCardUtility.SkillsColumnDivider, MagicCardUtility.SkillsColumnHeight);
                    MagicCardUtility.InfoPane(inRect, pawn.GetCompAbilityUserMagic(), pawn);
                    float x5 = Text.CalcSize("TM_Spells".Translate()).x;
                    Rect rect10 = new Rect(rect.width / 2f - x5 / 2f, rect9.yMax - 60f, rect.width, MagicCardUtility.HeaderSize);
                    Text.Font = GameFont.Small;
                    Widgets.Label(rect10, "TM_Spells".Translate().CapitalizeFirst());
                    Text.Font = GameFont.Small;
                    Widgets.DrawLineHorizontal(rect.x - 10f, rect10.yMax, rect.width - 15f);
                    Rect rect11 = new Rect(rect.x, rect10.yMax + MagicCardUtility.SectionOffset, rect10.width, MagicCardUtility.PowersColumnHeight);
                    if (comp.customClass != null)
                    {
                        Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                        MagicCardUtility.CustomPowersHandler(inRect3, comp, comp.MagicData.AllMagicPowersWithSkills, GetAbilityList(comp), TexButton.TMTex_SkillPointUsed);
                    }
                    else
                    {                        
                        if (pawn.story.traits.HasTrait(TorannMagicDefOf.InnerFire))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersIF), TexButton.TMTex_SkillPointUsed);
                            //if (pawn.GetCompAbilityUserMagic().spell_Firestorm == true)
                            //{                                
                            //    MagicCardUtility.PowersGUIHandlerTest(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersIF, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RayofHope, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Firebolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireclaw, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireball, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Firestorm, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandlerTest(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersIF, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RayofHope, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Firebolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireclaw, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireball, null, null, TexButton.TMTex_SkillPointUsed);
                            //}

                        }
                        if (pawn.story.traits.HasTrait(TorannMagicDefOf.HeartOfFrost))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersHoF), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_Blizzard == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersHoF, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Soothe, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Rainmaker, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Icebolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FrostRay, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Snowball, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Blizzard, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersHoF, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Soothe, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Rainmaker, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Icebolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FrostRay, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Snowball, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.StormBorn))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersSB), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_EyeOfTheStorm == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersSB, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AMP, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningBolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningCloud, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningStorm, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EyeOfTheStorm, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersSB, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AMP, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningBolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningCloud, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningStorm, null, null, TexButton.TMTex_SkillPointUsed);
                            //}

                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Arcanist))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersA), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_FoldReality == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersA, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Shadow, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_MagicMissile, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Blink, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Summon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Teleport, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FoldReality, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersA, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Shadow, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_MagicMissile, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Blink, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Summon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Teleport, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Paladin))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersP), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_HolyWrath == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersP, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_P_RayofHope, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Heal, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Shield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ValiantCharge, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overwhelm, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_HolyWrath, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersP, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_P_RayofHope, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Heal, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Shield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ValiantCharge, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overwhelm, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Summoner))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersS), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_SummonPoppi == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersS, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonMinion, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPylon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonExplosive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonElemental, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPoppi, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersS, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonMinion, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonPylon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonExplosive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SummonElemental, null, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Druid))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersD), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_RegrowLimb == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersD, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Poison, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SootheAnimal, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Regenerate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CureDisease, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RegrowLimb, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersD, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Poison, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SootheAnimal, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Regenerate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CureDisease, null, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Necromancer))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            List<TMAbilityDef> necroList = GetAbilityList(comp, comp.MagicData.MagicPowersN);
                            necroList.Remove(TorannMagicDefOf.TM_DeathBolt);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, necroList, TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_LichForm == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersN, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RaiseUndead, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_DeathMark, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FogOfTorment, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ConsumeCorpse, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CorpseExplosion, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LichForm, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersN, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RaiseUndead, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_DeathMark, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FogOfTorment, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ConsumeCorpse, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CorpseExplosion, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            List<TMAbilityDef> necroList = GetAbilityList(comp, comp.MagicData.MagicPowersN);
                            necroList.Remove(TorannMagicDefOf.TM_LichForm);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, necroList, TexButton.TMTex_SkillPointUsed);

                            //MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersN, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_RaiseUndead, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_DeathMark, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_FogOfTorment, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ConsumeCorpse, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_CorpseExplosion, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_DeathBolt, TexButton.TMTex_SkillPointUsed);
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Priest))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersPR), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_Resurrection == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersPR, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AdvancedHeal, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Purify, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_HealingCircle, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BestowMight, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Resurrection, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersPR, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AdvancedHeal, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Purify, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_HealingCircle, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BestowMight, null, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Bard))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersB), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_BattleHymn == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersB, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BardTraining, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Entertain, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Lullaby, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BattleHymn, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersB, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BardTraining, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Entertain, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Lullaby, null, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersSD), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_Scorn == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersSD, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SoulBond, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ShadowBolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Dominate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Attraction, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Scorn, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersSD, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SoulBond, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ShadowBolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Dominate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Attraction, null, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersWD), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_PsychicShock == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersWD, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SoulBond, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ShadowBolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Dominate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Repulsion, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_PsychicShock, null, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersWD, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_SoulBond, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ShadowBolt, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Dominate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Repulsion, null, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Geomancer))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersG), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_Meteor == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersG, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Stoneskin, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Encase, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthSprites, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthernHammer, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sentinel, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Meteor, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersG, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Stoneskin, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Encase, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthSprites, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthernHammer, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sentinel, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);                            
                            List<TMAbilityDef> technoList = GetAbilityList(comp, comp.MagicData.MagicPowersT);

                            if (comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower mp) => mp.abilityDef == TorannMagicDefOf.TM_TechnoBit).learned == true)
                            {
                                technoList.Remove(TorannMagicDefOf.TM_TechnoTurret);
                                technoList.Remove(TorannMagicDefOf.TM_TechnoWeapon);
                                MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, technoList, TexButton.TMTex_SkillPointUsed);

                                //if (pawn.GetCompAbilityUserMagic().spell_OrbitalStrike == true)
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoBit, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overdrive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sabotage, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_OrbitalStrike, null, TexButton.TMTex_SkillPointUsed);
                                //}
                                //else
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoBit, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overdrive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sabotage, null, null, TexButton.TMTex_SkillPointUsed);
                                //}
                            }
                            else if (comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower mp) => mp.abilityDef == TorannMagicDefOf.TM_TechnoTurret).learned == true)
                            {
                                technoList.Remove(TorannMagicDefOf.TM_TechnoBit);
                                technoList.Remove(TorannMagicDefOf.TM_TechnoWeapon);
                                MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, technoList, TexButton.TMTex_SkillPointUsed);

                                //if (pawn.GetCompAbilityUserMagic().spell_OrbitalStrike == true)
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overdrive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sabotage, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_OrbitalStrike, null, TexButton.TMTex_SkillPointUsed);
                                //}
                                //else
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overdrive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sabotage, null, null, TexButton.TMTex_SkillPointUsed);
                                //}
                            }
                            else if (comp.MagicData.MagicPowersT.FirstOrDefault<MagicPower>((MagicPower mp) => mp.abilityDef == TorannMagicDefOf.TM_TechnoWeapon).learned == true)
                            {
                                technoList.Remove(TorannMagicDefOf.TM_TechnoBit);
                                technoList.Remove(TorannMagicDefOf.TM_TechnoTurret);
                                MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, technoList, TexButton.TMTex_SkillPointUsed);

                                //if (pawn.GetCompAbilityUserMagic().spell_OrbitalStrike == true)
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoWeapon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overdrive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sabotage, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_OrbitalStrike, null, TexButton.TMTex_SkillPointUsed);
                                //}
                                //else
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoWeapon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overdrive, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Sabotage, null, null, TexButton.TMTex_SkillPointUsed);
                                //}
                            }
                            else
                            {
                                technoList.Remove(TorannMagicDefOf.TM_TechnoShield);
                                technoList.Remove(TorannMagicDefOf.TM_Overdrive);
                                technoList.Remove(TorannMagicDefOf.TM_Sabotage);
                                MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, technoList, TexButton.TMTex_SkillPointUsed);

                                //if (pawn.GetCompAbilityUserMagic().spell_OrbitalStrike == true)
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoBit, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoWeapon, null, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_OrbitalStrike, null, TexButton.TMTex_SkillPointUsed);
                                //}
                                //else
                                //{
                                //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersT, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoBit, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoTurret, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoWeapon, null, null, null, TexButton.TMTex_SkillPointUsed);
                                //}
                            }                            
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.BloodMage))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersBM), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_BloodMoon == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersBM, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodGift, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_IgniteBlood, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodForBlood, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Rend, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodMoon, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersBM, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodGift, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_IgniteBlood, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodForBlood, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodShield, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Rend, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Enchanter))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersE), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_Shapeshift == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersE, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchantedBody, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Transmutate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchanterStone, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchantWeapon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Polymorph, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Shapeshift, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersE, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchantedBody, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Transmutate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchanterStone, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EnchantWeapon, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Polymorph, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.Chronomancer))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersC), TexButton.TMTex_SkillPointUsed);

                            //if (pawn.GetCompAbilityUserMagic().spell_Recall == true)
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersC, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Prediction, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AlterFate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AccelerateTime, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ReverseTime, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ChronostaticField, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Recall, TexButton.TMTex_SkillPointUsed);
                            //}
                            //else
                            //{
                            //    MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersC, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Prediction, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AlterFate, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_AccelerateTime, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ReverseTime, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_ChronostaticField, null, TexButton.TMTex_SkillPointUsed);
                            //}
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            List<MagicPower> CMList = new List<MagicPower>();
                            CMList.Clear();
                            CMList.AddRange(comp.MagicData.MagicPowersCM);
                            for (int i = 0; i < comp.chaosPowers.Count; i++)
                            {
                                MagicPower mp = comp.MagicData.AllMagicPowersForChaosMage.FirstOrDefault<MagicPower>((MagicPower cm) => cm.TMabilityDefs[0] == comp.chaosPowers[i].Ability);
                                if (mp.learned)
                                {
                                    CMList.Add(mp);
                                }
                            }
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, CMList), TexButton.TMTex_SkillPointUsed);

                            //MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersCM, comp.customClass, TexButton.TMTex_SkillPointUsed);
                            //MagicCardUtility.PowersGUIHandler_CM(inRect3, comp, comp.MagicData.AllMagicPowersForChaosMage, comp.MagicData.MagicPowerSkill_ChaosTradition, comp.chaosPowers[0].Skills, comp.chaosPowers[1].Skills, comp.chaosPowers[2].Skills, comp.chaosPowers[3].Skills, comp.chaosPowers[4].Skills, TexButton.TMTex_SkillPointUsed);
                            //MagicCardUtility.PowersGUIHandler_CM(inRect3, comp, CMList, comp.MagicData.MagicPowerSkill_ChaosTradition, comp.chaosPowers[0].Skills, comp.chaosPowers[1].Skills, comp.chaosPowers[2].Skills, comp.chaosPowers[3].Skills, comp.chaosPowers[4].Skills, TexButton.TMTex_SkillPointUsed);
                        }
                        else if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer))
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.AllMagicPowersWithSkills, GetAbilityList(comp, comp.MagicData.MagicPowersW), TexButton.TMTex_SkillPointUsed);

                            //MagicCardUtility.PowersGUIHandler(inRect3, pawn.GetCompAbilityUserMagic(), pawn.GetCompAbilityUserMagic().MagicData.MagicPowersW, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_WandererCraft, pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Cantrips, null, null, null, null, TexButton.TMTex_SkillPointUsed);
                        }
                        else if(comp.AdvancedClasses.Count > 0)
                        {
                            Rect inRect3 = new Rect(rect.x, rect11.y, MagicCardUtility.PowersColumnWidth, MagicCardUtility.PowersColumnHeight);
                            MagicCardUtility.CustomPowersHandler(inRect3, comp, comp.MagicData.AllMagicPowersWithSkills, GetAbilityList(comp), TexButton.TMTex_SkillPointUsed);
                        }
                    }
                }
            }
            GUI.EndScrollView();
            //Widgets.EndScrollView();
            //GUI.EndGroup();
        }

        public static void InfoPane(Rect inRect, CompAbilityUserMagic compMagic, Pawn pawn)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width * 0.7f, MagicCardUtility.TextSize);
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect, "TM_Level".Translate().CapitalizeFirst() + ": " + compMagic.MagicData.MagicUserLevel.ToString());
            Text.Font = GameFont.Tiny;
            bool godMode = DebugSettings.godMode;
            if (godMode)
            {
                Rect rect2 = new Rect(rect.xMax, inRect.y, inRect.width * 0.3f, MagicCardUtility.TextSize);
                bool flag = Widgets.ButtonText(rect2, "+", true, false, true);
                if (flag)
                {
                    compMagic.LevelUp(true);
                }

                Rect rect22 = new Rect(rect.xMax + 60f, inRect.y, 50f, MagicCardUtility.TextSize * 2);
                bool flag22 = Widgets.ButtonText(rect22, "Reset Class", true, false, true);
                if (flag22)
                {
                    compMagic.ResetSkills();
                }
                //Rect rect23 = new Rect(rect.xMax + 115f, inRect.y, 50f, MagicCardUtility.TextSize * 2);
                //bool flag23 = Widgets.ButtonText(rect23, "Remove Powers", true, false, true);
                //if (flag23)
                //{
                //    compMagic.RemoveAbilityUser();
                //}                
            }
            Rect rect4 = new Rect(inRect.x, rect.yMax, inRect.width, MagicCardUtility.TextSize);
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect4, "TM_PointsAvail".Translate() + ": " + compMagic.MagicData.MagicAbilityPoints);
            Text.Font = GameFont.Tiny;            
            if(!godMode)
            {
                Rect rect6 = new Rect(rect4.xMax + 10f, rect.yMax, inRect.width + 100f, MagicCardUtility.TextSize);
                Widgets.Label(rect6, "TM_LastManaGainPct".Translate(
                    (compMagic.Mana.lastGainPct * 200).ToString("0.000")
                    ));
                string str1 = "Base mana gain: " + (200 * compMagic.Mana.baseManaGain).ToString("0.000") + "\nMana regen bonus: " + (200 * compMagic.Mana.modifiedManaGain).ToString("0.000");
                TooltipHandler.TipRegion(rect6, () => string.Concat(new string[]
                        {
                        str1,
                         "\n\nMana surge: +",
                        (200 * compMagic.Mana.drainManaSurge).ToString("0.000"),
                        "\nSyrrium boost: +",
                        (200*compMagic.Mana.drainSyrrium).ToString("0.000"),
                        "\nModified mana gain: ",
                        (200*(compMagic.Mana.baseManaGain + compMagic.Mana.modifiedManaGain + compMagic.Mana.drainSyrrium + compMagic.Mana.drainManaSurge)).ToString("0.000"),
                        "\nEnergy regen bonus: +",
                        (200*compMagic.Mana.drainEnergyHD).ToString("0.000"),
                        "\n\nMana weakness: -",
                        (200*compMagic.Mana.drainManaWeakness).ToString("0.000"),
                        "\nMinion cost: -",
                        (200*compMagic.Mana.drainMinion).ToString("0.000"),
                        "\nSprite cost: -",
                        (200*compMagic.Mana.drainSprites).ToString("0.000"),
                        "\nUndead cost: -",
                        (200*compMagic.Mana.drainUndead).ToString("0.000"),
                        "\nMana drain: -",
                        (200*compMagic.Mana.drainManaDrain).ToString("0.000"),
                        "\nMana sickness: -",
                        (200*compMagic.Mana.drainManaSickness).ToString("0.000"),
                        "\nParacytic drain: -",
                        (200*compMagic.Mana.paracyteCountReduction).ToString("0.000"),
                        //"\nSigil drain: -",
                        //(2*compMagic.Mana.drainSigils).ToString("0.000"),
                        "\nTotal mana upkeep: ",
                        (-200 *(compMagic.Mana.paracyteCountReduction + compMagic.Mana.drainManaSickness + compMagic.Mana.drainManaDrain + compMagic.Mana.drainUndead + compMagic.Mana.drainSprites + compMagic.Mana.drainMinion + compMagic.Mana.drainManaWeakness)).ToString("0.000"),
                        }), 398552);
                GUI.color = Color.white;
            }
            //"Base gain: ",
            //            (100 * compMagic.Mana.baseManaGain).ToString("0.000"),
            //            "\nMana surge: ",
            //            compMagic.Mana.drainManaSurge.ToString("0.000"),
            //            "\n\nMana weakness: ",
            //            compMagic.Mana.drainManaWeakness.ToString("0.000"),
            //            "\nMinion cost: ",
            //            compMagic.Mana.drainMinion.ToString("0.000"),
            //            "\nUndead cost: ",
            //            compMagic.Mana.drainUndead.ToString("0.000"),
            //            "\nMana drain: ",
            //            compMagic.Mana.drainManaDrain.ToString("0.000"),
            //            "\nMana sickness: ",
            //            compMagic.Mana.drainManaSickness.ToString("0.000"),
            Rect rect5 = new Rect(rect4.x, rect4.yMax + 3f, inRect.width + 100f, MagicCardUtility.HeaderSize * 0.6f);
            MagicCardUtility.DrawLevelBar(rect5, compMagic, pawn, inRect);
        }

        public static void DrawLevelBar(Rect rect, CompAbilityUserMagic compMagic, Pawn pawn, Rect rectG)
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
            TooltipHandler.TipRegion(rect, new TipSignal(() => MagicCardUtility.MagicXPTipString(compMagic), rect.GetHashCode()));
            float num2 = 14f;
            bool flag3 = rect.height < 50f;
            if (flag3)
            {
                num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect2 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height);
            rect2 = new Rect(rect2.x, rect2.y, rect2.width, rect2.height - num2);
            Widgets.FillableBar(rect2, compMagic.XPTillNextLevelPercent, (Texture2D)AccessTools.Field(typeof(Widgets), "BarFullTexHor").GetValue(null), BaseContent.GreyTex, false);
            Rect rect3 = new Rect(rect2.x + 272f + MagicCardUtility.MagicButtonPointSize, rectG.y, 136f, MagicCardUtility.TextSize);
            Rect rect31 = new Rect(rect2.x + 272f, rectG.y, MagicCardUtility.MagicButtonPointSize, MagicCardUtility.TextSize);
            Rect rect4 = new Rect(rect3.x + rect3.width + (MagicCardUtility.MagicButtonPointSize * 2), rectG.y, 136f, MagicCardUtility.TextSize);
            Rect rect41 = new Rect(rect3.x + rect3.width + MagicCardUtility.MagicButtonPointSize, rectG.y, MagicCardUtility.MagicButtonPointSize, MagicCardUtility.TextSize);
            Rect rect5 = new Rect(rect2.x + 272f + MagicCardUtility.MagicButtonPointSize, rectG.yMin + 24f, 136f, MagicCardUtility.TextSize);
            Rect rect51 = new Rect(rect2.x + 272f, rectG.yMin + 24f, MagicCardUtility.MagicButtonPointSize, MagicCardUtility.TextSize);

            List<MagicPowerSkill> skill1 = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_regen;
            List<MagicPowerSkill> skill2 = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_eff;
            List<MagicPowerSkill> skill3 = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_global_spirit;

            using (List<MagicPowerSkill>.Enumerator enumerator1 = skill1.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {                    
                    MagicPowerSkill skill = enumerator1.Current;
                    TooltipHandler.TipRegion(rect3, new TipSignal(() => skill.desc.Translate(), rect3.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMagic.MagicData.MagicAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect3, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect31, "+", true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect3, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_regen_pwr")
                            {
                                //compMagic.LevelUpSkill_global_regen(skill.label);
                                skill.level++;
                                compMagic.MagicData.MagicAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
            using (List<MagicPowerSkill>.Enumerator enumerator2 = skill2.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    MagicPowerSkill skill = enumerator2.Current;
                    TooltipHandler.TipRegion(rect4, new TipSignal(() => skill.desc.Translate(), rect4.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMagic.MagicData.MagicAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect41, "+", true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_eff_pwr")
                            {
                                //compMagic.LevelUpSkill_global_eff(skill.label);
                                skill.level++;
                                compMagic.MagicData.MagicAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
            using (List<MagicPowerSkill>.Enumerator enumerator3 = skill3.GetEnumerator())
            {
                while (enumerator3.MoveNext())
                {
                    MagicPowerSkill skill = enumerator3.Current;
                    TooltipHandler.TipRegion(rect5, new TipSignal(() => skill.desc.Translate(), rect5.GetHashCode()));
                    bool flag11 = skill.level >= skill.levelMax || compMagic.MagicData.MagicAbilityPoints == 0;
                    if (flag11)
                    {
                        Widgets.Label(rect5, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect51, "+", true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect5, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {
                            if (skill.label == "TM_global_spirit_pwr")
                            {
                                //compMagic.LevelUpSkill_global_spirit(skill.label);
                                skill.level++;
                                compMagic.MagicData.MagicAbilityPoints -= 1;
                            }
                        }
                    }
                }
            }
        }

        public static string MagicXPTipString(CompAbilityUserMagic compMagic)
        {
            string result;

            result = string.Concat(new string[]
            {
                compMagic.MagicUserXP.ToString(),
                " / ",
                compMagic.MagicUserXPTillNextLevel.ToString(),
                "\n",
                "TM_MagicXPDesc".Translate()
            });

            return result;
        }

        //Legacy Powers handler TODO:REMOVE
        //public static void PowersGUIHandler(Rect inRect, CompAbilityUserMagic compMagic, List<MagicPower> MagicPowers, List<MagicPowerSkill> MagicPowerSkill1, List<MagicPowerSkill> MagicPowerSkill2, List<MagicPowerSkill> MagicPowerSkill3, List<MagicPowerSkill> MagicPowerSkill4, List<MagicPowerSkill> MagicPowerSkill5, List<MagicPowerSkill> MagicPowerSkill6, Texture2D pointTexture)
        //{
        //    float num = inRect.y;
        //    int itnum = 1;
        //    bool flag999;
        //    using (List<MagicPower>.Enumerator enumerator = MagicPowers.GetEnumerator())
        //    {
        //        EnumerationStart:;
        //        while (enumerator.MoveNext())
        //        {                    
        //            MagicPower power = enumerator.Current;
        //            if(power.abilityDef == TorannMagicDefOf.TM_LichForm && compMagic.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
        //            {
        //                enumerator.MoveNext();
        //                power = enumerator.Current;                        
        //            }
        //            if(power.abilityDef.defName.Contains("TM_DeathBolt") && !compMagic.Pawn.story.traits.HasTrait(TorannMagicDefOf.Lich))
        //            {
        //                enumerator.MoveNext();
        //                goto EnumerationStart;
        //            }
        //            if(MagicPowerSkill4 == null)
        //            {
        //                if(power.abilityDef == TorannMagicDefOf.TM_TechnoShield || power.abilityDef == TorannMagicDefOf.TM_Sabotage || power.abilityDef == TorannMagicDefOf.TM_Overdrive)
        //                {
        //                    goto EnumerationStart;
        //                }
        //            }
        //            else
        //            {
        //                if((power.abilityDef == TorannMagicDefOf.TM_TechnoBit || power.abilityDef == TorannMagicDefOf.TM_TechnoTurret || power.abilityDef == TorannMagicDefOf.TM_TechnoWeapon) && !power.learned)
        //                {
        //                    goto EnumerationStart;
        //                }
        //            }
        //            Text.Font = GameFont.Small;
        //            Rect rect = new Rect(MagicCardUtility.MagicCardSize.x / 2f - MagicCardUtility.MagicButtonSize, num, MagicCardUtility.MagicButtonSize, MagicCardUtility.MagicButtonSize);
        //            if (itnum > 1)
        //            {
        //                Widgets.DrawLineHorizontal(0f + 20f, rect.y - 2f, MagicCardUtility.MagicCardSize.x - 40f);
        //            }
        //            if (power.level < 3 && TM_Calc.IsIconAbility_02(power.abilityDef))
        //            {

        //                TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
        //                {
        //                power.abilityDef.label,
        //                "\n\nCurrent Level:\n",
        //                power.abilityDescDef.description,
        //                "\n\nNext Level:\n",
        //                power.nextLevelAbilityDescDef?.description,
        //                "\n\n",
        //                "TM_CheckPointsForMoreInfo".Translate()
        //                }), 398462);
                        
        //            }
        //            else
        //            {
        //                TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
        //                    {
        //                    power.abilityDef.label,
        //                    "\n\n",
        //                    power.abilityDescDef.description,
        //                    "\n\n",
        //                    "TM_CheckPointsForMoreInfo".Translate()
        //                    }), 398462);
        //            }

        //            float x2 = Text.CalcSize("TM_Effeciency".Translate()).x;
        //            float x3 = Text.CalcSize("TM_Versatility".Translate()).x;
        //            Rect rect3 = new Rect(0f + MagicCardUtility.SpacingOffset, rect.y + 2f, MagicCardUtility.MagicCardSize.x, MagicCardUtility.ButtonSize * 1.15f);

        //            Rect rect5 = new Rect(rect3.x + rect3.width / 2f - x2, rect3.y, (rect3.width - 20f) / 3f, rect3.height);
        //            Rect rect6 = new Rect(rect3.width - x3 * 2f, rect3.y, rect3.width / 3f, rect3.height);

        //            float x4 = Text.CalcSize(" # / # ").x;
        //            //bool flag9 = power.abilityDef.label == "Ray of Hope" || power.abilityDef.label == "Soothing Breeze" || power.abilityDef.label == "Frost Ray" || power.abilityDef.label == "AMP" || power.abilityDef.label == "Shadow" || power.abilityDef.label == "Magic Missile" || power.abilityDef.label == "Blink" || power.abilityDef.label == "Summon" || power.abilityDef.label == "Shield"; //add all other buffs or xml based upgrades
                    
        //            if (TM_Calc.IsIconAbility_03(power.abilityDef))
        //            {
        //                flag999 = true;
        //            }
        //            else
        //            {
        //                flag999 = false;
        //            }
        //            Rect rectLabel = new Rect(0f + 20f, rect.yMin, 350f - 44f, MagicCardUtility.MagicButtonPointSize);
        //            //GUI.color = Color.yellow;
        //            Widgets.Label(rectLabel, power.abilityDef.LabelCap);
        //            //GUI.color = Color.white;
        //            if (enumerator.Current.learned != true)
        //            {
        //                Widgets.DrawTextureFitted(rect, power.Icon, 1f);                        
        //                Rect rectLearn = new Rect(rect.xMin - 44f, rect.yMin, 40f, MagicCardUtility.MagicButtonPointSize);
        //                if (compMagic.MagicData.MagicAbilityPoints >= enumerator.Current.learnCost)
        //                {
        //                    Text.Font = GameFont.Tiny;                            
        //                    bool flagLearn = Widgets.ButtonText(rectLearn, "TM_Learn".Translate(), true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
        //                    if (flagLearn)
        //                    {
        //                        enumerator.Current.learned = true;
        //                        if (!(enumerator.Current.abilityDef.defName == "TM_TechnoBit"))
        //                        {
        //                            compMagic.AddPawnAbility(enumerator.Current.abilityDef);
        //                        }
        //                        if ((enumerator.Current.abilityDef.defName == "TM_TechnoWeapon"))
        //                        {
        //                            compMagic.AddPawnAbility(TorannMagicDefOf.TM_NanoStimulant);
        //                        }
        //                        compMagic.MagicData.MagicAbilityPoints -= enumerator.Current.learnCost;
        //                    }
        //                }
        //                else
        //                {
        //                    if ((power.abilityDef.defName == "TM_TechnoShield" || power.abilityDef.defName == "TM_Sabotage" || power.abilityDef.defName == "TM_Overdrive"))
        //                    {
        //                        Rect rectToLearn = new Rect(rect.xMin - 268f, rect.yMin + 22f, 250f, MagicButtonPointSize);
        //                        Text.Font = GameFont.Tiny;
        //                        bool flagLearn = Widgets.ButtonText(rectToLearn, "TM_SpellLocked".Translate(power.abilityDef.LabelCap), false, false, false) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
        //                    }
        //                    else
        //                    {
        //                        Rect rectToLearn = new Rect(rect.xMin - 98f, rect.yMin, 100f, MagicButtonPointSize);
        //                        Text.Font = GameFont.Tiny;
        //                        bool flagLearn = Widgets.ButtonText(rectToLearn, "" + enumerator.Current.learnCost + " points to " + "TM_Learn".Translate(), false, false, false) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                bool flag10 = enumerator.Current.level >= 3 || compMagic.MagicData.MagicAbilityPoints < power.costToLevel;
        //                if (flag10)
        //                {
        //                    if (flag999)
        //                    {
        //                        if ((power.abilityDef.defName == "TM_Meteor" && compMagic.spell_Meteor != true) || (power.abilityDef.defName == "TM_OrbitalStrike" && compMagic.spell_OrbitalStrike != true) || (power.abilityDef.defName == "TM_BloodMoon" && compMagic.spell_BloodMoon != true))
        //                        {
        //                            Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                        }
        //                        else
        //                        {
        //                            Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                            Rect rect19 = new Rect(rect.xMax, rect.yMin, x4, MagicCardUtility.TextSize);
        //                            Widgets.Label(rect19, " " + enumerator.Current.level + " / " + enumerator.Current.maxLevel);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                    }
        //                }
        //                else
        //                {
        //                    if (flag999)
        //                    {
        //                        if ((power.abilityDef.defName == "TM_Meteor" && compMagic.spell_Meteor != true) || (power.abilityDef.defName == "TM_OrbitalStrike" && compMagic.spell_OrbitalStrike != true) || (power.abilityDef.defName == "TM_BloodMoon" && compMagic.spell_BloodMoon != true))
        //                        {
        //                            Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                        }
        //                        else
        //                        {
        //                            Rect rect10 = new Rect(rect.xMax, rect.yMin, x4, MagicCardUtility.TextSize);
        //                            bool flag1 = Widgets.ButtonImage(rect, power.Icon) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
        //                            Widgets.Label(rect10, " " + power.level + " / " + power.maxLevel);
        //                            if (flag1)
        //                            {
        //                                compMagic.LevelUpPower(power);
        //                                compMagic.MagicData.MagicAbilityPoints -= power.costToLevel;
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                    }
        //                }
        //                if ((power.abilityDef == TorannMagicDefOf.TM_Firestorm && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_Blizzard && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_EyeOfTheStorm && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_FoldReality && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_RegrowLimb && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_LichForm && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_SummonPoppi && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_BattleHymn && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_Scorn && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_PsychicShock && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_Meteor && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_OrbitalStrike && MagicPowerSkill5 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_BloodMoon && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_Shapeshift && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_Recall && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_HolyWrath && MagicPowerSkill6 == null) ||
        //                    (power.abilityDef == TorannMagicDefOf.TM_Resurrection && MagicPowerSkill5 == null))
        //                {
        //                    Rect rectMasterLock = new Rect(rect.xMax - 23f - "TM_MasterSpellLocked".Translate().Length * 4, rect.yMin + MagicCardUtility.MagicButtonSize + 4f, "TM_MasterSpellLocked".Translate().Length * 8, MagicCardUtility.TextSize * 3);
        //                    Widgets.Label(rectMasterLock, "TM_MasterSpellLocked".Translate(
        //                                power.abilityDef.LabelCap
        //                        ));
        //                }
        //                if (MagicPowerSkill4 == null && compMagic.Pawn.story.traits.HasTrait(TorannMagicDefOf.Technomancer))
        //                {
        //                    Rect rectTechnoPath = new Rect(rect.xMax - "TM_TechnomancerPathWarning".Translate().Length * 3, rect.yMin + (2*(MagicCardUtility.MagicButtonSize + 4f)), "TM_TechnomancerPathWarning".Translate().Length * 6, MagicCardUtility.TextSize * 3);
        //                    Widgets.Label(rectTechnoPath, "TM_TechnomancerPathWarning".Translate());
        //                }                       
        //            }

        //            Text.Font = GameFont.Tiny;
        //            float num2 = rect3.x;
        //            if (itnum == 1 && MagicPowerSkill1 != null)
        //            {
        //                DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill1, rect3);                        
        //                itnum++;
        //            }
        //            else if (itnum == 2 && MagicPowerSkill2 != null)
        //            {
        //                DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill2, rect3);
        //                itnum++;
        //            }
        //            else if (itnum == 3 && MagicPowerSkill3 != null)
        //            {
        //                DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill3, rect3);
        //                itnum++;
        //            }
        //            else if (itnum == 4 && MagicPowerSkill4 != null)
        //            {
        //                DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill4, rect3);
        //                itnum++;
        //            }
        //            else if (itnum == 5 && MagicPowerSkill5 != null)
        //            {
        //                DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill5, rect3);
        //                itnum++;
        //            }
        //            else if (itnum == 6 && MagicPowerSkill6 != null)
        //            {
        //                DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill6, rect3);
        //                itnum++;
        //            }
        //            else
        //            {
        //                //Log.Message("No skill iteration found.");
        //            }
        //            num += MagicCardUtility.MagicButtonSize + MagicCardUtility.TextSize + 4f;//MagicCardUtility.SpacingOffset; //was 4f                    
        //        }  
        //    }
        //}

        //Legacy skill handler TODO: REMOVE
        //public static void DrawSkillHandler(float num2, CompAbilityUserMagic compMagic, MagicPower power, List<MagicPower>.Enumerator enumerator, List<MagicPowerSkill> MagicPowerSkillN, Rect rect3)
        //{
        //    using (List<MagicPowerSkill>.Enumerator enumeratorN = MagicPowerSkillN.GetEnumerator())
        //    {
        //        while (enumeratorN.MoveNext())
        //        {
        //            Rect rect4 = new Rect(num2 + MagicCardUtility.MagicButtonPointSize, rect3.yMax, MagicCardUtility.MagicCardSize.x / 3f, rect3.height);
        //            Rect rect41 = new Rect(num2, rect4.y, MagicCardUtility.MagicButtonPointSize, MagicCardUtility.MagicButtonPointSize);
        //            Rect rect42 = new Rect(rect41.x, rect4.y, rect4.width - MagicCardUtility.MagicButtonPointSize, rect4.height / 2);
        //            MagicPowerSkill skill = enumeratorN.Current;
        //            TooltipHandler.TipRegion(rect42, new TipSignal(() => skill.desc.Translate(), rect4.GetHashCode()));
        //            bool flag11 = (skill.level >= skill.levelMax || compMagic.MagicData.MagicAbilityPoints == 0 || !enumerator.Current.learned) || 
        //                ((enumerator.Current.abilityDef == TorannMagicDefOf.TM_Shapeshift) && compMagic.MagicData.MagicAbilityPoints < 2) || 
        //                (skill.label == "TM_Polymorph_ver" && compMagic.MagicData.MagicAbilityPoints < 2) || 
        //                ((enumerator.Current.abilityDef ==  TorannMagicDefOf.TM_BardTraining) && compMagic.MagicData.MagicAbilityPoints < 2 ) || 
        //                ((skill.label == "TM_HolyWrath_ver" || skill.label == "TM_HolyWrath_pwr") && compMagic.MagicData.MagicAbilityPoints < 2) || 
        //                ((skill.label == "TM_Sentinel_pwr") && compMagic.MagicData.MagicAbilityPoints < 2) || 
        //                ((skill.label == "TM_EnchanterStone_ver") && compMagic.MagicData.MagicAbilityPoints < 2) ||
        //                ((skill.label == "TM_AlterFate_pwr") && compMagic.MagicData.MagicAbilityPoints < 2) ||
        //                ((enumerator.Current.abilityDef == TorannMagicDefOf.TM_ChaosTradition) && compMagic.MagicData.MagicAbilityPoints < 2 );
        //            if (flag11)
        //            {
        //                Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
        //            }
        //            else
        //            {
        //                bool flag12 = Widgets.ButtonText(rect41, "+", true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
        //                Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
        //                if (flag12)
        //                {
        //                    bool flag17 = compMagic.AbilityUser.story != null && compMagic.AbilityUser.story.DisabledWorkTagsBackstoryAndTraits == WorkTags.Violent && power.abilityDef.MainVerb.isViolent;
        //                    if (flag17)
        //                    {
        //                        Messages.Message("IsIncapableOfViolenceLower".Translate(
        //                            compMagic.parent.LabelShort
        //                        ), MessageTypeDefOf.RejectInput);
        //                        break;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_RayofHope" || enumerator.Current.abilityDef.defName == "TM_RayofHope_I" || enumerator.Current.abilityDef.defName == "TM_RayofHope_II" || enumerator.Current.abilityDef.defName == "TM_RayofHope_III")
        //                    {
        //                        compMagic.LevelUpSkill_RayofHope(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_P_RayofHope" || enumerator.Current.abilityDef.defName == "TM_P_RayofHope_I" || enumerator.Current.abilityDef.defName == "TM_P_RayofHope_II" || enumerator.Current.abilityDef.defName == "TM_P_RayofHope_III")
        //                    {
        //                        compMagic.LevelUpSkill_RayofHope(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Firebolt")
        //                    {
        //                        compMagic.LevelUpSkill_Firebolt(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Fireclaw")
        //                    {
        //                        compMagic.LevelUpSkill_Fireclaw(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Fireball")
        //                    {
        //                        compMagic.LevelUpSkill_Fireball(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Soothe" || enumerator.Current.abilityDef.defName == "TM_Soothe_I" || enumerator.Current.abilityDef.defName == "TM_Soothe_II" || enumerator.Current.abilityDef.defName == "TM_Soothe_III")
        //                    {
        //                        compMagic.LevelUpSkill_Soothe(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Icebolt")
        //                    {
        //                        compMagic.LevelUpSkill_Icebolt(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_FrostRay" || enumerator.Current.abilityDef.defName == "TM_FrostRay_I" || enumerator.Current.abilityDef.defName == "TM_FrostRay_II" || enumerator.Current.abilityDef.defName == "TM_FrostRay_III")
        //                    {
        //                        compMagic.LevelUpSkill_FrostRay(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Snowball")
        //                    {
        //                        compMagic.LevelUpSkill_Snowball(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Rainmaker")
        //                    {
        //                        compMagic.LevelUpSkill_Rainmaker(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_AMP" || enumerator.Current.abilityDef.defName == "TM_AMP_I" || enumerator.Current.abilityDef.defName == "TM_AMP_II" || enumerator.Current.abilityDef.defName == "TM_AMP_III")
        //                    {
        //                        compMagic.LevelUpSkill_AMP(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_LightningBolt")
        //                    {
        //                        compMagic.LevelUpSkill_LightningBolt(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_LightningCloud")
        //                    {
        //                        compMagic.LevelUpSkill_LightningCloud(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_LightningStorm")
        //                    {
        //                        compMagic.LevelUpSkill_LightningStorm(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Shadow" || enumerator.Current.abilityDef.defName == "TM_Shadow_I" || enumerator.Current.abilityDef.defName == "TM_Shadow_II" || enumerator.Current.abilityDef.defName == "TM_Shadow_III")
        //                    {
        //                        compMagic.LevelUpSkill_Shadow(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_MagicMissile" || enumerator.Current.abilityDef.defName == "TM_MagicMissile_I" || enumerator.Current.abilityDef.defName == "TM_MagicMissile_II" || enumerator.Current.abilityDef.defName == "TM_MagicMissile_III")
        //                    {
        //                        compMagic.LevelUpSkill_MagicMissile(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Blink" || enumerator.Current.abilityDef.defName == "TM_Blink_I" || enumerator.Current.abilityDef.defName == "TM_Blink_II" || enumerator.Current.abilityDef.defName == "TM_Blink_III")
        //                    {
        //                        compMagic.LevelUpSkill_Blink(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Summon" || enumerator.Current.abilityDef.defName == "TM_Summon_I" || enumerator.Current.abilityDef.defName == "TM_Summon_II" || enumerator.Current.abilityDef.defName == "TM_Summon_III")
        //                    {
        //                        compMagic.LevelUpSkill_Summon(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Teleport")
        //                    {
        //                        compMagic.LevelUpSkill_Teleport(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_FoldReality")
        //                    {
        //                        compMagic.LevelUpSkill_FoldReality(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Heal")
        //                    {
        //                        compMagic.LevelUpSkill_Heal(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Shield" || enumerator.Current.abilityDef.defName == "TM_Shield_I" || enumerator.Current.abilityDef.defName == "TM_Shield_II" || enumerator.Current.abilityDef.defName == "TM_Shield_III")
        //                    {
        //                        compMagic.LevelUpSkill_Shield(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ValiantCharge")
        //                    {
        //                        compMagic.LevelUpSkill_ValiantCharge(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Overwhelm")
        //                    {
        //                        compMagic.LevelUpSkill_Overwhelm(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_HolyWrath")
        //                    {
        //                        if(skill.label == "TM_HolyWrath_ver" || skill.label == "TM_HolyWrath_pwr")
        //                        {
        //                            compMagic.LevelUpSkill_Overwhelm(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 2;
        //                        }
        //                        else
        //                        {
        //                            compMagic.LevelUpSkill_Overwhelm(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 1;
        //                        }

        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Firestorm")
        //                    {
        //                        compMagic.LevelUpSkill_Firestorm(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Blizzard")
        //                    {
        //                        compMagic.LevelUpSkill_Blizzard(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SummonMinion")
        //                    {
        //                        compMagic.LevelUpSkill_SummonMinion(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SummonPylon")
        //                    {
        //                        compMagic.LevelUpSkill_SummonPylon(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SummonExplosive")
        //                    {
        //                        compMagic.LevelUpSkill_SummonExplosive(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SummonElemental")
        //                    {
        //                        compMagic.LevelUpSkill_SummonElemental(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SummonPoppi")
        //                    {
        //                        compMagic.LevelUpSkill_SummonPoppi(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Poison")
        //                    {
        //                        compMagic.LevelUpSkill_Poison(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SootheAnimal" || enumerator.Current.abilityDef.defName == "TM_SootheAnimal_I" || enumerator.Current.abilityDef.defName == "TM_SootheAnimal_II" || enumerator.Current.abilityDef.defName == "TM_SootheAnimal_III")
        //                    {
        //                        compMagic.LevelUpSkill_SootheAnimal(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Regenerate")
        //                    {
        //                        compMagic.LevelUpSkill_Regenerate(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_CureDisease")
        //                    {
        //                        compMagic.LevelUpSkill_CureDisease(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_RegrowLimb")
        //                    {
        //                        compMagic.LevelUpSkill_RegrowLimb(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_EyeOfTheStorm")
        //                    {
        //                        compMagic.LevelUpSkill_EyeOfTheStorm(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_RaiseUndead")
        //                    {
        //                        compMagic.LevelUpSkill_RaiseUndead(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_DeathMark" || enumerator.Current.abilityDef.defName == "TM_DeathMark_I" || enumerator.Current.abilityDef.defName == "TM_DeathMark_II" || enumerator.Current.abilityDef.defName == "TM_DeathMark_III")
        //                    {
        //                        compMagic.LevelUpSkill_DeathMark(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_FogOfTorment")
        //                    {
        //                        compMagic.LevelUpSkill_FogOfTorment(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ConsumeCorpse" || enumerator.Current.abilityDef.defName == "TM_ConsumeCorpse_I" || enumerator.Current.abilityDef.defName == "TM_ConsumeCorpse_II" || enumerator.Current.abilityDef.defName == "TM_ConsumeCorpse_III")
        //                    {
        //                        compMagic.LevelUpSkill_ConsumeCorpse(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_CorpseExplosion" || enumerator.Current.abilityDef.defName == "TM_CorpseExplosion_I" || enumerator.Current.abilityDef.defName == "TM_CorpseExplosion_II" || enumerator.Current.abilityDef.defName == "TM_CorpseExplosion_III")
        //                    {
        //                        compMagic.LevelUpSkill_CorpseExplosion(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_DeathBolt" || enumerator.Current.abilityDef.defName == "TM_DeathBolt_I" || enumerator.Current.abilityDef.defName == "TM_DeathBolt_II" || enumerator.Current.abilityDef.defName == "TM_DeathBolt_III")
        //                    {
        //                        compMagic.LevelUpSkill_DeathBolt(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_AdvancedHeal")
        //                    {
        //                        compMagic.LevelUpSkill_AdvancedHeal(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Purify")
        //                    {
        //                        compMagic.LevelUpSkill_Purify(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_HealingCircle" || enumerator.Current.abilityDef.defName == "TM_HealingCircle_I" || enumerator.Current.abilityDef.defName == "TM_HealingCircle_II" || enumerator.Current.abilityDef.defName == "TM_HealingCircle_III")
        //                    {
        //                        compMagic.LevelUpSkill_HealingCircle(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BestowMight" || enumerator.Current.abilityDef.defName == "TM_BestowMight_I" || enumerator.Current.abilityDef.defName == "TM_BestowMight_II" || enumerator.Current.abilityDef.defName == "TM_BestowMight_III")
        //                    {
        //                        compMagic.LevelUpSkill_BestowMight(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Resurrection")
        //                    {
        //                        compMagic.LevelUpSkill_Resurrection(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BardTraining")
        //                    {
        //                        compMagic.LevelUpSkill_BardTraining(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 2;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Entertain")
        //                    {
        //                        compMagic.LevelUpSkill_Entertain(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Inspire")
        //                    {
        //                        compMagic.LevelUpSkill_Inspire(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Lullaby" || enumerator.Current.abilityDef.defName == "TM_Lullaby_I" || enumerator.Current.abilityDef.defName == "TM_Lullaby_II" || enumerator.Current.abilityDef.defName == "TM_Lullaby_III")
        //                    {
        //                        compMagic.LevelUpSkill_Lullaby(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BattleHymn")
        //                    {
        //                        compMagic.LevelUpSkill_BattleHymn(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_SoulBond")
        //                    {
        //                        compMagic.LevelUpSkill_SoulBond(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ShadowBolt" || enumerator.Current.abilityDef.defName == "TM_ShadowBolt_I" || enumerator.Current.abilityDef.defName == "TM_ShadowBolt_II" || enumerator.Current.abilityDef.defName == "TM_ShadowBolt_III")
        //                    {
        //                        compMagic.LevelUpSkill_ShadowBolt(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Dominate")
        //                    {
        //                        compMagic.LevelUpSkill_Dominate(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Attraction" || enumerator.Current.abilityDef.defName == "TM_Attraction_I" || enumerator.Current.abilityDef.defName == "TM_Attraction_II" || enumerator.Current.abilityDef.defName == "TM_Attraction_III")
        //                    {
        //                        compMagic.LevelUpSkill_Attraction(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Repulsion" || enumerator.Current.abilityDef.defName == "TM_Repulsion_I" || enumerator.Current.abilityDef.defName == "TM_Repulsion_II" || enumerator.Current.abilityDef.defName == "TM_Repulsion_III")
        //                    {
        //                        compMagic.LevelUpSkill_Repulsion(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Scorn")
        //                    {
        //                        compMagic.LevelUpSkill_Scorn(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_PsychicShock")
        //                    {
        //                        compMagic.LevelUpSkill_PsychicShock(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Stoneskin")
        //                    {
        //                        compMagic.LevelUpSkill_Stoneskin(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Encase" || enumerator.Current.abilityDef.defName == "TM_Encase_I" || enumerator.Current.abilityDef.defName == "TM_Encase_II" || enumerator.Current.abilityDef.defName == "TM_Encase_III")
        //                    {
        //                        compMagic.LevelUpSkill_Encase(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_EarthSprites")
        //                    {
        //                        compMagic.LevelUpSkill_EarthSprites(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }                            
        //                    if (enumerator.Current.abilityDef.defName == "TM_EarthernHammer")
        //                    {
        //                        compMagic.LevelUpSkill_EarthernHammer(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Sentinel")
        //                    {
        //                        if (skill.label == "TM_Sentinel_pwr")
        //                        {
        //                            compMagic.LevelUpSkill_Sentinel(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 2;
        //                        }
        //                        else
        //                        {
        //                            compMagic.LevelUpSkill_Sentinel(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 1;
        //                        }
                                
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Meteor" || enumerator.Current.abilityDef.defName == "TM_Meteor_I" || enumerator.Current.abilityDef.defName == "TM_Meteor_II" || enumerator.Current.abilityDef.defName == "TM_Meteor_III")
        //                    {
        //                        compMagic.LevelUpSkill_Meteor(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_TechnoBit")
        //                    {
        //                        compMagic.LevelUpSkill_TechnoBit(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_TechnoTurret")
        //                    {
        //                        compMagic.LevelUpSkill_TechnoTurret(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_TechnoWeapon")
        //                    {
        //                        compMagic.LevelUpSkill_TechnoWeapon(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_TechnoShield")
        //                    {
        //                        compMagic.LevelUpSkill_TechnoShield(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Sabotage")
        //                    {
        //                        compMagic.LevelUpSkill_Sabotage(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Overdrive")
        //                    {
        //                        compMagic.LevelUpSkill_Overdrive(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_OrbitalStrike" || enumerator.Current.abilityDef.defName == "TM_OrbitalStrike_I" || enumerator.Current.abilityDef.defName == "TM_OrbitalStrike_II" || enumerator.Current.abilityDef.defName == "TM_OrbitalStrike_III")
        //                    {
        //                        compMagic.LevelUpSkill_OrbitalStrike(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }

        //                    if (enumerator.Current.abilityDef.defName == "TM_BloodGift")
        //                    {
        //                        compMagic.LevelUpSkill_BloodGift(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_IgniteBlood")
        //                    {
        //                        compMagic.LevelUpSkill_IgniteBlood(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BloodForBlood")
        //                    {
        //                        compMagic.LevelUpSkill_BloodForBlood(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BloodShield")
        //                    {
        //                        compMagic.LevelUpSkill_BloodShield(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Rend" || enumerator.Current.abilityDef.defName == "TM_Rend_I" || enumerator.Current.abilityDef.defName == "TM_Rend_II" || enumerator.Current.abilityDef.defName == "TM_Rend_III")
        //                    {
        //                        compMagic.LevelUpSkill_Rend(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_BloodMoon" || enumerator.Current.abilityDef.defName == "TM_BloodMoon_I" || enumerator.Current.abilityDef.defName == "TM_BloodMoon_II" || enumerator.Current.abilityDef.defName == "TM_BloodMoon_III")
        //                    {
        //                        compMagic.LevelUpSkill_BloodMoon(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }

        //                    if (enumerator.Current.abilityDef.defName == "TM_EnchantedBody")
        //                    {
        //                        compMagic.LevelUpSkill_EnchantedBody(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Transmutate")
        //                    {
        //                        compMagic.LevelUpSkill_Transmutate(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_EnchanterStone")
        //                    {
        //                        if (skill.label == "TM_EnchanterStone_ver")
        //                        {
        //                            compMagic.LevelUpSkill_Sentinel(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 2;
        //                        }
        //                        else
        //                        {
        //                            compMagic.LevelUpSkill_EnchanterStone(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 1;
        //                        }                                
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_EnchantWeapon")
        //                    {
        //                        compMagic.LevelUpSkill_EnchantWeapon(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Polymorph" || enumerator.Current.abilityDef.defName == "TM_Polymorph_I" || enumerator.Current.abilityDef.defName == "TM_Polymorph_II" || enumerator.Current.abilityDef.defName == "TM_Polymorph_III")
        //                    {
        //                        if (skill.label == "TM_Polymorph_ver")
        //                        {
        //                            compMagic.LevelUpSkill_Sentinel(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 2;
        //                        }
        //                        else
        //                        {
        //                            compMagic.LevelUpSkill_Sentinel(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 1;
        //                        }
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Shapeshift")
        //                    {
        //                        compMagic.LevelUpSkill_Shapeshift(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 2;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Prediction")
        //                    {
        //                        compMagic.LevelUpSkill_Prediction(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_AlterFate")
        //                    {
        //                        if (skill.label == "TM_AlterFate_pwr")
        //                        {
        //                            compMagic.LevelUpSkill_AlterFate(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 2;
        //                        }
        //                        else
        //                        {
        //                            compMagic.LevelUpSkill_AlterFate(skill.label);
        //                            skill.level++;
        //                            compMagic.MagicData.MagicAbilityPoints -= 1;
        //                        }                                
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_AccelerateTime")
        //                    {
        //                        compMagic.LevelUpSkill_AccelerateTime(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ReverseTime")
        //                    {
        //                        compMagic.LevelUpSkill_ReverseTime(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ChronostaticField" || enumerator.Current.abilityDef.defName == "TM_ChronostaticField_I" || enumerator.Current.abilityDef.defName == "TM_ChronostaticField_II" || enumerator.Current.abilityDef.defName == "TM_ChronostaticField_III")
        //                    {
        //                        compMagic.LevelUpSkill_ChronostaticField(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Recall")
        //                    {
        //                        compMagic.LevelUpSkill_Recall(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_WandererCraft")
        //                    {
        //                        compMagic.LevelUpSkill_Recall(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Cantrips")
        //                    {
        //                        compMagic.LevelUpSkill_Recall(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_ChaosTradition")
        //                    {
        //                        compMagic.LevelUpSkill_ChaosTradition(skill.label);
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 2;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_LightLance")
        //                    {                                
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                    if (enumerator.Current.abilityDef.defName == "TM_Sunfire" || enumerator.Current.abilityDef.defName == "TM_Sunfire_I" || enumerator.Current.abilityDef.defName == "TM_Sunfire_II" || enumerator.Current.abilityDef.defName == "TM_Sunfire_III")
        //                    {
        //                        skill.level++;
        //                        compMagic.MagicData.MagicAbilityPoints -= 1;
        //                    }
        //                }
        //            }
        //            num2 += (MagicCardUtility.MagicCardSize.x / 3) - MagicCardUtility.SpacingOffset;
        //        }                
        //    }
        //}

        //public static void PowersGUIHandler_CM(Rect inRect, CompAbilityUserMagic compMagic, List<MagicPower> MagicPowers, List<MagicPowerSkill> MagicPowerSkill1, List<MagicPowerSkill> MagicPowerSkill2, List<MagicPowerSkill> MagicPowerSkill3, List<MagicPowerSkill> MagicPowerSkill4, List<MagicPowerSkill> MagicPowerSkill5, List<MagicPowerSkill> MagicPowerSkill6, Texture2D pointTexture)
        //{
        //    float num = inRect.y;
        //    int itnum = 1;
        //    bool flag999;
        //    using (List<MagicPower>.Enumerator enumerator = MagicPowers.GetEnumerator())
        //    {
        //        EnumerationStart:;
        //        while (enumerator.MoveNext())
        //        {
        //            MagicPower power = enumerator.Current;                    
        //            if (power == null || !power.learned)
        //            {
        //                goto EnumerationStart;
        //            }

        //            Text.Font = GameFont.Small;
        //            Rect rect = new Rect(MagicCardUtility.MagicCardSize.x / 2f - MagicCardUtility.MagicButtonSize, num, MagicCardUtility.MagicButtonSize, MagicCardUtility.MagicButtonSize);
        //            if (itnum > 1)
        //            {
        //                Widgets.DrawLineHorizontal(0f + 20f, rect.y - 2f, 700f - 40f);
        //            }
        //            if (power.level < power.maxLevel && (power.TMabilityDefs.Count > 1 || TM_Calc.IsIconAbility_02(power.abilityDef)))
        //            {
        //                TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
        //                {
        //                power.abilityDef.label,
        //                "\n\nCurrent Level:\n",
        //                power.abilityDescDef.description,
        //                "\n\nNext Level:\n",
        //                power.nextLevelAbilityDescDef?.description,
        //                "\n\n",
        //                "TM_CheckPointsForMoreInfo".Translate()
        //                }), 398462);

        //            }
        //            else
        //            {
        //                TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
        //                    {
        //                    power.abilityDef.label,
        //                    "\n\n",
        //                    power.abilityDescDef.description,
        //                    "\n\n",
        //                    "TM_CheckPointsForMoreInfo".Translate()
        //                    }), 398462);
        //            }

        //            float x2 = Text.CalcSize("TM_Effeciency".Translate()).x;
        //            float x3 = Text.CalcSize("TM_Versatility".Translate()).x;
        //            Rect rect3 = new Rect(0f + MagicCardUtility.SpacingOffset, rect.y + 2f, MagicCardUtility.MagicCardSize.x, MagicCardUtility.ButtonSize * 1.15f);

        //            Rect rect5 = new Rect(rect3.x + rect3.width / 2f - x2, rect3.y, (rect3.width - 20f) / 3f, rect3.height);
        //            Rect rect6 = new Rect(rect3.width - x3 * 2f, rect3.y, rect3.width / 3f, rect3.height);

        //            float x4 = Text.CalcSize(" # / # ").x;

        //            if (power.TMabilityDefs.Count > 1 || TM_Calc.IsIconAbility_03(power.abilityDef))
        //            {
        //                flag999 = true;
        //            }
        //            else
        //            {
        //                flag999 = false;
        //            }

        //            bool flag10 = enumerator.Current.level >= enumerator.Current.maxLevel || compMagic.MagicData.MagicAbilityPoints < enumerator.Current.costToLevel;
        //            if (flag10)
        //            {
        //                if (flag999)
        //                {
        //                    Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                    Rect rect19 = new Rect(rect.xMax, rect.yMin, x4, MagicCardUtility.TextSize);
        //                    Widgets.Label(rect19, " " + enumerator.Current.level + " / " + enumerator.Current.maxLevel);
        //                }
        //                else
        //                {
        //                    Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                }
        //            }
        //            else
        //            {
        //                if (flag999)
        //                {
        //                    Rect rect10 = new Rect(rect.xMax, rect.yMin, x4, MagicCardUtility.TextSize);
        //                    bool flag1 = Widgets.ButtonImage(rect, power.Icon) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
        //                    Widgets.Label(rect10, " " + power.level + " / " + enumerator.Current.maxLevel);
        //                    if (flag1)
        //                    {
        //                        compMagic.LevelUpPower(power);
        //                        compMagic.MagicData.MagicAbilityPoints -= power.costToLevel;
        //                    }
        //                }
        //                else
        //                {
        //                    Widgets.DrawTextureFitted(rect, power.Icon, 1f);
        //                }
        //            }

        //            //Text.Font = GameFont.Tiny;
        //            //float num2 = rect3.x;
        //            //if (itnum == 1 && MagicPowerSkill1 != null)
        //            //{
        //            //    MagicCardUtility.DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill1, rect3);
        //            //    itnum++;
        //            //}
        //            //else if (itnum == 2 && MagicPowerSkill2 != null)
        //            //{
        //            //    MagicCardUtility.DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill2, rect3);
        //            //    itnum++;
        //            //}
        //            //else if (itnum == 3 && MagicPowerSkill3 != null)
        //            //{
        //            //    MagicCardUtility.DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill3, rect3);
        //            //    itnum++;
        //            //}
        //            //else if (itnum == 4 && MagicPowerSkill4 != null)
        //            //{
        //            //    MagicCardUtility.DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill4, rect3);
        //            //    itnum++;
        //            //}
        //            //else if (itnum == 5 && MagicPowerSkill5 != null)
        //            //{
        //            //    MagicCardUtility.DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill5, rect3);
        //            //    itnum++;
        //            //}
        //            //else if (itnum == 6 && MagicPowerSkill6 != null)
        //            //{
        //            //    MagicCardUtility.DrawSkillHandler(num2, compMagic, power, enumerator, MagicPowerSkill6, rect3);
        //            //    itnum++;
        //            //}
        //            //else
        //            //{
        //            //    //Log.Message("No skill iteration found.");
        //            //}
        //            TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
        //            Text.Font = GameFont.Tiny;
        //            float num2 = rect3.x;
        //            List<MagicPowerSkill> mpsList = new List<MagicPowerSkill>();
        //            mpsList.Clear();

        //            MagicPowerSkill mps = compMagic.MagicData.GetSkill_Power(ability);
        //            if (mps != null)
        //            {
        //                mpsList.Add(mps);
        //            }
        //            mps = compMagic.MagicData.GetSkill_Efficiency(ability);
        //            if (mps != null)
        //            {
        //                mpsList.Add(mps);
        //            }
        //            mps = compMagic.MagicData.GetSkill_Versatility(ability);
        //            if (mps != null)
        //            {
        //                mpsList.Add(mps);
        //            }

        //            if (mpsList.Count > 0)
        //            {
        //                CustomSkillHandler(num2, compMagic, power, enumerator, mpsList, rect3);
        //                itnum++;
        //            }
      
        //            num += MagicCardUtility.MagicButtonSize + MagicCardUtility.TextSize + 4f;//MagicCardUtility.SpacingOffset; //was 4f                    
        //        }
        //    }
        //}

        public static void CustomPowersHandler(Rect inRect, CompAbilityUserMagic compMagic, List<MagicPower> MagicPowers, List<TMAbilityDef> abilityList, Texture2D pointTexture)
        {
            float num = inRect.y;
            int itnum = 1;
            bool flag999;
            using (List<MagicPower>.Enumerator enumerator = MagicPowers.GetEnumerator())
            {
                EnumerationStart:;
                while (enumerator.MoveNext())
                {
                    MagicPower power = enumerator.Current;
                    TMAbilityDef ability = (TMAbilityDef)power.abilityDef;
                    if (!abilityList.Contains(ability))
                    {
                        goto EnumerationStart;
                    }
                    if (compMagic.Pawn.story.traits.HasTrait(TorannMagicDefOf.Warlock))
                    {
                        if (power == compMagic.MagicData.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                             power == compMagic.MagicData.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                             power == compMagic.MagicData.MagicPowersSD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate))
                        {
                            goto EnumerationStart;
                        }
                    }
                    else if (compMagic.Pawn.story.traits.HasTrait(TorannMagicDefOf.Succubus))
                    {
                        if (power == compMagic.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_SoulBond) ||
                             power == compMagic.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_ShadowBolt) ||
                             power == compMagic.MagicData.MagicPowersWD.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_Dominate))
                        {
                            goto EnumerationStart;
                        }
                    }

                    Text.Font = GameFont.Small;
                    Rect rect = new Rect(MagicCardUtility.MagicCardSize.x / 2f - MagicCardUtility.MagicButtonSize, num, MagicCardUtility.MagicButtonSize, MagicCardUtility.MagicButtonSize);
                    if (itnum > 1)
                    {
                        Widgets.DrawLineHorizontal(0f + 20f, rect.y - 2f, MagicCardUtility.MagicCardSize.x - 40f);
                    }
                    if (power.level < power.maxLevel && (power.TMabilityDefs.Count > 1 || TM_Calc.IsIconAbility_02(power.abilityDef)))
                    {
                        TooltipHandler.TipRegion(rect, () => string.Concat(new string[]
                        {
                        power.abilityDef.label,
                        "\n\nCurrent Level:\n",
                        power.abilityDescDef.description,
                        "\n\nNext Level:\n",
                        power.nextLevelAbilityDescDef?.description,
                        "\n\n",
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
                            "TM_CheckPointsForMoreInfo".Translate()
                            }), 398462);
                    }

                    float x2 = Text.CalcSize("TM_Effeciency".Translate()).x;
                    float x3 = Text.CalcSize("TM_Versatility".Translate()).x;
                    Rect rect3 = new Rect(0f + MagicCardUtility.SpacingOffset, rect.y + 2f, MagicCardUtility.MagicCardSize.x, MagicCardUtility.ButtonSize * 1.15f);

                    Rect rect5 = new Rect(rect3.x + rect3.width / 2f - x2, rect3.y, (rect3.width - 20f) / 3f, rect3.height);
                    Rect rect6 = new Rect(rect3.width - x3 * 2f, rect3.y, rect3.width / 3f, rect3.height);

                    float x4 = Text.CalcSize(" # / # ").x;
                    //bool flag9 = power.abilityDef.label == "Ray of Hope" || power.abilityDef.label == "Soothing Breeze" || power.abilityDef.label == "Frost Ray" || power.abilityDef.label == "AMP" || power.abilityDef.label == "Shadow" || power.abilityDef.label == "Magic Missile" || power.abilityDef.label == "Blink" || power.abilityDef.label == "Summon" || power.abilityDef.label == "Shield"; //add all other buffs or xml based upgrades

                    if (power.TMabilityDefs.Count > 1 || TM_Calc.IsIconAbility_03(power.abilityDef))
                    {
                        flag999 = true;
                    }
                    else
                    {
                        flag999 = false;
                    }
                    Rect rectLabel = new Rect(0f + 20f, rect.yMin, 350f - 44f, MagicCardUtility.MagicButtonPointSize);
                    //GUI.color = Color.yellow;
                    Widgets.Label(rectLabel, power.abilityDef.LabelCap);
                    //GUI.color = Color.white;
                    if (!power.learned)
                    {
                        Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                        Rect rectLearn = new Rect(rect.xMin - 44f, rect.yMin, 40f, MagicCardUtility.MagicButtonPointSize);
                        if ((compMagic.MagicData.MagicAbilityPoints >= power.learnCost) && !power.requiresScroll)
                        {
                            Text.Font = GameFont.Tiny;
                            bool flagLearn = Widgets.ButtonText(rectLearn, "TM_Learn".Translate(), true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                            if (flagLearn)
                            {
                                enumerator.Current.learned = true;
                                TMAbilityDef abilityLearned = (TMAbilityDef)enumerator.Current.abilityDef;
                                if (!(enumerator.Current.abilityDef.defName == "TM_TechnoBit") && abilityLearned.shouldInitialize)
                                {
                                    compMagic.AddPawnAbility(enumerator.Current.abilityDef);
                                }
                                if ((enumerator.Current.abilityDef.defName == "TM_TechnoWeapon"))
                                {
                                    compMagic.AddPawnAbility(TorannMagicDefOf.TM_NanoStimulant);
                                    compMagic.MagicData.MagicPowersStandalone.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_NanoStimulant).learned = true;
                                }
                                if(abilityLearned.childAbilities != null && abilityLearned.childAbilities.Count > 0)
                                {
                                    for (int c = 0; c < abilityLearned.childAbilities.Count; c++)
                                    {
                                        if (abilityLearned.childAbilities[c].shouldInitialize)
                                        {
                                            compMagic.AddPawnAbility(abilityLearned.childAbilities[c]);
                                        }
                                    }
                                }
                                compMagic.MagicData.MagicAbilityPoints -= enumerator.Current.learnCost;
                            }
                        }
                        else if(power.requiresScroll)
                        {
                            Rect rectToLearn = new Rect(rect.xMin - 268f, rect.yMin + 22f, 250f, MagicButtonPointSize);
                            Text.Font = GameFont.Tiny;
                            bool flagLearn = Widgets.ButtonText(rectToLearn, "TM_SpellLocked".Translate(power.abilityDef.LabelCap), false, false, false) && compMagic.AbilityUser.Faction == Faction.OfPlayer;                            
                        }
                        else
                        {
                            Rect rectToLearn = new Rect(rect.xMin - 98f, rect.yMin, 100f, MagicButtonPointSize);
                            Text.Font = GameFont.Tiny;
                            bool flagLearn = Widgets.ButtonText(rectToLearn, "" + enumerator.Current.learnCost + " points to " + "TM_Learn".Translate(), false, false, false) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                        }
                    }
                    else
                    {
                        bool flag10 = enumerator.Current.level >= power.maxLevel || compMagic.MagicData.MagicAbilityPoints < power.costToLevel;
                        if (flag10)
                        {
                            if (flag999)
                            {
                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                                Rect rect19 = new Rect(rect.xMax, rect.yMin, x4, MagicCardUtility.TextSize);
                                Widgets.Label(rect19, " " + enumerator.Current.level + " / " + enumerator.Current.maxLevel);                                
                            }
                            else
                            {
                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                            }
                        }
                        else
                        {
                            if (flag999)
                            {
                                Rect rect10 = new Rect(rect.xMax, rect.yMin, x4, MagicCardUtility.TextSize);
                                bool flag1 = Widgets.ButtonImage(rect, power.Icon) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                                Widgets.Label(rect10, " " + power.level + " / " + power.maxLevel);
                                if (flag1)
                                {
                                    compMagic.LevelUpPower(power);
                                    compMagic.MagicData.MagicAbilityPoints -= power.costToLevel;
                                }                                
                            }
                            else
                            {
                                Widgets.DrawTextureFitted(rect, power.Icon, 1f);
                            }
                        }
                    }

                    Text.Font = GameFont.Tiny;
                    float num2 = rect3.x;
                    List<MagicPowerSkill> mpsList = new List<MagicPowerSkill>();
                    mpsList.Clear();

                    MagicPowerSkill mps = compMagic.MagicData.GetSkill_Power(ability);
                    if (mps != null)
                    {
                        mpsList.Add(mps);
                    }
                    mps = compMagic.MagicData.GetSkill_Efficiency(ability);
                    if (mps != null)
                    {
                        mpsList.Add(mps);
                    }
                    mps = compMagic.MagicData.GetSkill_Versatility(ability);
                    if (mps != null)
                    {
                        mpsList.Add(mps);
                    }

                    if (mpsList.Count > 0)
                    {
                        CustomSkillHandler(num2, compMagic, power, enumerator, mpsList, rect3);                        
                    }
                    itnum++;
                    num += MagicCardUtility.MagicButtonSize + MagicCardUtility.TextSize + 4f;//MagicCardUtility.SpacingOffset; //was 4f                    
                }
            }
        }

        public static void CustomSkillHandler(float num2, CompAbilityUserMagic compMagic, MagicPower power, List<MagicPower>.Enumerator enumerator, List<MagicPowerSkill> MagicPowerSkillN, Rect rect3)
        {
            using (List<MagicPowerSkill>.Enumerator enumeratorN = MagicPowerSkillN.GetEnumerator())
            {
                while (enumeratorN.MoveNext())
                {
                    Rect rect4 = new Rect(num2 + MagicCardUtility.MagicButtonPointSize, rect3.yMax, MagicCardUtility.MagicCardSize.x / 3f, rect3.height);
                    Rect rect41 = new Rect(num2, rect4.y, MagicCardUtility.MagicButtonPointSize, MagicCardUtility.MagicButtonPointSize);
                    Rect rect42 = new Rect(rect41.x, rect4.y, rect4.width - MagicCardUtility.MagicButtonPointSize, rect4.height / 2);
                    MagicPowerSkill skill = enumeratorN.Current;
                    TooltipHandler.TipRegion(rect42, new TipSignal(() => skill.desc.Translate(), rect4.GetHashCode()));
                    bool flag11 = (skill.level >= skill.levelMax || compMagic.MagicData.MagicAbilityPoints == 0 || !enumerator.Current.learned ||
                        (skill.costToLevel > compMagic.MagicData.MagicAbilityPoints));
                    if (flag11)
                    {
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                    }
                    else
                    {
                        bool flag12 = Widgets.ButtonText(rect41, "+", true, false, true) && compMagic.AbilityUser.Faction == Faction.OfPlayer;
                        Widgets.Label(rect4, skill.label.Translate() + ": " + skill.level + " / " + skill.levelMax);
                        if (flag12)
                        {                            
                            bool flag17 = compMagic.AbilityUser.story != null && compMagic.AbilityUser.story.DisabledWorkTagsBackstoryAndTraits == WorkTags.Violent && power.abilityDef.MainVerb.isViolent;
                            if (flag17)
                            {
                                Messages.Message("IsIncapableOfViolenceLower".Translate(
                                    compMagic.parent.LabelShort
                                ), MessageTypeDefOf.RejectInput);
                                break;
                            }
                            skill.level++;
                            compMagic.MagicData.MagicAbilityPoints -= skill.costToLevel;
                            if (skill.label == "TM_LightSkip_pwr")
                            {
                                if (skill.level == 1)
                                {
                                    compMagic.AddPawnAbility(TorannMagicDefOf.TM_LightSkipMass);
                                }
                                if (skill.level == 2)
                                {
                                    compMagic.AddPawnAbility(TorannMagicDefOf.TM_LightSkipGlobal);
                                }
                            }
                        }
                    }
                    num2 += (MagicCardUtility.MagicCardSize.x / 3) - MagicCardUtility.SpacingOffset;
                }
            }
        }

    }
}