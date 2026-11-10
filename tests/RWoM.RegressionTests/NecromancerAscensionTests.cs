namespace RWoM.RegressionTests;

internal static class NecromancerAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        // Reuse the established saved-job/payment contract with the Necromancer offering and ultimate.
        yield return new("NecromancerAscension.ActualWorkSaveInterruptionAndLichUnlock", f => ClassAscensionJobTests.Work(f, true));
    }
}
