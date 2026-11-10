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
    public class FlyingObject_DragonStrike : Projectile, IThingHolder, ISuspendableThingHolder
    {
        private readonly ThingOwner<Thing> passenger;
        private bool returnPending, takingOff;
        private bool impactApplied;
        private int nextReturnAttempt;
        private Thing carriedCargo;
        private bool PrepareCargo(Pawn body)
        {
            var cargo = body.carryTracker?.CarriedThing;
            if (cargo == null) return true;
            if (body.inventory == null || !body.carryTracker.innerContainer.TryTransferToContainer(cargo, body.inventory.innerContainer, false)) return false;
            carriedCargo = cargo;
            return true;
        }
        private void RestoreCargo(Pawn body)
        {
            if (carriedCargo != null && body?.carryTracker != null && body.carryTracker.CarriedThing == null &&
                body.inventory?.innerContainer.Contains(carriedCargo) == true)
                body.inventory.innerContainer.TryTransferToContainer(carriedCargo, body.carryTracker.innerContainer, false);
        }
        public FlyingObject_DragonStrike() { passenger = new ThingOwner<Thing>(this, true); }
        public bool IsContentsSuspended => true;
        public ThingOwner GetDirectlyHeldThings() => passenger;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children, passenger);
        internal static bool ProtectsMap(Map map)
        {
            var definition = DefDatabase<ThingDef>.GetNamedSilentFail("FlyingObject_DragonStrike");
            return definition != null && map?.listerThings?.ThingsOfDef(definition)
                .Any(t => t is FlyingObject_DragonStrike flight && (flight.takingOff || flight.passenger.Count > 0)) == true;
        }
        protected new Vector3 origin;

        protected new Vector3 destination;

        protected float speed = 40f;
        private bool drafted = false;
        private int verVal = 0;

        protected new int ticksToImpact;

        protected Thing assignedTarget = null;
        protected Thing flyingThing;

        private float distanceToTarget = 0;
        private float damageMultiplier = 1;
        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int weaponDmg = 0;

        Pawn pawn;
        CompAbilityUserMight comp;
        private Pawn skillOwner;
        internal void SetSkillOwner(Pawn owner) => skillOwner = owner;
        // DamageInfo itself is not a deep-scribed object. Keep the launch packet intact.
        private DamageDef savedDamageDef;
        private Thing savedInstigator;
        private ThingDef savedWeapon;
        private float savedAmount, savedPenetration, savedAngle;
        private DamageInfo.SourceCategory savedCategory;

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
            Scribe_Values.Look(ref returnPending, "dragonReturnPending");
            Scribe_Values.Look(ref impactApplied, "dragonImpactApplied");
            Scribe_References.Look(ref carriedCargo, "dragonCarriedCargo");
            Scribe_Values.Look(ref drafted, "dragonDrafted");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                savedDamageDef = impactDamage?.Def;
                savedInstigator = impactDamage?.Instigator;
                savedWeapon = impactDamage?.Weapon;
                savedAmount = impactDamage?.Amount ?? 0;
                savedPenetration = impactDamage?.ArmorPenetrationInt ?? 0;
                savedAngle = impactDamage?.Angle ?? -1;
                savedCategory = impactDamage?.Category ?? DamageInfo.SourceCategory.ThingOrUnknown;
            }
            Scribe_Defs.Look(ref savedDamageDef, "dragonImpactDef");
            Scribe_References.Look(ref savedInstigator, "dragonImpactInstigator");
            Scribe_Defs.Look(ref savedWeapon, "dragonImpactWeapon");
            Scribe_Values.Look(ref savedAmount, "dragonImpactAmount");
            Scribe_Values.Look(ref savedPenetration, "dragonImpactPenetration");
            Scribe_Values.Look(ref savedAngle, "dragonImpactAngle", -1f);
            Scribe_Values.Look(ref savedCategory, "dragonImpactCategory", DamageInfo.SourceCategory.ThingOrUnknown);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                impactDamage = savedDamageDef == null ? (DamageInfo?)null :
                    new DamageInfo(savedDamageDef, savedAmount, savedPenetration, savedAngle, savedInstigator, null, savedWeapon, savedCategory);
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<float>(ref this.distanceToTarget, "distanceToTarget", 0, false);
            Scribe_Values.Look<float>(ref this.damageMultiplier, "damageMultiplier", 1, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && flyingThing != null && !passenger.Contains(flyingThing))
            {
                if (flyingThing.Spawned || flyingThing.holdingOwner != null)
                { flyingThing = null; returnPending = true; }
                else if (!passenger.TryAdd(flyingThing, false))
                    throw new InvalidOperationException("Dragon strike cannot restore its saved passenger.");
            }
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            if(distanceToTarget > 12)
            {
                damageMultiplier = .5f;
            }
            else if(distanceToTarget > 8)
            {
                damageMultiplier = .8f;
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
            if (passenger.Count != 0 || !Spawned || flyingThing?.Spawned != true || flyingThing.Map != Map ||
                launcher is not Pawn || !targ.IsValid || !targ.Cell.InBounds(Map))
            { Destroy(); return; }
            bool spawned = flyingThing.Spawned;            
            pawn = launcher as Pawn;
            drafted = pawn.Drafted;
            comp = pawn.GetCompAbilityUserMight();
            verVal = TM_Calc.GetSkillVersatilityLevel(skillOwner ?? pawn, TorannMagicDefOf.TM_DragonStrike, true);
            //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_DragonStrike, "TM_DragonStrike", "_ver", true);
            //this.verVal = comp.MightData.MightPowerSkill_DragonStrike.FirstOrDefault((MightPowerSkill x) => x.label == "TM_DragonStrike_ver").level;
            //if (comp.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    verVal = mver.level;
            //}
            if (spawned)
            {               
                var start = flyingThing.Position;
                takingOff = true;
                try
                {
                    if (!PrepareCargo(pawn)) { Destroy(); return; }
                    flyingThing.DeSpawn();
                    if (!passenger.TryAdd(flyingThing, false))
                    {
                        if (!flyingThing.Spawned && flyingThing.holdingOwner == null) GenSpawn.Spawn(flyingThing, start, Map);
                        RestoreCargo(pawn);
                        Destroy(); return;
                    }
                    this.flyingThing = flyingThing;
                }
                catch
                {
                    if (!flyingThing.Destroyed && !flyingThing.Spawned && flyingThing.holdingOwner == null) GenSpawn.Spawn(flyingThing, start, Map);
                    if (flyingThing.Spawned) RestoreCargo(pawn);
                    throw;
                }
                finally { takingOff = false; }
            }
            //
            ModOptions.Constants.SetPawnInFlight(true);
            //
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            this.distanceToTarget = (targ.Cell - origin.ToIntVec3()).LengthHorizontal;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;                
            }
            this.destination = targ.Cell.ToVector3();
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }      

        protected override void Tick()
        {
        }
        protected override void TickInterval(int delta)
        {
            if (returnPending)
            {
                if (Find.TickManager.TicksGame >= nextReturnAttempt) Destroy();
                return;
            }
            //base.Tick();
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                if(Find.TickManager.TicksGame % 2 == 0)
                {
                    FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.8f, 1.2f));
                }               
         
                //if(Find.TickManager.TicksGame % 10 == 0 && this.assignedTarget != null)
                //{
                //    this.origin = this.ExactPosition;
                //    this.destination = this.assignedTarget.DrawPos;
                //    this.ticksToImpact = this.StartingTicksToImpact;
                //}

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
                    //pawn.Drawer.DrawAt(this.DrawPos);  
                    
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
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
            bool flag = this.assignedTarget != null;
            if (flag)
            {
                Pawn targetPawn = this.assignedTarget as Pawn;
                bool flag2 = targetPawn != null && targetPawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.1f;
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
            if (impactApplied || returnPending) { Destroy(); return; }
            impactApplied = true;
            bool flag = hitThing == null;
            if (flag)
            {
                Pawn hitPawn;
                bool flag2 = (hitPawn = (base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn)) != null;
                if (flag2)
                {
                    hitThing = hitPawn;
                }
            }
            bool hasValue = this.impactDamage.HasValue && hitThing != null && hitThing is Pawn;
            if (hasValue)
            {
                Pawn hitPawn = hitThing as Pawn;
                this.impactDamage.Value.SetAmount(this.impactDamage.Value.Amount * this.damageMultiplier);
                try
                {
                    hitPawn.TakeDamage(this.impactDamage.Value);
                    if (distanceToTarget <= 6f && hitThing is Pawn)
                    {
                        if (!hitPawn.DestroyedOrNull() && !hitPawn.Downed && !hitPawn.Dead)
                        {
                            Vector3 launchVector = TM_Calc.GetVector(this.origin, hitThing.Position.ToVector3());
                            IntVec3 projectedPosition = hitThing.Position + ((10f - distanceToTarget) * (1 + (.15f * verVal)) * launchVector).ToIntVec3();
                            if (projectedPosition.IsValid && projectedPosition.InBoundsWithNullCheck(hitThing.Map))
                            {
                                LaunchFlyingObect(projectedPosition, hitPawn, Mathf.RoundToInt(10f - distanceToTarget));
                            }
                        }
                    }
                }
                catch (NullReferenceException ex)
                {

                }
            }
            Destroy(DestroyMode.Vanish);
        }

        private bool ReleaseNear(IntVec3 center)
        {
            if (passenger.Count == 0) return true;
            var body = passenger[0];
            foreach (var cell in GenRadial.RadialCellsAround(center, 3.9f, true))
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map) || cell.GetFirstPawn(Map) != null) continue;
                try
                {
                    if (passenger.TryDrop(body, cell, Map, ThingPlaceMode.Direct, out _, playDropSound: false)) return true;
                }
                catch
                {
                    if (!body.Destroyed && !body.Spawned && body.holdingOwner == null) passenger.TryAdd(body, false);
                    throw;
                }
            }
            return false;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            returnPending = true;
            if (passenger.Count > 0)
            {
                var body = passenger[0] as Pawn;
                if (!Spawned || Map.Disposed || (!ReleaseNear(Position) && !ReleaseNear(origin.ToIntVec3())))
                { nextReturnAttempt = Find.TickManager.TicksGame + 60; return; }
                if (body?.IsColonist == true && body.drafter != null) body.drafter.Drafted = drafted;
                RestoreCargo(body);
            }
            flyingThing = null;
            ModOptions.Constants.SetPawnInFlight(false);
            base.Destroy(mode);
        }
        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force)
        {
            bool flag = targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), pawn.Position, pawn.Map);
                    flyingObject.speed = 25 + force;
                    flyingObject.Launch(pawn, targetCell, pawn);
                }
            }
        }
    }
}
