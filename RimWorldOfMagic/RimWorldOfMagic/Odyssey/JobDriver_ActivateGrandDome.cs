using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    public class JobDriver_ActivateGrandDome : JobDriver
    {
        private int workedTicks;
        private int lastWorkTick = -1;
        private bool completed;
        private CompGrandAtmosphereDome Dome => (TargetA.Thing as ThingWithComps)?.GetComp<CompGrandAtmosphereDome>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workedTicks, "TM_grandDomeWorkedTicks", 0);
            Scribe_Values.Look(ref lastWorkTick, "TM_grandDomeLastWorkTick", -1);
            Scribe_Values.Look(ref completed, "TM_grandDomeCompleted", false);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Dome?.CanActivate(pawn, false) == true && pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil
            {
                initAction = () => { },
                tickAction = () =>
                {
                    if (completed) return;
                    if (Dome?.CanActivate(pawn) != true) { EndJobWith(JobCondition.Incompletable); return; }
                    int now = Find.TickManager.TicksGame;
                    if (now <= lastWorkTick) return;
                    lastWorkTick = now;
                    // Count actual ritual work, never wall-clock downtime, including save/load.
                    if (++workedTicks < Dome.Props.ritualTicks) return;
                    completed = true;
                    EndJobWith(Dome.CompleteActivation(pawn) ? JobCondition.Succeeded : JobCondition.Incompletable);
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
        }
    }
}
