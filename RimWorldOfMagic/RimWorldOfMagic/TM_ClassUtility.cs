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
        public static HashSet<ushort> NonCustomMagicAndMightTraitIndexes = new HashSet<ushort>()
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
            TorannMagicDefOf.TM_Wanderer.index,
            TorannMagicDefOf.TM_Monk.index,
            TorannMagicDefOf.DeathKnight.index,
            TorannMagicDefOf.TM_Psionic.index,
            TorannMagicDefOf.Gladiator.index,
            TorannMagicDefOf.TM_Sniper.index,
            TorannMagicDefOf.Bladedancer.index,
            TorannMagicDefOf.Ranger.index,
            TorannMagicDefOf.Faceless.index,
            TorannMagicDefOf.TM_Commander.index,
            TorannMagicDefOf.TM_SuperSoldier.index,
            TorannMagicDefOf.TM_Wayfarer.index
        };

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

        public static void LoadClassIndexes()
        {
            CustomClassTraitIndexes = new Dictionary<ushort, int>();
            for (int i = 0; i < CustomClasses.Count; i++)
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
            if (allTraits != null && allTraits.Count > 0)
            {
                if (CustomClassTraitIndexes == null)
                {
                    LoadClassIndexes();
                }
                for (int i = 0; i < allTraits.Count; i++)
                {
                    if (CustomClassTraitIndexes.ContainsKey(allTraits[i].def.index))
                    {
                        return CustomClassTraitIndexes[allTraits[i].def.index];
                    }
                }
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
            for(int i = 0; i < CustomClasses.Count; i++)
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
            hList.Clear();
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
            int index = CustomClassIndexOfTraitDef(td);
            if(index >= 0)
            {
                return CustomClasses[index];
            }
            return null;
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
