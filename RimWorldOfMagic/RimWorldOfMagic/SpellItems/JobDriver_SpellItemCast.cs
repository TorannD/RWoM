using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.SpellItems
{
    public sealed class JobDriver_SpellItemCast : JobDriver
    {
        private bool applied;
        private CompSpellItem Item => SpellItemCasting.Comp(job.targetA.Thing);

        public bool CanContinue => !applied && pawn?.CurJob == job && job.targetC.Thing == pawn
            && Item != null && (job.count == 0 ? Item.UseRejection(pawn) == null : Item.Matches(pawn, job.count))
            && SpellItemEffects.TargetRejection(pawn, job.targetB, Item.Props) == null
            && (Item.Props.spell != SpellItemKind.Portal || SpellItemPortal.ValidDestination(pawn.Map, job.targetB.Cell, job.globalTarget));

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;
        public override string GetReport() => "TM_SpellItem_Casting".Translate(job.targetA.Thing?.LabelShort ?? "");

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref applied, "spellItemApplied", false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddFinishAction(condition => Item?.Release(pawn, job.count));
            this.FailOn(() => !applied && !CanContinue);
            Toil prepare = Toils_General.Wait(Item?.Props.warmupTicks ?? 1)
                .WithProgressBarToilDelay(TargetIndex.C);
            var beginWait = prepare.initAction;
            prepare.initAction = () =>
            {
                if (job.count == 0) job.count = Item?.TryBegin(pawn) ?? 0;
                if (job.count == 0 || !CanContinue) { EndJobWith(JobCondition.Incompletable); return; }
                beginWait?.Invoke();
            };
            prepare.tickAction = () => pawn.rotationTracker?.FaceTarget(job.targetB);
            yield return prepare;
            Toil finish = ToilMaker.MakeToil("SpellItemComplete");
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            finish.initAction = () =>
            {
                if (applied) return;
                if (!CanContinue) { EndJobWith(JobCondition.Incompletable); return; }
                CompSpellItem item = Item;
                applied = true;
                if (!item.TryCommit(pawn, job.count, job.targetB))
                    EndJobWith(JobCondition.Incompletable);
            };
            yield return finish;
        }
    }
}
