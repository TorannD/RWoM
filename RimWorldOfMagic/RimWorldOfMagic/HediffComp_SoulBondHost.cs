using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_SoulBondHost : HediffComp
    {
        private bool initialized = false;
        private bool soulPawnRemove = false;

        public Pawn bonderPawn;
        public Pawn polyHost;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.bonderPawn, "bonderPawn", false);
            Scribe_References.Look<Pawn>(ref this.polyHost, "polyHost", false);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo,culprit);
            if (Pawn.Dead)
                ClassProgression.DemonSoulBond.EndOnDeath(bonderPawn,Pawn,parent.def.defName == "TM_SoulBondMentalHD",true);
        }

        public override string CompLabelInBracketsExtra => BonderPawn != null ? BonderPawn.LabelShort + "[-]" + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public Pawn BonderPawn
        {
            get
            {
                return this.bonderPawn;
            }
            set
            {
                this.bonderPawn = value;
            }
        }

        public string labelCap
        {
            get
            {
                if (this.BonderPawn != null)
                {
                    return base.Def.LabelCap + "(" + this.BonderPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (this.BonderPawn != null)
                {
                    return base.Def.label + "(" + this.BonderPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                for (int i = 0; i < 2; i++)
                {
                    TM_MoteMaker.ThrowArcaneMote(this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.6f, 1f));
                }
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
            bool flag4 = Find.TickManager.TicksGame % 600 == 0;
            if (flag4)
            {
                bool mental = parent.def.defName == "TM_SoulBondMentalHD";
                string source = mental ? "TM_WDSoulBondMentalHD" : "TM_SDSoulBondPhysicalHD";
                soulPawnRemove = bonderPawn.DestroyedOrNull() || bonderPawn.Dead ||
                    bonderPawn.GetCompAbilityUserMagic()?.soulBondPawn != Pawn ||
                    (!ClassProgression.SpiritRebirthState.TryGet(bonderPawn,out _) &&
                     !bonderPawn.health.hediffSet.hediffs.Any(h => h.def.defName == source));

            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.soulPawnRemove;
            }
        }
    }
}
