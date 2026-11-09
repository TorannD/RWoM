using System;
using RimWorld;
using Verse;

namespace TorannMagic
{
    /// <summary>
    /// Initializes the Multiplayer compatibility layer after ALL mod classes have
    /// been constructed — by that time the Multiplayer fork's API is initialized
    /// and MP.RegisterSyncMethod targets the real implementation, not the Dummy.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class TMPCompatStartup
    {
        static TMPCompatStartup()
        {
            if (TMPCompatAdapter.MultiplayerActive())
            {
                // Never let a compatibility failure break the mod constructor chain.
                try
                {
                    new HarmonyLib.Harmony("huginn.rwom.integrations.multiplayer").PatchAll(typeof(TMPCompatStartup).Assembly);
                    TMPCompatAdapter.Setup();
                }
                catch (Exception ex)
                {
                    Log.Error("[TMPCompat] setup failed: " + ex);
                }
            }
        }
    }
}
