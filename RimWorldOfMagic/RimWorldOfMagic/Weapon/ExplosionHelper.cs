using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Weapon
{
    public class ExplosionHelper
    {
        public static void Explode(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator,
            int damAmount = -1, float armorPenetration = -1f, SoundDef explosionSound = null,
            ThingDef weapon = null, ThingDef projectile = null, Thing intendedTarget = null,
            ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0.0f,
            int postExplosionSpawnThingCount = 1, GasType? postExplosionGasType = null,
            bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null,
            float preExplosionSpawnChance = 0.0f, int preExplosionSpawnThingCount = 1,
            float chanceToStartFire = 0.0f, bool damageFalloff = false,
            float? direction = null, List<Thing> ignoredThings = null,
            FloatRange? affectedAngle = null, bool doVisualEffects = true,
            float propagationSpeed = 1f, float excludeRadius = 0.0f,
            bool doSoundEffects = true, ThingDef postExplosionSpawnThingDefWater = null,
            float screenShakeFactor = 1f, SimpleCurve flammabilityChanceCurve = null,
            List<IntVec3> overrideCells = null, ThingDef postExplosionSpawnSingleThingDef = null,
            ThingDef preExplosionSpawnSingleThingDef = null,
            float? postExplosionGasRadiusOverride = null, int postExplosionGasAmount = 255 /*0xFF*/)
        {
            GenExplosion.DoExplosion(center, map, radius, damType, instigator, damAmount, armorPenetration,
                explosionSound, weapon, projectile, intendedTarget, postExplosionSpawnThingDef,
                postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType,
                postExplosionGasRadiusOverride, postExplosionGasAmount, applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, chanceToStartFire,
                damageFalloff, direction, ignoredThings, affectedAngle, doVisualEffects, propagationSpeed, excludeRadius,
                doSoundEffects, postExplosionSpawnThingDefWater, screenShakeFactor, flammabilityChanceCurve,
                overrideCells, postExplosionSpawnSingleThingDef, preExplosionSpawnSingleThingDef);
        }
    }
}