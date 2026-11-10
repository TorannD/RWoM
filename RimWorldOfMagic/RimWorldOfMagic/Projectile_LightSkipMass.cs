using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;
using HarmonyLib;
using RimWorld.Planet;

namespace TorannMagic
{
    public class Projectile_LightSkipMass : ClassProgression.Projectile_SpiritBolt
    {
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_LightSkip;
        protected override string SkillStem => "TM_LightSkip";
        int age = -1;
        int duration = 30;
        int strikeDelay = 4;
        int strikeNum = 1;
        float radius = 5;
        bool initialized = false;
        float angle = 0;
        bool allPawnsLaunched = false;
        Pawn pawn;
        IntVec3 launcherPosition = default(IntVec3);
        List<Thing> pods = new List<Thing>();
        bool recoverLegacyPods;
        List<Pawn> pawnList = new List<Pawn>();
        List<IntVec3> unroofedCells = new List<IntVec3>();

        IntVec3 safePos = default(IntVec3);


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
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                recoverLegacyPods = Scribe.loader.curXmlParent["TM_pendingLightPods"] == null;
            if (!recoverLegacyPods) Scribe_Collections.Look(ref pods, "TM_pendingLightPods", LookMode.Reference);
            Scribe_Values.Look(ref launcherPosition, "TM_lightFormationOrigin", origin.ToIntVec3());
            Scribe_Values.Look(ref draftFlag, "TM_lightDrafted");
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

        protected override void Tick()
        {
            base.Tick();
            //this.age++;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {            
            base.Impact(hitThing);
            if (!Spawned) return;

            if (!this.initialized)
            {
                pawn = launcher as Pawn;
                if (!Verb_LightSkip.ValidLanding(pawn, Map, Position) ||
                    projected && (!ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(pawn, out var held) || held != SourcePawn))
                { age = duration; Destroy(); return; }
                launcherPosition = pawn.Position;
                draftFlag = pawn.Drafted;


                pawnList.Clear();
                pods = new List<Thing>();
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
                    LaunchNextPod();
                }
            }
        }

        private void LaunchNextPod()
        {
            if (recoverLegacyPods || pods == null)
            {
                recoverLegacyPods = false;
                // Older saves recorded the passengers but not their departure pods.
                // Only their actual, still-spawned light transporters belong to this flight.
                pods = new List<Thing>();
                var casterPod = pawn?.holdingOwner?.Owner as CompTransporter;
                if (casterPod?.parent?.Map == Map && casterPod.parent.def == TorannMagicDefOf.TM_LightPod)
                {
                    launcherPosition = casterPod.parent.Position;
                    draftFlag = pawn.Drafted;
                }
                else if (pawn != null)
                {
                    var departing = Map.listerThings.ThingsOfDef(TorannMagicDefOf.TM_LightPodLeaving)
                        .OfType<WorldTransport.TM_DropPodLeaving>().FirstOrDefault(p => p.Contents?.innerContainer.Contains(pawn) == true);
                    if (departing != null)
                    {
                        launcherPosition = departing.Position;
                        draftFlag = departing.draftFlag;
                    }
                }
                if (pawnList != null)
                    foreach (var passenger in pawnList)
                        if (passenger?.holdingOwner?.Owner is CompTransporter owner && owner.parent.Spawned &&
                            owner.parent.Map == Map && owner.parent.def == TorannMagicDefOf.TM_LightPod &&
                            owner.innerContainer.Contains(passenger) && !pods.Contains(owner.parent))
                        {
                            owner.groupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
                            pods.Add(owner.parent);
                        }
            }
            while (pods.Count > 0)
            {
                var pod = pods[0];
                var transporter = pod?.TryGetComp<CompTransporter>();
                if (transporter != null && pod.Spawned && pod.Map == Map && transporter.innerContainer.Count > 0)
                {
                    var landing = GetRelativePositionOffset(Position, launcherPosition, pod.Position);
                    if (!landing.IsValid) return;
                    if (Verb_LightSkip.DispatchPod(transporter, Map.Tile, landing, draftFlag)) pods.RemoveAt(0);
                    return;
                }
                pods.RemoveAt(0);
            }
            allPawnsLaunched = true;
        }

        public void CreatePodGroup()
        {
            if (pawnList != null && pawnList.Count > 0)
            {
                for (int i = 0; i < pawnList.Count; i++)
                {
                    var transporter = Verb_LightSkip.PreparePod(pawnList[i]);
                    if (transporter != null) pods.Add(transporter.parent);
                }
            }
            else
            {
                allPawnsLaunched = true;
            }
        }

        public void LaunchLightPod(Thing pod, CompTransporter compTransporter, int destinationTile, IntVec3 destinationCell)
            => Verb_LightSkip.DispatchPod(compTransporter, destinationTile, destinationCell, draftFlag);

        public IntVec3 GetRelativePositionOffset(IntVec3 targetCell, IntVec3 relativePosition, IntVec3 offsetPosition)
        {
            IntVec3 cell = targetCell + (offsetPosition - relativePosition);
            if (CanLand(cell)) return cell;
            var alternatives = GetUnroofedCellsAround(targetCell, radius);
            if (alternatives.TryRandomElement(out var nearby)) return nearby;
            return CanLand(offsetPosition) ? offsetPosition : IntVec3.Invalid;
        }

        private bool CanLand(IntVec3 cell) => cell.InBounds(Map) && cell.Walkable(Map) && !cell.Roofed(Map);

        public List<IntVec3> GetUnroofedCellsAround(IntVec3 center, float r)
        {
            List<IntVec3> cellList = new List<IntVec3>();
            cellList.Clear();
            IEnumerable<IntVec3> allCells = GenRadial.RadialCellsAround(center, r, true);
            foreach (IntVec3 item in allCells)
            {
                if (CanLand(item))
                {
                    cellList.Add(item);
                }
            }
            return cellList;
        }

    }    
}
