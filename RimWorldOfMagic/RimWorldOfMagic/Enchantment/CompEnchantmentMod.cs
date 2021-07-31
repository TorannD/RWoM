using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.Enchantment
{
    internal class CompEnchantmentMod : Mod
    {
        public CompEnchantmentMod(ModContentPack mcp) : base(mcp)
        {
            LongEventHandler.ExecuteWhenFinished(new Action(CompEnchantmentMod.AddComp));
            LongEventHandler.ExecuteWhenFinished(new Action(CompEnchantmentMod.AddUniversalBodyparts));            
            LongEventHandler.ExecuteWhenFinished(new Action(CompEnchantmentMod.FillCloakPool));
        }

        private static void AddComp()
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

        private static void AddUniversalBodyparts()
        {
            IEnumerable<BodyPartDef> universalBodyParts = from def in DefDatabase<BodyPartDef>.AllDefs
                                                          where (def.destroyableByDamage)
                                                          select def;
            foreach (BodyPartDef current1 in universalBodyParts)
            {
                TorannMagicDefOf.UniversalRegrowth.appliedOnFixedBodyParts.AddDistinct(current1);
            }

            IEnumerable<ThingDef> universalPawnTypes = from def in DefDatabase<ThingDef>.AllDefs
                                                       where (def.category == ThingCategory.Pawn && !def.defName.Contains("TM_") && def.race.IsFlesh)
                                                       select def;
            foreach (ThingDef current2 in universalPawnTypes)
            {
                TorannMagicDefOf.UniversalRegrowth.recipeUsers.AddDistinct(current2);
                TorannMagicDefOf.AdministerOrbOfTheEternal.recipeUsers.AddDistinct(current2);
            }
        }        

        private static void FillCloakPool()
        {
            ModOptions.Constants.InitializeCloaks();
        }

    }
}
