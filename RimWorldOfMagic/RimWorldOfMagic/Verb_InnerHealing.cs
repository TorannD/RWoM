using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_InnerHealing : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            if (pawn == null || pawn.Dead) return true;

            bool hadInnerHealing = false;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def == TorannMagicDefOf.TM_HediffInnerHealing)
                {
                    pawn.health.RemoveHediff(hediff);
                    hadInnerHealing = true;
                    break;
                }
            }
            if (!hadInnerHealing)
            {
                HealthUtility.AdjustSeverity(pawn, HediffDef.Named("TM_HediffInnerHealing"), .5f );
                FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
            }
            return true;
        }
    }
}
