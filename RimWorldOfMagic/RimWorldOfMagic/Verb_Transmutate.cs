using RimWorld;
using Verse;
using TorannMagic.ClassProgression;

namespace TorannMagic
{
    public class Verb_Transmutate : Verb_SpiritTargetable
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(CasterPawn.Map)
                && !targ.Cell.Fogged(CasterPawn.Map) && (root - targ.Cell).LengthHorizontal < verbProps.range;
        }

        internal static Thing Source(Pawn body, Pawn owner, LocalTargetInfo target)
        {
            return TM_Calc.FindTransmutationSource(target.Cell, body, owner, target.Thing,
                out _, out _, out _, out _, out _, true);
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            burstShotsLeft = 0;
            return false;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            if (owner == null) return false;
            var source = TM_Calc.FindTransmutationSource(currentTarget.Cell, CasterPawn, owner, currentTarget.Thing,
                out var raw, out var stuff, out var noStuff, out var nutrition, out var corpse, true);
            if (source == null)
            {
                Messages.Message("TM_NoThingToTransmutate".Translate(CasterPawn.LabelShort), MessageTypeDefOf.RejectInput);
                return false;
            }
            var previous = EnchanterTrials.BeginTransmutation(Ability, source);
            try { return TM_Action.TransmutateFrom(CasterPawn, owner, source, noStuff, raw, stuff, nutrition, corpse); }
            finally { EnchanterTrials.EndTransmutation(previous); }
        }
    }
}
