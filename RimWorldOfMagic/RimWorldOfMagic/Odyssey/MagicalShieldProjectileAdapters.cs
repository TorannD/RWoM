using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    // Gameplay-specific extension point. Core must not depend on the shield system.
    public interface IMagicalShieldProjectileAdapter
    {
        bool Matches(Projectile projectile);
        bool AlreadyIntercepted(Projectile projectile);
        IMagicalShieldInterception Plan(Projectile projectile, MapComponent_MagicalCombat field,
            Vector3 crossing, List<Vector3> centers);
    }

    public interface IMagicalShieldInterception
    {
        float Damage { get; }
        bool CanCommit { get; }
        // Called once, only after the shield successfully pays Damage.
        void Commit();
    }

    public static class MagicalShieldProjectileAdapters
    {
        private static readonly List<IMagicalShieldProjectileAdapter> adapters = new List<IMagicalShieldProjectileAdapter>();
        public static void Register(IMagicalShieldProjectileAdapter adapter)
        {
            if (adapter == null) return;
            foreach (var existing in adapters)
                if (existing.GetType() == adapter.GetType()) return;
            adapters.Add(adapter);
        }
        public static void Unregister(IMagicalShieldProjectileAdapter adapter) => adapters.Remove(adapter);
        public static IMagicalShieldProjectileAdapter Find(Projectile projectile)
        {
            foreach (var adapter in adapters)
                if (adapter.Matches(projectile)) return adapter;
            return null;
        }
    }
}
