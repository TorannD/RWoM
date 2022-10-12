using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_HeavyBlow : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = currentTarget.Thing as Pawn;

            if (pawn == null || pawn.Dead) return true;

            bool hadHeavyBlow = false;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def == TorannMagicDefOf.TM_HediffHeavyBlow)
                {
                    pawn.health.RemoveHediff(hediff);
                    hadHeavyBlow = true;
                    break;
                }
            }

            if (hadHeavyBlow) return true;

            //if (pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Wayfarer))
            //{
            int lvl = pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_FieldTraining.First(mps => mps.label == "TM_FieldTraining_pwr").level;
            HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffHeavyBlow, .95f + .19f * lvl);
            FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
            //}
            //else
            //{
            //    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_HediffHeavyBlow, .5f);
            //    FleckMaker.ThrowDustPuff(pawn.Position, pawn.Map, 1f);
            //}
            return true;
        }
    }
}
