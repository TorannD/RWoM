using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthEnergyTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritEnergy.NativeDamageWithoutInstigatorCapturesOriginal", r => Run(r, (c, need) =>
        {
            var damage = new DamageInfo(DamageDefOf.Blunt, 40f);
            using var patch = new Patch(typeof(Pawn).GetMethod("PreApplyDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!,
                typeof(TorannMagicMod.Pawn_PreApplyDamage).GetMethod("Prefix")!);
            var result = c.Creator.TakeDamage(damage);
            Check.Equal(0f, result.totalDamageDealt, "native damage pipeline absorbed physical damage without instigator");
            AssertStone(c);
            need.GainNeed(-100f); need.NeedInterval();
            AssertStone(c);
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritEnergy.NativeNeedTickCapturesOriginal", r => Run(r, (c, need) =>
        {
            need.CurLevel = .001f; need.NeedInterval(); AssertStone(c); RuntimeLog.AssertClean();
        }));
        yield return new("SpiritEnergy.NativePendingShellScribeRetriesSameStone", r => Run(r, (c, need) =>
        {
            using (new Patch(typeof(Thing_SoulStone).GetMethod("TryCapture", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(RefuseCapture)))
                need.GainNeed(-10f);
            var state = ClassIdentity.Component(c.Creator).SpiritRebirth;
            Check.Equal(true, (bool)typeof(SpiritRebirthState).GetProperty("DepletionPending")!.GetValue(state)!, "zero latches saved pending state");
            Check.Same(c.Map.spawnedThings, c.Creator.holdingOwner, "failed capture leaves actual original on native map");
            var shell = c.Map.spawnedThings.OfType<Thing_SoulStone>().Single();
            Check.Equal(true, shell.ContainedSpirit == null, "pending shell is native map-owned and empty");
            var path = Path.Combine(r.WorkingDirectory, "spirit-energy-pending.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref shell, "shell"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            SpiritRebirthState loadedState = null!; Thing_SoulStone loadedShell = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loadedState, "state"); Scribe_Deep.Look(ref loadedShell, "shell");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(c.Creator); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            string id = shell.GetUniqueLoadID(); var pos = shell.Position;
            shell.DeSpawn(); shell.Destroy();
            GenSpawn.Spawn(loadedShell, pos, c.Map);
            Set(ClassIdentity.Component(c.Creator), "spiritRebirth", loadedState);
            Check.Equal(id, loadedShell.GetUniqueLoadID(), "native shell identity survives save");
            AdvanceRetry();
            ClassIdentity.Component(c.Creator).CompTick();
            AssertStone(c);
            Check.Same(loadedShell, c.Map.spawnedThings.OfType<Thing_SoulStone>().Single(), "retry uses saved same shell without second allocation");
            RuntimeLog.AssertClean();
        }));
        yield return new("SpiritEnergy.NativePlacementRefusalKeepsPawnAndRetries", r => Run(r, (c, need) =>
        {
            using (new Patch(typeof(GenPlace).GetMethods().Single(m => m.Name == "TryPlaceThing" && m.GetParameters().Any(p => p.IsOut)), nameof(RefuseCapture)))
                need.GainNeed(-10f);
            Check.Same(c.Map.spawnedThings, c.Creator.holdingOwner, "refused placement leaves original in native map custody");
            Check.Equal(0, c.Map.spawnedThings.OfType<Thing_SoulStone>().Count(), "failed empty shell is not orphaned on map");
            Check.Equal(0f, need.CurLevel, "failed placement keeps exact exhausted energy");
            AdvanceRetry();
            ClassIdentity.Component(c.Creator).CompTick(); AssertStone(c); RuntimeLog.AssertClean();
        }));
        foreach (bool destroy in new[] { false, true })
            yield return new("SpiritEnergy.NativeStaleEmptyShellRetries." + (destroy ? "Destroyed" : "Moved"), r => Run(r, (c, need) =>
            {
                using (new Patch(typeof(Thing_SoulStone).GetMethod("TryCapture", BindingFlags.Instance | BindingFlags.NonPublic)!, nameof(RefuseCapture))) need.GainNeed(-10f);
                var shell = c.Map.spawnedThings.OfType<Thing_SoulStone>().Single();
                shell.DeSpawn();
                if (destroy) shell.Destroy();
                else Check.Equal(true, c.Creator.inventory.innerContainer.TryAdd(shell, false), "native inventory legitimately moves empty shell");
                AdvanceRetry(); ClassIdentity.Component(c.Creator).CompTick(); AssertStone(c);
                Check.Equal(destroy, shell.Destroyed, "moved item is never destroyed as cleanup");
                if (!destroy) Check.Same(c.Creator.inventory.innerContainer, shell.holdingOwner, "moved empty shell retains its owner");
                RuntimeLog.AssertClean();
            }));
        yield return new("SpiritEnergy.NativeAbilityPaymentDefersCollapseAndChargesOnce", r => Run(r, (c, need) =>
        {
            var magic = c.Creator.GetComp<CompAbilityUserMagic>();
            var def = r.AddAbility("SpiritEnergyPaymentFixture"); def.requiredNeed = TorannMagicDefOf.TM_SpiritND; def.needCost = 10f;
            var ability = new MagicAbility(magic) { Def = def };
            need.CurLevel = 99f; need.GainNeed(50f);
            Check.Equal(100f, need.CurLevel, "positive gain clamps to actual capacity");
            Check.Equal(.01f, need.lastGainPct, "gain UI reflects actual clamped delta");
            Check.Equal(1f, need.baseSpiritGain + need.modifiedSpiritGain, "UI gain components sum to actual received energy");
            need.GainNeed(float.NaN); Check.Equal(100f, need.CurLevel, "nonfinite input cannot corrupt energy");
            need.GainNeed(-90f);
            Check.Equal(true, magic.Mana == null, "cost preflight fixture has no Mana owner");
            DefDatabase<TM_CustomClassDef>.Clear();
            DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [new TM_CustomClass {
                classTrait = TorannMagicDefOf.TM_Possessor, isMage = true, classMageAbilities = [def] }] });
            Check.Equal(true, SpiritRebirthCasting.Allows(c.Creator, def), "actual owned family passes form admission before resource preflight");
            def.needCost = 11f;
            Check.Equal(false, ability.CanCastPowerCheck(AbilityUser.AbilityContext.Player, out _), "insufficient Spirit rejected independently of absent Mana");
            def.needCost = 10f;
            // Existing native settlement contract: an unsuccessful attempt has no
            // payment receipt; settlement runs actual PostAbilityAttempt once.
            Set(c.Creator, "factionInt", null);
            TorannMagic.Odyssey.ManualTravelSafety.BeginAttempt(ability);
            ability.PostAbilityAttempt(); Check.Equal(10f, need.CurLevel, "failed unsettled attempt charges nothing");
            TorannMagic.Odyssey.ManualTravelSafety.Settle(ability);
            Check.Equal(0f, need.CurLevel, "successful native settlement debits exact Spirit cost");
            Check.Same(c.Map.spawnedThings, c.Creator.holdingOwner, "payment returns while original still spawned for caller bookkeeping");
            ability.PostAbilityAttempt(); Check.Equal(0f, need.CurLevel, "duplicate payment cannot charge again");
            ClassIdentity.Component(c.Creator).CompTick(); AssertStone(c); RuntimeLog.AssertClean();
        }));
        yield return new("SpiritEnergy.NativeEmptyDestroyExceptionReleasesRetryGuard", r => Run(r, (c, need) =>
        {
            using (new Patch(typeof(GenPlace).GetMethods().Single(m => m.Name == "TryPlaceThing" && m.GetParameters().Any(p => p.IsOut)), nameof(RefuseCapture)))
            using (new Patch(typeof(Thing_SoulStone).GetMethod(nameof(Thing_SoulStone.Destroy))!, nameof(ThrowEmptyDestroy)))
            {
                bool threw = false;
                try { need.GainNeed(-10f); }
                catch (InvalidOperationException e) { threw = e.Message == "injected empty shell destruction failure"; }
                Check.Equal(true, threw, "native empty-shell cleanup exception reaches caller");
            }
            Check.Same(c.Map.spawnedThings, c.Creator.holdingOwner, "cleanup exception never loses actual original");
            AdvanceRetry(); ClassIdentity.Component(c.Creator).CompTick();
            var state = ClassIdentity.Component(c.Creator).SpiritRebirth;
            Check.Equal(true, typeof(SpiritRebirthState).GetProperty("DepletionStone", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(state) == null,
                "retry guard released so next invocation can clean failed empty shell");
            AdvanceRetry(); ClassIdentity.Component(c.Creator).CompTick(); AssertStone(c); RuntimeLog.AssertClean();
        }));
    }
    static void AdvanceRetry() => Set(Find.TickManager, "ticksGameInt", Find.TickManager.TicksGame + 250);
    static bool RefuseCapture(ref bool __result) { __result = false; return false; }
    static bool ThrowEmptyDestroy(Thing_SoulStone __instance)
    {
        if (__instance.ContainedSpirit == null && !__instance.Spawned) throw new InvalidOperationException("injected empty shell destruction failure");
        return true;
    }
    sealed class Patch : IDisposable
    {
        readonly object harmony; readonly string id = "spirit-energy-" + Guid.NewGuid();
        public Patch(MethodBase target, string prefix) : this(target, typeof(SpiritRebirthEnergyTests).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)!) { }
        public Patch(MethodBase target, MethodInfo prefix)
        {
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, prefix)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [target, method, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void AssertStone(SpiritRebirthRitualTransactionTests.Context c)
    {
        var stones = c.Map.spawnedThings.OfType<Thing_SoulStone>().ToList();
        Check.Equal(1, stones.Count, "exactly one native map-owned soulstone");
        var stone = stones[0];
        Check.Same(c.Map.spawnedThings, stone.holdingOwner, "native map owns shell");
        Check.Same(c.Creator, stone.ContainedSpirit, "actual original retained without clone");
        Check.Same(stone.GetDirectlyHeldThings(), c.Creator.holdingOwner, "native stone owns original");
        Check.Equal(false, c.Creator.Destroyed || c.Creator.Dead, "original survives exhaustion");
        var state = ClassIdentity.Component(c.Creator).SpiritRebirth;
        Check.Equal(true, state.HasValidStone(c.Creator), "saved state agrees with real holder");
        Check.Equal(true, state.RecoveryRequired, "forced collapse starts full recovery debt");
        Check.Equal(0, state.RecoveryTicks, "full seven days remain");
    }
    internal static void Run(RuntimeFixture r, Action<SpiritRebirthRitualTransactionTests.Context, Need_Spirit> test)
    {
        using var language = new FleshStatTests.LanguageScope(r.Repository);
        LanguageDatabase.activeLanguage.keyedReplacements["TM_SpiritRebirthCastUnavailable"] = new LoadedLanguage.KeyedReplacement {
            key = "TM_SpiritRebirthCastUnavailable", value = "Unavailable in current Spirit form" };
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var classes = DefDatabase<TM_CustomClassDef>.AllDefsListForReading.ToArray();
        var priorNeed = TorannMagicDefOf.TM_SpiritND;
        var oldSettings = TorannMagic.ModOptions.Settings.Instance;
        var thread = typeof(UnityData).GetField("mainThreadId", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldThread = thread.GetValue(null);
        try
        {
            thread.SetValue(null, Environment.CurrentManagedThreadId);
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = "TM_SoulStone"; def.thingClass = typeof(Thing_SoulStone); def.category = ThingCategory.Item;
            def.stackLimit = 1; def.comps = []; def.size = new IntVec2(1, 1); def.destroyable = true;
            def.passability = Traversability.Standable; def.drawerType = DrawerType.RealtimeOnly;
            DefDatabase<ThingDef>.Add(def);
            TorannMagicDefOf.TM_SpiritND = new NeedDef { defName = "SpiritEnergyFixture", needClass = typeof(Need_Spirit) };
            SpiritRebirthRitualTransactionTests.Run(r, c =>
            {
                var pawn = c.Creator; var map = c.Map;
                Set(pawn.jobs, "curJob", null!);
                map.pawnDestinationReservationManager = new PawnDestinationReservationManager(); map.enrouteManager = new EnrouteManager(map);
                map.attackTargetReservationManager = new AttackTargetReservationManager(map);
                map.mapPawns = new MapPawns(map); map.autoSlaughterManager = new AutoSlaughterManager(map);
                pawn.def.comps ??= []; pawn.def.category = ThingCategory.Pawn; pawn.def.thingClass = typeof(Pawn);
                pawn.def.size = new IntVec2(1, 1); pawn.def.passability = Traversability.Standable; pawn.def.drawerType = DrawerType.RealtimeOnly;
                map.thingGrid.Register(pawn); map.listerThings.Add(pawn); map.attackTargetsCache.Notify_ThingSpawned(pawn);
                Check.Equal(true, SpiritRebirthStateTests.Invoke(ClassIdentity.Component(pawn), "BeginSpiritRebirth"), "explicit marked original");
                pawn.needs = new Pawn_NeedsTracker(pawn);
                var need = new Need_Spirit(pawn) { def = TorannMagicDefOf.TM_SpiritND, CurLevel = 10f };
                pawn.needs.AllNeeds.Add(need);
                test(c, need);
            });
        }
        finally
        {
            TorannMagicDefOf.TM_SpiritND = priorNeed; thread.SetValue(null, oldThread);
            TorannMagic.ModOptions.Settings.Instance = oldSettings;
            DefDatabase<ThingDef>.Clear(); foreach (var def in defs) DefDatabase<ThingDef>.Add(def);
            DefDatabase<TM_CustomClassDef>.Clear(); foreach (var def in classes) DefDatabase<TM_CustomClassDef>.Add(def);
        }
    }
}
