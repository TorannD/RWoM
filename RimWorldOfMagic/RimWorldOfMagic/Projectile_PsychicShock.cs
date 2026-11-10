using System.Linq;
using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
	class Projectile_PsychicShock : ClassProgression.Projectile_DemonField
    {
        protected override TMAbilityDef RankSpell => TorannMagicDefOf.TM_PsychicShock;
        private float arcaneDmg = 1f;
        private List<IntVec3> explosionCenters = new List<IntVec3>();
        private List<int> explosionRadii = new List<int>();
        private HashSet<int> shockedPawns = new HashSet<int>();

        public Projectile_PsychicShock() => duration = 240;

        protected override void ReadRanks(Pawn owner, TMAbilityDef spell)
        {
            var magic = owner.GetCompAbilityUserMagic();
            pwrVal = magic?.MagicData.GetSkill_Power(spell)?.level ?? 0;
            verVal = magic?.MagicData.GetSkill_Versatility(spell)?.level ?? 0;
            if (ModOptions.Settings.Instance.AIHardMode && !owner.IsColonist) pwrVal = verVal = 1;
            arcaneDmg = (magic?.arcaneDmg ?? 1f) * owner.GetStatValue(StatDefOf.PsychicSensitivity,false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            bool hasWaveData = true;
            Scribe_Values.Look(ref hasWaveData,"TM_shockWaveSaved",false,true);
            Scribe_Values.Look(ref initialized,"TM_shockInitialized");
            Scribe_Values.Look(ref age,"TM_shockAge");
            Scribe_Values.Look(ref arcaneDmg,"TM_shockPower",1f);
            Scribe_Collections.Look(ref explosionCenters,"TM_shockCenters",LookMode.Value);
            Scribe_Collections.Look(ref explosionRadii,"TM_shockRadii",LookMode.Value);
            Scribe_Collections.Look(ref shockedPawns,"TM_shockPawns",LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                explosionCenters ??= new List<IntVec3>();
                explosionRadii ??= new List<int>();
                shockedPawns ??= new HashSet<int>();
                // The old projectile did not save its victims or wavefronts. An already
                // landed legacy shot must expire without hitting the same pawns again.
                if ((!hasWaveData && (landed || ticksToImpact <= 0)) || explosionCenters.Count != explosionRadii.Count)
                {
                    initialized = true;
                    age = duration;
                }
            }
        }

        protected override void TickField()
        {
            if (!initialized)
            {
                InitializeRanks(RankSpell);
                if (SkillOwner == null) { Destroy(); return; }
                explosionCenters.Add(Position);
                explosionRadii.Add(1);
                shockedPawns.Add(pawn.thingIDNumber);
                initialized = true;
            }
            if (Find.TickManager.TicksGame % 6 != 0) return;
            for (int i = 0; i < explosionCenters.Count;)
            {
                var center = explosionCenters[i];
                int radius = explosionRadii[i];
                int end = GenRadial.NumCellsInRadius(radius);
                for (int j = GenRadial.NumCellsInRadius(radius - 1); j < end; j++)
                {
                    var cell = center + GenRadial.RadialPattern[j];
                    if (!cell.InBounds(Map)) continue;
                    if (radius <= 3)
                    {
                        float angle = (Quaternion.AngleAxis(90,Vector3.up) * GetVector(center,cell)).ToAngleFlat();
                        TM_MoteMaker.ThrowArcaneWaveMote(cell.ToVector3(),Map,.2f * (cell-center).LengthHorizontal,
                            .1f,.05f,.3f,0,3,angle,angle);
                    }
                    var victim = cell.GetFirstPawn(Map);
                    if (victim == null || victim.Dead) continue;
                    var owner = SkillOwner;
                    if (owner == null) { Destroy(); return; }
                    bool foreign = victim.Faction != owner.Faction;
                    int stun = Mathf.RoundToInt((foreign ? 4f : 1f) * (2 + def.projectile.GetDamageAmount(1,null) + pwrVal) * arcaneDmg);
                    damageEntities(victim,null,stun,DamageDefOf.Stun);
                    if (!victim.Dead && victim.RaceProps.IsFlesh && Rand.Chance(victim.GetStatValue(StatDefOf.PsychicSensitivity,false)) &&
                        shockedPawns.Add(victim.thingIDNumber))
                    {
                        explosionCenters.Add(victim.Position);
                        explosionRadii.Add(1);
                        if (victim != pawn && foreign && Rand.Chance(victim.GetStatValue(StatDefOf.PsychicSensitivity,false)))
                            DoMentalDamage(victim);
                    }
                }
                int count = explosionCenters.Count;
                int maxRadius = (count <= 3 ? 6 : count <= 5 ? 5 : count <= 7 ? 4 : 3) + verVal;
                if (++explosionRadii[i] <= maxRadius) i++;
                else
                {
                    explosionCenters.RemoveAt(i);
                    explosionRadii.RemoveAt(i);
                }
            }
            if (explosionCenters.Count == 0) Destroy();
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force)
        {
            bool flag = targetCell.IsValid && targetCell != default(IntVec3);
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
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, SkillOwner, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
            if (!victim.IsColonist && !victim.IsPrisoner && victim.Faction != null && !victim.Faction.HostileTo(SkillOwner.Faction))
            {
                Faction faction = victim.Faction;
                faction.TryAffectGoodwillWith(SkillOwner.Faction, -100, true, true, TorannMagicDefOf.TM_OffensiveMagic, victim);
            }
        }
    }
}
