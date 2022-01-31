using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
    public class TMJobDriver_CastAbilitySelf : JobDriver_CastAbilityVerb
    {
        public Verb_UseAbility verb = new Verb_UseAbility();
        bool cooldownFlag = false;
        bool energyFlag = false;
        bool validCastFlag = false;
        private bool wildCheck = false;
        private int duration;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            verb = this.pawn.CurJob.verbToUse as Verb_UseAbility;
           
            //if (this.Context == AbilityContext.Player)
            //{
            Find.Targeter.targetingSource = verb;
            //}
            if (this.verb.Ability.Def is TMAbilityDef)
            {
                TMAbilityDef tmAbility = (TMAbilityDef)(this.verb.Ability.Def);
                CompAbilityUserMight compMight = this.pawn.TryGetComp<CompAbilityUserMight>();
                CompAbilityUserMagic compMagic = this.pawn.TryGetComp<CompAbilityUserMagic>();
                if (tmAbility.manaCost > 0 && pawn.story != null && pawn.story.traits != null && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    if (this.pawn.Map.gameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
                    {
                        int amt = Mathf.RoundToInt(compMagic.ActualManaCost(tmAbility) * 100f);
                        if (amt > 5)
                        {
                            this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshFlash(this.Map, this.pawn.Position, TM_MatPool.blackLightning, TMDamageDefOf.DamageDefOf.TM_Arcane, this.pawn, amt, Mathf.Clamp((float)amt / 5f, 1f, 5f)));
                        }
                    }
                    if (compMagic != null && compMagic.Mana != null)
                    {
                        if (compMagic.ActualManaCost(tmAbility) > compMagic.Mana.CurLevel)
                        {
                            energyFlag = true;
                        }
                    }
                    else
                    {
                        energyFlag = true;
                    }
                }
                if (tmAbility.staminaCost > 0)
                {
                    if (compMight != null && compMight.Stamina != null)
                    {
                        if (compMight.ActualStaminaCost(tmAbility) > compMight.Stamina.CurLevel)
                        {
                            energyFlag = true;
                        }
                    }
                    else
                    {
                        energyFlag = true;
                    }
                }
            }
            validCastFlag = cooldownFlag || energyFlag;
            //yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            Toil toil1 = new Toil()
            {
                initAction = () => {
                    pawn.pather.StopDead();
                },
                defaultCompleteMode = ToilCompleteMode.Instant                
            };
            yield return toil1;
            //
            Toil combatToil = new Toil();
            combatToil.initAction = delegate
            {
                this.verb = combatToil.actor.jobs.curJob.verbToUse as Verb_UseAbility;
                if (verb != null && verb.verbProps != null)
                {
                    this.duration = (int)((this.verb.verbProps.warmupTime * 60) * this.pawn.GetStatValue(StatDefOf.AimingDelayFactor, false));
                    LocalTargetInfo target = combatToil.actor.jobs.curJob.GetTarget(TargetIndex.A);
                    if (target != null && !validCastFlag)
                    {
                        if (this.pawn.IsColonist)
                        {
                            verb.TryStartCastOn(target, false, true);
                        }
                        else
                        {
                            this.duration = 0;
                            verb.WarmupComplete();
                        }
                    }
                }
                else
                {
                    this.EndJobWith(JobCondition.Errored);
                }
            };
            combatToil.tickAction = delegate
            {
                if (this.pawn.Downed)
                {
                    EndJobWith(JobCondition.InterruptForced);
                }
                if (Find.TickManager.TicksGame % 12 == 0)
                {
                    TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));                    
                }

                this.duration--;
                if (!wildCheck && this.duration <= 6)
                {
                    wildCheck = true;
                    if (this.pawn.story != null && this.pawn.story.traits != null && this.pawn.story.traits.HasTrait(TorannMagicDefOf.ChaosMage) && Rand.Chance(.1f))
                    {
                        verb.Ability.PostAbilityAttempt();
                        TM_Action.DoWildSurge(this.pawn, this.pawn.GetComp<CompAbilityUserMagic>(), (MagicAbility)verb.Ability, (TMAbilityDef)verb.Ability.Def, TargetA);
                        EndJobWith(JobCondition.InterruptForced);
                    }
                }
            };
            combatToil.AddFinishAction(delegate
            {
                if (this.duration <= 5 && !this.pawn.DestroyedOrNull() && !this.pawn.Dead && !this.pawn.Downed)
                {
                    verb.Ability.PostAbilityAttempt();
                    this.pawn.ClearReservationsForJob(this.job);
                }
            });
            combatToil.defaultCompleteMode = ToilCompleteMode.FinishedBusy;
            this.pawn.ClearReservationsForJob(this.job);
            yield return combatToil;
            //
            //Toil toil = new Toil()
            //{
            //    initAction = () => verb.Ability.PostAbilityAttempt(),
            //    defaultCompleteMode = ToilCompleteMode.Instant
            //};
            //yield return toil;
            
        }
        
    }
}
