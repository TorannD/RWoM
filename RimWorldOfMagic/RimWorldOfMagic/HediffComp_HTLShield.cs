using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_HTLShield : HediffComp
    {

        private bool initialized = false;
        private int initializeDelay = 0;
        private bool removeNow = false;

        private int eventFrequency = 180;

        private float lastSeverity = 0;

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

        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null && this.initializeDelay > 1;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 0)
                {
                    severityAdjustment -= Rand.Range(2.5f, 4f);                  
                }
            }
            else
            {
                this.initializeDelay++;
            }
        }        

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow || this.parent.Severity <= .001f;
            }
        }        
    }
}
