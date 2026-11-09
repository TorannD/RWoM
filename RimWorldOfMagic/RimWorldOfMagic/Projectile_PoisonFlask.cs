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
        private bool trainingCaptured;

        internal void SpawnFog(Map map, IntVec3 cell)
        {
            if (GenSpawn.TrySpawn(TorannMagicDefOf.Fog_Poison, cell, map, out var spawned) && spawned is Gas gas)
                gas.destroyTick = Find.TickManager.TicksGame + duration;
        }

        internal void CaptureTraining(Pawn source)
        {
            if (trainingCaptured) return;
            int versatility=source!=null ? TM_Calc.GetSkillVersatilityLevel(source,TorannMagicDefOf.TM_PoisonFlask,false) : 0;
            int power=source!=null ? TM_Calc.GetSkillPowerLevel(source,TorannMagicDefOf.TM_PoisonFlask,false) : 0;
            radius=def.projectile.explosionRadius+.8f*versatility;
            duration=360+60*power;
            trainingCaptured=true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.age, "age", 0);
            Scribe_Values.Look<float>(ref this.radius, "radius", 4);
            // Old saves retain age/radius but not the cloud's initialization or
            // pulse clock. A nonnegative age means the initial impact already ran;
            // do not recreate its fog/explosion when loading such a save.
            Scribe_Values.Look(ref initialized, "initialized", age >= 0, true);
            Scribe_Values.Look(ref duration, "duration", 360);
            Scribe_Values.Look(ref lastStrike, "lastStrike", age >= 0 ? age - age % strikeDelay : 0, true);
            Scribe_Values.Look(ref trainingCaptured,"trainingCaptured");
        }

        protected override void TickInterval(int delta)
        {
            if (!initialized)
            {
                base.TickInterval(delta);
                return;
            }
            // 1.7 batches projectile movement. Once landed this is a stationary
            // cloud: its lifetime and 40-tick pulses must count elapsed ticks,
            // not calls to the movement/impact callback.
            for (int i = 0; i < delta && !Destroyed; i++) Impact(null);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            age++;            
            if (age < duration)
            {
                Pawn caster = this.launcher as Pawn;
                if (caster != null && !initialized)
                {
                    initialized = true;
                    // Legacy airborne flasks lack a snapshot; new launches bind
                    // the exact ability owner before the carrier can change.
                    if(!trainingCaptured) CaptureTraining(caster);
                    Map map = base.Map;
                    // The damage worker sets each cloud's lifetime during native
                    // explosion propagation; never mutate the shared trap gas Def.
                    GenExplosion.DoExplosion(base.Position, map, radius, TMDamageDefOf.DamageDefOf.TM_Poison, this, 0, 0, SoundDef.Named("TinyBell"), def, null, null, null, 1f, 1, null, null, 0, false, null, 0f, 0, 0.0f, false);
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
                    catch (System.Exception exception)
                    {
                        Compatibility.TM_Log.Error($"Poison flask {thingIDNumber} failed to apply its damage pulse; terminating cloud: {exception}");
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

    public sealed class DamageWorker_PoisonFlask : DamageWorker_AddInjury
    {
        public override void ExplosionAffectCell(Explosion explosion, IntVec3 cell, List<Thing> damagedThings,
            List<Thing> ignoredThings, bool canThrowMotes)
        {
            base.ExplosionAffectCell(explosion, cell, damagedThings, ignoredThings, canThrowMotes);
            // Leave other poison sources and saved legacy post-spawn instructions
            // alone. Native propagation still selects the affected, walkable cells.
            if (canThrowMotes && explosion.postExplosionSpawnThingDef == null &&
                explosion.instigator is Projectile_PoisonFlask flask && cell.Walkable(explosion.Map))
                flask.SpawnFog(explosion.Map, cell);
        }
    }
}


