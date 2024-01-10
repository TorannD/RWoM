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
        public static Dictionary<TraitDef, Settings.CheckboxOption> NonCustomMagicTraits = new ()
        {
            { TorannMagicDefOf.Arcanist, Settings.Arcanist },
            { TorannMagicDefOf.BloodMage, Settings.BloodMage },
            { TorannMagicDefOf.ChaosMage, Settings.ChaosMage },
            { TorannMagicDefOf.Chronomancer, Settings.Chronomancer },
            { TorannMagicDefOf.Druid, Settings.Druid },
            { TorannMagicDefOf.Enchanter, Settings.Enchanter },
            { TorannMagicDefOf.Faceless, Settings.Faceless },
            { TorannMagicDefOf.Geomancer, Settings.Geomancer },
            { TorannMagicDefOf.HeartOfFrost, Settings.IceMage },
            { TorannMagicDefOf.InnerFire, Settings.FireMage },
            { TorannMagicDefOf.Lich, Settings.Necromancer },
            { TorannMagicDefOf.Necromancer, Settings.Necromancer },
            { TorannMagicDefOf.Paladin, Settings.Paladin },
            { TorannMagicDefOf.Priest, Settings.Priest },
            { TorannMagicDefOf.StormBorn, Settings.LitMage },
            { TorannMagicDefOf.Succubus, Settings.Demonkin },
            { TorannMagicDefOf.Summoner, Settings.Summoner },
            { TorannMagicDefOf.TM_Bard, Settings.Bard },
            { TorannMagicDefOf.TM_Brightmage, Settings.Brightmage },
            { TorannMagicDefOf.TM_TheShadow, Settings.Shadow },
            { TorannMagicDefOf.TM_Wanderer, Settings.Wanderer },
            { TorannMagicDefOf.Technomancer, Settings.Technomancer },
            { TorannMagicDefOf.Warlock, Settings.Demonkin }
        };

        public static Dictionary<TraitDef, Settings.CheckboxOption> NonCustomFighterTraits = new ()
        {
            { TorannMagicDefOf.TM_Apothecary, Settings.Apothecary },
            { TorannMagicDefOf.Bladedancer, Settings.Bladedancer },
            { TorannMagicDefOf.DeathKnight, Settings.DeathKnight },
            { TorannMagicDefOf.Faceless, Settings.Faceless },
            { TorannMagicDefOf.Gladiator, Settings.Gladiator },
            { TorannMagicDefOf.Ranger, Settings.Ranger },
            { TorannMagicDefOf.TM_Commander, Settings.Commander },
            { TorannMagicDefOf.TM_Monk, Settings.Monk },
            { TorannMagicDefOf.TM_Psionic, Settings.Psionic },
            { TorannMagicDefOf.TM_Sniper, Settings.Sniper },
            { TorannMagicDefOf.TM_SuperSoldier, Settings.SuperSoldier },
            { TorannMagicDefOf.TM_TheShadow, Settings.Shadow },
            { TorannMagicDefOf.TM_Wayfarer, Settings.Wayfarer }
        };

        public static Dictionary<TraitDef, Settings.CheckboxOption> MageSupportTraits = new ()
        {
            { TorannMagicDefOf.TM_ArcaneConduitTD, Settings.ArcaneConduit },
            { TorannMagicDefOf.TM_ManaWellTD, Settings.ManaWell },
            { TorannMagicDefOf.TM_FaeBloodTD, Settings.FaeBlood },
            { TorannMagicDefOf.TM_EnlightenedTD, Settings.Enlightened },
            { TorannMagicDefOf.TM_CursedTD, Settings.Cursed }
        };

        public static Dictionary<TraitDef, Settings.CheckboxOption> FighterSupportTraits = new ()
        {
            { TorannMagicDefOf.TM_BoundlessTD, Settings.Boundless },
            { TorannMagicDefOf.TM_GiantsBloodTD, Settings.GiantsBlood }
        };

        // Special rules for generating pawns.
        public static Dictionary<TraitDef, Predicate<Pawn>> ClassSpawnValidators = new Dictionary<TraitDef, Predicate<Pawn>>
            {
                { TorannMagicDefOf.Warlock, static pawn => pawn.gender == Gender.Male },
                { TorannMagicDefOf.Succubus, static pawn => pawn.gender == Gender.Female },
                { TorannMagicDefOf.Lich, static pawn => false },  // Never spawn a lich under normal circumstances
                { TorannMagicDefOf.TM_Possessor, static pawn => false },  // Never spawn possessor under normal circumstances

            };

        public static HashSet<TraitDef> MightTraits;
        public static HashSet<TraitDef> MagicTraits;
        public static HashSet<TraitDef> AllClassTraits;
        public static Dictionary<ushort, TM_CustomClass> CustomClassTraitMap;
        public static Dictionary<ushort, TM_CustomClass> CustomAdvancedClassTraitMap;
        public static TM_CustomClass[] CustomClasses;
        public static TM_CustomClass[] CustomBaseClasses;
        public static TM_CustomClass[] CustomMageClasses;
        public static TM_CustomClass[] CustomFighterClasses;
        public static TM_CustomClass[] CustomAdvancedClasses;

        // Load all custom classes into the mod (regardless if enabled)
        public static void InitializeCustomClasses()
        {
            // A Helper method to set the trait trackers to no custom classes.
            static void setNoCustomClassDefaults()
            {
                CustomClassTraitMap = new Dictionary<ushort, TM_CustomClass>();
                CustomAdvancedClassTraitMap = new Dictionary<ushort, TM_CustomClass>();
                MagicTraits = new HashSet<TraitDef>(NonCustomMagicTraits.Keys);
                MightTraits = new HashSet<TraitDef>(NonCustomFighterTraits.Keys);
                AllClassTraits = new HashSet<TraitDef>(NonCustomFighterTraits.Keys);
                AllClassTraits.UnionWith(MagicTraits);
            }

            try
            {
                setNoCustomClassDefaults();
                CustomClasses = TM_CustomClassDef.Named("TM_CustomClasses").customClasses.ToArray();
                foreach (TM_CustomClass cc in CustomClasses)
                {
                    CustomClassTraitMap[cc.classTrait.index] = cc;
                    if (cc.isAdvancedClass) CustomAdvancedClassTraitMap[cc.classTrait.index] = cc;
                    if (cc.isMage) MagicTraits.Add(cc.classTrait);
                    if (cc.isFighter) MightTraits.Add(cc.classTrait);
                    AllClassTraits.Add(cc.classTrait);
                }
            }
            catch
            {
                Log.Error("[Rimworld of Magic] Initializing Custom Classes failed. Disabling Custom Classes");
                setNoCustomClassDefaults();
                CustomClasses = Array.Empty<TM_CustomClass>();
            }
        }

        // Load the enabled custom classes into appropriate variables after load + mod settings change
        public static void LoadCustomClasses()
        {
            try
            {
                var CustomBaseClassesList = new List<TM_CustomClass>();
                var CustomMageClassesList = new List<TM_CustomClass>();
                var CustomFighterClassesList = new List<TM_CustomClass>();
                var CustomAdvancedClassesList = new List<TM_CustomClass>();

                foreach (TM_CustomClass cc in CustomClasses)
                {
                    if (!Settings.Instance.CustomClass.TryGetValue(cc.classTrait.ToString(), true)) continue;

                    if (cc.isMage)
                    {
                        if (cc.isAdvancedClass)
                        {
                            if (cc.advancedClassOptions != null && cc.advancedClassOptions.canSpawnWithClass)
                            {
                                CustomMageClassesList.Add(cc);
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
                            }
                        }
                        else
                        {
                            CustomFighterClassesList.Add(cc);
                        }
                    }

                    if (!cc.isAdvancedClass)
                        CustomBaseClassesList.Add(cc); //base classes cannot also be advanced classes, but advanced classes can act like base classes
                    else
                        CustomAdvancedClassesList.Add(cc);

                    CustomBaseClasses = CustomBaseClassesList.ToArray();
                    CustomFighterClasses = CustomFighterClassesList.ToArray();
                    CustomMageClasses = CustomMageClassesList.ToArray();
                    CustomAdvancedClasses = CustomAdvancedClassesList.ToArray();
                }
                LoadClassIndexes();
            }
            catch
            {
                Log.Error("Loading Enabled Custom Classes Failed. Disabling...");
                CustomClasses = Array.Empty<TM_CustomClass>();
                CustomBaseClasses = Array.Empty<TM_CustomClass>();
                CustomMageClasses = Array.Empty<TM_CustomClass>();
                CustomFighterClasses = Array.Empty<TM_CustomClass>();
                CustomAdvancedClasses = Array.Empty<TM_CustomClass>();
            }
        }

        public static void LoadClassIndexes()
        {
            CustomClassTraitIndexes = new Dictionary<ushort, int>();
            for (int i = 0; i < CustomClasses.Length; i++)
            {
                CustomClassTraitIndexes[CustomClasses[i].classTrait.index] = i;
            }
        }

        public static HashSet<TraitDef> EnabledMageClasses = new HashSet<TraitDef>();
        public static HashSet<TraitDef> EnabledFighterClasses = new HashSet<TraitDef>();
        // Switch these two to hashsets if they get bigger than 10 traitdefs or go custom
        public static TraitDef[] EnabledMageSupportClasses;
        public static TraitDef[] EnabledFighterSupportClasses;
        public static void CacheEnabledClasses()
        {
            EnabledMageClasses.Clear();
            EnabledFighterClasses.Clear();
            // Add custom classes which have already checked if they are enabled
            foreach (TM_CustomClass cc in CustomMageClasses)
            {
                EnabledMageClasses.Add(cc.classTrait);
            }
            foreach (TM_CustomClass cc in CustomFighterClasses)
            {
                EnabledFighterClasses.Add(cc.classTrait);
            }
            // Add the base classes if they are enabled
            foreach (KeyValuePair<TraitDef, Settings.CheckboxOption> pair in NonCustomMagicTraits)
            {
                if (pair.Value.isEnabled) EnabledMageClasses.Add(pair.Key);
            }
            foreach (KeyValuePair<TraitDef, Settings.CheckboxOption> pair in NonCustomFighterTraits)
            {
                if (pair.Value.isEnabled) EnabledFighterClasses.Add(pair.Key);
            }
            // Handle the support classes. Use LINQ to avoid array annoyances
            EnabledMageSupportClasses = MageSupportTraits
                .Where(static pair => pair.Value.isEnabled)
                .Select(static pair => pair.Key)
                .ToArray();
            EnabledFighterSupportClasses = FighterSupportTraits
                .Where(static pair => pair.Value.isEnabled)
                .Select(static pair => pair.Key)
                .ToArray();
        }

        public static List<TraitDef> CustomClassTraitDefs
        {
            get => CustomClasses.Select(t => t.classTrait).ToList();           
        }

        // DEPRECATED: Use TM_ClassUtility.CustomClassTraitMap
        public static Dictionary<ushort, int> CustomClassTraitIndexes;  // Dictionary to more quickly determine trait's CustomClasses index

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
            for (int i = 0; i < CustomClasses.Length; i++)
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
