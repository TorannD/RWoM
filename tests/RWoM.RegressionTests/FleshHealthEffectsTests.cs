using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.FleshDevelopment;
using Verse;
using Verse.AI;
using UnityEngine;

namespace RWoM.RegressionTests;

internal static class FleshHealthEffectsTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshHealthEffects.HealthDefinitionsPassEngineConfigValidation", f =>
        {
            LoadHealthDefs(f);
            var names = new[] { FleshHealth.LevelDef }.Concat(FleshHealth.Passives.Concat(FleshHealth.Actives).Select(FleshHealth.DefName));
            foreach (var name in names)
            {
                var def = DefDatabase<HediffDef>.GetNamed(name);
                Check.Equal("", string.Join("\n", def.ConfigErrors()), name + " engine configuration errors");
            }
        });
        yield return new("FleshHealthEffects.HealthProjectionExists", _ =>
        {
            var type = typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.Hediff_FleshDevelopment");
            Check.NotNull(type, "A health-panel projection exposes progression and passive ranks");
        });
        yield return new("FleshHealthEffects.ActivationUsesEffects", _ =>
        {
            var type = typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.FleshEffects");
            Check.NotNull(type, "Flesh skills have a runtime visual-effect service");
        });
        yield return new("FleshHealthEffects.RealHealthEntriesReconcileAndDoNotStackStats", f => InGame(() =>
        {
            LoadHealthDefs(f);
            var pawn = Pawn(); var p = FleshRepository.Ensure(pawn);
            using var patches = new Patches("health");
            healthPawn = pawn;
            patches.Prefix(typeof(HediffSet).GetMethod("DirtyCache")!, nameof(HealthSideEffects));
            patches.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(HealthSideEffects));
            try
            {
                FleshHealth.Sync(pawn);
                Check.Equal(1, Entries(pawn).Length, "fresh undead shows level, no unlearned passives");
                Check.Equal(1, Entries(pawn)[0].DisplayLevel, "starts at level one");
                p.SkillPoints = 20; p.Thirst = 0;
                foreach (var skill in FleshHealth.Passives) { FleshActions.Upgrade(pawn, skill); FleshActions.Upgrade(pawn, skill); }
                Check.Equal(5, Entries(pawn).Length, "actual upgrade action adds all four learned passives");
                foreach (var h in Entries(pawn).Where(h => h.Skill.HasValue))
                {
                    Check.Equal(2, h.DisplayLevel, "current passive rank");
                    Check.Equal(true, h.Visible, "health row visible");
                    Check.Equal(true, h.def.stages == null || h.def.stages.Count == 0, "no bonus-granting stages");
                }
                Check.Equal(1.04f, FleshStatPatches.Adjust(p, "MoveSpeed", 1, 0), "health indicators do not change mobility arithmetic");
                Check.Equal(6, FleshHealth.Bonus(FleshSkill.Strength, 2), "exact damage bonus");
                Check.Equal(4, FleshHealth.Bonus(FleshSkill.Industry, 2), "exact work bonus");
                Check.Equal(20, FleshHealth.Bonus(FleshSkill.Capacity, 2), "exact capacity bonus");
                var level = Entries(pawn).Single(h => !h.Skill.HasValue);
                bool god = DebugSettings.godMode;
                try { DebugSettings.godMode = true; FleshActions.DebugLevelUp(pawn); }
                finally { DebugSettings.godMode = god; }
                Check.Equal(2, level.DisplayLevel, "God-mode command immediately reflected");
                p.AddExperience(p.RequiredExperience);
                Check.Equal(3, level.DisplayLevel, "normal XP level-up immediately reflected");
                FleshHealth.Sync(pawn); FleshHealth.Sync(pawn);
                Check.Equal(5, Entries(pawn).Length, "repeated reconciliation idempotent");
                var duplicate = new Hediff_FleshDevelopment { pawn = pawn, def = level.def };
                pawn.health.hediffSet.hediffs.Add(duplicate);
                FleshHealth.Sync(pawn);
                Check.Equal(5, Entries(pawn).Length, "duplicate saved indicators reconciled");
                pawn.health.hediffSet.hediffs.RemoveAll(h => h.def.defName == "TM_UndeadHD");
                Check.Equal(true, Entries(pawn).All(h => h.ShouldRemove && !h.Visible), "ordinary pawn retains no visible undead buffs");
            }
            finally { healthPawn = null; }
        }));
        yield return new("FleshHealthEffects.ExistingProgressGetsHealthRowsOnTick", f => InGame(() =>
        {
            LoadHealthDefs(f);
            var pawn = Pawn(); var p = FleshRepository.Ensure(pawn); p.Level = 7; p.SkillPoints = 5;
            p.TryUpgrade(FleshSkill.Strength);
            using var patches = new Patches("migration"); healthPawn = pawn;
            patches.Prefix(typeof(HediffSet).GetMethod("DirtyCache")!, nameof(HealthSideEffects));
            patches.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(HealthSideEffects));
            try
            {
                Set(Find.TickManager, "ticksGameInt", 250);
                FleshActions.Tick(pawn);
                Check.Equal(2, Entries(pawn).Length, "saved progress lacking indicators reconciles on simulation tick");
                Check.Equal(7, Entries(pawn).Single(h => !h.Skill.HasValue).DisplayLevel, "saved level preserved");
                Check.Equal(4, p.SkillPoints, "reconciliation charges no point");
            }
            finally { healthPawn = null; }
        }));
        yield return new("FleshHealthEffects.AllSelfActionsEmitOnlyOnSuccessAndAurasTick", _ => InGame(() =>
        {
            // Record the production rendering boundary, not a replacement gameplay/effect dispatcher.
            using var patches = new Patches("effects");
            patches.Prefix(typeof(FleshEffects).GetMethod("CanDraw")!, nameof(AllowRender));
            patches.Prefix(typeof(FleshEffects).GetMethod("Emit", BindingFlags.Static | BindingFlags.NonPublic)!, nameof(RecordEmission));
            patches.Prefix(typeof(HediffSet).GetMethod("DirtyCache")!, nameof(HealthSideEffects));
            patches.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(HealthSideEffects));
            foreach (var skill in new[] { FleshSkill.Restore, FleshSkill.Empower, FleshSkill.Harden, FleshSkill.Feast })
            {
                var pawn = Pawn(); var p = FleshRepository.Ensure(pawn); p.Experience = 40; p.SkillPoints = 100;
                healthPawn = pawn;
                foreach (var s in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Feast)) while (p.TryUpgrade(s)) { }
                p.TryUpgrade(FleshSkill.Feast);
                emissions.Clear();
                Check.Equal(true, FleshActions.TryActivate(pawn, skill), "real active action succeeds");
                string expected = "TM_Flesh" + (skill == FleshSkill.Restore ? "Regen" : skill.ToString()) + "FX";
                Check.Equal(true, emissions.Contains(expected), skill + " reaches emission boundary");
                int count = emissions.Count;
                Check.Equal(false, FleshActions.TryActivate(pawn, skill), "repeat blocked");
                Check.Equal(count, emissions.Count, "rejected action emits nothing");
            }
            var actor = Pawn(); var state = FleshRepository.Ensure(actor);
            healthPawn = actor;
            state.EmpowerUntil = state.HardenUntil = state.FeastUntil = 1000; state.Thirst = 0;
            emissions.Clear(); Set(Find.TickManager, "ticksGameInt", 90);
            FleshActions.Tick(actor);
            Check.Equal("TM_FleshEmpowerFX,TM_FleshHardenFX,TM_FleshFeastFX", string.Join(",", emissions), "actual gameplay tick emits each active aura");
            FleshActions.Tick(actor);
            Check.Equal(3, emissions.Count, "duplicate tick emits no duplicate aura");
            state.EmpowerUntil = state.HardenUntil = state.FeastUntil = 0;
            Set(Find.TickManager, "ticksGameInt", 180); FleshActions.Tick(actor);
            Check.Equal(3, emissions.Count, "inactive skills do not pulse");
            healthPawn = null;
        }));
        yield return new("FleshHealthEffects.ActiveRowsFollowCompletionExpiryAndRage", f => InGame(() =>
        {
            using var language = new FleshStatTests.LanguageScope(f.Repository);
            LoadHealthDefs(f); var pawn = Pawn(); var p = FleshRepository.Ensure(pawn); p.Thirst = 1; p.SkillPoints = 100;
            foreach (var skill in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Feast)) while (p.TryUpgrade(skill)) { }
            p.TryUpgrade(FleshSkill.Feast);
            using var patches = new Patches("active-health"); healthPawn = pawn;
            patches.Prefix(typeof(HediffSet).GetMethod("DirtyCache")!, nameof(HealthSideEffects));
            patches.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(HealthSideEffects));
            try
            {
                FleshHealth.Sync(pawn);
                Check.Equal(0, Entries(pawn).Count(h => h.Skill.HasValue && FleshHealth.IsActiveSkill(h.Skill.Value)), "learned but inactive buffs have no row");
                foreach (var skill in FleshHealth.Actives)
                {
                    Check.Equal(true, FleshActions.TryActivate(pawn, skill), "completed cast activates effect");
                    var entry = Entries(pawn).Single(h => h.Skill == skill);
                    Check.Equal(p.GetRank(skill), entry.DisplayLevel, "active rank shown");
                    Check.Equal(true, entry.Visible && !entry.ShouldRemove, "active buff visible immediately");
                    Check.Equal(false, string.IsNullOrWhiteSpace(entry.TipStringExtra), "bonus description present");
                    Check.Equal(true, entry.def.stages == null || entry.def.stages.Count == 0, "display cannot double grant stats");
                }
                p.EmpowerUntil = p.HardenUntil = p.FeastUntil = 0; p.FeastRage = true;
                FleshHealth.Sync(pawn);
                Check.Equal(1, Entries(pawn).Count(h => h.Skill.HasValue && FleshHealth.IsActiveSkill(h.Skill.Value)), "expired buffs removed, owned rage still visible");
                Check.Equal(true, Entries(pawn).Single(h => h.Skill == FleshSkill.Feast).Visible, "rage survives timer end");
                p.FeastRage = false; FleshHealth.Sync(pawn);
                Check.Equal(0, Entries(pawn).Count(h => h.Skill.HasValue && FleshHealth.IsActiveSkill(h.Skill.Value)), "stopped rage removed");
                p.EmpowerUntil = 1000; FleshHealth.Sync(pawn); FleshHealth.Sync(pawn);
                Check.Equal(1, Entries(pawn).Count(h => h.Skill == FleshSkill.Empower), "loaded active timer reconciles without duplicates");
            }
            finally { healthPawn = null; }
        }));
        yield return new("FleshHealthEffects.ConsumptionImpactAndSiphonSurviveTargetDestruction", _ => InGame(() =>
        {
            using var patches = new Patches("consumed");
            patches.Prefix(typeof(FleshEffects).GetMethod("CanDraw")!, nameof(AllowRender));
            patches.Prefix(typeof(FleshEffects).GetMethod("Emit", BindingFlags.Static | BindingFlags.NonPublic)!, nameof(RecordEmission));
            emissions.Clear();
            // Consumption takes a captured location: the already destroyed target is never dereferenced.
            FleshEffects.Consumed(Pawn(), null!, new Vector3(4, 0, 4));
            Check.Equal("TM_FleshBloodFX,TM_FleshSiphonFX,TM_FleshRegenFX", string.Join(",", emissions), "blood, moving siphon and receiver pulse");
            foreach (var owner in new[] { typeof(FleshActions).GetMethod("CompleteDevour")!, typeof(FleshCombatPatches).GetMethod("Bite")! })
                Check.Equal(true, Calls(owner, typeof(FleshEffects).GetMethod("Consumed")!), owner.Name + " compiled success path calls consumption effects");
        }));
        yield return new("FleshHealthEffects.OffMapSafeNoGameplayRandomAndBoundedPulse", _ =>
        {
            var pawn = Pawn(); var p = FleshRepository.Ensure(pawn); p.EmpowerUntil = p.HardenUntil = p.FeastUntil = 10000;
            Check.Equal(false, FleshEffects.CanDraw(pawn), "world pawn cannot draw");
            Check.Equal(false, FleshEffects.CanDraw(null!), "null pawn cannot draw");
            Rand.PushState(746);
            float expected;
            try { expected = Rand.Value; } finally { Rand.PopState(); }
            Rand.PushState(746);
            try
            {
                foreach (var skill in Enum.GetValues<FleshSkill>()) FleshEffects.Activated(pawn, skill);
                FleshEffects.Tick(pawn, p, 0, 10000); FleshEffects.Consumed(pawn, null!, Vector3.zero);
                Check.Equal(expected, Rand.Value, "off-map effects do not advance gameplay randomness");
            }
            finally { Rand.PopState(); }
            int pulses = Enumerable.Range(1, 600).Count(t => FleshEffects.PulseDue(t - 1, t, 42));
            Check.Equal(true, pulses <= 7, "bounded to one pulse per 90 ticks");
            Check.Equal(false, FleshEffects.PulseDue(100, 100, 42), "same tick no pulse");
            Check.Equal(false, FleshEffects.PulseDue(100, 50, 42), "backwards clock no pulse");
        });
        yield return new("FleshHealthEffects.EffectDefinitionsUseExistingRuntimeTextures", f =>
        {
            var doc = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/FleshDevelopment/FleshEffects.xml"));
            var defs = doc.Root!.Elements("FleckDef").Where(n => n.Element("defName") != null).ToArray();
            Check.Equal(6, defs.Length, "all six visual identities defined");
            foreach (var d in defs)
                Check.Equal(true, File.Exists(Path.Combine(f.Repository, "Textures", d.Element("graphicData")!.Element("texPath")!.Value + ".png")), d.Element("defName")!.Value + " runtime asset exists");
            var loaded = LoadEffectDefs(f);
            Check.Equal(6, loaded.Count, "all shipped effect defs load through Verse XML inheritance");
            foreach (var def in loaded)
            {
                Check.Equal(typeof(FleckSystemThrown), def.fleckSystemClass, "installed core FleckBase_Thrown inherited");
                Check.Equal(typeof(Graphic_Fleck), def.graphicData.graphicClass, "installed core fleck graphic inherited");
                Check.NotNull(def.graphicData.shaderType, "shader cross-reference resolves");
                Check.Equal(true, def.fadeInTime + def.solidTime + def.fadeOutTime > .5f, "nonzero visible lifetime");
            }
        });
        yield return new("FleshHealthEffects.RendererSubmissionRestoresRandomAndKeepsDefsImmutable", f => InGame(() =>
        {
            var def = LoadEffectDefs(f).Single(d => d.defName == "TM_FleshEmpowerFX");
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo { Size = new IntVec3(20, 1, 20) };
            map.flecks = (FleckManager)RuntimeHelpers.GetUninitializedObject(typeof(FleckManager));
            using var patches = new Patches("renderer-boundary");
            patches.Prefix(typeof(FleckManager).GetMethod("CreateFleck")!, nameof(RecordFleck));
            var method = typeof(FleshEffects).GetMethod("Submit", BindingFlags.Static | BindingFlags.NonPublic)!;
            Check.Equal(true, Calls(typeof(FleshEffects).GetMethod("Emit", BindingFlags.Static | BindingFlags.NonPublic)!, method), "real emitter uses isolated renderer submission");
            float fade = def.fadeOutTime; var color = def.graphicData.color;
            Rand.PushState(751); float expected;
            try { expected = Rand.Value; } finally { Rand.PopState(); }
            Rand.PushState(751);
            try
            {
                fleckCount = 0;
                method.Invoke(null, [map, FleckMaker.GetDataStatic(new Vector3(4, 0, 4), map, def, 1.8f)]);
                Check.Equal(1, fleckCount, "real emitter reaches renderer with loaded def");
                Check.Equal(expected, Rand.Value, "renderer-side random draw isolated from gameplay");
                Check.Equal(1.8f, emittedFleck.scale, "instance scale forwarded");
                Check.Same(def, emittedFleck.def, "shipped effect def forwarded");
                Check.Equal(fade, def.fadeOutTime, "definition lifetime unchanged");
                Check.Equal(color, def.graphicData.color, "shared color unchanged");
            }
            finally { Rand.PopState(); }
        }));
    }

    static Hediff_FleshDevelopment[] Entries(Pawn pawn) => pawn.health.hediffSet.hediffs.OfType<Hediff_FleshDevelopment>().ToArray();
    static Pawn? healthPawn;
    static bool HealthSideEffects(Pawn ___pawn) => ___pawn != healthPawn;
    static readonly List<string> emissions = [];
    static bool AllowRender(ref bool __result) { __result = true; return false; }
    static bool RecordEmission(string name) { emissions.Add(name); return false; }
    static int fleckCount;
    static FleckCreationData emittedFleck;
    static bool RecordFleck(FleckCreationData __0) { fleckCount++; emittedFleck = __0; _ = Rand.Value; return false; }
    static Pawn Pawn()
    {
        var race = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); race.race = new RaceProperties();
        var pawn = new Pawn { def = race, kindDef = new PawnKindDef { race = race } }; pawn.health = new Pawn_HealthTracker(pawn);
        pawn.mindState = (Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));
        pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
        Set(pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
        pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = "TM_UndeadHD" } });
        return pawn;
    }
    static void InGame(Action body)
    {
        var old = Current.Game;
        Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Set(Current.Game, "tickManager", new TickManager());
        Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
        try { body(); } finally { Current.Game = old; }
    }
    static void Set(object target, string name, object value)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new Exception("Missing fixture field: " + name);
    }
    static void LoadHealthDefs(RuntimeFixture f)
    {
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        var doc = new XmlDocument(); doc.Load(Path.Combine(f.Repository, "v1.7/Defs/FleshDevelopment/FleshHealth.xml"));
        foreach (XmlNode node in doc.DocumentElement!.ChildNodes) if (node.NodeType == XmlNodeType.Element) XmlInheritance.TryRegister(node, null);
        XmlInheritance.Resolve();
        foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element || node.Attributes?["Abstract"]?.Value == "True") continue;
            var def = DirectXmlToObject.ObjectFromXml<HediffDef>(node, true);
            if (DefDatabase<HediffDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<HediffDef>.Add(def);
        }
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
    }
    static bool Calls(MethodInfo caller, MethodInfo target)
    {
        var harmony = Assembly.Load("0Harmony");
        var processor = harmony.GetType("HarmonyLib.PatchProcessor")!;
        var method = processor.GetMethods().Single(m => m.Name == "GetOriginalInstructions" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        var instructions = (System.Collections.IEnumerable)method.Invoke(null, [caller, null])!;
        foreach (var instruction in instructions)
            if (Equals(instruction.GetType().GetField("operand")!.GetValue(instruction), target)) return true;
        return false;
    }
    static List<FleckDef> LoadEffectDefs(RuntimeFixture f)
    {
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        var core = new XmlDocument();
        core.Load(Path.Combine(Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!, "Data/Core/Defs/Effects/Fleck_Visual.xml"));
        foreach (XmlNode node in core.DocumentElement!.ChildNodes)
            if (node.Attributes?["Name"]?.Value is "FleckBase" or "FleckBase_Thrown") XmlInheritance.TryRegister(node, null);
        var own = new XmlDocument(); own.Load(Path.Combine(f.Repository, "v1.7/Defs/FleshDevelopment/FleshEffects.xml"));
        foreach (XmlNode node in own.DocumentElement!.ChildNodes) if (node.NodeType == XmlNodeType.Element) XmlInheritance.TryRegister(node, null);
        XmlInheritance.Resolve();
        foreach (string name in new[] { "Mote", "MoteGlow" })
            if (DefDatabase<ShaderTypeDef>.GetNamedSilentFail(name) == null) DefDatabase<ShaderTypeDef>.Add(new ShaderTypeDef { defName = name });
        var result = new List<FleckDef>();
        foreach (XmlNode node in own.DocumentElement!.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element || node.Attributes?["Abstract"]?.Value == "True") continue;
            var def = DirectXmlToObject.ObjectFromXml<FleckDef>(node, true);
            var existing = DefDatabase<FleckDef>.GetNamedSilentFail(def.defName);
            if (existing == null) DefDatabase<FleckDef>.Add(def);
            result.Add(existing ?? def);
        }
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        return result;
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly object instance; readonly Type type; readonly string id;
        public Patches(string suffix) { id = "rwom.regression.flesh-health-effects." + suffix; type = assembly.GetType("HarmonyLib.Harmony")!; instance = Activator.CreateInstance(type, id)!; }
        public void Prefix(MethodBase original, string name)
        {
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(FleshHealthEffectsTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            type.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5).Invoke(instance, [original, prefix, null, null, null]);
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(instance, [id]);
    }
}
