using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Golems
{
    public class JobDriver_GolemAttackStatic : JobDriver
    {
        private bool startedIncapacitated;
        private int numAttacksMade;
        public Verb attackVerb;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated", defaultValue: false);
            Scribe_Values.Look(ref numAttacksMade, "numAttacksMade", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            Toil init = new Toil();
            init.initAction = delegate
            {
                Pawn pawn2 = base.TargetThingA as Pawn;                
                if (pawn2 != null)
                {
                    startedIncapacitated = pawn2.Downed;
                }
                if(attackVerb == null)
                {
                    TMPawnGolem gp = pawn as TMPawnGolem;
                    attackVerb = gp.activeVerb;
                }
                if(this.job.verbToUse != null)
                {
                    attackVerb = this.job.verbToUse;
                }
                pawn.pather.StopDead();
            };
            init.tickAction = delegate
            {
                if (!base.TargetA.IsValid)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
                else
                {
                    if (base.TargetA.HasThing)
                    {
                        Pawn pawn = base.TargetA.Thing as Pawn;
                        if (base.TargetA.Thing.Destroyed || (pawn != null && !startedIncapacitated && pawn.Downed) || (pawn != null && pawn.IsInvisible()))
                        {
                            EndJobWith(JobCondition.Succeeded);
                            return;
                        }
                    }
                    if (numAttacksMade >= job.maxNumStaticAttacks && !base.pawn.stances.FullBodyBusy)
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }
                    else if (TryStartAttack(base.pawn, base.TargetA, attackVerb))
                    {
                        numAttacksMade++;
                    }
                    else if (!base.pawn.stances.FullBodyBusy)
                    {
                        if (job.endIfCantShootTargetFromCurPos && (attackVerb == null || !attackVerb.CanHitTargetFrom(base.pawn.Position, base.TargetA)))
                        {
                            EndJobWith(JobCondition.Incompletable);
                        }
                        else if (job.endIfCantShootInMelee)
                        {
                            if (attackVerb == null)
                            {
                                EndJobWith(JobCondition.Incompletable);
                            }
                            else
                            {
                                float num = attackVerb.verbProps.EffectiveMinRange(base.TargetA, base.pawn);
                                if ((float)base.pawn.Position.DistanceToSquared(base.TargetA.Cell) < num * num && base.pawn.Position.AdjacentTo8WayOrInside(base.TargetA.Cell))
                                {
                                    EndJobWith(JobCondition.Incompletable);
                                }
                            }
                        }
                    }
                }
            };
            init.defaultCompleteMode = ToilCompleteMode.Never;
            init.activeSkill = (() => Toils_Combat.GetActiveSkillForToil(init));
            yield return init;
        }

        bool TryStartAttack(Pawn pawn, LocalTargetInfo targ, Verb verb)
        {            
            if(pawn.stances.FullBodyBusy)
            {
                return false;
            }
            TMPawnGolem pg = pawn as TMPawnGolem;
            Verb v = verb != null ? verb : pg.GetBestVerb;     
            if((v.LastShotTick + (v.verbProps.defaultCooldownTime * 60)) >= Find.TickManager.TicksGame)
            {
                v = pg.GetBestVerb;
            }
            if (v != null)
            {
                Log.Message("starting attack with verb " + v.verbProps.verbClass.ToString());
                attackVerb = v;                
                pg.Golem.Energy.SubtractEnergy(v.verbProps.consumeFuelPerShot);
                pg.drawTickFlag = v.verbProps.consumeFuelPerShot > 0;
                return v.TryStartCastOn(targ);
            }
            return false;
        }
    }
}
