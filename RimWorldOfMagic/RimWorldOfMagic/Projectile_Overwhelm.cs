using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace TorannMagic
{
    class Projectile_Overwhelm : Projectile_AbilityBase
    {

        IntVec3 pos;
        MagicPowerSkill pwr;
        MagicPowerSkill ver;
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;
        private int strikeNum = 1;
        private bool initialized = false;

        private void Initialize(Pawn pawn)
        {
            GenClamor.DoClamor(this.launcher, 5f, ClamorDefOf.Impact);
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overwhelm.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overwhelm_pwr");
            ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Overwhelm.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Overwhelm_ver");
            
            pwrVal = pwr.level;
            verVal = ver.level;
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }
            this.arcaneDmg = comp.arcaneDmg;
            if (ModOptions.Settings.Instance.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }
            this.strikeNum = 1;
            initialized = true;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            //base.Impact(hitThing);
            
            Pawn pawn = this.launcher as Pawn;
            Map map = Map;
            if (!HasValidCaster(pawn, map))
            {
                Destroy();
                return;
            }
            if(!initialized)
            {
                Initialize(pawn);
                TM_Action.InvulnerableAoEFor(pawn.Position, map, 3 + verVal, Mathf.RoundToInt((90 + (10 * pwrVal)) * arcaneDmg), pawn.Faction);
            }

            if (Find.TickManager.TicksGame % 3 == 0)
            {
                DoBurstExplosion(pawn, map);
                this.strikeNum++;
            }
            if(strikeNum > 3 + verVal)
            {
                Destroy();
            }

        }        

        private void DoBurstExplosion(Pawn pawn, Map map)
        {
            if (!HasValidCaster(pawn, map)) return;
            foreach (var cell in GetBurstCells(pawn.Position, strikeNum, map))
                HolyExplosion(pwrVal, verVal, cell, map, 0.4f);
        }

        private static bool HasValidCaster(Pawn pawn, Map map)
        {
            return !pawn.DestroyedOrNull() && pawn.Spawned && !pawn.Dead &&
                map != null && pawn.Map == map && pawn.Position.InBounds(map);
        }

        internal static IEnumerable<IntVec3> GetBurstCells(IntVec3 center, int radius, Map map)
        {
            if (map == null || !center.InBounds(map) || radius < 1) yield break;
            // Both circles must follow the same caster position, including when
            // the caster has moved away from the projectile between strikes.
            var inner = radius > 1
                ? new HashSet<IntVec3>(GenRadial.RadialCellsAround(center, radius - 1, false))
                : null;
            foreach (var cell in GenRadial.RadialCellsAround(center, radius, false))
            {
                // Rings crossing the map boundary are normal, not a failure.
                if (cell.InBounds(map) && (inner == null || !inner.Contains(cell)))
                    yield return cell;
            }
        }

        protected void HolyExplosion(int pwr, int ver, IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;
            Explosion(pwr, pos, map, radius, TMDamageDefOf.DamageDefOf.TM_Overwhelm, this.launcher, null, def, this.equipmentDef, null, 0.4f, 1, false, null, 0f, 1);
            
            if (ver >= 2)
            {
                int rnd = Rand.Range(3, 12);
                if (rnd >= 5)
                {
                    Explosion(pwr, pos, map, radius, DamageDefOf.Stun, this.launcher, null, def, this.equipmentDef, null, 0.0f, 1, false, null, 0f, 1);
                }
            }
            //MoteMaker.MakeStaticMote(pos, map, ThingDefOf.Mote_HeatGlow, 2f);

        }

        public void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        {
            if (map == null)
            {
                Log.Warning("Tried to do explosion in a null map.");
                return;
            }
            if (!center.InBounds(map)) return;
            System.Random rnd = new System.Random();
            int modDamAmountRand = (pwr * 3) + GenMath.RoundRandom(rnd.Next(5, projectile.projectile.GetDamageAmount(1,null)));
            modDamAmountRand = Mathf.RoundToInt(modDamAmountRand * this.arcaneDmg);
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = false;
            explosion.chanceToStartFire = 0.0f;
            explosion.Position = center;
            explosion.radius = radius;
            explosion.damType = damType;
            explosion.instigator = instigator;
            explosion.damAmount =  ((projectile == null) ? GenMath.RoundRandom((float)damType.defaultDamage) : modDamAmountRand);
            explosion.weapon = source;
            explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
            explosion.preExplosionSpawnChance = preExplosionSpawnChance;
            explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
            explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
            explosion.postExplosionSpawnChance = postExplosionSpawnChance;
            explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
            explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.StartExplosion(explosionSound, null);            
        }

        private void DoPatternExplosion(Pawn pawn, Map map)
        {
            pos = pawn.Position;

            pos.x++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.z--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.x--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.x--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.x++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);
            pos.x++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);


            pos.x++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x++;
            pos.z--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x++;
            pos.z--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.z--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x -= 3;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x--;
            pos.z--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x--;
            pos.z--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x--;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.z += 3;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x--;
            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x--;
            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x += 3;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x++;
            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x++;
            pos.z++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            pos.x++;
            HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            if (verVal >= 1)
            {
                pos.x++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 3;
                pos.z--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 3;
                pos.z++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z -= 3;
                pos.x += 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z -= 3;
                pos.x--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 3;
                pos.z += 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 3;
                pos.z--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z += 3;
                pos.x -= 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 3;
                pos.z++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 6;
                pos.z += 4;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 4;
                pos.z -= 6;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 6;
                pos.z -= 4;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            }

            if (verVal >= 3)
            {
                pos.x++;
                pos.z++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x--;
                pos.z += 3;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x++;
                pos.z += 3;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z += 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 3;
                pos.z--;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 3;
                pos.z++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x++;
                pos.z -= 3;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x--;
                pos.z -= 3;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z -= 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 3;
                pos.z++;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x += 6;
                pos.z -= 2;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z += 10;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.x -= 10;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

                pos.z -= 10;
                HolyExplosion(pwrVal, verVal, pos, map, 0.4f);

            }
        }
    }
}
