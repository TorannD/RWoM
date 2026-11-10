namespace RWoM.RegressionTests;

internal static class FireAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FireAscension.ActualWorkSaveInterruptionAndFirestormUnlock", f => ClassAscensionJobTests.Work(f, fire: true));
    }
}
