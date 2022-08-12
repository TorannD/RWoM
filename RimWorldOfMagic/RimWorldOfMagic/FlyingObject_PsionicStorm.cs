using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_PsionicStorm : Projectile
    {
        private int verVal;

        Verb verb;

        protected new Vector3 origin;
        protected new Vector3 destination;
        protected Vector3 trueOrigin;
        protected Vector3 targetCenter;
        private Vector3 nearApex;
        private Vector3 farApex;
        private Vector3 direction;
        List<IntVec3> targetCells;

        public float curveVariance = 0; // 0 = no curve
        private List<Vector3> curvePoints = new List<Vector3>();
        private int destinationCurvePoint = 0;
        private int stage = 0;
        private float curveAngle = 0;

        protected float speed = 40f;

        protected new int ticksToImpact = 60;
        private int nextAttackTick = 0;

        protected Thing assignedTarget;
        protected Thing flyingThing;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;
        public bool explosion = false;
        public int timesToDamage = 3;
        public int weaponDmg = 0;

        Pawn pawn;

        //local variables
        float targetCellRadius = 4;
        float circleFlightSpeed = 10;
        float circleRadius = 10;
        int attackFrequencyLow = 10;
        int attackFrequencyHigh = 40;

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
            Scribe_Values.Look<Vector3>(ref this.trueOrigin, "trueOrigin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.nearApex, "nearApex", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.farApex, "farApex", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.direction, "direction", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.weaponDmg, "weaponDmg", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.destinationCurvePoint, "destinationCurvePoint", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_Collections.Look<IntVec3>(ref this.targetCells, "targetCells", LookMode.Value);
            Scribe_Collections.Look<Vector3>(ref this.curvePoints, "curvePoints", LookMode.Value);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
                this.curvePoints = new List<Vector3>();
                this.curvePoints.Clear();
                this.targetCells = new List<IntVec3>();
                this.targetCells.Clear();
                this.targetCells = GenRadial.RadialCellsAround(this.targetCenter.ToIntVec3(), 4, true).ToList();
                this.verVal = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_ver").level;
            }
            //flyingThing.ThingID += Rand.Range(0, 2147).ToString();
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, Verb verb)
        {
            this.verb = verb;
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            bool spawned = flyingThing.Spawned;
            this.pawn = launcher as Pawn;
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            //
            ModOptions.Constants.SetPawnInFlight(true);
            //
            this.origin = origin;
            this.trueOrigin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.targetCenter = targ.Cell.ToVector3Shifted();
            this.direction = GetVector(this.trueOrigin, this.targetCenter);
            this.nearApex = this.targetCenter + ((-this.circleRadius) * this.direction);
            this.farApex = this.targetCenter + (this.circleRadius * this.direction);
            this.destination = this.nearApex; //set initial destination to be outside of storm circle
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        public void CalculateCurvePoints(Vector3 start, Vector3 end, float variance)
        {
            this.destinationCurvePoint = 0;
            this.curvePoints.Clear();
            int variancePoints = 20;
            Vector3 initialVector = GetVector(start, end);
            initialVector.y = 0;
            float initialAngle = (initialVector).ToAngleFlat(); //Quaternion.AngleAxis(90, Vector3.up) *
            if (this.curveAngle == 0)
            {
                if (Rand.Chance(.5f))
                {
                    curveAngle = variance;
                }
                else
                {
                    variance = (-1) * variance;
                    curveAngle = variance;
                }
            }
            else
            {
                variance = this.curveAngle;
            }
            //calculate extra distance bolt travels around the ellipse
            float a = .5f * Vector3.Distance(start, end);
            float b = a * Mathf.Sin(.5f * Mathf.Deg2Rad * Mathf.Abs(this.curveAngle));
            float p = .5f * Mathf.PI * (3 * (a + b) - (Mathf.Sqrt((3 * a + b) * (a + 3 * b))));

            float incrementalDistance = p / variancePoints;
            float incrementalAngle = (variance / variancePoints) * 2;
            this.curvePoints.Add(start);
            for (int i = 1; i < variancePoints; i++)
            {
                this.curvePoints.Add(this.curvePoints[i - 1] + ((Quaternion.AngleAxis(variance, Vector3.up) * initialVector) * incrementalDistance)); //(Quaternion.AngleAxis(curveAngle, Vector3.up) *
                variance -= incrementalAngle;
            }
        }

        public override void Tick()
        {
            //base.Tick();
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map) || base.Position.DistanceToEdge(base.Map) <= 1;
            if (this.stage > 0 && this.stage < 4 && this.nextAttackTick < Find.TickManager.TicksGame)
            {
                IntVec3 targetVariation = this.targetCells.RandomElement();
                float angle = (Quaternion.AngleAxis(90, Vector3.up) * GetVector(this.ExactPosition, targetVariation.ToVector3Shifted())).ToAngleFlat();
                Vector3 drawPos = this.ExactPosition + (GetVector(this.ExactPosition, targetVariation.ToVector3Shifted()) * .5f);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiBlastStart, drawPos, base.Map, Rand.Range(.4f, .6f), Rand.Range(.0f, .05f), 0f, .1f, 0, 0, 0, angle); //throw psi blast start
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiBlastEnd, drawPos, base.Map, Rand.Range(.4f, .8f), Rand.Range(.0f, .1f), .2f, .3f, 0, Rand.Range(1f, 1.5f), angle, angle); //throw psi blast end 
                this.TryLaunchProjectile(ThingDef.Named("TM_Projectile_PsionicBlast"), targetVariation);
                this.nextAttackTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(attackFrequencyLow, attackFrequencyHigh) * (1 - .1f * this.verVal));
            }
            if (flag)
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                GenPlace.TryPlaceThing(this.flyingThing, base.Position, this.Map, ThingPlaceMode.Near);
                //GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
                ModOptions.Constants.SetPawnInFlight(false);
                Pawn p = this.flyingThing as Pawn;
                if (p.IsColonist)
                {
                    p.drafter.Drafted = true;
                    if (ModOptions.Settings.Instance.cameraSnap)
                    {
                        CameraJumper.TryJumpAndSelect(p);
                    }
                }
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.8f, 1.2f));
                bool flag2 = this.ticksToImpact <= 0;
                if (flag2)
                {
                    if (this.stage == 0)
                    {
                        CalculateCurvePoints(this.nearApex, this.farApex, 90);
                        this.origin = this.curvePoints[this.destinationCurvePoint];
                        this.destinationCurvePoint++;
                        this.destination = this.curvePoints[this.destinationCurvePoint];
                        this.speed = this.circleFlightSpeed;
                        this.ticksToImpact = this.StartingTicksToImpact;
                        this.nextAttackTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Rand.Range(attackFrequencyLow, attackFrequencyHigh) * (1 - .1f * this.verVal));
                        this.stage = 1;                        
                    }
                    else if(this.stage == 1)
                    {
                        if ((this.curvePoints.Count() - 1) > this.destinationCurvePoint)
                        {
                            this.origin = this.curvePoints[this.destinationCurvePoint];
                            this.destinationCurvePoint++;
                            this.destination = this.curvePoints[this.destinationCurvePoint];
                            this.ticksToImpact = this.StartingTicksToImpact;
                        }
                        else
                        {
                            this.origin = this.curvePoints[this.destinationCurvePoint];
                            CalculateCurvePoints(this.origin, this.nearApex, 90);
                            this.destinationCurvePoint++;
                            this.destination = this.curvePoints[this.destinationCurvePoint];
                            this.ticksToImpact = this.StartingTicksToImpact;
                            this.stage = 2;
                        }
                    }
                    else if(this.stage == 2)
                    {
                        if ((this.curvePoints.Count() - 1) > this.destinationCurvePoint)
                        {
                            this.origin = this.curvePoints[this.destinationCurvePoint];
                            this.destinationCurvePoint++;
                            this.destination = this.curvePoints[this.destinationCurvePoint];
                            this.ticksToImpact = this.StartingTicksToImpact;
                        }
                        else
                        {
                            this.origin = this.curvePoints[this.destinationCurvePoint];
                            this.destination = this.nearApex;
                            this.ticksToImpact = this.StartingTicksToImpact;
                            //this.speed = 15;
                            this.stage = 3;
                        }
                    }
                    else if (this.stage == 3)
                    {
                        this.speed = 25f;
                        this.origin = this.nearApex;
                        this.destination = this.trueOrigin;
                        this.ticksToImpact = this.StartingTicksToImpact;
                        this.stage = 4;                        
                    }
                    else
                    {
                        bool flag3 = this.DestinationCell.InBoundsWithNullCheck(base.Map);
                        if (flag3)
                        {
                            base.Position = this.DestinationCell;
                        }
                        this.ImpactSomething();
                    }
                }
                
            }
        }
               
        public override void Draw()
        {
            bool flag = this.flyingThing != null;
            if (flag)
            {
                float angleToCenter = GetVector(this.ExactPosition, this.targetCenter).ToAngleFlat();
                if (angleToCenter > -45 && angleToCenter < 45)
                {
                    this.flyingThing.Rotation = Rot4.East;
                }
                else if (angleToCenter > 45 && angleToCenter < 135)
                {
                    this.flyingThing.Rotation = Rot4.South;
                }
                else if (angleToCenter > 135 || angleToCenter < -135)
                {
                    this.flyingThing.Rotation = Rot4.West;
                }
                else
                {
                    this.flyingThing.Rotation = Rot4.North;
                }

                bool flag2 = this.flyingThing is Pawn;
                if (flag2)
                {
                    Vector3 arg_2B_0 = this.DrawPos;
                    bool flag3 = false;
                    if (flag3)
                    {
                        return;
                    }
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
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
                base.Comps_PostDraw();
            }
        }

        private void TryLaunchProjectile(ThingDef projectileDef, LocalTargetInfo launchTarget)
        {
                Vector3 drawPos = this.ExactPosition; 
                Projectile_AbilityBase projectile_AbilityBase = (Projectile_AbilityBase)GenSpawn.Spawn(projectileDef, this.ExactPosition.ToIntVec3(), this.Map);
                //ShotReport shotReport = ShotReport.HitReportFor(this.pawn, this.verb, launchTarget);
                SoundDef expr_C8 = TorannMagicDefOf.TM_AirWoosh;
                if (expr_C8 != null)
                {
                    SoundStarter.PlayOneShot(expr_C8, new TargetInfo(this.ExactPosition.ToIntVec3(), this.Map, false));
                }
                projectile_AbilityBase.Launch(this.pawn, TorannMagicDefOf.TM_PsionicBlast, drawPos, launchTarget, ProjectileHitFlags.All, false, null, null, null, null);
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
            bool flag = hitThing == null;
            if (flag)
            {
                Pawn pawn;
                bool flag2 = (pawn = (base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn)) != null;
                if (flag2)
                {
                    hitThing = pawn;
                }
            }
            bool hasValue = this.impactDamage.HasValue;
            if (hasValue)
            {
                for (int i = 0; i < this.timesToDamage; i++)
                {
                    bool flag3 = this.damageLaunched;
                    if (flag3)
                    {
                        this.flyingThing.TakeDamage(this.impactDamage.Value);
                    }
                    else
                    {
                        hitThing.TakeDamage(this.impactDamage.Value);
                    }
                }
                bool flag4 = this.explosion;
                if (flag4)
                {
                    GenExplosion.DoExplosion(base.Position, base.Map, 0.9f, DamageDefOf.Stun, this, -1, 0, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                }
            }
            GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
            ModOptions.Constants.SetPawnInFlight(false);
            Pawn p = this.flyingThing as Pawn;
            if(p.IsColonist)
            {
                p.drafter.Drafted = true;
                if (ModOptions.Settings.Instance.cameraSnap)
                {
                    CameraJumper.TryJumpAndSelect(p);
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }

        public Vector3 GetVector(Vector3 center, Vector3 objectPos)
        {
            Vector3 heading = (objectPos - center);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }
}
