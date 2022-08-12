using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_SpiritStorm : Projectile
    {

        protected new Vector3 origin;
        protected new Vector3 destination;

        private int searchDelay = 10;
        private int age = -1;
        private int duration = 1000;

        protected float speed = 6f;
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

        public float radius = 4;
        public float spellDamage = 5;
        public int destinationTick = 0;
        public int frenzyBonus = 0;

        public Vector3 ManualDestination = default(Vector3);
        public bool PlayerTargetSet = false;

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
            Scribe_Values.Look<int>(ref this.duration, "duration", 600, false);
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
            Scribe_Values.Look<float>(ref this.radius, "radius", 4);
            Scribe_Values.Look<float>(ref this.spellDamage, "spellDamage", 5);
            Scribe_Values.Look<int>(ref this.frenzyBonus, "frenzyBonus", 0);
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
            if(pawn != null && pawn.story != null && pawn.story.adulthood != null && pawn.story.adulthood.identifier == "tm_vengeful_spirit")
            {
                frenzyBonus = 8;
            }
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            this.duration = Mathf.RoundToInt(this.duration * comp.arcaneDmg);
            this.radius = this.def.projectile.explosionRadius + comp.MagicData.GetSkill_Versatility(TorannMagicDefOf.TM_SpiritStorm).level;
            this.spellDamage = this.def.projectile.GetDamageAmount(1f) * (1f + (.12f * comp.MagicData.GetSkill_Power(TorannMagicDefOf.TM_SpiritStorm).level)) * comp.arcaneDmg;
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
            StormEffects();
            this.destination = FindNearestTarget(this.launcher.DrawPos);
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        public Vector3 FindNearestTarget(Vector3 origin)
        {
            Vector3 destination = default(Vector3);
            if (PlayerTargetSet)
            {
                if (this.ExactPosition.ToIntVec3() == ManualDestination.ToIntVec3())
                {
                    PlayerTargetSet = false;
                }
                destination = ManualDestination;                
            }
            else
            {
                Pawn nearestEnemey = TM_Calc.FindNearestEnemy(this.Map, this.ExactPosition.ToIntVec3(), this.launcher.Faction, false, false);
                if(nearestEnemey != null)
                {
                    destination = nearestEnemey.DrawPos;
                }
                else
                {
                    Vector3 rndPos = base.Position.ToVector3Shifted();
                    rndPos.x += Rand.Range(-5f, 5f);
                    rndPos.z += Rand.Range(-5f, 5f);
                    destination = rndPos;
                }
            }
            return destination;
        }

        public void StormEffects()
        {
            List<Pawn> allPawns = TM_Calc.FindAllPawnsAround(this.Map, this.ExactPosition.ToIntVec3(), radius);
            if (allPawns != null && allPawns.Count > 0)
            {
                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn p = allPawns[i];
                    if (p != null && !p.Dead)
                    {
                        TM_Action.DamageEntities(p, null, spellDamage, TMDamageDefOf.DamageDefOf.TM_Spirit, this.launcher);
                    }
                    StormGraphics(p);
                }
            }
        }

        public void StormGraphics(Thing filth)
        {
            float angle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(filth.DrawPos, this.ExactPosition)).ToAngleFlat();
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Regen, filth.DrawPos, this.Map, Rand.Range(.2f, .4f), .8f, .2f, .4f, Rand.Range(-400, -100), 1.9f, angle, Rand.Range(0, 360));
        }

        public override void Tick()
        {
            //base.Tick();

            age++;
            this.searchDelay--;
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
                if (this.ticksToImpact <= 0)
                {
                    if (this.age < this.duration)
                    {
                        this.origin = this.ExactPosition;
                        this.destination = FindNearestTarget(this.origin);
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
                if(this.destinationTick < Find.TickManager.TicksGame && this.age < this.duration)
                {
                    this.destinationTick = Find.TickManager.TicksGame + 150;
                    this.origin = this.ExactPosition;
                    this.destination = FindNearestTarget(this.origin);
                    this.ticksToImpact = this.StartingTicksToImpact;
                }
                if (Find.TickManager.TicksGame % 6 == 0)
                {
                    Vector3 rndVec = this.ExactPosition;
                    rndVec.x += Rand.Range(-3f, 3f);
                    rndVec.z += Rand.Range(-3f, 3f);
                    Vector3 angle = TM_Calc.GetVector(rndVec, this.ExactPosition);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PurpleSmoke, rndVec, this.Map, Rand.Range(.8f, 1.5f), .3f, .1f, .25f, -300, 2, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), Rand.Range(0, 360));
                    Effecter effecter = TorannMagicDefOf.TM_SpiritStormED.Spawn();
                    effecter.scale *= (radius / 4f);
                    effecter.Trigger(new TargetInfo(this.ExactPosition.ToIntVec3(), this.Map, false), new TargetInfo(this.ExactPosition.ToIntVec3(), this.Map, false));
                    effecter.Cleanup();
                }
                if(this.searchDelay < 0)
                {
                    if(this.destination != default(Vector3))
                    {
                        this.searchDelay = Rand.Range(25, 35) - frenzyBonus;
                        StormEffects();
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
