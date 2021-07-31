using RimWorld.Planet;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    public class IncidentWorker_ArcaneScriptCaravan : IncidentWorker
    {
        //private const float TravelBufferMultiple = 0.1f;

        //private const float TravelBufferAbsolute = 1f;

        //private const int MaxTileDistance = 24;

        //private static List<Map> tmpAvailableMaps = new List<Map>();

        //private static readonly IntRange OfferDurationRange = new IntRange(15, 45);

        //private static readonly IntRange BaseValueWantedRange = new IntRange(500, 800);

        //private static readonly SimpleCurve ValueFactorFromWealthCurve = new SimpleCurve
        //{
        //    {
        //        new CurvePoint(0f, .4f),
        //        true
        //    },
        //    {
        //        new CurvePoint(50000f, 1f),
        //        true
        //    },
        //    {
        //        new CurvePoint(300000f, 1.5f),
        //        true
        //    }
        //};

        //protected override bool CanFireNowSub(IncidentParms parms)
        //{
        //    Map map;
        //    return base.CanFireNowSub(parms) && this.TryGetRandomAvailableTargetMap(out map) && IncidentWorker_ArcaneScriptCaravan.RandomNearbyTradeableSettlement(map.Tile) != null;
        //}

        //protected override bool TryExecuteWorker(IncidentParms parms)
        //{
        //    Map map;
        //    bool result;
        //    if (!this.TryGetRandomAvailableTargetMap(out map))
        //    {
        //        result = false;
        //    }
        //    else
        //    {
        //        Settlement settlement = IncidentWorker_ArcaneScriptCaravan.RandomNearbyTradeableSettlement(parms.target.Tile);
        //        if (settlement == null)
        //        {
        //            return false;
        //        }
        //        else
        //        {                    
        //            TradeRequestComp component = settlement.GetComponent<TradeRequestComp>();
        //            if (!this.GenerateCaravanRequest(component, (Map)parms.target))
        //            {
        //                return false;
        //            }
        //            string rewards = "";                    
        //            for (int i = 0; i < component.rewards.Count(); i++)
        //            {
        //                rewards += component.rewards[i].LabelCap + "\n";
        //            }
        //            Find.LetterStack.ReceiveLetter("LetterLabelArcaneScriptCaravan".Translate(), "LetterArcaneScriptCaravan".Translate(
        //        settlement.Label,
        //        GenLabel.ThingLabel(component.requestThingDef, null, component.requestCount).CapitalizeFirst(),
        //        rewards,
        //        (component.expiration - Find.TickManager.TicksGame).ToStringTicksToDays("F0")
        //            ), LetterDefOf.PositiveEvent, settlement, null);
        //            return true;
        //        }
        //    }
        //    return result;
        //}

        //private bool TryGetRandomAvailableTargetMap(out Map map)
        //{
        //    IncidentWorker_ArcaneScriptCaravan.tmpAvailableMaps.Clear();
        //    List<Map> maps = Find.Maps;
        //    for (int i = 0; i < maps.Count; i++)
        //    {
        //        if (maps[i].IsPlayerHome && this.AtLeast2HealthyColonists(maps[i]) && IncidentWorker_ArcaneScriptCaravan.RandomNearbyTradeableSettlement(maps[i].Tile) != null)
        //        {
        //            IncidentWorker_ArcaneScriptCaravan.tmpAvailableMaps.Add(maps[i]);
        //        }
        //    }
        //    bool result = IncidentWorker_ArcaneScriptCaravan.tmpAvailableMaps.TryRandomElement(out map);
        //    IncidentWorker_ArcaneScriptCaravan.tmpAvailableMaps.Clear();
        //    return result;
        //}

        //private bool AtLeast2HealthyColonists(Map map)
        //{
        //    List<Pawn> list = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        //    int num = 0;
        //    bool result;
        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        if (list[i].IsFreeColonist)
        //        {
        //            if (!HealthAIUtility.ShouldSeekMedicalRest(list[i]))
        //            {
        //                num++;
        //                if (num >= 2)
        //                {
        //                    result = true;
        //                    return result;
        //                }
        //            }
        //        }
        //    }
        //    result = false;
        //    return result;
        //}

        //public bool GenerateCaravanRequest(TradeRequestComp target, Map map)
        //{
        //    int num = this.RandomOfferDuration(map.Tile, target.parent.Tile);
        //    if (num < 1)
        //    {
        //        return false;
        //    }
        //    target.requestThingDef = IncidentWorker_ArcaneScriptCaravan.RandomRequestedThingDef();
        //    if (target.requestThingDef == null)
        //    {
        //        Log.Error("Attempted to create a caravan request, but couldn't find a valid request object");
        //        return false;
        //    }
        //    Thing item = new Thing();
        //    Faction playerFaction = new Faction();
        //    playerFaction.def = FactionDefOf.PlayerColony;
            
        //    target.requestCount = IncidentWorker_ArcaneScriptCaravan.RandomRequestCount(target.requestThingDef, map);
        //    target.rewards.ClearAndDestroyContents(DestroyMode.Vanish);
        //    System.Random random = new System.Random();
        //    int rnd = GenMath.RoundRandom(random.Next(0, 26));
        //    if (rnd < 1)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfInnerFire, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 1 && rnd < 2)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfHeartOfFrost, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 2 && rnd < 3)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfStormBorn, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 3 && rnd < 4)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfArcanist, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 4 && rnd < 5)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfValiant, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 5 && rnd < 6)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfSummoner, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 6 && rnd < 7)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfGladiator, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 7 && rnd < 8)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfDruid, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 8 && rnd < 9)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfNecromancer, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 9 && rnd < 10)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfPriest, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 10 && rnd < 11)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfBladedancer, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 11 && rnd < 12)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfSniper, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 12 && rnd < 13)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfRanger, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 13 && rnd < 14)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfQuestion, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 14 && rnd < 15)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfFaceless, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 15 && rnd < 16)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfDemons, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 16 && rnd < 17)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfBard, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else if (rnd >= 17 && rnd < 18)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfPsionic, null);
        //        target.rewards.TryAdd(item, true);
        //    }
        //    else
        //    {
        //        for (int i = 0; i < 3; i++)
        //        {
        //            if(Rand.Range(0,10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Blink, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Teleport, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Heal, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Rain, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Heater, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Cooler, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_InnerHealing, null);
        //                target.rewards.TryAdd(item, true);
        //            }

        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_HeavyBlow, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_DryGround, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_WetGround, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_GearRepair, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_ChargeBattery, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_Sprint, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_SmokeCloud, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_FightersFocus, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_EMP, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_Extinguish, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_StrongBack, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_SiphonMana, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_TransferMana, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_ManaShield, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SkillOf_ThickSkin, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_FertileLands, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_SummonMinion, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_CauterizeWound, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else if (Rand.Range(0, 10) > 9.3f)
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.SpellOf_SpellMending, null);
        //                target.rewards.TryAdd(item, true);
        //            }
        //            else
        //            {
        //                item = ThingMaker.MakeThing(TorannMagicDefOf.ManaPotion, null);
        //                target.rewards.TryAdd(item, true);
        //            }

        //        }
                    
        //    }

        //    if(item == null)
        //    {
        //        item = ThingMaker.MakeThing(TorannMagicDefOf.BookOfValiant, null);
        //        target.rewards.TryAdd(item, true);
        //    }           
        //    target.expiration = Find.TickManager.TicksGame + num;
        //    return true;
        //}

        //public static Settlement RandomNearbyTradeableSettlement(int originTile)
        //{
        //    return (from settlement in Find.WorldObjects.Settlements
        //            where settlement.Visitable && settlement.GetComponent<TradeRequestComp>() != null && !settlement.GetComponent<TradeRequestComp>().ActiveRequest && Find.WorldGrid.ApproxDistanceInTiles(originTile, settlement.Tile) < 36f && Find.WorldReachability.CanReach(originTile, settlement.Tile)
        //            select settlement).RandomElementWithFallback(null);
        //}

        //private static ThingDef RandomRequestedThingDef()
        //{
        //    Func<ThingDef, bool> globalValidator = delegate (ThingDef td)
        //    {
        //        //if (td.BaseMarketValue / td.BaseMass < 5f)
        //        //{
        //        //    return false;
        //        //}
        //        if (!td.alwaysHaulable)
        //        {
        //            return false;
        //        }
        //        CompProperties_Rottable compProperties = td.GetCompProperties<CompProperties_Rottable>();
        //        return (compProperties == null || compProperties.daysToRotStart >= 10f) && td != ThingDefOf.Silver && td.PlayerAcquirable;
        //    };
        //    if (Rand.Value < 0.8f)
        //    {
        //        ThingDef result = null;
        //        bool flag = (from td in DefDatabase<ThingDef>.AllDefs
        //                     where (td.IsWithinCategory(ThingCategoryDefOf.FoodMeals) || td.IsWithinCategory(ThingCategoryDefOf.PlantFoodRaw) || td.IsWithinCategory(ThingCategoryDefOf.PlantMatter) || td.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw)) && td.BaseMarketValue < 4f && globalValidator(td)
        //                     select td).TryRandomElement(out result);
        //        if (flag)
        //        {
        //            return result;
        //        }
        //    }
        //    return (from td in DefDatabase<ThingDef>.AllDefs
        //            where (td.IsWithinCategory(ThingCategoryDefOf.Medicine) || td.IsWithinCategory(ThingCategoryDefOf.Drugs) || td.IsWithinCategory(ThingCategoryDefOf.Weapons) || td.IsWithinCategory(ThingCategoryDefOf.Apparel) || td.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw)) && td.BaseMarketValue >= 4f && globalValidator(td)
        //            select td).RandomElementWithFallback(null);
        //}

        //private static int RandomRequestCount(ThingDef thingDef, Map map)
        //{
        //    float num = (float)IncidentWorker_ArcaneScriptCaravan.BaseValueWantedRange.RandomInRange;
        //    float wealthTotal = map.wealthWatcher.WealthTotal;
        //    num *= IncidentWorker_ArcaneScriptCaravan.ValueFactorFromWealthCurve.Evaluate(wealthTotal);
        //    return Mathf.Max(1, Mathf.RoundToInt(num / thingDef.BaseMarketValue));
        //}

        //private int RandomOfferDuration(int tileIdFrom, int tileIdTo)
        //{
        //    int num = IncidentWorker_ArcaneScriptCaravan.OfferDurationRange.RandomInRange;
        //    int num2 = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(tileIdFrom, tileIdTo, null);
        //    float num3 = (float)num2 / 60000f;
        //    int b = Mathf.CeilToInt(Mathf.Max(num3 + 1f, num3 * 1.1f));
        //    num = Mathf.Max(num, b);
        //    if (num > IncidentWorker_ArcaneScriptCaravan.OfferDurationRange.max)
        //    {
        //        return -1;
        //    }
        //    return 60000 * num;
        //}

    }
}
