namespace TorannMagic
{
    public class Verb_DismissGuardianSpirit : ClassProgression.Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(body,Ability);
            return body?.Spawned == true && currentTarget.Pawn == body &&
                ClassProgression.ShamanLifecycle.ReleaseGuardian(owner?.GetCompAbilityUserMagic());
        }
    }
}
