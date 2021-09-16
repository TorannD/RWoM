using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffComp_BrandingBase : HediffComp
    {
        private bool initializing = true;
        private int nextCheckTick = 0;

        public bool surging = false;
        public bool draining = false;
        private bool shouldRemove = false;
        Pawn branderPawn = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.branderPawn, "branderPawn", false);
            Scribe_Values.Look<bool>(ref this.surging, "surging", false, false);
            Scribe_Values.Look<bool>(ref this.draining, "draining", false, false);
        }

        public string GetCompLabel
        {
            get
            {
                string tmp = "";
                if(surging)
                {
                    tmp += "[+]";
                }
                else if(draining)
                {
                    tmp += "[-]";
                }
                tmp += base.CompLabelInBracketsExtra;
                return tmp;
            }
        }

        public override string CompLabelInBracketsExtra => branderPawn != null ? branderPawn.LabelShort + GetCompLabel : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if (branderPawn != null)
                {
                    return base.Def.LabelCap + "(" + branderPawn.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (branderPawn != null)
                {
                    return base.Def.label + "(" + branderPawn.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned && base.Pawn.Map != null)
            {
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 3f);
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

                if (Find.TickManager.TicksGame <= nextCheckTick)
                {
                    nextCheckTick = Find.TickManager.TicksGame + Rand.Range(400, 600);
                    if (branderPawn != null && !branderPawn.Dead && !branderPawn.Destroyed)
                    {
                        //do nothing
                    }
                    else
                    {
                        this.shouldRemove = true;
                    }
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.shouldRemove;
            }
        }
    }
}
