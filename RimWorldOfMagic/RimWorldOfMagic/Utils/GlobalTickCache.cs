using HarmonyLib;
using TorannMagic.ModOptions;
using Verse;

namespace TorannMagic.Utils
{
    /*
     * This class contains global variables that are needing to be accessed on a per tick basis.
     * When adding to this class, make sure you actually set the variable each tick by adding it to the harmony patch
     * below.
     */
    public static class GlobalTickCache
    {
        public static SettingsRef SettingsRef = new SettingsRef();
    }

    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    class GlobalTickCache__TickManager_DoSingleTick__Patch
    {
        static void Prefix()
        {
            GlobalTickCache.SettingsRef = new SettingsRef();
        }
    }
}
