using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;

namespace TorannMagic.Ideology
{
    public class JobDriver_SeverMagic : JobDriver
    {
        private const TargetIndex TargetPawnIndex = TargetIndex.A;

        protected Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private int age = -1;
        public int durationTicks = 500;
        public int rotationDegree = 0;
        public float moteSize = 1f;
        public int moteCount = 0;
        public int moteOsc = 1;

        public static void SeverMagic(Pawn pawn, Pawn doer)
        {
            ModOptions.TM_DebugTools.RemoveClass(pawn);
        }

        public static void CreateHistoryEventDef(Pawn pawn)
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_SeverMagicEvent, pawn.Named(HistoryEventArgsNames.Doer)));
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            this.FailOnDespawnedOrNull(TargetIndex.A);
            Pawn target = Target;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(20);
            Toil doFor = new Toil()
            {
                initAction = () =>
                {
                    if (this.age > this.durationTicks)
                    {
                        this.EndJobWith(JobCondition.InterruptForced);
                    }
                },
                tickAction = () =>
                {
                    if (Find.TickManager.TicksGame % 8 == 0)
                    {
                        Vector3 rndPos = target.DrawPos;
                        rndPos.x += (Rand.Range(-.2f, .2f));
                        rndPos.z += Rand.Range(-.2f, .2f);
                        rotationDegree += Rand.Range(8, 16);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlueSwirl, rndPos, this.pawn.Map, moteSize, 0.2f, .1f, .2f, 30+rotationDegree, 0, 0, rotationDegree);                      
                    }
                    if (Find.TickManager.TicksGame % 2 == 0)
                    {
                        ThingDef mote = null;
                        if (durationTicks >= 300)
                        {                            
                            switch (moteCount)
                            {
                                case 0:
                                    mote = TorannMagicDefOf.Mote_BlueSpireE;
                                    moteOsc = 1;
                                    moteCount += moteOsc;
                                    break;
                                case 1:
                                    mote = TorannMagicDefOf.Mote_BlueSpireEs;
                                    moteCount += moteOsc;
                                    break;
                                case 2:
                                    mote = TorannMagicDefOf.Mote_BlueSpireWs;
                                    moteCount += moteOsc;
                                    break;
                                default:
                                    mote = TorannMagicDefOf.Mote_BlueSpireW;
                                    moteOsc = -1;
                                    moteCount += moteOsc;
                                    break;
                            }
                        }
                        if (mote != null)
                        {
                            Vector3 drawPos = target.DrawPos;
                            drawPos.z += .001f * age;
                            TM_MoteMaker.ThrowGenericMote(mote,drawPos, this.pawn.Map, .45f + (float)(.001f * age), 0.05f, .03f, .04f, 0, 0, 0, 0);
                        }
                    }

                    if (age >= durationTicks)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    age++;
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            doFor.defaultDuration = this.durationTicks;
            doFor.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead)
                {
                    return 1f;
                }
                return 1f - (float)doFor.actor.jobs.curDriver.ticksLeftThisToil / this.durationTicks;

            }, false, 0f);
            doFor.AddFinishAction(delegate
            {
                if (age >= (int)(durationTicks * .95f))
                {
                    SeverMagic(target, pawn);
                    CreateHistoryEventDef(target);
                    TorannMagicDefOf.TM_WindLowSD.PlayOneShot(target);
                    SoundDefOf.Execute_Cut.PlayOneShot(target);
                    TargetInfo ti = new TargetInfo(target.Position, pawn.Map, false);
                    TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, pawn.Map, Vector3.zero, 2f, 0f, .1f, .4f, 1f, -2f);
                    HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_MagicSeverenceHD, 1f);
                }                
            });
            yield return doFor;

            yield return Toils_General.Wait(120);

        }
    }
}
