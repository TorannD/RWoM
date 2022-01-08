using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemAbilityEffect_AdvancedLaunch : GolemAbilityEffect
    {
        public ThingDef projectileDef = null;
        public int varianceMin = 0;
        public int varianceMax = 0;
        public int flySpeedMin = 10;
        public int flySpeedMax = 50;
        public float explosionRadius = 0f;
        public float missRadius = 1f;
        public int damageMin = 10;
        public int damageMax = 20;
        public int projectilesPerBurst = 1;
        DamageDef damageType;
        bool flyOverhead = true;
        float accuracy = 1f;
        float rangeAroundTarget = 0;
        

        public override void DoEffect(Pawn launcher, LocalTargetInfo target)
        {
            base.DoEffect(launcher, target);
            LocalTargetInfo tmpTarget = null;
            if(damageType == null)
            {
                damageType = DamageDefOf.Bomb;
            }
            Thing launchedThing = new Thing()
            {
                def = projectileDef
            };
            if (rangeAroundTarget != 0)
            {
                List<Pawn> tmpPawns = TM_Calc.FindPawnsNearTarget(launcher, Mathf.RoundToInt(rangeAroundTarget), target.Cell, true);
                if (tmpPawns != null)
                {
                    tmpTarget = new LocalTargetInfo(tmpPawns.RandomElement());
                }
            }
            for (int i = 0; i < projectilesPerBurst; i++)
            {
                if (!Rand.Chance(accuracy))
                {
                    tmpTarget = TM_Calc.FindValidCellWithinRange(tmpTarget.Cell, launcher.Map, missRadius);
                }
                FlyingObject_Advanced flyingObject = (FlyingObject_Advanced)GenSpawn.Spawn(this.projectileDef, launcher.Position, launcher.Map);
                flyingObject.AdvancedLaunch(launcher, projectileDef.projectile.preExplosionSpawnThingDef, projectileDef.projectile.preExplosionSpawnThingCount, Mathf.Clamp(Rand.Range(varianceMin, varianceMax), 
                    0, tmpTarget.Cell.DistanceToEdge(launcher.Map)), false, launcher.DrawPos, tmpTarget, launchedThing, Rand.Range(flySpeedMin, flySpeedMax), explosionRadius > 0, Rand.Range(damageMin, damageMax), 
                    explosionRadius, damageType, null, 0, flyOverhead, moteEffectSize: projectileDef.projectile.preExplosionSpawnChance);
            }
        }
    }
}
