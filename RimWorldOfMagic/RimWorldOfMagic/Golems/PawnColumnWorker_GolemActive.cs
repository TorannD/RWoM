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
    public class PawnColumnWorker_GolemActive : PawnColumnWorker_Checkbox
    {        

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if(pawn.Faction == Faction.OfPlayer)
            {
                CompGolem cg = pawn.TryGetComp<CompGolem>();
                if(cg != null)
                {
                    int num = (int)((rect.width - 24f) / 2f);
                    int num2 = Mathf.Max(3, 0);
                    Vector2 vector = new Vector2(rect.x + (float)num, rect.y + (float)num2);
                    Rect rect2 = new Rect(vector.x, vector.y, 24f, 24f);
                    bool checkOn = GetValue(pawn);
                    bool flag = checkOn;
                    Widgets.Checkbox(vector, ref flag, 24f, disabled: false);
                    if (Mouse.IsOver(rect2))
                    {
                        string tip = GetTip(pawn);
                        if (!tip.NullOrEmpty())
                        {
                            TooltipHandler.TipRegion(rect2, tip);
                        }
                    }
                    if(checkOn != flag)
                    {
                        SetValue(pawn, true, table);
                    }
                }
            }
        }

        protected override string GetTip(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                return "TM_ActivatesGolem_Time".Translate();
            }
            else
            {
                return "TM_DeActivatesGolem_Time".Translate();
            }
        }

        protected override bool GetValue(Pawn pawn)
        {
            return pawn.Spawned;
        }

        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            CompGolem cg = pawn.TryGetComp<CompGolem>();
            if (cg != null)
            {
                if(pawn.Spawned)
                {
                    cg.shouldDespawn = true;
                }
                else
                {
                    Building_TMGolemBase gb = cg.parent.ParentHolder as Building_TMGolemBase;
                    if(gb != null)
                    {
                        gb.activating = true;
                    }
                }
            }
        }
    }
}
