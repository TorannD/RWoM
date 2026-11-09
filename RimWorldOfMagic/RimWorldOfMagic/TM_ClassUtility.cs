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
        // Initialize before deferred definition loading so registry consumers can
        // safely observe an empty list during partial startup.
        public static List<TM_CustomClass> CustomClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomBaseClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomMageClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomFighterClasses = new List<TM_CustomClass>();
        public static readonly List<TM_CustomClass> CustomAdvancedClasses = new List<TM_CustomClass>();

        // TraitDef references, not database indexes: mods that mutate the trait
        // database during pawn generation (RimTraits removes and re-adds talent
        // defs inside PawnGenerator.GenerateTraits) shift every stored index,
        // which silently hid the grimoire button from trait-based classes.
        // References survive re-indexing, so this set needs no refresh hooks.
        public static HashSet<TraitDef> NonCustomMagicAndMightTraitIndexes = new HashSet<TraitDef>()
        {
            TorannMagicDefOf.Enchanter,
            TorannMagicDefOf.BloodMage,
            TorannMagicDefOf.Technomancer,
            TorannMagicDefOf.Geomancer,
            TorannMagicDefOf.Warlock,
            TorannMagicDefOf.Succubus,
            TorannMagicDefOf.Faceless,
            TorannMagicDefOf.InnerFire,
            TorannMagicDefOf.HeartOfFrost,
            TorannMagicDefOf.StormBorn,
            TorannMagicDefOf.Arcanist,
            TorannMagicDefOf.Paladin,
            TorannMagicDefOf.Summoner,
            TorannMagicDefOf.Druid,
            TorannMagicDefOf.Necromancer,
            TorannMagicDefOf.Lich,
            TorannMagicDefOf.Priest,
            TorannMagicDefOf.TM_Bard,
            TorannMagicDefOf.Chronomancer,
            TorannMagicDefOf.ChaosMage,
            TorannMagicDefOf.TM_Wanderer,
            TorannMagicDefOf.TM_Monk,
            TorannMagicDefOf.DeathKnight,
            TorannMagicDefOf.TM_Psionic,
            TorannMagicDefOf.Gladiator,
            TorannMagicDefOf.TM_Sniper,
            TorannMagicDefOf.Bladedancer,
            TorannMagicDefOf.Ranger,
            TorannMagicDefOf.TM_Commander,
            TorannMagicDefOf.TM_SuperSoldier,
            TorannMagicDefOf.TM_Wayfarer
        };

        //public static TM_CustomClass[] CustomClasses;
        //public static TM_CustomClass[] CustomBaseClasses;
        //public static TM_CustomClass[] CustomMageClasses;
        //public static TM_CustomClass[] CustomFighterClasses;
        //public static TM_CustomClass[] CustomAdvancedClasses;

        public static void RefreshBuiltinClassIndexes()
        {
            NonCustomMagicAndMightTraitIndexes.Clear();
            foreach (string name in TM_ClassGroups.MagicEntriesField.Select(entry => entry.trait)
                .Concat(TM_ClassGroups.MightEntriesField.Select(entry => entry.trait)).Distinct())
            {
                TraitDef trait = DefDatabase<TraitDef>.GetNamedSilentFail(name);
                if (trait != null) NonCustomMagicAndMightTraitIndexes.Add(trait);
            }
        }

        public static void LoadCustomClasses()
        {
            // A mod-settings file without the CustomClass key deserializes the
            // dictionary as null; LoadCustomClasses runs before the callback that
            // initializes it, so initialize it here before building the category caches.
            Settings.Instance.CustomClass ??= new Dictionary<string, bool>();

            TM_CustomClassDef named = TM_CustomClassDef.Named("TM_CustomClasses");
            CustomClasses = named?.customClasses ?? new List<TM_CustomClass>();
            //CustomAdvancedClassTraitIndexMap.Clear();
            //var CustomBaseClassesList = new List<TM_CustomClass>();
            //var CustomMageClassesList = new List<TM_CustomClass>();
            //var CustomFighterClassesList = new List<TM_CustomClass>();
            //var CustomAdvancedClassesList = new List<TM_CustomClass>();
            CustomBaseClasses.Clear();
            CustomMageClasses.Clear();
            CustomFighterClasses.Clear();
            CustomAdvancedClasses.Clear();

            // A broken XML row or an unresolved cross-reference must not kill the
            // whole registry: skip incomplete entries instead of throwing here.
            IEnumerable<TM_CustomClass> enabledCustomClasses = CustomClasses.Where(cc =>
                cc != null && cc.classTrait != null
                && Settings.Instance.CustomClass.TryGetValue(cc.classTrait.ToString(), true));

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
            // Deferred startup/settings reload can replace classes after the UI
            // has already cached its trait lists. Rebuild only registry-derived lists.
            TM_Data.ResetClassCaches();
            
        }

        public static void LoadClassIndexes()
        {
            CustomClassTraitIndexes = new Dictionary<ushort, int>();
            if (CustomClasses == null) return;
            for (int i = 0; i < CustomClasses.Count; i++)
            {
                if (CustomClasses[i]?.classTrait == null) continue;
                CustomClassTraitIndexes[CustomClasses[i].classTrait.index] = i;
            }
        }

        public static List<TraitDef> CustomClassTraitDefs
        {
            get => CustomClasses.Where(t => t?.classTrait != null).Select(t => t.classTrait).ToList();
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
                if (CustomClasses[i]?.classTrait == null || trait == null) continue;
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

