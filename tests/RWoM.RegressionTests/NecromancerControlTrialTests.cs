using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class NecromancerControlTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("NecromancerControlTrial.RealOwnerLinkAndStableSelection", Ownership);
        yield return new("NecromancerControlTrial.PauseResumeAndCompletionOnce", Completion);
        yield return new("NecromancerControlTrial.NativeNestedScribePreservesControl", Saved);
    }

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly Faction faction = new() { def = new FactionDef { isPlayer = true } };
        int nextId = 9000;
        public Pawn Owner;
        public CompAbilityUserMagic Magic;
        public DisciplineProgress Path;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)),
                (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var p = PawnWithComps(true, false); Owner = p.pawn; Magic = p.magic;
            Prepare(Owner);
            var comp = new CompClassProgression { parent = Owner }; Owner.AllComps.Add(comp);
            comp.BeginNewProgression(Vocation.Mage);
            comp.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
            Path = comp.State.Find("rwom.necromancer");
        }
        void Prepare(Pawn p)
        {
            p.thingIDNumber = nextId++;
            Set(p, "factionInt", faction); Set(p, "mapIndexOrState", (sbyte)0);
        }
        public Pawn Servant(Pawn? linkedOwner, bool listed = true)
        {
            var p = PawnWithComps(false, false).pawn; Prepare(p);
            var type = typeof(TorannMagicMod).Assembly.GetType("TorannMagic.HediffComp_Undead")!;
            var comp = (HediffComp)Activator.CreateInstance(type)!;
            var hediff = new HediffWithComps { pawn = p, def = new HediffDef { defName = "TrialOwnedUndead" }, comps = [comp] };
            comp.parent = hediff;
            type.GetField("linkedPawn")!.SetValue(comp, linkedOwner);
            p.health.hediffSet.hediffs.Add(hediff);
            if (listed) Magic.supportedUndead.Add(p);
            return p;
        }
        public void Tick(int tick)
        {
            Set(Find.TickManager, "ticksGameInt", tick);
            NecromancerControlTrial.Observe(Magic);
        }
        public void Dispose() => Current.Game = previous;
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var foreignOwner = PawnWithComps(false, false).pawn;
        var foreign = w.Servant(foreignOwner);
        Check.Equal(false, NecromancerControlTrial.IsOwnedServant(w.Owner, foreign), "supported list alone cannot forge owner link");
        w.Tick(0); w.Tick(60);
        Check.Equal<string?>(null, w.Path.ServantControl.SubjectId, "foreign servant never selected");
        var unlisted = w.Servant(w.Owner, false);
        Check.Equal(true, NecromancerControlTrial.IsOwnedServant(w.Owner, unlisted), "real hediff recognizes actual owner");
        w.Tick(120);
        Check.Equal<string?>(null, w.Path.ServantControl.SubjectId, "unlisted servant earns no service credit");
        w.Magic.supportedUndead.Add(unlisted);
        w.Tick(180); w.Tick(240);
        Check.Equal(60, w.Path.ServantControl.ControlledTicks, "owned linked listed servant starts control");
        var second = w.Servant(w.Owner);
        w.Magic.supportedUndead.Remove(second); w.Magic.supportedUndead.Insert(0, second);
        w.Tick(300);
        Check.Equal(unlisted.ThingID, w.Path.ServantControl.SubjectId, "adding earlier list entry preserves selected servant");
        Check.Equal(120, w.Path.ServantControl.ControlledTicks, "same selected servant keeps progress");
        Set(unlisted.health, "healthState", PawnHealthState.Dead);
        w.Tick(360);
        Check.Equal(second.ThingID, w.Path.ServantControl.SubjectId, "dead selected servant replaced by valid one");
        Check.Equal(0, w.Path.ServantControl.ControlledTicks, "death never transfers accumulated time");
        w.Tick(420);
        var removed = second.health.hediffSet.hediffs.OfType<HediffWithComps>().Single();
        second.health.hediffSet.hediffs.Remove(removed);
        removed.comps.Single().CompPostPostRemoved();
        Check.Equal(0, w.Path.ServantControl.ControlledTicks, "native removed-hediff callback resets selected progress immediately");
        w.Magic.supportedUndead.Remove(second); w.Tick(480);
        Check.Equal<string?>(null, w.Path.ServantControl.SubjectId, "removed selected servant loses selection");
        Check.Equal(0, w.Path.ServantControl.ControlledTicks, "list removal resets control");
        second.health.hediffSet.hediffs.Add(removed);
        w.Magic.supportedUndead.Add(second); w.Tick(540); w.Tick(600);
        Set(second, "factionInt", new Faction()); w.Tick(660);
        Check.Equal(0, w.Path.ServantControl.ControlledTicks, "faction change invalidates ownership");
    }

    static void Completion(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var servant = w.Servant(w.Owner);
        w.Magic.MagicData.MagicUserXP = 77; w.Magic.MagicData.MagicAbilityPoints = 4;
        w.Tick(0); w.Tick(60);
        Set(servant, "mapIndexOrState", (sbyte)-1);
        w.Tick(120);
        Set(servant, "mapIndexOrState", (sbyte)0);
        servant.health.hediffSet.hediffs.OfType<HediffWithComps>().Single().comps.Single().Notify_Spawned();
        w.Tick(180);
        Check.Equal(60, w.Path.ServantControl.ControlledTicks, "despawn and respawn retain progress without counting pause");
        w.Tick(240);
        Set(servant, "mapIndexOrState", (sbyte)1); w.Tick(300);
        Set(servant, "mapIndexOrState", (sbyte)0); w.Tick(360);
        Check.Equal(120, w.Path.ServantControl.ControlledTicks, "different maps pause control");
        NecromancerControlTrial.PauseOwner(w.Owner);
        Set(w.Owner, "mapIndexOrState", (sbyte)-1); w.Tick(420);
        Set(w.Owner, "mapIndexOrState", (sbyte)0); w.Tick(10420);
        Check.Equal(120, w.Path.ServantControl.ControlledTicks, "owner caravan or unloaded gap adds nothing");
        for (int tick = 10480; tick <= 70300; tick += 60) w.Tick(tick);
        Check.Equal(ServantControlProgress.RequiredTicks, w.Path.ServantControl.ControlledTicks, "same servant completes actual observed day");
        Check.Equal(1, w.Path.CompletedTrials.Count, "completion records one marker");
        Check.Equal(NecromancerControlTrial.TrialId, w.Path.CompletedTrials.Single(), "stable trial marker");
        w.Tick(70360); w.Tick(70360);
        NecromancerControlTrial.ServantLifecycle(w.Owner, servant, true);
        Check.Equal(1, w.Path.CompletedTrials.Count, "repeats and later loss do not duplicate or erase achievement");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "service does not advance stage");
        Check.Equal(77, w.Magic.MagicData.MagicUserXP, "service grants no XP");
        Check.Equal(4, w.Magic.MagicData.MagicAbilityPoints, "service grants no points");
    }

    static ClassProgressionState RoundTrip(RuntimeFixture f, ClassProgressionState state)
    {
        string file = System.IO.Path.Combine(f.WorkingDirectory, "necro-nested-" + Guid.NewGuid() + ".xml");
        try
        {
            Scribe.saver.InitSaving(file, "regression");
            Scribe_Deep.Look(ref state, "classProgression");
            Scribe.saver.FinalizeSaving();
        }
        finally { Scribe.ForceStop(); }
        ClassProgressionState loaded = null!;
        try
        {
            Scribe.loader.InitLoading(file);
            Scribe_Deep.Look(ref loaded, "classProgression");
            Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        return Check.NotNull(loaded, "native Scribe loads complete progression owner");
    }

    static void Saved(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var servant = w.Servant(w.Owner);
        var comp = ClassIdentity.Component(w.Owner);
        var data = w.Magic.MagicData;
        data.MagicUserXP = 93; data.MagicAbilityPoints = 6;
        w.Path.RecordCompletedTrial("rwom.test.preserved-trial");
        for (int tick = 0; tick <= 30000; tick += 60) w.Tick(tick);
        var loaded = RoundTrip(f, comp.State);
        var path = loaded.Find("rwom.necromancer");
        Check.Equal(Vocation.Mage, loaded.Vocation, "vocation survives nested Scribe");
        Check.Equal(InitiationStage.Foundation, path.Stage, "stage is not advanced by load");
        Check.Equal(servant.ThingID, path.ServantControl.SubjectId, "selected servant ID survives nested Scribe");
        Check.Equal(30000, path.ServantControl.ControlledTicks, "halfway control is deeply saved");
        Check.Equal("rwom.test.preserved-trial", path.CompletedTrials.Single(), "existing markers survive nested Scribe");
        Set(comp, "state", loaded); w.Path = path;
        w.Tick(30000);
        Check.Equal(30000, path.ServantControl.ControlledTicks, "loaded duplicate observation cannot add credit");
        w.Tick(30060);
        Check.Equal(30060, path.ServantControl.ControlledTicks, "same owned servant resumes through real service");
        for (int tick = 30120; tick <= 60000; tick += 60) w.Tick(tick);
        loaded = RoundTrip(f, comp.State); path = loaded.Find("rwom.necromancer");
        Set(comp, "state", loaded); w.Path = path;
        w.Tick(60060);
        Check.Equal(2, path.CompletedTrials.Count, "completed control and earlier marker both persist exactly once");
        Check.Equal(1, path.CompletedTrials.Count(x => x == NecromancerControlTrial.TrialId), "loaded completion is not awarded again");
        Check.Equal(60000, path.ServantControl.ControlledTicks, "completed tracker remains saved");
        Check.Same(data, w.Magic.MagicData, "loading progression never replaces spell data");
        Check.Equal(93, data.MagicUserXP, "load and resumed service grant no XP");
        Check.Equal(6, data.MagicAbilityPoints, "load and resumed service grant no points");
        Check.Equal(InitiationStage.Foundation, path.Stage, "completion still requires separate advancement transaction");
    }
}
