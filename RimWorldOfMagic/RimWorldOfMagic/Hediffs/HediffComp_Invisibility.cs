using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffComp_Invisibility : HediffComp
    {
        private bool initializing = true;

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
            }
            if (Find.TickManager.TicksGame % 30 == 0)
            {
                if(this.Pawn.CurJob != null && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing is Pawn)
                {
                    severityAdjustment -= 20;
                }
                Effecter InvisEffect = TorannMagicDefOf.TM_InvisibilityEffecter.Spawn();
                InvisEffect.Trigger(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                InvisEffect.Cleanup();
            }
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                severityAdjustment--;
            }
        }
    }
}
