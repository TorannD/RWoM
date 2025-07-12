using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;
using System;
using AbilityUser;

namespace TorannMagic
{
    internal class JobDriver_TM_GotoAndWait : JobDriver
    {
        private int age = -1;
        public int durationTicks = 60;
        Pawn waitForPawn = null;
        public JobDef targetJobDef = null;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);
            //this.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            
            Toil waitFor = new Toil()
            {
                initAction = () =>
                {
                    if (TargetB != null)
                    {
                        this.waitForPawn = TargetB.Thing as Pawn;
                        if(this.waitForPawn != null && this.waitForPawn.jobs != null && this.waitForPawn.jobs.curJob != null && this.waitForPawn.CurJobDef != null)
                        {
                            this.targetJobDef = this.waitForPawn.CurJobDef;
                        }
                    }
                    this.durationTicks = this.job.expiryInterval;
                    if(this.age > this.durationTicks)
                    {
                        this.EndJobWith(JobCondition.InterruptForced);
                    }
                },
                tickAction = () =>
                {
                    if(this.waitForPawn != null)
                    {
                        if (this.targetJobDef != null && this.waitForPawn.jobs != null && this.waitForPawn.jobs.curJob != null && this.waitForPawn.CurJobDef != this.targetJobDef)
                        {
                            this.EndJobWith(JobCondition.InterruptForced);
                        }
                    }
                    if (age > durationTicks)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    age++;
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            waitFor.defaultDuration = this.durationTicks;
            waitFor.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Downed)
                {
                    return 1f;
                }
                return 1f - (float)waitFor.actor.jobs.curDriver.ticksLeftThisToil / this.durationTicks;

            }, false, 0f);
            yield return waitFor;         
        }
    }
}