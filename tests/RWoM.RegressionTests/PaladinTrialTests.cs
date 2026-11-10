using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class PaladinTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("PaladinTrials.ThresholdAndSuccessfulHealingOnce", Thresholds);
        yield return new("PaladinTrials.RejectsForeignTargetsAndAbilityOwners", Ownership);
        yield return new("PaladinTrials.NativeHookAttaches", Hook);
        foreach (var test in PaladinAuraTrialTests.Cases()) yield return test;
    }

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public DisciplineProgress Path;
        public PawnAbility Ability;
        public Hediff_Injury Injury;
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TrialPaladin", profileId = "rwom.paladin", domain = DisciplineDomain.Magic });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic;
            Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.guest = new Pawn_GuestTracker(pawn);
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.kindDef = new PawnKindDef { defName = "TrialPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            var comp = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(comp);
            comp.BeginNewProgression(Vocation.Mage); comp.State.TryAddFoundation("rwom.paladin", DisciplineDomain.Magic);
            Path = comp.State.Find("rwom.paladin");
            Ability = new PawnAbility(Magic); Set(Ability, "powerDef", TorannMagicDefOf.TM_Heal);
            Magic.MagicData.MagicPowersP.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Heal)).learned = true;
            Ability.CooldownTicksLeft = 91; Magic.AbilityData.Powers.Add(Ability);
            Injury = new Hediff_Injury { pawn = Target, def = new HediffDef { defName = "TrialInjury" } };
            Target.health.hediffSet.hediffs.Add(Injury); Severity(8f);
        }
        public void Severity(float value) => Set(Injury, "severityInt", value);
        public PaladinTrials.Snapshot Capture() => PaladinTrials.Capture(Caster, Target, Ability);
        public void Dispose()
        {
            Current.Game = previous;
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
        }
    }

    static void Thresholds(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        w.Magic.MagicData.MagicUserXP = 44; w.Magic.MagicData.MagicAbilityPoints = 3;
        var skill = w.Magic.MagicData.MagicPowerSkill_Heal[0]; skill.level = 2;
        w.Severity(7.99f); Check.Same(null!, w.Capture()!, "requires at least eight severity before cast");
        w.Severity(8f); var unchanged = w.Capture();
        Check.Equal(false, PaladinTrials.Complete(unchanged, true), "successful no-op cast earns nothing");
        var failed = w.Capture(); w.Severity(0f);
        Check.Equal(false, PaladinTrials.Complete(failed, false), "failed cast cannot credit external healing");
        w.Severity(8f); var insufficient = w.Capture(); w.Severity(0.01f);
        Check.Equal(false, PaladinTrials.Complete(insufficient, true), "requires eight actual severity reduction");
        w.Severity(8f); var healed = w.Capture(); w.Severity(0f);
        Check.Equal(true, PaladinTrials.Complete(healed, true), "boundary healing completes trial");
        Check.Equal(false, PaladinTrials.Complete(healed, true), "snapshot may only be consumed once");
        w.Severity(30f); Check.Same(null!, w.Capture()!, "completed trial stores no further cast snapshots");
        Check.Equal(1, w.Path.CompletedTrials.Count, "one persistent completion marker only");
        Check.Equal(PaladinTrials.FoundationTrialId, w.Path.CompletedTrials[0], "stable trial identifier");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "trial never advances stage");
        Check.Equal(44, w.Magic.MagicData.MagicUserXP, "no XP reward");
        Check.Equal(3, w.Magic.MagicData.MagicAbilityPoints, "no point reward");
        Check.Equal(2, skill.level, "skill investment preserved");
        Check.Equal(91, w.Ability.CooldownTicksLeft, "trial does not alter cooldown");
    }

    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Same(null!, PaladinTrials.Capture(w.Caster, w.Caster, w.Ability)!, "self healing excluded");
        w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless));
        Check.Same(null!, w.Capture()!, "Faceless copy is excluded"); w.Caster.story.traits.allTraits.Clear();
        w.Target.guest.guestStatusInt = GuestStatus.Prisoner;
        Check.Same(null!, w.Capture()!, "prisoner treatment cannot earn trial"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        var learned = w.Magic.MagicData.MagicPowersP.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Heal));
        learned.learned = false; Check.Same(null!, w.Capture()!, "unlearned native ownership excluded"); learned.learned = true;
        Set(w.Ability, "powerDef", TorannMagicDefOf.TM_AdvancedHeal);
        Check.Same(null!, w.Capture()!, "Priest AdvancedHeal cannot substitute for native Heal"); Set(w.Ability, "powerDef", TorannMagicDefOf.TM_Heal);
        var faction = w.Target.Faction;
        Set(w.Target, "factionInt", null); Check.Same(null!, w.Capture()!, "factionless targets excluded");
        Set(w.Target, "factionInt", faction);
        Set(w.Target, "mapIndexOrState", (sbyte)1); Check.Same(null!, w.Capture()!, "cross-map targets excluded");
        Set(w.Target, "mapIndexOrState", (sbyte)0);
        var enemy = new Faction { def = new FactionDef() };
        Set(enemy, "relations", new List<FactionRelation> { new() { other = faction, kind = FactionRelationKind.Hostile } });
        Set(faction, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
        Set(w.Target, "factionInt", enemy); Check.Same(null!, w.Capture()!, "hostile targets excluded");
        Set(w.Target, "factionInt", faction);
        w.Target.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Undead));
        Check.Same(null!, w.Capture()!, "undead damage cannot earn healing trial");
        w.Target.story.traits.allTraits.Clear();
        var fake = new PawnAbility(w.Magic); Set(fake, "powerDef", TorannMagicDefOf.TM_Heal);
        Check.Same(null!, PaladinTrials.Capture(w.Caster, w.Target, fake)!, "same definition without permanent owner is excluded");
        w.Magic.AbilityData.Powers.Clear(); w.Magic.AbilityData.TemporaryWeaponPowers.Add(w.Ability);
        Check.Same(null!, w.Capture()!, "item or temporary ability is excluded");
        w.Magic.AbilityData.TemporaryWeaponPowers.Clear(); w.Magic.AbilityData.Powers.Add(w.Ability);
        var stale = w.Capture(); w.Severity(1f); w.Magic.AbilityData.Powers.Clear();
        Check.Equal(false, PaladinTrials.Complete(stale, true), "lost ownership before completion is excluded");
        Check.Equal(0, w.Path.CompletedTrials.Count, "rejected events never record completion");
    }

    static void Hook(RuntimeFixture f)
    {
        var native = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Verb_Heal")!
            .GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
        Check.Equal(typeof(bool), native.ReturnType, "hook targets original successful-cast result");
        var harmony = Assembly.Load("0Harmony"); var ht = harmony.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.paladin-trial." + Guid.NewGuid(); var instance = Activator.CreateInstance(ht, id)!;
        try
        {
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(instance, [typeof(PaladinHealingTrialPatch)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            var patched = ((IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!).ToList();
            Check.Equal(true, patched.Contains(native), "prefix/postfix compile and attach to original heal verb");
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(instance, [id]); }
    }
}

internal static class PaladinAuraTrialTests
{
    public static IEnumerable<TestCase> Cases() { yield return new("PaladinTrials.RayActualAllyEffectAndScribe", Ray); }
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
        public DisciplineProgress Path => Progression.State.Find("rwom.paladin");
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
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.paladin", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "PaladinTrialProfile", profileId = "rwom.paladin", domain = DisciplineDomain.Magic });
        }
        void Prepare(Pawn pawn, Faction faction, int id)
        {
            pawn.thingIDNumber = id; pawn.kindDef = new PawnKindDef { defName = "PaladinTrialPawn" };
            pawn.guest = new Pawn_GuestTracker(pawn); pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0); Position(pawn, 0);
        }
        public PawnAbility Ability(TMAbilityDef def, bool owned = true)
        {
            Magic.MagicData.MagicPowersP.Single(p => p.TMabilityDefs.Contains(def)).learned = true;
            var ability = new PawnAbility(Magic); Set(ability, "powerDef", def);
            if (owned) Magic.AbilityData.Powers.Add(ability);
            return ability;
        }
        public void Adept() { Path.RecordCompletedTrial(PaladinTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.paladin", InitiationStage.Foundation, PaladinTrials.FoundationTrialId); }
        public bool Foundation => Path.CompletedTrials.Contains(PaladinTrials.FoundationTrialId);
        public bool Mastery => Path.CompletedTrials.Contains(PaladinTrials.MasteryTrialId);
        public void Dispose()
        {
            Current.Game = previous;
            Restore(profiles); Restore(things); Restore(hediffs);
        }
        static void Restore<T>(T[] defs) where T : Def, new() { DefDatabase<T>.Clear(); foreach (var def in defs) DefDatabase<T>.Add(def); }
    }
    static void Position(Thing thing, int x) => Set(thing, "positionInt", new IntVec3(x, 0, 0));
    public sealed class Envelope : IExposable
    {
        public ClassProgressionState state = new();
        public Projectile_AbilityBase projectile = null!;
        public Hediff aura = null!;
        public void ExposeData()
        {
            Scribe_Deep.Look(ref state, "state"); Scribe_Deep.Look(ref aura, "aura");
            if (aura != null) PaladinTrials.ExposeAura(aura);
        }
    }
    static Envelope RoundTrip(RuntimeFixture f, Envelope data, string suffix)
    {
        var file = Path.Combine(f.WorkingDirectory, "paladin-" + suffix + ".xml");
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "data"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        return loaded;
    }

    static void Ray(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept();
        var previousAura = TorannMagicDefOf.TM_RayOfHope_AuraHD; var previousEffect = TorannMagicDefOf.RayofHope;
        try
        {
            TorannMagicDefOf.TM_RayOfHope_AuraHD = new HediffDef { defName = "PaladinTrialAura", hediffClass = typeof(Hediff) };
            TorannMagicDefOf.RayofHope = new HediffDef { defName = "PaladinTrialRay", hediffClass = typeof(Hediff), maxSeverity = 1 };
            DefDatabase<HediffDef>.Add(TorannMagicDefOf.TM_RayOfHope_AuraHD); DefDatabase<HediffDef>.Add(TorannMagicDefOf.RayofHope);
            var ability = w.Ability(TorannMagicDefOf.TM_P_RayofHope);
            Set(w.Target, "factionInt", w.Caster.Faction);
            var source = new Hediff { def = TorannMagicDefOf.TM_RayOfHope_AuraHD, pawn = w.Caster, loadID = 28011 };
            w.Caster.health.hediffSet.hediffs.Add(source);
            var effect = new Hediff { def = TorannMagicDefOf.RayofHope, pawn = w.Target }; w.Target.health.hediffSet.hediffs.Add(effect);
            PaladinTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(false, w.Mastery, "unstamped aura does not qualify despite actual benefit");
            Set(effect, "severityInt", 0f);
            var copied = w.Ability(TorannMagicDefOf.TM_P_RayofHope, false);
            w.Magic.AbilityData.TemporaryWeaponPowers.Add(copied); PaladinTrials.MarkAura(copied);
            PaladinTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(false, w.Mastery, "temporary copied Ray cannot stamp native provenance");
            Set(effect, "severityInt", 0f); PaladinTrials.MarkAura(ability);
            var unrelated = new PawnAbility(w.Magic); Set(unrelated, "powerDef", TorannMagicDefOf.TM_Soothe); PaladinTrials.MarkAura(unrelated);
            source.pawn = null!;
            var loaded = RoundTrip(f, new Envelope { state = w.Progression.State, aura = source }, "ray");
            w.Caster.health.hediffSet.hediffs.Remove(source); source = loaded.aura; source.pawn = w.Caster; w.Caster.health.hediffSet.hediffs.Add(source); Set(w.Progression, "state", loaded.state);
            Set(effect, "severityInt", 1f); PaladinTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(false, w.Mastery, "unchanged existing buff cannot qualify");
            Set(effect, "severityInt", 0f); w.Target.guest.guestStatusInt = GuestStatus.Prisoner;
            PaladinTrials.ApplyAura(source, w.Target, effect.def); Check.Equal(false, w.Mastery, "prisoner ally excluded");
            Set(effect, "severityInt", 0f); w.Target.guest.guestStatusInt = GuestStatus.Guest;
            PaladinTrials.ApplyAura(source, w.Target, effect.def);
            Check.Equal(1f, effect.Severity, "actual native health mutation occurred");
            Check.Equal(true, w.Mastery, "persisted owned Ray source survives unrelated aura and rewards actual ally effect");
        }
        finally { TorannMagicDefOf.TM_RayOfHope_AuraHD = previousAura; TorannMagicDefOf.RayofHope = previousEffect; }
    }
}


