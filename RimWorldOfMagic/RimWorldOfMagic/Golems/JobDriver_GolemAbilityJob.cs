using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using TorannMagic.TMDefs;
using UnityEngine;
using HarmonyLib;


namespace TorannMagic.Golems
{
    internal class JobDriver_GolemAbilityJob : JobDriver
    {
        private int age = -1;
        public int duration = 60;
        public TM_GolemAbility ability;
        Thing targetThing = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<TM_GolemAbility>(ref ability, "ability");
            Scribe_Values.Look<int>(ref age, "age");
            Scribe_Values.Look<int>(ref duration, "duration");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil doSpell = new Toil();
            doSpell.initAction = delegate
            {
                if (TargetA.Thing != null)
                {
                    targetThing = TargetA.Thing;
                }
                TMPawnGolem pg = pawn as TMPawnGolem;
                if(pg != null && pg.Golem.ActiveAbility != null)
                {
                    if (ability == null)
                    {
                        ability = pg.Golem.ActiveAbility;
                    }
                    this.duration = (int)((ability.golemAbilityDef.jobDuration * pg.Golem.DurationModifier) + (ability.golemAbilityDef.jobBurstCount * ability.golemAbilityDef.jobTicksBetweenBursts));
                    if(duration > 0)
                    {
                        pg.stances.SetStance(new Stance_Warmup(duration, targetThing, null));
                        Traverse.Create(root: pg.stances.curStance).Field(name: "drawAimPie").SetValue(true);
                    }
                }
                else
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }

                if (targetThing == null)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
                else if ( (targetThing.DestroyedOrNull() || targetThing.Map == null))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
                if (targetThing != null)
                {
                    this.pawn.rotationTracker.FaceTarget(targetThing);
                }
            };
            doSpell.tickAction = delegate
            {
                if(targetThing == null && TargetA.Thing != null)
                {
                    targetThing = TargetA.Thing;
                }
                if (targetThing != null && (targetThing.DestroyedOrNull() || targetThing.Map == null))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
                age++;
                ticksLeftThisToil = duration - age;
                if (ability.golemAbilityDef.tickMote != null && Find.TickManager.TicksGame % ability.golemAbilityDef.tickMoteFrequency == 0)
                {
                    float angle = Rand.Range(0f, 360f);
                    if (ability.golemAbilityDef.tickMoteVelocityTowardsTarget != 0)
                    {
                        angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(pawn.DrawPos, targetThing.DrawPos)).ToAngleFlat();
                    }
                    TM_MoteMaker.ThrowGenericMote(ability.golemAbilityDef.tickMote, pawn.DrawPos, pawn.Map, ability.golemAbilityDef.tickMoteSize, ability.golemAbilityDef.tickMote.mote.solidTime, ability.golemAbilityDef.tickMote.mote.fadeInTime,
                        ability.golemAbilityDef.tickMote.mote.fadeOutTime, Rand.Range(-50, 50), ability.golemAbilityDef.tickMoteVelocityTowardsTarget, angle, Rand.Range(0, 360));
                }
                if(age > ability.golemAbilityDef.jobDuration && Find.TickManager.TicksGame % ability.golemAbilityDef.jobTicksBetweenBursts == 0)
                {
                    if (ability != null && ability.golemAbilityDef.jobEffect != null)
                    {
                        foreach(GolemAbilityEffect je in ability.golemAbilityDef.jobEffect)
                        {
                            if(je.CanDoEffect(targetThing))
                            {
                                je.DoEffect(pawn, targetThing);
                            }
                        }
                    }
                }

                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
            };
            doSpell.defaultCompleteMode = ToilCompleteMode.Never;
            doSpell.defaultDuration = this.duration;
            doSpell.AddFinishAction(delegate
            {
                //if (ability != null && ability.golemAbilityDef.jobEffect != null)
                //{
                //    for(int i = 0; i < ability.golemAbilityDef.jobEffect.Count; i++)
                //    {
                //        foreach (GolemAbilityEffect je in ability.golemAbilityDef.jobEffect)
                //        {
                //            if (je.CanDoEffect(targetThing))
                //            {
                //                je.DoEffect(pawn, targetThing);
                //            }
                //        }
                //    }
                //}
                //AssignXP();
            });
            yield return doSpell;
        }
    }
}