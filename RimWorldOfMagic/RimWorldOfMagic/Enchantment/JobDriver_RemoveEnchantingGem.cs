using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;

namespace TorannMagic.Enchantment
{
    public class JobDriver_RemoveEnchantingGem : JobDriver
    {
        private int useDuration = -1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.useDuration, "useDuration", 0, false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);  //probably remove, drop should be immediate at location
            Toil drop = new Toil();
            drop.initAction = delegate
            {
                Pawn actor = drop.actor;
                CompEnchant comp = actor.TryGetComp<CompEnchant>();
                comp.enchantingContainer.TryDropAll(actor.Position, actor.Map, ThingPlaceMode.Near);
            };
            drop.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return drop;
        }
    }
}
