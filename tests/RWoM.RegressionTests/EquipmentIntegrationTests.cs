using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections;
using Verse;
using Verse.AI;
using RimWorld;
namespace RWoM.RegressionTests;

internal static class EquipmentIntegrationTests
{
    public static IEnumerable<TestCase> Cases()
    {
        var giddyPath = Environment.GetEnvironmentVariable("RWOM_TEST_GIDDYUP");
        Assembly? giddy = string.IsNullOrEmpty(giddyPath) ? null : Assembly.LoadFrom(giddyPath);
        yield return new("IntegrationEquipment.Ownership", f =>
        {
            var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
            Check.NotNull(asm.GetType("TorannMagic.Integrations.GiddyUpAdapter"), "mount adapter belongs to Integrations");
            Check.NotNull(asm.GetType("TorannMagic.Integrations.SidearmsAdapter"), "weapon memory adapter belongs to Integrations");
            Check.Equal(false, typeof(TorannMagic.ModCheck.GiddyUp).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Any(), "Content facade owns no reflected vendor fields");
            TorannMagic.ModCheck.SS.ClearWeaponMemory(null);
            TorannMagic.ModCheck.GiddyUp.ForceDismount(null);
            Check.Equal(true, TorannMagic.ModCheck.GiddyUp.GetMount(null) == null, "no integration mount fallback");
        });
        var ss = Environment.GetEnvironmentVariable("RWOM_TEST_SIDEARMS");
        if (giddy == null && string.IsNullOrEmpty(ss))
            yield return new("IntegrationEquipment.AbsentVendorsWithHooks", f =>
            {
                using var hooks = new Hooks();
                Check.Equal(false, TorannMagic.ModCheck.Validate.GiddyUp.Core_IsInitialized(), "missing mounting provider remains inactive");
                Check.Equal(false, TorannMagic.ModCheck.Validate.SimpleSidearms.IsInitialized(), "missing weapon provider remains inactive");
                TorannMagic.ModCheck.GiddyUp.ForceDismount(null);
                TorannMagic.ModCheck.SS.ClearWeaponMemory(null);
                Check.Equal(true, TorannMagic.ModCheck.GiddyUp.GetMount(new Pawn()) == null, "active integration without vendor has no mount");
            });
        if (!string.IsNullOrEmpty(ss))
            yield return new("IntegrationEquipment.InstalledSidearmsApi", f =>
            {
                using var game = new EmptyGame();
                var vendor = Assembly.LoadFrom(ss).GetType("Dreadarm.SimpleSidearms.Rimworld.CompSidearmMemory", true)!;
                var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
                var adapter = asm.GetType("TorannMagic.Integrations.SidearmsAdapter", true)!;
                Check.Equal(true, (bool)adapter.GetMethod("IsMemoryType")!.Invoke(null, [vendor])!, "current fork recognized");
                Check.NotNull(vendor.GetProperty("RememberedWeapons"), "native list property");
                Check.NotNull(vendor.GetMethod("GenerateRememberedWeaponsFromEquipped", Type.EmptyTypes), "native regenerate method");
                Check.Equal(1, vendor.GetMethod("ForgetSidearmMemory")!.GetParameters().Length, "native forget signature");
                var pawn = GrimoireProgressionTests.PawnWithComps(true, false).pawn;
                pawn.mindState = new Pawn_MindState(pawn);
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
                pawn.inventory = new Pawn_InventoryTracker(pawn);
                var memory = (ThingComp)Activator.CreateInstance(vendor)!;
                memory.parent = pawn;
                pawn.AllComps.Add(memory);
                var memories = (IList)vendor.GetField("rememberedWeapons")!.GetValue(memory)!;
                var pairType = memories.GetType().GetGenericArguments()[0];
                var weaponDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                weaponDef.defName = "RegressionRememberedWeapon";
                memories.Add(Activator.CreateInstance(pairType, [weaponDef, null]));
                Check.Equal(1, ((IList)vendor.GetProperty("RememberedWeapons")!.GetValue(memory)!).Count, "native memory is valid before adapter runs");
                using var hooks = new Hooks();
                Check.Equal(true, TorannMagic.ModCheck.Validate.SimpleSidearms.IsInitialized(), "native fork available through Content");
                TorannMagic.ModCheck.SS.ClearWeaponMemory(pawn);
                Check.Equal(0, ((IList)vendor.GetField("rememberedWeapons")!.GetValue(memory)!).Count, "real native memory cleared and rebuilt from empty equipment");
                var equippedDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                equippedDef.defName = "RegressionEquippedWeapon";
                equippedDef.category = ThingCategory.Item; equippedDef.equipmentType = EquipmentType.Primary; equippedDef.comps = [];
                var weapon = new ThingWithComps { def = equippedDef, stackCount = 1 };
                var equippable = new CompEquippable { parent = weapon };
                equippable.Initialize(new CompProperties { compClass = typeof(CompEquippable) });
                typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(weapon, new List<ThingComp> { equippable });
                pawn.equipment.AddEquipment(weapon);
                memories = (IList)vendor.GetField("rememberedWeapons")!.GetValue(memory)!;
                memories.Add(Activator.CreateInstance(pairType, [weaponDef, null]));
                TorannMagic.ModCheck.SS.ClearWeaponMemory(pawn);
                memories = (IList)vendor.GetField("rememberedWeapons")!.GetValue(memory)!;
                Check.Equal(1, memories.Count, "equipped weapon rebuilt once");
                Check.Equal(true, memories.Contains(Activator.CreateInstance(pairType, [equippedDef, null])), "equipped weapon replaces stale memory");
                Check.Equal(true, ReferenceEquals(weapon, pawn.equipment.Primary), "physical equipment remains owned");
                TorannMagic.ModCheck.SS.ClearWeaponMemory(pawn);
                Check.Equal(1, ((IList)vendor.GetField("rememberedWeapons")!.GetValue(memory)!).Count, "repeated clearing does not duplicate memory");
            });
        if (giddy != null)
            yield return new("IntegrationEquipment.InstalledMountLookup", f =>
            {
                var storageType = giddy.GetType("GiddyUp.ExtendedDataStorage", true)!;
                var singleton = storageType.GetProperty("Singleton")!;
                var previous = singleton.GetValue(null);
                var storage = Activator.CreateInstance(storageType, new object?[] { null })!;
                var rider = GrimoireProgressionTests.PawnWithComps(true, false).pawn;
                var mount = GrimoireProgressionTests.PawnWithComps(true, false).pawn;
                rider.thingIDNumber = 55501; mount.thingIDNumber = 55502;
                typeof(Thing).GetField("factionInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(rider, new Faction { def = new FactionDef { isPlayer = true } });
                mount.jobs = new Pawn_JobTracker(mount);
                mount.jobs.curJob = new Job { def = new JobDef { defName = "Mounted" } };
                var data = storageType.GetMethod("GetExtendedPawnData")!.Invoke(storage, [rider])!;
                data.GetType().GetProperty("Mount")!.SetValue(data, mount);
                var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!).GetType("TorannMagic.Integrations.GiddyUpAdapter", true)!;
                try
                {
                    singleton.SetValue(null, storage);
                    using var hooks = new Hooks();
                    Check.Equal(true, TorannMagic.ModCheck.Validate.GiddyUp.Core_IsInitialized(), "native mount integration available through Content");
                    Check.Equal(true, ReferenceEquals(mount, TorannMagic.ModCheck.GiddyUp.GetMount(rider)), "native storage lookup returns the same owner");
                    mount.jobs.curJob = null;
                    Check.Equal(true, TorannMagic.ModCheck.GiddyUp.GetMount(rider) == null, "stale mounted record ignored");
                    data.GetType().GetProperty("ReservedMount")!.SetValue(data, mount);
                    var animalData = storageType.GetMethod("GetExtendedPawnData")!.Invoke(storage, [mount])!;
                    animalData.GetType().GetProperty("ReservedBy")!.SetValue(animalData, rider);
                    animalData.GetType().GetProperty("Rider")!.SetValue(animalData, rider);
                    var mounted = (HashSet<int>)storageType.GetField("isMounted")!.GetValue(null)!;
                    mounted.Add(rider.thingIDNumber);
                    TorannMagic.ModCheck.GiddyUp.ForceDismount(rider);
                    Check.Equal(true, data.GetType().GetProperty("Mount")!.GetValue(data) == null, "native dismount clears mount");
                    Check.Equal(true, data.GetType().GetProperty("ReservedMount")!.GetValue(data) == null, "native dismount clears reservation");
                    Check.Equal(true, animalData.GetType().GetProperty("Rider")!.GetValue(animalData) == null, "native dismount clears rider");
                    Check.Equal(true, animalData.GetType().GetProperty("ReservedBy")!.GetValue(animalData) == null, "native dismount releases animal reservation");
                    Check.Equal(false, mounted.Contains(rider.thingIDNumber), "native mounted set cleared");
                }
                finally { singleton.SetValue(null, previous); }
            });
    }
    sealed class Hooks : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly object harmony;
        readonly string id = "rwom.tests.equipment." + Guid.NewGuid();
        public Hooks()
        {
            harmony = Activator.CreateInstance(type, id)!;
            var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
            foreach (var name in new[] { "MountingAvailablePatch", "WeaponMemoryAvailablePatch", "DismountPatch", "IsMountPatch", "GetMountPatch", "ClearWeaponMemoryPatch" })
            {
                var processor = type.GetMethod("CreateClassProcessor")!.Invoke(harmony, [asm.GetType("TorannMagic.Integrations." + name, true)])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    sealed class EmptyGame : IDisposable
    {
        readonly Game previous = Current.Game;
        public EmptyGame()
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(Current.Game, new List<Map>());
            typeof(Game).GetField("tickManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(Current.Game, new TickManager());
        }
        public void Dispose() => Current.Game = previous;
    }
}
