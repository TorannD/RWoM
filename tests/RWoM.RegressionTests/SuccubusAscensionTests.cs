namespace RWoM.RegressionTests;
internal static class SuccubusAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SuccubusAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, succubus: true));
    }
}
