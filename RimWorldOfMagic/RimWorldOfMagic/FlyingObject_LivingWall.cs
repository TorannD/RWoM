using RimWorld;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_LivingWall : Projectile
    {
        protected new Vector3 origin;        
        protected new Vector3 destination;
        protected Vector3 trueOrigin;
        protected Vector3 trueDestination;

        public float speed = 30f;
        protected new int ticksToImpact;
        protected Thing assignedTarget;
        protected Thing flyingThing;

        public ThingDef moteDef = null;
        public int moteFrequency = 0;

        public bool spinning = false;
        public float curveVariance = 0; // 0 = no curve
        private List<Vector3> curvePoints = new List<Vector3>();
        public float force = 1f;
        private int destinationCurvePoint = 0;
        private float impactRadius = 0;
        private int explosionDamage;
        private bool isExplosive = false;
        private DamageDef impactDamageType = null;
        private bool fliesOverhead = false;

        private bool earlyImpact = false;
        private float impactForce = 0;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;
        public bool explosion = false;
        public int weaponDmg = 0;
        private int doublesidedVariance = 0;

        private int searchEnemySpeed = 200;
        private float searchEnemyRange = 3f;
        private float enemyDamage = 12;

        private int idleFor = 0;
        private Thing targetWall;
        private bool shouldDestroy = false;

        Pawn pawn;
        public Pawn CasterPawn
        {
            get
            {
                if(!pawn.DestroyedOrNull() && !pawn.Dead)
                {
                    return pawn;
                }
                else
                {
                    shouldDestroy = true;
                }
                return null;
            }
        }

        //Magic related
        CompAbilityUserMagic comp;
        TMPawnSummoned newPawn = new TMPawnSummoned();

        public Building OccupiedWall
        {
            get
            {
                List<Thing> tmpList = this.Position.GetThingList(this.Map);
                foreach(Thing t in tmpList)
                {
                    if(TM_Calc.IsWall(t))
                    {
                        return t as Building;
                    }
                }
                return null;
            }
        }

        public Building DestinationWall
        {
            get
            {
                Building fromWall = null;
                if (this.curvePoints.Count > 0)
                {
                    List<Thing> destList = this.curvePoints[this.curvePoints.Count - 1].ToIntVec3().GetThingList(this.Map);

                    foreach (Thing w in destList)
                    {
                        if (TM_Calc.IsWall(w))
                        {
                            fromWall = w as Building;
                        }
                    }
                }
                if (fromWall == null)
                {
                    fromWall = this.OccupiedWall;
                }
                return fromWall;
            }
        }

        protected new int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((this.origin - this.destination).magnitude / (this.speed / 100f));
                bool flag = num < 1;
                if (flag)
                {
                    num = 1;
                }
                return num;
            }
        }

        protected new IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(this.destination);
            }
        }

        public new Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * this.def.Altitude;
            }
        }

        public new Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return this.ExactPosition;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_Values.Look<float>(ref this.curveVariance, "curveVariance", 1f, false);
            Scribe_Values.Look<float>(ref this.speed, "speed", 15f, false);
            Scribe_Collections.Look<Building>(ref this.connectedWalls, "connectedWalls", LookMode.Reference);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            flyingThing.ThingID += Rand.Range(0, 214).ToString();
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void ExactLaunch(ThingDef effectMote, int moteFrequencyTicks, bool shouldSpin, List<Vector3> travelPath, Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, int flyingSpeed, float _impactRadius)
        {
            this.moteFrequency = moteFrequencyTicks;
            this.moteDef = effectMote;
            this.impactRadius = _impactRadius;
            this.spinning = shouldSpin;
            this.speed = flyingSpeed;
            this.curvePoints = travelPath;
            this.curveVariance = 1;
            this.Launch(launcher, origin, targ, flyingThing, null);
        }

        public void AdvancedLaunch(Thing launcher, ThingDef effectMote, int moteFrequencyTicks, float curveAmount, bool shouldSpin, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, int flyingSpeed, bool isExplosion, int _impactDamage, float _impactRadius, DamageDef damageType, DamageInfo? newDamageInfo = null, int doubleVariance = 0, bool flyOverhead = false)
        {
            this.fliesOverhead = flyOverhead;
            this.explosionDamage = _impactDamage;
            this.isExplosive = isExplosion;
            this.impactRadius = _impactRadius;
            this.impactDamageType = damageType;
            this.moteFrequency = moteFrequencyTicks;
            this.moteDef = effectMote;
            this.curveVariance = curveAmount;
            this.spinning = shouldSpin;
            this.speed = flyingSpeed;
            this.doublesidedVariance = doubleVariance;
            this.curvePoints = new List<Vector3>();
            this.curvePoints.Clear();
            this.Launch(launcher, origin, targ, flyingThing, newDamageInfo);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            bool spawned = flyingThing.Spawned;            
            this.pawn = launcher as Pawn;
            if (spawned)
            {               
                flyingThing.DeSpawn();
            }
            this.launcher = launcher;
            this.trueOrigin = origin;
            this.trueDestination = targ.Cell.ToVector3();
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.speed = this.speed * this.force;
            this.origin = origin;
            if(this.curveVariance > 0)
            {
                CalculateCurvePoints(this.trueOrigin, this.trueDestination, this.curveVariance);
                this.destinationCurvePoint++;
                this.destination = this.curvePoints[this.destinationCurvePoint];
            }
            else
            {
                this.destination = this.trueDestination;
            }            
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }        

        public void CalculateCurvePoints(Vector3 start, Vector3 end, float variance)
        {
            int variancePoints = 20;
            Vector3 initialVector = GetVector(start, end);
            initialVector.y = 0;
            float initialAngle = (initialVector).ToAngleFlat(); 
            float curveAngle = variance;
            if(doublesidedVariance == 0 && Rand.Chance(.5f))
            { 
                curveAngle = (-1) * variance;
            }
            else
            {
                curveAngle = (doublesidedVariance * variance);
            }

            //calculate extra distance bolt travels around the ellipse
            float a = .5f * Vector3.Distance(start, end);
            float b = a * Mathf.Sin(.5f * Mathf.Deg2Rad * variance);
            float p = .5f * Mathf.PI * (3 * (a + b) - (Mathf.Sqrt((3 * a + b) * (a + 3 * b))));
                    
            float incrementalDistance = p / variancePoints; 
            float incrementalAngle = (curveAngle / variancePoints) * 2f;
            this.curvePoints.Add(this.trueOrigin);
            for(int i = 1; i <= (variancePoints + 1); i++)
            {
                this.curvePoints.Add(this.curvePoints[i - 1] + ((Quaternion.AngleAxis(curveAngle, Vector3.up) * initialVector) * incrementalDistance));
                curveAngle -= incrementalAngle;
            }
        }

        public Vector3 GetVector(Vector3 center, Vector3 objectPos)
        {
            Vector3 heading = (objectPos - center);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }

        public static Pawn closestThreat = null;
        public void FindClosestThreat()
        {            
            closestThreat = null;
            float closest = 999f;
            foreach(Pawn p in this.Map.mapPawns.AllPawnsSpawned)
            {
                float pDistance = (p.Position - this.Position).LengthHorizontal;
                if (!p.Dead && !p.Downed && CasterPawn != null && p.HostileTo(CasterPawn) &&  pDistance < closest)
                {
                    closest = pDistance;
                    closestThreat = p;
                }
            }
        }

        private List<Building> connectedWalls = new List<Building>();        
        public void FindClosestWallToTarget()
        {            
            if(connectedWalls == null)
            {
                connectedWalls = new List<Building>();
            }
            if(closestThreat != null && connectedWalls != null && connectedWalls.Count > 0)
            {
                float closest = (this.Position - closestThreat.Position).LengthHorizontal;
                Thing closestWall = null;
                foreach(Thing t in connectedWalls)
                {
                    float distance = (t.Position - closestThreat.Position).LengthHorizontal;
                    if (distance <= closest)
                    {
                        closest = distance;
                        closestWall = t;
                    }
                }
                this.targetWall = closestWall;
            }
            
        }

        public Thing FindClosestWallFromTarget()
        {
            Thing tmp = null;
            float closest = 999;
            IEnumerable<Building> allThings = from def in this.Map.listerThings.AllThings
                                              where (def is Building && TM_Calc.IsWall(def))
                                              select def as Building;
            foreach(Building b in allThings)
            {
                float dist = (b.Position - closestThreat.Position).LengthHorizontal;
                if (dist < closest)
                {
                    closest = dist;
                    tmp = b;
                }
            }
            return tmp;
        }

        public bool MoveToClosestWall()
        {
            if (curvePoints.Count > 1 && destinationCurvePoint > 0)
            {
                this.origin = curvePoints[destinationCurvePoint];
                this.destination = curvePoints[destinationCurvePoint - 1];
                this.ticksToImpact = this.StartingTicksToImpact;
                this.curvePoints.Clear();
                updatedPath.Clear();
                targetWall = null;
                return false;
            }
            else
            {
                if (OccupiedWall == null && CasterPawn != null)
                {
                    Building nearestWall = TM_Calc.FindNearestWall(this.Map, this.ExactPosition.ToIntVec3(), CasterPawn.Faction);
                    if(nearestWall != null)
                    {
                        this.Position = nearestWall.Position;
                        this.origin = nearestWall.DrawPos;
                        this.destination = nearestWall.DrawPos;
                        this.ticksToImpact = this.StartingTicksToImpact;
                        this.curvePoints.Clear();
                        updatedPath.Clear();
                        targetWall = null;
                        return false;
                    }
                }
            }
            //Log.Message("no nearby wall found - destroying");
            return true;
        }

        List<Vector3> updatedPath = new List<Vector3>();
        public void CreatePath()
        {
            List<IntVec3> tmpList = new List<IntVec3>();
            if (DestinationWall != null)
            {
                tmpList = TM_Calc.FindTPath(DestinationWall, targetWall, OccupiedWall.Faction);
            }
            updatedPath = TM_Calc.IntVec3List_To_Vector3List(tmpList);            
            targetWall = null;
        }

        private int nextWallUpdate = 0;
        private int nextWallHealthUpdate = 0;
        private int nextThreatUpdate = 0;
        private int nextWallSelectUpdate = 0;
        private bool pathLocked = false;
        private bool canAddNewPath = false;

        public void DoPathUpdate()
        {            
            CreatePath();
            pathLocked = false;
        }

        //never gets used, just repairs occupied wall
        private List<Building> damagedWallList = new List<Building>();
        public void DoWallHealthUpdate()
        {
            List<Building> rmvList = new List<Building>();
            rmvList.Clear();
            if(damagedWallList == null)
            {
                damagedWallList = new List<Building>();
                damagedWallList.Clear();
            }
            foreach(Building b in connectedWalls)
            {
                if(b.DestroyedOrNull())
                {
                    rmvList.Add(b);
                }
                else if(b.HitPoints < b.MaxHitPoints)
                {
                    damagedWallList.Add(b);
                }
            }
            foreach(Building b in rmvList)
            {
                connectedWalls.Remove(b);
            }
        }

        private bool wallUpdateLock = false;
        public void DoConnectedWallUpdate()
        {
            //incrementally finds connected walls and queues them for next move;
            //this should occur while the nurikabe moves, to appear seamless
            connectedWalls = TM_Calc.FindConnectedWalls(DestinationWall, 1.4f, 20f, true);
        }

        public void DoThreadedActions()
        {
            if (Find.TickManager.TicksGame > this.nextWallUpdate && OccupiedWall != null)
            {
                this.nextWallUpdate = Find.TickManager.TicksGame + Rand.Range(10, 20);
                DoConnectedWallUpdate();
            }

            if (Find.TickManager.TicksGame > this.nextThreatUpdate)
            {
                if (closestThreat != null)
                {
                    nextThreatUpdate = Find.TickManager.TicksGame + Rand.Range(4, 6);
                }
                else
                {
                    nextThreatUpdate = Find.TickManager.TicksGame + Rand.Range(120, 300);
                }
                FindClosestThreat();
            }
            if (Find.TickManager.TicksGame >= this.nextWallSelectUpdate && closestThreat != null)
            {
                nextWallSelectUpdate = Find.TickManager.TicksGame + Rand.Range(10, 20);
                FindClosestWallToTarget();
            }
            if (targetWall != null && targetWall.Position != this.Position && !pathLocked && updatedPath.Count <= 0)
            {
                pathLocked = true;
                DoPathUpdate();
            }
            threadLocked = false;
        }

        private IntVec3 lastGoodPosition = default(IntVec3);
        public void DoDirectActions()
        {
            if (OccupiedWall == null)
            {
                shouldDestroy = MoveToClosestWall();
            }
        }

        public void ChangePath()
        {            
            this.curvePoints.Clear();
            //Log.Message("adding new path");
            foreach (Vector3 v in this.updatedPath)
            {
                this.curvePoints.Add(v);
            }
            updatedPath.Clear();
            this.destinationCurvePoint = 0;
            pathLocked = false;            
        }

        public void UpdateStatus()
        {
            if (!CasterPawn.DestroyedOrNull() && !CasterPawn.Dead)
            {
                CompAbilityUserMagic comp = CasterPawn.TryGetComp<CompAbilityUserMagic>();
                //int verVal = TM_Calc.GetMagicSkillLevel(CasterPawn, comp.MagicData.MagicPowerSkill_LivingWall, "TM_LivingWall", "_ver", true);
                //int pwrVal = TM_Calc.GetMagicSkillLevel(CasterPawn, comp.MagicData.MagicPowerSkill_LivingWall, "TM_LivingWall", "_pwr", true);
                int verVal = TM_Calc.GetSkillVersatilityLevel(CasterPawn, TorannMagicDefOf.TM_LivingWall, true);
                int pwrVal = TM_Calc.GetSkillPowerLevel(CasterPawn, TorannMagicDefOf.TM_LivingWall, true);
                this.speed = 15 + (3 * verVal);
                this.searchEnemySpeed = 200 - (20 * verVal);
                this.enemyDamage = 12 + pwrVal;
                this.searchEnemyRange = 2.5f + (.2f * pwrVal);
            }
            else
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private bool threadLocked = false;
        private int threadLockTick = 0;
        private int searchEnemyTick = 0;
        static List<Thread> activeThreads = new List<Thread>();
        public override void Tick()
        {
            Vector3 exactPosition = this.ExactPosition;
            if (shouldDestroy)
            {
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                if (this.ticksToImpact >= 0 && this.moteDef != null && Find.TickManager.TicksGame % this.moteFrequency == 0)
                {
                    DrawEffects(exactPosition);
                }
                if (Find.TickManager.TicksGame > this.searchEnemyTick)
                {
                    this.searchEnemyTick = Mathf.RoundToInt(Rand.Range(.4f, .6f) * this.searchEnemySpeed) + Find.TickManager.TicksGame;
                    AttackNearby();
                    RepairOccupiedWall();
                    if(this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Map != this.Map)
                    {
                        shouldDestroy = true;
                    }
                }
                if (idleFor > 0)
                {
                    idleFor--;
                    if (closestThreat != null && !threadLocked)
                    {
                        threadLocked = true;
                        threadLockTick = Find.TickManager.TicksGame + 100;
                        Thread tryDirectPath = new Thread(DirectPath);
                        tryDirectPath.Start();
                    }
                }
                else
                {
                    this.ticksToImpact--;
                    //updates list of connected wall segments
                    if (!threadLocked)
                    {
                        threadLockTick = Find.TickManager.TicksGame + 100;
                        threadLocked = true;
                        Thread threadedActions = new Thread(DoThreadedActions);
                        activeThreads.Add(threadedActions);
                        threadedActions.Start();
                    }
                    if (Find.TickManager.TicksGame > threadLockTick)
                    {
                        for (int i = 0; i < activeThreads.Count; i++)
                        {
                            activeThreads[i].Abort();
                        }
                        activeThreads.Clear();
                        threadLocked = false;
                    }
                    DoDirectActions();
                    if (!shouldDestroy)
                    {
                        bool flag = !this.ExactPosition.InBounds(base.Map);
                        if (flag)
                        {
                            this.ticksToImpact++;
                            base.Position = this.ExactPosition.ToIntVec3();
                            this.Destroy(DestroyMode.Vanish);
                        }
                        else
                        {
                            base.Position = this.ExactPosition.ToIntVec3();
                            if (Find.TickManager.TicksGame % 3 == 0 && this.destination.ToIntVec3() != this.Position)
                            {
                                FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.4f, .6f));
                                float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.origin, this.destination)).ToAngleFlat();
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirtOverhead, this.DrawPos, this.Map, 1.2f, .05f, .15f, .38f, 0, 3f, angle, angle);
                            }

                            bool flag2 = this.ticksToImpact <= 0;
                            if (flag2)
                            {
                                if (this.curveVariance > 0)
                                {
                                    if ((this.curvePoints.Count() - 1) > this.destinationCurvePoint)
                                    {
                                        this.origin = curvePoints[destinationCurvePoint];
                                        this.destinationCurvePoint++;
                                        this.destination = this.curvePoints[this.destinationCurvePoint];
                                        this.ticksToImpact = this.StartingTicksToImpact;
                                        canAddNewPath = false;
                                    }
                                    else
                                    {
                                        bool flag3 = this.DestinationCell.InBounds(base.Map);
                                        if (flag3)
                                        {
                                            base.Position = this.DestinationCell;
                                        }
                                        canAddNewPath = true;
                                        this.IdleFlight();
                                    }
                                }
                                else
                                {
                                    bool flag3 = this.DestinationCell.InBounds(base.Map);
                                    if (flag3)
                                    {
                                        base.Position = this.DestinationCell;
                                    }
                                    this.ImpactSomething();
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual void IdleFlight()
        {
            if (updatedPath != null && updatedPath.Count > 0 && !pathLocked)
            {
                this.idleFor = 5;
                pathLocked = true;
                ChangePath();
            }
            else
            {
                this.idleFor = 60;
                this.origin = this.ExactPosition;
                this.destination = this.ExactPosition;
                this.ticksToImpact = this.StartingTicksToImpact;
            }
        }

        public void DirectPath()
        {
            Thing cwftWall = FindClosestWallFromTarget();
            if (cwftWall != null && (cwftWall.Position - closestThreat.Position).LengthHorizontal < (this.Position - closestThreat.Position).LengthHorizontal)
            {
                List<IntVec3> tmpList = TM_Calc.FindTPath(OccupiedWall, cwftWall, OccupiedWall.Faction);
                if (tmpList.Count > 0)
                {
                    updatedPath = TM_Calc.IntVec3List_To_Vector3List(tmpList);
                    ChangePath();
                }
            }
            
            targetWall = null;
            idleFor = 0;
            threadLocked = false;
        }

        public void AttackNearby()
        {
            List<Pawn> hitList = new List<Pawn>();
            hitList.Clear();
            List<Pawn> atkPawns = this.Map.mapPawns.AllPawnsSpawned.Where((Pawn x) => (x.Position - this.Position).LengthHorizontal <= this.searchEnemyRange).ToList();
            foreach(Pawn p in atkPawns)
            {
                if(p.HostileTo(CasterPawn) && !p.Dead && !p.Downed)
                {                    
                    float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.DrawPos, p.DrawPos)).ToAngleFlat();
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_WallSpike, TM_Calc.GetVectorBetween(this.DrawPos, p.DrawPos), this.Map, Rand.Range(1f, 1.2f), Rand.Range(.3f, .4f), 0f, Rand.Range(.1f, .2f), 0, Rand.Range(.1f, .3f), angle, angle); 
                    for(int i = 0; i < 5; i++)
                    {
                        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.DustPuff, this.DrawPos, this.Map, Rand.Range(.8f, 1.2f), .3f, .1f, .2f, Rand.Range(-200, 200), Rand.Range(1f, 3f), angle + Rand.Range(-15, 15), Rand.Range(0, 360));
                    }
                    FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.4f, .6f));
                    TM_Action.DamageEntities(p, null, enemyDamage, 0f, DamageDefOf.Stab, CasterPawn);
                }
            }
        }

        public void RepairOccupiedWall()
        {
            if(this.OccupiedWall != null && this.OccupiedWall.HitPoints < this.OccupiedWall.MaxHitPoints)
            {
                this.OccupiedWall.HitPoints = Mathf.Clamp(this.OccupiedWall.HitPoints + 10, 0, OccupiedWall.MaxHitPoints);
                FleckMaker.ThrowDustPuff(this.OccupiedWall.Position, this.Map, Rand.Range(.6f, 1f));
            }
        }

        public override void Draw()
        {
            bool flag = this.flyingThing != null;
            if (flag)
            {
                bool flag2 = this.flyingThing is Pawn;
                if (flag2)
                {
                    Vector3 arg_2B_0 = this.DrawPos;
                    bool flag4 = !this.DrawPos.ToIntVec3().IsValid;
                    if (flag4)
                    {
                        return;
                    }
                    Pawn pawn = this.flyingThing as Pawn;
                    pawn.Drawer.DrawAt(this.DrawPos);                      
                }
                else
                {
                    Vector3 drawP = this.DrawPos;
                    drawP.y = 8.1f;
                    Graphics.DrawMesh(MeshPool.plane10, drawP, Quaternion.identity, this.flyingThing.def.DrawMatSingle, 0);
                }
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, Quaternion.identity, this.flyingThing.def.DrawMatSingle, 0);
            }
            base.Comps_PostDraw();
        }

        private void DrawEffects(Vector3 effectVec)
        {
            effectVec.x += Rand.Range(-0.4f, 0.4f);
            effectVec.z += Rand.Range(-0.4f, 0.4f);
            TM_MoteMaker.ThrowGenericMote(this.moteDef, effectVec, this.Map, Rand.Range(.4f, .6f), Rand.Range(.05f, .1f), .03f, Rand.Range(.2f, .3f), Rand.Range(-200, 200), Rand.Range(.5f, 2f), Rand.Range(0, 360), Rand.Range(0, 360));
        }

        private void ImpactSomething()
        {
            bool flag = this.assignedTarget != null;
            if (flag)
            {
                Pawn pawn = this.assignedTarget as Pawn;
                bool flag2 = pawn != null && pawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f;
                if (flag2)
                {
                    this.Impact(null);
                }
                else
                {
                    this.Impact(this.assignedTarget);
                }
            }
            else
            {
                this.Impact(null);
            }
        }

        protected new void Impact(Thing hitThing)
        {           
            this.Destroy(DestroyMode.Vanish);            
        }
    }
}
