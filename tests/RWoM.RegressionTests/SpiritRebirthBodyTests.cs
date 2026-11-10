using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.Odyssey;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthBodyTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in SpellItemEffectTests.SpiritBodyVacuumCases()) yield return test;
        yield return new("SpiritRebirthBody.NativeNeedsWarriorAndZeroPreserved", r =>
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(NeedDefOf));
            using var f = new SpiritRebirthStateTests.Fixture(r);
            using var patches = new FleshStatTests.ProductionPatches(); patches.Add(typeof(TorannMagicMod.Pawn_NeedsTracker_Patch));
            var old = DefDatabase<NeedDef>.AllDefsListForReading.ToArray();
            var oldMana = TorannMagicDefOf.TM_Mana; var oldSpirit = TorannMagicDefOf.TM_SpiritND;
            var oldStamina = TorannMagicDefOf.TM_Stamina;
            var settings = TorannMagic.ModOptions.Settings.Instance;
            try
            {
                DefDatabase<NeedDef>.Clear();
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                TorannMagicDefOf.TM_Mana = Need("BodyMana", typeof(Need_Mana), true);
                TorannMagicDefOf.TM_Stamina = Need("BodyStamina", typeof(Need_Stamina), true);
                TorannMagicDefOf.TM_SpiritND = Need("BodySpirit", typeof(Need_Spirit), true);
                var rest = Need("BodyRest", typeof(Need_Rest), false);
                foreach (var p in new[] { f.Original, f.Host })
                {
                    p.ageTracker = new Pawn_AgeTracker(p);
                    p.kindDef = new PawnKindDef { defName = "SpiritBodyColonist", race = p.def };
                    p.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult } }];
                    Set(p.ageTracker, "cachedLifeStageIndex", 0);
                    Set(p.ageTracker, "ageBiologicalTicksInt", 25L * 3600000L);
                    Set(p,"factionInt",new Faction {def=new FactionDef {isPlayer=true}});
                }
                f.Comp.BeginNewProgression(Vocation.Warrior);
                Check.Equal(false, f.Magic.IsMagicUser, "saved warrior is not enrolled as mage");
                f.Begin();
                f.Original.needs.AddOrRemoveNeedsAsAppropriate();
                var spirit = Check.NotNull(f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "active original gets native Spirit need without legacy hediff");
                var mana = Check.NotNull(f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "warrior gets missing Mana carrier without role mutation");
                var stamina = Check.NotNull(f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_Stamina), "native update retains warrior stamina");
                Check.Equal(0f,stamina.CurLevel,"missing former reserve not refilled");
                Check.Equal(true, f.Original.needs.TryGetNeed(rest) == null, "physical rest removed");
                spirit.CurLevel = 0; mana.CurLevel = 0;
                Check.Equal(true, SpiritRebirthStateTests.Invoke(f.Comp.SpiritRebirth, "BeginDepletion", f.Original), "validated depletion enters pending retry state");
                f.Original.needs.AddOrRemoveNeedsAsAppropriate();
                Check.Same(spirit, f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "zero Spirit retry need preserved");
                Check.Same(mana, f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "existing Mana object preserved");
                Check.Equal(0f, mana.CurLevel, "refresh does not refill Mana");
                Check.Equal(0f, spirit.CurLevel, "refresh does not refill Spirit");
                Check.Equal(false, f.Magic.IsMagicUser, "need overlay does not change vocation");
                f.Hold(f.Host, f.Original);
                Check.Equal(true, SpiritRebirthStateTests.Invoke(f.Comp.SpiritRebirth, "TryEnterHost", f.Original, f.Host), "actual host takes original");
                f.Original.needs.AddOrRemoveNeedsAsAppropriate();
                Check.Same(spirit, f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "held original retains Spirit need");
                Check.Same(mana, f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "held original retains Mana upkeep owner");
                Check.Same(stamina,f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_Stamina),"hosted original retains its own stamina");
                Set(Current.Game,"tickManager",new TickManager());
                Set(Find.TickManager,"ticksGameInt",310);
                stamina.NeedInterval();
                float recovered=stamina.CurLevel;
                Check.Equal(true,recovered>0,$"original regains stamina while hosted (current {recovered}, max {stamina.MaxLevel}, base gain {((Need_Stamina)stamina).baseStaminaGain}, wild {f.Original.NonHumanlikeOrWildMan()}, multiplier {TorannMagic.ModOptions.Settings.Instance.needMultiplier})");
                stamina.NeedInterval();
                Check.Equal(recovered,stamina.CurLevel,"duplicate native interval never grants twice");
                var stoneDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                stoneDef.defName = "BodySoulStone"; stoneDef.thingClass = typeof(Thing_SoulStone); stoneDef.category = ThingCategory.Item; stoneDef.stackLimit = 1;
                var stone = new Thing_SoulStone { def = stoneDef };
                Check.Equal(true, SpiritRebirthStateTests.Invoke(stone, "TryCapture", f.Original, f.Host, false), "stone takes actual original");
                f.Original.needs.AddOrRemoveNeedsAsAppropriate();
                Check.Same(spirit, f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND), "stone retains Spirit need");
                Check.Same(mana, f.Original.needs.TryGetNeed(TorannMagicDefOf.TM_Mana), "stone retains Mana upkeep owner");
                Check.Equal(0f, spirit.CurLevel, "custody changes never refill Spirit");
                Check.Equal(0f, mana.CurLevel, "custody changes never refill Mana");
                f.Host.needs.AddOrRemoveNeedsAsAppropriate();
                Check.NotNull(f.Host.needs.TryGetNeed(rest), "host retains physical rest");
            }
            finally
            {
                TorannMagicDefOf.TM_Mana = oldMana; TorannMagicDefOf.TM_SpiritND = oldSpirit; TorannMagicDefOf.TM_Stamina = oldStamina;
                TorannMagic.ModOptions.Settings.Instance=settings;
                DefDatabase<NeedDef>.Clear(); foreach (var d in old) DefDatabase<NeedDef>.Add(d);
            }
        });
        yield return new("SpiritRebirthBody.CanonicalStatsOutsideNativeCache", r =>
        {
            using var f = new SpiritRebirthStateTests.Fixture(r);
            using var patches = new FleshStatTests.ProductionPatches();
            var type = typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression.SpiritRebirthBodyStatPatch");
            if (type != null) patches.Add(type);
            Set(Current.Game, "tickManager", new TickManager());
            var prior = TorannMagicDefOf.TM_SpiritTD;
            var priorStats = new Dictionary<FieldInfo, object?>();
            try
            {
                var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/PawnDefs/PawnKindDefs.xml"));
                var root = xml.Root!.Elements().Single(e => (string?)e.Attribute("Name") == "TM_BaseSpiritPawn");
                var race = xml.Root.Elements().Single(e => (string?)e.Element("defName") == "TM_SpiritTD");
                var canonical = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); canonical.statBases = [];
                foreach (string name in new[] { "Mass", "Flammability", "ToxicResistance", "ComfyTemperatureMin", "ComfyTemperatureMax", "MoveSpeed" })
                {
                    var field = typeof(StatDefOf).GetField(name)!; priorStats[field] = field.GetValue(null);
                    var stat = FleshStatTests.Stat(name, 3f); field.SetValue(null, stat);
                    float value = (float?)(race.Element("statBases")?.Element(name) ?? root.Element("statBases")!.Element(name)) ?? throw new Exception(name);
                    canonical.statBases.Add(new StatModifier { stat = stat, value = value });
                }
                TorannMagicDefOf.TM_SpiritTD = canonical;
                foreach (var entry in canonical.statBases) Check.Equal(3f, StatValue(f.Original, entry.stat), "prime native baseline cache");
                var state = f.Begin();
                foreach (var entry in canonical.statBases)
                {
                    Check.Equal(entry.value, StatValue(f.Original, entry.stat), "active form uses canonical Spirit stat");
                    Check.Equal(3f, StatValue(f.Host, entry.stat), "host stays physical");
                }
                Set(state, "form", SpiritRebirthForm.Dormant);
                foreach (var entry in canonical.statBases) Check.Equal(3f, StatValue(f.Original, entry.stat), "same tick removal exposes clean native baseline");
            }
            finally { TorannMagicDefOf.TM_SpiritTD = prior; foreach (var pair in priorStats) pair.Key.SetValue(null, pair.Value); }
        });
    }
    // Compile the native call after installing Harmony, even when this case runs first.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float StatValue(Pawn pawn, StatDef stat) => pawn.GetStatValue(stat, cacheStaleAfterTicks: 1000);
    static NeedDef Need(string name, Type type, bool enabled)
    {
        var def = new NeedDef { defName = name, needClass = type, onlyIfCausedByHediff = enabled, developmentalStageFilter = DevelopmentalStage.Adult };
        DefDatabase<NeedDef>.Add(def); return def;
    }
}

internal static partial class SpellItemEffectTests
{
    internal static IEnumerable<TestCase> SpiritBodyVacuumCases()
    {
        yield return new("SpiritRebirthBody.NativeVacuumTransitions", r => World(r, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1, Temperature = 21 };
            using var patches = new CreaturePatchScope();
            var pawn = w.Caster;
            pawn.def.stackLimit = 1;
            w.Map.spawnedThings ??= new ThingOwner<Thing>(w.Map);
            if (!w.Map.spawnedThings.Contains(pawn)) Check.Equal(true, w.Map.spawnedThings.TryAdd(pawn, false), "native map owner accepts original");
            var progression = new CompClassProgression { parent = pawn }; pawn.AllComps.Add(progression);
            Check.Equal(true, pawn.HarmedByVacuum, "prime native exposure cache");
            Check.Equal(true, SpiritRebirthStateTests.Invoke(progression, "BeginSpiritRebirth"), "explicit marker begins form");
            CreatureProtected(pawn);
            foreach (var form in new[] { SpiritRebirthForm.Dormant, SpiritRebirthForm.Possessed, SpiritRebirthForm.Stone })
            {
                Set(progression.SpiritRebirth, "form", form);
                Check.Equal(true, pawn.HarmedByVacuum, "inactive form restores exposure same tick");
                Check.Equal(0f, pawn.GetStatValue(StatDefOf.VacuumResistance, cacheStaleAfterTicks: 60), "no immune value leaked into cache");
            }
            Set(progression.SpiritRebirth, "form", SpiritRebirthForm.Ethereal); Set(progression.SpiritRebirth, "version", 999);
            Check.Equal(true, pawn.HarmedByVacuum, "unsupported marker never immune");
        }));
    }
}
