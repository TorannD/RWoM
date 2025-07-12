using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_TaskMaster : HediffComp
    {

        private bool initializing = true;
        private int nextTickAction = 0;

        public int duration = 1;

        private bool removeNow = false;

        public override void CompExposeData()
        {
            Scribe_Values.Look<int>(ref this.duration, "duration", 1, false);
            base.CompExposeData();
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
            if(Find.TickManager.TicksGame >= this.nextTickAction)
            {
                this.duration--;                
                this.nextTickAction = Find.TickManager.TicksGame + Rand.Range(600, 700);
                if (this.duration <= 0)
                {
                    this.removeNow = true;
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }
    }
}
