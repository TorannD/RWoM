using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class AbilityProjectileTests
{
    private static readonly List<Pawn> EffectRecipients = [];

    public static IEnumerable<TestCase> Cases()
    {
        foreach (var context in new[] { AbilityContext.Player, AbilityContext.AI })
        {
            var origin = context;
            yield return new("AbilityProjectile.OverheadEnemyTarget." + context, f => World(f, (actor, victim, projectile) =>
            {
                var ability = new MagicAbility { AbilityUser = new CompAbilityUserMagic { parent = actor }, Def = projectile.localAbilityDef };
                JobTarget(ability, origin, victim);
                projectile.Resolve();
                Console.WriteLine($"IMPACT {origin}: hit={projectile.Hit?.def.defName ?? "null"}; effects={string.Join(",", EffectRecipients.Select(p => p == actor ? "caster" : "victim"))}");
                Check.Same(victim, projectile.Hit, "Overhead ability impact reaches the selected enemy");
                Check.Equal(1, EffectRecipients.Count, "Exactly one effect recipient");
                Check.Same(victim, EffectRecipients.Single(), "Enemy spell cannot turn into a self debuff");
                Check.Equal(ProjectileHitFlags.None, projectile.HitFlags, "Overhead flight remains non-intercepting");
                Check.Equal(false, projectile.def.projectile.alwaysFreeIntercept, "Shared projectile interception policy is unchanged");
            }));
        }
        yield return new("AbilityProjectile.MissingEnemyDoesNotApplyToCaster", f => World(f, (actor, victim, projectile) =>
        {
            Set(victim, "mapIndexOrState", (sbyte)-1);
            projectile.Resolve();
            Check.Equal(0, EffectRecipients.Count, "Lost enemy cannot redirect its effects to the caster");
        }));
        yield return new("AbilityProjectile.OtherMapEnemyDoesNotApplyToCaster", f => World(f, (actor, victim, projectile) =>
        {
            Set(victim, "mapIndexOrState", (sbyte)1);
            projectile.Resolve();
            Check.Equal(0, EffectRecipients.Count, "Map transfer cannot redirect an enemy spell to the caster");
        }));
        yield return new("AbilityProjectile.ThickRoofStillBlocks", f => World(f, (actor, victim, projectile) =>
        {
            var roofs = new RoofDef[400]; roofs[6 + 6 * 20] = new RoofDef { isThickRoof = true };
            Set(projectile.Map.roofGrid, "roofGrid", roofs);
            projectile.Resolve();
            Check.Equal(true, projectile.WasDestroyed, "Original thick-roof interception is retained");
            Check.Equal(0, EffectRecipients.Count, "Blocked projectile applies no effect");
        }));
        yield return new("AbilityProjectile.SelfBuffStillWorks", f => World(f, (actor, victim, projectile) =>
        {
            projectile.usedTarget = projectile.intendedTarget = actor;
            projectile.localAbilityDef.MainVerb.AbilityTargetCategory = AbilityTargetCategory.TargetSelf;
            projectile.localAbilityDef.MainVerb.targetParams.canTargetSelf = true;
            projectile.Resolve();
            Check.Same(actor, EffectRecipients.Single(), "A real self-target buff retains its effect");
        }));
        yield return new("AbilityProjectile.GroundProjectileKeepsItsTarget", f => World(f, (actor, victim, projectile) =>
        {
            projectile.def.projectile.flyOverhead = false;
            projectile.Resolve();
            Check.Same(victim, projectile.Hit, "Ordinary projectile target remains unchanged");
            Check.Same(victim, EffectRecipients.Single(), "Ordinary projectile effect reaches its target");
        }));
        yield return new("AbilityProjectile.NoRequestedHitRemainsAMiss", f => World(f, (actor, victim, projectile) =>
        {
            projectile.HitFlags = ProjectileHitFlags.None;
            projectile.Resolve();
            Check.Equal<Thing?>(null, projectile.Hit, "A deliberately disabled impact must not be forced");
            Check.Equal(0, EffectRecipients.Count, "A deliberate miss cannot debuff the caster");
        }));
        yield return new("AbilityProjectile.DifferentUsedTargetRemainsAMiss", f => World(f, (actor, victim, projectile) =>
        {
            projectile.usedTarget = actor;
            projectile.HitFlags = ProjectileHitFlags.All;
            projectile.Resolve();
            Check.Equal<Thing?>(null, projectile.Hit, "Another pawn in the destination cell cannot substitute for the used target");
            Check.Equal(0, EffectRecipients.Count, "A changed trajectory cannot return effects to the caster");
        }));
        yield return new("AbilityProjectile.GroundMissDoesNotApplyToCaster", f => World(f, (actor, victim, projectile) =>
        {
            projectile.def.projectile.flyOverhead = false;
            Set(victim, "mapIndexOrState", (sbyte)-1);
            projectile.Resolve();
            Check.Equal(0, EffectRecipients.Count, "A lost ground-projectile target cannot become a self cast either");
        }));
        yield return new("AbilityProjectile.UnrelatedFrameworkIsUnchanged", f => World(f, (actor, victim, projectile) =>
        {
            projectile.localAbilityDef = Blank<AbilityUser.AbilityDef>();
            projectile.Resolve();
            Check.Equal<Thing?>(null, projectile.Hit, "Non-RWoM abilities retain their native hit policy");
            Check.Same(actor, EffectRecipients.Single(), "Non-RWoM abilities retain their framework's effect policy");
        }));
        yield return new("AbilityProjectile.CoveredTargetRemainsAMiss", f => World(f, (actor, victim, projectile) =>
        {
            var coverDef = Blank<ThingDef>(); coverDef.size = new IntVec2(1, 1); coverDef.fillPercent = 1f;
            coverDef.category = ThingCategory.Building;
            var cover = new Thing { def = coverDef };
            Set(cover, "mapIndexOrState", (sbyte)0); Set(cover, "positionInt", victim.Position);
            projectile.Map.thingGrid.Register(cover);
            Check.Equal(true, CoverUtility.ThingCovered(victim, projectile.Map), "Fixture uses real full cover");
            projectile.Resolve();
            Check.Equal<Thing?>(null, projectile.Hit, "Terminal repair cannot bypass native full-cover rejection");
            Check.Equal(0, EffectRecipients.Count, "A covered target cannot redirect effects to the caster");
        }));
        yield return new("AbilityProjectile.DerivedCasterEffectIsUnchanged", f => World(f, (actor, victim, projectile) =>
        {
            projectile.ApplyIntentionalCasterEffect();
            Check.Same(actor, EffectRecipients.Single(), "A derived projectile can still deliberately affect its caster");
        }));
        yield return new("AbilityProjectile.CellTargetFallbackIsUnchanged", f => World(f, (actor, victim, projectile) =>
        {
            projectile.usedTarget = projectile.intendedTarget = new IntVec3(6, 0, 6);
            projectile.Resolve();
            Check.Same(actor, EffectRecipients.Single(), "Cell-only casts preserve the original framework fallback");
        }));
    }

    private static void JobTarget(PawnAbility ability, AbilityContext context, Pawn target)
    {
        var job = ability.GetJob(context, target);
        Check.Same(target, job.targetA.Thing, "Real JecsLite job preserves the requested target");
        Check.Equal(context == AbilityContext.Player ? 1 : 0, job.count, "Manual and AI origins use their real job path");
    }

    private static void World(RuntimeFixture fixture, Action<Pawn, Pawn, RecordingProjectile> body)
    {
        var old = Current.Game;
        var game = Blank<Game>(); Set(game, "tickManager", new TickManager()); Set(game, "uniqueIDsManager", new UniqueIDsManager()); Current.Game = game;
        var map = Blank<Map>(); map.info = new MapInfo { Size = new IntVec3(20, 1, 20) };
        Set(game, "maps", new List<Map> { map, Blank<Map>() });
        map.cellIndices = new CellIndices(map); map.roofGrid = new RoofGrid(map); map.thingGrid = new ThingGrid(map);
        var actor = MakePawn("ProjectileCaster", new IntVec3(2, 0, 2));
        var victim = MakePawn("ProjectileVictim", new IntVec3(6, 0, 6));
        map.thingGrid.Register(actor); map.thingGrid.Register(victim);
        var definition = fixture.AddAbility("TM_OverheadTargetRegression");
        definition.abilityClass = typeof(MagicAbility);
        definition.MainVerb.AbilityTargetCategory = AbilityTargetCategory.TargetThing;
        definition.MainVerb.verbClass = typeof(Verb_SB);
        definition.MainVerb.targetParams = new TargetingParameters { canTargetPawns = true, canTargetSelf = false };
        var projectileDef = Blank<ProjectileDef_Ability>(); projectileDef.defName = "OverheadTargetRegression";
        projectileDef.projectile = new ProjectileProperties { flyOverhead = true };
        var projectile = new RecordingProjectile { def = projectileDef, localAbilityDef = definition, usedTarget = victim, intendedTarget = victim };
        Set(projectile, "mapIndexOrState", (sbyte)0); Set(projectile, "positionInt", new IntVec3(6, 0, 6));
        projectile.SeedLaunch(actor); projectile.HitFlags = ProjectileHitFlags.IntendedTarget;
        using var patches = new Patches();
        try
        {
            // Install the observation boundary before compiling production patches,
            // so the first wrapper invocation cannot inline the unpatched effect method.
            patches.RecordEffects();
            // Install the same class patches discovered by production's class-only scanner.
            // Absence on the baseline intentionally leaves the actual broken upstream path.
            foreach (string name in new[] { "TorannMagic.Compatibility.AbilityProjectileImpactPatch", "TorannMagic.Compatibility.AbilityProjectileRecipientPatch" })
                if (typeof(MagicAbility).Assembly.GetType(name) is Type type) patches.Add(type);
            patches.Add(typeof(TorannMagicMod.AbilityDef_Patch));
            EffectRecipients.Clear(); body(actor, victim, projectile);
        }
        finally { Current.Game = old; }
    }

    private static Pawn MakePawn(string name, IntVec3 position)
    {
        var definition = Blank<ThingDef>(); definition.defName = name; definition.category = ThingCategory.Pawn;
        definition.size = new IntVec2(1, 1);
        definition.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = definition }; pawn.health = new Pawn_HealthTracker(pawn);
        Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "positionInt", position);
        return pawn;
    }

    private static bool RecordEffects(Pawn victim) { EffectRecipients.Add(victim); return false; }

    private sealed class RecordingProjectile : Projectile_AbilityBase
    {
        public Thing? Hit;
        public bool WasDestroyed;
        public void ApplyIntentionalCasterEffect() => ApplyHediffsAndMentalStates(Caster, Caster, localApplyMentalStates, localAbilityDef);
        public void SeedLaunch(Pawn actor) { launcher = actor; destination = new UnityEngine.Vector3(6.5f, 0, 6.5f); }
        public void Resolve() => base.ImpactSomething();
        protected override void Impact(Thing hitThing, bool blockedByShield = false) { Hit = hitThing; base.Impact_Override(hitThing); }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) => WasDestroyed = true;
    }

    private sealed class Patches : IDisposable
    {
        private readonly Assembly assembly = Assembly.Load("0Harmony");
        private readonly object harmony;
        private readonly string id = "rwom.projectile-target-test." + Guid.NewGuid();
        public Patches() => harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        public void Add(Type type)
        {
            var processor = harmony.GetType().GetMethod("CreateClassProcessor")!.Invoke(harmony, [type])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
        }
        public void RecordEffects()
        {
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(AbilityProjectileTests).GetMethod(nameof(AbilityProjectileTests.RecordEffects), BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(Projectile_AbilityBase).GetMethod("ApplyHediffsAndMentalStates")!, hook, null, null, null]);
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }

    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    private static void Set(object target, string name, object value) => FleshStatTests.Set(target, name, value);
}

