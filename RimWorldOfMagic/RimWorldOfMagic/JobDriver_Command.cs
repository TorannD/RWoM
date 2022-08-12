using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI;
using UnityEngine;
using AbilityUser;

namespace TorannMagic
{
    internal class JobDriver_Command : JobDriver
    {
        private const TargetIndex caster = TargetIndex.B;

        int age = -1;
        int lastEffect = 0;
        int ticksTillEffects = 20;
        public int duration = 545;
        Vector3 positionBetween = Vector3.zero;


        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(TargetA, this.job, 1, 1, null, errorOnFailed))
            {
                return true;
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Pawn mentalPawn = TargetA.Thing as Pawn;
            Toil gotoPawn = new Toil()
            {
                initAction = () =>
                {
                    pawn.pather.StartPath(TargetA, PathEndMode.Touch);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoPawn;
            Toil doMotivate = new Toil();
            doMotivate.initAction = delegate
            {                    
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
                if (mentalPawn.DestroyedOrNull() || mentalPawn.Dead)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }

                this.pawn.rotationTracker.FaceTarget(TargetA);
                mentalPawn.rotationTracker.FaceTarget(this.pawn);
                mentalPawn.ClearAllReservations(false);
                this.pawn.ClearAllReservations(false);

            };
            doMotivate.tickAction = delegate
            {
                if (mentalPawn.DestroyedOrNull() || mentalPawn.Dead || !mentalPawn.InMentalState)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
                if (age > (lastEffect + ticksTillEffects))
                {
                    DoSpeechEffects(mentalPawn);
                    lastEffect = age;
                }
                if (!mentalPawn.Drafted && mentalPawn.CurJobDef != JobDefOf.Wait)
                {
                    if (mentalPawn.jobs.posture == PawnPosture.Standing)
                    {
                        Job job = new Job(JobDefOf.Wait, mentalPawn);
                        mentalPawn.jobs.TryTakeOrderedJob(job, JobTag.InMentalState);
                    }
                }
                if(mentalPawn.Drafted && mentalPawn.CurJobDef != JobDefOf.Wait_Combat)
                {
                    this.EndJobWith(JobCondition.InterruptForced);
                }
                age++;
                ticksLeftThisToil = duration - age;
                if((mentalPawn.Position - this.pawn.Position).LengthHorizontal > 5)
                {
                    age = duration + 1;
                }
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
            };
            doMotivate.defaultCompleteMode = ToilCompleteMode.Delay;
            doMotivate.defaultDuration = this.duration;
            doMotivate.WithProgressBar(TargetIndex.B, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead)
                {
                    return 1f;
                }
                return 1f - (float)doMotivate.actor.jobs.curDriver.ticksLeftThisToil / this.duration;

            }, false, 0f);
            doMotivate.AddFinishAction(delegate
            {
                mentalPawn.MentalState.RecoverFromState();
                AssignXP();                
                mentalPawn.jobs.EndCurrentJob(JobCondition.Succeeded, false);
            });
            yield return doMotivate;
        }

        private void DoSpeechEffects(Pawn mentalPawn)
        {
            int effectNum = Rand.RangeInclusive(0, 4);
            Color randomColor = new Color(Rand.Range(0f, 1f), Rand.Range(0f, 1f), Rand.Range(0f, 1f), Rand.Range(.7f, 1f));
            if(positionBetween == Vector3.zero)
            {
                positionBetween = TM_Calc.GetVectorBetween(pawn.DrawPos, mentalPawn.DrawPos);
            }

            float direction = 0f;
            Vector3 rndPos = positionBetween;
            rndPos.x += Rand.Range(-1f, 1f);
            rndPos.z += Rand.Range(-1f, 1f);
            ThingDef mote = TorannMagicDefOf.Mote_Twinkle;
            TM_MoteMaker.ThrowGenericMote(mote, rndPos, this.pawn.Map, Rand.Range(.2f, .7f), Rand.Range(.2f, .6f), Rand.Range(0f, .8f), Rand.Range(.5f,.8f), Rand.Range(-50, 50), Rand.Range(.5f, 1f), direction, Rand.Range(0,360));

        }        

        private void AssignXP()
        {
            CompAbilityUserMight comp = this.pawn.GetCompAbilityUserMight();

            if (comp != null)
            {
                try
                {

                    int xpBase = Rand.Range(50, 75);
                    int xpGain = Mathf.RoundToInt(xpBase * comp.xpGain);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "XP +" + xpGain, -1f);
                    comp.MightUserXP += xpGain;
                    if (this.pawn.needs.joy != null)
                    {
                        this.pawn.needs.joy.GainJoy(.4f, TorannMagicDefOf.Social);
                    }
                    if (this.pawn.skills != null)
                    {
                        this.pawn.skills.Learn(SkillDefOf.Social, Rand.Range(200f, 500f));
                    }
                }
                catch (NullReferenceException ex)
                {
                    //failed
                }
            }
        }        
    }
}