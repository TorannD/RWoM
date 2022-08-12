using AbilityUser;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;


namespace TorannMagic
{
	public class Projectile_PoisonFlask : Projectile_AbilityBase
	{
        private int age = -1;
        private int duration = 360;
        private bool initialized = false;
        private float radius = 4;
        private int strikeDelay = 40;
        private int lastStrike = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.age, "age", 0);
            Scribe_Values.Look<float>(ref this.radius, "radius", 4);
        }

        protected override void Impact(Thing hitThing)
        {
            age++;            
            if (age < duration)
            {
                Pawn caster = this.launcher as Pawn;
                if (caster != null && !initialized)
                {
                    initialized = true;
                    CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
                    int verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_PoisonFlask, false);
                    int pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_PoisonFlask, false);
                    Map map = base.Map;
                    radius = this.def.projectile.explosionRadius + (.8f * verVal);
                    duration = 360 + (60 * pwrVal);
                    ThingDef fog = TorannMagicDefOf.Fog_Poison;
                    fog.gas.expireSeconds.min = duration / 60;
                    fog.gas.expireSeconds.max = duration / 60;
                    GenExplosion.DoExplosion(base.Position, map, radius, TMDamageDefOf.DamageDefOf.TM_Poison, this, 0, 0, SoundDef.Named("TinyBell"), def, null, null, fog, 1f, 1, false, null, 0f, 0, 0.0f, false);
                }

                if (this.age >= this.lastStrike + this.strikeDelay)
                {
                    try
                    {
                        List<Pawn> pList = (from pawn in this.Map.mapPawns.AllPawnsSpawned
                                            where (!pawn.Dead && (pawn.Position - base.Position).LengthHorizontal <= radius && pawn.RaceProps != null && pawn.RaceProps.IsFlesh)
                                            select pawn).ToList();

                        for (int i = 0; i < pList.Count(); i++)
                        {
                            Pawn victim = pList[i];
                            List<BodyPartRecord> bprList = new List<BodyPartRecord>();
                            bprList.Clear();
                            BodyPartRecord bpr = null;
                            foreach (BodyPartRecord record in victim.def.race.body.AllParts)
                            {
                                if (record.def.tags.Contains(BodyPartTagDefOf.BreathingSource) || record.def.tags.Contains(BodyPartTagDefOf.BreathingPathway))
                                {
                                    if (victim.health != null && victim.health.hediffSet != null && !victim.health.hediffSet.PartIsMissing(record))
                                    {
                                        bprList.Add(record);
                                    }
                                }
                            }
                            if (bprList != null && bprList.Count > 0 && caster != null)
                            {
                                TM_Action.DamageEntities(victim, bprList.RandomElement(), Rand.Range(1f, 2f), 2f, TMDamageDefOf.DamageDefOf.TM_Poison, caster);
                            }
                        }
                    }
                    catch
                    {
                        Log.Message("Debug: poison trap failed to process triggered event - terminating poison trap");
                        age = this.duration;
                    }
                    this.lastStrike = this.age;
                }
            }
            base.Impact(hitThing);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (age >= this.duration)
            {
                base.Destroy(mode);
            }
        }
    }	
}


