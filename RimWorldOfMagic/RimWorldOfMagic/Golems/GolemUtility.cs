using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public static class GolemUtility
    {
        public static FloatMenuOption GotoLocationOption(IntVec3 clickCell, Pawn pawn, bool suppressAutoTakeableGoto)
        {
            if (suppressAutoTakeableGoto)
            {
                return null;
            }
            IntVec3 curLoc = CellFinder.StandableCellNear(clickCell, pawn.Map, 2.9f);
            if (curLoc.IsValid && curLoc != pawn.Position)
            {
                if (!pawn.CanReach(curLoc, PathEndMode.OnCell, Danger.Deadly))
                {
                    return new FloatMenuOption("CannotGoNoPath".Translate(), null);
                }
                Action action = delegate
                {
                    FloatMenuMakerMap.PawnGotoAction(clickCell, pawn, RCellFinder.BestOrderedGotoDestNear(curLoc, pawn));
                };
                return new FloatMenuOption("GoHere".Translate(), action, MenuOptionPriority.GoHere)
                {
                    autoTakeable = true,
                    autoTakeablePriority = 10f
                };
            }
            return null;
        }

        public static void MasterButton(Rect inRect, string masterName, CompGolem cg)
        {
            if (cg != null)
            {
                if (Widgets.ButtonText(inRect, masterName))
                {
                    List<string> tmpPawns = new List<string>();
                    tmpPawns.Add("None");
                    foreach (Pawn p in TM_Calc.GolemancersInFaction(cg.PawnGolem.Faction).Where((Pawn x) => x != cg.pawnMaster))
                    {
                        tmpPawns.Add(p.LabelShort);
                    }
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (string pawnName in tmpPawns)
                    {
                        string text = pawnName;
                        FloatMenuOption item = new FloatMenuOption(text, delegate
                        {
                            if (pawnName != masterName)
                            {
                                cg.pawnMaster = TM_Calc.GolemancersInFaction(cg.PawnGolem.Faction).FirstOrDefault((Pawn p) => p.LabelShort == pawnName);
                            }
                            if (pawnName == "None")
                            {
                                cg.pawnMaster = null;
                            }
                        });
                        list.Add(item);
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                TooltipHandler.TipRegion(inRect, "TM_GolemMasterDesc".Translate());
            }
        }
    }
}
