using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperArmorBreachTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SniperArmorBreach.NativeStackOwnershipExpiryAndSave", Stacking);
    }

    public static bool SkipInfrastructure() => false;
    private static void Stacking(RuntimeFixture f)
    {
        var previous = Current.Game;
        var defs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        const BindingFlags impl = BindingFlags.Static | BindingFlags.NonPublic;
        var api = typeof(SniperArmorBreach);
        var assembly = Assembly.Load("0Harmony");
        var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.breach." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            var patch = api.Assembly.GetType("TorannMagic.ClassProgression.SniperBreachArmorRoll", true)!;
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);

            // Isolate only rendering/cache and pawn death calculations from the headless run.
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,
                typeof(SniperArmorBreachTests).GetMethod(nameof(SkipInfrastructure))!)!;
            foreach (var hook in new[] { typeof(HediffSet).GetMethod("DirtyCache"), typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange") })
                ht.GetMethod("Patch")!.Invoke(harmony, [hook, prefix, null, null, null]);

            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            var ticks = new TickManager();
            Set(Current.Game, "tickManager", ticks);
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Set(Current.Game, "maps", new List<Map> {map});
            var def = new HediffDef { defName = "TM_SniperArmorBreach", hediffClass = api, initialSeverity = 1f };
            if (DefDatabase<HediffDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<HediffDef>.Add(def);
            var a = new Faction {loadID = 31, def = new FactionDef()};
            var b = new Faction {loadID = 32, def = new FactionDef()};
            var c = new Faction {loadID = 33, def = new FactionDef()};
            foreach (var faction in new[] {a,b,c})
                Set(faction, "relations", new[] {a,b,c}.Where(x => x != faction).Select(x => new FactionRelation {other=x,kind=FactionRelationKind.Hostile}).ToList());
            Pawn Fighter(Faction faction)
            {
                var pawn = PawnWithComps(false,false).pawn;
                pawn.kindDef = new PawnKindDef {race=pawn.def};
                pawn.guest = new Pawn_GuestTracker(pawn);
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                Set(pawn, "factionInt", faction);
                Set(pawn, "mapIndexOrState", (sbyte)0);
                return pawn;
            }
            var sniper = Fighter(a); var friend = Fighter(a); var outsider = Fighter(c); var victim = Fighter(b);
            void Hit(Pawn attacker, int rank) => api.GetMethod("Apply", impl)!.Invoke(null, [attacker, victim, rank]);
            int Pen(Pawn attacker) => (int)Math.Round((float)api.GetMethod("Penetration", impl)!.Invoke(null,
                [new DamageInfo(new DamageDef(), 10f, .3f, instigator:attacker), victim])! * 100f);
            void At(int now) => Set(ticks, "ticksGameInt", now);
            At(1000); Hit(sniper,1); Hit(sniper,0);
            Check.Equal(40, Pen(friend), "first opening applies to teammate");
            Check.Equal(30, Pen(outsider), "enemy third party cannot use it");
            At(1100); Hit(friend,3); Hit(outsider,2);
            Check.Equal(60, Pen(sniper), "separate teammate opening stacks");
            Check.Equal(45, Pen(outsider), "third party has only its own opening");
            Check.Equal(1, victim.health.hediffSet.hediffs.Count, "single medical condition");

            var set = victim.health.hediffSet;
            var path = Path.Combine(f.WorkingDirectory,"sniper-breach.xml");
            try { Scribe.saver.InitSaving(path,"test"); Scribe_Deep.Look(ref set,"health",victim); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            set = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref set,"health",victim); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            victim.health.hediffSet=set;
            Check.Equal(60, Pen(friend), "loaded openings indexed on restored pawn");
            At(1359); Check.Equal(60, Pen(friend), "both own stacks still live");
            At(1360); Check.Equal(50, Pen(friend), "first six-second window ends independently");
            At(1460); Check.Equal(30, Pen(friend), "last opening expired without waiting for health tick");
            var hediff=(SniperArmorBreach)set.hediffs.Single();
            hediff.TickInterval(60);
            Check.Equal(true, hediff.ShouldRemove, "native health cleanup can remove empty condition");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            DefDatabase<HediffDef>.Clear();
            foreach (var d in defs) DefDatabase<HediffDef>.Add(d);
            Current.Game=previous;
        }
    }
}
