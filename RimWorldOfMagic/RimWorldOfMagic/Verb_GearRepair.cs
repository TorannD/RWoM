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

            bool containedHediff = false;
            for (int i = 0; i < caster.health.hediffSet.hediffs.Count; i++)
            {
                if (caster.health.hediffSet.hediffs[i].def == TorannMagicDefOf.TM_HediffGearRepair)
                {
                    caster.health.RemoveHediff(caster.health.hediffSet.hediffs[i]);
                    containedHediff = true;
                    break;
                }
            }
            if (!containedHediff)
            {
                HealthUtility.AdjustSeverity(caster, HediffDef.Named("TM_HediffGearRepair"), .5f);
                FleckMaker.ThrowDustPuff(caster.Position, caster.Map, 1f);
            }
            return true;
        }
    }
}
