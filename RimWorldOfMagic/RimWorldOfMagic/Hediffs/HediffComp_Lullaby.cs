using RimWorld;
using Verse;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Lullaby : HediffComp
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
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
            }

            if (Find.TickManager.TicksGame % 20 == 0 && this.Pawn.Map != null)
            {
                CellRect cellRect = CellRect.CenteredOn(this.Pawn.Position, 2);
                cellRect.ClipInsideMap(this.Pawn.Map);
                using (IEnumerator<IntVec3> enumerator = cellRect.Cells.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Pawn approachingPawn = enumerator.Current.GetFirstPawn(this.Pawn.Map);
                        if (approachingPawn != null && this.Pawn.Faction != null & approachingPawn.HostileTo(this.Pawn.Faction))
                        {
                            //wake up!
                            if (this.Pawn.CurJob.def.defName == "JobDriver_SleepNow")
                            {
                                this.Pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, true);
                                this.Pawn.mindState.priorityWork.Clear();
                                this.Pawn.TryStartAttack(approachingPawn);
                                MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, "Disturbed!", -1);
                            }
                        }
                    }
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.parent.Severity < .1f;
            }
        }


    }
}
