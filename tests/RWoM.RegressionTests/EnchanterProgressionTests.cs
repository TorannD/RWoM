using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class EnchanterProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EnchanterProgression.NativeBooksAndStageGates", Books);
        yield return new("EnchanterProgression.NativeChildrenAndSavedRetention", Children);
        yield return new("EnchanterProgression.NativeChildManaCosts", Costs);
        yield return new("EnchanterProgression.NativeTransitionCleansOnlyUnownedChildren", ChildCleanup);
        yield return new("EnchanterProgression.KeyedUiCoverageInSevenLanguages", KeyedCoverage);
        yield return new("EnchanterProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("EnchanterProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("EnchanterProgression.LegacyEnrollmentBlockedAndSavedFlagPreserved", LegacyAndFlags);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.enchanter");
        public readonly ClassDisciplineDef Profile;
        public readonly Faction Friendly = new() { def = new FactionDef { isPlayer = true } };
        public readonly Faction Enemy = new() { def = new FactionDef() };
        public Fixture(RuntimeFixture f, bool initiate = true)
        {
            LoadBaseDefinitions(f);
            if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null)
                DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "EnchanterBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "EnchanterTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.enchanter");
            Profile = new ClassDisciplineDef { defName = "EnchanterProgression", profileId = "rwom.enchanter", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.Enchanter, magicPowerList = node.Element("magicPowerList")!.Value,
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
        Check.Equal("Torn_BookOfEnchanter", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfEnchanter", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersE;
        foreach (var def in new[] { TorannMagicDefOf.TM_EnchantedBody, TorannMagicDefOf.TM_Transmutate })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_EnchanterStone, TorannMagicDefOf.TM_EnchantWeapon, TorannMagicDefOf.TM_Polymorph, TorannMagicDefOf.TM_Shapeshift })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.enchanter.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_EnchanterStone, TorannMagicDefOf.TM_EnchantWeapon })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(4, powers.Count(p => p.learned), "two adept families");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeEnchanter", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.enchanter.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Polymorph)).learned, "Polymorph granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift)).learned, "Shapeshift remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsEnchanter(w.Caster), "saved Enchanter identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var powers = w.Magic.MagicData.MagicPowersE;
        var originalOwners = powers.ToArray();
        var originalSkills = new[] { w.Magic.MagicData.MagicPowerSkill_EnchantedBody, w.Magic.MagicData.MagicPowerSkill_Transmutate,
            w.Magic.MagicData.MagicPowerSkill_EnchanterStone, w.Magic.MagicData.MagicPowerSkill_EnchantWeapon,
            w.Magic.MagicData.MagicPowerSkill_Polymorph, w.Magic.MagicData.MagicPowerSkill_Shapeshift };
        var skillRecords = originalSkills.Select(list => list.ToArray()).ToArray();
        var inherited = w.Magic.MagicData.MagicPowersIF.First(); inherited.learned = true;
        inherited.level = 1; inherited.ticksUntilNextCast = 37;
        var shared = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Transmutate));
        shared.learned = true; shared.autocast = true; shared.ticksUntilNextCast = 823;
        
        w.Magic.MagicData.MagicPowerSkill_EnchantedBody.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation with shared owner");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, ReferenceEquals(shared, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Transmutate))), "native shared owner retained");
        Check.Equal(true, originalOwners.SequenceEqual(powers), "all native owner references retained");
        var restoredSkills = new[] { w.Magic.MagicData.MagicPowerSkill_EnchantedBody, w.Magic.MagicData.MagicPowerSkill_Transmutate,
            w.Magic.MagicData.MagicPowerSkill_EnchanterStone, w.Magic.MagicData.MagicPowerSkill_EnchantWeapon,
            w.Magic.MagicData.MagicPowerSkill_Polymorph, w.Magic.MagicData.MagicPowerSkill_Shapeshift };
        for (int i = 0; i < originalSkills.Length; i++)
        {
            Check.Equal(true, ReferenceEquals(originalSkills[i], restoredSkills[i]), "native skill list retained: " + i);
            Check.Equal(true, skillRecords[i].SequenceEqual(restoredSkills[i]), "native skill objects retained: " + i);
        }
        Check.Equal(true, inherited.learned, "inherited other-class grant retained");
        Check.Equal(1, inherited.level, "inherited other-class rank retained");
        Check.Equal(37, inherited.ticksUntilNextCast, "inherited other-class cooldown retained");
        Check.Equal(true, shared.autocast, "autocast retained");
        Check.Equal(823, shared.ticksUntilNextCast, "cooldown retained");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_EnchantedBody.First().level, "skill investment retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == shared.abilityDef), "shared runtime restored once");
        var locked = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift));
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, locked), "missing profile cannot unlock ultimate");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shared), "missing profile preserves learned shared owner");
    }
    static void LegacyAndFlags(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var native = new Trait(TorannMagicDefOf.Enchanter);
        w.Caster.story.traits.allTraits.Add(native);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy Enchanter is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        Check.Equal(0, w.Progression.State.Disciplines.Count, "legacy rejection preserves empty progression");
        w.Caster.story.traits.allTraits.Remove(native);
        w.Magic.spell_Shapeshift = true;
        var fold = w.Magic.MagicData.MagicPowersE.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift));
        fold.level = 0; fold.ticksUntilNextCast = 823; fold.autocast = true;
        w.Magic.MagicData.MagicPowerSkill_Shapeshift.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "fresh foundation with old spell flag");
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, fold.learned, "saved ultimate flag retains native owner");
        Check.Equal(0, fold.level, "flag repair preserves native spell rank");
        Check.Equal(TorannMagicDefOf.TM_Shapeshift, fold.abilityDef, "saved rank selects native Shapeshift");
        Check.Equal(823, fold.ticksUntilNextCast, "flag repair preserves cooldown");
        Check.Equal(true, fold.autocast, "flag repair preserves autocast");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_Shapeshift.First().level, "flag repair preserves skill investment");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, fold), "prior ownership survives stage gate");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == fold.abilityDef), "flag restored without duplicate ability");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "prior spell does not advance stage");
    }

    static void Children(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var data = w.Magic.MagicData;
        var body = data.MagicPowersE.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EnchantedBody));
        var shape = data.MagicPowersE.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shapeshift));
        var aura = data.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EnchantedAura));
        var wolf = data.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShapeshiftDW));
        var versatility = data.MagicPowerSkill_Shapeshift.Single(s => s.label == "TM_Shapeshift_ver");
        versatility.level = 3;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, aura.learned, "no Aura before Body");
        Check.Equal(false, wolf.learned, "skill alone does not grant wolf");
        ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook));
        Check.Equal(true, aura.learned && w.Magic.spell_EnchantedAura, "learned Body grants native Aura");
        w.Magic.ResolveClassSkills();
        Check.Equal(false, wolf.learned || w.Magic.spell_ShapeshiftDW, "foundation skill cannot grant wolf");
        shape.learned = true; versatility.level = 2;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, wolf.learned, "owned Shape needs versatility three");
        versatility.level = 3;
        w.Magic.ResolveClassSkills();
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == wolf.abilityDef), "skill maintenance immediately exposes wolf");
        Check.Equal(true, wolf.learned && w.Magic.spell_ShapeshiftDW, "owned Shape and native skill derive wolf");
        shape.learned = false; body.learned = false; versatility.level = 0;
        w.Magic.spell_EnchantedAura = false; w.Magic.spell_ShapeshiftDW = false;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, aura.learned && wolf.learned, "independently saved children survive");
        Check.Equal(true, w.Magic.spell_EnchantedAura && w.Magic.spell_ShapeshiftDW, "saved owners repair child flags");
        aura.learned = false; wolf.learned = false;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, aura.learned && wolf.learned, "saved child flags repair exact owners");
        var poly = data.MagicPowersE.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Polymorph));
        poly.learned = true; poly.level = 2;
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(TorannMagicDefOf.TM_Polymorph_II, poly.abilityDef, "native Polymorph rank retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == poly.abilityDef), "rank runtime restored once");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "saved grants never advance stage");
    }

    static void KeyedCoverage(RuntimeFixture f)
    {
        foreach (string language in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
        {
            var xml = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Languages", language, "Keyed/TM_GrimoireProgression.xml"));
            foreach (string suffix in new[] { "", "FoundationTrial", "MasteryTrial", "AttunementHint", "AscensionAction", "AscensionNeedsOffering", "AscensionRequirements", "AttunementAction", "AttunementNeedsOffering" })
            {
                string key = "TM_ProgressionEnchanter" + suffix;
                var entries = xml.Root!.Elements(key).ToArray();
                Check.Equal(1, entries.Length, language + " has exactly one " + key);
                Check.Equal(false, string.IsNullOrWhiteSpace(entries.Single().Value), language + " translates " + key);
            }
        }
    }

    static void ChildCleanup(RuntimeFixture f)
    {
        foreach (bool learned in new[] { false, true })
        {
            using var w = new Fixture(f, false);
            // Exercise the native trait-transition contract, not a new-model
            // respec (which deliberately clears all learned ownership).
            w.Caster.AllComps.Remove(w.Progression);
            w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Enchanter));
            w.Magic.magicPowersInitialized = true;
            var aura = w.Magic.MagicData.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EnchantedAura));
            var wolf = w.Magic.MagicData.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShapeshiftDW));
            aura.learned = learned; wolf.learned = learned;
            w.Magic.spell_EnchantedAura = true; w.Magic.spell_ShapeshiftDW = true;
            foreach (var child in new[] { aura, wolf })
            {
                TM_ClassState.AddRuntimeAbility(w.Magic, child.abilityDef);
                w.Magic.AbilityData.Powers.Single(a => a.Def == child.abilityDef).CooldownTicksLeft = 71;
            }
            ClassSpellAccess.RestoreEnchanterChildren(w.Magic);
            Check.Equal(learned, aura.learned, "new-model helper leaves classic Aura owner untouched");
            Check.Equal(learned, wolf.learned, "new-model helper leaves classic wolf owner untouched");
            TM_ClassState.CaptureBeforeTraitsChanged(w.Caster);
            w.Caster.story.traits.allTraits.RemoveAll(t => t.def == TorannMagicDefOf.Enchanter);
            TM_ClassState.OnTraitsChanged(w.Caster);
            foreach (var child in new[] { aura, wolf })
            {
                Check.Equal(learned, child.learned, "transition preserves standalone owner state");
                Check.Equal(learned ? 1 : 0, w.Magic.AbilityData.Powers.Count(a => a.Def == child.abilityDef), "only independently learned child survives native removal");
                if (learned) Check.Equal(71, w.Magic.AbilityData.Powers.Single(a => a.Def == child.abilityDef).CooldownTicksLeft, "retained child keeps runtime cooldown");
            }
            Check.Equal(learned, w.Magic.spell_EnchantedAura, "orphan Aura flag cleared, learned grant retained");
            Check.Equal(learned, w.Magic.spell_ShapeshiftDW, "orphan wolf flag cleared, learned grant retained");
        }
    }

    static void Costs(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Set(w.Caster, "mapIndexOrState", (sbyte)-1);
        var data = w.Magic.MagicData;
        var bodyEfficiency = data.GetSkill_Efficiency(TorannMagicDefOf.TM_EnchantedBody);
        var shapeEfficiency = data.GetSkill_Efficiency(TorannMagicDefOf.TM_Shapeshift);
        foreach (var pair in new[] { (TorannMagicDefOf.TM_EnchantedAura, bodyEfficiency), (TorannMagicDefOf.TM_ShapeshiftDW, shapeEfficiency) })
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
        var trait = TorannMagicDefOf.Enchanter; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "Enchanter");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Enchanter matches native Enchanter offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "native work restriction parity");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsEnchanter(w.Caster), "invalid stage is not Enchanter identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsEnchanter(w.Caster), "wrong domain is not Enchanter identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual Enchanter trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}