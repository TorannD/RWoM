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
    public class Building_TMSentinel : Building
    {
        private bool initialized = false;
        Pawn sustainerPawn = null;
        Pawn hostilePawn = null;
        TMPawnSummoned newPawn = new TMPawnSummoned();
        private int age = -1;
        private int pwrVal = 0;
        private int threatRange = 35;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Pawn>(ref this.sustainerPawn, "sustainerPawn", false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        }
                
        public override void Tick()
        {
            if (this.age > 10)
            {
                if (!initialized)
                {
                    List<Pawn> mapPawns = this.Map.mapPawns.AllPawnsSpawned;
                    for(int i = 0; i < mapPawns.Count(); i++)
                    {
                        if (!mapPawns[i].DestroyedOrNull() && mapPawns[i].Spawned && !mapPawns[i].Downed && mapPawns[i].RaceProps.Humanlike)
                        {
                            CompAbilityUserMagic comp = mapPawns[i].GetCompAbilityUserMagic();
                            if (comp.IsMagicUser && comp.summonedSentinels.Count > 0)
                            {
                                for (int j = 0; j < comp.summonedSentinels.Count(); j++)
                                {
                                    if(comp.summonedSentinels[j] == this)
                                    {
                                        this.sustainerPawn = comp.Pawn;
                                        pwrVal = comp.MagicData.MagicPowerSkill_Sentinel.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Sentinel_pwr").level;
                                        break;
                                    }                                        
                                }
                            }
                        }
                        if(this.sustainerPawn != null)
                        {
                            break;
                        }
                    }

                    for (int m = 0; m < 5; m++)
                    {
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, base.Position.ToVector3Shifted(), this.Map, Rand.Range(.5f, .8f), Rand.Range(.8f, 1.3f), .05f, Rand.Range(1f, 1.5f), Rand.Range(-20, 20), Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
                    }
                    initialized = true;
                }                

                if (Find.TickManager.TicksGame % 180 == 0)
                {
                    if (this.initialized)
                    {
                        if (this.sustainerPawn == null || this.sustainerPawn.Destroyed || this.sustainerPawn.Dead)
                        {
                            Messages.Message("TM_SentinelDeSpawn".Translate(
                                this.def.label
                            ), MessageTypeDefOf.NegativeEvent, false);
                            this.sustainerPawn = null;
                            this.Destroy(DestroyMode.Vanish);
                        }
                    }

                    if (this.sustainerPawn != null)
                    {
                        if (this.HitPoints < this.MaxHitPoints)
                        {
                            this.HitPoints += 1;
                            if (this.HitPoints > this.MaxHitPoints)
                            {
                                this.HitPoints = this.MaxHitPoints;
                            }
                        }
                        else
                        {
                            this.hostilePawn = SearchForTargets();

                            if (hostilePawn != null)
                            {
                                SpawnSentinel();
                                CompAbilityUserMagic comp = this.sustainerPawn.GetCompAbilityUserMagic();
                                comp.summonedSentinels.Remove(this);
                                comp.summonedSentinels.Add(this.newPawn as Thing);
                                this.Destroy(DestroyMode.Vanish);
                            }
                        }
                    }
                }
            }
            age++;
        }

        public Pawn SearchForTargets()
        {
            Pawn threat = null;

            List<Pawn> allPawns = this.Map.mapPawns.AllPawnsSpawned;
            for(int i = 0; i < allPawns.Count(); i++)
            {
                if (!allPawns[i].DestroyedOrNull())
                {
                    if (!allPawns[i].Dead && !allPawns[i].Downed && !allPawns[i].IsPrisonerInPrisonCell())
                    {
                        if ((allPawns[i].Position - this.Position).LengthHorizontal <= this.threatRange)
                        {
                            if ((allPawns[i].Faction != null || (allPawns[i].RaceProps.Animal && allPawns[i].InAggroMentalState)) && allPawns[i].Faction != this.sustainerPawn.Faction)
                            {
                                if (FactionUtility.HostileTo(this.sustainerPawn.Faction, allPawns[i].Faction) || (allPawns[i].RaceProps.Animal && allPawns[i].InAggroMentalState))
                                {
                                    if(!allPawns[i].IsPrisoner || (allPawns[i].IsPrisoner && allPawns[i].IsFighting()))
                                    { 
                                        threat = allPawns[i];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return threat;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            //end conditions

            base.Destroy(mode);
        }

        public void SpawnSentinel()
        {
            IntVec3 curCell = this.Position;
            SpawnThings sentinel = new SpawnThings();
            if(pwrVal == 2)
            {
                sentinel.def = ThingDef.Named("TM_Greater_SentinelR");
                sentinel.kindDef = PawnKindDef.Named("TM_Greater_Sentinel");
            }
            else if(pwrVal == 1)
            {
                sentinel.def = ThingDef.Named("TM_SentinelR");
                sentinel.kindDef = PawnKindDef.Named("TM_Sentinel");
            }
            else
            {
                sentinel.def = ThingDef.Named("TM_Lesser_SentinelR");
                sentinel.kindDef = PawnKindDef.Named("TM_Lesser_Sentinel");
            }
            sentinel.spawnCount = 1;
            SingleSpawnLoop(sentinel, curCell, this.Map);
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = this.sustainerPawn.Faction;
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
                        newPawn.Temporary = false;
                        //if (newPawn.Faction != Faction.OfPlayerSilentFail)
                        //{
                        //    newPawn.SetFaction(this.Caster.Faction, null);
                        //}
                        try
                        {
                            GenSpawn.Spawn(newPawn, position, map, Rot4.North, WipeMode.Vanish, false);
                            //GenPlace.TryPlaceThing(newPawn, position, map, ThingPlaceMode.Near, null, null);
                            CompSentinel compSentinel = newPawn.TryGetComp<CompSentinel>();
                            compSentinel.target = this.hostilePawn;
                            compSentinel.sentinelLoc = position;
                            compSentinel.rotation = this.Rotation;
                            compSentinel.sustainerPawn = this.sustainerPawn;
                        }
                        catch
                        {
                            Log.Message("TM_Exception".Translate(
                                "sentinel building",
                                this.def.defName
                                ));
                            this.Destroy(DestroyMode.Vanish);
                        }

                        if (newPawn.Faction != null && newPawn.Faction != Faction.OfPlayer)
                        {
                            try
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
                                    LordJob_DefendPoint lordJob = new LordJob_DefendPoint(newPawn.Position);
                                    lord = LordMaker.MakeNewLord(faction, lordJob, map, null);
                                }
                                lord.AddPawn(newPawn);
                            }
                            catch(NullReferenceException ex)
                            {
                                //
                            }
                        }
                    }
                }
                else
                {
                    Log.Message("Missing race");
                }
            }
        }
    }
}
