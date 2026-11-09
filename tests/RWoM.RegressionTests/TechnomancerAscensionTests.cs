namespace RWoM.RegressionTests;
internal static class TechnomancerAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("TechnomancerAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, technomancer: true));
    }
}
