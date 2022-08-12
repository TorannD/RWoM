using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace TorannMagic
{
    public class WorkGiver_DoMagicBill : WorkGiver_DoBill //WorkGiver_Scanner
    {
        private class DefCountList
        {
            private List<ThingDef> defs = new List<ThingDef>();

            private List<float> counts = new List<float>();

            public int Count => defs.Count;

            public float this[ThingDef def]
            {
                get
                {
                    int num = defs.IndexOf(def);
                    if (num < 0)
                    {
                        return 0f;
                    }
                    return counts[num];
                }
                set
                {
                    int num = defs.IndexOf(def);
                    if (num < 0)
                    {
                        defs.Add(def);
                        counts.Add(value);
                        num = defs.Count - 1;
                    }
                    else
                    {
                        counts[num] = value;
                    }
                    CheckRemove(num);
                }
            }

            public float GetCount(int index)
            {
                return counts[index];
            }

            public void SetCount(int index, float val)
            {
                counts[index] = val;
                CheckRemove(index);
            }

            public ThingDef GetDef(int index)
            {
                return defs[index];
            }

            private void CheckRemove(int index)
            {
                if (counts[index] == 0f)
                {
                    counts.RemoveAt(index);
                    defs.RemoveAt(index);
                }
            }

            public void Clear()
            {
                defs.Clear();
                counts.Clear();
            }

            public void GenerateFrom(List<Thing> things)
            {
                Clear();
                for (int i = 0; i < things.Count; i++)
                {
                    DefCountList defCountList;
                    ThingDef def;
                    (defCountList = this)[def = things[i].def] = defCountList[def] + (float)things[i].stackCount;
                }
            }
        }

        private List<ThingCount> chosenIngThings = new List<ThingCount>();

        private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 600);

        private static string MissingMaterialsTranslated;

        private static List<Thing> relevantThings = new List<Thing>();

        private static HashSet<Thing> processedThings = new HashSet<Thing>();

        private static List<Thing> newRelevantThings = new List<Thing>();

        private static List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();

        private static List<Thing> tmpMedicine = new List<Thing>();

        private static DefCountList availableCounts = new DefCountList();

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        private Building_TMMagicCircleBase magicCircle = null;

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count == 1)
                {
                    return ThingRequest.ForDef(def.fixedBillGiverDefs[0]);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Some;
        }

        public static void ResetStaticData()
        {
            MissingMaterialsTranslated = "MissingMaterials".Translate();
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            IBillGiver billGiver = thing as IBillGiver;
            if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
            {
                LocalTargetInfo target = thing;
                bool ignoreOtherReservations = forced;
                if (pawn.CanReserve(target, 1, -1, null, ignoreOtherReservations) && !thing.IsBurning() && !thing.IsForbidden(pawn))
                {                   

                    CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
                    if (compRefuelable != null && !compRefuelable.HasFuel)
                    {
                        if (!RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced))
                        {
                            return null;
                        }
                        return RefuelWorkGiverUtility.RefuelJob(pawn, thing, forced);
                    }
                    CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
                    if (compMagic != null && compMagic.Mana != null)
                    {
                        if (thing is Building_TMMagicCircle mc)
                        {
                            if(mc.InteractionCellOccupied())
                            {
                                return null;
                            }
                        }
                        else if (compMagic.Mana.CurLevel < .5f)
                        {
                            return null;
                        }
                    }

                    billGiver.BillStack.RemoveIncompletableBills();
                    return StartOrResumeBillJob(pawn, billGiver, thing);
                }
            }
            return null;
        }

        private static UnfinishedThing ClosestUnfinishedThingForBill(Pawn pawn, Bill_ProductionWithUft bill)
        {

            if (ModCheck.Validate.NoJobAuthors.IsInitialized())
            {
                Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && ((UnfinishedThing)t).Recipe == bill.recipe && ((UnfinishedThing)t).ingredients.TrueForAll((Thing x) => bill.IsFixedOrAllowedIngredient(x.def));
                IntVec3 position = pawn.Position;
                Map map = pawn.Map;
                ThingRequest thingReq = ThingRequest.ForDef(bill.recipe.unfinishedThingDef);
                PathEndMode peMode = PathEndMode.InteractionCell;
                TraverseParms traverseParams = TraverseParms.For(pawn, pawn.NormalMaxDanger());
                Predicate<Thing> validator = predicate;
                return (UnfinishedThing)GenClosest.ClosestThingReachable(position, map, thingReq, peMode, traverseParams, 9999f, validator);
            }
            else
            {
                Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && ((UnfinishedThing)t).Recipe == bill.recipe && ((UnfinishedThing)t).Creator == pawn && ((UnfinishedThing)t).ingredients.TrueForAll((Thing x) => bill.IsFixedOrAllowedIngredient(x.def)) && pawn.CanReserve(t);
                IntVec3 position = pawn.Position;
                Map map = pawn.Map;
                ThingRequest thingReq = ThingRequest.ForDef(bill.recipe.unfinishedThingDef);
                PathEndMode peMode = PathEndMode.InteractionCell;
                TraverseParms traverseParams = TraverseParms.For(pawn, pawn.NormalMaxDanger());
                Predicate<Thing> validator = predicate;
                return (UnfinishedThing)GenClosest.ClosestThingReachable(position, map, thingReq, peMode, traverseParams, 9999f, validator);
            }
        }

        private static Job FinishUftJob(Pawn pawn, UnfinishedThing uft, Bill_ProductionWithUft bill)
        {

            if (uft.Creator != pawn && !ModCheck.Validate.NoJobAuthors.IsInitialized())
            {
                Log.Error("Tried to get FinishUftJob for " + pawn + " finishing " + uft + " but its creator is " + uft.Creator);
                return null;
            }
            Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, bill.billStack.billGiver, uft);
            if (job != null && job.targetA.Thing != uft)
            {
                return job;
            }
            Job job2 = new Job(JobDefOf.DoBill, (Thing)bill.billStack.billGiver);
            job2.bill = bill;
            job2.targetQueueB = new List<LocalTargetInfo>
            {
                uft
            };
            job2.countQueue = new List<int>
            {
                1
            };
            job2.haulMode = HaulMode.ToCellNonStorage;
            return job2;
        }

        private Job StartOrResumeBillJob(Pawn pawn, IBillGiver giver, Thing thing)
        {
            for (int i = 0; i < giver.BillStack.Count; i++)
            {
                Bill bill = giver.BillStack[i];
                if ((bill.recipe.requiredGiverWorkType == null || bill.recipe.requiredGiverWorkType == def.workType) && (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + ReCheckFailedBillTicksRange.RandomInRange || FloatMenuMakerMap.makingFor == pawn))
                {
                    bill.lastIngredientSearchFailTicks = 0;
                    if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
                    {
                        bool issueBill = true;
                        this.magicCircle = thing as Building_TMMagicCircleBase;
                        
                        List<Pawn> billPawns = new List<Pawn>();
                        billPawns.Clear();
                        if (bill.recipe is MagicRecipeDef magicRecipe)
                        {
                            CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic(); 
                            if(magicCircle.IsActive)
                            {
                                issueBill = false;
                            }
                            if (!magicCircle.CanEverDoBill(bill, out billPawns, magicRecipe))
                            {
                                issueBill = false;
                            }
                            if(!billPawns.Contains(pawn) && !ModCheck.Validate.NoJobAuthors.IsInitialized())
                            {
                                issueBill = false;
                            }
                        }

                        if (issueBill)
                        {
                            SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
                            if (skillRequirement != null)
                            {
                                JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
                            }
                            else
                            {
                                Bill_ProductionWithUft bill_ProductionWithUft = bill as Bill_ProductionWithUft;
                                if (bill_ProductionWithUft != null)
                                {
                                    if (bill_ProductionWithUft.BoundUft != null)
                                    {
                                        if ((ModCheck.Validate.NoJobAuthors.IsInitialized() || bill_ProductionWithUft.BoundWorker == pawn) && pawn.CanReserveAndReach(bill_ProductionWithUft.BoundUft, PathEndMode.Touch, Danger.Deadly) && !bill_ProductionWithUft.BoundUft.IsForbidden(pawn))
                                        {
                                            return FinishUftJob(pawn, bill_ProductionWithUft.BoundUft, bill_ProductionWithUft);
                                        }
                                        continue;
                                    }
                                    UnfinishedThing unfinishedThing = ClosestUnfinishedThingForBill(pawn, bill_ProductionWithUft);
                                    if (unfinishedThing != null)
                                    {
                                        return FinishUftJob(pawn, unfinishedThing, bill_ProductionWithUft);
                                    }
                                }
                                if (TryFindBestBillIngredients(bill, pawn, (Thing)giver, chosenIngThings))
                                {
                                    this.magicCircle = thing as Building_TMMagicCircle;
                                    if (this.magicCircle != null && bill.recipe is MagicRecipeDef)
                                    {
                                        this.magicCircle.magicRecipeDef = bill.recipe as MagicRecipeDef;
                                        this.magicCircle.MageList.Clear();
                                        magicCircle.MageList.Add(pawn);
                                        //Log.Message("assigning magic bill to " + pawn.LabelShort);
                                        if (bill.recipe is MagicRecipeDef && billPawns.Count > 1)
                                        {
                                            for (int j = 0; j < billPawns.Count; j++)
                                            {
                                                if (billPawns[j] != pawn)
                                                {
                                                    magicCircle.MageList.Add(billPawns[j]);
                                                    magicCircle.IssueAssistJob(billPawns[j]);
                                                    //Log.Message("assisting magic bill to " + billPawns[j].LabelShort);
                                                }
                                            }
                                        }
                                        this.magicCircle.IsPending = true;
                                    }
                                    Job result = TryStartNewDoBillJob(pawn, bill, giver);
                                    chosenIngThings.Clear();
                                    return result;
                                }
                                if (FloatMenuMakerMap.makingFor != pawn)
                                {
                                    bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                                }
                                else
                                {
                                    JobFailReason.Is(MissingMaterialsTranslated, bill.Label);
                                }
                                chosenIngThings.Clear();
                            }
                        }
                    }
                }
            }
            chosenIngThings.Clear();
            return null;
        }

        private Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver)
        {
            Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, giver, null);
            if (job != null)
            {
                return job;
            }
            Job job2 = new Job(JobDefOf.DoBill, (Thing)giver);
            if (bill.recipe is MagicRecipeDef)
            {
                job2 = new Job(TorannMagicDefOf.JobDriver_DoMagicBill, (Thing)giver);
            }
            job2.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
            job2.countQueue = new List<int>(chosenIngThings.Count);
            for (int i = 0; i < chosenIngThings.Count; i++)
            {
                job2.targetQueueB.Add(chosenIngThings[i].Thing);
                job2.countQueue.Add(chosenIngThings[i].Count);
            }
            job2.haulMode = HaulMode.ToCellNonStorage;
            job2.bill = bill;
            return job2;
        }

        public bool ThingIsUsableBillGiver(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            Corpse corpse = thing as Corpse;
            Pawn pawn2 = null;
            if (corpse != null)
            {
                pawn2 = corpse.InnerPawn;
            }
            if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Contains(thing.def))
            {
                return true;
            }
            if (pawn != null)
            {
                if (def.billGiversAllHumanlikes && pawn.RaceProps.Humanlike)
                {
                    return true;
                }
                if (def.billGiversAllMechanoids && pawn.RaceProps.IsMechanoid)
                {
                    return true;
                }
                if (def.billGiversAllAnimals && pawn.RaceProps.Animal)
                {
                    return true;
                }
            }
            if (corpse != null && pawn2 != null)
            {
                if (def.billGiversAllHumanlikesCorpses && pawn2.RaceProps.Humanlike)
                {
                    return true;
                }
                if (def.billGiversAllMechanoidsCorpses && pawn2.RaceProps.IsMechanoid)
                {
                    return true;
                }
                if (def.billGiversAllAnimalsCorpses && pawn2.RaceProps.Animal)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen)
        {
            chosen.Clear();
            newRelevantThings.Clear();
            if (bill.recipe.ingredients.Count == 0)
            {
                return true;
            }
            IntVec3 rootCell = GetBillGiverRootCell(billGiver, pawn);
            Region rootReg = rootCell.GetRegion(pawn.Map);
            if (rootReg == null)
            {
                return false;
            }
            MakeIngredientsListInProcessingOrder(ingredientsOrdered, bill);
            relevantThings.Clear();
            processedThings.Clear();
            bool foundAll = false;
            Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden(pawn) && (float)(t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any((IngredientCount ingNeed) => ingNeed.filter.Allows(t)) && pawn.CanReserve(t);
            bool billGiverIsPawn = billGiver is Pawn;
            if (billGiverIsPawn)
            {
                AddEveryMedicineToRelevantThings(pawn, billGiver, relevantThings, baseValidator, pawn.Map);
                if (TryFindBestBillIngredientsInSet(relevantThings, bill, chosen))
                {
                    relevantThings.Clear();
                    ingredientsOrdered.Clear();
                    return true;
                }
            }
            TraverseParms traverseParams = TraverseParms.For(pawn);
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, isDestination: false);
            int adjacentRegionsAvailable = rootReg.Neighbors.Count((Region region) => entryCondition(rootReg, region));
            int regionsProcessed = 0;
            processedThings.AddRange(relevantThings);
            RegionProcessor regionProcessor = delegate (Region r)
            {
                List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                for (int i = 0; i < list.Count; i++)
                {
                    Thing thing = list[i];
                    if (!processedThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn) && baseValidator(thing) && (!thing.def.IsMedicine || !billGiverIsPawn))
                    {
                        newRelevantThings.Add(thing);
                        processedThings.Add(thing);
                    }
                }
                regionsProcessed++;
                if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                {
                    Comparison<Thing> comparison = delegate (Thing t1, Thing t2)
                    {
                        float num = (float)(t1.Position - rootCell).LengthHorizontalSquared;
                        float value = (float)(t2.Position - rootCell).LengthHorizontalSquared;
                        return num.CompareTo(value);
                    };
                    newRelevantThings.Sort(comparison);
                    relevantThings.AddRange(newRelevantThings);
                    newRelevantThings.Clear();
                    if (TryFindBestBillIngredientsInSet(relevantThings, bill, chosen))
                    {
                        foundAll = true;
                        return true;
                    }
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999);
            relevantThings.Clear();
            newRelevantThings.Clear();
            processedThings.Clear();
            ingredientsOrdered.Clear();
            return foundAll;
        }

        private static IntVec3 GetBillGiverRootCell(Thing billGiver, Pawn forPawn)
        {
            Building building = billGiver as Building;
            if (building != null)
            {
                if (building.def.hasInteractionCell)
                {
                    return building.InteractionCell;
                }
                Log.Error("Tried to find bill ingredients for " + billGiver + " which has no interaction cell.");
                return forPawn.Position;
            }
            return billGiver.Position;
        }

        private static void AddEveryMedicineToRelevantThings(Pawn pawn, Thing billGiver, List<Thing> relevantThings, Predicate<Thing> baseValidator, Map map)
        {
            MedicalCareCategory medicalCareCategory = GetMedicalCareCategory(billGiver);
            List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine);
            tmpMedicine.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (medicalCareCategory.AllowsMedicine(thing.def) && baseValidator(thing) && pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly))
                {
                    tmpMedicine.Add(thing);
                }
            }
            tmpMedicine.SortBy((Thing x) => 0f - x.GetStatValue(StatDefOf.MedicalPotency), (Thing x) => x.Position.DistanceToSquared(billGiver.Position));
            relevantThings.AddRange(tmpMedicine);
            tmpMedicine.Clear();
        }

        private static MedicalCareCategory GetMedicalCareCategory(Thing billGiver)
        {
            Pawn pawn = billGiver as Pawn;
            if (pawn != null && pawn.playerSettings != null)
            {
                return pawn.playerSettings.medCare;
            }
            return MedicalCareCategory.Best;
        }

        private static void MakeIngredientsListInProcessingOrder(List<IngredientCount> ingredientsOrdered, Bill bill)
        {
            ingredientsOrdered.Clear();
            if (bill.recipe.productHasIngredientStuff)
            {
                ingredientsOrdered.Add(bill.recipe.ingredients[0]);
            }
            for (int i = 0; i < bill.recipe.ingredients.Count; i++)
            {
                if (!bill.recipe.productHasIngredientStuff || i != 0)
                {
                    IngredientCount ingredientCount = bill.recipe.ingredients[i];
                    if (ingredientCount.IsFixedIngredient)
                    {
                        ingredientsOrdered.Add(ingredientCount);
                    }
                }
            }
            for (int j = 0; j < bill.recipe.ingredients.Count; j++)
            {
                IngredientCount item = bill.recipe.ingredients[j];
                if (!ingredientsOrdered.Contains(item))
                {
                    ingredientsOrdered.Add(item);
                }
            }
        }

        private static bool TryFindBestBillIngredientsInSet(List<Thing> availableThings, Bill bill, List<ThingCount> chosen)
        {
            if (bill.recipe.allowMixingIngredients)
            {
                return TryFindBestBillIngredientsInSet_AllowMix(availableThings, bill, chosen);
            }
            return TryFindBestBillIngredientsInSet_NoMix(availableThings, bill, chosen);
        }

        private static bool TryFindBestBillIngredientsInSet_NoMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen)
        {
            RecipeDef recipe = bill.recipe;
            chosen.Clear();
            availableCounts.Clear();
            availableCounts.GenerateFrom(availableThings);
            for (int i = 0; i < ingredientsOrdered.Count; i++)
            {
                IngredientCount ingredientCount = recipe.ingredients[i];
                bool flag = false;
                for (int j = 0; j < availableCounts.Count; j++)
                {
                    float num = (float)ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe);
                    if (!(num > availableCounts.GetCount(j)) && ingredientCount.filter.Allows(availableCounts.GetDef(j)) && (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(availableCounts.GetDef(j))))
                    {
                        for (int k = 0; k < availableThings.Count; k++)
                        {
                            if (availableThings[k].def == availableCounts.GetDef(j))
                            {
                                int num2 = availableThings[k].stackCount - ThingCountUtility.CountOf(chosen, availableThings[k]);
                                if (num2 > 0)
                                {
                                    int num3 = Mathf.Min(Mathf.FloorToInt(num), num2);
                                    ThingCountUtility.AddToList(chosen, availableThings[k], num3);
                                    num -= (float)num3;
                                    if (num < 0.001f)
                                    {
                                        flag = true;
                                        float count = availableCounts.GetCount(j);
                                        count -= (float)ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe);
                                        availableCounts.SetCount(j, count);
                                        break;
                                    }
                                }
                            }
                        }
                        if (flag)
                        {
                            break;
                        }
                    }
                }
                if (!flag)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryFindBestBillIngredientsInSet_AllowMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen)
        {            
            chosen.Clear();
            for (int i = 0; i < bill.recipe.ingredients.Count; i++)
            {
                IngredientCount ingredientCount = bill.recipe.ingredients[i];
                float num = ingredientCount.GetBaseCount();
                for (int j = 0; j < availableThings.Count; j++)
                {
                    Thing thing = availableThings[j];
                    if (ingredientCount.filter.Allows(thing) && (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(thing)))
                    {
                        float num2 = bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
                        int num3 = Mathf.Min(Mathf.CeilToInt(num / num2), thing.stackCount);
                        ThingCountUtility.AddToList(chosen, thing, num3);
                        num -= (float)num3 * num2;
                        if (num <= 0.0001f)
                        {
                            break;
                        }
                    }
                }
                if (num > 0.0001f)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
