using Verse;

namespace RWoM.RegressionTests;

// Restore databases after synthetic native-save definitions have been registered.
internal sealed class SavedDefinitions<T> : IDisposable where T : Def, new()
{
    private readonly T[] saved = DefDatabase<T>.AllDefsListForReading.ToArray();
    public void Dispose()
    {
        DefDatabase<T>.Clear();
        foreach (var definition in saved) DefDatabase<T>.Add(definition);
    }
}
