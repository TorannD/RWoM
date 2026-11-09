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
        private Pawn owner;
        internal Pawn Owner => owner;
        internal void Bind(Pawn source,int capacity)
        {
            if (owner != source) owner?.GetCompAbilityUserMagic()?.stoneskinPawns?.RemoveAll(p => p == Pawn);
            owner = source;
            maxSev = capacity;
            initializing = false;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref owner,"TM_stoneskinOwner");
            Scribe_Values.Look(ref maxSev, "maxSev", 4);
            Scribe_Values.Look(ref initializing, "TM_stoneskinInitializing", false, true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && !initializing)
                maxSev = Mathf.Max(maxSev,Mathf.RoundToInt(parent.Severity));
        }

        public override string CompLabelInBracketsExtra =>
            Mathf.RoundToInt(parent.Severity) + " / " + maxSev;

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

            if(Find.TickManager.TicksGame % 1800 == 0)
            {
                if(this.parent.Severity < this.maxSev)
                {
                    parent.Severity = Mathf.Min(maxSev,parent.Severity + 1f);
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
            owner?.GetCompAbilityUserMagic()?.stoneskinPawns?.RemoveAll(p => p == Pawn);
            if (Pawn?.Spawned == true)
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                info.pitchFactor = .7f;
                TorannMagicDefOf.EnergyShield_Broken.PlayOneShot(info);
                FleckMaker.ThrowLightningGlow(Pawn.DrawPos, Pawn.Map, 1.5f);
            }
            base.CompPostPostRemoved();
        }
    }
}
