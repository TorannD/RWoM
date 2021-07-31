using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TorannMagic
{
    public static class ItemCollectionGenerator_Internal_Arcane
    {
        private const float GemstoneChance = 0.03f;
        private const float LuciferiumChance = 0.2f;
        private const float ArcaneScriptChance = 0.06f;
        private const float DrugChance = 0.15f;
        private const float SpellChance = 0.2f;
        private const float SkillChance = 0.2f;
        private const float MasterSpellChance = 0.1f;

        private static readonly IntRange GemstoneCountRange = new IntRange(1, 2);
        private static readonly IntRange LuciferiumCountRange = new IntRange(8, 12);
        private static readonly IntRange RawMagicyteRange = new IntRange(40, 100);
        private static readonly IntRange ManaPotionRange = new IntRange(1, 4);
        private static readonly IntRange DrugCountRange = new IntRange(3, 10);
        private static readonly IntRange SpellCountRange = new IntRange(1, 2);
        private static readonly IntRange SkillCountRange = new IntRange(1, 2);

        private static float collectiveMarketValue = 0;

        public static List<Thing> Generate(int totalMarketValue)
        {
            List<Thing> outThings = new List<Thing>();
            outThings.Clear();
            for (int j = 0; j < 10; j++)
            {
                //Torn Scripts
                if (Rand.Chance(0.3f) && (totalMarketValue - collectiveMarketValue) > TorannMagicDefOf.Torn_BookOfArcanist.BaseMarketValue *.8f)
                {
                    Thing thing = ThingMaker.MakeThing(TM_Data.MageTornScriptList().RandomElement(), null);
                    if (thing != null)
                    {
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
                //Arcane Scripts
                if (Rand.Chance(ArcaneScriptChance) && (totalMarketValue - collectiveMarketValue) > TorannMagicDefOf.BookOfArcanist.BaseMarketValue *.8f)
                {
                    Thing thing = ThingMaker.MakeThing(TM_Data.AllBooksList().RandomElement(), null);
                    if (thing != null)
                    {
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
                //Mana Potions
                if (Rand.Chance(0.2f) && (totalMarketValue - collectiveMarketValue) > TorannMagicDefOf.ManaPotion.BaseMarketValue * ManaPotionRange.RandomInRange)
                {
                    int randomInRange = ManaPotionRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        Thing thing = ThingMaker.MakeThing(TorannMagicDefOf.ManaPotion, null);
                        thing.stackCount = ManaPotionRange.RandomInRange;
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue * thing.stackCount;
                    }
                }
                //Gemstones
                if (Rand.Chance(GemstoneChance) && (totalMarketValue - collectiveMarketValue) > 1000f)
                {
                    int randomInRange = GemstoneCountRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        List<Thing> gemstoneZero = new List<Thing>();
                        Thing item = null;
                        ItemCollectionGenerator_Gemstones icg_g = new ItemCollectionGenerator_Gemstones();
                        icg_g.Generate(1000, gemstoneZero);
                        item = gemstoneZero[0];
                        if (item != null)
                        {
                            outThings.Add(item);
                            collectiveMarketValue += item.MarketValue;
                        }
                    }
                }
                //Syrrium
                if (Rand.Chance(LuciferiumChance) && (totalMarketValue - collectiveMarketValue) > TorannMagicDefOf.TM_Syrrium.BaseMarketValue * LuciferiumCountRange.RandomInRange)
                {
                    Thing thing = ThingMaker.MakeThing(TorannMagicDefOf.TM_Syrrium, null);
                    thing.stackCount = LuciferiumCountRange.RandomInRange;
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue * thing.stackCount;
                }
                //Raw Magicyte
                if (Rand.Chance(DrugChance))
                {

                    Thing thing = ThingMaker.MakeThing(TorannMagicDefOf.RawMagicyte, null);
                    thing.stackCount = RawMagicyteRange.RandomInRange;
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue * thing.stackCount;

                }
                //Master Spells
                if (Rand.Chance(MasterSpellChance) && (totalMarketValue - collectiveMarketValue) > TorannMagicDefOf.SpellOf_Blizzard.BaseMarketValue)
                {
                    Thing thing = ThingMaker.MakeThing(TM_Data.MasterSpellList().RandomElement(), null);
                    if (thing != null)
                    {
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
                //Spells
                if (Rand.Chance(SpellChance) && (totalMarketValue - collectiveMarketValue) > 1000f)
                {
                    Thing thing = ThingMaker.MakeThing(TM_Data.StandardSpellList().RandomElement(), null);
                    if (thing != null)
                    {
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
                //Skills
                if (Rand.Chance(SpellChance) && (totalMarketValue - collectiveMarketValue) > 800f)
                {
                    int randomInRange = SkillCountRange.RandomInRange;
                    Thing thing = ThingMaker.MakeThing(TM_Data.StandardSkillList().RandomElement(), null);
                    if (thing != null)
                    {
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
            }
            return outThings;
        }
    }
}
