using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class TechnomancerProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("TechnomancerProgression.NativeBooksAndStageGates", Books);
        yield return new("TechnomancerProgression.NativeBitSustainCreatesAndRetainsOneHediff", BitSustain);
        yield return new("TechnomancerProgression.NativeNanoChildOwnershipAndBitInitialization", NanoChild);
        yield return new("TechnomancerProgression.LegacyTransitionNanoOwnership", NanoTransition);
        yield return new("TechnomancerProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("TechnomancerProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("TechnomancerProgression.LegacyEnrollmentBlockedAndSavedFlagPreserved", LegacyAndFlags);
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
        public DisciplineProgress Path => Progression.State.Find("rwom.technomancer");
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
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "TechnomancerBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "TechnomancerTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.technomancer");
            Profile = new ClassDisciplineDef { defName = "TechnomancerProgression", profileId = "rwom.technomancer", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.Technomancer, magicPowerList = node.Element("magicPowerList")!.Value,
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
            DefDatabase<DesignationDef>.Clear(); foreach (var d in designations) DefDatabase<DesignationDef>.Add(d);
        }
    }

    static bool SkipBitSmoke() => false;
    static bool SkipBitPhysiology() => false;
    static bool BitDrawPosition(ref UnityEngine.Vector3 __result) { __result = UnityEngine.Vector3.zero; return false; }
    static void BitSustain(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var oldDef = TorannMagicDefOf.TM_TechnoBitHD;
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "TechnomancerBitCapacity" });
        var harmonyAssembly = Assembly.Load("0Harmony");
        string patchId = "rwom.techno.bit.sustain." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.Harmony")!, patchId)!;
        void Patch(MethodInfo method, string prefixName)
        {
            var prefix = Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(TechnomancerProgressionTests).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { method, prefix, null, null, null });
        }
        try
        {
            // Headless boundaries cover presentation and unrelated death/downing
            // physiology. Sustain, HealthUtility, HediffMaker and the native Bit
            // comp insertion/severity remain real.
            foreach (var smoke in typeof(FleckMaker).GetMethods().Where(m => m.Name == "ThrowSmoke")) Patch(smoke, nameof(SkipBitSmoke));
            Patch(typeof(Pawn).GetProperty("DrawPos")!.GetMethod!, nameof(BitDrawPosition));
            Patch(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(SkipBitPhysiology));
            w.Caster.Map.events = new MapEvents(w.Caster.Map);
            Set(w.Caster.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
            w.Caster.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "TechnomancerBitBody" } } };
            TorannMagicDefOf.TM_TechnoBitHD = new HediffDef {
                defName = "TM_TechnoBitHD", hediffClass = typeof(HediffWithComps), initialSeverity = .5f, minSeverity = .001f, maxSeverity = 100f,
                comps = new List<HediffCompProperties> { new() { compClass = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.HediffComp_TechnoBit")! } }
            };
            TorannMagicDefOf.TM_TechnoBit.shouldInitialize = false;
            w.Magic.AbilityData.Powers.RemoveAll(a => a.Def == TorannMagicDefOf.TM_TechnoBit);
            var owner = w.Magic.MagicData.MagicPowersT.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoBit));
            owner.learned = false;
            w.Magic.ResolveClassSkills();
            Check.Equal(0, w.Caster.health.hediffSet.hediffs.Count(h => h.def == TorannMagicDefOf.TM_TechnoBitHD), "unlearned native Bit grants no hediff");
            owner.learned = true;
            w.Magic.ResolveClassSkills();
            var bit = w.Caster.health.hediffSet.hediffs.Single(h => h.def == TorannMagicDefOf.TM_TechnoBitHD);
            Check.Equal(true, bit is HediffWithComps withComps && withComps.comps.Any(c => c.GetType().Name == "HediffComp_TechnoBit"), "native repair comp created through HealthUtility");
            bit.Severity = 41f;
            w.Magic.ResolveClassSkills(); w.Magic.ResolveClassSkills();
            Check.Same(bit, w.Caster.health.hediffSet.hediffs.Single(h => h.def == TorannMagicDefOf.TM_TechnoBitHD), "repeated sustain retains exact native hediff");
            Check.Equal(41f, bit.Severity, "repeated sustain does not reset existing severity");
            Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_TechnoBit), "passive sustain needs no fake runtime cast");
            Check.Equal(0, w.Caster.story.traits.allTraits.Count, "native sustain works without inserted class trait");
        }
        finally
        {
            TorannMagicDefOf.TM_TechnoBitHD = oldDef;
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var def in capacities) DefDatabase<PawnCapacityDef>.Add(def);
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { patchId });
        }
    }

    static void NanoTransition(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        foreach (bool learned in new[] { false, true })
        {
            var p = PawnWithComps(true, false);
            p.magic.magicPowersInitialized = true;
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Technomancer));
            var weapon = p.magic.MagicData.MagicPowersT.Single(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoWeapon));
            weapon.learned = true;
            var nano = p.magic.MagicData.MagicPowersStandalone.Single(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_NanoStimulant));
            nano.learned = learned;
            p.magic.AddPawnAbility(TorannMagicDefOf.TM_TechnoWeapon);
            p.magic.AddPawnAbility(TorannMagicDefOf.TM_NanoStimulant);
            TM_ClassState.CaptureBeforeTraitsChanged(p.pawn);
            p.pawn.story.traits.allTraits.RemoveAll(t => t.def == TorannMagicDefOf.Technomancer);
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.HeartOfFrost));
            TM_ClassState.OnTraitsChanged(p.pawn);
            Check.Equal(learned, p.magic.AbilityData.Powers.Any(a => a.Def == TorannMagicDefOf.TM_NanoStimulant), "class child removed unless independently learned");
            Check.Equal(learned, nano.learned, "transition preserves standalone ownership");
        }
    }

    static void NanoChild(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var nano = w.Magic.MagicData.MagicPowersStandalone.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_NanoStimulant));
        var weapon = w.Magic.MagicData.MagicPowersT.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoWeapon));
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, nano.learned, "unselected path does not acquire child");
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation grants native weapon");
        Check.Equal(true, nano.learned, "native weapon persists standalone Nano owner");
        nano.autocast = true; nano.ticksUntilNextCast = 345;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == nano.abilityDef), "native Nano runtime deduplicated");
        Check.Equal(345, nano.ticksUntilNextCast, "Nano cooldown preserved");
        Check.Equal(true, nano.autocast, "Nano autocast preserved");
        nano.learned = false; weapon.learned = false;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, nano.learned, "unlearned weapon cannot acquire child");
        weapon.learned = true;
        Set(w.Path, "stage", (InitiationStage)0);
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, nano.learned, "invalid stage cannot acquire child");
        nano.learned = true;
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, nano.learned, "previously learned child remains owned");
        DefDatabase<ClassDisciplineDef>.Clear();
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, nano.learned, "missing profile preserves learned child");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == nano.abilityDef), "missing profile restores genuine child runtime");
        TorannMagicDefOf.TM_TechnoBit.shouldInitialize = false;
        w.Magic.AbilityData.Powers.RemoveAll(a => a.Def == TorannMagicDefOf.TM_TechnoBit);
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(0, w.Magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_TechnoBit), "passive bit gets no fake cast ability");
    }

    static void Books(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        w.Magic.MagicData.MagicAbilityPoints = 8; w.Magic.MagicData.MagicUserXP = 43;
        Check.Equal("Torn_BookOfMagitech", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfMagitech", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersT;
        foreach (var def in new[] { TorannMagicDefOf.TM_TechnoBit, TorannMagicDefOf.TM_TechnoWeapon })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_TechnoTurret, TorannMagicDefOf.TM_TechnoShield, TorannMagicDefOf.TM_Overdrive, TorannMagicDefOf.TM_Sabotage, TorannMagicDefOf.TM_OrbitalStrike })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.technomancer.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_TechnoTurret, TorannMagicDefOf.TM_TechnoShield })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(4, powers.Count(p => p.learned), "two adept families");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeTechnomancer", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.technomancer.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_Overdrive, TorannMagicDefOf.TM_Sabotage })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "master owns " + def.defName);
        Check.Equal(6, powers.Count(p => p.learned), "two master families");
        foreach (var def in new[] { TorannMagicDefOf.TM_TechnoShield, TorannMagicDefOf.TM_Overdrive, TorannMagicDefOf.TM_Sabotage, TorannMagicDefOf.TM_OrbitalStrike })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).requiresScroll, "native scroll restriction preserved " + def.defName);
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_OrbitalStrike)).learned, "OrbitalStrike remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsTechnomancer(w.Caster), "saved Technomancer identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var powers = w.Magic.MagicData.MagicPowersT;
        var shared = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoWeapon));
        shared.learned = true; shared.autocast = true; shared.ticksUntilNextCast = 823;
        
        w.Magic.MagicData.MagicPowerSkill_TechnoBit.First().level = 12;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation with shared owner");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, ReferenceEquals(shared, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_TechnoWeapon))), "native shared owner retained");
        Check.Equal(true, shared.autocast, "autocast retained");
        Check.Equal(823, shared.ticksUntilNextCast, "cooldown retained");
        Check.Equal(12, w.Magic.MagicData.MagicPowerSkill_TechnoBit.First().level, "skill investment retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == shared.abilityDef), "shared runtime restored once");
        var locked = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_OrbitalStrike));
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, locked), "missing profile cannot unlock ultimate");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shared), "missing profile preserves learned shared owner");
    }
    static void LegacyAndFlags(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var native = new Trait(TorannMagicDefOf.Technomancer);
        w.Caster.story.traits.allTraits.Add(native);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy Technomancer is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        Check.Equal(0, w.Progression.State.Disciplines.Count, "legacy rejection preserves empty progression");
        w.Caster.story.traits.allTraits.Remove(native);
        var unrelated = new MagicPower(new List<AbilityUser.AbilityDef> { TorannMagicDefOf.TM_OrbitalStrike });
        w.Magic.MagicData.MagicPowersStandalone.Add(unrelated);
        w.Magic.spell_OrbitalStrike = true;
        var fold = w.Magic.MagicData.MagicPowersT.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_OrbitalStrike));
        fold.level = 2; fold.ticksUntilNextCast = 823; fold.autocast = true;
        w.Magic.MagicData.MagicPowerSkill_OrbitalStrike.First().level = 2;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "fresh foundation with old spell flag");
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, fold.learned, "saved ultimate flag retains native owner");
        Check.Equal(false, unrelated.learned, "ultimate flag never repairs unrelated matching owner");
        Check.Equal(2, fold.level, "flag repair preserves native spell rank");
        Check.Equal(TorannMagicDefOf.TM_OrbitalStrike_II, fold.abilityDef, "saved rank selects OrbitalStrike II");
        Check.Equal(823, fold.ticksUntilNextCast, "flag repair preserves cooldown");
        Check.Equal(true, fold.autocast, "flag repair preserves autocast");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_OrbitalStrike.First().level, "flag repair preserves skill investment");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, fold), "prior ownership survives stage gate");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == fold.abilityDef), "flag restored without duplicate ability");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "prior spell does not advance stage");
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.Technomancer; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "Technomancer");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Technomancer matches native Technomancer offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "native work restriction parity");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsTechnomancer(w.Caster), "invalid stage is not Technomancer identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsTechnomancer(w.Caster), "wrong domain is not Technomancer identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual Technomancer trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}