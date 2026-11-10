using System.Reflection;
using System.Xml.Linq;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthFormTests
{
    static object Call(string type, string method, params object[] args)
        => typeof(SpiritRebirthState).Assembly.GetType("TorannMagic.ClassProgression." + type)!
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritRebirthForm.AutocastSettingsBelongToEachOwner", f =>
        {
            using var w = new SpiritRebirthStateTests.Fixture(f);
            using var defs = new Definitions(f);
            var definition = DefDatabase<TM_CustomPowerDef>.GetNamed("TM_SpiritDrain").customPower;
            var settings = definition.autocasting;
            var oldConditions = settings.advancedConditionDefs;
            float range = settings.maxRange;
            try
            {
                settings.advancedConditionDefs = ["native-condition"];
                var otherComp = new CompClassProgression { parent = w.Host }; w.Host.AllComps.Add(otherComp);
                w.Begin(); Check.Equal(true, SpiritRebirthStateTests.Invoke(otherComp, "BeginSpiritRebirth"), "second original explicitly marked");
                foreach (var pawn in new[] { w.Original, w.Host })
                    Check.Equal(true, (bool)Call("SpiritRebirthAbilities", "Ensure", pawn), "canonical owners initialized");
                MagicPower Owner(Pawn pawn) => ((MagicData)typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(pawn.GetComp<CompAbilityUserMagic>())!).magicPowerCustom.Single(p => p.TMabilityDefs[0] == definition.abilityDefs[0]);
                var first = Owner(w.Original); var second = Owner(w.Host);
                first.autocasting.maxRange = range + 19;
                first.autocasting.advancedConditionDefs.Add("owner-only-condition");
                Check.Equal(range, second.autocasting.maxRange, "one pawn cannot alter another pawn range");
                Check.Equal(range, settings.maxRange, "owner mutation cannot alter XML definition");
                Check.Equal(1, second.autocasting.advancedConditionDefs.Count, "second owner's condition list isolated");
                Check.Equal(1, settings.advancedConditionDefs.Count, "definition condition list isolated");
                var firstSettings = first.autocasting;
                Check.Equal(true, (bool)Call("SpiritRebirthAbilities", "Ensure", w.Original), "repeat initialization accepted");
                Check.Same(first, Owner(w.Original), "repeat retains original owner");
                Check.Same(firstSettings, first.autocasting, "repeat retains owner settings and edits");
                Check.Equal(range + 19, first.autocasting.maxRange, "repeat preserves local range");
            }
            finally { settings.advancedConditionDefs = oldConditions; settings.maxRange = range; }
        });
        yield return new("SpiritRebirthForm.ExactIdentityAndCustody", f =>
        {
            using var w = new SpiritRebirthStateTests.Fixture(f);
            bool Is(Pawn p) => (bool)Call("SpiritRebirthFormPolicy", "IsEthereal", p);
            Check.Equal(false, Is(w.Original), "unmarked original is not opted in");
            var state = w.Begin();
            Check.Equal(true, Is(w.Original), "valid live original qualifies off map");
            Check.Equal(false, (bool)Call("SpiritRebirthFormPolicy", "IsSpawnedEthereal", w.Original), "world effects require spawned pawn");
            Check.Equal(false, Is(w.Host), "host does not inherit form");
            var map = Find.Maps[0]; map.spawnedThings = new ThingOwner<Thing>(map);
            Check.Equal(true, map.spawnedThings.TryAdd(w.Original, false), "real native map owner accepts original");
            Set(w.Original, "mapIndexOrState", (sbyte)0);
            Check.Equal(true, (bool)Call("SpiritRebirthFormPolicy", "IsSpawnedEthereal", w.Original), "native map owner qualifies spawned original");
            map.spawnedThings.Remove(w.Original);
            Check.Equal(false, Is(w.Original), "map index alone cannot authorize malformed spawned custody");
            Set(w.Original, "mapIndexOrState", (sbyte)-1);
            var possession = w.Hold(w.Host, w.Original);
            Check.Equal(false, Is(w.Original), "stale ethereal marker cannot override actual native custody");
            possession.GetDirectlyHeldThings().Remove(w.Original);
            foreach (var form in new[] { SpiritRebirthForm.Dormant, SpiritRebirthForm.Possessed, SpiritRebirthForm.Stone, (SpiritRebirthForm)99 })
            { Set(state, "form", form); Check.Equal(false, Is(w.Original), "nonethereal or unsupported form denied"); }
            Set(state, "form", SpiritRebirthForm.Ethereal); Set(state, "version", 999);
            Check.Equal(false, Is(w.Original), "future version denied");
            Set(state, "version", SpiritRebirthState.CurrentVersion); Set(state, "original", w.Other);
            Check.Equal(false, Is(w.Original), "foreign identity denied");
        });
        yield return new("SpiritRebirthForm.MissingOnlySpiritOwnersAndScribe", f =>
        {
            using var w = new SpiritRebirthStateTests.Fixture(f);
            using var defs = new Definitions(f);
            bool Ensure() => (bool)Call("SpiritRebirthAbilities", "Ensure", w.Original);
            Set(w.Magic, "magicData", null!);
            Check.Equal(false, Ensure(), "unmarked caller cannot initialize data");
            w.Begin();
            Check.Equal(true, Ensure(), "valid original initializes exact family");
            var data = (MagicData)typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(w.Magic)!;
            Check.Equal(6, data.magicPowerCustom.Count, "six canonical owners only");
            Check.Equal(18, data.magicPowerSkill_Custom.Count, "eighteen Spirit skills only");
            Check.Equal("TM_SpiritDrain,TM_SpiritPossession", string.Join(",", data.magicPowerCustom.Where(p => p.learned).Select(p => p.TMabilityDefs[0].defName).OrderBy(n => n)), "only native guaranteed initial spells learned");
            var owner = data.magicPowerCustom.Single(p => p.TMabilityDefs[0].defName == "TM_DistortSpirit");
            owner.level = 2; owner.learned = false; owner.autocast = true; owner.ticksUntilNextCast = 417;
            var skill = data.magicPowerSkill_Custom[0]; skill.level = 3;
            data.MagicAbilityPoints = 17; data.MagicUserXP = 234;
            var runtime = w.Magic.AbilityData.Powers.First(); runtime.CooldownTicksLeft = 123;
            var learnedOwner = data.magicPowerCustom.First(p => p.learned); learnedOwner.maxLevel = 73;
            Check.Equal(true, Ensure(), "repeat accepted");
            Check.Same(owner, data.magicPowerCustom.Single(p => p.TMabilityDefs[0].defName == "TM_DistortSpirit"), "existing owner retained");
            Check.Equal(false, owner.learned, "existing unlearned owner never rerolled");
            Check.Equal(2, owner.level, "rank retained"); Check.Equal(417, owner.ticksUntilNextCast, "owner timer retained");
            Check.Equal(123, runtime.CooldownTicksLeft, "runtime cooldown retained");
            Check.Equal(73, learnedOwner.maxLevel, "runtime lookup does not rewrite saved owner metadata");
            Check.Equal(2, w.Magic.AbilityData.Powers.Count, "runtime repeated without duplicates");
            var path = f.Save(data); MagicData loaded = null!;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "magicData");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(w.Original); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            Set(w.Magic, "magicData", loaded);
            Check.Equal(true, Ensure(), "native Scribe data reconciles");
            Check.Equal(6, loaded.magicPowerCustom.Count, "save has no duplicate family");
            Check.Equal(18, loaded.magicPowerSkill_Custom.Count, "save has no duplicate skills");
            Check.Equal(17, loaded.MagicAbilityPoints, "saved points retained"); Check.Equal(234, loaded.MagicUserXP, "saved XP retained");
            Check.Equal(3, loaded.magicPowerSkill_Custom.Single(s => s.label == skill.label).level, "saved skill retained");
        });
        yield return new("SpiritRebirthForm.ClassedPawnKeepsGuaranteedSpirits", f =>
        {
            using var w = new SpiritRebirthStateTests.Fixture(f);
            using var defs = new Definitions(f);
            bool Ensure() => (bool)Call("SpiritRebirthAbilities", "Ensure", w.Original);
            // The classed-pawn state from the live report: a retained
            // MagicData whose aggregate custom getter already materialized
            // unlearned owners for the whole mage tree, including the
            // guaranteed spirit powers. The fresh-owner guarantee inside
            // EnsureSpiritOwner never ran for those owners.
            Set(w.Magic, "magicData", new MagicData());
            var data = (MagicData)typeof(CompAbilityUserMagic).GetField("magicData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(w.Magic)!;
            _ = data.MagicPowersCustom;
            var drain = data.magicPowerCustom.Single(p => p.TMabilityDefs[0].defName == "TM_SpiritDrain");
            var distort = data.magicPowerCustom.Single(p => p.TMabilityDefs[0].defName == "TM_DistortSpirit");
            Check.Equal(false, drain.learned, "classed pawn state: guaranteed owners start unlearned");
            Check.Equal(false, distort.learned, "chance powers start unlearned too");
            w.Begin();
            Check.Equal(true, Ensure(), "release accepted for the classed pawn");
            Check.Equal("TM_SpiritDrain,TM_SpiritPossession",
                string.Join(",", data.magicPowerCustom.Where(p => p.learned).Select(p => p.TMabilityDefs[0].defName).OrderBy(n => n)),
                "the guarantee reaches existing owners");
            Check.Equal(false, distort.learned, "chance powers stay never-rerolled");
            Check.Equal(true, w.Magic.AbilityData.Powers.Any(p => p?.Def?.defName == "TM_SpiritDrain"), "drain runtime granted");
            Check.Equal(true, w.Magic.AbilityData.Powers.Any(p => p?.Def?.defName == "TM_SpiritPossession"), "possession runtime granted");
        });
    }

    sealed class Definitions : IDisposable
    {
        readonly TM_CustomClassDef[] old = DefDatabase<TM_CustomClassDef>.AllDefsListForReading.ToArray();
        readonly Dictionary<TMAbilityDef, float> chances = new();
        readonly Dictionary<TMAbilityDef, bool> initialize = new();
        readonly Dictionary<TMAbilityDef, Type> abilityClasses = new();
        readonly TM_CustomPowerDef[] oldPowers = DefDatabase<TM_CustomPowerDef>.AllDefsListForReading.ToArray();
        public Definitions(RuntimeFixture f)
        {
            var xml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomClassDef.xml"));
            var native = xml.Descendants("li").Single(e => (string?)e.Element("classTrait") == "TM_Possessor");
            var abilities = native.Element("classMageAbilities")!.Elements().Select(e => DefDatabase<TMAbilityDef>.GetNamedSilentFail(e.Value) ?? f.AddAbility(e.Value)).ToList();
            var powersXml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomPowerDef.xml"));
            foreach (var name in new[] { "TM_SpiritRally", "TM_SummonSpiritAnimalMass" })
            {
                if (DefDatabase<TM_CustomPowerDef>.GetNamedSilentFail(name) != null) continue;
                var p = powersXml.Root!.Elements().Single(e => (string?)e.Element("defName") == name).Element("customPower")!;
                DefDatabase<TM_CustomPowerDef>.Add(new TM_CustomPowerDef { defName = name, customPower = new TM_CustomPower {
                    forMage = true, maxLevel = 3, learnCost = 2, costToLevel = 1,
                    abilityDefs = p.Element("abilityDefs")!.Elements().Select(e => (AbilityUser.AbilityDef)f.Ability(e.Value)).ToList(),
                    skills = p.Element("skills")!.Elements().Select(e => new TM_CustomSkill { label = (string)e.Element("label")!, description = (string)e.Element("description")!, levelMax = (int)e.Element("levelMax")!, costToLevel = (int)e.Element("costToLevel")! }).ToList() } });
            }
            var spellXml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Possessed.xml"));
            foreach (var a in abilities)
            {
                var node = spellXml.Root!.Elements().Single(e => (string?)e.Element("defName") == a.defName);
                chances[a] = a.learnChance; initialize[a] = a.shouldInitialize;
                abilityClasses[a] = a.abilityClass; a.abilityClass = typeof(MagicAbility);
                a.learnChance = (float?)node.Element("learnChance") ?? 1;
                a.shouldInitialize = (bool?)node.Element("shouldInitialize") ?? true;
            }
            DefDatabase<TM_CustomClassDef>.Clear();
            DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [new TM_CustomClass { classTrait = TorannMagicDefOf.TM_Possessor, isMage = true, classMageAbilities = abilities }] });
        }
        public void Dispose()
        {
            foreach (var pair in chances) pair.Key.learnChance = pair.Value;
            foreach (var pair in initialize) pair.Key.shouldInitialize = pair.Value;
            foreach (var pair in abilityClasses) pair.Key.abilityClass = pair.Value;
            DefDatabase<TM_CustomPowerDef>.Clear(); foreach (var d in oldPowers) DefDatabase<TM_CustomPowerDef>.Add(d);
            DefDatabase<TM_CustomClassDef>.Clear(); foreach (var d in old) DefDatabase<TM_CustomClassDef>.Add(d);
        }
    }
}
