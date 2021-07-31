using System;
using System.Collections.Generic;
using Verse;
using RimWorld;


namespace TorannMagic
{
    class ArcaneItemCollectionGeneratorUtility
    {
        public static List<ThingDef> allArcaneItems = new List<ThingDef>();

        public static void Reset()
        {
            ArcaneItemCollectionGeneratorUtility.allArcaneItems.Clear();
            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                //if ((current.category == ThingCategory.Item || current.Minifiable) && !current.isUnfinishedThing && !current.IsCorpse && current.PlayerAcquirable && current.graphicData != null && !typeof(MinifiedThing).IsAssignableFrom(current.thingClass))
                if ((current.defName == "ManaPotion"))
                {
                    ArcaneItemCollectionGeneratorUtility.allArcaneItems.Add(current);
                }
            }
            //ItemCollectionGenerator_Arcane.Reset();
        }
    }
}
