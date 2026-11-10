using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthCommandTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in CastSafetyTests.SpiritCommandCases()) yield return test;
        yield return new("SpiritRebirthCommand.CachedCommandsAndStaleExecution", Cached);
        yield return new("SpiritRebirthCommand.DirectActionsPreserveTraitsAndLinks", Direct);
    }
    internal static IDisposable Patches()
    {
        var patches = new FleshStatTests.ProductionPatches();
        foreach (string name in new[] { "SpiritRebirthAbilityVisibilityPatch", "SpiritRebirthCommandCachePatch", "SpiritRebirthToggleExecutionPatch", "SpiritRebirthDirectActionPatch" })
            SpiritRebirthActionTests.Patch(patches, name);
        return patches;
    }
    sealed class TraitDefinitions : IDisposable
    {
        readonly Dictionary<TraitDef, List<TraitDegreeData>> previous = new();
        public TraitDefinitions()
        {
            foreach (var def in new[] { TorannMagicDefOf.TM_Gifted, TorannMagicDefOf.PhysicalProdigy, TorannMagicDefOf.TM_Wanderer, TorannMagicDefOf.TM_Wayfarer })
            {
                previous[def] = def.degreeDatas;
                def.degreeDatas = [new TraitDegreeData { degree = 0, label = def.defName }];
            }
        }
        public void Dispose() { foreach (var entry in previous) entry.Key.degreeDatas = entry.Value; }
    }
    static void Cached(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r);
        using var patches = Patches();
        var toggles = new (object comp, string method, string field)[] {
            (f.Magic, "ToggleTechnoBit", "useTechnoBitToggle"), (f.Magic, "ToggleTechnoBitRepair", "useTechnoBitRepairToggle"),
            (f.Magic, "ToggleElementalShot", "useElementalShotToggle"), (f.Might, "ToggleCleave", "useCleaveToggle"),
            (f.Might, "ToggleCQC", "useCQCToggle"), (f.Might, "TogglePsionicAugmentation", "usePsionicAugmentationToggle"),
            (f.Might, "TogglePsionicMindAttack", "usePsionicMindAttackToggle") };
        var magicCache = (Dictionary<string, Command>)typeof(CompAbilityUserMagic).GetField("gizmoCommands", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Magic)!;
        var mightCache = (Dictionary<string, Command>)typeof(CompAbilityUserMight).GetField("gizmoCommands", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Might)!;
        var stale = new Command_Toggle { toggleAction = f.Magic.ToggleTechnoBit };
        magicCache["technoBit"] = stale;
        var other = new Command_Toggle { toggleAction = f.Might.ToggleCleave }; mightCache["cleave"] = other;
        Check.Same(stale, f.Magic.GetGizmoCommands("technoBit"), "unmarked cache lookup unchanged");
        Check.Same(other, f.Might.GetGizmoCommands("cleave"), "unmarked fighter cache lookup unchanged");
        f.Magic.ToggleTechnoBit(); Check.Equal(false, f.Magic.useTechnoBitToggle, "native unmarked toggle executes");
        f.Magic.ToggleTechnoBit();
        var minion = f.Other; f.Magic.summonedMinions.Add(minion);
        var minions = f.Magic.summonedMinions; var data = f.Magic.MagicData; var mightData = f.Might.MightData;
        data.MagicUserLevel = 9; data.MagicAbilityPoints = 7;
        f.Begin();
        Check.Equal(true, f.Magic.GetGizmoCommands("technoBit") == null, "marked cached magic command hidden");
        Check.Equal(true, f.Might.GetGizmoCommands("cleave") == null, "marked cached fighter command hidden");
        Check.Equal(true, f.Magic.GetGizmoCommands("unknown") == null, "unknown key remains absent");
        foreach (var item in toggles)
        {
            var field = item.comp.GetType().GetField(item.field)!; var before = field.GetValue(item.comp);
            item.comp.GetType().GetMethod(item.method)!.Invoke(item.comp, null);
            Check.Equal(before, field.GetValue(item.comp), item.method + " direct native invocation rejected");
        }
        stale.toggleAction(); Check.Equal(true, f.Magic.useTechnoBitToggle, "stale delegate rechecks marker at execution");
        Check.Same(stale, magicCache["technoBit"], "cache retained");
        Check.Same(minions, f.Magic.summonedMinions, "minion owner list retained");
        Check.Same(minion, minions.Single(), "existing minion not dismissed");
        Check.Same(data, f.Magic.MagicData, "magic data retained"); Check.Same(mightData, f.Might.MightData, "might data retained");
        Check.Equal(9, data.MagicUserLevel, "saved level retained"); Check.Equal(7, data.MagicAbilityPoints, "saved points retained");
        var hostMagic = f.Host.AllComps.OfType<CompAbilityUserMagic>().Single();
        hostMagic.ToggleTechnoBit(); Check.Equal(false, hostMagic.useTechnoBitToggle, "unmarked host direct toggle unchanged");
    }
    static void Direct(RuntimeFixture r)
    {
        using var f = new SpiritRebirthStateTests.Fixture(r);
        using var patches = Patches();
        using var traitsScope = new TraitDefinitions();
        var gifted = new Trait(TorannMagicDefOf.TM_Gifted); var prodigy = new Trait(TorannMagicDefOf.PhysicalProdigy);
        f.Original.story.traits.allTraits.Add(gifted); f.Original.story.traits.allTraits.Add(prodigy);
        var outOfBody = new HediffWithComps { pawn = f.Original, def = TorannMagicDefOf.TM_OutOfBodyHD };
        var hostEffect = new HediffWithComps { pawn = f.Host, def = TorannMagicDefOf.TM_SymbiosisHD };
        outOfBody.comps = [new HediffComp_SymbiosisCaster { parent = outOfBody, symbiosisHost = f.Host }];
        hostEffect.comps = [new HediffComp_SymbiosisHost { parent = hostEffect, symbiote = f.Original }];
        f.Original.health.hediffSet.hediffs.Add(outOfBody); f.Host.health.hediffSet.hediffs.Add(hostEffect);
        var traits = f.Original.story.traits.allTraits.ToArray(); f.Begin();
        TM_Action.PromoteWanderer(f.Original);
        Check.Equal(true, traits.SequenceEqual(f.Original.story.traits.allTraits), "direct Wanderer promotion cannot rewrite marked identity");
        TM_Action.PromoteWayfarer(f.Original);
        Check.Equal(true, traits.SequenceEqual(f.Original.story.traits.allTraits), "direct Wayfarer promotion cannot rewrite marked identity");
        TM_Action.RemoveSymbiosisCommand(f.Original);
        Check.Same(outOfBody, f.Original.health.hediffSet.hediffs.Single(), "marked original linked effect retained");
        Check.Same(hostEffect, f.Host.health.hediffSet.hediffs.Single(), "other pawn linked effect retained");
        f.Host.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Gifted));
        f.Host.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.PhysicalProdigy));
        TM_Action.PromoteWanderer(f.Host); TM_Action.PromoteWayfarer(f.Host);
        Check.Equal(true, f.Host.story.traits.HasTrait(TorannMagicDefOf.TM_Wanderer), "unmarked host Wanderer promotion unchanged");
        Check.Equal(true, f.Host.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer), "unmarked host Wayfarer promotion unchanged");
    }
}

internal static partial class CastSafetyTests
{
    public static IEnumerable<TestCase> SpiritCommandCases()
    {
        yield return new("SpiritRebirthCommand.NativeAbilityVisibilityAndEmptyEnergy", r => World(r, w =>
        {
            using var definitions = new SpiritCastingDefinitions(r);
            using var patches = SpiritRebirthCommandTests.Patches();
            var magic = new CompAbilityUserMagic { parent = w.Caster }; w.Caster.AllComps.Add(magic);
            var ability = new MagicAbility(magic) { Def = w.Def };
            ability.CooldownTicksLeft = 317;
            Check.Equal(true, ability.ShouldShowGizmo(), "native unmarked ability visible");
            MarkSpirit(w.Caster);
            Check.Equal(false, ability.ShouldShowGizmo(), "former ability hidden for marked original");
            ability.Def = TorannMagicDefOf.TM_SpiritDrain;
            Check.Equal(true, ability.ShouldShowGizmo(), "native Spirit family remains visible");
            w.Caster.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND).CurLevel = 0f;
            Check.Equal(true, ability.ShouldShowGizmo(), "zero energy does not hide Spirit command");
            Check.Equal(false, ability.CanCastPowerCheck(AbilityContext.Player, out _), "native command remains disabled at zero energy");
            Check.Equal(317, ability.CooldownTicksLeft, "visibility/failed admission retain runtime cooldown");
            var spoof = Blank<TMAbilityDef>(); spoof.defName = ability.Def.defName; ability.Def = spoof;
            Check.Equal(false, ability.ShouldShowGizmo(), "same-name spoof cannot appear as Spirit family");
            var hostMagic = new CompAbilityUserMagic { parent = w.Target }; w.Target.AllComps.Add(hostMagic);
            Check.Equal(true, new MagicAbility(hostMagic) { Def = w.Def }.ShouldShowGizmo(), "host former ability remains visible");
        }));
    }
}
