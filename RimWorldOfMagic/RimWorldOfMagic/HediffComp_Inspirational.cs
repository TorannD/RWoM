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

        public string labelCap => base.Def.LabelCap;
        public string label => base.Def.label;

        private void Initialize()
        {
            if (!Pawn.Spawned) return;
            MagicPowerSkill pwr = Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.First(mps => mps.label == "TM_Inspire_pwr");
            MagicPowerSkill ver = Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.First(mps => mps.label == "TM_Inspire_ver");
            pwrVal = pwr.level;
            verVal = ver.level;
            initializing = false;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn != null)
            {
                if (initializing)
                {
                    Initialize();
                }
            }
            if(Find.TickManager.TicksGame % 600 == 0)
            {
                MagicPowerSkill pwr = Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.First(mps => mps.label == "TM_Inspire_pwr");
                MagicPowerSkill ver = Pawn.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_Inspire.First(mps => mps.label == "TM_Inspire_ver");
                pwrVal = pwr.level;
                verVal = ver.level;
            }
            Map map = Pawn?.Map;
            int tickTimer = 1000 - (pwrVal * 100);
            if (Find.TickManager.TicksGame % tickTimer != 0 || map == null) return;

            CellRect cellRect = CellRect.CenteredOn(Pawn.Position, 3);
            cellRect.ClipInsideMap(map);

            IntVec3 curCell = cellRect.RandomCell;
            Pawn inspiredPawn = curCell.GetFirstPawn(map);
            if (inspiredPawn == null
                || !inspiredPawn.IsColonist
                || !inspiredPawn.RaceProps.Humanlike
                || inspiredPawn.Inspired
                || inspiredPawn == Pawn) return;

            InspirationDef id = TM_Calc.GetRandomAvailableInspirationDef(inspiredPawn);
            bool flag1 = id.defName == "ID_MiningFrenzy" || id.defName == "ID_FarmingFrenzy" || id == TorannMagicDefOf.ID_ArcanePathways;
            bool flag2 = id.defName == "ID_Introspection" || id.defName == "ID_Outgoing";
            bool flag3 = id.defName == "ID_ManaRegen" || id.defName == "ID_Champion";
            Vector3 drawPosAbove = inspiredPawn.DrawPos;
            drawPosAbove.z += .5f;
            switch (verVal)
            {
                case 0 when !flag1 && !flag2 && !flag3:
                    inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, .5f);
                    break;
                case 1 when !flag2 && !flag3:
                    inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, .7f);
                    break;
                case 2 when !flag3:
                    inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                    break;
                case 3:
                    inspiredPawn.mindState.inspirationHandler.TryStartInspiration(id);
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                    TM_MoteMaker.ThrowExclamationMote(drawPosAbove, inspiredPawn.Map, Rand.Range(.5f, .7f));
                    break;
                default:
                    //failed inspiration due to type
                    break;
            }
        }
    }
}
