using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace TorannMagic.Core
{
    public static class AbilityCore
    {
        private static Dictionary<string, Def> byId;
        private static IReadOnlyDictionary<string, Def> view;
        // Keep names rather than Def instances across database reloads.
        private static readonly Dictionary<string, (Type type, string name)> aliases = new();

        public static void Invalidate()
        {
            byId = null;
            view = null;
        }

        public static void Register(string spellId, Def def)
        {
            if (string.IsNullOrWhiteSpace(spellId) || string.IsNullOrEmpty(def?.defName)) return;
            EnsureRegistry();
            if (Add(spellId, def))
                aliases[spellId] = (def.GetType(), def.defName);
        }

        public static Def Resolve(string spellId) => TryResolve(spellId, out var def) ? def : null;

        public static bool TryResolve(string spellId, out Def def)
        {
            def = null;
            if (string.IsNullOrWhiteSpace(spellId)) return false;
            EnsureRegistry();
            return byId.TryGetValue(spellId, out def);
        }

        public static IReadOnlyDictionary<string, Def> Registry
        {
            get { EnsureRegistry(); return view; }
        }

        private static bool Add(string id, Def def)
        {
            if (byId.TryAdd(id, def) || ReferenceEquals(byId[id], def)) return true;
            Log.WarningOnce("[RWoM Core] Duplicate spell ID '" + id + "' on " + def.defName
                + "; keeping " + byId[id].defName + ".", GenText.StableStringHash("rwom.core.duplicate." + id));
            return false;
        }

        private static void EnsureRegistry()
        {
            if (byId != null) return;
            byId = new Dictionary<string, Def>(StringComparer.Ordinal);
            var current = new Dictionary<(Type, string), Def>();
            foreach (var type in GenDefDatabase.AllDefTypesWithDatabases())
                foreach (var def in GenDefDatabase.GetAllDefsInDatabaseForDef(type))
                {
                    if (aliases.Count != 0) current[(def.GetType(), def.defName)] = def;
                    string id = def.GetModExtension<SpellIdentityExtension>()?.spellId;
                    if (!string.IsNullOrWhiteSpace(id)) Add(id, def);
                }
            foreach (var alias in aliases)
                if (current.TryGetValue(alias.Value, out var def)) Add(alias.Key, def);
            view = new ReadOnlyDictionary<string, Def>(byId);
        }
    }
}
