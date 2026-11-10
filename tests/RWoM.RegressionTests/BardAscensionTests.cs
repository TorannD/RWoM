namespace RWoM.RegressionTests;
internal static class BardAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BardAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, bard: true));
    }
}
