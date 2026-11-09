using AbilityUser;

namespace TorannMagic
{
    class Verb_Recall : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var pawn = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(pawn, Ability);
            return pawn != null && !pawn.Downed && TM_Action.TryRecall(pawn, owner?.GetCompAbilityUserMagic(), false);
        }
    }
}
