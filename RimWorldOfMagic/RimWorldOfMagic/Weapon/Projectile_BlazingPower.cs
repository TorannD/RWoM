using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using AbilityUser;
using System.Linq;
using UnityEngine;

namespace TorannMagic.Weapon
{
    public class Projectile_BlazingPower : Projectile_AbilityBase
    {
        private float arcaneDmg = 1;

        protected override void Impact(Thing hitThing)
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
                    //TM_MoteMaker.MakePowerBeamMotePsionic(base.Position, map, 12f, 2f, .7f, .1f, .6f);
                    //List<Thing> thingList = base.Position.GetThingList(map);
                    //for(int i = 0; i < thingList.Count; i++)
                    //{
                    //    DamageEntities(thingList[i], null, this.def.projectile.GetDamageAmount(1, null), TMDamageDefOf.DamageDefOf.TM_BlazingPower, pawn);
                    //}
                    
                    GenExplosion.DoExplosion(base.Position, map, .8f, TMDamageDefOf.DamageDefOf.TM_BlazingPower, this.launcher, Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1, null) * this.arcaneDmg), 2, SoundDefOf.Crunch, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0.0f, true);
                }
                catch
                {
                    //don't care
                }
            }
        }
    }
}
