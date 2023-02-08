using HarmonyLib;
using Verse;

namespace TorannMagic.Utils
{
    /*
     * This class contains global variables that are needing to be accessed on a per tick basis.
     * When adding constants to this class, make sure you actually set the variable each tick by adding it to the
     * harmony patch (Prefix) below.
     */
    public static class TM_TickManager
    {
        // Constants
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
    class TM_TickManager__DoSingleTick__Prefix
    {
        /*
         * Only constants should go in this prefix (even they should live in the postfix, but it's a lot more work
         * to move everything from CompTick to this TickManager, so here it stays for now to guarantee they are
         * available)
         */
        static void Prefix()
        {
            // Repetitive modulo operations should be stored so we don't have to calculate more than necessary.
            TM_TickManager.tickMod6 = Find.TickManager.TicksGame % 6;
            TM_TickManager.tickMod20 = Find.TickManager.TicksGame % 20;
            TM_TickManager.tickMod30 = Find.TickManager.TicksGame % 30;
            TM_TickManager.tickMod60 = Find.TickManager.TicksGame % 60;
            TM_TickManager.tickMod67 = Find.TickManager.TicksGame % 67;
            TM_TickManager.tickMod300 = Find.TickManager.TicksGame % 300;
            TM_TickManager.tickMod600 = Find.TickManager.TicksGame % 600;
            TM_TickManager.tickMod2000 = Find.TickManager.TicksGame % 2000;
            TM_TickManager.tickMod2500 = Find.TickManager.TicksGame % 2500;
        }
    }
}
