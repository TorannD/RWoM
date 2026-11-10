using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_PsionicLeap : Projectile, IThingHolder, ISuspendableThingHolder
    {
        private readonly ThingOwner<Thing> passenger;
        private Thing carriedCargo;
        private bool returnPending, takingOff, resumeAttack, drafted=true;
        private int nextReturnAttempt;
        public FlyingObject_PsionicLeap() { passenger=new ThingOwner<Thing>(this,true); }
        public bool IsContentsSuspended => true;
        public ThingOwner GetDirectlyHeldThings() => passenger;
        public void GetChildHolders(List<IThingHolder> children) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(children,passenger);
        internal static bool ProtectsMap(Map map)
        {
            var definition=DefDatabase<ThingDef>.GetNamedSilentFail("FlyingObject_PsionicLeap");
            return definition!=null && map?.listerThings?.ThingsOfDef(definition)
                .Any(t=>t is FlyingObject_PsionicLeap leap && (leap.takingOff || leap.passenger.Count>0))==true;
        }

        private int effVal = 0;

        protected new Vector3 origin;
        private Vector3 trueOrigin;
        protected new Vector3 destination;
        private Vector3 trueDestination;
        private Vector3 direction;
        private float trueAngle;

        private bool earlyImpact = false;

        protected float speed = 75f;

        protected new int ticksToImpact = 60;

        protected Thing assignedTarget;
        protected Thing flyingThing;
        public DamageInfo? impactDamage;

        public bool damageLaunched = true;
        public bool explosion = false;
        public int weaponDmg = 0;

        Pawn pawn;
        Thing oldjobTarget = null;

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
            Scribe_Values.Look(ref returnPending,"psionicReturnPending");
            Scribe_Values.Look(ref resumeAttack,"psionicResumeAttack");
            Scribe_Values.Look(ref drafted,"psionicDrafted",true);
            Scribe_References.Look(ref carriedCargo,"psionicCarriedCargo");
            Scribe_Values.Look(ref trueOrigin,"psionicTrueOrigin");
            Scribe_Values.Look(ref trueDestination,"psionicTrueDestination");
            Scribe_Values.Look(ref trueAngle,"psionicTrueAngle");
            Scribe_References.Look(ref oldjobTarget,"psionicOldTarget");
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            if(Scribe.mode==LoadSaveMode.PostLoadInit && flyingThing!=null && !passenger.Contains(flyingThing))
            {
                if(flyingThing.Spawned || flyingThing.holdingOwner!=null)
                {flyingThing=null;returnPending=true;}
                else if(!passenger.TryAdd(flyingThing,false))
                    throw new InvalidOperationException("Psionic leap cannot restore its saved passenger.");
            }
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.3f, .5f), .1f, 0f, .1f, 0, 4f, this.trueAngle, this.trueAngle);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.5f, .6f), .1f, .04f, .1f, 0, 7f, this.trueAngle, this.trueAngle);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.7f, .8f), .1f, .08f, .1f, 0, 10f, this.trueAngle, this.trueAngle);
            }
            //flyingThing.ThingID += Rand.Range(0, 2147).ToString();
        }

        private Pawn skillOwner;
        internal void SetSkillOwner(Pawn owner) => skillOwner=owner;

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
            if(passenger.Count!=0 || !Spawned || flyingThing?.Spawned!=true || flyingThing.Map!=Map ||
                launcher is not Pawn body || !targ.IsValid || !targ.Cell.InBounds(Map) || targ.Cell==body.Position)
            {Destroy();return;}
            pawn=body;
            this.oldjobTarget = pawn.CurJob?.targetA.Thing;
            //Log.Message("pre leap target is " + this.oldjobTarget.LabelShort);
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            this.effVal = TM_Calc.GetSkillEfficiencyLevel(skillOwner ?? pawn, TorannMagicDefOf.TM_PsionicAugmentation, false); //comp.MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicAugmentation_eff").level;
            var start=body.Position;
            drafted=body.Drafted;trueOrigin=origin;takingOff=true;
            try
            {
                var cargo=body.carryTracker?.CarriedThing;
                if(cargo!=null)
                {
                    if(body.inventory==null || !body.carryTracker.innerContainer.TryTransferToContainer(cargo,body.inventory.innerContainer,false))
                    {Destroy();return;}
                    carriedCargo=cargo;
                }
                flyingThing.DeSpawn();
                if(!passenger.TryAdd(flyingThing,false))
                {
                    if(!flyingThing.Spawned && flyingThing.holdingOwner==null)GenSpawn.Spawn(flyingThing,start,Map);
                    Destroy();return;
                }
                this.flyingThing=flyingThing;
            }
            catch
            {
                if(!flyingThing.Destroyed && !flyingThing.Spawned && flyingThing.holdingOwner==null)GenSpawn.Spawn(flyingThing,start,Map);
                throw;
            }
            finally {takingOff=false;}
            this.origin = origin;
            this.trueOrigin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.trueDestination = targ.Cell.ToVector3Shifted();            
            this.direction = GetVector(this.trueOrigin.ToIntVec3(), targ.Cell);
            this.trueAngle = (Quaternion.AngleAxis(90, Vector3.up) * this.direction).ToAngleFlat();
            this.destination = targ.Cell.ToVector3Shifted();         
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        protected override void TickInterval(int delta) { }

        protected override void Tick()
        {
            if(returnPending)
            {
                if(Find.TickManager.TicksGame>=nextReturnAttempt)Destroy();
                return;
            }
            //base.Tick();
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                this.Destroy(DestroyMode.Vanish);
            }
            else if (!this.ExactPosition.ToIntVec3().Walkable(base.Map))
            {
                this.earlyImpact = true;
                this.ImpactSomething();
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.8f, 1.2f));
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
                bool flag3 = this.damageLaunched;
                if (flag3 && hitThing!=null)
                {
                    hitThing.TakeDamage(this.impactDamage.Value);
                }
                
                bool flag4 = this.explosion;
                if (flag4)
                {
                    GenExplosion.DoExplosion(base.Position, base.Map, 0.9f, DamageDefOf.Stun, this, -1, 0, null, null, null, null, null, 0f, 1, null, null, 0, false, null, 0f, 1, 0f, false);
                }
            }
            TM_MoteMaker.MakePowerBeamMotePsionic(this.ExactPosition.ToIntVec3(), this.Map, 10f, 2f, .7f, .1f, .6f);
            //GenExplosion.DoExplosion(this.ExactPosition.ToIntVec3(), this.Map, 1.7f, TMDamageDefOf.DamageDefOf.TM_PsionicInjury, this.pawn, Rand.Range(8, 12) + 2*this.effVal, 0, this.def.projectile.soundExplode, def, null, null, null, 0f, 1, false, null, 0f, 1, 0.0f, false);
            SearchForTargets(base.Position, 1.7f, this.Map);
            resumeAttack=true;
            this.Destroy(DestroyMode.Vanish);
        }

        private bool ReleaseNear(IntVec3 center)
        {
            if(passenger.Count==0)return true;
            var body=passenger[0];
            foreach(var cell in GenRadial.RadialCellsAround(center,3.9f,true))
            {
                if(!cell.InBounds(Map) || !cell.Standable(Map) || cell.GetFirstPawn(Map)!=null)continue;
                try
                {
                    if(passenger.TryDrop(body,cell,Map,ThingPlaceMode.Direct,out _,playDropSound:false))return true;
                }
                catch
                {
                    if(!body.Destroyed && !body.Spawned && body.holdingOwner==null)passenger.TryAdd(body,false);
                    throw;
                }
            }
            return false;
        }

        public override void Destroy(DestroyMode mode=DestroyMode.Vanish)
        {
            returnPending=true;
            if(passenger.Count>0)
            {
                var body=passenger[0] as Pawn;
                if(!Spawned || Map.Disposed || (!ReleaseNear(Position) && !ReleaseNear(trueOrigin.ToIntVec3())))
                {nextReturnAttempt=Find.TickManager.TicksGame+60;return;}
                if(body?.IsColonist==true && body.drafter!=null)body.drafter.Drafted=drafted;
                if(resumeAttack && body?.IsColonist==true && body.Drafted && !body.Dead && !body.Downed &&
                    oldjobTarget?.Spawned==true && oldjobTarget.Map==body.Map && !oldjobTarget.Destroyed)
                    body.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.AttackMelee,oldjobTarget),JobTag.DraftedOrder);
                if(carriedCargo!=null && body?.carryTracker!=null && body.carryTracker.CarriedThing==null &&
                    body.inventory?.innerContainer.Contains(carriedCargo)==true)
                    body.inventory.innerContainer.TryTransferToContainer(carriedCargo,body.carryTracker.innerContainer,false);
            }
            flyingThing=null;
            base.Destroy(mode);
        }

        public void SearchForTargets(IntVec3 center, float radius, Map map)
        {
            Pawn victim = null;
            IntVec3 curCell;
            IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(center, radius, true);
            for (int i = 0; i < targets.Count(); i++)
            {
                victim = null;
                curCell = targets.ToArray<IntVec3>()[i];
                if (curCell.InBoundsWithNullCheck(this.Map) && curCell.IsValid)
                {
                    victim = curCell.GetFirstPawn(map);
                }

                if (victim != null && victim != this.pawn && victim.Faction != this.pawn.Faction)
                {
                    DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_PsionicInjury, Rand.Range(8,12) + (2 * this.effVal), 0, (float)-1, this.pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                    victim.TakeDamage(dinfo);                    
                }
                targets.GetEnumerator().MoveNext();
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
                    pawn.Drawer.renderer.RenderPawnAt(this.DrawPos);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 25);
                }
                base.Comps_PostDraw();
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            return direction;
        }
    }
}
