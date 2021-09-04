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

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            verb = this.pawn.CurJob.verbToUse as Verb_UseAbility;
            Find.Targeter.targetingSource = verb;            
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            Toil toil1 = new Toil()
            {
                initAction = () => {
                    pawn.pather.StopDead();
                    //if (curJob.UseAbilityProps.isViolent)
                    //{
                    //    JobDriver_CastAbilityVerb.CheckForAutoAttack(this.pawn);
                    //}
                },
                defaultCompleteMode = ToilCompleteMode.Instant                
            };
            yield return toil1;
            Toil toil = new Toil()
            {
                initAction = () => verb.Ability.PostAbilityAttempt(),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return toil;
            
        }
        
    }
}
