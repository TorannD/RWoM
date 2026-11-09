using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class SummonerTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SummonerTrial.ObservedTimeAndSave", Timing);
        yield return new("SummonerTrial.NativeOwnedSpawnAndMineTrigger", f => Spawns(f, false));
        yield return new("SummonerTrial.HostedBothStagesSaveAndBrokenCustody", f => Spawns(f, true));
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly Faction friendly = new() { def = new FactionDef { isPlayer = true } };
        readonly Faction enemy = new() { def = new FactionDef() };
        public readonly Pawn Caster, Target;
        public Pawn? Host;
        public ThingOwner<Thing>? Holder;
        public HediffComp_SpiritPossession? Possession;
        IDisposable? ui;
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly HediffDef originalPossession = TorannMagicDefOf.TM_SpiritPossessionHD;
        public void EnterHost()
        {
            ui = (IDisposable)Activator.CreateInstance(typeof(SpiritRebirthRuntimeTests).GetNestedType("UiPauseScope", BindingFlags.NonPublic)!)!;
            Find.World.components = [];
            Find.World.UI = (WorldInterface)RuntimeHelpers.GetUninitializedObject(typeof(WorldInterface));
            Find.World.UI.tilePicker = new TilePicker();
            Set(Find.TickManager, "curTimeSpeed", TimeSpeed.Normal);
            var map = Current.Game.Maps[0];
            map.spawnedThings = new ThingOwner<Thing>(map);
            if (DefDatabase<DesignationDef>.DefCount == 0)
                DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "HostedSummonerTest" });
            map.designationManager = new DesignationManager(map);
            Host = PawnWithComps(false, false).pawn;
            Prepare(Host, friendly, 28003);
            Set(Caster, "mapIndexOrState", (sbyte)-1);
            Caster.def.stackLimit = 1;
            Check.Equal(true, SpiritRebirthStateTests.Invoke(Progression, "BeginSpiritRebirth"), "reborn owner");
            var hediff = new HediffWithComps { def = TorannMagicDefOf.TM_SpiritPossessionHD = new HediffDef { defName = "TrialHost" }, pawn = Host };
            var possession = Possession = new HediffComp_SpiritPossession { parent = hediff };
            hediff.comps = [possession]; Host.health.hediffSet.hediffs.Add(hediff);
            Holder = new ThingOwner<Thing>(possession); Set(possession, "innerContainer", Holder);
            Check.Equal(true, Holder.TryAdd(Caster, false), "host holds native pawn");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(Progression.SpiritRebirth, "TryEnterHost", Caster, Host), "reciprocal possession");
            Check.Equal(true, Caster.Map == null && Host.Map != null, "only the host is physically on the map");
        }
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.summoner");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Find.TickManager, "gameStartAbsTick", 1);
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            Set(friendly, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
            Set(enemy, "relations", new List<FactionRelation> { new() { other = friendly, kind = FactionRelationKind.Hostile } });
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic;
            Target = PawnWithComps(false, false).pawn;
            Prepare(Caster, friendly, 28001); Prepare(Target, enemy, 28002);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.summoner", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "SummonerTrialProfile", profileId = "rwom.summoner", domain = DisciplineDomain.Magic });
        }
        void Prepare(Pawn pawn, Faction faction, int id)
        {
            pawn.thingIDNumber = id; pawn.kindDef = new PawnKindDef { defName = "SummonerTrialPawn" };
            pawn.guest = new Pawn_GuestTracker(pawn); pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0); Position(pawn, 0);
        }
        public PawnAbility Ability(TMAbilityDef def, bool owned = true)
        {
            Magic.MagicData.MagicPowersS.Single(p => p.TMabilityDefs.Contains(def)).learned = true;
            var ability = new PawnAbility(Magic); Set(ability, "powerDef", def);
            if (owned) Magic.AbilityData.Powers.Add(ability);
            return ability;
        }
        public void Adept() { Path.RecordCompletedTrial(SummonerTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.summoner", InitiationStage.Foundation, SummonerTrials.FoundationTrialId); }
        public bool Foundation => Path.CompletedTrials.Contains(SummonerTrials.FoundationTrialId);
        public bool Mastery => Path.CompletedTrials.Contains(SummonerTrials.MasteryTrialId);
        public void Dispose()
        {
            ui?.Dispose(); SummonerTrials.EndLaunch(null!); TorannMagicDefOf.TM_SpiritPossessionHD = originalPossession; Current.Game = previous;
            Restore(profiles); Restore(things); Restore(hediffs); Restore(designations);
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var def in defs) DefDatabase<T>.Add(def); }
    }
    static void Position(Thing thing, int x) => Set(thing, "positionInt", new IntVec3(x, 0, 0));
    static ThingDef Definition(string name)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = name;
        return def;
    }
    public sealed class TestProjectile : Projectile_AbilityBase { public void Seed(Pawn pawn) => launcher = pawn; }
    static TestProjectile Projectile(Fixture w, TMAbilityDef def, string name, bool owned = true)
    {
        var ability = w.Ability(def, owned);
        var projectile = new TestProjectile { def = Definition(name) };
        var body = w.Host ?? w.Caster;
        projectile.Seed(body); projectile.localAbilityDef = def;
        if (w.Host != null && owned)
        {
            var source = new CompHostedSummon { parent = projectile };
            Set(source, "projected", true); Set(source, "owner", w.Caster);
            Set(projectile, "comps", new List<ThingComp> { source });
        }
        var verb = new Verb_BLOS { Ability = ability }; Set(verb, "caster", body);
        var previous = SummonerTrials.BeginLaunch(verb, projectile.def);
        try { SummonerTrials.Launched(projectile, body, def); } finally { SummonerTrials.EndLaunch(previous); }
        return projectile;
    }
    static void Spawns(RuntimeFixture f, bool hosted)
    {
        using var w = new Fixture(f);
        if (hosted) w.EnterHost();
        var minion = new TMPawnSummoned { def = w.Caster.def, thingIDNumber = 28111, Spawner = w.Caster, Temporary = true, validSummoning = true };
        minion.health = new Pawn_HealthTracker(minion);
        Set(minion, "mapIndexOrState", (sbyte)0); Set(minion, "factionInt", w.Caster.Faction);
        var copied = Projectile(w, TorannMagicDefOf.TM_SummonMinion, "Projectile_SummonMinion", false);
        SummonerTrials.Spawned(copied, minion, SummonerTrials.Spell.Minion);
        Check.Equal(true, w.Path.SummonerTrials.Minion == null, "item/copy cannot register summon");
        var native = Projectile(w, TorannMagicDefOf.TM_SummonMinion, "Projectile_SummonMinion");
        var projectileSave = RoundTrip(f, new Envelope(), native, null, w.Caster);

        if (hosted)
        {
            w.Holder!.Remove(w.Caster);
            SummonerTrials.Spawned(native, minion, SummonerTrials.Spell.Minion);
            Check.Equal(true, w.Path.SummonerTrials.Minion == null, "in-flight shot cannot earn credit after exit");
            Check.Equal(true, w.Holder.TryAdd(w.Caster, false), "restore the original before a valid impact");
        }
        Set(minion, "mapIndexOrState", (sbyte)-1);
        SummonerTrials.Spawned(native, minion, SummonerTrials.Spell.Minion);
        Check.Equal(true, w.Path.SummonerTrials.Minion == null, "failed unspawned minion excluded");
        Set(minion, "mapIndexOrState", (sbyte)0);
        SummonerTrials.Spawned(native, minion, SummonerTrials.Spell.Minion);
        Check.Equal(true, w.Path.SummonerTrials.Minion == minion, "native successful owned minion registers");
        var pylon = new ThingWithComps { def = Definition("DefensePylon"), thingIDNumber = 28112 };
        Set(pylon, "comps", new List<ThingComp> { new CompSummoned { parent = pylon, Spawner = w.Caster, Temporary = true } });
        Set(pylon, "mapIndexOrState", (sbyte)0); Set(pylon, "factionInt", w.Caster.Faction);
        var pylonShot = Projectile(w, TorannMagicDefOf.TM_SummonPylon, "Projectile_SummonPylon");
        if (hosted)
        {
            pylon.TryGetComp<CompSummoned>().Spawner = w.Host;
            SummonerTrials.Spawned(pylonShot, pylon, SummonerTrials.Spell.Pylon);
            Check.Equal(true, w.Path.SummonerTrials.Pylon == null, "host-owned pylon cannot qualify for soul's trial");
            pylon.TryGetComp<CompSummoned>().Spawner = w.Caster;
        }
        SummonerTrials.Spawned(pylonShot, pylon, SummonerTrials.Spell.Pylon);
        Check.Equal(true, w.Path.SummonerTrials.Pylon == pylon, "native pylon registers");
        Check.Equal(false, w.Foundation, "placement alone is not a trial");
        w.Path.SummonerTrials.ControlledTicks = 120;
        var replacement = new TMPawnSummoned { def = w.Caster.def, thingIDNumber = 28114, Spawner = w.Caster, Temporary = true, validSummoning = true };
        replacement.health = new Pawn_HealthTracker(replacement);
        Set(replacement, "mapIndexOrState", (sbyte)0); Set(replacement, "factionInt", w.Caster.Faction);
        SummonerTrials.Spawned(native, replacement, SummonerTrials.Spell.Minion);
        Check.Equal(0, w.Path.SummonerTrials.ControlledTicks, "replacement minion restarts observation");
        SummonerTrials.Spawned(native, minion, SummonerTrials.Spell.Minion);
        Set(pylon, "factionInt", w.Target.Faction);
        SummonerTrials.Observe(w.Caster, 0); SummonerTrials.Observe(w.Caster, 30);
        Check.Equal(0, w.Path.SummonerTrials.ControlledTicks, "captured pylon does not count");
        Set(pylon, "factionInt", w.Caster.Faction);
        w.Path.SummonerTrials.LastTick = -1; w.Path.SummonerTrials.LastActive = false;
        SummonerTrials.Observe(w.Caster, 0);
        if (hosted) w.Holder!.Remove(w.Caster);
        else w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless));
        SummonerTrials.Observe(w.Caster, 30);
        if (hosted) Check.Equal(true, w.Holder!.TryAdd(w.Caster, false), "restore custody after missed interval");
        else w.Caster.story.traits.allTraits.Clear();
        SummonerTrials.Observe(w.Caster, 60);
        Check.Equal(0, w.Path.SummonerTrials.ControlledTicks, "temporary ownership loss breaks interval");
        for (int tick = 90; tick <= 630; tick += 30)
        {
            if (!hosted) { SummonerTrials.Observe(w.Caster, tick); continue; }
            Set(Find.TickManager, "ticksGameInt", tick);
            float severity = 0;
            w.Possession!.CompPostTick(ref severity);
            w.Possession.CompPostTick(ref severity);
        }
        Check.Equal(570, w.Path.SummonerTrials.ControlledTicks, "bounded native coexistence observed");
        var saved = RoundTrip(f, new Envelope { progress = w.Path.SummonerTrials }, null, null, minion, pylon);
        Check.Equal(true, ReferenceEquals(minion, saved.progress.Minion), "saved minion resolves original reference without duplication");
        Check.Equal(true, ReferenceEquals(pylon, saved.progress.Pylon), "saved pylon resolves original reference without duplication");
        Check.Equal(570, saved.progress.ControlledTicks, "non-null reference progress survives Scribe");
        SummonerTrials.Observe(w.Caster, 660);
        Check.Equal(true, w.Foundation, "600 active ticks completes foundation");
        w.Progression.State.TryAdvanceAfterTrial("rwom.summoner", InitiationStage.Foundation, SummonerTrials.FoundationTrialId);
        var mine = new Building_ExplosiveProximityTrap { def = Definition("TM_ManaMine"), thingIDNumber = 28113 };
        Set(mine, "comps", new List<ThingComp> { new CompSummoned { parent = mine, Spawner = w.Caster, Temporary = true }, new CompExplosive { parent = mine } });
        Set(mine, "mapIndexOrState", (sbyte)0); Set(mine, "factionInt", w.Caster.Faction);
        var mineShot = Projectile(w, TorannMagicDefOf.TM_SummonExplosive, "Projectile_SummonExplosive");
        SummonerTrials.Spawned(mineShot, mine, SummonerTrials.Spell.Explosive);
        Set(mine, "trapSprung", true);
        SummonerTrials.WickStarted(mine, w.Target);
        Check.Equal(false, w.Mastery, "manual wick without native proximity witness excluded");
        SummonerTrials.Triggered(mine, w.Caster); SummonerTrials.WickStarted(mine, w.Caster);
        Check.Equal(false, w.Mastery, "friendly trigger excluded");
        SummonerTrials.Triggered(mine, w.Target);
        Check.Equal(false, w.Mastery, "trigger alone waits for wick");
        RoundTrip(f, new Envelope(), null, mine, w.Caster, w.Target);
        mine.TryGetComp<CompExplosive>().wickStarted = true;
        typeof(Building_ExplosiveProximityTrap).GetMethod("SpringSub", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(mine, [w.Target]);
        Check.Equal(false, w.Mastery, "already lit mine cannot earn a new wick transition");
        mine.TryGetComp<CompExplosive>().wickStarted = false;
        SummonerTrials.WickStarted(mine, w.Target);
        Check.Equal(false, w.Mastery, "refused wick does not complete");
        mine.TryGetComp<CompExplosive>().wickStarted = true;
        if (hosted)
        {
            w.Holder!.Remove(w.Caster);
            SummonerTrials.WickStarted(mine, w.Target);
            Check.Equal(false, w.Mastery, "lost host after the triggering contact cannot award mastery");
            Check.Equal(true, w.Holder.TryAdd(w.Caster, false), "restore exact original");
            SummonerTrials.Triggered(mine, w.Target);
        }
        SummonerTrials.WickStarted(mine, w.Target); SummonerTrials.WickStarted(mine, w.Target);
        Check.Equal(true, w.Mastery, "owned hostile trigger and wick completes once");
    }
    public sealed class Envelope : IExposable
    {
        public SummonerTrialProgress progress = new();
        public static TestProjectile? projectile;
        public static Building_ExplosiveProximityTrap? mine;
        public void ExposeData()
        {
            Scribe_Deep.Look(ref progress, "progress");
            if (projectile != null) SummonerTrials.ExposeProjectile(projectile);
            if (mine != null) SummonerTrials.ExposeMine(mine);
        }
    }
    static Envelope RoundTrip(RuntimeFixture f, Envelope data, TestProjectile? projectile, Building_ExplosiveProximityTrap? mine, params Thing[] references)
    {
        Envelope.projectile = projectile; Envelope.mine = mine;
        var file = Path.Combine(f.WorkingDirectory, "summoner-refs-" + Guid.NewGuid() + ".xml");
        try
        {
            try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            if (projectile != null) SummonerTrials.Launched(projectile, null!, null!);
            Envelope loaded = null!;
            try
            {
                Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach (var reference in references) directory.RegisterLoaded(reference);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            return loaded;
        }
        finally { Envelope.projectile = null; Envelope.mine = null; }
    }
    static void Timing(RuntimeFixture f)
    {
        var progress = new SummonerTrialProgress();
        progress.Observe(true, 0); progress.Observe(true, 30); progress.Observe(true, 30);
        Check.Equal(30, progress.ControlledTicks, "duplicate observation cannot double credit");
        progress.Observe(false, 40); progress.Observe(true, 60); progress.Observe(true, 90);
        Check.Equal(60, progress.ControlledTicks, "inactive intervals not credited");
        progress.Observe(true, 1000);
        Check.Equal(60, progress.ControlledTicks, "no offline catchup");
        var file = Path.Combine(f.WorkingDirectory, "summoner-time.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref progress, "progress"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        SummonerTrialProgress loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "progress"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Check.Equal(60, loaded.ControlledTicks, "partial progress persists");
        loaded.Observe(true, 1030);
        Check.Equal(60, loaded.ControlledTicks, "load boundary pauses observation");
        for (int tick = 1060; tick <= 2000; tick += 30) loaded.Observe(true, tick);
        Check.Equal(600, loaded.ControlledTicks, "observation bounded at target");
    }
}
