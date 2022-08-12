using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
	class Projectile_Icebolt : Projectile_AbilityBase
	{
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

        public override void Tick()
        {
            base.Tick();
        }
        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Icebolt.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Icebolt_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Icebolt.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Icebolt_ver");
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
            GenExplosion.DoExplosion(base.Position, map, 0.4f, TMDamageDefOf.DamageDefOf.Iceshard, this.launcher, Mathf.RoundToInt(this.def.projectile.GetDamageAmount(1,null) * this.arcaneDmg), 0, this.def.projectile.soundExplode, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 3);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < Rand.Range((2 + verVal), (3 + 4*verVal)); i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                if (pwrVal > 0)
                {
                    this.Shrapnel(pwrVal, randomCell, map, 0.4f);
                }
                else
                {
                    this.Shrapnel(1, randomCell, map, 0.4f);
                }
                
            }
        }

        protected void Shrapnel(int pwr, IntVec3 pos, Map map, float radius)
        {
            ThingDef def = this.def;
            Explosion(pwr, pos, map, radius, TMDamageDefOf.DamageDefOf.Iceshard, this.launcher, null, def, this.equipmentDef, TorannMagicDefOf.Mote_Base_Smoke, 0.4f, 1, false, null, 0f, 1);

        }

        public void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
        {
            System.Random rnd = new System.Random();
            int modDamAmountRand = GenMath.RoundRandom(Rand.Range(2 + pwr * 2, 5 + TMDamageDefOf.DamageDefOf.Iceshard.defaultDamage * pwr));  //4
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
    }
}
