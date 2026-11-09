namespace RWoM.RegressionTests;
internal static class EnchanterAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EnchanterAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, enchanter: true));
    }
}
