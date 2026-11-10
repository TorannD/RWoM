namespace RWoM.RegressionTests;
internal static class PaladinAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("PaladinAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, paladin: true));
    }
}