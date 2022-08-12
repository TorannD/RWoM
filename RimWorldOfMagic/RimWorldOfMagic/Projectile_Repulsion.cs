using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;


namespace TorannMagic
{
    class Projectile_Repulsion : Projectile_AbilityBase
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
        float casterSensitivity = 1f;
        List<IntVec3> cellList;
        Pawn pawn;
        IEnumerable<IntVec3> targets;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.strikeNum, "strikeNum", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
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
            this.age++;
        }

        protected override void Impact(Thing hitThing)
        {            
            base.Impact(hitThing);
           
            ThingDef def = this.def;
            Pawn victim = null;            

            if (!this.initialized)
            {
                this.pawn = this.launcher as Pawn;
                CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Repulsion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Repulsion_pwr");
                MagicPowerSkill ver = pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Repulsion.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Repulsion_ver");
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
                this.strikeDelay = this.strikeDelay - verVal;
                this.radius = this.def.projectile.explosionRadius;
                this.duration = Mathf.RoundToInt(this.radius * this.strikeDelay);
                this.initialized = true;
                this.targets = GenRadial.RadialCellsAround(base.Position, strikeNum, false);
                this.casterSensitivity = this.pawn.GetStatValue(StatDefOf.PsychicSensitivity, false);
                cellList = targets.ToList<IntVec3>();
            }

            if (Find.TickManager.TicksGame % this.strikeDelay == 0)
            {
                int force = Mathf.RoundToInt((10 + (2 * pwrVal) - strikeNum) * casterSensitivity);
                IntVec3 curCell;
                for (int i =0; i < cellList.Count; i++)
                {
                    curCell = cellList[i];
                    Vector3 angle = GetVector(base.Position, curCell);
                    TM_MoteMaker.ThrowArcaneWaveMote(curCell.ToVector3(), this.Map, .3f * (curCell - base.Position).LengthHorizontal, .1f, .05f, .3f, 0, 3, (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat(), (Quaternion.AngleAxis(90, Vector3.up) * angle).ToAngleFlat());
                    if (curCell.IsValid && curCell.InBoundsWithNullCheck(this.Map))
                    {
                        victim = curCell.GetFirstPawn(this.Map);
                        if (victim != null && !victim.Dead)
                        {
                            Vector3 launchVector = GetVector(base.Position, victim.Position);
                            IntVec3 projectedPosition = victim.Position + (force * launchVector).ToIntVec3();
                            if (projectedPosition.IsValid && projectedPosition.InBoundsWithNullCheck(this.Map))
                            {
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(pawn, victim, true)))
                                {
                                    damageEntities(victim, force * (.2f * verVal), DamageDefOf.Blunt);
                                    LaunchFlyingObect(projectedPosition, victim, force);
                                }
                            }
                        }
                    }
                }
                strikeNum++;
                IEnumerable<IntVec3> newTargets = GenRadial.RadialCellsAround(base.Position, strikeNum, false);
                try
                {
                    cellList = newTargets.Except(targets).ToList<IntVec3>();
                }
                catch
                {
                    cellList = newTargets.ToList<IntVec3>();
                }
                targets = newTargets;
            }
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force)
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
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, this.pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }    
}