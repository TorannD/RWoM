namespace RWoM.RegressionTests;
internal static class BrightmageAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BrightmageAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, brightmage: true));
    }
}
