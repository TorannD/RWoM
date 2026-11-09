using System;
using System.Runtime.CompilerServices;

namespace TorannMagic.Compatibility
{
    /// <summary>
    /// Content logging facade with optional external sink and Verse fallback.
    /// Use warnings/errors for failures, not successful casts; Trace is developer-only.
    /// </summary>
    public static class TM_Log
    {
        private static Action<string, string> fallback = WriteVerse;
        [ThreadStatic] private static int depth;
        [ThreadStatic] private static bool insideFallback;

        public static void Message(string message) { Message(null, message); }
        public static void Message(string category, string message) { Write("Message", category, message, null); }
        public static void Warning(string message) { Warning(null, message); }
        public static void Warning(string category, string message) { Write("Warning", category, message, null); }
        public static void Error(string message) { Error(null, message, null); }
        public static void Error(string message, Exception exception) { Error(null, message, exception); }
        public static void Error(string category, string message) { Error(category, message, null); }
        public static void Error(string category, string message, Exception exception) { Write("Error", category, message, exception); }

        /// <summary>Opt-in call sites only; never emits during normal production play.</summary>
        public static void Trace(string category, string message)
        {
            try
            {
                if (Verse.Prefs.DevMode) Write("Trace", category, message, null);
            }
            catch { /* Even developer diagnostics must not affect gameplay. */ }
        }

        private static void Write(string level, string category, string message, Exception exception)
        {
            try
            {
                string channel = string.IsNullOrWhiteSpace(category) ? "RWoM" : "RWoM." + category.Trim();
                message = message ?? string.Empty;
                // A sink may itself call a mod helper. Never send that event back to
                // the same sink, and never recursively call Verse from its fallback.
                if (depth != 0)
                {
                    if (level != "Trace") Fallback(level, channel, message, exception);
                    return;
                }
                depth++;
                try
                {
                    try { if (TryWriteExternal(level, channel, message, exception)) return; }
                    catch { /* Preserve the caller event when the optional sink fails. */ }
                    // Trace has no normal-log fallback: an absent/older framework must
                    // not turn optional high-frequency diagnostics into message spam.
                    if (level != "Trace") Fallback(level, channel, message, exception);
                }
                finally { depth--; }
            }
            catch { /* No exception from logging is allowed across the caller boundary. */ }
        }

        private static void Fallback(string level, string channel, string message, Exception exception)
        {
            if (insideFallback) return;
            insideFallback = true;
            try
            {
                string text = "[" + channel + "] " + message;
                if (exception != null)
                {
                    try { text += "\n" + exception; }
                    catch { text += "\nException details unavailable (exception formatter failed)."; }
                }
                fallback(level, text);
            }
            catch { /* A broken game logger cannot be repaired by recursively logging. */ }
            finally { insideFallback = false; }
        }

        /// <summary>Optional adapter boundary. False retains the original Verse fallback.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryWriteExternal(string level, string channel, string message, Exception exception) => false;

        private static void WriteVerse(string level, string text)
        {
            if (level == "Error") Verse.Log.Error(text);
            else if (level == "Warning") Verse.Log.Warning(text);
            else Verse.Log.Message(text);
        }

    }
}
