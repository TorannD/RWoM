using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using AbilityUser;
using Verse.Sound;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_TimeDelay : Projectile
    {
        protected new Vector3 origin;
        protected new Vector3 destination;
        private Vector3 direction;
        private Vector3 variationDestination;
        private Vector3 drawPosition;

        public float speed = 10f;
        public int spinRate = 0;        //spin rate > 0 makes the object rotate every spinRate Ticks
        public float xVariation = 0;    //x variation makes the object move side to side by +- variation
        public float zVariation = 0;    //z variation makes the object move up and down by +- variation
        private int rotation = 0;
        protected new int ticksToImpact;
        //protected new Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        private bool drafted = false;
        public float destroyPctAtEnd = 0f;

        public int moteFrequency = 0;
        public ThingDef moteDef = null;
        public float fadeInTime = .25f;
        public float fadeOutTime = .25f;
        public float solidTime = .5f;
        public float moteScale = 1f;

        public float force = 1f;
        public int duration = 600;

        private bool earlyImpact = false;
        private float impactForce = 0;
        private int variationShiftTick = 100;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int weaponDmg = 0;

        Pawn pawn;
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
                //Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                //return this.origin + b + Vector3.up * this.def.Altitude;
                return this.origin + Vector3.up * this.def.Altitude;
            }
        }

        public new Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.origin - this.destination);
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
            Scribe_Values.Look<bool>(ref this.drafted, "drafted", false, false);
            Scribe_Values.Look<float>(ref this.xVariation, "xVariation", 0, false);
            Scribe_Values.Look<float>(ref this.zVariation, "zVariation", 0, false);
            Scribe_Values.Look<float>(ref this.solidTime, "solidTime", .5f, false);
            Scribe_Values.Look<float>(ref this.fadeInTime, "fadeInTime", .25f, false);
            Scribe_Values.Look<float>(ref this.fadeOutTime, "fadeOutTime", .25f, false);
            Scribe_Defs.Look<ThingDef>(ref this.moteDef, "moteDef");
            Scribe_Values.Look<float>(ref this.moteScale, "moteScale", 1f, false);
            Scribe_Values.Look<int>(ref this.moteFrequency, "moteFrequency", 0, false);
            Scribe_Values.Look<float>(ref this.destroyPctAtEnd, "destroyPctAtEnd", 0f, false);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }

            this.direction = TM_Calc.GetVector(this.origin.ToIntVec3(), this.destination.ToIntVec3());
            //flyingThing.ThingID += Rand.Range(0, 2147).ToString();
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, int _spinRate)
        {
            this.spinRate = _spinRate;
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void LaunchVaryPosition(Thing launcher, LocalTargetInfo targ, Thing flyingThing, int _spinRate, float _xVariation, float _zVariation, ThingDef mote = null, int moteFreq = 0, float destroy = 0f)
        {
            this.destroyPctAtEnd = destroy;
            this.moteDef = mote;
            this.moteFrequency = moteFreq; 
            this.xVariation = _xVariation;
            this.zVariation = _zVariation;
            this.spinRate = _spinRate;
            this.Launch(launcher, flyingThing.DrawPos, flyingThing.Position, flyingThing, null);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {            
            bool spawned = flyingThing.Spawned;            
            pawn = launcher as Pawn;
            if (pawn != null && pawn.Drafted)
            {
                this.drafted = true;
            }
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.speed = this.speed * this.force;
            this.launcher = launcher;
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;

            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.destination = targ.Cell.ToVector3Shifted();
            this.ticksToImpact = this.StartingTicksToImpact;
            this.variationDestination = this.DrawPos;
            this.drawPosition = this.DrawPos;
            this.Initialize();
        }        

        public override void Tick()
        {
            this.duration--;
            base.Position = this.origin.ToIntVec3();
            bool flag2 = this.duration <= 0;
            if(this.moteDef != null && this.Map != null && Find.TickManager.TicksGame % this.moteFrequency == 0)
            {
                TM_MoteMaker.ThrowGenericMote(this.moteDef, this.ExactPosition, this.Map, Rand.Range(this.moteScale * .75f, this.moteScale * 1.25f), this.solidTime, this.fadeInTime, this.fadeOutTime, Rand.Range(200, 400), 0, 0, Rand.Range(0, 360));
            }
            if (flag2)
            {
                this.ImpactSomething();
            }

        }

        public override void Draw()
        {
            bool flag = this.flyingThing != null;
            if (flag)
            {
                if (this.spinRate > 0)
                {
                    if(Find.TickManager.TicksGame % this.spinRate ==0)
                    {
                        this.rotation++;
                        if(this.rotation >= 4)
                        {
                            this.rotation = 0;
                        }
                    }
                    if (rotation == 0)
                    {
                        this.flyingThing.Rotation = Rot4.West;
                    }
                    else if (rotation == 1)
                    {
                        this.flyingThing.Rotation = Rot4.North;
                    }
                    else if (rotation == 2)
                    {
                        this.flyingThing.Rotation = Rot4.East;
                    }
                    else
                    {
                        this.flyingThing.Rotation = Rot4.South;
                    }
                }

                bool flag2 = this.flyingThing is Pawn;
                if (flag2 && zVariation == 0 && xVariation == 0)
                {
                    Vector3 arg_2B_0 = this.DrawPos;
                    bool flag4 = !this.DrawPos.ToIntVec3().IsValid;
                    if (flag4)
                    {
                        return;
                    }
                    Pawn pawn = this.flyingThing as Pawn;
                    pawn.Drawer.DrawAt(this.DrawPos);
                    Material bubble = TM_MatPool.TimeBubble;
                    Vector3 vec3 = this.DrawPos;
                    vec3.y++;
                    Vector3 s = new Vector3(2f, 1f, 2f);
                    Matrix4x4 matrix = default(Matrix4x4);
                    matrix.SetTRS(vec3, Quaternion.AngleAxis(0, Vector3.up), s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, bubble, 0, null);
                    //Graphics.DrawMesh(MeshPool.plane10, vec3, this.ExactRotation, bubble, 0);
                }
                else if(zVariation != 0 || xVariation != 0)
                {
                    this.drawPosition = VariationPosition(this.drawPosition);
                    //bool flag4 = !this.DrawPos.ToIntVec3().IsValid;
                    //if (flag4)
                    //{
                    //    return;
                    //}
                    //Pawn pawn = this.flyingThing as Pawn;
                    //pawn.Drawer.DrawAt(this.DrawPos);
                    this.flyingThing.DrawAt(this.drawPosition);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
            }
            base.Comps_PostDraw();
        }

        private Vector3 VariationPosition(Vector3 currentDrawPos)
        {
            Vector3 startPos = currentDrawPos;
            float variance = (xVariation / 100f);
            if ((startPos.x - variationDestination.x) < -variance)
            {
                startPos.x += variance;
            }
            else if((startPos.x - variationDestination.x) > variance)
            {
                startPos.x += -variance;
            }
            else if (this.xVariation != 0)
            {
                variationDestination.x = this.DrawPos.x + Rand.Range(-xVariation, xVariation);
            }
            variance = (zVariation / 100f);
            if ((startPos.z - variationDestination.z) < -variance)
            {
                startPos.z += variance;
            }
            else if ((startPos.z - variationDestination.z) > variance)
            {
                startPos.z += -variance;
            }
            else if (this.zVariation != 0)
            {
                variationDestination.z = this.DrawPos.z + Rand.Range(-zVariation, zVariation);
            }

            return startPos;
        }


        private void ImpactSomething()
        {
            this.Impact(null);            
        }

        protected new void Impact(Thing hitThing)
        {
            if (this.Map != null)
            {
                GenPlace.TryPlaceThing(this.flyingThing, base.Position, base.Map, ThingPlaceMode.Direct);
                if (this.flyingThing is Pawn p)
                {
                    if (p.IsColonist && this.drafted && p.drafter != null)
                    {
                        p.drafter.Drafted = true;
                    }
                }

                if (this.destroyPctAtEnd != 0)
                {
                    int rangeMax = 10;
                    for (int i = 0; i < rangeMax; i++)
                    {
                        float direction = Rand.Range(0, 360);
                        Vector3 rndPos = this.flyingThing.DrawPos;
                        rndPos.x += Rand.Range(-.3f, .3f);
                        rndPos.z += Rand.Range(-.3f, .3f);
                        ThingDef mote = TorannMagicDefOf.Mote_Shadow;
                        TM_MoteMaker.ThrowGenericMote(mote, rndPos, this.Map, Rand.Range(.5f, 1f), 0.4f, Rand.Range(.1f, .4f), Rand.Range(1.2f, 2f), Rand.Range(-200, 200), Rand.Range(1.2f, 2f), direction, direction);

                    }
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(base.Position, this.Map, false), MaintenanceType.None);
                    info.pitchFactor = .8f;
                    info.volumeFactor = 1.2f;
                    TorannMagicDefOf.TM_Vibration.PlayOneShot(info);
                }

                if (this.destroyPctAtEnd >= 1f)
                {
                    this.flyingThing.Destroy(DestroyMode.Vanish);
                }
                else if (this.destroyPctAtEnd != 0)
                {
                    this.flyingThing.SplitOff(Mathf.RoundToInt(this.flyingThing.stackCount * this.destroyPctAtEnd)).Destroy(DestroyMode.Vanish);
                }
            }

            this.Destroy(DestroyMode.Vanish);
        }
    }
}
