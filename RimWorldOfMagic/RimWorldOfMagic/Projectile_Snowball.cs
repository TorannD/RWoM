using System.Linq;
using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
	class Projectile_Snowball : Projectile_AbilityBase
	{

        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

		protected override void Impact(Thing hitThing)
		{
			Map map = base.Map;
			base.Impact(hitThing);
			ThingDef def = this.def;
            
            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Snowball.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Snowball_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Snowball.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Snowball_ver");
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
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
            GenExplosion.DoExplosion(base.Position, map, Mathf.RoundToInt(this.def.projectile.explosionRadius + (0.7f * (float)pwrVal)), TMDamageDefOf.DamageDefOf.Snowball, this.launcher, (int)((this.def.projectile.GetDamageAmount(1,null) + 3*pwrVal) * this.arcaneDmg), 0, SoundDefOf.Crunch, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 3 + (verVal * 1));
			cellRect.ClipInsideMap(map);
			for (int i = 0; i < verVal * 4; i++)
			{
				IntVec3 randomCell = cellRect.RandomCell;
				this.IceExplosion(pwrVal, randomCell, map, (float)verVal * 0.4f);
				Vector3 loc = randomCell.ToVector3Shifted();
				FleckMaker.ThrowSmoke(loc, map, 4.2f);
				FleckMaker.ThrowSmoke(loc, map, 0.6f * (float)pwrVal);
				FleckMaker.ThrowAirPuffUp(loc, map);
				FleckMaker.ThrowDustPuff(loc, map, 1.0f * (float)pwrVal);
				AddSnowRadial(randomCell, map, this.def.projectile.explosionRadius, (this.def.projectile.explosionRadius / 2 )+ (0.7f * (float)verVal));
			}
			AddSnowRadial(base.Position, map, this.def.projectile.explosionRadius + (0.7f * (float)pwrVal), this.def.projectile.explosionRadius + (0.7f * (float)verVal));
		}

		protected void IceExplosion(int pwr, IntVec3 pos, Map map, float radius)
		{
			ThingDef def = this.def;
			Explosion(pwr, pos, map, radius, TMDamageDefOf.DamageDefOf.Snowball, this.launcher, SoundDefOf.Crunch, def, this.equipmentDef, TorannMagicDefOf.Mote_Base_Smoke, 1.2f, 1, false, null, 0f, 1);
            Explosion(pwr, pos, map, radius, DamageDefOf.Extinguish, this.launcher, this.def.projectile.soundExplode, def, this.equipmentDef, null, 1.8f, 1, false, null, 0f, 1);
        }

		public void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
		{
			System.Random rnd = new System.Random();
			int modDamAmountRand = GenMath.RoundRandom(rnd.Next(3, 5+(projectile.projectile.GetDamageAmount(1,null) * pwr)/2)); //7
            modDamAmountRand = Mathf.RoundToInt(modDamAmountRand * this.arcaneDmg);
			if (map == null)
			{
				Log.Warning("Tried to do explosion in a null map.");
				return;
			}
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = false;
            explosion.chanceToStartFire = 0.0f;
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
            explosion.StartExplosion(explosionSound, null);
		}

		public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
		{
			int num = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < num; i++)
			{
				IntVec3 intVec = center + GenRadial.RadialPattern[i];
				if (intVec.InBoundsWithNullCheck(map))
				{
					float lengthHorizontal = (center - intVec).LengthHorizontal;
					float num2 = 1f - lengthHorizontal / radius;
					map.snowGrid.AddDepth(intVec, num2 * depth);
                    
				}
			}
		}

        public override void Tick()
        {
            Vector3 rndPos = this.DrawPos;
            rndPos.x += Rand.Range(-.3f, .3f);
            rndPos.z += Rand.Range(-.3f, .3f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ice, rndPos, this.Map, Rand.Range(.7f, 1.3f), .05f, 0.05f, .15f, Rand.Range(-300, 300), Rand.Range(.8f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
            base.Tick();
        }

    }
}
