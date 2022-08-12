using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_Whirlwind : Projectile
    {

        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material cleavingMat = MaterialPool.MatFrom("Spells/cleave", ShaderDatabase.Transparent, FlyingObject_Whirlwind.cleaveColor);

        private static int verVal;
        private static int pwrVal;

        protected new Vector3 origin;

        protected new Vector3 destination;

        protected float speed = 40f;

        protected new int ticksToImpact;

        //protected Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;

        public DamageInfo? impactDamage;

        public bool damageLaunched = true;

        public bool explosion = false;

        public int timesToDamage = 3;

        public int weaponDmg = 0;

        Pawn pawn;

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
            Scribe_Values.Look<int>(ref this.timesToDamage, "timesToDamage", 0, false);
            Scribe_Values.Look<int>(ref this.weaponDmg, "weaponDmg", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            //Scribe_Deep.Look<Thing>(ref this.launcher, "launcher", new object[0]);             
            //Scribe_References.Look<Thing>(ref this.launcher, "launcher", false);
            //Scribe_References.Look<Thing>(ref this.flyingThing, "flyingThing", false);
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
                weaponDmg = GetWeaponDmg(pawn);
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (!pawn.IsColonist && settingsRef.AIHardMode)
                {
                    weaponDmg += 10;
                }
            }
            //flyingThing.ThingID += Rand.Range(0, 214).ToString();
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
            if (spawned)
            {
                flyingThing.DeSpawn();
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
            else
            {
                base.Position = this.ExactPosition.ToIntVec3();
                FleckMaker.ThrowDustPuff(base.Position, base.Map, Rand.Range(0.8f, 1.2f));
                if (Find.TickManager.TicksGame % 2 == 0)
                {
                    DoWhirlwindDamage();
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

        public void DoWhirlwindDamage()
        {
            if (weaponDmg != 0)
            {
                DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, weaponDmg, 0, (float)-1, pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                ApplyWhirlwindDamage(dinfo);
            }
            else
            {
                Log.Warning("failed to do whirlwind");
            }
        }

        public void ApplyWhirlwindDamage(DamageInfo dinfo)
        {

            bool flag = !dinfo.InstantPermanentInjury;
            if (flag)
            {
                bool flag2 = dinfo.Instigator != null;
                if (flag2)
                {
                    int num2 = Mathf.RoundToInt(dinfo.Amount);
                    bool flag3 = pawn != null && base.Position != default(IntVec3);
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
                                CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
                                verVal = TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_Whirlwind);
                                //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_Whirlwind, "TM_Whirlwind", "_ver", true);
                                //MightPowerSkill ver = comp.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_ver");
                                //verVal = ver.level;
                                //if(pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                                //{
                                //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                                //    verVal = mver.level;
                                //}
                                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Whirlwind, weaponDmg, 0, (float)-1, pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                System.Random random = new System.Random();
                                int rnd = GenMath.RoundRandom(random.Next(0, 100));
                                if (rnd < (verVal * 5))
                                {
                                    cleaveVictim.TakeDamage(dinfo2);
                                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), base.Map);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static int GetWeaponDmg(Pawn caster)
        {
            
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            //MightPowerSkill pwr = comp.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_pwr");
            //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_Whirlwind, "TM_Whirlwind", "_pwr", true);
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_Whirlwind, true);
            float dmgNum = comp.weaponDamage;

            if (caster.equipment.Primary != null && caster.equipment.Primary.def.IsRangedWeapon)
            {
                dmgNum = Mathf.RoundToInt(dmgNum * TorannMagicDefOf.TM_Whirlwind.weaponDamageFactor);
            }

            dmgNum = Mathf.RoundToInt(dmgNum * (1 + (.08f * pwrVal)));

            return (int)Mathf.Max(5, dmgNum);
        }

        public override void Draw()
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
                    pawn.Drawer.DrawAt(this.DrawPos);
                    DrawCleaving(this.DrawPos, 10);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
                base.Comps_PostDraw();
            }
        }

        private void DrawCleaving(Vector3 flyingPawn, int magnitude)
        {
            bool flag = !pawn.Dead && !pawn.Downed;
            if (flag)
            {
                Vector3 vector = flyingPawn;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                float angle = (float)Rand.Range(0, 360);
                Vector3 s = new Vector3(3f, 3f, 5f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, FlyingObject_Whirlwind.cleavingMat, 0);
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
            GenSpawn.Spawn(this.flyingThing, base.Position, base.Map);
            ModOptions.Constants.SetPawnInFlight(false);
            Pawn p = this.flyingThing as Pawn;
            if(p.IsColonist)
            {
                p.drafter.Drafted = true;
                if (ModOptions.Settings.Instance.cameraSnap)
                {
                    CameraJumper.TryJumpAndSelect(p);
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }
    }
}
