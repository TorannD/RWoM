using RimWorld;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_MagicShield : HediffComp
    {
        private static readonly Color shieldColor = new Color(160f, 160f, 160f);

        private int shieldFade;
        public int ShieldFade
        {
            get
            {
                return this.shieldFade;
            }
            set
            {
                this.shieldFade = value;
            }
        }

        private float sevChange;
        public float SevChange
        {
            get
            {
                return this.sevChange;
            }
            set
            {
                this.sevChange = value;
            }
        }

        private float lastSev = 0;

        private float energy;

        private bool initializing = true;

        private bool broken = false;

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

        private float EnergyLossPerTick  
        {
            get
            {
                return 1f;
            }
        }

        public float Energy
        {
            get
            {
                return this.energy;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(base.Pawn.Position, base.Pawn.Map, false));
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
            }
            this.energy = 2700; //45s
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
                ResolveSeverityChange();
                if (SevChange > 0.005f)
                {
                    TM_Action.DisplayShield(base.Pawn, SevChange);
                }
                this.energy -= this.EnergyLossPerTick;
                bool flag5 = this.energy <= 0;
                if (flag5)
                {
                    severityAdjustment = -10f;
                    this.Break();
                }

            }
            base.Pawn.SetPositionDirect(base.Pawn.Position);
        }

        private void ResolveSeverityChange()
        {
            SevChange = this.lastSev - this.parent.Severity; 
        }

        private void Break()
        {
            if (!broken)
            {
                SoundDefOf.EnergyShield_Broken.PlayOneShot(new TargetInfo(base.Pawn.Position, base.Pawn.Map, false));
                FleckMaker.Static(base.Pawn.TrueCenter(), base.Pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                for (int i = 0; i < 6; i++)
                {
                    Vector3 loc = base.Pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float)Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f);
                    FleckMaker.ThrowDustPuff(loc, base.Pawn.Map, Rand.Range(0.8f, 1.2f));
                }
                this.energy = 0f;
                broken = true;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref this.energy, "energy", 0f, false);
        }
    }
}
