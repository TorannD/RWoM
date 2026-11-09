namespace RWoM.RegressionTests;
internal static class GolemancerAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GolemancerAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, golemancer: true));
    }
}
