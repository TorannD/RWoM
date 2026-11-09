using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ShamanLifecycleTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ShamanLifecycle.ReplacementHexNeverInheritsAnotherShamansCommand", runtime =>
        {
            using var f = new Fixture(runtime);
            f.AddTarget();
            var original = f.Target.health.hediffSet.hediffs.Single(h=>h.def==TorannMagicDefOf.TM_HexHD);
            original.loadID=221;
            Check.Same(f.Target,f.Magic.HexedPawns.Single(),"old saves bind to their live effect");
            f.Target.health.hediffSet.hediffs.Remove(original);
            f.Target.health.hediffSet.hediffs.Add(new Hediff { pawn=f.Target,def=original.def,loadID=222 });
            Check.Equal(0,f.Magic.HexedPawns.Count,"foreign replacement does not restore this owner's command");
            typeof(CompAbilityUserMagic).GetMethod("RegisterHexTarget",Fields)!.Invoke(f.Magic,[f.Target]);
            Check.Same(f.Target,f.Magic.HexedPawns.Single(),"new successful application can acquire its own target again");
        });
        yield return new("ShamanLifecycle.RankedInheritedHexControlsFollowRealTargets", HexControls);
        yield return new("ShamanLifecycle.NoOwnerOrTargetsNeverGrantPermanentHexChildren", Empty);
        yield return new("ShamanLifecycle.CopiedAndItemHexControlsRemainUntilActualTargetsEnd", TemporaryOwnership);
        yield return new("ShamanLifecycle.NativeScribePreservesHexGuardianAndSummonerReferences", Save);
        yield return new("ShamanLifecycle.NativeGuardianControlPreservesTypeAndUpkeepSnapshot", Guardian);
        yield return new("ShamanLifecycle.ForeignGuardianCannotBeDismissedOrReplaced", runtime =>
        {
            using var f = new Fixture(runtime);
            f.Magic.bondedSpirit = f.Companion;
            f.Controller.summonerPawn = f.Target;
            var release = typeof(ShamanLifecycle).GetMethod("ReleaseGuardian",BindingFlags.Static | BindingFlags.NonPublic)!;
            Check.Equal(false,(bool)release.Invoke(null,[f.Magic])!,"refuses another owner's guardian");
            Check.Same(f.Companion,f.Magic.bondedSpirit,"refusal preserves the existing link");
            Check.Equal(false,f.Companion.Destroyed,"refusal keeps the actual animal");
        });
        yield return new("ShamanLifecycle.TotemAndHexChildrenUseNativeParentEfficiency", Costs);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
        readonly HediffDef oldHex = TorannMagicDefOf.TM_HexHD;
        readonly HediffDef[] hediffs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly ThingDef[] things = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly List<(TMAbilityDef def, Type type, bool init)> abilities = new();
        public readonly Pawn Caster, Target, Companion;
        public readonly CompAbilityUserMagic Magic;
        public readonly CompAnimalController Controller;
        public readonly ThingDef SpiritType;
        public Fixture(RuntimeFixture runtime, bool modern = true)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            LoadBaseDefinitions(runtime);
            foreach (var a in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { abilities.Add((a, a.abilityClass, a.shouldInitialize)); a.abilityClass = typeof(AbilityUser.PawnAbility); a.shouldInitialize = true; }
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic; Caster.thingIDNumber = 76201;
            Target = PawnWithComps(false, false).pawn; Target.thingIDNumber = 76202;
            Companion = PawnWithComps(false, false).pawn; Companion.thingIDNumber = 76203;
            Controller = new CompAnimalController { parent = Companion, summonerPawn = Caster }; Companion.AllComps.Add(Controller);
            SpiritType = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); SpiritType.defName = "ShamanLifecycleSpirit";
            DefDatabase<ThingDef>.Add(SpiritType);
            var hex = new HediffDef { defName = "TM_HexHD" };
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs.Where(d => d.defName != hex.defName)) DefDatabase<HediffDef>.Add(d);
            DefDatabase<HediffDef>.Add(hex); TorannMagicDefOf.TM_HexHD = hex;
            if (DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null) DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
            if (modern)
            {
                var progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(progression);
                progression.BeginNewProgression(Vocation.Mage);
                progression.State.TryAddFoundation("rwom.fire", DisciplineDomain.Magic); // Explicit inherited owner, no Shaman identity/profile.
            }
        }
        public MagicPower Hex => Magic.MagicData.MagicPowersShaman.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Hex));
        public void AddTarget()
        {
            Target.health.hediffSet.hediffs.Add(new Hediff { def = TorannMagicDefOf.TM_HexHD, pawn = Target });
            Magic.HexedPawns.Add(Target);
        }
        public void Dispose()
        {
            Current.Game = previous; System.Globalization.CultureInfo.CurrentCulture = culture; TorannMagicDefOf.TM_HexHD = oldHex;
            foreach (var a in abilities) { a.def.abilityClass = a.type; a.def.shouldInitialize = a.init; }
            DefDatabase<HediffDef>.Clear(); foreach (var d in hediffs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<ThingDef>.Clear(); foreach (var d in things) DefDatabase<ThingDef>.Add(d);
        }
    }
    static TMAbilityDef[] Controls => [TorannMagicDefOf.TM_Hex_Pain, TorannMagicDefOf.TM_Hex_MentalAssault, TorannMagicDefOf.TM_Hex_CriticalFail];
    static int Count(CompAbilityUserMagic comp, TMAbilityDef def) => comp.AbilityData.Powers.Count(p => p.Def == def);
    static void HexControls(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); f.Hex.learned = true; f.Hex.level = 2; f.AddTarget();
        f.Magic.ResolveClassSkills();
        foreach (var def in Controls) Check.Equal(1, Count(f.Magic, def), "native maintenance repairs ranked inherited control " + def.defName);
        var control = f.Magic.AbilityData.Powers.First(p => p.Def == Controls[0]); control.CooldownTicksLeft = 117;
        ShamanLifecycle.Restore(f.Magic); ShamanLifecycle.Restore(f.Magic);
        Check.Same(f.Target, f.Magic.HexedPawns.Single(), "actual target retained across repeated validation");
        Check.Same(control, f.Magic.AbilityData.Powers.First(p => p.Def == Controls[0]), "control instance retained");
        Check.Equal(117, control.CooldownTicksLeft, "control cooldown retained");
        Check.Equal(2, f.Hex.level, "native Hex rank preserved");
        f.Target.health.hediffSet.hediffs.Clear(); f.Magic.ResolveClassSkills();
        Check.Equal(0, f.Magic.HexedPawns.Count, "native target validation drops ended Hex");
        foreach (var def in Controls) Check.Equal(0, Count(f.Magic, def), "ended effect removes control");
        f.AddTarget(); ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(1, Count(f.Magic, def), "new real crow/native addition repairs control");
    }
    static void Empty(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); f.AddTarget();
        ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(0, Count(f.Magic, def), "real targets without learned owner grant no control");
        Check.Same(f.Target, f.Magic.HexedPawns.Single(), "unlearned owner does not erase saved target");
        f.Hex.learned = true; f.Magic.HexedPawns.Clear(); ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(0, Count(f.Magic, def), "learned owner without effects grants no control");
        Check.Equal(false, f.Magic.MagicData.MagicPowersStandalone.Any(p => p.learned && p.TMabilityDefs.Any(a => Controls.Contains(a))), "temporary controls never gain standalone owners");
        Check.Equal(true, f.Magic.bondedSpirit == null, "restore never summons guardian");
        Check.Equal(true, typeof(CompAbilityUserMagic).GetField("guardianSpiritType", Fields)!.GetValue(f.Magic) == null, "restore never rolls lazy guardian selection");
    }
    public sealed class NativeEnvelope : IExposable
    {
        public static CompAbilityUserMagic Magic = null!;
        public static CompAnimalController Animal = null!;
        public void ExposeData()
        {
            // Exercise actual native serialization/crossrefs. The unrelated full legacy
            // PostLoadInit class reconstruction is replaced by explicit new-model restore.
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            { Scribe.EnterNode("magic"); Magic.PostExposeData(); Scribe.ExitNode(); }
            Scribe.EnterNode("animal"); Animal.PostExposeData(); Scribe.ExitNode();
        }
    }
    static void TemporaryOwnership(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); f.AddTarget();
        // Native Verb_Hex grants controls even when its source was an item/copy.
        // A surviving control is evidence of that native effect, not a learned owner.
        f.Magic.AddPawnAbility(TorannMagicDefOf.TM_Hex_Pain);
        ShamanLifecycle.Restore(f.Magic); f.Magic.ResolveClassSkills();
        foreach (var def in Controls) Check.Equal(1, Count(f.Magic, def), "native item effect controls preserved/repaired");
        Check.Equal(false, f.Hex.learned, "item effect never becomes permanent Hex ownership");
        f.Target.health.hediffSet.hediffs.Clear(); ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(0, Count(f.Magic, def), "item controls end with actual targets");
        f.AddTarget(); f.Magic.AddPawnAbility(TorannMagicDefOf.TM_Hex_II);
        ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(1, Count(f.Magic, def), "actual copied ranked Hex repairs real effect controls");
        Check.Equal(false, f.Hex.learned, "runtime copy remains unlearned");
        f.Magic.RemovePawnAbility(TorannMagicDefOf.TM_Hex_II); ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(1, Count(f.Magic, def), "ongoing real effect controls survive source expiry");
        f.Target.health.hediffSet.hediffs.Clear(); ShamanLifecycle.Restore(f.Magic);
        foreach (var def in Controls) Check.Equal(0, Count(f.Magic, def), "copied controls end with actual targets");
    }
    static void Save(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); f.Hex.learned = true; f.Hex.level = 3; f.AddTarget();
        f.Magic.bondedSpirit = f.Companion; Set(f.Magic, "guardianSpiritType", f.SpiritType); f.Magic.maxMP = .68f;
        NativeEnvelope.Magic = f.Magic; NativeEnvelope.Animal = f.Controller;
        string path = Path.Combine(runtime.WorkingDirectory, "shaman-native-refs.xml"); var envelope = new NativeEnvelope();
        try
        {
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "native"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            f.Magic.HexedPawns.Clear(); f.Magic.bondedSpirit = null; Set(f.Magic, "guardianSpiritType", null); f.Controller.summonerPawn = null;
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "native");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", Fields)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(f.Caster); directory.RegisterLoaded(f.Target); directory.RegisterLoaded(f.Companion);
                Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            RuntimeLog.AssertClean();
            Check.Same(f.Target, f.Magic.HexedPawns.Single(), "native Hex list resolves exact target");
            Check.Same(f.Companion, f.Magic.bondedSpirit, "native guardian reference resolves");
            Check.Same(f.Caster, f.Controller.summonerPawn, "native animal summoner reference resolves");
            Check.Same(f.SpiritType, f.Magic.GuardianSpiritType, "saved chosen type not rerolled");
            ShamanLifecycle.Restore(f.Magic); f.Magic.ResolveSustainers();
            Check.Equal(3, f.Hex.level, "saved native ranked owner restored");
            foreach (var def in Controls) Check.Equal(1, Count(f.Magic, def), "saved actual Hex controls restored once");
            Check.Equal(.68f, f.Magic.maxMP, "existing saved upkeep-adjusted capacity retained");
        }
        finally { NativeEnvelope.Magic = null!; NativeEnvelope.Animal = null!; }
    }
    static void Guardian(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime); f.Magic.bondedSpirit = f.Companion; Set(f.Magic, "guardianSpiritType", f.SpiritType);
        f.Magic.maxMP = .73f; f.Magic.mpRegenRate = .81f;
        for (int i = 0; i < 2; i++) { ShamanLifecycle.Restore(f.Magic); f.Magic.ResolveSustainers(); }
        Check.Same(f.Companion, f.Magic.bondedSpirit, "restoration preserves existing real guardian");
        Check.Same(f.Caster, f.Controller.summonerPawn, "restoration preserves native summoner link");
        Check.Same(f.SpiritType, f.Magic.GuardianSpiritType, "restoration preserves selected spirit type");
        Check.Equal(.73f, f.Magic.maxMP, "restoration leaves computed upkeep capacity intact");
        Check.Equal(.81f, f.Magic.mpRegenRate, "restoration leaves computed upkeep regeneration intact");
        Check.Equal(1, Count(f.Magic, TorannMagicDefOf.TM_DismissGuardianSpirit), "real native guardian control appears once");
        f.Magic.bondedSpirit = null; f.Magic.ResolveSustainers();
        Check.Equal(0, Count(f.Magic, TorannMagicDefOf.TM_DismissGuardianSpirit), "native guardian control ends with actual reference");
    }
    static void Costs(RuntimeFixture runtime)
    {
        using var f = new Fixture(runtime);
        f.Magic.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Totems).level = 3;
        f.Magic.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_Hex).level = 2;
        foreach (var def in new[] { TorannMagicDefOf.TM_SummonTotemEarth, TorannMagicDefOf.TM_SummonTotemHealing, TorannMagicDefOf.TM_SummonTotemLightning }.Concat(Controls))
        {
            float mana = def.manaCost, efficiency = def.efficiencyReductionPercent;
            try
            {
                def.manaCost = .2f; def.efficiencyReductionPercent = .05f;
                int level = Controls.Contains(def) ? 2 : 3;
                Check.Equal(.2f * (1f - .05f * level), f.Magic.ActualManaCost(def), "native child shares parent efficiency " + def.defName);
            }
            finally { def.manaCost = mana; def.efficiencyReductionPercent = efficiency; }
        }
    }
}
