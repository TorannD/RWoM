using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// Live report (Player.log, save "Grand Army of the Republic"): every Super
// Soldier reload turned the equipped weapon into a "ghost" Spec_Base proxy.
// Root cause: 1.7 ThingOwner.Remove/Contains key on holdingOwner, so a proxy
// saved with a corrupted holder survived Rebuild's removal, the copy factory
// logged "already having primaryInt equipment" and leaked the fresh weapon,
// and the failed cycle left orphan proxies whose binding copy reference no
// longer resolves (drop does nothing, abilities reject the weapon). These
// cases pin the repaired cycle: reclaim the slot before the factory, and
// restore the stashed original for orphan bindings instead of stacking
// another copy on a broken slot.
internal static class SuperSoldierWeaponReloadTests
{
    private static readonly BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    // Real Spec weapons carry CompEquippable; the vanilla add-notify binds
    // their verbs to the pawn and dereferences the comp on every container
    // transfer. Stub weapons have no comps, so the fixture scope skips that
    // notify while production rebuild code runs. The prefix passes through
    // to the original whenever the scope is inactive, so other suites that
    // exercise real comps keep the vanilla behavior.
    private static bool testFactoryActive;

    // The real factories construct a fresh ThingDef, whose constructor loads
    // shader content that does not exist headless. The double keeps the
    // factory's observable contract: a fresh Spec_Base thing is equipped as
    // the primary and bound to the original. Scoped so the real factory keeps
    // running whenever any other suite exercises it.
    private static bool RifleFactoryDouble(Pawn caster, ThingWithComps thing)
    {
        if (!testFactoryActive) return true;
        var might = caster.AllComps.OfType<CompAbilityUserMight>().First();
        var def = DefDatabase<ThingDef>.GetNamedSilentFail("TM_RifleSpec_Base1");
        var spec = (ThingWithComps)ThingMaker.MakeThing(def);
        // Installed like the Equip helper: AddEquipment would run the vanilla
        // notify chain, whose verb wiring a stub weapon cannot satisfy.
        var holder = caster.equipment.GetDirectlyHeldThings();
        MakeNotifySafe(spec);
        ((List<ThingWithComps>)typeof(ThingOwner<ThingWithComps>)
            .GetField("innerList", Flags)!.GetValue(holder)!).Add(spec);
        Set(spec, "holdingOwner", holder);
        var binding = typeof(CompAbilityUserMight).GetField("specializedWeapon", Flags)!.GetValue(might)!;
        binding.GetType().GetMethod("Bind", Flags)!.Invoke(binding, new object?[] { thing, spec });
        return false;
    }

    static SuperSoldierWeaponReloadTests()
    {
        var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var harmony = System.Activator.CreateInstance(harmonyType, "rwom.test.soldierreload")!;
        var factoryPrefix = System.Activator.CreateInstance(
            Assembly.Load("0Harmony").GetType("HarmonyLib.HarmonyMethod")!,
            typeof(SuperSoldierWeaponReloadTests).GetMethod(nameof(RifleFactoryDouble),
                BindingFlags.NonPublic | BindingFlags.Static))!;
        var patch = harmonyType.GetMethods().First(m => m.Name == "Patch"
            && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(MethodBase));
        patch.Invoke(harmony, new[]
        {
            (MethodBase)typeof(TM_Action).GetMethod("DoAction_RifleSpecCopy",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!,
            factoryPrefix, null, null, null
        });
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Reload.CorruptedProxyIsReclaimedAndCopyRebuilt", CorruptedProxyIsReclaimedAndCopyRebuilt);
        yield return new("Reload.OrphanBindingRestoresOriginal", OrphanBindingRestoresOriginal);
        yield return new("Reload.OrdinaryPrimaryIsNeverReplaced", OrdinaryPrimaryIsNeverReplaced);
    }

    private static ThingDef RangedDef(string name)
    {
        var def = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = name;
        def.label = name;
        def.category = ThingCategory.Item;
        def.equipmentType = EquipmentType.Primary;
        def.thingClass = typeof(ThingWithComps);
        def.destroyable = true;
        def.graphicData = new GraphicData { texPath = "Things/Item/Weapon/" + name };
        Set(def, "verbs", new List<VerbProperties> { new VerbProperties { verbClass = typeof(Verb_Shoot) } });
        Set(def, "comps", new List<CompProperties>());
        return def;
    }

    // The vanilla add-notify dereferences CompEquippable on every equipped
    // weapon; real weapons always carry it, and the stubs must too. The
    // AllComps getter pads from a shared empty list, so the backing field
    // gets a private list.
    private static void MakeNotifySafe(ThingWithComps weapon)
    {
        var comp = new CompEquippable();
        Set(comp, "parent", weapon);
        comp.Initialize(new CompProperties { compClass = typeof(CompEquippable) });
        Set(weapon, "comps", new List<ThingComp> { comp });
    }

    private static void RegisterDef(ThingDef def)
    {
        if (DefDatabase<ThingDef>.GetNamedSilentFail(def.defName) == null) DefDatabase<ThingDef>.Add(def);
    }

    // ThingOwner.NotifyAdded probes HasAttachment(ThingDefOf.Fire); the
    // staged database binds the DefOf without a Fire def, so the probe
    // dereferences null on every transfer. Register the missing def once.
    private static void EnsureFireDef()
    {
        if (ThingDefOf.Fire != null) return;
        var fire = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        fire.defName = "Fire";
        fire.label = "fire";
        fire.category = ThingCategory.Item;
        fire.thingClass = typeof(Fire);
        DefDatabase<ThingDef>.Add(fire);
        typeof(ThingDefOf).GetField("Fire", Flags)!.SetValue(null, fire);
    }

    private static (Pawn pawn, CompAbilityUserMight might, Pawn_EquipmentTracker tracker, ThingOwner<ThingWithComps> stash, object binding)
        Soldier(RuntimeFixture f)
    {
        TorannMagic.ModOptions.Constants.SetRifleSpecCount(0);
        EnsureFireDef();
        // The reload chain needs a game shell (thing IDs from MakeThing) and
        // ThingOwner.NotifyAdded iterates Find.Maps; provide both when the
        // suite has no shell. The shell stays installed for later suites.
        var mapsField = typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (Current.Game == null)
        {
            var shell = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            typeof(Game).GetField("tickManager", Flags)!.SetValue(shell, new TickManager());
            typeof(Game).GetField("uniqueIDsManager", Flags)!.SetValue(shell, new UniqueIDsManager());
            mapsField!.SetValue(shell, new List<Map>());
            Current.Game = shell;
        }
        else if (mapsField!.GetValue(Current.Game) is not List<Map>)
        {
            mapsField.SetValue(Current.Game, new List<Map>());
        }
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var (pawn, _, might) = PawnWithComps(false, true);
        pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_SuperSoldier));
        var tracker = new Pawn_EquipmentTracker(pawn);
        Set(pawn, "equipment", tracker);
        // Verify the notify-scope patch actually intercepts before the cases
        // rely on it.
        var probe = new ThingWithComps { def = RangedDef("NotifyProbe") };
        MakeNotifySafe(probe);
        try { tracker.Notify_EquipmentAdded(probe); }
        catch (NullReferenceException)
        {
            throw new InvalidOperationException(
                "Notify_EquipmentAdded is not stub-weapon safe; the reload cases cannot run");
        }
        var mindStateType = typeof(Pawn).GetField("mindState")!.FieldType;
        // The real constructor chains into work trackers the synthetic pawn
        // lacks; AddEquipment only assigns one field on this object.
        Set(pawn, "mindState", System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(mindStateType));
        might.MightData.MightPowersSS.Single(p => p.abilityDef == TorannMagicDefOf.TM_RifleSpec).learned = true;
        _ = might.MightData.MightPowerSkill_RifleSpec;
        var stash = new ThingOwner<ThingWithComps>(tracker);
        Set(might, "equipmentContainer", stash);
        var binding = typeof(CompAbilityUserMight).GetField("specializedWeapon", Flags)!.GetValue(might)!;
        return (pawn, might, tracker, stash, binding);
    }

    private static void Equip(Pawn_EquipmentTracker tracker, ThingWithComps weapon)
    {
        // Mirror the fixture recipe used by the profile tests: the vanilla
        // notify hooks deref trackers a synthetic pawn does not carry, so the
        // membership is installed directly on the tracker container.
        var holder = tracker.GetDirectlyHeldThings();
        MakeNotifySafe(weapon);
        ((List<ThingWithComps>)typeof(ThingOwner<ThingWithComps>)
            .GetField("innerList", Flags)!.GetValue(holder)!).Add(weapon);
        Set(weapon, "holdingOwner", holder);
        Check.Same(tracker.Primary, weapon, "fixture precondition: weapon is the primary");
    }

    private static bool InvokeRebuild(CompAbilityUserMight might)
    {
        var reload = typeof(CompAbilityUserMight).Assembly.GetType(
            "TorannMagic.ClassProgression.SuperSoldierWeaponReload", true)!;
        testFactoryActive = true;
        try { return (bool)reload.GetMethod("Rebuild", Flags)!.Invoke(null, new object?[] { might })!; }
        finally { testFactoryActive = false; }
    }

    private static void InvokeBind(object binding, ThingWithComps original, ThingWithComps copy)
    {
        binding.GetType().GetMethod("Bind", Flags)!.Invoke(binding, new object?[] { original, copy });
    }

    // The reported state: the equipped proxy's holder no longer matches the
    // tracker, so the old removal silently failed and leaked the fresh copy.
    private static void CorruptedProxyIsReclaimedAndCopyRebuilt(RuntimeFixture f)
    {
        var (pawn, might, tracker, stash, binding) = Soldier(f);
        RegisterDef(RangedDef("TM_RifleSpec_Base1"));
        var original = new ThingWithComps { def = RangedDef("ReloadTestBattleRifle") };
        MakeNotifySafe(original);
        stash.TryAdd(original);
        var proxy = new ThingWithComps { def = RangedDef("TM_RifleSpec_Base1") };
        Equip(tracker, proxy); // install on the tracker list first
        InvokeBind(binding, original, proxy);
        // Corrupt the holder: the proxy stays in the tracker list but Remove's
        // Contains check keys on holdingOwner and used to no-op here.
        Set(proxy, "holdingOwner", null);

        Check.Equal(true, InvokeRebuild(might), "rebuild succeeds through the corrupted slot");

        Check.NotEqual(proxy, tracker.Primary, "stale proxy left the primary slot");
        Check.Equal(true, proxy.Destroyed, "stale proxy destroyed after the swap");
        Check.NotEqual(null, tracker.Primary, "a fresh copy holds the primary slot");
        Check.Equal(true, tracker.Primary.def.defName.Contains("Spec_Base"), "fresh copy is a spec weapon");
        Check.Equal(true, stash.Contains(original), "the original stays stashed");
    }

    // The state saved by every already-affected cycle: the binding's copy
    // reference no longer resolves, so rebuild must hand the stashed original
    // back instead of stacking another copy.
    private static void OrphanBindingRestoresOriginal(RuntimeFixture f)
    {
        var (pawn, might, tracker, stash, binding) = Soldier(f);
        var original = new ThingWithComps { def = RangedDef("ReloadTestBattleRifle") };
        MakeNotifySafe(original);
        stash.TryAdd(original);
        var proxy = new ThingWithComps { def = RangedDef("TM_RifleSpec_Base1") };
        Equip(tracker, proxy);
        InvokeBind(binding, original, null); // the copy reference died with the destroyed ghost
        var copyLost = (bool)binding.GetType().GetProperty("CopyLost", Flags)!.GetValue(binding)!;
        Check.Equal(true, copyLost, "fixture precondition: binding copy is lost");

        Check.Equal(true, InvokeRebuild(might), "orphan rebuild restores the original");

        Check.Same(original, tracker.Primary, "the real weapon is equipped again");
        Check.Equal(true, proxy.Destroyed, "the orphan proxy is gone");
        Check.Equal(false, stash.Contains(original), "the stash no longer holds the original");
        Check.Equal(-1, (int)typeof(CompAbilityUserMight).GetField("specWpnRegNum", Flags)!.GetValue(might)!,
            "registry released");
    }

    private static void OrdinaryPrimaryIsNeverReplaced(RuntimeFixture f)
    {
        var (pawn, might, tracker, stash, binding) = Soldier(f);
        var original = new ThingWithComps { def = RangedDef("ReloadTestBattleRifle") };
        MakeNotifySafe(original);
        stash.TryAdd(original);
        var rifle = new ThingWithComps { def = RangedDef("ReloadTestPlainRifle") };
        MakeNotifySafe(rifle);
        Equip(tracker, rifle);
        InvokeBind(binding, original, null);

        Check.Equal(false, InvokeRebuild(might), "ordinary equipment is never rebuilt");
        Check.Same(rifle, tracker.Primary, "the ordinary primary is untouched");
        Check.Equal(true, stash.Contains(original), "the stash is untouched");
    }
}
