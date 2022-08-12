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

        public IntVec3 iCell = new IntVec3();
        public override IntVec3 InteractionCell => iCell;

        CompAbilityUserMagic comp;
        public Pawn manPawn = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.mortarMaxRange, "mortarMaxRange", 80, false);
            Scribe_Values.Look<int>(ref this.mortarTicksToFire, "mortarTicksToFire", 900, false);
            Scribe_Values.Look<int>(ref this.rocketCount, "rocketCount", 1, false);
            Scribe_Values.Look<int>(ref this.rocketTicksToFire, "rocketTicksToFire", 600, false);
            Scribe_Values.Look<float>(ref this.rocketManaCost, "rocketManaCost", 0.05f, false);
            Scribe_Values.Look<float>(ref this.mortarManaCost, "mortarManaCost", 0.1f, false);
            Scribe_Values.Look<Pawn>(ref this.manPawn, "manPawn");
            Scribe_Values.Look<IntVec3>(ref this.iCell, "iCell");
            Scribe_Values.Look<int>(ref this.age, "age", 0);
            Scribe_Values.Look<int>(ref this.duration, "duration", 3600);
        }

        public override void Tick()
        {
            base.Tick();
            age++;
            //if (!manPawn.DestroyedOrNull() && !manPawn.Dead && !manPawn.Downed && manPawn.Position == this.InteractionCell)
            if(this.age <= this.duration)
            {
                if (!initialized)
                {
                    comp = manPawn.GetCompAbilityUserMagic();
                    this.verVal = comp.MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoTurret_ver").level;
                    this.pwrVal = comp.MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoTurret_pwr").level;
                    this.effVal = comp.MagicData.MagicPowerSkill_TechnoTurret.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoTurret_eff").level;
                    this.duration = 3600 + (300 * effVal);
                    if (this.verVal >= 5)
                    {
                        this.rocketTicksToFire = 600 - ((verVal - 5) * 20);
                        this.rocketCount = verVal / 5;
                        this.rocketManaCost = .04f - (.001f * this.effVal);
                    }
                    if (this.verVal >= 10)
                    {
                        this.mortarTicksToFire = 900 - ((verVal - 10) * 40);
                        this.mortarMaxRange += ((verVal - 10) * 5);
                        this.mortarManaCost = .08f - (.002f * effVal);
                    }                    
                    this.initialized = true;
                }

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
                    if ((powerComp == null || powerComp.PowerOn) && base.Spawned)
                    {
                        GunCompEq.verbTracker.VerbsTick();
                        if (!stunner.Stunned && AttackVerb.state != VerbState.Bursting)
                        {
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
                GenExplosion.DoExplosion(this.Position, this.Map, 1f, DamageDefOf.EMP, this, 0, 0, SoundDefOf.Crunch, null, null, this, null, 0, 0, false, null, 0, 0, 0, false);
                this.Destroy(DestroyMode.Vanish);
            }
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
            if (this.HitPoints < 1 && manPawn != null && !manPawn.Dead)
            {
                int rnd = Mathf.RoundToInt(Rand.Range(3, 5) - (.2f * this.effVal));
                for (int i = 0; i < rnd; i++)
                {
                    TM_Action.DamageEntities(manPawn, null, Rand.Range(4f, 8f), DamageDefOf.Burn, this);
                }
            }
            base.Destroy(mode);            
        }
    }
}
