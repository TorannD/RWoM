using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ArcanistProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ArcanistProgression.NativeBooksAndStageGates", Books);
        yield return new("ArcanistProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("ArcanistProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("ArcanistProgression.LegacyEnrollmentBlockedAndSavedFlagPreserved", LegacyAndFlags);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.arcanist");
        public readonly ClassDisciplineDef Profile;
        public readonly Faction Friendly = new() { def = new FactionDef { isPlayer = true } };
        public readonly Faction Enemy = new() { def = new FactionDef() };
        public Fixture(RuntimeFixture f, bool initiate = true)
        {
            LoadBaseDefinitions(f);
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "ArcanistBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "ArcanistTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.arcanist");
            Profile = new ClassDisciplineDef { defName = "ArcanistProgression", profileId = "rwom.arcanist", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.Arcanist, magicPowerList = node.Element("magicPowerList")!.Value,
                foundationBook = BookDef(node.Element("foundationBook")!.Value), adeptBook = BookDef(node.Element("adeptBook")!.Value),
                adeptTrialId = node.Element("adeptTrialId")!.Value,
                masterBook = BookDef(node.Element("masterBook")!.Value), masterTrialId = node.Element("masterTrialId")!.Value,
                spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell {
                    ability = DefDatabase<TMAbilityDef>.GetNamed(e.Element("ability")!.Value), stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value) }).ToList() };
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(Profile);
            if (initiate) Check.Equal(true, ClassInitiation.CompleteBook(Caster, Book(Profile.foundationBook)), "native foundation transaction succeeds");
        }
        static ThingDef BookDef(string name)
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
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
        }
    }

    static void Books(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        w.Magic.MagicData.MagicAbilityPoints = 8; w.Magic.MagicData.MagicUserXP = 43;
        Check.Equal("Torn_BookOfArcanist", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfArcanist", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersA;
        foreach (var def in new[] { TorannMagicDefOf.TM_MagicMissile, TorannMagicDefOf.TM_Blink })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_Shadow, TorannMagicDefOf.TM_Summon, TorannMagicDefOf.TM_Teleport, TorannMagicDefOf.TM_FoldReality })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.arcanist.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_Shadow, TorannMagicDefOf.TM_Summon })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(4, powers.Count(p => p.learned), "two adept families");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeArcanist", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.arcanist.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Teleport)).learned, "Teleport granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_FoldReality)).learned, "FoldReality remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsArcanist(w.Caster), "saved Arcanist identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var powers = w.Magic.MagicData.MagicPowersA;
        var priorBlink = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink));
        priorBlink.learned = true; priorBlink.level = 2; priorBlink.ticksUntilNextCast = 823;
        w.Magic.spell_Blink = true;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation with shared Blink owner");
        Check.Equal(true, ReferenceEquals(priorBlink, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink))), "foundation reuses shared Blink owner");
        Check.Equal(2, priorBlink.level, "foundation preserves shared Blink rank");
        Check.Equal(823, priorBlink.ticksUntilNextCast, "foundation preserves shared Blink cooldown");
        var teleport = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Teleport));
        teleport.learned = true; teleport.ticksUntilNextCast = 619;
        w.Magic.spell_Teleport = true;
        var shadow = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shadow));
        shadow.learned = true;
        var blink = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink));
        blink.level = 2; blink.autocast = true; blink.ticksUntilNextCast = 823;
        w.Magic.MagicData.MagicPowerSkill_Blink.First().level = 2;
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, shadow.learned, "previous Shadow owner preserved");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shadow), "owned Shadow always wins");
        Check.Equal(2, blink.level, "Blink native rank preserved");
        Check.Equal(true, blink.autocast, "autocast preserved");
        Check.Equal(823, blink.ticksUntilNextCast, "cooldown preserved");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_Blink.First().level, "skill investment preserved");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == blink.abilityDef), "ranked ability restored once");
        Check.Equal(true, ReferenceEquals(blink, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink))), "original owner retained");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.adeptBook)), "adept with prior Shadow owner");
        Check.Equal(true, ReferenceEquals(shadow, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Shadow))), "adept reuses prior owner");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == shadow.abilityDef), "adept does not duplicate Shadow runtime");
        Check.Equal(2, blink.level, "stage transaction preserves existing rank");
        Check.Equal(823, blink.ticksUntilNextCast, "stage transaction preserves cooldown");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, teleport), "prelearned shared Teleport survives stage gate");
        Check.Equal(619, teleport.ticksUntilNextCast, "shared Teleport cooldown preserved");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == teleport.abilityDef), "shared Teleport restored once");
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_FoldReality))), "missing profile stays locked");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shadow), "missing profile preserves learned owner");
    }
    static void LegacyAndFlags(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var native = new Trait(TorannMagicDefOf.Arcanist);
        w.Caster.story.traits.allTraits.Add(native);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy Arcanist is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        Check.Equal(0, w.Progression.State.Disciplines.Count, "legacy rejection preserves empty progression");
        w.Caster.story.traits.allTraits.Remove(native);
        w.Magic.spell_FoldReality = true;
        var fold = w.Magic.MagicData.MagicPowersA.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_FoldReality));
        fold.ticksUntilNextCast = 823; fold.autocast = true;
        w.Magic.MagicData.MagicPowerSkill_FoldReality.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "fresh foundation with old spell flag");
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, fold.learned, "saved Fold Reality flag retains native A owner");
        Check.Equal(823, fold.ticksUntilNextCast, "flag repair preserves cooldown");
        Check.Equal(true, fold.autocast, "flag repair preserves autocast");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_FoldReality.First().level, "flag repair preserves skill investment");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, fold), "prior ownership survives stage gate");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == fold.abilityDef), "flag restored without duplicate ability");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "prior spell does not advance stage");
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.Arcanist; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "Arcanist");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Arcanist matches native Arcanist offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "Arcanist adds no disabled work");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsArcanist(w.Caster), "invalid stage is not Arcanist identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsArcanist(w.Caster), "wrong domain is not Arcanist identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual Arcanist trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}