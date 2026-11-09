using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;
using UnityEngine;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class Bug6973Tests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (bool universal in new[] { false, true })
            yield return new("Bug6973.ForcedUntrainedRegrowth." + universal, f => Surgery(f, universal));
        foreach (var method in new[] { "ThrowManaPuff", "ThrowBarrierMote", "ThrowShadowMote", "ThrowRegenMote" })
            foreach (bool throws in new[] { false, true })
                yield return new("Bug6973.VisualRandom." + method + ".Throws=" + throws, f => Motes(f, method, throws));
    }
    static void Surgery(RuntimeFixture f, bool universal)
    {
        LoadBaseDefinitions(f);
        var oldRecipe = universal ? TorannMagicDefOf.UniversalRegrowth : TorannMagicDefOf.Regrowth;
        var recipe = new RecipeDef { defName = universal ? "UniversalRegrowth" : "Regrowth", defaultIngredientFilter = new ThingFilter() };
        if (universal) TorannMagicDefOf.UniversalRegrowth = recipe;
        else TorannMagicDefOf.Regrowth = recipe;
        var oldCached = typeof(RecipeDef).GetField("isSurgeryCached", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cached = oldCached.GetValue(recipe);
        try
        {
            oldCached.SetValue(recipe, true);
            var actor = PawnWithComps(true, false);
            actor.magic.spell_RegrowLimb = false;
            var patient = PawnWithComps(false, false).pawn;
            // This test exercises an already-created WorkGiver job, not bill UI initialization.
            var bill = (Bill_Medical)RuntimeHelpers.GetUninitializedObject(typeof(Bill_Medical));
            bill.recipe = recipe;
            var job = new Job { bill = bill };
            TorannMagicMod.RegrowthSurgery_WorkGiver_Patch.Postfix(ref job, actor.pawn, patient, true);
            Check.Equal<Job?>(null, job, "manual priority must not assign regrowth to an untrained mage");
            actor.magic.spell_RegrowLimb = true;
            actor.magic.arcaneDmg = 1f;
            actor.magic.maxMP = 1f;
            actor.pawn.ageTracker = new Pawn_AgeTracker(actor.pawn);
            Set(actor.pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
            actor.pawn.needs.AllNeeds.Add(new Need_Mana(actor.pawn) { def = TorannMagicDefOf.TM_Mana });
            actor.magic.Mana.CurLevel = 1f;
            job = new Job { bill = bill };
            var originalJob = job;
            TorannMagicMod.RegrowthSurgery_WorkGiver_Patch.Postfix(ref job, actor.pawn, patient, true);
            Check.Same(originalJob, job, "qualified funded surgeon retains manual job without active Doctor priority");
            Check.Equal(1f, actor.magic.Mana.CurLevel, "eligibility does not pay in advance");
            actor.magic.spell_RegrowLimb = false;
            actor.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Druid));
            actor.magic.MagicData.MagicPowersD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_RegrowLimb)).learned = true;
            TorannMagicMod.RegrowthSurgery_WorkGiver_Patch.Postfix(ref job, actor.pawn, patient, true);
            Check.Same(originalJob, job, "learned native class power qualifies without standalone tome flag");
            actor.magic.Mana.CurLevel = .1f;
            TorannMagicMod.RegrowthSurgery_WorkGiver_Patch.Postfix(ref job, actor.pawn, patient, true);
            Check.Equal<Job?>(null, job, "manual priority cannot bypass mana requirement");
            Check.Equal(.1f, actor.magic.Mana.CurLevel, "rejected assignment does not charge mana");
            bill.recipe = new RecipeDef { defName = "OtherSurgery" };
            oldCached.SetValue(bill.recipe, true);
            job = originalJob;
            TorannMagicMod.RegrowthSurgery_WorkGiver_Patch.Postfix(ref job, actor.pawn, patient, true);
            Check.Same(originalJob, job, "unrelated surgeries are untouched");
        }
        finally
        {
            oldCached.SetValue(recipe, cached);
            if (universal) TorannMagicDefOf.UniversalRegrowth = oldRecipe;
            else TorannMagicDefOf.Regrowth = oldRecipe;
        }
    }
    static bool visible;
    static bool throwOnSpawn;
    static int spawned;
    static bool Visibility(ref bool __result) { __result = visible; return false; }
    static bool Make(ThingDef def, ref Thing __result)
    { __result = new MoteThrown { def = def ?? (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)) }; return false; }
    static bool Spawn(Thing newThing, ref Thing __result)
    { spawned++; if (throwOnSpawn) throw new InvalidOperationException("test visual failure"); __result = newThing; return false; }
    sealed class Boundaries : IDisposable
    {
        readonly Assembly asm = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.bug6973." + Guid.NewGuid();
        public Boundaries() => harmony = Activator.CreateInstance(asm.GetType("HarmonyLib.Harmony")!, id)!;
        public void Prefix(MethodInfo method, string name)
        {
            var hook = Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,
                typeof(Bug6973Tests).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [method, hook, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void Motes(RuntimeFixture f, string method, bool throws)
    {
        using var boundaries = new Boundaries();
        boundaries.Prefix(typeof(GenView).GetMethod("ShouldSpawnMotesAt", [typeof(Vector3), typeof(Map), typeof(bool)])!, nameof(Visibility));
        boundaries.Prefix(typeof(ThingMaker).GetMethod("MakeThing")!, nameof(Make));
        boundaries.Prefix(typeof(GenSpawn).GetMethod("Spawn", [typeof(Thing),typeof(IntVec3),typeof(Map),typeof(WipeMode)])!, nameof(Spawn));
        var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.moteCounter = new MoteCounter();
        var old = TorannMagicDefOf.Mote_ManaPuff;
        TorannMagicDefOf.Mote_ManaPuff = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        try
        {
            float Next(bool show)
            {
                visible = show; spawned = 0;
                Rand.PushState(69173);
                try
                {
                    throwOnSpawn = throws;
                    try { typeof(TM_MoteMaker).GetMethod(method, [typeof(Vector3),typeof(Map),typeof(float)])!
                        .Invoke(null, [new Vector3(4,0,4), map, 1f]); }
                    catch (TargetInvocationException e) when (throws && e.InnerException?.Message == "test visual failure") { }
                    Check.Equal(show ? 1 : 0, spawned, "only visible map spawns visual particle");
                    return Rand.Value;
                }
                finally { Rand.PopState(); throwOnSpawn = false; }
            }
            Check.Equal(Next(false), Next(true), "viewing a different map must not change the next gameplay random value");
        }
        finally { TorannMagicDefOf.Mote_ManaPuff = old; }
    }
}
