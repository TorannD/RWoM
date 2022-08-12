using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
using AbilityUser;

namespace TorannMagic
{
    [Serializable]
    public class CompSkeletonLichController : ThingComp
	{
        private bool initialized = false;

        List<Pawn> threatList = new List<Pawn>();
        List<Pawn> closeThreats = new List<Pawn>();
        List<Pawn> farThreats = new List<Pawn>();
        public List<Building> buildingThreats = new List<Building>();

        public int nextRangedAttack = 0;
        public int nextAoEAttack = 0;
        public int nextKnockbackAttack = 0;
        public int nextChargeAttack = 0;
        public int nextTaunt = 0;
        public int castingCompleteTick = 0;

        private int rangedBurstShots = 0;
        private int rangedNextBurst = 0;
        private LocalTargetInfo rangedTarget = null;
        private LocalTargetInfo flightTarget = null;
        private bool shouldDoAOEAttack = false;
        private bool shouldDoKnockBackAttack = false;
        private bool shouldDoTaunt = false;
        private LocalTargetInfo attackTarget = null;
        public LocalTargetInfo tauntTarget = null;

        private int age = -1;

        public float geChance = .0023f;
        public float leChance = .011f;
        public float raiseRadius = 4f;
        public bool shouldAssault = false;

        //private int actionReady = 0;
        //private int actionTick = 0;

        //private LocalTargetInfo universalTarget = null;

        //This comp controller is unique to the undead lich and interfaces with the wandering lich event
        //Unique abilities:
        //Ranged attack - launches several death bolts, similar to a player lich master spell
        //AoE attack - creates fields of "fog of torment" that will heal other undead
        //Knockback attack - stuns (short) and curses nearby pawns
        //Charge attack - flight, used primarily to escape from damage or when too many enemies are nearby
        //Taunt - raises a host of undead skeletons to fight for the lich

        private Vector3 MoteDrawPos
        {
            get
            {
                Vector3 drawPos = this.Pawn.DrawPos;
                drawPos.z -= .6f;
                drawPos.x += Rand.Range(-.5f, .5f);
                return drawPos;
            }
        }

        public bool IsCasting
        {
            get
            {
                return this.castingCompleteTick >= Find.TickManager.TicksGame;
            }
        }

        public Pawn ParentPawn
        {
            get
            {
                return this.Pawn;
            }
        }

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
            if (this.rangedTarget != null && TM_Calc.HasLoSFromTo(this.Pawn.Position, this.rangedTarget, this.Pawn, 4, this.Props.maxRangeForFarThreat))
            {
                this.nextRangedAttack = (int)(this.Props.rangedCooldownTicks * Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
                this.rangedBurstShots = this.Props.rangedBurstCount;
                this.rangedNextBurst = Find.TickManager.TicksGame + this.Props.rangedTicksBetweenBursts;
                this.castingCompleteTick = Find.TickManager.TicksGame + this.Props.rangedAttackDelay;
                
                //this.Pawn.CurJob.SetTarget(this.Pawn.jobs.curDriver.rotateToFace, rangedTarget);
                TM_Action.PawnActionDelay(this.Pawn, this.Props.rangedAttackDelay, this.rangedTarget, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
            }
        }

        private void DoRangedAttack(LocalTargetInfo target)
        {
            bool flag = target.Cell != default(IntVec3);
            if (flag)
            {
                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                info.pitchFactor = .7f;
                info.volumeFactor = 2f;
                TorannMagicDefOf.TM_AirWoosh.PlayOneShot(info);

                CellRect cellRect = CellRect.CenteredOn(target.Cell, 4);
                cellRect.ClipInsideMap(this.Pawn.Map);
                IntVec3 destination = cellRect.RandomCell;

                if (destination != null)
                {
                    Thing launchedThing = new Thing()
                    {
                        def = TorannMagicDefOf.FlyingObject_DeathBolt
                    };
                    Pawn casterPawn = this.Pawn;
                    //LongEventHandler.QueueLongEvent(delegate
                    //{
                        FlyingObject_DeathBolt flyingObject = (FlyingObject_DeathBolt)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_DeathBolt, this.Pawn.Position, this.Pawn.Map);
                        flyingObject.Launch(this.Pawn, destination, launchedThing);
                    //}, "LaunchingFlyer", false, null);
                }
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

        private void StartAoEAttack(IntVec3 center, LocalTargetInfo target)
        {
            if (target.Thing != null && target.Thing.Map == this.Pawn.Map)
            {
                this.nextAoEAttack = (int)(this.Props.aoeCooldownTicks * Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
                this.castingCompleteTick = this.Props.aoeAttackDelay + Find.TickManager.TicksGame;
                TM_Action.PawnActionDelay(this.Pawn, this.Props.aoeAttackDelay, target, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
                this.shouldDoAOEAttack = true;
            }
        }

        private void DoAoEAttack(IntVec3 center, LocalTargetInfo target)
        {
            TM_CopyAndLaunchProjectile.CopyAndLaunchThing(TorannMagicDefOf.Projectile_FogOfTorment, this.Pawn, center, target, ProjectileHitFlags.All, null);
            this.shouldDoAOEAttack = false;
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

        private void StartKnockbackAttack(IntVec3 target, float radius)
        {
            this.nextKnockbackAttack = (int)(this.Props.knockbackCooldownTicks * Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
            this.castingCompleteTick = this.Props.knockbackAttackDelay + Find.TickManager.TicksGame;
            TM_Action.PawnActionDelay(this.Pawn, this.Props.knockbackAttackDelay, target, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
            this.shouldDoKnockBackAttack = true;
        }

        private void DoKnockbackAttack(IntVec3 target, float radius)
        {
            int pwrVal = 3;
            int verVal = 3;
            List<Pawn> TargetsAoE = TM_Calc.FindPawnsNearTarget(this.Pawn, 5, target, true);
            if (TargetsAoE != null && TargetsAoE.Count > 0)
            {
                for (int i = 0; i < TargetsAoE.Count; i++)
                {
                    Pawn victim = TargetsAoE[i];
                    if (!victim.RaceProps.IsMechanoid)
                    {
                        if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.Pawn, victim, true)))
                        {
                            HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_DeathMarkCurse"), (Rand.Range(1f + pwrVal, 4 + 2 * pwrVal)));
                            TM_MoteMaker.ThrowSiphonMote(victim.DrawPos, victim.Map, 1f);

                            if (Rand.Chance(verVal * .2f))
                            {
                                if (Rand.Chance(verVal * .1f)) //terror
                                {
                                    HealthUtility.AdjustSeverity(victim, HediffDef.Named("TM_Terror"), Rand.Range(3f * verVal, 5f * verVal));
                                    TM_MoteMaker.ThrowDiseaseMote(victim.DrawPos, victim.Map, 1f, .5f, .2f, .4f);
                                    MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Terror", -1);
                                }
                                if (Rand.Chance(verVal * .1f)) //berserk
                                {
                                    if (victim.mindState != null && victim.RaceProps != null && victim.RaceProps.Humanlike)
                                    {
                                        victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "cursed", true, false, null);
                                        FleckMaker.ThrowMicroSparks(victim.DrawPos, victim.Map);
                                        MoteMaker.ThrowText(victim.DrawPos, victim.Map, "Berserk", -1);
                                    }

                                }
                            }
                        }
                        else
                        {
                            MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                        }
                    }
                }
            }
            this.shouldDoKnockBackAttack = false;
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

        private void StartChargeAttack(IntVec3 t)
        {
            this.nextChargeAttack = this.Props.chargeCooldownTicks + Find.TickManager.TicksGame;
            bool flag = t != null && t.DistanceToEdge(this.Pawn.Map) > 6;
            if (flag && t.InBoundsWithNullCheck(this.Pawn.Map) && t.IsValid && t.Walkable(this.Pawn.Map) && Pawn.Position.DistanceTo(t) <= 60)
            {
                this.castingCompleteTick = Find.TickManager.TicksGame + this.Props.chargeAttackDelay;
                this.flightTarget = t;
                TM_Action.PawnActionDelay(this.Pawn, this.Props.chargeAttackDelay, t, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
            }
            else
            {
                this.flightTarget = null;
            }
        }

        private void DoChargeAttack(LocalTargetInfo t)
        {
            if (t != null && t.Cell.DistanceToEdge(this.Pawn.Map) > 6)
            {
                this.Pawn.rotationTracker.Face(t.CenterVector3);
               // Log.Message("flying to " + t.Cell);
                LongEventHandler.QueueLongEvent(delegate
                {
                    FlyingObject_Flight flyingObject = (FlyingObject_Flight)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Flight"), this.Pawn.Position, this.Pawn.Map);
                    flyingObject.Launch(this.Pawn, t.Cell, this.Pawn);
                }, "LaunchingFlyer", false, null);
                this.flightTarget = null;
            }
        }

        private int NextTaunt
        {
            get
            {
                if (this.Props.tauntCooldownTicks > 0)
                {
                    return this.nextTaunt;
                }
                else
                {
                    return Find.TickManager.TicksGame;
                }
            }
        }

        public void GotoRaiseLocation(Map map, LocalTargetInfo target)
        {
            this.nextTaunt = (int)(this.Props.tauntCooldownTicks * Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
            this.tauntTarget = target;
            Job job = new Job(JobDefOf.Goto, target);
            job.locomotionUrgency = LocomotionUrgency.Amble;
            this.Pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, false, false);
        }

        private void StartTaunt(Map map, LocalTargetInfo target)
        {
            this.castingCompleteTick = Find.TickManager.TicksGame + this.Props.tauntAttackDelay;
            TM_Action.PawnActionDelay(this.Pawn, this.Props.tauntAttackDelay, target, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.Pawn));
            this.shouldDoTaunt = true;
        }

        private void DoTaunt(Map map, LocalTargetInfo target)
        {
            this.shouldDoTaunt = false;
            this.tauntTarget = null;
            if (map != null)
            {
                SpawnSkeletonMinions(this.Pawn.Position, this.raiseRadius, this.Pawn.Faction);
                this.shouldAssault = true;
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

        public CompProperties_SkeletonLichController Props
        {
            get
            {
                return (CompProperties_SkeletonLichController)this.props;
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
                    HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_LichHD, .5f);
                    this.initialized = true;
                }                

                if (this.Pawn.Spawned)
                {                    
                    if (!this.Pawn.Downed)
                    {
                        if (!this.Pawn.stances.curStance.StanceBusy)
                        {
                            if (this.tauntTarget != null && this.Pawn.Faction != null && !this.Pawn.Faction.IsPlayer && this.NextTaunt < Find.TickManager.TicksGame && this.Pawn.CurJob.def != JobDefOf.Goto)
                            {
                                if((this.tauntTarget.Cell - this.Pawn.Position).LengthHorizontal > 5)
                                {
                                    GotoRaiseLocation(this.Pawn.Map, this.tauntTarget);
                                }
                                else
                                {
                                    StartTaunt(this.Pawn.Map, this.tauntTarget);
                                }
                            }

                            if(this.shouldDoTaunt)
                            {
                                DoTaunt(this.Pawn.Map, this.tauntTarget.Cell);
                            }

                            if (this.flightTarget != null)
                            {
                                DoChargeAttack(this.flightTarget.Cell);
                                goto exitTick;
                            }                            

                            if (this.rangedBurstShots > 0 && this.rangedNextBurst < Find.TickManager.TicksGame)
                            {
                                DoRangedAttack(this.rangedTarget);
                                this.rangedBurstShots--;
                                this.rangedNextBurst = Find.TickManager.TicksGame + this.Props.rangedTicksBetweenBursts;
                            }

                            if(shouldDoAOEAttack)
                            {
                                if (this.attackTarget != null)
                                {
                                    DoAoEAttack(this.attackTarget.Cell, this.attackTarget);
                                }
                            }

                            if(shouldDoKnockBackAttack)
                            {
                                if (this.attackTarget != null)
                                {
                                    DoKnockbackAttack(this.attackTarget.Cell, 5);
                                }
                            }

                            if (Find.TickManager.TicksGame % 30 == 0)
                            {
                                if (this.buildingThreats.Count() > 0)
                                {
                                    Building randomBuildingThreat = this.buildingThreats.RandomElement();
                                    if ((randomBuildingThreat.Position - this.Pawn.Position).LengthHorizontal < 80 && this.NextRangedAttack < Find.TickManager.TicksGame && TargetIsValid(randomBuildingThreat))
                                    {
                                        this.rangedTarget = randomBuildingThreat;
                                        StartRangedAttack();
                                    }
                                }

                                if (this.Pawn.CurJob != null && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null && this.Pawn.TargetCurrentlyAimingAt == null)
                                {
                                    Thing currentTargetThing = this.Pawn.CurJob.targetA.Thing;
                                    if ((currentTargetThing.Position - this.Pawn.Position).LengthHorizontal > (this.Props.maxRangeForCloseThreat * 2))
                                    {
                                        if (Rand.Chance(.6f) && this.NextRangedAttack < Find.TickManager.TicksGame && TargetIsValid(currentTargetThing))
                                        {
                                            this.rangedTarget = currentTargetThing;
                                            StartRangedAttack();
                                        }
                                    }
                                }
                                else if (this.Pawn.TargetCurrentlyAimingAt != null && this.closeThreats.Count() > 3)
                                {
                                    if (Rand.Chance(.4f) && this.NextAoEAttack < Find.TickManager.TicksGame && TM_Calc.HasLoSFromTo(this.Pawn.Position, this.attackTarget, this.Pawn, 0, 60))
                                    {
                                        this.attackTarget = this.Pawn.TargetCurrentlyAimingAt;
                                        StartAoEAttack(this.attackTarget.Cell, this.attackTarget);                                        
                                    }

                                    if (Rand.Chance(.8f) && this.NextAoEAttack < Find.TickManager.TicksGame && this.farThreats.Count() > (4 * this.closeThreats.Count()) && TM_Calc.HasLoSFromTo(this.Pawn.Position, this.attackTarget, this.Pawn, 0, 60))
                                    {
                                        Pawn p = this.farThreats.RandomElement();
                                        if (TM_Calc.FindAllPawnsAround(this.Pawn.Map, p.Position, 5, p.Faction, false).Count > 3)
                                        {
                                            this.attackTarget = p;
                                            StartAoEAttack(p.Position, p);
                                        }
                                    }
                                }

                                if(this.closeThreats != null && this.closeThreats.Count > 5)
                                {
                                    if (Rand.Chance(.2f) && this.NextKnockbackAttack < Find.TickManager.TicksGame)
                                    {
                                        Pawn p = this.closeThreats.RandomElement();
                                        if (p.health != null && p.health.hediffSet != null && !p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_DeathMarkHD))
                                        {
                                            this.attackTarget = p;
                                            StartKnockbackAttack(p.Position, 5);
                                        }
                                    }
                                    else if(Rand.Chance(.4f) && this.NextChargeAttack < Find.TickManager.TicksGame)
                                    {
                                        this.flightTarget = TM_Calc.TryFindSafeCell(this.Pawn, this.Pawn.Position, 40, 3, 2);
                                        StartChargeAttack(this.flightTarget.Cell);
                                    }
                                }

                                if (this.farThreats.Count() < 2 * this.closeThreats.Count() && Rand.Chance(.3f))
                                {
                                    if (this.NextChargeAttack < Find.TickManager.TicksGame && this.farThreats.Count >= 1)
                                    {
                                        Pawn tempTarget = this.farThreats.RandomElement();
                                        if (TargetIsValid(tempTarget) && (tempTarget.Position - this.Pawn.Position).LengthHorizontal > (this.Props.maxRangeForCloseThreat * 3) && (tempTarget.Position - this.Pawn.Position).LengthHorizontal < (this.Props.maxRangeForCloseThreat * 6))
                                        {
                                            this.flightTarget = tempTarget;
                                            StartChargeAttack(this.flightTarget.Cell);
                                        }
                                    }
                                }

                                if (this.farThreats.Count() > 2)
                                {
                                    if (Rand.Chance(.4f) && this.NextRangedAttack < Find.TickManager.TicksGame)
                                    {
                                        Pawn randomRangedPawn = this.farThreats.RandomElement();
                                        if ((randomRangedPawn.Position - this.Pawn.Position).LengthHorizontal < this.Props.maxRangeForFarThreat * 2f)
                                        {
                                            this.rangedTarget = randomRangedPawn;
                                            StartRangedAttack();
                                        }
                                    }

                                    if(Rand.Chance(.2f) && this.NextAoEAttack < Find.TickManager.TicksGame)
                                    {
                                        Pawn p = this.farThreats.RandomElement();
                                        if ((p.Position - this.Pawn.Position).LengthHorizontal < this.Props.maxRangeForFarThreat * 2f && TM_Calc.HasLoSFromTo(this.Pawn.Position, this.attackTarget, this.Pawn, 0, 60))
                                        {
                                            List<Pawn> threatPawns = TM_Calc.FindAllPawnsAround(this.Pawn.Map, p.Position, 5, p.Faction, true);
                                            if (threatPawns != null && threatPawns.Count > 3)
                                            {
                                                this.attackTarget = p;
                                                StartAoEAttack(this.attackTarget.Cell, this.attackTarget);
                                            }
                                        }
                                    }
                                }

                                if (this.Pawn.CurJob != null)
                                {
                                    if (this.Pawn.CurJob.targetA == null || this.Pawn.CurJob.targetA == this.Pawn)
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
                            }
                        }
                        else if(IsCasting)
                        {
                            if (Find.TickManager.TicksGame % 12 == 0)
                            {
                                TM_MoteMaker.ThrowCastingMote_Anti(this.Pawn.DrawPos, this.Pawn.Map, 2f);
                            }
                        }                        

                        if (Find.TickManager.TicksGame % 279 == 0)
                        {
                            DetermineThreats();
                        }
                    }

                    if (Find.TickManager.TicksGame % 4 == 0)
                    {
                        FleckDef rndMote = FleckDefOf.Smoke;
                        TM_MoteMaker.ThrowGenericFleck(rndMote, MoteDrawPos, this.Pawn.Map, Rand.Range(.4f, .5f), .1f, 0f, Rand.Range(.5f, .6f), Rand.Range(-40, 40), Rand.Range(.2f, .3f), Rand.Range(-95, -110), Rand.Range(0, 360));
                        TM_MoteMaker.ThrowGenericFleck(rndMote, MoteDrawPos, this.Pawn.Map, Rand.Range(.4f, .5f), .1f, 0f, Rand.Range(.5f, .6f), Rand.Range(-40, 40), Rand.Range(.2f, .3f), Rand.Range(90, 110), Rand.Range(0, 360));
                    }

                    if (this.Pawn.Downed)
                    {
                        this.Pawn.Kill(null);
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
            //Log.Message("taking damage");
            if (dinfo.Instigator is Building instigatorThing)
            {
                if (instigatorThing.Faction != null && instigatorThing.Faction != this.Pawn.Faction)
                {
                        //Log.Message("adding building threat");
                        this.buildingThreats.AddDistinct(instigatorThing);
                    
                }
            }
        }

        private void DetermineThreats()
        {
            //Log.Message("checking threats - lich");
            this.closeThreats.Clear();
            this.farThreats.Clear();
            List<Pawn> allPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (!allPawns[i].DestroyedOrNull() && allPawns[i] != this.Pawn)
                {
                    if (!allPawns[i].Dead && !allPawns[i].Downed)
                    {
                        if (allPawns[i].Faction != null && allPawns[i].Faction != this.Pawn.Faction)
                        {
                            if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForCloseThreat)
                            {
                                this.closeThreats.Add(allPawns[i]);
                            }
                            else if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForFarThreat)
                            {
                                this.farThreats.Add(allPawns[i]);                                    
                            }
                        }
                        if(allPawns[i].Faction == null && allPawns[i].InMentalState)
                        {
                            if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForCloseThreat)
                            {
                                this.closeThreats.Add(allPawns[i]);
                            }
                            else if ((allPawns[i].Position - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForFarThreat)
                            {
                                this.farThreats.Add(allPawns[i]);
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
                    if (randomMapPawn.Faction != null && randomMapPawn.Faction != this.Pawn.Faction)
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
            //LearnAndShareBuildingThreats();

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
            if(target is Pawn targetPawn)
            {
                return !targetPawn.Downed;
            }
            if(target.Position.DistanceToEdge(this.Pawn.Map) < 8)
            {
                return false;
            }
            if (target.Map != this.Pawn.Map)
            {
                return false;
            }
            if(target.Faction != null)
            {
                return target.Faction != this.Pawn.Faction && target.Faction.HostileTo(this.Pawn.Faction);
            }
            return true;
        }

        public void SpawnSkeletonMinions(IntVec3 center, float radius, Faction faction)
        {
            IntVec3 curCell;
            Map map = this.Pawn.Map;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(center, radius, true);
            for (int j = 0; j < targets.Count(); j++)
            {
                curCell = targets.ToArray<IntVec3>()[j];
                List<Thing> cellList = curCell.GetThingList(this.Pawn.Map);
                float corpseMult = 0f;
                for(int i =0; i < cellList.Count; i++)
                {
                    if(cellList[i] is Corpse)
                    {
                        corpseMult = .7f;
                        Corpse c = cellList[i] as Corpse;
                        c.Strip();
                        c.Destroy(DestroyMode.Vanish);
                    }
                }
                if (curCell.InBoundsWithNullCheck(map) && curCell.Walkable(map))
                {
                    SpawnThings skeleton = new SpawnThings();
                    if (Rand.Chance(geChance + corpseMult))
                    {
                        skeleton.def = TorannMagicDefOf.TM_GiantSkeletonR;
                        skeleton.kindDef = PawnKindDef.Named("TM_GiantSkeleton");
                    }
                    else if (Rand.Chance(leChance))
                    {
                        skeleton.def = TorannMagicDefOf.TM_SkeletonR;
                        skeleton.kindDef = PawnKindDef.Named("TM_Skeleton");
                    }
                    else
                    {
                        skeleton = null;
                    }
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Disease, curCell.ToVector3Shifted(), this.Pawn.Map, Rand.Range(1.5f, 2.4f), 1f, Rand.Range(.05f, .3f), Rand.Range(.8f, 2f), Rand.Range(0, 50), Rand.Range(.5f, 1f), 30, Rand.Range(0, 360));

                    if (skeleton != null)
                    {
                        TM_Action.SingleSpawnLoop(null, skeleton, curCell, map, 0, false, false, faction);
                    }
                }
            }
        }

        public void LearnAndShareBuildingThreats()
        {
            //Log.Message("sharing threats");
            List<Pawn> allPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
            for(int i =0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];
                if(p != this.Pawn && p.Faction == this.Pawn.Faction)
                {
                    if(p.def == TorannMagicDefOf.TM_SkeletonLichR)
                    {
                        CompSkeletonLichController comp = p.GetComp<CompSkeletonLichController>();
                        if (comp != null && comp.buildingThreats != null && comp.buildingThreats.Count > 0)
                        {
                            for (int j = 0; j < comp.buildingThreats.Count; j++)
                            {
                                this.buildingThreats.AddDistinct(comp.buildingThreats[j]);
                            }
                            for (int j = 0; j < this.buildingThreats.Count; j++)
                            {
                                comp.buildingThreats.AddDistinct(this.buildingThreats[j]);
                            }
                        }
                    }
                    else if(p.def == TorannMagicDefOf.TM_GiantSkeletonR)
                    {
                        CompSkeletonController comp = p.GetComp<CompSkeletonController>();
                        if (comp != null && comp.buildingThreats != null && comp.buildingThreats.Count > 0)
                        {
                            for (int j = 0; j < comp.buildingThreats.Count; j++)
                            {
                                this.buildingThreats.AddDistinct(comp.buildingThreats[j]);
                            }
                            for (int j = 0; j < this.buildingThreats.Count; j++)
                            {
                                comp.buildingThreats.AddDistinct(this.buildingThreats[j]);
                            }
                        }
                    }
                }
            }
            //Log.Message("ending threat share");
        }
    }
}