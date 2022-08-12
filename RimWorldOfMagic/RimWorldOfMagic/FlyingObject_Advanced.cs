using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_Advanced : Projectile
    {
        protected new Vector3 origin;        
        protected new Vector3 destination;
        protected Vector3 trueOrigin;
        protected Vector3 trueDestination;

        public float speed = 30f;
        public Vector3 travelVector = default(Vector3);
        protected new int ticksToImpact;
        //protected new Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;

        public ThingDef moteDef = null;
        public int moteFrequency = 0;
        public float moteSize = 1f;

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

        Pawn pawn;

        //Magic related
        CompAbilityUserMagic comp;
        TMPawnSummoned newPawn = new TMPawnSummoned();

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
            //Scribe_References.Look<Thing>(ref this.launcher, "launcher", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
        }

        public virtual void PreInitialize()
        {

        }

        private void Initialize()
        {
            PreInitialize();
            if (pawn != null)
            {
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            else
            {
                flyingThing.ThingID += Rand.Range(0, 214).ToString();
            }            
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void AdvancedLaunch(Thing launcher, ThingDef effectMote, int moteFrequencyTicks, float curveAmount, bool shouldSpin, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, int flyingSpeed, bool isExplosion, int _impactDamage, float _impactRadius, DamageDef damageType, DamageInfo? newDamageInfo = null, int doubleVariance = 0, bool flyOverhead = false, float moteEffectSize = 1f)
        {
            this.fliesOverhead = flyOverhead;
            this.explosionDamage = _impactDamage;
            this.isExplosive = isExplosion;
            this.impactRadius = _impactRadius;
            this.impactDamageType = damageType;
            this.moteFrequency = moteFrequencyTicks;
            this.moteDef = effectMote;
            this.moteSize = moteEffectSize;
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
            travelVector = initialVector;
            float initialAngle = (initialVector).ToAngleFlat(); //Quaternion.AngleAxis(90, Vector3.up) *
            float curveAngle = variance;
            if(doublesidedVariance == 0)
            {
                if (Rand.Chance(.5f))
                {
                    curveAngle = (-1) * variance;
                }
            }
            else
            {
                curveAngle = (doublesidedVariance * variance);
            }

            //calculate extra distance bolt travels around the ellipse
            float a = .47f * Vector3.Distance(start, end);
            float b = a * Mathf.Sin(.5f * Mathf.Deg2Rad * variance);
            float p = .5f * Mathf.PI * (3 * (a + b) - (Mathf.Sqrt((3 * a + b) * (a + 3 * b))));
                    
            float incrementalDistance = p / variancePoints; 
            float incrementalAngle = (curveAngle / (float)variancePoints) * 2f;
            this.curvePoints.Add(this.trueOrigin);
            for(int i = 1; i <= (variancePoints + 1); i++)
            {
                this.curvePoints.Add(this.curvePoints[i - 1] + ((Quaternion.AngleAxis(curveAngle, Vector3.up) * initialVector) * incrementalDistance)); //(Quaternion.AngleAxis(curveAngle, Vector3.up) *
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

        public virtual void PreTick()
        {

        }

        public override void Tick()
        {
            PreTick();
            Vector3 exactPosition = this.ExactPosition;
            if (this.ticksToImpact >= 0 && this.moteDef != null && Find.TickManager.TicksGame % this.moteFrequency == 0)
            {
                DrawEffects(exactPosition);
            }
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
            }
            else if(!this.ExactPosition.ToIntVec3().Walkable(base.Map) && !fliesOverhead)
            {
                this.earlyImpact = true;
                this.impactForce = (this.DestinationCell - this.ExactPosition.ToIntVec3()).LengthHorizontal + (this.speed * .2f);
                this.ImpactSomething();
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                if(moteDef == null && Find.TickManager.TicksGame % 3 == 0)
                {
                    FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.6f, .8f));
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
            PostTick();
        }

        public virtual void PostTick()
        {

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
                    Matrix4x4 matrix = new Matrix4x4();
                    matrix.SetTRS(this.DrawPos, this.ExactRotation, new Vector3(this.Graphic.drawSize.x, 13f, this.Graphic.drawSize.y));
                    Graphics.DrawMesh(MeshPool.plane10,matrix, this.flyingThing.def.DrawMatSingle, 0);
                }
            }
            //else
            //{
            //    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
            //}
            base.Comps_PostDraw();
        }

        public virtual void DrawEffects(Vector3 effectVec)
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
                hitThing.TakeDamage(this.impactDamage.Value);
            }
            ImpactOverride();
            if (this.flyingThing is Pawn p)
            {
                try
                {
                    SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                    GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);                   
                    if (this.earlyImpact)
                    {
                        damageEntities(p, this.impactForce, DamageDefOf.Blunt);
                        damageEntities(p, 2 * this.impactForce, DamageDefOf.Stun);
                    }
                    this.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                    GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                if(this.impactRadius > 0)
                {
                    if(this.isExplosive)
                    {
                        GenExplosion.DoExplosion(this.ExactPosition.ToIntVec3(), this.Map, this.impactRadius, this.impactDamageType, this.launcher as Pawn, this.explosionDamage, -1, this.impactDamageType.soundExplosion, def, null, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);
                    }
                    else
                    {
                        int num = GenRadial.NumCellsInRadius(this.impactRadius);
                        IntVec3 curCell;
                        for (int i = 0; i < num; i++)
                        {
                            curCell = this.ExactPosition.ToIntVec3() + GenRadial.RadialPattern[i];
                            List<Thing> hitList = new List<Thing>();
                            hitList = curCell.GetThingList(this.Map);
                            for (int j = 0; j < hitList.Count; j++)
                            {
                                if (hitList[j] is Pawn && hitList[j] != this.pawn)
                                {
                                    damageEntities(hitList[j], this.explosionDamage, this.impactDamageType);
                                }
                            }
                        }
                    }
                }
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public virtual void ImpactOverride()
        {

        }

        public void damageEntities(Thing e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, this.pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }
}
