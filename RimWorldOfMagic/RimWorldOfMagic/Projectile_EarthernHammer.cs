using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TorannMagic
{
    class Projectile_EarthernHammer : Projectile_AbilityBase
    {

        private IntVec3 strikeLoc = IntVec3.Invalid;

        private int age = -1;
        private bool initialized = false;
        private int gravityPoints = 0;      //ability ends when gravitypoints reduces to 0 - 3pts to pull from earth, 1pt to throw a nearby rock
        private int gravityStep = 0;        //pulls rocks from ground on first and every 3rd iteration (0, 3, 6)
        private int verVal;
        private int pwrVal;
        Pawn caster;
        Thing launchableThing = null;
        List<IntVec3> launchCells = new List<IntVec3>();
        List<Thing> launchableThings = new List<Thing>();

        //non-saved vars
        private int nextStrike = 0;
        private int radius = 4;
        private float arcaneDmg = 1;
        private int duration = 1000;         //absolute longest this ability will last - backstop
        private int spinRate = 20;
        private int ticksTillNextStrike = 50;
        private int baseGravityPoints = 8;
 
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);            
            Scribe_Values.Look<int>(ref this.gravityPoints, "gravityPoints", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Collections.Look<IntVec3>(ref this.launchCells, "launchCells", LookMode.Value);
            Scribe_Collections.Look<Thing>(ref this.launchableThings, "launchableThings", LookMode.Deep);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        private void Initialize()
        {
            caster = this.launcher as Pawn;
            CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
            pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EarthernHammer_pwr").level;
            verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EarthernHammer_ver").level;
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
                verVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
            }
            this.arcaneDmg = comp.arcaneDmg;
            if (settingsRef.AIHardMode && !caster.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }
            this.gravityPoints = this.baseGravityPoints + (pwrVal * 2);

            this.launchCells = new List<IntVec3>();
            this.launchCells.Clear();

            this.launchCells = GenRadial.RadialCellsAround(caster.Position, this.radius, false).ToList();
            for (int i = 0; i < launchCells.Count(); i++)
            {
                if (launchCells[i].IsValid && launchCells[i].InBoundsWithNullCheck(caster.Map))
                {
                    List<Thing> cellList = launchCells[i].GetThingList(caster.Map);
                    bool invalidCell = false;
                    for (int j = 0; j < cellList.Count(); j++)
                    {
                        try
                        {
                            if (cellList[j].def.designationCategory != null)
                            {
                                if (cellList[j].def.designationCategory == DesignationCategoryDefOf.Structure || cellList[j].def.altitudeLayer == AltitudeLayer.Building || cellList[j].def.altitudeLayer == AltitudeLayer.Item || cellList[j].def.altitudeLayer == AltitudeLayer.ItemImportant)
                                {
                                    invalidCell = true;
                                }
                            }

                            if (cellList[j].def.thingCategories != null)
                            {
                                if (cellList[j].def.thingCategories.Contains(ThingCategoryDefOf.StoneChunks) || cellList[j].def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks))
                                {
                                    this.launchableThings.Add(cellList[j]);
                                }
                            }
                        }
                        catch (NullReferenceException ex)
                        {
                            //Log.Message("threw exception " + ex);
                        }
                    }
                    if (invalidCell)
                    {
                        launchCells.Remove(launchCells[i]);
                    }
                }
            }
        }

        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);                       

            if (!initialized)
            {
                Initialize();
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Stun, (20 * (1 - (.2f * verVal))), 0, -1, this.caster, null, null, DamageInfo.SourceCategory.ThingOrUnknown, this.caster);
                this.caster.TakeDamage(dinfo);
                this.initialized = true;
            }
            if (!(this.caster.DestroyedOrNull() || this.caster.Dead || this.caster.Downed))
            {
                if (this.age > this.nextStrike)
                {
                    if (this.gravityStep == 0 || this.gravityStep % 3 == 0)
                    {
                        if (this.gravityPoints >= 3)
                        {
                            SpawnAndThrow();
                        }
                        else
                        {
                            if (this.gravityPoints > 0 && this.launchableThings.Count() > 0)
                            {
                                SearchAndThrow();
                            }
                            else
                            {
                                this.age = this.duration;
                            }
                        }
                    }
                    else if (this.gravityPoints > 0 && this.launchableThings.Count() > 0)
                    {
                        SearchAndThrow();
                    }
                    else
                    {
                        if (this.gravityPoints < 3)
                        {
                            this.age = this.duration;
                        }
                    }

                    this.nextStrike = this.age + Mathf.RoundToInt((ticksTillNextStrike * (1 - .15f * verVal)));
                    this.gravityStep++;
                }
            }
            else
            {
                this.age = this.duration;
            }
        }

        public void SpawnAndThrow()
        {
            this.gravityPoints -= 3;

            AbilityUser.SpawnThings tempPod = new SpawnThings();
            tempPod.def = ThingDef.Named("ChunkSandstone");
            tempPod.spawnCount = 1;
            IntVec3 origin = this.launchCells.RandomElement();          
            SingleSpawnLoop(tempPod, origin, this.caster.Map);

            float magnitude = (origin.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
            Find.CameraDriver.shaker.DoShake(6 / magnitude);

            ThrowStone(origin);
        }

        public void SearchAndThrow()
        {
            this.gravityPoints--;            

            this.launchableThing = this.launchableThings.RandomElement();
            this.launchableThings.Remove(this.launchableThing);
            IntVec3 origin = this.launchableThing.Position;

            ThrowStone(origin);
        }        

        public void ThrowStone(IntVec3 origin)
        {
            SoundInfo info = SoundInfo.InMap(new TargetInfo(origin, this.caster.Map, false), MaintenanceType.None);
            info.pitchFactor = .7f;
            info.volumeFactor = 2f;
            TorannMagicDefOf.TM_AirWoosh.PlayOneShot(info);

            CellRect cellRect = CellRect.CenteredOn(base.Position, radius+2);
            cellRect.ClipInsideMap(this.caster.Map);
            IntVec3 destination = cellRect.RandomCell;

            if (launchableThing != null && destination != null)
            {                
                float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(origin, destination)).ToAngleFlat();
                for (int m = 0; m < 4; m++)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, origin.ToVector3Shifted(), this.caster.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(3f, 5f), launchAngle += Rand.Range(-25,25), Rand.Range(0, 360));
                }
                FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), origin, this.caster.Map);
                flyingObject.force = this.arcaneDmg + .2f;
                flyingObject.Launch(this.caster, destination, this.launchableThing.SplitOff(1), this.spinRate);
            }
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = TM_Action.ResolveFaction(this.launcher as Pawn, spawnables, this.launcher.Faction);
                bool flag2 = spawnables.def.race != null;
                if (flag2)
                {
                    bool flag3 = spawnables.kindDef == null;
                    if (flag3)
                    {
                        Log.Error("Missing kinddef");
                    }
                    else
                    {
                        TM_Action.SpawnPawn(this.launcher as Pawn, spawnables, faction, position, 0, map);
                    }
                }
                else
                {
                    ThingDef def = spawnables.def;
                    ThingDef stuff = null;
                    bool madeFromStuff = def.MadeFromStuff;
                    if (madeFromStuff)
                    {
                        stuff = ThingDefOf.WoodLog;
                    }
                    this.launchableThing = ThingMaker.MakeThing(def, stuff);
                    GenSpawn.Spawn(this.launchableThing, position, map, Rot4.North, WipeMode.Vanish, false);
                }
            }
        }

    }
}
