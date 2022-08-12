using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using System.Diagnostics;
using UnityEngine;
using RimWorld;
using AbilityUser;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMMagicCircleBase : Building_WorkTable
    {

        private static readonly Vector2 BarSize = new Vector2(1.2f, 0.2f);
        private static readonly Material EnergyBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.0f, 0.0f, 1f), false);
        private static readonly Material EnergyBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.4f, 0.4f, 0.4f), false);

        private bool hasPendingJob = false;
        private bool isActive = false;
        private int activeDuration = 0;
        private int matRng = 0;
        private float matMagnitude = 5.2f;

        private int nextCircleEffect = 0;
        private int circleRotation = 0;

        private List<int> resetBills = new List<int>();
        private List<Thing> launchableThings = new List<Thing>();

        private bool suspendReset = false;
        private int resetDelay = 0;

        private List<Thing> recipeIngredients = new List<Thing>();

        public Job activeJob = null;
        public MagicRecipeDef magicRecipeDef = null;

        public float manaReq = 0;

        private List<Pawn> RandomPosIndexMages = new List<Pawn>();
        private List<IntVec3> RandomPosIndexPosition = new List<IntVec3>();

        private List<Pawn> mageList = new List<Pawn>();
        public List<Pawn> MageList
        {
            get
            {
                if(mageList == null)
                {
                    mageList = new List<Pawn>();
                    mageList.Clear();
                }
                return mageList;
            }
            set
            {
                if(mageList == null)
                {
                    mageList = new List<Pawn>();
                    mageList.Clear();
                }
                mageList = value;
            }
        }

        public virtual IntVec3 GetCircleCenter
        {
            get
            {
                IntVec3 center = this.InteractionCell;
                if (this.Rotation == Rot4.North)
                {
                    center.z -= 2;
                }
                else if (this.Rotation == Rot4.South)
                {
                    center.z += 2;
                }
                else if (this.Rotation == Rot4.West)
                {
                    center.x += 2;
                }
                else
                {
                    center.x -= 2;
                }
                return center;
            }
        }

        public Job ActiveJob
        {
            get
            {
                if(MageList.Count > 0)
                {
                    return this.MageList[0].CurJob;
                }
                else
                {
                    return null;
                }
            }
        }

        public virtual float SpellSuccessModifier
        {
            get
            {
                if(Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Jade"))
                    {
                        return .2f;
                    }
                    else if (this.Stuff == ThingDef.Named("Uranium"))
                    {
                        return .15f;
                    }      
                    else if(this.Stuff == TorannMagicDefOf.TM_Arcalleum)
                    {
                        return .1f;
                    }
                }
                return 0f;
            }
        }

        public virtual float MaterialCostModifier
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Silver"))
                    {
                        return .2f;
                    }
                }
                return 0f;
            }
        }

        public virtual float DurationModifier
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Gold"))
                    {
                        return .2f;
                    }
                    else if(this.Stuff == ThingDef.Named("Uranium"))
                    {
                        return .15f;
                    }
                }
                return 0f;
            }
        }

        public virtual float ManaCostModifer
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == TorannMagicDefOf.TM_Arcalleum)
                    {
                        return .2f;
                    }
                }
                return 0f;
            }
        }

        public virtual float PointModifer
        {
            get
            {
                if (Stuff != null)
                {
                    if (this.Stuff == ThingDef.Named("Gold"))
                    {
                        return .1f;
                    }
                    else if(this.Stuff == ThingDefOf.Plasteel)
                    {
                        return .15f;
                    }
                }
                return 0f;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.manaReq, "manaReq", 0f, false);
            Scribe_Collections.Look<Pawn>(ref this.mageList, "mageList", LookMode.Reference);
            Scribe_Defs.Look<MagicRecipeDef>(ref this.magicRecipeDef, "magicRecipeDef");
        }

        public bool IsPending
        {
            get
            {
                return this.hasPendingJob;
            }
            set
            {
                this.hasPendingJob = value;
            }
        }

        public bool IsActive
        {
            get
            {
                return this.isActive;
            }
        }

        public float PercentComplete
        {
            get
            {
                if(IsActive)
                {
                    return 1 - (this.activeDuration / (this.magicRecipeDef.workAmount / 100f));
                }
                else
                {
                    return 0f;
                }
            }
        }

        public int ExactRotation
        {
            get
            {
                return Mathf.RoundToInt((this.magicRecipeDef.workAmount/3000)*360*(this.PercentComplete*this.PercentComplete));
            }
        }

        public virtual IEnumerable<ThingDef> PotentiallyMissingIngredients(MagicRecipeDef mrDef, bool launch = false)
        {
            this.launchableThings = new List<Thing>();
            this.launchableThings.Clear();
            List<IngredientCount> ingredients = mrDef.ingredients;
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientCount ing = ingredients[i];
                bool foundIng = false;
                List<Thing> thingList = this.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
                int totalStackCount = 0;
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing = thingList[j];
                    if ((thing.Position - this.InteractionCell).LengthHorizontal < 4 && (ing.IsFixedIngredient || mrDef.fixedIngredientFilter.Allows(thing)) && ing.filter.Allows(thing))
                    {
                        this.launchableThings.Add(thing);
                        totalStackCount += thing.stackCount;
                        if (ing.IsFixedIngredient && totalStackCount >= ing.GetBaseCount())
                        {
                            foundIng = true;
                            break;
                        }
                    }
                }
                if (foundIng)
                {
                    continue;
                }
                if (ing.IsFixedIngredient)
                {
                    yield return ing.filter.AllowedThingDefs.First();
                    continue;
                }
                ThingDef def = (from x in ing.filter.AllowedThingDefs.InRandomOrder()
                                select x).FirstOrDefault((ThingDef x) => mrDef.fixedIngredientFilter.Allows(x));
                if (def != null)
                {
                    if (def.IsCorpse)
                    {
                        bool hasCorpses = false;
                        List<Thing> ingredient = new List<Thing>();
                        ingredient.Clear();
                        for (int u = 0; u < ActiveJob.RecipeDef.ingredients.Count; u++)
                        {
                            List<ThingDef> allowedThings = ActiveJob.RecipeDef.ingredients[u].filter.AllowedThingDefs.ToList();
                            if (allowedThings != null && allowedThings.Count > 0)
                            {
                                
                                int corpseCount = 0;
                                List<IntVec3> cellList = GenRadial.RadialCellsAround(this.InteractionCell, 3, true).ToList();
                                for (int j = 0; j < cellList.Count; j++)
                                {
                                    List<Thing> cellThings = cellList[j].GetThingList(this.Map).ToList();
                                    for (int k = 0; k < cellThings.Count; k++)
                                    {
                                        if (cellThings[k].def.IsCorpse)
                                        {
                                            corpseCount++;
                                        }
                                    }
                                    if (ActiveJob.RecipeDef.ingredients[i].GetBaseCount() <= corpseCount)
                                    {
                                        hasCorpses = true;                                        
                                    }
                                }
                            }
                        }
                        if(hasCorpses)
                        {
                            continue;
                        }
                        else
                        {
                            yield return def;
                        }
                    }
                    else
                    {
                        yield return def;
                    }
                }
            }
        }

        public virtual bool HasIngredients
        {
            get
            {
                bool result = false;
                if (ActiveJob != null && IsPending)
                {
                    if (ActiveJob.RecipeDef != null && ActiveJob.RecipeDef.ingredients != null)
                    {
                        //Log.Message("ingredients " + PotentiallyMissingIngredients(ActiveJob.RecipeDef as MagicRecipeDef).FirstOrDefault());
                        ThingDef missingThing = PotentiallyMissingIngredients(ActiveJob.RecipeDef as MagicRecipeDef).FirstOrDefault();
                        if (missingThing == null)
                        {
                            result = true;
                        }                        
                    }
                }
                return result;                       
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        }

        public override void Tick()
        { 
            bool billsActionable = false;
            if (this.suspendReset)
            {
                if(this.resetDelay < Find.TickManager.TicksGame)
                {
                    this.suspendReset = false;
                }
            }
            else if(IsActive)
            {
                //rotate magic circle (draw) and do effects until job is complete
                this.activeDuration--;
                if(this.nextCircleEffect <= Find.TickManager.TicksGame)
                {
                    Vector3 rndPos = GetCircleCenter.ToVector3Shifted();
                    rndPos.x += Rand.Range(-3, 3);
                    rndPos.z += Rand.Range(-3, 3);
                    FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.8f, 1.5f));
                    this.circleRotation = Mathf.Max(this.circleRotation - 2, 8);
                    this.nextCircleEffect = Find.TickManager.TicksGame + Mathf.Clamp(this.circleRotation, 8, 300);
                }
                if(this.activeDuration <= 0)
                {
                    ModOptions.Constants.SetBypassPrediction(true);
                    if (Rand.Chance(Mathf.Clamp01(this.magicRecipeDef.failChance - SpellSuccessModifier))) //- clamp on 0 and look for circle construction material for fail chance reduction
                    {
                        //do spell failure actions
                        DoFailActions();
                    }
                    else
                    {
                        //do spell success actions
                        DoSuccessActions();
                    }
                    DoActiveEffecter();
                    this.isActive = false;
                    this.magicRecipeDef = null;
                    ClearAllJobs();
                    ModOptions.Constants.SetBypassPrediction(false);
                }
            }
            else
            {
                if (Find.TickManager.TicksGame % 51 == 0)
                {
                    billsActionable = false;
                    for (int i = 0; i < this.BillStack.Bills.Count; i++)
                    {                        
                        Bill bill = this.BillStack.Bills[i];
                        MagicRecipeDef mrDef = this.BillStack.Bills[i].recipe as MagicRecipeDef;
                        List<Pawn> billDoers = new List<Pawn>();
                        if (CanEverDoBill(bill, out billDoers))
                        {
                            bill.suspended = false;
                            billsActionable = true;                            
                        }
                        else
                        {
                            bill.suspended = true;
                        }
                    }
                    if (billsActionable)
                    {
                        this.GetComp<CompRefuelable>().Refuel(1);
                        if(this.MageList.Count == 0)
                        {
                            ScanForRepeatJob();
                        }
                    }
                    else
                    {
                        this.GetComp<CompRefuelable>().ConsumeFuel(1);
                        if(this.hasPendingJob)
                        {
                            ClearAllJobs();
                        }
                    }
                }

                if (this.IsPending)
                {
                    if (Find.TickManager.TicksGame % 81 == 0 && !this.Map.gameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
                    {
                        if(this.magicRecipeDef == null && this.ActiveJob != null)
                        {
                            this.magicRecipeDef = this.ActiveJob.RecipeDef as MagicRecipeDef;
                        }
                        if (this.magicRecipeDef != null)
                        {
                            bool magesAvailable = false;
                            if (this.MageList.Count > 0)
                            {
                                magesAvailable = PendingMagesStillAvailable(this.MageList);
                            }
                            bool ingredientsAvailable = HasIngredients;
                            bool magesReady = false;
                            if (magesAvailable)
                            {                                
                                magesReady = MagesReadyToAssist(this.MageList);
                            }
                            else
                            {
                                ClearAllJobs();
                            }
                            //Log.Message("mages available " + magesAvailable + " ready: " + magesReady + " ingredients " + ingredientsAvailable);
                            if (magesAvailable && ingredientsAvailable && magesReady)
                            {
                                this.ActiveJob.bill.Notify_IterationCompleted(mageList[0], null);
                                this.isActive = true;
                                this.activeDuration = Mathf.RoundToInt(this.magicRecipeDef.workAmount / 100);
                                this.circleRotation = (int)(this.magicRecipeDef.workAmount / (1000));
                                this.nextCircleEffect = Find.TickManager.TicksGame + this.circleRotation;
                                LaunchJobPawns();
                                LaunchIngredients();
                            }
                        }
                    }
                }
            }
        }

        public virtual void DoActiveEffecter()
        {
        }

        public virtual void DoFailActions()
        {
            if(this.mageList != null && this.mageList.Count > 0 && this.magicRecipeDef != null)
            {
                for(int i =0; i < this.mageList.Count; i++)
                {
                    TM_Action.ConsumeManaXP(mageList[i], (this.magicRecipeDef.manaCost/this.mageList.Count) * (this.magicRecipeDef.failManaConsumed * (1f - ManaCostModifer)), .75f, true);
                    if(this.magicRecipeDef.failDamageApplied != 0)
                    {
                        TM_Action.DamageEntities(this.mageList[i], null, Rand.Range(this.magicRecipeDef.failDamageApplied * .75f, this.magicRecipeDef.failDamageApplied * 1.25f), TMDamageDefOf.DamageDefOf.TM_Arcane, this.mageList[i]);
                    }
                }
            }            
            string letterLabel = "LetterLabelRitualFail".Translate();
            if (this.magicRecipeDef.failDamageApplied > 0)
            {
                Find.LetterStack.ReceiveLetter(letterLabel, "LetterRitualFailDamage".Translate(this.magicRecipeDef.label, ((1f - this.magicRecipeDef.failChance) * 100), this.magicRecipeDef.failDamageApplied), LetterDefOf.NegativeEvent);
            }
            else
            {
                Find.LetterStack.ReceiveLetter(letterLabel, "LetterRitualFail".Translate(this.magicRecipeDef.label, (1f - this.magicRecipeDef.failChance) * 100f), LetterDefOf.NegativeEvent);
            }
        }

        public virtual void DoSuccessActions()
        {
            if (this.mageList != null && this.mageList.Count > 0 && this.magicRecipeDef != null)
            {
                for (int i = 0; i < this.mageList.Count; i++)
                {                    
                    TM_Action.ConsumeManaXP(mageList[i], (this.magicRecipeDef.manaCost/this.mageList.Count)*(1f-ManaCostModifer), 1f + ManaCostModifer, true);
                }

                if (this.magicRecipeDef.resultMapComponentConditions != null && this.magicRecipeDef.resultMapComponentConditions.Count > 0)
                {
                    for (int i = 0; i < this.magicRecipeDef.resultMapComponentConditions.Count; i++)
                    {
                        MagicMapComponent mmc = this.Map.GetComponent<MagicMapComponent>();
                        if (mmc != null)
                        {
                            mmc.ApplyComponentConditions(this.magicRecipeDef.resultMapComponentConditions[i]);
                        }
                    }
                }
                if (this.magicRecipeDef.resultConditions != null && this.magicRecipeDef.resultConditions.Count > 0)
                {
                    for (int i = 0; i < this.magicRecipeDef.resultConditions.Count; i++)
                    {                        
                        TMDefs.TM_Condition con = this.magicRecipeDef.resultConditions[i];
                        if (con.resultCondition != null || con.conditionRandom)
                        {
                            int range = con.countRange.RandomInRange;
                            for (int j = 0; j < range; j++)
                            {
                                TryGenerateMapCondition(con.resultCondition, this.Map, Mathf.RoundToInt(con.conditionDuration * (1+DurationModifier)), con.conditionPermanent, con.conditionRemove, con.conditionAdd, con.conditionReduceByDuration, con.conditionIncreaseByDuration, con.conditionRandom);
                            }
                        }
                        if(con.resultWeather != null)
                        {
                            TryGenerateWeatherEvent(con.resultWeather, this.Map);
                        }
                    }
                }
                if(this.magicRecipeDef.resultIncidents != null && this.magicRecipeDef.resultIncidents.Count > 0)
                {
                    int incidentCount = 0;
                    if (this.magicRecipeDef.selectRandomIncident)
                    {
                        incidentCount = this.magicRecipeDef.selectRandomIncidentCount;
                    }
                    else
                    {
                        incidentCount = this.magicRecipeDef.resultIncidents.Count;
                    }
                    for (int i = 0; i < incidentCount; i++)
                    {
                        TMDefs.TM_Incident inc = this.magicRecipeDef.resultIncidents[i];
                        if (this.magicRecipeDef.selectRandomIncident)
                        {
                            inc = this.magicRecipeDef.resultIncidents[Rand.RangeInclusive(0, this.magicRecipeDef.resultIncidents.Count - 1)];
                        }                        
                        if (inc.resultIncident != null)
                        {
                            int range = inc.countRange.RandomInRange;
                            for (int j = 0; j < range; j++)
                            {
                                TryGenerateIncident(inc.resultIncident, this.Map, Mathf.RoundToInt(inc.incidentPoints * (1f+PointModifer)), inc.incidentHostile, this.Position);
                            }
                        }
                    }
                }
                if (this.magicRecipeDef.resultHediffs != null && this.magicRecipeDef.resultHediffs.Count > 0)
                {
                    for (int i = 0; i < this.magicRecipeDef.resultHediffs.Count; i++)
                    {
                        TMDefs.TM_Hediff hd = this.magicRecipeDef.resultHediffs[i];
                        if (hd.resultHediff != null)
                        {
                            int range = hd.countRange.RandomInRange;
                            for (int j = 0; j < range; j++)
                            {
                                TryApplyHediff(hd.resultHediff, this.Faction, this.Map, this.MageList[0], (hd.hediffSeverity * (1f+ DurationModifier)), Mathf.RoundToInt(hd.maxHediffCount * (1f + PointModifer)), hd.checkResistance, hd.applyFriendly, hd.applyEnemy, hd.applyNeutral, hd.applyNullFaction, hd.moteDef);
                            }
                        }
                    }
                }
                if (this.magicRecipeDef.resultSpawnThings != null && this.magicRecipeDef.resultSpawnThings.Count > 0)
                {
                    for (int i = 0; i < this.magicRecipeDef.resultSpawnThings.Count; i++)
                    {
                        TMDefs.TM_SpawnThings st = this.magicRecipeDef.resultSpawnThings[i];
                        if (st.resultSpawnThing != null)
                        {
                            int range = st.countRange.RandomInRange;
                            for (int j = 0; j < range; j++)
                            {
                                IntVec3 spawnPos = GetCircleCenter;
                                spawnPos.x += Rand.Range(-(range - 1), (range - 1));
                                spawnPos.z += Rand.Range(-(range - 1), (range - 1));
                                TrySpawnThing(this.MageList.RandomElement(), st.resultSpawnThing, st.resultPawnKindDef, spawnPos, Mathf.RoundToInt(st.summonDuration * (1f+ DurationModifier)), st.summonTemporary, Mathf.RoundToInt(st.spawnThingCount * (1f + PointModifer)), Mathf.RoundToInt(st.spawnThingStackCount * (1f+ PointModifer)), st.spawnHostile);
                            }
                        }
                    }
                }                
            }            
        }

        public virtual bool InteractionCellOccupied()
        {
            List<Thing> thingList = this.InteractionCell.GetThingList(this.Map);
            for(int i = 0; i < thingList.Count; i++)
            {
                if(thingList[i] != this && thingList[i].def.EverHaulable)
                {
                    //Log.Message("ic occupied by " + thingList[i].LabelShort);
                    return true;
                }
            }
            return false;
        }

        public virtual bool CanEverDoBill(Bill bill, out List<Pawn> pawnsAble, MagicRecipeDef mrDefIn = null)
        {
            MagicRecipeDef mrDef = null;
            if (bill != null)
            {
                mrDef = bill.recipe as MagicRecipeDef;
            }
            if (mrDefIn != null)
            {
                mrDef = mrDefIn;
            }            
            pawnsAble = new List<Pawn>();
            pawnsAble.Clear();
            if (mrDef != null && mrDef is MagicRecipeDef)
            {
                float manaReq = mrDef.manaCost;
                if (mrDef.mageCount > 0)
                {
                    manaReq = mrDef.manaCost / mrDef.mageCount;
                }
                List<Pawn> magePawnsInRange = TM_Calc.FindNearbyMages(this.Position, this.Map, this.Faction, 50, true);
                if (magePawnsInRange != null && magePawnsInRange.Count > 0)
                {
                    for (int i =0; i < magePawnsInRange.Count; i++)
                    {
                        Pawn p = magePawnsInRange[i];
                        CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                        if (p.Spawned && !p.Drafted && !p.InMentalState && p.GetPosture() == PawnPosture.Standing && p.workSettings.WorkIsActive(TorannMagicDefOf.TM_Magic) && comp != null && comp.Mana != null && comp.Mana.CurLevel >= manaReq)
                        {
                            pawnsAble.Add(p);
                            if(pawnsAble.Count >= mrDef.mageCount)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        //public bool CanDoJob(CompAbilityUserMagic abilityUser, MagicRecipeDef mrDef, Thing workTable)
        //{
        //    if (mrDef.mageCount == 0)
        //    {
        //        manaReq = mrDef.manaCost;
        //    }
        //    else
        //    {
        //        manaReq = mrDef.manaCost / mrDef.mageCount;
        //    }
        //    if (!this.hasPendingJob && !this.isActive && abilityUser.Mana != null && abilityUser.Mana.CurLevel >= manaReq)
        //    {
        //        this.mageList = new List<Pawn>();
        //        this.mageList.Clear();
        //        mageList.Add(abilityUser.Pawn);
        //        if (mrDef.mageCount > 1)
        //        {
        //            List<Pawn> magePawnsInRange = TM_Calc.FindNearbyMages(workTable.Position, workTable.Map, workTable.Faction, 40, true);
        //            if (magePawnsInRange != null && magePawnsInRange.Count > 0)
        //            {
        //                //Log.Message("Found " + magePawnsInRange.Count + " mages");
        //                if (magePawnsInRange.Count >= mrDef.mageCount)
        //                {
        //                    for (int i = 0; i < magePawnsInRange.Count; i++)
        //                    {
        //                        Pawn p = magePawnsInRange[i];
        //                        CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
        //                        if (p != abilityUser.Pawn && p.workSettings.WorkIsActive(TorannMagicDefOf.TM_Magic) && comp != null && comp.Mana != null && comp.Mana.CurLevel >= manaReq && p.GetPosture() == PawnPosture.Standing && !p.InMentalState)
        //                        {
        //                            //Log.Message("" + p.LabelShort + " available to work recipe " + mrDef.defName);
        //                            mageList.Add(p);
        //                            if (mageList.Count >= mrDef.mageCount)
        //                            {
        //                                //Log.Message("" + mrDef.mageCount + " of " + mageList.Count + " found");                                        
        //                                //this.hasPendingJob = true;
        //                                this.mageList.Clear();
        //                                return true;
        //                                //break;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            //else
        //            //{
        //            //    Log.Message("no mages found");
        //            //}
        //        }
        //        else
        //        {
        //            this.hasPendingJob = true;
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public virtual void IssueAssistJob(Pawn pawn)
        {
            Job job = new Job(TorannMagicDefOf.JobDriver_AssistMagicCircle, GetMageIndexPosition(this.MageList, pawn), this);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public virtual void ClearAllJobs()
        {            
            for (int i = 0; i < MageList.Count; i++)
            {
                //MageList[i].jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                if (MageList[i].jobs != null)
                {
                    MageList[i].jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                    MageList[i].jobs.StopAll(true);
                }
            }
            MageList.Clear();
            this.activeJob = null;
            this.hasPendingJob = false;
            this.suspendReset = true;
            this.resetDelay = Find.TickManager.TicksGame + 301;
            this.TryGetComp<CompRefuelable>().ConsumeFuel(1);
            //this.resetBills = new List<int>();
            //resetBills.Clear();
            //for (int i = 0; i < this.billStack.Bills.Count; i++)
            //{
            //    resetBills.Add(this.billStack.Bills[i].billStac);
            //}
            //this.BillStack.Clear();
            //for (int i = 0; i < this.InteractionCell.GetThingList(this.Map).Count; i++)
            //{
            //    Thing icThing = this.InteractionCell.GetThingList(this.Map)[i];
            //    if (icThing != this && !(icThing is Pawn))
            //    {
            //        icThing.DeSpawn(DestroyMode.Vanish);
            //        GenPlace.TryPlaceThing(icThing, this.Position, this.Map, ThingPlaceMode.Near);
            //    }
            //}
        }

        public virtual void ScanForRepeatJob()
        {
            List<Pawn> allPawns = this.Map.mapPawns.AllPawnsSpawned;
            if(allPawns != null && allPawns.Count > 0)
            {
                for(int i = 0; i < allPawns.Count; i++)
                {
                    if(allPawns[i].jobs != null && allPawns[i].CurJobDef == TorannMagicDefOf.JobDriver_DoMagicBill)
                    {
                        //Log.Message("checking can do job");
                        this.MageList.Clear();
                        this.MageList.Add(allPawns[i]);
                        this.hasPendingJob = true;
                        this.magicRecipeDef = allPawns[i].CurJob.bill.recipe as MagicRecipeDef;
                        //Log.Message("checking mages available: " + mages.Count);
                       
                        //CanDoJob(allPawns[i].GetCompAbilityUserMagic(), allPawns[i].CurJob.bill.recipe as MagicRecipeDef, this);
                    }
                }
            }
        }

        public virtual bool PendingMagesStillAvailable(List<Pawn> mages)
        {
            //Log.Message("checking mages available " + mages.Count);
            for (int i = 0; i < mages.Count; i++)
            {
                //Log.Message("pawn " + i + " " + mages[i].LabelShort + " with job " + mages[i].CurJob);
                CompAbilityUserMagic comp = mages[i].GetCompAbilityUserMagic();
                if (comp != null && comp.Mana != null )
                {
                    //Log.Message("" + comp.Pawn.LabelShort + " is ready");
                    if (i == 0 && !((comp.Pawn.CurJobDef == TorannMagicDefOf.JobDriver_DoMagicBill || comp.Pawn.CurJobDef == JobDefOf.HaulToCell) && comp.Mana.CurLevel >= this.manaReq && !comp.Pawn.Drafted && !comp.Pawn.Destroyed && comp.Pawn.Spawned && !comp.Pawn.Downed && !comp.Pawn.InMentalState))
                    {
                        //pawn working recipe is no longer doing the job
                        //Log.Message("job pawn no longer available: " + mageList[i].LabelShort + " doing job  " + comp.Pawn.CurJobDef.defName);
                        return false;                        
                    }
                    if (i > 0 && !((comp.Pawn.CurJobDef == TorannMagicDefOf.JobDriver_AssistMagicCircle) && comp.Mana.CurLevel >= this.manaReq && !comp.Pawn.Drafted && !comp.Pawn.Destroyed && comp.Pawn.Spawned && !comp.Pawn.Downed && !comp.Pawn.InMentalState))
                    {
                        //Log.Message("" + comp.Pawn.LabelShort + " had job " + comp.Pawn.CurJobDef.defName + ";; searching for new pawn");
                        List<Pawn> replacementList = new List<Pawn>();
                        replacementList.Clear();
                        if(CanEverDoBill(null, out replacementList, this.ActiveJob.RecipeDef as MagicRecipeDef))
                        {
                            for(int j = 0; j < replacementList.Count; j++)
                            {
                                if(!mages.Contains(replacementList[j]))
                                {
                                    this.MageList.Remove(mages[i]);
                                    this.MageList.Insert(i, replacementList[j]);
                                    IssueAssistJob(replacementList[j]);
                                    //Log.Message("can still do bill");
                                    return true;
                                }
                            }
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                        //IssueAssistJob(comp.Pawn);
                        //try to give a new job
                        //return false;
                    }
                }
                else
                {
                    //Log.Message("returning mages NOT available");
                    return false;
                }
            }
            if (this.ActiveJob != null && this.ActiveJob.RecipeDef is MagicRecipeDef)
            {
                if (mages.Count() < (this.ActiveJob.RecipeDef as MagicRecipeDef).mageCount)
                {
                    List<Pawn> replacementList = new List<Pawn>();
                    replacementList.Clear();
                    if (CanEverDoBill(null, out replacementList, this.ActiveJob.RecipeDef as MagicRecipeDef))
                    {
                        int itCount = (this.ActiveJob.RecipeDef as MagicRecipeDef).mageCount - mages.Count();
                        for (int i = 0; i < replacementList.Count; i++)
                        {
                            if (!this.MageList.Contains(replacementList[i]))
                            {
                                this.MageList.Add(replacementList[i]);
                                IssueAssistJob(replacementList[i]);
                                itCount--;
                                if (itCount <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        public virtual void TryAssignAssistJobs(Pawn leadPawn)
        {
            List<Pawn> outPawns = new List<Pawn>();
            this.mageList.Clear();
            this.mageList.Add(leadPawn);
            if (CanEverDoBill(null, out outPawns, this.magicRecipeDef))
            {
                for(int i = 0; i < outPawns.Count; i++)
                {
                    if (outPawns[i] != leadPawn)
                    {
                        this.mageList.Add(outPawns[i]);
                        IssueAssistJob(outPawns[i]);
                    }
                }
            }
        }

        public bool MagesReadyToAssist(List<Pawn> mages)
        {
            for(int i =0; i < mages.Count; i++)
            {
                if(mages[i].Position != GetMageIndexPosition(mages, mages[i]))
                {
                    //Log.Message("" + mages[i] + " out of position");
                    return false;
                }
            }
            return true;
        }

        //[DebuggerHidden]
        //public override IEnumerable<Gizmo> GetGizmos()
        //{
        //    foreach (Gizmo g in base.GetGizmos())
        //    {
        //        yield return g;
        //    }
        //    if (DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>() != null)
        //    {
        //        yield return new Command_Action
        //        {
        //            action = new Action(this.MakeMatchingStockpile),
        //            hotKey = KeyBindingDefOf.Misc3,
        //            defaultDesc = "TM_CommandMakePortalStockpileDesc".Translate(),
        //            icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true),
        //            defaultLabel = "TM_CommandMakePortalStockpileLabel".Translate()
        //        };
        //    }
        //}

        //public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        //{
        //    List<FloatMenuOption> list = new List<FloatMenuOption>();
           
        //    return list;
        //}       

        public virtual void LaunchJobPawns()
        {
            if(this.MageList != null && this.MageList.Count > 0)
            {
                for (int i = 0; i < mageList.Count; i++)
                {
                    Pawn pawn = mageList[i];
                    if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                    {
                        if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                        {
                            ModCheck.GiddyUp.ForceDismount(pawn);
                        }
                        FlyingObject_TimeDelay flyingObject = (FlyingObject_TimeDelay)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_TimeDelay, pawn.Position, pawn.Map);
                        flyingObject.speed = 10f;
                        flyingObject.duration = this.activeDuration - 10;
                        flyingObject.solidTime = .2f;
                        flyingObject.fadeInTime = 0;
                        flyingObject.fadeOutTime = .45f;
                        flyingObject.moteScale = 1.5f;
                        flyingObject.LaunchVaryPosition(this, pawn.Position, pawn, 0, .2f, .5f, TorannMagicDefOf.Mote_Casting, 5);
                    }
                }
            }
        }

        public virtual void LaunchIngredients()
        {
            for (int i = 0; i < this.launchableThings.Count; i++)
            {
                //Log.Message("ingredient to launch " + launchableThings[i] + " with stack count " + launchableThings[i].stackCount);
                //ingredient.Clear();
                //if (ActiveJob.RecipeDef.ingredients[i].IsFixedIngredient)
                //{
                //    ingredient = this.Map.listerThings.ThingsOfDef(ActiveJob.RecipeDef.ingredients[i].FixedIngredient);
                //}
                //else
                //{
                //    for (int j = 0; j < this.Map.listerThings.AllThings.Count; j++)
                //    {
                //        Thing t = this.Map.listerThings.AllThings[j];
                //        if ((t.Position - this.InteractionCell).LengthHorizontal < 4 && ActiveJob.RecipeDef.ingredients[i].filter.AllowedThingDefs.Contains(t.def))
                //        {
                //            ingredient.Add(t);
                //        }
                //    }
                //}
                int launchCount = 0;
                float stackCount = this.launchableThings[i].stackCount * (1f - MaterialCostModifier);
                //if(this.magicRecipeDef.ingredients[i].FixedIngredient.smallVolume)
                //{
                //    stackCount *= 10f;
                //}
                //for (int j = 0; j < ingredient.Count; j++)
                //{
                //    if ((ingredient[j].Position - this.InteractionCell).LengthHorizontal <= 5 && launchedCount < stackCount)
                //    {
                //        int thisLaunchCount = Mathf.Clamp(ingredient[j].stackCount, 0, (int)stackCount - launchedCount);
                //        launchedCount += thisLaunchCount;
                launchCount = Mathf.RoundToInt(stackCount);
                Thing thing = launchableThings[i].SplitOff(launchCount);
                GenPlace.TryPlaceThing(thing, GetCircleCenter, this.Map, ThingPlaceMode.Direct);
                FlyingObject_TimeDelay flyingObject = (FlyingObject_TimeDelay)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_TimeDelay, GetCircleCenter, this.Map);
                flyingObject.speed = 10f;
                flyingObject.duration = this.activeDuration - 10;
                flyingObject.stackCount = launchCount;
                flyingObject.LaunchVaryPosition(this, thing.Position, thing, 0, .8f, .8f, null, 0, 1f);
                //i--;
                //    }
                //}
            }
        }

        private static void ConsumeIngredients(List<Thing> ingredients, RecipeDef recipe, Map map)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                recipe.Worker.ConsumeIngredient(ingredients[i], recipe, map);
            }
        }

        public virtual IntVec3 GetMageIndexPosition(List<Pawn> allMages, Pawn mage)
        {
            IntVec3 magePosition = default(IntVec3);
            for (int i = 0; i < allMages.Count; i++)
            {
                if (allMages[i] == mage)
                {
                    IntVec3 ic = this.InteractionCell;
                    if (i == 0)
                    {
                        //always interaction cell
                    }
                    else
                    {
                        ic = GetRandomStaticIndexPositionFor(mage);                        
                    }
                    magePosition = ic;
                }
            }
            return magePosition;
        }

        public IntVec3 GetRandomStaticIndexPositionFor(Pawn p)
        {
            if(this.RandomPosIndexMages == null || this.RandomPosIndexPosition == null)
            {
                this.RandomPosIndexPosition = new List<IntVec3>();
                this.RandomPosIndexPosition.Clear();
                this.RandomPosIndexMages = new List<Pawn>();
                this.RandomPosIndexMages.Clear();
            }
            for(int i = 0; i < this.RandomPosIndexMages.Count; i++)
            {
                if(p == this.RandomPosIndexMages[i])
                {
                    return this.RandomPosIndexPosition[i];
                }
            }
            IntVec3 ic = GetCircleCenter;
            ic.x += Rand.Range(Mathf.RoundToInt(-this.def.Size.x/2f), Mathf.RoundToInt(this.def.Size.x/2f));
            ic.z += Rand.Range(Mathf.RoundToInt(-this.def.Size.z / 2f), Mathf.RoundToInt(this.def.Size.z/2f));
            this.RandomPosIndexMages.Add(p);
            this.RandomPosIndexPosition.Add(ic);
            return ic;
        }

        public static void TryGenerateMapCondition(GameConditionDef gameCondition, Map map, int duration = -1, bool permanent = false, bool remove = false, bool add = false, bool reduce = false, bool increase = false, bool random = false)
        {
            GameCondition gc = null;
            List<GameCondition> gcList = new List<GameCondition>();
            map.GameConditionManager.GetAllGameConditionsAffectingMap(map, gcList);
            if (random && !add && gcList != null && gcList.Count > 0)
            {
                gc = map.GameConditionManager.GetActiveCondition(gcList.RandomElement().def);
            }
            else if(gameCondition != null && map.GameConditionManager.ConditionIsActive(gameCondition))
            {
                gc = map.GameConditionManager.GetActiveCondition(gameCondition);
            }
            else if(gameCondition != null)
            {
                gc = GameConditionMaker.MakeCondition(gameCondition, duration); 
            }
            else if(gameCondition == null && random && add)
            {
                IEnumerable<GameConditionDef> enumerable = from def in DefDatabase<GameConditionDef>.AllDefs
                                                   where (true)
                                                   select def;
                gc = GameConditionMaker.MakeCondition(enumerable.RandomElement());
            }

            if (gc != null)
            {
                if (permanent)
                {
                    gc.Permanent = true;
                }
                else if (remove)
                {
                    gc.End();
                }
                else if (duration != -1)
                {
                    if (reduce)
                    {
                        gc.Duration -= duration;
                        if (gc.Duration < 0)
                        {
                            gc.End();
                        }
                    }
                    else if(increase)
                    {
                        gc.Duration += duration;
                    }
                    else
                    {
                        gc.Duration = duration;
                    }
                }
                map.gameConditionManager.RegisterCondition(gc);
            }
            else
            {
                Messages.Message("No game condition found.", MessageTypeDefOf.RejectInput);
            }
        }

        public static void TryGenerateWeatherEvent(WeatherDef wd, Map map)
        {
            map.weatherManager.TransitionTo(wd);
        }

        public static void TryGenerateIncident(IncidentDef id, Map map, float points = 0f, bool hostile = false, IntVec3 centerSpawn = default(IntVec3))
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(id.category, map);
            if (points != 0)
            {
                parms.points = points;
            }
            if(hostile)
            {
                parms.faction = Find.FactionManager.RandomEnemyFaction();
            }
            id.Worker.TryExecute(parms);
        }

        public static void TryApplyHediff(HediffDef hediff, Faction faction, Map map, Pawn caster, float sev, int count = 0, bool checkResistance = false, bool friendly = false, bool enemy = false, bool neutral = false, bool nullFaction = false, ThingDef mote = null)
        {
            List<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            allPawns.Shuffle();
            if (allPawns != null && allPawns.Count > 0)
            {
                int maxCount = allPawns.Count;
                if (count != 0)
                {
                    maxCount = Mathf.Clamp(count, 0, allPawns.Count);
                }
                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn p = allPawns[i];
                    if (p.Faction != null)
                    {
                        if (friendly && p.Faction == faction)
                        {
                            if (checkResistance && Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, p, true)))
                            {
                                HealthUtility.AdjustSeverity(p, hediff, sev);
                            }
                            else
                            {
                                HealthUtility.AdjustSeverity(p, hediff, sev);
                            }
                            maxCount--;
                        }
                        if (enemy && p.Faction.HostileTo(faction))
                        {
                            if (checkResistance && Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, p, true)))
                            {
                                HealthUtility.AdjustSeverity(p, hediff, sev);
                            }
                            else
                            {
                                HealthUtility.AdjustSeverity(p, hediff, sev);
                            }
                            maxCount--;
                        }
                        if (neutral && !p.Faction.HostileTo(faction))
                        {
                            if (checkResistance && Rand.Chance(TM_Calc.GetSpellSuccessChance(caster, p, true)))
                            {
                                HealthUtility.AdjustSeverity(p, hediff, sev);
                            }
                            else
                            {
                                HealthUtility.AdjustSeverity(p, hediff, sev);
                            }
                            maxCount--;
                        }
                    }
                    else if (nullFaction && p.Faction == null)
                    {
                        HealthUtility.AdjustSeverity(p, hediff, sev);
                        maxCount--;
                    }
                    
                    if(mote != null)
                    {
                        TM_MoteMaker.ThrowGenericMote(mote, p.DrawPos, p.Map, Rand.Range(.75f, 1.25f), .25f, .05f, .25f, Rand.Range(-100, 100), Rand.Range(0, 1), Rand.Range(0, 360), Rand.Range(0, 360));
                    }
                    if(maxCount <= 0)
                    {
                        break;
                    }
                }
            }
        }

        public static void TrySpawnThing(Pawn caster, ThingDef thingDef, PawnKindDef kindDef, IntVec3 position, int duration, bool temporary, int count = 1, int stackCount = 1, bool hostile = false)
        {
            AbilityUser.SpawnThings spawnables = new SpawnThings();
            spawnables.def = thingDef;
            spawnables.kindDef = kindDef;
            spawnables.spawnCount = stackCount;
            spawnables.temporary = temporary;

            for (int i = 0; i < count; i++)
            {
                if (thingDef == TorannMagicDefOf.TM_SpiritTD)
                {
                    Pawn spiritPawn = TM_Action.GenerateSpiritPawn(position, caster.Faction);
                    GenSpawn.Spawn(spiritPawn, position, caster.Map);
                }
                else
                {
                    Thing thing = TM_Action.SingleSpawnLoop(caster, spawnables, position, caster.Map, duration, temporary, hostile, caster.Faction);
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> gizmoList = base.GetGizmos().ToList();
            Command command = BuildCopyCommandUtility.BuildCopyCommand(def, base.Stuff);
            if (command != null)
            {
                gizmoList.Add((Gizmo)command);
            }
            if (base.Faction == Faction.OfPlayer)
            {
                foreach (Command item in BuildRelatedCommandUtility.RelatedBuildCommands(def))
                {
                    gizmoList.Add((Gizmo)item);
                }
            }
            return gizmoList;
        }
    }
}
