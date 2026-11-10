using TorannMagic.FleshDevelopment;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using RimWorld;
using Verse;
using Verse.AI;
namespace RWoM.RegressionTests;
internal static class FleshCastingTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshCasting.DelayedJobExists", _ =>
            Check.NotNull(typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.JobDriver_FleshCast"), "Buffs and Feast use an interruptible preparation job"));
        foreach (var skill in new[] { FleshSkill.Empower, FleshSkill.Harden, FleshSkill.Feast })
        {
            var selected = skill;
            yield return new("FleshCasting.UIStartsDelayed." + skill, f => InWorld(f, pawn =>
            {
                using var scope = new Patches("start");
                scope.Prefix(typeof(Pawn_JobTracker).GetMethod("TryTakeOrderedJob")!, nameof(CaptureJob));
                ordered = null; orders = 0; var p = FleshRepository.TryGet(pawn); p.Thirst = 1;
                FleshPresentation.Activate(pawn, selected);
                Check.NotNull(ordered, "real UI requests a casting job");
                Check.Equal("TM_FleshCast", ordered!.def.defName, "self preparation job");
                Check.Same(pawn, ordered.targetA.Thing, "progress target is caster");
                Check.Equal((int)selected, ordered.count, "stable skill stored in vanilla saved job field");
                Check.Equal(1f, p.Thirst, "request incurs no thirst cost");
                Check.Equal(0, p.GetReadyTick(selected), "request starts no cooldown");
                Check.Equal(0, p.EmpowerUntil + p.HardenUntil + p.FeastUntil, "no instant active effect");
                FleshPresentation.Activate(pawn, selected);
                Check.Equal(1, orders, "repeat click cannot reset same preparation");
            }));
            yield return new("FleshCasting.EngineDelayCompletesOnce." + skill, f => InWorld(f, pawn =>
            {
                var driver = Driver(pawn, selected);
                using var scope = EndScope();
                driver.ReadyForNextToil();
                int duration = selected == FleshSkill.Feast ? 300 : 120;
                Check.Equal(duration, driver.ticksLeftThisToil, "real engine starts correct delay");
                Check.Equal(false, pawn.pather.Moving, "wait stops movement");
                var p = FleshRepository.TryGet(pawn); p.Thirst = 1;
                for (int i = 1; i < duration; i++) { Set(Find.TickManager, "ticksGameInt", i); driver.DriverTick(); }
                Check.Equal(0, p.GetReadyTick(selected), "no effect before last preparation tick");
                Set(Find.TickManager, "ticksGameInt", duration); driver.DriverTick();
                Check.Equal(duration + FleshActions.CooldownTicks(selected, p.GetRank(selected)), p.GetReadyTick(selected), "cooldown starts on completion");
                if (selected == FleshSkill.Feast) Check.Equal(duration + 3600, p.FeastUntil, "Feast duration starts after preparation");
                if (selected == FleshSkill.Empower) Check.Equal(duration + 18000, p.EmpowerUntil, "Empower duration starts after preparation");
                if (selected == FleshSkill.Harden) Check.Equal(duration + 18000, p.HardenUntil, "Harden duration starts after preparation");
                float thirst = p.Thirst;
                Toils(driver)[1].initAction();
                Check.Equal(thirst, p.Thirst, "completion is idempotent");
                Check.Equal(JobCondition.Succeeded, lastEnd, "job completes normally");
            }));
        }
        yield return new("FleshCasting.InterruptionAndInvalidCasterNeverCharge", f => InWorld(f, pawn =>
        {
            var p = FleshRepository.TryGet(pawn); p.Thirst = 1;
            var driver = Driver(pawn, FleshSkill.Empower);
            using var scope = EndScope();
            driver.ReadyForNextToil();
            for (int i = 0; i < 50; i++) driver.DriverTick();
            driver.Cleanup(JobCondition.InterruptForced);
            Check.Equal(0, p.GetReadyTick(FleshSkill.Empower), "cancellation cleanup never charges");
            Check.Equal(1f, p.Thirst, "cancellation preserves resource");
            Set(pawn, "mapIndexOrState", (sbyte)-1);
            Check.Equal(false, driver.CanContinue, "despawn invalidates preparation");
            Toils(driver)[1].initAction();
            Check.Equal(0, p.EmpowerUntil, "completion revalidates despawn");
            Check.Equal(JobCondition.Incompletable, lastEnd, "invalid completion ends cleanly");
            Set(pawn, "mapIndexOrState", (sbyte)0);
            driver = Driver(pawn, FleshSkill.Harden);
            Set(pawn.health, "healthState", PawnHealthState.Down);
            Check.Equal(false, driver.CanContinue, "downed caster cannot complete");
            Set(pawn.health, "healthState", PawnHealthState.Dead);
            Check.Equal(false, driver.CanContinue, "dead caster cannot complete");
            Set(pawn.health, "healthState", PawnHealthState.Mobile);
            Set(pawn.mindState.mentalStateHandler, "curStateInt", new MentalState_Berserk { pawn = pawn, def = new MentalStateDef { defName = "Berserk" } });
            Check.Equal(false, driver.CanContinue, "mental state interrupts preparation");
            Set(pawn.mindState.mentalStateHandler, "curStateInt", null!);
            pawn.health.hediffSet.hediffs.Clear();
            Check.Equal(false, driver.CanContinue, "loss of undead status invalidates preparation");
            Check.Equal(false, JobDriver_FleshCast.TryStart(pawn, FleshSkill.Harden), "ineligible request rejected");
        }));
        yield return new("FleshCasting.RealScribePreservesSkillAndRemainingDelay", f => InWorld(f, pawn =>
        {
            var driver = Driver(pawn, FleshSkill.Feast);
            using var scope = EndScope(); driver.ReadyForNextToil();
            for (int i = 0; i < 75; i++) driver.DriverTick();
            Check.Equal(225, driver.ticksLeftThisToil, "quarter of Feast preparation elapsed");
            var envelope = new Envelope(pawn) { Job = driver.job, Driver = driver };
            string file = Path.Combine(f.WorkingDirectory, "cast.xml");
            // Save real Job fields and driver timing. Pawn target is supplied by envelope on load;
            // this fixture does not claim a complete pawn/map reference round trip.
            envelope.Job.targetA = LocalTargetInfo.Invalid;
            Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref envelope, "cast", pawn); Scribe.saver.FinalizeSaving(); Scribe.ForceStop();
            envelope = null;
            Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref envelope, "cast", pawn); Scribe.loader.FinalizeLoading(); Scribe.ForceStop();
            Check.NotNull(envelope, "loaded cast envelope");
            driver = envelope!.Driver;
            envelope.Job.targetA = pawn; // Pawn references are outside this timing/identity envelope.
            StripGraphics(driver);
            Check.Equal(true, driver.CanContinue, "restored cast remains eligible");
            Check.Equal(FleshSkill.Feast, driver.Skill, "Job.count survives actual Scribe");
            Check.Equal(0, driver.CurToilIndex, "same preparation toil after load");
            Check.Equal(225, driver.ticksLeftThisToil, "loading never resets preparation duration");
            for (int i = 0; i < 224; i++) driver.DriverTick();
            Check.Equal(0, FleshRepository.TryGet(pawn).FeastUntil, "remaining preparation still required");
            Set(Find.TickManager, "ticksGameInt", 300); driver.DriverTick();
            Check.Equal(3900, FleshRepository.TryGet(pawn).FeastUntil, "resumed completion starts effect once");
        }));
        yield return new("FleshCasting.JobDefinitionAndPreparationContract", f => InWorld(f, pawn =>
        {
            var def = DefDatabase<JobDef>.GetNamed("TM_FleshCast");
            Check.Equal(typeof(JobDriver_FleshCast), def.driverClass, "shipped job driver resolved");
            Check.Equal(true, def.playerInterruptible, "player may cancel preparation");
            Check.Equal(0, JobDriver_FleshCast.PreparationTicks(FleshSkill.Restore), "restore stays instant");
            Check.Equal(0, JobDriver_FleshCast.PreparationTicks(FleshSkill.Devour), "feeding retains separate job");
            var driver = Driver(pawn, FleshSkill.Empower, false);
            var wait = Toils(driver)[0];
            Check.Equal(ToilCompleteMode.Delay, wait.defaultCompleteMode, "vanilla delayed toil");
            Check.Equal(true, wait.preTickActions?.Count > 0 || wait.preTickIntervalActions?.Count > 0, "standard progress effect installed");
            Check.Equal(true, Calls(Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_MULTIPLAYER_ADAPTER")!).GetType("TorannMagic.Integrations.FleshMultiplayerRegistration", true)!.GetMethod("Register")!, "TryStart"), "new request synchronized in MP registration");
        }));
        yield return new("FleshCasting.PreparationPulsesAreBoundedAndSeparateFromSuccess", f => InWorld(f, pawn =>
        {
            var driver = Driver(pawn, FleshSkill.Feast);
            using var scope = new Patches("warmup-effects");
            scope.Prefix(typeof(FleshEffects).GetMethod("CanDraw")!, nameof(AllowDraw));
            scope.Prefix(typeof(PawnTweener).GetMethod("TweenedPosRoot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, nameof(FixedDrawPosition));
            scope.Prefix(typeof(FleshEffects).GetMethod("Emit", BindingFlags.Static | BindingFlags.NonPublic)!, nameof(CaptureEffect));
            warmupEffects.Clear();
            var wait = Toils(driver)[0];
            for (int i = 0; i < 300; i++) { Set(Find.TickManager, "ticksGameInt", i); wait.tickAction(); }
            Check.Equal(10, warmupEffects.Count, "five second cast emits one pulse per thirty ticks");
            Check.Equal(true, warmupEffects.All(x => x == "TM_FleshSiphonFX"), "warmup uses gathering particles, not success flame");
            Check.Equal(0, FleshRepository.TryGet(pawn).FeastUntil, "warmup visuals never apply ability");
        }));
    }

    static Job? ordered; static int orders; static JobCondition lastEnd;
    static bool CaptureJob(Pawn_JobTracker __instance, Job job, ref bool __result) { ordered = job; orders++; Set(__instance, "curJob", job); __result = true; return false; }
    static bool EndJob(Pawn_JobTracker __instance, JobCondition condition) { lastEnd = condition; Set(__instance, "curJob", null!); return false; }
    static Patches EndScope() { var scope = new Patches("end"); scope.Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!, nameof(EndJob)); return scope; }
    static JobDriver_FleshCast Driver(Pawn pawn, FleshSkill skill, bool stripGraphics = true)
    {
        var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_FleshCast"), pawn); job.count = (int)skill;
        var driver = new JobDriver_FleshCast { pawn = pawn, job = job };
        Set(pawn.jobs, "curJob", job); Set(pawn.jobs, "curDriver", driver);
        typeof(JobDriver).GetMethod("SetupToils", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(driver, null);
        if (stripGraphics) StripGraphics(driver); return driver;
    }
    static List<Toil> Toils(JobDriver driver) => (List<Toil>)typeof(JobDriver).GetField("toils", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver)!;
    static void StripGraphics(JobDriver driver)
    {
        // Only progress-bar renderer callbacks are excluded; real DriverTick handles delay/transition.
        var wait = Toils(driver)[0]; wait.preTickActions?.Clear(); wait.preTickIntervalActions?.Clear();
    }
    static void InWorld(RuntimeFixture f, Action<Pawn> body)
    {
        var old = Current.Game;
        try
        {
            Current.Game = Blank<Game>(); Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map> { Blank<Map>() });
            var race = Blank<ThingDef>(); race.race = new RaceProperties();
            var pawn = new Pawn { def = race, kindDef = new PawnKindDef { race = race } };
            pawn.health = new Pawn_HealthTracker(pawn); pawn.jobs = new Pawn_JobTracker(pawn); pawn.stances = new Pawn_StanceTracker(pawn); pawn.pather = new Pawn_PathFollower(pawn);
            pawn.mindState = Blank<Pawn_MindState>(); pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
            Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
            pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = "TM_UndeadHD" } });
            using var healthScope = new Patches("health"); healthPawn = pawn;
            healthScope.Prefix(typeof(HediffSet).GetMethod("DirtyCache")!, nameof(HealthSideEffects));
            healthScope.Prefix(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, nameof(HealthSideEffects));
            var p = FleshRepository.Ensure(pawn); p.SkillPoints = 100; p.Thirst = 1;
            foreach (var skill in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Feast)) while (p.TryUpgrade(skill)) { }
            p.TryUpgrade(FleshSkill.Feast);
            var doc = new XmlDocument(); doc.Load(Path.Combine(f.Repository, "v1.7/Defs/FleshDevelopment/FleshGameplay.xml"));
            foreach (XmlNode node in doc.SelectNodes("Defs/JobDef")!)
            {
                var def = DirectXmlToObject.ObjectFromXml<JobDef>(node, false);
                if (DefDatabase<JobDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<JobDef>.Add(def);
            }
            body(pawn);
        }
        finally { healthPawn = null; Scribe.ForceStop(); Current.Game = old; }
    }
    static Pawn? healthPawn;
    static bool HealthSideEffects(Pawn ___pawn) => ___pawn != healthPawn;
    static readonly List<string> warmupEffects = [];
    static bool AllowDraw(ref bool __result) { __result = true; return false; }
    static bool CaptureEffect(string name) { warmupEffects.Add(name); return false; }
    static bool FixedDrawPosition(ref UnityEngine.Vector3 __result) { __result = new UnityEngine.Vector3(3, 0, 3); return false; }
    public sealed class Envelope(Pawn pawn) : IExposable
    {
        public Job Job = null!; public JobDriver_FleshCast Driver = null!;
        public void ExposeData()
        {
            Scribe_Deep.Look(ref Job, "job"); Scribe_Deep.Look(ref Driver, "driver");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Job.targetA = pawn; Driver.pawn = pawn; Driver.job = Job;
                Set(pawn.jobs, "curJob", Job); Set(pawn.jobs, "curDriver", Driver);
            }
        }
    }
    static bool Calls(MethodInfo caller, string target)
    {
        // Inspect metadata strings without loading the optional Multiplayer runtime assembly.
        var codes = typeof(System.Reflection.Emit.OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(System.Reflection.Emit.OpCode)).Select(f => (System.Reflection.Emit.OpCode)f.GetValue(null)!).ToDictionary(o => (ushort)o.Value);
        byte[] il = caller.GetMethodBody()!.GetILAsByteArray()!;
        for (int index = 0; index < il.Length;)
        {
            ushort code = il[index++]; if (code == 0xfe) code = (ushort)(0xfe00 | il[index++]);
            var operand = codes[code].OperandType;
            if (operand == System.Reflection.Emit.OperandType.InlineString && caller.Module.ResolveString(BitConverter.ToInt32(il, index)) == target) return true;
            index += operand switch {
                System.Reflection.Emit.OperandType.InlineNone => 0,
                System.Reflection.Emit.OperandType.ShortInlineBrTarget or System.Reflection.Emit.OperandType.ShortInlineI or System.Reflection.Emit.OperandType.ShortInlineVar => 1,
                System.Reflection.Emit.OperandType.InlineVar => 2,
                System.Reflection.Emit.OperandType.InlineI8 or System.Reflection.Emit.OperandType.InlineR => 8,
                System.Reflection.Emit.OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, index),
                _ => 4
            };
        }
        return false;
    }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static void Set(object target, string name, object value)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new Exception("Missing field " + name);
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly object instance; readonly Type type; readonly string id;
        public Patches(string suffix) { id = "rwom.regression.flesh-casting." + suffix; type = assembly.GetType("HarmonyLib.Harmony")!; instance = Activator.CreateInstance(type, id)!; }
        public void Prefix(MethodBase original, string name)
        {
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(FleshCastingTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            type.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5).Invoke(instance, [original, prefix, null, null, null]);
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(instance, [id]);
    }
}
