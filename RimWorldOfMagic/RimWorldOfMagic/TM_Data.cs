using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using RimWorld;
using System;

namespace TorannMagic
{
    public static class TM_Data
    {
        public static List<ThingDef> SpellList()
        {
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.defName.Contains("SpellOf_"))
                                               select def;
            return enumerable.ToList();
        }

        public static List<ThingDef> MasterSpellList()
        {
            List<ThingDef> masterSpellList = new List<ThingDef>();
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Firestorm);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Blizzard);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_EyeOfTheStorm);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_RegrowLimb);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_FoldReality);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Resurrection);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_HolyWrath);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_LichForm);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_SummonPoppi);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_BattleHymn);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_PsychicShock);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Scorn);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Meteor);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_OrbitalStrike);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_BloodMoon);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Shapeshift);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_Recall);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_SpiritOfLight);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_GuardianSpirit);
            masterSpellList.Add(TorannMagicDefOf.SpellOf_LivingWall);
            return masterSpellList;
        }

        public static List<ThingDef> RestrictedAbilities
        {
            get
            {
                List<ThingDef> restricted = new List<ThingDef>();
                restricted.Add(TorannMagicDefOf.SpellOf_BattleHymn);
                restricted.Add(TorannMagicDefOf.SpellOf_BlankMind);
                restricted.Add(TorannMagicDefOf.SpellOf_Blizzard);
                restricted.Add(TorannMagicDefOf.SpellOf_BloodMoon);
                restricted.Add(TorannMagicDefOf.SpellOf_BriarPatch);
                restricted.Add(TorannMagicDefOf.SpellOf_CauterizeWound);
                restricted.Add(TorannMagicDefOf.SpellOf_ChargeBattery);
                restricted.Add(TorannMagicDefOf.SpellOf_DryGround);
                restricted.Add(TorannMagicDefOf.SpellOf_EyeOfTheStorm);
                restricted.Add(TorannMagicDefOf.SpellOf_FertileLands);
                restricted.Add(TorannMagicDefOf.SpellOf_Firestorm);
                restricted.Add(TorannMagicDefOf.SpellOf_FoldReality);
                restricted.Add(TorannMagicDefOf.SpellOf_HeatShield);
                restricted.Add(TorannMagicDefOf.SpellOf_HolyWrath);
                restricted.Add(TorannMagicDefOf.SpellOf_LichForm);
                restricted.Add(TorannMagicDefOf.SpellOf_MechaniteReprogramming);
                restricted.Add(TorannMagicDefOf.SpellOf_Meteor);
                restricted.Add(TorannMagicDefOf.SpellOf_OrbitalStrike);
                restricted.Add(TorannMagicDefOf.SpellOf_Overdrive);
                restricted.Add(TorannMagicDefOf.SpellOf_PsychicShock);
                restricted.Add(TorannMagicDefOf.SpellOf_Recall);
                restricted.Add(TorannMagicDefOf.SpellOf_RegrowLimb);
                restricted.Add(TorannMagicDefOf.SpellOf_Resurrection);
                restricted.Add(TorannMagicDefOf.SpellOf_Sabotage);
                restricted.Add(TorannMagicDefOf.SpellOf_Scorn);
                restricted.Add(TorannMagicDefOf.SpellOf_Shapeshift);
                restricted.Add(TorannMagicDefOf.SpellOf_SummonPoppi);
                restricted.Add(TorannMagicDefOf.SpellOf_TechnoShield);
                restricted.Add(TorannMagicDefOf.SpellOf_WetGround);
                restricted.Add(TorannMagicDefOf.SpellOf_SpiritOfLight);
                restricted.Add(TorannMagicDefOf.SpellOf_GuardianSpirit);
                restricted.Add(TorannMagicDefOf.SpellOf_Discord);
                restricted.Add(TorannMagicDefOf.SpellOf_ShieldOther);
                restricted.AddRange(RestrictedAbilitiesXML);
                return restricted;
            }
        }

        public static List<ThingDef> RestrictedAbilitiesXML
        {
            get
            {
                IEnumerable<TMAbilityDef> enumerable = from def in DefDatabase<TMAbilityDef>.AllDefs
                                                   where (def.restrictedAbility)
                                                   select def;
                List<ThingDef> xmlRestrictedAbilities = new List<ThingDef>();
                xmlRestrictedAbilities.Clear();
                foreach(TMAbilityDef d in enumerable)
                {
                    if(d.restrictedAbility && d.learnItem != null)
                    {
                        xmlRestrictedAbilities.Add(d.learnItem);
                    }
                }
                return xmlRestrictedAbilities.ToList();
            }
        }

        public static List<ThingDef> StandardSpellList()
        {
            return SpellList().Except(MasterSpellList()).ToList();
        }

        public static List<ThingDef> StandardSkillList()
        {
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.defName.Contains("SkillOf_"))
                                               select def;
            return enumerable.ToList();
        }

        public static List<ThingDef> FighterBookList()
        {
            List<ThingDef> fighterBookList = new List<ThingDef>();
            fighterBookList.Add(TorannMagicDefOf.BookOfGladiator);
            fighterBookList.Add(TorannMagicDefOf.BookOfBladedancer);
            fighterBookList.Add(TorannMagicDefOf.BookOfDeathKnight);
            fighterBookList.Add(TorannMagicDefOf.BookOfFaceless);
            fighterBookList.Add(TorannMagicDefOf.BookOfPsionic);
            fighterBookList.Add(TorannMagicDefOf.BookOfRanger);
            fighterBookList.Add(TorannMagicDefOf.BookOfSniper);
            fighterBookList.Add(TorannMagicDefOf.BookOfMonk);
            fighterBookList.Add(TorannMagicDefOf.BookOfCommander);
            fighterBookList.Add(TorannMagicDefOf.BookOfSuperSoldier);
            foreach(TMDefs.TM_CustomClass cc in TM_ClassUtility.CustomClasses)
            {
                if (cc.isFighter && cc.fullScript != null)
                {
                    fighterBookList.Add(cc.fullScript);
                }
            }
            return fighterBookList;
        }

        public static List<ThingDef> MageBookList()
        {
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.defName.Contains("BookOf"))
                                               select def;

            enumerable = enumerable.Except(MageTornScriptList());
            return enumerable.Except(FighterBookList()).ToList();
        }

        public static List<ThingDef> AllBooksList()
        {
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.defName.Contains("BookOf"))
                                               select def;

            return enumerable.Except(MageTornScriptList()).ToList();
        }

        public static List<ThingDef> MageTornScriptList()
        {
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.defName.Contains("Torn_BookOf"))
                                               select def;

            return enumerable.ToList();
        }

        public static List<TraitDef> MagicTraits
        {
            get
            {
                List<TraitDef> magicTraits = new List<TraitDef>
                {
                    TorannMagicDefOf.Arcanist,
                    TorannMagicDefOf.InnerFire,
                    TorannMagicDefOf.HeartOfFrost,
                    TorannMagicDefOf.StormBorn,
                    TorannMagicDefOf.Druid,
                    TorannMagicDefOf.Priest,
                    TorannMagicDefOf.Necromancer,
                    TorannMagicDefOf.Technomancer,
                    TorannMagicDefOf.Geomancer,
                    TorannMagicDefOf.Warlock,
                    TorannMagicDefOf.Succubus,
                    TorannMagicDefOf.ChaosMage,
                    TorannMagicDefOf.Paladin,
                    TorannMagicDefOf.Summoner,
                    TorannMagicDefOf.Lich,
                    TorannMagicDefOf.TM_Bard,
                    TorannMagicDefOf.Chronomancer,
                    TorannMagicDefOf.Enchanter,
                    TorannMagicDefOf.BloodMage,
                    TorannMagicDefOf.TM_Wanderer,
                    TorannMagicDefOf.TM_Brightmage,
                    TorannMagicDefOf.TM_Shaman,
                    TorannMagicDefOf.TM_Golemancer
                };
                foreach (TMDefs.TM_CustomClass cc in TM_ClassUtility.CustomClasses)
                {
                    if (cc.isMage && !magicTraits.Contains(cc.classTrait))
                    {
                        magicTraits.Add(cc.classTrait);
                    }
                }
                return magicTraits;
            }
        }

        public static List<TraitDef> EnabledMagicTraits
        {
            get
            {
                List<TraitDef> magicTraits = new List<TraitDef>();
                
                if (ModOptions.Settings.Instance.Arcanist) { magicTraits.Add(TorannMagicDefOf.Arcanist); }
                if (ModOptions.Settings.Instance.FireMage) { magicTraits.Add(TorannMagicDefOf.InnerFire); }
                if (ModOptions.Settings.Instance.IceMage) { magicTraits.Add(TorannMagicDefOf.HeartOfFrost); }
                if (ModOptions.Settings.Instance.LitMage) { magicTraits.Add(TorannMagicDefOf.StormBorn); }
                if (ModOptions.Settings.Instance.Druid) { magicTraits.Add(TorannMagicDefOf.Druid); }
                if (ModOptions.Settings.Instance.Priest) { magicTraits.Add(TorannMagicDefOf.Priest); }
                if (ModOptions.Settings.Instance.Necromancer) { magicTraits.Add(TorannMagicDefOf.Necromancer); }
                if (ModOptions.Settings.Instance.Technomancer) { magicTraits.Add(TorannMagicDefOf.Technomancer); }
                if (ModOptions.Settings.Instance.Geomancer) { magicTraits.Add(TorannMagicDefOf.Geomancer); }
                if (ModOptions.Settings.Instance.Demonkin) { magicTraits.Add(TorannMagicDefOf.Warlock); }
                if (ModOptions.Settings.Instance.Demonkin) { magicTraits.Add(TorannMagicDefOf.Succubus); }
                if (ModOptions.Settings.Instance.ChaosMage) { magicTraits.Add(TorannMagicDefOf.ChaosMage); }
                if (ModOptions.Settings.Instance.Paladin) { magicTraits.Add(TorannMagicDefOf.Paladin); }
                if (ModOptions.Settings.Instance.Summoner) { magicTraits.Add(TorannMagicDefOf.Summoner); }
                if (ModOptions.Settings.Instance.Bard) { magicTraits.Add(TorannMagicDefOf.TM_Bard); }
                if (ModOptions.Settings.Instance.Chronomancer) { magicTraits.Add(TorannMagicDefOf.Chronomancer); }
                if (ModOptions.Settings.Instance.Enchanter) { magicTraits.Add(TorannMagicDefOf.Enchanter); }
                if (ModOptions.Settings.Instance.BloodMage) { magicTraits.Add(TorannMagicDefOf.BloodMage); }
                if (ModOptions.Settings.Instance.Brightmage) { magicTraits.Add(TorannMagicDefOf.TM_Brightmage); }
                if (ModOptions.Settings.Instance.Shaman) { magicTraits.Add(TorannMagicDefOf.TM_Shaman); }
                if (ModOptions.Settings.Instance.Golemancer) { magicTraits.Add(TorannMagicDefOf.TM_Golemancer); }
                foreach (TMDefs.TM_CustomClass cc in TM_ClassUtility.CustomClasses)
                {
                    if (cc.isMage && !magicTraits.Contains(cc.classTrait) && ModOptions.Settings.Instance.CustomClass[cc.classTrait.ToString()])
                    {
                        magicTraits.Add(cc.classTrait);
                    }
                }               
                return magicTraits;
            }
        }

        public static List<TraitDef> MightTraits
        {
            get
            {
                List<TraitDef> mightTraits = new List<TraitDef>
                {
                    TorannMagicDefOf.Bladedancer,
                    TorannMagicDefOf.DeathKnight,
                    TorannMagicDefOf.Gladiator,
                    TorannMagicDefOf.Faceless,
                    TorannMagicDefOf.TM_Sniper,
                    TorannMagicDefOf.Ranger,
                    TorannMagicDefOf.TM_Psionic,
                    TorannMagicDefOf.TM_Monk,
                    TorannMagicDefOf.TM_Commander,
                    TorannMagicDefOf.TM_SuperSoldier,
                    TorannMagicDefOf.TM_Wayfarer
                };
                foreach (TMDefs.TM_CustomClass cc in TM_ClassUtility.CustomClasses)
                {
                    if (cc.isFighter && !mightTraits.Contains(cc.classTrait))
                    {
                        mightTraits.Add(cc.classTrait);
                    }
                }
                return mightTraits;
            }
        }

        public static List<TraitDef> AllClassTraits
        {
            get
            {
                List<TraitDef> allClassTraits = new List<TraitDef>();
                allClassTraits.AddRange(MightTraits);
                allClassTraits.AddRange(MagicTraits);
                return allClassTraits;
            }
        }

        public static List<TraitDef> AllClassConflictTraits
        {
            get
            {
                List<TraitDef> allClassConflictTraits = new List<TraitDef>();
                allClassConflictTraits.AddRange(AllClassTraits);
                allClassConflictTraits.Add(TorannMagicDefOf.TM_Gifted);
                allClassConflictTraits.Add(TorannMagicDefOf.PhysicalProdigy);
                return allClassConflictTraits;
            }
        }

        public static List<TMAbilityDef> BrandList()
        {
            return new List<TMAbilityDef>
            {
                TorannMagicDefOf.TM_AwarenessBrand,
                TorannMagicDefOf.TM_EmotionBrand,
                TorannMagicDefOf.TM_FitnessBrand,
                TorannMagicDefOf.TM_ProtectionBrand,
                TorannMagicDefOf.TM_SiphonBrand,
                TorannMagicDefOf.TM_VitalityBrand
            };
        }

        private static HashSet<ThingDef> magicFociSet;
        public static HashSet<ThingDef> MagicFociList()
        {
            return magicFociSet ??= DefDatabase<ThingDef>.AllDefs.Where(static def =>
                WeaponCategoryList.Named("TM_Category_MagicalFoci").weaponDefNames.Contains(def.defName)).ToHashSet();
        }

        public static List<string> CustomWeaponCategoryList(string listDefName)
        {
            List<string> customWeaponDefNames = new List<string>();
            foreach(WeaponCategoryList wcl in DefDatabase<WeaponCategoryList>.AllDefs.Where(list => list.defName == listDefName))
            {
                customWeaponDefNames.AddRange(wcl.weaponDefNames);
            }
            return customWeaponDefNames;
        }

        private static HashSet<ThingDef> bowSet;
        public static HashSet<ThingDef> BowSet()
        {
            return bowSet ??= DefDatabase<ThingDef>.AllDefs.Where(static def =>
                WeaponCategoryList.Named("TM_Category_Bows").weaponDefNames.Contains(def.defName)).ToHashSet();
        }

        private static HashSet<ThingDef> pistolSet;
        public static HashSet<ThingDef> PistolSet()
        {
            return pistolSet ??= DefDatabase<ThingDef>.AllDefs.Where(static def =>
                WeaponCategoryList.Named("TM_Category_Pistols").weaponDefNames.Contains(def.defName)).ToHashSet();
        }

        private static HashSet<ThingDef> rifleSet;
        public static HashSet<ThingDef> RifleSet()
        {
            return rifleSet ??= DefDatabase<ThingDef>.AllDefs.Where(static def =>
                WeaponCategoryList.Named("TM_Category_Rifles").weaponDefNames.Contains(def.defName)).ToHashSet();
        }

        private static HashSet<ThingDef> shotgunSet;
        public static HashSet<ThingDef> ShotgunSet()
        {
            return shotgunSet ??= DefDatabase<ThingDef>.AllDefs.Where(static def =>
                WeaponCategoryList.Named("TM_Category_Shotguns").weaponDefNames.Contains(def.defName)).ToHashSet();
        }

        private static HashSet<HediffDef> ailmentSet;
        public static HashSet<HediffDef> AilmentSet()
        {
            return ailmentSet ??= DefDatabase<HediffDef>.AllDefs.Where(static def =>
                HediffCategoryList.Named("TM_Category_Hediffs").ailments.Any(hediff =>
                    hediff.hediffDefname == def.defName ||
                    hediff.containsDefnameString && def.defName.Contains(hediff.hediffDefname))
            ).ToHashSet();
        }

        private static HashSet<HediffDef> addictionSet;
        public static HashSet<HediffDef> AddictionSet()
        {
            return addictionSet ??= DefDatabase<HediffDef>.AllDefs.Where(static def =>
                HediffCategoryList.Named("TM_Category_Hediffs").addictions.Any(hediff =>
                    hediff.hediffDefname == def.defName ||
                    hediff.containsDefnameString && def.defName.Contains(hediff.hediffDefname))
                ).ToHashSet();
        }

        private static HashSet<HediffDef> mechaniteSet;
        public static HashSet<HediffDef> MechaniteSet()
        {
            return mechaniteSet ??= DefDatabase<HediffDef>.AllDefs.Where(static def =>
                HediffCategoryList.Named("TM_Category_Hediffs").mechanites.Any(hediff =>
                    hediff.hediffDefname == def.defName ||
                    hediff.containsDefnameString && def.defName.Contains(hediff.hediffDefname))
            ).ToHashSet();
        }

        private static HashSet<HediffDef> diseaseSet;
        public static HashSet<HediffDef> DiseaseSet()
        {
            return diseaseSet ??= DefDatabase<HediffDef>.AllDefs.Where(static def =>
                HediffCategoryList.Named("TM_Category_Hediffs").diseases.Any(hediff =>
                    hediff.hediffDefname == def.defName ||
                    hediff.containsDefnameString && def.defName.Contains(hediff.hediffDefname))
            ).ToHashSet();
        }

        private static TM_CustomPowerDef[] customFighterPowerDefs;
        public static TM_CustomPowerDef[] CustomFighterPowerDefs()
        {
            return customFighterPowerDefs ??= DefDatabase<TM_CustomPowerDef>.AllDefs.Where(static def =>
                def.customPower is { forFighter: true }).ToArray();
        }

        private static TM_CustomPowerDef[] customMagePowerDefs;
        public static IEnumerable<TM_CustomPowerDef> CustomMagePowerDefs()
        {
            return customMagePowerDefs ??= DefDatabase<TM_CustomPowerDef>.AllDefs.Where(static def =>
                def.customPower is { forMage: true }).ToArray();
        }

    }
}
