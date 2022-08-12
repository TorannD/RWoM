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
        public ModClassOptions(ModContentPack mcp) : base(mcp)
        {
            LongEventHandler.ExecuteWhenFinished(TM_ClassUtility.LoadCustomClasses);
            LongEventHandler.ExecuteWhenFinished(CheckForDisabledCustomClass);
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
                Settings.Instance.CustomClass.Clear();
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

            for (int i = 0; i < TM_CustomClassDef.Named("TM_CustomClasses").customClasses.Count; i++)
            {
                TMDefs.TM_CustomClass cc = TM_CustomClassDef.Named("TM_CustomClasses").customClasses[i];
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
                            customThings.AddDistinct(ThingDef.Named("TM_PistolSpec_Base0"));
                        }
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_RifleSpec))
                        {
                            customThings.AddDistinct(ThingDef.Named("TM_RifleSpec_Base0"));
                        }
                        if (cc.classFighterAbilities.Contains(TorannMagicDefOf.TM_ShotgunSpec))
                        {
                            customThings.AddDistinct(ThingDef.Named("TM_ShotgunSpec_Base0"));
                        }
                    }
                    if (cc.classMageAbilities != null && cc.classMageAbilities.Count > 0)
                    {
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_RegrowLimb))
                        {
                            customThings.AddDistinct(ThingDef.Named("SeedofRegrowth"));
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_SummonExplosive))
                        {
                            customThings.AddDistinct(ThingDef.Named("TM_ManaMine"));
                            customThings.AddDistinct(ThingDef.Named("TM_ManaMine_I"));
                            customThings.AddDistinct(ThingDef.Named("TM_ManaMine_II"));
                            customThings.AddDistinct(ThingDef.Named("TM_ManaMine_III"));
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_SummonPylon))
                        {
                            customThings.AddDistinct(ThingDef.Named("DefensePylon"));
                            customThings.AddDistinct(ThingDef.Named("DefensePylon_I"));
                            customThings.AddDistinct(ThingDef.Named("DefensePylon_II"));
                            customThings.AddDistinct(ThingDef.Named("DefensePylon_III"));
                            customThings.AddDistinct(ThingDef.Named("Bullet_DefensePylon"));
                            customThings.AddDistinct(ThingDef.Named("Launcher_DefensePylon"));
                            customThings.AddDistinct(ThingDef.Named("Launcher_DefensePylon_I"));
                            customThings.AddDistinct(ThingDef.Named("Launcher_DefensePylon_II"));
                            customThings.AddDistinct(ThingDef.Named("Launcher_DefensePylon_III"));
                            customThings.AddDistinct(ThingDef.Named("TM_Poppi"));
                        }
                        if (cc.classMageAbilities.Contains(TorannMagicDefOf.TM_SummonPoppi))
                        {
                            customThings.AddDistinct(ThingDef.Named("TM_Poppi"));
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
                    for (int k = 0; k < cc.learnableSpells.Count; k++)
                    {
                        removedSpells.Add(cc.learnableSpells[k]);
                    }
                    for (int k = 0; k < cc.learnableSkills.Count; k++)
                    {
                        removedSpells.Add(cc.learnableSkills[k]);
                    }
                    removedCustomThings.Add(cc.fullScript);
                }
            }

            foreach (ThingDef current in enumerable)
            {
                if (!settingsRef.Sniper)
                {
                    if(current.defName == "BookOfSniper")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Ranger)
                {
                    if (current.defName == "BookOfRanger" || current.defName == "TM_PoisonTrap")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Gladiator)
                {
                    if (current.defName == "BookOfGladiator")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Bladedancer)
                {
                    if (current.defName == "BookOfBladedancer")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Faceless)
                {
                    if (current.defName == "BookOfFaceless")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Psionic)
                {
                    if (current.defName == "BookOfPsionic")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.DeathKnight)
                {
                    if (current.defName == "BookOfDeathKnight")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Monk)
                {
                    if (current.defName == "BookOfMonk")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.Commander)
                {
                    if (current == TorannMagicDefOf.BookOfCommander)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                if (!settingsRef.SuperSoldier)
                {
                    if (current == TorannMagicDefOf.BookOfSuperSoldier || current == TorannMagicDefOf.TM_60mmMortar_Base)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                    if (current.defName.Contains("TM_PistolSpec_Base") && !customThings.Contains(ThingDef.Named("TM_PistolSpec_Base0")))
                    {
                        removedThings.Add(current);
                    }
                    if (current.defName.Contains("TM_RifleSpec_Base") && !customThings.Contains(ThingDef.Named("TM_RifleSpec_Base0")))
                    {
                        removedThings.Add(current);
                    }
                    if (current.defName.Contains("TM_ShotgunSpec_Base") && !customThings.Contains(ThingDef.Named("TM_ShotgunSpec_Base0")))
                    {
                        removedThings.Add(current);
                    }
                }

                if (!settingsRef.Arcanist)
                {
                    if (current.defName == "Torn_BookOfArcanist" || current.defName == "BookOfArcanist" || current.defName == "SpellOf_FoldReality")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if(current.defName == "SpellOf_FoldReality")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.FireMage)
                {
                    if (current.defName == "Torn_BookOfInnerFire" || current.defName == "BookOfInnerFire" || current.defName == "SpellOf_Firestorm" || current.defName == "SpellOf_DryGround")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_Firestorm" || current.defName == "SpellOf_DryGround")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.IceMage)
                {
                    if (current.defName == "Torn_BookOfHeartOfFrost" || current.defName == "BookOfHeartOfFrost" || current.defName == "SpellOf_Blizzard" || current.defName == "SpellOf_WetGround")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_Blizzard" || current.defName == "SpellOf_WetGround")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.LitMage)
                {
                    if (current.defName == "Torn_BookOfStormBorn" || current.defName == "BookOfStormBorn" || current.defName == "SpellOf_EyeOfTheStorm" || current.defName == "SpellOf_ChargeBattery")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_EyeOfTheStorm" || current.defName == "SpellOf_ChargeBattery")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Druid)
                {
                    if (current.defName == "Torn_BookOfNature" || current.defName == "BookOfNature" || current.defName == "SpellOf_RegrowLimb" || current.defName == "SeedofRegrowth" || current.defName == "SpellOf_FertileLands")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_RegrowLimb" ||  current.defName == "SpellOf_FertileLands")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Summoner)
                {
                    if (current.defName == "Torn_BookOfSummoner" || current.defName == "BookOfSummoner" || current.defName == "SpellOf_SummonPoppi" ||
                        current.defName == "TM_ManaMine" || current.defName == "TM_ManaMine_I" || current.defName == "TM_ManaMine_II" || current.defName == "TM_ManaMine_III" ||
                        current.defName == "DefensePylon" || current.defName == "DefensePylon_I" || current.defName == "DefensePylon_II" || current.defName == "DefensePylon_III" || current.defName == "Bullet_DefensePylon" ||
                        current.defName == "Launcher_DefensePylon" || current.defName == "Launcher_DefensePylon_I" || current.defName == "Launcher_DefensePylon_II" || current.defName == "Launcher_DefensePylon_III" ||
                        current.defName == "TM_Poppi")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_SummonPoppi")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Paladin)
                {
                    if (current.defName == "Torn_BookOfValiant" || current.defName == "BookOfValiant" || current.defName == "SpellOf_HolyWrath")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_HolyWrath")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Priest)
                {
                    if (current.defName == "Torn_BookOfPriest" || current.defName == "BookOfPriest" || current.defName == "SpellOf_Resurrection")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_Resurrection")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Bard)
                {
                    if (current.defName == "Torn_BookOfBard" || current.defName == "BookOfBard" || current.defName == "SpellOf_BattleHymn")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_BattleHymn")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Necromancer)
                {
                    if (current == TorannMagicDefOf.BookOfNecromancer || current == TorannMagicDefOf.Torn_BookOfUndead || current == TorannMagicDefOf.SpellOf_LichForm || current == TorannMagicDefOf.TM_Artifact_NecroticOrb)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current == TorannMagicDefOf.SpellOf_LichForm)
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Geomancer)
                {
                    if (current.defName == "Torn_BookOfEarth" || current.defName == "BookOfEarth" || current.defName == "SpellOf_Meteor" ||
                        current.defName == "TM_Lesser_SentinelR" || current.defName == "TM_SentinelR" || current.defName == "TM_Greater_SentinelR")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_Meteor")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Demonkin)
                {
                    if (current.defName == "Torn_BookOfDemons" || current.defName == "BookOfDemons" || current.defName == "SpellOf_Scorn" || current.defName == "SpellOf_PsychicShock")
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current.defName == "SpellOf_Scorn" || current.defName == "SpellOf_PsychicShock")
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Technomancer)
                {
                    if (current == TorannMagicDefOf.Torn_BookOfMagitech || current == TorannMagicDefOf.BookOfMagitech || current == TorannMagicDefOf.SpellOf_TechnoShield || current == TorannMagicDefOf.SpellOf_Sabotage || current == TorannMagicDefOf.SpellOf_Overdrive || current == TorannMagicDefOf.SpellOf_OrbitalStrike)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current == TorannMagicDefOf.SpellOf_TechnoShield || current == TorannMagicDefOf.SpellOf_Sabotage || current == TorannMagicDefOf.SpellOf_Overdrive || current == TorannMagicDefOf.SpellOf_OrbitalStrike)
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.BloodMage)
                {
                    if (current == TorannMagicDefOf.BookOfHemomancy || current == TorannMagicDefOf.Torn_BookOfHemomancy || current == TorannMagicDefOf.SpellOf_BloodMoon)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current == TorannMagicDefOf.SpellOf_BloodMoon)
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Enchanter)
                {
                    if (current == TorannMagicDefOf.BookOfEnchanter || current == TorannMagicDefOf.Torn_BookOfEnchanter || current == TorannMagicDefOf.SpellOf_Shapeshift)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current == TorannMagicDefOf.SpellOf_Shapeshift)
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.Chronomancer)
                {
                    if (current == TorannMagicDefOf.BookOfChronomancer || current == TorannMagicDefOf.Torn_BookOfChronomancer || current == TorannMagicDefOf.SpellOf_Recall)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
                else
                {
                    if (current == TorannMagicDefOf.SpellOf_Recall)
                    {
                        classSpells.Add(current);
                    }
                }
                if (!settingsRef.ChaosMage)
                {
                    if (current == TorannMagicDefOf.BookOfChaos || current == TorannMagicDefOf.Torn_BookOfChaos)
                    {
                        if (!customThings.Contains(current))
                        {
                            removedThings.Add(current);
                        }
                    }
                }
            }

            for(int i =0; i < removedCustomThings.Count; i++)
            {
                if(!removedThings.Contains(removedCustomThings[i]))
                {
                    removedThings.Add(removedCustomThings[i]);
                }
            }

            for(int i =0; i < removedSpells.Count; i++)
            {
                if(!customThings.Contains(removedSpells[i]) && !classSpells.Contains(removedSpells[i]))
                {
                    removedThings.Add(removedSpells[i]);
                    removedCustomThings.Add(removedSpells[i]);
                }
            }

            for (int i = 0; i < removedThings.Count(); i++)
            {
                //Log.Message("removing " + removedThings[i].defName + " from def database");
                removedThings[i].resourceReadoutPriority = ResourceCountPriority.Uncounted;
                DefDatabase<ThingDef>.AllDefsListForReading.Remove(removedThings[i]);
            }

            IEnumerable<RecipeDef> RecipeEnumerable = (from def in DefDatabase<RecipeDef>.AllDefs
                                                select def);
            List<RecipeDef> removedRecipes = new List<RecipeDef>();

            foreach (RecipeDef current in RecipeEnumerable)
            {
                if (!settingsRef.Arcanist)
                {
                    if (current.defName == "Make_SpellOf_FoldReality" && !customThings.Contains(TorannMagicDefOf.SpellOf_FoldReality))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.FireMage)
                {
                    if ((current.defName == "Make_SpellOf_Firestorm" && !customThings.Contains(TorannMagicDefOf.SpellOf_Firestorm)) || (current.defName == "Make_SpellOf_DryGround" && !customThings.Contains(TorannMagicDefOf.SpellOf_DryGround)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.IceMage)
                {
                    if ((current.defName == "Make_SpellOf_Overdrive" && !customThings.Contains(TorannMagicDefOf.SpellOf_Overdrive)) || (current.defName == "Make_SpellOf_WetGround" && !customThings.Contains(TorannMagicDefOf.SpellOf_WetGround)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.LitMage)
                {
                    if ((current.defName == "Make_SpellOf_EyeOfTheStorm" && !customThings.Contains(TorannMagicDefOf.SpellOf_EyeOfTheStorm)) || (current.defName == "Make_SpellOf_ChargeBattery" && !customThings.Contains(TorannMagicDefOf.SpellOf_ChargeBattery)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Druid)
                {
                    if ((current.defName == "Make_SpellOf_RegrowLimb" && !customThings.Contains(TorannMagicDefOf.SpellOf_RegrowLimb)) || (current.defName == "Make_SpellOf_FertileLands" && !customThings.Contains(TorannMagicDefOf.SpellOf_FertileLands)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Summoner)
                {
                    if ((current.defName == "Make_SpellOf_SummonPoppi" && !customThings.Contains(TorannMagicDefOf.SpellOf_SummonPoppi)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Paladin)
                {
                    if ((current.defName == "Make_SpellOf_HolyWrath" && !customThings.Contains(TorannMagicDefOf.SpellOf_HolyWrath)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Priest)
                {
                    if ((current.defName == "Make_SpellOf_Resurrection" && !customThings.Contains(TorannMagicDefOf.SpellOf_Resurrection)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Bard)
                {
                    if ((current.defName == "Make_SpellOf_BattleHymn" && !customThings.Contains(TorannMagicDefOf.SpellOf_BattleHymn)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Necromancer)
                {
                    if ((current.defName == "Make_SpellOf_FoldReality" && !customThings.Contains(TorannMagicDefOf.SpellOf_FoldReality)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Geomancer)
                {
                    if ((current.defName == "Make_SpellOf_Meteor" && !customThings.Contains(TorannMagicDefOf.SpellOf_Meteor)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Demonkin)
                {
                    if ((current.defName == "Make_SpellOf_Scorn" && !customThings.Contains(TorannMagicDefOf.SpellOf_Scorn)) || (current.defName == "Make_SpellOf_PsychicShock" && !customThings.Contains(TorannMagicDefOf.SpellOf_PsychicShock)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Technomancer)
                {
                    if ((current.defName == "Make_SpellOf_TechnoShield" && !customThings.Contains(TorannMagicDefOf.SpellOf_TechnoShield)) ||
                        (current.defName == "Make_SpellOf_Sabotage" && !customThings.Contains(TorannMagicDefOf.SpellOf_Sabotage)) ||
                        (current.defName == "Make_SpellOf_Overdrive" && !customThings.Contains(TorannMagicDefOf.SpellOf_Overdrive)) ||
                        (current.defName == "Make_SpellOf_OrbitalStrike" && !customThings.Contains(TorannMagicDefOf.SpellOf_OrbitalStrike)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.BloodMage)
                {
                    if ((current.defName == "Make_SpellOf_BloodMoon" && !customThings.Contains(TorannMagicDefOf.SpellOf_BloodMoon)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Enchanter)
                {
                    if ((current.defName == "Make_SpellOf_Shapeshift" && !customThings.Contains(TorannMagicDefOf.SpellOf_Shapeshift)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.Chronomancer)
                {
                    if ((current.defName == "Make_SpellOf_Recall" && !customThings.Contains(TorannMagicDefOf.SpellOf_Recall)))
                    {
                        removedRecipes.Add(current);
                    }
                }
                if (!settingsRef.SuperSoldier)
                {
                    if (!settingsRef.SuperSoldier)
                    { 
                        if ((current.defName == "Make_BookOfSuperSoldier" && !customThings.Contains(TorannMagicDefOf.BookOfSuperSoldier)))
                        {
                            removedRecipes.Add(current);
                        }
                    }
                }
            }

            for(int i = 0; i < removedCustomThings.Count; i++)
            {
                if(RecipeEnumerable.Any((RecipeDef x) => x.defName == "Make_" + removedCustomThings[i].defName))
                {
                    removedRecipes.Add(RecipeEnumerable.FirstOrDefault<RecipeDef>((RecipeDef x) => x.defName == ("Make_" + removedCustomThings[i].ToString())));
                }
            }

            for (int i = 0; i < removedRecipes.Count(); i++)
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
