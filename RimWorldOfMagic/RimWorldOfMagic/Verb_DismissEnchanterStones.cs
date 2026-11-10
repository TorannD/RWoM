using System.Linq;
using TorannMagic.ClassProgression;
using Verse;

namespace TorannMagic
{
    public class Verb_DismissEnchanterStones : Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            var body = CasterPawn;
            var magic = SpiritRebirthCasting.SkillOwner(body, Ability)?.GetCompAbilityUserMagic();
            if (currentTarget.Pawn != body || magic == null) return false;
            var oldest = magic.enchanterStones?.FirstOrDefault(s => !s.DestroyedOrNull());
            if (oldest == null) return false;
            oldest.Destroy(DestroyMode.Vanish);
            magic.SyncEnchanterStoneControl();
            return true;
        }
    }
}
