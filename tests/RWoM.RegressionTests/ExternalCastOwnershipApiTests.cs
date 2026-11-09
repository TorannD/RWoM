using System.Reflection;
using Verse;
using TorannMagic;

namespace RWoM.RegressionTests;

internal static class ExternalCastOwnershipApiTests
{
    public static IEnumerable<TestCase> Cases()
    {
        if (Environment.GetEnvironmentVariable("RWOM_TEST_CORE_ASSEMBLY") != "disabled")
            yield return new("IntegrationPackage.MainStartup", Startup);
        yield return new("Core.CastOwnership.RegistrationAndRelease", f =>
        {
            var path = CoreSdkTests.AssemblyPath(f.Repository);
            if (path == null) { Check.Equal(false, TorannMagic.AutoCast.TM_ExternalCastOwnership.IsCasting(new Pawn()), "Content works without Core"); return; }
            var api = Assembly.LoadFrom(path).GetType("TorannMagic.Core.CastOwnership", true)!;
            var register = api.GetMethod("Register")!;
            var remove = api.GetMethod("Unregister")!;
            var query = api.GetMethod("IsCasting")!;
            var pawn = new Pawn();
            bool busy = true;
            Func<Pawn,bool> provider = p => p == pawn && busy;
            Func<Pawn,bool> other = p => false;
            const string id = "regression.external-owner";
            try
            {
                Check.Equal(true,(bool)register.Invoke(null,[id,provider])!,"first registration");
                Check.Equal(false,(bool)register.Invoke(null,[id,other])!,"duplicate id cannot replace owner");
                Check.Equal(false,(bool)remove.Invoke(null,[id,other])!,"foreign callback cannot unregister owner");
                Check.Equal(false,(bool)query.Invoke(null,[null])!,"null pawn has no owner");
                Check.Equal(true,(bool)query.Invoke(null,[pawn])!,"Core query works");
                Check.Equal(true,TorannMagic.AutoCast.TM_ExternalCastOwnership.IsCasting(pawn),"Content consults neutral Core contract");
                busy = false;
                Check.Equal(false,TorannMagic.AutoCast.TM_ExternalCastOwnership.IsCasting(pawn),"ownership not cached on pawn");
                Check.Equal(true,(bool)remove.Invoke(null,[id,provider])!,"owner can unregister");
                busy = true;
                Check.Equal(false,(bool)query.Invoke(null,[pawn])!,"unregistered provider is inactive");
            }
            finally { remove.Invoke(null,[id,provider]); }
        });
    }
    static void Startup(RuntimeFixture f)
    {
        var adapter = Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_INTEGRATIONS_ASSEMBLY")!);
        var core = Assembly.LoadFrom(CoreSdkTests.AssemblyPath(f.Repository)!);
        var ownership = core.GetType("TorannMagic.Core.CastOwnership",true)!;
        var callback = (Func<Pawn,bool>)Delegate.CreateDelegate(typeof(Func<Pawn,bool>),adapter.GetType("TorannMagic.Integrations.VefCastOwnership",true)!.GetMethod("IsCasting")!);
        var ht=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        const string id="huginn.rwom.integrations";
        var harmony=Activator.CreateInstance(ht,id)!;
        int OptionalCount() => AppDomain.CurrentDomain.GetAssemblies().Count(a=>a.GetName().Name is "VEF" or "VanillaPsycastsExpanded");
        int before=OptionalCount();
        try
        {
            var startup=adapter.GetType("TorannMagic.Integrations.IntegrationStartup",true)!;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(startup.TypeHandle);
            Check.Equal(false,(bool)ownership.GetMethod("Register")!.Invoke(null,["rwom.integrations.vef",callback])!,"startup owns the VEF probe registration");
            Check.Equal(before,OptionalCount(),"startup does not load vendor assemblies");
            Check.Equal(true,typeof(MagicData).Assembly.GetType("TorannMagic.Compatibility.PridestalkerTraitConstructorPatch")==null,"Kure patch left Content");
            var def=new RimWorld.TraitDef {defName="TM_Pridestalker",degreeDatas=[new RimWorld.TraitDegreeData{degree=4}]};
            Check.Equal(4,new RimWorld.Trait(def,0).Degree,"real startup patches trait construction");
            Check.Equal(4,TorannMagic.Compatibility.ClassDefinitionCompatibility.CanonicalTraitDegree(def,0),"real startup patches roster preview");
            var installed = new List<MethodInfo>();
            foreach (var target in (IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!)
            {
                var info = ht.GetMethod("GetPatchInfo")!.Invoke(null, [target])!;
                foreach (var kind in new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" })
                    foreach (var patch in (System.Collections.IEnumerable)info.GetType().GetField(kind)!.GetValue(info)!)
                        if ((string)patch.GetType().GetField("owner")!.GetValue(patch)! == id)
                            installed.Add((MethodInfo)patch.GetType().GetProperty("PatchMethod")!.GetValue(patch)!);
            }
            foreach (var name in new[] { "LoggingBackendPatch", "MountingAvailablePatch", "WeaponMemoryAvailablePatch",
                "DismountPatch", "IsMountPatch", "GetMountPatch", "ClearWeaponMemoryPatch", "LegacyModPolicies", "LegacyBirthBridgePatch" })
                Check.Equal(1, installed.Count(m => m.DeclaringType?.Name == name), "startup installs exactly one " + name);
            // Override only provider discovery, not Harmony registration: this proves the
            // ordinary package startup reaches the logger's adapter boundary.
            var backend = adapter.GetType("TorannMagic.Integrations.LoggingBackend", true)!;
            var field = backend.GetField("findTypes", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = field.GetValue(null);
            var invalidate = backend.GetMethod("Invalidate", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                LoggingCompatTests.FakeFacade.Reset();
                LoggingCompatTests.FakeLifecycle.IsPrimary = true;
                LoggingCompatTests.FakeLifecycle.Throw = false;
                LoggingCompatTests.FakeLifecycle.RegisteredSinks = new object[] { new object() };
                field.SetValue(null, (Func<Type[]>)(() => new[] { typeof(LoggingCompatTests.FakeFacade), typeof(LoggingCompatTests.FakeLifecycle) }));
                invalidate.Invoke(null, null);
                TorannMagic.Compatibility.TM_Log.Warning("startup", "single external delivery");
                Check.Equal(1, LoggingCompatTests.FakeFacade.Entries.Count, "full startup delivers logging once");
                RuntimeLog.AssertClean();
            }
            finally { field.SetValue(null, previous); invalidate.Invoke(null, null); }
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            ownership.GetMethod("Unregister")!.Invoke(null,["rwom.integrations.vef",callback]);
        }
    }
}
