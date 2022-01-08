using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_Mk203GL : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int verVal = 0;
        private float mightPwr = 1f;
        IntVec3 strikePos = default(IntVec3);
        Pawn caster;
        private float radius = 4;

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                this.strikePos = base.Position;
                CompAbilityUserMight comp = caster.GetComp<CompAbilityUserMight>();
                //verVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_RifleSpec, "TM_RifleSpec", "_ver", true);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_RifleSpec, true);
                this.radius = this.def.projectile.explosionRadius;
                this.initialized = true;
            }

            GenExplosion.DoExplosion(this.strikePos, map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, this.launcher as Pawn, Mathf.RoundToInt((this.def.projectile.GetDamageAmount(1f) + (1.5f * verVal)) * mightPwr), 3, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
            strikePos.x += Mathf.RoundToInt(Rand.Range(-radius, radius));
            strikePos.z += Mathf.RoundToInt(Rand.Range(-radius, radius));
            GenExplosion.DoExplosion(this.strikePos, map, this.def.projectile.explosionRadius/2f, this.def.projectile.damageDef, this.launcher as Pawn, Mathf.RoundToInt(((this.def.projectile.GetDamageAmount(1f)/2f) + (1f * verVal)) * mightPwr), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
            strikePos = base.Position;
            strikePos.x += Mathf.RoundToInt(Rand.Range(-radius, radius));
            strikePos.z += Mathf.RoundToInt(Rand.Range(-radius, radius));
            GenExplosion.DoExplosion(this.strikePos, map, this.def.projectile.explosionRadius/2f, this.def.projectile.damageDef, this.launcher as Pawn, Mathf.RoundToInt(((this.def.projectile.GetDamageAmount(1f) / 2f) + (1f * verVal)) * mightPwr), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
        }

    }
}


