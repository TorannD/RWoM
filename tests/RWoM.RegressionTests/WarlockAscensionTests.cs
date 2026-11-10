namespace RWoM.RegressionTests;
internal static class WarlockAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("WarlockAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, warlock: true));
    }
}
