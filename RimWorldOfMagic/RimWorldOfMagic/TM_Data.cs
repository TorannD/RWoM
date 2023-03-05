using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using RimWorld;
using System;
using TorannMagic.ModOptions;

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

        public static List<TraitDef> EnabledMagicTraits
        // DEPRECATED. Use TM_ClassUtility.EnabledMagicTraits instead. This is just here to prevent save breaking for ArcanePathway.
        {
            get
            {
                List<TraitDef> magicTraits = new List<TraitDef>();

                if (Settings.Arcanist.isEnabled) { magicTraits.Add(TorannMagicDefOf.Arcanist); }
                if (Settings.FireMage.isEnabled) { magicTraits.Add(TorannMagicDefOf.InnerFire); }
                if (Settings.IceMage.isEnabled) { magicTraits.Add(TorannMagicDefOf.HeartOfFrost); }
                if (Settings.LitMage.isEnabled) { magicTraits.Add(TorannMagicDefOf.StormBorn); }
                if (Settings.Druid.isEnabled) { magicTraits.Add(TorannMagicDefOf.Druid); }
                if (Settings.Priest.isEnabled) { magicTraits.Add(TorannMagicDefOf.Priest); }
                if (Settings.Necromancer.isEnabled) { magicTraits.Add(TorannMagicDefOf.Necromancer); }
                if (Settings.Technomancer.isEnabled) { magicTraits.Add(TorannMagicDefOf.Technomancer); }
                if (Settings.Geomancer.isEnabled) { magicTraits.Add(TorannMagicDefOf.Geomancer); }
                if (Settings.Demonkin.isEnabled) { magicTraits.Add(TorannMagicDefOf.Warlock); }
                if (Settings.Demonkin.isEnabled) { magicTraits.Add(TorannMagicDefOf.Succubus); }
                if (Settings.ChaosMage.isEnabled) { magicTraits.Add(TorannMagicDefOf.ChaosMage); }
                if (Settings.Paladin.isEnabled) { magicTraits.Add(TorannMagicDefOf.Paladin); }
                if (Settings.Summoner.isEnabled) { magicTraits.Add(TorannMagicDefOf.Summoner); }
                if (Settings.Bard.isEnabled) { magicTraits.Add(TorannMagicDefOf.TM_Bard); }
                if (Settings.Chronomancer.isEnabled) { magicTraits.Add(TorannMagicDefOf.Chronomancer); }
                if (Settings.Enchanter.isEnabled) { magicTraits.Add(TorannMagicDefOf.Enchanter); }
                if (Settings.BloodMage.isEnabled) { magicTraits.Add(TorannMagicDefOf.BloodMage); }
                if (Settings.Brightmage.isEnabled) { magicTraits.Add(TorannMagicDefOf.TM_Brightmage); }
                if (Settings.Shaman.isEnabled) { magicTraits.Add(TorannMagicDefOf.TM_Shaman); }
                if (Settings.Golemancer.isEnabled) { magicTraits.Add(TorannMagicDefOf.TM_Golemancer); }
                foreach (TMDefs.TM_CustomClass cc in TM_ClassUtility.CustomClasses)
                {
                    if (cc.isMage && !magicTraits.Contains(cc.classTrait) && Settings.Instance.CustomClass[cc.classTrait.ToString()])
                    {
                        magicTraits.Add(cc.classTrait);
                    }
                }
                return magicTraits;
            }
        }

        public static List<TMAbilityDef> BrandList()
        {
            List<TMAbilityDef> tmpList = new List<TMAbilityDef>();
            tmpList.Clear();
            tmpList.Add(TorannMagicDefOf.TM_AwarenessBrand);
            tmpList.Add(TorannMagicDefOf.TM_EmotionBrand);
            tmpList.Add(TorannMagicDefOf.TM_FitnessBrand);
            tmpList.Add(TorannMagicDefOf.TM_ProtectionBrand);
            tmpList.Add(TorannMagicDefOf.TM_SiphonBrand);
            tmpList.Add(TorannMagicDefOf.TM_VitalityBrand);
            return tmpList;
        }

        public static List<ThingDef> MagicFociList()
        {
            List<ThingDef> magicFocis = new List<ThingDef>();
            magicFocis.Clear();
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (true)
                                               select def;
            List<string> magicFociList = WeaponCategoryList.Named("TM_Category_MagicalFoci").weaponDefNames;
            foreach (ThingDef current in enumerable)
            {
                for (int i = 0; i < magicFociList.Count; i++)
                {
                    if (current.defName == magicFociList[i].ToString() || magicFociList[i].ToString() == "*")
                    {
                        //Log.Message("adding magicFoci def " + current.defName);
                        magicFocis.AddDistinct(current);
                    }
                }
            }
            return magicFocis;
        }

        public static List<string> CustomWeaponCategoryList(string listDefName)
        {
            List<string> customWeaponDefNames = new List<string>();
            customWeaponDefNames.Clear();
            IEnumerable<WeaponCategoryList> enumerable = from def in DefDatabase<WeaponCategoryList>.AllDefs
                                                         where (def.defName == listDefName)
                                                         select def;
            foreach(WeaponCategoryList wcl in enumerable)
            {
                customWeaponDefNames.AddRange(wcl.weaponDefNames);
            }
            return customWeaponDefNames;
        }

        public static List<ThingDef> BowList()
        {
            List<ThingDef> bows = new List<ThingDef>();
            bows.Clear();
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (true)
                                               select def;
            List<string> bowList = WeaponCategoryList.Named("TM_Category_Bows").weaponDefNames;
            foreach (ThingDef current in enumerable)
            {
                for (int i = 0; i < bowList.Count; i++)
                {
                    if (current.defName == bowList[i].ToString() || bowList[i].ToString() == "*")
                    {
                        //Log.Message("adding bow def " + current.defName);
                        bows.AddDistinct(current);
                    }
                }
            }
            return bows;
        }

        public static List<ThingDef> PistolList()
        {
            List<ThingDef> pistols = new List<ThingDef>();
            pistols.Clear();
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (true)
                                               select def;
            List<string> pistolList = WeaponCategoryList.Named("TM_Category_Pistols").weaponDefNames;
            foreach (ThingDef current in enumerable)
            {
                for (int i = 0; i < pistolList.Count; i++)
                {
                    if (current.defName == pistolList[i].ToString() || pistolList[i].ToString() == "*")
                    {
                        //Log.Message("adding pistol def " + current.defName);
                        pistols.AddDistinct(current);
                    }
                }
            }
            return pistols;
        }

        public static List<ThingDef> RifleList()
        {
            List<ThingDef> rifles = new List<ThingDef>();
            rifles.Clear();
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (true)
                                               select def;
            List<string> rifleList = WeaponCategoryList.Named("TM_Category_Rifles").weaponDefNames;
            foreach (ThingDef current in enumerable)
            {
                for (int i = 0; i < rifleList.Count; i++)
                {
                    if (current.defName == rifleList[i].ToString() || rifleList[i].ToString() == "*")
                    {
                        //Log.Message("adding rifle def " + current.defName);
                        rifles.AddDistinct(current);
                    }
                }
            }
            return rifles;
        }

        public static List<ThingDef> ShotgunList()
        {
            List<ThingDef> shotguns = new List<ThingDef>();
            shotguns.Clear();
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (true)
                                               select def;
            List<string> shotgunList = WeaponCategoryList.Named("TM_Category_Shotguns").weaponDefNames;
            foreach (ThingDef current in enumerable)
            {
                for (int i = 0; i < shotgunList.Count; i++)
                {
                    if (current.defName == shotgunList[i].ToString() || shotgunList[i].ToString() == "*")
                    {
                        //Log.Message("adding shotgun def " + current.defName);
                        shotguns.AddDistinct(current);
                    }
                }
            }
            return shotguns;
        }

        public static List<HediffDef> AilmentList()
        {
            List<HediffDef> ailments = new List<HediffDef>();
            ailments.Clear();
            IEnumerable<HediffDef> enumerable = from def in DefDatabase<HediffDef>.AllDefs
                                               where (true)
                                               select def;
            List<TMDefs.TM_CategoryHediff> ailmentList = HediffCategoryList.Named("TM_Category_Hediffs").ailments;
            foreach (HediffDef current in enumerable)
            {
                for (int i = 0; i < ailmentList.Count; i++)
                {
                    if (current.defName == ailmentList[i].hediffDefname || (ailmentList[i].containsDefnameString && current.defName.Contains(ailmentList[i].hediffDefname)) || ailmentList[i].ToString() == "*")
                    {
                        //Log.Message("adding shotgun def " + current.defName);
                        ailments.AddDistinct(current);
                    }
                }
            }
            return ailments;
        }

        public static List<HediffDef> AddictionList()
        {
            List<HediffDef> addictions = new List<HediffDef>();
            addictions.Clear();
            IEnumerable<HediffDef> enumerable = from def in DefDatabase<HediffDef>.AllDefs
                                                where (true)
                                                select def;
            List<TMDefs.TM_CategoryHediff> addictionList = HediffCategoryList.Named("TM_Category_Hediffs").addictions;
            foreach (HediffDef current in enumerable)
            {
                for (int i = 0; i < addictionList.Count; i++)
                {
                    if (current.defName == addictionList[i].hediffDefname || (addictionList[i].containsDefnameString && current.defName.Contains(addictionList[i].hediffDefname)) || addictionList[i].ToString() == "*")
                    {
                        //Log.Message("adding shotgun def " + current.defName);
                        addictions.AddDistinct(current);
                    }
                }
            }
            return addictions;
        }

        public static List<HediffDef> MechaniteList()
        {
            List<HediffDef> mechanites = new List<HediffDef>();
            mechanites.Clear();
            IEnumerable<HediffDef> enumerable = from def in DefDatabase<HediffDef>.AllDefs
                                                where (true)
                                                select def;
            List<TMDefs.TM_CategoryHediff> mechaniteList = HediffCategoryList.Named("TM_Category_Hediffs").mechanites;
            foreach (HediffDef current in enumerable)
            {
                for (int i = 0; i < mechaniteList.Count; i++)
                {
                    if (current.defName == mechaniteList[i].hediffDefname || (mechaniteList[i].containsDefnameString && current.defName.Contains(mechaniteList[i].hediffDefname)) || mechaniteList[i].ToString() == "*")
                    {
                        //Log.Message("adding shotgun def " + current.defName);
                        mechanites.AddDistinct(current);
                    }
                }
            }
            return mechanites;
        }

        public static List<HediffDef> DiseaseList()
        {
            List<HediffDef> diseases = new List<HediffDef>();
            diseases.Clear();
            IEnumerable<HediffDef> enumerable = from def in DefDatabase<HediffDef>.AllDefs
                                                where (true)
                                                select def;
            List<TMDefs.TM_CategoryHediff> diseaseList = HediffCategoryList.Named("TM_Category_Hediffs").diseases;
            foreach (HediffDef current in enumerable)
            {
                for (int i = 0; i < diseaseList.Count; i++)
                {
                    if (current.defName == diseaseList[i].hediffDefname || (diseaseList[i].containsDefnameString && current.defName.Contains(diseaseList[i].hediffDefname)) || diseaseList[i].ToString() == "*")
                    {
                        //Log.Message("adding shotgun def " + current.defName);
                        diseases.AddDistinct(current);
                    }
                }
            }
            return diseases;
        }

        public static IEnumerable<TM_CustomPowerDef> CustomFighterPowerDefs()
        {
            IEnumerable<TM_CustomPowerDef> enumerable = from def in DefDatabase<TM_CustomPowerDef>.AllDefs
                                                        where (def.customPower != null && def.customPower.forFighter)
                                                        select def;
            return enumerable;
        }

        public static IEnumerable<TM_CustomPowerDef> CustomMagePowerDefs()
        {
            IEnumerable<TM_CustomPowerDef> enumerable = from def in DefDatabase<TM_CustomPowerDef>.AllDefs
                                                        where (def.customPower != null && def.customPower.forMage)
                                                        select def;
            return enumerable;
        }

    }
}
