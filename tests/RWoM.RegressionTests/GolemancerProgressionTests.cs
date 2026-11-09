using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class GolemancerProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GolemancerProgression.NativeBooksAndStageGates", Books);
        yield return new("GolemancerProgression.PaidBrandAcquisitionAndRestore", Children);
        yield return new("GolemancerProgression.BrandBoundsAndMissingDefinitions", BrandBounds);
        yield return new("GolemancerProgression.BrandLatchScribeRoundTrip", BrandSave);
        yield return new("GolemancerProgression.ExactPaidBrandScroll", BrandScroll);
        yield return new("GolemancerProgression.NativeChildManaCosts", Costs);
        yield return new("GolemancerProgression.KeyedUiCoverageInSevenLanguages", KeyedCoverage);
        yield return new("GolemancerProgression.PreservedOwnersRanksAndMissingProfileGate", Ownership);
        yield return new("GolemancerProgression.NativePassiveParityAndDeduplication", Passive);
        yield return new("GolemancerProgression.LegacyEnrollmentAndExactNativeScrollPayment", LegacyAndScroll);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly TorannMagic.TM_CustomPowerDef[] customDefs = DefDatabase<TorannMagic.TM_CustomPowerDef>.AllDefsListForReading.ToArray();
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly DesignationDef[] designations = DefDatabase<DesignationDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool initialize)> abilities = new();
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.golemancer");
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
            foreach (var name in new[] { "TM_Golemancy", "TM_RuneCarving", "TM_Branding" })
            {
                var abilityNode = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Golemancer.xml"))
                    .Root!.Elements().Single(e => (string?)e.Element("defName") == name);
                DefDatabase<TMAbilityDef>.GetNamed(name).shouldInitialize = bool.Parse(abilityNode.Element("shouldInitialize")!.Value);
            }
            var stock = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomPowerDef.xml"));
            foreach (var name in BrandNames)
            {
                var brandNode = stock.Root!.Elements().Single(e => (string?)e.Element("defName") == name).Element("customPower")!;
                DefDatabase<TorannMagic.TM_CustomPowerDef>.Add(new TorannMagic.TM_CustomPowerDef { defName = name,
                    customPower = new TorannMagic.TMDefs.TM_CustomPower { forMage = true,
                        abilityDefs = brandNode.Element("abilityDefs")!.Elements().Select(e => (AbilityUser.AbilityDef)DefDatabase<TMAbilityDef>.GetNamed(e.Value)).ToList() } });
            }
            TM_Data.ResetDefinitionCaches();
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "GolemancerBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "GolemancerTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.golemancer");
            Profile = new ClassDisciplineDef { defName = "GolemancerProgression", profileId = "rwom.golemancer", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.TM_Golemancer, magicPowerList = node.Element("magicPowerList")!.Value,
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
            DefDatabase<TorannMagic.TM_CustomPowerDef>.Clear(); foreach (var d in customDefs) DefDatabase<TorannMagic.TM_CustomPowerDef>.Add(d);
            TM_Data.ResetDefinitionCaches();
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
        Check.Equal("Torn_BookOfGolemancy", w.Profile.foundationBook.defName, "native torn book");
        Check.Equal("BookOfGolemancy", w.Profile.adeptBook.defName, "native full book");
        var torn = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, torn), "foundation transaction");
        Check.Equal(1, torn.stackCount, "one torn book paid");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, torn), "cannot repeat foundation");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersGolemancer;
        foreach (var def in new[] { TorannMagicDefOf.TM_Golemancy, TorannMagicDefOf.TM_RuneCarving })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "two foundation families");
        foreach (var def in new[] { TorannMagicDefOf.TM_Branding, TorannMagicDefOf.TM_SigilSurge, TorannMagicDefOf.TM_SigilDrain, TorannMagicDefOf.TM_LivingWall })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later family locked " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "foundation trial required");
        Check.Equal(2, full.stackCount, "rejected book untouched");
        Check.Equal("rwom.golemancer.foundation", w.Profile.adeptTrialId, "foundation trial id");
        w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "full book transaction");
        Check.Equal(1, full.stackCount, "one full book paid");
        foreach (var def in new[] { TorannMagicDefOf.TM_Branding, TorannMagicDefOf.TM_SigilSurge })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "adept owns " + def.defName);
        Check.Equal(4, powers.Count(p => p.learned), "two adept families");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "cannot repeat adept");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal("TM_MasterTomeGolemancer", master.def.defName, "master tome mapping");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "mastery trial required");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal("rwom.golemancer.mastery", w.Profile.masterTrialId, "mastery trial id");
        w.Path.RecordCompletedTrial(w.Profile.masterTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "master transaction");
        Check.Equal(1, master.stackCount, "one master paid");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SigilDrain)).learned, "SigilDrain granted");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LivingWall)).learned, "LivingWall remains locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "cannot repeat mastery");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class trait inserted");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "points preserved");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "XP preserved");
        Check.Equal(true, ClassIdentity.IsGolemancer(w.Caster), "saved Golemancer identity");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var powers = w.Magic.MagicData.MagicPowersGolemancer;
        var originalOwners = powers.ToArray();
        var originalSkills = new[] { w.Magic.MagicData.MagicPowerSkill_Golemancy, w.Magic.MagicData.MagicPowerSkill_RuneCarving,
            w.Magic.MagicData.MagicPowerSkill_Branding, w.Magic.MagicData.MagicPowerSkill_SigilSurge,
            w.Magic.MagicData.MagicPowerSkill_SigilDrain, w.Magic.MagicData.MagicPowerSkill_LivingWall };
        var skillRecords = originalSkills.Select(list => list.ToArray()).ToArray();
        var inherited = w.Magic.MagicData.MagicPowersIF.First(); inherited.learned = true;
        inherited.level = 1; inherited.ticksUntilNextCast = 37;
        var drain = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SigilDrain));
        drain.learned = true; drain.ticksUntilNextCast = 911;
        var shared = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SigilSurge));
        shared.level = 0; shared.learned = true; shared.autocast = true; shared.ticksUntilNextCast = 823;
        
        w.Magic.MagicData.MagicPowerSkill_Golemancy.First().level = 4;
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.foundationBook)), "foundation with shared owner");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(true, ReferenceEquals(shared, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SigilSurge))), "native shared owner retained");
        Check.Equal(true, originalOwners.SequenceEqual(powers), "all native owner references retained");
        var restoredSkills = new[] { w.Magic.MagicData.MagicPowerSkill_Golemancy, w.Magic.MagicData.MagicPowerSkill_RuneCarving,
            w.Magic.MagicData.MagicPowerSkill_Branding, w.Magic.MagicData.MagicPowerSkill_SigilSurge,
            w.Magic.MagicData.MagicPowerSkill_SigilDrain, w.Magic.MagicData.MagicPowerSkill_LivingWall };
        for (int i = 0; i < originalSkills.Length; i++)
        {
            Check.Equal(true, ReferenceEquals(originalSkills[i], restoredSkills[i]), "native skill list retained: " + i);
            Check.Equal(true, skillRecords[i].SequenceEqual(restoredSkills[i]), "native skill objects retained: " + i);
        }
        Check.Equal(true, inherited.learned, "inherited other-class grant retained");
        Check.Equal(1, inherited.level, "inherited other-class rank retained");
        Check.Equal(37, inherited.ticksUntilNextCast, "inherited other-class cooldown retained");
        Check.Equal(TorannMagicDefOf.TM_SigilSurge, shared.abilityDef, "native SigilSurge owner retained");
        Check.Equal(TorannMagicDefOf.TM_SigilDrain, drain.abilityDef, "native SigilDrain owner retained");
        Check.Equal(911, drain.ticksUntilNextCast, "native SigilDrain cooldown retained");
        Check.Equal(true, shared.autocast, "autocast retained");
        Check.Equal(823, shared.ticksUntilNextCast, "cooldown retained");
        Check.Equal(4, w.Magic.MagicData.MagicPowerSkill_Golemancy.First().level, "skill investment retained");
        Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == shared.abilityDef), "shared runtime restored once");
        var locked = powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LivingWall));
        DefDatabase<ClassDisciplineDef>.Clear();
        Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, locked), "missing profile cannot unlock ultimate");
        Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, shared), "missing profile preserves learned shared owner");
    }
    static readonly string[] BrandNames = { "TM_AwarenessBrand", "TM_EmotionBrand", "TM_FitnessBrand", "TM_ProtectionBrand", "TM_SiphonBrand", "TM_VitalityBrand" };
    static void Children(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var owners = GolemancerBrands.Owners(w.Magic.MagicData)!;
        Check.Equal(6, owners.Count, "six exact native standalone brand owners");
        ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(0, owners.Count(p => p.learned), "foundation restoration grants no brands");
        var book = w.Book(w.Profile.adeptBook); w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        var unpaid = new UnpaidScroll { def = w.Profile.adeptBook, stackCount = 1 };
        w.Caster.inventory.innerContainer.TryAdd(unpaid, false);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, unpaid), "failed destruction does not acquire Branding");
        Check.Equal(false, w.Path.GolemancerBrandsGranted, "failed payment leaves latch clear");
        Check.Equal(0, owners.Count(p => p.learned), "failed payment grants no random brands");
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "paid adept acquires Branding");
        Check.Equal(true, w.Path.GolemancerBrandsGranted, "one-time grant latched");
        var chosen = owners.Where(p => p.learned).ToArray(); Check.Equal(2, chosen.Length, "two distinct native brand grants");
        ClassSpellAccess.RestoreLearned(w.Magic); ClassSpellAccess.RestoreLearned(w.Magic);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "repeat cannot pay or reroll");
        Check.Equal(true, chosen.SequenceEqual(owners.Where(p => p.learned)), "restoration preserves chosen brand identities");
        foreach (var name in new[] { "TM_Golemancy", "TM_RuneCarving", "TM_Branding" })
            Check.Equal(false, w.Magic.AbilityData.Powers.Any(a => a.Def.defName == name), "hidden parent stays hidden: " + name);
        foreach (var owner in chosen) Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == owner.abilityDef), "learned brand runtime once");
        Check.Equal(true, w.Magic.livingWall == null, "restoration never creates wall");
    }

    static void BrandBounds(RuntimeFixture f)
    {
        foreach (int existing in new[] { 5, 6 })
        {
            using var w = new Fixture(f); var owners = GolemancerBrands.Owners(w.Magic.MagicData)!;
            foreach (var p in owners.Take(existing)) p.learned = true;
            w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
            Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.adeptBook)), "bounded remaining grant");
            Check.Equal(6, owners.Count(p => p.learned), "remaining candidates granted without duplicate/reroll loop");
        }
        using (var w = new Fixture(f))
        {
            w.Path.RecordCompletedTrial(w.Profile.adeptTrialId); var book = w.Book(w.Profile.adeptBook);
            DefDatabase<TorannMagic.TM_CustomPowerDef>.Clear(); TM_Data.ResetDefinitionCaches();
            Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "missing stock definitions fail before payment");
            Check.Equal(2, book.stackCount, "missing definition preserves book");
            Check.Equal(false, w.Path.GolemancerBrandsGranted, "missing definition preserves latch");
        }
        using (var w = new Fixture(f))
        {
            w.Magic.MagicData.MagicPowersGolemancer.Single(p => p.abilityDef == TorannMagicDefOf.TM_Branding).learned = true;
            w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
            Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.adeptBook)), "inherited Branding still advances");
            Check.Equal(0, GolemancerBrands.Owners(w.Magic.MagicData)!.Count(p => p.learned), "inherited Branding never inferred as new grant");
        }
    }

    static void BrandSave(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Path.RecordCompletedTrial(w.Profile.adeptTrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, w.Book(w.Profile.adeptBook)), "paid brand acquisition before save");
        string file = System.IO.Path.Combine(f.WorkingDirectory, "golemancer-brand-latch.xml");
        DisciplineProgress path = w.Path;
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref path, "path"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        path = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref path, "path"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Check.Equal(true, path.GolemancerBrandsGranted, "paid grant latch survives native Scribe round trip");
        Check.Equal(InitiationStage.Adept, path.Stage, "stage and latch saved together");
    }

    sealed class LanguageScope : IDisposable
    {
        readonly LoadedLanguage previous = LanguageDatabase.activeLanguage, previousDefault = LanguageDatabase.defaultLanguage;
        public LanguageScope(RuntimeFixture f)
        {
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            const string key = "TM_ProgressionStageLocked";
            string value = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Languages/English/Keyed/TM_GrimoireProgression.xml")).Root!.Element(key)!.Value;
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement> { [key] = new() { key = key, value = value } };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        }
        public void Dispose() { LanguageDatabase.activeLanguage = previous; LanguageDatabase.defaultLanguage = previousDefault; }
    }

    static bool SkipMessage() => false;
    sealed class MessageScope : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.golemancer-scroll-message." + Guid.NewGuid();
        public MessageScope()
        {
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(GolemancerProgressionTests).GetMethod(nameof(SkipMessage), BindingFlags.NonPublic | BindingFlags.Static)!)!;
            var original = typeof(Messages).GetMethod("Message", new[] { typeof(string), typeof(LookTargets), typeof(MessageTypeDef), typeof(bool) })!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [original, hook, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }

    static void BrandScroll(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        using var language = new LanguageScope(f);
        using var messages = new MessageScope(); // Only the headless rejection popup; native admission/payment remain real.
        var owner = GolemancerBrands.Owners(w.Magic.MagicData)!.Single(p => p.abilityDef == TorannMagicDefOf.TM_ProtectionBrand);
        var classic = PawnWithComps(true, false);
        var classicBrand = GolemancerBrands.Owners(classic.magic.MagicData)!.First();
        Check.Equal(true, ClassSpellAccess.CanLearn(classic.pawn, classicBrand), "classic learning gate remains unchanged");
        var ability = TorannMagicDefOf.TM_ProtectionBrand; var old = ability.learnItem;
        var def = Fixture.BookDef("SpellOf_BrandProtection"); ability.learnItem = def;
        var addon = new MagicPower(new List<AbilityUser.AbilityDef> { ability }) { learned = true };
        w.Magic.MagicData.MagicPowersCustom.Add(addon);
        try
        {
            var scroll = new ThingWithComps { def = def, stackCount = 2 }; w.Caster.inventory.innerContainer.TryAdd(scroll);
            var effect = new CompUseEffect_LearnSpell { parent = scroll };
            effect.DoEffect(w.Caster);
            Check.Equal(false, owner.learned, "Foundation cannot learn a native brand via addon overlap");
            Check.Equal(2, scroll.stackCount, "Foundation rejection does not consume brand scroll");
            Set(w.Path, "stage", InitiationStage.Adept);
            w.Magic.MagicData.MagicPowersGolemancer.Single(p => p.abilityDef == TorannMagicDefOf.TM_Branding).learned = true;
            var unpaid = new UnpaidScroll { def = def, stackCount = 1 }; w.Caster.inventory.innerContainer.TryAdd(unpaid, false);
            new CompUseEffect_LearnSpell { parent = unpaid }.DoEffect(w.Caster);
            Check.Equal(false, owner.learned, "no-op scroll destruction cannot grant brand");
            effect.DoEffect(w.Caster); effect.DoEffect(w.Caster);
            Check.Equal(true, owner.learned, "paid scroll learns exact standalone owner");
            Check.Equal(1, scroll.stackCount, "brand scroll paid once");
            Check.Equal(true, addon.learned && w.Magic.MagicData.MagicPowersCustom.Contains(addon), "addon owner preserved separately");
            Check.Equal(false, w.Path.GolemancerBrandsGranted, "scroll cannot consume or invent stage grant latch");
            DefDatabase<ClassDisciplineDef>.Clear();
            var locked = GolemancerBrands.Owners(w.Magic.MagicData)!.Single(p => p.abilityDef == TorannMagicDefOf.TM_FitnessBrand);
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, locked), "missing profile fails closed for new brand learning");
            Check.Equal(true, ClassSpellAccess.CanLearn(w.Caster, owner), "saved brand remains learned without profile");
        }
        finally { ability.learnItem = old; }
    }

    sealed class UnpaidScroll : ThingWithComps
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) { }
    }

    static void LegacyAndScroll(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var book = w.Book(w.Profile.foundationBook);
        var trait = new Trait(TorannMagicDefOf.TM_Golemancer);
        w.Caster.story.traits.allTraits.Add(trait);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "legacy stock custom class is not auto-enrolled");
        Check.Equal(2, book.stackCount, "rejected legacy enrollment does not pay");
        w.Caster.story.traits.allTraits.Remove(trait);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "native foundation transaction");
        var owner = w.Magic.MagicData.MagicPowersGolemancer.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_LivingWall));
        var ability = TorannMagicDefOf.TM_LivingWall;
        var addonOwner = new MagicPower(new List<AbilityUser.AbilityDef> { ability }) { learned = true };
        w.Magic.MagicData.MagicPowersCustom.Add(addonOwner);
        var oldScroll = ability.learnItem;
        var scrollDef = Fixture.BookDef("SpellOf_LivingWall");
        ability.learnItem = scrollDef;
        try
        {
            Check.Equal(false, ClassSpellAccess.CanLearnFromScroll(w.Caster, owner, scrollDef), "ultimate scroll cannot bypass foundation");
            Set(w.Path, "stage", InitiationStage.Ascended);
            var unpaidDef = Fixture.BookDef("SpellOf_LivingWall");
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
            Check.Equal(true, owner.learned, "native ultimate scroll owns exact Golemancer power");
            Check.Equal(true, addonOwner.learned && w.Magic.MagicData.MagicPowersCustom.Contains(addonOwner), "overlapping addon owner preserved separately");
            Check.Equal(1, scroll.stackCount, "native scroll payment consumed once");
            Check.Equal(1, w.Magic.AbilityData.Powers.Count(a => a.Def == ability), "ultimate runtime restored once");
            Check.Equal(true, w.Magic.livingWall == null, "learning does not create spirit");
            Check.Equal(true, w.Magic.customClass == null, "scroll does not synthesize custom identity");
        }
        finally { ability.learnItem = oldScroll; }
    }

    static void KeyedCoverage(RuntimeFixture f)
    {
        foreach (string language in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
        {
            var xml = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Languages", language, "Keyed/TM_GrimoireProgression.xml"));
            foreach (string suffix in new[] { "", "FoundationTrial", "MasteryTrial", "AttunementHint", "AscensionAction", "AscensionNeedsOffering", "AscensionRequirements", "AttunementAction", "AttunementNeedsOffering" })
            {
                string key = "TM_ProgressionGolemancer" + suffix;
                var entries = xml.Root!.Elements(key).ToArray();
                Check.Equal(1, entries.Length, language + " has exactly one " + key);
                Check.Equal(false, string.IsNullOrWhiteSpace(entries.Single().Value), language + " translates " + key);
            }
        }
    }

    static void Costs(RuntimeFixture f)
    {
        using var w = new Fixture(f); Set(w.Caster, "mapIndexOrState", (sbyte)-1);
        var def = TorannMagicDefOf.TM_SigilSurge; var skill = w.Magic.MagicData.GetSkill_Efficiency(def);
        float old = def.manaCost, reduction = def.efficiencyReductionPercent;
        try { def.manaCost = .4f; def.efficiencyReductionPercent = .1f; skill.level = 0;
            float baseline = w.Magic.ActualManaCost(def); skill.level = 2;
            Check.Equal(baseline * .8f, w.Magic.ActualManaCost(def), "native Sigil efficiency retained"); }
        finally { def.manaCost = old; def.efficiencyReductionPercent = reduction; }
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.TM_Golemancer; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_Golemancer");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Golemancer matches native Golemancer offset: " + offset.stat.defName);
            Check.Equal(WorkTags.None, ClassPassiveEffects.DisabledWork(w.Caster), "native work restriction parity");
            Set(w.Path, "stage", (InitiationStage)0);
            Check.Equal(false, ClassIdentity.IsGolemancer(w.Caster), "invalid stage is not Golemancer identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "invalid stage grants no passive");
            Set(w.Path, "stage", InitiationStage.Foundation);
            Set(w.Path, "domain", DisciplineDomain.Might);
            Check.Equal(false, ClassIdentity.IsGolemancer(w.Caster), "wrong domain is not Golemancer identity");
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "wrong domain grants no passive");
            Set(w.Path, "domain", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "missing profile grants no passive");
            DefDatabase<ClassDisciplineDef>.Add(w.Profile);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual Golemancer trait prevents duplicate offset: " + offset.stat.defName);
            native.suppressedByTrait = true;
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "suppressed trait remains sole passive owner");
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

}