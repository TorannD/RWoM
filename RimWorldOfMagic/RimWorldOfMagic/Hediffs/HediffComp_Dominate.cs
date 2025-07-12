using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Dominate : HediffComp
    {
        private bool initialized = false;
        private int age = 0;
        private int infectionRate = 120;
        private int lastInfection = 240;
        private int hediffPwr = 0;
        private int infectionRadius = 3;
        private int effVal = 0;
        private int verVal = 0;
        private float minimumSev = .3f;

        public int EffVal
        {
            get
            {
                return this.effVal;
            }
            set
            {
                this.effVal = value;
            }
        }

        public int VerVal
        {
            get
            {
                return this.verVal;
            }
            set
            {
                this.verVal = value;
            }
        }

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            this.minimumSev = .3f - (.03f * effVal);
            this.infectionRadius = 3 + verVal;

            if (spawned)
            {
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 1f);
                if (this.Def.defName == "TM_SDDominateHD_III" || this.Def.defName == "TM_WDDominateHD_III")
                {
                    hediffPwr = 3;
                }
                else if (this.Def.defName == "TM_SDDominateHD_II" || this.Def.defName == "TM_WDDominateHD_II")
                {
                    hediffPwr = 2;
                }
                else if (this.Def.defName == "TM_SDDominateHD_I" || this.Def.defName == "TM_WDDominateHD_I")
                {
                    hediffPwr = 1;
                }
                else
                {
                    hediffPwr = 0;
                }
                this.infectionRate -= hediffPwr * 40;
                for (int i = 0; i < 4; i++)
                {
                    TM_MoteMaker.ThrowShadowMote(base.Pawn.Position.ToVector3(), base.Pawn.Map, Rand.Range(.6f, 1f));
                }
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (base.Pawn != null & base.parent != null)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
            }
            this.age++;

            if (Find.TickManager.TicksGame % 60 == 0)
            {
                HealthUtility.AdjustSeverity(base.Pawn, this.Def, -0.1f);
            }

            if (age > (lastInfection + infectionRate) && this.parent.Severity > this.minimumSev)
            {
                bool infectionFlag = false;
                HealthUtility.AdjustSeverity(base.Pawn, this.Def, -1 * (this.minimumSev));
                this.lastInfection = this.age;
                Pawn pawn = base.Pawn as Pawn;
                Map map = pawn.Map;
                if (!pawn.DestroyedOrNull() && pawn.Map != null)
                {
                    IntVec3 curCell;
                    Pawn victim = null;
                    IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(pawn.Position, this.infectionRadius, true);
                    for (int i = 0; i < targets.Count(); i++)
                    {
                        curCell = targets.ToArray<IntVec3>()[i];
                        if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                        {
                            victim = curCell.GetFirstPawn(map);
                        }

                        if (victim != null && victim.Faction == pawn.Faction && infectionFlag == false && !victim.health.hediffSet.HasHediff(this.Def))
                        {
                            //bool hediffFlag = (victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SDDominateHD) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SDDominateHD_I) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SDDominateHD_II) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SDDominateHD_III) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WDDominateHD) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WDDominateHD) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WDDominateHD) ||
                            //    victim.health.hediffSet.HasHediff(TorannMagicDefOf.TM_WDDominateHD));
                            infectionFlag = true;
                            float angle = GetAngleFromTo(pawn.Position.ToVector3(), victim.Position.ToVector3());
                            HealthUtility.AdjustSeverity(victim, this.Def, this.parent.Severity);
                            for (int j = 0; j < 3; j++)
                            {
                                TM_MoteMaker.ThrowShadowMote(pawn.DrawPos, map, Rand.Range(.6f, 1f), Rand.Range(50, 80), Rand.Range(1f, 2f), angle + Rand.Range(-20, 20));
                            }
                        }
                    }

                }

            }
        }

        private float GetAngleFromTo(Vector3 from, Vector3 to)
        {
            Vector3 heading = (to - from);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            float directionAngle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
            return directionAngle;
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.parent.Severity < .1f;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.infectionRate, "infectionRate", 240, false);
            Scribe_Values.Look<int>(ref this.lastInfection, "lastInfection", 240, false);
            Scribe_Values.Look<int>(ref this.hediffPwr, "hediffPwr", 0, false);
            Scribe_Values.Look<int>(ref this.infectionRadius, "infectionRadius", 3, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
        }
        
    }
}
