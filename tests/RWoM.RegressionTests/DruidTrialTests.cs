using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class DruidTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("DruidTrials.ActualCalmingOnce", Calming);
        yield return new("DruidTrials.RegenerationActualApplicationAndThreshold", Regeneration);
        yield return new("DruidTrials.NativeOwnershipAndTargetBoundaries", Ownership);
        yield return new("DruidTrials.NativeHooksObserveActualEffects", Hooks);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly MentalStateDef previousManhunter = MentalStateDefOf.Manhunter, previousPermanent = MentalStateDefOf.ManhunterPermanent;
        readonly HediffDef previousRegen = TorannMagicDefOf.TM_Regeneration;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public DisciplineProgress Path;
        public PawnAbility Ability;
        public Hediff_Injury Injury;
        public Hediff Regen;
        public Fixture(RuntimeFixture f, bool adept = true)
        {
            LoadBaseDefinitions(f);
            MentalStateDefOf.Manhunter = new MentalStateDef { defName = "DruidTrialManhunter" };
            MentalStateDefOf.ManhunterPermanent = new MentalStateDef { defName = "DruidTrialManhunterPermanent" };
            TorannMagicDefOf.TM_Regeneration = new HediffDef { defName = "DruidTrialRegen", maxSeverity = 10f };
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic;
            Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.guest = new Pawn_GuestTracker(pawn); pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.kindDef = new PawnKindDef { defName = "TrialPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            var comp = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(comp);
            comp.BeginNewProgression(Vocation.Mage); comp.State.TryAddFoundation("rwom.druid", DisciplineDomain.Magic);
            Path = comp.State.Find("rwom.druid");
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TrialDruid", profileId = "rwom.druid", domain = DisciplineDomain.Magic });
            if (adept) {
            Path.RecordCompletedTrial(DruidTrials.FoundationTrialId);
            comp.State.TryAdvanceAfterTrial("rwom.druid", InitiationStage.Foundation, DruidTrials.FoundationTrialId);
            }
            Ability = new PawnAbility(Magic); Set(Ability, "powerDef", TorannMagicDefOf.TM_Regenerate);
            Magic.MagicData.MagicPowersD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Regenerate)).learned = true;
            Ability.CooldownTicksLeft = 91; Magic.AbilityData.Powers.Add(Ability);
            Injury = new Hediff_Injury { pawn = Target, def = new HediffDef { defName = "TrialInjury" } };
            Regen = new Hediff { pawn = Target, def = TorannMagicDefOf.TM_Regeneration };
            Target.health.hediffSet.hediffs.Add(Injury); Target.health.hediffSet.hediffs.Add(Regen);
            Set(Injury, "severityInt", 8f); Set(Regen, "severityInt", 1f);
        }
        public DruidTrials.Snapshot Capture() => DruidTrials.CaptureRegeneration(Ability, Target);
        public void Dispose() { MentalStateDefOf.Manhunter = previousManhunter; MentalStateDefOf.ManhunterPermanent = previousPermanent; TorannMagicDefOf.TM_Regeneration = previousRegen; Current.Game = previous; DefDatabase<ClassDisciplineDef>.Clear(); foreach (var p in profiles) DefDatabase<ClassDisciplineDef>.Add(p); }
    }
    static void Calming(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        Set(w.Ability, "powerDef", TorannMagicDefOf.TM_SootheAnimal);
        w.Magic.MagicData.MagicPowersD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SootheAnimal)).learned = true;
        w.Target.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        w.Target.def.defName = "DruidAnimal"; w.Target.def.race = new RaceProperties { intelligence = Intelligence.Animal };
        Set(w.Target.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
        Set(w.Target, "factionInt", null);
        Check.Same(null!, DruidTrials.CaptureAnimal(w.Ability, w.Target)!, "calm animal cannot earn trial");
        Set(w.Target.mindState.mentalStateHandler, "curStateInt", new Verse.AI.MentalState_Manhunter { pawn = w.Target, def = MentalStateDefOf.Manhunter });
        Check.Equal(false, DruidTrials.Complete(DruidTrials.CaptureAnimal(w.Ability, w.Target)), "unchanged manhunter earns nothing");
        var snapshot = DruidTrials.CaptureAnimal(w.Ability, w.Target);
        // Native Reset requires a full spawned pawn job/render context; prove its observable state boundary headlessly.
        Set(w.Target.mindState.mentalStateHandler, "curStateInt", null);
        Check.Equal(true, DruidTrials.Complete(snapshot), "actual calming qualifies");
        Check.Equal(false, DruidTrials.Complete(snapshot), "one cast cannot repeat");
        Check.Equal(InitiationStage.Foundation, w.Path.Stage, "trial leaves advancement to ritual");
    }
    static void Regeneration(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "fixture at adept");
        Set(w.Injury, "severityInt", 7.99f); Check.Same(null!, w.Capture()!, "eight injury threshold");
        Set(w.Injury, "severityInt", 8f);
        Check.Equal(false, DruidTrials.Complete(w.Capture()), "no effect earns nothing");
        var snapshot = w.Capture(); Check.Equal(true, snapshot != null, "eligible application snapshot exists");
        HealthUtility.AdjustSeverity(w.Target, TorannMagicDefOf.TM_Regeneration, 1f);
        Check.Equal(true, DruidTrials.Complete(snapshot), "actual added regeneration qualifies");
        Check.Equal(false, DruidTrials.Complete(snapshot), "one event only");
        Check.Same(null!, w.Capture()!, "completed trial ignores later effects");
        Check.Equal(InitiationStage.Adept, w.Path.Stage, "no automatic stage advancement");
        Check.Equal(91, w.Ability.CooldownTicksLeft, "cooldown preserved");
    }
    static void Ownership(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        Check.Same(null!, DruidTrials.CaptureRegeneration(w.Ability, w.Caster)!, "self excluded");
        w.Target.guest.guestStatusInt = GuestStatus.Prisoner; Check.Same(null!, w.Capture()!, "prisoner excluded"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        Set(w.Target, "mapIndexOrState", (sbyte)1); Check.Same(null!, w.Capture()!, "cross map excluded"); Set(w.Target, "mapIndexOrState", (sbyte)0);
        w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless)); Check.Same(null!, w.Capture()!, "copied power excluded"); w.Caster.story.traits.allTraits.Clear();
        var learned = w.Magic.MagicData.MagicPowersD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Regenerate));
        learned.learned = false; Check.Same(null!, w.Capture()!, "learned native owner required"); learned.learned = true;
        var stale = w.Capture(); w.Magic.AbilityData.Powers.Clear(); w.Magic.AbilityData.TemporaryWeaponPowers.Add(w.Ability);
        Set(w.Regen, "severityInt", 2f); Check.Equal(false, DruidTrials.Complete(stale), "lost ownership invalidates snapshot");
        Check.Same(null!, w.Capture()!, "temporary item ability excluded");
        Check.Same(null!, DruidTrials.CaptureAnimal(w.Ability, w.Target)!, "wrong spell cannot claim calming");
    }
    static void Hooks(RuntimeFixture f)
    {
        var root = f.Repository;
        Check.Equal(true, File.ReadAllText(Path.Combine(root, "RimWorldOfMagic/RimWorldOfMagic/Verb_SootheAnimal.cs")).Contains("DruidTrials.CaptureAnimal"), "native calming observer present");
        Check.Equal(true, File.ReadAllText(Path.Combine(root, "RimWorldOfMagic/RimWorldOfMagic/Verb_Regenerate.cs")).Contains("DruidTrials.CaptureRegeneration"), "native regeneration observer present");
    }
}







