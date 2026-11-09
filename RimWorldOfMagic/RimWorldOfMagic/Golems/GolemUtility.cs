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
                    FloatMenuOptionProvider_DraftedMove.PawnGotoAction(clickCell, pawn, RCellFinder.BestOrderedGotoDestNear(curLoc, pawn));
                    //FloatMenuMakerMap.PawnGotoAction(clickCell, pawn, RCellFinder.BestOrderedGotoDestNear(curLoc, pawn));
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
                    var list = MasterOptions(cg);
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                TooltipHandler.TipRegion(inRect, "TM_GolemMasterDesc".Translate());
            }
        }

        internal static List<FloatMenuOption> MasterOptions(CompGolem golem)
        {
            var list = new List<FloatMenuOption> { new FloatMenuOption("None".Translate(), () => golem.pawnMaster = null) };
            foreach (var master in TM_Calc.GolemancersInFaction(golem.PawnGolem.Faction))
            {
                if (master == golem.pawnMaster) continue;
                var body = ClassProgression.GolemancerLifecycle.MasterBody(master);
                string label = body == master ? master.LabelShortCap.ToString() : $"{master.LabelShortCap} ({body.LabelShortCap})";
                list.Add(new FloatMenuOption(label, () => {
                    if (TM_Calc.GolemancersInFaction(golem.PawnGolem.Faction).Contains(master))
                        golem.pawnMaster = master;
                }));
            }
            return list;
        }
    }
}
