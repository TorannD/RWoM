using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic
{
    internal class JobDriver_SummonDemon : JobDriver
    {
        private int age = -1;
        public int durationTicks = 1200;

        CompAbilityUserMagic comp;
        Pawn markedPawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil doFor = new Toil()
            {
                initAction = () =>
                {
                    this.comp = this.pawn.GetCompAbilityUserMagic();
                    this.markedPawn = comp.soulBondPawn;
                },
                tickAction = () =>
                {
                    if(!markedPawn.Spawned)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (age > durationTicks)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if (Find.TickManager.TicksGame % 12 == 0)
                    {
                        TM_MoteMaker.ThrowCastingMote(this.pawn.DrawPos, this.pawn.Map, Rand.Range(1.2f, 2f));
                        TM_MoteMaker.ThrowShadowMote(markedPawn.DrawPos, markedPawn.Map, Rand.Range(.8f, 1.2f), Rand.Range(-200, 200), Rand.Range(1, 2), Rand.Range(1.5f, 2f));
                    }
                    if(Find.TickManager.TicksGame % 6 ==0)
                    {
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Siphon, markedPawn.DrawPos, markedPawn.Map, Rand.Range(.15f, .3f), Rand.Range(.2f, .4f), Rand.Range(.1f, .2f), Rand.Range(.3f, .5f), Rand.Range(-300, 300), Rand.Range(.5f, 3f), Rand.Range(-90, 90), 0);
                    }
                    age++;
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            doFor.defaultDuration = this.durationTicks;
            doFor.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Downed)
                {
                    return 1f;
                }
                return 1f - (float)doFor.actor.jobs.curDriver.ticksLeftThisToil / this.durationTicks;

            }, false, 0f);
            yield return doFor;         
        }
    }
}