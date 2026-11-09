using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthBodyEffects
    {
        // A marker belongs to the original pawn, never its host. Even an unsupported
        // saved form must not reactivate the former body's class effects.
        internal static bool SuppressFormerBodyEffects(Pawn pawn)
            => ClassIdentity.Component(pawn)?.SpiritRebirth != null;
    }
}
