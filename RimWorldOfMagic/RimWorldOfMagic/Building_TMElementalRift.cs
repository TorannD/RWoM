using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Diagnostics;
using UnityEngine;
using RimWorld;
using AbilityUser;



namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMElementalRift : Building
    {

        private float arcaneEnergyCur = 0;
        private float arcaneEnergyMax = 1;

        private static readonly Material portalMat_1 = MaterialPool.MatFrom("Motes/rift_swirl1", false);
        private static readonly Material portalMat_2 = MaterialPool.MatFrom("Motes/rift_swirl2", false);
        private static readonly Material portalMat_3 = MaterialPool.MatFrom("Motes/rift_swirl3", false);

        private int ticksTillNextAssault = 0;
        private float eventFrequencyMultiplier = 1;
        private float difficultyMultiplier = 1;
        private float STDMultiplier = 0;
        private int ticksTillNextEvent = 0;
        private int eventTimer = 0;
        private int assaultTimer = 0;
        private int matRng = 0;
        private float matMagnitude = 0;
        private int rnd = 0;
        private int areaRadius = 1;
        private bool notifier = false;
        IntVec2 centerLocation;

        private bool initialized = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.arcaneEnergyCur, "arcaneEnergyCur", 0f, false);
            Scribe_Values.Look<float>(ref this.arcaneEnergyMax, "arcaneEnergyMax", 0f, false);
            Scribe_Values.Look<int>(ref this.rnd, "rnd", 0, false);
            Scribe_Values.Look<int>(ref this.ticksTillNextAssault, "ticksTillNextAssault", 0, false);
            Scribe_Values.Look<int>(ref this.ticksTillNextEvent, "ticksTillNextEvent", 0, false);
            Scribe_Values.Look<int>(ref this.assaultTimer, "assaultTimer", 0, false);
            Scribe_Values.Look<int>(ref this.eventTimer, "eventTimer", 0, false);
            Scribe_Values.Look<int>(ref this.areaRadius, "areaRadius", 1, false);
            Scribe_Values.Look<float>(ref this.eventFrequencyMultiplier, "eventFrequencyMultiplier", 1f, false);
            Scribe_Values.Look<bool>(ref this.notifier, "notifier", false, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
        }
        
        public float ArcaneEnergyCur
        {
            get
            {
                return arcaneEnergyCur;
            }
            set
            {
                arcaneEnergyCur = value;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        }
                
        public override void Tick()
        {
            if(!initialized)
            {
                DetermineElementalType();
                BeginAssaultCondition();
                SpawnCycle();
                ModOptions.SettingsRef settings = new ModOptions.SettingsRef();
                if (Find.Storyteller.difficulty.threatScale != 0)
                {
                    this.STDMultiplier = (float)(Find.Storyteller.difficulty.threatScale / 20f);
                }
                if(settings.riftChallenge < 2f)
                {
                    this.difficultyMultiplier = 1f;
                }
                else if(settings.riftChallenge < 3f)
                {
                    this.difficultyMultiplier = .85f;
                }
                else
                {
                    this.difficultyMultiplier = .75f;
                }
                this.difficultyMultiplier -= this.STDMultiplier;
                this.ticksTillNextAssault = (int)(Rand.Range(2600, 4000) * this.difficultyMultiplier);
                this.ticksTillNextEvent = (int)(Rand.Range(160, 300) * this.eventFrequencyMultiplier);
                initialized = true;
            }
            if(Find.TickManager.TicksGame % 8 == 0)
            {
                this.assaultTimer += 8;
                this.eventTimer += 8;
                this.matRng = Rand.RangeInclusive(0, 2);
                this.matMagnitude = 4 * this.arcaneEnergyMax;
                Vector3 rndVec = this.DrawPos;
                if (this.rnd < 2) //earth
                {
                    rndVec.x += Rand.Range(-5, 5);
                    rndVec.z += Rand.Range(-5, 5);
                    FleckMaker.ThrowSmoke(rndVec, this.Map, Rand.Range(.6f, 1.2f));
                }
                else if (this.rnd < 4) //fire
                {
                    rndVec.x += Rand.Range(-1.2f, 1.2f);
                    rndVec.z += Rand.Range(-1.2f, 1.2f);
                    TM_MoteMaker.ThrowFlames(rndVec, this.Map, Rand.Range(.6f, 1f));
                }
                else if (this.rnd < 6) //water
                {
                    rndVec.x += Rand.Range(-2f, 2f);
                    rndVec.z += Rand.Range(-2f, 2f);
                    TM_MoteMaker.ThrowManaPuff(rndVec, this.Map, Rand.Range(.6f, 1f));
                }
                else //air
                {
                    rndVec.x += Rand.Range(-2f, 2f);
                    rndVec.z += Rand.Range(-2f, 2f);
                    FleckMaker.ThrowLightningGlow(rndVec, this.Map, Rand.Range(.6f, .8f));
                }

            }
            if(this.eventTimer > this.ticksTillNextEvent)
            {
                DoMapEvent();
                this.eventTimer = 0;
                this.ticksTillNextEvent = (int)(Rand.Range(160, 300) * this.eventFrequencyMultiplier);
            }
            if(this.notifier == false && this.assaultTimer > (.9f * this.ticksTillNextAssault))
            {
                Messages.Message("TM_AssaultPending".Translate(), MessageTypeDefOf.ThreatSmall);
                this.notifier = true;
            }
            if (this.assaultTimer > this.ticksTillNextAssault)
            {
                SpawnCycle();
                this.assaultTimer = 0;
                this.ticksTillNextAssault = Mathf.RoundToInt(Rand.Range(2000, 3500) * this.difficultyMultiplier);
                this.notifier = false;
            }
        }

        public void DoMapEvent()
        {
            if (this.rnd < 2) //earth
            {
                //berserk random animal
                List<Pawn> animalList = this.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < animalList.Count; i++)
                {
                    int j = Rand.Range(0, animalList.Count);
                    if (animalList[j].RaceProps.Animal && !animalList[j].IsColonist && !animalList[j].def.defName.Contains("Elemental") && animalList[j].Faction == null)
                    {
                        animalList[j].mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, null);
                        i = animalList.Count;
                    }
                }                
            }
            else if(this.rnd < 4) //fire
            {
                FindGoodCenterLocation();
                if (Rand.Chance(.6f))
                {
                    SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Firestorm_Small, this.centerLocation.ToIntVec3, this.Map);
                }
                else
                {
                    SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Firestorm_Tiny, this.centerLocation.ToIntVec3, this.Map);
                }
            }
            else if(this.rnd < 6) //water
            {
                FindGoodCenterLocation();
                if (Rand.Chance(.6f))
                {
                    SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Small, this.centerLocation.ToIntVec3, this.Map);
                }
                else
                {
                    SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Blizzard_Large, this.centerLocation.ToIntVec3, this.Map);
                }
            }
            else //air
            {                
                FindGoodCenterLocation();
                Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(this.Map, this.centerLocation.ToIntVec3));
                GenExplosion.DoExplosion(this.centerLocation.ToIntVec3, this.Map, this.areaRadius, DamageDefOf.Bomb, null, Rand.Range(6, 16), 0, SoundDefOf.Thunder_OffMap, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);

            }
        }

        private void FindGoodCenterLocation()
        {
            if (base.Map.Size.x <= 16 || base.Map.Size.z <= 16)
            {
                throw new Exception("Map too small for elemental assault");
            }
            for (int i = 0; i < 10; i++)
            {
                this.centerLocation = new IntVec2(Rand.Range(8, base.Map.Size.x - 8), Rand.Range(8, base.Map.Size.z - 8));
                if (this.IsGoodCenterLocation(this.centerLocation))
                {
                    break;
                }
            }
        }

        private bool IsGoodLocationForStrike(IntVec3 loc)
        {
            bool flag1 = loc.InBoundsWithNullCheck(base.Map);
            bool flag2 = loc.IsValid;
            bool flag3 = !loc.Fogged(base.Map);
            bool flag4 = loc.DistanceToEdge(base.Map) > 2;            
            if(flag1 && flag2 && flag3 && flag4)
            {
                if(loc.Roofed(base.Map))
                {
                    return false;                    
                }
                return true;
            }
            return false;
        }

        private bool IsGoodCenterLocation(IntVec2 loc)
        {
            int num = 0;
            int num2 = (int)(3.14159274f * (float)this.areaRadius * (float)this.areaRadius / 2f);
            foreach (IntVec3 current in this.GetPotentiallyAffectedCells(loc))
            {
                if (this.IsGoodLocationForStrike(current))
                {
                    num++;
                }
                if (num >= num2)
                {
                    break;
                }                
            }

            return num >= num2 && (IsGoodLocationForStrike(loc.ToIntVec3));
        }

        [DebuggerHidden]
        private IEnumerable<IntVec3> GetPotentiallyAffectedCells(IntVec2 center)
        {
            for (int x = center.x - this.areaRadius; x <= center.x + this.areaRadius; x++)
            {
                for (int z = center.z - this.areaRadius; z <= center.z + this.areaRadius; z++)
                {
                    if ((center.x - x) * (center.x - x) + (center.z - z) * (center.z - z) <= this.areaRadius * this.areaRadius)
                    {
                        yield return new IntVec3(x, 0, z);
                    }
                }
            }
        }

        public void BeginAssaultCondition()
        {
            List<GameCondition> currentGameConditions = this.Map.gameConditionManager.ActiveConditions;
            WeatherDef weatherDef = new WeatherDef();
            if (this.rnd < 2) //earth
            {
                if(!this.Map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
                {
                    this.Map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(GameConditionDefOf.ToxicFallout, 500000));
                }
                this.eventFrequencyMultiplier = 4;
            }
            else if(this.rnd < 4) //fire
            {
                for(int i = 0; i < currentGameConditions.Count; i++)
                {
                    if (currentGameConditions[i].def == GameConditionDefOf.ColdSnap)
                    {
                        currentGameConditions[i].Duration = 0;
                        currentGameConditions[i].End();
                    }
                }
                if (!this.Map.GameConditionManager.ConditionIsActive(GameConditionDefOf.HeatWave))
                {
                    this.Map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(GameConditionDefOf.HeatWave, 500000));
                }
                this.eventFrequencyMultiplier = .5f;
                this.areaRadius = 2;
                
            }
            else if(this.rnd < 6) //water
            {
                for (int i = 0; i < currentGameConditions.Count; i++)
                {
                    if (currentGameConditions[i].def == GameConditionDefOf.HeatWave)
                    {
                        currentGameConditions[i].Duration = 0;
                        currentGameConditions[i].End();
                    }
                }
                if (!this.Map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ColdSnap))
                {
                    this.Map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(GameConditionDefOf.ColdSnap, 500000));
                }
                weatherDef = WeatherDef.Named("SnowHard");
                this.Map.weatherManager.TransitionTo(weatherDef);
                this.eventFrequencyMultiplier = .5f;
                this.areaRadius = 3;
            }
            else //air
            {
                weatherDef = WeatherDef.Named("RainyThunderstorm");
                this.Map.weatherManager.TransitionTo(weatherDef);
                this.eventFrequencyMultiplier = .4f;
                this.areaRadius = 2;
            }

        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            //end conditions
            List<GameCondition> currentGameConditions = this.Map.gameConditionManager.ActiveConditions;
            for(int i =0; i < currentGameConditions.Count; i++)
            {
                if (currentGameConditions[i].def == GameConditionDefOf.ColdSnap || currentGameConditions[i].def == GameConditionDefOf.HeatWave || currentGameConditions[i].def == GameConditionDefOf.ToxicFallout)
                {
                    currentGameConditions[i].End();
                }
            }
            base.Destroy(mode);
        }

        public void DetermineElementalType()
        {
            System.Random random = new System.Random();
            random = new System.Random();
            this.rnd = GenMath.RoundRandom(random.Next(0, 8));
        }

        public void SpawnCycle()
        {
            float wealthMultiplier = .7f;
            float wealth = this.Map.PlayerWealthForStoryteller;
            if(wealth > 20000)
            {
                wealthMultiplier = .8f;
            }
            if(wealth > 50000)
            {
                wealthMultiplier = 1f;
            }
            if(wealth > 100000)
            {
                wealthMultiplier = 1.25f;
            }
            if(wealth > 250000)
            {
                wealthMultiplier = 1.5f;
            }
            if(wealth > 500000)
            {
                wealthMultiplier = 2f;
            }
            if (wealth > 1000000)
            {
                wealthMultiplier = 2.5f;
            }
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            float geChance = 0.007f * wealthMultiplier;
            float riftChallenge = Mathf.Min(settingsRef.riftChallenge, 1f);
            float difficultyMod = 1f;
            if(riftChallenge >=3f)
            {
                difficultyMod = 1.1f;
            }
            else if(riftChallenge >= 2f)
            {
                difficultyMod = .8f;
            }
            else
            {
                difficultyMod = .65f;
            }

            if (settingsRef.riftChallenge > 1 )
            {
                geChance *= (difficultyMod * riftChallenge);
            }  
            else
            {
                geChance = 0;
            }
            float eChance = 0.035f * riftChallenge * (difficultyMod*wealthMultiplier);
            float leChance = 0.12f * riftChallenge * (difficultyMod*wealthMultiplier);            

            IntVec3 curCell;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(this.Position, 3, true);
            for (int j = 0; j < targets.Count(); j++)
            {
                curCell = targets.ToArray<IntVec3>()[j];
                if (curCell.InBoundsWithNullCheck(this.Map) && curCell.IsValid && curCell.Walkable(this.Map))
                {
                    SpawnThings rogueElemental = new SpawnThings();
                    if (rnd < 2)
                    {
                        if (Rand.Chance(geChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1f);
                            FleckMaker.ThrowMicroSparks(curCell.ToVector3(), this.Map);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            rogueElemental.def = TorannMagicDefOf.TM_GreaterEarth_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_GreaterEarth_Elemental");
                        }
                        else if (Rand.Chance(eChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1f);
                            FleckMaker.ThrowMicroSparks(curCell.ToVector3(), this.Map);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            rogueElemental.def = TorannMagicDefOf.TM_Earth_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_Earth_Elemental");
                        }
                        else if (Rand.Chance(leChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1f);
                            FleckMaker.ThrowMicroSparks(curCell.ToVector3(), this.Map);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            rogueElemental.def = TorannMagicDefOf.TM_LesserEarth_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_LesserEarth_Elemental");
                        }
                        else
                        {
                            rogueElemental = null;
                        }
                    }
                    else if (rnd >= 2 && rnd < 4)
                    {

                        if (Rand.Chance(geChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1);
                            FleckMaker.ThrowMicroSparks(curCell.ToVector3(), this.Map);
                            FleckMaker.ThrowFireGlow(curCell.ToVector3Shifted(), this.Map, 1);
                            FleckMaker.ThrowHeatGlow(curCell, this.Map, 1);
                            rogueElemental.def = TorannMagicDefOf.TM_GreaterFire_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_GreaterFire_Elemental");
                        }
                        else if (Rand.Chance(eChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1);
                            FleckMaker.ThrowMicroSparks(curCell.ToVector3(), this.Map);
                            FleckMaker.ThrowFireGlow(curCell.ToVector3Shifted(), this.Map, 1);
                            FleckMaker.ThrowHeatGlow(curCell, this.Map, 1);
                            rogueElemental.def = TorannMagicDefOf.TM_Fire_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_Fire_Elemental");
                        }
                        else if (Rand.Chance(leChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1);
                            FleckMaker.ThrowMicroSparks(curCell.ToVector3(), this.Map);
                            FleckMaker.ThrowFireGlow(curCell.ToVector3Shifted(), this.Map, 1);
                            FleckMaker.ThrowHeatGlow(curCell, this.Map, 1);
                            rogueElemental.def = TorannMagicDefOf.TM_LesserFire_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_LesserFire_Elemental");
                        }
                        else
                        {
                            rogueElemental = null;
                        }

                    }
                    else if (rnd >= 4 && rnd < 6)
                    {

                        if (Rand.Chance(geChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            rogueElemental.def = TorannMagicDefOf.TM_GreaterWater_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_GreaterWater_Elemental");
                        }
                        else if (Rand.Chance(eChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            rogueElemental.def = TorannMagicDefOf.TM_Water_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_Water_Elemental");
                        }
                        else if (Rand.Chance(leChance))
                        {
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.blue);
                            rogueElemental.def = TorannMagicDefOf.TM_LesserWater_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_LesserWater_Elemental");
                        }
                        else
                        {
                            rogueElemental = null;
                        }

                    }
                    else
                    {

                        if (Rand.Chance(geChance))
                        {
                            rogueElemental.def = TorannMagicDefOf.TM_GreaterWind_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_GreaterWind_Elemental");
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1 + 1 * 2);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.white);
                        }
                        else if (Rand.Chance(eChance))
                        {
                            rogueElemental.def = TorannMagicDefOf.TM_Wind_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_Wind_Elemental");
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1 + 1 * 2);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.white);
                        }
                        else if (Rand.Chance(leChance))
                        {
                            rogueElemental.def = TorannMagicDefOf.TM_LesserWind_ElementalR;
                            rogueElemental.kindDef = PawnKindDef.Named("TM_LesserWind_Elemental");
                            FleckMaker.ThrowSmoke(curCell.ToVector3(), this.Map, 1 + 1 * 2);
                            SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                            FleckMaker.ThrowTornadoDustPuff(curCell.ToVector3(), this.Map, 1, Color.white);
                        }
                        else
                        {
                            rogueElemental = null;
                        }

                    }
                    if (rogueElemental != null)
                    {
                        SingleSpawnLoop(rogueElemental, curCell, this.Map);
                    }
                }
            }
        }


        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("TM_ElementalFaction"));
                TMPawnSummoned newPawn = new TMPawnSummoned();
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    bool flag3 = spawnables.kindDef == null;
                    if (flag3)
                    {
                        Log.Error("Missing kinddef");
                    }
                    else
                    {
                        newPawn = (TMPawnSummoned)PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
                        newPawn.validSummoning = true;
                        //newPawn.Spawner = this.Caster;
                        newPawn.Temporary = false;
                        if (newPawn.Faction == null || !newPawn.Faction.HostileTo(Faction.OfPlayer))
                        {
                            Log.Message("elemental faction was null or not hostile - fixing");
                            newPawn.SetFaction(faction, null);
                            faction.TryAffectGoodwillWith(Faction.OfPlayer, -200, false, false, null, null);
                        }
                        GenSpawn.Spawn(newPawn, position, this.Map);
                        if (newPawn.Faction != null && newPawn.Faction != Faction.OfPlayer)
                        {
                            Lord lord = null;
                            if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != newPawn))
                            {
                                Predicate<Thing> validator = (Thing p) => p != newPawn && ((Pawn)p).GetLord() != null;
                                Pawn p2 = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator, null);
                                lord = p2.GetLord();
                            }
                            bool flag4 = lord == null;
                            if (flag4)
                            {
                                LordJob_AssaultColony lordJob = new LordJob_AssaultColony(newPawn.Faction, false, false, false, true, false);
                                lord = LordMaker.MakeNewLord(faction, lordJob, this.Map, null);
                            }
                            lord.AddPawn(newPawn);                           
                        }                      
                    }
                }
                else
                {
                    Log.Message("Missing race");
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            Vector3 vector = base.DrawPos;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(matMagnitude, matMagnitude, matMagnitude);
            Matrix4x4 matrix = default(Matrix4x4);
            float angle = 0f;
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            if (matRng == 0)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMElementalRift.portalMat_1, 0);
            }
            else if (matRng == 1)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMElementalRift.portalMat_2, 0);
            }
            else 
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMElementalRift.portalMat_3, 0);
            }            
        }
    }
}
