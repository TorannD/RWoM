using AbilityUser;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
	public class Projectile_DistortSpirit : Projectile_AbilityBase
	{
        private int verVal;
        private int pwrVal;
        private int effVal;

        private int age = -1;
        private int duration = 1;
        private float sevBonus = 0;

		protected override void Impact(Thing hitThing)
		{            
            Map map = base.Map;			
			ThingDef def = this.def;
            this.age++;
            this.Destroy(DestroyMode.Vanish);
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            Pawn caster = this.launcher as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            if (comp != null && comp.IsMagicUser)
            {
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_DistortSpirit, false);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_DistortSpirit, false);
                effVal = comp.MagicData.GetSkill_Efficiency(TorannMagicDefOf.TM_DistortSpirit).level;
            }
            if(caster.story != null && caster.story.adulthood != null && caster.story.adulthood.identifier == "tm_vengeful_spirit")
            {
                sevBonus += .06f;
            }
            List<Pawn> pawnList = new List<Pawn>();
            pawnList.Clear();
            int maxPawns = 5 + effVal;
            float radius = this.def.projectile.explosionRadius;
            for(int i = 0; i < map.mapPawns.AllPawnsSpawned.Count; i++)
            {
                Pawn p = map.mapPawns.AllPawnsSpawned[i];
                if((p.Position - this.Position).LengthHorizontal <= radius && p.RaceProps.IsMechanoid && !TM_Calc.IsUndead(p) && !TM_Calc.IsGolem(p))
                {
                    pawnList.Add(p);             
                    if(pawnList.Count >= maxPawns)
                    {
                        break;
                    }
                }                
            }
            float sev = ((.2f+sevBonus) + (.05f * pwrVal * comp.arcaneDmg)); 
            foreach(Pawn p in pawnList)
            {
                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_SpiritDistortionHD, sev);
                if(verVal > 0)
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_SpiritDrainHD, .1f * verVal);
                    if(verVal > 1 && !caster.DestroyedOrNull())
                    {
                        Need_Spirit ns = caster.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
                        if(ns != null)
                        {
                            ns.GainNeed(Rand.Range(.12f, .2f) * verVal * comp.arcaneDmg);
                        }
                    }
                }
            }
            Effecter effect = TorannMagicDefOf.TM_SpiritDistortionED.Spawn();
            effect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
            effect.Cleanup();
            GenClamor.DoClamor(this.launcher, radius, ClamorDefOf.Impact);
            this.age = this.duration;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {            
            if (this.age >= this.duration)
            {
                base.Destroy(mode);
            }
        }
    }	
}


