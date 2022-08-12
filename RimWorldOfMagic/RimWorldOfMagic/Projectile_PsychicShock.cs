using System.Linq;
using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
	class Projectile_PsychicShock : Projectile_AbilityBase
	{
        private bool initialized = false;
        private int age = 0;
        private int duration = 240; //backstop
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;
        private int frequency = 6;
        private int maxRadius = 6;
        List<IntVec3> explosionCenters = new List<IntVec3>();
        List<int> explosionRadii = new List<int>();
        List<Pawn> shockedPawns = new List<Pawn>();
        Pawn pawn;

        IEnumerable<IntVec3> targets;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            ThingDef def = this.def;

            if (!initialized)
            {
                pawn = this.launcher as Pawn;
                float psychicEnergy = pawn.GetStatValue(StatDefOf.PsychicSensitivity, false);
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_PsychicShock.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_PsychicShock_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_PsychicShock.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_PsychicShock_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                this.arcaneDmg = comp.arcaneDmg * psychicEnergy;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 1;
                    verVal = 1;
                }
                explosionCenters.Add(base.Position);
                explosionRadii.Add(1);
                shockedPawns.Add(pawn);
                this.maxRadius += verVal;
                this.initialized = true;
            }

            if (Find.TickManager.TicksGame % frequency == 0)
            {
                for (int i = 0; i < explosionCenters.Count(); i++)
                {
                    if (explosionRadii[i] == 1)
                    {
                        targets = GenRadial.RadialCellsAround(explosionCenters[i], explosionRadii[i], false);
                    }
                    else
                    {
                        IEnumerable<IntVec3> oldTargets = GenRadial.RadialCellsAround(explosionCenters[i], explosionRadii[i] - 1, false);
                        targets = GenRadial.RadialCellsAround(explosionCenters[i], explosionRadii[i], false).Except(oldTargets);
                    }
                    for (int j = 0; j < targets.Count(); j++)
                    {
                        IntVec3 curCell = targets.ToArray<IntVec3>()[j];
                        if (curCell.IsValid && curCell.InBoundsWithNullCheck(pawn.Map))
                        {
                            Vector3 angle = GetVector(explosionCenters[i], curCell);
                            if (explosionRadii[i] <= 3)
                            {
                                TM_MoteMaker.ThrowArcaneWaveMote(curCell.ToVector3(), pawn.Map, .2f * (curCell - explosionCenters[i]).LengthHorizontal, .1f, .05f, .3f, 0, 3, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                            }
                            Pawn victim = curCell.GetFirstPawn(pawn.Map);

                            if (victim != null && !victim.Dead)
                            {
                                if (victim.Faction != this.pawn.Faction)
                                {
                                    //GenExplosion.DoExplosion(curCell, pawn.Map, .4f, DamageDefOf.Stun, this.launcher, Mathf.RoundToInt((4 * (2+this.def.projectile.GetDamageAmount(1, null) + pwrVal) * this.arcaneDmg)), 0, SoundDefOf.Crunch, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                                    damageEntities(victim, null, Mathf.RoundToInt((4 * (2 + this.def.projectile.GetDamageAmount(1, null) + pwrVal) * this.arcaneDmg)), DamageDefOf.Stun);
                                }
                                else
                                {
                                    damageEntities(victim, null, Mathf.RoundToInt(((2 + this.def.projectile.GetDamageAmount(1, null) + pwrVal) * this.arcaneDmg)), DamageDefOf.Stun);
                                    //GenExplosion.DoExplosion(curCell, pawn.Map, .4f, DamageDefOf.Stun, this.launcher, Mathf.RoundToInt(((2+this.def.projectile.GetDamageAmount(1, null) + pwrVal) * this.arcaneDmg)), 0, SoundDefOf.Crunch, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                                }
                            }

                            if (victim != null && !victim.Dead && victim.RaceProps.IsFlesh && Rand.Chance(victim.GetStatValue(StatDefOf.PsychicSensitivity, false)))
                            {
                                if (!shockedPawns.Contains(victim))
                                {
                                    explosionCenters.Add(victim.Position);
                                    shockedPawns.Add(victim);
                                    explosionRadii.Add(1);
                                    if (victim != pawn && victim.Faction != pawn.Faction && Rand.Chance(victim.GetStatValue(StatDefOf.PsychicSensitivity, false)))
                                    {
                                        DoMentalDamage(victim);
                                    }
                                }
                            }
                            victim = null;
                        }
                    }
                    explosionRadii[i]++;
                    if(explosionCenters.Count <= 3)
                    {
                        
                        this.maxRadius = 6 + verVal;
                    }
                    else if(explosionCenters.Count <= 5)
                    {
                        this.maxRadius = 5 + verVal;
                    }
                    else if(explosionCenters.Count <= 7)
                    {
                        this.maxRadius = 4 + verVal;
                    }
                    else
                    {
                        this.maxRadius = 3 + verVal;
                    }
                    
                    if (explosionRadii[i] > this.maxRadius)
                    {
                        explosionRadii.Remove(explosionRadii[i]);
                        explosionCenters.Remove(explosionCenters[i]);
                    }
                    if (explosionCenters.Count() == 0)
                    {
                        this.age = this.duration;
                        this.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force)
        {
            bool flag = targetCell != null && targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), pawn.Position, pawn.Map);
                    flyingObject.speed = 25 + force;
                    flyingObject.Launch(pawn, targetCell, pawn);
                }
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public void DoMentalDamage(Pawn victim)
        {
            BodyPartRecord vitalPart = null;
            if (victim != null && !victim.Dead)
            {
                IEnumerable<BodyPartRecord> partSearch = victim.def.race.body.AllParts;
                vitalPart = partSearch.FirstOrDefault<BodyPartRecord>((BodyPartRecord x) => x.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource));
                if (vitalPart != null)
                {
                    float rnd = Rand.Range(0f, 1f);
                    if(rnd > .97f)
                    {
                        rnd = 5;
                    }
                    else if(rnd > .92f)
                    {
                        rnd = 2;
                    }
                    else if(rnd > .55f)
                    {
                        rnd = 1;
                    }
                    else
                    {
                        rnd = 0;
                    }
                    damageEntities(victim, vitalPart, Mathf.RoundToInt(((this.def.projectile.GetDamageAmount(1,null) + (.5f * pwrVal)) * this.arcaneDmg) * rnd), TMDamageDefOf.DamageDefOf.TM_Shadow);
                }
            }
        }

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            amt = (int)((float)amt * Rand.Range(.75f, 1.25f));
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, this.pawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
            if (!victim.IsColonist && !victim.IsPrisoner && victim.Faction != null && !victim.Faction.HostileTo(pawn.Faction))
            {
                Faction faction = victim.Faction;
                faction.TryAffectGoodwillWith(pawn.Faction, -100, true, true, TorannMagicDefOf.TM_OffensiveMagic, victim);
            }
        }
    }
}
