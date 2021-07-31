using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Stoneskin : HediffComp
    {
        private bool initializing = true;
        public int maxSev = 4;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.maxSev, "maxSev", 4, false);
        }

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                this.maxSev = Mathf.RoundToInt(this.parent.Severity);
                //FleckMaker.ThrowHeatGlow(base.Pawn.DrawPos.ToIntVec3(), base.Pawn.Map, 2f);
            }            
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);            
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }

            if (Find.Selector.FirstSelectedObject == this.Pawn)
            {
                HediffStage hediffStage = this.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_StoneskinHD"), false).CurStage;
                hediffStage.label = this.parent.Severity.ToString("0") + " charges";               
            }
            
            if(Find.TickManager.TicksGame % 1800 == 0)
            {
                if(this.parent.Severity < this.maxSev)
                {
                    this.parent.Severity++;
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.parent.Severity < 1;
            }
        }

        public override void CompPostPostRemoved()
        {
            SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
            info.pitchFactor = .7f;
            SoundDefOf.EnergyShield_Broken.PlayOneShot(info);
            FleckMaker.ThrowLightningGlow(this.Pawn.DrawPos, this.Pawn.Map, 1.5f);
            base.CompPostPostRemoved();
        }
    }
}
