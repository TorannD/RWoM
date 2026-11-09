using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;
using Verse.Sound;


namespace TorannMagic
{   
    [StaticConstructorOnStartup]
    public class Building_TechnoTurret : Building_TurretGun
    {
        int mortarMaxRange = 180;
        int mortarMinRange = 40;
        int mortarTicksToFire = 900;
        float mortarManaCost = .08f;

        int rocketMinRange = 5;
        int rocketTicksToFire = 600;
        int rocketCount = 1;
        int nextRocketFireTick = 0;
        float rocketManaCost = .04f;

        private int verVal = 0;
        private int pwrVal = 0;
        private int effVal = 0;

        private float capturedScale = 1f;
        internal int SkillPower => pwrVal;
        internal int SkillEfficiency => effVal;

        public int age = 0;
        public int duration = 3600;

        private bool MannedByColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction == Faction.OfPlayer;
        private bool MannedByNonColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction != Faction.OfPlayer;
        private bool PlayerControlled => (base.Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist;
        private bool Manned => MannedByColonist || MannedByNonColonist;
        private bool holdFire;
        private bool WarmingUp => burstWarmupTicksLeft > 0;
        private bool CanSetForcedTarget => mannableComp != null && PlayerControlled;
        private bool CanToggleHoldFire => PlayerControlled;
        private bool IsMortar => def.building.IsMortar;
        private bool IsMortarOrProjectileFliesOverhead => AttackVerb.ProjectileFliesOverhead() || IsMortar;
        private bool initialized = false;
        private bool burstActivated;

        public IntVec3 iCell = new IntVec3();
        public override IntVec3 InteractionCell => iCell;

        CompAbilityUserMagic comp;
        public Pawn manPawn = null;

        public bool TT_Active
        {
            get
            {
                if ((powerComp == null || powerComp.PowerOn) && (dormantComp == null || dormantComp.Awake) && (initiatableComp == null || initiatableComp.Initiated))
                {
                    if (interactableComp != null)
                    {
                        return burstActivated;
                    }
                    return true;
                }
                return false;
            }
        }

        public override AcceptanceReport ClaimableBy(Faction by)
        {
            //if (!base.ClaimableBy(by, reason))
            //{
            //    return false;
            //}
            //if (mannableComp != null && mannableComp.ManningPawn != null)
            //{
            //    return false;
            //}
            //if (TT_Active && mannableComp == null)
            //{
            //    return false;
            //}
            //if (((dormantComp != null && !dormantComp.Awake) || (initiatableComp != null && !initiatableComp.Initiated)) && (powerComp == null || powerComp.PowerOn))
            //{
            //    return false;
            //}
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            ExposeTechnoTurretData();
        }

        internal void ExposeTechnoTurretData()
        {
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.mortarMaxRange, "mortarMaxRange", 80, false);
            Scribe_Values.Look<int>(ref this.mortarTicksToFire, "mortarTicksToFire", 900, false);
            Scribe_Values.Look<int>(ref this.rocketCount, "rocketCount", 1, false);
            Scribe_Values.Look<int>(ref this.rocketTicksToFire, "rocketTicksToFire", 600, false);
            Scribe_Values.Look<float>(ref this.rocketManaCost, "rocketManaCost", 0.05f, false);
            Scribe_Values.Look<float>(ref this.mortarManaCost, "mortarManaCost", 0.1f, false);
            Scribe_Values.Look(ref burstActivated, "burstActivated", false);
            // Legacy manPawn was value-scribed, not a recoverable pawn reference.
            Scribe_References.Look(ref this.manPawn, "manPawnReference");
            Scribe_Values.Look<IntVec3>(ref this.iCell, "iCell");
            Scribe_Values.Look<int>(ref this.age, "age", 0);
            Scribe_Values.Look<int>(ref this.duration, "duration", 3600);
            Scribe_Values.Look(ref this.initialized, "technoTurretInitialized", this.age > 0);
            Scribe_Values.Look(ref this.effVal, "effVal", 0);
            Scribe_Values.Look(ref this.nextRocketFireTick, "nextRocketFireTick", 0);
            Scribe_Values.Look(ref capturedScale, "TM_turretScale", -1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                comp = null;
                if (capturedScale < 0f) capturedScale = manPawn?.GetCompAbilityUserMagic()?.arcaneDmg ?? 1f;
            }
        }

        internal void InitializeTechnoTurret()
        {
            if (manPawn.DestroyedOrNull()) { comp = null; return; }
            comp = manPawn.GetCompAbilityUserMagic();
            if (initialized || comp == null) return;
            var skills = comp.MagicData.MagicPowerSkill_TechnoTurret;
            var versatility = skills.FirstOrDefault(x => x.label == "TM_TechnoTurret_ver");
            var power = skills.FirstOrDefault(x => x.label == "TM_TechnoTurret_pwr");
            var efficiency = skills.FirstOrDefault(x => x.label == "TM_TechnoTurret_eff");
            if (versatility == null || power == null || efficiency == null) return;
            Configure(power.level, versatility.level, efficiency.level, comp.arcaneDmg);
        }

        private void Configure(int power, int versatility, int efficiency, float scale)
        {
            pwrVal = power;
            verVal = versatility;
            effVal = efficiency;
            capturedScale = scale;
            duration = 3600 + (300 * effVal);
            if (verVal >= 5)
            {
                rocketTicksToFire = 600 - ((verVal - 5) * 20);
                rocketCount = verVal / 5;
                rocketManaCost = .04f - (.001f * effVal);
            }
            if (verVal >= 10)
            {
                mortarTicksToFire = 900 - ((verVal - 10) * 40);
                mortarMaxRange += ((verVal - 10) * 5);
                mortarManaCost = .08f - (.002f * effVal);
            }
            initialized = true;
        }

        internal void Deploy(Pawn owner, IntVec3 interaction, int power, int versatility, int efficiency, float scale)
        {
            manPawn = owner;
            iCell = interaction;
            Configure(power, versatility, efficiency, scale);
            var summoned = this.TryGetComp<CompSummoned>();
            summoned.Temporary = true;
            summoned.TicksToDestroy = duration;
            StatDefOf.MaxHitPoints.Worker.ClearCacheForThing(this);
            HitPoints = MaxHitPoints;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            InitializeTechnoTurret();
            if (!initialized || gun == null) return;
            var verb = AttackVerb;
            var props = verb.verbProps.MemberwiseClone();
            props.burstShotCount = Mathf.RoundToInt((6 + .4f * pwrVal) * capturedScale);
            props.warmupTime = 1.5f - .03f * pwrVal;
            props.range = Mathf.RoundToInt((35 + verVal) * capturedScale);
            verb.verbProps = props;
        }

        protected override float BurstCooldownTime() => initialized ? 4.5f - .1f * pwrVal : base.BurstCooldownTime();

        protected override void Tick()
        {
            base.Tick();
            age++;
            //if (!manPawn.DestroyedOrNull() && !manPawn.Dead && !manPawn.Downed && manPawn.Position == this.InteractionCell)
            if (this.age <= this.duration)
            {
                InitializeTechnoTurret();

                if (!manPawn.DestroyedOrNull() && !manPawn.Dead && !manPawn.Downed && comp != null && comp.Mana != null)
                {
                    if (this.verVal >= 5 && this.nextRocketFireTick < Find.TickManager.TicksGame && this.TargetCurrentlyAimingAt != null && comp.Mana.CurLevel >= this.rocketManaCost)
                    {
                        if (this.TargetCurrentlyAimingAt.Cell.IsValid && this.TargetCurrentlyAimingAt.Cell.DistanceToEdge(this.Map) > 5 && (this.TargetCurrentlyAimingAt.Cell - this.Position).LengthHorizontal >= this.rocketMinRange)
                        {
                            bool flag = this.TargetCurrentlyAimingAt.Cell != default(IntVec3);
                            if (flag)
                            {
                                Thing launchedThing = new Thing()
                                {
                                    def = ThingDef.Named("FlyingObject_RocketSmall")
                                };
                                FlyingObject_Advanced flyingObject = (FlyingObject_Advanced)GenSpawn.Spawn(ThingDef.Named("FlyingObject_RocketSmall"), this.Position, this.Map);
                                flyingObject.AdvancedLaunch(this, TorannMagicDefOf.Mote_Base_Smoke, 1, Rand.Range(5, 25), false, this.DrawPos, this.TargetCurrentlyAimingAt.Cell, launchedThing, Rand.Range(32, 38), true, Mathf.RoundToInt(Rand.Range(22f, 30f) * comp.arcaneDmg), 2, TMDamageDefOf.DamageDefOf.TM_PersonnelBombDD, null);
                                this.rocketCount--;
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Position, this.Map, false), MaintenanceType.None);
                                info.pitchFactor = 1.3f;
                                info.volumeFactor = 1.5f;
                                TorannMagicDefOf.TM_AirWoosh.PlayOneShot(info);
                            }
                            if (rocketCount <= 0)
                            {
                                this.rocketCount = verVal / 5;
                                this.nextRocketFireTick = Find.TickManager.TicksGame + (600 - ((verVal - 5) * 20));
                                comp.Mana.CurLevel -= this.rocketManaCost;
                                comp.MagicUserXP += Rand.Range(9, 12);
                            }
                            else
                            {
                                this.nextRocketFireTick = Find.TickManager.TicksGame + 20;
                            }
                        }
                    }

                    if (this.verVal >= 10 && this.mortarTicksToFire < Find.TickManager.TicksGame && comp.Mana.CurLevel >= this.mortarManaCost)
                    {
                        this.mortarTicksToFire = Find.TickManager.TicksGame + (900 - ((verVal - 10) * 40));
                        Pawn target = TM_Calc.FindNearbyEnemy(this.Position, this.Map, this.Faction, this.mortarMaxRange, this.mortarMinRange);
                        if (target != null && target.Position.DistanceToEdge(this.Map) > 8)
                        {
                            bool flag = target.Position != default(IntVec3);
                            if (flag)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    IntVec3 rndTarget = target.Position;
                                    rndTarget.x += Rand.RangeInclusive(-6, 6);
                                    rndTarget.z += Rand.RangeInclusive(-6, 6);
                                    Projectile newProjectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Bullet_Shell_TechnoTurretExplosive"), this.Position, this.Map, WipeMode.Vanish);
                                    newProjectile.Launch(this, rndTarget, target, ProjectileHitFlags.All, false, null);
                                }
                            }
                            SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Position, this.Map, false), MaintenanceType.None);
                            info.pitchFactor = 1.3f;
                            info.volumeFactor = .8f;
                            SoundDef.Named("Mortar_LaunchA").PlayOneShot(info);
                            comp.Mana.CurLevel -= this.mortarManaCost;
                            comp.MagicUserXP += Rand.Range(12, 15);
                        }
                    }

                    if (CanExtractShell)
                    {
                        CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
                        if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
                        {
                            ExtractShell();
                        }
                    }
                    if (forcedTarget.IsValid && !CanSetForcedTarget)
                    {
                        ResetForcedTarget();
                    }
                    if (!CanToggleHoldFire)
                    {
                        holdFire = false;
                    }
                    if (forcedTarget.ThingDestroyed)
                    {
                        ResetForcedTarget();
                    }
                    if (TT_Active && !base.IsStunned && base.Spawned) //&& (mannableComp == null || mannableComp.MannedNow)
                    {
                        GunCompEq.verbTracker.VerbsTick();
                        if (AttackVerb.state != VerbState.Bursting)
                        {
                            burstActivated = false;
                            if (WarmingUp)
                            {
                                burstWarmupTicksLeft--;
                                if (burstWarmupTicksLeft == 0)
                                {
                                    BeginBurst();
                                }
                            }
                            else
                            {
                                if (burstCooldownTicksLeft > 0)
                                {
                                    burstCooldownTicksLeft--;
                                }
                                if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                                {
                                    TryStartShootSomething(canBeginBurstImmediately: true);
                                }
                            }
                            top.TurretTopTick();
                        }
                    }
                    else
                    {
                        ResetCurrentTarget();
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 rndPos = this.DrawPos;
                    rndPos.x += Rand.Range(-.5f, .5f);
                    rndPos.z += Rand.Range(-.5f, .5f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndPos, this.Map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                    FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.8f, 1.2f));
                    rndPos = this.DrawPos;
                    rndPos.x += Rand.Range(-.5f, .5f);
                    rndPos.z += Rand.Range(-.5f, .5f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.ElectricalSpark, rndPos, this.Map, Rand.Range(.4f, .7f), .2f, .05f, .1f, 0, 0, 0, Rand.Range(0, 360));
                }
                GenExplosion.DoExplosion(this.Position, this.Map, 1f, DamageDefOf.EMP, this, 0, 0, SoundDefOf.Crunch, null, null, this, null, 0, 0, null, null, 0, false, null, 0, 0, 0, false);
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public new void TryActivateBurst()
        {
            burstActivated = true;
            TryStartShootSomething(canBeginBurstImmediately: true);
        }

        private void ExtractShell()
        {
            GenPlace.TryPlaceThing(gun.TryGetComp<CompChangeableProjectile>().RemoveShell(), base.Position, base.Map, ThingPlaceMode.Near);
        }

        private void ResetForcedTarget()
        {
            forcedTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if (burstCooldownTicksLeft <= 0)
            {
                TryStartShootSomething(canBeginBurstImmediately: false);
            }
        }

        private void ResetCurrentTarget()
        {
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        private bool CanExtractShell
        {
            get
            {
                if (!PlayerControlled)
                {
                    return false;
                }
                return gun.TryGetComp<CompChangeableProjectile>()?.Loaded ?? false;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            var feedbackTarget = manPawn;
            if (ClassProgression.SpiritRebirthState.TryGet(manPawn, out var state))
                feedbackTarget = state.HasValidHost(manPawn) ? state.Host : null;
            if (this.HitPoints < 1 && feedbackTarget != null && !feedbackTarget.Dead)
            {
                int rnd = Mathf.RoundToInt(Rand.Range(3, 5) - (.2f * this.effVal));
                for (int i = 0; i < rnd; i++)
                {
                    TM_Action.DamageEntities(feedbackTarget, null, Rand.Range(4f, 8f), DamageDefOf.Burn, this);
                }
            }
            base.Destroy(mode);            
        }
    }
}
