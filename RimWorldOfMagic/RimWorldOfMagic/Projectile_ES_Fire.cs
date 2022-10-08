using System.Linq;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    class Projectile_ES_Fire : Projectile_AbilityBase
    {

        private int verVal = 0;
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;

            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_TechnoWeapon.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoWeapon_ver");
            verVal = ver.level;

            GenExplosion.DoExplosion(
                Position, map, 1f + verVal * .05f, DamageDefOf.Burn, launcher,
                damAmount: Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1) * comp.arcaneDmg * (1f + .02f * verVal)),
                armorPenetration: 0,
                explosionSound: this.def.projectile.soundExplode,
                weapon: def,
                projectile: equipmentDef,
                intendedTarget: intendedTarget.Thing
            );

        }
    }    
}


