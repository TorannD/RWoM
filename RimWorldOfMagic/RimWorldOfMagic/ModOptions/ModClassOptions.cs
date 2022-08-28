using RimWorld;
using System;
using System.Collections;
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
        private static Dictionary<ushort, Func<bool>> thingDefIndexToSettingsRefMap;
        private static Dictionary<ushort, Func<bool>> spellIndexToSettingsRefMap;
        private static Dictionary<ushort, (Func<bool> settingsValueGetter, ThingDef spell)> recipeIndexMap;

        public ModClassOptions(ModContentPack mcp) : base(mcp)
        {
            LongEventHandler.ExecuteWhenFinished(TM_ClassUtility.LoadCustomClasses);
            //TM_ClassUtility.LoadCustomClasses();
            LongEventHandler.ExecuteWhenFinished(CheckForDisabledCustomClass);
            LongEventHandler.ExecuteWhenFinished(InitializeThingDefDictionaries);
            LongEventHandler.ExecuteWhenFinished(RestrictClasses);
            LongEventHandler.ExecuteWhenFinished(InitializeFactionSettings);
            LongEventHandler.ExecuteWhenFinished(InitializeCustomClassActions);
            LongEventHandler.ExecuteWhenFinished(InitializeModBackstories);
        }

        private static void InitializeModBackstories()
        {
            Backstory TM_SpiritBS = new Backstory()
            {
                identifier = "tm_childhood_spirit",
                slot = BackstorySlot.Childhood,
                title = "TM_SpiritVerbatum".Translate(),
                baseDesc = "TM_BaseSpiritDesc".Translate(),
            };
            BackstoryDatabase.AddBackstory(TM_SpiritBS);
            Backstory TM_AncientSpiritBS = new Backstory()
            {
                identifier = "tm_ancient_spirit",
                slot = BackstorySlot.Adulthood,
                title = "TM_AncientSpiritVerbatum".Translate(),
                baseDesc = "TM_AncientSpiritDesc".Translate(),
            };
            BackstoryDatabase.AddBackstory(TM_AncientSpiritBS);
            Backstory TM_VengefulSpiritBS = new Backstory()
            {
                identifier = "tm_vengeful_spirit",
                slot = BackstorySlot.Adulthood,
                title = "TM_VengefulSpiritVerbatum".Translate(),
                baseDesc = "TM_VengefulSpiritDesc".Translate(),
            };
            BackstoryDatabase.AddBackstory(TM_VengefulSpiritBS);
            Backstory TM_LostSpiritBS = new Backstory()
            {
                identifier = "tm_lost_spirit",
                slot = BackstorySlot.Adulthood,
                title = "TM_LostSpiritVerbatum".Translate(),
                baseDesc = "TM_LostSpiritDesc".Translate(),
            };
            BackstoryDatabase.AddBackstory(TM_LostSpiritBS);
            Backstory TM_RegretSpiritBS = new Backstory()
            {
                identifier = "tm_regret_spirit",
                slot = BackstorySlot.Adulthood,
                title = "TM_RegretSpiritVerbatum".Translate(),
                baseDesc = "TM_RegretSpiritDesc".Translate(),
            };
            BackstoryDatabase.AddBackstory(TM_RegretSpiritBS);
        }

        private static void InitializeFactionSettings()
        {
            ModOptions.FactionDictionary.InitializeFactionSettings();
        }

        private static void CheckForDisabledCustomClass()
        {
            if(Settings.Instance.CustomClass == null)
            {
                Settings.Instance.CustomClass = new Dictionary<string, bool>();
            }
            for (int i = 0; i < TM_ClassUtility.CustomClasses.Count; i++)
            {
                TMDefs.TM_CustomClass customClass = TM_ClassUtility.CustomClasses[i];
                if(!Settings.Instance.CustomClass.Keys.Contains(customClass.classTrait.ToString()))
                {
                    Settings.Instance.CustomClass.Add(customClass.classTrait.ToString(), true);
                }
            }
        }

        private static void InitializeCustomClassActions()
        {
            //Conflicting trait levelset
            List<TraitDef> customTraits = new List<TraitDef>();
            customTraits.Clear();
            const string customIconType = "TM_Icon_Custom";

            for (int i = 0; i < TM_ClassUtility.CustomClasses.Count; i++)
            {
                TMDefs.TM_CustomClass customClass = TM_ClassUtility.CustomClasses[i];
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
                customClass.classTrait.conflictingTraits.AddRange(TM_Data.AllClassTraits);
            }

            IEnumerable<TraitDef> enumerable = from def in DefDatabase<TraitDef>.AllDefs
                                               where (TM_Data.AllClassTraits.Contains(def))
                                               select def;

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

        private static void InitializeThingDefDictionaries()
        {
            thingDefIndexToSettingsRefMap = new Dictionary<ushort, Func<bool>>
            {
                { TorannMagicDefOf.BookOfSniper.index, () => new SettingsRef().Sniper },
                { TorannMagicDefOf.BookOfRanger.index, () => new SettingsRef().Ranger },
                { TorannMagicDefOf.TM_PoisonTrap.index, () => new SettingsRef().Ranger },
                { TorannMagicDefOf.BookOfGladiator.index, () => new SettingsRef().Gladiator },
                { TorannMagicDefOf.BookOfBladedancer.index, () => new SettingsRef().Bladedancer },
                { TorannMagicDefOf.BookOfFaceless.index, () => new SettingsRef().Faceless },
                { TorannMagicDefOf.BookOfPsionic.index, () => new SettingsRef().Psionic },
                { TorannMagicDefOf.BookOfDeathKnight.index, () => new SettingsRef().DeathKnight },
                { TorannMagicDefOf.BookOfMonk.index, () => new SettingsRef().Monk },
                { TorannMagicDefOf.BookOfCommander.index, () => new SettingsRef().Commander },
                { TorannMagicDefOf.BookOfSuperSoldier.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_60mmMortar_Base.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base1.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base2.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base3.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base4.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base5.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base6.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base7.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base8.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base9.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base10.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base11.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base12.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base13.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base14.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base15.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base16.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base17.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base18.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base19.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base1.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base2.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base3.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base4.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base5.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base6.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base7.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base8.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base9.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base10.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base11.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base12.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base13.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base14.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base15.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base16.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base17.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base18.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base19.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base1.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base2.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base3.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base4.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base5.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base6.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base7.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base8.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base9.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base10.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base11.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base12.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base13.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base14.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base15.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base16.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base17.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base18.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base19.index, () => new SettingsRef().SuperSoldier },
                { TorannMagicDefOf.BookOfArcanist.index, () => new SettingsRef().Arcanist },
                { TorannMagicDefOf.Torn_BookOfArcanist.index, () => new SettingsRef().Arcanist },
                { TorannMagicDefOf.Torn_BookOfInnerFire.index, () => new SettingsRef().FireMage },
                { TorannMagicDefOf.BookOfInnerFire.index, () => new SettingsRef().FireMage },
                { TorannMagicDefOf.Torn_BookOfHeartOfFrost.index, () => new SettingsRef().IceMage },
                { TorannMagicDefOf.BookOfHeartOfFrost.index, () => new SettingsRef().IceMage },
                { TorannMagicDefOf.Torn_BookOfStormBorn.index, () => new SettingsRef().LitMage },
                { TorannMagicDefOf.BookOfStormBorn.index, () => new SettingsRef().LitMage },
                { TorannMagicDefOf.Torn_BookOfNature.index, () => new SettingsRef().Druid },
                { TorannMagicDefOf.BookOfDruid.index, () => new SettingsRef().Druid },
                { TorannMagicDefOf.SeedofRegrowth.index, () => new SettingsRef().Druid },
                { TorannMagicDefOf.Torn_BookOfSummoner.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.BookOfSummoner.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.TM_ManaMine.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.TM_ManaMine_I.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.TM_ManaMine_II.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.TM_ManaMine_III.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.DefensePylon.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.DefensePylon_I.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.DefensePylon_II.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.DefensePylon_III.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_I.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_II.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_III.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.TM_Poppi.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.Torn_BookOfValiant.index, () => new SettingsRef().Paladin },
                { TorannMagicDefOf.BookOfValiant.index, () => new SettingsRef().Paladin },
                { TorannMagicDefOf.Torn_BookOfPriest.index, () => new SettingsRef().Priest },
                { TorannMagicDefOf.BookOfPriest.index, () => new SettingsRef().Priest },
                { TorannMagicDefOf.Torn_BookOfBard.index, () => new SettingsRef().Bard },
                { TorannMagicDefOf.BookOfBard.index, () => new SettingsRef().Bard },
                { TorannMagicDefOf.BookOfNecromancer.index, () => new SettingsRef().Necromancer },
                { TorannMagicDefOf.Torn_BookOfUndead.index, () => new SettingsRef().Necromancer },
                { TorannMagicDefOf.TM_Artifact_NecroticOrb.index, () => new SettingsRef().Necromancer },
                { TorannMagicDefOf.Torn_BookOfEarth.index, () => new SettingsRef().Geomancer },
                { TorannMagicDefOf.BookOfEarth.index, () => new SettingsRef().Geomancer },
                { TorannMagicDefOf.TM_Lesser_SentinelR.index, () => new SettingsRef().Geomancer },
                { TorannMagicDefOf.TM_SentinelR.index, () => new SettingsRef().Geomancer },
                { TorannMagicDefOf.TM_Greater_SentinelR.index, () => new SettingsRef().Geomancer },
                { TorannMagicDefOf.Torn_BookOfDemons.index, () => new SettingsRef().Demonkin },
                { TorannMagicDefOf.BookOfDemons.index, () => new SettingsRef().Demonkin },
                { TorannMagicDefOf.Torn_BookOfMagitech.index, () => new SettingsRef().Technomancer },
                { TorannMagicDefOf.BookOfMagitech.index, () => new SettingsRef().Technomancer },
                { TorannMagicDefOf.Torn_BookOfHemomancy.index, () => new SettingsRef().BloodMage },
                { TorannMagicDefOf.BookOfHemomancy.index, () => new SettingsRef().BloodMage },
                { TorannMagicDefOf.Torn_BookOfEnchanter.index, () => new SettingsRef().Enchanter },
                { TorannMagicDefOf.BookOfEnchanter.index, () => new SettingsRef().Enchanter },
                { TorannMagicDefOf.Torn_BookOfChronomancer.index, () => new SettingsRef().Chronomancer },
                { TorannMagicDefOf.BookOfChronomancer.index, () => new SettingsRef().Chronomancer },
                { TorannMagicDefOf.Torn_BookOfChaos.index, () => new SettingsRef().ChaosMage },
                { TorannMagicDefOf.BookOfChaos.index, () => new SettingsRef().ChaosMage }
            };

            spellIndexToSettingsRefMap = new Dictionary<ushort, Func<bool>>
            {
                { TorannMagicDefOf.SpellOf_FoldReality.index, () => new SettingsRef().Arcanist },
                { TorannMagicDefOf.SpellOf_Firestorm.index, () => new SettingsRef().FireMage },
                { TorannMagicDefOf.SpellOf_DryGround.index, () => new SettingsRef().FireMage },
                { TorannMagicDefOf.SpellOf_Blizzard.index, () => new SettingsRef().IceMage },
                { TorannMagicDefOf.SpellOf_WetGround.index, () => new SettingsRef().IceMage },
                { TorannMagicDefOf.SpellOf_EyeOfTheStorm.index, () => new SettingsRef().LitMage },
                { TorannMagicDefOf.SpellOf_ChargeBattery.index, () => new SettingsRef().LitMage },
                { TorannMagicDefOf.SpellOf_RegrowLimb.index, () => new SettingsRef().Druid },
                { TorannMagicDefOf.SpellOf_FertileLands.index, () => new SettingsRef().Druid },
                { TorannMagicDefOf.SpellOf_SummonPoppi.index, () => new SettingsRef().Summoner },
                { TorannMagicDefOf.SpellOf_HolyWrath.index, () => new SettingsRef().Paladin },
                { TorannMagicDefOf.SpellOf_Resurrection.index, () => new SettingsRef().Priest },
                { TorannMagicDefOf.SpellOf_BattleHymn.index, () => new SettingsRef().Bard },
                { TorannMagicDefOf.SpellOf_LichForm.index, () => new SettingsRef().Necromancer },
                { TorannMagicDefOf.SpellOf_Meteor.index, () => new SettingsRef().Geomancer },
                { TorannMagicDefOf.SpellOf_Scorn.index, () => new SettingsRef().Demonkin },
                { TorannMagicDefOf.SpellOf_PsychicShock.index, () => new SettingsRef().Demonkin },
                { TorannMagicDefOf.SpellOf_TechnoShield.index, () => new SettingsRef().Technomancer },
                { TorannMagicDefOf.SpellOf_Sabotage.index, () => new SettingsRef().Technomancer },
                { TorannMagicDefOf.SpellOf_Overdrive.index, () => new SettingsRef().Technomancer },
                { TorannMagicDefOf.SpellOf_OrbitalStrike.index, () => new SettingsRef().Technomancer },
                { TorannMagicDefOf.SpellOf_BloodMoon.index, () => new SettingsRef().BloodMage },
                { TorannMagicDefOf.SpellOf_Shapeshift.index, () => new SettingsRef().Enchanter },
                { TorannMagicDefOf.SpellOf_Recall.index, () => new SettingsRef().Chronomancer },
            };

            recipeIndexMap = new Dictionary<ushort, (Func<bool> settingsValueGetter, ThingDef item)>
            {
                {
                    TorannMagicDefOf.Make_SpellOf_FoldReality.index,
                    (() => new SettingsRef().Arcanist, TorannMagicDefOf.SpellOf_FoldReality)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Firestorm.index,
                    (() => new SettingsRef().FireMage, TorannMagicDefOf.SpellOf_Firestorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_DryGround.index,
                    (() => new SettingsRef().FireMage, TorannMagicDefOf.SpellOf_DryGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Blizzard.index,
                    (() => new SettingsRef().IceMage, TorannMagicDefOf.SpellOf_Blizzard)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_WetGround.index,
                    (() => new SettingsRef().IceMage, TorannMagicDefOf.SpellOf_WetGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_EyeOfTheStorm.index,
                    (() => new SettingsRef().LitMage, TorannMagicDefOf.SpellOf_EyeOfTheStorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_ChargeBattery.index,
                    (() => new SettingsRef().LitMage, TorannMagicDefOf.SpellOf_ChargeBattery)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_RegrowLimb.index,
                    (() => new SettingsRef().Druid, TorannMagicDefOf.SpellOf_RegrowLimb)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_FertileLands.index,
                    (() => new SettingsRef().Druid, TorannMagicDefOf.SpellOf_FertileLands)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_SummonPoppi.index,
                    (() => new SettingsRef().Summoner, TorannMagicDefOf.SpellOf_SummonPoppi)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_HolyWrath.index,
                    (() => new SettingsRef().Paladin, TorannMagicDefOf.SpellOf_HolyWrath)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Resurrection.index,
                    (() => new SettingsRef().Priest, TorannMagicDefOf.SpellOf_Resurrection)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BattleHymn.index,
                    (() => new SettingsRef().Bard, TorannMagicDefOf.SpellOf_BattleHymn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_LichForm.index,
                    (() => new SettingsRef().Necromancer, TorannMagicDefOf.SpellOf_LichForm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Meteor.index,
                    (() => new SettingsRef().Geomancer, TorannMagicDefOf.SpellOf_Meteor)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Scorn.index,
                    (() => new SettingsRef().Demonkin, TorannMagicDefOf.SpellOf_Scorn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_PsychicShock.index,
                    (() => new SettingsRef().Demonkin, TorannMagicDefOf.SpellOf_PsychicShock)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_TechnoShield.index,
                    (() => new SettingsRef().Technomancer, TorannMagicDefOf.SpellOf_TechnoShield)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Sabotage.index,
                    (() => new SettingsRef().Technomancer, TorannMagicDefOf.SpellOf_Sabotage)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Overdrive.index,
                    (() => new SettingsRef().Technomancer, TorannMagicDefOf.SpellOf_Overdrive)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_OrbitalStrike.index,
                    (() => new SettingsRef().Technomancer, TorannMagicDefOf.SpellOf_OrbitalStrike)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BloodMoon.index,
                    (() => new SettingsRef().BloodMage, TorannMagicDefOf.SpellOf_BloodMoon)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Shapeshift.index,
                    (() => new SettingsRef().Enchanter, TorannMagicDefOf.SpellOf_Shapeshift)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Recall.index,
                    (() => new SettingsRef().Chronomancer, TorannMagicDefOf.SpellOf_Recall)
                },
                {
                    TorannMagicDefOf.Make_BookOfSuperSoldier.index,
                    (() => new SettingsRef().SuperSoldier, TorannMagicDefOf.BookOfSuperSoldier)
                }
            };
        }

        private static void RestrictClasses()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            IEnumerable<ThingDef> enumerable = (from def in DefDatabase<ThingDef>.AllDefs
                                                select def);
            List<ThingDef> removedThings = new List<ThingDef>();
            List<ThingDef> customThings = new List<ThingDef>();
            List<ThingDef> removedCustomThings = new List<ThingDef>();
            List<ThingDef> classSpells = new List<ThingDef>();
            List<ThingDef> removedSpells = new List<ThingDef>();

            List<TM_CustomClass> customClasses =
                TM_CustomClassDef.Named("TM_CustomClasses")?.customClasses ?? new List<TM_CustomClass>();
            for (int i = 0; i < customClasses.Count; i++)
            {
                TM_CustomClass cc = customClasses[i];
                if (Settings.Instance.CustomClass[cc.classTrait.ToString()])
                {
                    if (cc.tornScript != null)
                    {
                        customThings.AddDistinct(cc.tornScript);
                    }
                    if (cc.fullScript != null)
                    {
                        customThings.AddDistinct(cc.fullScript);
                    }
                    if (cc.learnableSkills != null && cc.learnableSkills.Count > 0)
                    {
                        for (int j = 0; j < cc.learnableSkills.Count; j++)
                        {
                            customThings.AddDistinct(cc.learnableSkills[j]);
                        }
                    }
                    if (cc.learnableSpells != null && cc.learnableSpells.Count > 0)
                    {
                        for (int j = 0; j < cc.learnableSpells.Count; j++)
                        {
                            customThings.AddDistinct(cc.learnableSpells[j]);
                        }
                    }
                    if (cc.classFighterAbilities != null && cc.classFighterAbilities.Count > 0)
                    {
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_PoisonTrap))
                        {
                            customThings.AddDistinct(ThingDef.Named("TM_PoisonTrap"));
                        }
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_60mmMortar))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_60mmMortar_Base);
                        }
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_PistolSpec))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_PistolSpec_Base0);
                        }
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_RifleSpec))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_RifleSpec_Base0);
                        }
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_ShotgunSpec))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_ShotgunSpec_Base0);
                        }
                    }
                    if (cc.classMageAbilities != null && cc.classMageAbilities.Count > 0)
                    {
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_RegrowLimb))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.SeedofRegrowth);
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_SummonExplosive))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_ManaMine);
                            customThings.AddDistinct(TorannMagicDefOf.TM_ManaMine_I);
                            customThings.AddDistinct(TorannMagicDefOf.TM_ManaMine_II);
                            customThings.AddDistinct(TorannMagicDefOf.TM_ManaMine_III);
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_SummonPylon))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.DefensePylon);
                            customThings.AddDistinct(TorannMagicDefOf.DefensePylon_I);
                            customThings.AddDistinct(TorannMagicDefOf.DefensePylon_II);
                            customThings.AddDistinct(TorannMagicDefOf.DefensePylon_III);
                            customThings.AddDistinct(TorannMagicDefOf.Bullet_DefensePylon);
                            customThings.AddDistinct(TorannMagicDefOf.Launcher_DefensePylon);
                            customThings.AddDistinct(TorannMagicDefOf.Launcher_DefensePylon_I);
                            customThings.AddDistinct(TorannMagicDefOf.Launcher_DefensePylon_II);
                            customThings.AddDistinct(TorannMagicDefOf.Launcher_DefensePylon_III);
                            customThings.AddDistinct(TorannMagicDefOf.TM_Poppi);
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_SummonPoppi))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_Poppi);
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_RaiseUndead))
                        {
                            customThings.AddDistinct(TorannMagicDefOf.TM_Artifact_NecroticOrb);
                        }
                    }
                }
                else
                {
                    if(cc.tornScript != null)
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
                if(!customThings.Contains(thingDef))
                    removedThings.Add(thingDef);
            }

            foreach (ThingDef current in enumerable)
            {
                // First check if this is a normal ThingDef we have a setting for
                Func<bool> settingsValueGetter = thingDefIndexToSettingsRefMap.TryGetValue(current.index);
                if (settingsValueGetter != null)
                {
                    if (!settingsValueGetter())
                        removeIfNotCustom(current);
                }
                // If that fails, next we check for spells
                else
                {
                    settingsValueGetter = spellIndexToSettingsRefMap.TryGetValue(current.index);
                    if (settingsValueGetter == null) continue;

                    if (settingsValueGetter())
                        classSpells.Add(current);
                    else
                        removeIfNotCustom(current);
                }
            }

            for (int i = 0; i < removedCustomThings.Count; i++)
            {
                if (!removedThings.Contains(removedCustomThings[i]))
                {
                    removedThings.Add(removedCustomThings[i]);
                }
            }

            for (int i = 0; i < removedSpells.Count; i++)
            {
                if (!customThings.Contains(removedSpells[i]) && !classSpells.Contains(removedSpells[i]))
                {
                    removedThings.Add(removedSpells[i]);
                    removedCustomThings.Add(removedSpells[i]);
                }
            }

            for (int i = 0; i < removedThings.Count; i++)
            {
                //Log.Message("removing " + removedThings[i].defName + " from def database");
                removedThings[i].resourceReadoutPriority = ResourceCountPriority.Uncounted;
                DefDatabase<ThingDef>.AllDefsListForReading.Remove(removedThings[i]);
            }

            IEnumerable<RecipeDef> RecipeEnumerable = DefDatabase<RecipeDef>.AllDefs;
            List<RecipeDef> removedRecipes = new List<RecipeDef>();
            Dictionary<string, RecipeDef> recipeDictionary = new Dictionary<string, RecipeDef>();

            bool anyRemovedCustomThings = removedCustomThings.Count > 0;
            foreach (RecipeDef current in RecipeEnumerable)
            {
                if (anyRemovedCustomThings)
                    recipeDictionary[current.defName] = current;

                (Func<bool> settingsValueGetter, ThingDef item) = recipeIndexMap.TryGetValue(current.index);
                if (settingsValueGetter == null) continue;
                if (settingsValueGetter()) continue;
                if (customThings.Contains(item)) continue;
                removedRecipes.Add(current);
            }

            if (anyRemovedCustomThings)
            {
                removedRecipes.AddRange(
                    removedCustomThings
                        .Select(td => recipeDictionary.TryGetValue($"Make_{td.defName}"))
                        .Where(recipeDef => recipeDef != null)
                );
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
