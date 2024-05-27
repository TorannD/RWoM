using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_GearRepair : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            if (caster == null || caster.Dead) return true;

            if (pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffGearRepair))
            {
                Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_HediffGearRepair);
                pawn.health.RemoveHediff(hd);
            }
            else
            {
                HealthUtility.AdjustSeverity(caster, TorannMagicDefOf.TM_HediffGearRepair, .5f);
                FleckMaker.ThrowDustPuff(caster.Position, pawn.Map, 1f);
            }

            return true;
        }
    }
}
