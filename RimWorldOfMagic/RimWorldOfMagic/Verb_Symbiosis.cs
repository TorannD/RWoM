using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;


namespace TorannMagic
{
    public class Verb_Symbiosis : ClassProgression.Verb_SpiritTargetable
    {

        bool validTarg;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
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

        internal static bool CanLink(Pawn body, Pawn other)
        {
            return body != null && !body.Dead && other != null && other != body && !other.Dead &&
                body.health?.hediffSet != null && other.health?.hediffSet != null &&
                other.RaceProps?.Humanlike == true && !TM_Calc.IsUndead(other) && other.Faction == body.Faction &&
                !body.health.hediffSet.HasHediff(TorannMagicDefOf.TM_OutOfBodyHD) &&
                !other.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SymbiosisHD);
        }

        protected override bool TryCastShot()
        {
            bool applied = ApplyHostedEffect();
            if (!applied)
            {
                var other = currentTarget.Pawn;
                Messages.Message(other?.health?.hediffSet?.HasHediff(TorannMagicDefOf.TM_SymbiosisHD) == true ?
                    "TM_AlreadyHasSymbiote".Translate(CasterPawn.LabelShort) : "TM_InvalidTarget".Translate(CasterPawn.LabelShort, Ability.Def.label),
                    MessageTypeDefOf.RejectInput);
            }
            PostCastShot(applied,out var fired);
            return fired;
        }

        protected override bool ApplyHostedEffect()
        {
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            var other = currentTarget.Pawn;
            if (owner == null || !CanLink(body,other)) return false;
            var source = HediffMaker.MakeHediff(TorannMagicDefOf.TM_OutOfBodyHD,body);
            var benefit = HediffMaker.MakeHediff(TorannMagicDefOf.TM_SymbiosisHD,other);
            var sourceLink = source.TryGetComp<HediffComp_SymbiosisCaster>();
            var targetLink = benefit.TryGetComp<HediffComp_SymbiosisHost>();
            if (sourceLink == null || targetLink == null) return false;
            sourceLink.symbiosisHost = other;
            sourceLink.originalId = owner != body ? owner.thingIDNumber : 0;
            targetLink.symbiote = body;
            source.Severity = .5f;
            benefit.Severity = .5f + TM_Calc.GetSkillPowerLevel(owner, TorannMagicDefOf.TM_Symbiosis);
            bool linked = false;
            try
            {
                other.health.AddHediff(benefit);
                body.health.AddHediff(source);
                linked = body.health.hediffSet.hediffs.Contains(source) && other.health.hediffSet.hediffs.Contains(benefit);
                return linked;
            }
            finally
            {
                if (!linked)
                {
                    if (other.health.hediffSet.hediffs.Contains(benefit)) other.health.RemoveHediff(benefit);
                    if (body.health.hediffSet.hediffs.Contains(source)) body.health.RemoveHediff(source);
                }
            }
        }
    }
}
