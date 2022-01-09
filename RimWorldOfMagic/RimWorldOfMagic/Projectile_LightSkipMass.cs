using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;
using RimWorld.Planet;

namespace TorannMagic
{
    public class Projectile_LightSkipMass : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 30;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 4;
        int strikeNum = 1;
        float radius = 5;
        bool initialized = false;
        float angle = 0;
        bool allPawnsLaunched = false;
        Pawn pawn;
        IntVec3 launcherPosition = default(IntVec3);
        List<Thing> pods = new List<Thing>();
        List<Pawn> pawnList = new List<Pawn>();
        List<IntVec3> unroofedCells = new List<IntVec3>();
        List<CompTransporter> podTList = new List<CompTransporter>();
        IntVec3 safePos = default(IntVec3);
        private int gi = 0;

        bool launchedFlag = false;
        bool pivotFlag = false;
        bool landedFlag = false;
        bool draftFlag = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.launchedFlag, "launchedFlag", false, false);
            Scribe_Values.Look<bool>(ref this.landedFlag, "landedFlag", false, false);
            Scribe_Values.Look<bool>(ref this.pivotFlag, "pivotFlag", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.strikeNum, "strikeNum", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.safePos, "safePos", default(IntVec3), false);
            Scribe_Values.Look<bool>(ref this.allPawnsLaunched, "allPawnsLaunched", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Collections.Look<IntVec3>(ref this.unroofedCells, "unroofedCells", LookMode.Value);
            Scribe_Collections.Look<Pawn>(ref this.pawnList, "pawnList", LookMode.Reference);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                ModOptions.Constants.SetPawnInFlight(false);
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            //this.age++;
        }

        protected override void Impact(Thing hitThing)
        {            
            base.Impact(hitThing);
            ThingDef def = this.def;

            if (!this.initialized)
            {
                this.pawn = this.launcher as Pawn;
                launcherPosition = this.Launcher.Position;
                CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
                //pwrVal = TM_Calc.GetMagicSkillLevel(this.pawn, comp.MagicData.MagicPowerSkill_LightSkip, "TM_LightSkip", "_pwr", false);
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_LightSkip, false);
                this.arcaneDmg = comp.arcaneDmg;
                this.draftFlag = this.pawn.drafter != null ? this.pawn.Drafted : false;
                this.gi = 0;
                podTList.Clear();
                pawnList.Clear();
                pods.Clear();
                this.initialized = true;                
            }

            if (!launchedFlag)
            {
                ModOptions.Constants.SetPawnInFlight(true);
                List<Pawn> tmpList = TM_Calc.FindAllPawnsAround(this.Map, launcherPosition, 5f, this.pawn.Faction, true);
                if (tmpList != null)
                {
                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        if (!tmpList[i].Position.Roofed(this.Map))
                        {
                            if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                            {
                                if (ModCheck.GiddyUp.IsMount(tmpList[i]))
                                {
                                    continue;
                                }
                            }
                            if (tmpList[i].carryTracker != null && tmpList[i].carryTracker.CarriedThing != null)
                            {
                                tmpList[i].carryTracker.TryDropCarriedThing(tmpList[i].Position, ThingPlaceMode.Near, out Thing _);
                            }
                            pawnList.Add(tmpList[i]);
                        }
                    }
                    unroofedCells = GetUnroofedCellsAround(base.Position, this.radius);
                    CreatePodGroup();
                    this.allPawnsLaunched = false;
                    launchedFlag = true;
                }
                else
                {
                    this.allPawnsLaunched = true;
                    launchedFlag = true;
                }
            }
            
            if (launchedFlag)
            {
                if (allPawnsLaunched)
                {
                    this.age++;
                    this.Destroy(DestroyMode.Vanish);
                }
                else if(Find.TickManager.TicksGame % 2 == 0)
                {
                    if (podTList != null && podTList.Count > 0)
                    {
                        if (gi < podTList.Count)
                        {
                            IntVec3 newPosition = GetRelativePositionOffset(base.Position, launcherPosition, pods[gi].Position);
                            GlobalTargetInfo gti = new GlobalTargetInfo(newPosition, base.Map, false);
                            LaunchLightPod(pods[gi], podTList[gi], gti.Tile, gti.Cell);
                            gi++;                           
                        }
                        else
                        {
                            allPawnsLaunched = true;
                        }
                    }
                    else
                    {
                        allPawnsLaunched = true;
                    }
                }
            }
        }

        public void CreatePodGroup()
        {
            if (pawnList != null && pawnList.Count > 0)
            {
                for (int i = 0; i < pawnList.Count; i++)
                {
                    Pawn p = pawnList[i];
                    Pawn mount = null;
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        mount = ModCheck.GiddyUp.GetMount(this.pawn);
                        ModCheck.GiddyUp.ForceDismount(pawn);
                    }
                    Thing pod = ThingMaker.MakeThing(TorannMagicDefOf.TM_LightPod, null);
                    CompLaunchable podL = pod.TryGetComp<CompLaunchable>();
                    CompTransporter podT = podL.Transporter;
                    GenSpawn.Spawn(pod, p.Position, this.Map, WipeMode.Vanish);
                    podT.groupID = 12;
                    p.DeSpawn();
                    if (mount != null)
                    {
                        mount.DeSpawn();
                        podT.innerContainer.TryAddOrTransfer(mount);
                    }
                    podT.innerContainer.TryAddOrTransfer(p);
                    podTList.Add(podT);
                    pods.Add(pod);
                }
            }
            else
            {
                allPawnsLaunched = true;
            }
        }

        public void LaunchLightPod(Thing pod, CompTransporter compTransporter, int destinationTile, IntVec3 destinationCell)
        {
            Map map = this.Map;
            int groupID = compTransporter.groupID;
            ThingOwner directlyHeldThings = compTransporter.GetDirectlyHeldThings();
            ActiveDropPod activeDropPod = (ActiveDropPod)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            activeDropPod.Contents = new ActiveDropPodInfo();
            activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, canMergeWithExistingStacks: true, destroyLeftover: true);
            WorldTransport.TM_DropPodLeaving obj = (WorldTransport.TM_DropPodLeaving)SkyfallerMaker.MakeSkyfaller(TorannMagicDefOf.TM_LightPodLeaving, activeDropPod);
            obj.groupID = groupID;
            obj.destinationTile = destinationTile;
            obj.arrivalAction = null;
            obj.arrivalCell = destinationCell;
            obj.draftFlag = this.draftFlag;
            compTransporter.CleanUpLoadingVars(map);
            compTransporter.parent.Destroy();
            GenSpawn.Spawn(obj, compTransporter.parent.Position, map);
        }

        public IntVec3 GetRelativePositionOffset(IntVec3 targetCell, IntVec3 relativePosition, IntVec3 offsetPosition)
        {
            IntVec3 cell = targetCell + (offsetPosition - relativePosition);
            if (cell.Roofed(this.Map))
            {
                cell = unroofedCells.RandomElement();
            }
            return cell;
        }

        public List<IntVec3> GetUnroofedCellsAround(IntVec3 center, float r)
        {
            List<IntVec3> cellList = new List<IntVec3>();
            cellList.Clear();
            IEnumerable<IntVec3> allCells = GenRadial.RadialCellsAround(center, r, true);
            foreach (IntVec3 item in allCells)
            {
                if(!item.Roofed(this.Map))
                {
                    cellList.Add(item);
                }
            }
            return cellList;
        }

    }    
}