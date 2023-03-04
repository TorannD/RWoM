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
        private static Dictionary<ushort, Func<bool>> thingDefIndexToSettingsRefMap;
        private static Dictionary<ushort, Func<bool>> spellIndexToSettingsRefMap;
        private static Dictionary<ushort, (Func<bool> settingsValueGetter, ThingDef spell)> recipeIndexMap;

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
            thingDefIndexToSettingsRefMap = new Dictionary<ushort, Func<bool>>
            {
                { TorannMagicDefOf.BookOfSniper.index, static () => Settings.Instance.Sniper },
                { TorannMagicDefOf.BookOfRanger.index, static () => Settings.Instance.Ranger },
                // { ThingDef.Named("TM_PoisonTrap").index, static () => Settings.Instance.Ranger },
                { TorannMagicDefOf.BookOfGladiator.index, static () => Settings.Instance.Gladiator },
                { TorannMagicDefOf.BookOfBladedancer.index, static () => Settings.Instance.Bladedancer },
                { TorannMagicDefOf.BookOfFaceless.index, static () => Settings.Instance.Faceless },
                { TorannMagicDefOf.BookOfPsionic.index, static () => Settings.Instance.Psionic },
                { TorannMagicDefOf.BookOfDeathKnight.index, static () => Settings.Instance.DeathKnight },
                { TorannMagicDefOf.BookOfMonk.index, static () => Settings.Instance.Monk },
                { TorannMagicDefOf.BookOfCommander.index, static () => Settings.Instance.Commander },
                { TorannMagicDefOf.BookOfSuperSoldier.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_60mmMortar_Base.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base1.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base2.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base3.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base4.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base5.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base6.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base7.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base8.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base9.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base10.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base11.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base12.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base13.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base14.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base15.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base16.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base17.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base18.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_PistolSpec_Base19.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base1.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base2.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base3.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base4.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base5.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base6.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base7.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base8.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base9.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base10.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base11.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base12.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base13.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base14.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base15.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base16.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base17.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base18.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_RifleSpec_Base19.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base1.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base2.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base3.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base4.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base5.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base6.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base7.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base8.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base9.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base10.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base11.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base12.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base13.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base14.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base15.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base16.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base17.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base18.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.TM_ShotgunSpec_Base19.index, static () => Settings.Instance.SuperSoldier },
                { TorannMagicDefOf.BookOfArcanist.index, static () => Settings.Instance.Arcanist },
                { TorannMagicDefOf.Torn_BookOfArcanist.index, static () => Settings.Instance.Arcanist },
                { TorannMagicDefOf.Torn_BookOfInnerFire.index, static () => Settings.Instance.FireMage },
                { TorannMagicDefOf.BookOfInnerFire.index, static () => Settings.Instance.FireMage },
                { TorannMagicDefOf.Torn_BookOfHeartOfFrost.index, static () => Settings.Instance.IceMage },
                { TorannMagicDefOf.BookOfHeartOfFrost.index, static () => Settings.Instance.IceMage },
                { TorannMagicDefOf.Torn_BookOfStormBorn.index, static () => Settings.Instance.LitMage },
                { TorannMagicDefOf.BookOfStormBorn.index, static () => Settings.Instance.LitMage },
                { TorannMagicDefOf.Torn_BookOfNature.index, static () => Settings.Instance.Druid },
                { TorannMagicDefOf.BookOfDruid.index, static () => Settings.Instance.Druid },
                { TorannMagicDefOf.SeedofRegrowth.index, static () => Settings.Instance.Druid },
                { TorannMagicDefOf.Torn_BookOfSummoner.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.BookOfSummoner.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine_I.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine_II.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_ManaMine_III.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon_I.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon_II.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.DefensePylon_III.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_I.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_II.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Launcher_DefensePylon_III.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.TM_Poppi.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.Torn_BookOfValiant.index, static () => Settings.Instance.Paladin },
                { TorannMagicDefOf.BookOfValiant.index, static () => Settings.Instance.Paladin },
                { TorannMagicDefOf.Torn_BookOfPriest.index, static () => Settings.Instance.Priest },
                { TorannMagicDefOf.BookOfPriest.index, static () => Settings.Instance.Priest },
                { TorannMagicDefOf.Torn_BookOfBard.index, static () => Settings.Instance.Bard },
                { TorannMagicDefOf.BookOfBard.index, static () => Settings.Instance.Bard },
                { TorannMagicDefOf.BookOfNecromancer.index, static () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.Torn_BookOfUndead.index, static () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.TM_Artifact_NecroticOrb.index, static () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.Torn_BookOfEarth.index, static () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.BookOfEarth.index, static () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.TM_Lesser_SentinelR.index, static () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.TM_SentinelR.index, static () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.TM_Greater_SentinelR.index, static () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.Torn_BookOfDemons.index, static () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.BookOfDemons.index, static () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.Torn_BookOfMagitech.index, static () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.BookOfMagitech.index, static () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.Torn_BookOfHemomancy.index, static () => Settings.Instance.BloodMage },
                { TorannMagicDefOf.BookOfHemomancy.index, static () => Settings.Instance.BloodMage },
                { TorannMagicDefOf.Torn_BookOfEnchanter.index, static () => Settings.Instance.Enchanter },
                { TorannMagicDefOf.BookOfEnchanter.index, static () => Settings.Instance.Enchanter },
                { TorannMagicDefOf.Torn_BookOfChronomancer.index, static () => Settings.Instance.Chronomancer },
                { TorannMagicDefOf.BookOfChronomancer.index, static () => Settings.Instance.Chronomancer },
                { TorannMagicDefOf.Torn_BookOfChaos.index, static () => Settings.Instance.ChaosMage },
                { TorannMagicDefOf.BookOfChaos.index, static () => Settings.Instance.ChaosMage }
            };

            spellIndexToSettingsRefMap = new Dictionary<ushort, Func<bool>>
            {
                { TorannMagicDefOf.SpellOf_FoldReality.index, static () => Settings.Instance.Arcanist },
                { TorannMagicDefOf.SpellOf_Firestorm.index, static () => Settings.Instance.FireMage },
                { TorannMagicDefOf.SpellOf_DryGround.index, static () => Settings.Instance.FireMage },
                { TorannMagicDefOf.SpellOf_Blizzard.index, static () => Settings.Instance.IceMage },
                { TorannMagicDefOf.SpellOf_WetGround.index, static () => Settings.Instance.IceMage },
                { TorannMagicDefOf.SpellOf_EyeOfTheStorm.index, static () => Settings.Instance.LitMage },
                { TorannMagicDefOf.SpellOf_ChargeBattery.index, static () => Settings.Instance.LitMage },
                { TorannMagicDefOf.SpellOf_RegrowLimb.index, static () => Settings.Instance.Druid },
                { TorannMagicDefOf.SpellOf_FertileLands.index, static () => Settings.Instance.Druid },
                { TorannMagicDefOf.SpellOf_SummonPoppi.index, static () => Settings.Instance.Summoner },
                { TorannMagicDefOf.SpellOf_HolyWrath.index, static () => Settings.Instance.Paladin },
                { TorannMagicDefOf.SpellOf_Resurrection.index, static () => Settings.Instance.Priest },
                { TorannMagicDefOf.SpellOf_BattleHymn.index, static () => Settings.Instance.Bard },
                { TorannMagicDefOf.SpellOf_LichForm.index, static () => Settings.Instance.Necromancer },
                { TorannMagicDefOf.SpellOf_Meteor.index, static () => Settings.Instance.Geomancer },
                { TorannMagicDefOf.SpellOf_Scorn.index, static () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.SpellOf_PsychicShock.index, static () => Settings.Instance.Demonkin },
                { TorannMagicDefOf.SpellOf_TechnoShield.index, static () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_Sabotage.index, static () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_Overdrive.index, static () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_OrbitalStrike.index, static () => Settings.Instance.Technomancer },
                { TorannMagicDefOf.SpellOf_BloodMoon.index, static () => Settings.Instance.BloodMage },
                { TorannMagicDefOf.SpellOf_Shapeshift.index, static () => Settings.Instance.Enchanter },
                { TorannMagicDefOf.SpellOf_Recall.index, static () => Settings.Instance.Chronomancer },
            };

            recipeIndexMap = new Dictionary<ushort, (Func<bool> settingsValueGetter, ThingDef item)>
            {
                {
                    TorannMagicDefOf.Make_SpellOf_FoldReality.index,
                    (static () => Settings.Instance.Arcanist, TorannMagicDefOf.SpellOf_FoldReality)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Firestorm.index,
                    (static () => Settings.Instance.FireMage, TorannMagicDefOf.SpellOf_Firestorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_DryGround.index,
                    (static () => Settings.Instance.FireMage, TorannMagicDefOf.SpellOf_DryGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Blizzard.index,
                    (static () => Settings.Instance.IceMage, TorannMagicDefOf.SpellOf_Blizzard)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_WetGround.index,
                    (static () => Settings.Instance.IceMage, TorannMagicDefOf.SpellOf_WetGround)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_EyeOfTheStorm.index,
                    (static () => Settings.Instance.LitMage, TorannMagicDefOf.SpellOf_EyeOfTheStorm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_ChargeBattery.index,
                    (static () => Settings.Instance.LitMage, TorannMagicDefOf.SpellOf_ChargeBattery)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_RegrowLimb.index,
                    (static () => Settings.Instance.Druid, TorannMagicDefOf.SpellOf_RegrowLimb)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_FertileLands.index,
                    (static () => Settings.Instance.Druid, TorannMagicDefOf.SpellOf_FertileLands)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_SummonPoppi.index,
                    (static () => Settings.Instance.Summoner, TorannMagicDefOf.SpellOf_SummonPoppi)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_HolyWrath.index,
                    (static () => Settings.Instance.Paladin, TorannMagicDefOf.SpellOf_HolyWrath)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Resurrection.index,
                    (static () => Settings.Instance.Priest, TorannMagicDefOf.SpellOf_Resurrection)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BattleHymn.index,
                    (static () => Settings.Instance.Bard, TorannMagicDefOf.SpellOf_BattleHymn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_LichForm.index,
                    (static () => Settings.Instance.Necromancer, TorannMagicDefOf.SpellOf_LichForm)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Meteor.index,
                    (static () => Settings.Instance.Geomancer, TorannMagicDefOf.SpellOf_Meteor)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Scorn.index,
                    (static () => Settings.Instance.Demonkin, TorannMagicDefOf.SpellOf_Scorn)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_PsychicShock.index,
                    (static () => Settings.Instance.Demonkin, TorannMagicDefOf.SpellOf_PsychicShock)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_TechnoShield.index,
                    (static () => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_TechnoShield)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Sabotage.index,
                    (static () => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_Sabotage)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Overdrive.index,
                    (static () => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_Overdrive)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_OrbitalStrike.index,
                    (static () => Settings.Instance.Technomancer, TorannMagicDefOf.SpellOf_OrbitalStrike)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_BloodMoon.index,
                    (static () => Settings.Instance.BloodMage, TorannMagicDefOf.SpellOf_BloodMoon)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Shapeshift.index,
                    (static () => Settings.Instance.Enchanter, TorannMagicDefOf.SpellOf_Shapeshift)
                },
                {
                    TorannMagicDefOf.Make_SpellOf_Recall.index,
                    (static () => Settings.Instance.Chronomancer, TorannMagicDefOf.SpellOf_Recall)
                },
                {
                    TorannMagicDefOf.Make_BookOfSuperSoldier.index,
                    (static () => Settings.Instance.SuperSoldier, TorannMagicDefOf.BookOfSuperSoldier)
                }
            };
        }

        public static Dictionary<TMAbilityDef, ThingDef[]> GetLinkedCustomFighterSkillThings()
        {
            return new Dictionary<TMAbilityDef, ThingDef[]>
            {
                // { TorannMagicDefOf.TM_PoisonTrap, new[] { ThingDef.Named("TM_PoisonTrap") } },
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

                (Func<bool> settingsValueGetter, ThingDef item) = recipeIndexMap.TryGetValue(current.index);
                if (settingsValueGetter == null) continue;
                if (settingsValueGetter()) continue;
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
