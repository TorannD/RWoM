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
    public class TMJobDriver_CastAbilityVerb : JobDriver_CastAbilityVerb
    { 

        private int duration;
        public AbilityContext context => job.count == 1 ? AbilityContext.Player : AbilityContext.AI;
        public Verb_UseAbility verb = null; //new Verb_UseAbility(); // = this.pawn.CurJob.verbToUse as Verb_UseAbility;
        private bool wildCheck = false;
        bool cooldownFlag = false;
        bool energyFlag = false;
        bool validCastFlag = false;
        private bool castStarted;

        public override void ExposeData()
        {
            //Scribe_Deep.Look<Verb_UseAbility>(ref verb, "verb");
            base.ExposeData();
            Scribe_Values.Look(ref castStarted, "tmCastStarted", false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);            
            Pawn targetPawn = null;

            verb = this.pawn.CurJob.verbToUse as Verb_UseAbility;
#if RW17
            // Reject forbidden/stale automatic targets before resource-dependent
            // setup (including Mana Storm side effects). Manual casts may still
            // walk into range through the existing positioning toil below.
            if (verb != null && !Compatibility.AbilityCastSafety.AllowsStart(verb, TargetA))
            {
                this.EndJobWith(JobCondition.Incompletable);
                yield break;
            }
#endif
            if(this.verb != null)
            {
                if (this.TargetA.HasThing && this.TargetA.Thing is Pawn && (!pawn.Position.InHorDistOf(this.TargetA.Cell, pawn.CurJob.verbToUse.verbProps.range) || !Verb.UseAbilityProps.canCastInMelee))
                {
                    Toil toil = Toils_Combat.GotoCastPosition(TargetIndex.A);
                    yield return toil;
                }                
                if (this.Context == AbilityContext.Player)
                {
                    Find.Targeter.targetingSource = this.verb;
                }
                
                if (this.TargetThingA != null)
                {
                    targetPawn = TargetThingA as Pawn;
                }

                cooldownFlag = this.verb.Ability.CooldownTicksLeft > 0;
                
                if (this.verb.Ability.Def is TMAbilityDef tmAbility)
                {
                    CompAbilityUserMight compMight = this.pawn.GetCompAbilityUserMight();
                    CompAbilityUserMagic compMagic = this.pawn.GetCompAbilityUserMagic();
                    //if (compMagic != null)
                    //{
                    //    compMagic.AIAbilityJob = null;
                    //}
                    //if (compMight != null && false)
                    //{
                    //    //compMight.AIAbilityJob = null;
                    //}
                    if (tmAbility.manaCost > 0 && pawn.story != null && pawn.story.traits != null && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        if (this.pawn.Map.gameConditionManager.ConditionIsActive(TorannMagicDefOf.TM_ManaStorm))
                        {
                            //DamageInfo dinfo2;
                            //BodyPartRecord vitalPart = null;
                            int amt = Mathf.RoundToInt(compMagic.ActualManaCost(tmAbility) * 100f);
                            //IEnumerable<BodyPartRecord> partSearch = pawn.def.race.body.AllParts;
                            //vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource));
                            //dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Arcane, amt, 10, 0, pawn as Thing, vitalPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
                            //dinfo2.SetAllowDamagePropagation(false);
                            //pawn.TakeDamage(dinfo2);
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
            }
            validCastFlag = cooldownFlag || energyFlag;

            if (targetPawn != null && (verb != null || this.pawn.CurJob.verbToUse != null))
            {
                //yield return Toils_Combat.CastVerb(TargetIndex.A, false);
                Toil combatToil = new Toil();
                //combatToil.FailOnDestroyedOrNull(TargetIndex.A);
                //combatToil.FailOnDespawnedOrNull(TargetIndex.A);
                //combatToil.FailOnDowned(TargetIndex.A);
                //CompAbilityUserMagic comp = this.pawn.GetCompAbilityUserMagic();                
                //JobDriver curDriver = this.pawn.jobs.curDriver;

                combatToil.initAction = delegate
                {
                    castStarted = false;
                    if (combatToil.actor.jobs.curJob.verbToUse == null) this.EndJobWith(JobCondition.Errored);
                    this.verb = combatToil.actor.jobs.curJob.verbToUse as Verb_UseAbility;

                    if (verb != null && verb.verbProps != null)
                    {
#if RW17
                        if (!Compatibility.AbilityCastSafety.CanBeginJobCast(verb, combatToil.actor.jobs.curJob.GetTarget(TargetIndex.A)))
                        { this.EndJobWith(JobCondition.Incompletable); return; }
#endif

                        try
                        {
                            this.duration = (int)((this.verb.verbProps.warmupTime * Compatibility.AbilityTiming.WarmupFor(this.verb) * 60) * this.pawn.GetStatValue(StatDefOf.AimingDelayFactor, false));
                        }
                        catch
                        {
                            this.duration = (int)(this.verb.verbProps.warmupTime * Compatibility.AbilityTiming.WarmupFor(this.verb) * 60);
                        }

                        if (this.pawn.RaceProps.Humanlike)
                        {
                            //if (this.pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                            //{
                            //    RemoveMimicAbility(verb);
                            //}

                            if (this.pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) && !validCastFlag)
                            {
                                PsionicEnergyCost(verb);
                            }

                            // Staged Spite pays when the projectile is successfully fired.
                            if (!validCastFlag && this.pawn.story.traits.HasTrait(TorannMagicDefOf.DeathKnight) &&
                                ClassProgression.DeathKnightMartialPath.Path(this.pawn) == null)
                            {
                                HateCost(verb);
                            }
                            if (validCastFlag)
                            {
                                if (context == AbilityContext.Player)
                                    Messages.Message("TM_InvalidAbility".Translate(this.pawn.LabelShort, this.verb.Ability.Def.label), MessageTypeDefOf.RejectInput, false);
                                this.EndJobWith(JobCondition.Incompletable);
                                return;
                            }
                        }

                        LocalTargetInfo target = combatToil.actor.jobs.curJob.GetTarget(TargetIndex.A);
                        if (target != null && !validCastFlag) 
                        {
                            castStarted = verb.TryStartCastOn(target, false, true);
                        }
                        if (!castStarted) { this.EndJobWith(JobCondition.Incompletable); return; }
                        using (IEnumerator<Hediff> enumerator = this.pawn.health.hediffSet.hediffs.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                Hediff rec = enumerator.Current;
                                if (rec.def == TorannMagicDefOf.TM_PossessionHD || rec.def == TorannMagicDefOf.TM_DisguiseHD || rec.def == TorannMagicDefOf.TM_DisguiseHD_I || rec.def == TorannMagicDefOf.TM_DisguiseHD_II || rec.def == TorannMagicDefOf.TM_DisguiseHD_III)
                                {
                                    this.pawn.health.RemoveHediff(rec);
                                }
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
                    if(this.pawn.Downed) EndJobWith(JobCondition.InterruptForced);
                    if (combatToil.actor.jobs.curJob.verbToUse == null) this.EndJobWith(JobCondition.Errored);
#if RW17
                    // A target destroyed or despawned mid-warmup leaves the vanilla
                    // warmup stance aiming at nothing (Stance_Warmup.AimDir NRE spam)
                    // and a cast job that never completes. End both immediately.
                    LocalTargetInfo warmupTarget = combatToil.actor.jobs.curJob.GetTarget(TargetIndex.A);
                    if (castStarted && warmupTarget.HasThing && (warmupTarget.Thing.Destroyed || !warmupTarget.Thing.Spawned))
                    {
                        this.pawn.stances?.SetStance(new Stance_Mobile());
                        this.EndJobWith(JobCondition.Incompletable);
                        return;
                    }
#endif
                    if (Find.TickManager.TicksGame % 12 == 0)
                    {
                        if (verb.Ability.Def == TorannMagicDefOf.TM_Artifact_TraitThief || verb.Ability.Def == TorannMagicDefOf.TM_Artifact_TraitInfuse)
                        {
                            float direction = Rand.Range(0, 360);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Arcane, pawn.DrawPos, pawn.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                        }
                        else
                        {
                            TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));
                        }
                    }

                    this.duration--;
                    if (!wildCheck && this.duration <= 6)
                    {
                        wildCheck = true;
                        if (this.pawn.story != null && this.pawn.story.traits != null && TorannMagic.ClassProgression.ClassIdentity.IsChaosMage(this.pawn) && Rand.Chance(.1f))
                        {
                            verb.Ability.PostAbilityAttempt();
                            TM_Action.DoWildSurge(this.pawn, this.pawn.GetCompAbilityUserMagic(), (MagicAbility)verb.Ability, (TMAbilityDef)verb.Ability.Def, TargetA);
                            EndJobWith(JobCondition.InterruptForced);
                        }
                    }
#if RW17
                    // Watchdog: a cast still running 30 in-game seconds past its
                    // intended duration is stuck (verb jammed, target unreachable).
                    // Free the pawn instead of freezing it in the casting stance.
                    if (castStarted && this.duration <= -1800)
                    {
                        this.pawn.stances?.SetStance(new Stance_Mobile());
                        this.EndJobWith(JobCondition.Incompletable);
                    }
#endif
                };
                combatToil.AddFinishAction(delegate
                {

                    if (castStarted && this.duration <= 5 && !this.pawn.DestroyedOrNull() && !this.pawn.Dead && !this.pawn.Downed)
                    {
                        //ShootLine shootLine;
                        //bool validTarg = verb.TryFindShootLineFromTo(pawn.Position, TargetLocA, out shootLine);
                        //bool inRange = (pawn.Position - TargetLocA).LengthHorizontal < verb.verbProps.range;
                        //if (inRange && validTarg)
                        //{
                        if (verb != null && verb.Ability != null && verb.Ability.Def is TMAbilityDef)
                        { 
                            TMAbilityDef tmad = (TMAbilityDef)(verb.Ability.Def);
                            if (tmad != null && tmad.relationsAdjustment != 0 && targetPawn.Faction != null && targetPawn.Faction != this.pawn.Faction && !targetPawn.Faction.HostileTo(this.pawn.Faction))
                            {
                                targetPawn.Faction.TryAffectGoodwillWith(this.pawn.Faction, tmad.relationsAdjustment, true, false, TorannMagicDefOf.TM_OffensiveMagic, null);
                            }
                        }
                        verb.Ability.PostAbilityAttempt();
                        this.pawn.ClearReservationsForJob(this.job);
                        //}
                    } 
                });
                //if (combatToil.actor.CurJob != this.job)
                //{
                //    curDriver.ReadyForNextToil();
                //}
                combatToil.defaultCompleteMode = ToilCompleteMode.FinishedBusy;
                this.pawn.ClearReservationsForJob(this.job);
                yield return combatToil;
                //Toil toil2 = new Toil()
                //{
                //    initAction = () =>
                //    {
                //        if (curJob.UseAbilityProps.isViolent)
                //        {
                //            JobDriver_CastAbilityVerb.CheckForAutoAttack(this.pawn);
                //        }
                //    },
                //    defaultCompleteMode = ToilCompleteMode.Instant
                //};
                //yield return toil2;
                //Toil toil1 = new Toil()
                //{
                //    initAction = () => curJob.Ability.PostAbilityAttempt(),
                //    defaultCompleteMode = ToilCompleteMode.Instant
                //};
                //yield return toil1;
            }
            else
            {

                if (verb != null && verb.verbProps != null && (pawn.Position - TargetLocA).LengthHorizontal < verb.verbProps.range)
                {
                    if (TargetLocA.IsValid && TargetLocA.InBoundsWithNullCheck(pawn.Map) && !TargetLocA.Fogged(pawn.Map))  //&& TargetLocA.Walkable(pawn.Map)
                    {
                        ShootLine shootLine;
                        bool validTarg = verb.TryFindShootLineFromTo(pawn.Position, TargetLocA, out shootLine);
                        if (validTarg)
                        {
                            //yield return Toils_Combat.CastVerb(TargetIndex.A, false);
                            //Toil toil2 = new Toil()
                            //{
                            //    initAction = () =>
                            //    {
                            //        if (curJob.UseAbilityProps.isViolent)
                            //        {
                            //            JobDriver_CastAbilityVerb.CheckForAutoAttack(this.pawn);
                            //        }

                            //    },
                            //    defaultCompleteMode = ToilCompleteMode.Instant
                            //};
                            //yield return toil2;
                            try
                            {
                                this.duration = (int)((this.verb.verbProps.warmupTime * Compatibility.AbilityTiming.WarmupFor(this.verb) * 60) * this.pawn.GetStatValue(StatDefOf.AimingDelayFactor, false));
                            }
                            catch
                            {
                                this.duration = (int)(this.verb.verbProps.warmupTime * Compatibility.AbilityTiming.WarmupFor(this.verb) * 60);
                            }
                            LocalTargetInfo target = base.TargetLocA;
                            Toil toil = new Toil();
                            toil.initAction = delegate
                            {
                                castStarted = false;

                                if (toil.actor.jobs.curJob.verbToUse == null) this.EndJobWith(JobCondition.Errored);
                                this.verb = toil.actor.jobs.curJob.verbToUse as Verb_UseAbility;
#if RW17
                                if (verb == null || !Compatibility.AbilityCastSafety.CanBeginJobCast(verb, target))
                                { this.EndJobWith(JobCondition.Incompletable); return; }
#endif
                                if (this.pawn.RaceProps.Humanlike)
                                {
                                    //if (this.pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                                    //{
                                    //    RemoveMimicAbility(verb);                                        
                                    //}

                                    if(this.pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic) && !validCastFlag)
                                    {
                                        PsionicEnergyCost(verb);
                                    }

                                    if (validCastFlag)
                                    {
                                        if (context == AbilityContext.Player)
                                            Messages.Message("TM_InvalidAbility".Translate(this.pawn.LabelShort, this.verb.Ability.Def.label), MessageTypeDefOf.RejectInput, false);
                                        this.EndJobWith(JobCondition.Incompletable);
                                        return;
                                    }

                                }

                                bool canFreeIntercept2 = false;
                                if (target != null && !validCastFlag)
                                {
                                    castStarted = verb.TryStartCastOn(target, false, canFreeIntercept2);
                                }
                                if (!castStarted) { this.EndJobWith(JobCondition.Incompletable); return; }
                                using (IEnumerator<Hediff> enumerator = this.pawn.health.hediffSet.hediffs.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        Hediff rec = enumerator.Current;
                                        if (rec.def == TorannMagicDefOf.TM_PossessionHD || rec.def == TorannMagicDefOf.TM_DisguiseHD || rec.def == TorannMagicDefOf.TM_DisguiseHD_I || rec.def == TorannMagicDefOf.TM_DisguiseHD_II || rec.def == TorannMagicDefOf.TM_DisguiseHD_III)
                                        {
                                            this.pawn.health.RemoveHediff(rec);
                                        }
                                    }
                                }
                            };
                            toil.tickAction = delegate
                            {

                                if (toil.actor.jobs.curJob.verbToUse == null) this.EndJobWith(JobCondition.Errored);
#if RW17
                                // Same mid-warmup target guard as the pawn-target branch:
                                // dead focus means AimDir NRE spam and a never-ending job.
                                if (castStarted && target.HasThing && (target.Thing.Destroyed || !target.Thing.Spawned))
                                {
                                    this.pawn.stances?.SetStance(new Stance_Mobile());
                                    this.EndJobWith(JobCondition.Incompletable);
                                    return;
                                }
#endif
                                if (Find.TickManager.TicksGame % 12 == 0)
                                {
                                    TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));
                                }
                                this.duration--;
                                if (!wildCheck && this.duration <= 6)
                                {
                                    wildCheck = true;
                                    if (this.pawn.story != null && this.pawn.story.traits != null && TorannMagic.ClassProgression.ClassIdentity.IsChaosMage(this.pawn) && Rand.Chance(.1f))
                                    {
                                        bool completeJob = TM_Action.DoWildSurge(this.pawn, this.pawn.GetCompAbilityUserMagic(), (MagicAbility)verb.Ability, (TMAbilityDef)verb.Ability.Def, TargetA);
                                        if (!completeJob)
                                        {
                                            verb.Ability.PostAbilityAttempt();
                                            EndJobWith(JobCondition.InterruptForced);
                                        }
                                    }
                                }
#if RW17
                                // Watchdog for the stuck-cast case, matching the
                                // pawn-target branch above.
                                if (castStarted && this.duration <= -1800)
                                {
                                    this.pawn.stances?.SetStance(new Stance_Mobile());
                                    this.EndJobWith(JobCondition.Incompletable);
                                }
#endif
                            };
                            toil.AddFinishAction(delegate
                            {                                
                                if (castStarted && this.duration <= 5 && !this.pawn.DestroyedOrNull() && !this.pawn.Dead && !this.pawn.Downed)
                                {
                                    verb.Ability.PostAbilityAttempt();
                                }
                                this.pawn.ClearReservationsForJob(this.job);
                            });
                            toil.defaultCompleteMode = ToilCompleteMode.FinishedBusy;
                            yield return toil;

                            //Toil toil1 = new Toil()
                            //{
                            //    initAction = () => curJob.Ability.PostAbilityAttempt(),
                            //    defaultCompleteMode = ToilCompleteMode.Instant
                            //};
                            //yield return toil1;
                        }
                        else
                        {
                            //No LoS
                            if (context == AbilityContext.Player)
                            {
                                Messages.Message("TM_OutOfLOS".Translate(
                                    pawn.LabelShort
                                ), MessageTypeDefOf.RejectInput);
                            }
                            pawn.ClearAllReservations(false);
                        }
                    }
                    else
                    {
                        pawn.ClearAllReservations(false);
                    }
                }
                else
                {
                    if(verb == null)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Errored, false, true);
                    }
                    else if (context == AbilityContext.Player)
                    {
                        //out of range
                        Messages.Message("TM_OutOfRange".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
            }
        }

        private void PsionicEnergyCost(Verb_UseAbility verbCast)
        {
            if (verbCast.AbilityProjectileDef.defName == "TM_Projectile_PsionicBlast")
            {
                HealthUtility.AdjustSeverity(this.pawn, HediffDef.Named("TM_PsionicHD"), -20f);
            }
            else if (verbCast.AbilityProjectileDef.defName == "Projectile_PsionicDash")
            {
                float sevReduct = ClassProgression.PsionicDashCast.PsiCost(this.pawn, TorannMagicDefOf.TM_PsionicDash);
                HealthUtility.AdjustSeverity(this.pawn, HediffDef.Named("TM_PsionicHD"), -sevReduct);
            }
            else if(verbCast.AbilityProjectileDef.defName == "Projectile_PsionicStorm")
            {
                float sevReduct = ClassProgression.PsionicDashCast.PsiCost(this.pawn, TorannMagicDefOf.TM_PsionicStorm);
                HealthUtility.AdjustSeverity(this.pawn, HediffDef.Named("TM_PsionicHD"), -sevReduct);
            }
        }

        private void HateCost(Verb_UseAbility verbCast)
        {
            Hediff hediff = null;
            for (int h = 0; h < this.pawn.health.hediffSet.hediffs.Count; h++)
            {
                if (this.pawn.health.hediffSet.hediffs[h].def.defName.Contains("TM_HateHD"))
                {
                    hediff = this.pawn.health.hediffSet.hediffs[h];
                }
            }
            if (hediff != null && verbCast.AbilityProjectileDef?.defName == "Projectile_Spite")
            {
                HealthUtility.AdjustSeverity(this.pawn, hediff.def, -20f);
            }            
        }

        private void RemoveMimicAbility(Verb_UseAbility verbCast)
        {
            CompAbilityUserMight mightComp = this.pawn.GetCompAbilityUserMight();
            CompAbilityUserMagic magicComp = this.pawn.GetCompAbilityUserMagic();
            if (mightComp.mimicAbility != null && mightComp.mimicAbility.MainVerb.verbClass == verbCast.verbProps.verbClass)
            {
                mightComp.RemovePawnAbility(mightComp.mimicAbility);
            }
            if (magicComp.mimicAbility != null && magicComp.mimicAbility.MainVerb.verbClass == verbCast.verbProps.verbClass)
            {
                magicComp.RemovePawnAbility(magicComp.mimicAbility);
            }
        }
    }
}
