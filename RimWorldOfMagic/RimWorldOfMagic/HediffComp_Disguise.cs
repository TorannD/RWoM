using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Disguise : HediffComp
    {

        private bool initialized = false;
        private bool hasDisguise = false;
        private bool hasPossess = false;
        private bool disguiseFlag = false;
        private bool possessFlag = false;
        private int age;

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
                if (base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD) || base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_I) || base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_II) || base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_PossessionHD_III))
                {
                    this.hasPossess = true;
                }
                if (base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD) || base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_I) || base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_II) || base.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DisguiseHD_III))
                {
                    this.hasDisguise = true;
                }
                if (this.parent.def.defName == "TM_DisguiseHD" || this.parent.def.defName == "TM_DisguiseHD_I" || this.parent.def.defName == "TM_DisguiseHD_II" || this.parent.def.defName == "TM_DisguiseHD_III")
                {
                    this.disguiseFlag = true;
                }
                if (this.parent.def.defName == "TM_PossessionHD" || this.parent.def.defName == "TM_PossessionHD_I" || this.parent.def.defName == "TM_PossessionHD_II" || this.parent.def.defName == "TM_PossessionHD_III")
                {
                    this.possessFlag = true;
                }
            }
            this.age = 60;
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
                Vector3 drawOverhead = base.Pawn.DrawPos;
                drawOverhead.z += .9f;
                drawOverhead.x += .2f;
                if(this.hasPossess)
                {
                    if(this.possessFlag)
                    {
                        TM_MoteMaker.ThrowTextMote(drawOverhead, base.Pawn.Map, Mathf.RoundToInt(this.parent.Severity).ToString(), Color.white, 1f / 66f, -1f);
                    }
                }
                else
                {
                    TM_MoteMaker.ThrowTextMote(drawOverhead, base.Pawn.Map, Mathf.RoundToInt(this.parent.Severity).ToString(), Color.white, 1f / 66f, -1f);
                }                

                if (this.age <=0)
                {
                    severityAdjustment--;
                    this.age = 60;                    
                }
                this.age--;                
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.parent.Severity < .1f;
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.disguiseFlag, "disguiseFlag", false, false);
            Scribe_Values.Look<bool>(ref this.possessFlag, "possessFlag", false, false);
            Scribe_Values.Look<bool>(ref this.hasPossess, "hasPossess", false, false);
            Scribe_Values.Look<bool>(ref this.hasDisguise, "hasDisguise", false, false);
        }
    }
}
