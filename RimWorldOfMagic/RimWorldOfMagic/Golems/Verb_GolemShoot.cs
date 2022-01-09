using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using RimWorld;
using TorannMagic.Golems;
using Verse.AI;

namespace TorannMagic.Golems
{
    public class Verb_GolemShoot : Verb_Shoot
    {
        public TMPawnGolem GolemPawn => CasterPawn as TMPawnGolem;
        public CompGolem GolemComp => GolemPawn.TryGetComp<CompGolem>();

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if ((root - targ.Cell).LengthHorizontal < this.verbProps.minRange)
            {
                return false;
            }
            return base.CanHitTargetFrom(root, targ);
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            if (verbProps.IsMeleeAttack)
            {
                Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                job.playerForced = true;
                Pawn pawn = target.Thing as Pawn;
                if (pawn != null)
                {
                    job.killIncappedTarget = pawn.Downed;
                }
                CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                if (target.Thing != null)
                {
                    GolemComp.threatTarget = target.Thing;
                }
            }
            else
            {
                float num = verbProps.EffectiveMinRange(target, CasterPawn);
                if ((float)CasterPawn.Position.DistanceToSquared(target.Cell) < num * num && CasterPawn.Position.AdjacentTo8WayOrInside(target.Cell))
                {
                    Messages.Message("MessageCantShootInMelee".Translate(), CasterPawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    Job job2 = JobMaker.MakeJob(verbProps.ai_IsWeapon ? TorannMagicDefOf.JobDriver_GolemAttackStatic : JobDefOf.UseVerbOnThing);
                    job2.verbToUse = this;
                    job2.targetA = target;
                    job2.endIfCantShootInMelee = true;
                    CasterPawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
                }
            }
        }

        protected override bool TryCastShot()
        {
            TMPawnGolem pg = this.CasterPawn as TMPawnGolem;
            if(pg != null)
            {
                pg.drawTickFlag = false;
            }
            return base.TryCastShot();
        }
    }
}
