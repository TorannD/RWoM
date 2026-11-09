using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.Core
{
    // Game-thread-only, read-only admission checks. Providers must not cast,
    // mutate a pawn, consume resources or register/unregister during a query.
    public static class CastOwnership
    {
        private static readonly Dictionary<string, Func<Pawn, bool>> providers =
            new Dictionary<string, Func<Pawn, bool>>(StringComparer.Ordinal);

        public static bool Register(string id, Func<Pawn, bool> provider)
        {
            if (string.IsNullOrWhiteSpace(id) || provider == null || providers.ContainsKey(id)) return false;
            providers.Add(id, provider);
            return true;
        }

        public static bool Unregister(string id, Func<Pawn, bool> provider)
        {
            if (id == null || !providers.TryGetValue(id, out var current) || current != provider) return false;
            return providers.Remove(id);
        }

        public static bool IsCasting(Pawn pawn)
        {
            if (pawn == null) return false;
            foreach (var pair in providers)
            {
                try { if (pair.Value(pawn)) return true; }
                catch (Exception e)
                {
                    // An uncertain owner must not be interrupted by automatic casting.
                    Log.ErrorOnce("RWoM Core cast ownership provider failed: " + pair.Key + ": " + e,
                        pair.Key.GetHashCode());
                    return true;
                }
            }
            return false;
        }
    }
}
