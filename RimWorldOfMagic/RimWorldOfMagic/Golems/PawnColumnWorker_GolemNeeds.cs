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
    public class PawnColumnWorker_GolemNeeds : PawnColumnWorker
    {

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 80);
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(120, GetMinWidth(table), GetMaxWidth(table));
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            return Mathf.Max(base.GetMinHeaderHeight(table), 65);
        }

        //public override void DoHeader(Rect rect, PawnTable table)
        //{
        //    base.DoHeader(rect, table);
        //    Text.Anchor = TextAnchor.LowerCenter;
        //    Rect rectHeader = new Rect(rect.x + 29f, rect.y, Mathf.Min(rect.width, 360f), 32f);
        //    Widgets.Label(rectHeader, "Energy");
        //    Text.Anchor = TextAnchor.UpperLeft;
        //}

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if(pawn.Faction == Faction.OfPlayer)
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                if(cg != null)
                {
                    float fillPct = 0f;
                    bool spawned = pawn.Spawned;
                    if (pawn.needs == null)
                        return;

                    if (spawned)
                    {
                        Need need = pawn.needs.TryGetNeed(TorannMagicDefOf.TM_GolemEnergy);
                        if (need == null)
                            return;
                        fillPct = need.CurLevelPercentage;
                    }
                    else
                    {
                        Building_TMGolemBase gb = pawn.ParentHolder as Building_TMGolemBase;
                        if (gb == null)
                            return;
                        if (gb.Energy == null)
                            return;
                        fillPct = gb.Energy.StoredEnergyPct;
                    }

                    float barHeight = 14f;
                    float barWidth = barHeight + 15f;
                    if (rect.height < 50f)
                    {
                        barHeight *= Mathf.InverseLerp(0f, 50f, rect.height);
                    }

                    Text.Font = (rect.height <= 55f) ? GameFont.Tiny : GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Rect rect3 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height / 2f);
                    rect3 = new Rect(rect3.x + barWidth, rect3.y, rect3.width - barWidth * 2f, rect3.height - barHeight);

                    Widgets.FillableBar(rect3, fillPct);

                    Text.Font = GameFont.Small;
                }
            }
        }
    }
}
