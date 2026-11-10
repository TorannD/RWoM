using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class NecromancerDeathMarkTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("NecromancerDeathMarkTrial.NativeCurseDefinition", f =>
        {
            var doc = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/HediffDefs/Hediffs_Necro.xml"));
            var curse = doc.Root!.Elements("HediffDef").Single(e => (string?)e.Element("defName") == "TM_DeathMarkCurse");
            var comps = curse.Element("comps")!.Elements().ToList();
            Check.Equal(1, comps.Count(e => (string?)e.Attribute("Class") == typeof(HediffCompProperties_DeathMarkTrial).FullName), "native curse owns exactly one trial tracker");
            Check.Equal("CombatBuffBase", (string?)curse.Attribute("ParentName"), "curse inherits combat buff lifetime");
            var buffs = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(f.Repository, "v1.7/Defs/HediffDefs/Hediffs_TMagic_Buffs.xml"));
            var parent = buffs.Root!.Elements("HediffDef").Single(e => (string?)e.Attribute("Name") == "CombatBuffBase");
            var timer = parent.Element("comps")!.Elements().Single(e => (string?)e.Attribute("Class") == "HediffCompProperties_Disappears").Element("disappearsAfterTicks")!;
            Check.Equal("1800", timer.Element("min")!.Value, "native minimum disappearance timer");
            Check.Equal("2400", timer.Element("max")!.Value, "native maximum disappearance timer");
        });
        yield return new("NecromancerDeathMarkTrial.OwnedApplicationDeathAndRepeat", Completion);
        yield return new("NecromancerDeathMarkTrial.RankThreeOwnedApplication", RankThree);
        yield return new("NecromancerDeathMarkTrial.EligibilityAndAbilityOwnership", Eligibility);
        yield return new("NecromancerDeathMarkTrial.TargetEligibilityAtCastAndDeath", Targets);
        yield return new("NecromancerDeathMarkTrial.ExpiryAndHediffRemoval", Expiry);
        yield return new("NecromancerDeathMarkTrial.TwoCastersShareConcreteActiveMark", Attribution);
        yield return new("NecromancerDeathMarkTrial.LowSeverityActiveAndReplacement", ActiveAndReplacement);
        yield return new("NecromancerDeathMarkTrial.NativeScribeCasterReferences", Saved);
    }

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly Faction friendly = new() { def = new FactionDef { isPlayer = true } };
        readonly Faction enemy = new() { def = new FactionDef() };
        int nextId = 9700;
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public DisciplineProgress Path;
        public PawnAbility Ability;
        public HediffWithComps Curse;
        public HediffComp_DeathMarkTrial Tracker;
        public HediffComp_Disappears Timer;
        public Fixture(RuntimeFixture f, bool adept = true)
        {
            LoadBaseDefinitions(f);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "DeathMarkNecromancer", profileId = "rwom.necromancer", domain = DisciplineDomain.Magic });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            Set(friendly, "relations", new List<FactionRelation> { new() { other = enemy, kind = FactionRelationKind.Hostile } });
            Set(enemy, "relations", new List<FactionRelation> { new() { other = friendly, kind = FactionRelationKind.Hostile } });
            (Caster, Magic, Path, Ability) = AddCaster(adept);
            Target = PawnWithComps(false, false).pawn; Prepare(Target, enemy);
            Curse = new HediffWithComps { pawn = Target, def = new HediffDef { defName = "TM_DeathMarkCurse" } };
            Tracker = new HediffComp_DeathMarkTrial { parent = Curse, props = new HediffCompProperties_DeathMarkTrial() };
            Timer = new HediffComp_Disappears { parent = Curse, props = new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange(1800, 2400) }, ticksToDisappear = 2000 };
            Curse.comps = [Timer, Tracker]; Set(Curse, "severityInt", 8f); Target.health.hediffSet.hediffs.Add(Curse);
        }
        void Prepare(Pawn pawn, Faction faction)
        {
            pawn.thingIDNumber = nextId++; pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
            pawn.kindDef = new PawnKindDef { defName = "DeathMarkPawn" }; pawn.guest = new Pawn_GuestTracker(pawn);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
        }
        public (Pawn, CompAbilityUserMagic, DisciplineProgress, PawnAbility) AddCaster(bool adept = true)
        {
            var p = PawnWithComps(true, false); Prepare(p.pawn, friendly);
            var progression = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(progression);
            progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation("rwom.necromancer", DisciplineDomain.Magic);
            var path = progression.State.Find("rwom.necromancer");
            if (adept)
            {
                path.RecordCompletedTrial(NecromancerControlTrial.TrialId);
                progression.State.TryAdvanceAfterTrial("rwom.necromancer", InitiationStage.Foundation, NecromancerControlTrial.TrialId);
            }
            var ability = new PawnAbility(p.magic); Set(ability, "powerDef", TorannMagicDefOf.TM_DeathMark);
            ability.CooldownTicksLeft = 73; p.magic.AbilityData.Powers.Add(ability);
            return (p.pawn, p.magic, path, ability);
        }
        public void Apply(float severity = 8f) => NecromancerDeathMarkTrial.Applied(Caster, Target, Ability, severity);
        public void Die()
        {
            Tracker.Notify_PawnKilled();
            Set(Target.health, "healthState", PawnHealthState.Dead);
            Set(Target, "mapIndexOrState", (sbyte)-2);
            Tracker.Notify_PawnDied(null);
        }
        public void AssertComplete(bool expected, string message) => Check.Equal(expected, Path.CompletedTrials.Contains(NecromancerDeathMarkTrial.TrialId), message);
        public void Dispose()
        {
            Current.Game = previous; DefDatabase<ClassDisciplineDef>.Clear();
            foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
        }
    }

    static void Completion(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        w.Magic.MagicData.MagicUserXP = 44; w.Magic.MagicData.MagicAbilityPoints = 3;
        w.Apply(); w.Tracker.Notify_PawnDied(null); w.AssertComplete(false, "living target callback excluded");
        w.Die(); w.AssertComplete(true, "owned application and marked enemy death complete trial");
        w.Die(); w.Apply(); w.Die();
        Check.Equal(1, w.Path.CompletedTrials.Count(x => x == NecromancerDeathMarkTrial.TrialId), "repeated effects complete once");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "trial does not advance stage");
        Check.Equal(44, w.Magic.MagicData.MagicUserXP, "XP unchanged");
        Check.Equal(3, w.Magic.MagicData.MagicAbilityPoints, "points unchanged");
        Check.Equal(73, w.Ability.CooldownTicksLeft, "cooldown unchanged");
    }

    static void Eligibility(RuntimeFixture f)
    {
        foreach (string mode in new[] { "foundation", "classic", "foreign", "item", "unowned", "wrong-spell" })
        {
            using var w = new Fixture(f, mode != "foundation");
            if (mode == "classic") { w.Caster.AllComps.Remove(ClassIdentity.Component(w.Caster)); w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Necromancer)); }
            if (mode == "foreign") { var other = w.AddCaster(); w.Ability = other.Item4; }
            if (mode == "item") { w.Magic.AbilityData.Powers.Clear(); w.Magic.AbilityData.TemporaryWeaponPowers.Add(w.Ability); }
            if (mode == "unowned") { w.Ability = new PawnAbility(w.Magic); Set(w.Ability, "powerDef", TorannMagicDefOf.TM_DeathMark); }
            if (mode == "wrong-spell") Set(w.Ability, "powerDef", TorannMagicDefOf.TM_AdvancedHeal);
            w.Apply(); w.Die(); w.AssertComplete(false, mode + " cannot earn trial");
        }
    }

    static void RankThree(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var power = w.Magic.MagicData.MagicPowersN.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_DeathMark));
        power.level = 3; Set(w.Ability, "powerDef", TorannMagicDefOf.TM_DeathMark_III);
        w.Magic.MagicData.MagicPowerSkill_DeathMark[0].level = 2;
        Check.Same(TorannMagicDefOf.TM_DeathMark_III, power.abilityDef, "fixture uses upgraded current rank");
        w.Apply(); w.Die(); w.AssertComplete(true, "owned upgraded mark completes trial");
        Check.Equal(3, power.level, "spell rank preserved");
        Check.Equal(2, w.Magic.MagicData.MagicPowerSkill_DeathMark[0].level, "skill investment preserved");
    }

    static void Targets(RuntimeFixture f)
    {
        foreach (bool atDeath in new[] { false, true })
        foreach (string mode in new[] { "prisoner", "slave", "ally", "factionless" })
        {
            using var w = new Fixture(f);
            if (atDeath) w.Apply();
            if (mode == "prisoner") w.Target.guest.guestStatusInt = GuestStatus.Prisoner;
            if (mode == "slave") w.Target.guest.guestStatusInt = GuestStatus.Slave;
            if (mode == "ally") Set(w.Target, "factionInt", w.Caster.Faction);
            if (mode == "factionless") Set(w.Target, "factionInt", null);
            if (!atDeath) w.Apply();
            w.Die(); w.AssertComplete(false, mode + " excluded at " + (atDeath ? "death" : "cast"));
        }
    }

    static void Expiry(RuntimeFixture f)
    {
        foreach (string mode in new[] { "expired", "removed-before-cast", "removed-before-death", "zero-curse", "zero-application", "nan-application" })
        {
            using var w = new Fixture(f);
            if (mode == "removed-before-cast") w.Target.health.hediffSet.hediffs.Clear();
            w.Apply(mode == "zero-application" ? 0 : mode == "nan-application" ? float.NaN : 8f);
            if (mode == "expired") w.Timer.ticksToDisappear = 0;
            if (mode == "removed-before-death") w.Target.health.hediffSet.hediffs.Clear();
            if (mode == "zero-curse") Set(w.Curse, "severityInt", 0f);
            w.Die(); w.AssertComplete(false, mode + " cannot complete trial");
        }
    }

    static void Attribution(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var other = w.AddCaster(); w.Apply(1f);
        Set(Find.TickManager, "ticksGameInt", 1000);
        NecromancerDeathMarkTrial.Applied(other.Item1, w.Target, other.Item4, 8f);
        Set(Find.TickManager, "ticksGameInt", 1250); w.Die();
        w.AssertComplete(true, "first caster remains author while concrete curse is active");
        Check.Equal(true, other.Item3.CompletedTrials.Contains(NecromancerDeathMarkTrial.TrialId), "later caster receives own credit");
        Check.Equal(2000, w.Timer.ticksToDisappear, "recording authors never refreshes native timer");
    }

    static void ActiveAndReplacement(RuntimeFixture f)
    {
        using (var w = new Fixture(f))
        {
            Set(w.Curse, "severityInt", 0.001f); w.Apply(0.001f);
            Set(Find.TickManager, "ticksGameInt", 1500); w.Timer.ticksToDisappear = 500;
            Check.Equal(false, w.Curse.ShouldRemove, "low severity curse remains active under native timer");
            w.Die(); w.AssertComplete(true, "severity-derived deadline does not expire an active curse");
        }
        using (var w = new Fixture(f))
        {
            w.Apply(); var stale = w.Tracker;
            w.Target.health.hediffSet.hediffs.Remove(w.Curse);
            var replacement = new HediffWithComps { pawn = w.Target, def = w.Curse.def };
            var tracker = new HediffComp_DeathMarkTrial { parent = replacement, props = new HediffCompProperties_DeathMarkTrial() };
            var timer = new HediffComp_Disappears { parent = replacement, props = new HediffCompProperties_Disappears(), ticksToDisappear = 2000 };
            replacement.comps = [timer, tracker]; Set(replacement, "severityInt", 8f); w.Target.health.hediffSet.hediffs.Add(replacement);
            Set(w.Target.health, "healthState", PawnHealthState.Dead);
            stale.Notify_PawnDied(null); tracker.Notify_PawnDied(null);
            w.AssertComplete(false, "removed curse authors do not carry over to a fresh unowned mark");
        }
    }

    public sealed class TrackerEnvelope : IExposable
    {
        readonly HediffComp_DeathMarkTrial tracker;
        public TrackerEnvelope(HediffComp_DeathMarkTrial tracker) { this.tracker = tracker; }
        public void ExposeData() => tracker.CompExposeData();
    }

    static void Saved(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var other = w.AddCaster(); w.Apply(1f);
        NecromancerDeathMarkTrial.Applied(other.Item1, w.Target, other.Item4, 8f);
        string file = System.IO.Path.Combine(f.WorkingDirectory, "death-mark-sources.xml");
        TrackerEnvelope? envelope = new(w.Tracker);
        try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref envelope, "mark", w.Tracker); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        w.Tracker = new HediffComp_DeathMarkTrial { parent = w.Curse, props = new HediffCompProperties_DeathMarkTrial() }; w.Curse.comps = [w.Timer, w.Tracker]; envelope = null;
        try
        {
            Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref envelope, "mark", w.Tracker);
            var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
            directory.RegisterLoaded(w.Caster); directory.RegisterLoaded(other.Item1); Scribe.loader.FinalizeLoading();
        }
        finally { Scribe.ForceStop(); }
        Set(Find.TickManager, "ticksGameInt", 1250); w.Die();
        w.AssertComplete(true, "native Scribe restores first caster reference without an artificial severity deadline");
        Check.Equal(true, other.Item3.CompletedTrials.Contains(NecromancerDeathMarkTrial.TrialId), "native Scribe restores second actual caster reference");
    }
}
