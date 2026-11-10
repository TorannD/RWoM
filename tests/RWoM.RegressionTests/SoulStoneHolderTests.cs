using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using RimWorld;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SoulStoneHolderTests
{
    public sealed class UnexpectedStoneDamageWorker : DamageWorker
    {
        public static int Calls;
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Calls++;
            throw new InvalidOperationException("Occupied soulstone allowed damage worker execution: " + dinfo.Def.defName);
        }
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SoulStoneHolder.PlayerImplantOrdersAndSevenLanguages", f =>
        {
            var jobs=XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/JobDefs/TM_SoulStoneImplant.xml")).Root!;
            Check.Equal(true,jobs.Elements().Any(e=>e.Element("driverClass")?.Value == "TorannMagic.ClassProgression.JobDriver_SoulStoneImplant"),"native job driver");
            foreach(var lang in new[]{"English","Russian","Ukrainian","Spanish","Portuguese","PortugueseBrazilian","ChineseSimplified"})
            {
                var root=Path.Combine(f.Repository,"v1.7/Languages",lang);
                var keys=XDocument.Load(Path.Combine(root,"Keyed/TM_SoulStoneImplant.xml")).Root!;
                foreach(var key in new[]{"TM_SoulStoneChooseHost","TM_SoulStoneCannotImplant","TM_SoulStoneHarmful","TM_SoulStoneRecoveryRemaining","TM_SoulStoneNoExitCell"})
                    Check.Equal(true,!string.IsNullOrWhiteSpace(keys.Element(key)?.Value),lang+" "+key);
                var warning=keys.Element("TM_SoulStoneHarmful")!.Value;
                Check.Equal(true,warning.Contains("{0}") && warning.Contains("{1}"),lang+" names both soul and host before confirming");
                var reports=XDocument.Load(Path.Combine(root,"DefInjected/JobDef/TM_SoulStoneImplant.xml")).Root!;
                foreach(var job in jobs.Elements().Where(e=>e.Element("defName")!=null))
                    Check.NotNull(reports.Element(job.Element("defName")!.Value+".reportString"),"localized player order "+lang);
            }
        });
        yield return new("SoulStoneHolder.ExactNativeCaptureImplantAndRecovery", CaptureImplant);
        yield return new("SoulStoneHolder.RejectForeignUnmarkedOccupiedAndRetry", Rejections);
        yield return new("SoulStoneHolder.OccupiedDestroyStackAndSplitSafety", Safety);
        yield return new("SoulStoneHolder.NativeStonePawnDeepScribe", r => Save(r));
        yield return new("SoulStoneHolder.FailedStateCommitRestoresExactOwner", Rollback);
    }

    static bool Invoke(object target, string method, params object[] args)
        => (bool)target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args)!;
    static Thing_SoulStone Stone()
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "RegressionSoulStone"; def.stackLimit = 1;
        def.thingClass = typeof(Thing_SoulStone); def.category = ThingCategory.Item;
        def.comps = new List<CompProperties>(); def.size = new IntVec2(1, 1);
        return new Thing_SoulStone { def = def, thingIDNumber = 67401 };
    }
    static HediffComp_SpiritPossession EmptyHost(SpiritRebirthStateTests.Fixture f, Pawn host)
    {
        var holder = f.Hold(host, f.Other);
        holder.GetDirectlyHeldThings().Remove(f.Other);
        return holder;
    }

    static void CaptureImplant(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r);
        var state = f.Begin(); var native = f.Hold(f.Host, f.Original);
        Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "native initial entry");
        var magic = f.Magic.MagicData; var might = f.Might.MightData; var identity = f.Original.GetUniqueLoadID();
        var stone = Stone();
        Check.Equal(true, Invoke(stone, "TryCapture", f.Original, f.Host, false), "capture transfers actual original");
        Check.Equal(0, native.GetDirectlyHeldThings().Count, "source empty");
        Check.Same(f.Original, stone.ContainedSpirit, "single same pawn");
        Check.Same(stone, f.Original.holdingOwner.Owner, "native owner points to stone");
        Check.Equal(true, state.HasValidStone(f.Original), "state agrees with actual custody");
        Check.Equal(false, Invoke(stone, "TryImplant", f.Other, native), "foreign expected soul denied");
        Check.Equal(true, Invoke(stone, "TryImplant", f.Original, native), "implant actual soul into native empty host");
        Check.Equal(true, stone.ContainedSpirit == null, "shell empty after commit");
        Check.Same(f.Original, native.SpiritPawn, "host owns actual pawn");
        Check.Equal(true, state.HasValidHost(f.Original), "native host authoritative");
        Check.Equal(false, state.CanVoluntarilyLeaveHost(f.Original), "week recovery gates voluntary exit");
        Check.Equal(true, Invoke(stone, "TryCapture", f.Original, f.Host, false), "forced depletion remains safe during recovery");
        Check.Same(stone.GetDirectlyHeldThings(), f.Original.holdingOwner, "forced depletion preserves native soul owner");
        Check.Equal(0, state.RecoveryTicks, "forced depletion does not bypass recovery");
        Check.Same(magic, f.Magic.MagicData, "magic data remains on original");
        Check.Same(might, f.Might.MightData, "might data remains on original");
        Check.Equal(identity, f.Original.GetUniqueLoadID(), "load identity unchanged");
    }

    static void Rejections(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); var stone = Stone();
        var native = f.Hold(f.Host, f.Original);
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Host, false), "unmarked legacy spirit rejected");
        Check.Same(native.GetDirectlyHeldThings(), f.Original.holdingOwner, "legacy custody preserved");
        var state = f.Begin();
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Host, false), "uncommitted native custody cannot act as ethereal");
        Check.Equal(true, Invoke(state, "TryEnterHost", f.Original, f.Host), "establish state");
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Other, false), "foreign host rejected");
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Host, true), "live host cannot claim death");
        Check.Equal(true, Invoke(stone, "TryCapture", f.Original, f.Host, false), "valid retry succeeds");
        Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Host, false), "duplicate capture rejected");
        Check.Equal(false, Invoke(stone, "TryCapture", f.Other, f.Host, false), "second soul rejected");
        var badTarget = EmptyHost(f, f.Other); badTarget.parent.pawn = f.Host;
        Check.Equal(false, Invoke(stone, "TryImplant", f.Original, badTarget), "foreign hediff parent rejected");
        Check.Same(stone.GetDirectlyHeldThings(), f.Original.holdingOwner, "failed implant preserves stone owner");
        Check.Equal(true, Invoke(stone, "TryImplant", f.Original, native), "valid implant retry succeeds");
    }

    static void Safety(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); var state = f.Begin();
        f.Hold(f.Host, f.Original); Invoke(state, "TryEnterHost", f.Original, f.Host);
        var stone = Stone(); Invoke(stone, "TryCapture", f.Original, f.Host, false);
        stone.def.useHitPoints = true;
        stone.HitPoints = 73;
        UnexpectedStoneDamageWorker.Calls = 0;
        foreach (string name in new[] { "Flame", "Bomb" })
        {
            // Keep the actual Thing.TakeDamage pipeline; the worker is a tripwire
            // proving occupied absorption exits before damage or ignition work.
            var damageDef = new DamageDef { defName = name, harmsHealth = true,
                workerClass = typeof(UnexpectedStoneDamageWorker),
                igniteChanceByTargetFlammability = new SimpleCurve { new CurvePoint(0f, 1f), new CurvePoint(1f, 1f) } };
            var result = stone.TakeDamage(new DamageInfo(damageDef, 1000000f));
            Check.Equal(0f, result.totalDamageDealt, name + " native damage pipeline reports no damage");
            Check.Equal(73, stone.HitPoints, name + " leaves occupied stone HP unchanged");
            Check.Equal(false, stone.Destroyed, name + " cannot destroy occupied stone");
            Check.Same(f.Original, stone.ContainedSpirit, name + " preserves actual contained original");
            var emptyProbe = Stone(); var damage = new DamageInfo(damageDef, 1000000f);
            emptyProbe.PreApplyDamage(ref damage, out bool absorbed);
            Check.Equal(false, absorbed, name + " empty shell retains ordinary damage admission");
        }
        Check.Equal(0, UnexpectedStoneDamageWorker.Calls, "absorbed damage never invokes worker");
        foreach (var mode in new[] { DestroyMode.Vanish, DestroyMode.KillFinalize, DestroyMode.Deconstruct }) stone.Destroy(mode);
        Check.Equal(false, stone.Destroyed, "occupied stone survives destruction attempts");
        Check.Same(f.Original, stone.ContainedSpirit, "soul retained after repeated destruction");
        var empty = Stone();
        Check.Equal(false, stone.CanStackWith(empty), "occupied cannot stack");
        Check.Equal(false, empty.CanStackWith(stone), "reverse stack also denied");
        Check.Same(stone, stone.SplitOff(1), "native singleton movement keeps exact stone");
        stone.stackCount = 2;
        Check.Equal(true, stone.SplitOff(1) == null, "malformed occupied stack cannot split soul");
        Check.Same(f.Original, stone.ContainedSpirit, "malformed split keeps soul");
        stone.stackCount = 1;
        var carrier = new ThingOwner<Thing>();
        Check.Equal(true, carrier.TryAdd(stone, false), "native off-map inventory holds actual stone");
        Check.Same(stone.GetDirectlyHeldThings(), f.Original.holdingOwner, "nested inventory keeps soul owner");
        Check.Equal(true, state.HasValidStone(f.Original), "nested stone custody valid");
    }

    internal static void Save(RuntimeFixture r, bool runtime = false)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r);
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        var needDefs = DefDatabase<NeedDef>.AllDefsListForReading.ToArray();
        var hairDefs = DefDatabase<HairDef>.AllDefsListForReading.ToArray();
        var priorMana = TorannMagicDefOf.TM_Mana; var priorSpirit = TorannMagicDefOf.TM_SpiritND;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        using var patches = new FleshStatTests.ProductionPatches();
        patches.Add(typeof(TorannMagicMod.Pawn_NeedsTracker_Patch));
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            Set(Current.Game, "tickManager", new TickManager());
            // Minimal native Pawn graph, as in CombatWarpod.FullHolderScribe;
            // serialize the actual Pawn and its actual progression comp, no envelope.
            var pawn = f.Original;
            pawn.def.thingClass = typeof(Pawn); pawn.def.category = ThingCategory.Pawn;
            pawn.def.race = new RaceProperties { body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef() } } };
            pawn.def.comps = new List<CompProperties> { new CompProperties { compClass = typeof(CompClassProgression) } };
            Set(pawn, "comps", new List<ThingComp> { f.Comp });
            if (runtime)
            {
                pawn.def.comps.Add(new CompProperties { compClass = typeof(CompAbilityUserMagic) });
                pawn.def.comps.Add(new CompProperties { compClass = typeof(CompAbilityUserMight) });
                pawn.AllComps.Add(f.Magic); pawn.AllComps.Add(f.Might);
            }
            if (!runtime) pawn.story = null;
            else
            {
                pawn.story.hairDef = new HairDef { defName = "SpiritRuntimeSavedHair" };
                DefDatabase<HairDef>.Add(pawn.story.hairDef);
            }
            pawn.Name = null;
            pawn.ageTracker = new Pawn_AgeTracker(pawn);
            Set(pawn.ageTracker, "ageBiologicalTicksInt", 25L * 3600000L);
            pawn.def.race.lifeStageAges = [new LifeStageAge { minAge = 13, def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }];
            Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
            DefDatabase<NeedDef>.Clear();
            TorannMagicDefOf.TM_Mana = new NeedDef { defName = "StoneScribeMana", needClass = typeof(Need_Mana), onlyIfCausedByHediff = true };
            TorannMagicDefOf.TM_SpiritND = new NeedDef { defName = "StoneScribeSpirit", needClass = typeof(Need_Spirit), onlyIfCausedByHediff = true };
            DefDatabase<NeedDef>.Add(TorannMagicDefOf.TM_Mana); DefDatabase<NeedDef>.Add(TorannMagicDefOf.TM_SpiritND);
            pawn.needs.AllNeeds.Clear();
            pawn.needs.AllNeeds.Add(new Need_Spirit(pawn) { def = TorannMagicDefOf.TM_SpiritND, CurLevel = 33f });
            pawn.needs.AllNeeds.Add(new Need_Mana(pawn) { def = TorannMagicDefOf.TM_Mana, CurLevel = .27f });
            f.Comp.BeginNewProgression(Vocation.WarriorMage);
            f.Comp.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
            f.Comp.DelayNextTomeQuest(1234);
            var state = f.Begin(); f.Hold(f.Host, pawn); Invoke(state, "TryEnterHost", pawn, f.Host);
            var stone = Stone(); Check.Equal(true, Invoke(stone, "TryCapture", pawn, f.Host, false), "save capture");
            if (runtime)
            {
                var ability = r.AddAbility("SpiritRuntimeSavedChain");
                f.Magic.chainedAbilitiesList.Add(new CompAbilityUserMagic.ChainedMagicAbility(ability, 42, true));
                f.Might.chainedAbilitiesList.Add(new CompAbilityUserMight.ChainedMightAbility(ability, 51, true));
                Set(typeof(CompAbilityUserMagic).GetField("spiritRuntimeClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Magic)!, "lastTick", 101);
                Set(typeof(CompAbilityUserMight).GetField("spiritRuntimeClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Might)!, "lastTick", 102);
                Set(pawn.needs.TryGetNeed<Need_Mana>(), "lastSpiritIntervalTick", 99);
            }
            if (capacities.Length == 0) DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "SoulStoneFixtureCapacity" });
            DefDatabase<ThingDef>.Add(pawn.def); DefDatabase<ThingDef>.Add(stone.def);
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager();
            Set(Current.Game, "worldInt", world);
            var path = Path.Combine(r.WorkingDirectory, "soul-stone-native-pawn.xml");
            var carrier = new Pawn();
            var inventory = new Pawn_InventoryTracker(carrier);
            Check.Equal(true, inventory.innerContainer.TryAdd(stone, false), "survivor inventory owns occupied stone before save");
            TMPawnSummoned? minion = null;
            if (runtime)
            {
                var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                race.defName = "SpiritSavedMinion"; race.thingClass = typeof(TMPawnSummoned); race.category = ThingCategory.Pawn;
                race.race = pawn.def.race; race.stackLimit = 1; race.destroyable = true; race.comps = []; race.size = new IntVec2(1, 1); race.drawerType = DrawerType.RealtimeOnly;
                DefDatabase<ThingDef>.Add(race);
                minion = new TMPawnSummoned { def = race, thingIDNumber = 67407, Spawner = pawn, validSummoning = true,
                    Temporary = true, TicksToDestroy = 1800, TicksLeft = 310 };
                minion.ageTracker = new Pawn_AgeTracker(minion);
                minion.health = new Pawn_HealthTracker(minion);
                f.Magic.summonedMinions.Add(minion);
            }
            try
            {
                Scribe.saver.InitSaving(path, "regression");
                Scribe_Deep.Look(ref inventory, "inventory", carrier);
                if (runtime) Scribe_Deep.Look(ref minion, "mapSummon");
                Scribe.saver.FinalizeSaving();
            }
            finally { Scribe.ForceStop(); }
            inventory = null!;
            minion = null;
            try
            {
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref inventory, "inventory", carrier);
                if (runtime) Scribe_Deep.Look(ref minion, "mapSummon");
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            var loaded = (Thing_SoulStone)inventory.innerContainer.Single();
            Check.Same(inventory.innerContainer, loaded.holdingOwner, "native inventory owns the restored stone");
            var soul = Check.NotNull(loaded.ContainedSpirit, "native graph restores contained Pawn");
            Check.Equal(false, ReferenceEquals(pawn, soul), "loaded graph rebuilt from deep Pawn data");
            Check.Equal(pawn.GetUniqueLoadID(), soul.GetUniqueLoadID(), "native pawn ID survives");
            Check.Equal(1, loaded.GetDirectlyHeldThings().Count, "exactly one contained pawn");
            Check.Same(loaded, soul.holdingOwner.Owner, "loaded native owner is loaded stone");
            Check.Equal(true, SpiritRebirthState.TryGet(soul, out var restored), "original reference resolves to loaded Pawn");
            Check.Same(loaded, restored.Stone, "state reference resolves to actual loaded stone");
            Check.Equal(true, restored.HasValidStone(soul), "loaded state and holder reciprocal");
            soul.needs.AddOrRemoveNeedsAsAppropriate();
            Check.Equal(33f, Check.NotNull(soul.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "loaded original Spirit need").CurLevel, "saved Spirit energy survives load and refresh");
            Check.Equal(.27f, Check.NotNull(soul.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "loaded original Mana need").CurLevel, "saved upkeep mana survives load and refresh");
            Check.Equal(true, restored.RecoveryRequired, "recovery debt survives native save");
            Check.Equal(0, restored.RecoveryTicks, "stone never advances recovery");
            if (runtime)
            {
                var magic = soul.GetComp<CompAbilityUserMagic>(); var might = soul.GetComp<CompAbilityUserMight>();
                Check.Same(minion!, magic.summonedMinions.Single(), "restored original pays for the exact saved summon");
                Check.Same(soul, minion!.Spawner, "summon still belongs to the soul in its stone");
                Check.Same(magic, minion.CompSummoner, "summon resolves the loaded original component");
                Check.Equal(310, minion.TicksLeft, "Scribe restores the elapsed summon lifetime");
                SpiritRebirthRitualTransactionTests.Run(r, context =>
                {
                    minion.def.race.intelligence = Intelligence.Humanlike;
                    Set(minion.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
                    using var setup = new SpiritRebirthReleaseTests.ReleaseFixture(r, context);
                    minion.needs = new Pawn_NeedsTracker(minion);
                    minion.stances = new Pawn_StanceTracker(minion);
                    minion.kindDef = new PawnKindDef { defName = "SpiritSavedMinionKind", race = minion.def };
                    minion.jobs = new Verse.AI.Pawn_JobTracker(minion);
                    minion.rotationTracker = new Pawn_RotationTracker(minion);
                    minion.mindState = new Verse.AI.Pawn_MindState(minion);
                    GenSpawn.Spawn(minion, new IntVec3(15, 0, 15), context.Map, Rot4.North, WipeMode.Vanish, true);
                    Check.Same(context.Map, minion.Map, "loaded summon spawned on the map");
                    Check.Equal(310, minion.TicksLeft, "loading the map must not renew the summon's lifetime");
                });
                Check.Equal(42, magic.chainedAbilitiesList.Single().expirationTicks, "native original Magic chain timer survives");
                Check.Equal(51, might.chainedAbilitiesList.Single().expirationTicks, "native original Might chain timer survives");
                foreach (var owner in new object[] { magic, might })
                {
                    var clock = owner.GetType().GetField("spiritRuntimeClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
                    Check.Equal(owner is CompAbilityUserMagic ? 101 : 102, (int)clock.GetType().GetField("lastTick", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(clock)!, "native independent comp clock receipt survives");
                }
                Check.Equal(99, (int)typeof(Need_Mana).GetField("lastSpiritIntervalTick", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(soul.needs.TryGetNeed<Need_Mana>())!, "native held need cadence survives");
            }
            var comp = soul.GetComp<CompClassProgression>();
            Check.Equal(true, comp.State.Find("rwom.necromancer") != null, "class discipline survives on original comp");
            Check.Equal(f.Comp.TomeQuestAvailableAtTick, comp.TomeQuestAvailableAtTick, "progression investment remains owned by pawn");
            loaded.Destroy(); Check.Equal(false, loaded.Destroyed, "restored occupied shell remains protected");
            RuntimeLog.AssertClean();
        }
        finally
        {
            DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var def in capacities) DefDatabase<PawnCapacityDef>.Add(def);
            TorannMagicDefOf.TM_Mana = priorMana; TorannMagicDefOf.TM_SpiritND = priorSpirit;
            DefDatabase<NeedDef>.Clear(); foreach (var def in needDefs) DefDatabase<NeedDef>.Add(def);
            DefDatabase<HairDef>.Clear(); foreach (var def in hairDefs) DefDatabase<HairDef>.Add(def);
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    static bool RejectCommit(ref bool __result) { __result = false; return false; }
    sealed class RejectedCommit : IDisposable
    {
        readonly object harmony;
        readonly string id = "rwom.soulstone.commit." + Guid.NewGuid();
        public RejectedCommit(string method)
        {
            var assembly = Assembly.Load("0Harmony");
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var target = typeof(SpiritRebirthState).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var patch = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,
                typeof(SoulStoneHolderTests).GetMethod(nameof(RejectCommit), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, patch, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }

    static void Rollback(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r); var state = f.Begin();
        var native = f.Hold(f.Host, f.Original); Invoke(state, "TryEnterHost", f.Original, f.Host);
        var stone = Stone(); var source = f.Original.holdingOwner;
        using (new RejectedCommit("TryEnterStone"))
            Check.Equal(false, Invoke(stone, "TryCapture", f.Original, f.Host, false), "injected commit refusal after native transfer");
        Check.Same(source, f.Original.holdingOwner, "failed capture restores exact prior native owner");
        Check.Same(f.Original, native.SpiritPawn, "original is restored without clone");
        Check.Equal(0, stone.GetDirectlyHeldThings().Count, "failed capture leaves empty stone");
        Check.Equal(true, state.HasValidHost(f.Original), "failed capture leaves source state valid");
        Check.Equal(true, Invoke(stone, "TryCapture", f.Original, f.Host, false), "capture retry succeeds");
        using (new RejectedCommit("TryEnterHost"))
            Check.Equal(false, Invoke(stone, "TryImplant", f.Original, native), "injected implant commit refusal");
        Check.Same(stone.GetDirectlyHeldThings(), f.Original.holdingOwner, "failed implant restores exact stone owner");
        Check.Equal(0, native.GetDirectlyHeldThings().Count, "failed implant empties destination");
        Check.Equal(true, state.HasValidStone(f.Original), "failed implant leaves valid stone state");
        Check.Equal(true, Invoke(stone, "TryImplant", f.Original, native), "implant retry succeeds");
    }
}
