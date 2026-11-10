namespace RWoM.RegressionTests;

internal static class IceAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IceAscension.ActualWorkSaveInterruptionAndBlizzardUnlock", f => ClassAscensionJobTests.Work(f, ice: true));
    }
}
