using System;
using Verse;

namespace TorannMagic.Integrations
{
    // The original AddToArray calls discarded their results: they warmed these
    // textures but never changed the cloak lookup. Preserve that behavior here.
    internal static class LegacyBodyTextures
    {
        internal static void Load(Action<string> load)
        {
            if (!ModsConfig.IsActive("ssulunge.BBBodySupport")) return;
            load("Equipment/demonlordcloakc_FemaleBB_south");
            load("Equipment/opencloak_FemaleBB_south");
        }
    }

    [StaticConstructorOnStartup]
    internal static class LegacyBodyTextureStartup
    {
        static LegacyBodyTextureStartup()
        {
            LegacyBodyTextures.Load(path => MaterialPool.MatFrom(path));
        }
    }
}
