using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic.Ideology
{
    public class HediffComp_BestowMagicClass : HediffComp
    {
        public bool selectableForInspiration = true;
        public bool delayedInspiration = false;
        public bool botchedRitual = false;
        private int ticksTillInspiration = 10;

        //unsaved
        bool shouldRemove = false;

        public override void CompExposeData()
        {
            Scribe_Values.Look<bool>(ref this.selectableForInspiration, "selectableForInspiration", true);
            Scribe_Values.Look<bool>(ref this.delayedInspiration, "delayedInspiration", false);
            Scribe_Values.Look<bool>(ref this.botchedRitual, "botchedRitual", false);
            Scribe_Values.Look<int>(ref this.ticksTillInspiration, "ticksTillInspiration", 10);
            base.CompExposeData();
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


        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned)
            {
                
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;

        public override void CompPostTick(ref float severityAdjustment)
        {

            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn.DestroyedOrNull();
            if (!flag)
            {
                if (base.Pawn.Spawned && this.Pawn.needs != null)
                {
                    if(TM_Calc.IsMagicUser(this.Pawn))
                    {
                        this.shouldRemove = true;
                    }
                    if (base.Pawn.Dead || base.Pawn.Downed)
                    {
                        this.shouldRemove = true;
                    }
                }
                if ((delayedInspiration || botchedRitual) && ClassProgression.ClassEnrollment.HasChosenClass(Pawn))
                {
                    shouldRemove = true;
                    return;
                }
                if(delayedInspiration)
                {
                    ticksTillInspiration--;
                    if(ticksTillInspiration <= 0)
                    {
                        delayedInspiration = false;
                        this.Pawn.mindState.inspirationHandler.TryStartInspiration(TorannMagicDefOf.ID_ArcanePathways);
                    }
                }
                if(botchedRitual)
                {
                    // A botched magical bestow cannot remove a physical gift received earlier.
                    var gift = Pawn.story.traits.GetTrait(TorannMagicDefOf.TM_Gifted);
                    if (gift != null) Pawn.story.traits.RemoveTrait(gift);
                    this.shouldRemove = true;
                }
            }
        }
    }
}
