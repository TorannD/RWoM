using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_HarvestPassion : Verb_UseAbility
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
            Map map = base.CasterPawn.Map;
            Pawn hitPawn = (Pawn)this.currentTarget;
            Pawn caster = base.CasterPawn;

            bool flag = hitPawn != null && !hitPawn.Dead && !hitPawn.RaceProps.Animal && hitPawn.skills != null && hitPawn.health != null && hitPawn.health.hediffSet != null;
            if (flag && !TM_Calc.IsUndead(hitPawn))
            {
                if (caster.Inspired)
                {
                    HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_HarvestPassionHD, .5f);
                    hitPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HarvestPassionHD).TryGetComp<HediffComp_HarvestPassion>().caster = caster;
                    caster.mindState.inspirationHandler.EndInspiration(caster.Inspiration);
                    if(!hitPawn.HostileTo(caster.Faction))
                    {
                        
                    }
                }
                else
                {
                    Messages.Message("TM_MustHaveInspiration".Translate(
                    this.CasterPawn.LabelShort,
                    this.Ability.Def.label
                ), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("TM_InvalidTarget".Translate(
                    this.CasterPawn.LabelShort,
                    this.Ability.Def.label
                ), MessageTypeDefOf.RejectInput);
            }
            this.PostCastShot(flag, out flag);
            return false;
        }
    }
}
