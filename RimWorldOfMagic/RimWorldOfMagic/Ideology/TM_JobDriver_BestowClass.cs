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
    public class JobDriver_BestowClass : JobDriver
    {
        private const TargetIndex TargetPawnIndex = TargetIndex.A;

        protected Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        private int age = -1;
        public int durationTicks = 500;
        public int rotationDegree = 0;
        public float moteSize = 1f;
        public int moteCount = 0;
        public int moteOsc = 1;

        public static void BestowClass(Pawn pawn, Pawn doer)
        {
            Thoughts.TM_Inspiration_ArcanePathway inspiration = pawn.Inspiration as Thoughts.TM_Inspiration_ArcanePathway;

            TraitDef td = TM_Data.EnabledMagicTraits[inspiration.mageIndex];
            if (td != null)
            {
                CompUseEffect_LearnMagic.FixTrait(pawn, pawn.story.traits.allTraits);
                if(td == TorannMagicDefOf.Priest)
                {
                    CompUseEffect_LearnMagic.FixPriestSkills(pawn);
                }
                if(td == TorannMagicDefOf.TM_Bard)
                {
                    CompUseEffect_LearnMagic.FixBardSkills(pawn);
                }
                pawn.story.traits.GainTrait(new Trait(td, td.degreeDatas.FirstOrDefault().degree, false));
            }
        }

        public static void CreateHistoryEventDef(Pawn pawn)
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(TorannMagicDefOf.TM_BestowMagicEvent, pawn.Named(HistoryEventArgsNames.Doer)));
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
                        if (age >= 300)
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
                            drawPos.z += .005f * (durationTicks - age);
                            TM_MoteMaker.ThrowGenericMote(mote, drawPos, this.pawn.Map, 1.5f - (float)(.002f * (age)), 0.05f, .03f, .04f, 0, 0, 0, 0);
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
                    if (target.Inspired && target.Inspiration.def == TorannMagicDefOf.ID_ArcanePathways)
                    {
                        BestowClass(target, pawn);
                        CreateHistoryEventDef(target);
                        TorannMagicDefOf.TM_WindLowSD.PlayOneShot(target);
                        TargetInfo ti = new TargetInfo(target.Position, pawn.Map, false);
                        TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, pawn.Map, Vector3.zero, 2f, 0f, .1f, .4f, 1f, -2f);
                    }
                    else
                    {
                        Messages.Message("TM_DoesNotHaveArcanePathwayInspiration".Translate(pawn.LabelShort), MessageTypeDefOf.RejectInput);
                    }
                }                
            });
            yield return doFor;

            yield return Toils_General.Wait(120);

        }
    }
}
