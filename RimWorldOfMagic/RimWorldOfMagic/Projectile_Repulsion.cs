using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using RimWorld;


namespace TorannMagic
{
    class Projectile_Repulsion : ClassProgression.Projectile_DemonField
    {
        protected override TMAbilityDef RankSpell => TorannMagicDefOf.TM_Repulsion;
        public Projectile_Repulsion() => duration = 20;
        int strikeDelay = 4;
        int strikeNum = 1;
        List<IntVec3> cellList;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1800, false);
            Scribe_Values.Look<int>(ref this.strikeDelay, "strikeDelay", 0, false);
            Scribe_Values.Look<int>(ref this.strikeNum, "strikeNum", 0, false);
            Scribe_Collections.Look<IntVec3>(ref this.cellList, "cellList", LookMode.Value);
        }

        protected override void TickField()
        {
            Pawn victim = null;            

            if (!this.initialized)
            {
                InitializeRanks(TorannMagicDefOf.TM_Repulsion);
                if (SkillOwner == null) { Destroy(); return; }
                this.strikeDelay = Math.Max(1,this.strikeDelay - verVal);
                float radius = this.def.projectile.explosionRadius;
                this.duration = Mathf.RoundToInt(radius * this.strikeDelay);
                this.initialized = true;
                cellList = Ring(base.Position,strikeNum);
            }

            if (Find.TickManager.TicksGame % Math.Max(1,this.strikeDelay) == 0)
            {
                int force = Mathf.RoundToInt((10 + (2 * pwrVal) - strikeNum) * casterSensitivity);
                IntVec3 curCell;
                for (int i =0; i < cellList.Count; i++)
                {
                    curCell = cellList[i];
                    if (!curCell.InBounds(Map)) continue;
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
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(SkillOwner, victim, true)))
                                {
                                    damageEntities(victim, force * (.2f * verVal), DamageDefOf.Blunt);
                                    LaunchFlyingObect(projectedPosition, victim, force);
                                }
                            }
                        }
                    }
                }
                strikeNum++;
                cellList = Ring(base.Position,strikeNum);
            }
        }

        internal static List<IntVec3> Ring(IntVec3 center,int radius)
        {
            int start = GenRadial.NumCellsInRadius(radius - 1);
            int end = GenRadial.NumCellsInRadius(radius);
            var cells = new List<IntVec3>(end - start);
            for (int i = start; i < end; i++) cells.Add(center + GenRadial.RadialPattern[i]);
            return cells;
        }

        public void LaunchFlyingObect(IntVec3 targetCell, Pawn pawn, int force)
        {
            bool flag = targetCell.IsValid && targetCell != default(IntVec3);
            if (flag)
            {
                if (pawn != null && pawn.Spawned && targetCell != pawn.Position && targetCell.InBounds(pawn.Map) && !pawn.Downed && !pawn.Dead)
                {
                    if (ModCheck.Validate.GiddyUp.Core_IsInitialized())
                    {
                        ModCheck.GiddyUp.ForceDismount(pawn);
                    }
                    FlyingObject_Spinning flyingObject = (FlyingObject_Spinning)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Spinning"), pawn.Position, pawn.Map);
                    flyingObject.speed = 25 + force;
                    var trial = ClassProgression.DemonTrials.BeforeEffect(this, pawn, true);
                    flyingObject.Launch(pawn, targetCell, pawn);
                    ClassProgression.DemonTrials.AfterEffect(trial, flyingObject);
                }
            }
        }

        public Vector3 GetVector(IntVec3 center, IntVec3 objectPos)
        {
            Vector3 heading = (objectPos - center).ToVector3();
            return heading.normalized;
        }

        public void damageEntities(Pawn e, float d, DamageDef type)
        {
            int amt = Mathf.RoundToInt(Rand.Range(.75f, 1.25f) * d);
            DamageInfo dinfo = new DamageInfo(type, amt, 0, (float)-1, SkillOwner, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            bool flag = e != null;
            if (flag)
            {
                e.TakeDamage(dinfo);
            }
        }
    }    
}