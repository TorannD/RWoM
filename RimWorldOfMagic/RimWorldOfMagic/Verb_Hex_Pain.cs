using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Hex_Pain : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
            int verVal = TM_Calc.GetSkillVersatilityLevel(CasterPawn, TorannMagicDefOf.TM_Hex);
            if (comp != null && comp.HexedPawns.Count > 0)
            {
                foreach(Pawn p in comp.HexedPawns)
                {
                    TM_Action.DamageEntities(p, null, Rand.Range(2f, 4f) * (1f + (.1f * verVal)), 2, TMDamageDefOf.DamageDefOf.TM_PainDD, caster);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodMist, p.DrawPos, p.Map, .7f, .2f, .2f, .3f, Rand.Range(-50, 50), Rand.Range(.5f, 1f), Rand.Range(-90, 90), Rand.Range(0, 360));
                }
            }
            return true;
        }
    }
}
