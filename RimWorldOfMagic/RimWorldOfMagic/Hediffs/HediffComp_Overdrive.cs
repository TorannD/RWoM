using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Overdrive : HediffComp
    {

        private bool initialized = false;
        private int feedbackRate = 300;
        private int nextFeedback = 0;
        private int hediffPwr = 0;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.feedbackRate, "feedbackRate", 300, false);
            Scribe_Values.Look<int>(ref this.nextFeedback, "nextFeedback", 0, false);
            Scribe_Values.Look<int>(ref this.hediffPwr, "hediffPwr", 0, false);
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
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 1f);
                if (this.Def.defName == "TM_OverdriveHD_III")
                {
                    hediffPwr = 3;
                }
                else if (this.Def.defName == "TM_OverdriveHD_II")
                {
                    hediffPwr = 2;
                }
                else if (this.Def.defName == "TM_OverdriveHD_I")
                {
                    hediffPwr = 1;
                }
                else
                {
                    hediffPwr = 0;
                }
                this.feedbackRate = 300 + (50 * hediffPwr);
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.parent.Severity < .01f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (base.Pawn != null & base.parent != null)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }

            if(Find.TickManager.TicksGame % 60 == 0)
            {
                this.parent.Severity -= (.015f * (1+hediffPwr));
                //HealthUtility.AdjustSeverity(base.Pawn, this.Def, -(0.025f* hediffPwr));
            }
            
            if (this.nextFeedback < Find.TickManager.TicksGame)
            {
                this.nextFeedback = Find.TickManager.TicksGame + this.feedbackRate;
                Pawn pawn = base.Pawn as Pawn;
                bool flag = pawn != null;
                if (flag)
                {
                    Vector3 rndPos = pawn.DrawPos;
                    rndPos.x += Rand.Range(-.2f, .2f);
                    rndPos.z += Rand.Range(-.2f, .2f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndPos, pawn.Map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                    FleckMaker.ThrowSmoke(rndPos, pawn.Map, Rand.Range(.8f, 1.2f));
                    rndPos = pawn.DrawPos;
                    rndPos.x += Rand.Range(-.2f, .2f);
                    rndPos.z += Rand.Range(-.2f, .2f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.ElectricalSpark, rndPos, pawn.Map, Rand.Range(.4f, .7f), .2f, .05f, .1f, 0, 0, 0, Rand.Range(0, 360));
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map, false), MaintenanceType.None);
                    info.pitchFactor = .4f;
                    info.volumeFactor = .4f;
                    SoundDefOf.TurretAcquireTarget.PlayOneShot(info);
                    if (Rand.Chance(.6f - (.06f * hediffPwr)))
                    {                        
                        TM_Action.DamageEntities(pawn, null, Rand.Range(1f, (4f - (.5f * hediffPwr))), DamageDefOf.Burn, pawn);
                    }
                }
            }
        }

        
    }
}
