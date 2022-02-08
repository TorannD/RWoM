using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic.Golems
{
    public class PawnColumnWorker_GolemRestPercent : PawnColumnWorker
    {

        private const float DefaultRestPercent = .1f;

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 120);
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(150, GetMinWidth(table), GetMaxWidth(table));
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            return Mathf.Max(base.GetMinHeaderHeight(table), 65);
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
            Rect rectHeader = new Rect(rect.x, rect.y + (rect.height - 65f), Mathf.Min(rect.width, 360f), 32f);
        }

        protected override string GetHeaderTip(PawnTable table)
        {
            return base.GetHeaderTip(table) + "TM_GolemAwakenMinimumDesc".Translate();
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if(pawn.Faction == Faction.OfPlayer)
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                if(cg != null)
                {
                    Text.Font = GameFont.Tiny;
                    Rect rectAwakeMinimum = new Rect(rect.x, rect.y, Mathf.Min(rect.width, 140), 32f);
                    cg.energyPctShouldAwaken = Widgets.HorizontalSlider(rectAwakeMinimum, cg.energyPctShouldAwaken, .1f, 1f, false, "TM_GolemAwakenMinimum".Translate() + " " + TM_GolemUtility.ShouldAwkenString(cg.energyPctShouldAwaken), "", "", .01f);
                }
            }
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        private float GetValueToCompare(Pawn pawn)
        {
            return pawn.TryGetComp<CompGolem>().threatRange;
        }
    }
}
