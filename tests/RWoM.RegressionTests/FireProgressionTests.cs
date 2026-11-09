using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class FireProgressionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ShadowDiscipline.BookAndCrossing", Shadow);
        yield return new("ChaosDiscipline.NativeAscension", r => ClassAscensionJobTests.Work(r, chaos:true));
        yield return new("ChaosDiscipline.BooksDrawsAnchorAndRefund", Chaos);
        foreach (var test in SpellMasteryTests.Cases()) yield return test;
        foreach (bool fireFirst in new[] { true, false })
            yield return new("ElementalHybrid.NativeBooks." + (fireFirst ? "FireFirst" : "IceFirst"), r => Hybrid(r, fireFirst));
        yield return new("ElementalHybrid.AscendFire", r => ClassAscensionJobTests.Work(r, fire:true, hybrid:true));
        yield return new("ElementalHybrid.AscendIce", r => ClassAscensionJobTests.Work(r, ice:true, hybrid:true));
        yield return new("FireProgression.NativeBooksAndStageGates", Books);
        yield return new("FireProgression.ActualResultThresholdAndScribe", Damage);
        yield return new("FireProgression.HostileTargetsAndProjectileAttribution", Targets);
        yield return new("FireProgression.NativeDamageHookAttaches", Hook);
        yield return new("FireProgression.NativeAuraToggleRanksAndAuthoredUpkeep", Aura);
        yield return new("FireProgression.NativeFirePassiveOffsetsAndDeduplication", Passive);
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
        public DisciplineProgress Path => Progression.State.Find("rwom.fire");
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
            DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "FireBookDesignation" });
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
                pawn.kindDef = new PawnKindDef { defName = "FireTrialPawn" };
                Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Set(Caster, "factionInt", Friendly); Set(Target, "factionInt", Enemy);
            Caster.inventory = new Pawn_InventoryTracker(Caster);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage);
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.fire");
            Profile = new ClassDisciplineDef { defName = "FireProgression", profileId = "rwom.fire", domain = DisciplineDomain.Magic,
                legacyTrait = TorannMagicDefOf.InnerFire, magicPowerList = node.Element("magicPowerList")!.Value,
                foundationBook = BookDef(node.Element("foundationBook")!.Value), adeptBook = BookDef(node.Element("adeptBook")!.Value),
                adeptTrialId = node.Element("adeptTrialId")!.Value,
                masterBook = BookDef(node.Element("masterBook")!.Value), masterTrialId = node.Element("masterTrialId")!.Value,
                spells = node.Element("spells")!.Elements().Select(e => new DisciplineSpell {
                    ability = DefDatabase<TMAbilityDef>.GetNamed(e.Element("ability")!.Value), stage = Enum.Parse<InitiationStage>(e.Element("stage")!.Value) }).ToList() };
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(Profile);
            if (initiate) Check.Equal(true, ClassInitiation.CompleteBook(Caster, Book(Profile.foundationBook)), "native foundation transaction succeeds");
        }
        internal static ThingDef BookDef(string name)
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

    static void Shadow(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var node = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml"))
            .Root!.Elements().Single(e => (string?)e.Element("profileId") == "rwom.shadow");
        var spell = DefDatabase<TMAbilityDef>.GetNamed(node.Descendants("ability").Single().Value);
        var profile = new ClassDisciplineDef { defName = "TestShadow", profileId = "rwom.shadow", domain = DisciplineDomain.Magic,
            legacyTrait = new TraitDef { defName = "TM_TheShadow" }, magicPowerList = node.Element("magicPowerList")!.Value,
            foundationBook = Fixture.BookDef(node.Element("foundationBook")!.Value),
            spells = [new DisciplineSpell { ability = spell, stage = InitiationStage.Foundation }] };
        DefDatabase<ClassDisciplineDef>.Add(profile);
        var book = w.Book(profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "real foundation transaction");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "no second charge");
        var path = w.Progression.State.Find(profile.profileId);
        var owned = w.Magic.AbilityData.Powers.Single(a => a.Def == spell);
        var entry = typeof(ClassIdentity).Assembly.GetType("TorannMagic.Verb_ShadowWalk")!
            .GetMethod("CompleteCrossing", BindingFlags.NonPublic | BindingFlags.Static)!;
        var dest = new IntVec3(20, 0, 20);
        Set(w.Caster,"positionInt",dest); Set(w.Target,"positionInt",dest);
        bool Cross(AbilityUser.PawnAbility cast, int distance) => (bool)entry.Invoke(null,
            [cast, w.Target, w.Caster.Map, dest - new IntVec3(distance, 0, 0)])!;
        Check.Equal(false, Cross(new MagicAbility(w.Magic) { Def = spell }, 8), "copied cast excluded");
        Check.Equal(false, Cross(owned, 7), "short hop does not finish trial");
        Check.Equal(true, Cross(owned, 8), "eight cell crossing unlocks cloak");
        Check.Equal(true, Cross(owned, 1), "later short crossings grant cloak");
        Check.Equal(false, Cross(owned, 0), "no cloak for stationary cast");
        Check.Equal(InitiationStage.Adept, path.Stage, "finished once");
        Check.Equal(1, path.CompletedTrials.Count, "one trial recorded");
        var saved = w.Progression.State;
        var file = Path.Combine(f.WorkingDirectory, "shadow-crossing.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref saved, "progression"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        ClassProgressionState loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "progression"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Set(w.Progression, "state", loaded);
        Check.Equal(true, Cross(owned, 1), "reward survives save and load");
    }

    static void Chaos(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        var x = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Defs/CustomDefs/TM_ClassDisciplines.xml")).Root!.Elements()
            .Single(e => (string?)e.Element("profileId") == "rwom.chaos");
        ThingDef BookDef(string field) => Fixture.BookDef(x.Element(field)!.Value);
        var profile=new ClassDisciplineDef { defName="ChaosTestProfile",profileId="rwom.chaos",domain=DisciplineDomain.Magic,
            legacyTrait=TorannMagicDefOf.ChaosMage,magicPowerList=x.Element("magicPowerList")!.Value,
            foundationBook=BookDef("foundationBook"),adeptBook=BookDef("adeptBook"),masterBook=BookDef("masterBook"),
            adeptTrialId=x.Element("adeptTrialId")!.Value,masterTrialId=x.Element("masterTrialId")!.Value,
            spells=[new DisciplineSpell {stage=InitiationStage.Foundation,ability=TorannMagicDefOf.TM_ChaosTradition}] };
        DefDatabase<ClassDisciplineDef>.Add(profile);
        var data=w.Magic.MagicData;
        var pool=data.MagicPowersIF.Concat(data.MagicPowersHoF).Where(p => p.TMabilityDefs.Count>0).ToList();
        foreach(var power in pool) power.learned=false;
        var permanent=pool[0]; permanent.learned=true;
        Set(data,"allMagicPowersForChaosMageList",pool);
        Check.Equal(true,ClassInitiation.CompleteBook(w.Caster,w.Book(profile.foundationBook)),"torn chaos book grants foundation");
        var path=w.Progression.State.Find(profile.profileId);
        var type=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.ChaosDiscipline")!;
        var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
        bool Deal() => (bool)type.GetMethod("Deal",flags)!.Invoke(null,[w.Magic,path])!;
        Check.Equal(true,Deal(),"successful first draw");
        var ledger=path.Chaos;
        Check.Equal(2,ledger.Borrowed.Count,"foundation slots");
        var source=TM_ChaosProgression.ActivePowers(w.Magic);
        Check.Equal(false,source.Contains(permanent),"permanent grant not in refund pool");
        void Use(MagicPower power)
        {
            var a=new MagicAbility(w.Magic) { Def=power.abilityDef };
            w.Magic.AbilityData.Powers.Add(a);
            try { type.GetMethod("RecordUse",flags)!.Invoke(null,[a]); }
            finally { w.Magic.AbilityData.Powers.Remove(a); }
        }
        foreach(var a in source) Use(a);
        Check.Equal(true,ClassInitiation.CompleteBook(w.Caster,w.Book(profile.adeptBook)),"two distinct spells complete first trial");
        for(int i=0;i<2;i++){Check.Equal(true,Deal(),"new set"); Use(TM_ChaosProgression.ActivePowers(w.Magic).First());}
        Check.Equal(true,ClassInitiation.CompleteBook(w.Caster,w.Book(profile.masterBook)),"two distinct draws unlock master tome");
        var choose=ledger.GetType().GetMethod("SelectAnchor",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var kept=TM_ChaosProgression.ActivePowers(w.Magic).First();
        string id=SpellIds.Of(kept.TMabilityDefs[0]);
        Check.Equal(false,(bool)choose.Invoke(ledger,[id,path])!,"master cannot anchor before ascension");
        path.RecordCompletedTrial("rwom.chaos.ascension");
        w.Progression.State.TryAdvanceAfterTrial(profile.profileId,InitiationStage.Master,"rwom.chaos.ascension");
        Check.Equal(true,(bool)choose.Invoke(ledger,[id,path])!,"select active spell");
        kept.level=1; kept.costToLevel=2; int before=data.MagicAbilityPoints;
        var saved=ledger; var file=Path.Combine(f.WorkingDirectory,"chaos-anchor.xml");
        try { Scribe.saver.InitSaving(file,"regression");Scribe_Deep.Look(ref saved,"chaos");Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        saved=null!;
        try { Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref saved,"chaos");Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Check.Equal(id,saved.Anchor,"load preserves pending choice");
        Check.Equal(true,saved.Borrowed.SequenceEqual(ledger.Borrowed),"load preserves draw order and spells");
        Set(path,"chaos",saved);
        Check.Equal(true,Deal(),"ascended draw completes");
        Check.Equal(true,saved.Borrowed.Contains(id) && saved.Anchor==null,"one-use anchor retained exact spell and cleared");
        Check.Equal(before+2,data.MagicAbilityPoints,"retained spell also refunds full investment exactly once");
        Check.Equal(0,kept.level,"retained spell has fresh ranks");
        Check.Equal(true,permanent.learned,"independent permanent spell survives");
        Check.Equal(4,saved.Borrowed.Count,"master slots retained at ascension");
        Check.Equal(0,w.Caster.story.traits.allTraits.Count,"no old chaos trait");
        RuntimeLog.AssertClean();
    }
    static void Hybrid(RuntimeFixture f, bool fireFirst)
    {
        ClassDisciplineDef ice;
        using (var i = new IceProgressionTests.Fixture(f, false)) ice = i.Profile;
        using var w = new Fixture(f, false);
        DefDatabase<ClassDisciplineDef>.Add(ice);
        w.Magic.MagicData.MagicPowerSkill_Soothe.First().level = 2;
        var profiles = fireFirst ? new[] { w.Profile, ice } : new[] { ice, w.Profile };
        foreach (var profile in profiles)
        {
            var book = w.Book(profile.foundationBook);
            Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, book), "actual foundation");
            Check.Equal(1, book.stackCount, "one torn book per branch");
            Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, book), "duplicate foundation refused");
        }
        var state = w.Progression.State;
        foreach (var profile in profiles)
        {
            var path = state.Find(profile.profileId);
            var book = w.Book(profile.adeptBook);
            Check.Equal(false, ClassInitiation.CompleteBook(w.Caster,book), "other path cannot waive trial");
            path.RecordCompletedTrial(profile.adeptTrialId);
            Check.Equal(true, ClassInitiation.CompleteBook(w.Caster,book), "exact full tome advances chosen branch");
        }
        foreach (var profile in profiles)
        {
            var path = state.Find(profile.profileId);
            path.RecordCompletedTrial(profile.masterTrialId);
            Check.Equal(true, ClassInitiation.CompleteBook(w.Caster,w.Book(profile.masterBook)), "separate master tomes");
            Check.Equal(InitiationStage.Master, path.Stage, "chosen branch advanced");
        }
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_Soothe.First().level, "previously invested rank retained");
        Check.Equal(2, state.Disciplines.Count, "two paths only");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "no class traits granted");
    }

    static void Books(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        w.Magic.MagicData.MagicAbilityPoints = 8; w.Magic.MagicData.MagicUserXP = 43;
        Check.Equal("Torn_BookOfInnerFire", w.Profile.foundationBook.defName, "native torn fire book mapping");
        Check.Equal("BookOfInnerFire", w.Profile.adeptBook.defName, "native full fire book mapping");
        var foundation = w.Book(w.Profile.foundationBook);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, foundation), "foundation book initiates fire");
        Check.Equal(1, foundation.stackCount, "exactly one foundation book consumed");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, foundation), "foundation cannot repeat");
        w.Magic.InitializeSpell(); w.Magic.ResolveFactionChange();
        var powers = w.Magic.MagicData.MagicPowersIF;
        foreach (var def in new[] { TorannMagicDefOf.TM_Firebolt, TorannMagicDefOf.TM_RayofHope })
            Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(def)).learned, "foundation owns " + def.defName);
        Check.Equal(2, powers.Count(p => p.learned), "exactly two foundation spells");
        Check.Equal(2, w.Magic.AbilityData.Powers.Count, "restore adds no later abilities");
        foreach (var def in new[] { TorannMagicDefOf.TM_Fireclaw, TorannMagicDefOf.TM_Fireball, TorannMagicDefOf.TM_Firestorm })
            Check.Equal(false, ClassSpellAccess.CanLearn(w.Caster, powers.Single(p => p.TMabilityDefs.Contains(def))), "later spell locked: " + def.defName);
        var full = w.Book(w.Profile.adeptBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "full tome cannot bypass damage trial");
        Check.Equal(2, full.stackCount, "rejected tome untouched");
        Check.Equal(FireboltDamageTrial.TrialId, w.Profile.adeptTrialId, "XML maps actual damage trial");
        FireboltDamageTrial.Complete(FireboltDamageTrial.Capture(w.Caster, w.Target), 100);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, full), "completed damage trial permits full tome");
        Check.Equal(1, full.stackCount, "one full tome paid");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "full tome advances adept");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fireclaw)).learned, "adept grants Fireclaw");
        Check.Equal(3, powers.Count(p => p.learned), "only one additional family learned");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, full), "adept cannot repeat payment");
        Check.Equal(1, full.stackCount, "repeat spends nothing");
        Check.Equal(0, w.Caster.story.traits.allTraits.Count, "new progression never inserts InnerFire trait");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "books and trial preserve points");
        Check.Equal(43, w.Magic.MagicData.MagicUserXP, "books and trial preserve XP");
        var master = w.Book(w.Profile.masterBook);
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "master tome cannot bypass pair trial");
        Check.Equal(2, master.stackCount, "rejected master untouched");
        Check.Equal(FireclawTrial.TrialId, w.Profile.masterTrialId, "XML matches pair trial");
        w.Path.RecordCompletedTrial(FireclawTrial.TrialId);
        Check.Equal(true, ClassInitiation.CompleteBook(w.Caster, master), "pair trial permits mastery");
        Check.Equal(1, master.stackCount, "one master tome paid");
        Check.Equal(InitiationStage.Master, w.Path.Stage, "master stage");
        Check.Equal(true, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Fireball)).learned, "master learns Fireball");
        Check.Equal(false, powers.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Firestorm)).learned, "Firestorm stays locked");
        Check.Equal(false, ClassInitiation.CompleteBook(w.Caster, master), "master cannot repeat");
        Check.Equal(1, master.stackCount, "repeat does not spend");
        Check.Equal(8, w.Magic.MagicData.MagicAbilityPoints, "master preserves points");
    }

    static void Damage(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            FireboltDamageTrial.Complete(FireboltDamageTrial.Capture(w.Caster, w.Target), invalid);
        Check.Equal(0f, w.Path.FireboltDamage, "absorbed/invalid damage does not count");
        var shot = FireboltDamageTrial.Capture(w.Caster, w.Target);
        FireboltDamageTrial.Complete(shot, 38); FireboltDamageTrial.Complete(shot, 38);
        Check.Equal(38f, w.Path.FireboltDamage, "same concrete damage result counts once");
        var saved = w.Progression.State; var file = System.IO.Path.Combine(f.WorkingDirectory, "fire-damage-state.xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref saved, "progression"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        ClassProgressionState loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "progression"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Set(w.Progression, "state", loaded);
        Check.Equal(38f, w.Path.FireboltDamage, "native Scribe preserves nested partial damage counter");
        FireboltDamageTrial.Complete(FireboltDamageTrial.Capture(w.Caster, w.Target), 61.5f);
        Check.Equal(false, w.Path.CompletedTrials.Contains(FireboltDamageTrial.TrialId), "99.5 damage is insufficient");
        shot = FireboltDamageTrial.Capture(w.Caster, w.Target);
        Set(w.Target.health, "healthState", PawnHealthState.Dead);
        FireboltDamageTrial.Complete(shot, 20);
        Check.Equal(100f, w.Path.FireboltDamage, "lethal hit captured before death counts and caps at 100");
        Check.Equal(1, w.Path.CompletedTrials.Count(t => t == FireboltDamageTrial.TrialId), "trial completed once");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "damage alone never advances stage");
        Set(w.Target.health, "healthState", PawnHealthState.Mobile);
        Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) == null, "completed trial accepts no more snapshots");
    }

    static void Targets(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave })
        {
            w.Target.guest.guestStatusInt = status;
            Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) == null, status + " excluded");
        }
        w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "factionInt", w.Friendly);
        Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) == null, "allied damage excluded");
        var aggressive = new MentalStateDef { defName = "FireTrialAggro", category = MentalStateCategory.Aggro };
        Set(w.Target.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Berserk { pawn = w.Target, def = aggressive });
        Check.Equal(true, w.Target.HostileTo(w.Caster), "native berserk hostility is active");
        Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) == null, "friendly berserk remains excluded despite aggression");
        Set(w.Target, "factionInt", null);
        Set(w.Target.mindState.mentalStateHandler, "curStateInt", null);
        Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) == null, "peaceful factionless target excluded");
        Set(w.Target.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Manhunter { pawn = w.Target, def = aggressive });
        Check.Equal(true, w.Target.HostileTo(w.Caster), "native manhunter hostility is active");
        Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) != null, "factionless aggressive manhunter qualifies");
        Set(w.Target.mindState.mentalStateHandler, "curStateInt", null);
        Set(w.Target, "factionInt", w.Enemy);
        var damageDefs = typeof(MagicAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf", true)!;
        RuntimeLog.EnsureHeadlessDefOfInitialized(damageDefs);
        var field = damageDefs.GetField("Firebolt")!;
        var firebolt = new DamageDef { defName = "Firebolt" }; var old = field.GetValue(null);
        field.SetValue(null, firebolt);
        try
        {
            var projectile = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); projectile.defName = "Projectile_Firebolt";
            var info = new DamageInfo(firebolt, 999, instigator: w.Caster, weapon: projectile);
            FireboltDamageTrialPatch.Prefix(w.Target, info, out var shot);
            FireboltDamageTrialPatch.Postfix(new DamageWorker.DamageResult { totalDamageDealt = 7 }, shot);
            Check.Equal(7f, w.Path.FireboltDamage, "native patch credits actual damage result, not requested amount");
            FireboltDamageTrialPatch.Prefix(new Thing(), info, out shot);
            Check.Equal(true, shot == null, "buildings cannot earn damage trial");
            projectile.defName = "Projectile_Fireball";
            FireboltDamageTrialPatch.Prefix(w.Target, info, out shot);
            Check.Equal(true, shot == null, "other projectile cannot contribute");
            projectile.defName = "Projectile_Firebolt";
            w.Magic.MagicData.MagicPowersIF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Firebolt)).learned = false;
            FireboltDamageTrialPatch.Prefix(w.Target, info, out shot);
            Check.Equal(true, shot == null, "unlearned Firebolt cannot contribute");
        }
        finally { field.SetValue(null, old); }
    }

    static bool SkipPhysiology() => false;

    static void Aura(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "FireAuraCapacity" });
        w.Caster.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "FireAuraBody" } } };
        var oldAura = TorannMagicDefOf.TM_InnerFire_AuraHD;
        var aura = new HediffDef { defName = "TM_InnerFire_AuraHD", hediffClass = typeof(Hediff), initialSeverity = .01f, maxSeverity = 4f };
        TorannMagicDefOf.TM_InnerFire_AuraHD = aura;
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.fire-aura." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        var costs = new List<(TMAbilityDef def, float energy, float regen, float efficiency)>();
        try
        {
            // Keep real native hediff insertion/removal; only physiology lacks headless database support.
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(FireProgressionTests).GetMethod(nameof(SkipPhysiology), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange"), prefix, null, null, null]);
            var xml = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_FireMage.xml"));
            var power = w.Magic.MagicData.MagicPowersIF.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_RayofHope));
            foreach (int rank in new[] { 0, 3 })
            {
                power.level = rank;
                var abilityDef = (TMAbilityDef)power.abilityDef;
                var node = xml.Root!.Elements().Single(e => (string?)e.Element("defName") == abilityDef.defName);
                float Cost(string name) => float.Parse(node.Element(name)!.Value, System.Globalization.CultureInfo.InvariantCulture);
                Check.Equal(.4f, Cost("upkeepEnergyCost"), "authored aura energy upkeep: " + rank);
                Check.Equal(.3f, Cost("upkeepRegenCost"), "authored aura regen upkeep: " + rank);
                Check.Equal(.1f, Cost("upkeepEfficiencyPercent"), "authored aura efficiency per rank: " + rank);
                costs.Add((abilityDef, abilityDef.upkeepEnergyCost, abilityDef.upkeepRegenCost, abilityDef.upkeepEfficiencyPercent));
                abilityDef.upkeepEnergyCost = Cost("upkeepEnergyCost"); abilityDef.upkeepRegenCost = Cost("upkeepRegenCost"); abilityDef.upkeepEfficiencyPercent = Cost("upkeepEfficiencyPercent");
                w.Magic.maxMP = 1f;
                ClassSpellAccess.RestoreLearned(w.Magic);
                var ability = w.Magic.AbilityData.Powers.Single(a => a.Def == abilityDef);
                var verb = new Verb_ApplyAura { caster = w.Caster, Ability = ability };
                var cast = typeof(Verb_ApplyAura).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!;
                bool autocast = power.autocast;
                Check.Equal(true, (bool)cast.Invoke(verb, null)!, "native aura first toggle succeeds");
                var hediff = w.Caster.health.hediffSet.hediffs.Single(h => h.def == aura);
                Check.Equal(rank + .5f, hediff.Severity, "native aura severity matches owned rank");
                Check.Equal(!autocast, power.autocast, "native aura toggle updates owned power state");
                Check.Equal(true, (bool)cast.Invoke(verb, null)!, "native aura second toggle succeeds");
                Check.Equal(0, w.Caster.health.hediffSet.hediffs.Count(h => h.def == aura), "native second toggle removes the concrete aura");
                Check.Equal(autocast, power.autocast, "second toggle restores original autocast state");
            }
            Check.Equal(0, w.Caster.story.traits.allTraits.Count, "traitless Fire owner operates native aura");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); TorannMagicDefOf.TM_InnerFire_AuraHD = oldAura;
            foreach (var c in costs) { c.def.upkeepEnergyCost = c.energy; c.def.upkeepRegenCost = c.regen; c.def.upkeepEfficiencyPercent = c.efficiency; }
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var d in capacities) DefDatabase<PawnCapacityDef>.Add(d);
        }
    }

    static void Passive(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var trait = TorannMagicDefOf.InnerFire; var oldDegrees = trait.degreeDatas;
        try
        {
            var node = XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/TraitDefs/Traits.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "InnerFire");
            var offsets = node.Element("degreeDatas")!.Element("li")!.Element("statOffsets")!.Elements()
                .Select(e => new StatModifier { stat = new StatDef { defName = e.Name.LocalName }, value = float.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture) }).ToList();
            trait.degreeDatas = [new TraitDegreeData { degree = 0, statOffsets = offsets }];
            var native = new Trait(trait);
            foreach (var offset in offsets)
                Check.Equal(native.OffsetOfStat(offset.stat), ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "saved Fire matches native InnerFire offset: " + offset.stat.defName);
            w.Caster.story.traits.allTraits.Add(native);
            foreach (var offset in offsets)
                Check.Equal(0f, ClassPassiveEffects.StatOffset(w.Caster, offset.stat), "actual InnerFire trait prevents duplicate offset: " + offset.stat.defName);
        }
        finally { trait.degreeDatas = oldDegrees; }
    }

    static void Hook(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var damageDefs = typeof(MagicAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf", true)!;
        RuntimeLog.EnsureHeadlessDefOfInitialized(damageDefs);
        var field = damageDefs.GetField("Firebolt")!; var old = field.GetValue(null);
        var firebolt = new DamageDef { defName = "Firebolt" }; field.SetValue(null, firebolt);
        var native = typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!;
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.fire-trial." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, [typeof(FireboltDamageTrialPatch)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            var patched = (IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!;
            Check.Equal(true, patched.Contains(native), "actual native damage prefix/postfix compile and attach");
            var projectile = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); projectile.defName = "Projectile_Firebolt";
            Check.Equal(true, FireboltDamageTrial.Capture(w.Caster, w.Target) != null, "native invocation has an eligible trial owner");
            var result = w.Target.TakeDamage(new DamageInfo(firebolt, 0, instigator: w.Caster, weapon: projectile));
            Check.Equal(0f, result.totalDamageDealt, "unmodified native TakeDamage returns zero actual damage");
            Check.Equal(0f, w.Path.FireboltDamage, "attached postfix does not reward native zero-damage result");
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); field.SetValue(null, old); }
    }
}
