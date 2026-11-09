namespace RWoM.RegressionTests;

internal static class LightningAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LightningAscension.ActualWorkSaveInterruptionAndEyeOfTheStormUnlock", f => ClassAscensionJobTests.Work(f, lightning: true));
    }
}
