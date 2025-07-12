using Verse;
using RimWorld;
using AbilityUser;
using UnityEngine;

namespace TorannMagic.Weapon
{
    public class Projectile_BlazingPower : Projectile_AbilityBase
    {
        private float arcaneDmg = 1;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn pawn = this.launcher as Pawn;
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (pawn != null)
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                if (comp != null && comp.IsMagicUser)
                {
                    this.arcaneDmg = comp.arcaneDmg;
                }
                try
                {
                    ExplosionHelper.Explode(base.Position, map, this.def.projectile.explosionRadius,
                        TMDamageDefOf.DamageDefOf.TM_BlazingPower, this.launcher,
                        Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1, null) * this.arcaneDmg),
                        2, SoundDefOf.Crunch, def, this.equipmentDef, null,
                        null, 0f, 1, null,
                        false, null, 0f,
                        1, 0.0f, true);
                }
                catch
                {
                    //don't care
                }
            }
        }
    }
}
