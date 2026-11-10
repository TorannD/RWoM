namespace RWoM.RegressionTests;
internal static class SummonerAscensionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SummonerAscension.ActualWorkSaveInterruptionAndPayment", f => ClassAscensionJobTests.Work(f, summoner: true));
    }
}