using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthHealthFormTests
{
    static bool Reconcile(Pawn p) => (bool)typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthHealthForm", true)!
        .GetMethod("Reconcile", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [p])!;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRebirthHealthForm.DefinitionAndSevenLocales", r =>
        {
            var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/HediffDefs/Hediffs_SpiritRebirth.xml"));
            var effect = xml.Root!.Element("HediffDef")!;
            Check.Equal("TorannMagic.ClassProgression.Hediff_SpiritRebirthForm", (string)effect.Element("hediffClass")!, "separate owned type");
            Check.Equal("HediffCompProperties_Invisibility", (string)effect.Element("comps")!.Element("li")!.Attribute("Class")!, "native invisibility comp");
            Check.Equal(true, (bool)effect.Element("comps")!.Element("li")!.Element("visibleToPlayer")!, "visible to player; no transparency claim");
            Check.Equal("Ludeon.RimWorld.Royalty,Ludeon.RimWorld.Anomaly", (string)effect.Element("comps")!.Element("li")!.Attribute("MayRequireAnyOf")!, "native DLC component optional");
            foreach (var locale in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
            {
                var lang = XDocument.Load(Path.Combine(r.Repository, "Languages", locale, "DefInjected/HediffDef/TM_SpiritRebirthForm.xml"));
                foreach (var key in new[] { "label", "description" }) Check.Equal(false, string.IsNullOrWhiteSpace((string?)lang.Root!.Element("TM_SpiritRebirthForm." + key)), locale + " " + key);
            }
        });
        yield return new("SpiritRebirthHealthForm.SavedOwnerCapacityAndZero", r =>
        {
            using var settings = new SkillSettings();
            using var f = new SpiritRebirthStateTests.Fixture(r); f.Begin();
            var data = f.Magic.MagicData; data.MagicUserLevel = 6;
            var skill = new MagicPowerSkill("TM_SpiritPossession_ver", "") { level = 2 };
            data.magicPowerSkill_Custom = [skill];
            var need = new Need_Spirit(f.Original) { def = new NeedDef { defName = "HealthFormSpirit", needClass = typeof(Need_Spirit) } };
            need.CurLevel = 0;
            Check.Equal(142f, need.MaxLevel, "capacity uses same original saved level and Spirit versatility");
            skill.level = 3; Check.Equal(157f, need.MaxLevel, "capacity reads changed saved skill without cloning owner");
            skill.level = 0; data.magicPowerSkill_Cantrips = [new MagicPowerSkill("TM_Cantrips_ver", "") { level = 10 }];
            Check.Equal(142f, need.MaxLevel, "saved Cantrips versatility preserves native level divided by five floor");
            TorannMagic.ModOptions.Settings.Instance.AIHardMode = true;
            Check.Equal(157f, need.MaxLevel, "native noncolonist AI hard mode uses rank three");
            TorannMagic.ModOptions.Settings.Instance.AIHardMode = false;
            skill.level = 3;
            var backstory = new BackstoryDef { defName = "OriginalAncientSpirit", identifier = "tm_ancient_spirit" }; f.Original.story.Adulthood = backstory;
            Check.Equal(207f, need.MaxLevel, "existing original ancient backstory retains its legacy bonus");
            Check.Same(backstory, f.Original.story.Adulthood, "capacity lookup preserves backstory object");
            Check.Equal(0f, need.CurLevel, "capacity change never refills need");
            Check.Same(data, f.Magic.MagicData, "original progression instance retained");
            Check.Same(skill, data.magicPowerSkill_Custom.Single(), "getter never replaces skill owner");
            Check.Equal(true, data.magicPowerCustom == null, "capacity getter does not initialize power families");
            need.CurLevel = 180f;
            f.Original.needs.AllNeeds.Add(need);
            f.Hold(f.Host, f.Original);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(f.Comp.SpiritRebirth, "TryEnterHost", f.Original, f.Host), "native host custody commits");
            Check.Equal(207f, need.MaxLevel, "host retains original elevated capacity");
            Check.Equal(180f, need.CurLevel, "host entry does not clamp existing energy");
            var stone = (Thing_SoulStone)typeof(SoulStoneHolderTests).GetMethod("Stone", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
            Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryCapture", f.Original, f.Host, false), "native stone transfer commits");
            Check.Equal(207f, need.MaxLevel, "stone retains original elevated capacity");
            Check.Equal(180f, need.CurLevel, "stone transfer does not clamp existing energy");
            Check.Same(need, f.Original.needs.AllNeeds.Single(), "transfers preserve actual elevated need owner");
            Set(f.Comp.SpiritRebirth, "form", SpiritRebirthForm.Dormant);
            Check.Equal(150f, need.MaxLevel, "inactive marker does not grant saved Spirit skill capacity");
        });
        foreach (bool dlc in new[] { true, false })
        yield return new("SpiritRebirthHealthForm.NativeHealthSingletonStagesAndRemoval." + (dlc ? "Royalty" : "NoDlc"), r => RunNative(r, dlc));
    }
    internal static void RunNative(RuntimeFixture r, bool dlc, Action<SpiritRebirthStateTests.Fixture, HediffDef, Need_Spirit>? continuation = null)
    {
            using var settings = new SkillSettings();
            using var f = new SpiritRebirthStateTests.Fixture(r);
            using var caps = (IDisposable)Activator.CreateInstance(typeof(SpiritNativeResurrectionTests).GetNestedType("CapacityDefinitions", BindingFlags.NonPublic)!, true)!;
            using var defs = new Definitions(r, dlc);
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World)); world.factionManager = new FactionManager();
            Set(Current.Game, "worldInt", world); Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(f.Original.def.race, "fleshType", new FleshTypeDef { defName = "FormOrganic", isOrganic = true });
            f.Original.ageTracker = new Pawn_AgeTracker(f.Original);
            f.Original.kindDef = new PawnKindDef { defName = "FormHuman", race = f.Original.def };
            f.Original.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }];
            Set(f.Original.ageTracker, "cachedLifeStageIndex", 0);
            Set(Find.TickManager, "ticksGameInt", 1000);
            f.Original.mindState = new Pawn_MindState(f.Original);
            f.Original.mindState.lastBecameVisibleTick = 0; f.Original.mindState.lastBecameInvisibleTick = -1;
            var core = new BodyPartRecord { def = new BodyPartDef { defName = "FormCore", hitPoints = 100, tags = [BodyPartTagDefOf.ConsciousnessSource, BodyPartTagDefOf.ManipulationLimbCore, BodyPartTagDefOf.MovingLimbCore] }, coverage = 1f };
            f.Original.def.race.body = new BodyDef { defName = "FormBody", corePart = core }; f.Original.def.race.body.ResolveReferences();
            var data = f.Magic.MagicData; var power = new MagicPowerSkill("TM_SpiritPossession_pwr", "") { level = 0 }; data.magicPowerSkill_Custom = [power];
            var need = new Need_Spirit(f.Original) { def = new NeedDef { defName = "RetainedSpirit", needClass = typeof(Need_Spirit) } }; need.CurLevel = 0;
            f.Original.needs.AllNeeds.Add(need);
            var foreign = new Hediff { pawn = f.Original, def = new HediffDef { defName = "ForeignIdentity", stages = [new HediffStage { painOffset = .3f }] } }; f.Original.health.hediffSet.hediffs.Add(foreign);
            Check.Equal(.3f, f.Original.health.hediffSet.PainTotal, "native pain exists before form");
            float originalConsciousness = f.Original.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            Check.Equal(true, f.Original.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) > 0f, "native manipulation available before form");
            Check.Equal(35f, f.Original.GetStatValue(StatDefOf.CarryingCapacity, cacheStaleAfterTicks: 1000), "prime native physical carrying cache");
            Check.Equal(1f, f.Original.GetStatValue(StatDefOf.PsychicSensitivity, cacheStaleAfterTicks: 1000), "prime native psychic sensitivity cache");
            Check.Equal(false, Reconcile(f.Original), "unmarked pawn has no effect mutation");
            f.Begin();
            Check.Equal(true, Reconcile(f.Original), "missing owned form added");
            var effect = f.Original.health.hediffSet.hediffs.Single(h => h.def == defs.Own);
            Check.Equal(dlc, f.Original.GetInvisibilityComp() != null, "native filtered component follows available DLC");
            Check.Equal(dlc, f.Original.IsPsychologicallyInvisible(), "native psychological visibility only with optional DLC comp");
            Check.Equal(1f, InvisibilityUtility.GetAlpha(f.Original), "native visibleToPlayer retains full alpha");
            Check.Same(TorannMagicDefOf.TM_SpiritPossessorHD.stages[0], effect.CurStage, "stage uses canonical read-only first stage");
            Check.Equal(0f, f.Original.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation), "native manipulation capacity suppressed");
            Check.Equal(0f, f.Original.health.hediffSet.PainTotal, "native pain factor suppressed");
            Check.Equal(0f, f.Original.GetStatValue(StatDefOf.CarryingCapacity, cacheStaleAfterTicks: 1000), "native stat factor suppresses cached carrying");
            Check.Equal(1.4f, f.Original.GetStatValue(StatDefOf.PsychicSensitivity, cacheStaleAfterTicks: 1000), "canonical first-stage psychic offset clears baseline cache");
            Check.Equal(false, Reconcile(f.Original), "unchanged effect does no transition work");
            Check.Same(effect, f.Original.health.hediffSet.hediffs.Single(h => h.def == defs.Own), "singleton owner retained");
            power.level = 2;
            effect.PostTickInterval(60);
            Check.Equal(false, Reconcile(f.Original), "native interval already reconciled changed power");
            Check.Same(TorannMagicDefOf.TM_SpiritPossessorHD.stages[2], effect.CurStage, "power level selects native stage III");
            Check.Equal(1.25f, f.Original.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness), "native health capacity sees canonical stage offset");
            Check.Equal(1.85f, f.Original.GetStatValue(StatDefOf.PsychicSensitivity, cacheStaleAfterTicks: 1000), "power transition clears prior psychic stat cache");
            Check.Same(need, f.Original.needs.AllNeeds.Single(), "form updates retain existing need owner");
            Check.Equal(0f, need.CurLevel, "form updates never refill zero energy");
            if (continuation != null) { continuation(f, defs.Own, need); return; }
            Set(f.Comp.SpiritRebirth, "form", SpiritRebirthForm.Dormant);
            Check.Equal(true, effect.CurStage == null, "inactive form becomes neutral before reconciliation");
            Check.Equal(true, Reconcile(f.Original), "inactive owned effect removed");
            Check.Same(foreign, f.Original.health.hediffSet.hediffs.Single(), "foreign health owner untouched");
            Check.Equal(true, f.Original.GetInvisibilityComp() == null, "native removal detaches invisibility component");
            Check.Equal(false, f.Original.IsPsychologicallyInvisible(), "native visibility restored after removal");
            Check.Equal(originalConsciousness, f.Original.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness), "native health cache reset after removal");
            Check.Equal(35f, f.Original.GetStatValue(StatDefOf.CarryingCapacity, cacheStaleAfterTicks: 1000), "native carrying cache reset after removal");
            Check.Equal(1f, f.Original.GetStatValue(StatDefOf.PsychicSensitivity, cacheStaleAfterTicks: 1000), "native psychic cache reset after removal");
    }
    sealed class SkillSettings : IDisposable
    {
        readonly TorannMagic.ModOptions.Settings previous = TorannMagic.ModOptions.Settings.Instance;
        public SkillSettings() => TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIHardMode = false };
        public void Dispose() => TorannMagic.ModOptions.Settings.Instance = previous;
    }
    sealed class Definitions : IDisposable
    {
        readonly HediffDef[] old = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly HediffDef oldLegacy = TorannMagicDefOf.TM_SpiritPossessorHD;
        readonly StatDef oldCarry = StatDefOf.CarryingCapacity;
        readonly StatDef oldPainThreshold = StatDefOf.PainShockThreshold;
        readonly StatDef oldPsychic = StatDefOf.PsychicSensitivity;
        readonly StatDef[] oldStats = DefDatabase<StatDef>.AllDefsListForReading.ToArray();
        readonly FieldInfo royalty = typeof(ModsConfig).GetField("royaltyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? oldRoyalty;
        readonly FieldInfo anomaly = typeof(ModsConfig).GetField("anomalyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? oldAnomaly;
        readonly FieldInfo installedRoyalty = typeof(ModLister).GetField("royaltyInstalled", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly FieldInfo installedAnomaly = typeof(ModLister).GetField("anomalyInstalled", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? oldInstalledRoyalty, oldInstalledAnomaly;
        readonly FieldInfo activeMods = typeof(ModsConfig).GetField("activeModsHashSet", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? oldActiveMods;
        readonly Dictionary<string, List<ModMetaData>> mods = (Dictionary<string, List<ModMetaData>>)typeof(ModLister).GetField("modsByPackageIdIgnorePostfix", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        readonly Dictionary<string, List<ModMetaData>> oldMods;
        public HediffDef Own { get; }
        public Definitions(RuntimeFixture r, bool dlc)
        {
            oldRoyalty = royalty.GetValue(null); royalty.SetValue(null, dlc);
            oldAnomaly = anomaly.GetValue(null); anomaly.SetValue(null, false);
            oldInstalledRoyalty = installedRoyalty.GetValue(null); installedRoyalty.SetValue(null, dlc);
            oldInstalledAnomaly = installedAnomaly.GetValue(null); installedAnomaly.SetValue(null, false);
            oldActiveMods = activeMods.GetValue(null); activeMods.SetValue(null, dlc ? new HashSet<string> { "ludeon.rimworld.royalty" } : new HashSet<string>());
            oldMods = new Dictionary<string, List<ModMetaData>>(mods); mods.Clear();
            var metadata = (ModMetaData)RuntimeHelpers.GetUninitializedObject(typeof(ModMetaData)); metadata.packageIdLowerCase = "ludeon.rimworld.royalty";
            mods[metadata.packageIdLowerCase] = [metadata];
            StatDefOf.CarryingCapacity = new StatDef { defName = "FormCarryingCapacity", defaultBaseValue = 35f, minValue = 0f, maxValue = 1000f, workerClass = typeof(StatWorker), cacheable = true };
            StatDefOf.PsychicSensitivity = new StatDef { defName = "FormPsychicSensitivity", defaultBaseValue = 1f, minValue = 0f, maxValue = 1000f, workerClass = typeof(StatWorker), cacheable = true };
            foreach (var stat in new[] { StatDefOf.CarryingCapacity, StatDefOf.PsychicSensitivity }) { DefDatabase<StatDef>.Add(stat); stat.Worker.SetCacheability(false); }
            StatDefOf.PainShockThreshold = new StatDef { defName = "FormPainThreshold", defaultBaseValue = .8f, minValue = 0f, maxValue = 1f, workerClass = typeof(StatWorker) };
            var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/HediffDefs/Hediffs_Possessed.xml"));
            var legacy = xml.Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_SpiritPossessorHD");
            TorannMagicDefOf.TM_SpiritPossessorHD = new HediffDef { defName = "TM_SpiritPossessorHD", stages = legacy.Element("stages")!.Elements().Select(e => new HediffStage {
                minSeverity = (float)e.Element("minSeverity")!, painFactor = (float)e.Element("painFactor")!,
                statFactors = [new StatModifier { stat = StatDefOf.CarryingCapacity, value = 0 }],
                statOffsets = [new StatModifier { stat = StatDefOf.PsychicSensitivity, value = (float)e.Element("statOffsets")!.Element("PsychicSensitivity")! }],
                capMods = e.Element("capMods")!.Elements().Select(c => new PawnCapacityModifier { capacity = DefDatabase<PawnCapacityDef>.GetNamed((string)c.Element("capacity")!), setMax = (float?)c.Element("setMax") ?? 999f, offset = (float?)c.Element("offset") ?? 0f }).ToList()
            }).ToList() };
            var nativeXml = new System.Xml.XmlDocument(); nativeXml.Load(Path.Combine(r.Repository, "v1.7/Defs/HediffDefs/Hediffs_SpiritRebirth.xml"));
            var comps = DirectXmlToObject.ObjectFromXml<List<HediffCompProperties>>(nativeXml.SelectSingleNode("Defs/HediffDef/comps")!, false);
            Check.Equal(dlc ? 1 : 0, comps.Count, "native XML loader filters optional DLC comp");
            Own = new HediffDef { defName = "TM_SpiritRebirthForm", hediffClass = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.Hediff_SpiritRebirthForm", true)!, initialSeverity = .5f, maxSeverity = 4f,
                forceRenderTreeRecache = true, comps = comps };
            DefDatabase<HediffDef>.Add(Own);
        }
        public void Dispose()
        {
            royalty.SetValue(null, oldRoyalty); anomaly.SetValue(null, oldAnomaly);
            installedRoyalty.SetValue(null, oldInstalledRoyalty); installedAnomaly.SetValue(null, oldInstalledAnomaly);
            activeMods.SetValue(null, oldActiveMods); mods.Clear(); foreach (var pair in oldMods) mods.Add(pair.Key, pair.Value);
            StatDefOf.PainShockThreshold = oldPainThreshold; StatDefOf.CarryingCapacity = oldCarry; TorannMagicDefOf.TM_SpiritPossessorHD = oldLegacy;
            StatDefOf.PsychicSensitivity = oldPsychic; DefDatabase<StatDef>.Clear(); foreach (var d in oldStats) DefDatabase<StatDef>.Add(d);
            DefDatabase<HediffDef>.Clear(); foreach (var d in old) DefDatabase<HediffDef>.Add(d);
        }
    }
}
