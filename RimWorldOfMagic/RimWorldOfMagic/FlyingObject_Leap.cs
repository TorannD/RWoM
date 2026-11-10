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
    public class FlyingObject_Leap : Projectile
    {
        protected new Vector3 origin;

        protected new Vector3 destination;

        protected float speed = 28f;
        private bool drafted = false;

        protected new int ticksToImpact;

        protected Thing assignedTarget;

        protected Thing flyingThing;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int weaponDmg = 0;

        private bool initialize = true;

        Pawn pawn;
        CompAbilityUserMagic comp;

        // Landing retry state: the leaping pawn is despawned while in flight, so a
        // blocked destination must be retried for a bounded interval instead of
        // losing the pawn; damage is applied at most once per flight.
        private const int MaxLandRetryTicks = 120;
        private int landRetryTicks = -1;
        private bool damageApplied = false;

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
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_Values.Look<int>(ref this.landRetryTicks, "landRetryTicks", -1, false);
            Scribe_Values.Look<bool>(ref this.damageApplied, "damageApplied", false, false);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                //MoteMaker.MakeStaticMote(pawn.TrueCenter(), pawn.Map, ThingDefOf.Mote_ExplosionFlash, 12f);
                //SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            //flyingThing.ThingID += Rand.Range(0, 2147).ToString();
            this.initialize = false;
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            this.Launch(launcher, base.Position.ToVector3Shifted(), targ, flyingThing, null);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            bool spawned = flyingThing != null && flyingThing.Spawned;
            pawn = launcher as Pawn;
            if (spawned)
            {               
                flyingThing.DeSpawn();
            }
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
            this.Initialize();
        }        

        protected override void Tick()
        {
        }
        protected override void TickInterval(int delta)
        {
            //base.Tick();
            if (this.landRetryTicks >= 0)
            {
                this.landRetryTicks += delta;
                if (this.landRetryTicks >= MaxLandRetryTicks)
                {
                    Log.Warning("TM Leap: could not place the leaping pawn within " + MaxLandRetryTicks + " ticks; the pawn remains despawned where it leapt from.");
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
                this.Impact(null);
                return;
            }
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
                // Never orphan the leaping pawn off-map: land at the closest
                // in-bounds cell instead of destroying the projectile mid-flight.
                this.ticksToImpact = 0;
                IntVec3 c = this.ExactPosition.ToIntVec3();
                if (base.Map != null)
                {
                    c.x = Mathf.Clamp(c.x, 0, base.Map.Size.x - 1);
                    c.z = Mathf.Clamp(c.z, 0, base.Map.Size.z - 1);
                    this.destination = c.ToVector3Shifted();
                    base.Position = c;
                    this.ImpactSomething();
                }
                else
                {
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                if(Find.TickManager.TicksGame % 2 == 0)
                {
                    FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.6f, .8f));
                }               
                
                bool flag2 = this.ticksToImpact <= 0;
                if (flag2)
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

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // flyingThing can be null after a failed save restore — never draw it.
            if (this.flyingThing != null)
            {
                if (this.flyingThing is Pawn pawn2)
                {
                    Vector3 arg_2B_0 = this.DrawPos;
                    bool flag4 = !this.DrawPos.ToIntVec3().IsValid;
                    if (flag4)
                    {
                        return;
                    }
                    pawn2.Drawer.renderer.RenderPawnAt(this.DrawPos);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
            }
            base.Comps_PostDraw();
        }

        private void DrawEffects(Vector3 pawnVec, Pawn flyingPawn, int magnitude)
        {
            bool flag = !pawn.Dead && !pawn.Downed;
            if (flag)
            {

            }
        }

        private void ImpactSomething()
        {
            // A destroyed/despawned target must not be passed into Impact.
            if (this.assignedTarget != null && !this.assignedTarget.Destroyed && this.assignedTarget.Spawned)
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
            try
            {
                if (hitThing == null && base.Map != null)
                {
                    Pawn pawn = base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn;
                    if (pawn != null)
                    {
                        hitThing = pawn;
                    }
                }
                if (this.impactDamage.HasValue && !this.damageApplied && hitThing != null && !hitThing.Destroyed)
                {
                    this.damageApplied = true;
                    hitThing.TakeDamage(this.impactDamage.Value);
                }

                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                if (this.flyingThing != null && !this.flyingThing.Destroyed && base.Map != null && !this.flyingThing.Spawned)
                {
                    // Retry placement for a bounded interval when the destination
                    // is blocked; the projectile survives until the pawn is back.
                    if (GenPlace.TryPlaceThing(this.flyingThing, base.Position, base.Map, ThingPlaceMode.Direct) ||
                        GenPlace.TryPlaceThing(this.flyingThing, base.Position, base.Map, ThingPlaceMode.Near))
                    {
                        this.landRetryTicks = -1;
                    }
                    else
                    {
                        if (this.landRetryTicks < 0)
                        {
                            this.landRetryTicks = 0;
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("TM Leap: error while landing: " + ex);
            }
            this.Destroy(DestroyMode.Vanish);
        }        
    }
}
