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

        public static TM_CustomClass[] CustomClasses;
        public static TM_CustomClass[] CustomBaseClasses;
        public static TM_CustomClass[] CustomMageClasses;
        public static TM_CustomClass[] CustomFighterClasses;
        public static TM_CustomClass[] CustomAdvancedClasses;

        public static readonly Dictionary<ushort, TM_CustomClass> CustomAdvancedClassTraitIndexMap = new Dictionary<ushort, TM_CustomClass>();
        public static readonly Dictionary<ushort, TM_CustomClass> CustomBaseClassTraitIndexMap = new Dictionary<ushort, TM_CustomClass>();
        // Dictionary to more quickly determine trait's CustomClasses index
        public static Dictionary<ushort, int> CustomClassTraitIndexes = new Dictionary<ushort, int>();

        public static void LoadCustomClasses()
        {
            CustomClasses = TM_CustomClassDef.Named("TM_CustomClasses").customClasses.ToArray();
            var CustomBaseClassesList = new List<TM_CustomClass>();
            var CustomMageClassesList = new List<TM_CustomClass>();
            var CustomFighterClassesList = new List<TM_CustomClass>();
            var CustomAdvancedClassesList = new List<TM_CustomClass>();
            
            if (CustomClasses == null) return;
            
            CustomAdvancedClassTraitIndexMap.Clear();
            CustomBaseClassTraitIndexMap.Clear();
            CustomClassTraitIndexes.Clear();

            IEnumerable<TM_CustomClass> enabledCustomClasses = CustomClasses.Where(cc =>
                Settings.Instance.CustomClass.TryGetValue(cc.classTrait.ToString(), true));

            foreach (TM_CustomClass cc in enabledCustomClasses)
            {
                if (cc.isMage)
                {
                    if (cc.isAdvancedClass && cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                    {
                        CustomMageClassesList.Add(cc);
                    }
                    else
                    {
                        CustomMageClassesList.Add(cc);
                    }
                }
                if (cc.isFighter)
                {
                    if (cc.isAdvancedClass && cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                    {
                        CustomFighterClassesList.Add(cc);
                    }
                    else
                    {
                        CustomFighterClassesList.Add(cc);
                    }
                }
                
                if (!cc.isAdvancedClass) {
                    CustomBaseClassesList.Add(cc); //base classes cannot also be advanced classes, but advanced classes can act like base classes
                    CustomBaseClassTraitIndexMap[cc.classTrait.index] = cc;
                }
                else
                {
                    CustomAdvancedClassesList.Add(cc);
                    CustomAdvancedClassTraitIndexMap[cc.classTrait.index] = cc;
                }
            }
            // These ALWAYS need to be set regardless of if there are any custom classes
            CustomBaseClasses = CustomBaseClassesList.ToArray();
            CustomFighterClasses = CustomFighterClassesList.ToArray();
            CustomMageClasses = CustomMageClassesList.ToArray();
            CustomAdvancedClasses = CustomAdvancedClassesList.ToArray();
            LoadClassIndexes();
        }

        public static void LoadClassIndexes()
        {
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

        public static int CustomClassIndexOfTraitDef(TraitDef trait)
        {
            return CustomClassTraitIndexes.TryGetValue(trait.index, -2);
        }

        public static int CustomClassIndexOfBaseMageClass(List<Trait> allTraits)
        {
            for (int i = 0; i < allTraits.Count; i++)
            {
                TM_CustomClass customClass = CustomBaseClassTraitIndexMap.TryGetValue(allTraits[i].def.index);
                if (customClass == null || !customClass.isMage) continue;
                return CustomClassTraitIndexes.TryGetValue(customClass.classTrait.index, -2);
            }

            return -2;
        }

        public static int CustomClassIndexOfBaseFighterClass(List<Trait> allTraits)
        {
            for (int i = 0; i < allTraits.Count; i++)
            {
                TM_CustomClass customClass = CustomBaseClassTraitIndexMap.TryGetValue(allTraits[i].def.index);
                if (customClass == null || !customClass.isFighter) continue;
                return CustomClassTraitIndexes.TryGetValue(customClass.classTrait.index, -2);
            }

            return -2;
        }

        public static HashSet<ushort> CustomClassHediffIndexes()
        {
            return CustomClasses.Where(cc => cc.classHediff != null).Select(cc => cc.classHediff.index).ToHashSet();
        }

        public static TM_CustomClass GetCustomClassOfTrait(TraitDef td)
        {
            int index = CustomClassIndexOfTraitDef(td);
            return index >= 0 ? CustomClasses[index] : null;
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
            List<TMDefs.TM_CustomClass> customFighters = CustomFighterClasses.ToList();
            if(customFighters.Count > 0)
            {
                return customFighters.RandomElement();
            }
            return null;
        }

        public static TMDefs.TM_CustomClass GetRandomCustomMage()
        {
            List<TMDefs.TM_CustomClass> customMages = CustomMageClasses.ToList();
            if (customMages.Count > 0)
            {
                return customMages.RandomElement();
            }
            return null;
        }

        public static List<TM_CustomClass> GetAdvancedClassesForPawn(Pawn p)
        {
            List<TM_CustomClass> ccList = new List<TM_CustomClass>();
            ccList.Clear();
            foreach(TM_CustomClass cc in CustomAdvancedClasses)
            {
                if(p.story.traits.HasTrait(cc.classTrait))
                {
                    ccList.Add(cc);
                }
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
