using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BrightmageLifecycleTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BrightmageLifecycle.NativeMissingResourceOnlyAndProfileGate", Resource);
        yield return new("BrightmageLifecycle.NativeLightSaveChargeAndBurstPayment", ResourceSavePayment);
        yield return new("BrightmageLifecycle.SavedSpiritControlsPreserveEnergyActionAndInheritedOwner", Spirit);
        yield return new("BrightmageLifecycle.NativeEqualizeAndSpiritEnergyCost", Equalize);
        yield return new("BrightmageLifecycle.NativeSpiritScribePreservesOwnerEnergyAndActions", SpiritSave);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
        readonly HediffDef oldResource = TorannMagicDefOf.TM_LightCapacitanceHD;
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly PawnCapacityDef[] capacities;
        readonly List<(TMAbilityDef def, Type type, bool init)> abilities = new();
        readonly object harmony;
        readonly string patchId = "rwom.bright.lifecycle." + Guid.NewGuid();
        public readonly HediffDef ResourceDef;
        public Fixture(RuntimeFixture f)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            LoadBaseDefinitions(f);
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
            capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
            DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "BrightResourceCapacity" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var assembly = Assembly.Load("0Harmony");
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, patchId)!;
            Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(SkipBoundary));
            Patch(typeof(Pawn).GetProperty("DrawPos")!.GetMethod!, nameof(DrawPosition));
            Patch(typeof(FlyingObject_SpiritOfLight).GetProperty("DrawPos")!.GetMethod!, nameof(DrawPosition));
            foreach (var m in typeof(FleckMaker).GetMethods().Where(m => m.Name == "ThrowLightningGlow")) Patch(m, nameof(SkipBoundary));
            ResourceDef = new HediffDef { defName = "TM_LightCapacitanceHD", hediffClass = typeof(HediffWithComps), maxSeverity = 250f,
                comps = [new HediffCompProperties { compClass = typeof(HediffComp_LightCapacitance) }] };
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs.Where(d => d.defName != ResourceDef.defName)) DefDatabase<HediffDef>.Add(d);
            DefDatabase<HediffDef>.Add(ResourceDef); TorannMagicDefOf.TM_LightCapacitanceHD = ResourceDef;
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "BrightLifecycle", profileId = "rwom.brightmage", domain = DisciplineDomain.Magic });
        }
        void Patch(MethodInfo method, string name)
        {
            var assembly = Assembly.Load("0Harmony");
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(BrightmageLifecycleTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { method, hook, null, null, null });
        }
        public (Pawn pawn, CompAbilityUserMagic magic) Caster(string id = "rwom.brightmage", bool modern = true)
        {
            var p = PawnWithComps(true, false);
            Set(p.pawn.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
            p.pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "BrightResourceBody" } } };
            if (modern)
            {
                var progression = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(progression);
                progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation(id, DisciplineDomain.Magic);
            }
            return (p.pawn, p.magic);
        }
        public void Dispose()
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { patchId });
            Current.Game = previous; TorannMagicDefOf.TM_LightCapacitanceHD = oldResource;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.init; }
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var d in capacities) DefDatabase<PawnCapacityDef>.Add(d);
        }
    }
    static bool SkipBoundary() => false;
    static bool DrawPosition(ref UnityEngine.Vector3 __result) { __result = UnityEngine.Vector3.zero; return false; }
    static void EnsureSpiritPresentationInitialized()
    {
        var type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        string id = "rwom.brightlife.material." + Guid.NewGuid(); var harmony = Activator.CreateInstance(type, id)!;
        var mods = LoadedModManager.RunningModsListForReading.ToArray();
        try
        {
            var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(BrightmageLifecycleTests).GetMethod(nameof(SkipBoundary), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            foreach (var method in typeof(MaterialPool).GetMethods().Where(m => m.Name == nameof(MaterialPool.MatFrom)))
                type.GetMethod("Patch")!.Invoke(harmony, [method, hook, null, null, null]);
            // ShaderDatabase may initialize while evaluating MatFrom arguments.
            // Type-resolution-only test packs have no live asset bundle folder.
            LoadedModManager.RunningModsListForReading.Clear();
            RuntimeHelpers.RunClassConstructor(typeof(FlyingObject_SpiritOfLight).TypeHandle);
        }
        finally
        {
            LoadedModManager.RunningModsListForReading.AddRange(mods);
            type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
        }
    }
    static HediffComp_LightCapacitance Energy(Pawn pawn) => pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD).TryGetComp<HediffComp_LightCapacitance>();
    static FlyingObject_SpiritOfLight SpiritObject()
    {
        EnsureSpiritPresentationInitialized();
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "BrightLifecycleSpirit"; def.thingClass = typeof(FlyingObject_SpiritOfLight); def.category = ThingCategory.Projectile; def.projectile = new ProjectileProperties(); def.comps = new();
        var spirit = new FlyingObject_SpiritOfLight { def = def, solAction = SoLAction.Guarding, queuedAction = SoLAction.Glow };
        spirit.LightEnergy = 47f; return spirit;
    }
    static int Controls(CompAbilityUserMagic comp) => comp.AbilityData.Powers.Count(p => p.Def == TorannMagicDefOf.TM_SoL_Equalize);
    static void Resource(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); var p = f.Caster();
        p.magic.ResolveClassSkills();
        Check.Equal(true, p.pawn.health.hediffSet.HasHediff(f.ResourceDef), "native class maintenance creates missing resource before explicit repair");
        Check.Equal(true, BrightmageLifecycle.Restore(p.magic), "modern maintenance handled");
        var hd = p.pawn.health.hediffSet.GetFirstHediffOfDef(f.ResourceDef); var energy = Energy(p.pawn);
        Check.Equal(true, hd is HediffWithComps && energy != null, "native HealthUtility creates real native resource comp");
        Check.Equal(10f, energy.LightEnergy, "native resource starts with ten light");
        Check.Equal(1f, hd.Severity, "native custom class default severity retained");
        energy.LightEnergy = 68.25f;
        p.magic.ResolveClassSkills(); BrightmageLifecycle.Restore(p.magic);
        Check.Same(hd, p.pawn.health.hediffSet.GetFirstHediffOfDef(f.ResourceDef), "missing-only repair retains exact resource");
        Check.Equal(68.25f, energy.LightEnergy, "maintenance neither regenerates nor pays energy");
        Check.Equal(true, p.magic.SoL == null, "class ownership never summons spirit");
        Check.Equal(0, Controls(p.magic), "class ownership never grants empty Equalize control");
        var other = f.Caster("rwom.fire"); BrightmageLifecycle.Restore(other.magic);
        Check.Equal(false, other.pawn.health.hediffSet.HasHediff(f.ResourceDef), "other discipline gets no light resource");
        DefDatabase<ClassDisciplineDef>.Clear(); var missing = f.Caster(); BrightmageLifecycle.Restore(missing.magic);
        Check.Equal(false, missing.pawn.health.hediffSet.HasHediff(f.ResourceDef), "missing profile cannot create resource");
        BrightmageLifecycle.Restore(p.magic); Check.Same(hd, p.pawn.health.hediffSet.GetFirstHediffOfDef(f.ResourceDef), "missing profile does not erase saved resource");
        var legacy = f.Caster(modern: false);
        Check.Equal(false, BrightmageLifecycle.Restore(legacy.magic), "classic maintenance untouched");
    }
    public sealed class EnergyEnvelope : IExposable
    {
        public static HediffComp_LightCapacitance Target = null!;
        public void ExposeData() => Target.CompExposeData();
    }
    static void ResourceSavePayment(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); var p = f.Caster(); BrightmageLifecycle.Restore(p.magic);
        var energy = Energy(p.pawn); energy.LightEnergy = 61.25f;
        float adjustment = 0;
        energy.CompPostTick(ref adjustment);
        float charged = energy.LightEnergy;
        Check.Equal(61.25f + .02f, charged, "scheduled native charge");
        energy.CompPostTick(ref adjustment);
        Check.Equal(charged, energy.LightEnergy, "same tick cannot charge twice");
        string path = Path.Combine(runtime.WorkingDirectory, "bright-native-energy.xml");
        EnergyEnvelope.Target = energy; var envelope = new EnergyEnvelope();
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "native"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        energy.LightEnergy = 0;
        Set(energy, "lastChargeTick", -1);
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "native"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); EnergyEnvelope.Target = null!; }
        BrightmageLifecycle.Restore(p.magic);
        energy.CompPostTick(ref adjustment);
        Check.Equal(charged, energy.LightEnergy, "native Scribe restores energy and last charged tick without load refill");
        typeof(HediffComp_LightCapacitance).GetMethod("UpdateCharge", Fields)!.Invoke(energy, null);
        Check.Equal(charged + .02f, energy.LightEnergy, "native detached charge formula applies once");
        var ability = new AbilityUser.PawnAbility(p.magic) { Def = TorannMagicDefOf.TM_LightBurst };
        var verb = new Verb_LightBurst { Ability = ability };
        float before = energy.LightEnergy;
        typeof(Verb_LightBurst).GetMethod("Initialize", Fields)!.Invoke(verb, new object[] { p.pawn });
        Check.Equal(before - 5f, energy.LightEnergy, "native two-burst initialization pays 2.5 per burst once");
        BrightmageLifecycle.Restore(p.magic); BrightmageLifecycle.Restore(p.magic);
        Check.Equal(before - 5f, energy.LightEnergy, "resource repair adds no second native payment");
    }
    static void Spirit(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); var p = f.Caster("rwom.fire"); var spirit = SpiritObject();
        p.magic.SoL = spirit; p.magic.spell_EqualizeLight = true;
        DefDatabase<ClassDisciplineDef>.Clear();
        BrightmageLifecycle.Restore(p.magic); BrightmageLifecycle.Restore(p.magic);
        Check.Same(spirit, p.magic.SoL, "inherited despawned spirit retained without profile");
        Check.Equal(47f, spirit.LightEnergy, "existing spirit energy unchanged");
        Check.Equal(SoLAction.Guarding, spirit.solAction, "current action untouched");
        Check.Equal(SoLAction.Glow, spirit.queuedAction, "queued action untouched");
        Check.Equal(1, Controls(p.magic), "stale saved true flag repaired exactly once");
        var control = p.magic.AbilityData.Powers.Single(a => a.Def == TorannMagicDefOf.TM_SoL_Equalize); control.CooldownTicksLeft = 123;
        BrightmageLifecycle.Restore(p.magic);
        Check.Same(control, p.magic.AbilityData.Powers.Single(a => a.Def == control.Def), "existing control identity retained");
        Check.Equal(123, control.CooldownTicksLeft, "control cooldown retained");
        Set(spirit, "mapIndexOrState", (sbyte)-2);
        Check.Equal(true, spirit.Destroyed, "fixture has a real destroyed Thing state");
        BrightmageLifecycle.Restore(p.magic);
        Check.Equal(true, p.magic.SoL == null, "destroyed reference released for native upkeep");
        Check.Equal(0, Controls(p.magic), "dead spirit control removed");
        Check.Equal(false, p.magic.spell_EqualizeLight, "dead spirit flag cleared");
    }
    static void Equalize(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); var p = f.Caster(); BrightmageLifecycle.Restore(p.magic);
        var energy = Energy(p.pawn); energy.LightEnergy = 20f;
        var spirit = SpiritObject(); spirit.LightEnergy = 60f; p.magic.SoL = spirit;
        BrightmageLifecycle.Restore(p.magic);
        var verb = new Verb_SoL_Equalize(); Set(verb, "caster", p.pawn);
        Check.Equal(false, (bool)typeof(Verb_SoL_Equalize).GetMethod("TryCastShot", Fields)!.Invoke(verb, null)!, "native false return is not effect failure");
        Check.Equal(40f, energy.LightEnergy, "native equalize sets caster average");
        Check.Equal(40f, spirit.LightEnergy, "native equalize sets same spirit average");
        spirit.effVal = 10;
        Check.Equal(9f, spirit.ActualLightCost(10f), "native spirit efficiency cost");
        Check.Equal(31f, spirit.LightEnergy, "native spirit payment occurs once");
        BrightmageLifecycle.Restore(p.magic);
        Check.Equal(31f, spirit.LightEnergy, "maintenance does not repay or refill spirit");
        Check.Equal(40f, energy.LightEnergy, "maintenance does not equalize again");
        spirit.verVal = 3;
        energy.LightEnergy = 100f; spirit.LightEnergy = 112f;
        typeof(Verb_SoL_Equalize).GetMethod("TryCastShot", Fields)!.Invoke(verb,null);
        Check.Equal(100f,energy.LightEnergy,"personal reservoir remains capped");
        Check.Equal(112f,spirit.LightEnergy,"surplus stays in the larger reservoir");
    }
    public sealed class SpiritEnvelope : IExposable
    {
        public FlyingObject_SpiritOfLight Spirit = null!;
        public void ExposeData() => Scribe_Deep.Look(ref Spirit, "spirit");
    }
    static void SpiritSave(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); var p = f.Caster(); p.pawn.thingIDNumber = 75101;
        var spirit = SpiritObject(); spirit.thingIDNumber = 75102;
        var priorDefs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        DefDatabase<ThingDef>.Add(spirit.def);
        Set(spirit, "pawn", p.pawn); Set(spirit, "age", 240);
        spirit.shouldDismiss = true;
        spirit.shouldGlow = true;
        spirit.spinning = true;
        spirit.delayCount = 17;
        var curve = new List<UnityEngine.Vector3>{new(3,0,8),new(4,0,8),new(5,0,9)};
        Set(spirit,"curvePoints",curve);
        Set(spirit,"destinationCurvePoint",1);
        Set(spirit,"speed",18f);
        spirit.glowCenter = new IntVec3(12, 0, 14);
        string path = Path.Combine(runtime.WorkingDirectory, "bright-native-spirit.xml");
        try
        {
            var envelope = new SpiritEnvelope { Spirit = spirit };
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "native"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            envelope = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "native");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", Fields)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(p.pawn); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            var loaded = envelope.Spirit;
            RuntimeLog.AssertClean();
            Check.Same(p.pawn, typeof(FlyingObject_SpiritOfLight).GetField("pawn", Fields)!.GetValue(loaded)!, "native Spirit Scribe resolves actual owner reference");
            Check.Equal(47f, loaded.LightEnergy, "native spirit energy saved");
            Check.Equal(SoLAction.Guarding, loaded.solAction, "native active action saved");
            Check.Equal(SoLAction.Glow, loaded.queuedAction, "native queued action saved");
            Check.Equal(true,loaded.shouldDismiss && loaded.shouldGlow && loaded.spinning,"pending dismissal, lighting and spin survive");
            Check.Equal(17,loaded.delayCount,"action delay survives");
            Check.Equal(18f,(float)typeof(FlyingObject_SpiritOfLight).GetField("speed",Fields)!.GetValue(loaded)!,"current flight speed survives");
            Check.Equal(1,(int)typeof(FlyingObject_SpiritOfLight).GetField("destinationCurvePoint",Fields)!.GetValue(loaded)!,"current curve segment survives");
            Check.Equal(true,((List<UnityEngine.Vector3>)typeof(FlyingObject_SpiritOfLight).GetField("curvePoints",Fields)!.GetValue(loaded)!).SequenceEqual(curve),"exact trajectory survives");
            Check.Equal(240, (int)typeof(FlyingObject_SpiritOfLight).GetField("age", Fields)!.GetValue(loaded)!, "native age retained");
            p.magic.SoL = loaded; p.magic.spell_EqualizeLight = true;
            BrightmageLifecycle.Restore(p.magic); BrightmageLifecycle.Restore(p.magic);
            Check.Same(loaded, p.magic.SoL, "maintenance retains actual loaded spirit");
            Check.Equal(47f, loaded.LightEnergy, "loaded spirit not refilled or spent by maintenance");
            Check.Equal(SoLAction.Guarding, loaded.solAction, "loaded action not reset by maintenance");
            Check.Equal(1, Controls(p.magic), "loaded spirit repairs stale true control once");
            var old = System.Xml.Linq.XDocument.Load(path);
            foreach (var field in old.Descendants().Where(x=>x.Name.LocalName.StartsWith("TM_sol",StringComparison.Ordinal)).ToArray())
                field.Remove();
            var oldFile=Path.Combine(runtime.WorkingDirectory,"bright-native-spirit-old.xml");
            old.Save(oldFile);
            envelope=null!;
            try
            {
                Scribe.loader.InitLoading(oldFile); Scribe_Deep.Look(ref envelope,"native");
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",Fields)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(p.pawn); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            Check.Equal(false,envelope.Spirit.shouldDismiss,"old save does not invent a dismissal");
            Check.Equal(0,((List<UnityEngine.Vector3>)typeof(FlyingObject_SpiritOfLight).GetField("curvePoints",Fields)!.GetValue(envelope.Spirit)!).Count,"old save has a valid empty trajectory");
            Check.Equal(47f,envelope.Spirit.LightEnergy,"old save keeps original energy");
        }
        finally
        {
            DefDatabase<ThingDef>.Clear(); foreach (var def in priorDefs) DefDatabase<ThingDef>.Add(def);
        }
    }
}
