using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.Enchantment
{
    internal class CompEnchantmentMod
    {
        public static void AddComp()
        {
            //unrelated, single time load mod check
            //foreach (ModContentPack p in LoadedModManager.RunningMods)
            //{
            //    Log.Message(p.Name + "");
            //}

            //&& def.HasComp(typeof(CompQuality))

            //IEnumerable<ThingCategoryDef> getExistingDefs = from def in DefDatabase<ThingCategoryDef>.AllDefs
            //                                   where (true)
            //                                   select def;
            //foreach(ThingCategoryDef c in getExistingDefs)
            //{
            //    Log.Message("thingCategoryDef has defname " + c.defName);
            //}
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (def.IsMeleeWeapon || def.IsRangedWeapon || def.IsApparel) && !def.HasComp(typeof(CompEnchantedItem))
                                               select def;
            Type typeFromHandle = typeof(ITab_Enchantment);
            InspectTabBase sharedInstance = InspectTabManager.GetSharedInstance(typeFromHandle);            
            foreach (ThingDef current in enumerable)
            {
                //if (current.defName != "TM_ThrumboAxe" && current.defName != "TM_FireWand" && current.defName != "TM_IceWand" && current.defName != "TM_LightningWand" &&
                //    current.defName != "TM_BlazingPowerStaff" && current.defName != "TM_DefenderStaff")
                if(!current.defName.Contains("TM_"))
                {
                    CompProperties_EnchantedItem item = new CompProperties_EnchantedItem
                    {
                        compClass = typeof(CompEnchantedItem)
                    };
                    current.comps.Add(item);

                    if (current.inspectorTabs == null || current.inspectorTabs.Count == 0)
                    {
                        current.inspectorTabs = new List<Type>();
                        current.inspectorTabsResolved = new List<InspectTabBase>();
                    }
                    current.inspectorTabs.Add(typeFromHandle);
                    current.inspectorTabsResolved.Add(sharedInstance);
                }
            }        
        }

        public static void InitializeUniversalBodyParts()
        {
            // Add all destroyable body parts to Regrowth
            foreach (BodyPartDef bodyPartDef in DefDatabase<BodyPartDef>.AllDefs)
            {
                if (!bodyPartDef.destroyableByDamage) continue;

                TorannMagicDefOf.UniversalRegrowth.appliedOnFixedBodyParts.AddDistinct(bodyPartDef);
            }
            // Add all pawn flesh things outside of this mod
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.category != ThingCategory.Pawn
                    || thingDef.defName.StartsWith("TM_")
                    || !thingDef.race.IsFlesh) continue;

                TorannMagicDefOf.UniversalRegrowth.recipeUsers.AddDistinct(thingDef);
                TorannMagicDefOf.AdministerOrbOfTheEternal.recipeUsers.AddDistinct(thingDef);

            }
        }
    }
}
