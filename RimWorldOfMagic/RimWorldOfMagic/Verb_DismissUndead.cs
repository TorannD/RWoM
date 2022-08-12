using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_DismissUndead : Verb_UseAbility
    {
        bool validTarg;
        //Used for non-unique abilities that can be used with shieldbelt
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
            Pawn caster = base.CasterPawn;
            Pawn target = this.currentTarget.Thing as Pawn;
            
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            if (comp.IsMagicUser && target != null)
            {
                if (target.RaceProps.Humanlike)
                {
                    if (target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadHD) && target.Faction == caster.Faction)
                    {
                        target.Kill(null, null);
                    }
                }
                else if (target.RaceProps.Animal)
                {
                    if (target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadAnimalHD) && target.Faction == caster.Faction)
                    {
                        target.Kill(null, null);
                    }
                }                
                else
                {
                    Messages.Message("TM_NoValidUndeadToDismiss".Translate(), MessageTypeDefOf.RejectInput);
                }
                
                if (!target.Dead && target.story != null && target.story.traits != null && target.story.traits.HasTrait(TorannMagicDefOf.Undead) && target.Faction == caster.Faction)
                {
                    target.Kill(null, null);
                }
            }
            return true;
        }
    }
}
