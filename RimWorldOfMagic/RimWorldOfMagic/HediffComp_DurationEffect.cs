using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_DurationEffect : HediffComp
    {

        private bool initialized = false;
        private int effectFrequency;
        private int ticksTillEffect;
        private float severityReduction;

        private float scaleAvg = 1f;
        private float fadeIn = .5f;
        private float fadeOut = .5f;
        private float solidTime = 0;
        private float velocity = 1f;
        private float velocityAngle = 0;
        private float lookAngle = 0;
        private int rotationRate = 0;


        ThingDef moteDef;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.ticksTillEffect, "ticksTillEffect", 0, false);
            Scribe_Values.Look<float>(ref this.severityReduction, "severityReduction", .1f, false);
            Scribe_Defs.Look<ThingDef>(ref this.moteDef, "moteDef");
            Scribe_Values.Look<float>(ref this.scaleAvg, "scaleAvg", 1f, false);
            Scribe_Values.Look<float>(ref this.fadeIn, "fadeIn", .5f, false);
            Scribe_Values.Look<float>(ref this.fadeOut, "fadeOut", .5f, false);
            Scribe_Values.Look<float>(ref this.solidTime, "solidTime", 0f, false);
            Scribe_Values.Look<float>(ref this.velocity, "velocity", 0f, false);
            Scribe_Values.Look<float>(ref this.velocityAngle, "velocityAngle", 0f, false);
            Scribe_Values.Look<float>(ref this.lookAngle, "lookAngle", 0f, false);
            Scribe_Values.Look<int>(ref this.rotationRate, "rotationRate", 0, false);
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
                if(this.parent.def == TorannMagicDefOf.TM_GravitySlowHD)
                {
                    this.effectFrequency = 120;
                    this.severityReduction = .2f;
                    this.moteDef = TorannMagicDefOf.Mote_ArcaneWaves;
                    this.scaleAvg = .25f;
                    this.solidTime = 1f;
                    this.fadeIn = .1f;
                    this.fadeOut = .75f;
                    this.rotationRate = 500;
                    this.velocity = 0;
                    this.velocityAngle = 0;
                    this.lookAngle = Rand.Range(0, 360);
                }
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
                if(this.ticksTillEffect <=0)
                {
                    severityAdjustment -= this.severityReduction;
                    this.ticksTillEffect = this.effectFrequency;
                    TM_MoteMaker.ThrowGenericMote(this.moteDef, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.75f*this.scaleAvg, 1.25f*this.scaleAvg), this.solidTime, this.fadeIn, this.fadeOut, this.rotationRate, this.velocity, this.velocityAngle, this.lookAngle);
                }
                this.ticksTillEffect--;
            }
            else
            {
                severityAdjustment = 0;
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.parent.Severity < .01f;
            }
        }
    }
}
