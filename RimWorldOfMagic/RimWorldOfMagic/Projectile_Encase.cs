using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse.AI;
using HarmonyLib;

namespace TorannMagic
{

    [StaticConstructorOnStartup]
    public class Projectile_Encase : Projectile_AbilityBase
    {
        private bool initialized = false;
        private bool wallActive = false;
        private int age = -1;
        private int duration = 1800;
        private int pwrVal = 0;
        private int verVal = 0;
        List<IntVec3> wallPositions = new List<IntVec3>();
        List<Thing> despawnedThingList = new List<Thing>();
        List<TerrainDef> terrainList = new List<TerrainDef>();

        List<TMDefs.Encase> wall = new List<TMDefs.Encase>();
        Pawn caster;

        //unsaved variables
        ThingDef spawnDef = ThingDef.Named("Sandstone");

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.wallActive, "wallActive", false, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Collections.Look<IntVec3>(ref this.wallPositions, "wallPositions", LookMode.Value);
            Scribe_Collections.Look<Thing>(ref this.despawnedThingList, "despawnedThingList", LookMode.Deep);
            Scribe_Collections.Look<TMDefs.Encase>(ref this.wall, "wall", LookMode.Deep);
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Encase.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Encase_pwr").level;
                verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Encase.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Encase_ver").level;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
                    verVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
                }
                if (settingsRef.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }

                if (pwrVal == 3)
                {
                    spawnDef = ThingDef.Named("Granite"); //900
                }
                else if (pwrVal == 2)
                {
                    spawnDef = ThingDef.Named("Limestone"); //700
                }
                else if (pwrVal == 1)
                {
                    spawnDef = ThingDef.Named("Slate"); //500
                }
                else
                {
                    spawnDef = ThingDef.Named("Sandstone"); //400
                }
                List<IntVec3> outerCells = GenRadial.RadialCellsAround(base.Position, this.def.projectile.explosionRadius + 1f, true).ToList();
                List<IntVec3> innerCells = GenRadial.RadialCellsAround(base.Position, this.def.projectile.explosionRadius - 1f, true).ToList();
                this.wallPositions = new List<IntVec3>();
                this.wallPositions.Clear();
                this.terrainList = new List<TerrainDef>();
                this.terrainList.Clear();
                this.wallPositions = outerCells.Except(innerCells).ToList();
                for (int t = 0; t < wallPositions.Count(); t++)
                {
                    TMDefs.Encase temp = new TMDefs.Encase(wallPositions[t], wallPositions[t].GetTerrain(caster.Map));
                    wall.Add(temp);
                    this.terrainList.Add(wallPositions[t].GetTerrain(caster.Map));
                }
                float magnitude = (base.Position.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
                Find.CameraDriver.shaker.DoShake(10 / magnitude);
                //for (int k = 0; k < wallPositions.Count(); k++)
                //{
                for(int k =0; k < wall.Count(); k++)
                { 
                    if(wall[k].position.IsValid && wall[k].position.InBoundsWithNullCheck(caster.Map) && !wall[k].position.Fogged(caster.Map) && !wall[k].position.InNoZoneEdgeArea(caster.Map))
                    //if (wallPositions[k].IsValid && wallPositions[k].InBoundsWithNullCheck(caster.Map) && !wallPositions[k].Fogged(caster.Map) && !wallPositions[k].InNoZoneEdgeArea(caster.Map))
                    {
                        if (wall[k].terrain.defName == "Marsh" || wall[k].terrain.defName == "WaterShallow" || wall[k].terrain.defName == "WaterMovingShallow" || wall[k].terrain.defName == "WaterOceanShallow" || wall[k].terrain.defName == "WaterMovingChestDeep")
                        { 
                            
                            //MoteSplash moteSplash = (MoteSplash)ThingMaker.MakeThing(ThingDefOf.Mote_WaterSplash, null);
                            FleckMaker.WaterSplash(wallPositions[k].ToVector3Shifted(), map, 1f, 1f);
                            //moteSplash.Initialize(wallPositions[k].ToVector3Shifted(), 8f, 1f);
                            //GenSpawn.Spawn(moteSplash, wallPositions[k], map, WipeMode.Vanish);
                        }
                        List<Thing> cellList = new List<Thing>();
                        bool hasWall = false;
                        try
                        {

                            cellList = wall[k].position.GetThingList(caster.Map);
                            for (int i = 0; i < cellList.Count(); i++)
                            {
                                if (cellList[i].def.designationCategory == DesignationCategoryDefOf.Structure || cellList[i].def.altitudeLayer == AltitudeLayer.Building || cellList[i].def.altitudeLayer == AltitudeLayer.Item || cellList[i].def.altitudeLayer == AltitudeLayer.ItemImportant)
                                {
                                    if (!cellList[i].def.EverHaulable)
                                    {
                                        hasWall = true;
                                        //this.terrainList.Remove(this.terrainList[k]);
                                        //this.wallPositions.Remove(this.wallPositions[k]); //don't do anything if a building/wall already exists
                                        wall.Remove(wall[k]);
                                        break;
                                    }
                                }
                            }
                        }
                        catch //remove square if it threw an error trying to get item list at this location
                        {
                            hasWall = true;
                            //this.terrainList.Remove(this.terrainList[k]);
                            //this.wallPositions.Remove(this.wallPositions[k]);
                            wall.Remove(wall[k]);
                            continue;
                        }

                        if (!hasWall)
                        {
                            bool spawnWall = true;
                            for (int i = 0; i < cellList.Count(); i++)
                            {
                                if (!(cellList[i] is Pawn)) //
                                {
                                    if (cellList[i].def.defName.Contains("Mote"))
                                    {
                                        //Log.Message("avoided storing " + cellList[i].def.defName);
                                    }
                                    else if (cellList[i].def.defName == "Fire" || cellList[i].def.defName == "Spark")
                                    {
                                        cellList[i].Destroy(DestroyMode.Vanish);
                                    }
                                    else
                                    {
                                        this.despawnedThingList.Add(cellList[i]);
                                        cellList[i].DeSpawn();
                                    }
                                }
                            }
                            if (spawnWall)
                            {
                                AbilityUser.SpawnThings tempSpawn = new SpawnThings()
                                {
                                    def = spawnDef,
                                    spawnCount = 1
                                };
                                try
                                {                                 
                                    SingleSpawnLoop(tempSpawn, wall[k].position, caster.Map);
                                    for (int m = 0; m < 4; m++)
                                    {
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, wall[k].position.ToVector3Shifted(), caster.Map, Rand.Range(.6f, .8f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
                                    }
                                }
                                catch
                                {
                                    //dont spawn wall
                                    continue;
                                }
                            }
                        }
                    }
                }
                this.duration = Mathf.RoundToInt(1800 + (240 * verVal) * comp.arcaneDmg);
                this.initialized = true;
                this.wallActive = true;
            }
            else if(this.initialized && this.wallActive && !(this.age < this.duration))
            {                
                for (int j = 0; j < wall.Count(); j++)
                {
                    Building structure = null;
                    structure = this.wall[j].position.GetFirstBuilding(this.Map);
                    if (structure != null)
                    {
                        structure.Destroy();
                        for (int m = 0; m < 4; m++)
                        {
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, wall[j].position.ToVector3Shifted(), this.Map, Rand.Range(.4f, .8f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
                        }
                    }
                    structure = null;
                    this.Map.terrainGrid.SetTerrain(wall[j].position, wall[j].terrain);
                }

                for (int i = 0; i < this.despawnedThingList.Count(); i++)
                {
                    GenSpawn.Spawn(this.despawnedThingList[i], this.despawnedThingList[i].Position, this.Map);
                }
                this.wallActive = false;
            }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Thing thing)
        {
            bool flag = targetCell != null && targetCell != default(IntVec3);
            if (flag)
            {
                if (thing != null && thing.Position.IsValid && !this.Destroyed && thing.Spawned && thing.Map != null)
                {
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), thing.Position, thing.Map);
                    flyingObject.speed = 22;
                    flyingObject.Launch(caster, targetCell, thing);
                }
            }
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            //bool flag2 = position.InBoundsWithNullCheck(map) && position.IsValid && !position.InNoZoneEdgeArea(map);
            if (flag)
            {
                Faction faction = this.caster.Faction;
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff)
                {                    
                    stuff = ThingDefOf.BlocksGranite;                   
                    
                }
                Thing thing = ThingMaker.MakeThing(def, stuff);
                GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish);
            }
        }

        public Vector3 GetVector(Vector3 casterPos, Vector3 targetPos)
        {
            Vector3 heading = (targetPos - casterPos);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.duration;
            if (!flag)
            {                
                base.Destroy(mode);
            }
        }
    }
}
