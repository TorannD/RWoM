using AbilityUser;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace TorannMagic
{
	public class Projectile_Extinguish : Projectile_AbilityBase
	{
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, this.launcher, this.def.projectile.GetDamageAmount(1, null), 0, SoundDefOf.Artillery_ShellLoaded, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
            Pawn p = this.launcher as Pawn;
            if (p != null)
            {
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                if (comp != null)
                {
                    if (comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 4)
                    {
                        List<Pawn> allPawns = TM_Calc.FindAllPawnsAround(map, base.Position, this.def.projectile.explosionRadius);
                        if (allPawns != null && allPawns.Count > 0)
                        {
                            for (int i = 0; i < allPawns.Count; i++)
                            {
                                Pawn ele = allPawns[i];
                                if(TM_Calc.IsElemental(ele))
                                {
                                    TM_Action.DamageEntities(ele, null, Rand.Range(10, 20), DamageDefOf.Burn, p);
                                }
                                else if(ele.def == TorannMagicDefOf.TM_DemonR)
                                {
                                    TM_Action.DamageEntities(ele, null, Rand.Range(30, 50), DamageDefOf.Burn, p);
                                }
                            }
                        }
                    }
                }
            }
        }
		
	}	
}


