using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using AbilityUser;
using Verse.Sound;
using UnityEngine;

namespace TorannMagic.Weapon
{
    class SeerRing_Lightning : Projectile_AbilityLaser
    {


        public override void Impact_Override(Thing hitThing)
        {
            Map map = base.Map;
            if (map == null) return;
            // Misses have no hitThing; damage may also destroy a hit target.
            // Capture the impact cell before invoking either effect path.
            IntVec3 impactCell = hitThing != null ? hitThing.Position : this.ExactPosition.ToIntVec3();
            base.Impact_Override(hitThing);
            Pawn pawn = this.launcher as Pawn;


            bool flag = hitThing != null && hitThing.Spawned && hitThing.Map == map;
            if (flag)
            {
                int DamageAmount = this.def.projectile.GetDamageAmount(1,null);
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, DamageAmount, .25f, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown);
                hitThing.TakeDamage(dinfo);

                bool flag2 = this.canStartFire && Rand.Range(0f, 1f) > this.startFireChance;
                if (flag2)
                {
                    hitThing.TryAttachFire(0.05f, null);
                }
                Pawn hitTarget;
                bool flag3 = (hitTarget = (hitThing as Pawn)) != null;
                if (flag3)
                {
                    this.PostImpactEffects(this.launcher as Pawn, hitTarget);
                }
            }
            EmitImpactVisuals(impactCell, map, hitThing == null || hitThing is Pawn);
            CellRect cellRect = CellRect.CenteredOn(impactCell, 2);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < Rand.Range(1, 8); i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                this.StaticExplosion(randomCell, map, 0.4f);
            }
        }

        // Cosmetic boundary kept separate from the hit and scatter effects.
        public static void EmitImpactVisuals(IntVec3 cell, Map map, bool sparks)
        {
            if (sparks)
            {
                FleckMaker.Static(cell.ToVector3Shifted(), map, FleckDefOf.MicroSparks);
                FleckMaker.Static(cell.ToVector3Shifted(), map, FleckDefOf.ShotHit_Dirt);
            }
            for (int i = 0; i < 2; i++)
                SoundDefOf.Thunder_OnMap.PlayOneShot(SoundInfo.InMap(new TargetInfo(cell, map, false), MaintenanceType.None));
        }

        protected void StaticExplosion(IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;
            Explosion(pos, map, radius, TMDamageDefOf.DamageDefOf.TM_Lightning, this.launcher, null, def, this.equipmentDef, ThingDefOf.Spark, 0.4f, 1, false, null, 0f, 1);

        }

        public static void Explosion(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = true, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        {
            System.Random rnd = new System.Random();
            int modDamAmountRand = GenMath.RoundRandom(Rand.Range(2, projectile.projectile.GetDamageAmount(1,null) / 2));
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
            explosion.armorPenetration = 1.5f;
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
