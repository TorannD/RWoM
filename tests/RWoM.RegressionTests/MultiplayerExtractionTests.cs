using System.Reflection;
using System.Xml;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections;
using Verse;
namespace RWoM.RegressionTests;

internal static class MultiplayerExtractionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("IntegrationMP.LegacyPublicFacades", f =>
        {
            var api = typeof(TorannMagic.MagicData).Assembly.GetType("TorannMagic.TMPCompat");
            Check.NotNull(api, "old public MP entry point stays in Content without vendor API");
            Check.NotNull(typeof(TorannMagic.FleshDevelopment.FleshMultiplayer).GetMethod("Register"), "old Flesh registration entry point stays callable");
        });
        yield return new("IntegrationMP.SharedLegacyPackageIds", f =>
        {
            var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_MULTIPLAYER_ADAPTER")!);
            var field = asm.GetType("TorannMagic.TMPCompatAdapter", true)!.GetField("MultiplayerPackageIds")!;
            Check.Equal(true, ReferenceEquals(TorannMagic.TMPCompat.MultiplayerPackageIds, field.GetValue(null)), "legacy extensions and adapter share the original mutable array");
        });
        yield return new("IntegrationMP.ContentHasNoExternalApiDependency", f =>
        {
            Check.Equal(false, typeof(TorannMagic.MagicData).Assembly.GetReferencedAssemblies().Any(a => a.Name!.Contains("Multiplayer")), "Content is independent of external Multiplayer API");
            Check.Equal(true, typeof(TorannMagic.MagicData).Assembly.GetType("TorannMagic.TMPCompatStartup") == null, "Content owns no MP startup registration");
        });
        yield return new("IntegrationMP.ConditionalRoot", f =>
        {
            var xml = new XmlDocument(); xml.Load(Path.Combine(f.Repository, "TorannMagic.Integrations/LoadFolders.xml"));
            var folders = new ModLoadFolders(); folders.LoadDataFromXmlCustom(xml.DocumentElement!);
            var root = folders.FoldersForVersion("1.7").SingleOrDefault(x => x.folderName == Path.Combine("v1.7", "MultiplayerCompat"));
            Check.NotNull(root, "one conditional MP root exists");
            var li = xml.SelectSingleNode("/loadFolders/v1.7/li[text()='v1.7/MultiplayerCompat']")!;
            Check.Equal("zetrith.multiplayer,rwmt.multiplayer", li.Attributes!["IfModActive"]!.Value, "both external forks gate one root");
            Check.Equal(2, root!.requiredAnyOfPackageIds.Count, "native parser treats fork identifiers as OR");
        });
        if (Environment.GetEnvironmentVariable("RWOM_TEST_NATIVE_MP") == "1")
        {
            yield return new("IntegrationMP.NativeRegistrationOnce", NativeRegistration);
            yield break;
        }
        yield return new("IntegrationMP.StartupWithoutProvider", f =>
        {
            int ApiCount() => AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "0MultiplayerAPI");
            var before = ApiCount();
            var asm = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_MULTIPLAYER_ADAPTER")!);
            var compat = asm.GetType("TorannMagic.TMPCompatAdapter", true)!;
            Check.Equal(false, (bool)compat.GetMethod("MultiplayerActive")!.Invoke(null, null)!, "no external Multiplayer provider in isolated harness");
            var startup = asm.GetType("TorannMagic.TMPCompatStartup", true)!;
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            Check.Equal(false, (bool)compat.GetProperty("Initialized")!.GetValue(null)!, "no registrations when provider absent");
            Check.Equal(before, ApiCount(), "absent startup does not load external API");
        });
    }

    static void NativeRegistration(RuntimeFixture f)
    {
        const string root = "C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/3820003322";
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var dir in new[] { "Assemblies", "AssembliesCustom" })
            {
                var path = Path.Combine(root, dir, name.Name + ".dll");
                if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
            }
            return null;
        }
        var activeField = typeof(ModsConfig).GetField("activeModsHashSet", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previousActive = activeField.GetValue(null);
        var pack = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        pack.assemblies = new ModAssemblyHandler(pack);
        var ht = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        var cleanup = Activator.CreateInstance(ht, "rwom.tests.mpcleanup")!;
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var api = Assembly.LoadFrom(Path.Combine(root, "Assemblies/0MultiplayerAPI.dll"));
            var vendor = Assembly.LoadFrom(Path.Combine(root, "AssembliesCustom/Multiplayer.dll"));
            // The MP mod constructor normally creates this tree before mod startup.
            // Use the native registry object; no fake RegisterSyncMethod/worker code.
            var workersField = vendor.GetType("Multiplayer.Client.SyncDict", true)!.GetField("syncWorkers", BindingFlags.Static | BindingFlags.NonPublic)!;
            workersField.SetValue(null, Activator.CreateInstance(workersField.FieldType));
            pack.assemblies.loadedAssemblies.Add(vendor);
            LoadedModManager.RunningModsListForReading.Add(pack);
            activeField.SetValue(null, new HashSet<string> { "rwmt.multiplayer" });
            var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_MULTIPLAYER_ADAPTER")!);
            var startup = adapter.GetType("TorannMagic.TMPCompatStartup", true)!;
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            RuntimeLog.AssertClean();
            Check.Equal(true, TorannMagic.TMPCompat.MultiplayerActive(), "public facade sees native provider");
            Check.Equal(true, TorannMagic.TMPCompat.Initialized, "native startup initialized");
            Check.Equal(true, (bool)api.GetType("Multiplayer.API.MP", true)!.GetField("enabled")!.GetValue(null)!, "real MP bridge owns API");
            var registry = (IDictionary)vendor.GetType("Multiplayer.Client.Sync", true)!.GetField("methodBaseToInternalId")!.GetValue(null)!;
            foreach (var method in new[] {
                typeof(AbilityUser.PawnAbility).GetMethod("UseAbility"),
                typeof(TorannMagic.MagicAbility).GetMethod("UseAbility"),
                typeof(TorannMagic.MightAbility).GetMethod("UseAbility"),
                typeof(TorannMagic.CompAbilityUserMagic).GetMethod("ToggleTechnoBit"),
                typeof(TorannMagic.CompAbilityUserMight).GetMethod("ToggleCleave"),
                typeof(TorannMagic.FleshDevelopment.FleshMultiplayer).GetMethod("StopFeast"),
                typeof(TorannMagic.FleshDevelopment.JobDriver_FleshCast).GetMethod("TryStart"),
                typeof(TorannMagic.SpellItems.SpellItemCasting).GetMethod("TryStartPortal") })
                Check.Equal(true, registry.Contains(method!), "native registry includes " + method!.Name);
            int count = registry.Count;
            var tree = workersField.GetValue(null)!;
            var entry = tree.GetType().GetMethod("GetOrAddEntry", [typeof(Type), typeof(bool), typeof(bool)])!.Invoke(tree, [typeof(AbilityUser.PawnAbility), false, false])!;
            Check.Equal(1, (int)entry.GetType().GetProperty("SyncWorkerCount")!.GetValue(entry)!, "one real ability serialization worker");
            foreach(var abilityType in new[] { typeof(TorannMagic.MagicAbility), typeof(TorannMagic.MightAbility) })
            {
                var derived = tree.GetType().GetMethod("GetOrAddEntry", [typeof(Type), typeof(bool), typeof(bool)])!.Invoke(tree, [abilityType, false, false])!;
                Check.Equal(1, (int)derived.GetType().GetProperty("SyncWorkerCount")!.GetValue(derived)!, "derived worker " + abilityType.Name);
                NativeNullAbilityRoundTrip(vendor, derived);
                NativeOwnedAbilityRoundTrip(f, vendor, derived, abilityType);
            }
            NativeNullAbilityRoundTrip(vendor, entry);
            NativeOwnedAbilityRoundTrip(f, vendor, entry);
            TorannMagic.TMPCompat.Setup();
            TorannMagic.FleshDevelopment.FleshMultiplayer.Register();
            RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            Check.Equal(count, registry.Count, "repeated setup and legacy registration do not duplicate methods");
            Check.Equal(1, (int)entry.GetType().GetProperty("SyncWorkerCount")!.GetValue(entry)!, "repeated setup does not duplicate serialization worker");
            Console.WriteLine("Native MP registrations: " + count);
        }
        finally
        {
            foreach (var id in new[] { "multiplayer", "rimworld.torann.tmagic", "huginn.rwom.integrations.multiplayer" })
                ht.GetMethod("UnpatchAll")!.Invoke(cleanup, [id]);
            activeField.SetValue(null, previousActive);
            LoadedModManager.RunningModsListForReading.Remove(pack);
            AssemblyLoadContext.Default.Resolving -= Resolve;
        }
    }

    static void NativeNullAbilityRoundTrip(Assembly vendor, object entry)
    {
        // Actual MP byte stream and registered worker. This sentinel case must
        // not require a pawn/map resolver or consume unrelated command bytes.
        var common = Assembly.Load("MultiplayerCommon");
        Type Native(string name) => vendor.GetType(name) ?? common.GetType(name, true)!;
        var serialization = Activator.CreateInstance(Native("Multiplayer.Common.SyncSerialization"), new object?[] { null })!;
        var writer = Activator.CreateInstance(Native("Multiplayer.Common.ByteWriter"), 0)!;
        var writing = Activator.CreateInstance(Native("Multiplayer.Client.WritingSyncWorker"), writer, serialization)!;
        var invoke = entry.GetType().GetMethod("Invoke")!;
        object?[] writeArgs = [writing, null];
        Check.Equal(true, (bool)invoke.Invoke(entry, writeArgs)!, "registered worker accepts null ability");
        var bytes = (byte[])writer.GetType().GetMethod("ToArray")!.Invoke(writer, null)!;
        Check.Equal(4, bytes.Length, "null ability writes only component sentinel");
        Check.Equal(-1, BitConverter.ToInt32(bytes), "legacy wire sentinel retained");
        var reader = Activator.CreateInstance(Native("Multiplayer.Common.ByteReader"), new object[] { bytes })!;
        var reading = Activator.CreateInstance(Native("Multiplayer.Client.ReadingSyncWorker"), reader, serialization)!;
        object?[] readArgs = [reading, null];
        Check.Equal(true, (bool)invoke.Invoke(entry, readArgs)!, "registered worker reads null sentinel");
        Check.Equal(true, readArgs[1] == null, "null ability stays null after native round-trip");
    }

    static void NativeOwnedAbilityRoundTrip(RuntimeFixture f, Assembly vendor, object entry, Type? abilityType = null)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var common = Assembly.Load("MultiplayerCommon");
        Type Native(string name) => vendor.GetType(name) ?? common.GetType(name, true)!;
        var serialization = Activator.CreateInstance(Native("Multiplayer.Common.SyncSerialization"), new object?[] { null })!;
        var tree = Native("Multiplayer.Client.SyncDictRimWorld").GetField("syncWorkers", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null)!;
        serialization.GetType().GetField("syncTree")!.SetValue(serialization, tree);
        var mp = Native("Multiplayer.Client.Multiplayer");
        var gameField = mp.GetField("game")!;
        var previousGame = gameField.GetValue(null);
        var game = RuntimeHelpers.GetUninitializedObject(gameField.FieldType);
        var thingsField = game.GetType().GetField("thingsById", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        var things = Activator.CreateInstance(thingsField.FieldType)!;
        thingsField.SetValue(game, things);
        object Context()
        {
            var context = Activator.CreateInstance(Native("Multiplayer.Client.MpContext"))!;
            // The containing command has already established the Thing parent context.
            context.GetType().GetField("syncingThingParent")!.SetValue(context, true);
            return context;
        }
        bool might = abilityType == typeof(TorannMagic.MightAbility);
        var source = GrimoireProgressionTests.PawnWithComps(!might, might).pawn;
        var destination = GrimoireProgressionTests.PawnWithComps(!might, might).pawn;
        source.thingIDNumber = destination.thingIDNumber = 78123;
        AbilityUser.PawnAbility Add(Pawn pawn, AbilityUser.AbilityDef def)
        {
            AbilityUser.CompAbilityUser comp = might ? pawn.GetComp<TorannMagic.CompAbilityUserMight>() : pawn.GetComp<TorannMagic.CompAbilityUserMagic>();
            var ability = abilityType == null ? new AbilityUser.PawnAbility(comp) :
                (AbilityUser.PawnAbility)Activator.CreateInstance(abilityType, comp)!;
            ability.Def = def;
            comp.AbilityData.Powers.Add(ability);
            comp.AbilityData.AllPowers = null;
            return ability;
        }
        Add(source, TorannMagic.TorannMagicDefOf.TM_Blink); var sent = Add(source, TorannMagic.TorannMagicDefOf.TM_Soothe);
        Add(destination, TorannMagic.TorannMagicDefOf.TM_Blink); var received = Add(destination, TorannMagic.TorannMagicDefOf.TM_Soothe);
        try
        {
            gameField.SetValue(null, game);
            things.GetType().GetMethod("Register")!.Invoke(things, [destination]);
            var writer = Activator.CreateInstance(Native("Multiplayer.Common.ByteWriter"), 0)!;
            writer.GetType().GetField("context")!.SetValue(writer, Context());
            var writing = Activator.CreateInstance(Native("Multiplayer.Client.WritingSyncWorker"), writer, serialization)!;
            var invoke = entry.GetType().GetMethod("Invoke")!;
            object?[] writeArgs = [writing, sent];
            Check.Equal(true, (bool)invoke.Invoke(entry, writeArgs)!, "native worker writes owned ability");
            var bytes = (byte[])writer.GetType().GetMethod("ToArray")!.Invoke(writer, null)!;
            Check.Equal(12, bytes.Length, "owned ability writes component index, power index and native pawn ID");
            var reader = Activator.CreateInstance(Native("Multiplayer.Common.ByteReader"), new object[] { bytes })!;
            reader.GetType().GetField("context")!.SetValue(reader, Context());
            var reading = Activator.CreateInstance(Native("Multiplayer.Client.ReadingSyncWorker"), reader, serialization)!;
            object?[] readArgs = [reading, null];
            Check.Equal(true, (bool)invoke.Invoke(entry, readArgs)!, "native worker reads owned ability");
            Check.Same(received, readArgs[1], "receiver resolves its own pawn and second ability, not sender instance");
            Check.Equal(0, (int)reader.GetType().GetProperty("Left")!.GetValue(reader)!, "command bytes fully consumed");
        }
        finally { gameField.SetValue(null, previousGame); }
    }
}
