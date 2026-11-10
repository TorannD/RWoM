using System;
using System.Reflection;
using Verse;

namespace TorannMagic
{
    // Reflection bridge to the optional core mod (huginn.rwom.core). The main
    // assembly has no compile-time core reference; every core API call goes
    // through this bridge and degrades to a no-op when the core is absent.
    public static class CoreBridge
    {
        private const string CoreTypeName = "TorannMagic.Core.AbilityCore";
        private const string ExtensionTypeName = "TorannMagic.Core.SpellIdentityExtension";
        private static bool _scanned;
        private static MethodInfo _register;
        private static MethodInfo _tryResolve;
        private static Func<Pawn, bool> _externalCasting;

        public static bool IsExternallyCasting(Pawn pawn)
        {
            Scan();
            return pawn != null && _externalCasting?.Invoke(pawn) == true;
        }

        public static bool Available
        {
            get { Scan(); return _register != null; }
        }

        private static void Scan()
        {
            if (_scanned) return;
            _scanned = true;
            _externalCasting = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // ReflectionTypeLoadException-safe: GetType(false) never throws for
                // plain type lookups, and the core is a plain net10 library.
                Type core = assembly.GetType(CoreTypeName, false);
                if (core == null) continue;
                CoreKnowledgeProvider.Bind(assembly);
                var ownership = assembly.GetType("TorannMagic.Core.CastOwnership", false)?.GetMethod("IsCasting", new[] { typeof(Pawn) });
                if (ownership != null) _externalCasting = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), ownership);
                _register = core.GetMethod("Register", new[] { typeof(string), typeof(Def) });
                _tryResolve = core.GetMethod("TryResolve", new[] { typeof(string), typeof(Def).MakeByRefType() });
                return;
            }
        }

        // Registers a def under its convention spellId plus any declared
        // extension spellId. No-op when the core mod is not loaded.
        public static void RegisterSpell(Def def)
        {
            Scan();
            if (_register == null || def == null || def.defName == null) return;
            _register.Invoke(null, new object[] { SpellIds.Convention(def), def });
            string id = DeclaredId(def);
            if (id != null) _register.Invoke(null, new object[] { id, def });
        }

        // Stable spellId → Def. False means "sleeping knowledge" (no def, never
        // an exception). Always false when the core mod is absent.
        public static bool TryResolve(string spellId, out Def def)
        {
            Scan();
            def = null;
            if (_tryResolve == null || spellId == null) return false;
            object[] args = { spellId, null };
            bool ok = _tryResolve.Invoke(null, args) is bool result && result;
            def = args[1] as Def;
            return ok;
        }

        internal static string DeclaredId(Def def)
        {
            object extension = FindExtension(def);
            string id = extension?.GetType().GetField("spellId")?.GetValue(extension) as string;
            return string.IsNullOrEmpty(id) ? null : id;
        }

        private static object FindExtension(Def def)
        {
            var extensions = def.modExtensions;
            if (extensions == null) return null;
            foreach (DefModExtension extension in extensions)
                if (extension != null && extension.GetType().FullName == ExtensionTypeName)
                    return extension;
            return null;
        }
    }
}
