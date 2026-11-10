using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;
internal static class TechnomancerTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("TechnomancerTrial.BitActualRepairAndOwnership", Bit);
        yield return new("TechnomancerTrial.ShieldDeltaAndOwnership", Shield);
        yield return new("TechnomancerTrial.StageDriftAndOwnerLoss", Drift);
        yield return new("TechnomancerTrial.FreshShieldAndRecipientBoundaries", FreshShield);
        yield return new("TechnomancerTrial.NativeShieldMutation", NativeShield);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly StatDef hp = StatDefOf.MaxHitPoints;
        readonly HediffDef[] shieldDefs = { TorannMagicDefOf.TM_TechnoShieldHD, TorannMagicDefOf.TM_TechnoShieldHD_I, TorannMagicDefOf.TM_TechnoShieldHD_II, TorannMagicDefOf.TM_TechnoShieldHD_III };
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public DisciplineProgress Path => Progression.State.Find("rwom.technomancer");
        public Fixture(RuntimeFixture f)
        {
            LoadBaseDefinitions(f);
            TorannMagicDefOf.TM_TechnoShieldHD = new HediffDef { defName = "TM_TechnoShieldHD", maxSeverity = 1000f };
            TorannMagicDefOf.TM_TechnoShieldHD_I = new HediffDef { defName = "TM_TechnoShieldHD_I", maxSeverity = 1000f };
            TorannMagicDefOf.TM_TechnoShieldHD_II = new HediffDef { defName = "TM_TechnoShieldHD_II", maxSeverity = 1000f };
            TorannMagicDefOf.TM_TechnoShieldHD_III = new HediffDef { defName = "TM_TechnoShieldHD_III", maxSeverity = 1000f };
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)), (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            foreach (var map in Current.Game.Maps) map.events = new MapEvents(map);
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic; Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.guest = new Pawn_GuestTracker(pawn); pawn.kindDef = new PawnKindDef { defName = "TechnomancerTrialPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.technomancer", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "TechnoTrialProfile", profileId = "rwom.technomancer", domain = DisciplineDomain.Magic });
            StatDefOf.MaxHitPoints = FleshStatTests.Stat("TechnoTrialHitPoints", 100);
        }
        public MagicPower Owner(TMAbilityDef def) => Magic.MagicData.MagicPowersT.Single(p => p.TMabilityDefs.Contains(def));
        public PawnAbility Ability(bool owned = true)
        {
            Owner(TorannMagicDefOf.TM_TechnoShield).learned = true;
            var ability = new PawnAbility(Magic); Set(ability, "powerDef", TorannMagicDefOf.TM_TechnoShield); ability.CooldownTicksLeft = 91;
            if (owned) Magic.AbilityData.Powers.Add(ability); return ability;
        }
        public void Adept() { Path.RecordCompletedTrial(TechnomancerTrials.FoundationTrialId); Progression.State.TryAdvanceAfterTrial("rwom.technomancer", InitiationStage.Foundation, TechnomancerTrials.FoundationTrialId); }
        public HediffComp Bit()
        {
            Owner(TorannMagicDefOf.TM_TechnoBit).learned = true; Magic.useTechnoBitToggle = true; Magic.useTechnoBitRepairToggle = true;
            var source = (HediffComp)Activator.CreateInstance(typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.HediffComp_TechnoBit")!, true)!;
            var hd = new HediffWithComps { pawn = Caster, def = new HediffDef { defName = "TM_TechnoBitHD" }, comps = new List<HediffComp> { source } };
            source.parent = hd; Caster.health.hediffSet.hediffs.Add(hd); return source;
        }
        public Building Building()
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.defName = "TrialBuilding"; def.category = ThingCategory.Building;
            var building = new Building { def = def }; Set(building, "factionInt", Caster.Faction); Set(building, "mapIndexOrState", (sbyte)0); building.HitPoints = 30; return building;
        }
        public Hediff Ward(float severity)
        {
            var hd = new Hediff { pawn = Target, def = TorannMagicDefOf.TM_TechnoShieldHD }; Set(hd, "severityInt", severity); Target.health.hediffSet.hediffs.Add(hd); return hd;
        }
        public void Dispose() { TorannMagicDefOf.TM_TechnoShieldHD = shieldDefs[0]; TorannMagicDefOf.TM_TechnoShieldHD_I = shieldDefs[1]; TorannMagicDefOf.TM_TechnoShieldHD_II = shieldDefs[2]; TorannMagicDefOf.TM_TechnoShieldHD_III = shieldDefs[3]; StatDefOf.MaxHitPoints = hp; Current.Game = previous; DefDatabase<ClassDisciplineDef>.Clear(); foreach (var def in profiles) DefDatabase<ClassDisciplineDef>.Add(def); }
    }
    static void Bit(RuntimeFixture f)
    {
        using var w = new Fixture(f); var source = w.Bit(); var building = w.Building();
        Check.Equal(false, TechnomancerTrials.CompleteRepair(TechnomancerTrials.CaptureRepair(source, building)), "no HP delta gives no credit");
        w.Magic.useTechnoBitRepairToggle = false; Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "disabled repair excluded"); w.Magic.useTechnoBitRepairToggle = true;
        Set(building, "mapIndexOrState", (sbyte)1); Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "other map excluded"); Set(building, "mapIndexOrState", (sbyte)0);
        w.Caster.health.hediffSet.hediffs.Remove(source.parent); Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "detached bit rejected"); w.Caster.health.hediffSet.hediffs.Add(source.parent);
        building.HitPoints = 100; Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "full building excluded"); building.HitPoints = 30;
        Set(building, "factionInt", new Faction()); Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "foreign building excluded"); Set(building, "factionInt", w.Caster.Faction);
        w.Owner(TorannMagicDefOf.TM_TechnoBit).learned = false; Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "unlearned passive excluded"); w.Owner(TorannMagicDefOf.TM_TechnoBit).learned = true;
        var before = TechnomancerTrials.CaptureRepair(source, building); building.HitPoints += 10;
        Check.Equal(true, TechnomancerTrials.CompleteRepair(before), "actual native bit repair earns foundation"); Check.Equal(false, TechnomancerTrials.CompleteRepair(before), "single use");
        Check.Equal(true, TechnomancerTrials.CaptureRepair(source, building) == null, "completed trial cannot repeat"); Check.Equal(InitiationStage.Foundation, w.Path.Stage, "still requires ritual");
    }
    static void Shield(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability(false);
        Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "copy/item instance rejected"); w.Magic.AbilityData.Powers.Add(ability);
        Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Caster) == null, "self rejected");
        w.Target.guest.guestStatusInt = GuestStatus.Prisoner; Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "prisoner rejected"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        w.Caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless)); Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "Faceless with learned owner rejected"); w.Caster.story.traits.allTraits.Clear();
        var ward = w.Ward(4); Check.Equal(false, TechnomancerTrials.CompleteShield(TechnomancerTrials.CaptureShield(ability, w.Target)), "old unchanged shield gives nothing");
        var before = TechnomancerTrials.CaptureShield(ability, w.Target); HealthUtility.AdjustSeverity(w.Target, ward.def, 3f);
        Check.Equal(true, TechnomancerTrials.CompleteShield(before), "real strengthening qualifies"); Check.Equal(false, TechnomancerTrials.CompleteShield(before), "single use"); Check.Equal(91, ability.CooldownTicksLeft, "cooldown unchanged"); Check.Equal(InitiationStage.Adept, w.Path.Stage, "no automatic advancement");
    }
    static void Drift(RuntimeFixture f)
    {
        using var w = new Fixture(f); var bit = w.Bit(); var b = w.Building(); var repair = TechnomancerTrials.CaptureRepair(bit, b); b.HitPoints += 10; w.Adept();
        Check.Equal(false, TechnomancerTrials.CompleteRepair(repair), "stage drift rejects repair");
        var ability = w.Ability(); var shield = TechnomancerTrials.CaptureShield(ability, w.Target); w.Ward(5); w.Magic.AbilityData.Powers.Remove(ability);
        Check.Equal(false, TechnomancerTrials.CompleteShield(shield), "runtime ownership removed during effect");
        w.Magic.AbilityData.Powers.Add(ability); shield = TechnomancerTrials.CaptureShield(ability, w.Target); Set(w.Target.health.hediffSet.hediffs.Last(), "severityInt", 8f); w.Owner(TorannMagicDefOf.TM_TechnoShield).learned = false;
        Check.Equal(false, TechnomancerTrials.CompleteShield(shield), "native owner unlearned during effect");
    }
    static void NativeShield(RuntimeFixture f)
    {
        foreach (var rank in new[] { 0, 1, 2, 3 })
        {
            using var w = new Fixture(f); w.Adept(); var ability = w.Ability();
            var ward = w.Ward(4); ward.def = new[] { TorannMagicDefOf.TM_TechnoShieldHD, TorannMagicDefOf.TM_TechnoShieldHD_I, TorannMagicDefOf.TM_TechnoShieldHD_II, TorannMagicDefOf.TM_TechnoShieldHD_III }[rank];
            var verb = new Verb_TechnoShield { Ability = ability }; Set(verb, "caster", w.Caster); Set(verb, "comp", w.Magic); Set(verb, "pwrVal", rank);
            var apply = typeof(Verb_TechnoShield).GetMethod("ApplyHediffs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            w.Magic.arcaneDmg = 0; apply.Invoke(verb, new object[] { w.Target });
            Check.Equal(false, w.Path.CompletedTrials.Contains(TechnomancerTrials.MasteryTrialId), "native zero-effect mutation rejected");
            w.Magic.arcaneDmg = 1; apply.Invoke(verb, new object[] { w.Target });
            Check.Equal(114f, ward.Severity, "native shield amount preserved for every rank");
            Check.Equal(true, w.Path.CompletedTrials.Contains(TechnomancerTrials.MasteryTrialId), "native mutation observes exact owned source");
        }
    }
    static void FreshShield(RuntimeFixture f)
    {
        using var w = new Fixture(f); w.Adept(); var ability = w.Ability();
        Set(w.Target, "mapIndexOrState", (sbyte)1); Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "cross-map ally excluded"); Set(w.Target, "mapIndexOrState", (sbyte)0);
        Set(w.Target, "factionInt", new Faction()); Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "foreign human excluded"); Set(w.Target, "factionInt", w.Caster.Faction);
        w.Target.guest.guestStatusInt = GuestStatus.Slave; Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "slave excluded"); w.Target.guest.guestStatusInt = GuestStatus.Guest;
        w.Magic.AbilityData.Powers.Remove(ability); w.Magic.AbilityData.TemporaryWeaponPowers.Add(ability);
        Check.Equal(true, TechnomancerTrials.CaptureShield(ability, w.Target) == null, "actual temporary weapon collection excluded");
        w.Magic.AbilityData.TemporaryWeaponPowers.Remove(ability); w.Magic.AbilityData.Powers.Add(ability);
        var before = TechnomancerTrials.CaptureShield(ability, w.Target); var ward = w.Ward(float.NaN);
        Check.Equal(false, TechnomancerTrials.CompleteShield(before), "invalid resulting severity rejected"); w.Target.health.hediffSet.hediffs.Remove(ward);
        before = TechnomancerTrials.CaptureShield(ability, w.Target); w.Ward(10);
        Check.Equal(true, TechnomancerTrials.CompleteShield(before), "new shield is an actual positive effect");
    }
}
