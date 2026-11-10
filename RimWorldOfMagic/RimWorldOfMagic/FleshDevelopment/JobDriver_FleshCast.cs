using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.FleshDevelopment
{
    public sealed class JobDriver_FleshCast : JobDriver
    {
        private bool applied;
        public FleshSkill Skill => (FleshSkill)job.count; // Vanilla Job.ExposeData persists count.
        public static int PreparationTicks(FleshSkill skill) => skill == FleshSkill.Feast ? 300 : skill == FleshSkill.Empower || skill == FleshSkill.Harden ? 120 : 0;
        public static bool TryStart(Pawn pawn, FleshSkill skill)
        {
            if (PreparationTicks(skill) == 0 || pawn?.Spawned != true || pawn.jobs == null || !FleshActions.CanActivate(pawn, skill)) return false;
            var def = DefDatabase<JobDef>.GetNamedSilentFail("TM_FleshCast");
            if (def == null || (pawn.CurJob?.def == def && pawn.CurJob.count == (int)skill)) return false;
            var job = JobMaker.MakeJob(def, pawn);
            job.count = (int)skill;
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        public override string GetReport() => "TM_Flesh_Casting".Translate(("TM_Flesh_" + Skill).Translate()).ToString();
        public bool CanContinue => !applied && PreparationTicks(Skill) > 0 && pawn?.Spawned == true && pawn.CurJob == job && job.targetA.Thing == pawn && FleshActions.CanActivate(pawn, Skill);
        public override void ExposeData()
        {
            base.ExposeData(); // Vanilla saves current toil and remaining preparation ticks.
            Scribe_Values.Look(ref applied, "fleshApplied", false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !applied && !CanContinue);
            // Wait stops pathing at init. Delay and progress are vanilla and survive save/load.
            var prepare = Toils_General.Wait(PreparationTicks(Skill)).WithProgressBarToilDelay(TargetIndex.A);
            prepare.tickAction = () => FleshEffects.Preparing(pawn, Skill);
            yield return prepare;
            var finish = ToilMaker.MakeToil("FleshCastComplete");
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            finish.initAction = () =>
            {
                if (applied) return;
                if (!CanContinue) { EndJobWith(JobCondition.Incompletable); return; }
                applied = true;
                // MP's ShouldSync is false while simulation ticks or a sync command executes.
                // This applies locally on each client; it does not enqueue another UI command.
                if (!FleshActions.TryActivate(pawn, Skill)) EndJobWith(JobCondition.Incompletable);
            };
            yield return finish;
        }
    }
}
