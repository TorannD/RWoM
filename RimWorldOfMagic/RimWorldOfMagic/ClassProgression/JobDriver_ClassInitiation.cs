using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public class JobDriver_ClassInitiation : JobDriver
    {
        private string selectedProfileId;
        private int workTicks;
        private int lastWorkTick = -1;
        private int useDuration = -1;
        private bool completionAttempted;
        private bool completed;
        private bool loaded;
        private Thing originalBook;
        public string SelectedProfileId => selectedProfileId;
        public int WorkTicks => workTicks;
        public bool Completed => completed;
        private Thing Book => job.GetTarget(TargetIndex.A).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref selectedProfileId, "classInitiationProfile");
            Scribe_References.Look(ref originalBook, "classInitiationBook");
            Scribe_Values.Look(ref workTicks, "classInitiationWorkTicks", 0);
            Scribe_Values.Look(ref lastWorkTick, "classInitiationLastWorkTick", -1);
            Scribe_Values.Look(ref useDuration, "classInitiationUseDuration", -1);
            Scribe_Values.Look(ref completionAttempted, "classInitiationAttempted", false);
            Scribe_Values.Look(ref completed, "classInitiationCompleted", false);
            if (Scribe.mode == LoadSaveMode.LoadingVars) loaded = true;
        }
        private bool Valid()
        {
            string fixedId = ClassInitiationMenu.ProfileForJob(job?.def);
            if (fixedId == null || (loaded && (selectedProfileId == null || originalBook == null))) return false;
            if (originalBook == null) originalBook = Book;
            if (!System.Object.ReferenceEquals(Book, originalBook)) return false;
            if (selectedProfileId == null) selectedProfileId = fixedId;
            if (selectedProfileId != fixedId || !ClassInitiation.CanUseFoundation(pawn, Book, selectedProfileId, out _)) return false;
            return Book.Spawned ? Book.Map == pawn.Map && !Book.IsForbidden(pawn)
                : Book.ParentHolder == pawn.carryTracker || Book.ParentHolder == pawn.inventory;
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            job.count = 1;
            if (!Valid()) return false;
            useDuration = Book.TryGetComp<CompUsable>()?.Props.useDuration ?? -1;
            return useDuration >= 0 && (!Book.Spawned || pawn.Reserve(job.targetA, job, 1, 1, null, errorOnFailed));
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            this.FailOn(() => !completed && (completionAttempted || !Valid()));
            var read = ToilMaker.MakeToil("ClassInitiationRead");
            read.defaultCompleteMode = ToilCompleteMode.Never;
            read.handlingFacing = true;
            read.tickAction = ReadTick;
            read.WithProgressBar(TargetIndex.A, () => useDuration <= 0 ? 1f : (float)workTicks / useDuration);
            yield return Toils_Jump.JumpIf(read, () => !Book.Spawned);
            // Native UseArtifact reads beside the concrete map item without moving it.
            yield return Toils_Goto.GotoThing(TargetIndex.A, Book.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch);
            yield return read;
        }
        private void ReadTick()
        {
            if (completed) { EndJobWith(JobCondition.Succeeded); return; }
            if (completionAttempted || !Valid() || useDuration < 0
                || (Book.Spawned && !pawn.CanReachImmediate(Book, Book.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.Touch)))
            { EndJobWith(JobCondition.Incompletable); return; }
            int now = Find.TickManager.TicksGame;
            if (now <= lastWorkTick) return;
            lastWorkTick = now;
            Book.TryGetComp<CompUseEffect_LearnMagic>()?.PrepareTick();
            pawn.rotationTracker.FaceTarget(Book);
            workTicks++;
            if (workTicks < useDuration) return;
            completionAttempted = true;
            completed = ClassInitiation.CompleteBook(pawn, Book, selectedProfileId);
            EndJobWith(completed ? JobCondition.Succeeded : JobCondition.Incompletable);
        }
    }
}
