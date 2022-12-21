using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using System;


namespace TorannMagic.Golems
{
    internal class JobDriver_GolemDespawnWait : JobDriver
    {
        private int age = -1;
        public int durationTicks = 10;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil toil = job.forceSleep ? Toils_LayDown.LayDown(TargetIndex.A, hasBed: false, lookForOtherJobs: false) : ToilMaker.MakeToil("MakeNewToils");
            Toil toil2 = toil;
            toil2.initAction = (Action)Delegate.Combine(toil2.initAction, (Action)delegate
            {
                pawn.pather?.StopDead();
            });
            Toil toil3 = toil;
            toil3.tickAction = (Action)Delegate.Combine(toil3.tickAction, (Action)delegate
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                if(!cg.shouldDespawn)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            });
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            if (job.overrideFacing != Rot4.Invalid)
            {
                toil.handlingFacing = true;
                Toil toil4 = toil;
                toil4.tickAction = (Action)Delegate.Combine(toil4.tickAction, (Action)delegate
                {
                    pawn.rotationTracker.FaceTarget(pawn.Position + job.overrideFacing.FacingCell);
                });
            }
            else if (pawn.mindState != null && pawn.mindState.duty != null && pawn.mindState.duty.focus != null && job.def != JobDefOf.Wait_Combat)
            {
                LocalTargetInfo focusLocal = pawn.mindState.duty.focus;
                toil.handlingFacing = true;
                Toil toil5 = toil;
                toil5.tickAction = (Action)Delegate.Combine(toil5.tickAction, (Action)delegate
                {
                    pawn.rotationTracker.FaceTarget(focusLocal);
                });
            }            
            yield return toil;

        }
    }
}