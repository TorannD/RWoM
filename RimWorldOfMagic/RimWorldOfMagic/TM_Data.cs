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
        //caching for lists so they're not generated every time
        private const int CacheTime = 60000; // cache for 12 ingame hours

        private static List<ThingDef> _SpellList = null;
        private static List<ThingDef> _MasterSpellList = null;
        private static List<ThingDef> _RestrictedAbilities = null;
        private static List<ThingDef> _RestrictedAbilitiesXML = null;
        private static List<ThingDef> _StandardSpellList = null;
        private static List<ThingDef> _StandardSkillList = null;
        private static List<ThingDef> _FighterBookList = null;
        private static List<ThingDef> _MageBookList = null;
        private static List<ThingDef> _AllBooksList = null;
        private static List<ThingDef> _MageTornScriptList = null;
        private static List<TraitDef> _MagicTraits = null;
        private static List<TraitDef> _EnabledMagicTraits = null;
        private static List<TraitDef> _MightTraits = null;
        private static List<TraitDef> _AllClassTraits = null;
        private static List<TraitDef> _AllClassConflictTraits = null;
        private static List<TMAbilityDef> _BrandList = null;
        private static List<ThingDef> _MagicFociList = null;
        private static List<ThingDef> _BowList = null;
        private static List<ThingDef> _PistolList = null;
        private static List<ThingDef> _RifleList = null;
        private static List<ThingDef> _ShotgunList = null;
        private static List<HediffDef> _AilmentList = null;
        private static List<HediffDef> _AddictionList = null;
        private static List<HediffDef> _MechaniteList = null;
        private static List<HediffDef> _DiseaseList = null;
        private static IEnumerable<TM_CustomPowerDef> _CustomFighterPowerDefs = null;
        private static IEnumerable<TM_CustomPowerDef> _CustomMagePowerDefs = null;

        private static readonly Dictionary<string, int> CacheTimers = new Dictionary<string, int>();

        // Runs on map load and when settings change
        public static void ResetCaches()
        {
            _EnabledMagicTraits = null;
            CacheTimers.Clear();
        }

        public static List<ThingDef> SpellList()
        {
            if (_SpellList == null)
            {
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.defName.Contains("SpellOf_"))
                                                   select def;
                _SpellList = enumerable.ToList();
            }
            return _SpellList;
        }

        public static List<ThingDef> MasterSpellList()
        {
            if (_MasterSpellList == null)
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
                _MasterSpellList = masterSpellList;
            }
            return _MasterSpellList;
        }

        public static List<ThingDef> RestrictedAbilities
        {
            get
            {
                if (_RestrictedAbilities == null)
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
                    _RestrictedAbilities = restricted;
                }
                return _RestrictedAbilities;
            }
        }

        public static List<ThingDef> RestrictedAbilitiesXML
        {
            get
            {
                if (_RestrictedAbilitiesXML == null)
                {
                    IEnumerable<TMAbilityDef> enumerable = from def in DefDatabase<TMAbilityDef>.AllDefs
                                                           where (def.restrictedAbility)
                                                           select def;
                    List<ThingDef> xmlRestrictedAbilities = new List<ThingDef>();
                    xmlRestrictedAbilities.Clear();
                    foreach (TMAbilityDef d in enumerable)
                    {
                        if (d.restrictedAbility && d.learnItem != null)
                        {
                            xmlRestrictedAbilities.Add(d.learnItem);
                        }
                    }
                    _RestrictedAbilitiesXML = xmlRestrictedAbilities.ToList();
                }
                return _RestrictedAbilitiesXML;
            }
        }

        public static List<ThingDef> StandardSpellList()
        {
            if(_StandardSpellList == null)
            {
                _StandardSpellList = SpellList().Except(MasterSpellList()).ToList();
            }
            return _StandardSpellList;
        }

        public static List<ThingDef> StandardSkillList()
        {
            if (_StandardSkillList == null)
            {
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.defName.Contains("SkillOf_"))
                                                   select def;
                _StandardSkillList = enumerable.ToList();
            }
            return _StandardSkillList;
        }

        public static List<ThingDef> FighterBookList()
        {
            if (_FighterBookList == null)
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
                foreach (TMDefs.TM_CustomClass cc in TM_ClassUtility.CustomClasses)
                {
                    if (cc.isFighter && cc.fullScript != null)
                    {
                        fighterBookList.Add(cc.fullScript);
                    }
                }
                _FighterBookList = fighterBookList;
            }
            return _FighterBookList;
        }

        public static List<ThingDef> MageBookList()
        {
            if (_MageBookList == null)
            {
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.defName.Contains("BookOf"))
                                                   select def;

                enumerable = enumerable.Except(MageTornScriptList());
                _MageBookList = enumerable.Except(FighterBookList()).ToList();
            }

            return _MageBookList;
        }

        public static List<ThingDef> AllBooksList()
        {
            if (_AllBooksList == null)
            {
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.defName.Contains("BookOf"))
                                                   select def;
                _AllBooksList = enumerable.Except(MageTornScriptList()).ToList();

            }
            return _AllBooksList;
        }

        public static List<ThingDef> MageTornScriptList()
        {
            if (_MageTornScriptList == null)
            {
                IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                   where (def.defName.Contains("Torn_BookOf"))
                                                   select def;
                _MageTornScriptList = enumerable.ToList();
            }

            return _MageTornScriptList;
        }

        public static List<TraitDef> MagicTraits
        {
            get
            {
                if (_MagicTraits == null)
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
                    _MagicTraits = magicTraits;
                }
                return _MagicTraits;
            }
        }

        public static List<TraitDef> EnabledMagicTraits
        {
            get
            {                
                if (_EnabledMagicTraits == null)
                {
                    List<TraitDef> magicTraits = new List<TraitDef>();
                    magicTraits.Clear();

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
                    _EnabledMagicTraits = magicTraits;
                }
                return _EnabledMagicTraits;
            }
        }

        public static List<TraitDef> MightTraits
        {
            get
            {
                if (_MightTraits == null)
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
                    _MightTraits = mightTraits;
                }
                return _MightTraits;
            }
        }

        public static List<TraitDef> AllClassTraits
        {
            get
            {
                if (_AllClassTraits == null)
                {
                    List<TraitDef> allClassTraits = new List<TraitDef>();
                    allClassTraits.Clear();
                    allClassTraits.AddRange(MightTraits);
                    allClassTraits.AddRange(MagicTraits);
                    //allClassTraits.AddRange(TM_ClassUtility.CustomClassTraitDefs);
                    _AllClassTraits = allClassTraits;
                }
                return _AllClassTraits;
            }
        }

        public static List<TraitDef> AllClassConflictTraits
        {
            get
            {
                if (_AllClassConflictTraits == null)
                {
                    List<TraitDef> allClassConflictTraits = new List<TraitDef>();
                    allClassConflictTraits.Clear();
                    allClassConflictTraits.AddRange(AllClassTraits);
                    allClassConflictTraits.Add(TorannMagicDefOf.TM_Gifted);
                    AllClassConflictTraits.Add(TorannMagicDefOf.PhysicalProdigy);
                    _AllClassConflictTraits = allClassConflictTraits;
                }
                return _AllClassConflictTraits;
            }
        }

        public static List<TMAbilityDef> BrandList()
        {
            if (_BrandList == null)
            {
                List<TMAbilityDef> tmpList = new List<TMAbilityDef>();
                tmpList.Clear();
                tmpList.Add(TorannMagicDefOf.TM_AwarenessBrand);
                tmpList.Add(TorannMagicDefOf.TM_EmotionBrand);
                tmpList.Add(TorannMagicDefOf.TM_FitnessBrand);
                tmpList.Add(TorannMagicDefOf.TM_ProtectionBrand);
                tmpList.Add(TorannMagicDefOf.TM_SiphonBrand);
                tmpList.Add(TorannMagicDefOf.TM_VitalityBrand);
                _BrandList = tmpList;
            }
            return _BrandList;
        }

        public static List<ThingDef> MagicFociList()
        {
            if (_MagicFociList == null)
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
                _MagicFociList = magicFocis;
            }
            return _MagicFociList;
        }

        private static readonly Dictionary<string, List<string>> CustomWeaponCategoryListCache = new Dictionary<string, List<string>>();
        public static List<string> CustomWeaponCategoryList(string listDefName)
        {
            if (CustomWeaponCategoryListCache.TryGetValue(listDefName, out var v))
            {
                return v;                
            }
            else
            {
                List<string> customWeaponDefNames = new List<string>();
                customWeaponDefNames.Clear();
                IEnumerable<WeaponCategoryList> enumerable = from def in DefDatabase<WeaponCategoryList>.AllDefs
                                                             where (def.defName == listDefName)
                                                             select def;
                foreach (WeaponCategoryList wcl in enumerable)
                {
                    customWeaponDefNames.AddRange(wcl.weaponDefNames);
                }
                CustomWeaponCategoryListCache.Add(listDefName, customWeaponDefNames);
                return customWeaponDefNames;
            }
        }

        public static List<ThingDef> BowList()
        {
            if (_BowList == null)
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
                _BowList = bows;
            }
            return _BowList;
        }

        public static List<ThingDef> PistolList()
        {
            if (_PistolList == null)
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
                _PistolList = pistols;
            }
            return _PistolList;
        }

        public static List<ThingDef> RifleList()
        {
            if (_RifleList == null)
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
                _RifleList = rifles;
            }
            return _RifleList;
        }

        public static List<ThingDef> ShotgunList()
        {
            if (_ShotgunList == null)
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
                _ShotgunList = shotguns;
            }
            return _ShotgunList;
        }

        public static List<HediffDef> AilmentList()
        {
            if (_AilmentList == null)
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
                _AilmentList = ailments;
            }
            return _AilmentList;
        }

        public static List<HediffDef> AddictionList()
        {
            if (_AddictionList == null)
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
                _AddictionList = addictions;
            }
            return _AddictionList;
        }

        public static List<HediffDef> MechaniteList()
        {
            if (_MechaniteList == null)
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
                _MechaniteList = mechanites;
            }
            return _MechaniteList;
        }

        public static List<HediffDef> DiseaseList()
        {
            if (_DiseaseList == null)
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
                _DiseaseList = diseases;
            }
            return _DiseaseList;
        }

        public static IEnumerable<TM_CustomPowerDef> CustomFighterPowerDefs()
        {
            if (_CustomFighterPowerDefs == null)
            {
                IEnumerable<TM_CustomPowerDef> enumerable = from def in DefDatabase<TM_CustomPowerDef>.AllDefs
                                                            where (def.customPower != null && def.customPower.forFighter)
                                                            select def;
                _CustomFighterPowerDefs = enumerable;
            }
            return _CustomFighterPowerDefs;
        }

        public static IEnumerable<TM_CustomPowerDef> CustomMagePowerDefs()
        {
            if (_CustomMagePowerDefs == null)
            {
                IEnumerable<TM_CustomPowerDef> enumerable = from def in DefDatabase<TM_CustomPowerDef>.AllDefs
                                                            where (def.customPower != null && def.customPower.forMage)
                                                            select def;
                _CustomMagePowerDefs = enumerable;
            }
            return _CustomMagePowerDefs;
        }

    }
}
