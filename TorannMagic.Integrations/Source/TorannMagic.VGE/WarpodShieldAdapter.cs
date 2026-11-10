using System.Collections.Generic;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace TorannMagic.VGE
{
    public sealed class WarpodShieldAdapter : IMagicalShieldProjectileAdapter
    {
        public bool Matches(Projectile projectile) => MagicalCombatWarpod.IsWarpod(projectile);
        public bool AlreadyIntercepted(Projectile projectile) => MagicalCombatWarpod.IsLanded(projectile);
        public IMagicalShieldInterception Plan(Projectile projectile, MapComponent_MagicalCombat field,
            Vector3 crossing, List<Vector3> centers)
        {
            return MagicalCombatWarpod.TryPlan(projectile, field, crossing, centers, out var landing, out var damage)
                ? new PlanReceipt(projectile, field, landing, damage) : null;
        }
        private sealed class PlanReceipt : IMagicalShieldInterception
        {
            private readonly Projectile projectile;
            private readonly MapComponent_MagicalCombat combatField;
            private readonly MagicalCombatWarpod.Landing landing;
            private bool committed;
            public float Damage { get; }
            public bool CanCommit => !committed && MagicalCombatWarpod.CanCommit(projectile, combatField, landing);
            public PlanReceipt(Projectile projectile, MapComponent_MagicalCombat field, MagicalCombatWarpod.Landing landing, float damage)
            { this.projectile = projectile; combatField = field; this.landing = landing; Damage = damage; }
            public void Commit()
            {
                if (committed) return;
                MagicalCombatWarpod.Commit(projectile, landing);
                committed = true;
            }
        }
    }
}

