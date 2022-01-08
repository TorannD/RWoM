using System.Linq;
using System;
using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
	public class Projectile_BreachingCharge : Projectile_AbilityBase
	{

        private bool initialized = false;
        private int verVal = 0;
        private float mightPwr = 1f;
        private int ticksToDetonation = 210;
        private int explosionCount = 5;
        Pawn caster;

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                CompAbilityUserMight comp = caster.GetComp<CompAbilityUserMight>();
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_ShotgunSpec, "TM_ShotgunSpec", "_ver", true); 
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_ShotgunSpec);
                this.explosionCount = 5;
                if(verVal >= 3)
                {
                    this.explosionCount++;
                }
                this.initialized = true;
            }

            landed = true;
            ticksToDetonation = def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction);            
        }

        private void Explode()
        {
            if (explosionCount > 0)
            {
                GenExplosion.DoExplosion(base.Position, base.Map, this.def.projectile.explosionRadius-explosionCount, this.def.projectile.damageDef, this.launcher as Pawn, Mathf.RoundToInt((this.def.projectile.GetDamageAmount(1f) * (1f + (.15f * verVal))) * mightPwr), this.def.projectile.damageDef.defaultArmorPenetration, this.def.projectile.damageDef.soundExplosion, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
            }
            else
            {
                Effecter exp = TorannMagicDefOf.GiantExplosion.Spawn();
                exp.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
                exp.Cleanup();
                this.Destroy(DestroyMode.Vanish);
            }
            explosionCount--;
        }

        public override void Tick()
        {
            base.Tick();
            ticksToDetonation--;
            if (ticksToDetonation <= 0)
            {
                Explode();
            }               
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (explosionCount <= 0)
            {
                base.Destroy(mode);
            }
        }
    }
}
