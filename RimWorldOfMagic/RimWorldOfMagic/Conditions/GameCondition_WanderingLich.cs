using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using AbilityUser;
using Verse.AI.Group;
using HarmonyLib;

namespace TorannMagic.Conditions
{
    class GameCondition_WanderingLich : GameCondition
    {
        public IntVec2 centerLocation;
        public IntVec2 edgeLocation;
        private int areaRadius = 4;
        bool initialized = false;
        bool disabled = false;
        public Thing thing;
        private int nextEventTick = 0;
        private int ticksBetweenEvents = 4000;

        public float geChance = .002f;
        public float leChance = .012f;

        public override void GameConditionTick()
        {
            base.GameConditionTick();            
            if(Find.TickManager.TicksGame % 60 == 0)
            {
                //Log.Message("wandering lich duration pct: " + (float)((float)this.TicksPassed / (float)this.Duration) + "  duration ticks: " + this.Duration);
                if(this.thing.DestroyedOrNull())
                {
                    this.End();
                }

                CompSkeletonLichController comp = thing.TryGetComp<CompSkeletonLichController>();
                if (this.nextEventTick <= Find.TickManager.TicksGame)
                {
                    if(comp != null && comp.tauntTarget == null)
                    {
                        this.nextEventTick = Find.TickManager.TicksGame + this.ticksBetweenEvents;
                        comp.raiseRadius = this.areaRadius;
                        comp.tauntTarget = TM_Calc.TryFindSafeCell(comp.ParentPawn, comp.ParentPawn.Position, 30, 1, 10);
                        if(comp.tauntTarget == null || comp.tauntTarget == default(IntVec3))
                        {
                            FindGoodCenterLocation();                            
                            comp.tauntTarget = this.centerLocation.ToIntVec3;
                        }
                        comp.geChance = this.geChance;
                        comp.leChance = this.leChance;
                        this.centerLocation = comp.tauntTarget.Cell.ToIntVec2;
                        ChangeDefendPoint(thing.Faction);
                    }
                }

                if (comp != null && comp.shouldAssault)
                {
                    comp.shouldAssault = false;
                    SendAssaultWave(comp.ParentPawn.Faction);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Thing>(ref this.thing, "thing", false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<IntVec2>(ref this.centerLocation, "centerLocation", default(IntVec2), false);
            Scribe_Values.Look<IntVec2>(ref this.edgeLocation, "edgeLocation", default(IntVec2), false);
            Scribe_Values.Look<bool>(ref this.disabled, "disabled", false, false);
            Scribe_Values.Look<int>(ref this.ticksBetweenEvents, "ticksBetweenEvents", 4000, false);
            Scribe_Values.Look<int>(ref this.nextEventTick, "nextEventTick", 0, false);
            Scribe_Values.Look<float>(ref this.leChance, "leChance", .012f, false);
            Scribe_Values.Look<float>(ref this.geChance, "geChance", .002f, false);
        }

        public override void Init()
        {
            bool tempAllow = false;
            Map map = this.SingleMap;
            MagicMapComponent mmc = map.GetComponent<MagicMapComponent>();
            if (mmc != null && mmc.allowAllIncidents)
            {
                tempAllow = true;
            }
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();            
            if (settingsRef.wanderingLichChallenge > 0 || tempAllow)
            {
                base.Init();
                this.disabled = false;
                this.FindGoodEdgeLocation();
                this.SpawnWanderingLich();
                this.SetEventParameters();
                if(settingsRef.wanderingLichChallenge >= 2)
                {
                    InitializeDeathSkies();
                }
                if(settingsRef.wanderingLichChallenge >= 3)
                {
                    InitializeSolarFlare();
                }

            }
            else
            {
                this.disabled = true;
                Log.Message("Wandering Lich spawning disabled.");
            }
        }

        private void InitializeSolarFlare()
        {
            GameConditionManager gameConditionManager = this.gameConditionManager;
            int duration = Mathf.RoundToInt(this.Duration);
            GameCondition cond = GameConditionMaker.MakeCondition(GameConditionDefOf.SolarFlare, duration);
            gameConditionManager.RegisterCondition(cond);
        }

        private void InitializeDeathSkies()
        {
            GameConditionManager gameConditionManager = this.gameConditionManager;
            int duration = Mathf.RoundToInt(this.Duration);
            GameCondition cond2 = GameConditionMaker.MakeCondition(TorannMagicDefOf.DarkClouds, duration);
            gameConditionManager.RegisterCondition(cond2);
        }

        private void SetEventParameters()
        {
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            float mult = Rand.Range(2f, 4f) + settingsRef.wanderingLichChallenge + Find.Storyteller.difficulty.threatScale;
            this.nextEventTick = Find.TickManager.TicksGame + 200;
            this.ticksBetweenEvents = Mathf.RoundToInt((float)this.Duration / mult);
        }

        public void ChangeDefendPoint(Faction faction)
        {
            Lord defendLord = null;
            List<Lord> allLords = this.SingleMap.lordManager.lords;
            for (int i = 0; i < allLords.Count; i++)
            {
                Lord lord = allLords[i];
                if (lord.faction != null && lord.faction == faction)
                {
                    if (lord.CurLordToil != null && lord.CurLordToil is LordToil_DefendPoint)
                    {
                        defendLord = lord;
                        LordToil_DefendPoint lordToil = defendLord.CurLordToil as LordToil_DefendPoint;
                        lordToil.SetDefendPoint(this.centerLocation.ToIntVec3);
                    }
                }
            }
            defendLord.CurLordToil.UpdateAllDuties();
        }

        public void SendAssaultWave(Faction faction, bool finalAssault = false)
        {
            Lord assaultLord = null;
            Lord defendLord = null;
            List<Lord> allLords = this.SingleMap.lordManager.lords;
            for(int i = 0; i < allLords.Count; i++)
            {
                Lord lord = allLords[i];
                if(lord.faction != null && lord.faction == faction)
                {
                    if(lord.CurLordToil != null && lord.CurLordToil is LordToil_DefendPoint)
                    {
                        defendLord = lord;
                        LordToil_DefendPoint lordToil = defendLord.CurLordToil as LordToil_DefendPoint;
                        lordToil.SetDefendPoint(this.centerLocation.ToIntVec3);
                    }
                    else if(lord.CurLordToil != null && lord.CurLordToil is LordToil_AssaultColony)
                    {
                        assaultLord = lord;
                    }
                }
            }
            if(assaultLord == null)
            {
                LordJob_AssaultColony lordJob = new LordJob_AssaultColony(faction, false, false, false, false, false);
                Traverse.Create(root: lordJob).Field(name: "canTimeoutOrFlee").SetValue(false);
                assaultLord = LordMaker.MakeNewLord(faction, lordJob, this.SingleMap, null);
            }
            if(defendLord == null)
            {
                LordJob_DefendPoint lordJob = new LordJob_DefendPoint(this.centerLocation.ToIntVec3);
                defendLord = LordMaker.MakeNewLord(faction, lordJob, this.SingleMap, null);
            }
            
            List<Pawn> allPawns = this.SingleMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (allPawns[i].Faction != null && allPawns[i].Faction == faction)
                {
                    Lord pawnLord = allPawns[i].GetLord();
                    if (allPawns[i].def == TorannMagicDefOf.TM_SkeletonR && pawnLord != null)
                    {
                        if (pawnLord != null)
                        {
                            if (pawnLord != assaultLord && (Rand.Chance(.4f) || finalAssault))
                            {
                                allPawns[i].GetLord()?.Notify_PawnLost(allPawns[i], PawnLostCondition.ForcedToJoinOtherLord);
                                assaultLord.AddPawn(allPawns[i]);
                            }
                        }
                        else
                        {
                            defendLord.AddPawn(allPawns[i]);
                        }
                    }
                    if (allPawns[i].def == TorannMagicDefOf.TM_GiantSkeletonR && pawnLord != null)
                    {
                        if (pawnLord != null)
                        {
                            if (pawnLord != assaultLord && (Rand.Chance(.25f) || finalAssault))
                            {
                                allPawns[i].GetLord()?.Notify_PawnLost(allPawns[i], PawnLostCondition.ForcedToJoinOtherLord);
                                assaultLord.AddPawn(allPawns[i]);
                            }
                        }
                        else
                        {
                            defendLord.AddPawn(allPawns[i]);
                        }
                    }
                    if (allPawns[i].def == TorannMagicDefOf.TM_SkeletonLichR)
                    {
                        if (finalAssault)
                        {
                            if (pawnLord != null && pawnLord != assaultLord)
                            {
                                allPawns[i].GetLord()?.Notify_PawnLost(allPawns[i], PawnLostCondition.ForcedToJoinOtherLord);
                                //pawnLord.ownedPawns.Remove(allPawns[i]);
                            }
                            assaultLord.AddPawn(allPawns[i]);
                        }
                    }
                }
            }
        }

        public void SpawnWanderingLich()
        {
            AbilityUser.SpawnThings spawnables = new SpawnThings();
            spawnables.def = TorannMagicDefOf.TM_SkeletonLichR;
            spawnables.kindDef = PawnKindDef.Named("TM_SkeletonLich");
            spawnables.temporary = false;            
            Faction faction = Find.FactionManager.FirstFactionOfDef(TorannMagicDefOf.TM_SkeletalFaction);
            if (faction != null)
            {
                if (!faction.HostileTo(Faction.OfPlayer))
                {
                    faction.TryAffectGoodwillWith(Faction.OfPlayerSilentFail, -200, false, false, null, null);
                }
            }
            else
            {
                faction = Find.FactionManager.RandomEnemyFaction(true, true, true, TechLevel.Undefined);
            }
            this.thing = TM_Action.SingleSpawnLoop(null, spawnables, edgeLocation.ToIntVec3, this.SingleMap, 0, false, false, faction);
            CalculateWealthModifier();
            SpawnSkeletonMinions(edgeLocation.ToIntVec3, areaRadius, faction);
        }

        public override void End()
        {
            CompSkeletonLichController comp = null;
            if(!thing.DestroyedOrNull())
            {
                comp = thing.TryGetComp<CompSkeletonLichController>();
                SendAssaultWave(comp.ParentPawn.Faction, true);
            }
            else
            {
                List<Pawn> allPawns = this.SingleMap.mapPawns.AllPawnsSpawned;
                for(int i = 0; i < allPawns.Count; i++)
                {
                    if(allPawns[i].def == TorannMagicDefOf.TM_SkeletonR || allPawns[i].def == TorannMagicDefOf.TM_GiantSkeletonR || allPawns[i].def == TorannMagicDefOf.TM_SkeletonLichR)
                    {
                        if(allPawns[i].Faction != Faction.OfPlayer)
                        {
                            allPawns[i].Kill(null, null);
                            i--;
                        }
                    }
                }
            }
            List<GameCondition> gcs = new List<GameCondition>();
            gcs = this.SingleMap.GameConditionManager.ActiveConditions;
            for(int i = 0; i < gcs.Count; i++)
            {
                if(gcs[i].def == TorannMagicDefOf.DarkClouds)
                {
                    gcs[i].End();
                }
                else if(gcs[i].def == GameConditionDefOf.SolarFlare)
                {
                    gcs[i].End();
                }
            }

            MagicMapComponent mmc = this.SingleMap.GetComponent<MagicMapComponent>();
            if (mmc != null && mmc.allowAllIncidents)
            {
                mmc.allowAllIncidents = false; ;
            }
            base.End();
        }

        private void FindGoodEdgeLocation()
        {
            bool centerLocFound = false;
            if (this.SingleMap.Size.x <= 32 || this.SingleMap.Size.z <= 32)
            {
                throw new Exception("Map too small for wandering lich");
            }
            for (int i = 0; i < 20; i++)
            {
                int xVar = 0;
                int zVar = 0;
                if (Rand.Chance(.5f)) 
                {
                    xVar = Rand.Range(8, base.SingleMap.Size.x - 8);
                    zVar = Rand.Chance(.5f) ? Rand.Range(8, 16) : Rand.Range(base.SingleMap.Size.z - 16, base.SingleMap.Size.z - 8);
                }
                else
                {
                    xVar = Rand.Chance(.5f) ? Rand.Range(8, 16) : Rand.Range(base.SingleMap.Size.x - 16, base.SingleMap.Size.x - 8);
                    zVar = Rand.Range(8, base.SingleMap.Size.z - 8);
                }
                this.edgeLocation = new IntVec2(xVar, zVar);
                if (this.IsGoodCenterLocation(this.edgeLocation))
                {
                    this.centerLocation = this.edgeLocation;
                    centerLocFound = true;
                    break;
                }
            }
            if(!centerLocFound)
            {
                FindGoodCenterLocation();
            }
        }

        private void FindGoodCenterLocation()
        {
            if (this.SingleMap.Size.x <= 16 || this.SingleMap.Size.z <= 16)
            {
                throw new Exception("Map too small for wandering lich");
            }
            for (int i = 0; i < 10; i++)
            {
                this.centerLocation = new IntVec2(Rand.Range(8, base.SingleMap.Size.x - 8), Rand.Range(8, base.SingleMap.Size.z - 8));
                if (this.IsGoodCenterLocation(this.centerLocation))
                {
                    break;
                }
            }
        }

        private bool IsGoodLocationForSpawn(IntVec3 loc)
        {
            return loc.InBoundsWithNullCheck(base.SingleMap) && !loc.Roofed(base.SingleMap) && loc.Standable(base.SingleMap) && loc.IsValid && !loc.Fogged(base.SingleMap) && loc.Walkable(base.SingleMap);
        }

        private bool IsGoodCenterLocation(IntVec2 loc)
        {
            int num = 0;
            int num2 = (int)(3.14159274f * (float)this.areaRadius * (float)this.areaRadius / 2f);
            foreach (IntVec3 current in this.GetPotentiallyAffectedCells(loc))
            {
                if (this.IsGoodLocationForSpawn(current))
                {
                    num++;
                }
                if (num >= num2)
                {
                    break;
                }
            }
            
            return num >= num2 && (IsGoodLocationForSpawn(loc.ToIntVec3));
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

        public void CalculateWealthModifier()
        {
            float wealthMultiplier = .7f;
            float wealth = this.SingleMap.PlayerWealthForStoryteller;
            if (wealth > 20000)
            {
                wealthMultiplier = .8f;
            }
            if (wealth > 50000)
            {
                wealthMultiplier = 1f;
            }
            if (wealth > 100000)
            {
                wealthMultiplier = 1.25f;
            }
            if (wealth > 250000)
            {
                wealthMultiplier = 1.5f;
            }
            if (wealth > 500000)
            {
                wealthMultiplier = 2.5f;
            }
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            geChance = 0.02f * wealthMultiplier * Mathf.Max(settingsRef.wanderingLichChallenge, 1f);
            leChance = 0.14f * Mathf.Max(settingsRef.wanderingLichChallenge, 1f) * wealthMultiplier;
        }

        public void SpawnSkeletonMinions(IntVec3 center, int radius, Faction faction)
        {
            IntVec3 curCell;
            Map map = this.SingleMap;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(center, radius, true);
            for (int j = 0; j < targets.Count(); j++)
            {
                curCell = targets.ToArray<IntVec3>()[j];
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid && curCell.Walkable(map))
                {
                    SpawnThings skeleton = new SpawnThings();
                    if (Rand.Chance(geChance))
                    {
                        skeleton.def = TorannMagicDefOf.TM_GiantSkeletonR;
                        skeleton.kindDef = PawnKindDef.Named("TM_GiantSkeleton");
                    }
                    else if (Rand.Chance(leChance))
                    {
                        skeleton.def = TorannMagicDefOf.TM_SkeletonR;
                        skeleton.kindDef = PawnKindDef.Named("TM_Skeleton");
                    }                    
                    else
                    {
                        skeleton = null;
                    }

                    if (skeleton != null)
                    {
                        TM_Action.SingleSpawnLoop(null, skeleton, curCell, map, 0, false, false, faction);
                    }
                }
            }
        }
    }
}
