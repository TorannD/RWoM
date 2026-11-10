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
    public class FlyingObject_Flight : Projectile
    {
        protected new Vector3 origin;

        protected new Vector3 destination;

        protected float speed = 20f;
        private bool drafted = false;

        protected new int ticksToImpact;

        protected Thing assignedTarget;
        protected Thing flyingThing;
        private IntVec3 acceptedDestination = IntVec3.Invalid;
        private bool acceptedDanger;
        private Map acceptedMap;
        private Pawn acceptedPassenger;
        private bool impactApplied;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int weaponDmg = 0;

        Pawn pawn;
        CompAbilityUserMagic comp;

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
            Scribe_Values.Look(ref acceptedDestination, "travelAcceptedDestination", IntVec3.Invalid);
            Scribe_Values.Look(ref acceptedDanger, "travelAcceptedDanger");
            Scribe_References.Look(ref acceptedMap, "travelAcceptedMap");
            Scribe_References.Look(ref acceptedPassenger, "travelAcceptedPassenger");
            Scribe_Values.Look(ref impactApplied, "travelImpactApplied");
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 4f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(2.4f, 3.6f));
            }
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

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            var passenger = flyingThing as Pawn;
            if (passenger != null && !Odyssey.ManualTravelSafety.CanComplete(passenger, Map, targ.Cell,
                (int)Math.Ceiling((origin - targ.Cell.ToVector3Shifted()).magnitude * 5f)))
            {
                Destroy();
                return;
            }
            acceptedDestination = targ.Cell;
            acceptedMap = Map;
            acceptedPassenger = passenger;
            acceptedDanger = passenger != null && Odyssey.ManualTravelSafety.HasConsent(passenger, passenger.CurJob, Map, targ.Cell);
            bool spawned = flyingThing != null && flyingThing.Spawned;
            pawn = launcher as Pawn;
            drafted = pawn?.Drafted ?? false;
            comp = pawn?.GetCompAbilityUserMagic();
            // Own the passenger before native despawn notifications can throw.
            this.origin = origin;
            this.destination = targ.Cell.ToVector3Shifted();
            this.ticksToImpact = this.StartingTicksToImpact;
            this.flyingThing = flyingThing;
            if (spawned)
            {
                try { flyingThing.DeSpawn(); }
                catch (Exception exception)
                {
                    ticksToImpact = 0;
                    Compatibility.TM_Log.Error("Flight", "Takeoff interrupted; retaining passenger for recovery: " + exception);
                    if (flyingThing.Spawned) { this.flyingThing = null; Destroy(); }
                    return;
                }
            }
            //
            ModOptions.Constants.SetPawnInFlight(true);
            //
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;

            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.destination = targ.Cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            this.ticksToImpact = this.StartingTicksToImpact;
            try { this.Initialize(); }
            catch (Exception exception) { Compatibility.TM_Log.Warning("Flight", "Flight began; visual effect failed: " + exception); }
        }      

        protected override void Tick()
        {
        }
        protected override void TickInterval(int delta)
        {
            if (ticksToImpact <= 0)
            {
                ImpactSomething();
                return;
            }
            //base.Tick();
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
                // Never orphan the flying pawn off-map: land at the closest
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
                    FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.8f, 1.2f));
                }               
                
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    Vector3 shiftVec = this.ExactPosition;
                    shiftVec.x += Rand.Range(-.3f, .3f);
                    shiftVec.z += Rand.Range(-.3f, .3f);
                    TM_MoteMaker.ThrowArcaneMote(shiftVec, base.Map, Rand.Range(.5f, .6f), .1f, .02f, .2f, 200, .3f);
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
                Pawn targetPawn = this.assignedTarget as Pawn;
                bool flag2 = targetPawn != null && targetPawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f;
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
                    Pawn hitPawn = base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn;
                    if (hitPawn != null)
                    {
                        hitThing = hitPawn;
                    }
                }
                bool applyImpact = !impactApplied;
                impactApplied = true;
                if (applyImpact && this.impactDamage.HasValue && hitThing != null && !hitThing.Destroyed)
                {
                    hitThing.TakeDamage(this.impactDamage.Value);
                }

                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                if (this.flyingThing != null && !this.flyingThing.Destroyed && base.Map != null)
                {
                    // Spawn exactly once; fall back to a nearby cell when the
                    // destination is blocked instead of re-spawning the pawn.
                    if (this.flyingThing.Spawned)
                    {
                        // already back on a map (should not happen) — keep it
                    }
                    else TrySafeLanding();
                }

                Pawn p = this.flyingThing as Pawn;
                if (p != null && p.Spawned && p.IsColonist && p.drafter != null)
                {
                    p.drafter.Drafted = this.drafted;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("TM Flight: error while landing, recovering the carrier pawn: " + ex);
                try
                {
                    Pawn p2 = this.flyingThing as Pawn;
                    if (p2 != null && !p2.Spawned && !p2.Destroyed && base.Map != null)
                    {
                        TrySafeLanding();
                    }
                }
                catch
                {
                }
            }
            finally
            {
                if (this.flyingThing == null || this.flyingThing.Destroyed || this.flyingThing.Spawned)
                {
                    ModOptions.Constants.SetPawnInFlight(false);
                    this.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private bool TrySafeLanding()
        {
            if (Map == null || flyingThing == null || flyingThing.Destroyed) return false;
            var passenger = flyingThing as Pawn;
            foreach (var center in new[] { Position, origin.ToIntVec3() })
                foreach (var cell in GenRadial.RadialCellsAround(center, 3.9f, true))
                {
                    if (!cell.InBounds(Map) || !cell.Standable(Map) || cell.GetFirstPawn(Map) != null) continue;
                    if (passenger != null && !Odyssey.TravelEnvironmentSafety.IsSafe(passenger, Map, cell)
                        && !(acceptedDanger && cell == acceptedDestination && Map == acceptedMap && passenger == acceptedPassenger)) continue;
                    if (GenPlace.TryPlaceThing(flyingThing, cell, Map, ThingPlaceMode.Direct)) return true;
                }
            return false;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (flyingThing != null && !flyingThing.Destroyed && !flyingThing.Spawned && !TrySafeLanding()) return;
            base.Destroy(mode);
        }
    }
}
