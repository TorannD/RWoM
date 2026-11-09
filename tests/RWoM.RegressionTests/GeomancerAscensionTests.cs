namespace RWoM.RegressionTests;
internal static class GeomancerAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GeomancerAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, geomancer: true));
    }
}
