using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;
using TorannMagic.ClassProgression;

namespace TorannMagic
{
    internal class JobDriver_SummonDemon : JobDriver
    {
        private int age;
        public int durationTicks = 1200;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref age,"TM_demonChannelAge");
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var channel = ToilMaker.MakeToil("Maintain demonic anchor");
            channel.defaultCompleteMode = ToilCompleteMode.Never;
            channel.tickAction = () =>
            {
                var offering = job.targetA.Pawn;
                if (!DemonRitual.TryChannel(pawn,offering,out _,out _))
                {
                    bool formed = offering?.ParentHolder is Projectile_SummonDemon pact && pact.HoldsFor(pawn,offering);
                    EndJobWith(formed ? JobCondition.Succeeded : JobCondition.Incompletable);
                    return;
                }
                if (++age >= durationTicks) { EndJobWith(JobCondition.Succeeded); return; }
                if (Find.TickManager.TicksGame % 12 == 0)
                {
                    TM_MoteMaker.ThrowCastingMote(pawn.DrawPos,pawn.Map,Rand.Range(1.2f,2f));
                    TM_MoteMaker.ThrowShadowMote(offering.DrawPos,offering.Map,Rand.Range(.8f,1.2f),Rand.Range(-200,200),Rand.Range(1,2),Rand.Range(1.5f,2f));
                }
                if (Find.TickManager.TicksGame % 6 == 0)
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, offering.DrawPos, offering.Map, Rand.Range(.15f, .3f), Rand.Range(.2f, .4f), Rand.Range(.1f, .2f), Rand.Range(.3f, .5f), Rand.Range(-300, 300), Rand.Range(.5f, 3f), Rand.Range(-90, 90), 0);
            };
            channel.WithProgressBar(TargetIndex.A,() => Mathf.Clamp01(age/(float)durationTicks),false);
            yield return channel;
        }
    }
}
