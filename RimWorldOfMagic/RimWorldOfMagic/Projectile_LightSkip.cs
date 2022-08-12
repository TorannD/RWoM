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
    class Projectile_LightSkip : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 45;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 4;
        int strikeNum = 1;
        float radius = 5;
        bool initialized = false;
        float angle = 0;
        List<IntVec3> cellList;
        Pawn pawn;
        IEnumerable<IntVec3> targets;
        Skyfaller skyfaller2;
        Skyfaller skyfaller;
        Map map;
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
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Collections.Look<IntVec3>(ref this.cellList, "cellList", LookMode.Value);
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

        protected override void Impact(Thing hitThing)
        {            
            ThingDef def = this.def;
            if (!this.initialized)
            {
                this.pawn = this.launcher as Pawn;
                this.map = this.pawn.Map;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightSkip.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_LightSkip_pwr");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                this.arcaneDmg = comp.arcaneDmg;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 1;
                    verVal = 1;
                }
                this.draftFlag = this.pawn.drafter != null ? this.pawn.Drafted : false;
                this.initialized = true;
            }

            if (!launchedFlag)
            {
                Pawn pawnToSkip = this.pawn;
                Pawn mount = null;
                ModOptions.Constants.SetPawnInFlight(true);
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    mount = ModCheck.GiddyUp.GetMount(this.pawn);
                    ModCheck.GiddyUp.ForceDismount(pawn);                    
                }
                if(pawnToSkip.carryTracker != null && pawnToSkip.carryTracker.CarriedThing != null)
                {
                    pawnToSkip.carryTracker.TryDropCarriedThing(pawnToSkip.Position, ThingPlaceMode.Near, out Thing _);
                }
                Thing pod = ThingMaker.MakeThing(TorannMagicDefOf.TM_LightPod, null);
                CompLaunchable podL = pod.TryGetComp<CompLaunchable>();
                CompTransporter podT = podL.Transporter;
                GenSpawn.Spawn(pod, pawnToSkip.Position, pawnToSkip.Map, WipeMode.Vanish);
                podT.groupID = 11;
                pawnToSkip.DeSpawn();
                if(mount != null)
                {
                    mount.DeSpawn();
                    podT.innerContainer.TryAddOrTransfer(mount);
                }
                podT.innerContainer.TryAddOrTransfer(pawnToSkip);                
                GlobalTargetInfo gti = new GlobalTargetInfo(base.Position, base.Map, false);
                LaunchLightPod(pod, podT, gti.Tile, gti.Cell);
                launchedFlag = true;
            }
            
            if (launchedFlag)
            {
                this.age++;
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public void LaunchLightPod(Thing pod, CompTransporter compTransporter, int destinationTile, IntVec3 destinationCell)
        {
            Map map = pod.Map;
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

    }    
}