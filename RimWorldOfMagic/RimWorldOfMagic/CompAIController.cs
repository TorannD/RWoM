using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace TorannMagic
{
    [Serializable]
    public class CompAIController : ThingComp
	{
        private bool initialized = false;

        List<Pawn> threatList = new List<Pawn>();
        List<Pawn> closeThreats = new List<Pawn>();
        List<Pawn> farThreats = new List<Pawn>();
        List<Pawn> meleeThreats = new List<Pawn>();
        List<Building> buildingThreats = new List<Building>();

        public int nextRangedAttack = 0;
        public int nextAoEAttack = 0;
        public int nextKnockbackAttack = 0;
        public int nextChargeAttack = 0;
        public int nextTaunt = 0;

        private int rangedBurstShots = 0;
        private int rangedNextBurst = 0;
        private float meleeRange = 1.4f;
        private LocalTargetInfo rangedTarget = null;

        private int age = -1;
        private bool deathOnce = false;

        //private int actionReady = 0;
        //private int actionTick = 0;

        //private LocalTargetInfo universalTarget = null;

        private int NextRangedAttack
        {
            get
            {
                if(this.Props.rangedCooldownTicks > 0)
                {
                    return this.nextRangedAttack;
                }
                else
                {
                    return Find.TickManager.TicksGame;
                }
            }
        }

        private void StartRangedAttack()
        {
            this.nextRangedAttack = this.Props.rangedCooldownTicks + Find.TickManager.TicksGame;
            this.rangedBurstShots = this.Props.rangedBurstCount;
            this.rangedNextBurst = Find.TickManager.TicksGame + this.Props.rangedTicksBetweenBursts;
            this.nextChargeAttack = Find.TickManager.TicksGame + 150;
        }

        private void DoRangedAttack(LocalTargetInfo target)
        {            
            bool flag = target.Cell != default(IntVec3);
            if (flag)
            {
                Thing launchedThing = new Thing()
                {
                    def = ThingDef.Named("FlyingObject_DemonBolt")
                };
                FlyingObject_Advanced flyingObject = (FlyingObject_Advanced)GenSpawn.Spawn(ThingDef.Named("FlyingObject_DemonBolt"), this.Pawn.Position, this.Pawn.Map);
                flyingObject.AdvancedLaunch(this.Pawn, TorannMagicDefOf.Mote_Demon_Flame, 2, Rand.Range(5, 60), false, this.Pawn.DrawPos, target.Cell, launchedThing, Rand.Range(32, 38), true, Rand.Range(18, 26), Rand.Range(1.4f, 2.4f), DamageDefOf.Burn, null);
            }
        }

        private int NextAoEAttack
        {
            get
            {
                if (this.Props.aoeCooldownTicks > 0)
                {
                    return this.nextAoEAttack;
                }
                else
                {
                    return Find.TickManager.TicksGame;
                }
            }
        }

        private void DoAoEAttack(IntVec3 center, bool isExplosion, float radius, DamageDef damageType, int damageAmount)
        {
            this.nextAoEAttack = this.Props.aoeCooldownTicks + Find.TickManager.TicksGame;
            List<IntVec3> targetCells = GenRadial.RadialCellsAround(center, radius, false).ToList();
            IntVec3 curCell = default(IntVec3);
            for (int i = 0; i < targetCells.Count(); i++)
            {
                curCell = targetCells[i];
                if (curCell.IsValid && curCell.InBoundsWithNullCheck(this.Pawn.Map))
                {
                    if(isExplosion)
                    {
                        GenExplosion.DoExplosion(curCell, this.Pawn.Map, .4f, damageType, this.Pawn, damageAmount, Rand.Range(0, damageAmount), TorannMagicDefOf.TM_SoftExplosion, null, null, null, null, 0f, 1, false, null, 0f, 0, 0.0f, false);
                    }
                    else
                    {
                        List<Thing> thingList = curCell.GetThingList(this.Pawn.Map);
                        for(int j = 0; j < thingList.Count(); j++)
                        {
                            DamageEntities(thingList[j], damageAmount, damageType, this.Pawn);
                        }
                    }
                }
            }
        }

        private int NextKnockbackAttack
        {
            get
            {
                if (this.Props.knockbackCooldownTicks > 0)
                {
                    return this.nextKnockbackAttack;
                }
                else
                {
                    return Find.TickManager.TicksGame;
                }
            }
        }

        private void DoKnockbackAttack(IntVec3 center, IntVec3 target, float radius, float force)
        {
            this.nextKnockbackAttack = this.Props.knockbackCooldownTicks + Find.TickManager.TicksGame;
            List<IntVec3> targetCells = GenRadial.RadialCellsAround(target, radius, true).ToList();
            IntVec3 curCell = default(IntVec3);
            for (int i = 0; i < targetCells.Count(); i++)
            {
                curCell = targetCells[i];
                if (curCell.IsValid && curCell.InBoundsWithNullCheck(this.Pawn.Map))
                {
                    Vector3 launchVector = GetVector(this.Pawn.Position, curCell);
                    Pawn knockbackPawn = curCell.GetFirstPawn(this.Pawn.Map);
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.6f, 1f), .01f, .01f, 1f, Rand.Range(50, 100), Rand.Range(5, 7), launchVector.ToAngleFlat(), Rand.Range(0, 360));
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, curCell.ToVector3Shifted(), this.Pawn.Map, Rand.Range(.6f, 1f), .01f, .01f, 1f, Rand.Range(50, 100), Rand.Range(5, 7), launchVector.ToAngleFlat(), Rand.Range(0, 360));
                    if (knockbackPawn != null && knockbackPawn != this.Pawn)
                    {
                        IntVec3 targetCell = knockbackPawn.Position + (force * force * launchVector).ToIntVec3();
                        bool flag = targetCell != null && targetCell != default(IntVec3);
                        if (flag)
                        {
                            if (knockbackPawn.Spawned && knockbackPawn.Map != null && !knockbackPawn.Dead)
                            {
                               FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), knockbackPawn.Position, knockbackPawn.Map, WipeMode.Vanish);
                                flyingObject.speed = 15 + (2*force);
                                flyingObject.Launch(this.Pawn, targetCell, knockbackPawn);
                            }
                        }
                    }
                }
            }
        }

        private int NextChargeAttack
        {
            get
            {
                if (this.Props.chargeCooldownTicks > 0)
                {
                    return this.nextChargeAttack;
                }
                else
                {
                    return Find.TickManager.TicksGame;
                }
            }
        }

        private void DoChargeAttack(LocalTargetInfo t)
        {
            this.nextChargeAttack = this.Props.chargeCooldownTicks + Find.TickManager.TicksGame;
            bool flag = t.Cell != default(IntVec3) && t.Cell.DistanceToEdge(this.Pawn.Map) > 6;
            if (flag)
            {
                this.Pawn.rotationTracker.Face(t.CenterVector3);
                FlyingObject_DemonFlight flyingObject = (FlyingObject_DemonFlight)GenSpawn.Spawn(ThingDef.Named("FlyingObject_DemonFlight"), this.Pawn.Position, this.Pawn.Map);
                flyingObject.Launch(this.Pawn, t.Cell, this.Pawn);

            }
        }

        private int NextTaunt
        {
            get
            {
                if (this.Props.chargeCooldownTicks > 0)
                {
                    return this.nextTaunt;
                }
                else
                {
                    return Find.TickManager.TicksGame;
                }
            }
        }

        private void DoTaunt(Map map)
        {
            this.nextTaunt = this.Props.tauntCooldownTicks + Find.TickManager.TicksGame;
            if (map != null)
            {
                List<Pawn> threatPawns = map.mapPawns.AllPawnsSpawned;
                bool anyPawnsTaunted = false;
                if (threatPawns != null && threatPawns.Count > 0)
                {
                    for (int i = 0; i < threatPawns.Count; i++)
                    {
                        if (threatPawns[i].Faction != null && this.Pawn.Faction != null && threatPawns[i].Faction.HostileTo(this.Pawn.Faction) && !threatPawns[i].IsColonist)
                        {
                            if (threatPawns[i].jobs != null && threatPawns[i].CurJob != null && threatPawns[i].CurJob.targetA != null && threatPawns[i].CurJob.targetA.Thing != null && threatPawns[i].CurJob.targetA.Thing != this.Pawn)
                            {
                                if (Rand.Chance(this.Props.tauntChance) && (threatPawns[i].Position - this.Pawn.Position).LengthHorizontal < 60)
                                {
                                    //Log.Message("taunting " + threatPawns[i].LabelShort + " doing job " + threatPawns[i].CurJobDef.defName + " with follow radius of " + threatPawns[i].CurJob.followRadius);
                                    if(threatPawns[i].CurJobDef == JobDefOf.Follow || threatPawns[i].CurJobDef == JobDefOf.FollowClose)
                                    {
                                        Job job = new Job(JobDefOf.AttackMelee, this.Pawn);
                                        threatPawns[i].jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }
                                    else
                                    {
                                        Job job = new Job(threatPawns[i].CurJobDef, this.Pawn);
                                        threatPawns[i].jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }                                        
                                    anyPawnsTaunted = true;                                    
                                    //Log.Message("taunting " + threatPawns[i].LabelShort);
                                }
                            }
                        }
                    }
                    if (anyPawnsTaunted)
                    {
                        MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, "TM_Taunting".Translate(), -1);
                    }
                }
            }
        }

        private Pawn Pawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                bool flag = pawn == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
        }

        private List<Pawn> PawnThreatList
        {
            get
            {
                return closeThreats.Union(farThreats).ToList();
            }
        }

        public CompProperties_AIController Props
        {
            get
            {
                return (CompProperties_AIController)this.props;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        public override void CompTick()
        {
            if (this.age > 0)
            {
                if (!this.initialized)
                {
                    if (this.Props.alwaysManhunter || this.Pawn.Faction != Faction.OfPlayer)
                    {
                        this.Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, null);
                    }
                    if (this.Pawn.def.defName == "TM_DemonR" || this.Pawn.def.defName == "TM_LesserDemonR")
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_DemonHD"), .5f);
                    }
                    this.initialized = true;
                }

                if (this.Pawn.Spawned)
                {
                    if (!this.Pawn.Downed)
                    {
                        if (this.NextTaunt < Find.TickManager.TicksGame)
                        {
                            DoTaunt(this.Pawn.Map);
                            this.nextTaunt = this.Props.tauntCooldownTicks + Find.TickManager.TicksGame;
                        }

                        if (this.rangedBurstShots > 0 && this.rangedNextBurst < Find.TickManager.TicksGame)
                        {
                            DoRangedAttack(this.rangedTarget);
                            this.rangedBurstShots--;
                            this.rangedNextBurst = Find.TickManager.TicksGame + this.Props.rangedTicksBetweenBursts;
                        }

                        if (Find.TickManager.TicksGame % 30 == 0)
                        {
                            if (this.buildingThreats.Count() > 0)
                            {
                                Building randomBuildingThreat = this.buildingThreats.RandomElement();
                                if ((randomBuildingThreat.Position - this.Pawn.Position).LengthHorizontal < 50 && this.NextRangedAttack < Find.TickManager.TicksGame && TargetIsValid(randomBuildingThreat))
                                {
                                    this.rangedTarget = randomBuildingThreat;
                                    StartRangedAttack();
                                }
                            }

                            Thing currentTargetThing = this.Pawn.CurJob.targetA.Thing;
                            if (currentTargetThing != null && this.Pawn.TargetCurrentlyAimingAt == null)
                            {
                                if ((currentTargetThing.Position - this.Pawn.Position).LengthHorizontal > (this.Props.maxRangeForCloseThreat * 2))
                                {
                                    if (Rand.Chance(.6f) && this.NextRangedAttack < Find.TickManager.TicksGame && TargetIsValid(currentTargetThing))
                                    {
                                        this.rangedTarget = currentTargetThing;
                                        StartRangedAttack();
                                    }
                                    else if (this.NextChargeAttack < Find.TickManager.TicksGame && TargetIsValid(currentTargetThing))
                                    {
                                        DoChargeAttack(currentTargetThing);
                                        goto exitTick;
                                    }
                                }
                            }
                            else if (this.Pawn.TargetCurrentlyAimingAt != null && this.closeThreats.Count() > 1)
                            {
                                if (Rand.Chance(.2f) && this.NextAoEAttack < Find.TickManager.TicksGame)
                                {
                                    DoAoEAttack(this.Pawn.Position, true, 2f, DamageDefOf.Stun, Rand.Range(4, 8));
                                }

                                if (Rand.Chance(.2f) && this.farThreats.Count() > (5 * this.closeThreats.Count()))
                                {
                                    this.Pawn.CurJob.targetA = this.farThreats.RandomElement();
                                }
                            }                            

                            if (this.closeThreats.Count() > 1 && ((this.closeThreats.Count() * 2) > this.farThreats.Count() || Rand.Chance(.3f)))
                            {
                                if (Rand.Chance(.8f) && this.NextKnockbackAttack < Find.TickManager.TicksGame)
                                {
                                    Pawn randomClosePawn = this.closeThreats.RandomElement();
                                    if ((randomClosePawn.Position - this.Pawn.Position).LengthHorizontal < 3 && TargetIsValid(randomClosePawn))
                                    {
                                        DoKnockbackAttack(this.Pawn.Position, randomClosePawn.Position, 1.4f, Rand.Range(3, 5f));
                                    }
                                }
                            }

                            if(this.farThreats.Count() > 2 * this.closeThreats.Count() && this.meleeThreats.Count() < 1 && Rand.Chance(.3f))
                            {
                                Pawn randomRangedPawn = this.farThreats.RandomElement();
                                if(this.NextChargeAttack < Find.TickManager.TicksGame)
                                {
                                    Thing tempTarget = this.farThreats.RandomElement();
                                    if (TargetIsValid(tempTarget))
                                    {
                                        this.Pawn.TryStartAttack(tempTarget);
                                        DoChargeAttack(tempTarget);
                                        goto exitTick;
                                    }
                                }
                            }

                            if (this.farThreats.Count() > 2)
                            {
                                if (Rand.Chance(.4f) && this.NextRangedAttack < Find.TickManager.TicksGame)
                                {
                                    Pawn randomRangedPawn = this.farThreats.RandomElement();
                                    if ((randomRangedPawn.Position - this.Pawn.Position).LengthHorizontal < this.Props.maxRangeForFarThreat * 1.2f)
                                    {
                                        this.rangedTarget = randomRangedPawn;
                                        StartRangedAttack();
                                    }
                                }
                            }

                            if (currentTargetThing == null || currentTargetThing == this.Pawn)
                            {
                                if (this.closeThreats.Count() > 0)
                                {
                                    Thing tempTarget = this.closeThreats.RandomElement();
                                    if (TargetIsValid(tempTarget))
                                    {
                                        this.Pawn.CurJob.targetA = tempTarget;
                                        this.Pawn.TryStartAttack(this.Pawn.CurJob.targetA);
                                    }
                                }
                                else if (this.farThreats.Count() > 0)
                                {
                                    Thing tempTarget = this.farThreats.RandomElement();
                                    if (TargetIsValid(tempTarget))
                                    {
                                        this.Pawn.CurJob.targetA = tempTarget;
                                        this.Pawn.TryStartAttack(this.Pawn.CurJob.targetA);
                                    }
                                }
                                else if (this.buildingThreats.Count() > 0)
                                {
                                    Thing tempTarget = this.buildingThreats.RandomElement();
                                    if (TargetIsValid(tempTarget))
                                    {
                                        this.Pawn.CurJob.targetA = tempTarget;
                                        this.Pawn.TryStartAttack(this.Pawn.CurJob.targetA);
                                    }
                                }
                            }

                        }

                        if (this.Pawn.Faction != Faction.OfPlayer && Find.TickManager.TicksGame % 300 == 0)
                        {
                            if (this.Props.alwaysManhunter)
                            {
                                this.Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, null);
                            }
                        }

                        if (Find.TickManager.TicksGame % 120 == 0)
                        {
                            DetermineThreats();
                        }
                    }

                    if (this.Pawn.Downed && this.Pawn.def.defName == "TM_DemonR" && Find.TickManager.TicksGame % 18 == 0)
                    {
                        if (!deathOnce)
                        {
                            CellRect cellRect = CellRect.CenteredOn(this.Pawn.Position, 3);
                            cellRect.ClipInsideMap(this.Pawn.Map);
                            GenExplosion.DoExplosion(cellRect.RandomCell, this.Pawn.Map, 2f, DamageDefOf.Burn, this.Pawn, Rand.Range(6, 12), -1, DamageDefOf.Bomb.soundExplosion, null, null, null, null, 0f, 1, false, null, 0f, 0, 0.2f, true);
                            DamageEntities(this.Pawn, 10f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.Pawn);
                            deathOnce = true;
                        }
                        else if(!this.Pawn.Dead)
                        {
                            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Spirit, 10, 2, (float)-1, this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                            this.Pawn.Kill(dinfo);
                        }
                    }
                }
            }
            exitTick:;
            age++;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);
            if (dinfo.Instigator is Building instigatorThing)
            {
                if (instigatorThing is Building)
                {
                    if (instigatorThing.Faction != null && instigatorThing.Faction != this.Pawn.Faction)
                    {
                        this.buildingThreats.AddDistinct(instigatorThing);
                    }
                }
            }
        }

        private void DetermineThreats()
        {
            try
            {
                this.closeThreats.Clear();
                this.farThreats.Clear();
                this.meleeThreats.Clear();
                List<Pawn> allPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count(); i++)
                {
                    if (!allPawns[i].DestroyedOrNull() && allPawns[i] != this.Pawn)
                    {
                        if (!allPawns[i].Dead && !allPawns[i].Downed)
                        {
                            if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForCloseThreat)
                            {
                                if (this.Pawn.Faction.HostileTo(allPawns[i].Faction))
                                {
                                    this.closeThreats.Add(allPawns[i]);
                                }
                                else if (allPawns[i].Faction == null && allPawns[i].InMentalState)
                                {
                                    this.closeThreats.Add(allPawns[i]);
                                }
                            }
                            else if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForFarThreat)
                            {
                                if (this.Pawn.Faction.HostileTo(allPawns[i].Faction))
                                {
                                    this.farThreats.Add(allPawns[i]);
                                }
                                else if (allPawns[i].Faction == null && allPawns[i].InMentalState)
                                {
                                    this.farThreats.Add(allPawns[i]);                                    
                                }
                            }
                            else if((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.meleeRange)
                            {
                                if (this.Pawn.Faction.HostileTo(allPawns[i].Faction))
                                {
                                    this.meleeThreats.Add(allPawns[i]);
                                }
                                else if (allPawns[i].Faction == null && allPawns[i].InMentalState)
                                {
                                    this.meleeThreats.Add(allPawns[i]);
                                }
                            }
                        }
                    }
                }
                if (this.closeThreats.Count() < 1 && this.farThreats.Count() < 1)
                {
                    Pawn randomMapPawn = allPawns.RandomElement();
                    if (TargetIsValid(randomMapPawn) && randomMapPawn.RaceProps.Humanlike)
                    {
                        if (randomMapPawn.Faction != null && randomMapPawn.Faction != this.Pawn.Faction && (this.Pawn.Faction.HostileTo(randomMapPawn.Faction) || randomMapPawn.InMentalState))
                        {
                            this.farThreats.Add(randomMapPawn);
                        }
                    }
                }
                for (int i = 0; i < this.buildingThreats.Count(); i++)
                {
                    if (this.buildingThreats[i].DestroyedOrNull())
                    {
                        this.buildingThreats.Remove(this.buildingThreats[i]);
                    }
                }
            }
            catch(NullReferenceException ex)
            {
                //Log.Message("Error processing threats" + ex);
            }
        }

        public void DamageEntities(Thing e, float d, DamageDef type, Thing instigator)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, Rand.Range(0,amt), (float)-1, instigator, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public bool TargetIsValid(Thing target)
        {
            if(target.DestroyedOrNull())
            {
                return false;
            }
            if(!target.Spawned)
            {
                return false;
            }
            if (target is Pawn targetPawn)
            {
                return !targetPawn.Downed;
            }
            if(target.Position.DistanceToEdge(this.Pawn.Map) < 8)
            {
                return false;
            }
            if(target.Faction != null && target.Faction != this.Pawn.Faction)
            {
                return (this.Pawn.Faction.HostileTo(target.Faction));
            }
            return true;
        }
    }
}