using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class HediffComp_ShadowSlayerCloak : HediffComp
    {
        private bool initialized = false;

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

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Find.TickManager.TicksGame % 21 == 0)
            {
                bool firingAtTarget = this.Pawn.TargetCurrentlyAimingAt != null && this.Pawn.TargetCurrentlyAimingAt.Thing != null;
                bool hasTargetedJob = this.Pawn.CurJob != null && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing is Pawn;
                if (firingAtTarget || hasTargetedJob)
                {
                    HediffComp_Disappears hdComp = base.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowSlayerCloakHD).TryGetComp<HediffComp_Disappears>();
                    if (hdComp != null)
                    {
                        hdComp.ticksToDisappear -= Rand.Range(40, 60);
                        if (hdComp.ticksToDisappear <= 0)
                        {
                            Effecter InvisEffect = TorannMagicDefOf.TM_InvisibilityEffecter.Spawn();
                            InvisEffect.Trigger(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
                            InvisEffect.Cleanup();
                        }
                    }
                }                
            }
        }
    }
}
