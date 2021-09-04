using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TorannMagic.Ideology
{
    public class JobDriver_CastIdeoAbility : JobDriver_CastVerbOnce
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
			this.FailOnDespawnedOrNull(TargetIndex.A);
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				pawn.pather.StopDead();
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return toil;
			Toil toil2 = Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
			if (job.ability != null && job.ability.def.showCastingProgressBar && job.verbToUse != null)
			{
				toil2.With_TM_Effects(TorannMagicDefOf.Mote_ManaVortex, 1, 3f, Rand.Range(.05f, .1f), Rand.Range(.05f, .1f), .3f, -100, 0, 0, Rand.Range(0,360));
				toil2.WithProgressBar(TargetIndex.A, () => job.verbToUse.WarmupProgress);
			}
			yield return toil2;
		}
    }
}
