using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic.Golems
{
    internal class JobDriver_GolemSelfTend : JobDriver
    {
        private int age = -1;
        public int durationTicks = 60;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = Toils_General.Wait(durationTicks);
            yield return wait;
            Toil tend = new Toil()
            {
                initAction = () =>
                {
                    foreach(Hediff hd in pawn.health.hediffSet.hediffs)
                    {
                        if(hd.Bleeding && hd.TendableNow() && !hd.IsTended())
                        {
                            TM_Action.TendWithoutNotice(hd, 1, 1);
                            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
                            break;
                        }
                    }                    
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return tend;
        }
    }
}