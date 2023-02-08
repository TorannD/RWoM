using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using System.Text;
using HarmonyLib;
using TorannMagic.Utils;

namespace TorannMagic.ModOptions
{
    internal class ModClassOptions : Mod
    {
        // These get initialized in InitializeThingDefDictionaries since things must load first before ThingDefs can be accessed.
        private static Dictionary<ushort, Func<bool>> thingDefIndexToSettingsMap;
        private static Dictionary<ushort, Func<bool>> spellIndexToSettingsMap;
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

        private static void InitializeFactionSettings()
        {
            ModOptions.FactionDictionary.InitializeFactionSettings();
        }

        private static void CheckForDisabledCustomClass()
        {
            if(Settings.Instance.CustomClass == null)
            {
                Settings.Instance.CustomClass = new Dictionary<string, bool>();
                Settings.Instance.CustomClass.Clear();
            }
            foreach (TMDefs.TM_CustomClass customClass in TM_ClassUtility.CustomClasses)
            {
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
            foreach (TMDefs.TM_CustomClass customClass in TM_ClassUtility.CustomClasses)
            {
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
            thingDefIndexToSettingsMap = new Dictionary<ushort, Func<bool>>
            {
                { TorannMagicDefOf.BookOfSniper.index, () => Settings.Instance.Sniper },
                { TorannMagicDefOf.BookOfRanger.index, () => Settings.Instance.Ranger },
                { TorannMagicDefOf.TM_PoisonTrap.index, () => Settings.Instance.Ranger },
                { TorannMagicDefOf.BookOfGladiator.index, () => Settings.Instance.Gladiator },
                { TorannMagicDefOf.BookOfBladedancer.index, () => Settings.Instance.Bladedancer },
                { TorannMagicDefOf.BookOfFaceless.index, () => Settings.Instance.Faceless },
                { TorannMagicDefOf.BookOfPsionic.index, () => Settings.Instance.Psionic },
                { TorannMagicDefOf.BookOfDeathKnight.index, () => Settings.Instance.DeathKnight },
                { TorannMagicDefOf.BookOfMonk.index, () => Settings.Instance.Monk },
                { TorannMagicDefOf.BookOfCommander.index, () => Settings.Instance.Commander },
                { TorannMagicDefOf.BookOfSuperSoldier.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_60mmMortar_Base.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base1.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base2.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base3.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base4.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base5.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base6.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base7.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base8.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base9.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base10.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base11.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base12.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base13.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base14.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base15.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base16.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base17.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base18.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base19.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base1.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base2.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base3.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base4.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base5.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base6.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base7.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base8.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base9.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base10.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base11.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base12.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base13.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base14.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base15.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base16.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base17.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base18.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base19.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base1.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base2.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base3.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base4.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base5.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base6.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base7.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base8.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base9.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base10.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base11.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base12.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base13.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base14.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base15.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base16.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base17.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base18.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base19.index, () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.BookOfArcanist.index, () => Settings.Instance.Arcanist },
                { TorannMagicDefOf.Torn_BookOfArcanist.index, () => Settings.Instance.Arcanist },
                { TorannMagicDefOf.Torn_BookOfInnerFire.index, () => Settings.Instance.FireMage },
                { TorannMagicDefOf.BookOfInnerFire.index, () => Settings.Instance.FireMage },
                { TorannMagicDefOf.Torn_BookOfHeartOfFrost.index, () => Settings.Instance.IceMage },
                { TorannMagicDefOf.BookOfHeartOfFrost.index, () => Settings.Instance.IceMage },
                { TorannMagicDefOf.Torn_BookOfStormBorn.index, () => Settings.Instance.LitMage },
                { TorannMagicDefOf.BookOfStormBorn.index, () => Settings.Instance.LitMage },
                { TorannMagicDefOf.Torn_BookOfNature.index, () => Settings.Instance.Druid },
                { TorannMagicDefOf.BookOfDruid.index, () => Settings.Instance.Druid },
                { TorannMagicDefOf.SeedofRegrowth.index, () => Settings.Instance.Druid },
                { TorannMagicDefOf.Torn_BookOfSummoner.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.BookOfSummoner.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine_I.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine_II.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine_III.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon_I.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon_II.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon_III.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_I.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_II.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_III.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_Poppi.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Torn_BookOfValiant.index, () => Settings.Instance.Paladin },
                { TorannMagicDefOf.BookOfValiant.index, () => Settings.Instance.Paladin },
                { TorannMagicDefOf.Torn_BookOfPriest.index, () => Settings.Instance.Priest },
                { TorannMagicDefOf.BookOfPriest.index, () => Settings.Instance.Priest },
                { TorannMagicDefOf.Torn_BookOfBard.index, () => Settings.Instance.Bard },
                { TorannMagicDefOf.BookOfBard.index, () => Settings.Instance.Bard },
                { TorannMagicDefOf.BookOfNecromancer.index, () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.Torn_BookOfUndead.index, () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.TM_Artifact_NecroticOrb.index, () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.Torn_BookOfEarth.index, () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.BookOfEarth.index, () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.TM_Lesser_SentinelR.index, () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.TM_SentinelR.index, () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.TM_Greater_SentinelR.index, () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.Torn_BookOfDemons.index, () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.BookOfDemons.index, () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.Torn_BookOfMagitech.index, () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.BookOfMagitech.index, () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.Torn_BookOfHemomancy.index, () => Settings.Instance.BloodMage },
                { TorannMagicDefOf.BookOfHemomancy.index, () => Settings.Instance.BloodMage },
                { TorannMagicDefOf.Torn_BookOfEnchanter.index, () => Settings.Instance.Enchanter },
                { TorannMagicDefOf.BookOfEnchanter.index, () => Settings.Instance.Enchanter },
                { TorannMagicDefOf.Torn_BookOfChronomancer.index, () => Settings.Instance.Chronomancer },
                { TorannMagicDefOf.BookOfChronomancer.index, () => Settings.Instance.Chronomancer },
                { TorannMagicDefOf.Torn_BookOfChaos.index, () => Settings.Instance.ChaosMage },
                { TorannMagicDefOf.BookOfChaos.index, () => Settings.Instance.ChaosMage }
            };

            spellIndexToSettingsMap = new Dictionary<ushort, Func<bool>>
            {
                { TorannMagicDefOf.SpellOf_FoldReality.index, () => Settings.Instance.Arcanist },
                { TorannMagicDefOf.SpellOf_Firestorm.index, () => Settings.Instance.FireMage },
                { TorannMagicDefOf.SpellOf_DryGround.index, () => Settings.Instance.FireMage },
                { TorannMagicDefOf.SpellOf_Blizzard.index, () => Settings.Instance.IceMage },
                { TorannMagicDefOf.SpellOf_WetGround.index, () => Settings.Instance.IceMage },
                { TorannMagicDefOf.SpellOf_EyeOfTheStorm.index, () => Settings.Instance.LitMage },
                { TorannMagicDefOf.SpellOf_ChargeBattery.index, () => Settings.Instance.LitMage },
                { TorannMagicDefOf.SpellOf_RegrowLimb.index, () => Settings.Instance.Druid },
                { TorannMagicDefOf.SpellOf_FertileLands.index, () => Settings.Instance.Druid },
                { TorannMagicDefOf.SpellOf_SummonPoppi.index, () => Settings.Instance.Summoner },
                { TorannMagicDefOf.SpellOf_HolyWrath.index, () => Settings.Instance.Paladin },
                { TorannMagicDefOf.SpellOf_Resurrection.index, () => Settings.Instance.Priest },
                { TorannMagicDefOf.SpellOf_BattleHymn.index, () => Settings.Instance.Bard },
                { TorannMagicDefOf.SpellOf_LichForm.index, () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.SpellOf_Meteor.index, () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.SpellOf_Scorn.index, () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.SpellOf_PsychicShock.index, () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.SpellOf_TechnoShield.index, () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_Sabotage.index, () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_Overdrive.index, () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_OrbitalStrike.index, () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_BloodMoon.index, () => Settings.Instance.BloodMage },
                { TorannMagicDefOf.SpellOf_Shapeshift.index, () => Settings.Instance.Enchanter },
                { TorannMagicDefOf.SpellOf_Recall.index, () => Settings.Instance.Chronomancer },
            };

            recipeIndexMap = new Dictionary<ushort, (Func<bool> settingsValueGetter, ThingDef item)>
            {
                {
                    TorannMagicDefOf.Make_SpellOf_FoldReality.index,
                    (() => Settings.Instance.Arcanist, TorannMagicDefOf.SpellOf_FoldReality)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Firestorm.index,
                    (() => Settings.Instance.FireMage, TorannMagicDefOf.SpellOf_Firestorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_DryGround.index,
                    (() => Settings.Instance.FireMage, TorannMagicDefOf.SpellOf_DryGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Blizzard.index,
                    (() => Settings.Instance.IceMage, TorannMagicDefOf.SpellOf_Blizzard)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_WetGround.index,
                    (() => Settings.Instance.IceMage, TorannMagicDefOf.SpellOf_WetGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_EyeOfTheStorm.index,
                    (() => Settings.Instance.LitMage, TorannMagicDefOf.SpellOf_EyeOfTheStorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_ChargeBattery.index,
                    (() => Settings.Instance.LitMage, TorannMagicDefOf.SpellOf_ChargeBattery)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_RegrowLimb.index,
                    (() => Settings.Instance.Druid, TorannMagicDefOf.SpellOf_RegrowLimb)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_FertileLands.index,
                    (() => Settings.Instance.Druid, TorannMagicDefOf.SpellOf_FertileLands)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_SummonPoppi.index,
                    (() => Settings.Instance.Summoner, TorannMagicDefOf.SpellOf_SummonPoppi)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_HolyWrath.index,
                    (() => Settings.Instance.Paladin, TorannMagicDefOf.SpellOf_HolyWrath)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Resurrection.index,
                    (() => Settings.Instance.Priest, TorannMagicDefOf.SpellOf_Resurrection)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BattleHymn.index,
                    (() => Settings.Instance.Bard, TorannMagicDefOf.SpellOf_BattleHymn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_LichForm.index,
                    (() => Settings.Instance.Necromancer, TorannMagicDefOf.SpellOf_LichForm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Meteor.index,
                    (() => Settings.Instance.Geomancer, TorannMagicDefOf.SpellOf_Meteor)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Scorn.index,
                    (() => Settings.Instance.Demonkin, TorannMagicDefOf.SpellOf_Scorn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_PsychicShock.index,
                    (() => Settings.Instance.Demonkin, TorannMagicDefOf.SpellOf_PsychicShock)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_TechnoShield.index,
                    (() => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_TechnoShield)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Sabotage.index,
                    (() => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_Sabotage)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Overdrive.index,
                    (() => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_Overdrive)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_OrbitalStrike.index,
                    (() => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_OrbitalStrike)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BloodMoon.index,
                    (() => Settings.Instance.BloodMage, TorannMagicDefOf.SpellOf_BloodMoon)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Shapeshift.index,
                    (() => Settings.Instance.Enchanter, TorannMagicDefOf.SpellOf_Shapeshift)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Recall.index,
                    (() => Settings.Instance.Chronomancer, TorannMagicDefOf.SpellOf_Recall)
                },
                {
                    TorannMagicDefOf.Make_BookOfSuperSoldier.index,
                    (() => Settings.Instance.SuperSoldier, TorannMagicDefOf.BookOfSuperSoldier)
                }
            };
        }

        private static void RestrictClasses()
        {
            IEnumerable<ThingDef> enumerable = (from def in DefDatabase<ThingDef>.AllDefs
                                                select def);
            List<ThingDef> removedThings = new List<ThingDef>();
            List<ThingDef> customThings = new List<ThingDef>();
            List<ThingDef> removedCustomThings = new List<ThingDef>();
            List<ThingDef> classSpells = new List<ThingDef>();
            List<ThingDef> removedSpells = new List<ThingDef>();
            TM_CustomClassDef ccDef = TM_CustomClassDef.Named("TM_CustomClasses");
            if (ccDef != null)
            {
                for (int i = 0; i < ccDef.customClasses.Count; i++)
                {
                    TMDefs.TM_CustomClass cc = ccDef.customClasses[i];
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
                        if (cc.tornScript != null)
                        {
                            removedCustomThings.Add(cc.tornScript);
                        }
                        //for (int k = 0; k < cc.learnableSpells.Count; k++)
                        //{
                        //    removedSpells.Add(cc.learnableSpells[k]);
                        //}
                        //for (int k = 0; k < cc.learnableSkills.Count; k++)
                        //{
                        //    removedSpells.Add(cc.learnableSkills[k]);
                        //}
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

                foreach (ThingDef current in enumerable)
                {
                    // First check if this is a normal ThingDef we have a setting for
                    Func<bool> settingsValueGetter = thingDefIndexToSettingsMap.TryGetValue(current.index);
                    if (settingsValueGetter != null)
                    {
                        if (!settingsValueGetter())
                            removeIfNotCustom(current);
                    }
                    // If that fails, next we check for spells
                    else
                    {
                        settingsValueGetter = spellIndexToSettingsMap.TryGetValue(current.index);
                        if (settingsValueGetter == null) continue;

                        if (settingsValueGetter())
                            classSpells.Add(current);
                        else
                            removeIfNotCustom(current);
                    }
                }

                //foreach (ThingDef current in enumerable)
                //{
                //    if (!Settings.Instance.Sniper)
                //    {
                //        if (current.defName == "BookOfSniper")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Ranger)
                //    {
                //        if (current.defName == "BookOfRanger" || current.defName == "TM_PoisonTrap")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Gladiator)
                //    {
                //        if (current.defName == "BookOfGladiator")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Bladedancer)
                //    {
                //        if (current.defName == "BookOfBladedancer")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Faceless)
                //    {
                //        if (current.defName == "BookOfFaceless")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Psionic)
                //    {
                //        if (current.defName == "BookOfPsionic")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.DeathKnight)
                //    {
                //        if (current.defName == "BookOfDeathKnight")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Monk)
                //    {
                //        if (current.defName == "BookOfMonk")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.Commander)
                //    {
                //        if (current == TorannMagicDefOf.BookOfCommander)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    if (!Settings.Instance.SuperSoldier)
                //    {
                //        if (current == TorannMagicDefOf.BookOfSuperSoldier || current == TorannMagicDefOf.TM_60mmMortar_Base)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //        if (current.defName.Contains("TM_PistolSpec_Base") && !customThings.Contains(ThingDef.Named("TM_PistolSpec_Base0")))
                //        {
                //            removedThings.Add(current);
                //        }
                //        if (current.defName.Contains("TM_RifleSpec_Base") && !customThings.Contains(ThingDef.Named("TM_RifleSpec_Base0")))
                //        {
                //            removedThings.Add(current);
                //        }
                //        if (current.defName.Contains("TM_ShotgunSpec_Base") && !customThings.Contains(ThingDef.Named("TM_ShotgunSpec_Base0")))
                //        {
                //            removedThings.Add(current);
                //        }
                //    }

                //    if (!Settings.Instance.Arcanist)
                //    {
                //        if (current.defName == "Torn_BookOfArcanist" || current.defName == "BookOfArcanist" || current.defName == "SpellOf_FoldReality")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_FoldReality")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.FireMage)
                //    {
                //        if (current.defName == "Torn_BookOfInnerFire" || current.defName == "BookOfInnerFire" || current.defName == "SpellOf_Firestorm" || current.defName == "SpellOf_DryGround")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_Firestorm" || current.defName == "SpellOf_DryGround")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.IceMage)
                //    {
                //        if (current.defName == "Torn_BookOfHeartOfFrost" || current.defName == "BookOfHeartOfFrost" || current.defName == "SpellOf_Blizzard" || current.defName == "SpellOf_WetGround")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_Blizzard" || current.defName == "SpellOf_WetGround")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.LitMage)
                //    {
                //        if (current.defName == "Torn_BookOfStormBorn" || current.defName == "BookOfStormBorn" || current.defName == "SpellOf_EyeOfTheStorm" || current.defName == "SpellOf_ChargeBattery")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_EyeOfTheStorm" || current.defName == "SpellOf_ChargeBattery")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Druid)
                //    {
                //        if (current.defName == "Torn_BookOfNature" || current.defName == "BookOfNature" || current.defName == "SpellOf_RegrowLimb" || current.defName == "SeedofRegrowth" || current.defName == "SpellOf_FertileLands")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_RegrowLimb" || current.defName == "SpellOf_FertileLands")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Summoner)
                //    {
                //        if (current.defName == "Torn_BookOfSummoner" || current.defName == "BookOfSummoner" || current.defName == "SpellOf_SummonPoppi" ||
                //            current.defName == "TM_ManaMine" || current.defName == "TM_ManaMine_I" || current.defName == "TM_ManaMine_II" || current.defName == "TM_ManaMine_III" ||
                //            current.defName == "DefensePylon" || current.defName == "DefensePylon_I" || current.defName == "DefensePylon_II" || current.defName == "DefensePylon_III" || current.defName == "Bullet_DefensePylon" ||
                //            current.defName == "Launcher_DefensePylon" || current.defName == "Launcher_DefensePylon_I" || current.defName == "Launcher_DefensePylon_II" || current.defName == "Launcher_DefensePylon_III" ||
                //            current.defName == "TM_Poppi")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_SummonPoppi")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Paladin)
                //    {
                //        if (current.defName == "Torn_BookOfValiant" || current.defName == "BookOfValiant" || current.defName == "SpellOf_HolyWrath")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_HolyWrath")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Priest)
                //    {
                //        if (current.defName == "Torn_BookOfPriest" || current.defName == "BookOfPriest" || current.defName == "SpellOf_Resurrection")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_Resurrection")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Bard)
                //    {
                //        if (current.defName == "Torn_BookOfBard" || current.defName == "BookOfBard" || current.defName == "SpellOf_BattleHymn")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_BattleHymn")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Necromancer)
                //    {
                //        if (current == TorannMagicDefOf.BookOfNecromancer || current == TorannMagicDefOf.Torn_BookOfUndead || current == TorannMagicDefOf.SpellOf_LichForm || current == TorannMagicDefOf.TM_Artifact_NecroticOrb)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current == TorannMagicDefOf.SpellOf_LichForm)
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Geomancer)
                //    {
                //        if (current.defName == "Torn_BookOfEarth" || current.defName == "BookOfEarth" || current.defName == "SpellOf_Meteor" ||
                //            current.defName == "TM_Lesser_SentinelR" || current.defName == "TM_SentinelR" || current.defName == "TM_Greater_SentinelR")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_Meteor")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Demonkin)
                //    {
                //        if (current.defName == "Torn_BookOfDemons" || current.defName == "BookOfDemons" || current.defName == "SpellOf_Scorn" || current.defName == "SpellOf_PsychicShock")
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current.defName == "SpellOf_Scorn" || current.defName == "SpellOf_PsychicShock")
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Technomancer)
                //    {
                //        if (current == TorannMagicDefOf.Torn_BookOfMagitech || current == TorannMagicDefOf.BookOfMagitech || current == TorannMagicDefOf.SpellOf_TechnoShield || current == TorannMagicDefOf.SpellOf_Sabotage || current == TorannMagicDefOf.SpellOf_Overdrive || current == TorannMagicDefOf.SpellOf_OrbitalStrike)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current == TorannMagicDefOf.SpellOf_TechnoShield || current == TorannMagicDefOf.SpellOf_Sabotage || current == TorannMagicDefOf.SpellOf_Overdrive || current == TorannMagicDefOf.SpellOf_OrbitalStrike)
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.BloodMage)
                //    {
                //        if (current == TorannMagicDefOf.BookOfHemomancy || current == TorannMagicDefOf.Torn_BookOfHemomancy || current == TorannMagicDefOf.SpellOf_BloodMoon)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current == TorannMagicDefOf.SpellOf_BloodMoon)
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Enchanter)
                //    {
                //        if (current == TorannMagicDefOf.BookOfEnchanter || current == TorannMagicDefOf.Torn_BookOfEnchanter || current == TorannMagicDefOf.SpellOf_Shapeshift)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current == TorannMagicDefOf.SpellOf_Shapeshift)
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.Chronomancer)
                //    {
                //        if (current == TorannMagicDefOf.BookOfChronomancer || current == TorannMagicDefOf.Torn_BookOfChronomancer || current == TorannMagicDefOf.SpellOf_Recall)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        if (current == TorannMagicDefOf.SpellOf_Recall)
                //        {
                //            classSpells.Add(current);
                //        }
                //    }
                //    if (!Settings.Instance.ChaosMage)
                //    {
                //        if (current == TorannMagicDefOf.BookOfChaos || current == TorannMagicDefOf.Torn_BookOfChaos)
                //        {
                //            if (!customThings.Contains(current))
                //            {
                //                removedThings.Add(current);
                //            }
                //        }
                //    }
                //}

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
                    //if (!Settings.Instance.Arcanist)
                    //{
                    //    if (current.defName == "Make_SpellOf_FoldReality" && !customThings.Contains(TorannMagicDefOf.SpellOf_FoldReality))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.FireMage)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Firestorm" && !customThings.Contains(TorannMagicDefOf.SpellOf_Firestorm)) || (current.defName == "Make_SpellOf_DryGround" && !customThings.Contains(TorannMagicDefOf.SpellOf_DryGround)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.IceMage)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Overdrive" && !customThings.Contains(TorannMagicDefOf.SpellOf_Overdrive)) || (current.defName == "Make_SpellOf_WetGround" && !customThings.Contains(TorannMagicDefOf.SpellOf_WetGround)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.LitMage)
                    //{
                    //    if ((current.defName == "Make_SpellOf_EyeOfTheStorm" && !customThings.Contains(TorannMagicDefOf.SpellOf_EyeOfTheStorm)) || (current.defName == "Make_SpellOf_ChargeBattery" && !customThings.Contains(TorannMagicDefOf.SpellOf_ChargeBattery)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Druid)
                    //{
                    //    if ((current.defName == "Make_SpellOf_RegrowLimb" && !customThings.Contains(TorannMagicDefOf.SpellOf_RegrowLimb)) || (current.defName == "Make_SpellOf_FertileLands" && !customThings.Contains(TorannMagicDefOf.SpellOf_FertileLands)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Summoner)
                    //{
                    //    if ((current.defName == "Make_SpellOf_SummonPoppi" && !customThings.Contains(TorannMagicDefOf.SpellOf_SummonPoppi)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Paladin)
                    //{
                    //    if ((current.defName == "Make_SpellOf_HolyWrath" && !customThings.Contains(TorannMagicDefOf.SpellOf_HolyWrath)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Priest)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Resurrection" && !customThings.Contains(TorannMagicDefOf.SpellOf_Resurrection)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Bard)
                    //{
                    //    if ((current.defName == "Make_SpellOf_BattleHymn" && !customThings.Contains(TorannMagicDefOf.SpellOf_BattleHymn)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Necromancer)
                    //{
                    //    if ((current.defName == "Make_SpellOf_FoldReality" && !customThings.Contains(TorannMagicDefOf.SpellOf_FoldReality)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Geomancer)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Meteor" && !customThings.Contains(TorannMagicDefOf.SpellOf_Meteor)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Demonkin)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Scorn" && !customThings.Contains(TorannMagicDefOf.SpellOf_Scorn)) || (current.defName == "Make_SpellOf_PsychicShock" && !customThings.Contains(TorannMagicDefOf.SpellOf_PsychicShock)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Technomancer)
                    //{
                    //    if ((current.defName == "Make_SpellOf_TechnoShield" && !customThings.Contains(TorannMagicDefOf.SpellOf_TechnoShield)) ||
                    //        (current.defName == "Make_SpellOf_Sabotage" && !customThings.Contains(TorannMagicDefOf.SpellOf_Sabotage)) ||
                    //        (current.defName == "Make_SpellOf_Overdrive" && !customThings.Contains(TorannMagicDefOf.SpellOf_Overdrive)) ||
                    //        (current.defName == "Make_SpellOf_OrbitalStrike" && !customThings.Contains(TorannMagicDefOf.SpellOf_OrbitalStrike)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.BloodMage)
                    //{
                    //    if ((current.defName == "Make_SpellOf_BloodMoon" && !customThings.Contains(TorannMagicDefOf.SpellOf_BloodMoon)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Enchanter)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Shapeshift" && !customThings.Contains(TorannMagicDefOf.SpellOf_Shapeshift)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.Chronomancer)
                    //{
                    //    if ((current.defName == "Make_SpellOf_Recall" && !customThings.Contains(TorannMagicDefOf.SpellOf_Recall)))
                    //    {
                    //        removedRecipes.Add(current);
                    //    }
                    //}
                    //if (!Settings.Instance.SuperSoldier)
                    //{
                    //    if (!Settings.Instance.SuperSoldier)
                    //    {
                    //        if ((current.defName == "Make_BookOfSuperSoldier" && !customThings.Contains(TorannMagicDefOf.BookOfSuperSoldier)))
                    //        {
                    //            removedRecipes.Add(current);
                    //        }
                    //    }
                    //}
                }

                if (anyRemovedCustomThings)
                {
                    removedRecipes.AddRange(removedCustomThings
                        .Select(td => recipeDictionary.TryGetValue($"Make_{td.defName}"))
                            .Where(recipeDef => recipeDef != null)
                                            );
                }
                //for (int i = 0; i < removedCustomThings.Count; i++)
                //{
                //    if (RecipeEnumerable.Any((RecipeDef x) => x.defName == "Make_" + removedCustomThings[i].defName))
                //    {
                //        removedRecipes.Add(RecipeEnumerable.FirstOrDefault<RecipeDef>((RecipeDef x) => x.defName == ("Make_" + removedCustomThings[i].ToString())));
                //    }
                //}

                for (int i = 0; i < removedRecipes.Count; i++)
                {
                    //Log.Message("removing " + removedRecipes[i].defName + " from def database");
                    DefDatabase<RecipeDef>.AllDefsListForReading.Remove(removedRecipes[i]);
                }
            }
            else
            {
                Log.Warning("RimWorld of Magic Class Restrictions failed to load properly - no classes were disabled.");
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
