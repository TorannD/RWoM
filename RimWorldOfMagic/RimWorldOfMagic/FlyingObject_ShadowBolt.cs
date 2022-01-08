using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_ShadowBolt : Projectile
    {

        private static readonly Color lightningColor = new Color(160f, 160f, 160f);
        private static readonly Material OrbMat = MaterialPool.MatFrom("Spells/shadowbolt", false);

        protected new Vector3 origin;
        protected new Vector3 destination;
        protected Vector3 direction;
        private float directionAngle;

        private int age = -1;
        private float arcaneDmg = 1;
        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        public Vector3 drawingScale;
        public Vector3 drawingPosition;

        private int pwrVal = 0;
        private int verVal = 0;
        float radius = 1.4f;

        private float proximityRadius = .4f;
        private int proximityFrequency = 6;

        protected float speed = 30f;
        protected new int ticksToImpact;

        protected Thing assignedTarget;
        protected Thing flyingThing;
        Pawn pawn;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

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

        public new  Vector3 ExactPosition
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
            Scribe_Values.Look<Vector3>(ref this.direction, "direction", default(Vector3), false);
            Scribe_Values.Look<float>(ref this.directionAngle, "directionAngle", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 1.4f, false);
            Scribe_Values.Look<int>(ref this.proximityFrequency, "proximityFrequency", 6, false);
            Scribe_Values.Look<float>(ref this.proximityRadius, "proximityRadius", .4f, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 6f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
            }
            GetVector();
            flyingThing.ThingID += Rand.Range(0, 214).ToString();
            this.initialized = false;
        }

        public void GetVector()
        {
            Vector3 heading = (this.destination - this.ExactPosition);
            float distance = heading.magnitude;
            this.direction = heading / distance;
            this.directionAngle = (Quaternion.AngleAxis(90, Vector3.up) * this.direction).ToAngleFlat();
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

            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();

            this.arcaneDmg = comp.arcaneDmg;
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_ShadowBolt, true);
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_ShadowBolt, true);
            //MagicPowerSkill pwr = pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_ShadowBolt.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ShadowBolt_pwr");
            //MagicPowerSkill ver = pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_ShadowBolt.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ShadowBolt_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            
            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    MightPowerSkill mpwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
            //    MightPowerSkill mver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
            //    pwrVal = mpwr.level;
            //    verVal = mver.level;
            //}
            //if (settingsRef.AIHardMode && !pawn.IsColonist)
            //{
            //    pwrVal = 3;
            //    verVal = 3;
            //}
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.speed = this.def.projectile.speed;
            this.proximityRadius += (.4f * verVal);
            this.proximityFrequency -= verVal;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            float distanceAccuracyModifier = (targ.Cell.ToVector3Shifted() - this.pawn.Position.ToVector3Shifted()).MagnitudeHorizontal() *.1f;
            this.destination = targ.Cell.ToVector3Shifted();
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        public override void Tick()
        {
            //base.Tick();
            age++;
            if (this.ticksToImpact >= 0)
            {
                DrawEffects(this.ExactPosition, base.Map);
            }
            this.ticksToImpact--;
            base.Position = this.ExactPosition.ToIntVec3();
            bool flag = !this.ExactPosition.InBounds(base.Map);
            if (flag)
            {
                this.ticksToImpact++;
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                if(Find.TickManager.TicksGame % this.proximityFrequency ==0)
                {
                    DamageThingsAtPosition();
                }                
                bool flag2 = this.ticksToImpact <= 0;
                if (flag2)
                {
                    bool flag3 = this.DestinationCell.InBounds(base.Map);
                    if (flag3)
                    {
                        base.Position = this.DestinationCell;
                    }
                    this.ImpactSomething();
                }
            }
        }

        public void DrawEffects(Vector3 effectVec, Map map)
        {
            effectVec += this.direction;
            effectVec.x += Rand.Range(-0.4f, 0.4f);
            effectVec.z += Rand.Range(-0.4f, 0.4f);
            TM_MoteMaker.ThrowShadowMote(effectVec, map, Rand.Range(.6f, 1f));
        }

        public override void Draw()
        {
            bool flag = this.flyingThing != null;
            if (flag)
            {
                Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                base.Comps_PostDraw();
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
                Pawn pawn2;
                bool flag2 = (pawn2 = (base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn)) != null;
                if (flag2)
                {
                    hitThing = pawn2;
                }
            }        
            if(hitThing != null)
            {
                damageEntities(hitThing, Mathf.RoundToInt(Rand.Range(this.def.projectile.GetDamageAmount(1,null) * .8f, this.def.projectile.GetDamageAmount(1,null) * 1.4f) * this.arcaneDmg));
            }
            TM_MoteMaker.ThrowShadowCleaveMote(this.ExactPosition, this.Map, 2f + (.4f * pwrVal), .05f, .1f, .3f, 0, (5f+ pwrVal), this.directionAngle);
            TorannMagicDefOf.TM_SoftExplosion.PlayOneShot(new TargetInfo(this.ExactPosition.ToIntVec3(), this.pawn.Map, false));
            int num = GenRadial.NumCellsInRadius(1+(.4f* pwrVal));

            Vector3 cleaveVector;
            IntVec3 intVec;
            for (int i = 0; i < num; i++)
            {
                cleaveVector = this.ExactPosition + (Quaternion.AngleAxis(-45, Vector3.up) * ((1.5f + (.5f*pwrVal)) * this.direction));
                intVec = cleaveVector.ToIntVec3() + GenRadial.RadialPattern[i];
                //GenExplosion.DoExplosion(intVec, base.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f * this.def.projectile.GetDamageAmount(1,null), 1.1f * this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);

                if (intVec.IsValid && intVec.InBounds(this.Map))
                {
                    List<Thing> hitList = new List<Thing>();
                    hitList = intVec.GetThingList(base.Map);
                    for (int j = 0; j < hitList.Count; j++)
                    {
                        if (hitList[j] is Pawn && hitList[j] != this.pawn)
                        {
                            damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1,null) * .6f, this.def.projectile.GetDamageAmount(1,null) * .8f) * (float)(1f + .1 * pwrVal) * this.arcaneDmg)));
                        }
                    }
                }
                cleaveVector = this.ExactPosition + (Quaternion.AngleAxis(45, Vector3.up) * ((1.5f + (.5f * pwrVal)) * this.direction));
                intVec = cleaveVector.ToIntVec3() + GenRadial.RadialPattern[i];
                //GenExplosion.DoExplosion(intVec, base.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f * this.def.projectile.GetDamageAmount(1,null), 1.1f * this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);

                if (intVec.IsValid && intVec.InBounds(this.Map))
                {
                    List<Thing> hitList = new List<Thing>();
                    hitList = intVec.GetThingList(base.Map);
                    for (int j = 0; j < hitList.Count; j++)
                    {
                        if (hitList[j] is Pawn && hitList[j] != this.pawn)
                        {
                            damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1,null) * .5f, this.def.projectile.GetDamageAmount(1,null) * .7f) * (float)(1f + .1 * pwrVal) * this.arcaneDmg)));
                        }
                    }
                }
                cleaveVector = this.ExactPosition + ((2 + (.3f * (float)pwrVal)) * this.direction);
                intVec = cleaveVector.ToIntVec3() + GenRadial.RadialPattern[i];
                //GenExplosion.DoExplosion(intVec, base.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f*this.def.projectile.GetDamageAmount(1,null), 1.1f*this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);

                if (intVec.IsValid && intVec.InBounds(this.Map))
                {
                    List<Thing> hitList = new List<Thing>();
                    hitList = intVec.GetThingList(base.Map);
                    for (int j = 0; j < hitList.Count; j++)
                    {
                        if (hitList[j] is Pawn && hitList[j] != this.pawn)
                        {
                            damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1,null) * .5f, this.def.projectile.GetDamageAmount(1,null) * .7f) * (float)(1f + .1 * pwrVal) * this.arcaneDmg)));

                        }
                    }
                }
            }
            this.Destroy(DestroyMode.Vanish);
            //GenExplosion.DoExplosion(base.Position, base.Map, this.radius, TMDamageDefOf.DamageDefOf.TM_DeathBolt, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f*this.def.projectile.GetDamageAmount(1,null), 1.1f*this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);
        }

        public void DamageThingsAtPosition()
        {
            int num = GenRadial.NumCellsInRadius(this.proximityRadius);
            IntVec3 curCell;
            for (int i = 0; i < num; i++)
            {
                curCell = this.ExactPosition.ToIntVec3() + GenRadial.RadialPattern[i];
                List<Thing> hitList = new List<Thing>();
                hitList = curCell.GetThingList(base.Map);
                for (int j = 0; j < hitList.Count; j++)
                {
                    if (hitList[j] is Pawn && hitList[j] != this.pawn && hitList[j].Faction != this.pawn.Faction)
                    {
                        damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1,null) * .4f, this.def.projectile.GetDamageAmount(1,null) * .6f)) * this.arcaneDmg));
                        TM_MoteMaker.ThrowShadowCleaveMote(this.ExactPosition, this.Map, Rand.Range(.2f, .4f), .01f, .2f, .4f, 500, 0, 0);
                        TorannMagicDefOf.TM_Vibration.PlayOneShot(new TargetInfo(this.ExactPosition.ToIntVec3(), pawn.Map, false));
                    }
                }
            }
        }

        public void damageEntities(Thing e, int amt)
        {
            TM_Action.DamageEntities(e, null, amt, 2, TMDamageDefOf.DamageDefOf.TM_Shadow, this.pawn);
            //DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Shadow, amt, 2, (float)(1f - this.directionAngle/360f), this.pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            //bool flag = e != null;
            //if (flag)
            //{
            //    e.TakeDamage(dinfo);
            //}
        }
    }
}
