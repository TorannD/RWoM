using TorannMagic.ClassProgression;

namespace TorannMagic
{
    class Verb_ShadowCall : Verb_SpiritTargetable
    {
        protected override bool TryCastShot()
        {
            burstShotsLeft = 0;
            return Ability is MagicAbility power && DemonBondTravel.Cast(CasterPawn, power);
        }
    }
}
