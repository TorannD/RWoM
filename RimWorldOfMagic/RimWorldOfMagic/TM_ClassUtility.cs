using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using RimWorld;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using AbilityUser;
using TorannMagic.Enchantment;
using System.Text;
using TorannMagic.TMDefs;
using TorannMagic.ModOptions;

namespace TorannMagic
{
    public static class TM_ClassUtility
    {
        // Since these should not be modified during gameplay except in extraordinary exceptions, use arrays for faster access
        // I strongly suspect that several of these should not even be arrays, but rather HashSets.
        public static TM_CustomClass[] CustomClasses;
        public static readonly Dictionary<ushort, TM_CustomClass> CustomAdvancedClassTraitIndexMap = new Dictionary<ushort, TM_CustomClass>();
        public static TM_CustomClass[] CustomBaseClasses;
        public static TM_CustomClass[] CustomMageClasses;
        public static TM_CustomClass[] CustomFighterClasses;

        public static void LoadCustomClasses()
        {
            CustomClasses = TM_CustomClassDef.Named("TM_CustomClasses").customClasses.ToArray();
            CustomAdvancedClassTraitIndexMap.Clear();
            var CustomBaseClassesList = new List<TM_CustomClass>();
            var CustomMageClassesList = new List<TM_CustomClass>();
            var CustomFighterClassesList = new List<TM_CustomClass>();

            foreach (TM_CustomClass cc in CustomClasses.Where(cc => Settings.Instance.CustomClass[cc.classTrait.ToString()]))
            {
                if (cc.isMage)
                {
                    if (cc.isAdvancedClass)
                    {
                        if (cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                        {
                            CustomMageClassesList.Add(cc);
                            CustomAdvancedClassTraitIndexMap[cc.classTrait.index] = cc;
                        }
                    }
                    else
                    {
                        CustomMageClassesList.Add(cc);
                    }
                }
                if (cc.isFighter)
                {
                    if (cc.isAdvancedClass)
                    {
                        if (cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                        {
                            CustomFighterClassesList.Add(cc);
                            CustomAdvancedClassTraitIndexMap[cc.classTrait.index] = cc;
                        }
                    }
                    else
                    {
                        CustomFighterClassesList.Add(cc);
                    }
                }
                if (!cc.isAdvancedClass) CustomBaseClassesList.Add(cc); //base classes cannot also be advanced classes, but advanced classes can act like base classes
                else CustomAdvancedClassTraitIndexMap[cc.classTrait.index] = cc;
            }

            CustomBaseClasses = CustomBaseClassesList.ToArray();
            CustomMageClasses = CustomMageClassesList.ToArray();
            CustomFighterClasses = CustomFighterClassesList.ToArray();

            CustomClassTraitIndexes = new Dictionary<ushort, int>();
            for (int i = 0; i < CustomClasses.Length; i++)
            {
                CustomClassTraitIndexes[CustomClasses[i].classTrait.index] = i;
            }
        }

        public static List<TraitDef> CustomClassTraitDefs
        {
            get => CustomClasses.Select(t => t.classTrait).ToList();
        }

        private static Dictionary<ushort, int> CustomClassTraitIndexes;  // Dictionary to more quickly determine trait's CustomClasses index

        public static int IsCustomClassIndex(List<Trait> allTraits)
        {
            if (allTraits == null || allTraits.Count <= 0) return -2;
            try
            {
                for (int i = 0; i < allTraits.Count; i++)
                {
                    int index = CustomClassTraitIndexes.TryGetValue(allTraits[i].def.index, -1);
                    if (index != -1)
                    {
                        return index;
                    }
                }
                return -2;
            }
            catch (NullReferenceException)
            {
                LoadCustomClasses();  // Will ALWAYS set CustomClassTraitIndexes
                return IsCustomClassIndex(allTraits);
            }
        }

        public static int CustomClassIndexOfBaseMageClass(List<Trait> allTraits)
        {
            for (int i = 0; i < CustomClasses.Length; i++)
            {
                if (CustomClasses[i].isAdvancedClass) continue;
                if (!CustomClasses[i].isMage) continue;
                for (int j = 0; j < allTraits.Count; j++)
                {
                    if (allTraits[j].def == CustomClasses[i].classTrait)
                    {
                        return i;
                    }
                }
            }
            return -2;
        }

        public static int CustomClassIndexOfBaseFighterClass(List<Trait> allTraits)
        {
            for(int i = 0; i < CustomClasses.Length; i++)
            {
                if (CustomClasses[i].isAdvancedClass) continue;
                if (!CustomClasses[i].isFighter) continue;
                for(int j = 0; j < allTraits.Count; j++)
                {
                    if(allTraits[j].def == CustomClasses[i].classTrait)
                    {
                        return i;
                    }
                }
            }
            return -2;
        }

        public static List<HediffDef> CustomClassHediffs()
        {
            List<HediffDef> hList = new List<HediffDef>();
            foreach(TM_CustomClass cc in CustomClasses)
            {
                if(cc.classHediff != null)
                {
                    hList.Add(cc.classHediff);
                }
            }
            return hList;
        }

        public static TM_CustomClass GetCustomClassOfTrait(TraitDef td)
        {
            return CustomClasses.FirstOrDefault(customClass => customClass.classTrait == td);
        }

        public static List<MagicPowerSkill> GetAssociatedMagicPowerSkill(CompAbilityUserMagic comp, MagicPower power)
        {
            string str = power.TMabilityDefs.First().defName + "_";
            List<MagicPowerSkill> skills = new List<MagicPowerSkill>();
            for (int i = 0; i < comp.MagicData.AllMagicPowerSkills.Count; i++)
            {
                MagicPowerSkill mps = comp.MagicData.AllMagicPowerSkills[i];
                if (mps.label.Contains(str))
                {
                    skills.Add(mps);
                }
            }
            return skills;
        }

        public static List<MightPowerSkill> GetAssociatedMightPowerSkill(CompAbilityUserMight comp, TMAbilityDef abilityDef, string var)
        {
            string str = abilityDef.defName + "_" + var;
            List<MightPowerSkill> skills = new List<MightPowerSkill>();
            for (int i = 0; i < comp.MightData.AllMightPowerSkills.Count; i++)
            {
                MightPowerSkill mps = comp.MightData.AllMightPowerSkills[i];
                if (mps.label.Contains(str))
                {
                    skills.Add(mps);
                }
            }
            return skills;
        }

        public static MightPowerSkill GetMightPowerSkillFromLabel(CompAbilityUserMight comp, string label)
        {
            if(comp != null && comp.MightData != null && comp.MightData.AllMightPowerSkills.Count > 0)
            {
                for(int i = 0; i < comp.MightData.AllMightPowerSkills.Count; i++)
                {
                    MightPowerSkill mps = comp.MightData.AllMightPowerSkills[i];
                    if(mps.label == label)
                    {
                        return mps;
                    }
                }
            }
            return null;
        }

        public static MagicPowerSkill GetMagicPowerSkillFromLabel(CompAbilityUserMagic comp, string label)
        {
            if (comp != null && comp.MagicData != null && comp.MagicData.AllMagicPowerSkills.Count > 0)
            {
                for (int i = 0; i < comp.MagicData.AllMagicPowerSkills.Count; i++)
                {
                    MagicPowerSkill mps = comp.MagicData.AllMagicPowerSkills[i];
                    if (mps.label == label)
                    {
                        return mps;
                    }
                }
            }
            return null;
        }

        public static TMDefs.TM_CustomClass GetRandomCustomFighter()
        {
            return CustomFighterClasses.Length > 0 ? CustomFighterClasses.RandomElement() : null;
        }

        public static TMDefs.TM_CustomClass GetRandomCustomMage()
        {
            return CustomMageClasses.Length > 0 ? CustomMageClasses.RandomElement() : null;
        }

        public static List<TM_CustomClass> GetAdvancedClassesForPawn(Pawn p)
        {
            List<TM_CustomClass> ccList = new List<TM_CustomClass>();
            for (int i = 0; i < p.story.traits.allTraits.Count; i++)
            {
                TM_CustomClass cc = CustomAdvancedClassTraitIndexMap.TryGetValue(p.story.traits.allTraits[i].def.index);
                if (cc != null) ccList.Add(cc);
            }

            return ccList;
        }

        public static bool ClassHasAbility(TMAbilityDef ability, CompAbilityUserMagic compMagic = null, CompAbilityUserMight compMight = null)
        {
            if(compMagic != null && compMagic.customClass != null && compMagic.customClass.classAbilities.Contains(ability))
            {
                return true;
            }
            else if(compMight != null && compMight.customClass != null && compMight.customClass.classAbilities.Contains(ability))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ClassHasHediff(HediffDef hdDef, CompAbilityUserMagic compMagic = null, CompAbilityUserMight compMight = null)
        {
            if (compMagic != null && compMagic.customClass != null && compMagic.customClass.classHediff == hdDef)
            {
                return true;
            }
            else if (compMight != null && compMight.customClass != null && compMight.customClass.classHediff == hdDef)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
