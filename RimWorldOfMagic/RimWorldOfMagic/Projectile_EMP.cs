using AbilityUser;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace TorannMagic
{
	public class Projectile_EMP : Projectile_AbilityBase
	{
        protected override void Impact(Thing hitThing)
        {
            Pawn caster = this.launcher as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;            
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, this.def.projectile.damageDef, this.launcher, (int)(this.def.projectile.GetDamageAmount(1,null) * comp.arcaneDmg), 0, SoundDefOf.Artillery_ShellLoaded, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, true);
            if (caster != null && comp != null)
            {
                if (comp.MagicData?.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 14)
                {
                    List<Pawn> allPawns = TM_Calc.FindAllPawnsAround(map, base.Position, this.def.projectile.explosionRadius);
                    if (allPawns != null && allPawns.Count > 0)
                    {
                        for (int i = 0; i < allPawns.Count; i++)
                        {
                            Pawn e = allPawns[i];
                            if (TM_Calc.IsRobotPawn(e) && Rand.Chance(.4f))
                            {
                                float rnd = Rand.Range(0f, 1f);
                                TM_Action.DoAction_SabotagePawn(e, caster, rnd, 0, 1f, this.launcher);
                            }
                        }
                    }
                }                
            }
        }		
	}	
}


