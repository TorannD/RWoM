using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_PsionicDash : Projectile
    {
        private int verVal = 0;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;

        protected new Vector3 origin;
        private Vector3 trueOrigin;
        protected new Vector3 destination;
        private Vector3 trueDestination;
        private Vector3 direction;
        private float trueAngle;

        private int dashStep = 0;
        private bool earlyImpact = false;
        private int drawTicks = 300;
        private bool shouldDrawPawn = true;

        protected float speed = 15f;

        protected new int ticksToImpact = 60;

        protected Thing assignedTarget;
        protected Thing flyingThing;
        public DamageInfo? impactDamage;

        public bool damageLaunched = true;
        public bool explosion = false;
        public int weaponDmg = 0;
        private bool drafted = false;
        Pawn pawn;

        //step variables
        private float sideForwardMagnitude = 2f;
        private float sideMagnitude = 3f;
        private float explosiveMagnitude = 1f;

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
            Scribe_Values.Look<Vector3>(ref this.trueOrigin, "trueOrigin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.trueDestination, "trueDestination", default(Vector3), false);            
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look<int>(ref this.weaponDmg, "weaponDmg", 0, false);
            Scribe_Values.Look<int>(ref this.dashStep, "dashStep", 0, false);
            Scribe_Values.Look<float>(ref this.trueAngle, "trueAngle", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_PsionicDash, false);
                pwrVal = TM_Calc.GetSkillPowerLevel(pawn, TorannMagicDefOf.TM_PsionicDash, false);
                //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_PsionicDash, "TM_PsionicDash", "_ver", true);
                //pwrVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_PsionicDash, "TM_PsionicDash", "_pwr", true);
                //this.pwrVal = comp.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicDash_pwr").level;
                //this.verVal = comp.MightData.MightPowerSkill_PsionicDash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicDash_ver").level;
                this.arcaneDmg = comp.mightPwr;
                //if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    this.pwrVal = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr").level;
                //    this.verVal = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level;
                //}                
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
            bool spawned = flyingThing.Spawned;
            pawn = launcher as Pawn;
            if (pawn.Drafted)
            {
                this.drafted = true;
            }
            if (spawned)
            {
                flyingThing.DeSpawn();
            }
            //
            ModOptions.Constants.SetPawnInFlight(true);
            //
            this.origin = origin;
            this.trueOrigin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.speed = 15;
            this.trueDestination = targ.Cell.ToVector3Shifted();            
            this.direction = GetVector(this.trueOrigin.ToIntVec3(), targ.Cell);
            this.trueAngle = (Quaternion.AngleAxis(90, Vector3.up) * this.direction).ToAngleFlat();
            this.destination = this.origin + (this.direction * 3f);         
            this.ticksToImpact = this.StartingTicksToImpact;
            this.Initialize();
        }

        public override void Tick()
        {
            //base.Tick();
            this.drawTicks--;
            if(this.drawTicks <= 0)
            {
                this.shouldDrawPawn = false;
            }
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            //if (flag)
            //{
            //    this.ticksToImpact++;
            //    base.Position = this.ExactPosition.ToIntVec3();
            //    this.Destroy(DestroyMode.Vanish);
            //}
            if (flag || !this.ExactPosition.ToIntVec3().Walkable(base.Map) || this.ExactPosition.ToIntVec3().DistanceToEdge(base.Map) <= 1)
            {
                this.earlyImpact = true;
                this.ImpactSomething();
            }
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.8f, 1.2f));
                if (Find.TickManager.TicksGame % 4 == 0)
                {
                    ApplyDashDamage();
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

        private void ImpactSomething()
        {
            if (this.dashStep == 0 && !this.earlyImpact) //1
            {
                this.shouldDrawPawn = false;
                this.speed = 30;
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.3f, .5f), .1f, 0f, .1f, 0, 4f, this.trueAngle, this.trueAngle);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.5f, .6f), .1f, .04f, .1f, 0, 7f, this.trueAngle, this.trueAngle);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.7f, .8f), .1f, .08f, .1f, 0, 10f, this.trueAngle, this.trueAngle);
                this.origin = this.ExactPosition;
                this.destination = this.origin + (this.direction * 2f);
                this.ticksToImpact = this.StartingTicksToImpact;
                this.dashStep = 1;
            }
            else if (this.dashStep == 1 && !this.earlyImpact) //2
            {
                ExplosiveStep(this.explosiveMagnitude);
                this.dashStep = 2;
            }
            else if (this.dashStep == 2 && !this.earlyImpact) //3
            {
                SideStep(90, this.sideMagnitude/2, this.sideForwardMagnitude);
                this.dashStep = 3;

            }
            else if (this.dashStep == 3 && !this.earlyImpact) //4
            {
                ExplosiveStep(this.explosiveMagnitude);
                this.dashStep = 4;
            }
            else if (this.dashStep == 4 && !this.earlyImpact) //5
            {
                SideStep(-90, this.sideMagnitude, this.sideForwardMagnitude);
                this.dashStep = 5;
            }
            else if (this.dashStep == 5 && !this.earlyImpact) //6 - check for skill upgrades
            {
                ExplosiveStep(this.explosiveMagnitude); 
                if (this.verVal > 0)
                {
                    this.dashStep = 6;
                }
                else
                {
                    this.dashStep = 20;
                }
            }
            else if(this.dashStep == 6 && !this.earlyImpact) //skill step 1
            {
                SideStep(90, this.sideMagnitude, this.sideForwardMagnitude);
                this.dashStep = 7;

            }
            else if (this.dashStep == 7 && !this.earlyImpact)
            {
                ExplosiveStep(this.explosiveMagnitude);
                if (this.verVal > 1)
                {
                    this.dashStep = 8;
                }
                else
                {
                    this.dashStep = 21;
                }
            }
            else if (this.dashStep == 8 && !this.earlyImpact) //skill step 2
            {
                SideStep(-90, this.sideMagnitude, this.sideForwardMagnitude);
                this.dashStep = 9;

            }
            else if (this.dashStep == 9 && !this.earlyImpact)
            {
                ExplosiveStep(this.explosiveMagnitude);
                if (this.verVal > 2)
                {
                    this.dashStep = 10;
                }
                else
                {
                    this.dashStep = 20;
                }
            }
            else if (this.dashStep == 10 && !this.earlyImpact) //skill step 3
            {
                SideStep(90, this.sideMagnitude, this.sideForwardMagnitude);
                this.dashStep = 11;

            }
            else if (this.dashStep == 11 && !this.earlyImpact)
            {
                ExplosiveStep(this.explosiveMagnitude);
                this.dashStep = 21;
            }
            else if (this.dashStep == 20 && !this.earlyImpact) //Recenter
            {
                SideStep(90, this.sideMagnitude / 2, this.sideForwardMagnitude);
                this.dashStep = 22;
            }
            else if(this.dashStep == 21 && !this.earlyImpact)
            {
                SideStep(-90, this.sideMagnitude / 2, this.sideForwardMagnitude);
                this.dashStep = 22;
            }
            else if (this.dashStep == 22 && !this.earlyImpact) //End
            {
                ExplosiveStepFinal(this.explosiveMagnitude/2f);
                this.dashStep = 50;
            }
            else
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
        }

        private void SideStep(float angle, float sideMagnitude, float forwardMagnitude)
        {
            this.shouldDrawPawn = false;
            this.speed = 60;
            this.origin = this.ExactPosition + ((Quaternion.AngleAxis(angle, Vector3.up) * this.direction) * sideMagnitude);
            this.destination = this.origin + (this.direction * forwardMagnitude);
            this.ticksToImpact = this.StartingTicksToImpact;            
        }

        private void ExplosiveStep(float forwardMagnitude)
        {
            this.drawTicks = 120;
            this.speed = 40;
            TM_MoteMaker.MakePowerBeamMotePsionic(base.Position, this.Map, 10f, 2f, .7f, .1f, .6f);
            GenExplosion.DoExplosion(base.Position, this.Map, 1.7f, TMDamageDefOf.DamageDefOf.TM_PsionicInjury, this.pawn, Mathf.RoundToInt(Rand.Range(8, 14) * (1+ .1f * pwrVal) * this.arcaneDmg), 0, this.def.projectile.soundExplode, def, null, null, null, 0f, 1, false, null, 0f, 1, 0.0f, false);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.3f, .5f), .1f, 0f, .1f, 0, 4f, this.trueAngle, this.trueAngle);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.5f, .6f), .1f, .04f, .1f, 0, 7f, this.trueAngle, this.trueAngle);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PsiCurrent, this.ExactPosition, this.Map, Rand.Range(.7f, .8f), .1f, .08f, .1f, 0, 10f, this.trueAngle, this.trueAngle);
            this.origin = this.ExactPosition;
            this.destination = this.origin + (this.direction * forwardMagnitude);
            this.ticksToImpact = this.StartingTicksToImpact;
        }

        private void ExplosiveStepFinal(float forwardMagnitude)
        {
            this.shouldDrawPawn = true;
            this.drawTicks = 60;
            this.speed = 20;
            TM_MoteMaker.MakePowerBeamMotePsionic(base.Position, this.Map, 10f, 2f, .7f, .1f, .6f);
            GenExplosion.DoExplosion(base.Position, this.Map, 1.7f, TMDamageDefOf.DamageDefOf.TM_PsionicInjury, this.pawn, Mathf.RoundToInt(Rand.Range(10, 16) * (1 + .1f * pwrVal) * this.arcaneDmg), 0, this.def.projectile.soundExplode, def, null, null, null, 0f, 1, false, null, 0f, 1, 0.0f, false);
            this.origin = this.ExactPosition;
            this.destination = this.origin + (this.direction * forwardMagnitude);
            this.ticksToImpact = this.StartingTicksToImpact;
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
                if (flag3)
                {
                    hitThing.TakeDamage(this.impactDamage.Value);
                }
                
                bool flag4 = this.explosion;
                if (flag4)
                {
                    GenExplosion.DoExplosion(base.Position, base.Map, 0.9f, DamageDefOf.Stun, this, -1, 0, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                }
            }
            GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
            ModOptions.Constants.SetPawnInFlight(false);
            Pawn p = this.flyingThing as Pawn;
            if (p.IsColonist && this.drafted)
            {
                p.drafter.Drafted = true;
                if (ModOptions.Settings.Instance.cameraSnap)
                {
                    CameraJumper.TryJumpAndSelect(p);
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }

        public void ApplyDashDamage()
        {
            DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_PsionicInjury, Rand.Range(6,10) * (1 + .1f * pwrVal) * this.arcaneDmg, 0, (float)-1, pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);

            bool flag3 = base.Position != default(IntVec3);
            if (flag3)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 intVec = base.Position + GenAdj.AdjacentCells[i];
                    Pawn cleaveVictim = new Pawn();
                    cleaveVictim = intVec.GetFirstPawn(base.Map);
                    if (cleaveVictim != null && cleaveVictim.Faction != pawn.Faction)
                    {
                        cleaveVictim.TakeDamage(dinfo);
                        FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), base.Map);
      
                        //System.Random random = new System.Random();
                        //int rnd = GenMath.RoundRandom(random.Next(0, 100));
                        //if (rnd < (pwrVal * 5))
                        //{
                        //    cleaveVictim.TakeDamage(dinfo2);
                        //    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), base.Map);
                        //}
                    }
                }
            }

        }

        public override void Draw()
        {
            bool flag = this.flyingThing != null;
            if (flag)
            {
                if (this.shouldDrawPawn)
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
                        pawn.Drawer.DrawAt(this.DrawPos);
                    }
                    else
                    {
                        Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 25);
                    }
                    base.Comps_PostDraw();
                }
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
