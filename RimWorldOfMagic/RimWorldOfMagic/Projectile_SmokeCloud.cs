using AbilityUser;
using RimWorld;
using Verse;
using System.Linq;
using System;
using System.Collections.Generic;

namespace TorannMagic
{
	public class Projectile_SmokeCloud : Projectile_AbilityBase
	{
		protected override void Impact(Thing hitThing)
		{            
            Map map = base.Map;
			base.Impact(hitThing);
			ThingDef def = this.def;
            Pawn p = launcher as Pawn;
            float explosionRadius = this.def.projectile.explosionRadius;
            if (p != null)
            {
                if (p.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 2)
                {
                    explosionRadius += 2f;
                }
                if (p.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 1)
                {
                    List<Pawn> blindedPawns = TM_Calc.FindAllPawnsAround(map, base.Position, explosionRadius);
                    if (blindedPawns != null && blindedPawns.Count > 0)
                    {
                        for (int i = 0; i < blindedPawns.Count; i++)
                        {
                            if (blindedPawns[i].Faction != null && blindedPawns[i].Faction.HostileTo(p.Faction))
                            {
                                HealthUtility.AdjustSeverity(blindedPawns[i], HediffDef.Named("TM_Blind"), .6f);
                            }
                            else if (blindedPawns[i].Faction != p.Faction)
                            {
                                HealthUtility.AdjustSeverity(blindedPawns[i], HediffDef.Named("TM_Blind"), .6f);
                                if (blindedPawns[i].RaceProps.Animal)
                                {
                                    if (Rand.Chance(.5f))
                                    {
                                        blindedPawns[i].mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false, false, null, false);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            GenExplosion.DoExplosion(base.Position, map, explosionRadius, this.def.projectile.damageDef, this.launcher, this.def.projectile.GetDamageAmount(1,null), 0, SoundDefOf.Artillery_ShellLoaded, def, this.equipmentDef, null, ThingDefOf.Gas_Smoke, 1f, 1, false, null, 0f, 1, 0f, false);

		}		
	}	
}


