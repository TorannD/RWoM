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
        public static List<TM_CustomClass> CustomClasses;
        public static readonly List<TM_CustomClass> CustomBaseClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomMageClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomFighterClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomAdvancedClasses = new List<TM_CustomClass>();

        //public static TM_CustomClass[] CustomClasses;
        //public static readonly Dictionary<ushort, TM_CustomClass> CustomAdvancedClassTraitIndexMap = new Dictionary<ushort, TM_CustomClass>();
        //public static TM_CustomClass[] CustomBaseClasses;
        //public static TM_CustomClass[] CustomMageClasses;
        //public static TM_CustomClass[] CustomFighterClasses;
        //public static TM_CustomClass[] CustomAdvancedClasses;

        public static HashSet<ushort> MagicTraitIndexes = new HashSet<ushort>
        {
            TorannMagicDefOf.Enchanter.index,
            TorannMagicDefOf.BloodMage.index,
            TorannMagicDefOf.Technomancer.index,
            TorannMagicDefOf.Geomancer.index,
            TorannMagicDefOf.Warlock.index,
            TorannMagicDefOf.Succubus.index,
            TorannMagicDefOf.Faceless.index,
            TorannMagicDefOf.InnerFire.index,
            TorannMagicDefOf.HeartOfFrost.index,
            TorannMagicDefOf.StormBorn.index,
            TorannMagicDefOf.Arcanist.index,
            TorannMagicDefOf.Paladin.index,
            TorannMagicDefOf.Summoner.index,
            TorannMagicDefOf.Druid.index,
            TorannMagicDefOf.Necromancer.index,
            TorannMagicDefOf.Lich.index,
            TorannMagicDefOf.Priest.index,
            TorannMagicDefOf.TM_Bard.index,
            TorannMagicDefOf.Chronomancer.index,
            TorannMagicDefOf.ChaosMage.index,
            TorannMagicDefOf.TM_Wanderer.index
        };

        public static void LoadCustomClasses()
        {
            TM_CustomClassDef named = TM_CustomClassDef.Named("TM_CustomClasses");
            if (named == null) return;

            CustomClasses = named.customClasses;
            //CustomAdvancedClassTraitIndexMap.Clear();
            //var CustomBaseClassesList = new List<TM_CustomClass>();
            //var CustomMageClassesList = new List<TM_CustomClass>();
            //var CustomFighterClassesList = new List<TM_CustomClass>();
            //var CustomAdvancedClassesList = new List<TM_CustomClass>();
            CustomBaseClasses.Clear();
            CustomMageClasses.Clear();
            CustomFighterClasses.Clear();
            CustomAdvancedClasses.Clear();

            IEnumerable<TM_CustomClass> enabledCustomClasses = CustomClasses.Where(cc =>
                Settings.Instance.CustomClass.TryGetValue(cc.classTrait.ToString(), true));

            foreach (TM_CustomClass cc in enabledCustomClasses)
            {
                if (cc.isMage)
                {
                    if (cc.isAdvancedClass)
                    {
                        if (cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                        {
                            CustomMageClasses.Add(cc);
                        }
                    }
                    else
                    {
                        CustomMageClasses.Add(cc);
                    }
                }
                if (cc.isFighter)
                {
                    if (cc.isAdvancedClass)
                    {
                        if (cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                        {
                            CustomFighterClasses.Add(cc);
                        }
                    }
                    else
                    {
                        CustomFighterClasses.Add(cc);
                    }
                }
                if (!cc.isAdvancedClass) CustomBaseClasses.Add(cc); //base classes cannot also be advanced classes, but advanced classes can act like base classes
                else
                {
                    CustomAdvancedClasses.Add(cc);
                    //CustomAdvancedClassTraitIndexMap[cc.classTrait.index] = cc;
                }
                //CustomBaseClasses = CustomBaseClassesList.ToArray();
                //CustomFighterClasses = CustomFighterClassesList.ToArray();
                //CustomMageClasses = CustomMageClassesList.ToArray();
                //CustomAdvancedClasses = CustomAdvancedClassesList.ToArray();
            }
            LoadClassIndexes();

        }

        private static Dictionary<ushort, int> LoadClassIndexes()
        {
            customClassTraitIndexes = new Dictionary<ushort, int>();
            for (int i = 0; i < CustomClasses.Count; i++)
            {
                customClassTraitIndexes[CustomClasses[i].classTrait.index] = i;
            }

            return customClassTraitIndexes;
        }

        public static List<TraitDef> CustomClassTraitDefs => CustomClasses.Select(t => t.classTrait).ToList();

        private static Dictionary<ushort, int> customClassTraitIndexes;  // Dictionary to more quickly determine trait's CustomClasses index
        public static Dictionary<ushort, int> CustomClassTraitIndexes => customClassTraitIndexes ?? LoadClassIndexes();

        public static int IsCustomClassIndex(List<Trait> allTraits)
        {
            if (allTraits == null || allTraits.Count <= 0) return -2;

            for (int i = 0; i < allTraits.Count; i++)
            {
                int index = CustomClassTraitIndexes.TryGetValue(allTraits[i].def.index, -1);
                if (index == -1) continue;
                return index;
            }
            return -2;
        }

        public static int CustomClassIndexOfTraitDef(TraitDef trait)
        {
            for (int i = 0; i < CustomClasses.Count; i++)
            {
                if (CustomClasses[i].classTrait.defName == trait.defName)
                {
                    return i;
                }
            }
            return -2;
        }

        public static int CustomClassIndexOfBaseMageClass(List<Trait> allTraits)
        {
            for (int i = 0; i < CustomClasses.Count; i++)
            {
                if (CustomClasses[i].isAdvancedClass) continue;
                if (!CustomClasses[i].isMage) continue;

                TraitDef classTrait = CustomClasses[i].classTrait;
                if (allTraits.Any(t => t.def == classTrait)) return i;
            }
            return -2;
        }

        public static int CustomClassIndexOfBaseFighterClass(List<Trait> allTraits)
        {
            for(int i = 0; i < CustomClasses.Count; i++)
            {
                if (CustomClasses[i].isAdvancedClass) continue;
                if (!CustomClasses[i].isFighter) continue;

                TraitDef classTrait = CustomClasses[i].classTrait;
                if (allTraits.Any(t => t.def == classTrait)) return i;
            }
            return -2;
        }

        public static List<HediffDef> CustomClassHediffs()
        {
            return CustomClasses
                .Where(cc => cc.classHediff != null)
                .Select(cc => cc.classHediff)
                .ToList();
        }

        public static TM_CustomClass GetCustomClassOfTrait(TraitDef td)
        {
            int index = CustomClassIndexOfTraitDef(td);
            return index >= 0 ? CustomClasses[index] : null;
        }

        public static List<MagicPowerSkill> GetAssociatedMagicPowerSkill(CompAbilityUserMagic comp, MagicPower power)
        {
            string str = power.TMabilityDefs.First().defName + "_";
            return comp.MagicData.AllMagicPowerSkills.Where(mps => mps.label.Contains(str)).ToList();
        }

        public static List<MightPowerSkill> GetAssociatedMightPowerSkill(CompAbilityUserMight comp, TMAbilityDef abilityDef, string var)
        {
            string str = abilityDef.defName + "_" + var;
            return comp.MightData.AllMightPowerSkills.Where(mps => mps.label.Contains(str)).ToList();
        }

        public static MightPowerSkill GetMightPowerSkillFromLabel(CompAbilityUserMight comp, string label)
        {
            if (comp?.MightData == null || comp.MightData.AllMightPowerSkills.Count == 0) return null;

            return comp.MightData.AllMightPowerSkills.FirstOrDefault(mps => mps.label == label);
        }

        public static MagicPowerSkill GetMagicPowerSkillFromLabel(CompAbilityUserMagic comp, string label)
        {
            if (comp?.MagicData == null || comp.MagicData.AllMagicPowerSkills.Count == 0) return null;

            return comp.MagicData.AllMagicPowerSkills.FirstOrDefault(mps => mps.label == label);
        }

        public static TM_CustomClass GetRandomCustomFighter()
        {
            return CustomFighterClasses.Count > 0 ? CustomFighterClasses.RandomElement() : null;
        }

        public static TM_CustomClass GetRandomCustomMage()
        {
            return CustomMageClasses.Count > 0 ? CustomMageClasses.RandomElement() : null;
        }

        public static List<TM_CustomClass> GetAdvancedClassesForPawn(Pawn p)
        {
            return CustomAdvancedClasses.Where(cc => p.story.traits.HasTrait(cc.classTrait)).ToList();
        }

        public static bool ClassHasAbility(TMAbilityDef ability, CompAbilityUserTMBase comp = null)
        {
            return comp?.customClass != null && comp.customClass.classAbilities.Contains(ability);
        }

        public static bool ClassHasHediff(HediffDef hdDef, CompAbilityUserMagic compMagic = null, CompAbilityUserMight compMight = null)
        {
            if (compMagic?.customClass != null && compMagic.customClass.classHediff == hdDef)
            {
                return true;
            }
            else if (compMight?.customClass != null && compMight.customClass.classHediff == hdDef)
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
