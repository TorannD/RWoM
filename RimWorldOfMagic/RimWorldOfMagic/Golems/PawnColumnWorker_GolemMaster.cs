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
    public class PawnColumnWorker_GolemMaster : PawnColumnWorker
    {

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 120);
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(140, GetMinWidth(table), GetMaxWidth(table));
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            return Mathf.Max(base.GetMinHeaderHeight(table), 65);
        }

        protected override string GetHeaderTip(PawnTable table)
        {
            return base.GetHeaderTip(table) + "TM_GolemMasterDesc".Translate();
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if(pawn.Faction == Faction.OfPlayer)
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                if(cg != null)
                {
                    Text.Font = GameFont.Tiny;
                    string pawnMasterName = "None";
                    if (cg.pawnMaster != null)
                    {
                        pawnMasterName = cg.pawnMaster.Label;
                    }
                    GolemUtility.MasterButton(rect, pawnMasterName, cg);
                    Text.Font = GameFont.Small;
                }
            }
        }
    }
}
