using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic
{
    class FloatMenuOptionProvider_ArcaneForge : FloatMenuOptionProvider
    {

        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            RimWorld.JobGiver_Work jobGiver_Work = pawn.thinker.TryGetMainTreeThinkNode<RimWorld.JobGiver_Work>();
            if (jobGiver_Work != null)
            {
                foreach (Thing item in pawn.Map.thingGrid.ThingsAt(clickedThing.Position))
                {
                    if (item is Building && (item.def == TorannMagicDefOf.TableArcaneForge))
                    {
                        CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                        if (comp != null && comp.Mana != null && comp.Mana.CurLevel < .5f)
                        {

                            string text = null;
                            text = "TM_InsufficientManaForJob".Translate((comp.Mana.CurLevel * 100).ToString("0.##"));
                            return new FloatMenuOption("TM_InsufficientManaForJob".Translate((comp.Mana.CurLevel * 100).ToString("0.##")), null);                            
                        }
                    }
                }
            }
            return base.GetSingleOptionFor(clickedThing, context);
        }
    }
}
