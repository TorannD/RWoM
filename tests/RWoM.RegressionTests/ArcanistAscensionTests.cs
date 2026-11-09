namespace RWoM.RegressionTests;

internal static class ArcanistAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ArcanistAscension.ActualWorkSaveInterruptionAndFoldRealityUnlock", f => ClassAscensionJobTests.Work(f, arcanist: true));
    }
}