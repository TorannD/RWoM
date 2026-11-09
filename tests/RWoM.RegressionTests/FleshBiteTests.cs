using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.FleshDevelopment;
using Verse;

namespace RWoM.RegressionTests;

internal static class FleshBiteTests
{
    public static void Initialize()
    {
        foreach (var type in new[] { typeof(DamageDefOf), typeof(ImplementOwnerTypeDefOf) })
        { RuntimeHelpers.RunClassConstructor(type.TypeHandle); RuntimeLog.AcceptHeadlessDefOfInitialization(type.Name); }
    }
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshBite.NewAttackPipelineExists", _ => Check.NotNull(typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.FleshBiteAttacks"), "actual bite attack conversion, not just a rare extra damage packet"));
        yield return new("FleshBite.RegisteredShotConvertsDamageAndVanillaLogsRepeatedly", _ => World((actor, victim, verb, patches) =>
        {
            var p = FleshRepository.Ensure(actor); p.FeastUntil = 1000; p.Thirst = 10;
            var original = verb.maneuver; var props = verb.verbProps; var tool = verb.tool;
            for (int i = 0; i < 2; i++)
            {
                packets.Clear(); Run(verb);
                Check.Equal(1, packets.Count, "Feast replaces bonus weapon packets with one jaw attack");
                Check.Same(DamageDefOf.Bite, packets[0].Def, "every Feast attack uses actual Bite damage");
                Check.Same(biteManeuver.combatLogRulesHit, lastLog!.RuleDef, "vanilla bite combat-log grammar");
                Check.Same(original, verb.maneuver, "maneuver restored after each shot");
                Check.Same(props, verb.verbProps, "shared verb props never replaced or mutated");
                Check.Same(tool, verb.tool, "shared tool never mutated");
            }
            p.FeastUntil = 0;
            Rand.PushState(Seed(false));
            try { packets.Clear(); Run(verb); } finally { Rand.PopState(); }
            Check.Same(DamageDefOf.Blunt, packets[0].Def, "normal attack restored after Feast when critical does not roll");
            Check.Equal(2, packets.Count, "ordinary attack retains original bonus packet");
            Check.Equal(original.combatLogRulesHit.defName, lastLog!.RuleDef?.defName, "normal combat log restored");
        }));
        yield return new("FleshBite.CriticalAndNativeBitesMissesAndScopeCleanup", _ => World((actor, victim, verb, patches) =>
        {
            positiveDamage = true;
            FleshRepository.Ensure(actor).Thirst = 1;
            Rand.PushState(Seed(true));
            try { packets.Clear(); Run(verb); } finally { Rand.PopState(); }
            Check.Equal(1, packets.Count, "critical replaces packet sequence");
            Check.Same(DamageDefOf.Bite, packets[0].Def, "outside Feast critical is actual Bite");
            Check.Equal(true, packets[0].Amount >= 12f && packets[0].Amount <= 18f, "critical multiplies ordinary randomized 10 damage by 1.5");
            FleshStatTests.Near(.01f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "critical bite applies one dose");
            Check.Equal(0, severRolls, "outside critical has no extra sever roll");
            miss = true; FleshRepository.TryGet(actor).FeastUntil = 1000;
            packets.Clear(); Run(verb);
            Check.Equal(0, packets.Count, "miss inflicts no damage or conditions");
            FleshStatTests.Near(.01f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "miss adds no poison");
            Check.Same(biteManeuver.combatLogRulesMiss, lastLog!.RuleDef, "miss still correctly describes attempted bite");
            miss = false; throwShot = true;
            var maneuver = verb.maneuver;
            try { Run(verb); throw new Exception("Expected fixture shot exception"); } catch (TargetInvocationException) { }
            finally { throwShot = false; }
            Check.Same(maneuver, verb.maneuver, "exception restores per-instance maneuver");
            Check.Equal<FleshBiteAttacks.Attack?>(null, FleshBiteAttacks.Current, "exception clears attack scope");
            FleshRepository.TryGet(actor).FeastUntil = 0; verb.verbProps.meleeDamageDef = DamageDefOf.Bite;
            Rand.PushState(Seed(false));
            try { packets.Clear(); Run(verb); } finally { Rand.PopState(); }
            Check.Same(DamageDefOf.Bite, packets[0].Def, "native undead bite remains a bite without critical roll");
            FleshStatTests.Near(.02f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "native bite plus bonus packet still adds only one dose");
        }));
        yield return new("FleshBite.PositiveOnlyToxicityResistanceAndRealPartInfection", _ => World((actor, victim, verb, patches) =>
        {
            var injury = new Hediff_Injury { pawn = victim, Part = victim.RaceProps.body.corePart, def = new HediffDef { defName = "TestBiteInjury" } };
            injury.Severity = 5;
            victim.health.hediffSet.hediffs.Add(injury);
            var result = new DamageWorker.DamageResult { totalDamageDealt = 5, hediffs = [injury], parts = [injury.Part] };
            FleshBiteAttacks.ApplyConditions(actor, victim, new DamageWorker.DamageResult(), true);
            Check.Equal(false, victim.health.hediffSet.HasHediff(HediffDefOf.ToxicBuildup), "absorbed bite adds no toxin");
            FleshBiteAttacks.ApplyConditions(actor, victim, result, false);
            FleshStatTests.Near(.01f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "small toxin dose per successful bite");
            StatDefOf.ToxicResistance.defaultBaseValue = 1;
            FleshBiteAttacks.ApplyConditions(actor, victim, result, false);
            FleshStatTests.Near(.01f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "full toxic resistance blocks added dose");
            StatDefOf.ToxicResistance.defaultBaseValue = .5f;
            Check.Equal(true, FleshBiteAttacks.Infectable(victim, injury.Part), "fixture part is infectable");
            Check.Equal(true, injury.Severity > 0 && victim.health.hediffSet.hediffs.Contains(injury), "fixture retains actual injury");
            Check.Equal(true, victim.health.hediffSet.GetNotMissingParts().Contains(injury.Part), "fixture anatomical tree retains part");
            forceInfection = true;
            Rand.PushState(Seed(true, .3f));
            try { FleshBiteAttacks.ApplyConditions(actor, victim, result, true); } finally { Rand.PopState(); }
            FleshStatTests.Near(.015f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "partial resistance scales dose");
            var infection = victim.health.hediffSet.hediffs.Single(h => h.def == HediffDefOf.WoundInfection);
            Check.Same(injury.Part, infection.Part, "infection applied to actual surviving injured part");
            Rand.PushState(Seed(true, .3f));
            try { FleshBiteAttacks.ApplyConditions(actor, victim, result, true); } finally { Rand.PopState(); }
            Check.Equal(1, victim.health.hediffSet.hediffs.Count(h => h.def == HediffDefOf.WoundInfection), "no duplicate infection on same part");
            victim.health.hediffSet.hediffs.Remove(infection);
            victim.health.hediffSet.hediffs.Remove(injury);
            Rand.PushState(Seed(true, .3f));
            try { FleshBiteAttacks.ApplyConditions(actor, victim, result, true); } finally { Rand.PopState(); }
            Check.Equal(false, victim.health.hediffSet.HasHediff(HediffDefOf.WoundInfection), "removed/severed wound cannot be infected");
        }));
        yield return new("FleshBite.SuccessfulAttackProcsOnceAndSeverResultStaysInLogResult", _ => World((actor, victim, verb, patches) =>
        {
            positiveDamage = true; forceSever = false; forceInfection = true;
            var p = FleshRepository.Ensure(actor); p.FeastUntil = 1000; p.Thirst = 10;
            Run(verb); Run(verb);
            FleshStatTests.Near(.02f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "repeated successful attacks apply one dose each");
            Check.Equal(2, severRolls, "one 20% sever roll per successful attack, no one-shot latch");
            Check.Equal(1, infectionRolls, "already infected part does not roll for duplicate infection");
            Check.Equal(1, victim.health.hediffSet.hediffs.Count(h => h.def == HediffDefOf.WoundInfection), "successful bite infects once");
            var child = new BodyPartRecord { parent = victim.RaceProps.body.corePart, def = new BodyPartDef { defName = "TestFinger", hitPoints = 10 }, coverage = .2f, coverageAbs = .2f };
            victim.RaceProps.body.corePart.parts.Add(child); victim.RaceProps.body.AllParts.Add(child);
            forceSever = true; packets.Clear(); Run(verb);
            Check.Equal(2, packets.Count, "successful sever adds exactly one secondary bite packet");
            FleshStatTests.Near(.03f, victim.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup).Severity, "sever packet does not add a second toxin dose");
            Check.Equal(true, lastResult!.parts.Contains(child), "combined result supplied to vanilla log includes severed part");
            Check.Equal(true, lastResult.hediffs.Any(h => h is Hediff_MissingPart && h.Part == child), "severed-part injury is associated with same log result");
            Check.Equal(false, victim.health.hediffSet.hediffs.Any(h => h.def == HediffDefOf.WoundInfection && h.Part == child), "no infection on severed part");
        }));
        yield return new("FleshBite.MechanicalTargetsStillReceiveBiteButNoBiologicalEffects", _ => World((actor, victim, verb, patches) =>
        {
            positiveDamage = true; forceSever = true; forceInfection = true;
            FleshRepository.Ensure(actor).FeastUntil = 1000;
            FleshStatTests.Set(victim.def.race, "fleshType", new FleshTypeDef { isOrganic = false });
            packets.Clear(); Run(verb);
            Check.Same(DamageDefOf.Bite, packets.Single().Def, "Feast physical attacks remain bites against machines");
            Check.Equal(false, victim.health.hediffSet.HasHediff(HediffDefOf.ToxicBuildup), "mechanical victim immune to toxin");
            Check.Equal(0, severRolls, "mechanical victim never rolled for eating parts");
            Check.Equal(0, infectionRolls, "mechanical victim never rolled for infection");
        }));
        yield return new("FleshBite.AttributionNestedScopesAndDiseaseExclusions", _ => World((actor, victim, verb, patches) =>
        {
            var source = new DamageInfo(DamageDefOf.Blunt, 8, .2f, 90, actor, weapon: victim.def, instigatorGuilty: false);
            var converted = FleshBiteAttacks.ConvertedDamage([source], new FleshBiteAttacks.Attack { Converted = true, Attacker = actor }).Single();
            Check.Equal(false, converted.InstigatorGuilty, "converted attack preserves drafted guilt attribution");
            Check.Same(actor.def, converted.Weapon, "bite damage attributes jaw, not held weapon");
            FleshRepository.Ensure(actor).FeastUntil = 1000;
            var original = verb.maneuver;
            FleshBiteAttacks.Prefix(verb, out var outer); FleshBiteAttacks.Prefix(verb, out var inner);
            FleshBiteAttacks.Finalizer(null!, inner); Check.Same(outer, FleshBiteAttacks.Current, "nested attack restores outer context");
            FleshBiteAttacks.Finalizer(null!, outer); Check.Same(original, verb.maneuver, "outer cleanup restores original maneuver");
            var part = victim.RaceProps.body.corePart;
            diseaseFactor = 0;
            Check.Equal(false, FleshBiteAttacks.Infectable(victim, part), "vanilla disease immunity gate respected");
            diseaseFactor = 1;
            victim.health.hediffSet.hediffs.Add(new Hediff_AddedPart { pawn = victim, Part = part, def = new HediffDef { addedPartProps = new() } });
            Check.Equal(false, FleshBiteAttacks.Infectable(victim, part), "artificial part cannot be infected");
            actor.health.hediffSet.hediffs.Clear(); positiveDamage = true; packets.Clear(); Run(verb);
            Check.Same(DamageDefOf.Blunt, packets[0].Def, "ordinary non-undead actor is not converted");
            Check.Equal(false, victim.health.hediffSet.HasHediff(HediffDefOf.ToxicBuildup), "ordinary actor adds no undead toxin");
        }));
    }

    static readonly MethodInfo shot = typeof(Verb_MeleeAttack).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly MethodInfo damage = typeof(Verb_MeleeAttackDamage).GetMethod("ApplyMeleeDamageToTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly List<DamageInfo> packets = [];
    static BattleLogEntry_MeleeCombat? lastLog;
    static DamageWorker.DamageResult? lastResult;
    static ManeuverDef biteManeuver = null!;
    static bool miss, throwShot;
    static Pawn? actorFixture, victimFixture;
    static void Run(Verb_MeleeAttackDamage verb)
    {
        phase = "before fixture shot boundary";
        try { shot.Invoke(verb, null); }
        catch (TargetInvocationException) { Console.WriteLine("SHOT PHASE: " + phase); throw; }
    }
    static string phase = "";
    // Only replace animation/hit-roll/world side effects. Execute actual log + actual damage iterator/dispatch with production Harmony hooks.
    static bool ShotBoundary(Verb_MeleeAttack __instance, ref bool __result)
    {
        phase = "creating actual combat log";
        if (throwShot) throw new InvalidOperationException("fixture shot failure");
        lastLog = __instance.CreateCombatLog(m => miss ? m.combatLogRulesMiss : m.combatLogRulesHit, !miss);
        phase = "actual damage iterator";
        if (!miss) lastResult = (DamageWorker.DamageResult)damage.Invoke(__instance, [__instance.CurrentTarget])!;
        __result = !miss; return false;
    }
    static bool BaseDamage(ref float __result) { __result = 10; return false; }
    static bool Armor(ref float __result) { __result = 0; return false; }
    static bool DamageBoundary(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
    {
        packets.Add(dinfo); __result = new DamageWorker.DamageResult { totalDamageDealt = positiveDamage ? 5 : 0 };
        if (positiveDamage && __instance is Pawn pawn)
        {
            var part = dinfo.HitPart ?? pawn.RaceProps.body.corePart;
            Hediff injury = dinfo.HitPart != null ? new Hediff_MissingPart() : new Hediff_Injury();
            injury.pawn = pawn; injury.Part = part; injury.def = new HediffDef { defName = "TestDamage" }; injury.Severity = 5;
            pawn.health.hediffSet.hediffs.Add(injury);
            __result.AddPart(pawn, part); __result.AddHediff(injury);
        }
        return false;
    }
    static bool HealthBoundary(Pawn ___pawn) => ___pawn != actorFixture && ___pawn != victimFixture;
    static float diseaseFactor = 1;
    static bool DiseaseBoundary(ref float __result) { __result = diseaseFactor; return false; }
    static bool forceInfection;
    static bool positiveDamage;
    static bool? forceSever;
    static int severRolls, infectionRolls;
    static bool ChanceBoundary(float chance, ref bool __result)
    {
        if (Math.Abs(chance - .2f) < .00001f) { severRolls++; if (forceSever.HasValue) { __result = forceSever.Value; return false; } }
        if (Math.Abs(chance - .3f) < .00001f) { infectionRolls++; if (forceInfection) { __result = true; return false; } }
        return true;
    }
    static bool DrawPosition(ref UnityEngine.Vector3 __result) { __result = UnityEngine.Vector3.zero; return false; }
    static int Seed(bool succeeds, float chance = .1f)
    {
        for (int seed = 1; seed < 1000; seed++)
        { Rand.PushState(seed); bool roll; try { roll = Rand.Chance(chance); } finally { Rand.PopState(); } if (roll == succeeds) return seed; }
        throw new Exception("Could not find deterministic seed");
    }
    static void World(Action<Pawn,Pawn,Verb_MeleeAttackDamage,Hooks> body) => FleshStatTests.InGame(() =>
    {
        FleshStatTests.Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
        FleshStatTests.Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
        FleshStatTests.Set(Current.Game, "battleLog", new BattleLog());
        Find.TickManager.gameStartAbsTick = 100;
        var originalToxin = HediffDefOf.ToxicBuildup; var originalInfection = HediffDefOf.WoundInfection; var originalResistance = StatDefOf.ToxicResistance;
        var originalBite = DamageDefOf.Bite; var originalBlunt = DamageDefOf.Blunt;
        var originalMeat = StatDefOf.MeatAmount; StatDefOf.MeatAmount = FleshStatTests.Stat("FleshTestMeat", 50);
        DamageDefOf.Bite = new DamageDef { defName = "Bite" }; DamageDefOf.Blunt = new DamageDef { defName = "Blunt" };
        HediffDefOf.ToxicBuildup = new HediffDef { defName = "FleshTestToxin", hediffClass = typeof(Hediff), initialSeverity = 0 };
        HediffDefOf.WoundInfection = new HediffDef { defName = "FleshTestInfection", hediffClass = typeof(Hediff), initialSeverity = .001f };
        StatDefOf.ToxicResistance = FleshStatTests.Stat("FleshTestResistance", 0);
        using var patches = new Hooks();
        try
        {
            var actor = MakePawn(); var victim = MakePawn(); actorFixture = actor; victimFixture = victim;
            FleshRepository.Ensure(actor);
            var original = new ManeuverDef { defName = "FleshTestPunch", combatLogRulesHit = new RulePackDef { defName = "FleshTestPunchHit" }, combatLogRulesMiss = new RulePackDef { defName = "FleshTestPunchMiss" }, logEntryDef = new LogEntryDef() };
            biteManeuver = DefDatabase<ManeuverDef>.GetNamedSilentFail("Bite")!;
            if (biteManeuver == null) { biteManeuver = new ManeuverDef { defName = "Bite", combatLogRulesHit = new RulePackDef { defName = "Maneuver_Bite_MeleeHit" }, combatLogRulesMiss = new RulePackDef { defName = "Maneuver_Bite_MeleeMiss" }, logEntryDef = new LogEntryDef() }; DefDatabase<ManeuverDef>.Add(biteManeuver); }
            if (DefDatabase<ImplementOwnerTypeDef>.GetNamedSilentFail("Bodypart") == null) DefDatabase<ImplementOwnerTypeDef>.Add(new ImplementOwnerTypeDef { defName = "Bodypart" });
            var verb = new Verb_MeleeAttackDamage { verbProps = new VerbProperties { meleeDamageDef = DamageDefOf.Blunt, meleeDamageBaseAmount = 10, verbClass = typeof(Verb_MeleeAttackDamage) }, maneuver = original,
                tool = new Tool { power = 10, extraMeleeDamages = [new ExtraDamage { def = DamageDefOf.Blunt, amount = 2, chance = 1, armorPenetration = 0 }] }, verbTracker = new VerbTracker(actor) };
            FleshStatTests.Set(verb, "caster", actor); FleshStatTests.Set(verb, "currentTarget", new LocalTargetInfo(victim));
            patches.Add(typeof(FleshBiteAttacks)); patches.Add(typeof(FleshBiteAttacks.DamagePatch)); patches.Add(typeof(FleshBiteAttacks.LogPatch));
            patches.Add(typeof(FleshCombatPatches)); patches.Add(typeof(FleshDamageResultPatch));
            patches.Prefix(shot, nameof(ShotBoundary), -1000);
            patches.Prefix(typeof(VerbProperties).GetMethod("AdjustedMeleeDamageAmount", [typeof(Verb), typeof(Pawn)])!, nameof(BaseDamage));
            patches.Prefix(typeof(VerbProperties).GetMethod("AdjustedArmorPenetration", [typeof(Verb), typeof(Pawn)])!, nameof(Armor));
            patches.Prefix(typeof(Thing).GetMethod("TakeDamage")!, nameof(DamageBoundary));
            patches.Prefix(typeof(HediffSet).GetMethod("DirtyCache")!, nameof(HealthBoundary));
            patches.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(HealthBoundary));
            patches.Prefix(typeof(ImmunityHandler).GetMethod("DiseaseContractChanceFactor", [typeof(HediffDef), typeof(BodyPartRecord)])!, nameof(DiseaseBoundary));
            patches.Prefix(typeof(Rand).GetMethod("Chance")!, nameof(ChanceBoundary));
            patches.Prefix(typeof(Pawn).GetProperty("DrawPos")!.GetMethod!, nameof(DrawPosition));
            body(actor, victim, verb, patches);
        }
        finally
        { actorFixture = victimFixture = null; miss = throwShot = forceInfection = positiveDamage = false; forceSever = null; diseaseFactor = 1; severRolls = infectionRolls = 0; FleshBiteAttacks.Current = null; HediffDefOf.ToxicBuildup = originalToxin; HediffDefOf.WoundInfection = originalInfection; StatDefOf.ToxicResistance = originalResistance; DamageDefOf.Bite = originalBite; DamageDefOf.Blunt = originalBlunt; StatDefOf.MeatAmount = originalMeat; }
    });
    static Pawn MakePawn()
    {
        var pawn = FleshStatTests.Pawn();
        // Use the native getter: a late getter patch can be bypassed by callers
        // already compiled during earlier tests in the same process.
        pawn.def.race.baseHealthScale = 1f;
        pawn.def.race.lifeStageAges = [new LifeStageAge { minAge = 13, def = new LifeStageDef { developmentalStage = DevelopmentalStage.Adult, healthScaleFactor = 1f } }];
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        FleshStatTests.Set(pawn.ageTracker, "ageBiologicalTicksInt", 25L * 3600000L);
        FleshStatTests.Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
        Check.Equal(1f, pawn.HealthScale, "bite fixture uses native adult health scale");
        pawn.def.race.meatDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        FleshStatTests.Set(pawn.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
        pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "TestFlesh", hitPoints = 50 } } };
        pawn.def.race.body.AllParts.Add(pawn.def.race.body.corePart);
        pawn.kindDef = new PawnKindDef { race = pawn.def }; pawn.stances = new Pawn_StanceTracker(pawn);
        pawn.records = (Pawn_RecordsTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_RecordsTracker)); FleshStatTests.Set(pawn.records, "pawn", pawn);
        pawn.drafter = new Pawn_DraftController(pawn);
        pawn.mindState = (Verse.AI.Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Verse.AI.Pawn_MindState));
        pawn.mindState.mentalStateHandler = new Verse.AI.MentalStateHandler(pawn);
        FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)0);
        return pawn;
    }
    internal sealed class Hooks : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly Type type; readonly object instance; readonly string id = "rwom.flesh-bite.test." + Guid.NewGuid();
        public Hooks() { type = assembly.GetType("HarmonyLib.Harmony")!; instance = Activator.CreateInstance(type, id)!; }
        public void Add(Type patch) { var processor = type.GetMethod("CreateClassProcessor")!.Invoke(instance, [patch])!; processor.GetType().GetMethod("Patch")!.Invoke(processor, null); }
        public void Prefix(MethodBase original, string name, int priority = 400)
        {
            var method = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(FleshBiteTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            method.GetType().GetField("priority")!.SetValue(method, priority);
            type.GetMethod("Patch")!.Invoke(instance, [original, method, null, null, null]);
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(instance, [id]);
    }
}
