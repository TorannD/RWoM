using HarmonyLib;
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
        public static int tickMod6;
        public static int tickMod20;
        public static int tickMod30;
        public static int tickMod60;
        public static int tickMod67;
        public static int tickMod300;
        public static int tickMod600;
        public static int tickMod2000;
        public static int tickMod2500;
    }

    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    class GlobalTickCache__TickManager_DoSingleTick__Patch
    {
        static void Prefix()
        {
            GlobalTickCache.tickMod6 = Find.TickManager.TicksGame % 6;
            GlobalTickCache.tickMod20 = Find.TickManager.TicksGame % 20;
            GlobalTickCache.tickMod30 = Find.TickManager.TicksGame % 30;
            GlobalTickCache.tickMod60 = Find.TickManager.TicksGame % 60;
            GlobalTickCache.tickMod67 = Find.TickManager.TicksGame % 67;
            GlobalTickCache.tickMod300 = Find.TickManager.TicksGame % 300;
            GlobalTickCache.tickMod600 = Find.TickManager.TicksGame % 600;
            GlobalTickCache.tickMod2000 = Find.TickManager.TicksGame % 2000;
            GlobalTickCache.tickMod2500 = Find.TickManager.TicksGame % 2500;
        }
    }
}
