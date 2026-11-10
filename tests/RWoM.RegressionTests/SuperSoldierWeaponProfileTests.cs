using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// Live report (Trello b3GEW7Og): a Super Soldier pawn holding a weapon lost
// every gizmo mid-save with no logged error. The vanilla gizmo grid wipes the
// pawn's buttons when an exception escapes lazy GetGizmos enumeration, and the
// weapon fork derefenced comp.MightData and the spec power list that a stale
// mid-save state does not guarantee (the load-time Rebuild chain swallowed
// the same failure as a plain log line). These cases pin the degraded states:
// absent might data resolves to "no profile" instead of throwing.
internal static class SuperSoldierWeaponProfileTests
{
    private static Type Production(string name) =>
        typeof(CompClassProgression).Assembly.GetType("TorannMagic.ClassProgression." + name, true)!;

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SuperSoldierWeaponProfile.NullMightDataDegradesToNoProfile", NullMightDataDegradesToNoProfile);
        yield return new("SuperSoldierWeaponProfile.EquipmentAddToleratesMissingData", EquipmentAddToleratesMissingData);
    }

    // A pawn holding the martial discipline and the trait, but whose comp has
    // no might data (isMightUser false, mightData null) — the stale state a
    // mid-save conversion can leave behind.
    private static Pawn StaleSoldier(RuntimeFixture f, out Pawn_EquipmentTracker tracker)
    {
        // The beta progression gate must be on for the martial state to resolve.
        var featureWasEnabled = ClassProgressionFeature.Enabled;
        ClassProgressionFeature.Enabled = true;
        var (pawn, _, might) = PawnWithComps(false, false);
        Check.Equal(false, (bool)typeof(CompAbilityUserMight).GetField("isMightUser", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(might)!, "fixture precondition: not a might user");
        Check.Same(null, typeof(CompAbilityUserMight).GetField("mightData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(might),
            "fixture precondition: no might data");
        Set(pawn, "equipment", tracker = new Pawn_EquipmentTracker(pawn));
        pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_SuperSoldier));
        var progression = new CompClassProgression { parent = pawn };
        pawn.AllComps.Add(progression);
        var state = new ClassProgressionState(Vocation.Warrior);
        Check.Equal(true, state.TryAddFoundation("rwom.supersoldier", DisciplineDomain.Might), "martial foundation added");
        Set(progression, "state", state);
        var training = Production("SuperSoldierMartialPath").GetMethod("Training", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new[] { state.Find("rwom.supersoldier") })!;
        Set(training, "School", "TM_PistolSpec");
        // Localize the state chain the TryGet owner resolution relies on.
        Check.Equal(true, ClassProgressionFeature.Enabled, "feature enabled");
        Check.Equal(true, ClassIdentity.TryGetState(pawn, out var identityState), "identity state resolves");
        Check.NotNull(identityState.Find("rwom.supersoldier"), "discipline present");
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "SSProfileTestPistolWeapon"; def.category = ThingCategory.Item;
        def.equipmentType = EquipmentType.Primary;
        Set(def, "verbs", new List<VerbProperties> { new VerbProperties { verbClass = typeof(Verb_Shoot) } });
        var weapon = new ThingWithComps { def = def };
        var container = new ThingOwner<ThingWithComps>(tracker);
        typeof(Pawn_EquipmentTracker).GetField("equipment", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(tracker, container);
        // The vanilla AddEquipment notify hooks deref trackers a synthetic
        // pawn does not carry; the probe paths only read the container.
        var things = (List<ThingWithComps>)typeof(ThingOwner<ThingWithComps>)
            .GetField("innerList", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(container)!;
        things.Add(weapon);
        // TryAdd would set this; the holder is what TryGet pattern-matches on.
        Set(weapon, "holdingOwner", container);
        Check.Same(weapon, tracker.Primary, "test weapon is the primary");
        return pawn;
    }

    // TryGet runs inside vanilla gizmo/verb/stat paths, so the stale state
    // must resolve to "no profile" instead of throwing inside the lazy
    // GetGizmos enumeration (the throw wiped every drafted gizmo silently).
    private static void NullMightDataDegradesToNoProfile(RuntimeFixture f)
    {
        var body = StaleSoldier(f, out var tracker);
        var args = new object?[] { tracker.Primary, null, null };
        Check.Equal(false, (bool)Production("SuperSoldierWeaponProfile").GetMethod("TryGet", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!, "stale state resolves to no profile");
        Check.Equal("TM_PistolSpec", args[1], "school resolved before the data gate");
        var hasWeapon = Production("SuperSoldierWeaponProfile").GetMethod("HasWeapon", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        Check.Equal(false, (bool)hasWeapon.Invoke(null, new object?[] { body })!, "HasWeapon stays false without throwing");
    }

    // PawnEquipment_Add_Postfix derefenced comp.MightData and the spec power
    // list on every equipment add for class pawns; at load the Rebuild chain
    // swallowed that failure as a plain log line.
    private static void EquipmentAddToleratesMissingData(RuntimeFixture f)
    {
        var body = StaleSoldier(f, out var tracker);
        var method = typeof(TorannMagic.TorannMagicMod).GetMethod("PawnEquipment_Add_Postfix",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        method.Invoke(null, new object?[] { tracker, tracker.Primary, body });
        Check.Equal(true, true, "postfix completed without throwing");
    }
}
