using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.FleshDevelopment;
using Verse;

namespace RWoM.RegressionTests;

internal static class FleshStatTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshStats.ActualRegisteredPatchChangesGameStatPath", _ => InGame(() =>
        {
            // FleshStatPatches are installed once at harness startup (production
            // parity); per-test installation misses already-JIT-compiled stat paths.
            var pawn = Pawn(); var p = FleshRepository.Ensure(pawn);
            p.Thirst = 0; p.SkillPoints = 100;
            for (int i = 0; i < 10; i++) p.TryUpgrade(FleshSkill.Mobility);
            var move = Stat("MoveSpeed", 4f);
            Near(4.8f, pawn.GetStatValue(move), "engine GetStatValue applies learned mobility once");
            p.EmpowerUntil = 18000; p.TryUpgrade(FleshSkill.Empower);
            Near(5.2f, pawn.GetStatValue(move), "engine applies active empowerment");
            p.Thirst = p.MaxThirst;
            Near(4.16f, pawn.GetStatValue(move), "maximum thirst can mask most of the bonus");
            pawn.health.hediffSet.hediffs.Clear();
            Near(4f, pawn.GetStatValue(move), "ordinary pawn unaffected");
        }));
        yield return new("FleshStats.CachedValuesRefreshWhenFleshStateChanges", _ => InGame(() =>
        {
            var pawn = Pawn(); var p = FleshRepository.Ensure(pawn); p.Thirst = 0; p.SkillPoints = 5;
            var work = Stat("WorkSpeedGlobal", 1f); DefDatabase<StatDef>.Add(work);
            var craft = Stat("FleshTestCraftSpeed", 2f); craft.postProcessStatFactors = [work]; DefDatabase<StatDef>.Add(craft);
            FleshStatPatches.Refresh(pawn);
            Near(2f, pawn.GetStatValue(craft, cacheStaleAfterTicks: 1000), "prime actual engine cache");
            p.TryUpgrade(FleshSkill.Industry); FleshStatPatches.Refresh(pawn);
            Near(2.04f, pawn.GetStatValue(craft, cacheStaleAfterTicks: 1000), "dependent crafting cache updates in same tick");
            p.TryUpgrade(FleshSkill.Empower); p.EmpowerUntil = 10; FleshStatPatches.Refresh(pawn);
            Near(2.24f, pawn.GetStatValue(craft, cacheStaleAfterTicks: 1000), "active buff invalidates dependent cache once");
            Set(Find.TickManager, "ticksGameInt", 10); FleshStatPatches.Refresh(pawn);
            Near(2.04f, pawn.GetStatValue(craft, cacheStaleAfterTicks: 1000), "expiry removes cached buff immediately");
            p.Thirst = p.MaxThirst; FleshStatPatches.Refresh(pawn);
            Near(1.632f, pawn.GetStatValue(craft, cacheStaleAfterTicks: 1000), "thirst threshold invalidates cached work value");
            p.Thirst = 0; FleshStatPatches.Refresh(pawn);
            Near(2.04f, pawn.GetStatValue(craft, cacheStaleAfterTicks: 1000), "feeding restores work value");
        }));
        yield return new("FleshStats.PawnInfoExplanationAndOtherStatBonuses", f => InGame(() =>
        {
            using var language = new LanguageScope(f.Repository);
            var pawn = Pawn(); var p = FleshRepository.Ensure(pawn); p.SkillPoints = 10; p.Thirst = 0;
            p.TryUpgrade(FleshSkill.Strength); p.TryUpgrade(FleshSkill.Harden);
            var melee = Stat("MeleeDamageFactor", 1f); var armor = Stat("ArmorRating_Sharp", .1f);
            Near(1.03f, pawn.GetStatValue(melee), "actual melee damage factor");
            Near(.1f, pawn.GetStatValue(armor), "inactive hardening grants nothing");
            p.HardenUntil = 500;
            Near(.3f, pawn.GetStatValue(armor), "actual sharp armor addition");
            var req = StatRequest.For(pawn);
            string explanation = melee.Worker.GetExplanationFinalizePart(req, ToStringNumberSense.Factor, pawn.GetStatValue(melee));
            Check.Equal(true, explanation.Contains("+3%"), "real pawn-info explanation contains learned bonus");
            explanation = armor.Worker.GetExplanationFinalizePart(req, ToStringNumberSense.Factor, pawn.GetStatValue(armor));
            Check.Equal(true, explanation.Contains("20%"), "real pawn-info explanation contains active armor bonus");
            p.Thirst = p.MaxThirst;
            var move = Stat("MoveSpeed", 4f);
            explanation = move.Worker.GetExplanationFinalizePart(req, ToStringNumberSense.Factor, pawn.GetStatValue(move));
            Check.Equal(true, explanation.Contains("80%"), "thirst penalty is explained");
            pawn.health.hediffSet.hediffs.Clear();
            Check.Equal<string?>(null, FleshStatPatches.Explanation(pawn, "MoveSpeed"), "no flesh explanation for ordinary pawn");
        }));
    }

    internal static void Near(float expected, float actual, string message)
    { if (Math.Abs(expected - actual) > .0001f) throw new Exception($"{message}: expected {expected}, got {actual}"); }
    internal static StatDef Stat(string name, float value)
    {
        var def = new StatDef { defName = name, label = name, defaultBaseValue = value, minValue = 0, maxValue = 999, workerClass = typeof(FixedBaseWorker), cacheable = true };
        def.Worker.SetCacheability(false);
        return def;
    }
    public class FixedBaseWorker : StatWorker
    { public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true) => stat.defaultBaseValue; }
    internal static Pawn Pawn()
    {
        var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); race.race = new RaceProperties();
        var pawn = new Pawn { def = race }; pawn.health = new Pawn_HealthTracker(pawn);
        pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = "TM_UndeadHD" } });
        return pawn;
    }
    internal static void InGame(Action action)
    {
        var old = Current.Game;
        Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Set(Current.Game, "tickManager", new TickManager());
        Current.Game.Scenario = new Scenario();
        try { action(); } finally { Current.Game = old; }
    }
    internal static void Set(object target, string name, object value)
    {
        for (Type? t = target.GetType(); t != null; t = t.BaseType)
        { var field = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); if (field != null) { field.SetValue(target, value); return; } }
        throw new Exception("Missing fixture field " + name);
    }
    internal sealed class ProductionPatches : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly object harmony;
        readonly string id = "rwom.regression.flesh-stats." + Guid.NewGuid();
        public ProductionPatches() => harmony = Activator.CreateInstance(type, id)!;
        public void Add(Type patch)
        {
            var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    internal sealed class LanguageScope : IDisposable
    {
        readonly LoadedLanguage old = LanguageDatabase.activeLanguage, oldDefault = LanguageDatabase.defaultLanguage;
        public LanguageScope(string repo)
        {
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = System.Xml.Linq.XDocument.Load(Path.Combine(repo, "Languages/English/Keyed/TM_FleshDevelopment.xml"))
                .Root!.Elements().ToDictionary(x => x.Name.LocalName, x => new LoadedLanguage.KeyedReplacement { key = x.Name.LocalName, value = x.Value });
            language.keyedReplacements["StatsReport_FinalValue"] = new LoadedLanguage.KeyedReplacement { key = "StatsReport_FinalValue", value = "Final value" };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        }
        public void Dispose() { LanguageDatabase.activeLanguage = old; LanguageDatabase.defaultLanguage = oldDefault; }
    }
}
