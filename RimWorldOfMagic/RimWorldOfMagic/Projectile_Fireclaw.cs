using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
	class Projectile_Fireclaw : Projectile_AbilityBase
	{
		private IntVec3 strikeLoc = IntVec3.Invalid;

		private int age = -1;
        //private int j = 0;
        //private int tendrilCount = 10;
        //private int offspring = 1;
        //private int offspringLimit = 3;

		private int duration = 60;
		private int delay = 1;
		private int lastStrike = 0;

        private bool cflag = true;
        private bool oflag = true;
        private bool primed = true;

        float hyp = 0;
        float angleRad = 0;
        float angleDeg = 0;
        float xProb = 0;

        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1;

        //private List<IntVec3> currentPos = new List<IntVec3>();
        //private List<int> posAge =  new List<int>();
        //private List<bool> posFlag =  new List<bool>();
        //private List<float> posxWeight =  new List<float>();
        //private List<float> poszWeight =  new List<float>();

        private IntVec3 currentPos = IntVec3.Invalid;
        private IntVec3 currentPos1 = IntVec3.Invalid;
        private IntVec3 currentPos11 = IntVec3.Invalid;
        private IntVec3 currentPos12 = IntVec3.Invalid;
        private IntVec3 currentPos2 = IntVec3.Invalid;
        private IntVec3 currentPos21 = IntVec3.Invalid;
        private IntVec3 currentPos22 = IntVec3.Invalid;
        private IntVec3 currentPos3 = IntVec3.Invalid;
        private IntVec3 currentPos31 = IntVec3.Invalid;
        private IntVec3 currentPos4 = IntVec3.Invalid;
        private IntVec3 currentPos5 = IntVec3.Invalid;

        bool posFlag = false;
        bool pos1Flag = false;
        bool pos11Flag = false;
        bool pos12Flag = false;
        bool pos2Flag = false;
        bool pos21Flag = false;
        bool pos22Flag = false;
        bool pos3Flag = false;
        bool pos31Flag = false;
        bool pos4Flag = false;
        bool pos5Flag = false;

        bool posFlagw = true;
        bool pos1Flagw = true;
        bool pos11Flagw = true;
        bool pos12Flagw = true;
        bool pos2Flagw = true;
        bool pos21Flagw = true;
        bool pos22Flagw = true;
        bool pos3Flagw = true;
        bool pos31Flagw = true;
        bool pos4Flagw = true;
        bool pos5Flagw = true;

        private int posAge;
        private int pos1Age;
        private int pos11Age;
        private int pos12Age;
        private int pos2Age;
        private int pos21Age;
        private int pos22Age;
        private int pos3Age;
        private int pos31Age;
        private int pos4Age;
        private int pos5Age;

        private float posx1weight = (float)Rand.Range(-100, 100) / 400;
        private float posz1weight = (float)Rand.Range(-100, 100) / 400;
        private float posx2weight = (float)Rand.Range(-100, 100) / 350;
        private float posz2weight = (float)Rand.Range(-100, 100) / 350;
        private float posx3weight = (float)Rand.Range(-100, 100) / 350;
        private float posz3weight = (float)Rand.Range(-100, 100) / 350;
        private float posx4weight = (float)Rand.Range(-100, 100) / 300;
        private float posz4weight = (float)Rand.Range(-100, 100) / 300;
        private float posx5weight = (float)Rand.Range(-100, 100) / 300;
        private float posz5weight = (float)Rand.Range(-100, 100) / 300;

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
            base.Impact(hitThing);
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireclaw.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Fireclaw_pwr");
            MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Fireclaw.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Fireclaw_ver");
            pwrVal = pwr.level;
            verVal = ver.level;
            if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                pwrVal = mpwr.level;
                verVal = mver.level;
            }
            if (settingsRef.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }
            IntVec3 target = base.Position;
            IntVec3 origin = this.origin.ToIntVec3();

            if (cflag)
            {
                hyp = Mathf.Sqrt((Mathf.Pow(origin.x - target.x, 2)) + (Mathf.Pow(origin.z - target.z, 2)));
                duration = (Mathf.RoundToInt(hyp) + 7)*2;
                angleRad = Mathf.Asin(Mathf.Abs(origin.x - target.x) / hyp);
                angleDeg = Mathf.Rad2Deg * angleRad;
                xProb = angleDeg / 90;
                cflag = false;  //dont redo calculations
            }

			if (oflag)
			{
                //this.currentPos[j] = origin;
                //this.posAge[j] = 0;
                //this.posFlag[j] = true;
                //this.j++;
                currentPos = origin;
                posFlag = true;
                pos11Age = 0;
                oflag = false;
            }


            if (this.primed == true)
			{
				if (((this.delay + this.lastStrike) < this.age))
				{
					float rand = (float)Rand.Range(0, 100);
					bool flag = rand <= (xProb * 100);
					int rand2 = Rand.Range(0, 10);  //used for variance

                    if (rand >= 40 && !pos1Flag && posFlag && this.age >= 10)
                    {
                        currentPos1 = currentPos;
                        pos1Flag = true;
                        pos1Age = this.age;
                    }
                    if (rand2 < 3 && !pos11Flag && pos1Flag && this.age >= 15 && verVal >= 1)
                    {
                        currentPos11 = currentPos1;
                        pos11Flag = true;
                        pos11Age = this.age;
                    }
                    if (rand2 < 3 && !pos12Flag && pos11Flag && this.age >= 25 && verVal >= 2)
                    {
                        currentPos12 = currentPos1;
                        pos12Flag = true;
                        pos12Age = this.age;
                    }
                    if (rand >= 40 && !pos2Flag && pos1Flag && this.age >= 20)
                    {
                        currentPos2 = currentPos;
                        pos2Flag = true;
                        pos2Age = this.age;
                    }
                    if (rand2 < 3 && !pos21Flag && pos2Flag && this.age >= 25 && verVal >= 1)
                    {
                        currentPos21 = currentPos2;
                        pos21Flag = true;
                        pos21Age = this.age;
                    }
                    if (rand2 < 3 && !pos22Flag && pos21Flag && this.age >= 35 && verVal >= 2)
                    {
                        currentPos22 = currentPos2;
                        pos22Flag = true;
                        pos22Age = this.age;
                    }
                    if (rand >= 40 && !pos3Flag && pos2Flag && this.age >= 30)
                    {
                        currentPos3 = currentPos;
                        pos3Flag = true;
                        pos3Age = this.age;
                    }
                    if (rand2 < 3 && !pos31Flag && pos3Flag && this.age >= 35 && verVal >= 1)
                    {
                        currentPos31 = currentPos3;
                        pos31Flag = true;
                        pos31Age = this.age;
                    }
                    if (rand >= 40 && !pos4Flag && pos3Flag && this.age >= 40)
                    {
                        currentPos4 = currentPos;
                        pos4Flag = true;
                        pos4Age = this.age;
                    }
                    if (rand >= 40 && !pos5Flag && pos4Flag && this.age >= 45)
                    {
                        currentPos5 = currentPos;
                        pos5Flag = true;
                        pos5Age = this.age;
                    }

                    //strike
                    if ( posFlag && posFlagw)
                    {
                        currentPos = GetNewPos(currentPos, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + posAge) / 2)), 0, 0, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos.Walkable(base.Map))
                            {
                                this.posFlagw = false;
                            }
                            else
                            {
                                if (currentPos.x != origin.x & currentPos.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos, map, 1.2f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos, map, 0.4f);
                                    }
                                    this.lastStrike = this.age;
                                }
                            }
                        }
                        catch
                        {
                            this.posFlagw = false;
                        }

                    }
                    //1
                    if (pos1Flag && pos1Flagw)
                    {
                        currentPos1 = GetNewPos(currentPos1, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos1Age) / 2)), posx1weight, posz1weight, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos1.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos1.Walkable(base.Map))
                            {
                                this.pos1Flagw = false;
                            }
                            else
                            {
                                if (currentPos1.x != origin.x & currentPos1.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos1, map, 1.0f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos1, map, 0.4f);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            this.pos1Flagw = false;
                        }
                    }
                    if (pos11Flag && pos11Flagw)
                    {
                        currentPos11 = GetNewPos(currentPos11, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos11Age) / 2)), posx1weight * 1.5f, posz1weight * 1.5f, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos11.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos11.Walkable(base.Map))
                            {
                                this.pos11Flagw = false;
                            }
                            else
                            {
                                if (currentPos11.x != origin.x & currentPos11.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos11, map, 0.8f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos11, map, 0.4f);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            this.pos11Flagw = false;
                        }
                    }
                    if (pos12Flag && pos12Flagw)
                    {
                        currentPos12 = GetNewPos(currentPos12, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos12Age) / 2)), posx1weight * 2f, posz1weight * 2f, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos12.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos12.Walkable(base.Map))
                            {
                                this.pos12Flagw = false;
                            }
                            else
                            {
                                if (currentPos12.x != origin.x & currentPos12.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos12, map, 0.8f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos12, map, 0.4f);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            this.pos12Flagw = false;
                        }
                    }
                    //2
                    if (pos2Flag && pos2Flagw)
                    {
                        currentPos2 = GetNewPos(currentPos2, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos2Age) / 2)), posx2weight, posz2weight, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos2.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos2.Walkable(base.Map))
                            {
                                this.pos2Flagw = false;
                            }
                            else
                            {
                                if (currentPos2.x != origin.x & currentPos2.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos2, map, 1.0f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos2, map, 0.4f);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            this.pos2Flagw = false;
                        }
                    }
                    if (pos21Flag && pos21Flagw)
                    {
                        currentPos21 = GetNewPos(currentPos21, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos21Age) / 2)), posx2weight * 1.5f, posz2weight * 1.5f, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos21.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos21.Walkable(base.Map))
                            {
                                this.pos21Flagw = false;
                            }
                            else
                            {
                                if (currentPos21.x != origin.x & currentPos21.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos21, map, 0.8f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos21, map, 0.4f);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            this.pos21Flagw = false;
                        }
                    }
                    if (pos22Flag && pos22Flagw)
                    {
                        currentPos22 = GetNewPos(currentPos22, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos22Age) / 2)), posx2weight * 2f, posz2weight * 2f, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos22.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos22.Walkable(base.Map))
                            {
                                this.pos22Flagw = false;
                            }
                            else
                            {
                                if (currentPos22.x != origin.x & currentPos22.z != origin.z)
                                {
                                    if (verVal >= 3)
                                    {
                                        this.FireExplosion(pwrVal, currentPos22, map, 0.8f);
                                    }
                                    else
                                    {
                                        this.FireExplosion(pwrVal, currentPos22, map, 0.4f);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            this.pos22Flagw = false;
                        }
                    }
                    //3
                    if (pos3Flag && pos3Flagw)
                    {
                        currentPos3 = GetNewPos(currentPos3, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos3Age) / 2)), posx3weight, posz3weight, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos3.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos3.Walkable(base.Map))
                            {
                                this.pos3Flagw = false;
                            }
                            else
                            {
                                if (verVal >= 3)
                                {
                                    this.FireExplosion(pwrVal, currentPos3, map, 1.0f);
                                }
                                else
                                {
                                    this.FireExplosion(pwrVal, currentPos3, map, 0.4f);
                                }
                            }
                        }
                        catch
                        {
                            this.pos3Flagw = false;
                        }
                    }
                    if (pos31Flag && pos31Flagw)
                    {
                        currentPos31 = GetNewPos(currentPos31, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos31Age) / 2)), posx3weight * 1.5f, posz3weight * 1.5f, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos31.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos31.Walkable(base.Map))
                            {
                                this.pos31Flagw = false;
                            }
                            else
                            {
                                if (verVal >= 3)
                                {
                                    this.FireExplosion(pwrVal, currentPos31, map, 0.8f);
                                }
                                else
                                {
                                    this.FireExplosion(pwrVal, currentPos31, map, 0.4f);
                                }
                            }
                        }
                        catch
                        {
                            this.pos31Flagw = false;
                        }
                    }
                    //4
                    if (pos4Flag && pos4Flagw)
                    {
                        currentPos4 = GetNewPos(currentPos4, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos4Age) / 2)), posx4weight, posz4weight, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos4.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos4.Walkable(base.Map))
                            {
                                this.pos4Flagw = false;
                            }
                            else
                            {
                                if (verVal >= 3)
                                {
                                    this.FireExplosion(pwrVal, currentPos4, map, 0.8f);
                                }
                                else
                                {
                                    this.FireExplosion(pwrVal, currentPos4, map, 0.4f);
                                }
                            }
                        }
                        catch
                        {
                            this.pos4Flagw = false;
                        }
                    }
                    //5
                    if (pos5Flag && pos5Flagw)
                    {
                        currentPos5 = GetNewPos(currentPos5, origin.x <= target.x, origin.z <= target.z, this.age >= (((duration + pos5Age) / 2)), posx5weight, posz5weight, xProb, 1 - xProb);
                        try
                        {
                            if ((currentPos5.GetTerrain(base.Map).passability == Traversability.Impassable) || !currentPos5.Walkable(base.Map))
                            {
                                this.pos5Flagw = false;
                            }
                            else
                            {
                                if (verVal >= 3)
                                {
                                    this.FireExplosion(pwrVal, currentPos5, map, 0.8f);
                                }
                                else
                                {
                                    this.FireExplosion(pwrVal, currentPos5, map, 0.4f);
                                }
                            }
                        }
                        catch
                        {
                            this.pos5Flagw = false;
                        }
                    }
                    //for (int i=0; i < j; i++)
                    //{
                    //    if (i == 10)
                    //    {
                    //        currentPos[i] = GetNewPos(currentPos[i], origin.x <= target.x, origin.z <= target.z, this.age >= (((duration - posAge[i]) / 2) + duration), 0, 0, xProb, 1-xProb);
                    //    }
                    //    else if (i < 3)
                    //    {
                    //        currentPos[i] = GetNewPos(currentPos[i], origin.x <= target.x, origin.z <= target.z, this.age >= (((duration - posAge[i]) / 2) + duration), posxWeight[i] * 3, poszWeight[i]*3, xProb, 1 - xProb);
                    //    }
                    //    else
                    //    {
                    //        currentPos[i] = GetNewPos(currentPos[i], origin.x <= target.x, origin.z <= target.z, this.age >= (((duration - posAge[i]) / 2) + duration), posxWeight[i] * 5, poszWeight[i] * 5, xProb, 1 - xProb);
                    //    }
                    //    this.FireExplosion(currentPos[i], map, 0.4f);
                    //    this.lastStrike = this.age;
                    //}

                    bool flag1 = this.age <= this.duration;
					if (!flag1)
					{
						this.primed = false;
						return;

					}

				}

			}
		}


		protected void FireExplosion(int pwr, IntVec3 pos, Map map, float radius)
		{
			ThingDef def = this.def;
            try
            {
                Explosion(pwr, pos, map, radius, DamageDefOf.Burn, this.launcher, null, def, this.equipmentDef, ThingDefOf.Filth_Ash, 0.4f, 1, false, null, 0f, 1);
            }
            catch
            {
                this.age = this.duration;
            }
			

		}

        private IntVec3 GetNewPos(IntVec3 curPos, bool xdir, bool zdir, bool halfway, float zvar, float xvar, float xguide, float zguide)
        {
            float rand = (float)Rand.Range(0, 100);  					
            bool flagx = rand <= ((xguide + Mathf.Abs(xvar)) * 100);    
                                                                        
            bool flagy = rand <= ((zguide + Mathf.Abs(zvar)) * 100);	
            if (halfway)												
            {
                xvar = (-1 * xvar);
                zvar = (-1 * zvar);
            }

            if (xdir && zdir)											
            {
                //top right
                if (flagx)												
                {
                    if (xguide + xvar >= 0) { curPos.x++; }				
                    else { curPos.x--; }								
                }
                if (flagy)												
                {
                    if (zguide + zvar >= 0) { curPos.z++; }				
                    else { curPos.z--; }								
                }
            }
            if (xdir && !zdir)
            {
                //bottom right
                if (flagx)
                {
                    if (xguide + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if ((-1 * zguide) + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (!xdir && zdir)
            {
                //top left
                if (flagx)
                {
                    if ((-1 * xguide) + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if (zguide + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            if (!xdir && !zdir)
            {
                //bottom left
                if (flagx)
                {
                    if ((-1 * xguide) + xvar >= 0) { curPos.x++; }
                    else { curPos.x--; }
                }
                if (flagy)
                {
                    if ((-1 * zguide) + zvar >= 0) { curPos.z++; }
                    else { curPos.z--; }
                }
            }
            else
            {
                //no direction identified
            }
            return curPos;
        }

        public void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound = null, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
		{
			System.Random rnd = new System.Random();
			int modDamAmountRand = ((pwr * 5)) + GenMath.RoundRandom(rnd.Next(8, projectile.projectile.GetDamageAmount(1,null)));  //36
            modDamAmountRand = Mathf.RoundToInt(modDamAmountRand * this.arcaneDmg);
			if (map == null)
			{
				Log.Warning("Tried to do explosion in a null map.");
				return;
			}
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.damageFalloff = true;
            explosion.chanceToStartFire = 0.1f;
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
	}
}
