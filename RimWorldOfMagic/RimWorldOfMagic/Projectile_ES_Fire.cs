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

            GenExplosion.DoExplosion(base.Position, map, 1f + (verVal * .05f), DamageDefOf.Burn, this.launcher, Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1,null) * comp.arcaneDmg * (1f + .02f * verVal)), 0, this.def.projectile.soundExplode, def, this.equipmentDef, this.intendedTarget.Thing, null, 0f, 1, false, null, 0f, 1, 0.0f, false);

        }
    }    
}


