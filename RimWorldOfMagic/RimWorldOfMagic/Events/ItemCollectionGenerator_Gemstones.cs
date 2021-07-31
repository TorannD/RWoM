using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic
{
    public class ItemCollectionGenerator_Gemstones
    {
        private const float RawMagicyteChance = 0.4f;
        private const float MinorMagicyteChance = 0.2f;
        private const float MagicyteChance = 0.07f;
        private const float MajorMagicyteChance = 0.02f;

        private int MagicyteTypes = 5;

        private static readonly IntRange RawMagicyteRange = new IntRange(8, 16);

        private float collectiveMarketValue = 0;

        public List<Thing> Generate(int totalMarketValue, List<Thing> outThings)
        {            
            for (int j = 0; j < 10; j++)
            {
                //Raw Magicyte
                if (Rand.Chance(RawMagicyteChance) && (totalMarketValue - collectiveMarketValue) > TorannMagicDefOf.RawMagicyte.BaseMarketValue * 10)
                {
                    Thing thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte, null);
                    thing.stackCount = RawMagicyteRange.RandomInRange;
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue * thing.stackCount;                 
                }
                //Minor Magicyte
                if (Rand.Chance(MinorMagicyteChance) && (totalMarketValue - collectiveMarketValue) > ThingDef.Named("TM_EStone_maxMP_minor").BaseMarketValue)
                {
                    Thing thing = null;
                    int rnd = Rand.RangeInclusive(1, this.MagicyteTypes);
                    switch (rnd)
                    {
                        case 1:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_maxMP_minor"),null);
                            break;
                        case 2:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_mpRegenRate_minor"), null);
                            break;
                        case 3:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_mpCost_minor"), null);
                            break;
                        case 4:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_coolDown_minor"), null);
                            break;
                        case 5:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_xpGain_minor"), null);
                            break;
                    }
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue;
                }
                //Magicyte
                if (Rand.Chance(MagicyteChance) && (totalMarketValue - collectiveMarketValue) > ThingDef.Named("TM_EStone_maxMP").BaseMarketValue)
                {
                    Thing thing = null;
                    int rnd = Rand.RangeInclusive(1, this.MagicyteTypes);
                    switch (rnd)
                    {
                        case 1:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_maxMP"), null);
                            break;
                        case 2:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_mpRegenRate"), null);
                            break;
                        case 3:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_mpCost"), null);
                            break;
                        case 4:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_coolDown"), null);
                            break;
                        case 5:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_xpGain"), null);
                            break;
                    }
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue;
                }
                //Major Magicyte
                if (Rand.Chance(MajorMagicyteChance) && (totalMarketValue - collectiveMarketValue) > ThingDef.Named("TM_EStone_maxMP_major").BaseMarketValue)
                {
                    Thing thing = null;
                    int rnd = Rand.RangeInclusive(1, this.MagicyteTypes);
                    switch (rnd)
                    {
                        case 1:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_maxMP_major"), null);
                            break;
                        case 2:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_mpRegenRate_major"), null);
                            break;
                        case 3:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_mpCost_major"), null);
                            break;
                        case 4:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_coolDown_major"), null);
                            break;
                        case 5:
                            thing = ThingMaker.MakeThing(ThingDef.Named("TM_EStone_xpGain_major"), null);
                            break;
                    }
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue;
                }                
            }
            
            return outThings;
        }
    }
}
