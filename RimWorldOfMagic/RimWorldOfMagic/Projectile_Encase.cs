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
        private List<Thing> sections = new List<Thing>();

        List<TMDefs.Encase> wall = new List<TMDefs.Encase>();
        Pawn caster;
        private Pawn projectedOwner;
        private float potency = 1f;
        internal Pawn SpellOwner => projectedOwner ?? launcher as Pawn;

        internal void PrepareHosted(MagicAbility power)
        {
            projectedOwner = power.Pawn;
            pwrVal = ClassProgression.SpiritRebirthRuntime.SavedRank(power.MagicUser.ExistingMagicData,"TM_Encase_pwr");
            verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(power.MagicUser.ExistingMagicData,"TM_Encase_ver");
            potency = power.MagicUser.arcaneDmg;
        }

        //unsaved variables
        ThingDef spawnDef = ThingDef.Named("Sandstone");

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref projectedOwner, "TM_encaseOwner");
            Scribe_Values.Look(ref potency, "TM_encasePotency", 1f);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.wallActive, "wallActive", false, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Collections.Look<IntVec3>(ref this.wallPositions, "wallPositions", LookMode.Value);
            Scribe_Collections.Look<Thing>(ref this.despawnedThingList, "despawnedThingList", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.Saving)
                sections.RemoveAll(section => section == null || section.Destroyed || !section.Spawned || section.Map != Map);
            Scribe_Collections.Look(ref sections, "TM_encaseSections", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) sections ??= new List<Thing>();
            Scribe_Collections.Look<TMDefs.Encase>(ref this.wall, "wall", LookMode.Deep);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                if (projectedOwner == null)
                {
                    var comp = caster.GetCompAbilityUserMagic();
                    potency = comp.arcaneDmg;
                    pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Encase.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Encase_pwr").level;
                    verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Encase.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Encase_ver").level;
                    if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                    {
                        pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
                        verVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
                    }
                    if (ModOptions.Settings.Instance.AIHardMode && !caster.IsColonist)
                    {
                        pwrVal = 3;
                        verVal = 3;
                    }
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
                wallPositions = outerCells.Except(innerCells).Where(cell => cell.InBounds(map)).ToList();
                foreach (var cell in wallPositions)
                    wall.Add(new TMDefs.Encase(cell, cell.GetTerrain(map)));
                float magnitude = (base.Position.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
                Find.CameraDriver.shaker.DoShake(10 / magnitude);
                //for (int k = 0; k < wallPositions.Count(); k++)
                //{
                for(int k =0; k < wall.Count(); k++)
                { 
                    if(wall[k].position.IsValid && wall[k].position.InBoundsWithNullCheck(map) && !wall[k].position.Fogged(map) && !wall[k].position.InNoZoneEdgeArea(map))
                    //if (wallPositions[k].IsValid && wallPositions[k].InBoundsWithNullCheck(map) && !wallPositions[k].Fogged(map) && !wallPositions[k].InNoZoneEdgeArea(map))
                    {
                        if (wall[k].terrain.defName == "Marsh" || wall[k].terrain.defName == "WaterShallow" || wall[k].terrain.defName == "WaterMovingShallow" || wall[k].terrain.defName == "WaterOceanShallow" || wall[k].terrain.defName == "WaterMovingChestDeep")
                        { 
                            
                            //MoteSplash moteSplash = (MoteSplash)ThingMaker.MakeThing(ThingDefOf.Mote_WaterSplash, null);
                            FleckMaker.WaterSplash(wallPositions[k].ToVector3Shifted(), map, 1f, 1f);
                            //moteSplash.Initialize(wallPositions[k].ToVector3Shifted(), 8f, 1f);
                            //GenSpawn.Spawn(moteSplash, wallPositions[k], map, WipeMode.Vanish);
                        }
                        // Despawning mutates the map's cell list. Inspect one snapshot for the whole section.
                        var cellList = wall[k].position.GetThingList(map).ToList();
                        if (!cellList.Any(thing => !thing.def.EverHaulable &&
                            (thing.def.designationCategory == TorannMagicDefOf.Structure || thing.def.altitudeLayer == AltitudeLayer.Building ||
                             thing.def.altitudeLayer == AltitudeLayer.Item || thing.def.altitudeLayer == AltitudeLayer.ItemImportant)))
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
                                    SingleSpawnLoop(tempSpawn, wall[k].position, map);
                                    for (int m = 0; m < 4; m++)
                                    {
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, wall[k].position.ToVector3Shifted(), map, Rand.Range(.6f, .8f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(1f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
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
                this.duration = Mathf.RoundToInt(1800 + (240 * verVal) * potency);
                this.initialized = true;
                this.wallActive = true;
            }
        }

        private void EndField(Map map)
        {
            // Older saves recorded only positions. A replacement in one of those cells cannot be identified as ours.
            foreach (var section in sections)
            {
                if (section?.Spawned != true || section.Map != map) continue;
                var cell = section.Position;
                section.Destroy();
                for (int m = 0; m < 4; m++)
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, cell.ToVector3Shifted(), map, Rand.Range(.4f, .8f), .05f, .05f, Rand.Range(.2f, .3f), Rand.Range(-20, 20), Rand.Range(1f, 2f), Rand.Range(0, 360), 0);
            }
            sections.Clear();
            // A colonist may have rebuilt on the original tile while the field was active.
            // Keep ownership of any item that cannot be safely placed yet, including across saves.
            for (int i = despawnedThingList.Count - 1; i >= 0; i--)
            {
                var thing = despawnedThingList[i];
                if (thing == null || thing.Destroyed || thing.Spawned ||
                    GenPlace.TryPlaceThing(thing, thing.Position, map, ThingPlaceMode.Near,
                        extraValidator: cell => !cell.GetThingList(map).Any(other => GenSpawn.SpawningWipes(thing.def, other.def))))
                    despawnedThingList.RemoveAt(i);
            }
            if (despawnedThingList.Count == 0)
            {
                wallActive = false;
                base.Destroy();
            }
            else duration = age + 60;
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Thing thing)
        {
            bool flag = targetCell.IsValid && targetCell != default(IntVec3);
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
                ThingDef def = spawnables.def;
                ThingDef stuff = null;
                bool madeFromStuff = def.MadeFromStuff;
                if (madeFromStuff)
                {                    
                    stuff = ThingDefOf.BlocksGranite;                   
                    
                }
                Thing thing = ThingMaker.MakeThing(def, stuff);
                GenSpawn.Spawn(thing, position, map, Rot4.North, WipeMode.Vanish);
                sections.Add(thing);
                ClassProgression.GeomancerTrials.WallSpawned(this, thing, position, map);
            }
        }

        // Natural rocks are normally saved as a compressed cell grid and receive fresh IDs on load.
        [HarmonyPatch(typeof(CompressibilityDecider), nameof(CompressibilityDecider.DetermineReferences))]
        internal static class PreserveGeomancerReferences
        {
            private static void Postfix(Map ___map, HashSet<Thing> ___referencedThings)
            {
                foreach (var projectile in ___map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile))
                    if (projectile is Projectile_Encase encase)
                    {
                        foreach (var section in encase.sections)
                            if (section?.Spawned == true && section.Map == ___map) ___referencedThings.Add(section);
                    }
                    else if (projectile is Projectile_EarthernHammer hammer)
                    {
                        foreach (var candidate in hammer.StoneTargets)
                            if (candidate?.Spawned == true && candidate.Map == ___map) ___referencedThings.Add(candidate);
                    }
            }
        }

        public Vector3 GetVector(Vector3 casterPos, Vector3 targetPos)
        {
            Vector3 heading = (targetPos - casterPos);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        protected override void Tick()
        {
            if (!wallActive) base.Tick();
            else if (age >= duration) EndField(Map);
            age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.duration;
            if (!flag && !wallActive)
            {                
                base.Destroy(mode);
            }
        }
    }
}
