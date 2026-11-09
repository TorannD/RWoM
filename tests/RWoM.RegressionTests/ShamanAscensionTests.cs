namespace RWoM.RegressionTests;
internal static class ShamanAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ShamanAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, shaman: true));
    }
}
