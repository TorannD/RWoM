using System.Runtime.CompilerServices;

namespace TorannMagic
{
    // Binary-compatible public entry points. External MP implementation and API
    // references live exclusively in the optional Integrations adapter.
    public static class TMPCompat
    {
        public const string MultiplayerPackageId = "zetrith.multiplayer";
        public static readonly string[] MultiplayerPackageIds = { MultiplayerPackageId, "rwmt.multiplayer" };
        public static bool Initialized { [MethodImpl(MethodImplOptions.NoInlining)] get => false; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool MultiplayerActive() => false;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Setup() { }
    }
}
