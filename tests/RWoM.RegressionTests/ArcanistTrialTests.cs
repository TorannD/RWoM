using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class ArcanistTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ArcanistTrial.BlinkActualMovementAndOwnership", Blink);
        yield return new("ArcanistTrial.NativeBlinkSuccessAndBlockedEndpoint", TeleportSpiritTests.ArcanistOwnedBlink);
        yield return new("ArcanistTrial.SummonPlacedMergedStackAndFailures", Summon);
        yield return new("ArcanistTrial.SummonTargetsOrdinaryItemsAtEveryRank", f =>
        {
            var xml = new System.Xml.XmlDocument();
            xml.Load(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Arcanist.xml"));
            var itemDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            itemDef.category = ThingCategory.Item;
            itemDef.isAutoAttackableMapObject = false;
            var item = new ThingWithComps { def = itemDef };
            foreach (var suffix in new[] { "", "_I", "_II", "_III" })
            {
                var node = xml.SelectSingleNode($"/Defs/*[defName='TM_Summon{suffix}']/MainVerb/targetParams");
                var parameters = DirectXmlToObject.ObjectFromXml<TargetingParameters>(node!, false);
                Check.Equal(true, parameters.CanTarget(new TargetInfo(item)), "ordinary nonattackable cargo " + suffix);
                Check.Equal(false, parameters.canTargetBuildings, "building is not cargo");
            }
        });
        yield return new("ArcanistTrial.MissileAttributionDamageAndScribe", Missile);
        yield return new("ArcanistTrial.ShadowActualAllyEffectAndScribe", Shadow);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly Faction friendly = new() { def = new FactionDef { isPlayer = true } };
        readonly Faction enemy = new() { def = new FactionDef() };
        public readonly Pawn Caster, Target;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.arcanist");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Find.TickManager, "gameStartAbsTick", 1);
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            Set(friendly, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
            Set(enemy, "relations", new List<FactionRelation> { new() { other = friendly, kind = FactionRelationKind.Hostile } });
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic;
            Target = PawnWithComps(false, false).pawn;
            Prepare(Caster, friendly, 28001); Prepare(Target, enemy, 28002);
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.arcanist", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "ArcanistTrialProfile", profileId = "rwom.arcanist", domain = DisciplineDomain.Magic });
        }
        void Prepare(Pawn pawn, Faction faction, int id)
        {
            pawn.thingIDNumber = id; pawn.kindDef = new PawnKindDef { defName = "ArcanistTrialPawn" };
            pawn.guest = new Pawn_GuestTracker(pawn); pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0); Position(pawn, 0);
        }
        public PawnAbility Ability(TMAbilityDef def, bool owned = true)
        {
            Magic.MagicData.MagicPowersA.Single(p => p.TMabilityDefs.Contains(def)).learned = true;
            var ability = new PawnAbility(Magic); Set(ability, "powerDef", def);
            if (owned) Magic.AbilityData.Powers.Add(ability);
            return ability;
        }
        public void Adept() { Path.RecordCompletedTrial(ArcanistTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.arcanist", InitiationStage.Foundation, ArcanistTrials.FoundationTrialId); }
        public bool Foundation => Path.CompletedTrials.Contains(ArcanistTrials.FoundationTrialId);
        public bool Mastery => Path.CompletedTrials.Contains(ArcanistTrials.MasteryTrialId);
        public void Dispose()
        {
            ArcanistTrials.ExitImpact(null!); ArcanistTrials.EndLaunch(null!); Current.Game = previous;
            Restore(profiles); Restore(things); Restore(hediffs);
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var def in defs) DefDatabase<T>.Add(def); }
    }
    static void Position(Thing thing, int x) => Set(thing, "positionInt", new IntVec3(x, 0, 0));
    static void Blink(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var copied = w.Ability(TorannMagicDefOf.TM_Blink, false);
        w.Magic.AbilityData.TemporaryWeaponPowers.Add(copied);
        w.Magic.AbilityData.TemporaryApparelPowers.Add(copied);
        Check.Equal(true, ArcanistTrials.CaptureMove(copied, w.Caster, ArcanistTrials.Spell.Blink) == null, "copy/item instance is excluded even when learned");
        var ability = w.Ability(TorannMagicDefOf.TM_Blink);
        w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless));
        Check.Equal(true, ArcanistTrials.CaptureMove(ability, w.Caster, ArcanistTrials.Spell.Blink) == null, "Faceless copied class excluded");
        w.Caster.story.traits.allTraits.Clear();
        var move = ArcanistTrials.CaptureMove(ability, w.Caster, ArcanistTrials.Spell.Blink);
        ArcanistTrials.CompleteBlink(move);
        Check.Equal(false, w.Foundation, "same-cell/failed move cannot complete");
        move = ArcanistTrials.CaptureMove(ability, w.Caster, ArcanistTrials.Spell.Blink);
        Position(w.Caster, 7); ArcanistTrials.CompleteBlink(move);
        Check.Equal(false, w.Foundation, "short relocation does not meet distance");
        move = ArcanistTrials.CaptureMove(ability, w.Caster, ArcanistTrials.Spell.Blink);
        Position(w.Caster, 15); ArcanistTrials.CompleteBlink(move); ArcanistTrials.CompleteBlink(move);
        Check.Equal(true, w.Foundation, "actual eight-cell movement completes once");
    }
    static void Summon(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept();
        var ability = w.Ability(TorannMagicDefOf.TM_Summon);
        var itemDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        itemDef.defName = "ArcanistTestItem"; itemDef.category = ThingCategory.Item;
        var item = new Thing { def = itemDef, stackCount = 10 }; Set(item, "mapIndexOrState", (sbyte)0); Position(item, 8);
        var merged = new Thing { def = itemDef, stackCount = 50 }; Set(merged, "mapIndexOrState", (sbyte)0); Position(merged, 1);
        var move = ArcanistTrials.CaptureMove(ability, item, ArcanistTrials.Spell.Summon);
        ArcanistTrials.CompleteSummon(move, merged, false);
        Check.Equal(false, w.Mastery, "failed placement cannot claim a nearby preexisting stack");
        move = ArcanistTrials.CaptureMove(ability, item, ArcanistTrials.Spell.Summon);
        ArcanistTrials.CompleteSummon(move, item, true);
        Check.Equal(false, w.Mastery, "unchanged position cannot qualify");
        Position(item, 7); move = ArcanistTrials.CaptureMove(ability, item, ArcanistTrials.Spell.Summon);
        ArcanistTrials.CompleteSummon(move, merged, true);
        Check.Equal(false, w.Mastery, "short retrieval cannot qualify");
        Position(item, 8); move = ArcanistTrials.CaptureMove(ability, item, ArcanistTrials.Spell.Summon);
        Set(item, "mapIndexOrState", (sbyte)-2); merged.stackCount += item.stackCount;
        ArcanistTrials.CompleteSummon(move, merged, true); ArcanistTrials.CompleteSummon(move, merged, true);
        Check.Equal(true, w.Mastery, "successful retrieval uses surviving merged stack, not destroyed original");
    }
    public sealed class TestProjectile : Projectile_AbilityBase { public void SeedCaster(Pawn pawn) => launcher = pawn; }
    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new();
        public Projectile_AbilityBase projectile = null!;
        public Hediff aura = null!;
        public void ExposeData()
        {
            Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref projectile, "projectile"); Scribe_Deep.Look(ref aura, "aura");
            if (aura != null) ArcanistTrials.ExposeAura(aura);
        }
    }
    static Envelope RoundTrip(RuntimeFixture f, Envelope data, string suffix)
    {
        var file = Path.Combine(f.WorkingDirectory, "arcanist-" + suffix + ".xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        return loaded;
    }
    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.arcanist.tests." + Guid.NewGuid();
        readonly object harmony;
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            foreach (var patch in new[] { typeof(LightningLaunchScopePatch), typeof(LightningLaunchPatch), typeof(LightningProjectileSavePatch), typeof(ArcanistImpactPatch), typeof(ArcanistDamagePatch) })
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [patch])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void Missile(RuntimeFixture f)
    {
        using var w = new Fixture(f); using var hooks = new Hooks();
        var ability = w.Ability(TorannMagicDefOf.TM_MagicMissile, false);
        var verb = new Verb_BLOS { Ability = ability }; Set(verb, "caster", w.Caster);
        var def = (ProjectileDef_Ability)RuntimeHelpers.GetUninitializedObject(typeof(ProjectileDef_Ability));
        def.defName = "Projectile_MagicMissile"; def.thingClass = typeof(TestProjectile); def.category = ThingCategory.Projectile;
        def.projectile = new ProjectileProperties { speed = 100 }; def.comps = new List<CompProperties>(); DefDatabase<ThingDef>.Add(def);
        var projectile = new TestProjectile { def = def, thingIDNumber = 28991 }; Set(projectile, "mapIndexOrState", (sbyte)0);
        void Launch()
        {
            var prior = ArcanistTrials.BeginLaunch(verb, def);
            try { projectile.Launch(w.Caster, ability.Def, UnityEngine.Vector3.zero, IntVec3.Zero, ProjectileHitFlags.IntendedTarget); }
            finally { ArcanistTrials.EndLaunch(prior); }
        }
        var damage = new DamageDef { defName = "TM_MagicMissile", workerClass = typeof(DamageWorker) };
        void Hit(float amount, bool nested = false)
        {
            var prior = ArcanistTrials.EnterImpact(projectile, w.Target);
            var hit = ArcanistTrials.BeginHit(w.Target, new DamageInfo(damage, amount, instigator: w.Caster));
            if (nested)
            {
                var child = ArcanistTrials.BeginHit(w.Target, new DamageInfo(damage, 100, instigator: w.Caster));
                ArcanistTrials.CompleteHit(child, 100); ArcanistTrials.EndHit(child);
            }
            ArcanistTrials.CompleteHit(hit, amount); ArcanistTrials.CompleteHit(hit, amount);
            ArcanistTrials.EndHit(hit); ArcanistTrials.ExitImpact(prior);
        }
        Launch(); Hit(20); Check.Equal(0f, w.Path.ArcanistTrials.MissileDamage, "copied instance cannot stamp native launch");
        w.Magic.AbilityData.Powers.Add(ability); Launch();
        foreach (var amount in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) Hit(amount);
        Check.Equal(0f, w.Path.ArcanistTrials.MissileDamage, "actual finite positive damage only");
        w.Target.guest.guestStatusInt = GuestStatus.Prisoner; Hit(20); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Check.Equal(0f, w.Path.ArcanistTrials.MissileDamage, "prisoners excluded");
        Hit(7, true); Check.Equal(7f, w.Path.ArcanistTrials.MissileDamage, "nested/repeated packet cannot borrow source");
        projectile.SeedCaster(null!);
        var loaded = RoundTrip(f, new Envelope { state = w.Progression.State, projectile = projectile }, "missile");
        projectile = (TestProjectile)loaded.projectile; projectile.SeedCaster(w.Caster); Set(w.Progression, "state", loaded.state);
        Check.Equal(7f, w.Path.ArcanistTrials.MissileDamage, "bounded damage progress survives save");
        Hit(13); Check.Equal(true, w.Foundation, "native projectile source survives save and completes at twenty");
        Check.Equal(20f, w.Path.ArcanistTrials.MissileDamage, "damage capped at threshold");
    }
    static void Shadow(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept();
        var previousAura = TorannMagicDefOf.TM_Shadow_AuraHD; var previousEffect = TorannMagicDefOf.Shadow;
        try
        {
            TorannMagicDefOf.TM_Shadow_AuraHD = new HediffDef { defName = "ArcanistTrialAura", hediffClass = typeof(Hediff) };
            TorannMagicDefOf.Shadow = new HediffDef { defName = "ArcanistTrialShadow", hediffClass = typeof(Hediff), maxSeverity = 1 };
            DefDatabase<HediffDef>.Add(TorannMagicDefOf.TM_Shadow_AuraHD); DefDatabase<HediffDef>.Add(TorannMagicDefOf.Shadow);
            var ability = w.Ability(TorannMagicDefOf.TM_Shadow);
            Set(w.Target, "factionInt", w.Caster.Faction);
            var source = new Hediff { def = TorannMagicDefOf.TM_Shadow_AuraHD, pawn = w.Caster, loadID = 28011 };
            w.Caster.health.hediffSet.hediffs.Add(source);
            var effect = new Hediff { def = TorannMagicDefOf.Shadow, pawn = w.Target }; w.Target.health.hediffSet.hediffs.Add(effect);
            ArcanistTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(false, w.Mastery, "unstamped aura does not qualify despite actual benefit");
            Set(effect, "severityInt", 0f); ArcanistTrials.MarkAura(ability);
            var unrelated = new PawnAbility(w.Magic); Set(unrelated, "powerDef", TorannMagicDefOf.TM_Soothe); ArcanistTrials.MarkAura(unrelated);
            source.pawn = null!;
            var loaded = RoundTrip(f, new Envelope { state = w.Progression.State, aura = source }, "shadow");
            w.Caster.health.hediffSet.hediffs.Remove(source); source = loaded.aura; source.pawn = w.Caster; w.Caster.health.hediffSet.hediffs.Add(source); Set(w.Progression, "state", loaded.state);
            Set(effect, "severityInt", 1f); ArcanistTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(false, w.Mastery, "unchanged existing buff cannot qualify");
            Set(effect, "severityInt", 0f); w.Target.guest.guestStatusInt = GuestStatus.Prisoner;
            ArcanistTrials.ApplyAura(source, w.Target, effect.def); Check.Equal(false, w.Mastery, "prisoner ally excluded");
            Set(effect, "severityInt", 0f); w.Target.guest.guestStatusInt = GuestStatus.Guest;
            ArcanistTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(1f, effect.Severity, "actual native health mutation occurred");
            Check.Equal(true, w.Mastery, "persisted owned Shadow source survives unrelated aura and rewards actual ally effect");
        }
        finally { TorannMagicDefOf.TM_Shadow_AuraHD = previousAura; TorannMagicDefOf.Shadow = previousEffect; }
    }
}

internal static partial class TeleportSpiritTests
{
    internal static void ArcanistOwnedBlink(RuntimeFixture fixture) => World(fixture, w =>
    {
        GrimoireProgressionTests.LoadBaseDefinitions(fixture);
        using var profile = new DefinitionScope<ClassDisciplineDef>(new ClassDisciplineDef { defName = "ArcanistMovementProfile", profileId = "rwom.arcanist", domain = DisciplineDomain.Magic });
        var magic = new CompAbilityUserMagic { parent = w.Caster, customIndex = -1 };
        Set(magic, "initializedIsMagicUser", true); Set(magic, "isMagicUser", true); Set(magic, "magicData", new MagicData(magic));
        w.Caster.AllComps.Add(magic); w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) }; TM_Calc.InvalidateCompCache(w.Caster);
        var progression = new CompClassProgression { parent = w.Caster }; w.Caster.AllComps.Add(progression);
        progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation("rwom.arcanist", DisciplineDomain.Magic);
        magic.MagicData.MagicPowersA.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink)).learned = true;
        var verb = (Verb_UseAbility)w.Verb("Verb_Blink", fixture);
        var ability = new PawnAbility(magic); Set(ability, "powerDef", TorannMagicDefOf.TM_Blink); magic.AbilityData.Powers.Add(ability); verb.Ability = ability;
        using var patches = w.MovementBoundaries();
        // Existing ManualTravel tests own payment checks; this test follows the actual
        // movement, notification and success callback with a paid cast admitted.
        patches.Prefix(typeof(TorannMagic.Odyssey.ManualTravelSafety).GetMethod("CanPay")!, nameof(ArcanistAdmitPayment));
        var path = progression.State.Find("rwom.arcanist");
        var blocked = new IntVec3(12, 0, 4); w.Building(blocked); Set(verb, "currentTarget", new LocalTargetInfo(blocked));
        Check.Equal(false, Fire(verb), "real blocked Blink fails");
        Check.Equal(false, path.CompletedTrials.Contains(ArcanistTrials.FoundationTrialId), "failed native move earns no trial credit");
        var endpoint = new IntVec3(12, 0, 10); Set(verb, "currentTarget", new LocalTargetInfo(endpoint));
        Check.Equal(true, Fire(verb), "real owned Blink completes");
        Check.Equal(endpoint, w.Caster.Position, "real movement helper reaches selected coordinates");
        Check.Equal(true, path.CompletedTrials.Contains(ArcanistTrials.FoundationTrialId), "native Blink success callback records trial");
        w.AssertPreserved();
    });
    static bool ArcanistAdmitPayment(ref bool __result) { __result = true; return false; }
}

