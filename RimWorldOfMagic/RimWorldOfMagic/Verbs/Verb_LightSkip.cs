using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using Verse.Sound;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_LightSkip : Verb_UseAbility  
    {

        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt), Light Skip requires unroofed destination
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = !targ.Cell.Roofed(base.CasterPawn.Map);
                }
                else
                {
                    //out of range
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
            if (map != null && !pawn.Position.Roofed(map))
            {
                base.TryCastShot();
            }
            else
            {
                Messages.Message("TM_CannotCastUnderRoof".Translate(pawn.LabelShort, Ability.Def.label), MessageTypeDefOf.NegativeEvent);
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Light Skip: " + StringsToTranslate.AU_CastFailure, -1f);
            }
            return result;
        }
    }
}
