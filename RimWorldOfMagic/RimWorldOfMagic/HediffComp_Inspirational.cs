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
        private bool initializing = true;
        private int pwrVal = 0;
        private int verVal = 0;

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
                MagicPowerSkill pwr = this.Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Inspire_pwr");
                MagicPowerSkill ver = this.Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Inspire_ver");
                this.pwrVal = pwr.level;
                this.verVal = ver.level;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn.DestroyedOrNull()) return;

            if (initializing)
            {
                initializing = false;
                this.Initialize();
            }
            
            if(Find.TickManager.TicksGame % 600 == 0)
            {
                MagicPowerSkill pwr = this.Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.First((MagicPowerSkill x) => x.label == "TM_Inspire_pwr");
                MagicPowerSkill ver = this.Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.First((MagicPowerSkill x) => x.label == "TM_Inspire_ver");
                this.pwrVal = pwr.level;
                this.verVal = ver.level;
            }
            Map map = base.Pawn.Map;
            int tickTimer = 1000 - (pwrVal * 100);
            if (Find.TickManager.TicksGame % tickTimer == 0 && map != null)
            {
                CellRect cellRect = CellRect.CenteredOn(base.Pawn.Position, 3);
                cellRect.ClipInsideMap(map);

                IntVec3 curCell = cellRect.RandomCell;
                Pawn inspiredPawn = curCell.GetFirstPawn(map);
                if(inspiredPawn != null && inspiredPawn.IsColonist && inspiredPawn.RaceProps.Humanlike && !inspiredPawn.Inspired && inspiredPawn != this.Pawn && inspiredPawn.story != null && inspiredPawn.story.traits != null && inspiredPawn.workSettings != null)
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
                            inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, .5f);
                        }
                        else if (verVal == 1 && !flag2 && !flag3)
                        {
                            inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, .7f);
                        }
                        else if (verVal == 2 && !flag3)
                        {
                            inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                            TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                        }
                        else if (verVal == 3)
                        {
                            inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
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
