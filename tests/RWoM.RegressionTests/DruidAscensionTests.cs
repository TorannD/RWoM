namespace RWoM.RegressionTests;
internal static class DruidAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("DruidAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, druid: true));
    }
}
