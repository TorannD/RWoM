using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_DispelLivingWall : Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            bool result = false;
            PostCastShot(result,out result);
            return result;
        }

        protected override bool ApplyHostedEffect()
        {
            var owner = SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            var wall = owner?.GetCompAbilityUserMagic()?.livingWall;
            if (currentTarget.Pawn != CasterPawn || wall?.Spawned != true || wall.CasterPawn != owner) return false;
            wall.Destroy(DestroyMode.Vanish);
            return true;
        }
    }
}
