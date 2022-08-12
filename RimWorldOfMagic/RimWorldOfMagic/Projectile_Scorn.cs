using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;

namespace TorannMagic
{
    class Projectile_Scorn : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 20;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 4;
        int strikeNum = 1;
        float radius = 5;
        bool initialized = false;
        float angle = 0;
        List<IntVec3> cellList;
        Pawn pawn;
        IEnumerable<IntVec3> targets;
        Skyfaller skyfaller2;
        Skyfaller skyfaller;
        Map map;
        IntVec3 safePos = default(IntVec3);

        bool launchedFlag = false;
        bool pivotFlag = false;
        bool landedFlag = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.launchedFlag, "launchedFlag", false, false);
            Scribe_Values.Look<bool>(ref this.landedFlag, "landedFlag", false, false);
            Scribe_Values.Look<bool>(ref this.pivotFlag, "pivotFlag", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.strikeNum, "strikeNum", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.safePos, "safePos", default(IntVec3), false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Collections.Look<IntVec3>(ref this.cellList, "cellList", LookMode.Value);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age < duration;
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        public override void Tick()
        {
            base.Tick();
            //this.age++;
        }

        protected override void Impact(Thing hitThing)
        {            
            base.Impact(hitThing);
           
            ThingDef def = this.def;          

            if (!this.initialized)
            {
                this.pawn = this.launcher as Pawn;
                this.map = this.pawn.Map;                
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Scorn.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Scorn_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Scorn.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Scorn_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                this.arcaneDmg = comp.arcaneDmg;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 1;
                    verVal = 1;
                }
                this.radius = this.def.projectile.explosionRadius + verVal;
                //this.duration = Mathf.RoundToInt(this.radius * this.strikeDelay);
                this.initialized = true;
            }

            if (!launchedFlag)
            {
                ModOptions.Constants.SetPawnInFlight(true);
                if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                {
                    ModCheck.GiddyUp.ForceDismount(pawn);
                }
                skyfaller = SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_ScornLeaving"), pawn.Position, this.map);
                if(base.Position.x < pawn.Position.x)
                {
                    this.angle = Rand.Range(20, 40);
                }
                else
                {
                    this.angle = Rand.Range(-40, -20);
                }
                skyfaller.angle = this.angle;
                launchedFlag = true;
                pawn.DeSpawn();
            }
            if (skyfaller.DestroyedOrNull() && !pivotFlag)
            {
                safePos = base.Position;
                if (safePos.x > this.map.Size.x - 5)
                {
                    safePos.x = this.map.Size.x - 5;
                }
                else if (safePos.x < 5)
                {
                    safePos.x = 5;
                }

                if (safePos.z > this.map.Size.z - 5)
                {
                    safePos.z = this.map.Size.z - 5;
                }
                else if (safePos.z < 5)
                {
                    safePos.z = 5;
                }
                skyfaller2 = SkyfallerMaker.SpawnSkyfaller(ThingDef.Named("TM_ScornIncoming"), safePos, this.map);
                skyfaller2.angle = this.angle;
                pivotFlag = true;

            }

            if (skyfaller2.DestroyedOrNull() && pivotFlag && launchedFlag && !landedFlag)
            {
                landedFlag = true;
                GenSpawn.Spawn(pawn, safePos, this.map);
                if (pawn.drafter != null)
                {
                    pawn.drafter.Drafted = true;
                }
                ModOptions.Constants.SetPawnInFlight(false);
                if(verVal == 0)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_DemonScornHD, 60f + (pwrVal * 15));
                }
                else if(verVal == 1)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_DemonScornHD_I, 60f + (pwrVal * 15));
                }
                else if(verVal == 2)
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_DemonScornHD_II, 60f + (pwrVal * 15));
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_DemonScornHD_III, 60f + (pwrVal * 15));
                }
            }
            if(landedFlag)
            { 
                if (Find.TickManager.TicksGame % strikeDelay == 0)
                {
                    if (safePos.DistanceToEdge(this.map) > strikeNum)
                    {
                        List<IntVec3> targets;
                        if (strikeNum == 1)
                        {
                            targets = GenRadial.RadialCellsAround(safePos, this.strikeNum, false).ToList();
                        }
                        else
                        {
                            IEnumerable<IntVec3> oldTargets = GenRadial.RadialCellsAround(base.Position, this.strikeNum - 1, false);
                            targets = GenRadial.RadialCellsAround(safePos, this.strikeNum, false).Except(oldTargets).ToList();
                        }
                        for (int j = 0; j < targets.Count(); j++)
                        {
                            IntVec3 curCell = targets[j];
                            if (this.map != null && curCell.IsValid && curCell.InBoundsWithNullCheck(this.map))
                            {
                                GenExplosion.DoExplosion(curCell, this.Map, .4f, TMDamageDefOf.DamageDefOf.TM_Shadow, this.pawn, (int)((this.def.projectile.GetDamageAmount(1, null) * (1 + .15 * pwrVal)) * this.arcaneDmg * Rand.Range(.75f, 1.25f)), 0, TorannMagicDefOf.TM_SoftExplosion, def, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                            }
                        }
                        this.strikeNum++;
                    }
                    else
                    {
                        strikeNum = (int)this.radius + 1;
                    }
                }               
            }
            if (this.strikeNum > this.radius)
            {
                this.age = this.duration;
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force)
        {
            bool flag = targetCell != null && targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), pawn.Position, pawn.Map);
                    flyingObject.speed = 25 + force;
                    flyingObject.Launch(pawn, targetCell, pawn);
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

        public void damageEntities(Pawn e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.5f, 1.5f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }    
}