using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_BurningFury : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            bool flag = pawn != null && !pawn.Dead;
            if (flag)
            {
                if(pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_BurningFuryHD))
                {
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def == TorannMagicDefOf.TM_BurningFuryHD)
                        {
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, TorannMagicDefOf.TM_BurningFuryHD, 1f);
                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, 1f);
                }
            }
            return true;
        }
    }
}
