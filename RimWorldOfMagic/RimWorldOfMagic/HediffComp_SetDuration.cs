using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class HediffComp_SetDuration : HediffComp
    {
        public int duration = 10;

        public override void CompExposeData()
        {
            Scribe_Values.Look<int>(ref this.duration, "duration", 10, false);
            base.CompExposeData();
        }

        public HediffCompProperties_SetDuration Props => (HediffCompProperties_SetDuration)base.props;

        public override void CompPostMake()
        {
            this.duration = Props.duration;
        }

        public bool initialized = false;
        public bool removeNow = false;

        public virtual string labelCap
        {
            get
            {
                return base.Def.LabelCap + (" seconds remaining " + this.duration.ToString("#"));
            }
        }

        public virtual string label
        {
            get
            {
                return base.Def.label + (" seconds remaining " + this.duration.ToString("#"));
            }
        }


        public virtual void Initialize()
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
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                this.duration--;
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

        //public override void CompPostMerged(Hediff other)
        //{
        //    base.CompPostMerged(other);
        //    if (other.def == this.parent.def)
        //    {
        //        HediffComp_SetDuration hdComp = other.TryGetComp<HediffComp_SetDuration>();
        //        if (hdComp != null && hdComp.duration > 0)
        //        {
        //            this.duration += hdComp.duration;
        //        }
        //        this.parent.Severity = Mathf.Max(other.Severity, this.parent.Severity);
        //    }
        //}
    }
}
