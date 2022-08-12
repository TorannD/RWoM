using AbilityUser;
using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
	public class Projectile_Fireball : Projectile_AbilityBase
	{
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

		protected override void Impact(Thing hitThing)
		{
            
            Map map = base.Map;
			base.Impact(hitThing);
			ThingDef def = this.def;
            //GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, DamageDefOf.Bomb, this.launcher, SoundDefOf.PlanetkillerImpact, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1);
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius, DamageDefOf.Bomb, this.launcher, Mathf.RoundToInt(Rand.Range(this.def.projectile.GetDamageAmount(1,null)/2, this.def.projectile.GetDamageAmount(1,null)) * this.arcaneDmg), 0, TorannMagicDefOf.TM_SoftExplosion, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0.1f, true);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 5);
			cellRect.ClipInsideMap(map);
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireball.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Fireball_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireball.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Fireball_ver");
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
            if(settingsRef.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }
            for (int i = 0; i < (pwrVal * 3); i++)
			{
				IntVec3 randomCell = cellRect.RandomCell;
                if(randomCell.IsValid && randomCell.InBoundsWithNullCheck(map) && !randomCell.Fogged(map))
                {
                    this.FireExplosion(randomCell, map, 2.2f, ver);
                }
                else
                {
                    i--;
                }
				
			}
		}

		protected void FireExplosion(IntVec3 pos, Map map, float radius, MagicPowerSkill ver)
		{
            ThingDef def = this.def;
            if (verVal == 0)
            {
                Explosion(pos, map, radius, DamageDefOf.Flame, this.launcher, null, def, this.equipmentDef, null, 0.3f, 1, false, null, 0f, 1);
            }
            else if (verVal == 1)
            {
                Explosion(pos, map, radius, TMDamageDefOf.DamageDefOf.TM_Fireball_I, this.launcher, null, def, this.equipmentDef, null, 0.5f, 1, false, null, 0f, 1);
            }
            else if (verVal == 2)
            {
                Explosion(pos, map, radius, TMDamageDefOf.DamageDefOf.TM_Fireball_II, this.launcher, null, def, this.equipmentDef, null, 0.8f, 1, false, null, 0f, 1);
            }
            else if (verVal == 3)
            {
                Explosion(pos, map, radius, TMDamageDefOf.DamageDefOf.TM_Fireball_III, this.launcher, null, def, this.equipmentDef, null, 1.1f, 1, false, null, 0f, 1);
            }
            else
            {
                Log.Message("Fireball Versatility level not recognized: " + verVal);
            }
        }

		public void Explosion(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
		{
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, 1f);
            System.Random rnd = new System.Random();
			int modDamAmountRand = (int)GenMath.RoundRandom(rnd.Next(6, projectile.projectile.GetDamageAmount(1,null) / 2));
            modDamAmountRand = Mathf.RoundToInt(modDamAmountRand * this.arcaneDmg);
			if (map == null)
			{
				Log.Warning("Tried to do explosion in a null map.");
				return;
			}
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
			explosion.Position = center;
			explosion.radius = radius;
			explosion.damType = damType;
			explosion.instigator = instigator;
			explosion.damAmount = ((projectile == null) ? GenMath.RoundRandom((float)damType.defaultDamage) : modDamAmountRand);
			explosion.weapon = source;
			explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
			explosion.preExplosionSpawnChance = preExplosionSpawnChance;
			explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
			explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
			explosion.postExplosionSpawnChance = postExplosionSpawnChance;
			explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
			explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.damageFalloff = true;
            explosion.chanceToStartFire = 0.05f;
            explosion.StartExplosion(explosionSound, null);
            
		}

        public override void Tick()
        {
            Vector3 rndPos = this.DrawPos;
            rndPos.x += Rand.Range(-.4f, .4f);
            rndPos.z += Rand.Range(-.4f, .4f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Heat, rndPos, this.Map, Rand.Range(.5f, .6f), .05f, 0.15f, .1f, Rand.Range(-300, 300), Rand.Range(.8f, 1.3f), Rand.Range(0, 360), Rand.Range(0, 360));
            base.Tick();
        }

    }	
}


