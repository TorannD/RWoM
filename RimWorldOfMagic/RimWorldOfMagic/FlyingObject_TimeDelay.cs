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
    public class FlyingObject_TimeDelay : Projectile, IThingHolder, ISuspendableThingHolder
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
        private ThingOwner<Thing> contents;
        private Thing legacyPayload;
        private Thing flyingThing => contents?.Count > 0 ? contents[0] : null;
        private bool takingOff, endCostPaid;
        public FlyingObject_TimeDelay() { contents = new ThingOwner<Thing>(this,true); }
        public bool IsContentsSuspended => true;
        public ThingOwner GetDirectlyHeldThings() => contents;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children,contents);
        internal static bool ProtectsMap(Map map)
        {
            var def = TorannMagicDefOf.FlyingObject_TimeDelay;
            if (map?.listerThings == null || def == null) return false;
            foreach (var thing in map.listerThings.ThingsOfDef(def))
                if (thing is FlyingObject_TimeDelay field && (field.takingOff || field.flyingThing is Pawn p && p.Faction?.IsPlayer == true)) return true;
            return false;
        }
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
            Scribe_Deep.Look(ref contents,"TM_suspendedPayload",this);
            if (Scribe.mode != LoadSaveMode.Saving) Scribe_Deep.Look(ref legacyPayload,"flyingThing");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                contents ??= new ThingOwner<Thing>(this,true);
                if (legacyPayload != null && !legacyPayload.Spawned && legacyPayload.holdingOwner == null)
                    contents.TryAdd(legacyPayload,false);
                legacyPayload = null;
                drawPosition = variationDestination = origin;
            }
            Scribe_Values.Look(ref endCostPaid,"TM_stasisCostPaid");
            Scribe_Values.Look(ref spinRate,"TM_stasisSpin");
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
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
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
            if (flyingThing == null || flyingThing.Destroyed || contents.Count != 0 || flyingThing.Spawned && flyingThing.Map != Map) return;
            bool spawned = flyingThing.Spawned;
            var returnCell = spawned ? flyingThing.Position : Position;
            pawn = launcher as Pawn;
            if (flyingThing is Pawn passenger && passenger.Drafted)
            {
                this.drafted = true;
            }
            takingOff = flyingThing is Pawn taking && taking.Faction?.IsPlayer == true;
            try
            {
                if (spawned) flyingThing.DeSpawn();
                if (!contents.TryAddOrTransfer(flyingThing,false))
                {
                    if (spawned && !flyingThing.Spawned && flyingThing.holdingOwner == null) GenSpawn.Spawn(flyingThing,returnCell,Map);
                    Destroy();
                    return;
                }
            }
            finally { takingOff = false; }
            this.speed = this.speed * this.force;
            this.launcher = launcher;
            this.origin = origin;
            this.impactDamage = newDamageInfo;

            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.destination = targ.Cell.ToVector3Shifted();
            this.ticksToImpact = this.StartingTicksToImpact;
            this.variationDestination = base.Position.ToVector3Shifted(); //this.DrawPos //not initialized?
            this.drawPosition = base.Position.ToVector3Shifted(); //this.DrawPos; 
            this.Initialize();
        }

        protected override void Tick()
        {
        }

        protected override void TickInterval(int delta)
        {
            this.duration -= delta;
            base.Position = this.origin.ToIntVec3();
            bool flag2 = this.duration <= 0;
            if(this.moteDef != null && this.Map != null && this.moteFrequency > 0 && Find.TickManager.TicksGame % this.moteFrequency == 0)
            {
                TM_MoteMaker.ThrowGenericMote(this.moteDef, this.ExactPosition, this.Map, Rand.Range(this.moteScale * .75f, this.moteScale * 1.25f), this.solidTime, this.fadeInTime, this.fadeOutTime, Rand.Range(200, 400), 0, 0, Rand.Range(0, 360));
            }
            if (flag2)
            {
                this.ImpactSomething();
            }

        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
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
                    pawn.Drawer.renderer.RenderPawnAt(this.DrawPos);
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
                    //this.flyingThing.DrawAt(this.drawPosition);
                    this.flyingThing.DrawNowAt(drawPosition);
                    //this.flyingThing.DrawAt(drawPosition);
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
            startPos.y = 10f;
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
            Finish(true);
        }

        private void Finish(bool naturalExpiry)
        {
            var payload = flyingThing;
            if (payload == null || payload.Destroyed)
            {
                base.Destroy();
                return;
            }
            if (Map == null) return;
            if (naturalExpiry && !endCostPaid)
            {
                endCostPaid = true;
                if (destroyPctAtEnd > 0f)
                {
                    int cost = Mathf.Clamp(Mathf.RoundToInt(payload.stackCount * destroyPctAtEnd),0,payload.stackCount);
                    bool fullyConsumed = cost == payload.stackCount;
                    if (fullyConsumed) payload.Destroy(DestroyMode.Vanish);
                    else if (cost > 0) payload.SplitOff(cost).Destroy(DestroyMode.Vanish);
                    for (int i = 0; i < 10; i++)
                    {
                        var pos = DrawPos;
                        pos.x += Rand.Range(-.3f,.3f);
                        pos.z += Rand.Range(-.3f,.3f);
                        pos.y += Rand.Range(0,1.2f);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow,pos,Map,Rand.Range(.5f,1f),.4f,
                            Rand.Range(.1f,.4f),Rand.Range(.2f,1.2f),Rand.Range(-200,200),Rand.Range(1.2f,2f),Rand.Range(0,360),0);
                    }
                    var info = SoundInfo.InMap(new TargetInfo(Position,Map));
                    info.pitchFactor = .8f;
                    info.volumeFactor = 1.2f;
                    TorannMagicDefOf.TM_Vibration.PlayOneShot(info);
                    if (fullyConsumed)
                    {
                        base.Destroy();
                        return;
                    }
                }
            }
            foreach (var cell in GenRadial.RadialCellsAround(Position,6.9f,true))
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map)) continue;
                if (payload is Pawn && cell.GetFirstPawn(Map) != null) continue;
                if (contents.TryDrop(payload,cell,Map,ThingPlaceMode.Direct,out var landed,playDropSound:false))
                {
                    if (landed is Pawn p && p.IsColonist && drafted && p.drafter != null) p.drafter.Drafted = true;
                    base.Destroy();
                    return;
                }
            }
            duration = 60;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (flyingThing != null) Finish(false);
            else base.Destroy(mode);
        }
    }
}
