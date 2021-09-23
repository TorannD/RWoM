using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;


namespace TorannMagic.Golems
{
    internal class JobDriver_GolemDespawn : JobDriver
    {
        private int age = -1;
        public int durationTicks = 60;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {        
            Toil wait = Toils_General.WaitWith(TargetIndex.A, durationTicks, true, true);
            yield return wait;
            Toil despawn = new Toil()
            {
                initAction = () =>
                {
                    pawn.TryGetComp<CompGolem>().DeSpawnGolem();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return despawn;
        }
    }
}