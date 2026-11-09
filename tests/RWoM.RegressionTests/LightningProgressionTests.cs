using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class LightningProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("LightningProgression.NativeBooksAndStageGates", Books);
        yield return new("LightningProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("LightningProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("LightningProgression.LegacyEnrollmentBlockedAndSavedFlagPreserved", LegacyAndFlags);
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
        public DisciplineProgress Path => Progression.State.Find("rwom.lightning");
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
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "LightningBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "LightningTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.lightning");
            Profile = new ClassDisciplineDef { defName = "LightningProgression", profileId = "rwom.lightning", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.StormBorn, magicPowerList = node.Element("magicPowerList")!.Value,
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
        Check.Equal("Torn_BookOfStormBorn", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfStormBorn", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersSB;
        foreach (var def in new[] { TorannMagicDefOf.TM_LightningBolt, TorannMagicDefOf.TM_AMP })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_LightningCloud, TorannMagicDefOf.TM_LightningStorm, TorannMagicDefOf.TM_EyeOfTheStorm })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.lightning.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_LightningCloud })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(3, powers.Count(p => p.learned), "one adept family");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeLightning", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.lightning.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningStorm)).learned, "LightningStorm granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EyeOfTheStorm)).learned, "EyeOfTheStorm remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsLightningMage(w.Caster), "saved Lightning identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var powers = w.Magic.MagicData.MagicPowersSB;
        var cloud = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningCloud));
        cloud.learned = true;
        var amp = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_AMP));
        amp.level = 2; amp.autocast = true; amp.ticksUntilNextCast = 823;
        w.Magic.MagicData.MagicPowerSkill_AMP.First().level = 2;
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, cloud.learned, "previous Cloud owner preserved");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, cloud), "owned Cloud always wins");
        Check.Equal(2, amp.level, "AMP native rank preserved");
        Check.Equal(true, amp.autocast, "autocast preserved");
        Check.Equal(823, amp.ticksUntilNextCast, "cooldown preserved");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_AMP.First().level, "skill investment preserved");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == amp.abilityDef), "ranked ability restored once");
        Check.Equal(true, ReferenceEquals(amp, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_AMP))), "original owner retained");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.adeptBook)), "adept with prior Cloud owner");
        Check.Equal(true, ReferenceEquals(cloud, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningCloud))), "adept reuses prior owner");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == cloud.abilityDef), "adept does not duplicate Cloud runtime");
        Check.Equal(2, amp.level, "stage transaction preserves existing rank");
        Check.Equal(823, amp.ticksUntilNextCast, "stage transaction preserves cooldown");
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LightningStorm))), "missing profile stays locked");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, cloud), "missing profile preserves learned owner");
    }
    static void LegacyAndFlags(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var native = new Trait(TorannMagicDefOf.StormBorn);
        w.Caster.story.traits.allTraits.Add(native);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy StormBorn is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        Check.Equal(0, w.Progression.State.Disciplines.Count, "legacy rejection preserves empty progression");
        w.Caster.story.traits.allTraits.Remove(native);
        w.Magic.spell_EyeOfTheStorm = true;
        var eye = w.Magic.MagicData.MagicPowersSB.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_EyeOfTheStorm));
        eye.ticksUntilNextCast = 823; eye.autocast = true;
        w.Magic.MagicData.MagicPowerSkill_EyeOfTheStorm.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "fresh foundation with old spell flag");
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, eye.learned, "saved Eye flag retains native SB owner");
        Check.Equal(823, eye.ticksUntilNextCast, "flag repair preserves cooldown");
        Check.Equal(true, eye.autocast, "flag repair preserves autocast");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_EyeOfTheStorm.First().level, "flag repair preserves skill investment");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, eye), "prior ownership survives stage gate");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == eye.abilityDef), "flag restored without duplicate ability");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "prior spell does not advance stage");
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.StormBorn; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "StormBorn");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Lightning matches native StormBorn offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "Lightning adds no disabled work");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsLightningMage(w.Caster), "invalid stage is not Lightning identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsLightningMage(w.Caster), "wrong domain is not Lightning identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual StormBorn trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}