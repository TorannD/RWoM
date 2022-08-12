using AbilityUser;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public struct BloodFire : IExposable
    {
        public IntVec3 position;
        public int pulseCount;        

        public BloodFire(IntVec3 pos, int pulse)
        {
            position = pos;
            pulseCount = pulse;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref pulseCount, "pulseCount", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.position, "position", default(IntVec3), false);
        }
    }

    public class Projectile_IgniteBlood : Projectile_AbilityBase
	{
        private int verVal;
        private int pwrVal;

        private int age = -1;
        private int spreadRate = 12;
        private float arcaneDmg = 1;

        private bool initialized = false;
        int duration = 500;
        public List<ThingDef> bloodTypes = new List<ThingDef>();
        private ThingDef pawnBloodDef = null;

        List<BloodFire> BF = new List<BloodFire>();       

        Vector3 direction = default(Vector3);
        

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 500, false);
            Scribe_Values.Look<int>(ref this.spreadRate, "spreadRate", 12, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<Vector3>(ref this.direction, "direction", default(Vector3), false);
            Scribe_Collections.Look<ThingDef>(ref this.bloodTypes, "bloodTypes", LookMode.Def);
            Scribe_Collections.Look<BloodFire>(ref this.BF, "BF", LookMode.Deep);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Draw()
        {
            if (!initialized)
            {
                base.Draw();
            }
        }

        protected override void Impact(Thing hitThing)
		{
            if (!this.initialized)
            {
                base.Impact(hitThing);
                this.initialized = true;
                this.BF = new List<BloodFire>();
                this.BF.Clear();
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                Pawn pawn = this.launcher as Pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill bpwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_pwr");
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_IgniteBlood.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_IgniteBlood_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_IgniteBlood.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_IgniteBlood_ver");
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
                this.arcaneDmg *= (1 + (.1f * bpwr.level));
                this.spreadRate -= 2 * verVal;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.bloodTypes = new List<ThingDef>();
                this.bloodTypes.Clear();
                if (settingsRef.unrestrictedBloodTypes)
                {
                    this.pawnBloodDef = pawn.RaceProps.BloodDef;
                    this.bloodTypes = TM_Calc.GetAllRaceBloodTypes();
                }
                else
                {
                    this.pawnBloodDef = ThingDefOf.Filth_Blood;
                    this.bloodTypes.Add(this.pawnBloodDef);
                }
                
                List<IntVec3> cellList = GenRadial.RadialCellsAround(base.Position, this.def.projectile.explosionRadius, true).ToList();

                Filth filth = (Filth)ThingMaker.MakeThing(this.pawnBloodDef);
                GenSpawn.Spawn(filth, base.Position, pawn.Map);
                //FilthMaker.MakeFilth(base.Position, this.Map, ThingDefOf.Filth_Blood, 1);
                for (int i = 0; i < 30; i++)
                {
                    IntVec3 randomCell = cellList.RandomElement();
                    if (randomCell.IsValid && randomCell.InBoundsWithNullCheck(pawn.Map) && !randomCell.Fogged(pawn.Map) && randomCell.Walkable(pawn.Map))
                    {
                        //FilthMaker.MakeFilth(randomCell, this.Map, ThingDefOf.Filth_Blood, 1);
                        //Log.Message("creating blood at " + randomCell);
                        Filth filth2 = (Filth)ThingMaker.MakeThing(this.pawnBloodDef);
                        GenSpawn.Spawn(filth2, randomCell, pawn.Map);
                    }
                }
                this.BF.Add(new BloodFire(base.Position, 0));
            }

            if(this.age > 0 && Find.TickManager.TicksGame % this.spreadRate == 0)
            {
                BurnBloodAtCell();
                FindNearbyBloodCells();
            }

            if(this.BF.Count <= 0)
            {
                this.age = this.duration;
            }

            if (this.age >= this.duration)
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public void BurnBloodAtCell()
        {
            for (int i = 0; i < this.BF.Count; i++)
            {
                List<Thing> thingList = this.BF[i].position.GetThingList(this.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != null && this.bloodTypes.Contains(thingList[j].def))
                    {
                        thingList[j].Destroy(DestroyMode.Vanish);
                    }
                }
                this.BF[i] = new BloodFire(this.BF[i].position, this.BF[i].pulseCount + 1);
                GenExplosion.DoExplosion(this.BF[i].position, this.Map, .2f + (.4f * BF[i].pulseCount), TMDamageDefOf.DamageDefOf.TM_BloodBurn, this.launcher, Mathf.RoundToInt((Rand.Range(2.8f, 4.5f) * (1 + (.12f * pwrVal))) * this.arcaneDmg), .5f, TorannMagicDefOf.TM_FireWooshSD, null, null, null, null, 0f, 1, false, null, 0f, 1, 0.0f, false);
                if(this.BF[i].pulseCount >= 3)
                {                    
                    this.BF.Remove(this.BF[i]);
                }                
            }
        }

        public void FindNearbyBloodCells()
        {
            int BFCount = this.BF.Count;
            for(int i =0; i < BFCount; i++)
            {
                List<IntVec3> cellList = GenRadial.RadialCellsAround(this.BF[i].position, .4f + this.BF[i].pulseCount, false).ToList();
                for (int j = 0; j < cellList.Count; j++)
                {
                    if (cellList[j].IsValid && cellList[j].InBoundsWithNullCheck(this.Map))
                    {
                        List<Thing> thingList = cellList[j].GetThingList(this.Map);
                        for (int k = 0; k < thingList.Count; k++)
                        {
                            if (thingList[k] != null && this.bloodTypes.Contains(thingList[k].def))
                            {
                                bool flag = false;
                                for (int z = 0; z < this.BF.Count; z++)
                                {
                                    if(BF[z].position == cellList[j])
                                    {
                                        flag = true; //already exists as an active blood flame position
                                    }
                                }
                                if (!flag)
                                {
                                    this.BF.Add(new BloodFire(cellList[j], 0));
                                }
                            }
                        }
                    }
                }
            }
        }
        

        public override void Tick()
        {
            if (!this.initialized)
            {
                if (this.age < 2)
                {
                    this.direction = TM_Calc.GetVector(this.launcher.DrawPos, base.Position.ToVector3Shifted());
                }
                Vector3 rndPos = this.DrawPos;
                rndPos.x += Rand.Range(-.4f, .4f);
                rndPos.z += Rand.Range(-.4f, .4f);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, rndPos, this.Map, Rand.Range(.9f, 1.2f), .05f, 0f, .25f, Rand.Range(-300, 300), Rand.Range(8f, 12f), (Quaternion.AngleAxis(Rand.Range(60,120), Vector3.up) * this.direction).ToAngleFlat(), Rand.Range(0, 360));
            }
            else
            {
                age++;
            }
            base.Tick();            
        }

    }	
}


