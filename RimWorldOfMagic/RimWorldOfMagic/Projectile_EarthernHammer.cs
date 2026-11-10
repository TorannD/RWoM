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

        private HashSet<string> legacyTargetIds;

        private int age = -1;
        private bool initialized = false;
        private int gravityPoints = 0;      //ability ends when gravitypoints reduces to 0 - 3pts to pull from earth, 1pt to throw a nearby rock
        private int gravityStep = 0;        //pulls rocks from ground on first and every 3rd iteration (0, 3, 6)
        private int verVal;
        private int pwrVal;
        Pawn caster;
        private Pawn projectedOwner;
        internal IEnumerable<Thing> StoneTargets => launchableThings ?? Enumerable.Empty<Thing>();

        internal void PrepareHosted(MagicAbility power)
        {
            projectedOwner = power.Pawn;
            pwrVal = ClassProgression.SpiritRebirthRuntime.SavedRank(power.MagicUser.ExistingMagicData,"TM_EarthernHammer_pwr");
            verVal = ClassProgression.SpiritRebirthRuntime.SavedRank(power.MagicUser.ExistingMagicData,"TM_EarthernHammer_ver");
            arcaneDmg = power.MagicUser.arcaneDmg;
        }
        Thing launchableThing = null;
        List<IntVec3> launchCells = new List<IntVec3>();
        List<Thing> launchableThings = new List<Thing>();


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
            Scribe_References.Look(ref projectedOwner,"TM_hammerOwner");
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);            
            Scribe_Values.Look<int>(ref this.gravityPoints, "gravityPoints", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.caster, "caster", false);
            Scribe_Collections.Look<IntVec3>(ref this.launchCells, "launchCells", LookMode.Value);
            Scribe_Values.Look(ref gravityStep, "TM_hammerStep");
            Scribe_Values.Look(ref nextStrike, "TM_hammerNextStrike");
            Scribe_Values.Look(ref arcaneDmg, "TM_hammerPotency", 1f);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                RestoreLegacyTargets();
                launchableThings.RemoveAll(t => !AvailableStone(t));
            }
            Scribe_Collections.Look(ref launchableThings, "TM_hammerTargets", LookMode.Reference);
            // Legacy entries were deep copies of map items. Read only their IDs, never instantiate another stack.
            if (Scribe.mode == LoadSaveMode.LoadingVars && Scribe.loader.curXmlParent["TM_hammerTargets"] == null &&
                Scribe.loader.curXmlParent["launchableThings"] is System.Xml.XmlElement oldList)
                legacyTargetIds = oldList.ChildNodes.OfType<System.Xml.XmlElement>().Select(n => n["id"]?.InnerText)
                    .Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                launchableThings ??= new List<Thing>();
                if (legacyTargetIds != null) LongEventHandler.ExecuteWhenFinished(RestoreLegacyTargets);
            }
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
            if (projectedOwner == null)
            {
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                pwrVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EarthernHammer_pwr").level;
                verVal = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_EarthernHammer.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_EarthernHammer_ver").level;
                if (caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    pwrVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
                    verVal = caster.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                if (ModOptions.Settings.Instance.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
            }
            this.gravityPoints = this.baseGravityPoints + (pwrVal * 2);

            launchCells.Clear();
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, radius, false))
            {
                if (!cell.InBounds(Map)) continue;
                var contents = cell.GetThingList(Map);
                launchableThings.AddRange(contents.Where(IsStone));
                if (!contents.Any(BlocksSource)) launchCells.Add(cell);
            }
        }

        private static bool IsStone(Thing thing) => thing?.def.thingCategories is { } categories &&
            (categories.Contains(ThingCategoryDefOf.StoneChunks) || categories.Contains(ThingCategoryDefOf.StoneBlocks));

        private static bool BlocksSource(Thing thing) => thing.def.designationCategory != null &&
            (thing.def.designationCategory == TorannMagicDefOf.Structure || thing.def.altitudeLayer == AltitudeLayer.Building ||
             thing.def.altitudeLayer == AltitudeLayer.Item || thing.def.altitudeLayer == AltitudeLayer.ItemImportant);

        private bool AvailableStone(Thing thing) => thing?.Spawned == true && thing.Map == Map && thing.stackCount > 0 && IsStone(thing);

        private void RestoreLegacyTargets()
        {
            if (legacyTargetIds == null || Map == null) return;
            if (legacyTargetIds.Count != 0)
                launchableThings.AddRange(Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
                    .Where(t => legacyTargetIds.Contains(t.ThingID) && AvailableStone(t)));
            legacyTargetIds = null;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing);                       

            if (launcher is not Pawn physical || !physical.Spawned || physical.Map != Map || physical.Dead || physical.Downed ||
                projectedOwner != null && (!ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(physical,out var held) || held != projectedOwner))
            {
                age = duration;
                return;
            }
            RestoreLegacyTargets();
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
                    launchableThings.RemoveAll(t => !AvailableStone(t));
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
            launchCells.RemoveAll(c => !c.InBounds(Map) || c.GetThingList(Map).Any(BlocksSource));
            if (!launchCells.TryRandomElement(out var origin))
            {
                if (launchableThings.Count != 0) SearchAndThrow();
                else age = duration;
                return;
            }
            this.gravityPoints -= 3;

            AbilityUser.SpawnThings tempPod = new SpawnThings();
            tempPod.def = ThingDef.Named("ChunkSandstone");
            tempPod.spawnCount = 1;
            SingleSpawnLoop(tempPod, origin, this.caster.Map);

            float magnitude = (origin.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
            Find.CameraDriver.shaker.DoShake(6 / magnitude);

            ThrowStone(origin);
        }

        public void SearchAndThrow()
        {
            launchableThings.RemoveAll(t => !AvailableStone(t));
            if (!launchableThings.TryRandomElement(out launchableThing)) return;
            this.gravityPoints--;
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

            if (launchableThing != null && destination.IsValid)
            {                
                float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(origin, destination)).ToAngleFlat();
                for (int m = 0; m < 4; m++)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, origin.ToVector3Shifted(), this.caster.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(3f, 5f), launchAngle += Rand.Range(-25,25), Rand.Range(0, 360));
                }
                FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), origin, this.caster.Map);
                flyingObject.force = this.arcaneDmg + .2f;
                flyingObject.hammerOwner = projectedOwner;
                flyingObject.Launch(this.caster, destination, this.launchableThing.SplitOff(1), this.spinRate);
            }
        }

        protected override void Tick()
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
