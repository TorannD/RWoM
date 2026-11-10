using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using AbilityUser;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class FlyingObject_Whirlwind : Projectile, IThingHolder, ISuspendableThingHolder
    {

        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material cleavingMat = MaterialPool.MatFrom("Spells/cleave", ShaderDatabase.Transparent, FlyingObject_Whirlwind.cleaveColor);

        private int versatility = -1;
        private Pawn skillOwner;
        private Thing carriedCargo;
        private bool drafted = true;
        private IntVec3 orderedLanding = IntVec3.Invalid;
        private bool landingConsent;

        protected float speed = 40f;

        //protected Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        private readonly ThingOwner<Thing> passenger;
        private bool takingOff, returnPending;
        private int nextReturnAttempt;

        public FlyingObject_Whirlwind() { passenger = new ThingOwner<Thing>(this, true); }
        public bool IsContentsSuspended => true;
        public ThingOwner GetDirectlyHeldThings() => passenger;
        public void GetChildHolders(List<IThingHolder> children) => ThingOwnerUtility.AppendThingHoldersFromThings(children,passenger);

        internal static bool ProtectsMap(Map map)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("FlyingObject_Whirlwind");
            return def != null && map?.listerThings?.ThingsOfDef(def)
                .Any(t => t is FlyingObject_Whirlwind flight && (flight.takingOff || flight.passenger.Count > 0)) == true;
        }

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
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                // Legacy subclasses wrote the current flight values after the base projectile.
                var record = Scribe.loader.curXmlParent;
                foreach (var key in new[] { "origin", "destination", "ticksToImpact" })
                {
                    var entries = record.SelectNodes(key);
                    if (entries.Count > 1) record.InsertBefore(entries[entries.Count-1],entries[0]);
                }
            }
            base.ExposeData();
            Scribe_Values.Look(ref returnPending,"TM_whirlwindReturnPending");
            Scribe_Values.Look(ref versatility,"TM_whirlwindVersatility",-1);
            Scribe_Values.Look(ref drafted,"TM_whirlwindDrafted",true);
            Scribe_Values.Look(ref orderedLanding,"TM_whirlwindOrderedLanding",IntVec3.Invalid);
            Scribe_Values.Look(ref landingConsent,"TM_whirlwindLandingConsent");
            Scribe_References.Look(ref skillOwner,"TM_whirlwindSkillOwner");
            Scribe_References.Look(ref carriedCargo,"TM_whirlwindCargo");
            Scribe_Values.Look<int>(ref this.timesToDamage, "timesToDamage", 0, false);
            Scribe_Values.Look<int>(ref this.weaponDmg, "weaponDmg", 0, false);
            Scribe_Values.Look<bool>(ref this.damageLaunched, "damageLaunched", true, false);
            Scribe_Values.Look<bool>(ref this.explosion, "explosion", false, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_Deep.Look<Thing>(ref this.flyingThing, "flyingThing", new object[0]);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && flyingThing != null && !passenger.Contains(flyingThing))
            {
                if (flyingThing.Spawned || flyingThing.holdingOwner != null)
                {
                    // An independently spawned or owned passenger must never be landed a second time.
                    flyingThing = null;
                    returnPending = true;
                }
                else if (!passenger.TryAdd(flyingThing,false))
                    throw new InvalidOperationException("Whirlwind cannot restore its saved passenger.");
            }
        }

        private void Initialize()
        {
            if (pawn != null)
            {
                FleckMaker.Static(this.origin, this.Map, FleckDefOf.ExplosionFlash, 12f);
                SoundDefOf.Ambient_AltitudeWind.sustainFadeoutTime.Equals(30.0f);
                FleckMaker.ThrowDustPuff(this.origin, this.Map, Rand.Range(1.2f, 1.8f));
                weaponDmg = WeaponDamage(pawn,SkillOwner);
                
                if (!pawn.IsColonist && ModOptions.Settings.Instance.AIHardMode)
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
            => TakeOff(launcher,origin,targ,flyingThing,newDamageInfo,null);

        internal void LaunchAbility(Pawn body,MightAbility power,IntVec3 cell)
            => TakeOff(body,body.Position.ToVector3Shifted(),cell,body,null,power);

        private Pawn SkillOwner => skillOwner ?? pawn;
        private void TakeOff(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo,MightAbility power)
        {
            if (passenger.Count != 0 || !Spawned || flyingThing?.Spawned != true || flyingThing.Map != Map ||
                !targ.IsValid || !targ.Cell.InBounds(Map)) return;
            var start = flyingThing.Position;
            pawn = launcher as Pawn;
            skillOwner = power?.Pawn != pawn ? power?.Pawn : null;
            if (power != null)
            {
                orderedLanding=targ.Cell;
                landingConsent=Odyssey.ManualTravelSafety.HasConsent(pawn,pawn.CurJob,Map,orderedLanding);
            }
            if (SkillOwner!=null) versatility=TM_Calc.GetSkillVersatilityLevel(SkillOwner,TorannMagicDefOf.TM_Whirlwind);
            var body=flyingThing as Pawn;
            drafted=body?.Drafted==true;
            takingOff = true;
            try
            {
                var carried = body?.carryTracker?.CarriedThing;
                if (carried != null)
                {
                    if (body.inventory == null || !body.carryTracker.innerContainer.TryTransferToContainer(carried,body.inventory.innerContainer,false))
                    { Destroy(); return; }
                    carriedCargo=carried;
                }
                flyingThing.DeSpawn();
                if (!passenger.TryAdd(flyingThing,false))
                {
                    if (!flyingThing.Spawned && flyingThing.holdingOwner == null) GenSpawn.Spawn(flyingThing,start,Map);
                    Destroy();
                    return;
                }
                this.flyingThing = flyingThing;
            }
            catch
            {
                if (!flyingThing.Destroyed && !flyingThing.Spawned && flyingThing.holdingOwner == null)
                    GenSpawn.Spawn(flyingThing,start,Map);
                throw;
            }
            finally
            {
                takingOff = false;
                if (!passenger.Contains(flyingThing)) RestoreCargo(body);
            }
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            bool flag = targ.Thing != null;
            if (flag)
            {
                this.assignedTarget = targ.Thing;
            }
            this.destination = targ.Cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            this.ticksToImpact = this.StartingTicksToImpact;
            if (power != null) Odyssey.ManualTravelSafety.Settle(power);
            this.Initialize();
        }

        protected override void TickInterval(int delta) { }

        protected override void Tick()
        {
            if (returnPending || flyingThing == null)
            {
                if (Find.TickManager.TicksGame >= nextReturnAttempt) Destroy();
                return;
            }
            this.ticksToImpact--;
            bool flag = !this.ExactPosition.InBoundsWithNullCheck(base.Map);
            if (flag)
            {
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
                DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Cleave, weaponDmg, 0, (float)-1, SkillOwner, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
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
                            if (intVec.InBounds(this.Map))
                            {
                                Pawn cleaveVictim = intVec.GetFirstPawn(base.Map);
                                if (cleaveVictim != null && cleaveVictim.Faction != pawn.Faction)
                                {
                                    cleaveVictim.TakeDamage(dinfo);
                                    FleckMaker.ThrowMicroSparks(cleaveVictim.Position.ToVector3(), base.Map);
                                    if (versatility<0) versatility=TM_Calc.GetSkillVersatilityLevel(SkillOwner,TorannMagicDefOf.TM_Whirlwind);
                                    //verVal = TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_Whirlwind, "TM_Whirlwind", "_ver", true);
                                    //MightPowerSkill ver = comp.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_ver");
                                    //verVal = ver.level;
                                    //if(pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                                    //{
                                    //    MightPowerSkill mver = comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                                    //    verVal = mver.level;
                                    //}
                                    DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_Whirlwind, weaponDmg, 0, (float)-1, SkillOwner, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                    System.Random random = new System.Random();
                                    int rnd = GenMath.RoundRandom(random.Next(0, 100));
                                    if (rnd < (versatility * 5))
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
        }

        public static int GetWeaponDmg(Pawn caster) => WeaponDamage(caster,caster);
        private static int WeaponDamage(Pawn caster,Pawn owner)
        {
            
            CompAbilityUserMight comp = caster.GetCompAbilityUserMight();
            //MightPowerSkill pwr = comp.MightData.MightPowerSkill_Whirlwind.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Whirlwind_pwr");
            //pwrVal = TM_Calc.GetMightSkillLevel(caster, comp.MightData.MightPowerSkill_Whirlwind, "TM_Whirlwind", "_pwr", true);
            int pwrVal = TM_Calc.GetSkillPowerLevel(owner, TorannMagicDefOf.TM_Whirlwind, true);
            float dmgNum = comp.weaponDamage;

            if (caster.equipment.Primary != null && caster.equipment.Primary.def.IsRangedWeapon)
            {
                dmgNum = Mathf.RoundToInt(dmgNum * TorannMagicDefOf.TM_Whirlwind.weaponDamageFactor);
            }

            dmgNum = Mathf.RoundToInt(dmgNum * (1 + (.08f * pwrVal)));

            return (int)Mathf.Max(5, dmgNum);
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
            if (returnPending) return;
            returnPending = true;
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
                    else if (hitThing != null)
                    {
                        hitThing.TakeDamage(this.impactDamage.Value);
                    }
                }
                bool flag4 = this.explosion;
                if (flag4)
                {
                    GenExplosion.DoExplosion(base.Position, base.Map, 0.9f, DamageDefOf.Stun, this, -1, 0, null, null, null, null, null, 0f, 1, null, null, 0, false, null, 0f, 1, 0f, false);
                }
            }
            Destroy(DestroyMode.Vanish);
        }

        private bool ReleaseNear(Thing body, IntVec3 center, bool returning = false)
        {
            foreach (var cell in GenRadial.RadialCellsAround(center,3.9f,true))
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map) || cell.GetFirstPawn(Map) != null) continue;
                if (orderedLanding.IsValid && body is Pawn traveler &&
                    !(returning && cell==origin.ToIntVec3()) &&
                    !Odyssey.TravelEnvironmentSafety.Evaluate(traveler,Map,cell).IsSafe &&
                    !(landingConsent && cell==orderedLanding)) continue;
                try
                {
                    if (passenger.TryDrop(body,cell,Map,ThingPlaceMode.Direct,out _,playDropSound:false)) return true;
                }
                catch
                {
                    if (!body.Destroyed && !body.Spawned && body.holdingOwner == null) passenger.TryAdd(body,false);
                    throw;
                }
            }
            return false;
        }

        private void RestoreCargo(Pawn body)
        {
            if (carriedCargo != null && body?.carryTracker!=null && body.carryTracker.CarriedThing==null &&
                body.inventory?.innerContainer?.Contains(carriedCargo)==true)
                body.inventory.innerContainer.TryTransferToContainer(carriedCargo,body.carryTracker.innerContainer,false);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            returnPending = true;
            if (passenger.Count > 0)
            {
                var body = passenger[0];
                if (!Spawned || Map.Disposed || (!ReleaseNear(body,Position) && !ReleaseNear(body,origin.ToIntVec3(),returning:true)))
                {
                    nextReturnAttempt = Find.TickManager.TicksGame + 60;
                    return;
                }
                if (body is Pawn released)
                {
                    RestoreCargo(released);
                    if (released.IsColonist && released.drafter != null) released.drafter.Drafted = drafted;
                }
            }
            flyingThing = null;
            base.Destroy(mode);
        }
    }
}
