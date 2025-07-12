using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;


namespace TorannMagic
{
    internal class JobDriver_Channel_Discord : JobDriver
    {
        private const TargetIndex building = TargetIndex.A;

        int age = -1;
        int discordFrequency = 20;
        int moteFrequency = 12;
        int duration = 2010;
        int headDamageCount = 0;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompAbilityUserMagic comp = this.pawn.GetCompAbilityUserMagic();
            Toil discordance = new Toil();
            Pawn target = this.TargetThingA as Pawn;
            discordance.initAction = delegate
            {
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }    
                if(target.DestroyedOrNull())
                {
                    this.EndJobWith(JobCondition.Errored);
                }
                if (target.Map == null)
                {
                    this.EndJobWith(JobCondition.Errored);
                }
                if (target.Dead)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }
                Map map = this.pawn.Map;
                ticksLeftThisToil = 10;
                headDamageCount = 0;                
            };
            discordance.tickAction = delegate
            {
                if(Find.TickManager.TicksGame % this.moteFrequency == 0)
                {
                    TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));
                    float angle = Rand.Range(0, 360);
                    ThingDef mote = TorannMagicDefOf.Mote_Psi_Grayscale;
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Grayscale, this.pawn.DrawPos, this.pawn.Map, Rand.Range(.25f, .6f), .1f, .05f, .05f, 0, Rand.Range(4f, 6f), angle, angle);
                }
                if (Find.TickManager.TicksGame % this.discordFrequency == 0)
                {
                    Vector3 headPos = target.DrawPos;
                    headPos.z += .3f;
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Bolt, headPos, target.Map, .6f, .5f, .1f, .5f, Rand.Range(-10, 10), Rand.Range(.4f, .6f), Rand.Range(-90, 90), Rand.Range(0, 360));
                    float manaCost = comp.ActualManaCost(TorannMagicDefOf.TM_Discord);
                    if(comp.Mana.CurLevel >= manaCost && !target.DestroyedOrNull() && !target.Dead && target.Map != null)
                    {
                        comp.Mana.CurLevel -= manaCost;
                        float ch = TM_Calc.GetSpellSuccessChance(this.pawn, target, true);
                        if (target.Faction == this.pawn.Faction)
                        {
                            HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_DiscordSafeHD, .4f * ch);
                            Hediff hd = target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DiscordHD);
                            if (hd != null && hd.Severity > 5)
                            {
                                if(Rand.Chance(.2f))
                                {
                                    TM_Action.DamageEntities(target, null, 6, DamageDefOf.Stun, this.pawn);
                                }
                             }
                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_DiscordHD, .4f * ch);
                            Hediff hd = target.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_DiscordHD);
                            if(hd != null && hd.Severity > 5)
                            {
                                BodyPartRecord bpr = target.health.hediffSet.GetBrain();
                                BodyPartRecord head = target.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == BodyPartDefOf.Head);
                                if (bpr != null)
                                {
                                    if (Rand.Chance(.22f * ch))
                                    {
                                        for (int i = 0; i < 2; i++)
                                        {
                                            TM_MoteMaker.ThrowBloodSquirt(headPos, target.Map, Rand.Range(.6f, 1f));
                                        }
                                        TM_Action.DamageEntities(target, bpr, Rand.Range(1, 3), 2f, TMDamageDefOf.DamageDefOf.TM_DistortionDD, this.pawn);
                                        headDamageCount++;
                                    }
                                    else if(Rand.Chance((.07f * ch)+(headDamageCount *.3f)) && head != null)
                                    {
                                        for (int i = 0; i < 3; i++)
                                        {
                                            TM_MoteMaker.ThrowBloodSquirt(headPos, target.Map, Rand.Range(.6f, 1f));
                                        }
                                        for(int i = 0; i < 3; i++)
                                        {
                                            float moteSize = Rand.Range(.5f, .8f);
                                            float solidTime = Rand.Range(.6f, .8f);
                                            float fadeOutTime = Rand.Range(.2f, .4f);
                                            float velocity = Rand.Range(1.5f, 2.5f);
                                            float velocityAngle = Rand.Range(0f, 360f);
                                            for (int j = 0; j < 3; j++)
                                            {
                                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, headPos, target.Map, moteSize - (.1f *j), solidTime + (.1f * j), 0f, fadeOutTime + (.05f *j), Rand.Range(-50, 50), velocity + (.5f*j), velocityAngle, Rand.Range(0, 360));
                                            }
                                        }
                                        TM_Action.DamageEntities(target, head, Rand.Range(50, 80), 2f, TMDamageDefOf.DamageDefOf.TM_DistortionDD, this.pawn);
                                    }
                                }
                            }
                        }
                        if(target.Dead)
                        {
                            this.age = this.duration;
                        }
                    }
                    else
                    {
                        this.age = this.duration;
                    }
                }                
                age++;               
                ticksLeftThisToil = Mathf.RoundToInt(((float)(duration - age) / (float)duration)*100f);
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }                              
            };
            discordance.defaultCompleteMode = ToilCompleteMode.Delay;
            discordance.defaultDuration = this.duration;
            discordance.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Downed)
                {
                    return 1f;
                }
                return 1f - (float)discordance.actor.jobs.curDriver.ticksLeftThisToil/100;

            }, false, 0f);
            discordance.AddFinishAction(delegate
            {

            });
            yield return discordance;
        }
    }
}
