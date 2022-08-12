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
    public class CompSkeletonController : ThingComp
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

        private int rangedBurstShots = 0;
        private int rangedNextBurst = 0;
        private LocalTargetInfo rangedTarget = null;
        private Thing launchableThing = null;

        private int scanTick = 279;

        private int age = -1;

        //private int actionReady = 0;
        //private int actionTick = 0;

        //private LocalTargetInfo universalTarget = null;

        public override void PostDraw()
        {
            base.PostDraw();
            if (this.NextChargeAttack < Find.TickManager.TicksGame)
            {
                float matMagnitude = 2.5f;
                if (this.Pawn.def == TorannMagicDefOf.TM_GiantSkeletonR)
                {
                    Vector3 vector = ChainDrawPos;
                    vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                    Vector3 s = new Vector3(matMagnitude, 1, matMagnitude);
                    Matrix4x4 matrix = default(Matrix4x4);
                    float angle = 0;
                    if (this.Pawn.Rotation == Rot4.North || this.Pawn.Rotation == Rot4.South)
                    {
                        angle = Rand.Range(0, 360);
                        matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                        Graphics.DrawMesh(MeshPool.plane10, matrix, TM_MatPool.circleChain, 0);
                    }
                    else
                    {
                        angle = Rand.Range(40, 60);
                        if (this.Pawn.Rotation == Rot4.West)
                        {
                            angle *= -1;
                        }
                        matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                        Graphics.DrawMesh(MeshPool.plane10, matrix, TM_MatPool.lineChain, 0);
                    }
                }
            }
        }

        private Vector3 ChainDrawPos
        {
            get
            {
                Vector3 drawpos = this.Pawn.DrawPos;
                if(this.Pawn.Rotation == Rot4.North)
                {
                    drawpos.x += 1.05f;
                    drawpos.z += .49f;
                }
                else if(this.Pawn.Rotation == Rot4.East)
                {
                    drawpos.x -= .23f;
                    drawpos.z += 1.03f;
                }
                else if(this.Pawn.Rotation == Rot4.West)
                {
                    drawpos.x += .23f;
                    drawpos.z += 1.03f;
                }
                else
                {
                    drawpos.x -= 1.05f;
                    drawpos.z += .49f;
                }
                return drawpos;
            }
        }

        private Vector3 MoteDrawPos
        {
            get
            {
                Vector3 drawPos = this.Pawn.DrawPos;
                drawPos.z -= .9f;
                drawPos.x += Rand.Range(-.5f, .5f);
                return drawPos;
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
            if (this.rangedTarget != null && (this.rangedTarget.Cell - this.Pawn.Position).LengthHorizontal <= this.Props.maxRangeForFarThreat)
            {
                this.nextRangedAttack = (int)(this.Props.rangedCooldownTicks * Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
                this.launchableThing = null;
                this.launchableThing = FindNearbyObject(ThingCategoryDefOf.Corpses, 1.8f);
                if (this.launchableThing != null)
                {
                    this.rangedBurstShots = this.Props.rangedBurstCount;
                    this.rangedNextBurst = Find.TickManager.TicksGame + this.Props.rangedTicksBetweenBursts;
                    this.nextChargeAttack = Find.TickManager.TicksGame + 150;
                    TM_Action.PawnActionDelay(this.Pawn, 120, this.rangedTarget, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
                }
                else if (this.launchableThing == null && Rand.Chance(.1f))
                {
                    this.rangedBurstShots = this.Props.rangedBurstCount;
                    this.rangedNextBurst = Find.TickManager.TicksGame + this.Props.rangedTicksBetweenBursts;
                    this.nextChargeAttack = Find.TickManager.TicksGame + 150;
                    TM_Action.PawnActionDelay(this.Pawn, 120, this.rangedTarget, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
                }
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

                CellRect cellRect = CellRect.CenteredOn(target.Cell, 3);
                cellRect.ClipInsideMap(this.Pawn.Map);
                IntVec3 destination = cellRect.RandomCell;

                if (launchableThing != null && destination != null)
                {
                    float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.Pawn.Position, destination)).ToAngleFlat();
                    for (int m = 0; m < 4; m++)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, this.Pawn.Position.ToVector3Shifted(), this.Pawn.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(3f, 5f), launchAngle += Rand.Range(-25, 25), Rand.Range(0, 360));
                    }
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), this.Pawn.Position, this.Pawn.Map);
                    flyingObject.force = 1.4f;
                    flyingObject.Launch(this.Pawn, destination, this.launchableThing.SplitOff(1), Rand.Range(45, 65));
                }
                else if (launchableThing == null && destination != null)
                {
                    float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.Pawn.Position, destination)).ToAngleFlat();
                    for (int m = 0; m < 4; m++)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, this.Pawn.Position.ToVector3Shifted(), this.Pawn.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(3f, 5f), launchAngle += Rand.Range(-25, 25), Rand.Range(0, 360));
                    }
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_SpinningBone"), this.Pawn.Position, this.Pawn.Map);
                    flyingObject.force = 1.4f;
                    flyingObject.Launch(this.Pawn, destination, null, Rand.Range(120, 150));
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

        private void DoAoEAttack(IntVec3 center, bool isExplosion, float radius, DamageDef damageType, int damageAmount, ThingDef moteDef = null)
        {
            this.nextAoEAttack = (int)(this.Props.aoeCooldownTicks * Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
            List<IntVec3> targetCells = GenRadial.RadialCellsAround(center, radius, false).ToList();
            IntVec3 curCell = default(IntVec3);
            if (damageAmount > 0)
            {
                for (int i = 0; i < targetCells.Count(); i++)
                {
                    curCell = targetCells[i];
                    if (curCell.IsValid && curCell.InBoundsWithNullCheck(this.Pawn.Map))
                    {
                        if (isExplosion)
                        {
                            GenExplosion.DoExplosion(curCell, this.Pawn.Map, .4f, damageType, this.Pawn, damageAmount, Rand.Range(0, damageAmount), TorannMagicDefOf.TM_SoftExplosion, null, null, null, null, 0f, 1, false, null, 0f, 0, 0.0f, false);
                        }
                        else
                        {
                            List<Thing> thingList = curCell.GetThingList(this.Pawn.Map);
                            for (int j = 0; j < thingList.Count(); j++)
                            {
                                TM_Action.DamageEntities(thingList[j], null, damageAmount, damageType, this.Pawn);
                            }
                        }
                    }
                }
            }
            if(moteDef != null)
            {
                TM_MoteMaker.ThrowGenericMote(moteDef, this.Pawn.Position.ToVector3Shifted(), this.Pawn.Map, radius + 2, .25f, .25f, 1.75f, 0, 0, 0, Rand.Range(0, 360));
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
                    Vector3 launchVector = TM_Calc.GetVector(this.Pawn.Position, curCell);
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
            if(t == null)
            {
                t = this.rangedTarget;
            }
            this.nextChargeAttack = this.Props.chargeCooldownTicks + Find.TickManager.TicksGame;
            bool flag = t.Cell != default(IntVec3) && t.Cell.DistanceToEdge(this.Pawn.Map) > 6;
            float magnitude = (t.Cell - this.Pawn.Position).LengthHorizontal * .35f;
            if (flag && t.Thing != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 moteDirection = TM_Calc.GetVector(this.Pawn.Position, t.Cell);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GrappleHook, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(1.1f, 1.4f), 0.15f, .02f + (.08f * i), .3f - (.04f * i), Rand.Range(-10, 10), magnitude + magnitude * i, (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), Rand.Chance(.5f) ? (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat() : (Quaternion.AngleAxis(-90, Vector3.up) * moteDirection).ToAngleFlat());
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_GrappleHook, t.Thing.DrawPos, this.Pawn.Map, Rand.Range(1.1f, 1.4f), 0.15f, .02f + (.08f * i), .3f - (.04f * i), Rand.Range(-10, 10), magnitude + magnitude * i, (Quaternion.AngleAxis(-90, Vector3.up) * moteDirection).ToAngleFlat(), Rand.Chance(.5f) ? (Quaternion.AngleAxis(-90, Vector3.up) * moteDirection).ToAngleFlat() : (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat());
                }
                PullObject(t.Thing);
                TM_Action.PawnActionDelay(this.Pawn, 60, this.rangedTarget, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
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

        private void DoTaunt(Map map)
        {
            this.nextTaunt = (int)(this.Props.tauntCooldownTicks*Rand.Range(.9f, 1.1f)) + Find.TickManager.TicksGame;
            if (map != null)
            {
                List<Pawn> threatPawns = map.mapPawns.AllPawnsSpawned;
                bool anyPawnsTaunted = false;
                if (threatPawns != null && threatPawns.Count > 0)
                {
                    int count = Mathf.Min(threatPawns.Count, 10);
                    for (int i = 0; i < count; i++)
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
                        TM_Action.PawnActionDelay(this.Pawn, 30, this.rangedTarget, this.Pawn.meleeVerbs.TryGetMeleeVerb(this.rangedTarget.Thing));
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

        public CompProperties_SkeletonController Props
        {
            get
            {
                return (CompProperties_SkeletonController)this.props;
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
                    //HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_UndeadHD, .5f);
                    this.initialized = true;
                }

                if (this.Pawn.Spawned)
                {
                    if (!this.Pawn.Downed)
                    {
                        if (this.Pawn.Faction != null && this.Pawn.Faction.IsPlayer && this.NextTaunt < Find.TickManager.TicksGame && Rand.Chance(.2f))
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
                                    else if (this.NextChargeAttack < Find.TickManager.TicksGame && TargetIsValid(currentTargetThing) && TM_Calc.HasLoSFromTo(this.Pawn.Position, currentTargetThing, this.Pawn, 3, this.Props.maxRangeForCloseThreat * 3) && currentTargetThing is Pawn)
                                    {
                                        DoChargeAttack(currentTargetThing);
                                        goto exitTick;
                                    }
                                }
                            }

                            if (this.closeThreats.Count() > 1)
                            {
                                if (Rand.Chance(.2f) && this.NextAoEAttack < Find.TickManager.TicksGame)
                                {
                                    //DoAoEAttack(this.Pawn.Position, true, 1.4f, DamageDefOf.Stun, Rand.Range(2, 4), null);
                                    Find.CameraDriver.shaker.DoShake(4);
                                    DoAoEAttack(this.Pawn.Position, false, 1f, DamageDefOf.Stun, Rand.Range(2, 4), null);
                                    DoAoEAttack(this.Pawn.Position, false, 2f, DamageDefOf.Crush, Rand.Range(6, 12), TorannMagicDefOf.Mote_EarthCrack);
                                    DoAoEAttack(this.Pawn.Position, false, 2.5f, DamageDefOf.Crush, 0, TorannMagicDefOf.Mote_EarthCrack);
                                }

                                if (Rand.Chance(.1f) && this.farThreats.Count() > (5 * this.closeThreats.Count()))
                                {
                                    this.Pawn.CurJob.targetA = this.farThreats.RandomElement();
                                }
                            }

                            //    if (this.closeThreats.Count() > 1 && ((this.closeThreats.Count() * 2) > this.farThreats.Count() || Rand.Chance(.3f)))
                            //    {
                            //        if (Rand.Chance(.8f) && this.NextKnockbackAttack < Find.TickManager.TicksGame)
                            //        {
                            //            Pawn randomClosePawn = this.closeThreats.RandomElement();
                            //            if ((randomClosePawn.Position - this.Pawn.Position).LengthHorizontal < 3 && TargetIsValid(randomClosePawn))
                            //            {
                            //                DoKnockbackAttack(this.Pawn.Position, randomClosePawn.Position, 1.4f, Rand.Range(3, 5f));
                            //            }
                            //        }
                            //    }

                            if (this.farThreats.Count() > 2 * this.closeThreats.Count() && Rand.Chance(.3f))
                            {
                                Pawn randomRangedPawn = this.farThreats.RandomElement();
                                if (this.NextChargeAttack < Find.TickManager.TicksGame)
                                {
                                    Thing tempTarget = this.farThreats.RandomElement();
                                    if (TargetIsValid(tempTarget) && TM_Calc.HasLoSFromTo(this.Pawn.Position, tempTarget, this.Pawn, this.Props.maxRangeForCloseThreat, this.Props.maxRangeForCloseThreat * 4))
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
                                    if ((randomRangedPawn.Position - this.Pawn.Position).LengthHorizontal < this.Props.maxRangeForFarThreat * 2f)
                                    {
                                        this.rangedTarget = randomRangedPawn;
                                        StartRangedAttack();
                                    }
                                }
                            }

                            if (this.Pawn.CurJob != null)
                            {
                                if (this.Pawn.CurJob.targetA == null  || this.Pawn.CurJob.targetA == this.Pawn)
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

                        if (Find.TickManager.TicksGame >= this.scanTick)
                        {
                            this.scanTick = Rand.Range(250, 320) + Find.TickManager.TicksGame;
                            DetermineThreats();
                        }
                    }

                    if (this.Pawn.Downed)
                    {
                        this.Pawn.Kill(null);
                    }

                    if (this.Pawn.def == TorannMagicDefOf.TM_GiantSkeletonR && Find.TickManager.TicksGame % 9 == 0)
                    {
                        ThingDef rndMote = TorannMagicDefOf.Mote_BoneDust;
                        TM_MoteMaker.ThrowGenericMote(rndMote, MoteDrawPos, this.Pawn.Map, .6f, .1f, 0f, .6f, Rand.Range(-200, 200), 0f, 0, Rand.Range(0, 360));
                        TM_MoteMaker.ThrowGenericMote(rndMote, MoteDrawPos, this.Pawn.Map, .6f, .1f, 0f, .6f, Rand.Range(-200, 200), 0f, 0, Rand.Range(0, 360));
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
                if (instigatorThing.Faction != null && instigatorThing.Faction != this.Pawn.Faction)
                {
                    this.buildingThreats.AddDistinct(instigatorThing);
                }                
            }
        }

        private void DetermineThreats()
        {
            this.closeThreats.Clear();
            this.farThreats.Clear();
            List<Pawn> allPawns = this.Pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                if (!allPawns[i].DestroyedOrNull() && allPawns[i] != this.Pawn)
                {
                    if (!allPawns[i].Dead && !allPawns[i].Downed)
                    {
                        if (allPawns[i].Faction != null && (allPawns[i].Faction.HostileTo(this.Pawn.Faction)) && !allPawns[i].IsPrisoner)
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
                        if (allPawns[i].Faction == null && allPawns[i].InMentalState)
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
                    if (randomMapPawn.Faction != null && randomMapPawn.Faction.HostileTo(this.Pawn.Faction))
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
            if(target.Faction != null)
            {
                return target.Faction != this.Pawn.Faction && target.Faction.HostileTo(this.Pawn.Faction);
            }
            return true;
        }

        public Thing FindNearbyObject(ThingCategoryDef tcd, float radius)
        {
            List<IntVec3> searchCells = GenRadial.RadialCellsAround(this.Pawn.Position, radius, true).ToList();
            List<Thing> returnThings = new List<Thing>();
            returnThings.Clear();
            for (int i = 0; i < searchCells.Count(); i++)
            {
                if (searchCells[i].IsValid && searchCells[i].InBoundsWithNullCheck(this.Pawn.Map))
                {
                    List<Thing> cellList = searchCells[i].GetThingList(this.Pawn.Map);
                    for (int j = 0; j<cellList.Count(); j++)
                    {
                        try
                        {
                            if (cellList[j].def.thingCategories != null)
                            {
                                if (cellList[j].def.thingCategories.Contains(ThingCategoryDefOf.StoneChunks) || cellList[j].def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks) || cellList[j].def.thingCategories.Contains(tcd))
                                {
                                    returnThings.Add(cellList[j]);
                                }
                                if(cellList[j] is Corpse)
                                {
                                    returnThings.Add(cellList[j]);
                                }
                            }
                            if(cellList[j].def == TorannMagicDefOf.TM_SkeletonR && TM_Calc.HasLoSFromTo(this.Pawn.Position, this.rangedTarget, this.Pawn, 4, this.Props.maxRangeForFarThreat))
                            {
                                returnThings.Add(cellList[j]);
                            }
                        }
                        catch (NullReferenceException ex)
                        {
                            //Log.Message("threw exception " + ex);
                        }
                    }
                }
            }
            if (returnThings != null && returnThings.Count > 0)
            {
                return returnThings.RandomElement();
            }
            return null;
        }

        public void PullObject(Thing t)
        {
            Thing summonableThing = t;
            Pawn victim = null;
            if (summonableThing != null)
            {
                victim = t as Pawn;
                if (victim != null)
                {
                    DamageInfo dinfo2 = new DamageInfo(DamageDefOf.Stun, 10, 10, -1, this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown, victim);
                    if (!victim.RaceProps.Humanlike || victim.Faction == this.Pawn.Faction)
                    {
                        FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), victim.Position, this.Pawn.Map, WipeMode.Vanish);
                        flyingObject.speed = 25;
                        flyingObject.Launch(victim, this.Pawn.Position, victim);
                    }
                    else if (victim.RaceProps.Humanlike && victim.Faction != this.Pawn.Faction && Rand.Chance(TM_Calc.GetSpellSuccessChance(this.Pawn, victim, true)))
                    {
                        FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), victim.Position, this.Pawn.Map, WipeMode.Vanish);
                        flyingObject.speed = Rand.Range(23f, 27f);
                        flyingObject.Launch(victim, this.Pawn.Position, victim);
                    }
                    else
                    {
                        MoteMaker.ThrowText(victim.DrawPos, victim.Map, "TM_ResistedSpell".Translate(), -1);
                    }
                }
            }
        }
    }
}