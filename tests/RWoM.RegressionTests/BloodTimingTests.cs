using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using System.Runtime.CompilerServices;
using AbilityUser;
using TorannMagic;
using TorannMagic.Compatibility;
using TorannMagic.ModOptions;
using Verse;

namespace RWoM.RegressionTests;
internal static class BloodTimingTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BloodGift.OffMapCastRejectedBeforeEffects", f => OffMap());
        yield return new("Timing.GlobalScopeDefaultsAndInvalidValues", f => Timing());
        yield return new("BloodGift.HeadAndEyesExcluded", f => Head());
        yield return new("Timing.NativeGetterHooks", f => NativeTiming());
        yield return new("Timing.SettingsScribeAndLegacyDefaults", SaveTiming);
    }
    static void Head()
    {
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(BodyPartTagDefOf));
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(RimWorld.BodyPartDefOf));
        var saved = RimWorld.BodyPartDefOf.Head;
        try
        {
            var headDef = new BodyPartDef { defName = "Head", bleedRate = 1, tags = [] };
            RimWorld.BodyPartDefOf.Head = headDef;
            var head = new BodyPartRecord { def = headDef, depth = BodyPartDepth.Outside, coverageAbs = .1f };
            var eye = new BodyPartRecord { def = new BodyPartDef { defName = "Eye", bleedRate = 1, tags = [] }, parent = head, depth = BodyPartDepth.Outside, coverageAbs = .01f };
            var method = typeof(MagicData).Assembly.GetType("TorannMagic.BloodGiftSafety")!.GetMethod("CanCut", BindingFlags.Static | BindingFlags.NonPublic)!;
            var pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            pawn.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            pawn.def.race = new RaceProperties { body = new BodyDef { corePart = head } };
            pawn.health = new Pawn_HealthTracker(pawn);
            foreach (var part in new[] { head, eye })
                Check.Equal(false, (bool)method.Invoke(null, [pawn, part, 6f])!, "head and descendants rejected before damage");
        }
        finally { RimWorld.BodyPartDefOf.Head = saved; }
    }
    static void SaveTiming(RuntimeFixture f)
    {
        var saved = Settings.Instance;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        string path = Path.Combine(f.WorkingDirectory, "timing-settings.xml");
        try
        {
            var settings = new Settings { abilityCooldownMultiplier = .5f, abilityWarmupMultiplier = 2f };
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref settings, "settings"); Scribe.saver.FinalizeSaving(); Scribe.ForceStop();
            Settings? loaded = null;
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "settings"); Scribe.loader.FinalizeLoading(); Scribe.ForceStop();
            Check.Equal(.5f, loaded!.abilityCooldownMultiplier, "cooldown setting persists");
            Check.Equal(2f, loaded.abilityWarmupMultiplier, "warmup setting persists");
            var xml = System.Xml.Linq.XDocument.Load(path);
            xml.Descendants("abilityCooldownMultiplier").Remove(); xml.Descendants("abilityWarmupMultiplier").Remove(); xml.Save(path);
            loaded = null;
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "settings"); Scribe.loader.FinalizeLoading(); Scribe.ForceStop();
            Check.Equal(1f, loaded!.abilityCooldownMultiplier, "old settings default to normal cooldown");
            Check.Equal(1f, loaded.abilityWarmupMultiplier, "old settings default to normal warmup");
        }
        finally { Scribe.ForceStop(); Settings.Instance = saved; System.Globalization.CultureInfo.CurrentCulture = culture; }
    }
    static void NativeTiming()
    {
        var saved = Settings.Instance;
        var a = Assembly.Load("0Harmony"); var ht = a.GetType("HarmonyLib.Harmony")!;
        string id = "rwom.timing." + Guid.NewGuid(); var h = Activator.CreateInstance(ht, id)!;
        try
        {
            var settings = new Settings { abilityCooldownMultiplier = .5f, abilityWarmupMultiplier = 2f };
            foreach (var pair in new[] { (typeof(PawnAbility).GetProperty("MaxCastingTicks")!.GetMethod!, typeof(AbilityCooldownSettingPatch)), (typeof(Verb).GetProperty("WarmupTime")!.GetMethod!, typeof(AbilityWarmupSettingPatch)) })
            {
                var hook = Activator.CreateInstance(a.GetType("HarmonyLib.HarmonyMethod")!, pair.Item2.GetMethod("Postfix")!)!;
                ht.GetMethod("Patch")!.Invoke(h, [pair.Item1, null, hook, null, null]);
            }
            var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
            def.MainVerb = new VerbProperties_Ability { SecondsToRecharge = 10, warmupTime = 2 };
            foreach (var type in new[] { typeof(MagicAbility), typeof(MightAbility), typeof(PawnAbility) })
            {
                var ability = (PawnAbility)RuntimeHelpers.GetUninitializedObject(type);
                FleshStatTests.Set(ability, "powerDef", def);
                ability.CooldownTicksLeft = 123;
                Check.Equal(300, ability.MaxCastingTicks, "real native getter applies cooldown once");
                Check.Equal(123, ability.CooldownTicksLeft, "existing timer is not rescaled");
                var verb = new Verb_BloodGift { Ability = ability, verbProps = def.MainVerb };
                verb.verbTracker = new VerbTracker((Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn)));
                Check.Equal(4f, verb.WarmupTime, "real native getter scales warmup");
                Check.Equal(2f, def.MainVerb.warmupTime, "shared definition is never mutated");
            }
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(h, [id]); Settings.Instance = saved; }
    }
    static void OffMap()
    {
        var pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)-1);
        var verb = new Verb_BloodGift();
        verb.caster = pawn;
        Check.Equal(false, (bool)typeof(Verb_BloodGift).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(verb, null)!, "caravan pawn rejected before health, payment or map effects");
    }
    static void Timing()
    {
        var saved = Settings.Instance;
        try
        {
            var settings = new Settings();
            Check.Equal(100, AbilityTiming.Cooldown(100), "default preserves timing");
            settings.abilityCooldownMultiplier = .5f;
            settings.abilityWarmupMultiplier = 2f;
            var ability = (MagicAbility)RuntimeHelpers.GetUninitializedObject(typeof(MagicAbility));
            var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
            FleshStatTests.Set(ability, "powerDef", def);
            int cooldown = 100;
            AbilityCooldownSettingPatch.Postfix(ability, ref cooldown);
            Check.Equal(50, cooldown, "RWoM cooldown scaled once");
            var verb = new Verb_BloodGift { Ability = ability };
            float warmup = 2;
            AbilityWarmupSettingPatch.Postfix(verb, ref warmup);
            Check.Equal(4f, warmup, "independent warmup scale");
            FleshStatTests.Set(ability, "powerDef", (AbilityUser.AbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(AbilityUser.AbilityDef)));
            cooldown = 100; warmup = 2;
            AbilityCooldownSettingPatch.Postfix(ability, ref cooldown);
            AbilityWarmupSettingPatch.Postfix(verb, ref warmup);
            Check.Equal(100, cooldown, "foreign abilities unchanged");
            Check.Equal(2f, warmup, "foreign warmup unchanged");
            foreach(float bad in new[] {float.NaN, float.PositiveInfinity, float.NegativeInfinity})
                Check.Equal(1f, AbilityTiming.SafeMultiplier(bad), "nonfinite settings use default");
            Check.Equal(.1f, AbilityTiming.SafeMultiplier(-1), "lower bound");
            Check.Equal(5f, AbilityTiming.SafeMultiplier(100), "upper bound");
        }
        finally { Settings.Instance = saved; }
    }
}
