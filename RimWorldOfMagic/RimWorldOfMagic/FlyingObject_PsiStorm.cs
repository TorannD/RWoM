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
    public class FlyingObject_PsiStorm : Projectile
    {

        protected Vector3 orbPosition;
        protected Vector3 directionToOrb;
        protected IntVec3 centerLoc;

        private int[] ticksTillHeavy = new int[200];

        private List<IntVec3> boltOrigin = new List<IntVec3>();
        private List<IntVec3> boltDestination = new List<IntVec3>();
        private List<Vector3> boltPosition = new List<Vector3>();
        private List<Vector3> boltVector = new List<Vector3>();
        private List<int> boltTick = new List<int>();
        private List<float> boltMagnitude = new List<float>();

        List<IntVec3> strikeCells;

        private int effectsTick = 0;
        private int boltDelayTicks = 10;
        private int nextStrikeGenTick = 0;
        private float magnitudeAdjuster = 1f;
        private float initialOffsetMagnitude = 10f;

        private float directionAngle;

        private int age = -1;
        private int duration = 240;
        private float arcaneDmg = 1;
        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        public Vector3 drawingScale;
        public Vector3 drawingPosition;

        private int pwrVal = 0;
        private int verVal = 0;
        private int effVal = 0;
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

        public override Vector3 DrawPos
        {
            get
            {
                return this.orbPosition;
            }
        }

        public new Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.orbPosition - this.centerLoc.ToVector3Shifted());
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
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
                FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, Rand.Range(1.2f, 1.8f));
            }
            this.boltOrigin = new List<IntVec3>();
            this.boltPosition = new List<Vector3>();
            this.boltDestination = new List<IntVec3>();
            this.boltVector = new List<Vector3>();
            this.strikeCells = new List<IntVec3>();
            this.boltTick = new List<int>();
            this.boltMagnitude = new List<float>();
            this.strikeCells.Clear();
            this.strikeCells = GenRadial.RadialCellsAround(this.centerLoc, 7, true).ToList();
            for(int i =0; i < this.strikeCells.Count(); i++)
            {
                if(!this.strikeCells[i].InBounds(this.pawn.Map) || !this.strikeCells[i].IsValid)
                {
                    this.strikeCells.Remove(this.strikeCells[i]);
                }
            }
            flyingThing.ThingID += Rand.Range(0, 214).ToString();
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
            
            CompAbilityUserMight comp = pawn.GetComp<CompAbilityUserMight>();
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            this.arcaneDmg = comp.mightPwr;
            //MightPowerSkill pwr = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_pwr");
            //MightPowerSkill ver = pawn.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_PsionicStorm.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicStorm_ver");
            //verVal = ver.level;
            //pwrVal = pwr.level;
            verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_PsionicStorm, false);
            pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_PsionicStorm, false);
            if (settingsRef.AIHardMode && !pawn.IsColonist)
            {
                pwrVal = 3;
                verVal = 3;
            }
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            //determine true center, calculate strike radius
            //determine pawn position relative to true center, if pawn is to the right set orb top right of strike radius (multiple of a vector shift ~30deg offset of north and in the direction of the pawn)
            //this position is the orb position, set orb exact position to this (make sure to check if out of bounds, if so, put the orb on the other side, if too high, put orb below)
            //tick checks for pawn status (exists, alive, not downed, performing job)
            //maintains a 'strike interval' that slowly decreases as the spell remains active (to a minimum point mathf.max(minimum interval, strike interval (which changes)
            //if strike time - pick two spots within the strike radius, one is origin, one is destination, get a vector from origin to dest
            //with a small strike delay (1-3 ticks), continue to generate a new lighting strike from the origin to the dest until the position is the same as the destination
            //might need to set an array to handle the lightning bolts, if you want more than 1 at a time
            //if the caster runs out of stamina or psi to sustain the ability, then it ends the job and the psi storm should fade
            //psi storm job does nothing but stand there and subtract energy
            //might use tm_thunderstrike sound to reduce volume of the strike, play thunder offmap each iteration, generate a "cloud" of motes around the orb
            //might need to pull the mesh maker functionality from wizardry

            this.centerLoc = targ.Cell;
            this.impactDamage = newDamageInfo;
            this.speed = 0f;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.Initialize();
            GetOrbOffset();
        }

        private void GetOrbOffset()
        {
            Vector3 offsetVec = default(Vector3);
            if (this.centerLoc.x < this.pawn.Position.x)
            {
                offsetVec.x = .4f;                
            }
            else
            {
                offsetVec.x = -.4f;
            }
            offsetVec.z = .866f;
            this.orbPosition = (this.centerLoc.ToVector3Shifted() + (this.initialOffsetMagnitude * offsetVec));
            if(!this.orbPosition.InBounds(this.pawn.Map) || !this.orbPosition.ToIntVec3().IsValid)
            {
                offsetVec.x *= -1f;
                this.orbPosition = (this.centerLoc.ToVector3Shifted() + (this.initialOffsetMagnitude * offsetVec));
                if (!this.orbPosition.InBounds(this.pawn.Map) || !this.orbPosition.ToIntVec3().IsValid)
                {
                    offsetVec.z *= -1f;
                    this.orbPosition = (this.centerLoc.ToVector3Shifted() + (this.initialOffsetMagnitude * offsetVec));
                    if (!this.orbPosition.InBounds(this.pawn.Map) || !this.orbPosition.ToIntVec3().IsValid)
                    {
                        offsetVec.x *= -1f;
                        this.orbPosition = (this.centerLoc.ToVector3Shifted() + (this.initialOffsetMagnitude * offsetVec));
                        if (!this.orbPosition.InBounds(this.pawn.Map) || !this.orbPosition.ToIntVec3().IsValid)
                        {
                            Log.Message("No valid cell found to begin psionic storm.");
                            this.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
            }
            //this.orbPosition.z = (int)AltitudeLayer.MoteOverhead;
        }

        public override void Tick()
        {
            //base.Tick();
            age++;
            if (!pawn.DestroyedOrNull() && !pawn.Dead && !pawn.Downed && this.age > 0)
            { 
                //if job def is on pawn...
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    DrawEffects(this.orbPosition, this.pawn.Map);
                }

                if(this.nextStrikeGenTick < Find.TickManager.TicksGame)
                {
                    this.nextStrikeGenTick = Find.TickManager.TicksGame + 360;
                    GenerateNewBolt();                    
                }

                DrawBoltMeshes();
            }
            else if(this.age > this.duration)
            {
                Destroy(DestroyMode.Vanish);
            }
            else
            {
                //fade out
            }
        }

        private void DrawBoltMeshes()
        {
            for (int i = 0; i < this.boltPosition.Count(); i++)
            {
                if(this.boltTick[i] < 0)
                {
                    this.boltTick[i] = this.boltDelayTicks;
                    if(this.boltPosition[i].ToIntVec3() == this.boltDestination[i] || ((this.boltDestination[i] - this.boltOrigin[i]).LengthHorizontal <= (this.boltPosition[i].ToIntVec3() - this.boltOrigin[i]).LengthHorizontal))
                    {
                        //clears this instance of a bolt
                        this.boltOrigin.Remove(this.boltOrigin[i]);
                        this.boltDestination.Remove(this.boltDestination[i]);
                        this.boltPosition.Remove(this.boltPosition[i]);
                        this.boltTick.Remove(this.boltTick[i]);
                        this.boltVector.Remove(this.boltVector[i]);
                        this.boltMagnitude.Remove(this.boltMagnitude[i]);
                    }
                    else
                    {
                        //int rnd = Rand.RangeInclusive(0, 2);
                        //if (rnd == 0)
                        //{
                        //    this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.doubleForkLightning, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 1f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        //}
                        //else if (rnd == 1)
                        //{
                        //    this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.singleForkLightning, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 1f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        //}
                        //else if (rnd == 2)
                        //{
                        //    this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.multiForkLightning, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 1f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        //}
                        FleckMaker.ThrowHeatGlow(this.boltPosition[i].ToIntVec3(), this.pawn.Map, 1f);
                        //else if (rnd == 3)
                        //{
                        //    this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.doubleForkLightning, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 2f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        //}
                        //else if (rnd == 4)
                        //{
                        //    this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.standardLightning, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 2f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        //}
                        //else if (rnd == 5)
                        //{
                        //    this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.psiMote, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 2f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        //}
                        this.pawn.Map.weatherManager.eventHandler.AddEvent(new TM_MapMesh(this.pawn.Map, TM_MatPool.standardLightning, this.orbPosition.ToIntVec3(), this.boltPosition[i].ToIntVec3(), 2f, AltitudeLayer.MoteOverhead, 6, 25, 10));
                        MoveBoltPos(i);
                    }
                }
                else
                {
                    this.boltTick[i]--;
                }
            }
        }

        private void MoveBoltPos(int i)
        {
            this.boltPosition[i] = this.boltOrigin[i].ToVector3Shifted() + (this.boltVector[i] * this.boltMagnitude[i]);
            this.boltMagnitude[i] += this.magnitudeAdjuster;
        }

        private void GenerateNewBolt()
        {
            IntVec3 origin = this.strikeCells.RandomElement();
            IntVec3 destination = this.strikeCells.RandomElement();
            this.boltOrigin.Add(origin);
            this.boltDestination.Add(destination);
            this.boltVector.Add(TM_Calc.GetVector(origin, destination));
            this.boltPosition.Add(origin.ToVector3Shifted());
            this.boltTick.Add(0);
            this.boltMagnitude.Add(this.magnitudeAdjuster);
        }

        public void DrawEffects(Vector3 effectVec, Map map)
        {
            effectVec.x += Rand.Range(-0.4f, 0.4f);
            effectVec.z += Rand.Range(-0.4f, 0.4f);
            FleckMaker.ThrowLightningGlow(effectVec, map, Rand.Range(.6f, .9f));
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

        //private void ImpactSomething()
        //{
        //    bool flag = this.assignedTarget != null;
        //    if (flag)
        //    {
        //        Pawn pawn = this.assignedTarget as Pawn;
        //        bool flag2 = pawn != null && pawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f;
        //        if (flag2)
        //        {
        //            this.Impact(null);
        //        }
        //        else
        //        {
        //            this.Impact(this.assignedTarget);
        //        }
        //    }
        //    else
        //    {
        //        this.Impact(null);
        //    }
        //}

        //protected virtual void Impact(Thing hitThing)
        //{
        //    bool flag = hitThing == null;
        //    if (flag)
        //    {
        //        Pawn pawn;
        //        bool flag2 = (pawn = (base.Position.GetThingList(base.Map).FirstOrDefault((Thing x) => x == this.assignedTarget) as Pawn)) != null;
        //        if (flag2)
        //        {
        //            hitThing = pawn;
        //        }
        //    }
        //    if (hitThing != null)
        //    {
        //        damageEntities(hitThing, Mathf.RoundToInt(Rand.Range(this.def.projectile.GetDamageAmount(1, null) * .75f, this.def.projectile.GetDamageAmount(1, null) * 1.25f)));
        //    }
        //    TM_MoteMaker.ThrowShadowCleaveMote(this.ExactPosition, this.Map, 2f + (.4f * pwrVal), .05f, .1f, .3f, 0, (5f + pwrVal), this.directionAngle);
        //    TorannMagicDefOf.TM_SoftExplosion.PlayOneShot(new TargetInfo(this.ExactPosition.ToIntVec3(), this.pawn.Map, false));
        //    int num = GenRadial.NumCellsInRadius(1 + (.4f * pwrVal));

        //    Vector3 cleaveVector;
        //    IntVec3 intVec;
        //    for (int i = 0; i < num; i++)
        //    {
        //        cleaveVector = this.ExactPosition + (Quaternion.AngleAxis(-45, Vector3.up) * ((1.5f + (.5f * pwrVal)) * this.direction));
        //        intVec = cleaveVector.ToIntVec3() + GenRadial.RadialPattern[i];
        //        //GenExplosion.DoExplosion(intVec, base.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f * this.def.projectile.GetDamageAmount(1,null), 1.1f * this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);

        //        if (intVec.IsValid && intVec.InBounds(this.Map))
        //        {
        //            List<Thing> hitList = new List<Thing>();
        //            hitList = intVec.GetThingList(base.Map);
        //            for (int j = 0; j < hitList.Count; j++)
        //            {
        //                if (hitList[j] is Pawn && hitList[j] != this.pawn)
        //                {
        //                    damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1, null) * .6f, this.def.projectile.GetDamageAmount(1, null) * .8f) * (float)(1f + .1 * pwrVal) * this.arcaneDmg)));
        //                }
        //            }
        //        }
        //        cleaveVector = this.ExactPosition + (Quaternion.AngleAxis(45, Vector3.up) * ((1.5f + (.5f * pwrVal)) * this.direction));
        //        intVec = cleaveVector.ToIntVec3() + GenRadial.RadialPattern[i];
        //        //GenExplosion.DoExplosion(intVec, base.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f * this.def.projectile.GetDamageAmount(1,null), 1.1f * this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);

        //        if (intVec.IsValid && intVec.InBounds(this.Map))
        //        {
        //            List<Thing> hitList = new List<Thing>();
        //            hitList = intVec.GetThingList(base.Map);
        //            for (int j = 0; j < hitList.Count; j++)
        //            {
        //                if (hitList[j] is Pawn && hitList[j] != this.pawn)
        //                {
        //                    damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1, null) * .5f, this.def.projectile.GetDamageAmount(1, null) * .7f) * (float)(1f + .1 * pwrVal) * this.arcaneDmg)));
        //                }
        //            }
        //        }
        //        cleaveVector = this.ExactPosition + ((2 + (.3f * (float)pwrVal)) * this.direction);
        //        intVec = cleaveVector.ToIntVec3() + GenRadial.RadialPattern[i];
        //        //GenExplosion.DoExplosion(intVec, base.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f*this.def.projectile.GetDamageAmount(1,null), 1.1f*this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);

        //        if (intVec.IsValid && intVec.InBounds(this.Map))
        //        {
        //            List<Thing> hitList = new List<Thing>();
        //            hitList = intVec.GetThingList(base.Map);
        //            for (int j = 0; j < hitList.Count; j++)
        //            {
        //                if (hitList[j] is Pawn && hitList[j] != this.pawn)
        //                {
        //                    damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1, null) * .5f, this.def.projectile.GetDamageAmount(1, null) * .7f) * (float)(1f + .1 * pwrVal) * this.arcaneDmg)));
        //                }
        //            }
        //        }
        //    }
        //    this.Destroy(DestroyMode.Vanish);
        //    //GenExplosion.DoExplosion(base.Position, base.Map, this.radius, TMDamageDefOf.DamageDefOf.TM_DeathBolt, this.launcher as Pawn, Mathf.RoundToInt((Rand.Range(.6f*this.def.projectile.GetDamageAmount(1,null), 1.1f*this.def.projectile.GetDamageAmount(1,null)) + (5f * pwrVal)) * this.arcaneDmg), this.def.projectile.soundExplode, def, null, null, 0f, 1, false, null, 0f, 0, 0.0f, true);
        //}

        //public void DamageThingsAtPosition()
        //{
        //    int num = GenRadial.NumCellsInRadius(this.proximityRadius);
        //    IntVec3 curCell;
        //    for (int i = 0; i < num; i++)
        //    {
        //        curCell = this.ExactPosition.ToIntVec3() + GenRadial.RadialPattern[i];
        //        List<Thing> hitList = new List<Thing>();
        //        hitList = curCell.GetThingList(base.Map);
        //        for (int j = 0; j < hitList.Count; j++)
        //        {
        //            if (hitList[j] is Pawn && hitList[j] != this.pawn)
        //            {
        //                damageEntities(hitList[j], Mathf.RoundToInt((Rand.Range(this.def.projectile.GetDamageAmount(1, null) * .2f, this.def.projectile.GetDamageAmount(1, null) * .3f)) * this.arcaneDmg));
        //                TM_MoteMaker.ThrowShadowCleaveMote(this.ExactPosition, this.Map, Rand.Range(.2f, .4f), .01f, .2f, .4f, 500, 0, 0);
        //                TorannMagicDefOf.TM_Vibration.PlayOneShot(new TargetInfo(this.ExactPosition.ToIntVec3(), pawn.Map, false));
        //            }
        //        }
        //    }
        //}

        public void damageEntities(Thing e, int amt)
        {
            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Shadow, amt, 2, (float)(1f - this.directionAngle / 360f), null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }
}
