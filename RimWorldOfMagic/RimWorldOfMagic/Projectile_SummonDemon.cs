using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_SummonDemon : Projectile_Ability
    {

        private int age = -1;

        private bool initialized = false;
        private bool summoning = false;
        private bool summoningComplete = false;
        private int summoningDuration = 300;
        private int nextBlackLightning = 0;
        private int lightingCount = 1;
        private bool destroyed = false;

        private int duration = 6660;

        CompAbilityUserMagic comp;
        Pawn sacrificedPawn = null;
        Pawn demonPawn = null;
        Pawn casterPawn = null;
        List<IntVec3> summoningCircle = new List<IntVec3>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.summoning, "summoning", false, false);
            Scribe_Values.Look<bool>(ref this.summoningComplete, "summoningComplete", false, false);
            Scribe_Values.Look<bool>(ref this.destroyed, "destroyed", false, false);
            Scribe_References.Look<Pawn>(ref this.demonPawn, "demonPawn", false);
            Scribe_References.Look<Pawn>(ref this.sacrificedPawn, "sacrificedPawn", false);
            Scribe_Collections.Look<IntVec3>(ref this.summoningCircle, "summoningCircle", LookMode.Value);
        }

        public override void Tick()
        {
            base.Tick();
            if(this.demonPawn != null)
            {
                if (this.demonPawn.Destroyed || this.demonPawn.Dead)
                {
                    this.age = this.duration + this.summoningDuration + 1;
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < (duration + summoningDuration);
            if (!flag)
            {
                if (this.sacrificedPawn != null)
                {
                    GenPlace.TryPlaceThing(this.sacrificedPawn, this.summoningCircle[0], this.Map, ThingPlaceMode.Near, null, null);
                    HealthUtility.AdjustSeverity(this.sacrificedPawn, HediffDef.Named("TM_DemonicPriceHD"), 2f);
                }
                base.Destroy(mode);
            }
        }

        private void Initialize()
        {
            this.casterPawn = this.launcher as Pawn;
            this.comp = this.casterPawn.GetCompAbilityUserMagic();
            this.sacrificedPawn = comp.soulBondPawn;
            this.sacrificedPawn.TakeDamage(new DamageInfo(DamageDefOf.Stun, 50, 50, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, this.sacrificedPawn));
            HealthUtility.AdjustSeverity(this.sacrificedPawn, HediffDef.Named("TM_HediffTimedInvulnerable"), 2);
            this.summoningCircle = new List<IntVec3>();
            this.summoningCircle.Clear();
            this.summoningCircle = GenRadial.RadialCellsAround(this.sacrificedPawn.Position, 5, false).ToList();
            this.age = 0;
            this.summoning = true;
            this.initialized = true;
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            //base.Impact(hitThing);

            if (!initialized)
            {
                Initialize();
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_SummoningCircle, this.summoningCircle[0].ToVector3Shifted(), this.Map, 3f, 2f, 1f, 2f, 0, 0, 0, Rand.Range(0, 360));
            }

            if (summoning)
            {
                if(Find.TickManager.TicksGame % 2 ==0)
                {
                    IntVec3 randomCircleCell = this.summoningCircle.RandomElement();
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Demon_Flame, randomCircleCell.ToVector3(), this.sacrificedPawn.Map, Rand.Range(.5f, .9f), Rand.Range(.2f, .3f), .05f, Rand.Range(.2f, .4f), Rand.Range(-400, 400), Rand.Range(.8f, 1.2f) * (randomCircleCell - this.sacrificedPawn.Position).LengthHorizontal, (Quaternion.AngleAxis(90, Vector3.up) * GetVector(randomCircleCell, this.sacrificedPawn.Position)).ToAngleFlat(), Rand.Range(0, 359));
                }

                if(this.nextBlackLightning < this.age)
                {
                    DoLightningStrike();
                    this.nextBlackLightning = this.age + Rand.Range(45,70);
                    this.lightingCount++;
                }
            }

            if(!this.summoningComplete && this.age > this.summoningDuration)
            {
                for (int i = 0; i < 3; i++)
                {
                    DoLightningStrike();
                }
                IntVec3 centerCell = this.sacrificedPawn.Position;
                SpawnThings spawnThing = new SpawnThings();
                spawnThing.factionDef = TorannMagicDefOf.TM_SummonedFaction;
                spawnThing.spawnCount = 1;
                spawnThing.temporary = false;
                spawnThing.def = TorannMagicDefOf.TM_DemonR;
                spawnThing.kindDef = PawnKindDef.Named("TM_Demon");
                map = this.Map;
                this.sacrificedPawn.DeSpawn();                
                SingleSpawnLoop(spawnThing, centerCell, map);

                FleckMaker.ThrowSmoke(centerCell.ToVector3(), map, 2);
                FleckMaker.ThrowMicroSparks(centerCell.ToVector3(), map);
                FleckMaker.ThrowHeatGlow(centerCell, map, 2);

                SoundInfo info = SoundInfo.InMap(new TargetInfo(centerCell, map, false), MaintenanceType.None);
                info.pitchFactor = 1.3f;
                info.volumeFactor = 1.6f;
                TorannMagicDefOf.TM_DemonDeath.PlayOneShot(info);
                this.summoningComplete = true;
                this.summoning = false;
            }
            Destroy();
        }

        public void DoLightningStrike()
        {
            Map.weatherManager.eventHandler.AddEvent(new TM_WeatherEvent_MeshFlash(this.Map, this.summoningCircle.RandomElement(), TM_MatPool.redLightning));
        }

        public void SingleSpawnLoop(SpawnThings spawnables, IntVec3 position, Map map)
        {
            bool flag = spawnables.def != null;
            if (flag)
            {
                Faction faction = this.launcher.Faction;
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
                        TMPawnSummoned newPawn = new TMPawnSummoned();
                        newPawn = (TMPawnSummoned)PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
                        newPawn.validSummoning = true;
                        newPawn.Spawner = this.casterPawn;
                        newPawn.Temporary = true;
                        newPawn.TicksToDestroy = this.duration;
                        try
                        {
                            GenSpawn.Spawn(newPawn, position, map);
                            this.demonPawn = newPawn;
                        }
                        catch
                        {
                            this.age = this.duration;
                            Log.Message("TM_Exception".Translate(
                                this.casterPawn.LabelShort,
                                this.def.defName
                                ));
                            this.Destroy(DestroyMode.Vanish);
                        }
                        if (newPawn.Faction != null && newPawn.Faction != Faction.OfPlayer)
                        {
                            Lord lord = null;
                            if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != newPawn))
                            {
                                Predicate<Thing> validator = (Thing p) => p != newPawn && ((Pawn)p).GetLord() != null;
                                Pawn p2 = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator, null);
                                lord = p2.GetLord();
                            }
                            bool flag4 = lord == null;
                            if (flag4)
                            {
                                LordJob_DefendPoint lordJob = new LordJob_DefendPoint(newPawn.Position);
                                lord = LordMaker.MakeNewLord(faction, lordJob, map, null);
                            }
                            lord.AddPawn(newPawn);
                        }
                    }
                }
                else
                {
                    Log.Message("Missing race");
                }
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }
}
