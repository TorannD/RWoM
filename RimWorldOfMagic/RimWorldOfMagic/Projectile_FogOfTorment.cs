using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;


namespace TorannMagic
{
    class Projectile_FogOfTorment : ClassProgression.Projectile_SpiritImpact
    {
        int age = -1;
        int duration = 1440;
        protected override string SkillStem => "TM_FogOfTorment";
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_FogOfTorment;
        internal static int Duration(int versatility) => 1440 + 180 * versatility;
        int strikeDelay = 120;
        int lastStrike = 0;
        bool initialized = false;


        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            this.age++;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing, blockedByShield);
            if (Destroyed || map == null) return;
            Pawn pawn = SourcePawn;
            if (pawn == null) { age = duration; Destroy(); return; }
            if (!projected)
            {
                var magic = pawn.GetCompAbilityUserMagic();
                if (magic?.MagicData != null)
                {
                    var skills = Skills(magic.MagicData);
                    pwrVal = SpellRank(pawn, skills, SkillStem, "_pwr");
                    verVal = SpellRank(pawn, skills, SkillStem, "_ver");
                    arcaneDmg = magic.arcaneDmg;
                    if (ModOptions.Settings.Instance.AIHardMode && !pawn.IsColonist) pwrVal = verVal = 3;
                }
                else if (pawn.def == TorannMagicDefOf.TM_SkeletonLichR && !initialized)
                {
                    pwrVal = Rand.RangeInclusive(0, 3);
                    verVal = Rand.RangeInclusive(0, 3);
                }
            }

            if (!initialized)
            {
                duration = Duration(verVal);
                strikeDelay = 120 - 18 * verVal;
                initialized = true;
                GenExplosion.DoExplosion(Position, map, def.projectile.explosionRadius + verVal,
                    TMDamageDefOf.DamageDefOf.TM_Torment, SpellInstigator, 0, 0, def.projectile.soundExplode, def, equipmentDef);
            }

            if (this.age > this.lastStrike + this.strikeDelay)
            {
                foreach (var curCell in GenRadial.RadialCellsAround(Position, def.projectile.explosionRadius + verVal, true))
                {
                    if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                    {
                        var victim = curCell.GetFirstPawn(map);
                        if(victim != null && !victim.Dead && victim.RaceProps.FleshType.isOrganic)
                        {
                            if(TM_Calc.IsUndead(victim))
                            {
                                //heals undead
                                Hediff_Injury injuryToHeal = victim.health.hediffSet.hediffs
                                    .OfType<Hediff_Injury>()
                                    .FirstOrDefault(injury => injury.CanHealNaturally());
                                injuryToHeal?.Heal(2.0f + pwrVal);

                            }
                            else
                            {
                                //kills living
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(pawn, victim) - .4f))
                                {
                                    float targetMassFactor = victim.def.BaseMass != 0 ? 50f/victim.def.BaseMass : 1f;
                                    damageEntities(victim, Mathf.RoundToInt(Rand.Range(2f + (1f * pwrVal), 4f + (1f * pwrVal)) * this.arcaneDmg * targetMassFactor), TMDamageDefOf.DamageDefOf.TM_Torment);
                                }
                            }
                        }
                    }
                }
                this.lastStrike = this.age;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1440, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "shockDelay", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrike, "lastStrike", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look(ref arcaneDmg, "TM_fogScale", 1f);
        }

        public void damageEntities(Pawn e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.5f, 1.5f) * d);
            e?.TakeDamage(new DamageInfo(type, amt, 0, -1f, SpellInstigator));
        }
    }

    public sealed class DamageWorker_Torment : DamageWorker_AddInjury
    {
        public override void ExplosionAffectCell(Explosion explosion, IntVec3 cell, List<Thing> damagedThings,
            List<Thing> ignoredThings, bool canThrowMotes)
        {
            base.ExplosionAffectCell(explosion, cell, damagedThings, ignoredThings, canThrowMotes);
            // Old in-flight explosions still use their saved native post-spawn instruction.
            if (!canThrowMotes || explosion.weapon != TorannMagicDefOf.Projectile_FogOfTorment ||
                explosion.postExplosionSpawnThingDef != null || !cell.Walkable(explosion.Map)) return;
            if (GenSpawn.TrySpawn(TorannMagicDefOf.Fog_Torment, cell, explosion.Map, out var spawned) && spawned is Gas gas)
            {
                int rank = Mathf.RoundToInt(explosion.radius - explosion.weapon.projectile.explosionRadius);
                gas.destroyTick = Find.TickManager.TicksGame + Projectile_FogOfTorment.Duration(rank);
            }
        }
    }
}
