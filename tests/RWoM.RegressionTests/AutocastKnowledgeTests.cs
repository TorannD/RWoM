using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityDef = AbilityUser.AbilityDef;
using AutocastPolicy = TorannMagic.AutoCast.AutocastPolicy;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// Ф4 step 3: the autocast dispatch gates powers through the knowledge store.
// A knowledge entry overrides the legacy learned/autocast flags; without an
// entry (store absent or import not run) legacy flags decide, so classic pawns
// are unchanged. The core registry resolves convention ids through the bridge;
// unknown ids resolve false instead of throwing.
internal static class AutocastKnowledgeTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("AutocastKnowledge.GateOverridesLegacyFlags", GateOverridesLegacyFlags);
        yield return new("AutocastKnowledge.GateWithoutEntryFallsBackLegacy", GateWithoutEntryFallsBackLegacy);
        yield return new("AutocastKnowledge.GateNullSafe", GateNullSafe);
        yield return new("AutocastKnowledge.SpellIdConvention", SpellIdConvention);
        yield return new("AutocastKnowledge.CoreRegistryRoundTrip", CoreRegistryRoundTrip);
        yield return new("AutocastKnowledge.KnowledgeFilterAppliesGate", KnowledgeFilterAppliesGate);
    }

    private static TMAbilityDef Ability(string name)
    {
        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = name;
        def.label = name;
        def.MainVerb = new AbilityUser.VerbProperties_Ability();
        return def;
    }

    private static MagicPower Power(TMAbilityDef def, bool learned, bool autocast)
        => new MagicPower(new List<AbilityDef> { def }) { learned = learned, autocast = autocast };

    // RED 1: a knowledge entry wins over the legacy flags in both directions.
    private static void GateOverridesLegacyFlags(RuntimeFixture _)
    {
        var def = Ability("TM_KnowledgeGateA");
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };

        // Legacy says on, the store says off — the store wins.
        var off = knowledge.GetOrCreate(SpellIds.Of(def), 0);
        off.learned = true;
        off.autocast = false;
        Check.Equal(false, AutocastPolicy.EnabledForAutocast(Power(def, true, true), knowledge),
            "knowledge autocast=false suppresses a legacy-enabled power");

        // Legacy says off, the store says on — the store wins.
        var defB = Ability("TM_KnowledgeGateB");
        var on = knowledge.GetOrCreate(SpellIds.Of(defB), 0);
        on.learned = true;
        on.autocast = true;
        Check.Equal(true, AutocastPolicy.EnabledForAutocast(Power(defB, true, false), knowledge),
            "knowledge autocast=true enables a legacy-disabled power");
    }

    // RED 2: no store or no entry — the legacy flags decide unchanged.
    private static void GateWithoutEntryFallsBackLegacy(RuntimeFixture _)
    {
        var defOn = Ability("TM_KnowledgeGateC");
        var defOff = Ability("TM_KnowledgeGateD");
        Check.Equal(true, AutocastPolicy.EnabledForAutocast(Power(defOn, true, true), null),
            "null store falls back to legacy flags");
        Check.Equal(false, AutocastPolicy.EnabledForAutocast(Power(defOn, true, false), null),
            "null store keeps legacy autocast=false off");

        var emptyStore = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        Check.Equal(true, AutocastPolicy.EnabledForAutocast(Power(defOff, true, true), emptyStore),
            "store without an entry falls back to legacy flags");
        Check.Equal(false, AutocastPolicy.EnabledForAutocast(Power(defOff, false, true), emptyStore),
            "store without an entry keeps legacy learned=false off");
    }

    // RED 3: missing power/def never throws.
    private static void GateNullSafe(RuntimeFixture _)
    {
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        Check.Equal(false, AutocastPolicy.EnabledForAutocast((MagicPower)null, knowledge), "null power is gated off");
        // Note: the MagicPower(List) ctor NREs on an empty list (legacy landmine);
        // saved sanitized-empty powers are built by Scribe, not that ctor.
        Check.Equal(false, AutocastPolicy.EnabledForAutocast(
            new MagicPower { learned = true, autocast = true, TMabilityDefs = new List<AbilityDef>() }, knowledge),
            "power with an empty def list is gated off");
    }

    // RED 4: the convention is one shared form — "rwom." + lower-case defName —
    // used identically by the legacy import and the gate lookups.
    private static void SpellIdConvention(RuntimeFixture _)
    {
        var def = Ability("TM_KnowledgeGateE");
        Check.Equal("rwom.tm_knowledgegatee", SpellIds.Of(def), "spellId convention is rwom.+lower defName");
        Check.Equal(true, SpellIds.Of(null) == null, "null def resolves to null id");

        // A store entry created the way ImportLegacy does is findable by the gate.
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        knowledge.GetOrCreate(SpellIds.Of(def), 0);
        Check.NotNull(knowledge.Find(SpellIds.Of(def)), "gate lookup finds the imported entry id");
    }

    // RED 5: the core registry resolves convention ids through the bridge and
    // returns false (not throw) for unknown ids; an explicit extension spellId
    // registers under its own id.
    private static void CoreRegistryRoundTrip(RuntimeFixture f)
    {
        var coreDll = CoreSdkTests.AssemblyPath(f.Repository);
        if (!File.Exists(coreDll))
        {
            Check.Equal(false, CoreBridge.Available, "core assembly absent — bridge degrades to a no-op");
            return;
        }
        Assembly.LoadFrom(coreDll);
        Check.Equal(true, CoreBridge.Available, "core bridge resolves the loaded core assembly");

        var def = Ability("TM_KnowledgeGateF");
        CoreBridge.RegisterSpell(def);
        Check.Equal(true, CoreBridge.TryResolve("rwom.tm_knowledgegatef", out var resolved), "convention id resolves");
        Check.Same(def, resolved, "resolved def is the registered def");
        Check.Equal(false, CoreBridge.TryResolve("rwom.never_registered_def", out _), "unknown id resolves false without throwing");

        var extDef = Ability("TM_KnowledgeGateG");
        var extType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("TorannMagic.Core.SpellIdentityExtension", false))
            .First(t => t != null);
        var ext = (DefModExtension)Activator.CreateInstance(extType);
        extType.GetField("spellId")!.SetValue(ext, "rwom.custom.heal");
        extDef.modExtensions = new List<DefModExtension> { ext };
        CoreBridge.RegisterSpell(extDef);
        Check.Equal(true, CoreBridge.TryResolve("rwom.custom.heal", out var extResolved), "extension spellId resolves");
        Check.Same(extDef, extResolved, "extension id maps to its def");
        Check.Equal(true, CoreBridge.TryResolve("rwom.tm_knowledgegateg", out _), "convention id still registered alongside extension id");
    }

    // RED 6: the knowledge-aware filter excludes store-disabled powers and
    // keeps the rest of the legacy filtering intact.
    private static void KnowledgeFilterAppliesGate(RuntimeFixture _)
    {
        var onDef = Ability("TM_KnowledgeGateH");
        var offDef = Ability("TM_KnowledgeGateI");
        var onPower = Power(onDef, true, true);
        var offPower = Power(offDef, true, true);
        var powers = new List<MagicPower> { onPower, offPower };
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        var entry = knowledge.GetOrCreate(SpellIds.Of(offDef), 0);
        entry.learned = true;
        entry.autocast = false;

        var allowed = AutocastPolicy.KnowledgePowers(powers, knowledge).ToList();
        Check.Equal(1, allowed.Count, "knowledge-disabled power excluded from the dispatch source");
        Check.Same(onPower, allowed[0], "knowledge-enabled power retained");
    }
}
