using System.Xml.Linq;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static class ServantControlProgressTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ServantControlProgress.AdjacentControlPauseAndReset", Continuity);
        yield return new("ServantControlProgress.NativeScribeHalfwayAndCompletion", Saved);
    }

    static void Continuity(RuntimeFixture f)
    {
        var p = new ServantControlProgress();
        p.Observe("servant-a", true, 0);
        p.Observe("servant-a", true, 60);
        Check.Equal(60, p.ControlledTicks, "adjacent active observations count actual ticks");
        p.Observe("servant-a", true, 60);
        p.Observe("servant-a", true, 20);
        Check.Equal(60, p.ControlledTicks, "duplicate and backward observations add nothing");
        p.Observe("servant-a", true, 120);
        Check.Equal(120, p.ControlledTicks, "backward observation never rewinds clock");
        p.Observe("servant-a", true, 10120);
        Check.Equal(120, p.ControlledTicks, "10000 tick gap adds zero, not capped sixty");
        p.Observe("servant-a", false, 10180);
        p.Observe("servant-a", false, 10240);
        p.Observe("servant-a", true, 10300);
        Check.Equal(120, p.ControlledTicks, "both ends of interval must be active");
        Check.Equal("servant-a", p.SubjectId, "pause retains subject");
        p.Observe("servant-a", true, 10360);
        Check.Equal(180, p.ControlledTicks, "resume counts only adjacent active interval");
        p.Observe("servant-a", false, 10360);
        p.Observe("servant-a", true, 10360);
        p.Observe("servant-a", true, 10420);
        Check.Equal(180, p.ControlledTicks, "duplicate active cannot undo reported pause");
        p.Observe("servant-b", true, 10480);
        Check.Equal(0, p.ControlledTicks, "changed servant resets progress");
        p.Observe("servant-b", true, 10540);
        Check.Equal(60, p.ControlledTicks, "new servant starts its own interval");
        p.Observe(null!, false, 10600);
        Check.Equal<string?>(null, p.SubjectId, "lost servant clears identity");
        Check.Equal(0, p.ControlledTicks, "lost servant resets progress");
    }

    static string Save(RuntimeFixture f, ServantControlProgress p)
    {
        string path = Path.Combine(f.WorkingDirectory, "servant-control-" + Guid.NewGuid() + ".xml");
        try
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref p, "servantControl");
            Scribe.saver.FinalizeSaving();
        }
        finally { Scribe.ForceStop(); }
        return path;
    }

    static ServantControlProgress Load(string path)
    {
        ServantControlProgress p = null!;
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref p, "servantControl");
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        return Check.NotNull(p, "native Scribe restores tracker");
    }

    static void Saved(RuntimeFixture f)
    {
        var p = new ServantControlProgress();
        for (int tick = 0; tick <= 30000; tick += 60) p.Observe("same-owned-servant", true, tick);
        Check.Equal(30000, p.ControlledTicks, "halfway reached through actual observations");
        p = Load(Save(f, p));
        p.Observe("same-owned-servant", true, 30000);
        p = Load(Save(f, p));
        p.Observe("same-owned-servant", true, 30000);
        Check.Equal(30000, p.ControlledTicks, "repeated save/load on same tick cannot pay twice");
        p.Observe("same-owned-servant", false, 30060);
        p = Load(Save(f, p));
        p.Observe("same-owned-servant", true, 40060);
        Check.Equal(30000, p.ControlledTicks, "loaded pause cannot catch up offline gap");
        for (int tick = 40120; tick <= 70060; tick += 60) p.Observe("same-owned-servant", true, tick);
        Check.Equal(ServantControlProgress.RequiredTicks, p.ControlledTicks, "same servant completes full observed day");
        p = Load(Save(f, p));
        p.Observe("same-owned-servant", true, 70120);
        Check.Equal(ServantControlProgress.RequiredTicks, p.ControlledTicks, "completion is saved and bounded");
        var path = Save(f, p);
        var xml = XDocument.Load(path);
        xml.Descendants("lastTick").Remove();
        xml.Descendants("lastActive").Remove();
        xml.Descendants("controlledTicks").Single().Value = "30000";
        xml.Save(path);
        p = Load(path);
        p.Observe("same-owned-servant", true, 70180);
        Check.Equal(30000, p.ControlledTicks, "missing continuity fields establish baseline without credit");
        p.Observe("same-owned-servant", true, 70240);
        Check.Equal(30060, p.ControlledTicks, "new adjacent interval resumes legacy payload safely");
    }
}
