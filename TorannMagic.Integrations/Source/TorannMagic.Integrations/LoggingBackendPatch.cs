using System;
using HarmonyLib;
using TorannMagic.Compatibility;

namespace TorannMagic.Integrations
{
    [HarmonyPatch(typeof(TM_Log), nameof(TM_Log.TryWriteExternal))]
    internal static class LoggingBackendPatch
    {
        static void Postfix(string level, string channel, string message, Exception exception, ref bool __result)
        {
            if (__result) return;
            try { __result = LoggingBackend.TryWrite(level, channel, message, exception); }
            catch { __result = false; }
        }
    }
}
