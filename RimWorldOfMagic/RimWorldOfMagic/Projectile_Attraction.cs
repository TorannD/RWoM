using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TorannMagic
{
    class Projectile_Attraction : Projectile_AbilityBase
    {
        int age = -1;
        int duration = 1200;
        int verVal = 0;
        int pwrVal = 0;
        float arcaneDmg = 1;
        int strikeDelay = 6;
        float radius = 5;
        bool initialized = false;
        List<IntVec3> cellList;
        List<IntVec3> hediffCellList;
        Pawn pawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn", false);
            Scribe_Collections.Look<IntVec3>(ref this.cellList, "cellList", LookMode.Value);
            Scribe_Collections.Look<IntVec3>(ref this.hediffCellList, "hediffCellList", LookMode.Value);
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
            this.age++;
        }

        protected override void Impact(Thing hitThing)
        {            
            base.Impact(hitThing);
           
            ThingDef def = this.def;
            Pawn victim = null;

            if (!this.initialized)
            {
                pawn = this.launcher as Pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Attraction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Attraction_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Attraction.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Attraction_ver");
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                pwrVal = pwr.level;
                verVal = ver.level;
                if (pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    MightPowerSkill mpwr = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                    MightPowerSkill mver = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                    pwrVal = mpwr.level;
                    verVal = mver.level;
                }
                this.arcaneDmg = comp.arcaneDmg;
                if (settingsRef.AIHardMode && !pawn.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.duration = this.duration + (120 * verVal);
                this.strikeDelay = this.strikeDelay - verVal;
                this.radius = this.def.projectile.explosionRadius + (1.5f * pwrVal);
                //GenExplosion.DoExplosion(base.Position, this.Map, this.radius, TMDamageDefOf.DamageDefOf.TM_Shadow, this.pawn, (int)((this.def.projectile.GetDamageAmount(1, null) * (1 + .15 * pwrVal)) * this.arcaneDmg * Rand.Range(.75f, 1.25f)), 0, TorannMagicDefOf.TM_SoftExplosion, def, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);

                this.initialized = true;
                IEnumerable<IntVec3> hediffCells = GenRadial.RadialCellsAround(base.Position, 2, true);
                hediffCellList = hediffCells.ToList<IntVec3>();
                IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(base.Position, this.radius, false).Except(hediffCells);
                cellList = targets.ToList<IntVec3>();
                for(int i = 0; i < cellList.Count(); i ++)
                {
                    if (cellList[i].IsValid && cellList[i].InBoundsWithNullCheck(pawn.Map))
                    {
                        victim = cellList[i].GetFirstPawn(pawn.Map);
                        if (victim != null && !victim.Dead && !victim.Downed)
                        {
                            HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_GravitySlowHD, .5f);
                        }
                    }
                }
            }
            IntVec3 curCell = cellList.RandomElement();
            //Vector3 angle = GetVector(base.Position, curCell);
            //TM_MoteMaker.ThrowArcaneWaveMote(curCell.ToVector3(), base.Map, .4f * (curCell - base.Position).LengthHorizontal, .1f, .05f, .5f, 0, Rand.Range(1, 2), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(-90, Vector3.up) * angle).ToAngleFlat());

            if (Find.TickManager.TicksGame % this.strikeDelay == 0 && this.Map != null)
            {
                if (this.pwrVal == 0)
                {
                    Effecter AttractionEffect = TorannMagicDefOf.TM_AttractionEffecter.Spawn();
                    AttractionEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
                    AttractionEffect.Cleanup();
                }
                else if(this.pwrVal ==1)
                {
                    Effecter AttractionEffect = TorannMagicDefOf.TM_AttractionEffecter_I.Spawn();
                    AttractionEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
                    AttractionEffect.Cleanup();
                }
                else if(this.pwrVal == 2)
                {
                    Effecter AttractionEffect = TorannMagicDefOf.TM_AttractionEffecter_II.Spawn();
                    AttractionEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
                    AttractionEffect.Cleanup();
                }
                else
                {
                    Effecter AttractionEffect = TorannMagicDefOf.TM_AttractionEffecter_III.Spawn();
                    AttractionEffect.Trigger(new TargetInfo(base.Position, this.Map, false), new TargetInfo(base.Position, this.Map, false));
                    AttractionEffect.Cleanup();
                }
                for (int i = 0; i < 3; i++)
                {
                    curCell = cellList.RandomElement();
                    if (curCell.IsValid && curCell.InBoundsWithNullCheck(base.Map))
                    {
                        victim = curCell.GetFirstPawn(base.Map);
                        if (victim != null && !victim.Dead && victim.RaceProps.IsFlesh && victim != this.pawn)
                        {                            
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.pawn, victim) - .4f))
                            {
                                Vector3 launchVector = GetVector(base.Position, victim.Position);
                                HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_GravitySlowHD, (.4f + (.1f * verVal)));
                                LaunchFlyingObect(victim.Position + (2f * (1 + (.4f * pwrVal)) * launchVector).ToIntVec3(), victim);
                            }
                        }
                        //else if (victim != null && !victim.Dead && !victim.RaceProps.IsFlesh)
                        //{
                        //    HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_GravitySlowHD, .4f + (.1f * verVal));
                        //}
                    }
                }
                for(int i =0; i < hediffCellList.Count(); i++)
                {
                    curCell = hediffCellList[i];
                    if (curCell.IsValid && curCell.InBoundsWithNullCheck(base.Map))
                    {
                        victim = curCell.GetFirstPawn(base.Map);
                        if (victim != null && !victim.Dead && victim != this.pawn)
                        {
                            if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.pawn, victim) - .4f))
                            {
                                HealthUtility.AdjustSeverity(victim, TorannMagicDefOf.TM_GravitySlowHD, .3f + (.1f * verVal));
                            }
                        }
                    }
                }   
            }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn)
        {
            bool flag = targetCell != null && targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Position.IsValid && pawn.Spawned && pawn.Map != null && !pawn.Downed && !pawn.Dead)
                {
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        ModCheck.GiddyUp.ForceDismount(pawn);
                    }
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(TorannMagicDefOf.FlyingObject_Spinning, pawn.Position, pawn.Map);
                    flyingObject.Launch(pawn, targetCell, pawn);
                }
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (center - objectPos).ToVector3();
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