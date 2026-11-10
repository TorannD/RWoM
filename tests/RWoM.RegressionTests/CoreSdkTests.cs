using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static class CoreSdkTests
{
    internal static string? AssemblyPath(string repo)
    {
        var selection = Environment.GetEnvironmentVariable("RWOM_TEST_CORE_ASSEMBLY");
        return selection == "disabled" ? null : selection ?? Path.Combine(repo, "TorannMagic.Core", "Assemblies", "TorannMagic.Core.dll");
    }

    internal static object[] Rows(Pawn pawn)
    {
        Check.Equal(true, CoreBridge.Available, "host available");
        var api = Assembly.Load("TorannMagic.Core").GetType("TorannMagic.Core.PawnDisciplines", true)!;
        object?[] args = [pawn, null];
        Check.Equal(true, (bool)api.GetMethod("TryRead")!.Invoke(null, args)!, "host returned a snapshot");
        return ((System.Collections.IEnumerable)args[1]!).Cast<object>().ToArray();
    }

    internal static void Disciplines(RuntimeFixture fixture)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(fixture);
        var p = GrimoireProgressionTests.PawnWithComps(false, false);
        var comp = new TorannMagic.ClassProgression.CompClassProgression { parent = p.pawn };
        p.pawn.AllComps.Add(comp);
        comp.BeginNewProgression(TorannMagic.ClassProgression.Vocation.Mage);
        foreach (string id in new[] { "rwom.fire", "rwom.ice" })
            Check.Equal(true, comp.State.TryAddFoundation(id, TorannMagic.ClassProgression.DisciplineDomain.Magic), "add foundation");
        var rows = Rows(p.pawn);
        Check.Equal(2, rows.Length, "both hybrid disciplines");
        Check.Equal("rwom.ice", rows[1].GetType().GetProperty("Id")!.GetValue(rows[1]), "second identity");
        Check.Equal("Foundation", rows[0].GetType().GetProperty("Stage")!.GetValue(rows[0])!.ToString(), "actual stage");
        bool previous = TorannMagic.ClassProgression.ClassProgressionFeature.Enabled;
        try
        {
            TorannMagic.ClassProgression.ClassProgressionFeature.Enabled = false;
            Check.Equal(0, Rows(p.pawn).Length, "disabled beta does not expose inactive disciplines");
            Check.Equal(2, comp.State.Disciplines.Count, "stored paths preserved");
        }
        finally { TorannMagic.ClassProgression.ClassProgressionFeature.Enabled = previous; }
    }
    internal static object Query(Pawn? pawn, string id, string expected)
    {
        Check.Equal(true, CoreBridge.Available, "host initialized");
        var api = Assembly.Load("TorannMagic.Core").GetType("TorannMagic.Core.PawnSpellKnowledge", true)!;
        object?[] args = [pawn, id, null];
        Check.Equal(expected, api.GetMethod("Read")!.Invoke(null, args)!.ToString(), "public status");
        return args[2]!;
    }

    internal static void Knowledge(RuntimeFixture fixture)
    {
        var pawn = new Pawn();
        GrimoireProgressionTests.Set(pawn, "comps", new List<ThingComp>());
        Query(null, "irrelevant", "InvalidRequest");
        Query(pawn, " ", "InvalidRequest");
        Query(pawn, "sdk.missing", "NoRecord");
        var store = new AbilityKnowledgeComp { parent = pawn };
        pawn.AllComps.Add(store);
        Query(pawn, "sdk.missing", "PendingImport");
        store.ImportLegacy();
        Query(pawn, "sdk.missing", "NoRecord");
        var record = store.GetOrCreate("sdk.dormant", 2);
        record.learned = false;
        record.autocast = true;
        record.pwr = 3; record.eff = 4; record.ver = 5;
        var enabled = TorannMagic.ClassProgression.ClassProgressionFeature.Enabled;
        try
        {
            TorannMagic.ClassProgression.ClassProgressionFeature.Enabled = false;
            var snapshot = Query(pawn, record.spellId, "MissingDefinition");
            record.pwr = 1;
            foreach (var (field, value) in new (string, object)[] { ("SpellId", record.spellId),
                ("Learned", false), ("Autocast", true), ("Tier", 2), ("Power", 3), ("Efficiency", 4), ("Versatility", 5) })
                Check.Equal(value, snapshot.GetType().GetProperty(field)!.GetValue(snapshot), field);
            Check.Equal(1, store.Spells.Count, "queries never enroll or create records");
        }
        finally { TorannMagic.ClassProgression.ClassProgressionFeature.Enabled = enabled; }
    }
    public static void CheckContract(RuntimeFixture fixture, string path)
    {
        var core = Assembly.LoadFrom(path);
        var registry = core.GetType("TorannMagic.Core.AbilityCore", true)!;
        var resource = core.GetType("TorannMagic.Core.TMResource", true)!;
        var defs = DefDatabase<NeedDef>.AllDefsListForReading.ToArray();
        const string id = "sdk.test";
        var needDef = new NeedDef { defName = "CoreSdkTravel", needClass = typeof(Need_Travel) };
        try
        {
            DefDatabase<NeedDef>.Clear();
            var pawn = new Pawn();
            pawn.needs = new Pawn_NeedsTracker(pawn);
            DefDatabase<NeedDef>.Add(needDef);
            registry.GetMethod("Register")!.Invoke(null, [id, needDef]);
            object? Resolve() => registry.GetMethod("Resolve")!.Invoke(null, [id]);
            Check.Same(needDef, Resolve(), "registered alias resolves");
            DefDatabase<NeedDef>.Clear();
            registry.GetMethod("Invalidate")!.Invoke(null, null);
            Check.Equal(true, Resolve() == null, "removed Def stays dormant");
            var replacement = new NeedDef { defName = needDef.defName, needClass = typeof(Need_Travel) };
            DefDatabase<NeedDef>.Add(replacement);
            registry.GetMethod("Invalidate")!.Invoke(null, null);
            Check.Same(replacement, Resolve(), "rebinds alias to current Def");

            var need = (Need_Travel)RuntimeHelpers.GetUninitializedObject(typeof(Need_Travel));
            need.def = replacement;
            pawn.needs.AllNeeds.Add(need);
            need.CurLevel = .5f;
            foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check.Equal(false, (bool)resource.GetMethod("TryConsume")!.Invoke(null, [pawn, replacement.defName, invalid])!, "reject invalid debit");
                resource.GetMethod("TryAdd")!.Invoke(null, [pawn, replacement.defName, invalid]);
                Check.Equal(.5f, need.CurLevel, "invalid credit and debit preserve balance");
            }
            Check.Equal(true, (bool)resource.GetMethod("TryConsume")!.Invoke(null, [pawn, replacement.defName, .25f])!, "valid debit");
            Check.Equal(.25f, need.CurLevel, "charged once");
        }
        finally
        {
            DefDatabase<NeedDef>.Clear();
            foreach (var def in defs) DefDatabase<NeedDef>.Add(def);
            registry.GetMethod("Invalidate")!.Invoke(null, null);
        }
    }
}
