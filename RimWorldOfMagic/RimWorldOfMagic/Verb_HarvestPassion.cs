using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_HarvestPassion : ClassProgression.Verb_SpiritTargetable
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

        internal static bool CanHarvest(Pawn body, Pawn owner, Pawn target)
        {
            return body?.Spawned == true && body.Inspired && owner?.skills != null && target?.Spawned == true &&
                target != body && target != owner && target.Map == body.Map && !target.Dead && target.RaceProps.Humanlike &&
                !TM_Calc.IsUndead(target) && target.health?.hediffSet != null &&
                !target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HarvestPassionHD) &&
                target.skills?.skills?.Any(skill => skill.passion > owner.skills.GetSkill(skill.def).passion) == true;
        }

        protected override bool TryCastShot()
        {
            bool applied = ApplyHostedEffect();
            if (!applied)
                Messages.Message(CasterPawn.Inspired ? "TM_InvalidTarget".Translate(CasterPawn.LabelShort,Ability.Def.label) :
                    "TM_MustHaveInspiration".Translate(CasterPawn.LabelShort,Ability.Def.label), MessageTypeDefOf.RejectInput);
            PostCastShot(applied,out var fired);
            return fired;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body, Ability);
            var target = currentTarget.Pawn;
            if (!CanHarvest(body, owner, target)) return false;
            var effect = HediffMaker.MakeHediff(TorannMagicDefOf.TM_HarvestPassionHD, target);
            var harvest = effect.TryGetComp<HediffComp_HarvestPassion>();
            if (harvest == null) return false;
            harvest.caster = body;
            harvest.originalId = owner != body ? owner.thingIDNumber : 0;
            effect.Severity = .5f;
            target.health.AddHediff(effect);
            return target.health.hediffSet.hediffs.Contains(effect);
        }
    }
}
