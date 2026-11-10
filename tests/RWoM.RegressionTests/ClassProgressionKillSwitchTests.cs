using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// Contract for the "new progression (beta)" kill switch: with
// ClassProgressionFeature.Enabled = false the mod must behave like the classic
// system. Program.Main enables the flag for the whole suite, so every case here
// disables it locally and restores it via a finally block.
internal static class ClassProgressionKillSwitchTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("KillSwitch.BootstrapReadsControllerSettingsBeforeItsConstructor", BootstrapSettings);
        yield return new("KillSwitch.DisabledSaveRetainsProgression", EnsureCompSkips);
        yield return new("KillSwitch.IdentityFallsBackToClassicTraits", IdentityFallback);
        yield return new("KillSwitch.AuraChainAppliesClassicSeverity", AuraSeverity);
        yield return new("KillSwitch.GravitySlowStillApplied", GravitySlow);
        yield return new("KillSwitch.WeatherTransitionStillNative", WeatherNative);
        yield return new("KillSwitch.SpellAccessClassicDefaults", SpellAccessDefaults);
    }

    private static void BootstrapSettings(RuntimeFixture f)
    {
        // Match the two Mod instances and the real filename discriminator RimWorld uses.
        var path=typeof(GenFilePaths).GetField("saveDataPath",BindingFlags.Static|BindingFlags.NonPublic)!;
        var previous=path.GetValue(null);var original=TorannMagic.ModOptions.Settings.Instance;var enabled=ClassProgressionFeature.Enabled;
        try
        {
            var profile=Path.Combine(f.WorkingDirectory,"profile");
            Directory.CreateDirectory(Path.Combine(profile,"Config"));path.SetValue(null,profile);
            var pack=(ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
            Set(pack,"rootDirInt",new DirectoryInfo(Path.Combine(f.WorkingDirectory,"SampleRWoM")));
            var bootstrap=(TorannMagicMod)RuntimeHelpers.GetUninitializedObject(typeof(TorannMagicMod));
            Set(bootstrap,"intContent",pack);
            File.WriteAllText(Path.Combine(profile,"Config","Mod_SampleRWoM_Controller.xml"),
                "<SettingsBlock><ModSettings Class=\"TorannMagic.ModOptions.Settings\"><newProgressionBeta>True</newProgressionBeta></ModSettings></SettingsBlock>");
            TorannMagic.ModOptions.Settings.Instance=null;
            ClassProgressionFeature.SnapshotFromSettings(bootstrap);
            Check.Equal(true,ClassProgressionFeature.Enabled,"early bootstrap reads the owner of the saved setting");
            TorannMagic.ModOptions.Settings.Instance.newProgressionBeta=false;
            ClassProgressionFeature.SnapshotFromSettings(bootstrap);
            Check.Equal(false,ClassProgressionFeature.Enabled,"constructed settings owner takes precedence");
        }
        finally {path.SetValue(null,previous);TorannMagic.ModOptions.Settings.Instance=original;ClassProgressionFeature.Enabled=enabled;}
    }

    private sealed class Flag : IDisposable
    {
        public Flag() => ClassProgressionFeature.Enabled = false;
        public void Dispose() => ClassProgressionFeature.Enabled = true;
    }

    // Minimal game shell (same pattern as IceTrialTests.Fixture) so HealthUtility
    // and the weather manager can run headless.
    private sealed class GameEnv : IDisposable
    {
        readonly Game previous = Current.Game;
        public GameEnv()
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Find.TickManager, "gameStartAbsTick", 1);
            Set(Current.Game, "battleLog", new BattleLog());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager();
            Set(Current.Game, "worldInt", world);
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
        }
        public void Dispose() => Current.Game = previous;
    }

    private static ThingDef HumanDef()
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "KillSwitchHuman";
        def.category = ThingCategory.Pawn; def.thingClass = typeof(Pawn);
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        return def;
    }

    private static bool HasProgressionComp(ThingDef def)
        => def.comps?.Any(c => c?.compClass == typeof(CompClassProgression)) == true;

    private static void EnsureCompSkips(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        // Positive control: with the suite default (enabled) the comp is injected.
        var enabledDef = HumanDef();
        ClassProgressionBootstrap.EnsureComp(enabledDef);
        Check.Equal(true, HasProgressionComp(enabledDef), "enabled feature injects the progression comp");

        using (new Flag())
        {
            var def = HumanDef();
            ClassProgressionBootstrap.EnsureComp(def);
            Check.Equal(true, HasProgressionComp(def), "disabled feature keeps the save carrier");
            Check.Equal(false, new CompClassProgression().BeginNewProgression(Vocation.Mage), "disabled enrollment");
        }
        var original = new Carrier();
        original.Comp.BeginNewProgression(Vocation.Mage);
        original.Comp.State.TryAddFoundation("rwom.fire", DisciplineDomain.Magic);
        original.Comp.State.TryChooseNoviceSpell("rwom.first");
        original.Comp.DelayNextTomeQuest(123);
        string path = System.IO.Path.Combine(f.WorkingDirectory, "beta.xml");
        void Save(Carrier value)
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref value, "carrier");
            Scribe.saver.FinalizeSaving();
        }
        Carrier Load()
        {
            Carrier value = null;
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref value, "carrier");
            Scribe.loader.FinalizeLoading();
            return value;
        }
        Save(original);
        using (new Flag())
        {
            var paused = Load();
            paused.Comp.CompTick();
            Save(paused);
        }
        var restored = Load();
        Check.Equal(InitiationStage.Foundation, restored.Comp.State.Find("rwom.fire").Stage, "discipline survives disabled resave");
        Check.Equal("rwom.first", restored.Comp.State.NoviceSpells.Single(), "novice choice survives disabled resave");
        Check.Equal(60123, restored.Comp.TomeQuestAvailableAtTick, "quest timer survives disabled resave");
    }

    public sealed class Carrier : IExposable
    {
        public CompClassProgression Comp = new();
        public void ExposeData() => Comp.PostExposeData();
    }

    private static void IdentityFallback(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var traitPawn = PawnWithComps(true, false).pawn;
        traitPawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Priest));

        // No progression state anywhere: the classic trait fallback applies either way.
        Check.Equal(true, ClassIdentity.IsPriest(traitPawn), "trait fallback works while enabled");
        using (new Flag())
        {
            Check.Equal(true, ClassIdentity.IsPriest(traitPawn), "trait fallback preserved while disabled");
        }

        // A pawn with ONLY progression state (no legacy trait) must resolve classic
        // (not a priest) while disabled, and as a priest again once enabled.
        var statePawn = PawnWithComps(true, false).pawn;
        var progression = new CompClassProgression { parent = statePawn };
        statePawn.AllComps.Add(progression);
        progression.BeginNewProgression(Vocation.Mage);
        progression.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
        Check.Equal(true, ClassIdentity.IsPriest(statePawn), "state counts while enabled");
        using (new Flag())
        {
            Check.Equal(false, ClassIdentity.IsPriest(statePawn), "disabled flag ignores progression state");
        }
        Check.Equal(true, ClassIdentity.IsPriest(statePawn), "state counts again once the flag is restored");
    }

    private static void AuraSeverity(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var previousAura = TorannMagicDefOf.TM_SoothingBreeze_AuraHD;
        var previousEffect = TorannMagicDefOf.SoothingBreeze;
        try
        {
            TorannMagicDefOf.TM_SoothingBreeze_AuraHD = new HediffDef { defName = "KillSwitchAura", hediffClass = typeof(Hediff) };
            TorannMagicDefOf.SoothingBreeze = new HediffDef { defName = "KillSwitchSoothing", hediffClass = typeof(Hediff), maxSeverity = 1 };
            using var env = new GameEnv();
            var caster = PawnWithComps(true, false).pawn;  // no progression comp: the trial path is null
            var target = PawnWithComps(false, false).pawn;
            // A detached source hediff: no aura tag exists, so the trial path is null
            // and only the classic mutation must run.
            var source = new Hediff { def = TorannMagicDefOf.TM_SoothingBreeze_AuraHD, pawn = caster };
            var effect = new Hediff { def = TorannMagicDefOf.SoothingBreeze, pawn = target };
            target.health.hediffSet.hediffs.Add(effect);
            Set(effect, "severityInt", 0f);
            using (new Flag())
            {
                // The chain head runs: the classic mutation inside IceTrials.ApplyAura
                // must still fire even though no trial can be recorded.
                PaladinTrials.ApplyAura(source, target, effect.def);
            }
            Check.Equal(1f, effect.Severity, "classic aura severity still applied while disabled");
        }
        finally
        {
            TorannMagicDefOf.TM_SoothingBreeze_AuraHD = previousAura;
            TorannMagicDefOf.SoothingBreeze = previousEffect;
        }
    }

    private static void GravitySlow(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var previousSlow = TorannMagicDefOf.TM_GravitySlowHD;
        try
        {
            TorannMagicDefOf.TM_GravitySlowHD = new HediffDef { defName = "KillSwitchGravity", hediffClass = typeof(Hediff), maxSeverity = 1 };
            using var env = new GameEnv();
            var victim = PawnWithComps(false, false).pawn;
            var slow = new Hediff { def = TorannMagicDefOf.TM_GravitySlowHD, pawn = victim };
            victim.health.hediffSet.hediffs.Add(slow);
            Set(slow, "severityInt", 0f);
            using (new Flag())
            {
                DemonTrials.AdjustSlow(null, victim, 0.4f);
            }
            Check.Equal(0.4f, slow.Severity, "classic gravity slow severity still applied while disabled");
        }
        finally { TorannMagicDefOf.TM_GravitySlowHD = previousSlow; }
    }

    private static void WeatherNative(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(WeatherDefOf));
        var clear = new WeatherDef { defName = "KillSwitchClear" };
        var rain = new WeatherDef { defName = "KillSwitchRain" };
        using var env = new GameEnv();
        var caster = PawnWithComps(true, false).pawn;
        var map = Find.Maps[0];
        map.weatherManager = new WeatherManager(map) { curWeather = clear };
        using (new Flag())
        {
            IceTrials.TransitionWeather(caster, map, rain);
        }
        Check.Same(rain, map.weatherManager.curWeather, "native weather transition still runs while disabled");
    }

    private static void SpellAccessDefaults(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var (pawn, magic, _) = PawnWithComps(true, false);
        var power = magic.MagicData.AllMagicPowersWithSkills.First(p => !p.learned);
        power.learned = false;
        using (new Flag())
        {
            Check.Equal(true, ClassSpellAccess.CanLearn(pawn, power), "disabled flag keeps classic learnability");
            Check.Equal(false, ClassSpellAccess.RestoreLearned(magic), "disabled flag defers to the classic restore path");
        }
        Check.Equal(true, ClassSpellAccess.CanLearn(pawn, power), "learnability unchanged after restore");
    }
}
