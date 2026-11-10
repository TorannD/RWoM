using System.Collections.Concurrent;
using System.Reflection;
using TorannMagic.Compatibility;
using Verse;

namespace RWoM.RegressionTests;

internal static class LoggingCompatTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LoggingCompat.WithoutIntegrationUsesVerse", _ =>
        {
            RuntimeLog.AssertClean();
            var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            try
            {
                Check.Equal(false, TM_Log.TryWriteExternal("Warning", "RWoM", "absent adapter", null!), "unpatched optional boundary declines event");
                TM_Log.Warning("without Integration");
                Check.Equal(1, problems.Count, "Content alone writes once to actual Verse sink");
                Check.Equal(true, problems[0].Contains("[RWoM] without Integration"), "Content alone preserves message");
            }
            finally { RuntimeLog.Clear(); }
        });
        yield return new("LoggingCompat.BackendOwnedByIntegrations", _ =>
        {
            Check.Equal(false, typeof(TM_Log).GetNestedTypes(BindingFlags.NonPublic).Any(t => t.Name == "Provider"), "Content does not own external provider");
            Check.NotNull(Backend, "Integrations owns external discovery");
        });
        yield return new("LoggingCompat.AbsentAndIncompatibleProviderPreserveSeverityAndException", _ =>
        {
            foreach (Type? facade in new Type?[] { null, typeof(IncompatibleFacade) })
            {
                var output = new ConcurrentQueue<(string Level, string Text)>();
                using var scope = Configure(facade, typeof(FakeLifecycle), output);
                TM_Log.Message("started");
                TM_Log.Warning("progression", "missing skill");
                var error = new InvalidOperationException("original failure");
                TM_Log.Error("possession", "restore failed", error);
                var entries = output.ToArray();
                Check.Equal(3, entries.Length, "Every unavailable-provider message falls back exactly once");
                Check.Equal("Message", entries[0].Level, "Information severity");
                Check.Equal("[RWoM] started", entries[0].Text, "Default context");
                Check.Equal("Warning", entries[1].Level, "Warning severity");
                Check.Equal("[RWoM.progression] missing skill", entries[1].Text, "Category context");
                Check.Equal("Error", entries[2].Level, "Error severity");
                Check.Equal(true, entries[2].Text.Contains(error.ToString()), "Original exception details preserved");
            }
        });
        yield return new("LoggingCompat.AbsentProviderWritesToRealVerseLog", _ =>
        {
            using var discovery = Override("findTypes", (Func<Type[]?>)(() => null));
            var problems = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            RuntimeLog.AssertClean();
            try
            {
                TM_Log.Message("normal information");
                Check.Equal(0, problems.Count, "Message remains below the real warning/error sink threshold");
                TM_Log.Warning("progression", "real fallback warning");
                var exception = new InvalidOperationException("real fallback exception");
                TM_Log.Error("possession", "real fallback error", exception);
                Check.Equal(2, problems.Count, "Real Verse warning/error calls reach the existing runtime sink");
                Check.Equal(true, problems[0].Contains("[RWoM.progression] real fallback warning"), "Real fallback keeps category");
                Check.Equal(true, problems[1].Contains(exception.ToString()), "Real fallback keeps original exception");
            }
            finally { RuntimeLog.Clear(); }
        });
        yield return new("LoggingCompat.DocumentedFacadeUsesExactChannelsAndExceptionOverload", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(typeof(FakeFacade), typeof(FakeLifecycle), output);
            TM_Log.Message("initialized");
            TM_Log.Warning("autocast", "invalid target");
            TM_Log.Error("progression", "missing owner");
            var exception = new InvalidOperationException("save failed");
            TM_Log.Error("possession", "restore failed", exception);
            TM_Log.Error("uncategorized failure", exception);
            var entries = FakeFacade.Entries.ToArray();
            Check.Equal(5, entries.Length, "All documented overloads routed");
            Check.Equal(0, output.Count, "No duplicate Verse write after successful provider call");
            Check.Equal("Info", entries[0].Level, "Message maps to InfoTo");
            Check.Equal("RWoM", entries[0].Channel, "Default channel");
            Check.Equal("Warn", entries[1].Level, "Warning maps to WarnTo");
            Check.Equal("RWoM.autocast", entries[1].Channel, "Autocast category");
            Check.Equal("RWoM.progression", entries[2].Channel, "Progression category");
            Check.Equal("Error", entries[2].Level, "Plain error overload");
            Check.Equal("RWoM.possession", entries[3].Channel, "Possession category");
            Check.Same(exception, entries[3].Exception, "Exception remains a structured exception, not just text");
            Check.Equal("RWoM", entries[4].Channel, "Exception-only convenience API");
            Check.Equal("invalid target", entries[1].Text, "Literal text preserved");
        });
        yield return new("LoggingCompat.UnreadyAndDegradedProviderRecoverWithoutRediscovery", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(typeof(FakeFacade), typeof(FakeLifecycle), output);
            FakeLifecycle.RegisteredSinks = Array.Empty<object>();
            TM_Log.Warning("before framework initialization");
            FakeLifecycle.RegisteredSinks = new object[] { new object() };
            TM_Log.Warning("after framework initialization");
            FakeLifecycle.IsPrimary = false;
            TM_Log.Error("degraded provider");
            FakeLifecycle.IsPrimary = true;
            TM_Log.Error("provider recovered");
            Check.Equal(2, output.Count, "Uninitialized/degraded calls use fallback");
            Check.Equal(2, FakeFacade.Entries.Count, "Initialized/recovered calls use provider");
        });
        yield return new("LoggingCompat.ThrowingProviderAndFallbackCannotEscapeOrLoseOriginal", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(typeof(FakeFacade), typeof(FakeLifecycle), output);
            FakeFacade.Throw = true;
            var exception = new InvalidOperationException("original exception");
            TM_Log.Warning("provider failure");
            TM_Log.Error("failure", exception);
            Check.Equal(2, output.Count, "Each failed provider call falls back");
            Check.Equal(true, output.Last().Text.Contains(exception.ToString()), "Provider exception cannot replace caller exception");
            FakeLifecycle.Throw = true;
            TM_Log.Error("readiness getter failed");
            Check.Equal(3, output.Count, "Readiness failure also falls back");
            using (Override("fallback", (Action<string, string>)((_, _) => throw new InvalidOperationException("fallback unavailable"))))
                TM_Log.Error("fallback failure is contained", new ThrowingException());
        });
        yield return new("LoggingCompat.ReentrantProviderAndFallbackAreBounded", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(typeof(FakeFacade), typeof(FakeLifecycle), output);
            FakeFacade.OnWrite = () => TM_Log.Warning("provider reentered wrapper");
            TM_Log.Error("outer event");
            Check.Equal(1, FakeFacade.Entries.Count, "Provider does not recursively reenter itself");
            Check.Equal(1, output.Count, "Nested event uses Verse fallback");
            int fallbackCalls = 0;
            FakeFacade.Throw = true;
            using (Override("fallback", (Action<string, string>)((_, _) =>
            {
                fallbackCalls++;
                TM_Log.Error("fallback reentered wrapper");
            })))
                TM_Log.Error("outer fallback");
            Check.Equal(1, fallbackCalls, "Fallback cannot recursively reenter itself");
        });
        yield return new("LoggingCompat.DiscoveryIsCachedAndInvalidationFindsLateProvider", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(null, null, output);
            int discoveries = 0;
            Type? facade = null;
            using var discovery = Override("findTypes", (Func<Type[]?>)(() =>
            {
                discoveries++;
                return facade == null ? null : new[] { facade, typeof(FakeLifecycle) };
            }));
            Invalidate();
            for (int i = 0; i < 10; i++) TM_Log.Warning("before late load");
            Check.Equal(1, discoveries, "Absent provider scan is cached");
            facade = typeof(FakeFacade);
            Invalidate(); // Same invalidation called by AppDomain.AssemblyLoad.
            for (int i = 0; i < 10; i++) TM_Log.Warning("after late load");
            Check.Equal(2, discoveries, "Only a load-generation change triggers rediscovery");
            Check.Equal(10, FakeFacade.Entries.Count, "Late-loaded provider becomes usable");
            Check.Equal(10, output.Count, "Pre-load diagnostics were not lost");
        });
        yield return new("LoggingCompat.ConcurrentCallsRemainExactlyOnce", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(typeof(FakeFacade), typeof(FakeLifecycle), output);
            Parallel.For(0, 128, i => TM_Log.Warning("autocast", "event " + i));
            Check.Equal(128, FakeFacade.Entries.Count, "Every concurrent event delivered once");
            Check.Equal(128, FakeFacade.Entries.Select(e => e.Text).Distinct().Count(), "No duplicate event");
            Check.Equal(0, output.Count, "Concurrent provider success has no fallback duplication");
        });
        yield return new("LoggingCompat.TraceIsDeveloperOnlyAndOptional", _ =>
        {
            var output = new ConcurrentQueue<(string Level, string Text)>();
            using var scope = Configure(typeof(FakeFacade), typeof(FakeLifecycle), output);
            // Prefs.DevMode's public setter applies graphics settings and accesses
            // WindowStack. Change only its in-memory backing value in this headless test.
            object prefs = typeof(Prefs).GetField("data", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var devMode = prefs.GetType().GetField("devMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
            bool previous = (bool)devMode.GetValue(prefs)!;
            try
            {
                devMode.SetValue(prefs, false);
                TM_Log.Trace("autocast", "quiet production cast");
                Check.Equal(0, FakeFacade.Entries.Count, "Production trace is silent");
                Check.Equal(0, output.Count, "Production trace has no Verse fallback");
                devMode.SetValue(prefs, true);
                TM_Log.Trace("autocast", "developer diagnostic");
                Check.Equal("Trace", FakeFacade.Entries.Single().Level, "Developer trace keeps low severity");
            }
            finally { devMode.SetValue(prefs, previous); }
        });
        string? installedPath = Environment.GetEnvironmentVariable("RWOM_LOGGING_FRAMEWORK_PATH");
        if (!string.IsNullOrWhiteSpace(installedPath))
            yield return new("LoggingCompat.InstalledFrameworkPublicApiSmoke", _ =>
            {
                var output = new ConcurrentQueue<(string Level, string Text)>();
                using var fallback = Override("fallback", (Action<string, string>)((level, text) => output.Enqueue((level, text))));
                var assembly = Assembly.LoadFrom(Path.GetFullPath(installedPath));
                Check.Equal("RimWorks.RimLogging", assembly.GetName().Name, "Requested installed logging assembly identity");
                TM_Log.Warning("compatibility", "headless optional-provider readiness probe");
                Check.NotNull(Backend.GetField("provider", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null),
                    "Real installed facade and lifecycle bind without mandatory references");
                Check.Equal(1, output.Count, "Uninitialized installed framework still preserves warning via fallback");
                // Do not initialize the framework, register sinks, or touch installed settings.
            });
    }

    internal static IDisposable Configure(Type? facade, Type? lifecycle, ConcurrentQueue<(string Level, string Text)> output)
    {
        FakeFacade.Reset();
        FakeLifecycle.IsPrimary = true;
        FakeLifecycle.Throw = false;
        FakeLifecycle.RegisteredSinks = new object[] { new object() };
        return new Scope(
            Override("findTypes", (Func<Type[]?>)(() => facade == null ? null : new[] { facade, lifecycle! })),
            Override("fallback", (Action<string, string>)((level, text) => output.Enqueue((level, text)))));
    }

    private static Type Backend => Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Integrations.LoggingBackend", true)!;
    private static int hookUsers;
    private static IDisposable Override(string name, object value)
    {
        var field = (name == "fallback" ? typeof(TM_Log) : Backend).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!;
        var ht = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var harmony = Activator.CreateInstance(ht, "rwom.tests.logging-extraction")!;
        if (hookUsers++ == 0)
            {
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, new object[] { Backend.Assembly.GetType("TorannMagic.Integrations.LoggingBackendPatch", true)! })!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
        }
        var previous = field.GetValue(null);
        field.SetValue(null, value);
        Invalidate();
        return new Restore(() => { field.SetValue(null, previous); Invalidate(); if (--hookUsers == 0) ht.GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { "rwom.tests.logging-extraction" }); });
    }

    private static void Invalidate() => Backend.GetMethod("Invalidate", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);

    private sealed class Scope(params IDisposable[] values) : IDisposable
    {
        public void Dispose() { foreach (var value in values.Reverse()) value.Dispose(); }
    }
    private sealed class Restore(Action restore) : IDisposable { public void Dispose() => restore(); }
    private sealed class ThrowingException : Exception { public override string ToString() => throw new InvalidOperationException("bad exception formatter"); }

    public static class IncompatibleFacade { public static void Warning(string message) => throw new InvalidOperationException(message); }
    public static class FakeLifecycle
    {
        public static bool IsPrimary { get; set; }
        private static IReadOnlyList<object> sinks = Array.Empty<object>();
        public static bool Throw;
        public static IReadOnlyList<object> RegisteredSinks
        {
            get => Throw ? throw new InvalidOperationException("not initialized") : sinks;
            set => sinks = value;
        }
    }
    public sealed record Entry(string Level, string Channel, string Text, Exception? Exception);
    public static class FakeFacade
    {
        public static readonly ConcurrentQueue<Entry> Entries = new();
        public static bool Throw;
        public static Action? OnWrite;
        public static void Reset() { Entries.Clear(); Throw = false; OnWrite = null; }
        private static void Write(string level, string channel, string text, Exception? exception = null)
        {
            if (Throw) throw new InvalidOperationException("provider unavailable");
            Entries.Enqueue(new(level, channel, text, exception));
            OnWrite?.Invoke();
        }
        // Exact upstream public facade signatures, including caller-info parameters.
        public static void InfoTo(string channel, string message, object context, int line = 0, string file = "") => Write("Info", channel, message);
        public static void WarnTo(string channel, string message, object context, int line = 0, string file = "") => Write("Warn", channel, message);
        public static void ErrorTo(string channel, string message, object context, int line = 0, string file = "") => Write("Error", channel, message);
        public static void ErrorTo(string channel, Exception exception, string message, int line = 0, string file = "") => Write("Error", channel, message, exception);
        public static void TraceTo(string channel, string message, object context, int line = 0, string file = "") => Write("Trace", channel, message);
    }
}
