using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BrightmageProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BrightmageProgression.NativeBooksAndStageGates", Books);
        yield return new("BrightmageProgression.NativeChildrenAndSavedRetention", Children);
        yield return new("BrightmageProgression.NativeChildManaCosts", Costs);
        yield return new("BrightmageProgression.KeyedUiCoverageInSevenLanguages", KeyedCoverage);
        yield return new("BrightmageProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("BrightmageProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("BrightmageProgression.LegacyEnrollmentAndExactNativeScrollPayment", LegacyAndScroll);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly HediffDef previousLight = TorannMagicDefOf.TM_LightCapacitanceHD;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.brightmage");
        public readonly ClassDisciplineDef Profile;
        public readonly Faction Friendly = new() { def = new FactionDef { isPlayer = true } };
        public readonly Faction Enemy = new() { def = new FactionDef() };
        public Fixture(RuntimeFixture f, bool initiate = true)
        {
            LoadBaseDefinitions(f);
            // Native resource creation has its own lifecycle fixture; keep this owner fixture independent.
            TorannMagicDefOf.TM_LightCapacitanceHD = null;
            if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            TorannMagicDefOf.TM_LightSkipMass.shouldInitialize = false;
            TorannMagicDefOf.TM_LightSkipGlobal.shouldInitialize = false;
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "BrightmageBookDesignation" });
            map.designationManager = new DesignationManager(map); map.listerMergeables = new ListerMergeables(map);
            map.reservationManager = new Verse.AI.ReservationManager(map); map.physicalInteractionReservationManager = new PhysicalInteractionReservationManager();
            Set(Current.Game, "maps", new List<Map> { map });
            Set(Friendly, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } });
            Set(Enemy, "relations", new List<FactionRelation> { new() { other = Friendly, kind = FactionRelationKind.Hostile } });
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic;
            Target = PawnWithComps(false, false).pawn;
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn); pawn.guest = new Pawn_GuestTracker(pawn);
                pawn.kindDef = new PawnKindDef { defName = "BrightmageTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.brightmage");
            Profile = new ClassDisciplineDef { defName = "BrightmageProgression", profileId = "rwom.brightmage", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.TM_Brightmage, magicPowerList = node.Element("magicPowerList")!.Value,
                foundationBook = BookDef(node.Element("foundationBook")!.Value), adeptBook = BookDef(node.Element("adeptBook")!.Value),
                adeptTrialId = node.Element("adeptTrialId")!.Value,
                masterBook = BookDef(node.Element("masterBook")!.Value), masterTrialId = node.Element("masterTrialId")!.Value,
                spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell {
                    ability = DefDatabase<TMAbilityDef>.GetNamed(e.Element("ability")!.Value), stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value) }).ToList() };
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(Profile);
            if (initiate) Check.Equal(true, ClassInitiation.CompleteBook(Caster, Book(Profile.foundationBook)), "native foundation transaction succeeds");
        }
        public static ThingDef BookDef(string name)
        {
            var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            d.defName = name; d.thingClass = typeof(Thing); d.category = ThingCategory.Item; d.destroyable = true; d.stackLimit = 75; d.drawerType = DrawerType.RealtimeOnly;
            return d;
        }
        public Thing Book(ThingDef def)
        {
            var book = new Thing { def = def, stackCount = 2 };
            Check.Equal(true, Caster.inventory.innerContainer.TryAdd(book), "native inventory owns book"); return book;
        }
        public void Dispose()
        {
            Current.Game = previous;
            TorannMagicDefOf.TM_LightCapacitanceHD = previousLight;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.initialize; }
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
        }
    }

    static void Books(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        w.Magic.MagicData.MagicAbilityPoints = 8; w.Magic.MagicData.MagicUserXP = 43;
        Check.Equal("Torn_BookOfTheSun", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfTheSun", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersBrightmage;
        foreach (var def in new[] { TorannMagicDefOf.TM_LightLance, TorannMagicDefOf.TM_Sunfire })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_LightBurst, TorannMagicDefOf.TM_LightSkip, TorannMagicDefOf.TM_Refraction, TorannMagicDefOf.TM_SpiritOfLight })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.brightmage.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_LightBurst, TorannMagicDefOf.TM_LightSkip })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(4, powers.Count(p => p.learned), "two adept families");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeBrightmage", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.brightmage.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Refraction)).learned, "Refraction granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritOfLight)).learned, "SpiritOfLight remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsBrightmage(w.Caster), "saved Brightmage identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var powers = w.Magic.MagicData.MagicPowersBrightmage;
        var originalOwners = powers.ToArray();
        var originalSkills = new[] { w.Magic.MagicData.MagicPowerSkill_LightLance, w.Magic.MagicData.MagicPowerSkill_Sunfire,
            w.Magic.MagicData.MagicPowerSkill_LightBurst, w.Magic.MagicData.MagicPowerSkill_LightSkip,
            w.Magic.MagicData.MagicPowerSkill_Refraction, w.Magic.MagicData.MagicPowerSkill_SpiritOfLight };
        var skillRecords = originalSkills.Select(list => list.ToArray()).ToArray();
        var inherited = w.Magic.MagicData.MagicPowersIF.First(); inherited.learned = true;
        inherited.level = 1; inherited.ticksUntilNextCast = 37;
        var shared = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sunfire));
        shared.level = 2; shared.learned = true; shared.autocast = true; shared.ticksUntilNextCast = 823;
        
        w.Magic.MagicData.MagicPowerSkill_LightLance.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation with shared owner");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, ReferenceEquals(shared, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sunfire))), "native shared owner retained");
        Check.Equal(true, originalOwners.SequenceEqual(powers), "all native owner references retained");
        var restoredSkills = new[] { w.Magic.MagicData.MagicPowerSkill_LightLance, w.Magic.MagicData.MagicPowerSkill_Sunfire,
            w.Magic.MagicData.MagicPowerSkill_LightBurst, w.Magic.MagicData.MagicPowerSkill_LightSkip,
            w.Magic.MagicData.MagicPowerSkill_Refraction, w.Magic.MagicData.MagicPowerSkill_SpiritOfLight };
        for (int i = 0; i < originalSkills.Length; i++)
        {
            Check.Equal(true, ReferenceEquals(originalSkills[i], restoredSkills[i]), "native skill list retained: " + i);
            Check.Equal(true, skillRecords[i].SequenceEqual(restoredSkills[i]), "native skill objects retained: " + i);
        }
        Check.Equal(true, inherited.learned, "inherited other-class grant retained");
        Check.Equal(1, inherited.level, "inherited other-class rank retained");
        Check.Equal(37, inherited.ticksUntilNextCast, "inherited other-class cooldown retained");
        Check.Equal(TorannMagicDefOf.TM_Sunfire_II, shared.abilityDef, "native Sunfire rank retained");
        Check.Equal(true, shared.autocast, "autocast retained");
        Check.Equal(823, shared.ticksUntilNextCast, "cooldown retained");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_LightLance.First().level, "skill investment retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == shared.abilityDef), "shared runtime restored once");
        var locked = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritOfLight));
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, locked), "missing profile cannot unlock ultimate");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shared), "missing profile preserves learned shared owner");
    }
    static void Children(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var data = w.Magic.MagicData;
        var skip = data.MagicPowersBrightmage.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightSkip));
        var mass = data.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightSkipMass));
        var global = data.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightSkipGlobal));
        var power = data.MagicPowerSkill_LightSkip.Single(s => s.label == "TM_LightSkip_pwr");
        power.level = 2;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, mass.learned || global.learned, "skill investment alone never grants children");
        power.level = 0; w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.adeptBook)), "native adept earns LightSkip");
        Check.Equal(true, skip.learned, "exact native LightSkip owner learned");
        Check.Equal(false, mass.learned || global.learned, "power zero does not grant children");
        power.level = 1; ClassSpellAccess.RestoreBrightmageChildren(w.Magic);
        Check.Equal(true, mass.learned, "power one grants native Mass");
        Check.Equal(false, global.learned, "Global remains gated at power one");
        power.level = 2; ClassSpellAccess.RestoreBrightmageChildren(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, global.learned, "power two grants native Global");
        foreach (var child in new[] { mass, global })
            Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == child.abilityDef), "noninitializing native child runtime repaired once");
        skip.learned = false; power.level = 0;
        DefDatabase<ClassDisciplineDef>.Clear();
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, mass.learned && global.learned, "independent saved children survive parent/profile loss");
        Check.Equal(false, skip.learned, "child ownership never grants main owner");
        Check.Equal(true, w.Magic.SoL == null, "child restore never summons spirit");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no stock custom trait inserted");
        Check.Equal(true, w.Magic.customClass == null, "no stock custom identity synthesized");
    }

    sealed class UnpaidScroll : ThingWithComps
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { }
    }

    static void LegacyAndScroll(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var trait = new Trait(TorannMagicDefOf.TM_Brightmage);
        w.Caster.story.traits.allTraits.Add(trait);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy stock custom class is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        w.Caster.story.traits.allTraits.Remove(trait);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "native foundation transaction");
        var owner = w.Magic.MagicData.MagicPowersBrightmage.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SpiritOfLight));
        var ability = TorannMagicDefOf.TM_SpiritOfLight;
        var addonOwner = new MagicPower(new List<AbilityUser.AbilityDef> { ability }) { learned = true };
        w.Magic.MagicData.MagicPowersCustom.Add(addonOwner);
        var oldScroll = ability.learnItem;
        var scrollDef = Fixture.BookDef("SpellOf_SpiritOfLight");
        ability.learnItem = scrollDef;
        try
        {
            Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(w.Caster, owner, scrollDef), "ultimate scroll cannot bypass foundation");
            Set(w.Path, "stage", InitiationStage.Ascended);
            var unpaidDef = Fixture.BookDef("SpellOf_SpiritOfLight");
            ability.learnItem = unpaidDef;
            var unpaid = new UnpaidScroll { def = unpaidDef, stackCount = 1 };
            Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(unpaid), "unpaid scroll inventory ownership");
            new CompUseEffect_LearnSpell { parent = unpaid }.DoEffect(w.Caster);
            Check.Equal(false, owner.learned, "failed destruction never grants ultimate");
            Check.Equal(false, unpaid.Destroyed, "unpaid scroll retained");
            Check.Equal(1, unpaid.stackCount, "unpaid quantity retained");
            ability.learnItem = scrollDef;
            var scroll = new ThingWithComps { def = scrollDef, stackCount = 2 };
            Check.Equal(true, w.Caster.inventory.innerContainer.TryAdd(scroll), "scroll inventory ownership");
            var effect = new CompUseEffect_LearnSpell { parent = scroll };
            effect.DoEffect(w.Caster); effect.DoEffect(w.Caster);
            Check.Equal(true, owner.learned, "native ultimate scroll owns exact Brightmage power");
            Check.Equal(true, addonOwner.learned && w.Magic.MagicData.MagicPowersCustom.Contains(addonOwner), "overlapping addon owner preserved separately");
            Check.Equal(1, scroll.stackCount, "native scroll payment consumed once");
            Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == ability), "ultimate runtime restored once");
            Check.Equal(true, w.Magic.SoL == null, "learning does not create spirit");
            Check.Equal(true, w.Magic.customClass == null, "scroll does not synthesize custom identity");
        }
        finally { ability.learnItem = oldScroll; }
    }

    static void KeyedCoverage(RuntimeFixture f)
    {
        foreach (string language in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
        {
            var xml = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Languages", language, "Keyed/TM_GrimoireProgression.xml"));
            foreach (string suffix in new[] { "", "FoundationTrial", "MasteryTrial", "AttunementHint", "AscensionAction", "AscensionNeedsOffering", "AscensionRequirements", "AttunementAction", "AttunementNeedsOffering", "TrialProgress" })
            {
                string key = "TM_ProgressionBrightmage" + suffix;
                var entries = xml.Root!.Elements(key).ToArray();
                Check.Equal(1, entries.Length, language + " has exactly one " + key);
                Check.Equal(false, string.IsNullOrWhiteSpace(entries.Single().Value), language + " translates " + key);
            }
        }
    }

    static void Costs(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Set(w.Caster, "mapIndexOrState", (sbyte)-1);
        var data = w.Magic.MagicData;
        var efficiency = data.GetSkill_Efficiency(TorannMagicDefOf.TM_LightSkip);
        foreach (var pair in new[] { (TorannMagicDefOf.TM_LightSkipMass, efficiency), (TorannMagicDefOf.TM_LightSkipGlobal, efficiency) })
        {
            var ability = pair.Item1; var skill = pair.Item2;
            var oldCost = ability.manaCost; var oldReduction = ability.efficiencyReductionPercent;
            try
            {
                ability.manaCost = .4f; ability.efficiencyReductionPercent = .1f;
                skill.level = 0;
                float baseline = w.Magic.ActualManaCost(ability);
                skill.level = 2;
                Check.Equal(baseline * .8f, w.Magic.ActualManaCost(ability), "native child uses main efficiency: " + ability.defName);
            }
            finally { ability.manaCost = oldCost; ability.efficiencyReductionPercent = oldReduction; }
        }
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.TM_Brightmage; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_Brightmage");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Brightmage matches native Brightmage offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "native work restriction parity");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsBrightmage(w.Caster), "invalid stage is not Brightmage identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsBrightmage(w.Caster), "wrong domain is not Brightmage identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual Brightmage trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}