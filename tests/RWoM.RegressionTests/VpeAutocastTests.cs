using AbilityUser;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using RimWorld;
using TorannMagic;
using TorannMagic.AutoCast;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class VpeAutocastTests
{
    private const string VefPath = "C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/2023507013/1.7/Assemblies/VEF.dll";
    private static int dispatches;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("VpeAutocast.OptionalAbsentAndOrdinaryWarmup", f =>
        {
            Check.Equal(false, typeof(MagicAbility).Assembly.GetReferencedAssemblies().Any(a => a.Name == "VEF" || a.Name == "VanillaPsycastsExpanded"), "production has no optional assembly dependency");
            var optionalBefore = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "VEF" || a.GetName().Name == "VanillaPsycastsExpanded").ToArray();
            Console.WriteLine("VPE OPTIONAL ASSEMBLIES BEFORE OWNERSHIP CHECK: " + optionalBefore.Length);
            var pawn = Pawn(true);
            FleshStatTests.Set(pawn.jobs, "curJob", new Job { def = new JobDef { defName = "OrdinaryAttack", driverClass = typeof(JobDriver_AttackStatic) } });
            var stance = (Stance_Warmup)RuntimeHelpers.GetUninitializedObject(typeof(Stance_Warmup));
            stance.verb = new Verb_MeleeAttackDamage(); pawn.stances.curStance = stance;
            var helper = typeof(MagicAbility).Assembly.GetType("TorannMagic.AutoCast.TM_ExternalCastOwnership")!;
            Check.Equal(false, (bool)helper.GetMethod("IsCasting")!.Invoke(null, [pawn])!, "ordinary non-VEF warmup is unaffected");
            Check.Equal(optionalBefore.Length, AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "VEF" || a.GetName().Name == "VanillaPsycastsExpanded"), "ordinary ownership check never loads optional assemblies");
        });
        if (Environment.GetEnvironmentVariable("RWOM_TEST_CORE_ASSEMBLY") == "disabled")
        {
            Console.WriteLine("CAPABILITY SKIPPED: VEF/VPE adapter requires Core; Content-alone ownership check remains active.");
            yield break;
        }
        if (!File.Exists(VefPath) || !File.Exists("C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/2842502659/1.7/Assemblies/VanillaPsycastsExpanded.dll")) yield break;
        foreach (bool player in new[] { false, true })
        foreach (bool warmup in new[] { false, true })
        {
            bool p = player, w = warmup;
            yield return new($"VpeAutocast.DispatchPreservesExternalCast.Player{p}.Warmup{w}", f => Dispatch(p, w));
        }
    }
    internal static IDisposable IntegrationOwnershipScope()
    {
        var core = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_TEST_CORE_ASSEMBLY")!);
        var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
        var callback = (Func<Pawn,bool>)Delegate.CreateDelegate(typeof(Func<Pawn,bool>), adapter.GetType("TorannMagic.Integrations.VefCastOwnership",true)!.GetMethod("IsCasting")!);
        var api = core.GetType("TorannMagic.Core.CastOwnership",true)!;
        Check.Equal(true,(bool)api.GetMethod("Register")!.Invoke(null,["test.vef",callback])!,"test adapter registered");
        return new Cleanup(() => api.GetMethod("Unregister")!.Invoke(null,["test.vef",callback]));
    }
    sealed class Cleanup(Action action) : IDisposable { public void Dispose() => action(); }
    private static void Dispatch(bool player, bool warmup)
    {
        using var ownership = IntegrationOwnershipScope();
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(VefPath);
        var driver = assembly.GetType("VEF.Abilities.JobDriver_GotoTargetAndCastAbilityOnce", true)!;
        var verbType = assembly.GetType("VEF.Abilities.Verb_CastAbility", true)!;
        var pawn = Pawn(player);
        var vpeRoot = "C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/2842502659/1.7";
        var vpe = AssemblyLoadContext.Default.LoadFromAssemblyPath(vpeRoot + "/Assemblies/VanillaPsycastsExpanded.dll");
        var actualAbilityType = vpe.GetType("VanillaPsycastsExpanded.Ability_WordOf", true)!;
        Check.Equal(true, assembly.GetType("VEF.Abilities.Ability")!.IsAssignableFrom(actualAbilityType), "installed VPE ability uses actual VEF ability pipeline");
        Check.Equal(true, System.Xml.Linq.XDocument.Load(vpeRoot + "/Defs/AbilityDefs/Base.xml").Descendants("jobDef").Any(e => e.Value == "VFEA_GotoTargetAndUseAbility"), "installed VPE defaults to the approach-and-cast driver");
        var original = new Job { def = new JobDef { defName = "ExternalCast", driverClass = warmup ? typeof(JobDriver_AttackStatic) : driver } };
        FleshStatTests.Set(pawn.jobs, "curJob", original);
        if (warmup)
        {
            var stance = (Stance_Warmup)RuntimeHelpers.GetUninitializedObject(typeof(Stance_Warmup));
            stance.verb = (Verb)RuntimeHelpers.GetUninitializedObject(verbType);
            pawn.stances.curStance = stance;
        }
        var oldSettings = TorannMagic.ModOptions.Settings.Instance;
        try
        {
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { autocastQueueing = false, autocastEnabled = true, AICasting = true };
            using var patch = new SinkPatch(typeof(Pawn_JobTracker).GetMethod(nameof(Pawn_JobTracker.TryTakeOrderedJob))!, nameof(OrderedJob));
            dispatches = 0;
            DoJob.Execute(new Job { def = new JobDef { defName = "AutomaticRWoMCast" } }, pawn);
            Check.Equal(0, dispatches, "automatic dispatch must not replace the actual VEF cast owner");
            Check.Same(original, pawn.CurJob, "external job remains owned by VEF");
            TorannMagic.ModOptions.Settings.Instance.autocastQueueing = true;
            DoJob.Execute(new Job { def = new JobDef { defName = "QueuedAutomaticRWoMCast" } }, pawn);
            Check.Equal(0, pawn.jobs.jobQueue.Count, "no automatic job queued while VEF owns casting");
            Check.Equal(0, dispatches, "queue-enabled NPC dispatch also preserves external casting");
            TorannMagic.ModOptions.Settings.Instance.autocastQueueing = false;
            // The public admission boundaries must return before reading powers,
            // charging resources or creating a Jecs job (the fixture has no powers).
            var magic = new CompAbilityUserMagic { parent = pawn };
            var might = new CompAbilityUserMight { parent = pawn };
            magic.ResolveAutoCast(); might.ResolveAutoCast();
            magic.ResolveAIAutoCast(); might.ResolveAIAutoCast();
            Check.Equal(false, TM_TravelAutocast.Admitted(pawn), "travel cannot move an externally casting pawn");
            var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef)); def.defName = "RegressionExternalAdmission";
            var ability = new MagicAbility { AbilityUser = magic, Def = def };
            var verb = new Verb_UseAbility { caster = pawn, Ability = ability };
            FleshStatTests.Set(ability, "verb", verb);
            var safety = typeof(MagicAbility).Assembly.GetType("TorannMagic.Compatibility.AbilityCastSafety")!;
            Check.Equal(false, (bool)safety.GetMethod("CanAutocast")!.Invoke(null, [verb, (LocalTargetInfo)pawn])!, "automatic final admission rejects before target/resource processing");
            var prefix = typeof(MagicAbility).Assembly.GetType("TorannMagic.Compatibility.AbilityAutomaticJobAdmissionPatch")!.GetMethod("Prefix")!;
            object?[] manual = [ability, AbilityContext.Player, (LocalTargetInfo)pawn, null];
            Check.Equal(false, (bool)prefix.Invoke(null, manual)!, "off-map manual request is rejected even with an external cast");
            var previousGame = Current.Game;
            try
            {
                var game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
                FleshStatTests.Set(game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
                Current.Game = game;
                FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)0);
                Check.Equal(true, (bool)prefix.Invoke(null, manual)!, "on-map manual RWoM request keeps native admission");
            }
            finally
            {
                FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)-1);
                Current.Game = previousGame;
            }
            Check.Equal(false, TM_AutocastCombat.IsAbilityJob(original), "external support job is not NPC combat evidence");

            pawn.stances.curStance = null;
            var comp = (ThingComp)RuntimeHelpers.GetUninitializedObject(assembly.GetType("VEF.Abilities.CompAbilities", true)!);
            comp.parent = pawn;
            comp.GetType().GetField("currentlyCasting")!.SetValue(comp, RuntimeHelpers.GetUninitializedObject(actualAbilityType));
            FleshStatTests.Set(pawn, "comps", new List<ThingComp> { comp });
            FleshStatTests.Set(pawn.jobs, "curJob", new Job { def = new JobDef { defName = "OrdinaryWork" } });
            DoJob.Execute(new Job { def = new JobDef { defName = "AutomaticRWoMCast" } }, pawn);
            Check.Equal(1, dispatches, "ordinary autocast resumes immediately after external cast ends");
            TorannMagic.ModOptions.Settings.Instance.autocastQueueing = true;
            DoJob.Execute(new Job { def = new JobDef { defName = "QueuedAutomaticRWoMCast" } }, pawn);
            Check.Equal(player ? 1 : 0, pawn.jobs.jobQueue.Count, "ordinary player queueing resumes after external cast ends");
            Check.Equal(player ? 1 : 2, dispatches, "ordinary NPC immediate dispatch resumes with queue setting on");
        }
        finally { TorannMagic.ModOptions.Settings.Instance = oldSettings; }
    }
    private static bool OrderedJob(Pawn_JobTracker __instance, Job job)
    {
        dispatches++;
        FleshStatTests.Set(__instance, "curJob", job);
        return false; // Controlled engine scheduling sink; actual RWoM dispatch runs.
    }
    private static Pawn Pawn(bool player)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = "VpeRegressionPawn"; def.race = new RaceProperties { intelligence = Intelligence.Humanlike }; def.category = ThingCategory.Pawn;
        var pawn = new Pawn { def = def };
        pawn.jobs = new Pawn_JobTracker(pawn); pawn.stances = new Pawn_StanceTracker(pawn);
        pawn.health = new Pawn_HealthTracker(pawn); pawn.guest = new Pawn_GuestTracker(pawn);
        FleshStatTests.Set(pawn, "factionInt", new Faction { def = new FactionDef { isPlayer = player } });
        return pawn;
    }
    private sealed class SinkPatch : IDisposable
    {
        readonly Type harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly object harmony;
        readonly string id = "rwom.vpe." + Guid.NewGuid();
        public SinkPatch(MethodInfo target, string prefix)
        {
            harmony = Activator.CreateInstance(harmonyType, id)!;
            var methodType = harmonyType.Assembly.GetType("HarmonyLib.HarmonyMethod")!;
            var method = Activator.CreateInstance(methodType, typeof(VpeAutocastTests).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            var patch = harmonyType.GetMethods().Single(m => m.Name == "Patch");
            object?[] args = new object?[patch.GetParameters().Length]; args[0] = target; args[1] = method;
            patch.Invoke(harmony, args);
        }
        public void Dispose() => harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}






