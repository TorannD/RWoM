using System;
using RimWorld;
using Verse;
using AbilityUser;

namespace TorannMagic.Weapon
{
    class SeerRing_Ice : Projectile_AbilityBase
    {
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            Pawn pawn = this.launcher as Pawn;
            try
            {
                GenExplosion.DoExplosion(base.Position, map, 0.4f, TMDamageDefOf.DamageDefOf.Iceshard, this.launcher, this.def.projectile.GetDamageAmount(1,null), 3, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
            }
            catch
            {
                //don't care
            }
            CellRect cellRect = CellRect.CenteredOn(base.Position, 2);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < Rand.Range(2, 10); i++)
            {
                try
                {
                    IntVec3 randomCell = cellRect.RandomCell;
                    this.Shrapnel(2, randomCell, map, 0.4f);
                }
                catch
                {
                    //don't care
                }
            }
        }

        protected void Shrapnel(int pwr, IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;
            Explosion(pwr, pos, map, radius, TMDamageDefOf.DamageDefOf.Iceshard, this.launcher, null, def, this.equipmentDef, TorannMagicDefOf.Mote_Base_Smoke, 0.4f, 1, false, null, 0f, 1);

        }

        public static void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        {
            int modDamAmountRand = GenMath.RoundRandom(Rand.Range(3, projectile.projectile.GetDamageAmount(1,null) / 2));  
            if (map == null)
            {
                Log.Warning("Tried to do explosion in a null map.");
                return;
            }
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = false;
            explosion.chanceToStartFire = 0.0f;
            explosion.Position = center;
            explosion.radius = radius;
            explosion.damType = damType;
            explosion.instigator = instigator;
            explosion.damAmount = ((projectile == null) ? GenMath.RoundRandom((float)damType.defaultDamage) : modDamAmountRand);
            explosion.armorPenetration = 2;
            explosion.weapon = source;
            explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
            explosion.preExplosionSpawnChance = preExplosionSpawnChance;
            explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
            explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
            explosion.postExplosionSpawnChance = postExplosionSpawnChance;
            explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
            explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.StartExplosion(explosionSound, null);
        }
    }
}
