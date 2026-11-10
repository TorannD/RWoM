using System.Reflection;
using TorannMagic;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static partial class CastSafetyTests
{
    static IEnumerable<TestCase> EtherShellJobCases()
    {
        yield return new("Odyssey.EtherShell.JobDoesNotChargeFailedShell", f => World(f, w =>
        {
            var type = typeof(MagicAbility).Assembly.GetType("TorannMagic.Odyssey.EtherShellAbility");
            Check.Equal(true, type != null, "shell requires success-sensitive payment");
            var ability = (MagicAbility)Activator.CreateInstance(type!)!;
            ability.Def = w.Def;
            int initialCooldown = ability.CooldownTicksLeft;
            w.Verb.Ability = ability;
            var driver = new Driver { pawn = w.Caster, job = new Job { def = w.Job.def, targetA = w.Target, verbToUse = w.Verb } };
            var toil = driver.Toils().Last(); toil.actor = w.Caster;
            toil.initAction(); toil.Cleanup(0, driver);
            Check.Equal(1, w.Verb.Shots, "job reached its shot but did not apply a shell");
            Check.Equal(initialCooldown, ability.CooldownTicksLeft, "failed shell receives no cooldown");
        }));
    }
}
