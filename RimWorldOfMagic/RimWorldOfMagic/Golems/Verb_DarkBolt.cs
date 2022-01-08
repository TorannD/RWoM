using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using RimWorld;
using TorannMagic.Golems;

namespace TorannMagic.Golems
{
    public class Verb_DarkBolt : Verb_GolemShoot
    {
        protected override bool TryCastShot()
        {
            LocalTargetInfo tmpTarget = null;
            Pawn p = currentTarget.Pawn;
            Thing launchedThing = new Thing()
            {
                def = this.verbProps.defaultProjectile
            };

            tmpTarget = currentTarget;
            if (!Rand.Chance(this.verbProps.accuracyLong))
            {
                tmpTarget = TM_Calc.FindValidCellWithinRange(currentTarget.Cell, CasterPawn.Map, 3);
            }
            FlyingObject_Advanced flyingObject = (FlyingObject_Advanced)GenSpawn.Spawn(this.verbProps.defaultProjectile, CasterPawn.Position, CasterPawn.Map);
            flyingObject.AdvancedLaunch(CasterPawn, TorannMagicDefOf.Mote_Shadow, 3, 0, false, CasterPawn.DrawPos, tmpTarget, launchedThing, 40, this.verbProps.defaultProjectile.projectile.explosionRadius > 0, Rand.Range(25,35),
                this.verbProps.defaultProjectile.projectile.explosionRadius, this.verbProps.defaultProjectile.projectile.damageDef, null, 0, this.verbProps.defaultProjectile.projectile.flyOverhead, 1f);
            
            return base.TryCastShot();
        }
    }
}
