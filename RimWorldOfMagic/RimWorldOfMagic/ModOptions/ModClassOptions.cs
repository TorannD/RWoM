using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using System.Text;
using HarmonyLib;
using TorannMagic.TMDefs;
using TorannMagic.Utils;

namespace TorannMagic.ModOptions
{
    internal class ModClassOptions : Mod
    {
        // These get initialized in InitializeThingDefDictionaries since things must load first before ThingDefs can be accessed.
        private static Dictionary<ushort, Settings.CheckboxOption> thingDefIndexToSettingsRefMap;
        private static Dictionary<ushort, Settings.CheckboxOption> spellIndexToSettingsRefMap;
        private static Dictionary<ushort, (Settings.CheckboxOption option, ThingDef spell)> recipeIndexMap;

        public ModClassOptions(ModContentPack content) : base(content) { }

        // Requires TM_ClassUtility.InitializeCustomClasses and ModClassOptions.InitializeThingDefDictionaries to be ran before
        public static void ReloadSettings()
        {
            TM_ClassUtility.LoadCustomClasses();
            TM_ClassUtility.CacheEnabledClasses();
            CheckForDisabledCustomClass();
            RestrictClasses();
            InitializeFactionSettings();
        }

        public static void InitializeModBackstories()
        {
            BackstorySlot TM_BS = (BackstorySlot)13;            

            //BackstoryDef TM_SpiritBS = new BackstoryDef()
            //{
            //    identifier = "tm_childhood_spirit",
            //    slot = BackstorySlot.Childhood,
            //    title = "TM_SpiritVerbatum".Translate(),
            //    baseDesc = "TM_BaseSpiritDesc".Translate(),
            //};
            //BackstoryDatabase.AddBackstory(TM_SpiritBS);
            //Backstory TM_AncientSpiritBS = new Backstory()
            //{
            //    identifier = "tm_ancient_spirit",
            //    slot = BackstorySlot.Adulthood,
            //    title = "TM_AncientSpiritVerbatum".Translate(),
            //    baseDesc = "TM_AncientSpiritDesc".Translate(),
            //};
            //BackstoryDatabase.AddBackstory(TM_AncientSpiritBS);
            //Backstory TM_VengefulSpiritBS = new Backstory()
            //{
            //    identifier = "tm_vengeful_spirit",
            //    slot = BackstorySlot.Adulthood,
            //    title = "TM_VengefulSpiritVerbatum".Translate(),
            //    baseDesc = "TM_VengefulSpiritDesc".Translate(),
            //};
            //BackstoryDatabase.AddBackstory(TM_VengefulSpiritBS);
            //Backstory TM_LostSpiritBS = new Backstory()
            //{
            //    identifier = "tm_lost_spirit",
            //    slot = BackstorySlot.Adulthood,
            //    title = "TM_LostSpiritVerbatum".Translate(),
            //    baseDesc = "TM_LostSpiritDesc".Translate(),
            //};
            //BackstoryDatabase.AddBackstory(TM_LostSpiritBS);
            //Backstory TM_RegretSpiritBS = new Backstory()
            //{
            //    identifier = "tm_regret_spirit",
            //    slot = BackstorySlot.Adulthood,
            //    title = "TM_RegretSpiritVerbatum".Translate(),
            //    baseDesc = "TM_RegretSpiritDesc".Translate(),
            //};
            //BackstoryDatabase.AddBackstory(TM_RegretSpiritBS);
        }

        public static void InitializeFactionSettings()
        {
            ModOptions.FactionDictionary.InitializeFactionSettings();
        }

        public static void CheckForDisabledCustomClass()
        {
            for (int i = 0; i < TM_ClassUtility.CustomClasses.Length; i++)
            {
                TM_CustomClass customClass = TM_ClassUtility.CustomClasses[i];
                if(!Settings.Instance.CustomClass.Keys.Contains(customClass.classTrait.ToString()))
                {
                    Settings.Instance.CustomClass.Add(customClass.classTrait.ToString(), true);
                }
            }
        }

        public static void InitializeCustomClassActions()
        {
            //Conflicting trait levelset
            List<TraitDef> customTraits = new List<TraitDef>();
            const string customIconType = "TM_Icon_Custom";

            TM_CustomClassDef named = TM_CustomClassDef.Named("TM_CustomClasses");
            if (named == null) return;
            List<TM_CustomClass> customClasses = named.customClasses;

            for (int i = 0; i < customClasses.Count; i++)
            {
                TM_CustomClass customClass = customClasses[i];
                //customTraits.AddDistinct(customClass.classTrait);
                if (customTraits.Contains(customClass.classTrait))
                {
                    Log.Warning($"RimWorld of Magic trait {customClass.classTrait} already added. This is likely a naming conflict between mods.");
                }
                else
                {
                    // Map the trait to the texture to avoid having to TryGetComp
                    // Get material
                    Material mat = TM_RenderQueue.fighterMarkMat;
                    if (customClass.classIconPath != "")
                        mat = MaterialPool.MatFrom("Other/" + customClass.classIconPath);
                    else if (customClass.classTexturePath != "")
                        mat = MaterialPool.MatFrom("Other/ClassTextures/" + customClass.classTexturePath, true);
                    mat.color = customClass.classIconColor;

                    // Get texture
                    Texture2D customTexture = TM_MatPool.DefaultCustomMageIcon;
                    if (customClass.classTexturePath != "")
                    {
                        customTexture = ContentFinder<Texture2D>.Get("Other/ClassTextures/" + customClass.classTexturePath, true);
                    }

                    TraitIconMap.Set(customClass.classTrait, new TraitIconMap.TraitIconValue(mat, customTexture, customIconType));
                    // Add custom trait to list for processing
                    customTraits.Add(customClass.classTrait);
                }
                customClass.classTrait.conflictingTraits.AddRange(TM_ClassUtility.AllClassTraits);
            }

            IEnumerable<TraitDef> enumerable =
                DefDatabase<TraitDef>.AllDefs.Where(static def => TM_ClassUtility.AllClassTraits.Contains(def));

            foreach (TraitDef current in enumerable)
            {
                current.conflictingTraits.AddRange(customTraits);
            }

            for (int i = 0; i < customTraits.Count; i++)
            {
                for (int j = 0; j < customTraits.Count; j++)
                {
                    if (customTraits[i] != customTraits[j])
                    {
                        customTraits[i].conflictingTraits.Add(customTraits[j]);
                    }
                }
            }
        }
        public static void InitializeThingDefDictionaries()
        {
            thingDefIndexToSettingsRefMap = new Dictionary<ushort, Settings.CheckboxOption>
            {
                { TorannMagicDefOf.BookOfSniper.index, Settings.Sniper },
                { TorannMagicDefOf.BookOfRanger.index, Settings.Ranger },
                { ThingDef.Named("TM_PoisonTrap").index, Settings.Ranger },
                { TorannMagicDefOf.BookOfGladiator.index, Settings.Gladiator },
                { TorannMagicDefOf.BookOfBladedancer.index, Settings.Bladedancer },
                { TorannMagicDefOf.BookOfFaceless.index, Settings.Faceless },
                { TorannMagicDefOf.BookOfPsionic.index, Settings.Psionic },
                { TorannMagicDefOf.BookOfDeathKnight.index, Settings.DeathKnight },
                { TorannMagicDefOf.BookOfMonk.index, Settings.Monk },
                { TorannMagicDefOf.BookOfCommander.index, Settings.Commander },
                { TorannMagicDefOf.BookOfSuperSoldier.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_60mmMortar_Base.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base1.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base2.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base3.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base4.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base5.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base6.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base7.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base8.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base9.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base10.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base11.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base12.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base13.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base14.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base15.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base16.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base17.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base18.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base19.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base1.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base2.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base3.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base4.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base5.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base6.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base7.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base8.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base9.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base10.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base11.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base12.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base13.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base14.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base15.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base16.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base17.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base18.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base19.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base1.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base2.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base3.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base4.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base5.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base6.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base7.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base8.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base9.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base10.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base11.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base12.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base13.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base14.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base15.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base16.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base17.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base18.index, Settings.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base19.index, Settings.SuperSoldier },
                { TorannMagicDefOf.BookOfArcanist.index, Settings.Arcanist },
                { TorannMagicDefOf.Torn_BookOfArcanist.index, Settings.Arcanist },
                { TorannMagicDefOf.Torn_BookOfInnerFire.index, Settings.FireMage },
                { TorannMagicDefOf.BookOfInnerFire.index, Settings.FireMage },
                { TorannMagicDefOf.Torn_BookOfHeartOfFrost.index, Settings.IceMage },
                { TorannMagicDefOf.BookOfHeartOfFrost.index, Settings.IceMage },
                { TorannMagicDefOf.Torn_BookOfStormBorn.index, Settings.LitMage },
                { TorannMagicDefOf.BookOfStormBorn.index, Settings.LitMage },
                { TorannMagicDefOf.Torn_BookOfNature.index, Settings.Druid },
                { TorannMagicDefOf.BookOfDruid.index, Settings.Druid },
                { TorannMagicDefOf.SeedofRegrowth.index, Settings.Druid },
                { TorannMagicDefOf.Torn_BookOfSummoner.index, Settings.Summoner },
                { TorannMagicDefOf.BookOfSummoner.index, Settings.Summoner },
                { TorannMagicDefOf.TM_ManaMine.index, Settings.Summoner },
                { TorannMagicDefOf.TM_ManaMine_I.index, Settings.Summoner },
                { TorannMagicDefOf.TM_ManaMine_II.index, Settings.Summoner },
                { TorannMagicDefOf.TM_ManaMine_III.index, Settings.Summoner },
                { TorannMagicDefOf.DefensePylon.index, Settings.Summoner },
                { TorannMagicDefOf.DefensePylon_I.index, Settings.Summoner },
                { TorannMagicDefOf.DefensePylon_II.index, Settings.Summoner },
                { TorannMagicDefOf.DefensePylon_III.index, Settings.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon.index, Settings.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_I.index, Settings.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_II.index, Settings.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_III.index, Settings.Summoner },
                { TorannMagicDefOf.TM_Poppi.index, Settings.Summoner },
                { TorannMagicDefOf.Torn_BookOfValiant.index, Settings.Paladin },
                { TorannMagicDefOf.BookOfValiant.index, Settings.Paladin },
                { TorannMagicDefOf.Torn_BookOfPriest.index, Settings.Priest },
                { TorannMagicDefOf.BookOfPriest.index, Settings.Priest },
                { TorannMagicDefOf.Torn_BookOfBard.index, Settings.Bard },
                { TorannMagicDefOf.BookOfBard.index, Settings.Bard },
                { TorannMagicDefOf.BookOfNecromancer.index, Settings.Necromancer },
                { TorannMagicDefOf.Torn_BookOfUndead.index, Settings.Necromancer },
                { TorannMagicDefOf.TM_Artifact_NecroticOrb.index, Settings.Necromancer },
                { TorannMagicDefOf.Torn_BookOfEarth.index, Settings.Geomancer },
                { TorannMagicDefOf.BookOfEarth.index, Settings.Geomancer },
                { TorannMagicDefOf.TM_Lesser_SentinelR.index, Settings.Geomancer },
                { TorannMagicDefOf.TM_SentinelR.index, Settings.Geomancer },
                { TorannMagicDefOf.TM_Greater_SentinelR.index, Settings.Geomancer },
                { TorannMagicDefOf.Torn_BookOfDemons.index, Settings.Demonkin },
                { TorannMagicDefOf.BookOfDemons.index, Settings.Demonkin },
                { TorannMagicDefOf.Torn_BookOfMagitech.index, Settings.Technomancer },
                { TorannMagicDefOf.BookOfMagitech.index, Settings.Technomancer },
                { TorannMagicDefOf.Torn_BookOfHemomancy.index, Settings.BloodMage },
                { TorannMagicDefOf.BookOfHemomancy.index, Settings.BloodMage },
                { TorannMagicDefOf.Torn_BookOfEnchanter.index, Settings.Enchanter },
                { TorannMagicDefOf.BookOfEnchanter.index, Settings.Enchanter },
                { TorannMagicDefOf.Torn_BookOfChronomancer.index, Settings.Chronomancer },
                { TorannMagicDefOf.BookOfChronomancer.index, Settings.Chronomancer },
                { TorannMagicDefOf.Torn_BookOfChaos.index, Settings.ChaosMage },
                { TorannMagicDefOf.BookOfChaos.index, Settings.ChaosMage }
            };

            spellIndexToSettingsRefMap = new Dictionary<ushort, Settings.CheckboxOption>
            {
                { TorannMagicDefOf.SpellOf_FoldReality.index, Settings.Arcanist },
                { TorannMagicDefOf.SpellOf_Firestorm.index, Settings.FireMage },
                { TorannMagicDefOf.SpellOf_DryGround.index, Settings.FireMage },
                { TorannMagicDefOf.SpellOf_Blizzard.index, Settings.IceMage },
                { TorannMagicDefOf.SpellOf_WetGround.index, Settings.IceMage },
                { TorannMagicDefOf.SpellOf_EyeOfTheStorm.index, Settings.LitMage },
                { TorannMagicDefOf.SpellOf_ChargeBattery.index, Settings.LitMage },
                { TorannMagicDefOf.SpellOf_RegrowLimb.index, Settings.Druid },
                { TorannMagicDefOf.SpellOf_FertileLands.index, Settings.Druid },
                { TorannMagicDefOf.SpellOf_SummonPoppi.index, Settings.Summoner },
                { TorannMagicDefOf.SpellOf_HolyWrath.index, Settings.Paladin },
                { TorannMagicDefOf.SpellOf_Resurrection.index, Settings.Priest },
                { TorannMagicDefOf.SpellOf_BattleHymn.index, Settings.Bard },
                { TorannMagicDefOf.SpellOf_LichForm.index, Settings.Necromancer },
                { TorannMagicDefOf.SpellOf_Meteor.index, Settings.Geomancer },
                { TorannMagicDefOf.SpellOf_Scorn.index, Settings.Demonkin },
                { TorannMagicDefOf.SpellOf_PsychicShock.index, Settings.Demonkin },
                { TorannMagicDefOf.SpellOf_TechnoShield.index, Settings.Technomancer },
                { TorannMagicDefOf.SpellOf_Sabotage.index, Settings.Technomancer },
                { TorannMagicDefOf.SpellOf_Overdrive.index, Settings.Technomancer },
                { TorannMagicDefOf.SpellOf_OrbitalStrike.index, Settings.Technomancer },
                { TorannMagicDefOf.SpellOf_BloodMoon.index, Settings.BloodMage },
                { TorannMagicDefOf.SpellOf_Shapeshift.index, Settings.Enchanter },
                { TorannMagicDefOf.SpellOf_Recall.index, Settings.Chronomancer },
            };

            recipeIndexMap = new Dictionary<ushort, (Settings.CheckboxOption classOption, ThingDef item)>
            {
                {
                    TorannMagicDefOf.Make_SpellOf_FoldReality.index,
                    (Settings.Arcanist, TorannMagicDefOf.SpellOf_FoldReality)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Firestorm.index,
                    (Settings.FireMage, TorannMagicDefOf.SpellOf_Firestorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_DryGround.index,
                    (Settings.FireMage, TorannMagicDefOf.SpellOf_DryGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Blizzard.index,
                    (Settings.IceMage, TorannMagicDefOf.SpellOf_Blizzard)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_WetGround.index,
                    (Settings.IceMage, TorannMagicDefOf.SpellOf_WetGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_EyeOfTheStorm.index,
                    (Settings.LitMage, TorannMagicDefOf.SpellOf_EyeOfTheStorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_ChargeBattery.index,
                    (Settings.LitMage, TorannMagicDefOf.SpellOf_ChargeBattery)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_RegrowLimb.index,
                    (Settings.Druid, TorannMagicDefOf.SpellOf_RegrowLimb)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_FertileLands.index,
                    (Settings.Druid, TorannMagicDefOf.SpellOf_FertileLands)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_SummonPoppi.index,
                    (Settings.Summoner, TorannMagicDefOf.SpellOf_SummonPoppi)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_HolyWrath.index,
                    (Settings.Paladin, TorannMagicDefOf.SpellOf_HolyWrath)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Resurrection.index,
                    (Settings.Priest, TorannMagicDefOf.SpellOf_Resurrection)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BattleHymn.index,
                    (Settings.Bard, TorannMagicDefOf.SpellOf_BattleHymn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_LichForm.index,
                    (Settings.Necromancer, TorannMagicDefOf.SpellOf_LichForm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Meteor.index,
                    (Settings.Geomancer, TorannMagicDefOf.SpellOf_Meteor)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Scorn.index,
                    (Settings.Demonkin, TorannMagicDefOf.SpellOf_Scorn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_PsychicShock.index,
                    (Settings.Demonkin, TorannMagicDefOf.SpellOf_PsychicShock)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_TechnoShield.index,
                    (Settings.Technomancer, TorannMagicDefOf.SpellOf_TechnoShield)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Sabotage.index,
                    (Settings.Technomancer, TorannMagicDefOf.SpellOf_Sabotage)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Overdrive.index,
                    (Settings.Technomancer, TorannMagicDefOf.SpellOf_Overdrive)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_OrbitalStrike.index,
                    (Settings.Technomancer, TorannMagicDefOf.SpellOf_OrbitalStrike)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BloodMoon.index,
                    (Settings.BloodMage, TorannMagicDefOf.SpellOf_BloodMoon)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Shapeshift.index,
                    (Settings.Enchanter, TorannMagicDefOf.SpellOf_Shapeshift)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Recall.index,
                    (Settings.Chronomancer, TorannMagicDefOf.SpellOf_Recall)
                },
                {
                    TorannMagicDefOf.Make_BookOfSuperSoldier.index,
                    (Settings.SuperSoldier, TorannMagicDefOf.BookOfSuperSoldier)
                }
            };
        }

        public static Dictionary<TMAbilityDef, ThingDef[]> GetLinkedCustomFighterSkillThings()
        {
            return new Dictionary<TMAbilityDef, ThingDef[]>
            {
                { TorannMagicDefOf.TM_PoisonTrap, new[] { ThingDef.Named("TM_PoisonTrap") } },
                { TorannMagicDefOf.TM_60mmMortar, new[] { TorannMagicDefOf.TM_60mmMortar_Base } },
                { TorannMagicDefOf.TM_PistolSpec, new[] { TorannMagicDefOf.TM_PistolSpec_Base0 } },
                { TorannMagicDefOf.TM_RifleSpec, new[] { TorannMagicDefOf.TM_RifleSpec_Base0 } },
                { TorannMagicDefOf.TM_ShotgunSpec, new[] { TorannMagicDefOf.TM_ShotgunSpec_Base0 } }
            };
        }

        public static Dictionary<TMAbilityDef, ThingDef[]> GetLinkedCustomMageSkillThings()
        {
            return new Dictionary<TMAbilityDef, ThingDef[]>
            {
                { TorannMagicDefOf.TM_RegrowLimb, new[] { TorannMagicDefOf.SeedofRegrowth } },
                { TorannMagicDefOf.TM_SummonExplosive, new[]
                {
                    TorannMagicDefOf.TM_ManaMine,
                    TorannMagicDefOf.TM_ManaMine_I,
                    TorannMagicDefOf.TM_ManaMine_II,
                    TorannMagicDefOf.TM_ManaMine_III
                }},
                { TorannMagicDefOf.TM_SummonPylon, new[]
                {
                    TorannMagicDefOf.DefensePylon,
                    TorannMagicDefOf.DefensePylon_I,
                    TorannMagicDefOf.DefensePylon_II,
                    TorannMagicDefOf.DefensePylon_III,
                    TorannMagicDefOf.Bullet_DefensePylon,
                    TorannMagicDefOf.Launcher_DefensePylon,
                    TorannMagicDefOf.Launcher_DefensePylon_I,
                    TorannMagicDefOf.Launcher_DefensePylon_II,
                    TorannMagicDefOf.Launcher_DefensePylon_III
                }},
                { TorannMagicDefOf.TM_SummonPoppi, new[] { TorannMagicDefOf.TM_Poppi }},
                { TorannMagicDefOf.TM_RaiseUndead, new[] { TorannMagicDefOf.TM_Artifact_NecroticOrb }}
            };
        }

        public static void RestrictClasses()
        {
            HashSet<ThingDef> removedThings = new HashSet<ThingDef>();
            HashSet<ThingDef> customThings = new HashSet<ThingDef>();
            HashSet<ThingDef> removedCustomThings = new HashSet<ThingDef>();
            List<ThingDef> classSpells = new List<ThingDef>();
            List<ThingDef> removedSpells = new List<ThingDef>();
            TM_CustomClassDef customClassDef = TM_CustomClassDef.Named("TM_CustomClasses");
            if (customClassDef == null)
            {
                Log.Error("[Rimworld of Magic] Could not load Custom Classes. Something went wrong.");
                return;
            }

            for (int ccIndex = 0; ccIndex < customClassDef.customClasses.Count; ccIndex++)
            {
                TM_CustomClass cc = customClassDef.customClasses[ccIndex];
                if (Settings.Instance.CustomClass[cc.classTrait.ToString()])
                {
                    if (cc.tornScript != null) customThings.Add(cc.tornScript);
                    if (cc.fullScript != null) customThings.Add(cc.fullScript);
                    if (cc.learnableSkills != null) customThings.AddRange(cc.learnableSkills);
                    if (cc.learnableSpells != null) customThings.AddRange(cc.learnableSpells);
                    if (cc.classFighterAbilities != null)
                    {
                        for (int i = 0; i < cc.classFighterAbilities.Count; i++)
                        {
                            customThings.AddRange(GetLinkedCustomFighterSkillThings().TryGetValue(
                                cc.classFighterAbilities[i], Array.Empty<ThingDef>()));
                        }
                    }
                    if (cc.classMageAbilities != null)
                    {
                        for (int i = 0; i < cc.classMageAbilities.Count; i++)
                        {
                            customThings.AddRange(GetLinkedCustomMageSkillThings().TryGetValue(
                                cc.classMageAbilities[i], Array.Empty<ThingDef>()));
                        }
                    }
                }
                else
                {
                    if (cc.tornScript != null)
                    {
                        removedCustomThings.Add(cc.tornScript);
                    }
                    removedSpells.AddRange(cc.learnableSpells);
                    removedSpells.AddRange(cc.learnableSkills);
                    removedCustomThings.Add(cc.fullScript);
                }
            }

            void removeIfNotCustom(ThingDef thingDef)
            {
                if (!customThings.Contains(thingDef))
                    removedThings.Add(thingDef);
            }

            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                // First check if this is a normal ThingDef we have a setting for
                Settings.CheckboxOption option = thingDefIndexToSettingsRefMap.TryGetValue(current.index);
                if (option != null)
                {
                    if (!option.isEnabled)
                        removeIfNotCustom(current);
                }
                // If that fails, next we check for spells
                else
                {
                    option = spellIndexToSettingsRefMap.TryGetValue(current.index);
                    if (option == null) continue;

                    if (option.isEnabled)
                        classSpells.Add(current);
                    else
                        removeIfNotCustom(current);
                }
            }

            removedThings.UnionWith(removedCustomThings);

            for (int i = 0; i < removedSpells.Count; i++)
            {
                if (!customThings.Contains(removedSpells[i]) && !classSpells.Contains(removedSpells[i]))
                {
                    removedThings.Add(removedSpells[i]);
                    removedCustomThings.Add(removedSpells[i]);
                }
            }

            foreach (ThingDef thingDef in removedThings)
            {
                if (thingDef == null) continue;  // TODO: Find where the null is coming from.
                thingDef.resourceReadoutPriority = ResourceCountPriority.Uncounted;
                DefDatabase<ThingDef>.AllDefsListForReading.Remove(thingDef);
            }

            List<RecipeDef> removedRecipes = new List<RecipeDef>();
            Dictionary<string, RecipeDef> recipeDictionary = new Dictionary<string, RecipeDef>();
            bool anyRemovedCustomThings = removedCustomThings.Count > 0;

            foreach (RecipeDef current in DefDatabase<RecipeDef>.AllDefs)
            {
                if (anyRemovedCustomThings)
                    recipeDictionary[current.defName] = current;

                (Settings.CheckboxOption option, ThingDef item) = recipeIndexMap.TryGetValue(current.index);
                if (option == null) continue;
                if (option.isEnabled) continue;
                if (customThings.Contains(item)) continue;
                removedRecipes.Add(current);
            }

            foreach (ThingDef thingDef in removedCustomThings)
            {
                RecipeDef recipeDef = recipeDictionary.TryGetValue($"Make_{thingDef?.defName}");  // TODO: remove ? when the null todo above is taken care of
                if (recipeDef != null) removedRecipes.Add(recipeDef);
            }

            for (int i = 0; i < removedRecipes.Count; i++)
            {
                //Log.Message("removing " + removedRecipes[i].defName + " from def database");
                DefDatabase<RecipeDef>.AllDefsListForReading.Remove(removedRecipes[i]);
            }
        }

        private static void RemoveStuffFromDatabase(Type databaseType, IEnumerable<Def> defs)
        {
            IEnumerable<Def> enumerable = (defs as Def[]) ?? defs.ToArray();
            if (enumerable.Any())
            {
                Traverse traverse = Traverse.Create(databaseType).Method("Remove", enumerable.First());
                foreach (Def item in enumerable)
                {
                    //Log.Message("- " + item.label);
                    traverse.GetValue(item);
                }
            }
        }
    }
}
