using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Enrage : HediffComp
    {
        public bool consumeJoy = false;
        public float reductionFactor = 1f;        

        //unsaved
        float reductionAmount = .04f;
        bool shouldRemove = false;

        public override void CompExposeData()
        {
            Scribe_Values.Look<bool>(ref this.consumeJoy, "consumeJoy", false);
            Scribe_Values.Look<float>(ref this.reductionFactor, "reductionFactor", 1f);
            base.CompExposeData();
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
            if (spawned)
            {

            }
        }


        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;

        public override void CompPostTick(ref float severityAdjustment)
        {
            bool usedEmotions = false;
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn.DestroyedOrNull();
            if (!flag)
            {
                if (base.Pawn.Spawned && this.Pawn.needs != null)
                {
                    if(Find.TickManager.TicksGame % 8 == 0)
                    {
                        DrawEffects();
                    }
                    if (Find.TickManager.TicksGame % 155 == 0)
                    {
                        float tickCost = (reductionAmount * reductionFactor);
                        for (int i = 0; i < this.Pawn.needs.AllNeeds.Count; i++)
                        {
                            Need n = this.Pawn.needs.AllNeeds[i];
                            if(consumeJoy && n.def == NeedDefOf.Joy && n.CurLevel >= tickCost)
                            {
                                n.CurLevel -= tickCost;
                                usedEmotions = true;
                                break;
                            }
                            if(n.def.defName == "Mood" && n.CurLevel >= tickCost)
                            {
                                n.CurLevel -= tickCost;
                                usedEmotions = true;
                                break;
                            }
                        }
                        if (!usedEmotions)
                        {
                            if (this.parent.Severity >= tickCost)
                            {
                                severityAdjustment = (-1f * tickCost);
                            }
                            else
                            {
                                shouldRemove = true;
                            }
                        }
                    }
                    if(base.Pawn.Dead || base.Pawn.Downed)
                    {
                        this.shouldRemove = true;
                    }
                }
            }
        }

        public void DrawEffects()
        {
            Vector3 headOffset = this.Pawn.DrawPos;
            headOffset.z += .4f;
            float throwAngle = Rand.Range(-20, 20);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, headOffset, this.Pawn.Map, Rand.Range(.4f, .6f), .12f, .01f, .1f, 0, .5f, throwAngle, Rand.Range(0, 360));
            //if (this.Pawn.Rotation == Rot4.East)
            //{
                
            //}
            //else if (this.Pawn.Rotation == Rot4.West)
            //{

            //    headOffset.z += .65f * sizeOffset;
            //    headOffset.x += -.4f * sizeOffset;
            //    float throwAngle = Rand.Range(5, 20);
            //    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Demon_Flame, headOffset, this.Pawn.Map, Rand.Range(.5f, .8f) * sizeOffset, Rand.Range(.12f, .18f), .01f, Rand.Range(.1f, .15f), 0, Rand.Range(2.4f, 2.8f) * sizeOffset, throwAngle, Rand.Range(0, 360));
            //}
            //else if (this.Pawn.Rotation == Rot4.South)
            //{
            //    headOffset.z += .75f * sizeOffset;
            //    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Demon_Flame, headOffset, this.Pawn.Map, Rand.Range(.5f, .8f) * sizeOffset, Rand.Range(.12f, .18f), .01f, Rand.Range(.1f, .15f), 0, Rand.Range(2.4f, 2.8f) * sizeOffset, Rand.Range(-30, 30), Rand.Range(0, 360));
            //}
            //else
            //{
            //    headOffset.z += .75f * sizeOffset;
            //    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Demon_Flame, headOffset, this.Pawn.Map, Rand.Range(.5f, .8f) * sizeOffset, Rand.Range(.12f, .18f), .01f, Rand.Range(.1f, .15f), 0, Rand.Range(2.4f, 2.8f) * sizeOffset, Rand.Range(-30, 30), Rand.Range(0, 360));
            //}
        }

    }
}
