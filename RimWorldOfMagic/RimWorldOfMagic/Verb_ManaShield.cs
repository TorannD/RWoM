using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    class Verb_ManaShield : Verb_UseAbility  
    {

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            bool result = false;

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;

            if (pawn != null && !pawn.Downed)
            {
                bool hadManaShield = false;
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.def == TorannMagicDefOf.TM_ManaShieldHD)
                    {
                        pawn.health.RemoveHediff(hediff);
                        hadManaShield = true;
                        break;
                    }
                }
                if (!hadManaShield)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_ManaShieldHD, 1f);
                }
                TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                TM_MoteMaker.ThrowManaPuff(pawn.DrawPos, pawn.Map, .75f);
                TM_MoteMaker.ThrowSiphonMote(pawn.DrawPos, pawn.Map, 1f);
                result = true;
            }

            if (!result)
            {
                Log.Warning("failed to TryCastShot");
            }
            burstShotsLeft = 0;

            return result;
        }
    }
}
