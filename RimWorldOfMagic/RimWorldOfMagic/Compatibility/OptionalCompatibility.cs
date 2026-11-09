using System.Runtime.CompilerServices;
using Verse;

namespace TorannMagic.Compatibility
{
    public static class OptionalCompatibility
    {
        // Compatibility facade identifiers are not proof of support: Integrations
        // supplies their policies only when the corresponding provider is active.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsEnabled(string feature) => false;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NotifyLegacyBirth(Pawn pawn) { }
    }
}
