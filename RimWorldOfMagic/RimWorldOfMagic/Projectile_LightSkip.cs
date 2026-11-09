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
    class Projectile_LightSkip : ClassProgression.Projectile_SpiritBolt
    {
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_LightSkip;
        protected override string SkillStem => "TM_LightSkip";
        int age = -1;
        int duration = 45;
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

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {            
            ThingDef def = this.def;
            if (!this.initialized)
            {
                pawn = launcher as Pawn;
                if (!Verb_LightSkip.ValidLanding(pawn, Map, Position) ||
                    projected && (!ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(pawn, out var held) || held != SourcePawn))
                { age = duration; Destroy(); return; }
                this.draftFlag = this.pawn.drafter != null ? this.pawn.Drafted : false;
                this.initialized = true;
            }

            if (!launchedFlag)
            {
                Pawn pawnToSkip = this.pawn;
                ModOptions.Constants.SetPawnInFlight(true);
                if(pawnToSkip.carryTracker != null && pawnToSkip.carryTracker.CarriedThing != null)
                {
                    pawnToSkip.carryTracker.TryDropCarriedThing(pawnToSkip.Position, ThingPlaceMode.Near, out Thing _);
                }
                var podT = pawnToSkip.holdingOwner?.Owner as CompTransporter ?? Verb_LightSkip.PreparePod(pawnToSkip);
                if (podT == null) { age = duration; Destroy(); return; }

                GlobalTargetInfo gti = new GlobalTargetInfo(base.Position, base.Map, false);
                launchedFlag = Verb_LightSkip.DispatchPod(podT, gti.Tile, gti.Cell, draftFlag);
            }
            
            if (launchedFlag)
            {
                this.age++;
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public void LaunchLightPod(Thing pod, CompTransporter compTransporter, int destinationTile, IntVec3 destinationCell)
            => Verb_LightSkip.DispatchPod(compTransporter, destinationTile, destinationCell, draftFlag);

    }    
}
