using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic
{
    public class Verb_DismissMinion : ClassProgression.Verb_SpiritTargetable
    {
        internal static TMPawnSummoned OldestOwned(CompAbilityUserMagic owner, bool allowMissingOwner = false)
            => owner?.summonedMinions?.OfType<TMPawnSummoned>().FirstOrDefault(p => !p.Dead && !p.Destroyed &&
                p.Temporary && p.TicksLeft > 1 && (p.Spawner == owner.Pawn || (allowMissingOwner && p.Spawner == null)));

        protected override bool TryCastShot()
        {
            var owner = CasterPawn?.GetCompAbilityUserMagic();
            if (currentTarget.Pawn == CasterPawn && owner?.IsMagicUser == true && !Dismiss(owner, true))
                Messages.Message("TM_NoMinionToDismiss".Translate(CasterPawn.LabelShort), MessageTypeDefOf.RejectInput);
            return true;
        }

        protected override bool ApplyHostedEffect()
        {
            if (currentTarget.Pawn != CasterPawn || !(Ability is MagicAbility power)) return false;
            return Dismiss(power.MagicUser, false);
        }

        private static bool Dismiss(CompAbilityUserMagic owner, bool allowMissingOwner)
        {
            var minion = OldestOwned(owner, allowMissingOwner);
            if (minion == null) return false;
            minion.TicksLeft = 1;
            return true;
        }
    }
}
