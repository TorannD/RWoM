namespace RWoM.RegressionTests;
internal static class BloodMageAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BloodMageAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, bloodmage: true));
    }
}
