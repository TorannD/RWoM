namespace RWoM.RegressionTests;
internal static class ChronomancerAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ChronomancerAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, chronomancer: true));
    }
}
