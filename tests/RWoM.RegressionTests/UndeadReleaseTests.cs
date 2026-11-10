using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class UndeadReleaseTests
{
    static readonly Type CompType = typeof(TorannMagicMod).Assembly.GetType("TorannMagic.HediffComp_Undead")!;
    public static void Initialize()
    {
        foreach (var type in new[] { typeof(PrisonerInteractionModeDefOf), typeof(SlaveInteractionModeDefOf), typeof(FleshTypeDefOf) })
        { RuntimeHelpers.RunClassConstructor(type.TypeHandle); RuntimeLog.AcceptHeadlessDefOfInitialization(type.Name); }
    }
    public static IEnumerable<TestCase> Cases()
    {
        foreach (bool interval in new[] { false, true })
        foreach (bool animal in new[] { false, true })
        {
            bool useInterval = interval, isAnimal = animal;
            yield return new($"UndeadRelease.DeadOwner{(animal ? "Animal" : "Human")}{(interval ? "Interval" : "Tick")}", _ => World(() =>
            {
                var owner = MakePawn(); SetDead(owner);
                var pawn = MakePawn(isAnimal); var comp = Link(pawn, owner);
                Tick(comp, useInterval);
                Check.Equal(1, pawn.KillCalls, "confirmed dead owner collapses minion at tick 1, not the maintenance boundary");
                Check.Equal(true, pawn.Dead, "collapse requests actual death, not wild/hostile release");
                Tick(comp, useInterval);
                Check.Equal(1, pawn.KillCalls, "already dead minion is not killed twice");
            }));
        }
        yield return new("UndeadRelease.LivingNullAndOtherOwnersArePreserved", _ => World(() =>
        {
            var owner = MakePawn(); var other = MakePawn(); SetDead(other);
            var a = MakePawn(); var b = MakePawn(); var c = MakePawn();
            var ca = Link(a, owner); var cb = Link(b, null); var cc = Link(c, other);
            foreach (bool interval in new[] { false, true }) { Tick(ca, interval); Tick(cb, interval); }
            Check.Equal(0, a.KillCalls, "living off-map/caravan owner is valid");
            Check.Equal(0, b.KillCalls, "unresolved save/creation link retains existing grace period");
            Tick(cc, true);
            Check.Equal(1, c.KillCalls, "only actual deceased owner's minion collapses");
        }));
        yield return new("UndeadRelease.SummonedSkeletonExclusionAndReentrantKill", _ => World(() =>
        {
            var owner = MakePawn(); SetDead(owner);
            var skeleton = MakePawn(); var comp = Link(skeleton, owner);
            var old = TorannMagicDefOf.TM_SkeletonR;
            try { TorannMagicDefOf.TM_SkeletonR = skeleton.def; Tick(comp, true); Check.Equal(0, skeleton.KillCalls, "independent summoned skeleton keeps existing exclusion"); }
            finally { TorannMagicDefOf.TM_SkeletonR = old; }
            var pawn = MakePawn(); comp = Link(pawn, owner);
            pawn.DuringKill = () => Tick(comp, true);
            Tick(comp, true);
            Check.Equal(1, pawn.KillCalls, "callback re-entry cannot recursively kill same minion");
        }));
        yield return new("UndeadRelease.GuestResetAllowedButCaptureStillBlocked", _ =>
        {
            var pawn = MakePawn(); var old = TorannMagicDefOf.TM_UndeadHD;
            try
            {
                var def = new HediffDef { defName = "TestUndead" }; TorannMagicDefOf.TM_UndeadHD = def;
                pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = def });
                var prefix = typeof(TorannMagicMod).GetMethod("SetGuestStatusForUndead_Prefix", BindingFlags.Static | BindingFlags.NonPublic)!;
                Check.Equal(true, (bool)prefix.Invoke(null, [null, pawn, GuestStatus.Guest])!, "release must execute real guest cleanup");
                Check.Equal(false, (bool)prefix.Invoke(null, [new Faction(), pawn, GuestStatus.Prisoner])!, "undead cannot become prisoners");
                Check.Equal(false, (bool)prefix.Invoke(null, [new Faction(), pawn, GuestStatus.Slave])!, "undead cannot acquire a guest/slave status");
            }
            finally { TorannMagicDefOf.TM_UndeadHD = old; }
        });
        yield return new("UndeadRelease.FactionAlreadyReleasedIsIdempotent", _ =>
        {
            var pawn = MakePawn(); Release(pawn);
            Check.Equal(0, pawn.FactionCalls, "no repeated SetFaction(null) on factionless pawn");
            Set(pawn, "factionInt", new Faction()); Release(pawn); Release(pawn);
            Check.Equal(1, pawn.FactionCalls, "release called only once");
        });
        yield return new("UndeadRelease.RealGuestResetClearsSlaveState", _ => World(() =>
        {
            var pawn = MakePawn(); var old = TorannMagicDefOf.TM_UndeadHD;
            using var patches = new Patches();
            try
            {
                var def = new HediffDef { defName = "TestUndead" }; TorannMagicDefOf.TM_UndeadHD = def;
                pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = def });
                pawn.guest = new Pawn_GuestTracker(pawn);
                Set(pawn.guest, "guestStatusInt", GuestStatus.Slave);
                Set(pawn.guest, "slaveFactionInt", new Faction());
                patches.Prefix(typeof(Pawn_GuestTracker).GetMethod("SetGuestStatus")!, typeof(TorannMagicMod).GetMethod("SetGuestStatusForUndead_Prefix", BindingFlags.NonPublic | BindingFlags.Static)!);
                pawn.guest.SetGuestStatus(null);
                Check.Equal(false, pawn.guest.IsSlave, "real engine cleared slave state through registered mod prefix");
                Check.Equal<Faction?>(null, pawn.guest.SlaveFaction, "real engine cleared stored slave faction");
                Check.Equal<Faction?>(null, pawn.guest.HostFaction, "real engine has no stale host");
            }
            finally { TorannMagicDefOf.TM_UndeadHD = old; }
        }));
        foreach (bool afterAssignment in new[] { false, true })
        {
            bool changed = afterAssignment;
            yield return new($"UndeadRelease.PartialFactionFailure{(changed ? "After" : "Before")}DoesNotAbortDeath", _ =>
            {
                var pawn = MakePawn(); var old = TorannMagicDefOf.TM_UndeadHD;
                try
                {
                    var def = new HediffDef { defName = "TestUndead" }; TorannMagicDefOf.TM_UndeadHD = def;
                    pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = def });
                    pawn.def.thingClass = typeof(TMPawnSummoned); // overlapping release categories
                    Set(pawn, "factionInt", new Faction { def = new FactionDef() }); pawn.FactionFailure = changed ? 2 : 1;
                    Pawn instance = pawn;
                    Check.Equal(true, TorannMagicMod.Undead_Kill_Prefix.Prefix(ref instance, null), "cleanup exception cannot cancel death");
                    Check.Equal(1, pawn.FactionCalls, "overlapping summoned/undead categories do not retry partial transaction");
                    Check.Equal(changed, pawn.Faction == null, "partial faction state recorded without unsafe direct-write fallback");
                    var messages = (List<string>)typeof(RuntimeLog).GetField("Problems", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                    Check.Equal(1, messages.Count, "one diagnostic for one failed cleanup");
                    Check.Equal(true, messages[0].Contains(nameof(RecordingPawn.SetFaction)), "full stack identifies actual failing method");
                    Check.Equal(false, messages[0].Contains("guest-tracker mod postfix"), "no invented third-party diagnosis");
                    Console.WriteLine("EXPECTED INJECTED FAILURE: " + messages[0]); RuntimeLog.Clear();
                }
                finally { TorannMagicDefOf.TM_UndeadHD = old; }
            });
        }
    }
    static void Release(Pawn pawn) => typeof(TorannMagicMod).GetMethod("TryReleaseFaction", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [pawn]);
    static void World(Action action) => FleshStatTests.InGame(() => { Set(Find.TickManager, "ticksGameInt", 1); Set(Current.Game, "maps", new List<Map>()); action(); });
    static RecordingPawn MakePawn(bool animal = false)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = animal ? "TestAnimal" : "TestHuman"; def.race = new RaceProperties { intelligence = animal ? Intelligence.Animal : Intelligence.Humanlike };
        Set(def.race, "fleshType", new FleshTypeDef { isOrganic = true });
        var pawn = new RecordingPawn { def = def, kindDef = new PawnKindDef { race = def } };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.mindState = (Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));
        pawn.mindState.mentalStateHandler = new Verse.AI.MentalStateHandler(pawn);
        return pawn;
    }
    static HediffComp Link(Pawn pawn, Pawn? owner)
    {
        var comp = (HediffComp)Activator.CreateInstance(CompType)!;
        comp.parent = new HediffWithComps { pawn = pawn, def = new HediffDef { defName = "TestLinkedUndead" } };
        CompType.GetField("linkedPawn")!.SetValue(comp, owner);
        return comp;
    }
    static void Tick(HediffComp comp, bool interval)
    { float severity = 0; if (interval) comp.CompPostTickInterval(ref severity, 250); else comp.CompPostTick(ref severity); }
    static void SetDead(Pawn pawn) => Set(pawn.health, "healthState", PawnHealthState.Dead);
    static void Set(object target, string field, object value) => FleshStatTests.Set(target, field, value);
    public sealed class RecordingPawn : Pawn
    {
        public int KillCalls, FactionCalls, FactionFailure;
        public Action? DuringKill;
        public override void Kill(DamageInfo? dinfo, Hediff? exactCulprit = null)
        { KillCalls++; if (KillCalls > 2) throw new Exception("recursive collapse"); DuringKill?.Invoke(); SetDead(this); }
        public override void SetFaction(Faction newFaction, Pawn? recruiter = null)
        {
            FactionCalls++;
            if (FactionFailure == 1) throw new InvalidOperationException("fixture before assignment");
            Set(this, "factionInt", newFaction);
            if (FactionFailure == 2) throw new InvalidOperationException("fixture after assignment");
        }
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.regression.undead-release." + Guid.NewGuid();
        public Patches() => harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        public void Prefix(MethodInfo target, MethodInfo method)
        {
            var patch = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, method)!;
            harmony.GetType().GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5).Invoke(harmony, [target, patch, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}
