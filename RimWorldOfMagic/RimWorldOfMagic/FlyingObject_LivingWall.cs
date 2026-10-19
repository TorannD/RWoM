using RimWorld;
using System;
using TorannMagic.Utils;
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

        public Building OccupiedWall => FindWall(Position, Map);

        public Building DestinationWall
        {
            get
            {
                var last = curvePoints != null && curvePoints.Count > 0
                    ? FindWall(curvePoints[curvePoints.Count - 1].ToIntVec3(), Map) : null;
                return last ?? OccupiedWall;
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
                Vector3 b = (this.destination - this.origin) * Mathf.Clamp01(1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
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
            Scribe_Collections.Look(ref curvePoints, "curvePoints", LookMode.Value);
            Scribe_Values.Look(ref destinationCurvePoint, "destinationCurvePoint", 0);
            Scribe_Values.Look(ref idleFor, "idleFor", 0);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                RestoreRouteAfterLoad();
        }

        private void RestoreRouteAfterLoad()
        {
            // Old saves did not store curve points. Continue their current segment.
            if (curvePoints == null || curvePoints.Count == 0 || curvePoints.Count > MaxSearchCells ||
                destinationCurvePoint < 0 || destinationCurvePoint >= curvePoints.Count)
            {
                curvePoints = new List<Vector3> { origin, destination };
                destinationCurvePoint = 1;
            }
            updatedPath = new List<Vector3>();
            CancelPlanning();
            closestThreat = null;
            nextThreatUpdate = 0;
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

        private Pawn closestThreat;
        private List<Vector3> updatedPath = new List<Vector3>();
        private LivingWallSearch wallSearch;
        private Map searchMap;
        private Building searchStart;
        private Pawn searchThreat;
        private Faction searchFaction;
        private Map plannedMap;
        private Faction plannedFaction;
        private int nextPathSearchTick;
        private int nextThreatUpdate;
        private const int SearchBudgetPerTick = 64;
        private const int MaxSearchCells = 4096;
        private const int PathRetryTicks = 15;

        private static Building FindWall(IntVec3 cell, Map map, Faction faction = null)
        {
            if (map == null || !cell.InBounds(map)) return null;
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building wall && !wall.Destroyed && wall.Spawned &&
                    (faction == null || wall.Faction == faction) && TM_Calc.IsWall(wall))
                    return wall;
            }
            return null;
        }

        private bool IsValidThreat(Pawn target)
        {
            return !target.DestroyedOrNull() && target.Spawned && target.Map == Map &&
                !target.Dead && !target.Downed && target.HostileTo(pawn);
        }

        public void FindClosestThreat()
        {
            var previous = closestThreat;
            closestThreat = null;
            float closest = 999f * 999f;
            var candidates = Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                float distance = (candidate.Position - Position).LengthHorizontalSquared;
                if (distance < closest && IsValidThreat(candidate))
                {
                    closest = distance;
                    closestThreat = candidate;
                }
            }
            if (previous != closestThreat)
                CancelPlanning();
        }

        // Called only from TickInterval: all world reads, RNG and path publication
        // now happen on the game thread, with a bounded search slice each update.
        private void UpdateWallActions(int currentTick)
        {
            if (closestThreat != null && !IsValidThreat(closestThreat))
            {
                closestThreat = null;
                CancelPlanning();
                nextThreatUpdate = 0;
            }
            if (currentTick >= nextThreatUpdate)
            {
                FindClosestThreat();
                nextThreatUpdate = currentTick + (closestThreat == null ? Rand.Range(120, 300) : Rand.Range(4, 6));
            }
            if (closestThreat == null) return;

            if (wallSearch != null && (searchMap != Map || searchThreat != closestThreat ||
                searchStart.DestroyedOrNull() || !searchStart.Spawned || searchStart.Map != Map ||
                searchStart.Faction != searchFaction || searchFaction != pawn.Faction))
                CancelPlanning();

            if (wallSearch == null && updatedPath.Count == 0 && currentTick >= nextPathSearchTick)
            {
                searchStart = DestinationWall;
                nextPathSearchTick = currentTick + PathRetryTicks;
                if (searchStart == null || searchStart.Faction != pawn.Faction)
                {
                    ClearSearch();
                    return;
                }
                searchMap = Map;
                searchThreat = closestThreat;
                searchFaction = searchStart.Faction;
                // Capture this search's context, not mutable state from a later search.
                var map = searchMap;
                var faction = searchFaction;
                wallSearch = new LivingWallSearch(searchStart.Position,
                    cell => FindWall(cell, map, faction) != null, MaxSearchCells);
            }
            if (wallSearch == null) return;
            wallSearch.Step(SearchBudgetPerTick);
            if (!wallSearch.Completed) return;

            var start = wallSearch.Start;
            var best = start;
            float closest = (start - closestThreat.Position).LengthHorizontalSquared;
            foreach (var cell in wallSearch.Cells)
            {
                float distance = (cell - closestThreat.Position).LengthHorizontalSquared;
                if (distance < closest && FindWall(cell, searchMap, searchFaction) != null)
                {
                    best = cell;
                    closest = distance;
                }
            }
            var path = wallSearch.PathTo(best);
            if (path.Count > 1)
            {
                plannedMap = searchMap;
                plannedFaction = searchFaction;
                updatedPath = TM_Calc.IntVec3List_To_Vector3List(path);
            }
            ClearSearch();
            nextPathSearchTick = currentTick + PathRetryTicks;
        }

        private void ClearSearch()
        {
            wallSearch = null;
            searchMap = null;
            searchStart = null;
            searchThreat = null;
            searchFaction = null;
        }

        private void CancelPlanning()
        {
            ClearSearch();
            updatedPath?.Clear();
            plannedMap = null;
            plannedFaction = null;
            nextPathSearchTick = 0;
        }

        private bool TryUseQueuedPath()
        {
            if (updatedPath == null || updatedPath.Count < 2) return false;
            bool valid = plannedMap == Map && updatedPath[0].ToIntVec3() == Position &&
                (pawn == null || plannedFaction == pawn.Faction);
            for (int i = 0; valid && i < updatedPath.Count; i++)
            {
                var cell = updatedPath[i].ToIntVec3();
                valid = FindWall(cell, Map, plannedFaction) != null;
                if (i > 0)
                {
                    var previous = updatedPath[i - 1].ToIntVec3();
                    valid &= Math.Abs(cell.x - previous.x) + Math.Abs(cell.z - previous.z) == 1;
                }
            }
            if (!valid)
            {
                CancelPlanning();
                return false;
            }
            ChangePath();
            return true;
        }

        public void ChangePath()
        {
            if (updatedPath == null || updatedPath.Count < 2) return;
            curvePoints = updatedPath;
            updatedPath = new List<Vector3>();
            destinationCurvePoint = 1;
            origin = curvePoints[0];
            destination = curvePoints[1];
            ticksToImpact = StartingTicksToImpact;
            idleFor = 0;
        }

        // Recover only onto a surviving adjacent segment of this route. There is
        // no map-wide teleport or worker thread left running after a wall is lost.
        public bool MoveToClosestWall()
        {
            if (curvePoints != null && destinationCurvePoint > 0 && destinationCurvePoint < curvePoints.Count)
            {
                var previous = curvePoints[destinationCurvePoint - 1].ToIntVec3();
                if (FindWall(previous, Map, pawn.Faction) != null)
                {
                    Position = previous;
                    CancelPlanning();
                    IdleFlight();
                    return false;
                }
            }
            return true;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            CancelPlanning();
            closestThreat = null;
            if (curvePoints != null) IdleFlight();
            base.DeSpawn(mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            CancelPlanning();
            closestThreat = null;
            base.Destroy(mode);
        }

        public void UpdateStatus()
        {
            if (!CasterPawn.DestroyedOrNull() && !CasterPawn.Dead)
            {
                CompAbilityUserMagic comp = CasterPawn.GetCompAbilityUserMagic();
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

        private int searchEnemyTick;
        protected override void Tick()
        {
        }

        protected override void TickInterval(int delta)
        {
            if (!Spawned)
            {
                CancelPlanning();
                closestThreat = null;
                return;
            }
            if (shouldDestroy || pawn.DestroyedOrNull() || !pawn.Spawned || pawn.Dead || pawn.Map != Map)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            if (FindWall(Position, Map, pawn.Faction) == null)
            {
                if (MoveToClosestWall()) Destroy(DestroyMode.Vanish);
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            int elapsed = Math.Max(1, delta);
            UpdateWallActions(currentTick);
            if (ticksToImpact >= 0 && moteDef != null && moteFrequency > 0 && currentTick % moteFrequency == 0)
                DrawEffects(ExactPosition);
            if (currentTick > searchEnemyTick)
            {
                searchEnemyTick = currentTick + Mathf.RoundToInt(Rand.Range(.4f, .6f) * searchEnemySpeed);
                AttackNearby();
                RepairOccupiedWall();
            }
            if (idleFor > 0)
            {
                idleFor = Math.Max(0, idleFor - elapsed);
                if (!TryUseQueuedPath()) return;
            }

            AdvanceMovement(elapsed);
            if (Destroyed) return;
            if (!ExactPosition.InBoundsWithNullCheck(Map))
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (currentTick % 3 == 0 && DestinationCell != Position)
            {
                FleckMaker.ThrowDustPuff(Position, Map, Rand.Range(.4f, .6f));
                float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(origin, destination)).ToAngleFlat();
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirtOverhead, DrawPos, Map,
                    1.2f, .05f, .15f, .38f, 0, 3f, angle, angle);
            }
        }

        private void AdvanceMovement(int elapsed)
        {
            if (FindWall(DestinationCell, Map, pawn.Faction) == null)
            {
                CancelPlanning();
                if (MoveToClosestWall()) Destroy(DestroyMode.Vanish);
                return;
            }
            ticksToImpact -= elapsed;
            // Carry excess elapsed time through successive segments. A route has
            // at most MaxSearchCells points, so even a large interval is bounded.
            while (ticksToImpact <= 0)
            {
                int remainder = -ticksToImpact;
                if (!DestinationCell.InBoundsWithNullCheck(Map))
                {
                    Destroy(DestroyMode.Vanish);
                    return;
                }
                Position = DestinationCell;
                if (curveVariance <= 0)
                {
                    ImpactSomething();
                    return;
                }
                if (destinationCurvePoint + 1 < curvePoints.Count)
                {
                    var next = curvePoints[destinationCurvePoint + 1].ToIntVec3();
                    if (FindWall(next, Map, pawn.Faction) == null)
                    {
                        CancelPlanning();
                        IdleFlight();
                        return;
                    }
                    origin = destination;
                    destination = curvePoints[++destinationCurvePoint];
                    ticksToImpact = StartingTicksToImpact - remainder;
                }
                else if (TryUseQueuedPath())
                {
                    ticksToImpact -= remainder;
                }
                else
                {
                    IdleFlight();
                    return;
                }
            }
        }

        public virtual void IdleFlight()
        {
            if (TryUseQueuedPath()) return;
            idleFor = 60;
            origin = Position.ToVector3Shifted();
            destination = origin;
            ticksToImpact = StartingTicksToImpact;
            curvePoints.Clear();
            curvePoints.Add(origin);
            destinationCurvePoint = 0;
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


        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
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
                    pawn.Drawer.renderer.RenderPawnAt(this.DrawPos);                      
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


