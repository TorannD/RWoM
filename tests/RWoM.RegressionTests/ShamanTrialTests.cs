using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class ShamanTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ShamanTrial.NativeHealingDeltaAndBound", Healing);
        yield return new("ShamanTrial.HealingSourceRecipientsAndDrift", HealingBoundaries);
        yield return new("ShamanTrial.BuildingSourceAndCounterNativeScribe", Save);
        yield return new("ShamanTrial.HexRequiresNewEffectAndOwnListDelta", Hex);
        yield return new("ShamanTrial.NativeTotemTickHealing", NativeTick);
        yield return new("ShamanTrial.NativeHexSuccessAndResistance", NativeHex);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly PawnCapacityDef[] capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly ThingDef oldTotem = TorannMagicDefOf.TM_HealingTotem;
        readonly HediffDef oldHex = TorannMagicDefOf.TM_HexHD;
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public Faction Enemy;
        public DisciplineProgress Path => Progression.State.Find("rwom.shaman");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            if (DefDatabase<PawnCapacityDef>.DefCount == 0) DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "ShamanTrialCapacity" });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) map.events = new MapEvents(map);
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World)); world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } }; Enemy = new Faction { def = new FactionDef() };
            world.factionManager.AllFactionsListForReading.Add(faction); world.factionManager.AllFactionsListForReading.Add(Enemy); Set(world.factionManager, "ofPlayer", faction);
            Set(faction, "relations", new List<FactionRelation> { new() { other = Enemy, kind = FactionRelationKind.Hostile } });
            Set(Enemy, "relations", new List<FactionRelation> { new() { other = faction, kind = FactionRelationKind.Hostile } });
            int id = 53100;
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.thingIDNumber = id++; pawn.guest = new Pawn_GuestTracker(pawn); pawn.kindDef = new PawnKindDef { defName = "ShamanTrialPawn" };
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "ShamanTrialBody", hitPoints = 100 } } };
                Set(pawn.def.race, "fleshType", new FleshTypeDef { defName = "ShamanTrialFlesh", isOrganic = true });
                Set(pawn, "positionInt", new IntVec3(3, 0, 3)); Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "factionInt", faction);
            }
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.shaman", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "ShamanTrialProfile", profileId = "rwom.shaman", domain = DisciplineDomain.Magic });
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = "TM_HealingTotem"; def.thingClass = typeof(Building_TMTotem_Healing); def.category = ThingCategory.Building; def.comps = new();
            TorannMagicDefOf.TM_HealingTotem = def; DefDatabase<ThingDef>.Add(def);
            TorannMagicDefOf.TM_HexHD = new HediffDef { defName = "TM_HexHD", maxSeverity = 1000, hediffClass = typeof(Hediff) }; DefDatabase<HediffDef>.Add(TorannMagicDefOf.TM_HexHD);
        }
        public MagicPower Owner(bool hex) => Magic.MagicData.MagicPowersShaman.Single(p => p.TMabilityDefs.Contains(hex ? TorannMagicDefOf.TM_Hex : TorannMagicDefOf.TM_Totems));
        public PawnAbility Ability(bool hex = false, bool owned = true)
        { Owner(hex).learned = true; var a = new PawnAbility(Magic); Set(a, "powerDef", hex ? Owner(true).abilityDef : TorannMagicDefOf.TM_SummonTotemHealing); if (owned) Magic.AbilityData.Powers.Add(a); return a; }
        public void Adept() { Path.RecordCompletedTrial(ShamanTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.shaman", InitiationStage.Foundation, ShamanTrials.FoundationTrialId); Set(Target, "factionInt", Enemy); }
        public Building_TMTotem_Healing Totem(bool owned = true)
        {
            var snapshot = ShamanTrials.CaptureSpawn(Ability(owned: owned)); var b = new Building_TMTotem_Healing { def = TorannMagicDefOf.TM_HealingTotem, thingIDNumber = 53200 };
            Set(b, "positionInt", new IntVec3(4, 0, 4)); Set(b, "mapIndexOrState", (sbyte)0); Set(b, "factionInt", Caster.Faction); Set(b, "comps", new List<ThingComp>());
            ShamanTrials.CompleteSpawn(snapshot, b); return b;
        }
        public Hediff_Injury Injury(float severity = 20)
        {
            var hd = new Hediff_Injury { pawn = Target, def = new HediffDef { defName = "ShamanTrialInjury", maxSeverity = 1000, injuryProps = new InjuryProps { bleedRate = 0 } }, Part = Target.RaceProps.body.corePart };
            Set(hd, "severityInt", severity); Target.health.hediffSet.hediffs.Add(hd); return hd;
        }
        public void Heal(Building_TMTotem_Healing b, float amount)
        { var before = ShamanTrials.CaptureHealing(b, Target); TM_Action.DoAction_HealPawn(null, Target, 1, amount); ShamanTrials.CompleteHealing(before); ShamanTrials.CompleteHealing(before); }
        public Hediff HexEffect()
        { var hd = new Hediff { pawn = Target, def = TorannMagicDefOf.TM_HexHD }; Set(hd, "severityInt", 1f); Target.health.hediffSet.hediffs.Add(hd); return hd; }
        public void Dispose()
        {
            Current.Game = previous; TorannMagicDefOf.TM_HealingTotem = oldTotem; TorannMagicDefOf.TM_HexHD = oldHex; Restore(profiles); Restore(things); Restore(hediffs); Restore(capacities);
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var d in defs) DefDatabase<T>.Add(d); }
    }
    static void Healing(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); var b = w.Totem(); var injury = w.Injury();
        var before = ShamanTrials.CaptureHealing(b, w.Target); ShamanTrials.CompleteHealing(before); Check.Equal(0f, w.Path.ShamanTrials.Healing, "unchanged injury grants nothing");
        w.Heal(b, 3); Check.Equal(17f, injury.Severity, "native healer=null reduced actual injury"); Check.Equal(3f, w.Path.ShamanTrials.Healing, "single observed result credited once");
        w.Heal(b, 6); Check.Equal(8f, w.Path.ShamanTrials.Healing, "actual healing bounded8"); Check.Equal(true, w.Path.CompletedTrials.Contains(ShamanTrials.FoundationTrialId), "threshold completes trial"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "ritual still required");
    }
    static void HealingBoundaries(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); var b = w.Totem(); w.Injury(100);
        w.Heal(w.Totem(false), 2);
        foreach (var status in new[] { GuestStatus.Prisoner, GuestStatus.Slave }) { w.Target.guest.guestStatusInt = status; w.Heal(b, 2); } w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "factionInt", w.Enemy); w.Heal(b, 2); Set(w.Target, "factionInt", w.Caster.Faction);
        Set(w.Target, "mapIndexOrState", (sbyte)1); w.Heal(b, 2); Set(w.Target, "mapIndexOrState", (sbyte)0);
        w.Owner(false).learned = false; w.Heal(b, 2); w.Owner(false).learned = true;
        var before = ShamanTrials.CaptureHealing(b, w.Target); TM_Action.DoAction_HealPawn(null, w.Target, 1, 2); w.Adept(); ShamanTrials.CompleteHealing(before);
        Check.Equal(0f, w.Path.ShamanTrials.Healing, "copy captive hostile othermap owner and stage boundaries exclude credit");
    }
    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new(); public Building_TMTotem_Healing totem = null!;
        public void ExposeData() { Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref totem, "totem"); }
    }
    static void Save(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(); var b = w.Totem(); w.Injury(); w.Heal(b, 3);
        Set(b,"nextSearch",12345); Set(b,"initialized",true);
        var envelope = new Envelope { state = w.Progression.State, totem = b }; var file = Path.Combine(f.WorkingDirectory, "shaman-totem.xml");
        try { Scribe.saver.InitSaving(file, "trial"); Scribe_Deep.Look(ref envelope, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try
        {
            Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data");
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(w.Caster); Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        Check.Same(w.Caster, loaded.totem.trialCaster, "native building Scribe resolves exact original caster");
        Check.Equal(12345, (int)typeof(Building_TMTotem_Healing).GetField("nextSearch",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(loaded.totem)!, "load preserves the scheduled pulse");
        Set(w.Progression, "state", loaded.state); Set(loaded.totem, "mapIndexOrState", (sbyte)0);
        Check.Equal(3f, w.Path.ShamanTrials.Healing, "healing counter survives save"); w.Heal(loaded.totem, 5); Check.Equal(8f, w.Path.ShamanTrials.Healing, "saved source continues actual healing");
    }
    static void Hex(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability(true);
        Check.Equal(true, ShamanTrials.CaptureHex(w.Ability(true, false), w.Target) == null, "copied runtime rejected");
        Check.Equal(false, ShamanTrials.CompleteHex(ShamanTrials.CaptureHex(ability, w.Target)), "resisted/no mutation rejected");
        var hd = w.HexEffect(); Check.Equal(true, ShamanTrials.CaptureHex(ability, w.Target) == null, "old/foreign Hex excluded"); w.Target.health.hediffSet.hediffs.Remove(hd);
        w.Magic.HexedPawns.Add(w.Target); Check.Equal(false, w.Magic.HexedPawns.Contains(w.Target), "native getter removes stale membership without HexHD");
        hd = w.HexEffect(); w.Magic.HexedPawns.Add(w.Target); Check.Equal(true, ShamanTrials.CaptureHex(ability, w.Target) == null, "existing own Hex and membership excluded"); w.Target.health.hediffSet.hediffs.Remove(hd); w.Magic.HexedPawns.Clear();
        var before = ShamanTrials.CaptureHex(ability, w.Target); hd = w.HexEffect(); Check.Equal(false, ShamanTrials.CompleteHex(before), "new effect without own list mutation excluded"); w.Target.health.hediffSet.hediffs.Remove(hd);
        before = ShamanTrials.CaptureHex(ability, w.Target); hd = w.HexEffect(); w.Magic.HexedPawns.Add(w.Target); w.Magic.AbilityData.Powers.Remove(ability);
        Check.Equal(false, ShamanTrials.CompleteHex(before), "runtime ownership drift excluded"); w.Magic.AbilityData.Powers.Add(ability); w.Target.health.hediffSet.hediffs.Remove(hd); w.Magic.HexedPawns.Clear();
        before = ShamanTrials.CaptureHex(ability, w.Target); w.HexEffect(); w.Magic.HexedPawns.Add(w.Target);
        Check.Equal(true, ShamanTrials.CompleteHex(before), "actual new Hex and new exact owner-list target qualify"); Check.Equal(false, ShamanTrials.CompleteHex(before), "single use");
    }
    static Pawn? nearby;
    static bool Nearby(ref Pawn __result) { __result = nearby!; return false; }
    static bool Skip() => false;
    static bool Position(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    static bool Resistance(ref float __result) { __result = 1; return false; }
    static bool PostCast(bool inResult, ref bool outResult) { outResult = inResult; return false; }
    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!; readonly string id = "rwom.shamantrial." + Guid.NewGuid(); readonly object harmony;
        public Hooks(bool tick = false, bool hex = false)
        {
            harmony = Activator.CreateInstance(type, id)!;
            Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(Skip));
            if (tick || hex)
            {
                Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(Position));
                foreach (var m in typeof(TM_MoteMaker).GetMethods().Where(m => m.Name == "ThrowGenericMote")) Prefix(m, nameof(Skip));
                foreach (var m in typeof(MoteMaker).GetMethods().Where(m => m.Name == "ThrowText")) Prefix(m, nameof(Skip));
            }
            if (tick) Prefix(typeof(TM_Calc).GetMethods().Single(m => m.Name == "FindNearbyInjuredPawn" && m.GetParameters()[0].ParameterType == typeof(IntVec3)), nameof(Nearby));
            if (hex)
            {
                foreach (var m in typeof(TM_Calc).GetMethods().Where(m => m.Name == "GetSpellSuccessChance")) Prefix(m, nameof(Resistance));
                Prefix(typeof(Verb_UseAbility).GetMethod("PostCastShot")!, nameof(PostCast));
            }
        }
        void Prefix(MethodInfo m, string name)
        { var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ShamanTrialTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!; type.GetMethod("Patch")!.Invoke(harmony, [m, hook, null, null, null]); }
        public void Dispose() { type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); nearby = null; }
    }
    static void NativeTick(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(tick: true); var b = w.Totem(); var injury = w.Injury(); nearby = w.Target;
        Set(b, "initialized", true); Set(b, "nextSearch", 0); var before = injury.Severity;
        typeof(Building_TMTotem_Healing).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(b, null);
        Check.Equal(true, injury.Severity < before, "native totem actually heals injury");
        Check.Equal(before - injury.Severity, w.Path.ShamanTrials.Healing, "observer records exact native delta with healer null");
    }
    static void NativeHex(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks(hex: true); using var language = new SpellItemEffectTests.LanguageScope(f.Repository); w.Adept(); var ability = w.Ability(true);
        LanguageDatabase.activeLanguage.keyedReplacements["TM_ResistedSpell"] = new LoadedLanguage.KeyedReplacement { key = "TM_ResistedSpell", value = "Resisted" };
        // An existing other target keeps dynamic-control creation outside this effect test; lifecycle tests own those controls.
        var existing = new Hediff { pawn = w.Caster, def = TorannMagicDefOf.TM_HexHD }; Set(existing, "severityInt", 1f); w.Caster.health.hediffSet.hediffs.Add(existing);
        w.Magic.HexedPawns.Add(w.Caster);
        var verb = new Verb_Hex { Ability = ability, verbProps = ability.Def.MainVerb }; Set(verb, "caster", w.Caster); Set(verb, "currentTarget", new LocalTargetInfo(w.Target));
        var cast = typeof(Verb_Hex).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!;
        int SeedFor(bool succeeds)
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                Rand.PushState(seed);
                try { if (Rand.Chance(.4f) == succeeds) return seed; }
                finally { Rand.PopState(); }
            }
            throw new InvalidOperationException("No deterministic Hex seed found");
        }
        Rand.PushState(SeedFor(false));
        try { cast.Invoke(verb, null); } finally { Rand.PopState(); }
        Check.Equal(false, w.Target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HexHD), "deterministic native resisted branch creates no Hex");
        Check.Equal(false, w.Magic.HexedPawns.Contains(w.Target), "native resisted branch adds no target");
        Check.Equal(false, w.Path.CompletedTrials.Contains(ShamanTrials.MasteryTrialId), "native resisted branch earns nothing");
        Rand.PushState(SeedFor(true));
        try { cast.Invoke(verb, null); } finally { Rand.PopState(); }
        Check.Equal(true, w.Target.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HexHD), "native successful branch created actual Hex");
        Check.Equal(true, w.Magic.HexedPawns.Contains(w.Target), "native successful branch added target to owner");
        Check.Equal(true, w.Path.CompletedTrials.Contains(ShamanTrials.MasteryTrialId), "native success observed");
    }
}
