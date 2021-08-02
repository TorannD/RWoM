using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_DirtDevil : Projectile
    {

        protected new Vector3 origin;
        protected new Vector3 destination;

        private int searchDelay = 10;
        private int age = -1;
        private int duration = 12000;

        protected float speed = 9f;
        protected new int ticksToImpact;

        //protected new Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        Pawn pawn;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int timesToDamage = 3;

        public int weaponDmg = 0;

        private bool initialized = true;

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
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 12000, false);
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.timesToDamage, "timesToDamage", 0, false);
            Scribe_Values.Look<int>(ref this.searchDelay, "searchDelay", 10, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            //Scribe_References.Look<Thing>(ref this.launcher, "launcher", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 1f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            //flyingThing.ThingID += Rand.Range(0, 214).ToString();
            this.initialized = false;
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
            bool spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
            if (comp != null)
            {
                if (comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_pwr").level >= 3)
                {
                    this.speed = 12;
                }
                if (comp.MagicData.MagicPowerSkill_Cantrips.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Cantrips_ver").level >= 4)
                {
                    this.duration = Mathf.RoundToInt(this.duration * 1.25f);
                }
            }
            this.duration = Mathf.RoundToInt(this.duration * comp.arcaneDmg);
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.launcher = launcher;
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            CleanFilth();
            this.destination = FindNearestFilth(this.launcher.DrawPos);
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        public Vector3 FindNearestFilth(Vector3 origin)
        {
            Vector3 destination = default(Vector3);
            List<Thing> filthList = this.Map.listerFilthInHomeArea.FilthInHomeArea;
            Thing closestDirt = null;
            float dirtPos = 0;
            for (int i = 0; i < filthList.Count; i++)
            {
                if (closestDirt != null)
                {
                    float dirtDistance = (filthList[i].DrawPos - origin).magnitude;
                    if (dirtDistance < dirtPos)
                    {
                        dirtPos = dirtDistance;
                        closestDirt = filthList[i];
                    }
                }
                else
                {
                    closestDirt = filthList[i];
                    dirtPos = (filthList[i].DrawPos - origin).magnitude;
                }
            }

            if (closestDirt != null)
            {
                destination = closestDirt.DrawPos;
            }
            else
            {
                this.age = this.duration;
                destination = this.destination;
            }
            return destination;
        }

        public void CleanFilth()
        {
            List<Thing> allThings = new List<Thing>();
            List<Thing> allFilth = new List<Thing>();
            allThings.Clear();
            allFilth.Clear();
            List<IntVec3> cellsAround = GenRadial.RadialCellsAround(this.Position, 1.4f, true).ToList();
            for(int i =0; i < cellsAround.Count; i++)
            {
                allThings = cellsAround[i].GetThingList(this.Map);
                for(int j = 0; j < allThings.Count; j++)
                {
                    if(allThings[j].def.category == ThingCategory.Filth || allThings[j].def.IsFilth)
                    {
                        allFilth.Add(allThings[j]);
                    }
                }
            }
            for(int i = 0; i < allFilth.Count; i++)
            {
                CleanGraphics(allFilth[i]);
                allFilth[i].Destroy(DestroyMode.Vanish);
            }
        }

        public void CleanGraphics(Thing filth)
        {
            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.MicroSparks, this.ExactPosition, this.Map, Rand.Range(.3f, .5f), .6f, .2f, .4f, Rand.Range(-400, -100), .3f, Rand.Range(0,360), Rand.Range(0, 360));
            Vector3 angle = TM_Calc.GetVector(filth.DrawPos, this.ExactPosition);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, filth.DrawPos, this.Map, Rand.Range(.4f, .6f), .1f, .05f, .25f, -200, 2, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), Rand.Range(0,360));
        }

        public override void Tick()
        {
            //base.Tick();

            age++;
            this.searchDelay--;
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBounds(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                bool flag2 = this.ticksToImpact <= 0;
                if (flag2)
                {
                    if (this.age < this.duration)
                    {
                        this.origin = this.ExactPosition;
                        this.destination = FindNearestFilth(this.origin);
                        this.ticksToImpact = this.StartingTicksToImpact;
                    }
                    else
                    {
                        bool flag3 = this.DestinationCell.InBounds(base.Map);
                        if (flag3)
                        {
                            base.Position = this.DestinationCell;
                        }
                        this.ImpactSomething();
                    }
                }
                if (Find.TickManager.TicksGame % 4 == 0)
                {
                    Vector3 rndVec = this.ExactPosition;
                    rndVec.x += Rand.Range(-1f, 1f);
                    rndVec.z += Rand.Range(-1f, 1f);
                    Vector3 angle = TM_Calc.GetVector(rndVec, this.ExactPosition);
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, rndVec, this.Map, Rand.Range(.8f, 1.5f), .1f, .05f, .15f, -300, 2, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), Rand.Range(0, 360));
                }
                if(this.searchDelay < 0)
                {
                    if(this.destination != default(Vector3))
                    {
                        this.searchDelay = Rand.Range(10, 20);
                        CleanFilth();
                    }
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
                Pawn hitPawn;
                bool flag2 = (hitPawn = (base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn)) != null;
                if (flag2)
                {
                    hitThing = hitPawn;
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

            this.Destroy(DestroyMode.Vanish);
        }        
    }
}
