using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace TorannMagic.Ideology
{
    public class HediffComp_MagicSeverence : HediffComp
    {
        public bool selectableForRetaliation = true;
        public bool delayedMindburn = false;
        private int ticksTillDeath = 10;

        //unsaved
        bool shouldRemove = false;

        public override void CompExposeData()
        {
            Scribe_Values.Look<bool>(ref this.selectableForRetaliation, "selectableForRetaliation", true);
            Scribe_Values.Look<bool>(ref this.delayedMindburn, "delayedMindburn", false);
            Scribe_Values.Look<int>(ref this.ticksTillDeath, "ticksTillDeath", 10);
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
                if(delayedMindburn)
                {
                    ticksTillDeath--;
                    if(ticksTillDeath <= 0)
                    {
                        TM_Action.KillPawnByMindBurn(this.Pawn);
                    }
                }
            }
        }
    }
}
