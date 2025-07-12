using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_LowFlight : HediffComp
    {
        private bool initialized = false;
        public List<Graphic> _nakedGraphicCycle = new List<Graphic>();
        public Graphic _nakedGraphicDefault = null;
        public Graphic _nakedGraphicActive = null;
        int cycleIndex = 0;
        public int cycleFrequency = 12;
        public float minDistanceToFly = 10;

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

        public Graphic GetActiveGraphic
        {
            get
            {
                if (this.parent.Severity > 1f)
                {
                    return _nakedGraphicActive;
                }
                return _nakedGraphicDefault;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (base.Pawn != null & base.parent != null && !base.Pawn.Dead && !base.Pawn.Downed && base.Pawn.Map != null)
            {
                if (!initialized)
                {
                    if(this.Pawn.def == TorannMagicDefOf.TM_SpiritCrowR)
                    {
                        _nakedGraphicCycle.Clear();
                        _nakedGraphicCycle.Add(Pawn.kindDef.lifeStages[0].bodyGraphicData.Graphic);
                        _nakedGraphicCycle.Add(Pawn.kindDef.lifeStages[1].bodyGraphicData.Graphic);
                        _nakedGraphicDefault = Pawn.ageTracker.CurKindLifeStage.bodyGraphicData.Graphic;
                        //_nakedGraphicCycle.Add(GraphicDatabase.Get<Graphic_Multi>("PawnKind/HPL_Crow_FlyingDown", ShaderDatabase.Cutout, this.Pawn.Graphic.drawSize, Color.white));
                        _nakedGraphicActive = _nakedGraphicCycle[0];
                    }
                    initialized = true;
                }

                if (this._nakedGraphicActive != null && this._nakedGraphicCycle != null && this._nakedGraphicCycle.Count > 0)
                {
                    if (Find.TickManager.TicksGame % 31 == 0)
                    {
                        if (this.Pawn.jobs != null && this.Pawn.CurJobDef != JobDefOf.Wait_Wander && this.Pawn.CurJobDef != JobDefOf.Wait && this.Pawn.CurJobDef != JobDefOf.GotoWander)
                        {
                            Thing carriedThing = this.Pawn.carryTracker.CarriedThing;
                            LocalTargetInfo target = this.Pawn.CurJob.targetA;
                            if (carriedThing != null)
                            {
                                target = this.Pawn.CurJob.targetB;
                            }
                            if (target != null && (target.Cell - this.Pawn.Position).LengthHorizontal > this.minDistanceToFly)
                            {
                                this.parent.Severity = 1.5f;
                            }
                            else
                            {
                                this.parent.Severity = .5f;
                            }
                        }
                        else
                        {
                            this.parent.Severity = .5f;
                        }
                    }

                    if (parent.Severity >= 1f && Find.TickManager.TicksGame % cycleFrequency == 0)
                    {
                        cycleIndex++;
                        if (cycleIndex >= _nakedGraphicCycle.Count)
                        {
                            cycleIndex = 0;
                        }
                        _nakedGraphicActive = _nakedGraphicCycle[cycleIndex];
                    }
                }
            }
        }
    }
}
