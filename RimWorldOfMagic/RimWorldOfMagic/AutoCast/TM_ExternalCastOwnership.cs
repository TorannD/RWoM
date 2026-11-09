using Verse;

namespace TorannMagic.AutoCast
{
    // External ownership is separate from combat evidence. Content knows no vendor types.
    public static class TM_ExternalCastOwnership
    {
        public static bool IsCasting(Pawn pawn) => CoreBridge.IsExternallyCasting(pawn);
    }
}
