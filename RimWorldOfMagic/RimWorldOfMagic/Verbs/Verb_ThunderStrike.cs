using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_ThunderStrike : Verb_UseAbility
    {

        bool flag10;
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

            if (this.CasterPawn.equipment.Primary == null)
            {
                base.TryCastShot();
                return true;
            }
            else
            {
                Messages.Message("MustBeUnarmed".Translate(
                    this.CasterPawn.LabelCap,
                    this.verbProps.label
                ), MessageTypeDefOf.RejectInput);
                return false;
            }

            this.burstShotsLeft = 0;
            this.PostCastShot(flag10, out flag10);
            return flag10;
        }        
    }
}
