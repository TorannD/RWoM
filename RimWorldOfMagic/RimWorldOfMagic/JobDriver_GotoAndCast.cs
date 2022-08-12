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
    internal class JobDriver_GotoAndCast : JobDriver
    {
        private const TargetIndex caster = TargetIndex.B;

        int age = -1;
        int lastEffect = 0;
        int ticksTillEffects = 20;
        public int duration = 5;
        Vector3 positionBetween = Vector3.zero;
        public PawnAbility ability = null;
        Thing targetThing = null;

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
            if (TargetA.Thing != null)
            {
                targetThing = TargetA.Thing;
            }
            Toil gotoThing = new Toil()
            {
                initAction = () =>
                {
                    pawn.pather.StartPath(TargetA, PathEndMode.Touch);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoThing;
            Toil doSpell = new Toil();
            doSpell.initAction = delegate
            {
                if(ability != null)
                {
                    this.duration = (int)(ability.Def.MainVerb.warmupTime * 60 * this.pawn.GetStatValue(StatDefOf.AimingDelayFactor, false));
                }
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
                if (targetThing != null && (targetThing.DestroyedOrNull() || targetThing.Map == null))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }

                if (targetThing != null)
                {
                    this.pawn.rotationTracker.FaceTarget(targetThing);
                }
            };
            doSpell.tickAction = delegate
            {
                if (targetThing != null && (targetThing.DestroyedOrNull() || targetThing.Map == null))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }                
                age++;
                ticksLeftThisToil = duration - age;
                if (Find.TickManager.TicksGame % 12 == 0)
                {
                    TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));                    
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
                if (ability != null)
                {
                    if(ability.Def == TorannMagicDefOf.TM_Transmutate && targetThing != null)
                    {
                        bool flagRawResource = false;
                        bool flagStuffItem = false;
                        bool flagNoStuffItem = false;
                        bool flagNutrition = false;
                        bool flagCorpse = false;

                        TM_Calc.GetTransmutableThingFromCell(targetThing.Position, pawn, out flagRawResource, out flagStuffItem, out flagNoStuffItem, out flagNutrition, out flagCorpse);
                        TM_Action.DoTransmutate(pawn, targetThing, flagNoStuffItem, flagRawResource, flagStuffItem, flagNutrition, flagCorpse);                      
                    }
                    else if(ability.Def == TorannMagicDefOf.TM_RegrowLimb)
                    {
                        AbilityUser.SpawnThings tempThing = new SpawnThings();
                        tempThing.def = ThingDef.Named("SeedofRegrowth");
                        Verb_RegrowLimb.SingleSpawnLoop(tempThing, TargetA.Cell, pawn.Map);
                    }
                    ability.PostAbilityAttempt();
                }
                //AssignXP();
            });
            yield return doSpell;
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