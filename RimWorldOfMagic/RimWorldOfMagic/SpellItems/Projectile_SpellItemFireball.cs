using RimWorld;
using Verse;

namespace TorannMagic.SpellItems
{
    // Item fireballs have no ability-user dependency or class/mana scaling.
    public class Projectile_SpellItemFireball : Projectile
    {
        public const float BlastRadius = 1.9f;
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // Base Impact destroys the projectile; retain the impact location first.
            Map map = Map;
            IntVec3 center = Position;
            Thing instigator = launcher;
            ThingDef projectileDef = def;
            base.Impact(hitThing, blockedByShield);
            EmitFireballImpact(center, map, instigator, projectileDef);
        }

        // Isolated explosion boundary for headless impact/launch contracts.
        public static void EmitFireballImpact(IntVec3 center, Map map, Thing launcher, ThingDef projectile)
        {
            if (map == null || !center.InBounds(map)) return;
            GenExplosion.DoExplosion(center, map, BlastRadius, DamageDefOf.Flame, launcher,
                damAmount: 18, armorPenetration: 0f, explosionSound: TorannMagicDefOf.TM_SoftExplosion,
                projectile: projectile, chanceToStartFire: 0.1f, damageFalloff: false);
        }
    }
}
