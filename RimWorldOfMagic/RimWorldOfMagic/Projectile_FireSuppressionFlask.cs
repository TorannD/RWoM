using AbilityUser;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace TorannMagic
{
	public class Projectile_FireSuppressionFlask : Projectile_AbilityBase
	{
        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            ThingDef def = this.def;
            GenExplosion.DoExplosion(
	            Position, Map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, launcher,
	            damAmount: this.def.projectile.GetDamageAmount(1),
	            armorPenetration: 0,
	            explosionSound: DamageDefOf.Smoke.soundExplosion,
	            weapon: def,
	            projectile: equipmentDef,
	            postExplosionSpawnThingDef: this.def.projectile.postExplosionSpawnThingDef,
	            postExplosionSpawnChance: this.def.projectile.postExplosionSpawnChance,
	            applyDamageToExplosionCellsNeighbors: true
	        );
        }		
	}	
}
