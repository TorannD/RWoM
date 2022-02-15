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
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, this.launcher, this.def.projectile.GetDamageAmount(1, null), 0, DamageDefOf.Smoke.soundExplosion, def, this.equipmentDef, null, this.def.projectile.postExplosionSpawnThingDef, this.def.projectile.postExplosionSpawnChance, 1, true, null, 0f, 1, 0f, false);
           
        }		
	}	
}