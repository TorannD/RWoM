using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    class HediffComp_Inspirational : HediffComp
    {
        private int lastCheck = -1;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastCheck, "TM_lastInspirationCheck", -1);
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

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            int now = Find.TickManager.TicksGame;
            if (now % 100 != 0 || now == lastCheck) return;
            var body = ClassProgression.BardTrials.InspirationBody(Pawn);
            if (body == null) return;
            lastCheck = now;
            var skills = Pawn.GetCompAbilityUserMagic()?.MagicData.MagicPowerSkill_Inspire;
            int pwrVal = skills?.FirstOrDefault(s => s.label == "TM_Inspire_pwr")?.level ?? 0;
            int verVal = skills?.FirstOrDefault(s => s.label == "TM_Inspire_ver")?.level ?? 0;
            int interval = Math.Max(100, 1000 - pwrVal * 100);
            Map map = body.Map;
            if (now % interval == 0)
            {
                var cellRect = CellRect.CenteredOn(body.Position, 3).ClipInsideMap(map);

                IntVec3 curCell = cellRect.RandomCell;
                Pawn inspiredPawn = curCell.GetFirstPawn(map);
                if(inspiredPawn != null && inspiredPawn.IsColonist && inspiredPawn.RaceProps.Humanlike && !inspiredPawn.Inspired && inspiredPawn != body && inspiredPawn.story != null && inspiredPawn.story.traits != null && inspiredPawn.workSettings != null)
                {
                    InspirationDef id = TM_Calc.GetRandomAvailableInspirationDef(inspiredPawn);
                    if (id != null)
                    {
                        bool flag1 = id.defName == "ID_MiningFrenzy" || id.defName == "ID_FarmingFrenzy" || id == TorannMagicDefOf.ID_ArcanePathways;
                        bool flag2 = id.defName == "ID_Introspection" || id.defName == "ID_Outgoing";
                        bool flag3 = id.defName == "ID_ManaRegen" || id.defName == "ID_Champion";
                        Vector3 drawPosAbove = inspiredPawn.DrawPos;
                        drawPosAbove.z += .5f;
                        if (verVal == 0 && !flag1 && !flag2 && !flag3)
                        {
                            ClassProgression.BardTrials.Inspire(this.parent, inspiredPawn, id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, .5f);
                        }
                        else if (verVal == 1 && !flag2 && !flag3)
                        {
                            ClassProgression.BardTrials.Inspire(this.parent, inspiredPawn, id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, .7f);
                        }
                        else if (verVal == 2 && !flag3)
                        {
                            ClassProgression.BardTrials.Inspire(this.parent, inspiredPawn, id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                        }
                        else if (verVal == 3)
                        {
                            ClassProgression.BardTrials.Inspire(this.parent, inspiredPawn, id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                        }
                        else
                        {
                            //failed inspiration due to type
                        }
                    }
                }
            }
        }        
    }
}
