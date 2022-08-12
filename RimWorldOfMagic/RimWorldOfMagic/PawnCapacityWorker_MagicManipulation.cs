using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    public class PawnCapacityWorker_MagicManipulation : PawnCapacityWorker
    {
        public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            CompAbilityUserMagic comp = diffSet.pawn.GetCompAbilityUserMagic();
            float num = 0;
            if(comp != null && comp.IsMagicUser && !comp.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            {
                num = 1;
            }
            return num * Mathf.Min(diffSet.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness), 1f);
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return body.HasPartWithTag(BodyPartTagDefOf.ConsciousnessSource);
        }
    }
}
