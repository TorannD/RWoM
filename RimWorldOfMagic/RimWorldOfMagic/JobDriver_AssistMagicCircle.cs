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
    public class JobDriver_AssistMagicCircle : JobDriver
    {
        private int age = -1;
        public int durationTicks = 600;
        public int totalWaitDuration = 0;
        Pawn waitForPawn = null;
        Thing waitForThing = null;
        public JobDef targetJobDef = null;
        public Building_TMMagicCircleBase circle = null;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOnDestroyedOrNull(TargetIndex.A);
            //this.FailOnDowned(TargetIndex.A);
            //this.FailOnCannotTouch(TargetIndex.A, PathEndMode.OnCell);

            circle = TargetB.Thing as Building_TMMagicCircleBase;
            Toil gotoPortal = new Toil()
            {
                initAction = () =>
                {
                    pawn.pather.StartPath(TargetA.Cell, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoPortal;

            Toil waitFor = new Toil()
            {
                initAction = () =>
                {
                    if (circle != null)
                    {
                        this.pawn.rotationTracker.FaceTarget(circle.GetCircleCenter);
                        
                    }
                    if (this.age > this.durationTicks)
                    {
                        this.EndJobWith(JobCondition.InterruptForced);
                    }
                },
                tickAction = () =>
                {
                    if (this.circle != null)
                    {
                        this.pawn.rotationTracker.FaceTarget(circle.GetCircleCenter);
                    }
                    else
                    {
                        if(TargetB.Thing != null && TargetB.Thing is Building_TMMagicCircleBase)
                        {
                            circle = TargetB.Thing as Building_TMMagicCircleBase;
                        }
                    }
                    if (age > durationTicks)
                    {
                        if (circle != null)
                        {
                            if (circle.IsPending)
                            {
                                this.totalWaitDuration += age;
                                if(totalWaitDuration >= (15 * this.durationTicks))
                                {
                                    this.EndJobWith(JobCondition.Incompletable);
                                }
                                age = 0;
                            }
                            else
                            {
                                this.EndJobWith(JobCondition.InterruptForced);
                            }
                        }
                        else
                        {
                            this.EndJobWith(JobCondition.Succeeded);
                        }
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
                return (float)((float)age / (float)this.durationTicks); 

            }, false, 0f);
            yield return waitFor;
        }
    }
}