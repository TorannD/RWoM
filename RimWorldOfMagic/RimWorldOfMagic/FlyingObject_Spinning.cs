using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    //[StaticConstructorOnStartup]
    public class FlyingObject_Spinning : Projectile
    {
        protected new Vector3 origin;
        protected new Vector3 destination;
        private Vector3 direction;

        public float speed = 25f;
        public int spinRate = 0;        //spin rate > 0 makes the object rotate every spinRate Ticks
        private int rotation = 0;
        protected new int ticksToImpact;
        //protected new Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        private bool drafted = false;

        public float force = 1f;

        private bool earlyImpact = false;
        private float impactForce = 0;

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
            Scribe_Values.Look<bool>(ref this.drafted, "drafted", false, false);
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

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        { 
            bool spawned = flyingThing != null && flyingThing.Spawned;            
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
            this.Initialize();
        }        

        public override void Tick()
        {
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
            else if(!this.ExactPosition.ToIntVec3().Walkable(base.Map))
            {
                this.earlyImpact = true;
                this.impactForce = (this.DestinationCell - this.ExactPosition.ToIntVec3()).LengthHorizontal + (this.speed * .2f);
                this.ImpactSomething();
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                if(Find.TickManager.TicksGame % 3 == 0)
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
                else if(this.flyingThing is Corpse)
                {
                    Vector3 arg_2B_0 = this.DrawPos;
                    bool flag4 = !this.DrawPos.ToIntVec3().IsValid;
                    if (flag4)
                    {
                        return;
                    }
                    Corpse corpse = this.flyingThing as Corpse;
                    corpse.InnerPawn.Rotation = this.flyingThing.Rotation;
                    corpse.InnerPawn.Drawer.renderer.RenderPawnAt(this.DrawPos);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
            }
            else
            {
                if (this.spinRate > 0)
                {
                    if (Find.TickManager.TicksGame % this.spinRate == 0)
                    {
                        this.rotation++;
                        if (this.rotation >= 4)
                        {
                            this.rotation = 0;
                        }
                    }
                    if (rotation == 0)
                    {
                        this.Rotation = Rot4.West;
                    }
                    else if (rotation == 1)
                    {
                        this.Rotation = Rot4.North;
                    }
                    else if (rotation == 2)
                    {
                        this.Rotation = Rot4.East;
                    }
                    else
                    {
                        this.Rotation = Rot4.South;
                    }
                }
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, (this.ExactRotation), this.def.DrawMatSingle, 0);
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
            try
            {
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);

                if (this.flyingThing != null)
                {
                    GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
                    if (this.flyingThing is Pawn p)
                    {
                        if (p.IsColonist && this.drafted)
                        {
                            p.drafter.Drafted = true;
                        }
                        if (this.earlyImpact)
                        {
                            damageEntities(p, this.impactForce, DamageDefOf.Blunt);
                            damageEntities(p, this.impactForce, DamageDefOf.Stun);
                        }
                    }
                    else if (flyingThing.def.thingCategories != null && (flyingThing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks) || flyingThing.def.thingCategories.Contains(ThingCategoryDef.Named("StoneChunks"))))
                    {
                        float radius = 4f;
                        Vector3 center = this.ExactPosition;
                        if (this.earlyImpact)
                        {
                            bool wallFlag90neg = false;
                            IntVec3 wallCheck = (center + (Quaternion.AngleAxis(-90, Vector3.up) * this.direction)).ToIntVec3();
                            FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), base.Map);
                            wallFlag90neg = wallCheck.Walkable(base.Map);

                            wallCheck = (center + (Quaternion.AngleAxis(90, Vector3.up) * this.direction)).ToIntVec3();
                            FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), base.Map);
                            bool wallFlag90 = wallCheck.Walkable(base.Map);

                            if ((!wallFlag90 && !wallFlag90neg) || (wallFlag90 && wallFlag90neg))
                            {
                                //fragment energy bounces in reverse direction of travel
                                center = center + ((Quaternion.AngleAxis(180, Vector3.up) * this.direction) * 3);
                            }
                            else if (wallFlag90)
                            {
                                center = center + ((Quaternion.AngleAxis(90, Vector3.up) * this.direction) * 3);
                            }
                            else if (wallFlag90neg)
                            {
                                center = center + ((Quaternion.AngleAxis(-90, Vector3.up) * this.direction) * 3);
                            }

                        }

                        List<IntVec3> damageRing = GenRadial.RadialCellsAround(base.Position, radius, true).ToList();
                        List<IntVec3> outsideRing = GenRadial.RadialCellsAround(base.Position, radius, false).Except(GenRadial.RadialCellsAround(base.Position, radius - 1, true)).ToList();
                        for (int i = 0; i < damageRing.Count; i++)
                        {
                            List<Thing> allThings = damageRing[i].GetThingList(base.Map);
                            for (int j = 0; j < allThings.Count; j++)
                            {
                                if (allThings[j] is Pawn)
                                {
                                    damageEntities(allThings[j], Rand.Range(14, 22), DamageDefOf.Blunt);
                                }
                                else if (allThings[j] is Building)
                                {
                                    damageEntities(allThings[j], Rand.Range(56, 88), DamageDefOf.Blunt);
                                }
                                else
                                {
                                    if (Rand.Chance(.1f))
                                    {
                                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Filth_RubbleRock), damageRing[i], base.Map, ThingPlaceMode.Near);
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < outsideRing.Count; i++)
                        {
                            IntVec3 intVec = outsideRing[i];
                            if (intVec.IsValid && intVec.InBoundsWithNullCheck(base.Map))
                            {
                                Vector3 moteDirection = TM_Calc.GetVector(this.ExactPosition.ToIntVec3(), intVec);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Rubble, this.ExactPosition, base.Map, Rand.Range(.3f, .6f), .2f, .02f, .05f, Rand.Range(-100, 100), Rand.Range(8f, 13f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, this.ExactPosition, base.Map, Rand.Range(.9f, 1.2f), .3f, .02f, Rand.Range(.25f, .4f), Rand.Range(-100, 100), Rand.Range(5f, 8f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                                GenExplosion.DoExplosion(intVec, base.Map, .4f, DamageDefOf.Blunt, pawn, 0, 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null, ThingDefOf.Filth_RubbleRock, .25f, 1, false, null, 0f, 1, 0, false);
                                //FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), base.Map, Rand.Range(.6f, 1f));
                            }
                        }
                        //damageEntities(this.flyingThing, 305, DamageDefOf.Blunt);
                        this.flyingThing.Destroy(DestroyMode.Vanish);
                    }
                    else if ((flyingThing.def.thingCategories != null && (flyingThing.def.thingCategories.Contains(ThingCategoryDefOf.Corpses))) || this.flyingThing is Corpse)
                    {
                        Corpse flyingCorpse = this.flyingThing as Corpse;
                        float radius = 3f;
                        Vector3 center = this.ExactPosition;
                        if (this.earlyImpact)
                        {
                            bool wallFlag90neg = false;
                            IntVec3 wallCheck = (center + (Quaternion.AngleAxis(-90, Vector3.up) * this.direction)).ToIntVec3();
                            FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), base.Map);
                            wallFlag90neg = wallCheck.Walkable(base.Map);

                            wallCheck = (center + (Quaternion.AngleAxis(90, Vector3.up) * this.direction)).ToIntVec3();
                            FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), base.Map);
                            bool wallFlag90 = wallCheck.Walkable(base.Map);

                            if ((!wallFlag90 && !wallFlag90neg) || (wallFlag90 && wallFlag90neg))
                            {
                                //fragment energy bounces in reverse direction of travel
                                center = center + ((Quaternion.AngleAxis(180, Vector3.up) * this.direction) * 3);
                            }
                            else if (wallFlag90)
                            {
                                center = center + ((Quaternion.AngleAxis(90, Vector3.up) * this.direction) * 3);
                            }
                            else if (wallFlag90neg)
                            {
                                center = center + ((Quaternion.AngleAxis(-90, Vector3.up) * this.direction) * 3);
                            }

                        }

                        List<IntVec3> damageRing = GenRadial.RadialCellsAround(base.Position, radius, true).ToList();
                        List<IntVec3> outsideRing = GenRadial.RadialCellsAround(base.Position, radius, false).Except(GenRadial.RadialCellsAround(base.Position, radius - 1, true)).ToList();
                        Filth filth = (Filth)ThingMaker.MakeThing(flyingCorpse.InnerPawn.def.race.BloodDef);
                        for (int i = 0; i < damageRing.Count; i++)
                        {
                            List<Thing> allThings = damageRing[i].GetThingList(base.Map);
                            for (int j = 0; j < allThings.Count; j++)
                            {
                                if (allThings[j] is Pawn)
                                {
                                    damageEntities(allThings[j], Rand.Range(18, 28), DamageDefOf.Blunt);
                                }
                                else if (allThings[j] is Building)
                                {
                                    damageEntities(allThings[j], Rand.Range(56, 88), DamageDefOf.Blunt);
                                }
                                else
                                {
                                    if (Rand.Chance(.05f))
                                    {
                                        if (filth != null)
                                        {
                                            filth = (Filth)ThingMaker.MakeThing(flyingCorpse.InnerPawn.def.race.BloodDef);
                                            GenPlace.TryPlaceThing(filth, damageRing[i], base.Map, ThingPlaceMode.Near);
                                        }
                                        else
                                        {
                                            GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Filth_Blood), damageRing[i], base.Map, ThingPlaceMode.Near);
                                        }
                                    }
                                    if (Rand.Chance(.05f))
                                    {
                                        GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Filth_CorpseBile), damageRing[i], base.Map, ThingPlaceMode.Near);
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < outsideRing.Count; i++)
                        {
                            IntVec3 intVec = outsideRing[i];
                            if (intVec.IsValid && intVec.InBoundsWithNullCheck(base.Map))
                            {
                                Vector3 moteDirection = TM_Calc.GetVector(this.ExactPosition.ToIntVec3(), intVec);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, this.ExactPosition, base.Map, Rand.Range(.3f, .6f), .2f, .02f, .05f, Rand.Range(-100, 100), Rand.Range(4f, 13f), (Quaternion.AngleAxis(Rand.Range(60, 120), Vector3.up) * moteDirection).ToAngleFlat(), 0);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, this.ExactPosition, base.Map, Rand.Range(.9f, 1.2f), .3f, .02f, Rand.Range(.25f, .4f), Rand.Range(-100, 100), Rand.Range(5f, 8f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                                GenExplosion.DoExplosion(intVec, base.Map, .4f, DamageDefOf.Blunt, pawn, 0, 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null, filth.def, .08f, 1, false, null, 0f, 1, 0, false);
                                //FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), base.Map, Rand.Range(.6f, 1f));
                            }
                        }
                        //damageEntities(this.flyingThing, 305, DamageDefOf.Blunt);
                        //this.flyingThing.Destroy(DestroyMode.Vanish);
                    }
                }
                else
                {
                    float radius = 2f;
                    Vector3 center = this.ExactPosition;
                    if (this.earlyImpact)
                    {
                        bool wallFlag90neg = false;
                        IntVec3 wallCheck = (center + (Quaternion.AngleAxis(-90, Vector3.up) * this.direction)).ToIntVec3();
                        FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), base.Map);
                        wallFlag90neg = wallCheck.Walkable(base.Map);

                        wallCheck = (center + (Quaternion.AngleAxis(90, Vector3.up) * this.direction)).ToIntVec3();
                        FleckMaker.ThrowMicroSparks(wallCheck.ToVector3Shifted(), base.Map);
                        bool wallFlag90 = wallCheck.Walkable(base.Map);

                        if ((!wallFlag90 && !wallFlag90neg) || (wallFlag90 && wallFlag90neg))
                        {
                            //fragment energy bounces in reverse direction of travel
                            center = center + ((Quaternion.AngleAxis(180, Vector3.up) * this.direction) * 3);
                        }
                        else if (wallFlag90)
                        {
                            center = center + ((Quaternion.AngleAxis(90, Vector3.up) * this.direction) * 3);
                        }
                        else if (wallFlag90neg)
                        {
                            center = center + ((Quaternion.AngleAxis(-90, Vector3.up) * this.direction) * 3);
                        }

                    }

                    List<IntVec3> damageRing = GenRadial.RadialCellsAround(base.Position, radius, true).ToList();
                    List<IntVec3> outsideRing = GenRadial.RadialCellsAround(base.Position, radius, false).Except(GenRadial.RadialCellsAround(base.Position, radius - 1, true)).ToList();
                    for (int i = 0; i < damageRing.Count; i++)
                    {
                        List<Thing> allThings = damageRing[i].GetThingList(base.Map);
                        for (int j = 0; j < allThings.Count; j++)
                        {
                            if (allThings[j] is Pawn)
                            {
                                damageEntities(allThings[j], Rand.Range(10, 16), DamageDefOf.Blunt);
                            }
                            else if (allThings[j] is Building)
                            {
                                damageEntities(allThings[j], Rand.Range(32, 88), DamageDefOf.Blunt);
                            }
                            else
                            {
                                if (Rand.Chance(.1f))
                                {
                                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Filth_RubbleRock), damageRing[i], base.Map, ThingPlaceMode.Near);
                                }
                            }
                        }
                    }
                    for (int i = 0; i < outsideRing.Count; i++)
                    {
                        IntVec3 intVec = outsideRing[i];
                        if (intVec.IsValid && intVec.InBoundsWithNullCheck(base.Map))
                        {
                            Vector3 moteDirection = TM_Calc.GetVector(this.ExactPosition.ToIntVec3(), intVec);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Rubble, this.ExactPosition, base.Map, Rand.Range(.3f, .6f), .2f, .02f, .05f, Rand.Range(-100, 100), Rand.Range(8f, 13f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, this.ExactPosition, base.Map, Rand.Range(.9f, 1.2f), .3f, .02f, Rand.Range(.25f, .4f), Rand.Range(-100, 100), Rand.Range(5f, 8f), (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), 0);
                            GenExplosion.DoExplosion(intVec, base.Map, .4f, DamageDefOf.Blunt, pawn, 0, 0, SoundDefOf.Pawn_Melee_Punch_HitBuilding, null, null, null, null, .4f, 1, false, null, 0f, 1, 0, false);
                            //FleckMaker.ThrowSmoke(intVec.ToVector3Shifted(), base.Map, Rand.Range(.6f, 1f));
                        }
                    }
                }
                this.Destroy(DestroyMode.Vanish);
            }
            catch
            {
                if (this.flyingThing != null)
                {
                    if (!this.flyingThing.Spawned)
                    {
                        GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
                        Log.Message("catch");
                    }
                }
                this.Destroy(DestroyMode.Vanish);
            }
}

        public void damageEntities(Thing e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, this.launcher, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }
}
