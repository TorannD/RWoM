using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_SoL_CreateLight : Verb_UseAbility  
    {
        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    //out of range
                    validTarg = true;
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

            Pawn pawn = this.CasterPawn;
            Map map = this.CasterPawn.Map;

            if (pawn != null && !pawn.Downed)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if (comp != null && comp.SoL != null)
                {
                    if (!comp.SoL.IsGlowing)
                    {
                        comp.SoL.glowCenter = this.currentTarget.Cell;
                    }
                    comp.SoL.shouldGlow = true;                    
                }
            }
                
            this.burstShotsLeft = 0;
            return false;
        }
    }
}
