using Brutal.Logging;
using Microsoft.Extensions.Logging;

namespace RWoM.RegressionTests;

// Scribe catches some load/save exceptions and sends them to Verse.Log. Capture the
// real log sink so those errors fail tests instead of silently producing a green run.
internal sealed class RuntimeLog : ILoggerProvider, ILogger
{
    private static readonly List<string> Problems = [];

    public static void Initialize() => LogSystem.Initialize(builder => builder.AddProvider(new RuntimeLog()));
    public static void Clear() => Problems.Clear();
    public static void EnsureHeadlessDefOfInitialized(Type type)
    {
        AssertClean();
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        if (Problems.Count != 0) AcceptHeadlessDefOfInitialization(type.Name);
    }
    public static void AcceptHeadlessNeedInitialization()
    {
        string[] expected =
        [
            "Used UnityData before it's initialized.",
            "Tried to get a resource \"UI/Misc/BarInstantMarker\" from a different thread. All resources must be loaded in the main thread.",
            "Tried to get a resource \"UI/Misc/NeedUnitDivider\" from a different thread. All resources must be loaded in the main thread."
        ];
        if (!Problems.SequenceEqual(expected))
            throw new InvalidOperationException("Unexpected headless Need initialization diagnostics:\n" + string.Join("\n", Problems));
        Console.WriteLine("HEADLESS SETUP: Need UI textures unavailable; health/needs policy tests do not render them.");
        Problems.Clear();
    }
    public static void AcceptHeadlessDefOfInitialization(params string[] names)
    {
        string Warning(string name) => "Tried to use an uninitialized DefOf of type " + name + ". DefOfs are initialized right after all defs all loaded. Uninitialized DefOfs will return only nulls. (hint: don't use DefOfs as default field values in Defs, try to resolve them in ResolveReferences() instead)";
        if (names.Length == 0) names = ["AbilityDefOf", "HediffDefOf", "JobDefOf", "RulePackDefOf"];
        if (!Problems.SequenceEqual(names.Select(Warning)))
            throw new InvalidOperationException("Unexpected headless DefOf initialization diagnostics:\n" + string.Join("\n", Problems));
        Console.WriteLine("HEADLESS SETUP: " + string.Join("/", names) + " initialized with test definitions only.");
        Problems.Clear();
    }
    public static void AssertClean()
    {
        if (Problems.Count > 0)
            throw new InvalidOperationException("Runtime logged warnings/errors:\n" + string.Join("\n", Problems));
    }

    public ILogger CreateLogger(string categoryName) => this;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public void Dispose() { }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
            Problems.Add(formatter(state, exception));
    }
}
