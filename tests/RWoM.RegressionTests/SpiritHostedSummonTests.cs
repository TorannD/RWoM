using System.Reflection;
using System.Xml.Linq;
using AbilityUser;
using Verse.AI;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static partial class SpiritMapCollapseTests
{
    sealed class SummonCompSave : IExposable
    {
        public CompHostedSummon Shot = new();
        public CompLeaper Poppi = new();
        public void ExposeData() { Shot.PostExposeData(); Poppi.PostExposeData(); }
    }

    private static T RoundTripSummon<T>(RuntimeFixture r, string name, T before, params ILoadReferenceable[] references) where T : class, IExposable
    {
        var path = Path.Combine(r.WorkingDirectory, name + ".xml");
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref before, "summon"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            T after = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref after, "summon");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                foreach (var value in references) directory.RegisterLoaded(value);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            return after;
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
    }

    static bool NativeAbilityOwner(Verb __instance, ref IVerbOwner __result) => AbilityUserMod.get_DirectOwner_Prefix(__instance, ref __result);
    static Func<PawnKindDef, Pawn>? nextGeneratedSummon;
    static bool GenerateSummonedPawn(PawnKindDef __0, ref Pawn __result) { __result = nextGeneratedSummon!(__0); return false; }

    private static TMPawnSummoned PreparedSummon(Pawn soul, int id, bool poppi = false)
    {
        var pawn = new TMPawnSummoned { def = soul.def, kindDef = soul.kindDef, thingIDNumber = id };
        pawn.ageTracker = new Pawn_AgeTracker(pawn); pawn.health = new Pawn_HealthTracker(pawn);
        pawn.needs = new Pawn_NeedsTracker(pawn); pawn.mindState = new Pawn_MindState(pawn);
        pawn.stances = new Pawn_StanceTracker(pawn); pawn.rotationTracker = new Pawn_RotationTracker(pawn);
        pawn.jobs = new Pawn_JobTracker(pawn); pawn.pather = new Pawn_PathFollower(pawn);
        Set(pawn, "factionInt", soul.Faction);
        if (poppi) Set(pawn, "comps", new List<ThingComp> { new CompLeaper { parent = pawn } });
        return pawn;
    }

    private static void ReplaySummon(string family, RuntimeFixture r, SpiritRebirthRitualTransactionTests.Context c, Projectile_AbilityBase launched, IntVec3 cell)
    {
        var soul = c.Original; var host = c.Creator; var magic = soul.GetCompAbilityUserMagic();
        var oldWind = SoundDefOf.Ambient_AltitudeWind;
        var changedRaces = new Dictionary<FieldInfo, object?>();
        var kinds = DefDatabase<PawnKindDef>.AllDefsListForReading.ToArray();
        var abilityDefs = DefDatabase<AbilityUser.AbilityDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<ThingDef>.Add(launched.def);
            if (DefDatabase<AbilityUser.AbilityDef>.GetNamedSilentFail(launched.localAbilityDef.defName) == null)
                DefDatabase<AbilityUser.AbilityDef>.Add(launched.localAbilityDef);
            var replay = RoundTripSummon(r, "paid-" + family + "-flight", launched, host, soul, soul.Faction, c.Map);
            launched.DeSpawn();
            GenSpawn.Spawn(replay, cell, c.Map, Rot4.North, WipeMode.Vanish, true);
            var stone = (Thing_SoulStone)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("TM_SoulStone"));
            var capture = typeof(Thing_SoulStone).GetMethod("TryCapture", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check.Equal(true, (bool)capture.Invoke(stone, [soul, host, false])!, "before impact the actual original moves from its host to a stone");
            Check.Same(soul, stone.ContainedSpirit, "stone owns the caster");
            var levels = (List<MagicPowerSkill>)typeof(MagicData).GetProperty("MagicPowerSkill_Summon" + family)!.GetValue(magic.MagicData)!;
            levels.Single(s => s.label == "TM_Summon" + family + "_pwr").level = 3;
            var expectedKinds = new List<string>();
            void RegisterKind(string name, string fieldName)
            {
                var field = typeof(TorannMagicDefOf).GetField(fieldName)!;
                changedRaces[field] = field.GetValue(null); field.SetValue(null, soul.def);
                if (DefDatabase<PawnKindDef>.GetNamedSilentFail(name) == null)
                    DefDatabase<PawnKindDef>.Add(new PawnKindDef { defName = name, race = soul.def });
            }
            if (family == "Minion")
            {
                RegisterKind("TM_Minion", "TM_MinionR");
                expectedKinds.AddRange(Enumerable.Repeat("TM_Minion", 2));
            }
            else if (family == "Elemental")
            {
                int roll = (int)typeof(CompHostedSummon).GetProperty("ElementRoll", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(replay.GetComp<CompHostedSummon>())!;
                string element = new[] { "Earth", "Fire", "Water", "Wind" }[roll/2];
                string lesser = "TM_Lesser" + element + "_Elemental", normal = "TM_" + element + "_Elemental";
                RegisterKind(lesser, lesser + "R"); RegisterKind(normal, normal + "R");
                expectedKinds.AddRange([lesser, lesser, normal]);
            }
            else if (family == "Poppi")
            {
                RegisterKind("TM_Poppi", "TM_Poppi");
                expectedKinds.AddRange(Enumerable.Repeat("TM_Poppi", 5));
            }
            else
            {
                var structure = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                structure.defName = family == "Pylon" ? "DefensePylon_I" : "TM_ManaMine_I";
                structure.thingClass = typeof(ThingWithComps); structure.size = new IntVec2(1,1); structure.stackLimit = 1;
                structure.category = ThingCategory.Building; structure.building = new BuildingProperties { isEdifice = false };
                structure.destroyable = true; structure.drawerType = DrawerType.RealtimeOnly; structure.comps = [new CompProperties_Summoned()];
                DefDatabase<ThingDef>.Add(structure);
            }
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(FactionDefOf));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(ClamorDefOf));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(SoundDefOf));
            SoundDefOf.Ambient_AltitudeWind = new SoundDef { defName = "SummonWind" };
            using var smoke = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowSmoke), [typeof(UnityEngine.Vector3), typeof(Map), typeof(float)])!, nameof(CancelNative));
            using var spark = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowMicroSparks), [typeof(UnityEngine.Vector3), typeof(Map)])!, nameof(CancelNative));
            using var glow = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowHeatGlow), [typeof(IntVec3), typeof(Map), typeof(float)])!, nameof(CancelNative));
            using var generation = new Patch(typeof(PawnGenerator).GetMethods().Single(m => m.Name == "GeneratePawn" && m.GetParameters().Length == 3), nameof(GenerateSummonedPawn));
            using var fire = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowFireGlow), [typeof(UnityEngine.Vector3), typeof(Map), typeof(float)])!, nameof(CancelNative));
            using var tornado = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowTornadoDustPuff), [typeof(UnityEngine.Vector3), typeof(Map), typeof(float), typeof(UnityEngine.Color)])!, nameof(CancelNative));
            using var clamor = new Patch(typeof(GenClamor).GetMethod(nameof(GenClamor.DoClamor), [typeof(Thing), typeof(float), typeof(ClamorDef)])!, nameof(CancelNative));
            var made = new List<TMPawnSummoned>();
            var selected = new List<string>();
            nextGeneratedSummon = kind =>
            {
                selected.Add(kind.defName);
                var pawn = PreparedSummon(soul, 81100 + made.Count, family == "Poppi");
                made.Add(pawn); return pawn;
            };
            var impact = replay.GetType().GetMethod("Impact", BindingFlags.Instance | BindingFlags.NonPublic)!;
            impact.Invoke(replay, [null, false]);
            Check.Equal(true, selected.SequenceEqual(expectedKinds), "native variant, count and rank survive paid flight for " + family);
            if (expectedKinds.Count == 0)
            {
                var structure = cell.GetThingList(c.Map).Single(t => t.def.defName == (family == "Pylon" ? "DefensePylon_I" : "TM_ManaMine_I"));
                Check.Same(soul, structure.TryGetComp<CompSummoned>().Spawner, "loaded shot creates a ranked structure for the soul now in a stone");
            }
            Check.Equal(true, made.All(p => p.Spawner == soul && p.Map == c.Map && p.Temporary), "actual generated creatures retain original custody");
            if (family == "Poppi") Check.Equal(true, made.All(p => Math.Abs(p.GetComp<CompLeaper>().explosionRadius - 2.2f) < .00001f), "saved versatility affects each Poppi blast");
            Check.Equal(family == "Minion" ? 2 : 0, magic.summonedMinions.Count, "only minions enter the stone owner's native upkeep");
            Check.Equal(0, host.GetCompAbilityUserMagic().summonedMinions.Count, "launcher has no upkeep entries");
            impact.Invoke(replay, [null, false]);
            Check.Equal(true, selected.SequenceEqual(expectedKinds), "consumed impact never spawns another group");
        }
        finally
        {
            nextGeneratedSummon = null;
            foreach (var entry in changedRaces) entry.Key.SetValue(null, entry.Value);
            SoundDefOf.Ambient_AltitudeWind = oldWind;
            DefDatabase<PawnKindDef>.Clear(); foreach (var k in kinds) DefDatabase<PawnKindDef>.Add(k);
            DefDatabase<AbilityUser.AbilityDef>.Clear(); foreach (var d in abilityDefs) DefDatabase<AbilityUser.AbilityDef>.Add(d);
        }
    }

    private static IEnumerable<TestCase> HostedSummonCases()
    {
        foreach (var kind in new[] { "Minion", "Elemental", "Poppi" })
            yield return new("SpiritProjectedSummon.NativeSpawn." + kind, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var custody = new Fixture(r, c);
                var soul = c.Original; var magic = soul.GetCompAbilityUserMagic();
                soul.def.destroyable = true;
                var native = typeof(CompHostedSummon).Assembly.GetType("TorannMagic.Projectile_Summon" + kind)!;
                using var shadow = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                var shot = (Projectile_AbilityBase)Activator.CreateInstance(native)!;
                shot.def = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                shot.def.defName = "TestSummon" + kind;
                Set(shot, "launcher", c.Creator); Set(shot, "mapIndexOrState", (sbyte)0); Set(shot, "pawn", soul);
                if (kind == "Minion") Set(shot, "comp", magic);
                var cell = new IntVec3(15, 0, 15);
                Set(shot, "positionInt", cell);
                var dismissType = TorannMagicDefOf.TM_DismissMinion.abilityClass;
                TorannMagicDefOf.TM_DismissMinion.abilityClass = typeof(MagicAbility);
                TMPawnSummoned? generated = null;
                int id = 79500;
                try
                {
                    nextGeneratedSummon = _ =>
                    {
                        generated = PreparedSummon(soul, id++, kind == "Poppi");
                        return generated;
                    };
                    using var generation = new Patch(typeof(PawnGenerator).GetMethods().Single(m => m.Name == "GeneratePawn" && m.GetParameters().Length == 3), nameof(GenerateSummonedPawn));
                    var settings = new SpawnThings { def = soul.def, kindDef = soul.kindDef };
                    native.GetMethod("SingleSpawnLoop")!.Invoke(shot, [settings, cell, c.Map]);
                    Check.NotNull(generated, "native spawn requested a pawn of the selected kind");
                    Check.Same(c.Map, generated!.Map, "normal GenSpawn placed the generated pawn");
                    Check.Same(soul, generated.Spawner, "unspawned soul owns the generated creature, not the physical launcher");
                    Check.Equal(true, generated.Temporary && generated.TicksLeft == generated.TicksToDestroy && generated.TicksLeft > 0, "fresh native duration");
                    Check.Equal(kind == "Minion", magic.summonedMinions.Contains(generated), "only minions enter original upkeep list");
                    Check.Equal(false, c.Creator.GetCompAbilityUserMagic().summonedMinions.Contains(generated), "no duplicate host ledger");
                    if (kind == "Minion")
                    {
                        using var language = new FleshStatTests.LanguageScope(r.Repository);
                        LanguageDatabase.activeLanguage.keyedReplacements["TM_SummonedCreatureLimitExceeded"] = new LoadedLanguage.KeyedReplacement { key = "TM_SummonedCreatureLimitExceeded", value = "{0} dismissed the oldest minion." };
                        Set(Find.TickManager, "gameStartAbsTick", 1);
                        var oldest = generated;
                        var foreign = PreparedSummon(soul, id++);
                        foreign.Spawner = c.Creator;
                        magic.summonedMinions.Add(foreign);
                        magic.summonedMinions.Add(oldest);
                        using var smoke = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowSmoke), [typeof(UnityEngine.Vector3), typeof(Map), typeof(float)])!, nameof(CancelNative));
                        using var glow = new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowHeatGlow), [typeof(IntVec3), typeof(Map), typeof(float)])!, nameof(CancelNative));
                        using var message = new Patch(typeof(Messages).GetMethods().Single(m => m.Name == "Message" && m.GetParameters() is var pars &&
                            pars.Length == 3 && pars[1].ParameterType == typeof(MessageTypeDef) && pars[2].ParameterType == typeof(bool)), nameof(CancelNative));
                        for (int i = 0; i < 4; i++) native.GetMethod("SingleSpawnLoop")!.Invoke(shot, [settings, cell, c.Map]);
                        Check.Equal(false, foreign.Destroyed || magic.summonedMinions.Contains(foreign), "stale foreign entry removed without destroying another owner's pawn");
                        Check.Equal(4, magic.summonedMinions.Count, "fifth successful native spawn evicts oldest");
                        Check.Equal(true, oldest.Destroyed && !magic.summonedMinions.Contains(oldest), "expired owner reference removed");
                    }
                    RuntimeLog.AssertClean();
                }
                finally { nextGeneratedSummon = null; TorannMagicDefOf.TM_DismissMinion.abilityClass = dismissType; }
            }));
        foreach (bool mine in new[] { false, true })
            yield return new("SpiritProjectedSummon.NativeSpawn." + (mine ? "Mine" : "Pylon"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var custody = new Fixture(r, c);
                RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(FactionDefOf));
                var def = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                def.defName = mine ? "TM_ManaMine_I" : "DefensePylon_I";
                def.size = new IntVec2(1, 1); def.stackLimit = 1; def.category = ThingCategory.Building;
                def.destroyable = true; def.drawerType = DrawerType.RealtimeOnly;
                def.thingClass = typeof(ThingWithComps); def.comps = [new CompProperties_Summoned()];
                def.building = new BuildingProperties { isEdifice = false };
                var cell = new IntVec3(16, 0, 16);
                var shot = mine ? (Projectile_AbilityBase)new Projectile_SummonExplosive() : new Projectile_SummonPylon();
                using var shadow = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
                Set(shot, "launcher", c.Creator);
                var settings = new SpawnThings { def = def };
                if (mine) Projectile_SummonExplosive.SingleSpawnLoop(settings, cell, c.Map, c.Original, 4200, (Projectile_SummonExplosive)shot);
                else
                {
                    Set(shot, "duration", 4200);
                    shot.GetType().GetMethod("SpawnOwned", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(shot, [settings, cell, c.Map, c.Original]);
                }
                var building = cell.GetThingList(c.Map).Single(t => t.def == def);
                var owner = building.TryGetComp<CompSummoned>();
                Check.Same(c.Original, owner.Spawner, "exact held soul owns the placed structure");
                Check.Same(c.Original.GetCompAbilityUserMagic(), owner.CompSummoner, "structure resolves original pool");
                Check.Equal(true, owner.Temporary && owner.TicksToDestroy == 4200, "native duration and temporary state");
                Check.Equal(0, c.Creator.GetCompAbilityUserMagic().summonedMinions.Count, "structures do not count as host minions");
                var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
                try
                {
                    DefDatabase<ThingDef>.Add(def);
                    Set(Find.TickManager, "ticksGameInt", 1);
                    owner.CompTick();
                    Set(owner, "ticksLeft", 1670);
                    var loaded = RoundTripSummon(r, mine ? "mine" : "pylon", (ThingWithComps)building, c.Original, c.Original.Faction, c.Map);
                    var restored = loaded.GetComp<CompSummoned>();
                    restored.CompTick();
                    Check.Same(c.Original, restored.Spawner, "structure Scribe preserves the unspawned soul's reference");
                    Check.Equal(1670, restored.TicksLeft, "loaded structure keeps its elapsed lifetime");
                }
                finally { DefDatabase<ThingDef>.Clear(); foreach (var d in defs) DefDatabase<ThingDef>.Add(d); }
                RuntimeLog.AssertClean();
            }));
        foreach (var family in new[] { "Minion", "Elemental", "Pylon", "Explosive", "Poppi", "SpiritWolves", "SpiritWolves_I", "SpiritWolves_II", "SpiritWolves_III" })
        yield return new("SpiritProjectedSummon.CastJob." + family, r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var custody = new Fixture(r, c);
            using var shadow = new Patch(typeof(MaterialPool).GetMethod("MatFrom", [typeof(string), typeof(UnityEngine.Shader)])!, nameof(CancelNative));
            var soul = c.Original; var body = c.Creator;
            body.def.race.lifeStageAges = soul.def.race.lifeStageAges;
            body.ageTracker ??= new Pawn_AgeTracker(body);
            body.ageTracker.AgeBiologicalTicks = 25L * GenDate.TicksPerYear;
            body.pather ??= new Pawn_PathFollower(body);
            var magic = soul.GetCompAbilityUserMagic();
            using var ownership = new Patch(typeof(Verb).GetProperty(nameof(Verb.DirectOwner))!.GetMethod!, nameof(NativeAbilityOwner));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(WeatherDefOf));
            c.Map.weatherManager = new WeatherManager(c.Map) { curWeather = new WeatherDef { defName = "SchoolClear" } };
            bool pack = family.StartsWith("SpiritWolves", StringComparison.Ordinal);
            var def = (TMAbilityDef)typeof(TorannMagicDefOf).GetField("TM_" + (pack ? family : "Summon" + family))!.GetValue(null)!;
            var oldWolfRace = TorannMagicDefOf.TM_SpiritWolfR; var oldWolfKind = TorannMagicDefOf.TM_SpiritWolf;
            var oldEnrage = TorannMagicDefOf.TM_EnrageHD;
            var wolves = new List<TMPawnSummoned>();
            using var smoke = pack ? new Patch(typeof(FleckMaker).GetMethod(nameof(FleckMaker.ThrowSmoke), [typeof(UnityEngine.Vector3), typeof(Map), typeof(float)])!, nameof(CancelNative)) : null;
            using var wolfGenerator = pack ? new Patch(typeof(PawnGenerator).GetMethods().Single(m => m.Name == "GeneratePawn" && m.GetParameters().Length == 3), nameof(GenerateSummonedPawn)) : null;
            var oldVerb = def.MainVerb; var oldCost = def.manaCost; var oldConsume = def.consumeEnergy;
            var settings = TorannMagic.ModOptions.Settings.Instance;
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            var events = DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
            var oldEvent = TorannMagicDefOf.TM_UsedMagic;
            var oldWeakness = TorannMagicDefOf.TM_ArcaneWeakness;
            try
            {
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                    DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
                TorannMagicDefOf.TM_ArcaneWeakness = new HediffDef { defName = "SchoolArcaneWeakness", hediffClass = typeof(Hediff), maxSeverity = 100 };
                var weakness = new Hediff { def = TorannMagicDefOf.TM_ArcaneWeakness, pawn = soul };
                soul.health.hediffSet.hediffs.Add(weakness);
                TorannMagicDefOf.TM_UsedMagic = new HistoryEventDef { defName = "NativeSummonPayment" };
                DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedMagic);
                var history = (History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));
                history.historyEventsManager = new HistoryEventsManager(); Set(Current.Game, "history", history);
                using var notify = new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!, nameof(CancelNative));
                var bullet = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                bullet.category = ThingCategory.Projectile;
                bullet.thingClass = pack ? typeof(Projectile_AbilityBase) : typeof(CompHostedSummon).Assembly.GetType("TorannMagic.Projectile_Summon" + family)!;
                bullet.defName = "Projectile_Summon" + family; bullet.drawerType = DrawerType.RealtimeOnly;
                bullet.projectile = new ProjectileProperties { speed = 200, flyOverhead = true, explosionRadius = pack ? 2 : 0 };
                bullet.comps = [new CompProperties { compClass = typeof(CompHostedSummon) }];
                bullet.size = new IntVec2(1, 1); bullet.stackLimit = 1; bullet.destroyable = true;
                if (family == "Explosive" || family == "Pylon")
                {
                    var structure = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    structure.defName = family == "Pylon" ? "DefensePylon" : "TM_ManaMine";
                    structure.size = new IntVec2(1,1); structure.category = ThingCategory.Building; structure.building = new BuildingProperties(); structure.comps = [];
                    DefDatabase<ThingDef>.Add(structure);
                }
                if (pack)
                {
                    TorannMagicDefOf.TM_SpiritWolfR = soul.def; TorannMagicDefOf.TM_SpiritWolf = soul.kindDef;
                    TorannMagicDefOf.TM_EnrageHD = new HediffDef { defName = "TestSpiritWolfRage", hediffClass = typeof(Hediff), maxSeverity = 5 };
                    nextGeneratedSummon = _ =>
                    {
                        var wolf = PreparedSummon(soul, 79800 + wolves.Count); wolves.Add(wolf); return wolf;
                    };
                }
                def.MainVerb = new VerbProperties_Ability { verbClass = pack ? typeof(Verb_SummonSpiritWolves) : typeof(Verb_SB), defaultProjectile = bullet, warmupTime = 1,
                    burstShotCount = 1, SecondsToRecharge = 10, range = 25, isViolent = false,
                    targetParams = new TargetingParameters { canTargetLocations = true } };
                def.manaCost = .4f; def.consumeEnergy = true;
                soul.ageTracker.AgeBiologicalTicks = 25L * GenDate.TicksPerYear;
                magic.maxMP = magic.mpCost = magic.coolDown = 1f;
                if (magic.Mana == null) soul.needs.AllNeeds.Add(new Need_Mana(soul) { def = TorannMagicDefOf.TM_Mana });
                magic.Mana.CurLevel = .85f;
                string stem = pack ? "TM_SpiritWolves" : def.defName;
                var levels = pack ? magic.MagicData.MagicPowerSkill_SpiritWolves :
                    (List<MagicPowerSkill>)typeof(MagicData).GetProperty("MagicPowerSkill_Summon" + family)!.GetValue(magic.MagicData)!;
                levels.Single(s => s.label == stem + "_pwr").level = pack ? 2 : 1;
                levels.Single(s => s.label == stem + "_ver").level = 1;
                magic.arcaneDmg = 1f;
                var power = new MagicAbility(magic) { Def = def }; magic.AbilityData.Powers.Add(power); magic.AbilityData.AllPowers = null;
                var jobName = pack ? "TM_SpiritPack" + family.Substring("SpiritWolves".Length) : "TM_SpiritSummon" + family;
                var job = JobMaker.MakeJob(new JobDef { defName = jobName }, new IntVec3(15, 0, 15), soul);
                var driver = new JobDriver_SpiritBolt { pawn = body, job = job };
                Set(body.jobs, "curJob", job); Set(body.jobs, "curDriver", driver);
                var toils = ((IEnumerable<Toil>)typeof(JobDriver_SpiritBolt).GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null)!).ToArray();
                toils[0].initAction();
                Check.Equal(.85f, magic.Mana.CurLevel, "warmup has not paid");
                float cost = magic.ActualManaCost(def);
                toils[1].initAction();
                Check.Equal(.85f - cost, magic.Mana.CurLevel, "original paid once on launch");
                Check.Equal(true, cost > 0 && power.CooldownTicksLeft > 0, "native paid cooldown");
                if (pack)
                {
                    Check.Equal(6, wolves.Count, "five wolves plus one native versatility level");
                    Check.Equal(true, wolves.All(p => p.Spawner == soul && p.Map == c.Map && p.Temporary && p.TicksLeft > 0), "six native pawns belong to the original");
                    Check.Equal(true, wolves.All(p => p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnrageHD)?.Severity == .4f), "wolf rage takes original rank");
                    Check.Equal(0, magic.summonedMinions.Count, "wolves have independent expiry, not minion upkeep");
                }
                else
                {
                    var shot = c.Map.listerThings.AllThings.OfType<Projectile_AbilityBase>().Single(t => t.def == bullet);
                    Check.Same(body, shot.Caster, "physical host launched the native shot");
                    var source = shot.GetComp<CompHostedSummon>();
                    Check.Same(soul, typeof(CompHostedSummon).GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source), "flight retains original payer");
                    ReplaySummon(family, r, c, shot, job.targetA.Cell);
                }
                RuntimeLog.AssertClean();
            }
            finally
            {
                TorannMagicDefOf.TM_SpiritWolfR = oldWolfRace; TorannMagicDefOf.TM_SpiritWolf = oldWolfKind; TorannMagicDefOf.TM_EnrageHD = oldEnrage; nextGeneratedSummon = null;
                def.MainVerb = oldVerb; def.manaCost = oldCost; def.consumeEnergy = oldConsume; TorannMagic.ModOptions.Settings.Instance = settings;
                TorannMagicDefOf.TM_UsedMagic = oldEvent; TorannMagicDefOf.TM_ArcaneWeakness = oldWeakness;
                DefDatabase<HediffDef>.Clear(); foreach (var h in hediffs) DefDatabase<HediffDef>.Add(h);
                DefDatabase<HistoryEventDef>.Clear(); foreach (var e in events) DefDatabase<HistoryEventDef>.Add(e);
                DefDatabase<ThingDef>.Clear(); foreach (var item in thingDefs) DefDatabase<ThingDef>.Add(item);
            }
        }));
        yield return new("SpiritProjectedSummon.DismissMinion.OwnershipAndPayment", r => SpiritRebirthRitualTransactionTests.Run(r, c =>
        {
            using var custody = new Fixture(r, c);
            var soul = c.Original; var host = c.Creator;
            var owner = soul.GetCompAbilityUserMagic(); var hostMagic = host.GetCompAbilityUserMagic();
            var def = TorannMagicDefOf.TM_DismissMinion;
            var oldType = def.abilityClass; var oldVerb = def.MainVerb; var oldConsume = def.consumeEnergy;
            var settings = TorannMagic.ModOptions.Settings.Instance;
            var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
            var events = DefDatabase<HistoryEventDef>.AllDefsListForReading.ToArray();
            var oldEvent = TorannMagicDefOf.TM_UsedMagic;
            try
            {
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                    DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
                TorannMagicDefOf.TM_UsedMagic = new HistoryEventDef { defName = "DismissMinionUse" };
                DefDatabase<HistoryEventDef>.Add(TorannMagicDefOf.TM_UsedMagic);
                var history = (History)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(History));
                history.historyEventsManager = new HistoryEventsManager(); Set(Current.Game, "history", history);
                using var notify = new Patch(typeof(IdeoUtility).GetMethod(nameof(IdeoUtility.Notify_HistoryEvent))!, nameof(CancelNative));
                def.abilityClass = typeof(MagicAbility);
                def.consumeEnergy = true;
                def.MainVerb = new VerbProperties_Ability { verbClass = typeof(Verb_DismissMinion), AbilityTargetCategory = AbilityTargetCategory.TargetSelf,
                    burstShotCount = 1, SecondsToRecharge = 4, targetParams = new TargetingParameters { canTargetSelf = true } };
                owner.maxMP = owner.mpCost = owner.coolDown = 1f;
                if (owner.Mana == null) soul.needs.AllNeeds.Add(new Need_Mana(soul) { def = TorannMagicDefOf.TM_Mana });
                owner.Mana.CurLevel = 0;
                var sync = typeof(CompAbilityUserMagic).GetMethod("SyncHostedMinionDismissal", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var foreign = PreparedSummon(soul, 80100); foreign.Spawner = host; foreign.Temporary = true; foreign.TicksLeft = 200;
                var owned = PreparedSummon(soul, 80101); owned.Spawner = soul; owned.Temporary = true; owned.TicksLeft = 190;
                owner.summonedMinions.Add(foreign);
                sync.Invoke(owner, null);
                Check.Equal(false, owner.AbilityData.Powers.Any(p => p.Def == def), "foreign stale list cannot grant Dismiss");
                owner.summonedMinions.Add(owned);
                var stone = new Thing_SoulStone { def = ThingDef.Named("TM_SoulStone") };
                Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryCapture", soul, host, false), "stone holds the summoner before re-entry");
                Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryImplant", soul, custody.Possession), "native holder returns the summoner");
                Check.Equal(true, owner.AbilityData.Powers.Any(p => p.Def == def), "dismiss available immediately on re-entry without an economy tick");
                var dismiss = owner.AbilityData.Powers.Single(p => p.Def == def);
                var find = Production("SpiritHostedBolt").GetMethod("Find", BindingFlags.NonPublic | BindingFlags.Static)!;
                Check.Same(dismiss, find.Invoke(null, [host, soul, def]), "host command resolves the original source immediately after return");
                Check.Same(owner, dismiss.AbilityUser, "original holds the actual dynamic ability");
                Check.Equal(false, hostMagic.AbilityData.Powers.Any(p => p.Def == def), "no host copy");
                dismiss.CooldownTicksLeft = 20;
                var duplicate = new MagicAbility(owner) { Def = def, CooldownTicksLeft = 83 };
                owner.AbilityData.Powers.Add(duplicate); owner.AbilityData.Powers.Add(duplicate);
                sync.Invoke(owner, null); sync.Invoke(owner, null);
                Check.Same(duplicate, owner.AbilityData.Powers.Single(p => p.Def == def), "duplicate save keeps highest cooldown and one actual instance");
                Check.Equal(83, duplicate.CooldownTicksLeft, "sync never resets cooldown");
                duplicate.CooldownTicksLeft = -1;
                var job = JobMaker.MakeJob(new JobDef { defName = "TM_SpiritDismissMinion" }, host, soul);
                var driver = new JobDriver_SpiritBolt { pawn = host, job = job };
                Set(host.jobs, "curJob", job); Set(host.jobs, "curDriver", driver);
                var toils = ((IEnumerable<Toil>)typeof(JobDriver_SpiritBolt).GetMethod("MakeNewToils", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(driver, null)!).ToArray();
                toils[0].initAction();
                Check.Equal(190, owned.TicksLeft, "warmup leaves the minion intact");
                toils[1].initAction();
                Check.Equal(1, owned.TicksLeft, "native dismissal schedules expiry of the exact original's minion");
                Check.Equal(200, foreign.TicksLeft, "host minion stays intact");
                Check.Equal(0f, owner.Mana.CurLevel, "dismissal works at zero mana");
                Check.Equal(true, duplicate.CooldownTicksLeft > 0, "native cooldown belongs to the original");
                sync.Invoke(owner, null);
                Check.Equal(false, owner.AbilityData.Powers.Any(p => p.Def == def), "dynamic ability disappears without eligible minions");
                RuntimeLog.AssertClean();
            }
            finally
            {
                def.abilityClass = oldType; def.MainVerb = oldVerb; def.consumeEnergy = oldConsume; TorannMagic.ModOptions.Settings.Instance = settings;
                TorannMagicDefOf.TM_UsedMagic = oldEvent;
                DefDatabase<HediffDef>.Clear(); foreach (var h in hediffs) DefDatabase<HediffDef>.Add(h);
                DefDatabase<HistoryEventDef>.Clear(); foreach (var e in events) DefDatabase<HistoryEventDef>.Add(e);
            }
        }));
        yield return new("SpiritProjectedSummon.NativeJobsAndDefinitions", r =>
        {
            var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Summoner.xml")).Root!;
            var jobs = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/JobDefs/TM_SpiritSummoner.xml")).Root!;
            foreach (var family in new[] { "Minion","Elemental","Pylon","Explosive","Poppi" })
            {
                var spell = xml.Elements().Single(x => (string?)x.Element("defName") == "TM_Summon" + family);
                var shot = xml.Elements().Single(x => (string?)x.Element("defName") == spell.Element("MainVerb")!.Element("defaultProjectile")!.Value);
                Check.Equal(typeof(CompHostedSummon).FullName, shot.Element("comps")!.Element("li")!.Element("compClass")!.Value, "native projectile carries a persisted owner");
            }
            Check.Equal(10, jobs.Elements().Count(), "five projectile jobs, dismissal and four wolf ranks");
            foreach (string lang in new[] { "English","Russian","Ukrainian","Spanish","Portuguese","PortugueseBrazilian","ChineseSimplified" })
            {
                var local = XDocument.Load(Path.Combine(r.Repository, "v1.7/Languages", lang, "DefInjected/JobDef/TM_SpiritSummoner.xml")).Root!;
                foreach (var job in jobs.Elements())
                {
                    Check.Equal(typeof(JobDriver_SpiritBolt).FullName, job.Element("driverClass")!.Value, "native warmup job");
                    Check.NotNull(local.Element(job.Element("defName")!.Value + ".reportString"), lang + " report");
                }
            }
        });
        foreach (bool success in new[] { true, false })
            yield return new("SpiritProjectedSummon.SavedClaim." + (success ? "Spawned" : "Failed"), r => SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                using var custody = new Fixture(r, c);
                var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
                var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
                float oldPrice = TorannMagicDefOf.TM_SummonMinion.manaCost;
                var oldSettings = TorannMagic.ModOptions.Settings.Instance;
                try
                {
                    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                    TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                    if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                        DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
                    var magic = c.Original.GetCompAbilityUserMagic();
                    c.Original.ageTracker.AgeBiologicalTicks = 25L * GenDate.TicksPerYear;
                    magic.maxMP = 1f; magic.mpCost = 1f; TorannMagicDefOf.TM_SummonMinion.manaCost = .2f;
                    var mana = magic.Mana;
                    if (mana == null)
                    {
                        mana = new Need_Mana(c.Original) { def = TorannMagicDefOf.TM_Mana };
                        c.Original.needs.AllNeeds.Add(mana);
                    }
                    var ability = new MagicAbility(magic) { Def = TorannMagicDefOf.TM_SummonMinion };
                    var levels = magic.MagicData.MagicPowerSkill_SummonMinion;
                    levels.Single(s => s.label == "TM_SummonMinion_pwr").level = 1;
                    levels.Single(s => s.label == "TM_SummonMinion_ver").level = 2;
                    magic.arcaneDmg = 1.25f;
                    var data = new SummonCompSave();
                    var type = typeof(CompHostedSummon);
                    MethodInfo Method(string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
                    Method("Bind").Invoke(data.Shot, [ability]);
                    var price = magic.ActualManaCost(ability.magicDef);
                    Check.Equal(true, price > 0 && price < .8f, "actual positive payment");
                    levels.Single(s => s.label == "TM_SummonMinion_pwr").level = 3;
                    data.Poppi.explosionRadius = 3.17f;
                    Set(data.Poppi, "nextLeap", 87);
                    data = RoundTripSummon(r, "summon-" + success, data, c.Original);
                    object?[] args = [null];
                    Check.Equal(true, (bool)Method("Take").Invoke(data.Shot, args)!, "loaded projected shot admitted once");
                    Check.Same(c.Original, args[0], "native source is held original");
                    Check.Equal(1, (int)type.GetProperty("Power", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(data.Shot)!, "power from launch");
                    Check.Equal(2, (int)type.GetProperty("Versatility", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(data.Shot)!, "versatility from launch");
                    Check.Equal(1.25f, (float)type.GetProperty("Scale", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(data.Shot)!, "scale from launch");
                    Check.Equal(3.17f, data.Poppi.explosionRadius, "summoned Poppi blast size survives load");
                    if (success) Method("NotifySpawned").Invoke(data.Shot, [c.Creator]);
                    data = RoundTripSummon(r, "summon-" + success, data, c.Original);
                    Check.Equal(false, (bool)Method("Take").Invoke(data.Shot, args)!, "consumed receipt cannot spawn again after reload");
                    mana.CurLevel = .1f;
                    float expected = Math.Min(mana.MaxLevel, .1f + (success ? 0 : price));
                    data.Shot.PostDestroy(DestroyMode.Vanish, c.Map);
                    data.Shot.PostDestroy(DestroyMode.Vanish, c.Map);
                    Check.Equal(expected, mana.CurLevel, "exactly one native failed placement refund, no refund for a spawn");
                    if (success)
                    {
                        var elemental = new MagicAbility(magic) { Def = TorannMagicDefOf.TM_SummonElemental };
                        var pending = new SummonCompSave();
                        Method("Bind").Invoke(pending.Shot, [elemental]);
                        var variety = type.GetProperty("ElementRoll", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        int roll = (int)variety.GetValue(pending.Shot)!;
                        pending = RoundTripSummon(r, "elemental-kind", pending, c.Original);
                        Check.Equal(roll, (int)variety.GetValue(pending.Shot)!, "paid elemental type stays fixed over reload");
                    }
                }
                finally
                {
                    TorannMagicDefOf.TM_SummonMinion.manaCost = oldPrice;
                    TorannMagic.ModOptions.Settings.Instance = oldSettings;
                    System.Globalization.CultureInfo.CurrentCulture = oldCulture;
                    DefDatabase<HediffDef>.Clear(); foreach (var def in hediffs) DefDatabase<HediffDef>.Add(def);
                }
            }));
    }
}
