using AbilityUser;
using RimWorld;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic
{
	public class Projectile_LightningStorm : Projectile_AbilityBase
	{

		private IntVec3 strikeLoc = IntVec3.Invalid;

		private int age = -1;

		private bool primed = true;

		private int duration = 480;

		private int boltDelay = 0;

		private int lastStrike = 0;

		private int strikeInt = 0;

        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			bool flag = this.age < duration;
			if (!flag)
			{
				base.Destroy(mode);
			}
		}

		protected override void Impact(Thing hitThing)
		{
			Map map = base.Map;
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Ability);
            Destroy();

            Pawn pawn = this.launcher as Pawn;
            
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
                this.arcaneDmg = pawn.GetCompAbilityUserMight().mightPwr;
            }
            else
            {
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningStorm.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_LightningStorm_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_LightningStorm.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_LightningStorm_ver");
                pwrVal = pwr.level;
                verVal = ver.level;
                this.arcaneDmg = comp.arcaneDmg;
            }
            
            if (settingsRef.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }

            duration = 480 + (verVal * 60);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 8);
			cellRect.ClipInsideMap(map);

			if (this.primed == true)
			{
				if (((this.boltDelay + this.lastStrike) < this.age))
				{
					IntVec3 randomCell = cellRect.RandomCell;
                    if (randomCell.IsValid && randomCell.InBoundsWithNullCheck(base.Map))
                    {
                        //Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, randomCell));
                        Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshFlash(map, randomCell, TM_MatPool.standardLightning, DamageDefOf.Flame, this.launcher, -1, 1.9f, 1f, 1.5f));
                        this.LightningBlast(pwrVal, randomCell, map, 2.2f);
                        strikeInt++;
                        this.lastStrike = this.age;
                        this.boltDelay = Rand.Range(8 - (pwrVal), 40 - (pwrVal * 4));
                         
                        bool flag1 = this.age <= duration;
                        if (!flag1)
                        {
                            this.primed = false;
                            map.weatherDecider.DisableRainFor(0);
                            map.weatherDecider.StartNextWeather();
                        }
                    }
				}
			}
		}

		protected void LightningBlast(int pwr, IntVec3 pos, Map map, float radius)
		{
			ThingDef def = this.def;
            SoundDef exp = TorannMagicDefOf.TM_FireBombSD;
            Explosion(pwr, pos, map, radius, DamageDefOf.EMP, this.launcher, exp, def, this.equipmentDef, ThingDefOf.Spark, 4.4f, 1, false, null, 0f, 1);
            Explosion(pwr, pos, map, radius, DamageDefOf.Stun, this.launcher, exp, def, this.equipmentDef, ThingDefOf.Mote_Stun, 1.4f, 1, false, null, 0f, 1);
            Explosion(pwr, pos, map, radius, DamageDefOf.Bomb, this.launcher, exp, def, this.equipmentDef, TorannMagicDefOf.Mote_Base_Smoke, 0.4f, 1, false, null, 0f, 1);
        }

		public void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
		{
			System.Random rnd = new System.Random();
			int modDamAmountRand = GenMath.RoundRandom(rnd.Next(4 + (pwr*1), projectile.projectile.GetDamageAmount(1,null) + (pwr * 2)));
            modDamAmountRand = Mathf.RoundToInt(modDamAmountRand * this.arcaneDmg);
            if (pwr >= 1)
            {
                radius = (float)(rnd.Next(pwr, pwr*2)/1.8);
            }
			if (map == null)
			{
				Log.Warning("Tried to do explosion in a null map.");
				return;
			}
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = true;
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

		public override void Tick()
		{
			base.Tick();
			this.age++;
		}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.primed, "primed", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
            Scribe_Values.Look<int>(ref this.boltDelay, "boltDelay", 0, false);
            Scribe_Values.Look<int>(ref this.lastStrike, "lastStrike", 0, false);
            Scribe_Values.Look<int>(ref this.strikeInt, "strikeInt", 0, false);
        }
    }
}
